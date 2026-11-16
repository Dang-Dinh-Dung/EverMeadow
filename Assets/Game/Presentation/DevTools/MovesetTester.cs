using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Evermeadow.Core.CharacterController;
using Evermeadow.Core.Moveset;
using Evermeadow.Core.Skills;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Input.Unity;
using Evermeadow.Foundation.Time;
using Evermeadow.Presentation.SkillRuntime.Unity;
using UnityEngine;

namespace Evermeadow.Presentation.DevTools
{
    /// <summary>
    /// Dev panel (Editor / development builds, F9 shows / hides) to try the moveset system in Play Mode before stones,
    /// weapons, Input actions and the skill runtime exist: pick a weapon family and 2–4 sockets, socket any moveset in each
    /// (refusals and their conflicts are shown), then press the move buttons — left / right / middle / side mouse buttons,
    /// Q E R F — and read what the gesture recognizer saw and which skill <see cref="MoveSelector"/> chose, from the player's
    /// real state (ground / air / dodge, moving, held direction) or a forced context, then casts it on the player through the
    /// <see cref="SkillCaster"/> (minimal skill runtime). A press while the skill is before its End is refused; follow-up
    /// link windows count from the skill's End. Moveset and skill edits saved in the tools apply immediately.
    /// The buttons are dev-only actions built in code (<see cref="DevButtonPoller"/>), so they also fire the regular
    /// actions bound to the same keys (left mouse = Attack, Q = Heal, E = Interact, R = EatBuff) until OQ-S18 settles them.
    /// </summary>
    /// <example>
    /// <code>
    /// var tester = gameObject.AddComponent&lt;MovesetTester&gt;();
    /// tester.Init(clock, input, () =&gt; inputService.TopMode == InputMode.Gameplay, player.Controller, movesets, skills);
    /// </code>
    /// </example>
    public sealed class MovesetTester : MonoBehaviour
    {
        private static readonly string[] Bindings =
        {
            "<Mouse>/leftButton", "<Mouse>/rightButton", "<Mouse>/middleButton", "<Mouse>/backButton", "<Mouse>/forwardButton",
            "<Keyboard>/q", "<Keyboard>/e", "<Keyboard>/r", "<Keyboard>/f",
            "<Keyboard>/f9"
        };

        private static readonly string[] ButtonKeys = { "chuột trái", "chuột phải", "chuột giữa", "chuột bên (lùi)", "chuột bên (tiến)", "Q", "E", "R", "F" };
        private const int ToggleIndex = 9, MaxLog = 24, MaxSockets = MovesetLoadout.MaxSockets;
        private static readonly string[] ContextNames = { "Auto", "Ground", "Air", "Dash" };

        private IFrameClock _clock;
        private IInputQueries _input;
        private Func<bool> _gameplayTop;
        private PlayerController _player;
        private MovesetRepository _movesets;
        private SkillRepository _skills;
        private SkillCaster _caster;

        private DevButtonPoller _poller;
        private readonly MoveGestureRecognizer _gestures = new MoveGestureRecognizer(MoveGestureSettings.Default);
        private readonly List<DevButtonEvent> _raw = new List<DevButtonEvent>(16);
        private readonly List<MoveInputEvent> _events = new List<MoveInputEvent>(16);
        private readonly Cooldowns _cooldowns = new Cooldowns();

        private bool _visible = true, _showMap = true;
        private Rect _window = new Rect(10, 10, 520, 10);
        private List<string> _families = new List<string>(WeaponFamilies.Defaults);
        private int _family, _sockets = 3, _contextMode;
        private readonly string[] _selected = new string[MaxSockets];
        private readonly string[] _slotStatus = new string[MaxSockets];
        private readonly bool[] _slotOk = new bool[MaxSockets];
        private MovesetLoadout _loadout;
        private string _chain = "";
        private long _chainEnd;
        private readonly List<string> _log = new List<string>();

        /// <summary>Hands the tester what it reads. <paramref name="player"/> may be null (context then defaults to ground).</summary>
        public void Init(IFrameClock clock, IInputQueries input, Func<bool> gameplayTop, PlayerController player, MovesetRepository movesets,
            SkillRepository skills, SkillCaster caster = null)
        {
            if (_caster != null) _caster.Trace -= Log;
            _caster = caster;
            if (_caster != null) _caster.Trace += Log;
            _clock = clock;
            _input = input;
            _gameplayTop = gameplayTop;
            _player = player;
            if (_movesets != null) _movesets.Changed -= OnMovesetsChanged;
            _movesets = movesets;
            _skills = skills;
            if (_movesets != null) _movesets.Changed += OnMovesetsChanged;
            LoadFamilies();
            Rebuild();
        }

