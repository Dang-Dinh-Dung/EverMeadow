using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Evermeadow.Core.Moveset;
using Evermeadow.Core.Skills;
using UnityEditor;

namespace Evermeadow.Presentation.SkillTool.Editor
{
    /// <summary>One moveset file found in the movesets folder.</summary>
    public readonly struct MovesetFileInfo
    {
        /// <summary>Moveset id (file name).</summary>
        public readonly string Id;
        /// <summary>Project-relative path.</summary>
        public readonly string Path;
        /// <summary>Menu label: id, name, rows, weapons.</summary>
        public readonly string Label;

        /// <summary>Creates an entry.</summary>
        public MovesetFileInfo(string id, string path, string label)
        {
            Id = id;
            Path = path;
            Label = label;
        }
    }

    /// <summary>
    /// File side of the Moveset Tool: moveset XML in <c>Assets/StreamingAssets/Movesets/</c>, id = file name (six digits
    /// from 100000, its own namespace). Saves only valid movesets whose skills exist, and only when the bytes change.
    /// Editor only.
    /// </summary>
    /// <example>
    /// <code>
    /// string id = MovesetToolFiles.NextId();
    /// List&lt;SkillError&gt; errors = MovesetToolFiles.Save(MovesetData.CreateDefault(id));
    /// </code>
    /// </example>
    public static class MovesetToolFiles
    {
        /// <summary>Project-relative folder the game reads.</summary>
        public const string Folder = "Assets/StreamingAssets/Movesets";

        /// <summary>Project-relative path of the weapon family list (one family per line).</summary>
        public const string WeaponFamiliesPath = "Assets/StreamingAssets/Weapons/weapon_families.txt";

        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        /// <summary>The weapon families to offer: the list file, or <see cref="WeaponFamilies.Defaults"/> when it does not exist.</summary>
        public static List<string> LoadWeaponFamilies(List<SkillError> errors = null)
        {
            string full = SkillToolFiles.FullPath(WeaponFamiliesPath);
            if (!File.Exists(full)) return WeaponFamilies.Defaults.ToList();
            return WeaponFamilies.Parse(File.ReadAllText(full, Utf8), errors);
        }

        /// <summary>Set form of <see cref="LoadWeaponFamilies"/>.</summary>
        public static ISet<string> KnownWeapons() => new HashSet<string>(LoadWeaponFamilies(), StringComparer.Ordinal);

