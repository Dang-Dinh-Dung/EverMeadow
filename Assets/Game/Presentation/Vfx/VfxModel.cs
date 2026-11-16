using System;
using System.Collections.Generic;

namespace Evermeadow.Presentation.Vfx
{
    /// <summary>Kind of skill effect; also the sub-folder of <c>Assets/Game/Art/VFX/</c> it lives in.</summary>
    public enum VfxCategory
    {
        /// <summary>Weapon trails and smears (vfx_cmn_smear_[family]).</summary>
        Slash,
        /// <summary>Impact sparks where a hit lands.</summary>
        Hit,
        /// <summary>Signature effects of special skills.</summary>
        Special,
        /// <summary>Auras and charge glows around a creature.</summary>
        Aura,
        /// <summary>Projectiles and their trails.</summary>
        Projectile,
        /// <summary>Buff / debuff markers.</summary>
        Buff,
        /// <summary>Dust, debris, ground marks.</summary>
        Environment,
        /// <summary>Anything else.</summary>
        Other
    }

    /// <summary>One effect known to the catalog: its key (the prefab name), category and asset path.</summary>
    public readonly struct VfxCatalogEntry
    {
        /// <summary>Key used by skills (Effect.PrefabName, Hit.HitEffectAddressable): the prefab file name.</summary>
        public readonly string Key;
        /// <summary>Category.</summary>
        public readonly VfxCategory Category;
        /// <summary>Project-relative asset path of the prefab.</summary>
        public readonly string AssetPath;

        /// <summary>Creates an entry.</summary>
        public VfxCatalogEntry(string key, VfxCategory category, string assetPath)
        {
            Key = key;
            Category = category;
            AssetPath = assetPath;
        }
    }

    /// <summary>
    /// Naming rules of the art bible for VFX (<c>vfx_&lt;region|cmn&gt;_&lt;name&gt;</c>, lower case, digits and underscores)
    /// and the folder → category mapping of <c>Assets/Game/Art/VFX/&lt;Category&gt;/</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// VfxNaming.IsValidKey("vfx_cmn_smear_spear");             // true
    /// VfxNaming.CategoryOf("Assets/Game/Art/VFX/Hit/vfx_cmn_hit_spark.prefab"); // Hit
    /// </code>
    /// </example>
    public static class VfxNaming
    {
        /// <summary>Root folder of skill effect prefabs.</summary>
        public const string Root = "Assets/Game/Art/VFX";

