using System.Linq;
using Evermeadow.Presentation.Vfx;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Presentation.Vfx
{
    /// <summary>
    /// Pure VFX rules shared by the runtime spawner and the Skill Tool picker: naming (art bible vfx_&lt;region&gt;_&lt;name&gt;),
    /// folder → category, flipbook frame maths, lifetime resolution, and the index (duplicates, filter).
    /// </summary>
    public class VfxModelTests
    {
        private const string Root = VfxNaming.Root;

        private static VfxCatalogEntry E(string key, VfxCategory c) => new VfxCatalogEntry(key, c, $"{Root}/{c}/{key}.prefab");

        // ------------------------------------------------------------ naming

        [TestCase("vfx_cmn_smear_spear", true)]
        [TestCase("vfx_wd_leaf_burst", true)]
        [TestCase("vfx_cmn_hit2", true)]
        [TestCase("vfx_cmn", false)]            // needs region and name
        [TestCase("vfx__spark", false)]         // empty part
        [TestCase("vfx_cmn_", false)]           // trailing underscore
        [TestCase("VFX_cmn_spark", false)]      // upper case
        [TestCase("vfx_cmn_Spark", false)]
        [TestCase("vfx-cmn-spark", false)]
        [TestCase("fx_cmn_spark", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void KeyName_ValidOnlyForVfxRegionNameLowerCase(string key, bool expected) => Assert.AreEqual(expected, VfxNaming.IsValidKey(key));

        [Test]
        public void RegionOf_ReturnsSecondPartOrEmpty()
        {
            Assert.AreEqual("cmn", VfxNaming.RegionOf("vfx_cmn_smear_spear"));
            Assert.AreEqual("wd", VfxNaming.RegionOf("vfx_wd_leaf"));
            Assert.AreEqual("", VfxNaming.RegionOf("bad"));
        }

        [TestCase(Root + "/Hit/vfx_cmn_hit_spark.prefab", VfxCategory.Hit)]
        [TestCase(Root + "/slash/vfx_cmn_smear_spear.prefab", VfxCategory.Slash)]          // folder case ignored
        [TestCase(Root + "/Special/Boss/vfx_cmn_boss_roar.prefab", VfxCategory.Special)]   // first folder decides
        [TestCase(@"Assets\Game\Art\VFX\Aura\vfx_cmn_glow.prefab", VfxCategory.Aura)]      // backslashes
        [TestCase(Root + "/Misc/vfx_cmn_x.prefab", VfxCategory.Other)]                     // unknown folder
        [TestCase(Root + "/42/vfx_cmn_x.prefab", VfxCategory.Other)]                       // numeric folder is not an enum value
        [TestCase(Root + "/vfx_cmn_x.prefab", VfxCategory.Other)]                          // directly under root
        [TestCase("Assets/Other/Hit/vfx_cmn_x.prefab", VfxCategory.Other)]                 // outside root
        public void AssetPath_CategoryFromFirstFolderUnderRoot(string path, VfxCategory expected) => Assert.AreEqual(expected, VfxNaming.CategoryOf(path));

        [Test]
        public void KeyOf_ReturnsFileNameWithoutExtension()
        {
            Assert.AreEqual("vfx_cmn_hit_spark", VfxNaming.KeyOf(Root + "/Hit/vfx_cmn_hit_spark.prefab"));
            Assert.AreEqual("vfx_cmn_a", VfxNaming.KeyOf(@"Assets\Game\Art\VFX\Hit\vfx_cmn_a.prefab"));
            Assert.AreEqual("vfx_cmn_a", VfxNaming.KeyOf("vfx_cmn_a"));
        }

        // ------------------------------------------------------------ time

        [Test]
        public void FlipbookLength_FramesOverFpsOrZero()
        {
            Assert.AreEqual(0.5, VfxTime.FlipbookLength(12, 6), 1e-12);
            Assert.AreEqual(0.0, VfxTime.FlipbookLength(0, 6));
            Assert.AreEqual(0.0, VfxTime.FlipbookLength(12, 0));
        }

        [TestCase(0.0, false, 0)]
        [TestCase(-1.0, false, 0)]
        [TestCase(0.0833, false, 0)]
        [TestCase(0.25, false, 3)]       // exact frame boundary (0.25 * 12 = 3)
        [TestCase(1.0 / 12.0, false, 1)] // boundary despite float error
        [TestCase(0.49, false, 5)]
        [TestCase(10.0, false, 5)]       // clamps on the last frame
        [TestCase(0.5, true, 0)]         // wraps
        [TestCase(0.75, true, 3)]
        public void FrameAt_TwelveFpsSixFrames_ClampsOrWraps(double t, bool loop, int expected) =>
            Assert.AreEqual(expected, VfxTime.FrameAt(t, 12, 6, loop));

        [Test]
        public void FrameAt_ProductJustBelowBoundary_StillReachesFrame()
        {
            // 0.29 * 100 = 28.999999999999996 in double: without the epsilon the 29th frame shows one frame late.
            Assert.AreEqual(29, VfxTime.FrameAt(0.29, 100, 50, false));
        }

        [Test]
        public void FrameAt_DegenerateInputs_ReturnsFirstFrame()
        {
            Assert.AreEqual(0, VfxTime.FrameAt(1.0, 12, 0, false));
            Assert.AreEqual(0, VfxTime.FrameAt(1.0, 0, 6, true));
            Assert.AreEqual(0, VfxTime.FrameAt(double.NaN, 12, 6, false));
        }

        [Test]
        public void Lifetime_ExplicitWinsElseLongestContentOrLoopForever()
        {
            Assert.AreEqual(0.3, VfxTime.Resolve(0.3, 0.5, 2.0, true), "explicit duration wins, even when looping");
            Assert.AreEqual(0.8, VfxTime.Resolve(0, 0.5, 0.8, false), "longest content");
            Assert.AreEqual(0.5, VfxTime.Resolve(0, 0.5, 0.2, false));
            Assert.AreEqual(1.0, VfxTime.Resolve(0, 0, 0, false), "empty effect falls back to one second");
            Assert.IsTrue(double.IsPositiveInfinity(VfxTime.Resolve(0, 0.5, 0.8, true)), "loop without duration lives until stopped");
            Assert.AreEqual(0.8, VfxTime.Resolve(-1, 0.5, 0.8, false), "negative duration means auto");
        }

        // ------------------------------------------------------------ index

        [Test]
        public void Index_ListsByCategoryThenKey()
        {
            var index = new VfxIndex(new[]
            {
                E("vfx_cmn_hit_b", VfxCategory.Hit), E("vfx_cmn_smear_spear", VfxCategory.Slash),
                E("vfx_cmn_hit_a", VfxCategory.Hit), E("vfx_cmn_aura", VfxCategory.Aura)
            });
            CollectionAssert.AreEqual(new[] { "vfx_cmn_smear_spear", "vfx_cmn_hit_a", "vfx_cmn_hit_b", "vfx_cmn_aura" }, index.All.Select(e => e.Key));
            Assert.AreEqual(4, index.Count);
            CollectionAssert.IsEmpty(index.Problems);
        }

        [Test]
        public void DuplicateKey_FirstWinsAndIsReported()
        {
            var first = new VfxCatalogEntry("vfx_cmn_spark", VfxCategory.Hit, Root + "/Hit/vfx_cmn_spark.prefab");
            var second = new VfxCatalogEntry("vfx_cmn_spark", VfxCategory.Special, Root + "/Special/vfx_cmn_spark.prefab");
            var index = new VfxIndex(new[] { first, second });
            Assert.AreEqual(1, index.Count);
            Assert.IsTrue(index.TryGet("vfx_cmn_spark", out VfxCatalogEntry got));
            Assert.AreEqual(VfxCategory.Hit, got.Category);
            Assert.AreEqual(1, index.Problems.Count);
            StringAssert.Contains("duplicate", index.Problems[0]);
        }

        [Test]
        public void BadName_ReportedButKept()
        {
            var index = new VfxIndex(new[] { E("Spark", VfxCategory.Hit) });
            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(1, index.Problems.Count);
            StringAssert.Contains("naming rule", index.Problems[0]);
        }

        [Test]
        public void TryGet_UnknownOrNullKey_ReturnsFalse()
        {
            var index = new VfxIndex(new[] { E("vfx_cmn_a", VfxCategory.Hit) });
            Assert.IsFalse(index.TryGet("vfx_cmn_b", out _));
            Assert.IsFalse(index.TryGet(null, out _));
        }

        [Test]
        public void Filter_ByCategoryAndEveryWord_CaseInsensitive()
        {
            var index = new VfxIndex(new[]
            {
                E("vfx_cmn_smear_spear", VfxCategory.Slash), E("vfx_cmn_smear_sword", VfxCategory.Slash),
                E("vfx_cmn_hit_spark", VfxCategory.Hit), E("vfx_wd_spear_rain", VfxCategory.Special)
            });
            Assert.AreEqual(4, index.Filter("", null).Count);
            Assert.AreEqual(4, index.Filter(null, null).Count);
            CollectionAssert.AreEqual(new[] { "vfx_cmn_smear_spear", "vfx_cmn_smear_sword" }, index.Filter("", VfxCategory.Slash).Select(e => e.Key));
            CollectionAssert.AreEqual(new[] { "vfx_cmn_smear_spear", "vfx_wd_spear_rain" }, index.Filter("SPEAR", null).Select(e => e.Key), "case-insensitive");
            CollectionAssert.AreEqual(new[] { "vfx_cmn_smear_spear" }, index.Filter("spear  smear", null).Select(e => e.Key), "every word must match");
            CollectionAssert.AreEqual(new[] { "vfx_wd_spear_rain" }, index.Filter("spear", VfxCategory.Special).Select(e => e.Key));
            CollectionAssert.IsEmpty(index.Filter("spark", VfxCategory.Slash));
        }
    }
}
