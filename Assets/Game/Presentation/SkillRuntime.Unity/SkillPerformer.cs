using System;
using System.Collections.Generic;
using System.Globalization;
using Evermeadow.Core.CharacterController;
using Evermeadow.Core.Skills;
using Evermeadow.Foundation;
using Evermeadow.Presentation.Vfx;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Evermeadow.Presentation.SkillRuntime.Unity
{
    /// <summary>
    /// Shows a running skill on its actor — the player or a creature (<see cref="ISkillActor"/>; minimal runtime, user
    /// decisions 2026-11-14):
    /// <list type="bullet">
    /// <item><b>Animation</b> plays the clip of the animation slot <c>AnimationType</c> (attack1, skill2… — the clip of the
    /// actor's controller / look named like the slot; older names are matched by ending / containing it) on a PlayableGraph laid over the Animator, at <c>TimeScale</c> ×
    /// game clock scale; a non-looping clip holds its last frame; <see cref="ReturnToIdle"/> removes the graph and puts the
    /// Animator back in <c>idle</c>.</item>
    /// <item><b>Effect</b> spawns <c>PrefabName</c> from the VFX catalog at the pivot (owner + <c>PivotOffset</c> mirrored by
    /// the facing, or <c>PivotPosition</c>), mirrored for a left facing, scaled, attached to the owner for
    /// <c>BindType AttachToTarget</c>.</item>
    /// <item><b>Hit</b> draws its area (Arc / Box from <c>SubValue</c> × <c>AreaScale</c>) — no damage until Combat exists.</item>
    /// <item>Other nodes (Move, MoveControl, LookAt, CameraAction, Knockback, Buff) and Search are logged only; Search finds
    /// nothing.</item>
    /// </list>
    /// Single-thread only; dispose to free the graph.
    /// </summary>
    /// <example>
    /// <code>
    /// var performer = new SkillPerformer(actor, fx.Vfx, fx.Areas);
    /// var run = new SkillRun(def, performer);
    /// run.Start(clock.ScaledNow);
    /// void Update() { run.Tick(clock.ScaledNow); performer.Tick(clock.Scale); }
    /// </code>
    /// </example>
    public sealed class SkillPerformer : ISkillPerformer, IDisposable
    {
        private static readonly int IdleState = Animator.StringToHash("idle");

        private readonly ISkillActor _actor;
        private readonly Transform _owner;
        private readonly Animator _animator;
        private readonly VfxSpawner _vfx;
        private readonly SkillAreaDebug _area;
        private readonly HashSet<string> _warned = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<int, SkillVfx> _effects = new Dictionary<int, SkillVfx>();

        /// <summary>One line per node event (start, stop, skipped and why) for dev tools.</summary>
        public event Action<string> Trace;

        private PlayableGraph _graph;
        private AnimationClipPlayable _clip;
        private bool _loop;
        private double _length, _timeScale = 1;

        /// <summary>Creates a performer for <paramref name="actor"/>. <paramref name="vfx"/> and <paramref name="area"/> may be null.</summary>
        public SkillPerformer(ISkillActor actor, VfxSpawner vfx, SkillAreaDebug area)
        {
            _actor = actor ?? throw new ArgumentNullException(nameof(actor));
            _owner = actor.Transform;
            _animator = actor.Animator;
            _vfx = vfx;
            _area = area;
        }

        /// <inheritdoc />
        public double Begin(SkillNodeData node, double suggestedSeconds)
        {
            // One failing node must not stop the rest of the tree: report it and let the run go on.
            try
            {
                switch (node.Kind)
                {
                    case SkillNodeKind.Animation: return PlayAnimation(node);
                    case SkillNodeKind.Effect: return SpawnEffect(node, suggestedSeconds);
                    case SkillNodeKind.Hit:
                        if (DevOptions.ShowHitArea && !DrawArea(node, suggestedSeconds, SkillAreaDebug.HitColour))
                            Say($"✖ {node.Id}:Hit vùng kích thước 0 (SubValue {node.Get("SubValue")}: Arc cần số thứ hai = bán kính > 0) → không vẽ");
                        Say($"▶ {node.Id}:Hit {(node.Get("AreaType") == nameof(SkillAreaType.Box) ? "Box" : "Arc")} {node.Get("SubValue")} · {Secs(suggestedSeconds)} (chưa gây sát thương)");
                        return suggestedSeconds;
                    default:
                        WarnOnce(node.Kind.ToString(), $"node {node.Kind} chưa được bộ chạy tối thiểu hỗ trợ (chỉ chạy thời gian, không làm gì).");
                        Say($"▶ {node.Id}:{node.Kind} (chưa hỗ trợ, chỉ tính thời gian) · {Secs(suggestedSeconds)}");
                        return suggestedSeconds;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Say($"✖ {node.Id}:{node.Kind} lỗi: {e.Message}");
                return suggestedSeconds;
            }
        }

        /// <inheritdoc />
        public void Finish(SkillNodeData node, bool interrupted)
        {
            Say($"{(interrupted ? "■" : "✓")} {node.Id}:{node.Kind} {(interrupted ? "dừng theo kỹ năng" : "xong")}");
            if (node.Kind == SkillNodeKind.Effect && _effects.TryGetValue(node.Id, out SkillVfx fx))
            {
                _effects.Remove(node.Id);
                if (interrupted && fx != null && fx.IsPlaying) _vfx?.Stop(fx);   // aborted skill: cut its effects too
            }
        }

        /// <inheritdoc />
        public bool Search(SkillNodeData node)
        {
            WarnOnce("Search", "node Search chưa có hệ tìm creature: luôn coi là không tìm thấy (đi nhánh \"Khi không tìm thấy\").");
            if (DevOptions.ShowSearchArea) DrawArea(node, 0.3, SkillAreaDebug.SearchColour);
            Say($"▶ {node.Id}:Search → không tìm thấy (chưa có hệ tìm creature)");
            return false;
        }

        /// <inheritdoc />
        public void ReturnToIdle()
        {
            Say("■ kỹ năng kết thúc → animation về idle");
            StopGraph();
            if (_animator != null && _animator.isActiveAndEnabled && _animator.HasState(0, IdleState)) _animator.Play(IdleState, 0, 0f);
        }

        /// <summary>Every frame: keeps the clip at the game clock speed, holds the last frame or loops.</summary>
        public void Tick(double clockScale)
        {
            if (!_graph.IsValid()) return;
            double t = _clip.GetTime();
            if (_length > 0 && t >= _length)
            {
                if (_loop) _clip.SetTime(t % _length);
                else
                {
                    _clip.SetTime(_length - 1e-4);   // hold the last frame until the skill ends or another Animation starts
                    _clip.SetSpeed(0);
                    return;
                }
            }
            _clip.SetSpeed(_timeScale * clockScale);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            StopGraph();
            _effects.Clear();
        }

        // ------------------------------------------------------------------ nodes

        private double PlayAnimation(SkillNodeData node)
        {
            string name = node.Get("AnimationType").Trim();
            bool loop = node.Get("IsLoop") == "true";
            if (name.Length == 0)
            {
                WarnOnce("anim:", "node Animation " + node.Id + " chưa điền AnimationType (tên clip) — bỏ qua.");
                Say($"✖ {node.Id}:Animation chưa điền AnimationType");
                return loop ? double.PositiveInfinity : 0;
            }
            AnimationClip clip = FindClip(name);
            if (clip == null)
            {
                string have = ClipNames();
                bool legacy = have.Contains("attack_ground") || have.Contains("hitstun") || have.Contains("move") || have.Contains("idle,") || have.EndsWith("idle");
                WarnOnce("anim:" + name, $"{_owner?.name ?? "actor"} không có clip cho slot '{name}'. Clip đang có: {have}" +
                    (legacy ? ". Rig này còn dùng TÊN CLIP CŨ: chạy menu Evermeadow ▸ Animation ▸ Migrate Rigs To Animation Slots (đổi idle → idle1, attack_ground → attack1, …)." : ""));
                Say($"✖ {node.Id}:Animation rig không có slot '{name}' (xem Console để biết clip đang có)");
                return loop ? double.PositiveInfinity : 0;
            }
            StopGraph();
            _timeScale = Math.Max(0.01, SkillRun.Number(node, "TimeScale"));
            _loop = loop;
            _length = clip.length;
            _graph = PlayableGraph.Create("Skill animation");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _clip = AnimationClipPlayable.Create(_graph, clip);
            _clip.SetApplyFootIK(false);
            var output = AnimationPlayableOutput.Create(_graph, "Skill", _animator);
            output.SetSourcePlayable(_clip);
            _clip.SetSpeed(_timeScale);
            _graph.Play();
            Say($"▶ {node.Id}:Animation clip '{clip.name}' · {(loop ? "lặp" : Secs(clip.length / _timeScale))}");
            return loop ? double.PositiveInfinity : clip.length / _timeScale;
        }

        private double SpawnEffect(SkillNodeData node, double suggestedSeconds)
        {
            string key = node.Get("PrefabName").Trim();
            if (key.Length == 0)
            {
                WarnOnce("fx:", "node Effect " + node.Id + " chưa chọn PrefabName — bỏ qua.");
                Say($"✖ {node.Id}:Effect chưa chọn PrefabName");
                return suggestedSeconds;
            }
            if (_vfx == null)
            {
                WarnOnce("fx-none", "không có VFX catalog cho bộ chạy kỹ năng (gán VfxCatalog vào GameBootstrap) — Effect không hiện.");
                Say($"✖ {node.Id}:Effect {key}: không có VFX catalog");
                return suggestedSeconds;
            }
            int facing = Facing();
            Vector3 pos = PivotPoint(node, facing);
            Quaternion rot = facing < 0 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            float scale = (float)Math.Max(0.01, SkillRun.Number(node, "Scale"));
            float duration = (float)Math.Max(0, SkillRun.Number(node, "Duration"));
            Transform attach = node.Get("BindType") == nameof(SkillBindType.AttachToTarget) ? _owner : null;
            SkillVfx fx = _vfx.Spawn(key, pos, rot, scale, duration, attach);
            if (fx == null)
            {
                Say($"✖ {node.Id}:Effect '{key}' không có trong VFX catalog (Evermeadow ▸ VFX ▸ Rebuild Catalog)");
                return suggestedSeconds;
            }
            _effects[node.Id] = fx;
            double life = fx.LifetimeFor(duration);
            Say($"▶ {node.Id}:Effect '{key}' tại ({pos.x:0.00}, {pos.y:0.00}){(attach != null ? " bám theo nhân vật" : "")} · {Secs(life)} · {Describe(fx)}");
            return double.IsInfinity(life) ? double.PositiveInfinity : life;
        }

        // Hit / Search area exactly like the node: pivot (+ offset mirrored by the facing), Arc (angle, radius) or Box (length
        // along the facing, height) from SubValue × AreaScale, for the node's active time; a steady Hit that follows its pivot
        // moves with the owner. Drawn only when its switch is on (Evermeadow ▸ Project Settings).
        private bool DrawArea(SkillNodeData node, double seconds, Color colour)
        {
            if (_area == null) return false;
            int facing = Facing();
            Vector3 center = PivotPoint(node, facing);
            Vector3 sub = Vec(node.Get("SubValue"));
            float scale = (float)Math.Max(0.01, SkillRun.Number(node, "AreaScale"));
            bool follow = node.Kind == SkillNodeKind.Hit && node.Get("IsSteady") == "true" && node.Get("IsFollowPivot") == "true" &&
                          node.Get("PivotType") != nameof(SkillPivotType.Position);
            Transform with = follow ? _owner : null;
            if (node.Get("AreaType") == nameof(SkillAreaType.Box))
            {
                float length = sub.y * scale, height = (sub.z > 0 ? sub.z : sub.x) * scale;
                if (length <= 0 || height <= 0) return false;
                _area.ShowBox(center, facing, length, height, (float)seconds, colour, with);
            }
            else
            {
                if (sub.x <= 0 || sub.y * scale <= 0) return false;
                _area.ShowArc(center, facing, sub.x, sub.y * scale, (float)seconds, colour, with);
            }
            return true;
        }

        // ------------------------------------------------------------------ helpers

        private int Facing() => _actor.Facing >= 0 ? 1 : -1;

        // Owner pivot (+ offset mirrored by the facing), or an absolute position. Creature pivots other than the owner need
        // creature lookup (OQ-S14) and fall back to the owner.
        private Vector3 PivotPoint(SkillNodeData node, int facing)
        {
            Vector3 offset = Vec(node.Get("PivotOffset"));
            if (node.Get("PivotType") == nameof(SkillPivotType.Position))
            {
                Vector3 p = Vec(node.Get("PivotPosition"));
                return new Vector3(p.x + offset.x * facing, p.y + offset.y, 0f);
            }
            Vector2 owner = _actor.Position;
            return new Vector3(owner.x + offset.x * facing, owner.y + offset.y, 0f);
        }

        private AnimationClip FindClip(string name)
        {
            RuntimeAnimatorController rac = _animator != null ? _animator.runtimeAnimatorController : null;
            if (rac == null) return null;
            AnimationClip[] clips = rac.animationClips;
            foreach (AnimationClip c in clips)
                if (c != null && string.Equals(c.name, name, StringComparison.OrdinalIgnoreCase)) return c;
            foreach (AnimationClip c in clips)
                if (c != null && c.name.EndsWith("_" + name, StringComparison.OrdinalIgnoreCase)) return c;
            foreach (AnimationClip c in clips)
                if (c != null && c.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return c;
            return null;
        }

        private string ClipNames()
        {
            RuntimeAnimatorController rac = _animator != null ? _animator.runtimeAnimatorController : null;
            if (rac == null) return "(không có Animator)";
            var names = new SortedSet<string>(StringComparer.Ordinal);
            foreach (AnimationClip c in rac.animationClips)
                if (c != null) names.Add(c.name);
            return string.Join(", ", names);
        }

        private void Say(string line) => Trace?.Invoke(line);

        private static string Secs(double s) => double.IsInfinity(s) ? "tới khi kỹ năng kết thúc" : s.ToString("0.00", CultureInfo.InvariantCulture) + " s";

        // "1 renderer, cỡ 1.0×1.0 u, order 20" — size against the 2 × 3 u character tells whether it can hide behind it.
        private static string Describe(SkillVfx fx)
        {
            int n = 0, order = int.MinValue;
            Bounds b = default;
            foreach (Renderer r in fx.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || r is SpriteRenderer sr && sr.sprite == null) continue;
                b = n == 0 ? r.bounds : Encapsulate(b, r.bounds);
                order = Math.Max(order, r.sortingOrder);
                n++;
            }
            return n == 0 ? "KHÔNG có renderer nào hiện (sprite trống / tắt?)"
                : $"{n} renderer, cỡ {b.size.x:0.0}×{b.size.y:0.0} u, order {order}";
        }

        private static Bounds Encapsulate(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }

        private void StopGraph()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }

        private void WarnOnce(string key, string message)
        {
            if (_warned.Add(key)) Debug.LogWarning("[Skill] " + message);
        }

        private static Vector3 Vec(string text)
        {
            string[] p = (text ?? "").Split(',');
            float F(int i) => i < p.Length && float.TryParse(p[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;
            return new Vector3(F(0), F(1), F(2));
        }
    }
}
