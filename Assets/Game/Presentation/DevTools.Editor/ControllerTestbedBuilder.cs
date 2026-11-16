using System;
using System.IO;
using Evermeadow.Core.CharacterController.Unity;
using Evermeadow.Foundation.Input.Unity;
using Evermeadow.Presentation.CharacterAnimation.Unity;
using Evermeadow.Presentation.SpriteAnimation.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evermeadow.Presentation.DevTools.Editor
{
    /// <summary>
    /// Builds the Character Controller testbed scene (dev only), in art-bible units (1 u = 1 tile = 16 px; decision C-12):
    /// floor, a 5-tile step, two one-way platforms, a 45° slope to a plateau, a low ceiling, a 7-tile gap, walls, the animated
    /// player, a follow camera framing the 640×360 reference view and the composition root.
    /// Menu: Evermeadow ▸ Dev ▸ Build Controller Testbed. Batch:
    /// <c>Unity -batchmode -quit -executeMethod Evermeadow.Presentation.DevTools.Editor.ControllerTestbedBuilder.Build</c>.
    /// The bootstrap type is resolved by name so this tool never references <c>Evermeadow.Bootstrap</c>.
    /// </summary>
    public static class ControllerTestbedBuilder
    {
        private const string ScenePath = "Assets/Scenes/Dev/ControllerTestbed.unity";
        private const string SpritePath = "Assets/Game/Presentation/DevTools.Editor/DevSquare.png";
        private const string ActionsPath = "Assets/Game/Input/EvermeadowInput.inputactions";
        private const string PlayerArtFolder = "Assets/Game/Rigs/Player";

        private static readonly Color Solid = new Color(0.36f, 0.42f, 0.30f);
        private static readonly Color OneWay = new Color(0.72f, 0.58f, 0.32f);

        /// <summary>
        /// Creates (or overwrites) the testbed scene. The player uses the first Animator Controller found in
        /// Assets/Game/Rigs/Player (or below); without one it is drawn as a block of its collider size.
        /// </summary>
        [MenuItem("Evermeadow/Dev/Build Controller Testbed")]
        public static void Build()
        {
            RuntimeAnimatorController animation = FindController(PlayerArtFolder);
            Sprite square = EnsureSprite();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(DevCameraFollow)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 11.25f; // 360 px / 16 PPU / 2: the 640×360 reference view
            cam.transform.position = new Vector3(-9f, 6f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.78f, 0.86f);

            var level = new GameObject("Level").transform;
            Block(level, square, "Floor", new Vector2(-28f, -2f), new Vector2(42f, 0f), Solid);
            Block(level, square, "Floor (after gap)", new Vector2(49f, -2f), new Vector2(92f, 0f), Solid);
            Block(level, square, "Wall left", new Vector2(-30f, 0f), new Vector2(-28f, 32f), Solid);
            Block(level, square, "Wall right", new Vector2(92f, 0f), new Vector2(94f, 32f), Solid);
            Block(level, square, "Low ceiling", new Vector2(-24f, 8.3f), new Vector2(-12f, 10.5f), Solid);
            Block(level, square, "Step 5 tiles", new Vector2(-5f, 0f), new Vector2(0f, 5f), Solid);
            Block(level, square, "One-way low", new Vector2(5f, 3.5f), new Vector2(14f, 4f), OneWay, oneWay: true);
            Block(level, square, "One-way high", new Vector2(8f, 8f), new Vector2(17f, 8.5f), OneWay, oneWay: true);

            // 45° slope (a square rotated 45°, top vertex at y = 11.5) up to a plateau
            var ramp = Block(level, square, "Slope 45°", Vector2.zero, new Vector2(16.26f, 16.26f), Solid);
            ramp.transform.position = new Vector3(32f, 0f, 0f);
            ramp.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            Block(level, square, "Plateau", new Vector2(32f, 0f), new Vector2(42f, 11.5f), Solid);

            GameObject player = BuildPlayer(animation, square, new Vector3(-9f, 3f, 0f));
            cam.GetComponent<DevCameraFollow>().SetTarget(player.transform);
            var follow = new SerializedObject(cam.GetComponent<DevCameraFollow>());
            follow.FindProperty("target").objectReferenceValue = player.transform;
            follow.ApplyModifiedPropertiesWithoutUndo();

            var root = new GameObject("GameBootstrap");
            root.AddComponent<InputRoot>();
            Type bootstrapType = Type.GetType("Evermeadow.Bootstrap.GameBootstrap, Evermeadow.Bootstrap", throwOnError: true);
            Component bootstrap = root.AddComponent(bootstrapType);
            var inputRoot = new SerializedObject(root.GetComponent<InputRoot>());
            inputRoot.FindProperty("actions").objectReferenceValue = AssetDatabase.LoadMainAssetAtPath(ActionsPath);
            inputRoot.ApplyModifiedPropertiesWithoutUndo();
            var boot = new SerializedObject(bootstrap);
            boot.FindProperty("player").objectReferenceValue = player.GetComponent<CharacterBody>();
            boot.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[ControllerTestbed] Saved {ScenePath}");
        }

        private static GameObject BuildPlayer(RuntimeAnimatorController animation, Sprite square, Vector3 position)
        {
            var player = new GameObject("Player", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(CharacterBody));
            player.transform.position = position;
            const float halfHeight = 1.5f; // ColliderHeight 3 u (48 px) / 2; rig pivot is at the feet

            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, -halfHeight, 0f);

            if (animation == null)
            {
                // No art yet: a block of the collider size (2.077 × 3 u) so movement can still be tested.
                var block = new GameObject("Block", typeof(SpriteRenderer));
                block.transform.SetParent(visual.transform, false);
                block.transform.localPosition = new Vector3(0f, halfHeight, 0f);
                block.transform.localScale = new Vector3(2.077f, 3f, 1f);
                var sr = block.GetComponent<SpriteRenderer>();
                sr.sprite = square;
                sr.color = new Color(0.92f, 0.86f, 0.62f);
                sr.sortingOrder = 10;
                return player;
            }

            var actor = visual.AddComponent<AnimatedSprite>(); // adds its Animator and SpriteRenderer
            actor.Renderer.sortingOrder = 10;
            actor.Animator.runtimeAnimatorController = animation;

            SpriteRenderer Pip(string name, float x)
            {
                var go = new GameObject(name, typeof(SpriteRenderer));
                go.transform.SetParent(player.transform, false);
                go.transform.localPosition = new Vector3(x, halfHeight + 0.4f, 0f);
                go.transform.localScale = new Vector3(0.1875f, 0.1875f, 1f); // 3 px at PPU 16 from the 1 u dev square
                var r = go.GetComponent<SpriteRenderer>();
                r.sprite = square;
                r.sortingOrder = 11;
                r.enabled = false;
                return r;
            }

            var driver = player.AddComponent<PlayerAnimationDriver>();
            var driverSo = new SerializedObject(driver);
            driverSo.FindProperty("body").objectReferenceValue = player.GetComponent<CharacterBody>();
            driverSo.FindProperty("actor").objectReferenceValue = actor;
            driverSo.FindProperty("pipJump").objectReferenceValue = Pip("Pip air jump", -0.19f);
            driverSo.FindProperty("pipDodge").objectReferenceValue = Pip("Pip air dodge", 0.19f);
            driverSo.ApplyModifiedPropertiesWithoutUndo();
            return player;
        }

        private static RuntimeAnimatorController FindController(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return null;
            string[] guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { folder });
            return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static GameObject Block(Transform parent, Sprite sprite, string name, Vector2 min, Vector2 max, Color color, bool oneWay = false)
        {
            var go = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D));
            go.transform.SetParent(parent, false);
            Vector2 size = max - min;
            go.transform.position = min + size * 0.5f;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            // Added before the sprite existed, the collider auto-sized to an empty sprite (0.0001); size it explicitly.
            var box = go.GetComponent<BoxCollider2D>();
            box.size = Vector2.one;
            box.offset = Vector2.zero;
            if (oneWay) go.AddComponent<OneWayPlatform>();
            return go;
        }

        private static Sprite EnsureSprite()
        {
            if (!File.Exists(SpritePath))
            {
                var tex = new Texture2D(4, 4);
                var pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(pixels);
                File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(SpritePath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 4f;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }
    }
}
