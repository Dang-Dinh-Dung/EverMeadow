using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Evermeadow.Presentation.SpriteAnimation.Editor
{
    /// <summary>
    /// Evermeadow ▸ Animation ▸ Sprite Clip Editor: puts sprites into an AnimationClip without a scene object. Pick the character
    /// (an Animator Controller under Assets/Game/Rigs), its look (base or a weapon / season override) and the clip, so only that
    /// character's clips are listed; then drop sprites (or whole sliced PNGs) from any folder. Frames are ordered by name with
    /// numbers compared as numbers, and can be reordered or removed before Apply. Picking a clip loads the sprites it already has.
    /// </summary>
    public sealed class SpriteClipWindow : EditorWindow
    {
        private AnimationClip _clip;
        private AnimatorController _character;
        private AnimatorOverrideController _look;
        private AnimatorController[] _characters = new AnimatorController[0];
        private AnimatorOverrideController[] _looks = new AnimatorOverrideController[0];
        private (string state, AnimationClip clip)[] _clips = new (string, AnimationClip)[0];
        private readonly List<Sprite> _sprites = new List<Sprite>();
        private float _fps = 10f;
        private bool _loop = true;
        private Vector2 _scroll;

        [MenuItem("Evermeadow/Animation/Sprite Clip Editor", priority = 300)]
        private static void Open()
        {
            var w = GetWindow<SpriteClipWindow>("Sprite Clip Editor");
            w.Refresh();
            if (Selection.activeObject is AnimationClip clip && AnimationAssetMenu.FindOwner(clip, out AnimatorController c, out AnimatorOverrideController l))
            {
                w.Pick(c, l);
                w.Load(clip);
            }
            w.AddSprites(Selection.objects);
        }

        private void OnEnable() => Refresh();

        private void OnProjectChange()
        {
            Refresh();
            Repaint();
        }

        // Re-reads the character list and keeps the current picks when they still exist.
        private void Refresh()
        {
            _characters = AnimationAssetMenu.Characters();
            if (_character != null && !_characters.Contains(_character)) _character = null;
            if (_character == null && _characters.Length > 0) _character = _characters[0];
            Pick(_character, _look);
        }

        private void Pick(AnimatorController character, AnimatorOverrideController look)
        {
            _character = character;
            _looks = character != null ? AnimationAssetMenu.LooksOf(character) : new AnimatorOverrideController[0];
            _look = look != null && _looks.Contains(look) ? look : null;
            _clips = character != null ? AnimationAssetMenu.ClipsOf(character, _look) : new (string, AnimationClip)[0];
            if (_clip != null && !_clips.Any(c => c.clip == _clip)) Load(null);
        }

        private static string Label(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (path.StartsWith(AnimationAssetMenu.Root + "/")) path = path.Substring(AnimationAssetMenu.Root.Length + 1);
            return System.IO.Path.ChangeExtension(path, null);
        }

        private void Load(AnimationClip clip)
        {
            _clip = clip;
            _sprites.Clear();
            if (clip == null) return;
            _sprites.AddRange(AnimationAssetMenu.GetSprites(clip));
            _fps = clip.frameRate;
            _loop = AnimationUtility.GetAnimationClipSettings(clip).loopTime;
        }

        private void AddSprites(IEnumerable<Object> objects)
        {
            Sprite[] added = AnimationAssetMenu.SpritesOf(objects);
            _sprites.AddRange(added.Where(s => !_sprites.Contains(s)));
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "1. Chọn Nhân vật, Look (gốc hoặc vũ khí / mùa) rồi Clip.\n" +
                "2. Kéo các frame đã cắt, hoặc cả file PNG, vào vùng bên dưới (từ thư mục nào cũng được).\n" +
                "3. Kiểm tra thứ tự, chỉnh FPS, rồi bấm Apply.", MessageType.Info);

            if (_characters.Length == 0)
            {
                EditorGUILayout.HelpBox($"Chưa có Animator Controller nào trong {AnimationAssetMenu.Root}. Tạo bằng Evermeadow ▸ Animation ▸ " +
                                        "Create Player / NPC / Creature Animator.", MessageType.Warning);
                return;
            }

            int ci = EditorGUILayout.Popup("Nhân vật", System.Array.IndexOf(_characters, _character), _characters.Select(Label).ToArray());
            if (ci >= 0 && _characters[ci] != _character) Pick(_characters[ci], null);

            string[] lookNames = new[] { "Gốc (Base)" }
                .Concat(_looks.Select(l => System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(l)))).ToArray();
            int li = EditorGUILayout.Popup("Look", _look == null ? 0 : System.Array.IndexOf(_looks, _look) + 1, lookNames);
            AnimatorOverrideController look = li <= 0 ? null : _looks[li - 1];
            if (look != _look) Pick(_character, look);

            int clipIndex = System.Array.FindIndex(_clips, c => c.clip == _clip);
            string[] clipNames = _clips.Select(c => $"{c.state}  ({AnimationAssetMenu.GetSprites(c.clip).Length} frame)").ToArray();
            int pi = EditorGUILayout.Popup("Clip", clipIndex, clipNames);
            if (pi >= 0 && pi != clipIndex) Load(_clips[pi].clip);
            if (_clip == null) return;

            _fps = EditorGUILayout.FloatField(new GUIContent("Frames / second", "60 / số tick của frame: idle 10, run 12, đòn đánh 20 (art bible §8.7)."), _fps);
            _loop = EditorGUILayout.Toggle(new GUIContent("Loop", "Lặp lại; tắt thì giữ frame cuối."), _loop);

            Rect drop = GUILayoutUtility.GetRect(0f, 48f, GUILayout.ExpandWidth(true));
            GUI.Box(drop, $"Thả sprite hoặc PNG vào đây  ({_sprites.Count} frame)", EditorStyles.helpBox);
            HandleDrop(drop);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sắp theo tên")) _sprites.Sort((a, b) => string.CompareOrdinal(AnimationAssetMenu.NaturalKey(a.name), AnimationAssetMenu.NaturalKey(b.name)));
                if (GUILayout.Button("Xóa hết")) _sprites.Clear();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _sprites.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(24f));
                    _sprites[i] = (Sprite)EditorGUILayout.ObjectField(_sprites[i], typeof(Sprite), false);
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("▲", GUILayout.Width(24f))) (_sprites[i - 1], _sprites[i]) = (_sprites[i], _sprites[i - 1]);
                    GUI.enabled = i < _sprites.Count - 1;
                    if (GUILayout.Button("▼", GUILayout.Width(24f))) (_sprites[i + 1], _sprites[i]) = (_sprites[i], _sprites[i + 1]);
                    GUI.enabled = true;
                    if (GUILayout.Button("✕", GUILayout.Width(24f)))
                    {
                        _sprites.RemoveAt(i);
                        break;
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            _sprites.RemoveAll(s => s == null);
            GUI.enabled = _sprites.Count > 0 && _fps > 0f;
            if (GUILayout.Button($"Apply vào {_clip.name}", GUILayout.Height(28f)))
            {
                AnimationAssetMenu.SetSprites(_clip, _sprites.ToArray(), _fps, _loop);
                ShowNotification(new GUIContent($"{_clip.name}: {_sprites.Count} frame"));
            }
            GUI.enabled = true;
        }

        private void HandleDrop(Rect area)
        {
            Event e = Event.current;
            if (!area.Contains(e.mousePosition) || (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)) return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AddSprites(DragAndDrop.objectReferences);
            }
            e.Use();
        }
    }
}