        private void OnEnable() => _poller = new DevButtonPoller(Bindings);

        private void OnDisable()
        {
            _poller?.Dispose();
            _poller = null;
            _gestures.Reset();
        }

        private void OnDestroy()
        {
            if (_caster != null) _caster.Trace -= Log;
            if (_movesets != null) _movesets.Changed -= OnMovesetsChanged;
        }

        private void OnMovesetsChanged(SkillReloadEvent e)
        {
            LoadFamilies();
            Rebuild();
            if (e.FileName != "*") Log($"↻ moveset {e.Subject}: {e.Result}");
        }

        private void Update()
        {
            if (_poller == null || _clock == null) return;
            if (!DevOptions.ShowMovesetTester)
            {
                _gestures.Reset();
                return;   // switched off in Evermeadow ▸ Project Settings
            }
            _raw.Clear();
            _poller.Poll(_raw);
            long now = _clock.ScaledNow;
            bool gameplay = _gameplayTop == null || _gameplayTop();
            _events.Clear();
            foreach (DevButtonEvent raw in _raw)
            {
                if (raw.Index == ToggleIndex)
                {
                    if (raw.Pressed) _visible = !_visible;
                    continue;
                }
                if (!gameplay) continue;
                if (raw.Pressed && raw.Index <= (int)MoveButton.Button3 && PointerOverPanel()) continue;   // clicking the panel itself
                var button = (MoveButton)raw.Index;
                if (raw.Pressed) _gestures.Press(button, now, _events);
                else _gestures.Release(button, now, _events);
            }
            if (!gameplay)
            {
                _gestures.Reset();
                return;
            }
            _gestures.Update(now, _events);
            foreach (MoveInputEvent e in _events) Handle(e, now);
            if (_caster != null && _chain.Length > 0 && _chain == _caster.LastSkillId) _chainEnd = _caster.LastEndTick;   // window from End
        }

        // ------------------------------------------------------------------ selection

        private void Handle(MoveInputEvent e, long now)
        {
            if (_loadout == null || _movesets == null) return;
            ReadState(out SkillContext ctx, out bool moving, out SkillDir dir);
            string state = StateText(ctx, moving, dir);
            var q = new MoveQuery(e.Input, ctx, moving, dir, _chain, _chainEnd, now);
            _cooldowns.Now = now;
            MoveChoice c = MoveSelector.Select(_loadout, _movesets.Library, in q, _cooldowns);
            if (!c.Found)
            {
                MoveChoice cooling = MoveSelector.Select(_loadout, _movesets.Library, in q);
                if (cooling.Found)
                    Log($"{e.Input}  [{state}] → {cooling.SkillId} đang hồi chiêu (còn {_cooldowns.Remaining(cooling.Moveset.Id, cooling.Row.Order, now):0.0} s)");
                else Log($"{e.Input}  [{state}] → không có đòn");
                return;
            }
            bool hasSkill = _skills != null && _skills.Library.TryGet(c.SkillId, out _);
            string where = $"ô {c.Slot} · moveset {c.Moveset.Id} dòng {c.Row.Order}{(c.Row.HasAfter ? " · đòn nối" : "")}";
            if (_caster == null)
            {
                Log($"{e.Input}  [{state}] → skill {c.SkillId}{(hasSkill ? "" : " (chưa có file skill!)")}   {where}");
                _chain = c.SkillId;
                _chainEnd = now;   // no caster: the move counts as ending at once
            }
            else if (_caster.TryCast(c.SkillId, out string why))
            {
                Log($"{e.Input}  [{state}] → ra đòn skill {c.SkillId}   {where}");
                _chain = c.SkillId;
                _chainEnd = _caster.LastEndTick;
            }
            else
            {
                Log($"{e.Input}  [{state}] → skill {c.SkillId} không ra được: {why}");
                return;
            }
            if (c.Row.CooldownTicks > 0) _cooldowns.Start(c.Moveset.Id, c.Row.Order, now + c.Row.CooldownTicks);
        }

