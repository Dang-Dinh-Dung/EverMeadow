using System;
using Evermeadow.Core.CharacterController;
using Evermeadow.Presentation.Vfx;
using Evermeadow.Foundation.Time;
using UnityEngine;

namespace Evermeadow.Presentation.SkillRuntime.Unity
{
    /// <summary>
    /// Whoever casts a skill — the player or a creature (user decision 2026-11-14: one skill runtime for both). The skill
    /// runtime reads where it stands and which way it faces, plays skill slot clips on its Animator, and tells it when a
    /// skill starts / reaches its End so it can stop moving meanwhile.
    /// </summary>
    public interface ISkillActor
    {
        /// <summary>World position of the actor's centre.</summary>
        Vector2 Position { get; }
        /// <summary>+1 facing right, −1 facing left.</summary>
        int Facing { get; }
        /// <summary>Transform effects attach to.</summary>
        Transform Transform { get; }
        /// <summary>Animator that plays the skill slot clips (null = no animation).</summary>
        Animator Animator { get; }
        /// <summary>True when a skill starts, false when it reaches its End: the actor holds still in between.</summary>
        void SetCasting(bool casting);
    }

    /// <summary>
    /// The player as a skill actor: position and facing from the <see cref="PlayerController"/>; while casting it holds a
    /// <see cref="ControlLockLevel.MoveOnly"/> lock (no move, jump or dodge).
    /// </summary>
    /// <example>
    /// <code>
    /// caster.Init(clock, new PlayerSkillActor(body.Controller, body.transform, animator), fx, skills);
    /// </code>
    /// </example>
    public sealed class PlayerSkillActor : ISkillActor
    {
        private readonly PlayerController _ctl;
        private ControlLockHandle _lock;
        private bool _locked;

        /// <summary>Wraps the player.</summary>
        public PlayerSkillActor(PlayerController controller, Transform transform, Animator animator)
        {
            _ctl = controller ?? throw new ArgumentNullException(nameof(controller));
            Transform = transform;
            Animator = animator;
        }

        /// <inheritdoc />
        public Vector2 Position => new Vector2(_ctl.Position.X, _ctl.Position.Y);

        /// <inheritdoc />
        public int Facing => _ctl.Facing >= 0 ? 1 : -1;

        /// <inheritdoc />
        public Transform Transform { get; }

        /// <inheritdoc />
        public Animator Animator { get; }

        /// <inheritdoc />
        public void SetCasting(bool casting)
        {
            if (casting && !_locked)
            {
                _lock = _ctl.RequestControlLock(this, () => Transform != null, ControlLockLevel.MoveOnly);
                _locked = true;
            }
            else if (!casting && _locked)
            {
                _ctl.ReleaseControlLock(_lock);
                _locked = false;
            }
        }
    }

    /// <summary>
    /// Shared presentation of every running skill: one <see cref="VfxSpawner"/> (pooled effects, ticked by the game clock so
    /// hit-stop holds them, drawn above characters) and one <see cref="SkillAreaDebug"/>. <c>GameBootstrap</c> adds it once;
    /// every <see cref="SkillCaster"/> (player, creatures) uses it.
    /// </summary>
    /// <example>
    /// <code>
    /// var fx = gameObject.AddComponent&lt;SkillFxHost&gt;();
    /// fx.Init(clock, vfxCatalog);
    /// </code>
    /// </example>
    public sealed class SkillFxHost : MonoBehaviour
    {
        /// <summary>Skill effects draw at or above this order on the Default layer (player sprite 10, its pips 11).</summary>
        public const int EffectSortingFloor = 20;

        private IFrameClock _clock;

        /// <summary>Effect spawner (null without a catalog: Effect nodes are then skipped).</summary>
        public VfxSpawner Vfx { get; private set; }

        /// <summary>Hit area outlines.</summary>
        public SkillAreaDebug Areas { get; private set; }

        /// <summary>Creates the spawner for <paramref name="catalog"/> (may be null).</summary>
        public void Init(IFrameClock clock, VfxCatalog catalog)
        {
            _clock = clock;
            Vfx?.Dispose();
            Vfx = catalog != null ? new VfxSpawner(catalog, new GameObject("Skill FX").transform) { SortingOrderFloor = EffectSortingFloor } : null;
            if (Areas == null) Areas = TryGetComponent(out SkillAreaDebug areas) ? areas : gameObject.AddComponent<SkillAreaDebug>();
            Debug.Log(catalog != null
                ? $"[Skill] Skill FX ready: VFX catalog with {catalog.Entries.Count} effect(s)."
                : "[Skill] Skill FX without a VFX catalog: Effect nodes will not show.");
        }

        private void Update()
        {
            if (_clock != null) Vfx?.Tick((float)(_clock.ScaledDelta / (double)Ticks.PerSecond));
        }

        private void OnDestroy() => Vfx?.Dispose();
    }
}
