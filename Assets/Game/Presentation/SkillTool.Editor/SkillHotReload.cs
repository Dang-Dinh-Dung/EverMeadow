using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Evermeadow.Core.Moveset;
using Evermeadow.Core.Moveset.Unity;
using Evermeadow.Core.Skills;
using Evermeadow.Core.Skills.Unity;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Evermeadow.Presentation.SkillTool.Editor
{
    /// <summary>
    /// Hot reload of skill files into Play Mode: after the Skill Tool saves a file (or any skill XML under
    /// <see cref="SkillToolFiles.Folder"/> is imported, moved or deleted while playing), the file is pushed into every running
    /// <see cref="SkillLibraryRunner"/>; the next cast of that skill uses the new version, no Play Mode restart. A file with
    /// errors is refused by the game, which keeps the version it had. Editor only.
    /// </summary>
    /// <example>
    /// <code>
    /// string line = SkillHotReload.Push("Assets/StreamingAssets/Skills/100001.xml");   // "" outside Play Mode
    /// </code>
    /// </example>
    public static class SkillHotReload
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        /// <summary>True for a skill / table XML inside the skills folder.</summary>
        public static bool IsSkillFile(string projectRelative) =>
            projectRelative != null && projectRelative.Replace('\\', '/').StartsWith(SkillToolFiles.Folder + "/", StringComparison.OrdinalIgnoreCase) &&
            projectRelative.EndsWith(FolderSkillTextSource.Extension, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Pushes the current state of one file (deleted → removed) into the running game. Returns a one-line summary for the
        /// Skill Tool status bar, or "" outside Play Mode / for files outside the skills folder.
        /// </summary>
        public static string Push(string projectRelative)
        {
            if (!EditorApplication.isPlaying || !IsSkillFile(projectRelative)) return "";
            SkillLibraryRunner[] runners = Object.FindObjectsByType<SkillLibraryRunner>(FindObjectsSortMode.None);
            if (runners.Length == 0) return "Play Mode: no SkillLibraryRunner in the scene, so the game has no skill data to update.";

            string rel = projectRelative.Replace('\\', '/');
            string name = rel.Substring(SkillToolFiles.Folder.Length + 1);
            string full = SkillToolFiles.FullPath(rel);
            string xml = File.Exists(full) ? File.ReadAllText(full, Utf8) : null;

            var errors = new List<SkillError>();
            var summary = new List<string>();
            foreach (SkillLibraryRunner runner in runners)
            {
                SkillRepository repo = runner.Ensure();
                SkillReloadResult r = xml != null ? repo.ReloadText(name, xml, errors) : repo.RemoveFile(name);
                summary.Add(Describe(r, name, repo.Version));
            }
            foreach (SkillError e in errors.Distinct()) Debug.LogWarning("[Skills] Hot reload refused: " + e);
            return string.Join(" / ", summary.Distinct());
        }

        private static string Describe(SkillReloadResult r, string name, int version)
        {
            switch (r)
            {
                case SkillReloadResult.Rejected: return $"Play Mode: {name} has errors, the game keeps its previous version (see Console).";
                case SkillReloadResult.Removed: return $"Play Mode: {name} removed from the game.";
                case SkillReloadResult.Ignored: return "";
                default: return $"Play Mode: live, the next cast uses this version (data v{version}).";
            }
        }
    }

    /// <summary>Hot reload of moveset files into Play Mode (same rules as <see cref="SkillHotReload"/>). Editor only.</summary>
    /// <example>
    /// <code>
    /// string line = MovesetHotReload.Push("Assets/StreamingAssets/Movesets/100000.xml");
    /// </code>
    /// </example>
    public static class MovesetHotReload
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        /// <summary>True for an XML inside the movesets folder.</summary>
        public static bool IsMovesetFile(string projectRelative) =>
            projectRelative != null && projectRelative.Replace('\\', '/').StartsWith(MovesetToolFiles.Folder + "/", StringComparison.OrdinalIgnoreCase) &&
            projectRelative.EndsWith(MovesetXml.Extension, StringComparison.OrdinalIgnoreCase);

        /// <summary>Pushes one moveset file (deleted → removed) into every running <see cref="MovesetLibraryRunner"/>; "" outside Play Mode.</summary>
        public static string Push(string projectRelative)
        {
            if (!EditorApplication.isPlaying || !IsMovesetFile(projectRelative)) return "";
            MovesetLibraryRunner[] runners = Object.FindObjectsByType<MovesetLibraryRunner>(FindObjectsSortMode.None);
            if (runners.Length == 0) return "Play Mode: no MovesetLibraryRunner in the scene, so the game has no moveset data to update.";
            string rel = projectRelative.Replace('\\', '/');
            string name = rel.Substring(MovesetToolFiles.Folder.Length + 1);
            string full = SkillToolFiles.FullPath(rel);
            string xml = File.Exists(full) ? File.ReadAllText(full, Utf8) : null;
            var errors = new List<SkillError>();
            var summary = new List<string>();
            foreach (MovesetLibraryRunner runner in runners)
            {
                MovesetRepository repo = runner.Ensure();
                SkillReloadResult r = xml != null ? repo.ReloadText(name, xml, errors) : repo.RemoveFile(name);
                summary.Add(r == SkillReloadResult.Rejected ? $"Play Mode: {name} has errors, the game keeps its previous version (see Console)."
                    : r == SkillReloadResult.Removed ? $"Play Mode: {name} removed from the game."
                    : r == SkillReloadResult.Ignored ? "" : $"Play Mode: live, the next press uses this moveset (data v{repo.Version}).");
            }
            foreach (SkillError e in errors.Distinct()) Debug.LogWarning("[Moveset] Hot reload refused: " + e);
            return string.Join(" / ", summary.Distinct());
        }
    }

    /// <summary>Pushes skill and moveset files changed on disk during Play Mode (hand edits, git, Save) into the running game.</summary>
    public sealed class SkillFolderPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!EditorApplication.isPlaying) return;
            foreach (string path in imported.Concat(moved).Concat(deleted).Concat(movedFrom))
            {
                if (SkillHotReload.IsSkillFile(path)) SkillHotReload.Push(path);
                else if (MovesetHotReload.IsMovesetFile(path)) MovesetHotReload.Push(path);
            }
        }
    }
}
