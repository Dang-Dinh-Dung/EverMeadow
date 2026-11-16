using System;
using System.IO;
using Evermeadow.Foundation;
using UnityEngine;

namespace Evermeadow.Presentation.Settings.Unity
{
    /// <summary>Where the settings text lives (a file in the game; memory in tests).</summary>
    public interface ISettingsStorage
    {
        /// <summary>Reads the stored text; false when nothing is stored yet.</summary>
        bool TryRead(out string text);

        /// <summary>Replaces the stored text.</summary>
        void Write(string text);
    }

    /// <summary><c>settings.json</c> in <see cref="Application.persistentDataPath"/>, per machine and outside save slots (TD-B2).</summary>
    public sealed class FileSettingsStorage : ISettingsStorage
    {
        /// <summary>File name inside the persistent data folder.</summary>
        public const string FileName = "settings.json";

        private readonly string _path;

        /// <summary>Creates storage at <paramref name="folder"/>/settings.json (default: the persistent data folder).</summary>
        public FileSettingsStorage(string folder = null) => _path = Path.Combine(folder ?? Application.persistentDataPath, FileName);

        /// <summary>Full path of the file.</summary>
        public string FilePath => _path;

        /// <inheritdoc />
        public bool TryRead(out string text)
        {
            text = File.Exists(_path) ? File.ReadAllText(_path) : null;
            return text != null;
        }

        /// <inheritdoc />
        public void Write(string text)
        {
            string tmp = _path + ".tmp";
            File.WriteAllText(tmp, text); // write then swap, so a crash never leaves a half-written file
            if (File.Exists(_path)) File.Replace(tmp, _path, null);
            else File.Move(tmp, _path);
        }
    }

    /// <summary>
    /// Player settings stored per machine (systems index: Settings &amp; Accessibility; TD-B8: Settings pushes values into the
    /// systems that use them, they never read Settings). Holds the key bindings now; volume, difficulty and accessibility options
    /// join the same file later. Unreadable files fall back to defaults with one error, never an exception.
    /// </summary>
    public sealed class SettingsService
    {
        /// <summary>Current file format version.</summary>
        public const int Version = 1;

        [Serializable]
        private sealed class Data
        {
            public int version = Version;
            public string bindingOverrides = string.Empty;
        }

        private readonly ISettingsStorage _storage;
        private readonly ILogSink _log;
        private Data _data = new Data();

        /// <summary>Creates the service on <paramref name="storage"/>; call <see cref="Load"/> before reading values.</summary>
        public SettingsService(ISettingsStorage storage, ILogSink log)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _log = log ?? new NullLogSink();
        }

        /// <summary>Key binding overrides (Input System JSON, from <c>RebindController.ExportBindingOverrides</c>); empty = defaults.</summary>
        public string BindingOverrides
        {
            get => _data.bindingOverrides ?? string.Empty;
            set => _data.bindingOverrides = value ?? string.Empty;
        }

        /// <summary>Reads the stored settings; missing → defaults, unreadable → defaults and one error. False when defaults were used.</summary>
        public bool Load()
        {
            _data = new Data();
            string text;
            try
            {
                if (!_storage.TryRead(out text) || string.IsNullOrWhiteSpace(text)) return false;
            }
            catch (Exception e)
            {
                _log.Error("Settings.ReadFailed", $"Settings could not be read; using defaults. {e.Message}");
                return false;
            }

            try
            {
                Data loaded = JsonUtility.FromJson<Data>(text);
                if (loaded == null) throw new FormatException("empty document");
                if (loaded.version > Version) _log.Warn("Settings.NewerVersion", $"Settings version {loaded.version} is newer than {Version}; reading what is known.");
                _data = loaded;
                return true;
            }
            catch (Exception e)
            {
                _log.Error("Settings.Unreadable", $"Settings file is damaged; using defaults. {e.Message}");
                return false;
            }
        }

        /// <summary>Writes the current settings. Failures are logged, never thrown (a full disk must not crash the game).</summary>
        public bool Save()
        {
            try
            {
                _data.version = Version;
                _storage.Write(JsonUtility.ToJson(_data, true));
                return true;
            }
            catch (Exception e)
            {
                _log.Error("Settings.WriteFailed", $"Settings could not be saved. {e.Message}");
                return false;
            }
        }
    }
}
