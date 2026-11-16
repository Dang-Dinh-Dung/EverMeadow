using System.Collections.Generic;
using System.Linq;
using Evermeadow.Presentation.SpriteAnimation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Evermeadow.Tests.EditMode.Presentation
{
    /// <summary>The Sprite Clip Editor's clip writing and reading (in-memory clip and sprites, no asset written).</summary>
    public class SpriteClipTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created) Object.DestroyImmediate(o);
            _created.Clear();
        }

        private Sprite[] Sprites(params string[] names)
        {
            var tex = new Texture2D(4, 4);
            _created.Add(tex);
            return names.Select(n =>
            {
                Sprite s = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f), 16f);
                s.name = n;
                _created.Add(s);
                return s;
            }).ToArray();
        }

        [Test, Description("One key per frame at the chosen rate, plus a closing key; reading it back gives the frames only")]
        public void SetSprites_ThenGetSprites_RoundTrip()
        {
            var clip = new AnimationClip();
            _created.Add(clip);
            Sprite[] frames = Sprites("idle_0", "idle_1", "idle_2");

            AnimationAssetMenu.SetSprites(clip, frames, 10f, loop: false);

            Assert.AreEqual(10f, clip.frameRate);
            Assert.AreEqual(0.3f, clip.length, 1e-4f, "the last frame lasts a full frame");
            Assert.IsFalse(AnimationUtility.GetAnimationClipSettings(clip).loopTime);
            CollectionAssert.AreEqual(frames, AnimationAssetMenu.GetSprites(clip));
        }

        [Test, Description("The clip list of a character follows its states; with a look it lists the look's clips")]
        public void ClipsOf_StateOrder_LookClipsReplaceBase()
        {
            var ac = new AnimatorController();
            ac.AddLayer("Base Layer");
            _created.Add(ac);
            AnimationClip idle = Track(new AnimationClip { name = "idle" }), walk = Track(new AnimationClip { name = "walk" });
            ac.layers[0].stateMachine.AddState("idle").motion = idle;
            ac.layers[0].stateMachine.AddState("walk").motion = walk;
            AnimationClip spearWalk = Track(new AnimationClip { name = "walk" });
            var spear = Track(new AnimatorOverrideController(ac));
            spear[walk] = spearWalk;

            var baseClips = AnimationAssetMenu.ClipsOf(ac);
            var lookClips = AnimationAssetMenu.ClipsOf(ac, spear);

            CollectionAssert.AreEqual(new[] { "idle", "walk" }, baseClips.Select(c => c.state));
            CollectionAssert.AreEqual(new[] { idle, walk }, baseClips.Select(c => c.clip));
            CollectionAssert.AreEqual(new[] { idle, spearWalk }, lookClips.Select(c => c.clip), "idle not overridden yet: base clip");
        }

        private T Track<T>(T o) where T : Object
        {
            _created.Add(o);
            return o;
        }

        [Test, Description("Frames sort by the numbers in their names: idle_2 before idle_10")]
        public void SpritesOf_NaturalNameOrder()
        {
            Sprite[] s = Sprites("idle_10", "idle_2", "idle_0", "idle_1");

            string[] names = AnimationAssetMenu.SpritesOf(s).Select(x => x.name).ToArray();

            CollectionAssert.AreEqual(new[] { "idle_0", "idle_1", "idle_2", "idle_10" }, names);
        }
    }
}
