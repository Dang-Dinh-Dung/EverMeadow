using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evermeadow.Presentation.Vfx
{
    /// <summary>
    /// The list of every skill effect prefab under <c>Assets/Game/Art/VFX/</c>, by key (prefab name). Rebuilt automatically by
    /// the editor whenever a prefab there is added, moved or deleted (menu <c>Evermeadow ▸ VFX ▸ Rebuild Catalog</c> does it
    /// by hand); never edited by hand. The game references this one asset, so every listed prefab ships in the build.
    /// </summary>
    /// <example>
    /// <code>
    /// if (catalog.TryGetPrefab("vfx_cmn_hit_spark", out SkillVfx prefab)) Instantiate(prefab);
    /// </code>
    /// </example>
    [CreateAssetMenu(menuName = "Evermeadow/VFX Catalog", fileName = "VfxCatalog")]
    public sealed class VfxCatalog : ScriptableObject
    {
        /// <summary>One catalog row.</summary>
        [Serializable]
        public sealed class Entry
        {
            /// <summary>Key (prefab name).</summary>
            public string Key;
            /// <summary>Category (its folder).</summary>
            public VfxCategory Category;
            /// <summary>The prefab.</summary>
            public SkillVfx Prefab;
            /// <summary>Asset path when the catalog was built (for messages).</summary>
            public string Path;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        private Dictionary<string, Entry> _byKey;

        /// <summary>Rows (by category then key).</summary>
        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>Replaces every row (editor builder only).</summary>
        public void SetEntries(List<Entry> entries)
        {
            _entries = entries ?? new List<Entry>();
            _byKey = null;
        }

        /// <summary>Looks up a prefab by key.</summary>
        public bool TryGetPrefab(string key, out SkillVfx prefab)
        {
            if (_byKey == null)
            {
                _byKey = new Dictionary<string, Entry>(StringComparer.Ordinal);
                foreach (Entry e in _entries)
                    if (e?.Prefab != null && !string.IsNullOrEmpty(e.Key) && !_byKey.ContainsKey(e.Key)) _byKey.Add(e.Key, e);
            }
            prefab = null;
            if (key == null || !_byKey.TryGetValue(key, out Entry entry)) return false;
            prefab = entry.Prefab;
            return prefab != null;
        }

        /// <summary>The rows as a pure index (filters, problems).</summary>
        public VfxIndex BuildIndex()
        {
            var list = new List<VfxCatalogEntry>(_entries.Count);
            foreach (Entry e in _entries)
                if (e != null) list.Add(new VfxCatalogEntry(e.Key, e.Category, e.Path));
            return new VfxIndex(list);
        }

        private void OnValidate() => _byKey = null;
    }
}