        /// <summary>True for <c>vfx_&lt;region&gt;_&lt;name&gt;</c>: lower-case letters, digits, underscores, at least three parts.</summary>
        public static bool IsValidKey(string key)
        {
            if (string.IsNullOrEmpty(key) || !key.StartsWith("vfx_", StringComparison.Ordinal)) return false;
            foreach (char c in key)
                if (!(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_')) return false;
            string[] parts = key.Split('_');
            if (parts.Length < 3) return false;
            foreach (string p in parts)
                if (p.Length == 0) return false;
            return true;
        }

        /// <summary>Region part of a key (<c>cmn</c> for shared effects), or empty.</summary>
        public static string RegionOf(string key)
        {
            if (!IsValidKey(key)) return "";
            return key.Split('_')[1];
        }

        /// <summary>Category from the first folder under <see cref="Root"/>; <see cref="VfxCategory.Other"/> when unknown.</summary>
        public static VfxCategory CategoryOf(string assetPath)
        {
            string p = (assetPath ?? "").Replace('\\', '/');
            if (!p.StartsWith(Root + "/", StringComparison.OrdinalIgnoreCase)) return VfxCategory.Other;
            string rest = p.Substring(Root.Length + 1);
            int slash = rest.IndexOf('/');
            if (slash <= 0) return VfxCategory.Other;
            return Enum.TryParse(rest.Substring(0, slash), true, out VfxCategory c) && Enum.IsDefined(typeof(VfxCategory), c) ? c : VfxCategory.Other;
        }

        /// <summary>Key (file name without extension) of an asset path.</summary>
        public static string KeyOf(string assetPath)
        {
            string p = (assetPath ?? "").Replace('\\', '/');
            string file = p.Substring(p.LastIndexOf('/') + 1);
            int dot = file.LastIndexOf('.');
            return dot > 0 ? file.Substring(0, dot) : file;
        }
    }

    /// <summary>Time maths of effects, kept pure so runtime playback and the editor preview agree exactly.</summary>
    /// <example>
    /// <code>
    /// int frame = VfxTime.FrameAt(0.25, fps: 12, frameCount: 6, loop: false);   // 3
    /// double life = VfxTime.Resolve(0, VfxTime.FlipbookLength(12, 6), particleLength: 0.8, loop: false); // 0.8
    /// </code>
    /// </example>
    public static class VfxTime
    {
        /// <summary>Length in seconds of a flipbook of <paramref name="frameCount"/> frames at <paramref name="fps"/>.</summary>
        public static double FlipbookLength(double fps, int frameCount) => fps > 0 && frameCount > 0 ? frameCount / fps : 0.0;

        /// <summary>Frame index shown at <paramref name="time"/> seconds: clamps on the last frame, or wraps when looping.</summary>
        public static int FrameAt(double time, double fps, int frameCount, bool loop)
        {
            if (frameCount <= 0 || fps <= 0 || double.IsNaN(time) || time <= 0) return 0;
            long frame = (long)Math.Floor(time * fps + 1e-9);
            return loop ? (int)(frame % frameCount) : (int)Math.Min(frame, frameCount - 1);
        }

        /// <summary>
        /// Lifetime of an effect: an explicit duration wins (&gt; 0); otherwise the longer of its flipbooks and particles.
        /// A looping effect without an explicit duration lives until stopped (<see cref="double.PositiveInfinity"/>).
        /// </summary>
        public static double Resolve(double explicitDuration, double flipbookLength, double particleLength, bool loop)
        {
            if (explicitDuration > 0) return explicitDuration;
            if (loop) return double.PositiveInfinity;
            double content = Math.Max(flipbookLength, particleLength);
            return content > 0 ? content : 1.0;
        }
    }

    /// <summary>
    /// The effects the picker and the game know, by key. Built from the catalog asset (runtime) or the asset database (editor);
    /// reports duplicate keys and keys that break the naming rule; filters for the Skill Tool picker.
    /// </summary>
    /// <example>
    /// <code>
    /// var index = new VfxIndex(entries);
    /// foreach (VfxCatalogEntry e in index.Filter("smear", VfxCategory.Slash)) Draw(e);
    /// </code>
    /// </example>
    public sealed class VfxIndex
    {
        private readonly Dictionary<string, VfxCatalogEntry> _byKey = new Dictionary<string, VfxCatalogEntry>(StringComparer.Ordinal);
        private readonly List<VfxCatalogEntry> _all = new List<VfxCatalogEntry>();

        /// <summary>Problems found while indexing (duplicate keys, bad names).</summary>
        public readonly List<string> Problems = new List<string>();

        /// <summary>Indexes <paramref name="entries"/>; the first of two equal keys wins.</summary>
        public VfxIndex(IEnumerable<VfxCatalogEntry> entries)
        {
            foreach (VfxCatalogEntry e in entries)
            {
                if (!VfxNaming.IsValidKey(e.Key)) Problems.Add($"'{e.Key}' ({e.AssetPath}) breaks the naming rule vfx_<region>_<name>");
                if (_byKey.ContainsKey(e.Key))
                {
                    Problems.Add($"duplicate key '{e.Key}': {_byKey[e.Key].AssetPath} and {e.AssetPath}");
                    continue;
                }
                _byKey.Add(e.Key, e);
                _all.Add(e);
            }
            _all.Sort((a, b) => a.Category != b.Category ? a.Category.CompareTo(b.Category) : string.CompareOrdinal(a.Key, b.Key));
        }

        /// <summary>Number of effects.</summary>
        public int Count => _all.Count;

        /// <summary>Every effect, by category then key.</summary>
        public IReadOnlyList<VfxCatalogEntry> All => _all;

        /// <summary>Looks up an effect by key.</summary>
        public bool TryGet(string key, out VfxCatalogEntry entry)
        {
            if (key != null) return _byKey.TryGetValue(key, out entry);
            entry = default;
            return false;
        }

        /// <summary>Effects whose key contains every word of <paramref name="search"/> (case-insensitive), in <paramref name="category"/> (null = all).</summary>
        public List<VfxCatalogEntry> Filter(string search, VfxCategory? category)
        {
            string[] words = (search ?? "").ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<VfxCatalogEntry>();
            foreach (VfxCatalogEntry e in _all)
            {
                if (category.HasValue && e.Category != category.Value) continue;
                bool all = true;
                foreach (string w in words)
                    if (e.Key.IndexOf(w, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        all = false;
                        break;
                    }
                if (all) result.Add(e);
            }
            return result;
        }
    }
}
