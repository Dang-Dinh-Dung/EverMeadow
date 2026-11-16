using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace Evermeadow.Foundation.Input.Unity
{
    /// <summary>
    /// <c>GetGlyph(action)</c> of GDD Input Rule 14: the key bound to an action on the active device, as display data.
    /// UI calls it on <c>DeviceChanged</c>, never per frame.
    /// </summary>
    public sealed class GlyphProvider
    {
        private const string KeyboardGroup = "Keyboard&Mouse";
        private const string GamepadGroup = "Gamepad";

        private readonly UnityInputAdapter _adapter;
        private readonly IInputDevices _devices;
        private GlyphFamily _override = GlyphFamily.Auto;

        /// <summary>Creates a provider reading bindings from the adapter's runtime asset.</summary>
        public GlyphProvider(UnityInputAdapter adapter, IInputDevices devices)
        {
            _adapter = adapter;
            _devices = devices;
        }

        /// <summary>Settings push (Rule 14): Auto follows the device layout.</summary>
        public void SetGlyphFamily(GlyphFamily family) => _override = family;

        /// <summary>Glyph of <paramref name="action"/> on the active device.</summary>
        public Glyph GetGlyph(InputActionId action) => GetGlyph(action, _devices.Active, Gamepad.current);

        /// <summary>Glyph of <paramref name="action"/> for <paramref name="device"/>, using <paramref name="pad"/>'s layout (test hook).</summary>
        public Glyph GetGlyph(InputActionId action, DeviceKind device, Gamepad pad)
        {
            InputAction a = _adapter.ActionFor(action);
            if (a == null) return default;
            string group = device == DeviceKind.Gamepad ? GamepadGroup : KeyboardGroup;

            for (int i = 0; i < a.bindings.Count; i++)
            {
                InputBinding b = a.bindings[i];
                if (b.isComposite || string.IsNullOrEmpty(b.effectivePath) || b.groups == null || !b.groups.Contains(group)) continue;

                if (device == DeviceKind.KeyboardMouse)
                {
                    string label = InputControlPath.ToHumanReadableString(b.effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
                    return new Glyph(device, GlyphFamily.Auto, b.effectivePath, label);
                }

                GlyphFamily family = ResolveFamily(pad);
                string control = b.effectivePath.Substring(b.effectivePath.LastIndexOf('/') + 1);
                return new Glyph(device, family, b.effectivePath, GlyphNames.Gamepad(control, family));
            }
            return default;
        }

        private GlyphFamily ResolveFamily(Gamepad pad)
        {
            if (_override != GlyphFamily.Auto) return _override;
            return pad is DualShockGamepad ? GlyphFamily.PlayStation : GlyphFamily.Xbox;
        }
    }
}
