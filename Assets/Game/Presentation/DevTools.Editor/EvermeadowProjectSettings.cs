using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Evermeadow.Foundation;
using UnityEditor;
using UnityEngine;

namespace Evermeadow.Presentation.DevTools.Editor
{
    /// <summary>
    /// <c>Evermeadow ▸ Project Settings</c> (also Edit ▸ Project Settings ▸ Evermeadow): debug switches for designers (user
    /// request 2026-11-14) — Show Hit Area, Show Search Area, Log Skill Trace, Log AI Decisions, Show Moveset Tester. Saved in
    /// <c>ProjectSettings/EvermeadowDevOptions.json</c> (shared through git), loaded when the Editor starts, pushed into
    /// <see cref="DevOptions"/>; flipping a switch during Play Mode applies at once. Editor only.
    /// </summary>
    [InitializeOnLoad]
    public static class EvermeadowProjectSettings
    {
        /// <summary>Settings file, relative to the project folder.</summary>
        public const string FilePath = "ProjectSettings/EvermeadowDevOptions.json";

        /// <summary>Path of the page in the Project Settings window.</summary>
        public const string SettingsPath = "Project/Evermeadow";

        [Serializable]
        private sealed class Data
        {
            public bool showHitArea;
            public bool showSearchArea;
            public bool logSkillTrace;
            public bool logAiDecisions;
            public bool showMovesetTester = true;
        }

        private static readonly (string label, string help)[] Switches =
        {
            ("Show Hit Area", "Vẽ vùng sát thương (màu xanh) của mỗi node Hit khi skill chạy: đúng vị trí (pivot + offset theo hướng mặt), " +
                              "hình (Arc: góc, bán kính / Box: dài, cao) từ SubValue × AreaScale, và thời lượng (SteadyDuration; đánh tức thì hiện 0,1 s). " +
                              "Hit Steady bật IsFollowPivot thì vùng đi theo nhân vật."),
            ("Show Search Area", "Vẽ vùng tìm kiếm (màu vàng) của mỗi node Search trong 0,3 s."),
            ("Log Skill Trace", "Ghi ra Console từng sự kiện node của skill: bắt đầu, xong, bị dừng, bị bỏ qua và lý do."),
            ("Log AI Decisions", "Ghi ra Console mỗi lần quái đổi nhánh Behaviour Tree (tuần tra → đuổi → đánh) và lần tung skill bị từ chối."),
            ("Show Moveset Tester", "Hiện bảng Moveset Tester (F9) khi Play: khảm moveset, xem input → đòn, log node của skill.")
        };

        static EvermeadowProjectSettings() => Load();

        /// <summary>Opens the page.</summary>
        [MenuItem("Evermeadow/Project Settings", priority = 0)]
        public static void Open() => SettingsService.OpenProjectSettings(SettingsPath);

        /// <summary>Registers the page in the Project Settings window.</summary>
        [SettingsProvider]
        public static SettingsProvider Provider() =>
            new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "Evermeadow",
                guiHandler = _ => Draw(),
                keywords = new HashSet<string> { "Evermeadow", "debug", "hit", "area", "skill", "trace", "AI", "moveset" }
            };

        /// <summary>Reads the file into <see cref="DevOptions"/> (defaults when it does not exist).</summary>
        public static void Load()
        {
            var d = new Data();
            string full = FullPath();
            try
            {
                if (File.Exists(full)) JsonUtility.FromJsonOverwrite(File.ReadAllText(full, Encoding.UTF8), d);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Evermeadow] {FilePath} unreadable, using defaults: {e.Message}");
            }
            DevOptions.ShowHitArea = d.showHitArea;
            DevOptions.ShowSearchArea = d.showSearchArea;
            DevOptions.LogSkillTrace = d.logSkillTrace;
            DevOptions.LogAiDecisions = d.logAiDecisions;
            DevOptions.ShowMovesetTester = d.showMovesetTester;
            DevOptions.NotifyChanged();
        }

        /// <summary>Writes <see cref="DevOptions"/> to the file.</summary>
        public static void Save()
        {
            var d = new Data
            {
                showHitArea = DevOptions.ShowHitArea,
                showSearchArea = DevOptions.ShowSearchArea,
                logSkillTrace = DevOptions.LogSkillTrace,
                logAiDecisions = DevOptions.LogAiDecisions,
                showMovesetTester = DevOptions.ShowMovesetTester
            };
            File.WriteAllText(FullPath(), JsonUtility.ToJson(d, true) + "\n", new UTF8Encoding(false));
        }

        private static void Draw()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Công cụ hỗ trợ người làm game. Áp dụng ngay cả khi đang Play; không ảnh hưởng bản build. " +
                                       "Lưu trong " + FilePath + " (dùng chung cả nhóm).", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(6);

            EditorGUI.BeginChangeCheck();
            DevOptions.ShowHitArea = Switch(0, DevOptions.ShowHitArea);
            DevOptions.ShowSearchArea = Switch(1, DevOptions.ShowSearchArea);
            DevOptions.LogSkillTrace = Switch(2, DevOptions.LogSkillTrace);
            DevOptions.LogAiDecisions = Switch(3, DevOptions.LogAiDecisions);
            DevOptions.ShowMovesetTester = Switch(4, DevOptions.ShowMovesetTester);
            if (EditorGUI.EndChangeCheck())
            {
                Save();
                DevOptions.NotifyChanged();
            }
        }

        private static bool Switch(int i, bool value)
        {
            bool v = EditorGUILayout.ToggleLeft(new GUIContent(Switches[i].label, Switches[i].help), value, EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
                EditorGUILayout.LabelField(Switches[i].help, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
            return v;
        }

        private static string FullPath() => Path.GetFullPath(Path.Combine(Application.dataPath, "..", FilePath));
    }
}
