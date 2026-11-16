// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do the farm half and the outing half still feed each other when the world is a boss-gated chain of single-biome maps with a far second village?
// Date: 2026-10-29
//
// Small shared helpers. Copied (not imported) from the CombatFeel prototype and
// put in a namespace, because both prototypes compile into Assembly-CSharp and
// would otherwise collide on class names. Round 2 adds seeded noise and a
// camera that follows both axes and clamps to the current map.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DayLoopProto
{
    public enum Item { Healroot, Emberpepper, Potion, Meal, Ore, Bone, Gold, BossCore, Fish, BigFish, GrilledFish, SandKey, StoneCharm }

    public interface IHittable
    {
        void TakeHit(int damage, Vector2 fromPosition);
    }

    public static class Visuals
    {
        static readonly Dictionary<Color, Sprite> _cache = new Dictionary<Color, Sprite>();

        public static Sprite Solid(Color color)
        {
            if (_cache.TryGetValue(color, out var cached) && cached != null)
                return cached;

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            _cache[color] = sprite;
            return sprite;
        }

        public static GameObject Box(string name, Vector2 pos, Vector2 size, Color color, int order = 0, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Solid(Color.white);
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }

        // Box from a bottom-left corner, which is how landmark parts are authored.
        public static GameObject Rect(string name, float x0, float y0, float x1, float y1, Color color, int order, Transform parent) =>
            Box(name, new Vector2((x0 + x1) / 2f, (y0 + y1) / 2f), new Vector2(x1 - x0, y1 - y0), color, order, parent);
    }

    // Deterministic per (world seed, map id): the same seed always rebuilds the
    // same map, so a revisited map looks the way the player remembers it.
    public static class Noise
    {
        public static int Seed(string id, int worldSeed)
        {
            uint h = 2166136261u ^ (uint)worldSeed;
            foreach (char c in id) { h ^= c; h *= 16777619u; }
            return (int)(h & 0x7fffffff);
        }

        // Value-noise fbm in [0,1], same octave mix as the HTML world map.
        public static float[] Fbm(System.Random r, int n, float sampleStep)
        {
            var a = new float[n];
            float total = 0f;
            foreach (var (scaleTiles, weight) in new[] { (24f, 1f), (10f, 0.5f), (4f, 0.25f) })
            {
                float s = scaleTiles / sampleStep;
                int k = Mathf.CeilToInt(n / s) + 2;
                var knots = new float[k];
                for (int i = 0; i < k; i++) knots[i] = (float)r.NextDouble();
                for (int x = 0; x < n; x++)
                {
                    int i = Mathf.FloorToInt(x / s);
                    float f = x / s - i, u = f * f * (3f - 2f * f);
                    a[x] += weight * (knots[i] * (1f - u) + knots[i + 1] * u);
                }
                total += weight;
            }
            for (int x = 0; x < n; x++) a[x] /= total;
            return a;
        }

        public static float Range(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);
    }

    // Same fix as CombatFeel round 2: always restore to 1, never to a captured value.
    public static class HitStop
    {
        static Coroutine _active;
        static MonoBehaviour _runner;

        public static void Trigger(MonoBehaviour runner, float duration, float scale)
        {
            if (_runner != null && _active != null)
                _runner.StopCoroutine(_active);

            _runner = runner;
            _active = runner.StartCoroutine(Run(duration, scale));
        }

        public static void Clear()
        {
            if (_runner != null && _active != null) _runner.StopCoroutine(_active);
            _active = null;
            Time.timeScale = 1f;
        }

        static IEnumerator Run(float duration, float scale)
        {
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            _active = null;
        }
    }

    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float mapWidth = 60f;
        public float yOffset = 1.5f;
        public float smooth = 10f;
        Camera _cam;

        void Awake() => _cam = GetComponent<Camera>();

        Vector3 Desired()
        {
            float half = _cam.orthographicSize * _cam.aspect;
            float x = mapWidth <= 2f * half ? mapWidth / 2f : Mathf.Clamp(target.position.x, half, mapWidth - half);
            return new Vector3(x, target.position.y + yOffset, -10f);
        }

        void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.Lerp(transform.position, Desired(), 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime));
        }

        public void Snap()
        {
            if (target != null) transform.position = Desired();
        }
    }
}
