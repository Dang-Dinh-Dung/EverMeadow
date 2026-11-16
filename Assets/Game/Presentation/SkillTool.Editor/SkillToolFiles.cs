using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Evermeadow.Core.Skills;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Evermeadow.Presentation.SkillTool.Editor
{
    /// <summary>One skill file found in the skills folder.</summary>
    public readonly struct SkillFileInfo
    {
        /// <summary>Skill id (file name).</summary>
        public readonly string Id;
        /// <summary>Project-relative path.</summary>
        public readonly string Path;
        /// <summary>Menu label: id, owner, split.</summary>
        public readonly string Label;

        /// <summary>Creates an entry.</summary>
        public SkillFileInfo(string id, string path, string label)
        {
            Id = id;
            Path = path;
            Label = label;
        }
    }

    /// <summary>
    /// File side of the Skill Tool: the skill XML in <c>Assets/StreamingAssets/Skills/</c> is the only source (user decision
    /// 2026-11-06); a skill's id is its file name, six digits from 100000. Saves only files the game would accept and only
    /// when the bytes change. Editor only.
    /// </summary>
    /// <example>
    /// <code>
    /// string id = SkillToolFiles.NextId();                      // "100000" for an empty folder
    /// List&lt;SkillError&gt; errors = SkillToolFiles.Save(SkillGraphData.CreateDefault(id));
    /// </code>
    /// </example>
    public static class SkillToolFiles
    {
        /// <summary>Project-relative folder the game reads (ADR-0002 Decision 4).</summary>
        public const string Folder = "Assets/StreamingAssets/Skills";

        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        /// <summary>Absolute path of a project-relative path.</summary>
        public static string FullPath(string projectRelative) =>
            System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", projectRelative));

        /// <summary>Every skill file (not tables) in the folder, by id.</summary>
        public static List<SkillFileInfo> ListSkills()
        {
            var list = new List<SkillFileInfo>();
            foreach (string rel in AllXml())
            {
                string id = SkillXmlReader.IdFromFileName(rel);
                if (!SkillXmlReader.IsSkillId(id)) continue;
                string text = File.ReadAllText(FullPath(rel), Utf8);
                if (SkillXmlReader.RootName(text) != "skill") continue;
                var errors = new List<SkillError>();
                bool read = SkillXmlReader.TryParseData(text, id, out SkillGraphData g, errors);
                bool valid = read && SkillGraphValidator.Validate(g, errors) && errors.Count == 0;
                string label = read
                    ? $"{id}  ({g.Owner.ToString().ToLowerInvariant()}, {g.Nodes.Count} nodes{(valid ? "" : ", has errors")})"
                    : $"{id}  (unreadable)";
                list.Add(new SkillFileInfo(id, rel, label));
            }
            return list.OrderBy(i => i.Id, StringComparer.Ordinal).ToList();
        }

        /// <summary>The shared animation slot list (<see cref="Evermeadow.Presentation.SpriteAnimation.AnimationSlots.FilePath"/>, or its defaults).</summary>
        public static List<string> AnimationSlots()
        {
            string full = FullPath(Evermeadow.Presentation.SpriteAnimation.AnimationSlots.FilePath);
            return File.Exists(full)
                ? Evermeadow.Presentation.SpriteAnimation.AnimationSlots.Parse(File.ReadAllText(full, Utf8), null)
                : Evermeadow.Presentation.SpriteAnimation.AnimationSlots.Defaults.ToList();
        }

        /// <summary>Opens the slot list for editing (creates it with the defaults first when missing).</summary>
        public static void EditAnimationSlots()
        {
            string rel = Evermeadow.Presentation.SpriteAnimation.AnimationSlots.FilePath;
            string full = FullPath(rel);
            if (!File.Exists(full))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full));
                File.WriteAllBytes(full, Utf8.GetBytes(Evermeadow.Presentation.SpriteAnimation.AnimationSlots.DefaultFileText));
                AssetDatabase.Refresh();
            }
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rel);
            if (asset != null) AssetDatabase.OpenAsset(asset);
            else EditorUtility.RevealInFinder(full);
        }

        /// <summary>Next free id: one above the largest id in use, or 100000.</summary>
        public static string NextId()
        {
            int max = SkillXmlReader.MinSkillId - 1;
            foreach (string rel in AllXml())
            {
                string id = SkillXmlReader.IdFromFileName(rel);
                if (SkillXmlReader.IsSkillId(id)) max = Math.Max(max, int.Parse(id));
            }
            return max >= SkillXmlReader.MaxSkillId ? null : (max + 1).ToString();
        }

        /// <summary>Path of an existing skill file with this id, or the default path for a new one.</summary>
        public static string PathOf(string id) =>
            AllXml().FirstOrDefault(rel => SkillXmlReader.IdFromFileName(rel) == id) ?? Folder + "/" + id + FolderSkillTextSource.Extension;

        /// <summary>Opens a skill file as a graph (even with errors, which are added to <paramref name="errors"/>); null when missing or unreadable.</summary>
        public static SkillGraphData Load(string id, List<SkillError> errors)
        {
            string full = FullPath(PathOf(id));
            if (!File.Exists(full))
            {
                errors.Add(new SkillError(id, "Missing", "no file " + PathOf(id)));
                return null;
            }
            // Lenient: a file with errors still opens so it can be fixed; the problems are reported.
            if (!SkillXmlReader.TryParseData(File.ReadAllText(full, Utf8), id, out SkillGraphData g, errors)) return null;
            SkillGraphValidator.Validate(g, errors);
            return g;
        }

        /// <summary>Writes a skill; returns the reader's errors (nothing is written when there are any).</summary>
        public static List<SkillError> Save(SkillGraphData graph)
        {
            var errors = new List<SkillError>();
            string xml = SkillXmlWriter.Write(graph);
            if (!SkillXmlReader.TryParse(xml, graph.Id, out _, errors)) return errors;
            Write(PathOf(graph.Id), xml);
            return errors;
        }

        /// <summary>Loads the whole folder like the game does, plus the library checks. Empty list = clean.</summary>
        public static List<SkillError> ValidateFolder(out int skillCount)
        {
            var errors = new List<SkillError>();
            SkillLibrary library = SkillLoader.Load(new FolderSkillTextSource(FullPath(Folder)), errors, verifyPayloadHash: true);
            SkillLibraryValidator.Validate(library, errors);
            skillCount = library.SkillCount;
            return errors;
        }

        private static void Write(string projectRelative, string xml)
        {
            string full = FullPath(projectRelative);
            byte[] bytes = Utf8.GetBytes(xml);
            if (File.Exists(full) && File.ReadAllBytes(full).SequenceEqual(bytes)) return;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full));
            File.WriteAllBytes(full, bytes);
            AssetDatabase.Refresh();
        }

        private static List<string> AllXml()
        {
            string root = FullPath(Folder);
            if (!Directory.Exists(root)) return new List<string>();
            return Directory.GetFiles(root, "*" + FolderSkillTextSource.Extension, SearchOption.AllDirectories)
                .Select(f => Folder + "/" + f.Substring(root.Length + 1).Replace('\\', '/'))
                .OrderBy(p => p, StringComparer.Ordinal).ToList();
        }
    }

    /// <summary>Build gate: the build fails when the skills folder would not load cleanly. Never writes.</summary>
    public sealed class SkillBuildCheck : IPreprocessBuildWithReport
    {
        /// <inheritdoc />
        public int callbackOrder => 0;

        /// <inheritdoc />
        public void OnPreprocessBuild(BuildReport report)
        {
            // GDD AC-D07: a body edited outside the Skill Tool (PayloadHashMismatch) fails the build like any load error.
            List<SkillError> errors = SkillToolFiles.ValidateFolder(out _);
            if (errors.Count > 0)
                throw new BuildFailedException("[Skills] " + errors.Count + " problem(s):\n" + string.Join("\n", errors));
        }
    }
}
