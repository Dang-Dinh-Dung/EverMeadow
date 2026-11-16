using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Evermeadow.Presentation.Vfx.Editor
{
    /// <summary>
    /// Keeps <c>Assets/Game/Art/VFX/VfxCatalog.asset</c> equal to the effect prefabs under <see cref="VfxNaming.Root"/>:
    /// every prefab with a <see cref="SkillVfx"/> root, keyed by its file name, category from its folder. Runs after any
    /// prefab there is imported, moved or deleted (deferred out of the import callback; never in batch mode), and from
    /// <c>Evermeadow ▸ VFX ▸ Rebuild Catalog</c>. Editor only.
    /// </summary>
    /// <example>
    /// <code>
    /// VfxIndex index = VfxCatalogBuilder.Scan(out var prefabs);   // fresh scan for the picker
    /// bool ok = VfxCatalogBuilder.Exists("vfx_cmn_hit_spark");    // cached, cleared when prefabs change
    /// VfxCatalogBuilder.Rebuild();                                             // write the catalog asset
    /// </code>
    /// </example>
    public static class VfxCatalogBuilder
    {
        /// <summary>Project path of the catalog asset.</summary>
        public const string CatalogPath = VfxNaming.Root + "/VfxCatalog.asset";

        private static HashSet<string> _known;

        /// <summary>True when an effect prefab with this key exists (cached; the cache is cleared when VFX prefabs change).</summary>
        public static bool Exists(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (_known == null)
            {
                Scan(out Dictionary<string, SkillVfx> prefabs);
                _known = new HashSet<string>(prefabs.Keys);
            }
            return _known.Contains(key);
        }

        /// <summary>Forgets the cached key set (next <see cref="Exists"/> rescans).</summary>
        public static void InvalidateCache() => _known = null;

        /// <summary>Scans the VFX folder; returns the index and the prefab of each key.</summary>
        public static VfxIndex Scan(out Dictionary<string, SkillVfx> prefabs)
        {
            prefabs = new Dictionary<string, SkillVfx>();
            var entries = new List<VfxCatalogEntry>();
            if (!AssetDatabase.IsValidFolder(VfxNaming.Root)) return new VfxIndex(entries);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { VfxNaming.Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                SkillVfx fx = go != null ? go.GetComponent<SkillVfx>() : null;
                if (fx == null) continue;
                string key = VfxNaming.KeyOf(path);
                entries.Add(new VfxCatalogEntry(key, VfxNaming.CategoryOf(path), path));
                if (!prefabs.ContainsKey(key)) prefabs.Add(key, fx);
            }
            return new VfxIndex(entries);
        }

        /// <summary>Rewrites the catalog asset (creating folder and asset if needed). Returns the problems found.</summary>
        [MenuItem("Evermeadow/VFX/Rebuild Catalog", priority = 400)]
        public static List<string> Rebuild()
        {
            VfxIndex index = Scan(out Dictionary<string, SkillVfx> prefabs);
            EnsureFolders();
            var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<VfxCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var rows = index.All.Select(e => new VfxCatalog.Entry { Key = e.Key, Category = e.Category, Prefab = prefabs[e.Key], Path = e.AssetPath }).ToList();
            bool same = catalog.Entries.Count == rows.Count && catalog.Entries.Zip(rows, (a, b) =>
                a.Key == b.Key && a.Category == b.Category && a.Prefab == b.Prefab && a.Path == b.Path).All(x => x);
            if (!same)
            {
                catalog.SetEntries(rows);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);
            }
            foreach (string p in index.Problems) Debug.LogWarning("[VFX] " + p);
            return index.Problems;
        }

        /// <summary>Creates <c>Assets/Game/Art/VFX/&lt;Category&gt;/</c> for every category.</summary>
        public static void EnsureFolders()
        {
            EnsureFolder(VfxNaming.Root);
            foreach (VfxCategory c in System.Enum.GetValues(typeof(VfxCategory))) EnsureFolder(VfxNaming.Root + "/" + c);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    /// <summary>
    /// Rebuilds the catalog when effect prefabs change, then (in Play Mode) drops the running game's pooled copies of the
    /// changed effects so the next spawn uses the edited prefab (hot reload), and tells open browsers to refresh.
    /// </summary>
    public sealed class VfxCatalogPostprocessor : AssetPostprocessor
    {
        private static bool _pending;
        private static readonly HashSet<string> ChangedKeys = new HashSet<string>();

        /// <summary>Raised after the catalog was rebuilt because effect prefabs changed (the keys that changed).</summary>
        public static event System.Action<IReadOnlyCollection<string>> EffectsChanged;

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (Application.isBatchMode) return;
            bool Touches(string p) => p.StartsWith(VfxNaming.Root + "/") && p.EndsWith(".prefab");
            string[] touched = imported.Concat(deleted).Concat(moved).Concat(movedFrom).Where(Touches).ToArray();
            if (touched.Length == 0) return;
            VfxCatalogBuilder.InvalidateCache();
            foreach (string p in touched) ChangedKeys.Add(VfxNaming.KeyOf(p));
            if (_pending) return;
            _pending = true;
            EditorApplication.delayCall += () =>
            {
                _pending = false;
                var keys = ChangedKeys.ToList();
                ChangedKeys.Clear();
                VfxCatalogBuilder.Rebuild();
                if (EditorApplication.isPlaying) PushToGame(keys);
                EffectsChanged?.Invoke(keys);
            };
        }

        private static void PushToGame(List<string> keys)
        {
            int runners = 0;
            foreach (VfxRunner runner in Object.FindObjectsByType<VfxRunner>(FindObjectsSortMode.None))
            {
                if (runner.Spawner == null) continue;
                foreach (string key in keys) runner.Spawner.Invalidate(key);
                runners++;
            }
            if (runners > 0) Debug.Log($"[VFX] Hot reload: {string.Join(", ", keys)} — the next spawn uses the edited prefab.");
        }
    }

    /// <summary>Menu entries that create starter effect prefabs in the right folder.</summary>
    public static class VfxPrefabFactory
    {
        private const string UnlitSpriteMaterial = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        /// <summary>Sorting order of new effect prefabs: above characters (player 10).</summary>
        public const int DefaultSortingOrder = 20;

        /// <summary>New flipbook effect (SkillVfx + one VfxFlipbook child) in the selected VFX folder.</summary>
        [MenuItem("Assets/Create/Evermeadow/Skill VFX (Flipbook)", priority = 210)]
        public static void CreateFlipbook() => Create(false);

        /// <summary>New particle effect (SkillVfx + one 2D-friendly ParticleSystem child) in the selected VFX folder.</summary>
        [MenuItem("Assets/Create/Evermeadow/Skill VFX (Particle)", priority = 211)]
        public static void CreateParticle() => Create(true);

        private static void Create(bool particle)
        {
            VfxCatalogBuilder.EnsureFolders();
            string folder = SelectedFolder();
            if (!folder.StartsWith(VfxNaming.Root + "/")) folder = VfxNaming.Root + "/" + (particle ? VfxCategory.Hit : VfxCategory.Slash);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/vfx_cmn_new_{(particle ? "particle" : "flipbook")}.prefab");

            var root = new GameObject(Path.GetFileNameWithoutExtension(path));
            root.AddComponent<SkillVfx>();
            var material = AssetDatabase.LoadAssetAtPath<Material>(UnlitSpriteMaterial);
            if (particle)
            {
                var child = new GameObject("Particles");
                child.transform.SetParent(root.transform, false);
                var ps = child.AddComponent<ParticleSystem>();
                ParticleSystem.MainModule main = ps.main;
                main.duration = 0.5f;
                main.loop = false;
                main.startLifetime = 0.4f;
                main.startSpeed = 3f;
                main.startSize = 0.15f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.playOnAwake = false;
                main.maxParticles = 64;
                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });
                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.1f;
                var renderer = child.GetComponent<ParticleSystemRenderer>();
                if (material != null) renderer.sharedMaterial = material;
                renderer.sortingOrder = DefaultSortingOrder;
            }
            else
            {
                var child = new GameObject("Flipbook");
                child.transform.SetParent(root.transform, false);
                var sr = child.AddComponent<SpriteRenderer>();
                if (material != null) sr.sharedMaterial = material;
                sr.sortingOrder = DefaultSortingOrder;
                child.AddComponent<VfxFlipbook>();
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[VFX] Created {path}. Rename it vfx_<region>_<name> (e.g. vfx_cmn_smear_spear) and fill in the frames / particles.");
        }

        private static string SelectedFolder()
        {
            Object o = Selection.activeObject;
            string p = o != null ? AssetDatabase.GetAssetPath(o) : "";
            if (string.IsNullOrEmpty(p)) return "";
            return AssetDatabase.IsValidFolder(p) ? p : Path.GetDirectoryName(p)?.Replace('\\', '/') ?? "";
        }
    }
}
