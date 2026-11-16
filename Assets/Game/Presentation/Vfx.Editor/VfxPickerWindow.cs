using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Evermeadow.Presentation.Vfx.Editor
{
    /// <summary>
    /// The effect browser: every prefab under <c>Assets/Game/Art/VFX/</c> as a grid (filter by category, search by name) and a
    /// live preview of the selected effect (Play / Pause / Restart, time slider, loop, scale, background). Opened from the
    /// Skill Tool (Effect.PrefabName, Hit.HitEffectAddressable) to pick a key, or from <c>Evermeadow ▸ VFX ▸ Effect Browser</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// VfxPickerWindow.Pick(currentKey, VfxCategory.Hit, key => node.Set("HitEffectAddressable", key));
    /// </code>
    /// </example>
    public sealed class VfxPickerWindow : EditorWindow
    {
        private const float Cell = 92f, Thumb = 72f, PreviewWidth = 380f;
        private static readonly string[] CategoryNames = BuildCategoryNames();

        private Action<string> _onPick;
        private string _search = "";
        private int _category;          // 0 = All, i = (VfxCategory)(i - 1)
        private string _selected = "";
        private VfxIndex _index;
        private Dictionary<string, SkillVfx> _prefabs = new Dictionary<string, SkillVfx>();
        private Vector2 _scroll;
        private VfxPreview _preview;
        private float _time, _scale = 1f;
        private bool _playing = true, _loop = true;
        private Color _background = new Color(0.16f, 0.16f, 0.18f);
        private double _lastTick;

        /// <summary>Opens the browser to pick a key; <paramref name="onPick"/> receives the key ("" for none).</summary>
        public static void Pick(string current, VfxCategory? category, Action<string> onPick)
        {
            var w = CreateInstance<VfxPickerWindow>();
            w.titleContent = new GUIContent("Pick Effect");
            w._onPick = onPick;
            w._selected = current ?? "";
            w._category = category.HasValue ? (int)category.Value + 1 : 0;
            w.minSize = new Vector2(820, 480);
            w.ShowUtility();
            w.Refresh();
        }

        /// <summary>Opens the browser without picking (to look at effects).</summary>
        [MenuItem("Evermeadow/VFX/Effect Browser", priority = 401)]
        public static void Browse() => Pick("", null, null);

        private void OnEnable()
        {
            _lastTick = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            VfxCatalogPostprocessor.EffectsChanged += OnEffectsChanged;
        }

        // A prefab was edited / added / removed (also in Play Mode): rescan and reload the preview, keeping the time.
        private void OnEffectsChanged(IReadOnlyCollection<string> keys)
        {
            float time = _time;
            Refresh();
            _time = time;
            Repaint();
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            VfxCatalogPostprocessor.EffectsChanged -= OnEffectsChanged;
            _preview?.Dispose();
            _preview = null;
        }

        private void Refresh()
        {
            _index = VfxCatalogBuilder.Scan(out _prefabs);
            LoadPreview();
        }

        private void LoadPreview()
        {
            _preview ??= new VfxPreview();
            _prefabs.TryGetValue(_selected ?? "", out SkillVfx prefab);
            _preview.Load(prefab);
            _time = 0f;
        }

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - _lastTick);
            _lastTick = now;
            if (!_playing || _preview?.Effect == null) return;
            _time += dt;
            double life = _preview.Lifetime;
            double span = double.IsInfinity(life) ? 3.0 : life + 0.3;
            if (_time > span) _time = _loop ? 0f : (float)span;
            Repaint();
        }

        private void OnGUI()
        {
            if (_index == null) Refresh();
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawGrid();
            DrawPreviewPane();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _category = GUILayout.Toolbar(_category, CategoryNames, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            GUILayout.Space(8);
            _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(160));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("Refresh", "Quét lại thư mục Assets/Game/Art/VFX"), EditorStyles.toolbarButton)) Refresh();
            if (GUILayout.Button(new GUIContent("New ▾", "Tạo prefab effect mẫu"), EditorStyles.toolbarButton))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Flipbook effect"), false, () => { VfxPrefabFactory.CreateFlipbook(); Refresh(); });
                menu.AddItem(new GUIContent("Particle effect"), false, () => { VfxPrefabFactory.CreateParticle(); Refresh(); });
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            VfxCategory? cat = _category == 0 ? (VfxCategory?)null : (VfxCategory)(_category - 1);
            List<VfxCatalogEntry> items = _index.Filter(_search, cat);
            if (_index.Count == 0)
                EditorGUILayout.HelpBox($"Chưa có effect nào. Tạo prefab trong {VfxNaming.Root}/<Loại>/ (New ▾ hoặc Assets ▸ Create ▸ Evermeadow ▸ Skill VFX), " +
                                        "gắn component SkillVfx, đặt tên vfx_<vùng>_<tên>.", MessageType.Info);
            foreach (string p in _index.Problems) EditorGUILayout.HelpBox(p, MessageType.Warning);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            float width = position.width - PreviewWidth - 24;
            int columns = Mathf.Max(1, Mathf.FloorToInt(width / Cell));
            if (_onPick != null) items.Insert(0, new VfxCatalogEntry("", VfxCategory.Other, ""));   // "None"
            for (int i = 0; i < items.Count; i += columns)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < columns && i + c < items.Count; c++) DrawCell(items[i + c]);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCell(VfxCatalogEntry e)
        {
            Rect r = GUILayoutUtility.GetRect(Cell, Cell + 18, GUILayout.Width(Cell));
            bool selected = e.Key == _selected;
            if (selected) EditorGUI.DrawRect(r, new Color(0.24f, 0.45f, 0.75f, 0.6f));
            var thumbRect = new Rect(r.x + (Cell - Thumb) / 2, r.y + 4, Thumb, Thumb);
            if (e.Key.Length == 0)
            {
                GUI.Label(thumbRect, "None", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 14 });
            }
            else
            {
                _prefabs.TryGetValue(e.Key, out SkillVfx prefab);
                Texture tex = prefab != null ? (AssetPreview.GetAssetPreview(prefab.gameObject) ?? AssetPreview.GetMiniThumbnail(prefab.gameObject)) : null;
                EditorGUI.DrawRect(thumbRect, new Color(0.12f, 0.12f, 0.12f));
                if (tex != null) GUI.DrawTexture(thumbRect, tex, ScaleMode.ScaleToFit);
                if (AssetPreview.IsLoadingAssetPreviews()) Repaint();
            }
            var labelRect = new Rect(r.x, r.y + Thumb + 6, Cell, 30);
            GUI.Label(labelRect, new GUIContent(e.Key.Length == 0 ? "(không dùng)" : e.Key, e.Key), new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, alignment = TextAnchor.UpperCenter });

            Event ev = Event.current;
            if (ev.type == EventType.MouseDown && r.Contains(ev.mousePosition))
            {
                if (_selected != e.Key)
                {
                    _selected = e.Key;
                    LoadPreview();
                }
                if (ev.clickCount == 2 && _onPick != null) Choose();
                ev.Use();
                Repaint();
            }
        }

        private void DrawPreviewPane()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(PreviewWidth));
            Rect area = GUILayoutUtility.GetRect(PreviewWidth, 280, GUILayout.ExpandWidth(false));
            if (_preview?.Effect != null) _preview.Draw(area, _time, _scale, _background);
            else
            {
                EditorGUI.DrawRect(area, _background);
                GUI.Label(area, _selected.Length == 0 ? "Chọn một effect để xem" : "Không tìm thấy prefab", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_playing ? "❚❚ Pause" : "▶ Play", GUILayout.Width(80))) _playing = !_playing;
            if (GUILayout.Button("⟲ Restart", GUILayout.Width(80)))
            {
                _time = 0f;
                _playing = true;
            }
            _loop = GUILayout.Toggle(_loop, "Loop");
            EditorGUILayout.EndHorizontal();

            double life = _preview?.Lifetime ?? 0;
            float span = (float)(double.IsInfinity(life) ? 3.0 : life + 0.3);
            _time = EditorGUILayout.Slider(new GUIContent("Time (s)", "Kéo để xem effect ở từng thời điểm"), _time, 0f, Mathf.Max(0.1f, span));
            _scale = EditorGUILayout.Slider(new GUIContent("Scale", "Thu phóng như trường Scale của node Effect"), _scale, 0.25f, 4f);
            _background = EditorGUILayout.ColorField("Background", _background);

            if (_prefabs.TryGetValue(_selected ?? "", out SkillVfx fx) && fx != null && _index.TryGet(_selected, out VfxCatalogEntry entry))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Key", entry.Key, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Category", entry.Category.ToString());
                EditorGUILayout.LabelField("Lifetime", double.IsInfinity(life) ? "loop (until stopped)" : life.ToString("0.###") + " s");
                EditorGUILayout.LabelField("Path", entry.AssetPath, EditorStyles.miniLabel);
                if (!string.IsNullOrEmpty(fx.Note)) EditorGUILayout.HelpBox(fx.Note, MessageType.None);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Ping")) EditorGUIUtility.PingObject(fx.gameObject);
                if (GUILayout.Button("Open Prefab")) AssetDatabase.OpenAsset(fx.gameObject);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.FlexibleSpace();
            if (_onPick != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(90))) Close();
                using (new EditorGUI.DisabledScope(_selected.Length > 0 && !_prefabs.ContainsKey(_selected)))
                    if (GUILayout.Button(_selected.Length == 0 ? "Clear" : "Select", GUILayout.Width(110))) Choose();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void Choose()
        {
            _onPick?.Invoke(_selected ?? "");
            Close();
        }

        private static string[] BuildCategoryNames()
        {
            var names = new List<string> { "All" };
            foreach (VfxCategory c in Enum.GetValues(typeof(VfxCategory))) names.Add(c.ToString());
            return names.ToArray();
        }
    }
}
