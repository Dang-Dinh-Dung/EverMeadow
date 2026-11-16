using Evermeadow.Foundation.Time;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Evermeadow.Foundation.Input.Unity
{
    /// <summary>
    /// Builds the Input system for the composition root (ADR-0001 Decision 5) and holds its serialized data: the action asset and
    /// the tuning knobs. Keeps Input System types out of <c>Evermeadow.Bootstrap</c>, so only Input assemblies reference the
    /// package (GDD Input Rule 1, AC-R01).
    /// </summary>
    public sealed class InputRoot : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private InputConfig config = new InputConfig();

        private UnityInputAdapter _adapter;
        private RebindController _rebind;

        /// <summary>The core service (queries, modes, devices). Null before <see cref="Build"/>.</summary>
        public InputService Service { get; private set; }

        /// <summary>Rebinding API for Settings.</summary>
        public RebindController Rebind => _rebind;

        /// <summary>Glyph API for HUD and prompts.</summary>
        public GlyphProvider Glyphs { get; private set; }

        /// <summary>Creates the service, adapter, rebind controller, glyph provider and the frame driver on this GameObject.</summary>
        public void Build(FrameClock clock, ILogSink log)
        {
            Service = new InputService(clock, config, log, UnityInputAdapter.IsGamepadConnected, Debug.isDebugBuild);
            _adapter = new UnityInputAdapter(actions, Service, log);
            _rebind = new RebindController(_adapter, Service, log);
            Glyphs = new GlyphProvider(_adapter, Service);
            gameObject.AddComponent<FrameDriver>().Init(clock, Service, _adapter, new UnityRealTimeSource());
            _adapter.ReportHeldButtons(Service.AddLatchedHold);
        }

        private void OnDestroy()
        {
            _rebind?.Dispose();
            _adapter?.Dispose();
        }
    }
}