        private void ReadState(out SkillContext ctx, out bool moving, out SkillDir dir)
        {
            switch (_contextMode)
            {
                case 1: ctx = SkillContext.Ground; break;
                case 2: ctx = SkillContext.Air; break;
                case 3: ctx = SkillContext.Dash; break;
                default:
                    ctx = _player == null ? SkillContext.Ground
                        : _player.State == CharacterState.Dodge ? SkillContext.Dash
                        : _player.IsGrounded ? SkillContext.Ground : SkillContext.Air;
                    break;
            }
            Vec2 m = _input != null ? _input.ReadMove() : Vec2.Zero;
            moving = Mathf.Abs(m.X) > 0.3f;
            if (Mathf.Abs(m.Y) > Mathf.Abs(m.X) && Mathf.Abs(m.Y) > 0.3f) dir = m.Y > 0 ? SkillDir.Up : SkillDir.Down;
            else if (Mathf.Abs(m.X) > 0.3f) dir = Math.Sign(m.X) == (_player != null ? _player.Facing : 1) ? SkillDir.Forward : SkillDir.Back;
            else dir = SkillDir.Neutral;
        }

        private static string StateText(SkillContext ctx, bool moving, SkillDir dir) =>
            MovesetXml.ContextText(ctx) + (moving ? " moving" : " still") + " " + MovesetXml.DirText(dir);

        // ------------------------------------------------------------------ sockets

        private void LoadFamilies()
        {
            var list = new List<string>();
            try
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Weapons", "weapon_families.txt");
                list.AddRange(File.Exists(path) ? WeaponFamilies.Parse(File.ReadAllText(path), null) : WeaponFamilies.Defaults);
            }
            catch (IOException)
            {
                list.AddRange(WeaponFamilies.Defaults);
            }
            if (_movesets != null)
                foreach (MovesetDef d in _movesets.Library.All)
                    foreach (string w in d.Weapons)
                        if (!list.Contains(w)) list.Add(w);   // families used by movesets stay testable
            if (list.Count == 0) list.Add("weapon");
            string current = _families.Count > 0 && _family < _families.Count ? _families[_family] : null;
            _families = list;
            _family = Math.Max(0, current != null ? _families.IndexOf(current) : 0);
        }

        private void Rebuild()
        {
            _loadout = new MovesetLoadout(_families[_family], _sockets);
            MovesetLibrary lib = _movesets?.Library;
            for (int s = 0; s < MaxSockets; s++)
            {
                _slotOk[s] = true;
                _slotStatus[s] = "";
                if (s >= _sockets || _selected[s] == null) continue;
                if (lib == null || !lib.TryGet(_selected[s], out MovesetDef def))
                {
                    _slotOk[s] = false;
                    _slotStatus[s] = "moveset không còn tồn tại";
                    continue;
                }
                var conflicts = new List<MoveConflict>();
                SocketResult r = _loadout.TrySocket(s, def, lib, conflicts);
                _slotOk[s] = r == SocketResult.Ok;
                _slotStatus[s] = r == SocketResult.Ok ? "OK"
                    : r == SocketResult.WeaponNotAllowed ? "bị chặn: không hợp loại vũ khí"
                    : r == SocketResult.AlreadySocketed ? "bị chặn: đã khảm ở ô khác"
                    : "bị chặn (trùng): " + string.Join("; ", conflicts.Select(c => $"dòng {c.Row} trùng ô {c.OtherSlot} dòng {c.OtherRow} ({c.Input})"));
            }
            _gestures.Configure(MoveGestureRecognizer.InputsOf(_loadout, lib));
        }

        private void CycleSocket(int slot, int delta)
        {
            var ids = new List<string> { null };
            if (_movesets != null) ids.AddRange(_movesets.Library.All.Select(d => d.Id).OrderBy(i => i, StringComparer.Ordinal));
            int i = Math.Max(0, ids.IndexOf(_selected[slot]));
            _selected[slot] = ids[(i + delta + ids.Count) % ids.Count];
            Rebuild();
        }

        // ------------------------------------------------------------------ UI

        private bool PointerOverPanel()
        {
            Vector2? p = _poller?.PointerScreenPosition;
            return _visible && p.HasValue && _window.Contains(new Vector2(p.Value.x, Screen.height - p.Value.y));
        }

        private void OnGUI()
        {
            if (!DevOptions.ShowMovesetTester) return;
            if (!_visible)
            {
                GUI.Label(new Rect(10, 10, 300, 20), "Moveset Tester: F9");
                return;
            }
            _window = GUILayout.Window(GetInstanceID(), _window, DrawWindow, "Moveset Tester  (F9 ẩn / hiện, kéo để di chuyển)");
        }

