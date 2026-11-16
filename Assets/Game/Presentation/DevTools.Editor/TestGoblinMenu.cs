using System.IO;
using System.Linq;
using System.Text;
using Evermeadow.Core.CharacterController.Unity;
using Evermeadow.Core.Skills;
using Evermeadow.Presentation.Creatures.Unity;
using Evermeadow.Presentation.SkillTool.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evermeadow.Presentation.DevTools.Editor
{
    /// <summary>
    /// Dev menu <c>Evermeadow ▸ Dev ▸ Add Test Goblin</c>: puts a goblin in the open scene (right of the player) to try the
    /// creature runtime — its Animator plays idle1 / walk by itself, its attack is a skill cast by its behaviour tree. Creates
    /// what is missing, never overwrites: a creature skill (Animation attack1 → End when the clip ends, plus a Hit area) and
    /// <c>StreamingAssets/AI/goblin.xml</c> (attack in range, chase in sight, patrol otherwise). Save the scene afterwards.
    /// </summary>
    public static class TestGoblinMenu
    {
        private const string ControllerPath = "Assets/Game/Rigs/Creature/goblin.controller";
        private const string BehaviourPath = "Assets/StreamingAssets/AI/goblin.xml";

        /// <summary>Adds the goblin and its data.</summary>
        [MenuItem("Evermeadow/Dev/Add Test Goblin", priority = 20)]
        public static void AddGoblin()
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogWarning($"[Dev] {ControllerPath} not found: create it with Evermeadow ▸ Animation ▸ Create Creature Animator.");
                return;
            }
            string skill = EnsureCreatureSkill();
            if (skill == null) return;
            EnsureBehaviour(skill);

            CharacterBody player = Object.FindAnyObjectByType<CharacterBody>();
            Vector3 at = player != null ? player.transform.position + new Vector3(6f, 0f, 0f)
                : SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;

            var goblin = new GameObject("Goblin");
            goblin.transform.position = new Vector3(at.x, at.y, 0f);
            var visual = new GameObject("Visual", typeof(SpriteRenderer), typeof(Animator));
            visual.transform.SetParent(goblin.transform, false);
            visual.GetComponent<SpriteRenderer>().sortingOrder = 9;   // under the player (10), under skill effects (20)
            visual.GetComponent<Animator>().runtimeAnimatorController = controller;
            goblin.AddComponent<CreatureBody>();

            Undo.RegisterCreatedObjectUndo(goblin, "Add Test Goblin");
            EditorSceneManager.MarkSceneDirty(goblin.scene);
            Selection.activeGameObject = goblin;
            Debug.Log($"[Dev] Goblin added (skill {skill}, behaviour {BehaviourPath}). Save the scene, then Play: it patrols, chases you and casts its skill.");
        }

        // An existing creature skill, or a new one: Start → Animation attack1 → (end) End, Start → Hit (0.15 s, 120° arc, r 1.2).
        private static string EnsureCreatureSkill()
        {
            foreach (SkillFileInfo f in SkillToolFiles.ListSkills())
                if (f.Label.Contains("(creature")) return f.Id;

            string id = SkillToolFiles.NextId();
            if (id == null)
            {
                Debug.LogWarning("[Dev] No free skill id left.");
                return null;
            }
            var g = new SkillGraphData { Id = id, Owner = SkillOwner.Creature };
            g.Nodes.Add(new SkillNodeData { Id = 0, Kind = SkillNodeKind.Start, X = -300, Y = 0 });
            var anim = new SkillNodeData { Id = 1, Kind = SkillNodeKind.Animation, X = 0, Y = 0 };
            anim.Values["AnimationType"] = "attack1";
            var hit = new SkillNodeData { Id = 2, Kind = SkillNodeKind.Hit, X = 0, Y = 320 };
            hit.Values["ActionDelay"] = "0.15";
            hit.Values["SubValue"] = "120,1.2,0";
            var end = new SkillNodeData { Id = 3, Kind = SkillNodeKind.End, X = 380, Y = 0 };
            g.Nodes.Add(anim);
            g.Nodes.Add(hit);
            g.Nodes.Add(end);
            g.Links.Add(new SkillLink { From = 0, To = 1, When = SkillTrigger.Start });
            g.Links.Add(new SkillLink { From = 0, To = 2, When = SkillTrigger.Start });
            g.Links.Add(new SkillLink { From = 1, To = 3, When = SkillTrigger.End });
            var errors = SkillToolFiles.Save(g);   // SearchObjectFlag default Enemies = hostile to the owner (the player here)
            if (errors.Count > 0)
            {
                Debug.LogWarning("[Dev] Could not create the goblin skill: " + string.Join("; ", errors.Select(e => e.ToString())));
                return null;
            }
            Debug.Log($"[Dev] Created creature skill {id} (attack1 → End, Hit area). Edit it in the Skill Tool.");
            return id;
        }

        private static void EnsureBehaviour(string skill)
        {
            string full = SkillToolFiles.FullPath(BehaviourPath);
            if (File.Exists(full)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            string xml =
                "<behaviour schema_version=\"1\">\n" +
                "  <selector>\n" +
                $"    <sequence><in_range max=\"1.6\"/><cast skill=\"{skill}\" cooldown=\"1.5\"/></sequence>\n" +
                "    <sequence><in_range max=\"8\"/><chase stop=\"1.3\"/></sequence>\n" +
                "    <patrol/>\n" +
                "  </selector>\n" +
                "</behaviour>\n";
            File.WriteAllBytes(full, new UTF8Encoding(false).GetBytes(xml));
            AssetDatabase.Refresh();
            Debug.Log($"[Dev] Created {BehaviourPath}.");
        }
    }
}
