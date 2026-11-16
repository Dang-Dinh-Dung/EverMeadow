using Evermeadow.Presentation.Settings.Unity;
using Evermeadow.Tests.EditMode.Foundation;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Presentation
{
    /// <summary>Settings load and save on in-memory storage: round trip, missing and damaged files, write failures.</summary>
    public class SettingsServiceTests
    {
        private sealed class MemoryStorage : ISettingsStorage
        {
            public string Text;
            public bool FailWrites;

            public bool TryRead(out string text)
            {
                text = Text;
                return text != null;
            }

            public void Write(string text)
            {
                if (FailWrites) throw new System.IO.IOException("disk full");
                Text = text;
            }
        }

        [Test]
        public void SaveThenLoad_KeepsBindingOverrides()
        {
            var storage = new MemoryStorage();
            var a = new SettingsService(storage, new RecordingLogSink()) { BindingOverrides = "{\"bindings\":[]}" };

            a.Save();
            var b = new SettingsService(storage, new RecordingLogSink());
            bool loaded = b.Load();

            Assert.IsTrue(loaded);
            Assert.AreEqual("{\"bindings\":[]}", b.BindingOverrides);
        }

        [Test, Description("First launch: no file → defaults, no error")]
        public void Load_NothingStored_DefaultsSilently()
        {
            var log = new RecordingLogSink();
            var s = new SettingsService(new MemoryStorage(), log);

            bool loaded = s.Load();

            Assert.IsFalse(loaded);
            Assert.AreEqual(string.Empty, s.BindingOverrides);
            Assert.AreEqual(0, log.Total);
        }

        [Test, Description("A damaged file never throws: defaults and one error")]
        public void Load_Damaged_DefaultsAndOneError()
        {
            var log = new RecordingLogSink();
            var s = new SettingsService(new MemoryStorage { Text = "{ not json" }, log);

            bool loaded = s.Load();

            Assert.IsFalse(loaded);
            Assert.AreEqual(string.Empty, s.BindingOverrides);
            Assert.AreEqual(1, log.Errors);
        }

        [Test, Description("A failed write is logged, never thrown")]
        public void Save_WriteFails_LoggedNotThrown()
        {
            var log = new RecordingLogSink();
            var s = new SettingsService(new MemoryStorage { FailWrites = true }, log);

            bool saved = s.Save();

            Assert.IsFalse(saved);
            Assert.AreEqual(1, log.Errors);
        }
    }
}