        private void DrawWindow(int id)
        {
            MovesetLibrary lib = _movesets?.Library;
            GUILayout.Label($"Moveset: {lib?.Count ?? 0}   Skill: {_skills?.Library.SkillCount ?? 0}   — sửa trong Moveset Tool rồi Save là áp dụng ngay");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vũ khí", GUILayout.Width(60));
            if (GUILayout.Button("◀", GUILayout.Width(26))) { _family = (_family + _families.Count - 1) % _families.Count; Rebuild(); }
            GUILayout.Label(_families[_family], GUILayout.Width(110));
            if (GUILayout.Button("▶", GUILayout.Width(26))) { _family = (_family + 1) % _families.Count; Rebuild(); }
            GUILayout.Space(16);
            GUILayout.Label("Số ô", GUILayout.Width(36));
            int sockets = GUILayout.Toolbar(_sockets - 2, new[] { "2", "3", "4" }, GUILayout.Width(110)) + 2;
            if (sockets != _sockets) { _sockets = sockets; Rebuild(); }
            GUILayout.EndHorizontal();

            for (int s = 0; s < _sockets; s++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Ô {s}", GUILayout.Width(60));
                if (GUILayout.Button("◀", GUILayout.Width(26))) CycleSocket(s, -1);
                string name = _selected[s] == null ? "(trống)" : lib != null && lib.TryGet(_selected[s], out MovesetDef d) && d.Name.Length > 0 ? $"{d.Id} {d.Name}" : _selected[s];
                GUILayout.Label(name, GUILayout.Width(180));
                if (GUILayout.Button("▶", GUILayout.Width(26))) CycleSocket(s, +1);
                Color old = GUI.contentColor;
                if (!_slotOk[s]) GUI.contentColor = new Color(1f, 0.55f, 0.4f);
                GUILayout.Label(_slotStatus[s]);
                GUI.contentColor = old;
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ngữ cảnh", GUILayout.Width(60));
            _contextMode = GUILayout.Toolbar(_contextMode, ContextNames, GUILayout.Width(240));
            ReadState(out SkillContext ctx, out bool moving, out SkillDir dir);
            GUILayout.Label("→ " + StateText(ctx, moving, dir));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Chuỗi: " + (_chain.Length == 0 ? "(không)" : _chain), GUILayout.Width(200));
            if (GUILayout.Button("Xóa chuỗi", GUILayout.Width(90))) _chain = "";
            if (GUILayout.Button("Reset hồi chiêu", GUILayout.Width(120))) _cooldowns.Clear();
            if (GUILayout.Button("Xóa log", GUILayout.Width(70))) _log.Clear();
            GUILayout.EndHorizontal();

            _showMap = GUILayout.Toggle(_showMap, "Bảng input → đòn (phím: Button1 chuột trái · 2 chuột phải · 3 chuột giữa · 4/5 chuột bên · Key1–4 = Q E R F)");
            if (_showMap && _loadout != null && lib != null)
            {
                bool any = false;
                for (int s = 0; s < _loadout.SocketCount; s++)
                {
                    if (!lib.TryGet(_loadout.Get(s), out MovesetDef def)) continue;
                    foreach (MovesetRow row in def.Rows)
                    {
                        any = true;
                        string cond = MovesetXml.ContextText(row.Context) + (row.Moving != MovingFilter.Any ? " " + row.Moving.ToString().ToLowerInvariant() : "") +
                                      (row.AnyDir ? "" : " " + MovesetXml.DirText(row.Dir)) + (row.HasAfter ? " sau " + string.Join(",", row.AfterIds) : "");
                        GUILayout.Label($"   {row.Input} ({ButtonKeys[(int)row.Input.Button]})  [{cond}] → {row.SkillId}   ô {s}{(row.CooldownTicks > 0 ? " · hồi chiêu" : "")}");
                    }
                }
                if (!any) GUILayout.Label("   (chưa khảm moveset nào — mọi đòn đến từ đá khảm)");
            }

            GUILayout.Label("Log (mới nhất trên cùng):");
            for (int i = _log.Count - 1; i >= 0; i--) GUILayout.Label("   " + _log[i]);
            GUI.DragWindow();
        }

        private void Log(string line)
        {
            _log.Add(line);
            if (_log.Count > MaxLog) _log.RemoveAt(0);
        }

        private sealed class Cooldowns : IMoveCooldowns
        {
            private readonly Dictionary<string, long> _readyAt = new Dictionary<string, long>(StringComparer.Ordinal);
            public long Now;

            public bool IsCoolingDown(string movesetId, int row) => _readyAt.TryGetValue(movesetId + "#" + row, out long t) && t > Now;

            public double Remaining(string movesetId, int row, long now) =>
                _readyAt.TryGetValue(movesetId + "#" + row, out long t) ? Math.Max(0, t - now) / (double)Ticks.PerSecond : 0;

            public void Start(string movesetId, int row, long readyAt) => _readyAt[movesetId + "#" + row] = readyAt;

            public void Clear() => _readyAt.Clear();
        }
    }
}
