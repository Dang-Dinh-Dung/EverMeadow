using System;

namespace Evermeadow.Core.Moveset
{
    /// <summary>
    /// Logical move buttons a moveset row can use (rebindable in Input; never physical keys). Default bindings
    /// (keyboard + mouse / gamepad) belong to the Input action asset: Button1 = left mouse, Button2 = right mouse,
    /// Button3 = middle mouse, Button4 / Button5 = mouse side buttons, Key1–Key4 = keyboard skill keys (Q, E, R, F).
    /// </summary>
    public enum MoveButton
    {
        /// <summary>Default: left mouse.</summary>
        Button1,
        /// <summary>Default: right mouse.</summary>
        Button2,
        /// <summary>Default: middle mouse.</summary>
        Button3,
        /// <summary>Default: mouse side button (back).</summary>
        Button4,
        /// <summary>Default: mouse side button (forward).</summary>
        Button5,
        /// <summary>Default: Q.</summary>
        Key1,
        /// <summary>Default: E.</summary>
        Key2,
        /// <summary>Default: R.</summary>
        Key3,
        /// <summary>Default: F.</summary>
        Key4
    }

    /// <summary>How the button is used (recognised by <see cref="MoveGestureRecognizer"/>).</summary>
    public enum MoveGesture
    {
        /// <summary>One press (fires on press, unless the button also has a Hold or Chord row: then on release / window end).</summary>
        Press,
        /// <summary>Second press within the double-tap window (the first press still fires its Press).</summary>
        DoubleTap,
        /// <summary>Held down for the hold time.</summary>
        Hold,
        /// <summary>Two buttons pressed together (within the chord window), e.g. left + right mouse.</summary>
        Chord
    }

    /// <summary>
    /// The input of a moveset row: a button and a gesture (a second button for <see cref="MoveGesture.Chord"/>, stored in
    /// ascending order so A+B equals B+A). Text form: <c>Button1</c>, <c>Button1:double</c>, <c>Button1:hold</c>,
    /// <c>Button1+Button2</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// MoveInput.TryParse("Button2+Button1", out MoveInput chord);   // == MoveInput.Chord(Button1, Button2)
    /// string text = MoveInput.DoubleTap(MoveButton.Button1).ToString();   // "Button1:double"
    /// </code>
    /// </example>
    public readonly struct MoveInput : IEquatable<MoveInput>
    {
        /// <summary>Button (the lower one of a chord).</summary>
        public readonly MoveButton Button;
        /// <summary>Gesture.</summary>
        public readonly MoveGesture Gesture;
        /// <summary>Second button of a chord (equal to <see cref="Button"/> otherwise).</summary>
        public readonly MoveButton Second;

        private MoveInput(MoveButton button, MoveGesture gesture, MoveButton second)
        {
            Button = button;
            Gesture = gesture;
            Second = second;
        }

        /// <summary>A single press.</summary>
        public static MoveInput Press(MoveButton b) => new MoveInput(b, MoveGesture.Press, b);

        /// <summary>A double tap.</summary>
        public static MoveInput DoubleTap(MoveButton b) => new MoveInput(b, MoveGesture.DoubleTap, b);

        /// <summary>A hold.</summary>
        public static MoveInput Hold(MoveButton b) => new MoveInput(b, MoveGesture.Hold, b);

        /// <summary>Two buttons together (order does not matter). <paramref name="a"/> must differ from <paramref name="b"/>.</summary>
        public static MoveInput Chord(MoveButton a, MoveButton b)
        {
            if (a == b) throw new ArgumentException("a chord needs two different buttons");
            return a < b ? new MoveInput(a, MoveGesture.Chord, b) : new MoveInput(b, MoveGesture.Chord, a);
        }

        /// <summary>True when this input uses <paramref name="b"/>.</summary>
        public bool Uses(MoveButton b) => Button == b || Second == b;

        /// <summary>Parses the text form; false for anything else (unknown button, A+A, unknown gesture).</summary>
        public static bool TryParse(string text, out MoveInput input)
        {
            input = default;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.Trim();
            int plus = t.IndexOf('+');
            if (plus >= 0)
            {
                if (!TryButton(t.Substring(0, plus), out MoveButton a) || !TryButton(t.Substring(plus + 1), out MoveButton b) || a == b) return false;
                input = Chord(a, b);
                return true;
            }
            int colon = t.IndexOf(':');
            if (colon < 0)
            {
                if (!TryButton(t, out MoveButton p)) return false;
                input = Press(p);
                return true;
            }
            if (!TryButton(t.Substring(0, colon), out MoveButton button)) return false;
            switch (t.Substring(colon + 1).Trim())
            {
                case "press": input = Press(button); return true;
                case "double": input = DoubleTap(button); return true;
                case "hold": input = Hold(button); return true;
                default: return false;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            switch (Gesture)
            {
                case MoveGesture.DoubleTap: return Button + ":double";
                case MoveGesture.Hold: return Button + ":hold";
                case MoveGesture.Chord: return Button + "+" + Second;
                default: return Button.ToString();
            }
        }

        /// <inheritdoc />
        public bool Equals(MoveInput other) => Button == other.Button && Gesture == other.Gesture && Second == other.Second;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is MoveInput other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => ((int)Button * 31 + (int)Gesture) * 31 + (int)Second;

        /// <summary>Equality.</summary>
        public static bool operator ==(MoveInput a, MoveInput b) => a.Equals(b);

        /// <summary>Inequality.</summary>
        public static bool operator !=(MoveInput a, MoveInput b) => !a.Equals(b);

        private static bool TryButton(string s, out MoveButton b)
        {
            b = default;
            string t = s.Trim();
            if (t.Length == 0 || char.IsDigit(t[0]) || t[0] == '-') return false;   // names only, never numbers
            return Enum.TryParse(t, false, out b) && Enum.IsDefined(typeof(MoveButton), b);
        }
    }
}
