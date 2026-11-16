// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does the combat feel agile and weighted (Hollow Knight-paced, one notch heavier)?
// Date: 2026-10-24
//
// Builds the whole prototype scene from code on Awake, so the only manual
// setup is: new empty scene, one empty GameObject, attach this component,
// press Play. See prototypes/combat-feel-concept/README.md for the full
// setup steps and what to watch for.

using UnityEngine;

public class PrototypeBootstrap : MonoBehaviour
{
    void Awake()
    {
        BuildCamera();
        BuildGround();
        var player = BuildPlayer();
        var enemy = BuildEnemy(player.transform);
        player.enemy = enemy;
        BuildHud(player, enemy);
    }

    void BuildCamera()
    {
        if (Camera.main != null) return;
        var camGo = new GameObject("PrototypeCamera");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.tag = "MainCamera";
    }

    void BuildGround()
    {
        var ground = new GameObject("Ground");
        ground.transform.position = new Vector3(0, -3.5f, 0);
        ground.transform.localScale = new Vector3(16f, 1f, 1f);
        var sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = PrototypeVisuals.SolidSquare(new Color(0.3f, 0.3f, 0.32f));
        var box = ground.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;
    }

    CombatFeelPlayer BuildPlayer()
    {
        var go = new GameObject("Player");
        go.transform.position = new Vector3(-3f, 0f, 0f);
        go.transform.localScale = new Vector3(0.9f, 1.3f, 1f);
        go.AddComponent<SpriteRenderer>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        return go.AddComponent<CombatFeelPlayer>();
    }

    DummyEnemy BuildEnemy(Transform player)
    {
        var go = new GameObject("DummyEnemy");
        go.transform.position = new Vector3(3f, -2.4f, 0f); // resting on the ground top (~y=-3.0)
        go.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        go.AddComponent<SpriteRenderer>();
        // Round 3 fix: rounds 1-2 had no collider here, so the player's
        // OverlapBox never found the enemy and no hit ever landed. Trigger,
        // so it is hittable but does not physically block the player.
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        col.isTrigger = true;
        var enemy = go.AddComponent<DummyEnemy>();
        enemy.player = player;
        return enemy;
    }

    void BuildHud(CombatFeelPlayer player, DummyEnemy enemy)
    {
        var go = new GameObject("PrototypeHUD");
        var hud = go.AddComponent<PrototypeHUD>();
        hud.player = player;
        hud.enemy = enemy;
    }
}
