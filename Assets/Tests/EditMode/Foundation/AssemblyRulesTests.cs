using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Evermeadow.Tests.EditMode.Foundation
{
    /// <summary>
    /// Static enforcement of GDD Input Rule 1 and ADR-0001 asmdef rules (AC-R01, AC-R40 core part). Reads project source files:
    /// a static scan by design, not a behavioural unit test.
    /// </summary>
    public class AssemblyRulesTests
    {
        private static readonly string GameRoot = Path.Combine(Application.dataPath, "Game");
        private static readonly string[] Layers = { "Foundation", "Core", "Feature", "Presentation" };

        private sealed class Asmdef
        {
            public string Path;
            public string Name;
            public List<string> References = new List<string>();
            public bool NoEngineReferences;
        }

        [System.Serializable]
        private sealed class AsmdefJson
        {
            public string name;
            public string[] references;
            public bool noEngineReferences;
        }

        private static List<Asmdef> GameAsmdefs() =>
            Directory.GetFiles(GameRoot, "*.asmdef", SearchOption.AllDirectories).Select(p =>
            {
                var json = JsonUtility.FromJson<AsmdefJson>(File.ReadAllText(p));
                return new Asmdef { Path = p, Name = json.name, References = (json.references ?? new string[0]).ToList(), NoEngineReferences = json.noEngineReferences };
            }).ToList();

        private static bool IsInputAssembly(string name) => name.StartsWith("Evermeadow.Foundation.Input");

        [Test, Description("AC-R01")]
        public void GameAssemblies_OnlyInputAdapterReferencesInputSystem()
        {
            var offenders = GameAsmdefs()
                .Where(a => a.References.Contains("Unity.InputSystem") && a.Name != "Evermeadow.Foundation.Input.Unity")
                .Select(a => a.Name).ToList();

            CollectionAssert.IsEmpty(offenders);
        }

        [Test, Description("AC-R01")]
        public void GameSource_NoDirectDeviceReadsOutsideInputAdapter()
        {
            var forbidden = new Regex(@"\b(Keyboard|Gamepad|Mouse)\.current\b|\bUnityEngine\.Input\.|\bInput\.Get(Key|Axis|Button|Mouse)");
            string adapterDir = Path.Combine(GameRoot, "Foundation", "Input.Unity");

            var offenders = Directory.GetFiles(GameRoot, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.StartsWith(adapterDir))
                .Where(f => forbidden.IsMatch(File.ReadAllText(f)))
                .Select(f => f.Substring(GameRoot.Length)).ToList();

            CollectionAssert.IsEmpty(offenders);
        }

        [Test, Description("ADR-0001 Decision 1; AC-R40: cores reference no engine")]
        public void CoreAssemblies_HaveNoEngineReferences()
        {
            var cores = GameAsmdefs().Where(a => a.Name.StartsWith("Evermeadow.Foundation.") && !a.Name.EndsWith(".Unity")).ToList();

            Assert.IsNotEmpty(cores);
            foreach (Asmdef a in cores) Assert.IsTrue(a.NoEngineReferences, a.Name);
        }

        [Test, Description("ADR-0001 Implementation Guidelines: same or lower layer only; nothing references Bootstrap")]
        public void GameAssemblies_ReferenceOnlySameOrLowerLayerAndNeverBootstrap()
        {
            List<Asmdef> all = GameAsmdefs();
            foreach (Asmdef a in all)
            {
                CollectionAssert.DoesNotContain(a.References, "Evermeadow.Bootstrap", a.Name);
                if (a.Name == "Evermeadow.Bootstrap") continue;
                int own = LayerOf(a.Name);
                Assert.GreaterOrEqual(own, 0, $"{a.Name} does not follow Evermeadow.<Layer>.<System>[.Unity]");
                foreach (string r in a.References.Where(r => r.StartsWith("Evermeadow.")))
                    Assert.LessOrEqual(LayerOf(r), own, $"{a.Name} → {r}");
            }
        }

        [Test, Description("AC-R01: only Input assemblies may name the package")]
        public void InputAdapter_IsTheOnlyInputAssemblyWithEngineAccess()
        {
            var engineInput = GameAsmdefs().Where(a => IsInputAssembly(a.Name) && !a.NoEngineReferences).Select(a => a.Name).ToList();

            CollectionAssert.AreEqual(new[] { "Evermeadow.Foundation.Input.Unity" }, engineInput);
        }

        private static int LayerOf(string asmdefName)
        {
            string[] parts = asmdefName.Split('.');
            return parts.Length >= 3 ? System.Array.IndexOf(Layers, parts[1]) : -1;
        }
    }
}
