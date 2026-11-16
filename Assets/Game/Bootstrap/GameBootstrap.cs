using Evermeadow.Core.CharacterController.Unity;
using Evermeadow.Core.Moveset;
using Evermeadow.Core.Moveset.Unity;
using Evermeadow.Core.Skills;
using Evermeadow.Core.Skills.Unity;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Input.Unity;
using Evermeadow.Foundation.Time;
using Evermeadow.Presentation.Creatures.Unity;
using Evermeadow.Presentation.DevTools;
using Evermeadow.Presentation.SkillRuntime.Unity;
using Evermeadow.Presentation.Vfx;
using Evermeadow.Presentation.Menus.Unity;
using Evermeadow.Presentation.Settings.Unity;
using UnityEngine;

namespace Evermeadow.Bootstrap
{
    /// <summary>
    /// Composition root (ADR-0001 Decision 5): creates the frame clock, the Input system and the settings, pushes the saved key
    /// bindings into Input (TD-B8), and hands everything to the systems that use them, including the pause menu (added when the
    /// scene has none). No static state; nothing references this assembly.
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    [RequireComponent(typeof(InputRoot))]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private CharacterBody player;
        [SerializeField, Tooltip("VFX catalog for skill effects (Assets/Game/Art/VFX/VfxCatalog.asset). Empty in the Editor = loaded from that path.")]
        private VfxCatalog vfxCatalog;

        private FrameClock _clock;
        private SettingsService _settings;
        private SkillLibraryRunner _skills;
        private MovesetLibraryRunner _movesets;

        /// <summary>Frame clock for gameplay systems (read side).</summary>
        public IFrameClock Clock => _clock;

        /// <summary>Scale control; inject only into Combat and the pause owner (ADR-0001 Decision 2).</summary>
        public ITimeScaleControl TimeScale => _clock;

        /// <summary>Player settings (per machine, settings.json).</summary>
        public SettingsService Settings => _settings;

        /// <summary>Live skill data (StreamingAssets/Skills, hot-reloaded from the Skill Tool in Play Mode).</summary>
        public SkillRepository Skills => _skills != null ? _skills.Repository : null;

        /// <summary>Live movesets (StreamingAssets/Movesets, hot-reloaded from the Moveset Tool in Play Mode).</summary>
        public MovesetRepository Movesets => _movesets != null ? _movesets.Repository : null;

        /// <summary>Input queries for gameplay systems.</summary>
        public IInputQueries InputQueries => GetComponent<InputRoot>().Service;

        /// <summary>Input mode stack.</summary>
        public IInputModes InputModes => GetComponent<InputRoot>().Service;

        /// <summary>Active device and device events.</summary>
        public IInputDevices InputDevices => GetComponent<InputRoot>().Service;

        private void Awake()
        {
            ILogSink log = new UnityLogSink();
            _clock = new FrameClock(log);
            InputRoot input = GetComponent<InputRoot>();
            input.Build(_clock, log);

            _settings = new SettingsService(new FileSettingsStorage(), log);
            _settings.Load();
            if (!input.Rebind.ApplyBindingOverrides(_settings.BindingOverrides))
            {
                _settings.BindingOverrides = input.Rebind.ExportBindingOverrides(); // keep only what survived the load checks
                _settings.Save();
            }

            if (!TryGetComponent(out _skills)) _skills = gameObject.AddComponent<SkillLibraryRunner>();
            _skills.Ensure();
            if (!TryGetComponent(out _movesets)) _movesets = gameObject.AddComponent<MovesetLibraryRunner>();
            _movesets.Ensure();

            if (player != null) player.Init(_clock, input.Service, log);

            if (!TryGetComponent(out PauseMenu menu)) menu = gameObject.AddComponent<PauseMenu>();
            menu.Init(input.Service, input.Service, _clock, input.Rebind, _settings);

            // Minimal skill runtime (GDD Skill & Moveset OQ-S16): one shared skill FX, a caster for the player (moveset) and one
            // per creature (behaviour tree). Attack animations only ever come from skills (user decision 2026-11-14).
            if (!TryGetComponent(out SkillFxHost fx)) fx = gameObject.AddComponent<SkillFxHost>();
            fx.Init(_clock, LoadVfxCatalog());
            if (!TryGetComponent(out SkillCaster caster)) caster = gameObject.AddComponent<SkillCaster>();
            if (player != null)
                caster.Init(_clock, new PlayerSkillActor(player.Controller, player.transform, player.GetComponentInChildren<Animator>()), fx, _skills.Repository);
            foreach (CreatureBody creature in FindObjectsByType<CreatureBody>(FindObjectsSortMode.None))
                creature.Init(_clock, player != null ? player.transform : null, fx, _skills.Repository);

            // Dev only (Editor, development builds): F9 panel to try sockets and movesets before Input / Controller use them.
            if (Debug.isDebugBuild)
            {
                if (!TryGetComponent(out MovesetTester tester)) tester = gameObject.AddComponent<MovesetTester>();
                InputService service = input.Service;
                tester.Init(_clock, service, () => service.TopMode == InputMode.Gameplay, player != null ? player.Controller : null,
                    _movesets.Repository, _skills.Repository, player != null ? caster : null);
            }
        }

        private VfxCatalog LoadVfxCatalog()
        {
#if UNITY_EDITOR
            if (vfxCatalog == null)
                vfxCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<VfxCatalog>("Assets/Game/Art/VFX/VfxCatalog.asset");
#endif
            if (vfxCatalog == null) Debug.LogWarning("[Skill] No VFX catalog on GameBootstrap: skill effects will not show.");
            return vfxCatalog;
        }
    }
}