        /// <summary>Creates the list file with the defaults when missing (so it can be edited); returns its project path.</summary>
        public static string EnsureWeaponFamiliesFile()
        {
            string full = SkillToolFiles.FullPath(WeaponFamiliesPath);
            if (File.Exists(full)) return WeaponFamiliesPath;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full));
            File.WriteAllBytes(full, Utf8.GetBytes(WeaponFamilies.DefaultFileText));
            AssetDatabase.Refresh();
            return WeaponFamiliesPath;
        }

        /// <summary>Every moveset file, by id.</summary>
        public static List<MovesetFileInfo> ListMovesets()
        {
            var list = new List<MovesetFileInfo>();
            foreach (string rel in AllXml())
            {
                string id = MovesetXml.IdFromFileName(rel);
                if (!MovesetXml.IsMovesetId(id)) continue;
                var errors = new List<SkillError>();
                string text = File.ReadAllText(SkillToolFiles.FullPath(rel), Utf8);
                string label;
                if (MovesetXml.TryRead(text, id, out MovesetData m, errors))
                {
                    MovesetValidator.Validate(m, errors);
                    string name = string.IsNullOrEmpty(m.Name) ? "" : " " + m.Name;
                    label = $"{id}{name}  ({m.Rows.Count} rows, {(m.AnyWeapon ? "any weapon" : string.Join(",", m.Weapons))}{(errors.Count > 0 ? ", has errors" : "")})";
                }
                else label = $"{id}  (unreadable)";
                list.Add(new MovesetFileInfo(id, rel, label));
            }
            return list.OrderBy(i => i.Id, StringComparer.Ordinal).ToList();
        }

        /// <summary>Next free id: one above the largest in use, or 100000; null when none is left.</summary>
        public static string NextId()
        {
            int max = SkillXmlReader.MinSkillId - 1;
            foreach (string rel in AllXml())
            {
                string id = MovesetXml.IdFromFileName(rel);
                if (MovesetXml.IsMovesetId(id)) max = Math.Max(max, int.Parse(id));
            }
            return max >= SkillXmlReader.MaxSkillId ? null : (max + 1).ToString();
        }

        /// <summary>Path of an existing moveset file with this id, or the default path for a new one.</summary>
        public static string PathOf(string id) =>
            AllXml().FirstOrDefault(rel => MovesetXml.IdFromFileName(rel) == id) ?? Folder + "/" + id + MovesetXml.Extension;

        /// <summary>Opens a moveset leniently (a file with mistakes still opens; problems go to <paramref name="errors"/>). Null when unreadable.</summary>
        public static MovesetData Load(string id, List<SkillError> errors)
        {
            string full = SkillToolFiles.FullPath(PathOf(id));
            if (!File.Exists(full))
            {
                errors.Add(new SkillError("moveset:" + id, "MissingFile", PathOf(id) + " does not exist"));
                return null;
            }
            if (!MovesetXml.TryRead(File.ReadAllText(full, Utf8), id, out MovesetData m, errors)) return null;
            MovesetValidator.Validate(m, errors, KnownSkills(), KnownWeapons());
            return m;
        }

        /// <summary>Writes a moveset; returns the problems (nothing is written when there are any).</summary>
        public static List<SkillError> Save(MovesetData m)
        {
            var errors = new List<SkillError>();
            MovesetValidator.Validate(m, errors, KnownSkills(), KnownWeapons());
            if (errors.Count > 0) return errors;
            string xml = MovesetXml.Write(m);
            string full = SkillToolFiles.FullPath(PathOf(m.Id));
            byte[] bytes = Utf8.GetBytes(xml);
            if (File.Exists(full) && File.ReadAllBytes(full).SequenceEqual(bytes)) return errors;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full));
            File.WriteAllBytes(full, bytes);
            AssetDatabase.Refresh();
            return errors;
        }

        /// <summary>Every valid moveset in the folder plus <paramref name="extra"/> (the one being edited, replacing its file).</summary>
        public static MovesetLibrary LoadLibrary(MovesetData extra, List<SkillError> errors)
        {
            var lib = new MovesetLibrary();
            foreach (string rel in AllXml())
            {
                string id = MovesetXml.IdFromFileName(rel);
                if (extra != null && id == extra.Id) continue;
                if (MovesetXml.TryParse(File.ReadAllText(SkillToolFiles.FullPath(rel), Utf8), id, out MovesetDef def, errors)) lib.Replace(def);
            }
            if (extra != null && MovesetXml.TryParse(MovesetXml.Write(extra), extra.Id, out MovesetDef edited, errors)) lib.Replace(edited);
            return lib;
        }

        /// <summary>Checks every moveset file like the game, plus "every skill exists". Empty list = clean.</summary>
        public static List<SkillError> ValidateFolder(out int count)
        {
            var errors = new List<SkillError>();
            ISet<string> skills = KnownSkills();
            count = 0;
            foreach (string rel in AllXml())
            {
                string id = MovesetXml.IdFromFileName(rel);
                var local = new List<SkillError>();
                if (MovesetXml.TryParse(File.ReadAllText(SkillToolFiles.FullPath(rel), Utf8), id, out _, local, skills)) count++;
                foreach (SkillError e in local) errors.Add(new SkillError(e.Subject, e.Code, rel + ": " + e.Message));
            }
            return errors;
        }

        /// <summary>Ids of every skill file.</summary>
        public static ISet<string> KnownSkills() => new HashSet<string>(SkillToolFiles.ListSkills().Select(s => s.Id), StringComparer.Ordinal);

        private static List<string> AllXml()
        {
            string root = SkillToolFiles.FullPath(Folder);
            if (!Directory.Exists(root)) return new List<string>();
            return Directory.GetFiles(root, "*" + MovesetXml.Extension, SearchOption.AllDirectories)
                .Select(f => Folder + "/" + f.Substring(root.Length + 1).Replace('\\', '/'))
                .OrderBy(p => p, StringComparer.Ordinal).ToList();
        }
    }
}
