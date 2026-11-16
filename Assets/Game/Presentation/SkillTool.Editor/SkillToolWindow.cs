using System.Collections.Generic;
using System.Linq;
using Evermeadow.Core.Skills;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Evermeadow.Presentation.SkillTool.Editor
{
    /// <summary>
    /// The Skill Tool (menu <c>Evermeadow ▸ Skills ▸ Skill Tool</c>), modelled on the reference tool (user decision
    /// 2026-11-09): a skill is an event graph — Start and nodes (Animation, Effect, Search, Hit, Move, MoveControl, LookAt,
    /// CameraAction, Knockback, Buff) linked parent → child with a trigger on each edge, and data wires between outputs and
    /// fields. Toolbar: New, Open, Save, Save As, Memo, Copy, Paste, Undo, Redo. The XML files in
    /// <c>Assets/StreamingAssets/Skills/</c> are the only data; a skill's id is its file name. Editor only.
    /// </summary>
    public sealed class SkillToolWindow : EditorWindow
    {
        private const int MaxUndo = 100;

        [SerializeField] private string _skillId = "";
        [SerializeField] private string _wipXml = "";
        [SerializeField] private bool _skillDirty;

        private readonly List<string> _undo = new List<string>();
        private readonly List<string> _redo = new List<string>();
        private bool _restoring, _capturePending;
        private SkillGraphView _graph;
        private Label _title, _status;

        /// <summary>Opens the tool.</summary>
        [MenuItem("Evermeadow/Skills/Skill Tool", priority = 300)]
        public static void Open()
        {
            var w = GetWindow<SkillToolWindow>();
            w.titleContent = new GUIContent("Skill Tool");
            w.minSize = new Vector2(820, 480);
        }

        /// <summary>Opens the tool on skill <paramref name="id"/> (asks first when the open skill has unsaved changes).</summary>
        public static void OpenSkill(string id)
        {
            Open();
            var w = GetWindow<SkillToolWindow>();
            if (w._skillId == id || !SkillXmlReader.IsSkillId(id) || !ConfirmDiscard(w._skillDirty)) return;
            if (w._graph == null)
            {
                w._skillId = id;   // CreateGUI has not run yet: RestoreSkill opens it
                w._wipXml = "";
                w._skillDirty = false;
                return;
            }
            if (w.TryLoadSkill(id)) w._skillDirty = false;
            w.Refresh();
        }

        /// <summary>Loads the skills folder like the game and logs every problem.</summary>
        [MenuItem("Evermeadow/Skills/Validate Skill Folder", priority = 301)]
        public static void ValidateMenu() => LogValidation();

        /// <summary>Shows the skills folder in the file browser.</summary>
        [MenuItem("Evermeadow/Skills/Show Skill Folder", priority = 302)]
        public static void ShowFolder()
        {
            string full = SkillToolFiles.FullPath(SkillToolFiles.Folder);
            if (System.IO.Directory.Exists(full)) EditorUtility.RevealInFinder(full);
            else Debug.Log($"[Skills] {SkillToolFiles.Folder} does not exist yet: save a skill first.");
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            var bar = new Toolbar();
            Button(bar, "New", NewSkill, "Kỹ năng mới với id kế tiếp còn trống (từ 100000)");
            Button(bar, "Open ▾", ShowOpenSkillMenu, "Mở một file kỹ năng trong thư mục Skills");
            Button(bar, "Save", SaveSkill, "Ghi <id>.xml (Ctrl+S). Không lưu nếu kỹ năng có lỗi");
            Button(bar, "Save As", SaveSkillAsNew, "Chép kỹ năng sang id kế tiếp còn trống");
            bar.Add(new ToolbarSpacer { style = { width = 10 } });
            Button(bar, "Memo", () => _graph.AddMemoAtCenter(), "Thêm ghi chú vàng vào giữa canvas");
            Button(bar, "Copy", () => _graph.CopySelection(), "Chép các node / memo đang chọn (Ctrl+C)");
            Button(bar, "Paste", () => _graph.PasteClipboard(), "Dán node / memo đã chép (Ctrl+V)");
            Button(bar, "Undo", Undo, "Hoàn tác (Ctrl+Z)");
            Button(bar, "Redo", Redo, "Làm lại (Ctrl+Y)");
            bar.Add(new ToolbarSpacer { flex = true });
            Button(bar, "Validate Folder", () => SetStatus(LogValidation(), false), "Nạp cả thư mục như game và báo lỗi");
            _title = new Label { style = { unityTextAlign = TextAnchor.MiddleRight, marginLeft = 8, marginRight = 6, unityFontStyleAndWeight = FontStyle.Bold } };
            bar.Add(_title);
            root.Add(bar);

            _graph = new SkillGraphView();
            _graph.Changed += OnGraphChanged;
            var pane = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            pane.Add(new SkillNodePalette(_graph));
            pane.Add(_graph);
            root.Add(pane);

            _status = new Label { style = { whiteSpace = WhiteSpace.Normal, paddingLeft = 6, paddingRight = 6, paddingTop = 3, paddingBottom = 3, minHeight = 20 } };
            root.Add(_status);

            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            RestoreSkill();
        }

        private void OnDisable()
        {
            // Keep unsaved edits across a domain reload.
            if (_graph?.StartNode != null && _skillDirty) _wipXml = SkillXmlWriter.Write(_graph.Export());
        }

        /// <inheritdoc />
        public override void SaveChanges()
        {
            if (_skillDirty) SaveSkill();
            base.SaveChanges();
        }

        // ------------------------------------------------------------------ open / new / save

        private void RestoreSkill()
        {
            if (!string.IsNullOrEmpty(_wipXml) && SkillXmlReader.TryParseData(_wipXml, _skillId, out SkillGraphData wip, new List<SkillError>()))
            {
                LoadGraph(wip, true);
                SetStatus("Restored unsaved edits of " + _skillId + ".", false);
            }
            else if (SkillXmlReader.IsSkillId(_skillId) && TryLoadSkill(_skillId))
            {
                _skillDirty = false;
            }
            else
            {
                SetStatus("New creates a skill with the next free id; Open ▾ lists the skills in " + SkillToolFiles.Folder + ".", false);
            }
            Refresh();
        }

        private void NewSkill()
        {
            if (!ConfirmDiscard(_skillDirty)) return;
            string id = SkillToolFiles.NextId();
            if (id == null)
            {
                SetStatus("No free id left (999999 is in use).", true);
                return;
            }
            _skillId = id;
            _wipXml = "";
            LoadGraph(SkillGraphData.CreateDefault(id), true);
            _skillDirty = true;
            SetStatus($"New skill {id} — Save to create {SkillToolFiles.Folder}/{id}.xml.", false);
            Refresh();
        }

        private void ShowOpenSkillMenu()
        {
            var menu = new GenericMenu();
            List<SkillFileInfo> skills = SkillToolFiles.ListSkills();
            if (skills.Count == 0) menu.AddDisabledItem(new GUIContent("No skill files yet"));
            foreach (SkillFileInfo s in skills)
            {
                string id = s.Id;
                menu.AddItem(new GUIContent(s.Label.Replace("/", "∕")), id == _skillId, () =>
                {
                    if (!ConfirmDiscard(_skillDirty)) return;
                    if (TryLoadSkill(id)) _skillDirty = false;
                    Refresh();
                });
            }
            menu.ShowAsContext();
        }

        private bool TryLoadSkill(string id)
        {
            var errors = new List<SkillError>();
            SkillGraphData g = SkillToolFiles.Load(id, errors);
            if (g == null)
            {
                SetStatus($"Cannot open {id}: " + Join(errors), true);
                return false;
            }
            _skillId = id;
            _wipXml = "";
            LoadGraph(g, true);
            if (errors.Count > 0) SetStatus($"Opened {SkillToolFiles.PathOf(id)} — {errors.Count} problem(s) to fix: " + Join(errors), true);
            else SetStatus($"Opened {SkillToolFiles.PathOf(id)}." + Lint(g), false);
            return true;
        }

        private void SaveSkill() => SaveSkillAs(null);

        private void SaveSkillAsNew()
        {
            string id = SkillToolFiles.NextId();
            if (id == null) SetStatus("No free id left.", true);
            else SaveSkillAs(id);
        }

        private void SaveSkillAs(string newId)
        {
            if (_graph.StartNode == null)
            {
                SetStatus("No skill open.", true);
                return;
            }
            SkillGraphData g = _graph.Export();
            if (newId != null) g.Id = newId;
            List<SkillError> errors = SkillToolFiles.Save(g);
            if (errors.Count > 0)
            {
                SetStatus("Not saved: " + Join(errors), true);
                return;
            }
            _skillId = g.Id;
            _wipXml = "";
            _skillDirty = false;
            if (newId != null) LoadGraph(g, false);
            string live = SkillHotReload.Push(SkillToolFiles.PathOf(g.Id));   // Play Mode: the game picks the new version up now
            SetStatus($"Saved {SkillToolFiles.PathOf(g.Id)}." + (live.Length > 0 ? " " + live : "") + Lint(g), live.Contains("errors"));
            Refresh();
        }

        // ------------------------------------------------------------------ undo / redo (snapshots of the graph)

        private void LoadGraph(SkillGraphData g, bool frame)
        {
            _restoring = true;
            _graph.Load(g, frame);
            _restoring = false;
            _undo.Clear();
            _redo.Clear();
            _undo.Add(SkillXmlWriter.Write(_graph.Export()));
        }

        private void OnGraphChanged()
        {
            if (_restoring) return;
            _skillDirty = true;
            Refresh();
            if (_capturePending) return;
            _capturePending = true;
            _graph.schedule.Execute(CaptureSnapshot);   // one snapshot per burst of changes
        }

        private void CaptureSnapshot()
        {
            _capturePending = false;
            if (_graph.StartNode == null) return;
            string xml = SkillXmlWriter.Write(_graph.Export());
            if (_undo.Count > 0 && _undo[_undo.Count - 1] == xml) return;
            _undo.Add(xml);
            if (_undo.Count > MaxUndo) _undo.RemoveAt(0);
            _redo.Clear();
        }

        private void Undo()
        {
            if (_capturePending) CaptureSnapshot();
            if (_undo.Count < 2)
            {
                SetStatus("Nothing to undo.", false);
                return;
            }
            _redo.Add(_undo[_undo.Count - 1]);
            _undo.RemoveAt(_undo.Count - 1);
            Restore(_undo[_undo.Count - 1]);
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
            Restore(xml);
        }

        private void Restore(string xml)
        {
            if (!SkillXmlReader.TryParseData(xml, _skillId, out SkillGraphData g, new List<SkillError>())) return;
            _restoring = true;
            _graph.Load(g, false);
            _restoring = false;
            _skillDirty = true;
            Refresh();
        }

        // ------------------------------------------------------------------ shared

        private void Refresh()
        {
            hasUnsavedChanges = _skillDirty;
            saveChangesMessage = "The Skill Tool has unsaved changes. Save them?";
            if (_title == null) return;
            _title.text = (SkillXmlReader.IsSkillId(_skillId) ? "Skill " + _skillId : "No skill") + (_skillDirty ? "  •" : "");
        }

        private void SetStatus(string text, bool error)
        {
            if (_status == null) return;
            _status.text = text;
            bool warn = text.Contains("⚠");
            _status.style.color = error ? new Color(1f, 0.55f, 0.5f) : warn ? new Color(1f, 0.75f, 0.35f) : new Color(0.8f, 0.8f, 0.8f);
        }

        // Non-blocking advice (SkillLint): e.g. an End that cuts an Animation before it can show.
        private static string Lint(SkillGraphData g)
        {
            List<string> warnings = SkillLint.Warnings(g);
            return warnings.Count == 0 ? "" : "\n⚠ " + string.Join("\n⚠ ", warnings);
        }

        private void OnKeyDown(KeyDownEvent e)
        {
            if (!(e.ctrlKey || e.commandKey)) return;
            if (e.keyCode == KeyCode.S) SaveSkill();
            else if (e.keyCode == KeyCode.Z && !e.shiftKey) Undo();
            else if (e.keyCode == KeyCode.Y || e.keyCode == KeyCode.Z && e.shiftKey) Redo();
            else return;
            e.StopPropagation();
        }

        private static void Button(Toolbar bar, string text, System.Action action, string tooltip) =>
            bar.Add(new ToolbarButton(action) { text = text, tooltip = tooltip });

        private static string Join(List<SkillError> errors) =>
            string.Join("  ·  ", errors.Take(6).Select(e => $"[{e.Code}] {e.Message}")) + (errors.Count > 6 ? $"  … (+{errors.Count - 6})" : "");

        private static bool ConfirmDiscard(bool dirty) =>
            !dirty || EditorUtility.DisplayDialog("Unsaved changes", "Discard the unsaved changes?", "Discard", "Cancel");

        private static string LogValidation()
        {
            List<SkillError> errors = SkillToolFiles.ValidateFolder(out int count);
            if (errors.Count == 0)
            {
                string ok = $"Skill folder clean: {count} skill(s) load without errors.";
                Debug.Log("[Skills] " + ok);
                return ok;
            }
            foreach (SkillError e in errors) Debug.LogWarning("[Skills] " + e);
            return $"{errors.Count} problem(s) in the skill folder (see Console): " + string.Join("  ·  ", errors.Take(3));
        }
    }
}
