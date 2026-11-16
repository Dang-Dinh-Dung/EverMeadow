using System.Collections.Generic;
using System.Linq;
using Evermeadow.Presentation.SpriteAnimation;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Presentation
{
    /// <summary>Shared animation slots (user decision 2026-11-14): one name set for every rig; automatic vs skill slots.</summary>
    public class AnimationSlotsTests
    {
        [Test]
        public void Defaults_HaveAutomaticAndNumberedSkillSlots()
        {
            CollectionAssert.IsSubsetOf(AnimationSlots.Automatic, AnimationSlots.Defaults);
            CollectionAssert.Contains(AnimationSlots.Defaults, "idle2");
            CollectionAssert.Contains(AnimationSlots.Defaults, "attack12");
            CollectionAssert.Contains(AnimationSlots.Defaults, "skill8");
            CollectionAssert.AllItemsAreUnique(AnimationSlots.Defaults);
            Assert.IsTrue(AnimationSlots.Defaults.All(AnimationSlots.IsName));
        }

        [TestCase("idle1", true)]
        [TestCase("walk", true)]
        [TestCase("jump_fall", true)]
        [TestCase("dodge", true)]
        [TestCase("hurt", true)]
        [TestCase("die", true)]
        [TestCase("idle2", false)]
        [TestCase("attack1", false)]
        [TestCase("skill3", false)]
        public void IsAutomatic_LocomotionAndReactionsOnly(string slot, bool expected) => Assert.AreEqual(expected, AnimationSlots.IsAutomatic(slot));

        [Test]
        public void SkillSlots_DropsAutomaticKeepsOrder()
        {
            CollectionAssert.AreEqual(new[] { "attack2", "idle3", "attack1" }, AnimationSlots.SkillSlots(new[] { "walk", "attack2", "idle3", "hurt", "attack1" }));
        }

        [Test]
        public void Parse_CommentsBadNamesRepeats_AndAutomaticAlwaysPresent()
        {
            var errors = new List<string>();
            List<string> list = AnimationSlots.Parse("# slots\r\nattack1\n  spin_kick  # new\n\nattack1\nBig Hit\n", errors);

            CollectionAssert.IsSubsetOf(AnimationSlots.Automatic, list, "automatic slots are added when missing");
            CollectionAssert.Contains(list, "attack1");
            CollectionAssert.Contains(list, "spin_kick");
            Assert.AreEqual(1, list.Count(s => s == "attack1"));
            Assert.AreEqual(2, errors.Count, string.Join("\n", errors));
            StringAssert.Contains("line 5", errors[0]);
            StringAssert.Contains("line 6", errors[1]);
        }

        [Test]
        public void Parse_DefaultFile_GivesDefaults()
        {
            CollectionAssert.AreEquivalent(AnimationSlots.Defaults, AnimationSlots.Parse(AnimationSlots.DefaultFileText, null));
        }

        [Test]
        public void NewClipDefaults_LoopAndFrameLength()
        {
            Assert.IsTrue(AnimationSlots.LoopsByDefault("idle2"));
            Assert.IsTrue(AnimationSlots.LoopsByDefault("run"));
            Assert.IsFalse(AnimationSlots.LoopsByDefault("attack1"));
            Assert.AreEqual(3, AnimationSlots.DefaultTicks("attack4"));
            Assert.AreEqual(6, AnimationSlots.DefaultTicks("idle1"));
            Assert.AreEqual(4, AnimationSlots.DefaultTicks("hurt"));
        }
    }
}
