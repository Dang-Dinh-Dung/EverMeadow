using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Evermeadow.Core.Skills
{
    /// <summary>
    /// Reads every <c>*.xml</c> file under one folder (sub-folders included) as skill / command-table text (ADR-0002 Decision 4,
    /// user decision 2026-11-06: real .xml files in their own folder, no Addressables). The game passes
    /// <c>Application.streamingAssetsPath + "/Skills"</c> so the files ship as-is in a PC build. Files are returned in ordinal
    /// order of their relative path so loading is deterministic on every machine.
    /// </summary>
    /// <example>
    /// <code>
    /// var source = new FolderSkillTextSource(Path.Combine(Application.streamingAssetsPath, "Skills"));
    /// SkillLibrary library = SkillLoader.Load(source, errors);
    /// </code>
    /// </example>
    public sealed class FolderSkillTextSource : ISkillTextSource
    {
        /// <summary>Extension of skill and command-table files.</summary>
        public const string Extension = ".xml";

        private readonly string _root;
        private readonly Func<string, bool> _exists;
        private readonly Func<string, string[]> _listFiles;
        private readonly Func<string, string> _readText;

        /// <summary>Reads from <paramref name="rootDirectory"/> on disk.</summary>
        public FolderSkillTextSource(string rootDirectory)
            : this(rootDirectory, Directory.Exists,
                dir => Directory.GetFiles(dir, "*" + Extension, SearchOption.AllDirectories),
                path => File.ReadAllText(path, Encoding.UTF8))
        {
        }

        /// <summary>Test seam: file-system calls are injected so unit tests do no I/O.</summary>
        public FolderSkillTextSource(string rootDirectory, Func<string, bool> directoryExists, Func<string, string[]> listFiles,
            Func<string, string> readText)
        {
            _root = rootDirectory ?? "";
            _exists = directoryExists ?? throw new ArgumentNullException(nameof(directoryExists));
            _listFiles = listFiles ?? throw new ArgumentNullException(nameof(listFiles));
            _readText = readText ?? throw new ArgumentNullException(nameof(readText));
        }

        /// <summary>The folder read by <see cref="ReadAll"/>.</summary>
        public string RootDirectory => _root;

        /// <summary>True when the folder exists. A missing folder reads as empty, so the game runs on fallback skills.</summary>
        public bool RootExists => _root.Length > 0 && _exists(_root);

        /// <inheritdoc />
        public void ReadAll(List<SkillSourceText> into)
        {
            if (into == null) throw new ArgumentNullException(nameof(into));
            if (!RootExists) return;

            string[] files = _listFiles(_root) ?? Array.Empty<string>();
            var names = new string[files.Length];
            for (int i = 0; i < files.Length; i++) names[i] = Relative(files[i]);
            Array.Sort(names, files, StringComparer.Ordinal);

            for (int i = 0; i < files.Length; i++)
            {
                if (!files[i].EndsWith(Extension, StringComparison.OrdinalIgnoreCase)) continue;
                into.Add(new SkillSourceText(names[i], _readText(files[i])));
            }
        }

        private string Relative(string path)
        {
            string p = path.Replace('\\', '/');
            string root = _root.Replace('\\', '/').TrimEnd('/') + "/";
            return p.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? p.Substring(root.Length) : p;
        }
    }
}
