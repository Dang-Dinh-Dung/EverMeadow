using Evermeadow.Foundation.Input;

namespace Evermeadow.Presentation.Menus
{
    /// <summary>One row of the Controls screen: a section title, a button action or one direction of an axis.</summary>
    public readonly struct ControlsRow
    {
        /// <summary>Section title ("Di chuyển"…) when <see cref="IsHeader"/>; otherwise the action's player-facing name.</summary>
        public readonly string Label;
        /// <summary>True for a section title (not selectable).</summary>
        public readonly bool IsHeader;
        /// <summary>Button action, or None for an axis direction.</summary>
        public readonly InputActionId Action;
        /// <summary>Axis of a direction row.</summary>
        public readonly AxisId Axis;
        /// <summary>Composite part of a direction row (up, down, left, right), or null.</summary>
        public readonly string Part;
        /// <summary>Text shown in the gamepad columns when the gamepad side cannot be rebound (stick directions), or null.</summary>
        public readonly string FixedGamepadText;

        private ControlsRow(string label, bool header, InputActionId action, AxisId axis, string part, string fixedGamepad)
        {
            Label = label;
            IsHeader = header;
            Action = action;
            Axis = axis;
            Part = part;
            FixedGamepadText = fixedGamepad;
        }

        /// <summary>A section title.</summary>
        public static ControlsRow Header(string label) => new ControlsRow(label, true, InputActionId.None, AxisId.Move, null, null);

        /// <summary>A button action.</summary>
        public static ControlsRow Button(string label, InputActionId action) => new ControlsRow(label, false, action, AxisId.Move, null, null);

        /// <summary>One direction of the Move axis (keyboard keys rebindable; the stick and D-pad are fixed).</summary>
        public static ControlsRow Direction(string label, string part) =>
            new ControlsRow(label, false, InputActionId.None, AxisId.Move, part, "Cần trái / D-pad");
    }

    /// <summary>
    /// Rows of the Controls screen (GDD Input Rule 17: every action can be rebound except the fixed Pause/Cancel slots), with
    /// Vietnamese player-facing names. Columns are keyboard primary, keyboard secondary, gamepad primary, gamepad secondary.
    /// </summary>
    public static class ControlsLayout
    {
        /// <summary>Selectable cells per binding row: keyboard primary / secondary, gamepad primary / secondary.</summary>
        public const int BindingColumns = 4;

        /// <summary>The rows, in display order.</summary>
        public static readonly ControlsRow[] Rows =
        {
            ControlsRow.Header("Di chuyển"),
            ControlsRow.Direction("Lên", "up"),
            ControlsRow.Direction("Xuống", "down"),
            ControlsRow.Direction("Trái", "left"),
            ControlsRow.Direction("Phải", "right"),
            ControlsRow.Button("Nhảy", InputActionId.Jump),
            ControlsRow.Button("Đi bộ / Chạy", InputActionId.Run),
            ControlsRow.Button("Lướt", InputActionId.Dodge),
            ControlsRow.Button("Rơi qua sàn (trống = Xuống + Nhảy)", InputActionId.DropThrough),

            ControlsRow.Header("Chiến đấu"),
            ControlsRow.Button("Tấn công", InputActionId.Attack),
            ControlsRow.Button("Cắm xuống (trống = Xuống + Tấn công)", InputActionId.Plunge),
            ControlsRow.Button("Hồi máu", InputActionId.Heal),
            ControlsRow.Button("Ăn món buff", InputActionId.EatBuff),

            ControlsRow.Header("Chung"),
            ControlsRow.Button("Tương tác", InputActionId.Interact),
            ControlsRow.Button("Bản đồ", InputActionId.Map),
            ControlsRow.Button("Tạm dừng", InputActionId.Pause),

            ControlsRow.Header("Menu"),
            ControlsRow.Button("Xác nhận", InputActionId.Confirm),
            ControlsRow.Button("Quay lại", InputActionId.Cancel),
            ControlsRow.Button("Tab trái", InputActionId.TabLeft),
            ControlsRow.Button("Tab phải", InputActionId.TabRight),

            ControlsRow.Header("Câu cá"),
            ControlsRow.Button("Giật / nâng cần", InputActionId.Reel)
        };
    }
}
