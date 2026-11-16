using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Evermeadow.Core.CharacterController;
using Evermeadow.Presentation.CharacterAnimation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Evermeadow.Presentation.SpriteAnimation.Editor
{
    /// <summary>
    /// Project menu for character animation (Evermeadow ▸ Animation), kept out of Unity's Assets ▸ Create menu:
    /// <list type="bullet">
    /// <item>Create Player / NPC / Creature Animator: an Animator Controller with every state, parameter and transition, plus one
    /// empty clip per state in <c>Base/</c> (frame rate from the art bible §8.7 frame durations).</item>
    /// <item>Create Look: an Animator Override Controller of the selected controller with its own empty clips, one per base clip
    /// (a weapon for the player, a season for an NPC).</item>
    /// <item>Sprite Clip Editor (<see cref="SpriteClipWindow"/>): puts sprites into a clip, one per frame.</item>
    /// </list>
    /// Player / NPC / Creature assets go to Assets/Game/Rigs/Player, /NPC, /Creature, or the selected subfolder inside them.
    /// </summary>
    public static class AnimationAssetMenu
    {
        private const string Menu = "Evermeadow/Animation/";

        /// <summary>Root folder of character animation assets; one subfolder per kind.</summary>
        public const string Root = "Assets/Game/Rigs";
        public const string PlayerFolder = Root + "/Player";
        public const string NpcFolder = Root + "/NPC";
        public const string CreatureFolder = Root + "/Creature";

        /// <summary>NPC action states (Action = 1, 2, …).</summary>
        public static readonly string[] NpcActions = { "talk", "work", "sit", "emote" };

        /// <summary>Creature action states (Action = 1, 2, …).</summary>
        /// <summary>Creature reactions wired in its controller (Action = 1, 2); its attacks are skill slots.</summary>
        public static readonly string[] CreatureActions = { AnimationSlots.Hurt, AnimationSlots.Die };

        /// <summary>Skill slot clips a new rig starts with (more: add a clip named like the slot, then Rebuild Animator From Clips).</summary>
        public static readonly string[] StarterSkillSlots = { "attack1", "attack2", "attack3" };

        // Old clip names → animation slots (migration 2026-11-14).
        private static readonly Dictionary<string, string> PlayerRename = new Dictionary<string, string>
        {
            ["idle"] = "idle1", ["rise"] = "jump_rise", ["apex"] = "jump_apex", ["fall"] = "jump_fall", ["hitstun"] = "hurt",
            ["defeated"] = "die", ["attack_ground"] = "attack1", ["attack_air"] = "attack2", ["attack_dash"] = "attack3",
            ["plunge"] = "attack4", ["plunge_land"] = "attack5"
        };

        private static readonly Dictionary<string, string> CreatureRename = new Dictionary<string, string>
        {
            ["idle"] = "idle1", ["move"] = "walk", ["attack"] = "attack1"
        };

        private static readonly EditorCurveBinding SpriteBinding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        [MenuItem(Menu + "Create Player Animator", priority = 100)]
        private static void CreatePlayer() => CreatePlayerAnimator(FolderIn(PlayerFolder));

        [MenuItem(Menu + "Create NPC Animator", priority = 101)]
        private static void CreateNpc()
        {
            string folder = FolderIn(NpcFolder);
            var names = new[] { AnimationSlots.Idle, AnimationSlots.Walk }.Concat(NpcActions).ToArray();
            CreateController(folder, "npc", names, (ac, clips) => CharacterAnimatorBuilder.BuildSimple(ac, clips, AnimationSlots.Idle, AnimationSlots.Walk, NpcActions));
        }

        [MenuItem(Menu + "Create Creature Animator", priority = 102)]
        private static void CreateCreature()
        {
            string folder = FolderIn(CreatureFolder);
            var names = new[] { AnimationSlots.Idle, AnimationSlots.Walk }.Concat(CreatureActions).Concat(StarterSkillSlots).ToArray();
            CreateController(folder, "creature", names, (ac, clips) =>
                CharacterAnimatorBuilder.BuildSimple(ac, clips, AnimationSlots.Idle, AnimationSlots.Walk, CreatureActions, StarterSkillSlots));
        }

        /// <summary>Creates the player Animator Controller and its base clips in <paramref name="folder"/>.</summary>
        public static AnimatorController CreatePlayerAnimator(string folder) =>
            CreateController(folder, "player", PlayerClips.All.Concat(StarterSkillSlots).ToArray(),
                (ac, clips) => CharacterAnimatorBuilder.BuildPlayer(ac, clips, new ControllerConfig().ApexHangThreshold, StarterSkillSlots));

        /// <summary>
        /// Rebuilds the selected character controller in place (same asset, looks keep working) from the clips in its
        /// <c>Base/</c> folder: automatic slots are wired, every other clip named like a slot becomes a skill slot state.
        /// </summary>
        [MenuItem(Menu + "Rebuild Animator From Clips", priority = 110)]
        private static void RebuildSelected()
        {
            AnimatorController ac = SelectedController();
            if (ac == null)
            {
                Debug.LogWarning("[Animation] Select a character Animator Controller (or one of its looks) first.");
                return;
            }
            Debug.Log("[Animation] " + Rebuild(ac));
        }

        [MenuItem(Menu + "Rebuild Animator From Clips", true)]
        private static bool RebuildSelectedValidate() => SelectedController() != null;

        /// <summary>Rebuilds <paramref name="ac"/> from the clips of its Base folder; returns a one-line report (missing slots…).</summary>
        public static string Rebuild(AnimatorController ac)
        {
            string path = AssetDatabase.GetAssetPath(ac);
            string folder = Path.GetDirectoryName(path).Replace('\\', '/');
            var clips = new Dictionary<string, AnimationClip>();
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { folder + "/Base" }))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (clip != null && !clips.ContainsKey(clip.name)) clips.Add(clip.name, clip);
            }
            List<string> slots = SlotList();
            var skill = AnimationSlots.SkillSlots(slots).Where(clips.ContainsKey).ToList();
            var extra = clips.Keys.Where(k => !slots.Contains(k)).OrderBy(k => k, System.StringComparer.Ordinal).ToList();
            CharacterAnimatorBuilder.Clear(ac);
            string[] wired;
            if (path.StartsWith(PlayerFolder + "/"))
            {
                CharacterAnimatorBuilder.BuildPlayer(ac, clips, new ControllerConfig().ApexHangThreshold, skill);
                wired = PlayerClips.All;
            }
            else if (path.StartsWith(NpcFolder + "/"))
            {
                CharacterAnimatorBuilder.BuildSimple(ac, clips, AnimationSlots.Idle, AnimationSlots.Walk, NpcActions);
                wired = new[] { AnimationSlots.Idle, AnimationSlots.Walk }.Concat(NpcActions).ToArray();
            }
            else
            {
                CharacterAnimatorBuilder.BuildSimple(ac, clips, AnimationSlots.Idle, AnimationSlots.Walk, CreatureActions, skill);
                wired = new[] { AnimationSlots.Idle, AnimationSlots.Walk }.Concat(CreatureActions).ToArray();
            }
            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            var missing = wired.Where(w => !clips.ContainsKey(w)).ToList();
            return $"Rebuilt {path}: {skill.Count} skill slot(s) [{string.Join(", ", skill)}]" +
                   (missing.Count > 0 ? $"; missing clips (state left empty): {string.Join(", ", missing)}" : "") +
                   (extra.Count > 0 ? $"; clips not named like a slot (ignored): {string.Join(", ", extra)}" : "");
        }

        /// <summary>
        /// One-off migration (2026-11-14): renames the old clips of every player / creature rig to animation slot names
        /// (idle → idle1, attack_ground → attack1, hitstun → hurt, creature move → walk…) in Base and every look, then rebuilds
        /// each character controller. Assets keep their GUIDs, so scenes, prefabs and looks keep their references.
        /// </summary>
        [MenuItem(Menu + "Migrate Rigs To Animation Slots", priority = 111)]
        public static void MigrateRigs()
        {
            var log = new List<string>();
            RenameUnder(PlayerFolder, PlayerRename, log);
            RenameUnder(CreatureFolder, CreatureRename, log);
            AssetDatabase.SaveAssets();
            foreach (AnimatorController ac in Characters()) log.Add(Rebuild(ac));
            Debug.Log("[Animation] Migration to animation slots:\n" + string.Join("\n", log));
        }

        private static void RenameUnder(string root, Dictionary<string, string> map, List<string> log)
        {
            if (!AssetDatabase.IsValidFolder(root)) return;
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (!map.TryGetValue(name, out string slot)) continue;
                string error = AssetDatabase.RenameAsset(path, slot);
                log.Add(string.IsNullOrEmpty(error) ? $"{path} → {slot}" : $"{path}: NOT renamed ({error})");
            }
        }

        /// <summary>The animation slot list (<see cref="AnimationSlots.FilePath"/>, or the defaults).</summary>
        public static List<string> SlotList()
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", AnimationSlots.FilePath));
            return File.Exists(full) ? AnimationSlots.Parse(File.ReadAllText(full), null) : AnimationSlots.Defaults.ToList();
        }

        [MenuItem(Menu + "Create Look (Weapon or Season)", priority = 200)]
        private static void CreateLookMenu() => CreateLook(SelectedController(), "look");

        [MenuItem(Menu + "Create Look (Weapon or Season)", true)]
        private static bool CreateLookValidate() => SelectedController() != null;

        /// <summary>
        /// Creates an override of <paramref name="baseController"/> in a new folder next to it: one empty clip per base clip (same
        /// name, frame rate and loop) and <c>&lt;folder&gt;.overrideController</c> mapping base → new clips.
        /// </summary>
        public static AnimatorOverrideController CreateLook(AnimatorController baseController, string name)
        {
            string baseFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(baseController)).Replace('\\', '/');
            string folder = AssetDatabase.GenerateUniqueAssetPath($"{baseFolder}/{name}");
            AssetDatabase.CreateFolder(baseFolder, Path.GetFileName(folder));

            var map = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (AnimationClip src in baseController.animationClips.Distinct())
            {
                bool loop = AnimationUtility.GetAnimationClipSettings(src).loopTime;
                AnimationClip clip = CharacterAnimatorBuilder.NewClip(src.name, loop, src.frameRate);
                AssetDatabase.CreateAsset(clip, $"{folder}/{src.name}.anim");
                map.Add(new KeyValuePair<AnimationClip, AnimationClip>(src, clip));
            }
            var overrides = new AnimatorOverrideController(baseController);
            overrides.ApplyOverrides(map);
            AssetDatabase.CreateAsset(overrides, $"{folder}/{Path.GetFileName(folder)}.overrideController");
            AssetDatabase.SaveAssets();
            Select(overrides);
            return overrides;
        }

        /// <summary>
        /// Replaces the sprite keys of <paramref name="clip"/> with <paramref name="sprites"/>, one per frame at
        /// <paramref name="fps"/>, plus a closing key so the last frame lasts a full frame; sets the clip's frame rate and loop.
        /// </summary>
        public static void SetSprites(AnimationClip clip, Sprite[] sprites, float fps, bool loop)
        {
            Undo.RecordObject(clip, "Set Sprites To Clip");
            clip.frameRate = fps;
            float step = 1f / fps;
            var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
            for (int i = 0; i < sprites.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i * step, value = sprites[i] };
            keys[sprites.Length] = new ObjectReferenceKeyframe { time = sprites.Length * step, value = sprites[sprites.Length - 1] };
            AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The frames of <paramref name="clip"/>, without the closing key <see cref="SetSprites"/> adds.</summary>
        public static Sprite[] GetSprites(AnimationClip clip)
        {
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, SpriteBinding) ?? new ObjectReferenceKeyframe[0];
            int n = keys.Length;
            if (n >= 2 && keys[n - 1].value == keys[n - 2].value) n--; // closing key
            return keys.Take(n).Select(k => k.value as Sprite).Where(sp => sp != null).ToArray();
        }

        /// <summary>
        /// Sprites in <paramref name="objects"/> (sliced sub-sprites of any texture included), ordered by name with numbers compared
        /// as numbers.
        /// </summary>
        public static Sprite[] SpritesOf(IEnumerable<Object> objects)
        {
            var sprites = new List<Sprite>();
            foreach (Object o in objects)
            {
                if (o is Sprite s) sprites.Add(s);
                else if (o is Texture2D t)
                    sprites.AddRange(AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(t)).OfType<Sprite>());
            }
            return sprites.Distinct().OrderBy(s => NaturalKey(s.name), System.StringComparer.Ordinal).ToArray();
        }

        /// <summary>Every character Animator Controller under <see cref="Root"/>, ordered by path.</summary>
        public static AnimatorController[] Characters() =>
            AssetDatabase.FindAssets("t:AnimatorController", new[] { Root })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, System.StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<AnimatorController>).Where(c => c != null).ToArray();

        /// <summary>The looks (override controllers) of <paramref name="character"/> under <see cref="Root"/>, ordered by path.</summary>
        public static AnimatorOverrideController[] LooksOf(AnimatorController character) =>
            AssetDatabase.FindAssets("t:AnimatorOverrideController", new[] { Root })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, System.StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>)
                .Where(o => o != null && o.runtimeAnimatorController == character).ToArray();

        /// <summary>
        /// The clips of <paramref name="character"/> in state order (idle, walk, run…), each named by its state; with
        /// <paramref name="look"/>, the look's clip for each state (the base clip where the look has none).
        /// </summary>
        public static (string state, AnimationClip clip)[] ClipsOf(AnimatorController character, AnimatorOverrideController look = null)
        {
            var map = new Dictionary<AnimationClip, AnimationClip>();
            if (look != null)
            {
                var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                look.GetOverrides(pairs);
                foreach (KeyValuePair<AnimationClip, AnimationClip> kv in pairs)
                {
                    if (kv.Key != null && kv.Value != null) map[kv.Key] = kv.Value;
                }
            }
            var result = new List<(string state, AnimationClip clip)>();
            var seen = new HashSet<AnimationClip>();
            foreach (AnimatorControllerLayer layer in character.layers)
            {
                foreach (ChildAnimatorState s in layer.stateMachine.states)
                {
                    if (!(s.state.motion is AnimationClip clip) || !seen.Add(clip)) continue;
                    result.Add((s.state.name, map.TryGetValue(clip, out AnimationClip over) ? over : clip));
                }
            }
            return result.ToArray();
        }

        /// <summary>Character and look that own <paramref name="clip"/> (look null = base clip); false when none does.</summary>
        public static bool FindOwner(AnimationClip clip, out AnimatorController character, out AnimatorOverrideController look)
        {
            foreach (AnimatorController c in Characters())
            {
                character = c;
                look = null;
                if (ClipsOf(c).Any(x => x.clip == clip)) return true;
                foreach (AnimatorOverrideController l in LooksOf(c))
                {
                    look = l;
                    if (ClipsOf(c, l).Any(x => x.clip == clip)) return true;
                }
            }
            character = null;
            look = null;
            return false;
        }

        /// <summary>Sort key comparing the numbers inside a name as numbers ("idle_2" before "idle_10").</summary>
        public static string NaturalKey(string name) => Regex.Replace(name, @"\d+", m => m.Value.PadLeft(8, '0'));

        // ---------------------------------------------------------------- helpers

        private static AnimatorController CreateController(string folder, string fileName, string[] clipNames,
            System.Action<AnimatorController, IReadOnlyDictionary<string, AnimationClip>> build)
        {
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{fileName}.controller");
            string baseFolder = AssetDatabase.GenerateUniqueAssetPath($"{folder}/Base");
            AssetDatabase.CreateFolder(folder, Path.GetFileName(baseFolder));

            var clips = new Dictionary<string, AnimationClip>();
            foreach (string name in clipNames)
            {
                AnimationClip clip = CharacterAnimatorBuilder.NewClip(name, IsLoop(name), 60f / DefaultTicks(name));
                AssetDatabase.CreateAsset(clip, $"{baseFolder}/{name}.anim");
                clips.Add(name, clip);
            }

            AnimatorController ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            build(ac, clips);
            AssetDatabase.SaveAssets();
            Select(ac);
            return ac;
        }

        /// <summary>
        /// The selected folder when it lies inside <paramref name="kindFolder"/> (one subfolder per NPC or creature species),
        /// else <paramref name="kindFolder"/>. Missing folders are created.
        /// </summary>
        public static string FolderIn(string kindFolder)
        {
            EnsureFolder(kindFolder);
            string selected = SelectedFolder();
            return selected == kindFolder || selected.StartsWith(kindFolder + "/") ? selected : kindFolder;
        }

        private static AnimatorController SelectedController()
        {
            Object o = Selection.activeObject;
            if (o is AnimatorController ac) return ac;
            if (o is AnimatorOverrideController oc) return oc.runtimeAnimatorController as AnimatorController;
            return null;
        }

        private static void Select(Object asset)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static string SelectedFolder()
        {
            string path = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : "Assets";
            if (string.IsNullOrEmpty(path)) return "Assets";
            return AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        }

        private static bool IsLoop(string clip) => AnimationSlots.LoopsByDefault(clip) || clip == "talk" || clip == "work" || clip == "sit";

        private static int DefaultTicks(string clip) => clip == "talk" || clip == "sit" ? 6 : AnimationSlots.DefaultTicks(clip);
    }
}
