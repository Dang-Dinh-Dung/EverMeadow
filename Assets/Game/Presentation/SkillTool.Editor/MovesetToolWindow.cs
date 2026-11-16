using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evermeadow.Core.Moveset;
using Evermeadow.Core.Skills;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Evermeadow.Presentation.SkillTool.Editor
{
    /// <summary>
    /// Moveset Tool (<c>Evermeadow ▸ Skills ▸ Moveset Tool</c>): reads and writes the moveset XML that socket stones point
    /// to. Each row = input (button + gesture) + conditions (ground / air / dash, still / moving, direction, follow-up of a
    /// previous skill) + the skill id to cast (picked from the Skill Tool files; ↗ opens it in the Skill Tool) + priority and
    /// cooldown. The Socket test pane sockets saved movesets (and the one being edited) into a weapon to show what is
    /// refused and the resulting input map. Saving in Play Mode updates the running game at once. Editor only.
    /// </summary>
    public sealed class MovesetToolWindow : EditorWindow
    {
        private const int MaxUndo = 100;
        private const string Empty = "(trống)";
        private static readonly List<string> Dirs = new List<string> { "any", "neutral", "up", "down", "forward", "back" };
        private static readonly int ButtonCount = Enum.GetValues(typeof(MoveButton)).Length;

        // Column widths: #, button, gesture, second, context, moving, dir, skill, open, after, link, priority, cooldown.
        private static readonly float[] W = { 22, 78, 78, 78, 112, 72, 76, 96, 24, 120, 54, 44, 58 };

        [SerializeField] private string _id = "";
        [SerializeField] private string _wipXml = "";
        [SerializeField] private bool _dirty;
        [SerializeField] private string _testWeaponFamily = "";
        [SerializeField] private int _testSocketCount = 3;
        [SerializeField] private List<string> _testIds = new List<string>();

        private MovesetData _data;
        private ISet<string> _knownSkills = new HashSet<string>();
        private readonly List<string> _undo = new List<string>();
        private readonly List<string> _redo = new List<string>();
        private Label _title, _status, _problems, _testResult, _help;
        private TextField _name, _note;
        private VisualElement _weaponChoices;
        private List<string> _families = new List<string>();
        private VisualElement _body, _rows, _testSlots, _testWeapon;
        private Foldout _testFold;

        /// <summary>Opens the tool.</summary>
        [MenuItem("Evermeadow/Skills/Moveset Tool", priority = 310)]
        public static void Open()
        {
            var w = GetWindow<MovesetToolWindow>();
            w.titleContent = new GUIContent("Moveset Tool");
            w.minSize = new Vector2(980, 420);
        }

        /// <summary>Checks every moveset file and logs the problems.</summary>
        [MenuItem("Evermeadow/Skills/Validate Moveset Folder", priority = 311)]
        public static void ValidateMenu() => LogValidation();

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            var bar = new Toolbar();
            Button(bar, "New", NewMoveset, "Moveset mới với id kế tiếp còn trống (từ 100000)");
            Button(bar, "Open ▾", ShowOpenMenu, "Mở một moveset trong " + MovesetToolFiles.Folder);
            Button(bar, "Save", Save, "Ghi <id>.xml (Ctrl+S). Không lưu nếu có lỗi. Đang Play thì game dùng ngay");
            Button(bar, "Save As", SaveAsNew, "Chép moveset sang id kế tiếp còn trống");
            bar.Add(new ToolbarSpacer { style = { width = 10 } });
            Button(bar, "Undo", Undo, "Hoàn tác (Ctrl+Z)");
            Button(bar, "Redo", Redo, "Làm lại (Ctrl+Y)");
            bar.Add(new ToolbarSpacer { flex = true });
            Button(bar, "Validate Folder", () => SetStatus(LogValidation(), false), "Kiểm mọi moveset như game, kể cả skill id có tồn tại");
            _title = new Label { style = { unityTextAlign = TextAnchor.MiddleRight, marginLeft = 8, marginRight = 6, unityFontStyleAndWeight = FontStyle.Bold } };
            bar.Add(_title);
            root.Add(bar);

            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal) { style = { flexGrow = 1 } };
            _body = new VisualElement { style = { paddingLeft = 6, paddingRight = 6, paddingTop = 4 } };
            scroll.Add(_body);
            root.Add(scroll);

            _name = new TextField("Name") { isDelayed = true, tooltip = "Tên để người làm game nhận ra (không phải chữ hiện cho người chơi), vd 'Đánh thường – chuỗi 3 nhát'" };
            _name.RegisterValueChangedCallback(e => Edit(() => _data.Name = e.newValue ?? ""));
            _note = new TextField("Note") { isDelayed = true, multiline = true, tooltip = "Ghi chú tự do cho người làm game" };
            _note.style.minHeight = 36;
            _note.RegisterValueChangedCallback(e => Edit(() => _data.Note = e.newValue ?? ""));
            _body.Add(_name);
            _body.Add(WeaponsField());
            _body.Add(_note);

            _body.Add(new Label("Rows — mỗi dòng: input + điều kiện → skill") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8, marginBottom = 2 } });
            _body.Add(HeaderRow());
            _rows = new VisualElement();
            _body.Add(_rows);
            var add = new Button(() => Edit(() => _data.Rows.Add(_data.Rows.Count > 0 ? _data.Rows[_data.Rows.Count - 1].Clone() : new MovesetRowData { Skill = "100000" }), rebuild: true))
            {
                text = "+ Row",
                tooltip = "Thêm dòng (chép dòng cuối)",
                style = { width = 80, marginTop = 4 }
            };
            _body.Add(add);
            _problems = new Label { style = { whiteSpace = WhiteSpace.Normal, marginTop = 6, color = new Color(1f, 0.6f, 0.5f) } };
            _body.Add(_problems);

            _testFold = new Foldout { text = "Socket test — thử khảm đá (moveset) vào vũ khí", value = false, style = { marginTop = 10 } };
            _testFold.RegisterValueChangedCallback(e =>
            {
                if (e.target == _testFold) RefreshSocketTest();
            });
            var weapon = new VisualElement();   // filled by RefreshSocketTest: a popup of the family list
            _testWeapon = weapon;
            var sockets = new SliderInt("Sockets", MovesetLoadout.MinSockets, MovesetLoadout.MaxSockets) { value = Mathf.Clamp(_testSocketCount, 2, 4), showInputField = true, tooltip = "Số ô khảm của vũ khí (2–4)" };
            sockets.RegisterValueChangedCallback(e =>
            {
                _testSocketCount = e.newValue;
                RefreshSocketTest();
            });
            _testSlots = new VisualElement();
            _testResult = new Label { style = { whiteSpace = WhiteSpace.Normal, marginTop = 4 } };
            _testFold.Add(weapon);
            _testFold.Add(sockets);
            _testFold.Add(_testSlots);
            _testFold.Add(_testResult);
            _body.Add(_testFold);

            _status = new Label { style = { whiteSpace = WhiteSpace.Normal, paddingLeft = 6, paddingRight = 6, paddingTop = 3, paddingBottom = 3, minHeight = 20 } };
            root.Add(_status);
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            _knownSkills = MovesetToolFiles.KnownSkills();
            _families = MovesetToolFiles.LoadWeaponFamilies();
            Restore();
        }

        private void OnFocus()
        {
            if (_rows == null) return;
            _knownSkills = MovesetToolFiles.KnownSkills();   // skills may have been added in the Skill Tool
            _families = MovesetToolFiles.LoadWeaponFamilies();   // the family list may have been edited
            RebuildWeapons();
            Revalidate();
        }

        private void OnDisable()
        {
            if (_data != null && _dirty) _wipXml = MovesetXml.Write(_data);   // keep unsaved edits across a domain reload / Play Mode
        }

        /// <inheritdoc />
        public override void SaveChanges()
        {
            if (_dirty) Save();
            base.SaveChanges();
        }

        // ------------------------------------------------------------------ open / new / save

        private void Restore()
        {
            _data = null;
            if (!string.IsNullOrEmpty(_wipXml) && MovesetXml.TryRead(_wipXml, _id, out MovesetData wip, null))
            {
                _data = wip;
                SetStatus("Restored unsaved edits of moveset " + _id + ".", false);
            }
            else if (MovesetXml.IsMovesetId(_id))
            {
                _dirty = false;
                LoadFile(_id);
                return;
            }
            else SetStatus("New creates a moveset with the next free id; Open ▾ lists the files in " + MovesetToolFiles.Folder + ".", false);
            ResetUndo();
            RebuildAll();
        }

        private void NewMoveset()
        {
            if (!ConfirmDiscard()) return;
            string id = MovesetToolFiles.NextId();
            if (id == null)
            {
                SetStatus("No free id left (999999 is in use).", true);
                return;
            }
            _id = id;
            _data = MovesetData.CreateDefault(id);
            if (_knownSkills.Count > 0) _data.Rows[0].Skill = _knownSkills.OrderBy(s => s, StringComparer.Ordinal).First();
            _wipXml = "";
            _dirty = true;
            ResetUndo();
            RebuildAll();
            SetStatus($"New moveset {id} — Save to create {MovesetToolFiles.Folder}/{id}.xml.", false);
        }

        private void ShowOpenMenu()
        {
            var menu = new GenericMenu();
            List<MovesetFileInfo> files = MovesetToolFiles.ListMovesets();
            if (files.Count == 0) menu.AddDisabledItem(new GUIContent("No moveset files yet"));
            foreach (MovesetFileInfo f in files)
            {
                string id = f.Id;
                menu.AddItem(new GUIContent(f.Label.Replace("/", "∕")), id == _id, () =>
                {
                    if (ConfirmDiscard()) LoadFile(id);
                });
            }
            menu.ShowAsContext();
        }

        private void LoadFile(string id)
        {
            var errors = new List<SkillError>();
            MovesetData m = MovesetToolFiles.Load(id, errors);
            if (m == null)
            {
                SetStatus($"Cannot open {id}: " + Join(errors), true);
                return;
            }
            _id = id;
            _data = m;
            _wipXml = "";
            _dirty = false;
            ResetUndo();
            RebuildAll();
            if (errors.Count > 0) SetStatus($"Opened {MovesetToolFiles.PathOf(id)} — {errors.Count} problem(s) to fix.", true);
            else SetStatus($"Opened {MovesetToolFiles.PathOf(id)}.", false);
        }

        private void Save() => SaveAs(null);

        private void SaveAsNew()
        {
            string id = MovesetToolFiles.NextId();
            if (id == null) SetStatus("No free id left.", true);
            else SaveAs(id);
        }

        private void SaveAs(string newId)
        {
            if (_data == null)
            {
                SetStatus("No moveset open.", true);
                return;
            }
            MovesetData m = _data.Clone();
            if (newId != null) m.Id = newId;
            _knownSkills = MovesetToolFiles.KnownSkills();
            List<SkillError> errors = MovesetToolFiles.Save(m);
            if (errors.Count > 0)
            {
                SetStatus("Not saved: " + Join(errors), true);
                return;
            }
            _data = m;
            _id = m.Id;
            _wipXml = "";
            _dirty = false;
            if (newId != null) RebuildAll();
            string live = MovesetHotReload.Push(MovesetToolFiles.PathOf(m.Id));
            SetStatus($"Saved {MovesetToolFiles.PathOf(m.Id)}." + (live.Length > 0 ? " " + live : ""), live.Contains("errors"));
            Refresh();
            RefreshSocketTest();
        }

        // ------------------------------------------------------------------ editing

        private void Edit(Action change, bool rebuild = false, bool weapons = false)
        {
            if (_data == null) return;
            change();
            _dirty = true;
            if (rebuild) RebuildRows();
            if (weapons) RebuildWeapons();
            Capture();
            Revalidate();
            Refresh();
            RefreshSocketTest();
        }

        // "Weapons": one toggle for Any and one per family of the list; families in the file but not in the list show in orange.
        private VisualElement WeaponsField()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.FlexStart, marginTop = 2, marginBottom = 2 } };
            row.Add(new Label("Weapons")
            {
                tooltip = "Loại vũ khí khảm được đá mang moveset này. Any = mọi loại; hoặc chọn một / nhiều họ vũ khí",
                style = { width = 150, minWidth = 150, paddingLeft = 3, paddingTop = 2 }
            });
            _weaponChoices = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, flexGrow = 1, flexShrink = 1 } };
            row.Add(_weaponChoices);
            return row;
        }

        private void RebuildWeapons()
        {
            if (_weaponChoices == null) return;
            _weaponChoices.Clear();
            if (_data == null) return;
            var any = new Toggle("Any") { value = _data.AnyWeapon, tooltip = "Mọi loại vũ khí đều khảm được", style = { marginRight = 14 } };
            any.labelElement.style.minWidth = 0;
            any.RegisterValueChangedCallback(e => Edit(() =>
            {
                _data.AnyWeapon = e.newValue;
                if (e.newValue) _data.Weapons.Clear();
            }, weapons: true));
            _weaponChoices.Add(any);

            var shown = new List<string>(_families);
            foreach (string w in _data.Weapons)
                if (!shown.Contains(w)) shown.Add(w);   // in the file but not in the list: still visible so it can be removed
            foreach (string family in shown)
            {
                string f = family;
                bool known = _families.Contains(f);
                var t = new Toggle(f)
                {
                    value = !_data.AnyWeapon && _data.Weapons.Contains(f),
                    tooltip = known ? "Họ vũ khí " + f : "'" + f + "' không có trong danh sách " + MovesetToolFiles.WeaponFamiliesPath + " — bỏ chọn hoặc thêm vào danh sách",
                    style = { marginRight = 10 }
                };
                if (!known) t.labelElement.style.color = new Color(1f, 0.6f, 0.2f);
                t.labelElement.style.minWidth = 0;
                t.RegisterValueChangedCallback(e => Edit(() =>
                {
                    if (e.newValue)
                    {
                        _data.AnyWeapon = false;
                        if (!_data.Weapons.Contains(f)) _data.Weapons.Add(f);
                        _data.Weapons = shown.Where(_data.Weapons.Contains).ToList();   // keep the list order
                    }
                    else _data.Weapons.Remove(f);
                }, weapons: true));
                _weaponChoices.Add(t);
            }
            _weaponChoices.Add(new Button(EditFamilyList) { text = "Sửa danh sách…", tooltip = "Mở " + MovesetToolFiles.WeaponFamiliesPath + " (tạo với 5 họ mặc định nếu chưa có)" });
        }

        private static void EditFamilyList()
        {
            string path = MovesetToolFiles.EnsureWeaponFamiliesFile();
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset != null) AssetDatabase.OpenAsset(asset);
            else EditorUtility.RevealInFinder(SkillToolFiles.FullPath(path));
        }

        private void RebuildAll()
        {
            bool open = _data != null;
            _body.SetEnabled(open);
            _name.SetValueWithoutNotify(open ? _data.Name : "");
            RebuildWeapons();
            _note.SetValueWithoutNotify(open ? _data.Note : "");
            RebuildRows();
            Revalidate();
            Refresh();
            RefreshSocketTest();
        }

        private void RebuildRows()
        {
            _rows.Clear();
            if (_data == null) return;
            for (int i = 0; i < _data.Rows.Count; i++) _rows.Add(RowElement(i));
        }

        private static readonly string[] ColumnNames = { "#", "Button", "Gesture", "+ Button", "Context", "Moving", "Dir", "Skill", "", "After", "Link", "Prio", "Cooldown" };

        private static readonly string[] ColumnTips =
        {
            "# — thứ tự dòng: khi mọi thứ khác bằng nhau, dòng trên thắng.",
            "Button — nút logic (đổi phím được). Mặc định: Button1 chuột trái, Button2 chuột phải, Button3 chuột giữa, Button4/5 nút bên chuột, Key1–4 = Q E R F.",
            "Gesture — Press: nhấn · DoubleTap: nhấn đúp · Hold: giữ · Chord: nhấn cùng lúc với nút thứ hai.",
            "+ Button — nút thứ hai của Chord (vd Button1 + Button2 = chuột trái + phải cùng lúc).",
            "Context — dùng được ở đâu: Ground (trên đất), Air (trên không), Dash (đang lướt / né). Chọn nhiều được; Everything = cả ba.",
            "Moving — chỉ cho dòng chỉ-Ground: Still đứng yên, Moving đang chạy, Any cả hai.",
            "Dir — hướng đang giữ lúc bấm, so với hướng mặt: any, neutral (không giữ), up, down, forward, back.",
            "Skill — file Skill Tool thể hiện đòn này; bấm để chọn. Chữ cam = skill không tồn tại.",
            "↗ — mở skill này trong Skill Tool.",
            "After — đòn nối: id các skill mà đòn ngay trước phải là (cách nhau bằng dấu phẩy). Trống = đòn mở đầu. Vd chuỗi 1→2→3.",
            "Link — link window (giây): sau khi đòn trước kết thúc, còn bao lâu thì đòn nối vẫn ra. Chỉ dùng khi có After.",
            "Prio — ưu tiên: số lớn thắng khi nhiều dòng cùng khớp.",
            "Cooldown — hồi chiêu (giây) của dòng này, 0 = không có."
        };

        private const string HelpIdle = "Di chuột lên tên cột hoặc một ô để xem giải thích.";

        private VisualElement HeaderRow()
        {
            var block = new VisualElement();
            var line = Line();
            for (int i = 0; i < ColumnNames.Length; i++)
                line.Add(Col(new Label(ColumnNames[i]) { style = { unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 3 } }, i));
            block.Add(line);
            _help = new Label(HelpIdle)
            {
                style = { whiteSpace = WhiteSpace.Normal, color = new Color(0.65f, 0.75f, 0.85f), minHeight = 18, marginBottom = 2, paddingLeft = 3, unityFontStyleAndWeight = FontStyle.Italic }
            };
            block.Add(_help);
            return block;
        }

        // Fixed column width + the column's explanation as tooltip and in the help line under the header (tooltips need the
        // mouse to rest and the window to have focus; the help line always shows).
        private T Col<T>(T e, int column) where T : VisualElement
        {
            Sized(e, column);
            if (string.IsNullOrEmpty(e.tooltip)) e.tooltip = ColumnTips[column];
            e.RegisterCallback<PointerEnterEvent>(_ => { if (_help != null) _help.text = ColumnTips[column]; });
            e.RegisterCallback<PointerLeaveEvent>(_ => { if (_help != null) _help.text = HelpIdle; });
            return e;
        }

        private VisualElement RowElement(int index)
        {
            MovesetRowData r = _data.Rows[index];
            var line = Line();
            line.Add(new Label(index.ToString()) { style = { width = W[0], unityTextAlign = TextAnchor.MiddleCenter } });

            var button = Col(new EnumField(r.Input.Button), 1);
            var gesture = Col(new EnumField(r.Input.Gesture), 2);
            MoveButton secondInitial = r.Input.Gesture == MoveGesture.Chord ? r.Input.Second : (MoveButton)(((int)r.Input.Button + 1) % ButtonCount);
            var second = Col(new EnumField(secondInitial), 3);
            second.style.visibility = r.Input.Gesture == MoveGesture.Chord ? Visibility.Visible : Visibility.Hidden;
            void ApplyInput()
            {
                var b = (MoveButton)button.value;
                var g = (MoveGesture)gesture.value;
                var s = (MoveButton)second.value;
                if (g == MoveGesture.Chord && s == b)
                {
                    s = (MoveButton)(((int)b + 1) % ButtonCount);
                    second.SetValueWithoutNotify(s);
                }
                second.style.visibility = g == MoveGesture.Chord ? Visibility.Visible : Visibility.Hidden;
                Edit(() => r.Input = g == MoveGesture.DoubleTap ? MoveInput.DoubleTap(b) : g == MoveGesture.Hold ? MoveInput.Hold(b) : g == MoveGesture.Chord ? MoveInput.Chord(b, s) : MoveInput.Press(b));
            }
            button.RegisterValueChangedCallback(_ => ApplyInput());
            gesture.RegisterValueChangedCallback(_ => ApplyInput());
            second.RegisterValueChangedCallback(_ => ApplyInput());
            line.Add(button);
            line.Add(gesture);
            line.Add(second);

            var context = Col(new EnumFlagsField(r.Context), 4);
            context.RegisterValueChangedCallback(e => Edit(() => r.Context = (SkillContext)Convert.ToInt32(e.newValue) & MovesetXml.AllContexts));
            line.Add(context);

            var moving = Col(new EnumField(r.Moving), 5);
            moving.RegisterValueChangedCallback(e => Edit(() => r.Moving = (MovingFilter)e.newValue));
            line.Add(moving);

            var dir = Col(new PopupField<string>(Dirs, r.AnyDir ? "any" : MovesetXml.DirText(r.Dir)), 6);
            dir.RegisterValueChangedCallback(e => Edit(() =>
            {
                r.AnyDir = e.newValue == "any";
                if (!r.AnyDir) r.Dir = (SkillDir)Enum.Parse(typeof(SkillDir), e.newValue, true);
            }));
            line.Add(dir);

            var skill = Col(new Button { text = SkillText(r.Skill), style = { unityTextAlign = TextAnchor.MiddleLeft } }, 7);
            skill.style.color = _knownSkills.Contains(r.Skill) ? new Color(0.9f, 0.9f, 0.9f) : new Color(1f, 0.6f, 0.2f);
            skill.clicked += () => ShowSkillMenu(r, skill);
            line.Add(skill);
            line.Add(Col(new Button(() => SkillToolWindow.OpenSkill(r.Skill)) { text = "↗", tooltip = "Mở skill " + r.Skill + " trong Skill Tool" }, 8));

            var link = Col(new FloatField { value = (float)r.LinkWindow, isDelayed = true }, 10);
            var after = Col(new TextField { value = string.Join(",", r.After), isDelayed = true }, 9);
            after.RegisterValueChangedCallback(e => Edit(() =>
            {
                r.After = (e.newValue ?? "").Split(',').Select(a => a.Trim()).Where(a => a.Length > 0).ToList();
                link.SetEnabled(r.After.Count > 0);
            }));
            link.SetEnabled(r.After.Count > 0);
            link.RegisterValueChangedCallback(e => Edit(() => r.LinkWindow = Math.Round(e.newValue, 4)));
            line.Add(after);
            line.Add(link);

            var priority = Col(new IntegerField { value = r.Priority, isDelayed = true }, 11);
            priority.RegisterValueChangedCallback(e => Edit(() => r.Priority = e.newValue));
            line.Add(priority);
            var cooldown = Col(new FloatField { value = (float)r.Cooldown, isDelayed = true }, 12);
            cooldown.RegisterValueChangedCallback(e => Edit(() => r.Cooldown = Math.Round(e.newValue, 4)));
            line.Add(cooldown);

            line.Add(Small("↑", "Lên", () => Move(index, -1)));
            line.Add(Small("↓", "Xuống", () => Move(index, +1)));
            line.Add(Small("⧉", "Nhân đôi dòng", () => Edit(() => _data.Rows.Insert(index + 1, r.Clone()), rebuild: true)));
            line.Add(Small("✕", "Xóa dòng", () => Edit(() => _data.Rows.RemoveAt(index), rebuild: true)));
            return line;
        }

        private void Move(int index, int delta)
        {
            int to = index + delta;
            if (to < 0 || to >= _data.Rows.Count) return;
            Edit(() =>
            {
                MovesetRowData row = _data.Rows[index];
                _data.Rows.RemoveAt(index);
                _data.Rows.Insert(to, row);
            }, rebuild: true);
        }

        private void ShowSkillMenu(MovesetRowData r, Button target)
        {
            var menu = new GenericMenu();
            List<SkillFileInfo> skills = SkillToolFiles.ListSkills();
            if (skills.Count == 0) menu.AddDisabledItem(new GUIContent("No skill files yet — create one in the Skill Tool"));
            foreach (SkillFileInfo s in skills)
            {
                string id = s.Id;
                menu.AddItem(new GUIContent(s.Label.Replace("/", "∕")), id == r.Skill, () =>
                {
                    _knownSkills.Add(id);
                    target.text = SkillText(id);
                    target.style.color = new Color(0.9f, 0.9f, 0.9f);
                    Edit(() => r.Skill = id);
                });
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Open Skill Tool"), false, SkillToolWindow.Open);
            menu.ShowAsContext();
        }

        private void Revalidate()
        {
            if (_problems == null) return;
            if (_data == null)
            {
                _problems.text = "";
                return;
            }
            var errors = new List<SkillError>();
            MovesetValidator.Validate(_data, errors, _knownSkills, new HashSet<string>(_families));
            _problems.text = errors.Count == 0 ? "" : $"{errors.Count} problem(s) — Save is blocked until fixed:\n" + string.Join("\n", errors.Select(e => $"• [{e.Code}] {e.Message}"));
        }

        // ------------------------------------------------------------------ socket test

        private void RefreshSocketTest()
        {
            if (_testFold == null || !_testFold.value) return;
            var familyChoices = new List<string>(_families);
            if (familyChoices.Count == 0) familyChoices.Add("weapon");
            if (!familyChoices.Contains(_testWeaponFamily)) _testWeaponFamily = familyChoices[0];
            _testWeapon.Clear();
            var weaponPopup = new PopupField<string>("Weapon family", familyChoices, _testWeaponFamily) { tooltip = "Họ vũ khí đang cầm khi thử" };
            weaponPopup.RegisterValueChangedCallback(e =>
            {
                _testWeaponFamily = e.newValue;
                RefreshSocketTest();
            });
            _testWeapon.Add(weaponPopup);
            var errors = new List<SkillError>();
            MovesetLibrary lib = MovesetToolFiles.LoadLibrary(_data, errors);
            List<string> ids = lib.All.Select(d => d.Id).OrderBy(i => i, StringComparer.Ordinal).ToList();
            var choices = new List<string> { Empty };
            choices.AddRange(ids.Select(i => Label(lib, i)));
            while (_testIds.Count < _testSocketCount) _testIds.Add("");
            while (_testIds.Count > _testSocketCount) _testIds.RemoveAt(_testIds.Count - 1);

            _testSlots.Clear();
            for (int s = 0; s < _testSocketCount; s++)
            {
                int slot = s;
                string current = lib.TryGet(_testIds[s], out _) ? Label(lib, _testIds[s]) : Empty;
                var popup = new PopupField<string>($"Socket {s}", choices, current);
                popup.RegisterValueChangedCallback(e =>
                {
                    _testIds[slot] = e.newValue == Empty ? "" : e.newValue.Split(' ')[0];
                    RefreshSocketTest();
                });
                _testSlots.Add(popup);
            }

            var sb = new StringBuilder();
            MovesetLoadout loadout;
            try { loadout = new MovesetLoadout(_testWeaponFamily, _testSocketCount); }
            catch (ArgumentException e)
            {
                _testResult.text = e.Message;
                return;
            }
            for (int s = 0; s < _testSocketCount; s++)
            {
                if (!lib.TryGet(_testIds[s], out MovesetDef def))
                {
                    sb.AppendLine($"Socket {s}: empty");
                    continue;
                }
                var conflicts = new List<MoveConflict>();
                SocketResult r = loadout.TrySocket(s, def, lib, conflicts);
                sb.AppendLine($"Socket {s}: {def.Id} → {(r == SocketResult.Ok ? "OK" : "REFUSED: " + r)}");
                foreach (MoveConflict c in conflicts) sb.AppendLine("    " + c);
            }
            sb.AppendLine();
            sb.AppendLine("Input map (what each press does with this weapon):");
            bool any = false;
            for (int s = 0; s < loadout.SocketCount; s++)
            {
                if (!lib.TryGet(loadout.Get(s), out MovesetDef def)) continue;
                foreach (MovesetRow row in def.Rows)
                {
                    any = true;
                    string cond = MovesetXml.ContextText(row.Context) + (row.Moving != MovingFilter.Any ? " " + row.Moving.ToString().ToLowerInvariant() : "") +
                                  (row.AnyDir ? "" : " " + MovesetXml.DirText(row.Dir)) + (row.HasAfter ? " after " + string.Join(",", row.AfterIds) : "");
                    sb.AppendLine($"  {row.Input,-18} {cond,-30} → skill {row.SkillId}   (socket {s}, moveset {def.Id} row {row.Order}{(row.Priority != 0 ? ", prio " + row.Priority : "")}{(row.CooldownTicks > 0 ? ", cooldown" : "")})");
                }
            }
            if (!any) sb.AppendLine("  (nothing — every move comes from a socketed stone)");
            if (errors.Count > 0) sb.AppendLine().AppendLine($"{errors.Count} moveset file(s) with errors are not offered (Validate Folder).");
            _testResult.text = sb.ToString();
        }

        private static string Label(MovesetLibrary lib, string id) =>
            lib.TryGet(id, out MovesetDef d) && d.Name.Length > 0 ? $"{id} {d.Name.Replace("/", "∕")}" : id;

        // ------------------------------------------------------------------ undo / redo

        private void ResetUndo()
        {
            _undo.Clear();
            _redo.Clear();
            if (_data != null) _undo.Add(MovesetXml.Write(_data));
        }

        private void Capture()
        {
            string xml = MovesetXml.Write(_data);
            if (_undo.Count > 0 && _undo[_undo.Count - 1] == xml) return;
            _undo.Add(xml);
            if (_undo.Count > MaxUndo) _undo.RemoveAt(0);
            _redo.Clear();
        }

        private void Undo()
        {
            if (_undo.Count < 2)
            {
                SetStatus("Nothing to undo.", false);
                return;
            }
            _redo.Add(_undo[_undo.Count - 1]);
            _undo.RemoveAt(_undo.Count - 1);
            ApplySnapshot(_undo[_undo.Count - 1]);
        }

        private void Redo()
        {
            if (_redo.Count == 0)
            {
                SetStatus("Nothing to redo.", false);
                return;
            }
            string xml = _redo[_redo.Count - 1];
            _redo.RemoveAt(_redo.Count - 1);
            _undo.Add(xml);
            ApplySnapshot(xml);
        }

        private void ApplySnapshot(string xml)
        {
            if (!MovesetXml.TryRead(xml, _id, out MovesetData m, null)) return;
            _data = m;
            _dirty = true;
            RebuildAll();
        }

        // ------------------------------------------------------------------ shared

        private void Refresh()
        {
            hasUnsavedChanges = _dirty;
            saveChangesMessage = "The Moveset Tool has unsaved changes. Save them?";
            if (_title != null) _title.text = (_data != null ? "Moveset " + _data.Id : "No moveset") + (_dirty ? "  •" : "");
        }

        private void SetStatus(string text, bool error)
        {
            if (_status == null) return;
            _status.text = text;
            _status.style.color = error ? new Color(1f, 0.55f, 0.5f) : new Color(0.8f, 0.8f, 0.8f);
        }

        private void OnKeyDown(KeyDownEvent e)
        {
            if (!(e.ctrlKey || e.commandKey)) return;
            if (e.keyCode == KeyCode.S) Save();
            else if (e.keyCode == KeyCode.Z && !e.shiftKey) Undo();
            else if (e.keyCode == KeyCode.Y || e.keyCode == KeyCode.Z && e.shiftKey) Redo();
            else return;
            e.StopPropagation();
        }

        private bool ConfirmDiscard() =>
            !_dirty || EditorUtility.DisplayDialog("Unsaved changes", "Discard the unsaved changes of this moveset?", "Discard", "Cancel");

        private static string SkillText(string id) => (string.IsNullOrEmpty(id) ? "(chọn skill)" : id) + "  ▾";

        private static VisualElement Line() =>
            new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, minHeight = 22, flexShrink = 0 } };

        private static T Sized<T>(T e, int column) where T : VisualElement
        {
            e.style.width = W[column];
            e.style.minWidth = W[column];
            e.style.marginLeft = 1;
            e.style.marginRight = 1;
            return e;
        }

        private static Button Small(string text, string tooltip, Action action) =>
            new Button(action) { text = text, tooltip = tooltip, style = { width = 22, paddingLeft = 0, paddingRight = 0 } };

        private static void Button(Toolbar bar, string text, Action action, string tooltip) =>
            bar.Add(new ToolbarButton(action) { text = text, tooltip = tooltip });

        private static string Join(List<SkillError> errors) =>
            string.Join("  ·  ", errors.Take(5).Select(e => $"[{e.Code}] {e.Message}")) + (errors.Count > 5 ? $"  … (+{errors.Count - 5})" : "");

        private static string LogValidation()
        {
            List<SkillError> errors = MovesetToolFiles.ValidateFolder(out int count);
            if (errors.Count == 0)
            {
                string ok = $"Moveset folder clean: {count} moveset(s) load and every skill exists.";
                Debug.Log("[Moveset] " + ok);
                return ok;
            }
            foreach (SkillError e in errors) Debug.LogWarning("[Moveset] " + e);
            return $"{errors.Count} problem(s) in the moveset folder (see Console): " + string.Join("  ·  ", errors.Take(3));
        }
    }
}
