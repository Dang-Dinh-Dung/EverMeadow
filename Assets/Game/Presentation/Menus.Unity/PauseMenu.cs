using System;
using System.Collections.Generic;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Input.Unity;
using Evermeadow.Foundation.Time;
using Evermeadow.Presentation.Settings.Unity;
using UnityEngine;
using UnityEngine.UIElements;

namespace Evermeadow.Presentation.Menus.Unity
{
    /// <summary>
    /// Pause menu and Controls screen (UI Toolkit, built in code). Pause (Esc / Menu, Gameplay only, I-8) opens it: the game
    /// stops (time scale 0, as the pause owner of ADR-0001) and the Menu input mode is pushed. Keyboard and gamepad move through
    /// the cells with Navigate / Confirm / Cancel read from Input (GDD Input Rule 1); the mouse can hover and click. The Controls
    /// screen rebinds every action through <see cref="RebindController"/> (Rule 17: conflicts offer Swap or Cancel, the fixed
    /// Pause/Cancel keys are locked) and saves the bindings to <see cref="SettingsService"/> after each change.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenu : MonoBehaviour
    {
        /// <summary>Resources path of the fallback UI theme.</summary>
        public const string ThemeResource = "EvermeadowRuntimeTheme";

        private static readonly Color Ink = new Color32(0x2A, 0x1E, 0x1A, 0xF2);
        private static readonly Color Paper = new Color32(0xF3, 0xE9, 0xD2, 0xFF);
        private static readonly Color Amber = new Color32(0xE8, 0xA3, 0x3B, 0xFF);
        private static readonly Color Cell = new Color32(0x4A, 0x3A, 0x32, 0xFF);
        private static readonly Color Muted = new Color32(0xB8, 0xA8, 0x90, 0xFF);

        [SerializeField, Tooltip("Theme của UI Toolkit; để trống thì dùng Resources/" + ThemeResource + ".")]
        private ThemeStyleSheet theme;
        [SerializeField, Tooltip("Cửa sổ đệm (giây, đồng hồ thật) cho Pause / Xác nhận / Quay lại.")]
        private float inputWindowSeconds = 0.15f;
        [SerializeField] private int sortingOrder = 100;

        private enum Screen { Closed, Pause, Controls, Listening, Conflict }

        private struct CellRef
        {
            public Button Button;
            public int Slot;      // binding slot, −1 = none
            public bool Locked;   // fixed Pause/Cancel key or the stick
            public bool Gamepad;
            public int Row;       // index into ControlsLayout.Rows
        }

        private IInputQueries _queries;
        private IInputModes _modes;
        private ITimeScaleControl _time;
        private RebindController _rebind;
        private SettingsService _settings;
        private long _window;

        private ModeHandle _menuMode;
        private ScaleHandle _pause;
        private Screen _screen = Screen.Closed;
        private readonly MenuGrid _grid = new MenuGrid();
        private readonly List<CellRef[]> _cells = new List<CellRef[]>();
        private readonly List<RebindSlotView> _slotScratch = new List<RebindSlotView>();
        private CellRef _target;
        private bool _startWhenReleased;
        private bool _drainNext;
        private int _savedRow = -1, _savedColumn;
        private readonly Dictionary<Button, System.Action> _actions = new Dictionary<Button, System.Action>();

        private PanelSettings _panel;
        private VisualElement _overlay, _pausePanel, _controlsPanel, _dialog, _controlsRows;
        private ScrollView _scroll;
        private Label _dialogText, _status;
        private Button[] _pauseButtons, _dialogButtons, _controlsFooter;
        private readonly List<(Button button, CellRef cell)> _bindingCells = new List<(Button, CellRef)>();

        /// <summary>True while the menu is open.</summary>
        public bool IsOpen => _screen != Screen.Closed;

        /// <summary>Wires the menu (composition root). <paramref name="rebind"/> or <paramref name="settings"/> may be null: no Controls screen.</summary>
        public void Init(IInputQueries queries, IInputModes modes, ITimeScaleControl time, RebindController rebind, SettingsService settings)
        {
            _queries = queries ?? throw new ArgumentNullException(nameof(queries));
            _modes = modes ?? throw new ArgumentNullException(nameof(modes));
            _time = time;
            _rebind = rebind;
            _settings = settings;
            _window = Ticks.FromSeconds(inputWindowSeconds);
            _queries.RegisterConsumer(this, InputActionId.Pause, _window, ClockKind.Real);
            _queries.RegisterConsumer(this, InputActionId.Confirm, _window, ClockKind.Real);
            _queries.RegisterConsumer(this, InputActionId.Cancel, _window, ClockKind.Real);
            BuildUi();
        }

        /// <summary>Opens the pause menu (also what Pause does).</summary>
        public void Open()
        {
            if (IsOpen || _queries == null) return;
            _menuMode = _modes.Push(InputMode.Menu, this, () => this != null && IsOpen);
            if (_time != null) _pause = _time.PushScale(0.0, this, () => this != null && IsOpen);
            _overlay.style.display = DisplayStyle.Flex;
            Show(Screen.Pause);
        }

        /// <summary>Opens the Controls screen directly (opening the menu first if needed).</summary>
        public void OpenControls()
        {
            if (!IsOpen) Open();
            if (IsOpen) Show(Screen.Controls);
        }

        /// <summary>Closes the menu and resumes the game.</summary>
        public void Close()
        {
            if (!IsOpen) return;
            if (_rebind != null && _rebind.IsRebinding) _rebind.CancelRebind();
            _screen = Screen.Closed;
            _overlay.style.display = DisplayStyle.None;
            _modes.Pop(_menuMode);
            if (_time != null && _pause.IsValid) _time.PopScale(_pause);
        }

        private void OnDestroy()
        {
            if (IsOpen) Close();
            if (_panel != null) Destroy(_panel);
        }

        // ---------------------------------------------------------------- input

        private void Update()
        {
            if (_queries == null) return;
            if (!IsOpen)
            {
                if (_queries.TryConsume(InputActionId.Pause, _window, ClockKind.Real, out _)) Open();
                return;
            }

            if (_drainNext)
            {
                // The key that finished a rebind (Esc, the new key) must not also act on the menu this frame.
                _drainNext = false;
                Drain();
                return;
            }

            if (_screen == Screen.Listening)
            {
                Drain(); // keys pressed while listening belong to the rebind, not to the menu
                if (_startWhenReleased && !_queries.IsHeld(InputActionId.Confirm))
                {
                    _startWhenReleased = false;
                    _rebind.StartRebindSlot(_target.Slot, OnRebindResult);
                }
                return;
            }

            if (_queries.TryGetNavigateStep(out Vec2 step) && _grid.Move(Math.Sign(step.X), -Math.Sign(step.Y))) Highlight();
            if (_queries.TryConsume(InputActionId.Confirm, _window, ClockKind.Real, out _)) Activate();
            else if (_queries.TryConsume(InputActionId.Cancel, _window, ClockKind.Real, out _)) Back();
        }

        private void Drain()
        {
            _queries.TryGetNavigateStep(out _);
            _queries.TryConsume(InputActionId.Confirm, _window, ClockKind.Real, out _);
            _queries.TryConsume(InputActionId.Cancel, _window, ClockKind.Real, out _);
        }

        private void Activate()
        {
            if (_grid.Row < 0 || _grid.Row >= _cells.Count) return;
            CellRef[] row = _cells[_grid.Row];
            if (_grid.Column >= row.Length) return;
            CellRef c = row[_grid.Column];
            if (c.Button == null || !c.Button.enabledSelf) return;
            if (c.Slot == -2) { if (_actions.TryGetValue(c.Button, out System.Action act)) act(); }
            else ChooseBinding(c);
        }

        private void Back()
        {
            switch (_screen)
            {
                case Screen.Pause: Close(); break;
                case Screen.Controls: Show(Screen.Pause); break;
                case Screen.Conflict: _rebind.CommitRebind(false); break;
            }
        }

        // ---------------------------------------------------------------- screens

        private void Show(Screen screen)
        {
            if (_screen == Screen.Controls && screen != Screen.Controls)
            {
                _savedRow = _grid.Row;
                _savedColumn = _grid.Column;
            }
            if (screen == Screen.Pause) _savedRow = -1;
            _screen = screen;
            _pausePanel.style.display = screen == Screen.Pause ? DisplayStyle.Flex : DisplayStyle.None;
            _controlsPanel.style.display = screen == Screen.Controls || screen == Screen.Listening || screen == Screen.Conflict
                ? DisplayStyle.Flex : DisplayStyle.None;
            _dialog.style.display = screen == Screen.Listening || screen == Screen.Conflict ? DisplayStyle.Flex : DisplayStyle.None;
            _cells.Clear();

            switch (screen)
            {
                case Screen.Pause:
                    foreach (Button b in _pauseButtons) _cells.Add(new[] { Action(b) });
                    break;
                case Screen.Controls:
                    RefreshBindings();
                    foreach ((Button _, CellRef[] row) in BindingRows()) _cells.Add(row);
                    _cells.Add(new[] { Action(_controlsFooter[0]), Action(_controlsFooter[1]) });
                    break;
                case Screen.Conflict:
                    _cells.Add(new[] { Action(_dialogButtons[0]), Action(_dialogButtons[1]) });
                    break;
            }

            var widths = new int[_cells.Count];
            for (int i = 0; i < widths.Length; i++) widths[i] = _cells[i].Length;
            _grid.SetLayout(widths);
            if (screen == Screen.Controls && _savedRow >= 0) _grid.Focus(Math.Min(_savedRow, widths.Length - 1), _savedColumn);
            Highlight();
        }

        private static CellRef Action(Button b) => new CellRef { Button = b, Slot = -2 };

        private IEnumerable<(Button label, CellRef[] cells)> BindingRows()
        {
            int i = 0;
            while (i < _bindingCells.Count)
            {
                int row = _bindingCells[i].cell.Row;
                var cells = new List<CellRef>();
                while (i < _bindingCells.Count && _bindingCells[i].cell.Row == row) cells.Add(_bindingCells[i++].cell);
                yield return (null, cells.ToArray());
            }
        }

        private void Highlight()
        {
            for (int r = 0; r < _cells.Count; r++)
            {
                for (int c = 0; c < _cells[r].Length; c++)
                {
                    Button b = _cells[r][c].Button;
                    if (b == null) continue;
                    bool on = r == _grid.Row && c == _grid.Column;
                    b.style.backgroundColor = on ? Amber : Cell;
                    b.style.color = on ? Ink : (_cells[r][c].Locked ? Muted : Paper);
                    if (on && _screen == Screen.Controls && IsInside(b, _scroll.contentContainer)) _scroll.ScrollTo(b); // footer is outside
                }
            }
        }

        private static bool IsInside(VisualElement e, VisualElement ancestor)
        {
            for (VisualElement p = e.parent; p != null; p = p.parent)
            {
                if (p == ancestor) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- rebinding

        private void ChooseBinding(CellRef c)
        {
            if (_rebind == null) return;
            if (c.Locked || c.Slot < 0)
            {
                SetStatus(c.Slot < 0 && !c.Locked ? "Ô này không đổi được." : "Phím cố định: Esc / Menu luôn dành cho Tạm dừng và Quay lại; cần trái và D-pad luôn là di chuyển.");
                return;
            }
            _target = c;
            _startWhenReleased = true;
            _dialogText.text = c.Gamepad
                ? $"Bấm nút mới trên tay cầm cho \"{ControlsLayout.Rows[c.Row].Label}\".\nMenu / Options để hủy."
                : $"Bấm phím mới cho \"{ControlsLayout.Rows[c.Row].Label}\".\nEsc để hủy.";
            _dialogButtons[0].style.display = DisplayStyle.None;
            _dialogButtons[1].style.display = DisplayStyle.None;
            Show(Screen.Listening);
        }

        private void OnRebindResult(RebindResult r)
        {
            _drainNext = true;
            switch (r.Status)
            {
                case RebindStatus.Applied:
                    Save();
                    Show(Screen.Controls);
                    SetStatus("Đã đổi phím.");
                    break;
                case RebindStatus.Conflict:
                    var names = new List<string>();
                    foreach (InputActionId a in r.Conflicts) names.Add(NameOf(a));
                    _dialogText.text = $"\"{RebindController.DisplayName(_rebind.PendingPath)}\" đang dùng cho: {string.Join(", ", names)}.\n" +
                                       "Hoán đổi để hành động kia nhận phím cũ?";
                    _dialogButtons[0].style.display = DisplayStyle.Flex;
                    _dialogButtons[1].style.display = DisplayStyle.Flex;
                    Show(Screen.Conflict);
                    break;
                case RebindStatus.Rejected:
                    Show(Screen.Controls);
                    SetStatus(Explain(r.Outcome));
                    break;
                default:
                    if (IsOpen) Show(Screen.Controls);
                    SetStatus(r.CancelReason == RebindCancelReason.DeviceLost ? "Tay cầm bị rút: đã hủy đổi phím." : "Đã hủy.");
                    break;
            }
        }

        private static string Explain(RebindOutcome outcome)
        {
            switch (outcome)
            {
                case RebindOutcome.RejectedFixedKey: return "Esc / Menu luôn dành cho Tạm dừng và Quay lại.";
                case RebindOutcome.RejectedFixedSlot: return "Ô này cố định.";
                case RebindOutcome.RejectedMenuUnbound: return "Menu cần ít nhất một phím Xác nhận và Quay lại trên mỗi thiết bị.";
                case RebindOutcome.RejectedSwapConflict: return "Hoán đổi sẽ làm trùng phím với hành động khác.";
                default: return "Không dùng được phím này.";
            }
        }

        private static string NameOf(InputActionId action)
        {
            if (action == InputActionId.None) return "Di chuyển";
            foreach (ControlsRow row in ControlsLayout.Rows)
            {
                if (!row.IsHeader && row.Part == null && row.Action == action) return row.Label;
            }
            return action.ToString();
        }

        private void ResetAll()
        {
            _rebind.ResetBindings();
            Save();
            RefreshBindings();
            Highlight();
            SetStatus("Đã khôi phục phím mặc định.");
        }

        private void Save()
        {
            if (_settings == null) return;
            _settings.BindingOverrides = _rebind.ExportBindingOverrides();
            _settings.Save();
        }

        private void SetStatus(string text) => _status.text = text;

        // Fills every binding cell with its slot and key name.
        private void RefreshBindings()
        {
            if (_rebind == null) return;
            for (int i = 0; i < _bindingCells.Count; i++)
            {
                (Button button, CellRef cell) = _bindingCells[i];
                ControlsRow row = ControlsLayout.Rows[cell.Row];
                int column = i % ControlsLayout.BindingColumns;
                bool gamepad = column >= 2;
                cell.Slot = -1;
                cell.Locked = false;
                cell.Gamepad = gamepad;

                if (gamepad && row.FixedGamepadText != null)
                {
                    cell.Locked = true;
                    button.text = column == 2 ? row.FixedGamepadText : "";
                }
                else
                {
                    DeviceKind device = gamepad ? DeviceKind.Gamepad : DeviceKind.KeyboardMouse;
                    if (row.Part != null) _rebind.GetAxisSlots(row.Axis, row.Part, device, _slotScratch);
                    else _rebind.GetSlots(row.Action, device, _slotScratch);
                    int n = column % 2;
                    if (n < _slotScratch.Count)
                    {
                        RebindSlotView v = _slotScratch[n];
                        cell.Slot = v.Slot;
                        cell.Locked = v.Fixed;
                        button.text = v.Fixed ? v.Label + "  (cố định)" : v.Label;
                    }
                    else button.text = "";
                }
                _bindingCells[i] = (button, cell);
            }
        }

        // ---------------------------------------------------------------- building

        private void BuildUi()
        {
            _panel = ScriptableObject.CreateInstance<PanelSettings>();
            _panel.name = "Menu Panel";
            _panel.themeStyleSheet = theme != null ? theme : Resources.Load<ThemeStyleSheet>(ThemeResource);
            _panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _panel.referenceResolution = new Vector2Int(1920, 1080);
            _panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            _panel.match = 0.5f;
            _panel.sortingOrder = sortingOrder;

            var host = new GameObject("Menu UI");
            host.transform.SetParent(transform, false);
            host.SetActive(false);
            var doc = host.AddComponent<UIDocument>();
            doc.panelSettings = _panel;
            host.SetActive(true);
            VisualElement root = doc.rootVisualElement;

            _overlay = new VisualElement { name = "overlay", pickingMode = PickingMode.Position };
            Fill(_overlay);
            _overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.Center;
            _overlay.style.display = DisplayStyle.None;
            root.Add(_overlay);

            _pausePanel = Panel(420f);
            _pausePanel.Add(Title("Tạm dừng"));
            _pauseButtons = new[]
            {
                MenuButton("Tiếp tục", Close),
                MenuButton("Điều khiển", () => Show(Screen.Controls)),
                MenuButton("Thoát game", Application.Quit)
            };
            _pauseButtons[1].SetEnabled(_rebind != null);
            foreach (Button b in _pauseButtons) _pausePanel.Add(b);
            _overlay.Add(_pausePanel);

            _controlsPanel = Panel(1200f);
            _controlsPanel.style.height = Length.Percent(90);
            _controlsPanel.Add(Title("Điều khiển"));
            _controlsPanel.Add(ColumnHeader());
            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.style.flexGrow = 1f;
            _controlsRows = _scroll.contentContainer;
            BuildBindingRows();
            _controlsPanel.Add(_scroll);
            _status = new Label("Enter / A: đổi phím   ·   Esc / B: quay lại");
            _status.style.color = Muted;
            _status.style.marginTop = 8f;
            _status.style.whiteSpace = WhiteSpace.Normal;
            _controlsPanel.Add(_status);
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.SpaceBetween;
            _controlsFooter = new[] { MenuButton("Khôi phục mặc định", ResetAll), MenuButton("Quay lại", () => Show(Screen.Pause)) };
            foreach (Button b in _controlsFooter)
            {
                b.style.width = Length.Percent(48);
                footer.Add(b);
            }
            _controlsPanel.Add(footer);
            _overlay.Add(_controlsPanel);

            _dialog = new VisualElement();
            Fill(_dialog);
            _dialog.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
            _dialog.style.alignItems = Align.Center;
            _dialog.style.justifyContent = Justify.Center;
            VisualElement box = Panel(640f);
            _dialogText = new Label { style = { color = Paper, fontSize = 26, whiteSpace = WhiteSpace.Normal, unityTextAlign = TextAnchor.MiddleCenter, marginBottom = 16 } };
            box.Add(_dialogText);
            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            _dialogButtons = new[] { MenuButton("Hoán đổi", () => _rebind.CommitRebind(true)), MenuButton("Hủy", () => _rebind.CommitRebind(false)) };
            foreach (Button b in _dialogButtons)
            {
                b.style.width = Length.Percent(48);
                buttons.Add(b);
            }
            box.Add(buttons);
            _dialog.Add(box);
            _overlay.Add(_dialog);
        }

        private void BuildBindingRows()
        {
            for (int r = 0; r < ControlsLayout.Rows.Length; r++)
            {
                ControlsRow row = ControlsLayout.Rows[r];
                if (row.IsHeader)
                {
                    Label h = Title(row.Label);
                    h.style.fontSize = 24;
                    h.style.marginTop = 14f;
                    h.style.color = Amber;
                    _controlsRows.Add(h);
                    continue;
                }

                var line = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4f } };
                var label = new Label(row.Label) { style = { color = Paper, fontSize = 22, width = Length.Percent(32), whiteSpace = WhiteSpace.Normal } };
                line.Add(label);
                for (int c = 0; c < ControlsLayout.BindingColumns; c++)
                {
                    int rowIndex = r;
                    var cell = new CellRef { Row = r, Slot = -1 };
                    Button b = MenuButton("", null);
                    b.style.width = Length.Percent(16.5f);
                    b.style.marginLeft = c == 2 ? 14f : 2f;
                    int column = c, cellIndex = _bindingCells.Count;
                    b.clicked += () => ChooseBinding(_bindingCells[cellIndex].cell);
                    b.RegisterCallback<PointerEnterEvent>(_ => HoverBinding(rowIndex, column));
                    cell.Button = b;
                    _bindingCells.Add((b, cell));
                    line.Add(b);
                }
                _controlsRows.Add(line);
            }
        }

        private void HoverBinding(int layoutRow, int column)
        {
            if (_screen != Screen.Controls) return;
            for (int r = 0; r < _cells.Count; r++)
            {
                if (_cells[r].Length > column && _cells[r][column].Slot != -2 && _cells[r][column].Row == layoutRow && _grid.Focus(r, column))
                {
                    Highlight();
                    return;
                }
            }
        }

        private static VisualElement ColumnHeader()
        {
            var line = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6f } };
            line.Add(new Label("") { style = { width = Length.Percent(32) } });
            string[] names = { "Bàn phím", "Bàn phím (phụ)", "Tay cầm", "Tay cầm (phụ)" };
            for (int c = 0; c < names.Length; c++)
                line.Add(new Label(names[c]) { style = { color = Muted, fontSize = 18, width = Length.Percent(16.5f), marginLeft = c == 2 ? 14f : 2f, unityTextAlign = TextAnchor.MiddleCenter } });
            return line;
        }

        private static void Fill(VisualElement e)
        {
            e.style.position = Position.Absolute;
            e.style.left = 0;
            e.style.right = 0;
            e.style.top = 0;
            e.style.bottom = 0;
        }

        private static VisualElement Panel(float width)
        {
            var p = new VisualElement();
            p.style.width = width;
            p.style.maxWidth = Length.Percent(95);
            p.style.backgroundColor = Ink;
            p.style.paddingLeft = p.style.paddingRight = 28f;
            p.style.paddingTop = p.style.paddingBottom = 24f;
            p.style.borderTopLeftRadius = p.style.borderTopRightRadius = p.style.borderBottomLeftRadius = p.style.borderBottomRightRadius = 10f;
            p.style.borderLeftWidth = p.style.borderRightWidth = p.style.borderTopWidth = p.style.borderBottomWidth = 2f;
            p.style.borderLeftColor = p.style.borderRightColor = p.style.borderTopColor = p.style.borderBottomColor = Amber;
            return p;
        }

        private static Label Title(string text) =>
            new Label(text) { style = { color = Paper, fontSize = 34, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 12f } };

        private Button MenuButton(string text, System.Action onClick)
        {
            var b = new Button { text = text, focusable = false };
            if (onClick != null)
            {
                b.clicked += onClick;
                _actions[b] = onClick;
            }
            b.style.fontSize = 22;
            b.style.height = 44f;
            b.style.marginTop = b.style.marginBottom = 4f;
            b.style.backgroundColor = Cell;
            b.style.color = Paper;
            b.style.borderLeftWidth = b.style.borderRightWidth = b.style.borderTopWidth = b.style.borderBottomWidth = 0f;
            b.RegisterCallback<PointerEnterEvent>(_ => HoverAction(b));
            return b;
        }

        private void HoverAction(Button b)
        {
            for (int r = 0; r < _cells.Count; r++)
            {
                for (int c = 0; c < _cells[r].Length; c++)
                {
                    if (_cells[r][c].Button != b || _cells[r][c].Slot != -2) continue;
                    if (_grid.Focus(r, c)) Highlight();
                    return;
                }
            }
        }
    }
}
