namespace Evermeadow.Foundation.Input
{
    /// <summary>Gamepad glyph family (GDD Input Rule 14). Auto follows the device layout; Settings may override.</summary>
    public enum GlyphFamily
    {
        /// <summary>Follow the layout the device reports; generic or unknown layouts use Xbox.</summary>
        Auto,
        /// <summary>Xbox labels (A, B, X, Y, LB, RB…).</summary>
        Xbox,
        /// <summary>PlayStation labels (Cross, Circle, Square, Triangle, L1, R1…).</summary>
        PlayStation
    }

    /// <summary>What a prompt shows for an action on the active device (display data only; UI draws it).</summary>
    public readonly struct Glyph
    {
        /// <summary>Device family the glyph is for.</summary>
        public readonly DeviceKind Device;
        /// <summary>Resolved gamepad family (Xbox or PlayStation); Auto for keyboard and mouse.</summary>
        public readonly GlyphFamily Family;
        /// <summary>Control path of the binding shown, e.g. "&lt;Gamepad&gt;/buttonSouth".</summary>
        public readonly string ControlPath;
        /// <summary>Label to draw, e.g. "Space", "A", "Cross".</summary>
        public readonly string Label;

        /// <summary>Creates a glyph.</summary>
        public Glyph(DeviceKind device, GlyphFamily family, string controlPath, string label)
        {
            Device = device;
            Family = family;
            ControlPath = controlPath;
            Label = label;
        }
    }

    /// <summary>Gamepad control name → label per family (Rule 14). Pure data.</summary>
    public static class GlyphNames
    {
        /// <summary>Label for gamepad control <paramref name="control"/> (last path segment, e.g. "buttonSouth").</summary>
        public static string Gamepad(string control, GlyphFamily family)
        {
            bool ps = family == GlyphFamily.PlayStation;
            switch (control)
            {
                case "buttonSouth": return ps ? "Cross" : "A";
                case "buttonEast": return ps ? "Circle" : "B";
                case "buttonWest": return ps ? "Square" : "X";
                case "buttonNorth": return ps ? "Triangle" : "Y";
                case "leftShoulder": return ps ? "L1" : "LB";
                case "rightShoulder": return ps ? "R1" : "RB";
                case "leftTrigger": return ps ? "L2" : "LT";
                case "rightTrigger": return ps ? "R2" : "RT";
                case "start": return ps ? "Options" : "Menu";
                case "select": return ps ? "Touchpad" : "View";
                case "leftStick": return ps ? "L3" : "LS";
                case "rightStick": return ps ? "R3" : "RS";
                case "leftStickPress": return ps ? "L3" : "LS";
                case "rightStickPress": return ps ? "R3" : "RS";
                case "dpad": return "D-pad";
                default: return control;
            }
        }
    }
}
