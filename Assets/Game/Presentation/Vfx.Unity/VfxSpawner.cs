using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Evermeadow.Presentation.Vfx
{
    /// <summary>
    /// Spawns skill effects by key from a <see cref="VfxCatalog"/>, pooled per key (no Instantiate / Destroy per hit), and
    /// returns them to the pool when they finish. Time is pushed in through <see cref="Tick"/> so the owner can feed the game
    /// clock (hit-stop, pause). No allocation per spawn once a pool is warm. Single-thread only. The skill runtime (Effect
    /// node, Hit effect) will call <see cref="Spawn"/>; until then <see cref="VfxRunner"/> hosts one for scenes. Editing a prefab
    /// while playing reaches the game through <see cref="Invalidate"/> (no restart).
    /// </summary>
    /// <example>
    /// <code>
    /// var spawner = new VfxSpawner(catalog, fxRoot);
    /// spawner.Spawn("vfx_cmn_hit_spark", hitPoint, Quaternion.identity);
    /// void Update() => spawner.Tick(gameClock.DeltaSeconds);
    /// </code>
    /// </example>
    public sealed class VfxSpawner : IDisposable
    {
        private const int MaxPerKey = 32;

        private readonly VfxCatalog _catalog;
        private readonly Transform _root;
        private readonly Dictionary<string, ObjectPool<SkillVfx>> _pools = new Dictionary<string, ObjectPool<SkillVfx>>(StringComparer.Ordinal);
        private readonly List<SkillVfx> _active = new List<SkillVfx>(64);
        private readonly HashSet<string> _warned = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _generation = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<string> _keys = new List<string>();

        /// <summary>Creates a spawner; instances live under <paramref name="root"/> unless attached to a target.</summary>
        public VfxSpawner(VfxCatalog catalog, Transform root)
        {
            _catalog = catalog;
            _root = root;
        }

        /// <summary>Effects currently playing.</summary>
        public int ActiveCount => _active.Count;

        /// <summary>
        /// Sorting floor for effects on the Default sorting layer: a renderer whose order is below it gets the floor added
        /// (relative order inside the prefab is kept), so skill effects draw above characters even when the prefab was left at
        /// order 0. 0 = leave prefabs as authored. Applies to instances created after it is set.
        /// </summary>
        public int SortingOrderFloor { get; set; }

        /// <summary>
        /// Spawns <paramref name="key"/> at a world pose. With <paramref name="attachTo"/> the effect follows that transform
        /// (Effect BindType AttachToTarget). Returns null (and warns once per key) when the key is unknown.
        /// </summary>
        public SkillVfx Spawn(string key, Vector3 position, Quaternion rotation, float scale = 1f, float durationOverride = 0f, Transform attachTo = null)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!_pools.TryGetValue(key, out ObjectPool<SkillVfx> pool))
            {
                if (_catalog == null || !_catalog.TryGetPrefab(key, out SkillVfx prefab))
                {
                    if (_warned.Add(key)) Debug.LogWarning($"[VFX] Unknown effect '{key}' (not in the VFX catalog).");
                    return null;
                }
                int generation = GenerationOf(key);
                pool = new ObjectPool<SkillVfx>(
                    () =>
                    {
                        SkillVfx fx = UnityEngine.Object.Instantiate(prefab, _root);
                        if (SortingOrderFloor != 0)
                            foreach (Renderer r in fx.GetComponentsInChildren<Renderer>(true))
                                if (r.sortingLayerID == 0 && r.sortingOrder < SortingOrderFloor) r.sortingOrder += SortingOrderFloor;
                        fx.Key = key;
                        fx.Generation = generation;
                        fx.gameObject.SetActive(false);
                        return fx;
                    },
                    fx => fx.gameObject.SetActive(true),
                    fx =>
                    {
                        fx.gameObject.SetActive(false);
                        fx.transform.SetParent(_root, false);
                    },
                    fx => { if (fx != null) UnityEngine.Object.Destroy(fx.gameObject); },
                    false, 2, MaxPerKey);
                _pools.Add(key, pool);
            }

            SkillVfx instance = pool.Get();
            Transform t = instance.transform;
            t.SetParent(attachTo != null ? attachTo : _root, true);
            t.SetPositionAndRotation(position, rotation);
            t.localScale = Vector3.one * scale;
            instance.Play(durationOverride);
            _active.Add(instance);
            return instance;
        }

        /// <summary>Stops an effect early (it still fades out unless <paramref name="immediate"/>).</summary>
        public void Stop(SkillVfx fx, bool immediate = false)
        {
            if (fx != null) fx.Stop(immediate);
        }

        /// <summary>Advances every playing effect by <paramref name="dt"/> seconds and returns finished ones to their pool.</summary>
        public void Tick(float dt)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                SkillVfx fx = _active[i];
                if (fx == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }
                if (fx.Advance(dt)) continue;
                _active.RemoveAt(i);
                if (_pools.TryGetValue(fx.Key, out ObjectPool<SkillVfx> pool) && fx.Generation == GenerationOf(fx.Key)) pool.Release(fx);
                else UnityEngine.Object.Destroy(fx.gameObject);   // made from an older version of the prefab
            }
        }

        /// <summary>
        /// Forgets the pooled instances of <paramref name="key"/> (its prefab was edited or removed, or the catalog changed):
        /// the next <see cref="Spawn"/> instantiates the current prefab. Effects already playing finish with their old look
        /// and are then destroyed instead of pooled. Used by the Editor while playing (hot reload).
        /// </summary>
        public void Invalidate(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_pools.TryGetValue(key, out ObjectPool<SkillVfx> pool))
            {
                _pools.Remove(key);
                pool.Clear();
            }
            _generation[key] = GenerationOf(key) + 1;
            _warned.Remove(key);   // a key that was unknown may exist now
        }

        /// <summary><see cref="Invalidate"/> for every key (the whole catalog changed).</summary>
        public void InvalidateAll()
        {
            _keys.Clear();
            _keys.AddRange(_pools.Keys);
            _keys.AddRange(_generation.Keys);
            foreach (string key in _keys) Invalidate(key);
            _warned.Clear();
        }

        private int GenerationOf(string key) => _generation.TryGetValue(key, out int g) ? g : 0;

        /// <summary>Destroys every pooled and active instance.</summary>
        public void Dispose()
        {
            foreach (SkillVfx fx in _active)
                if (fx != null) UnityEngine.Object.Destroy(fx.gameObject);
            _active.Clear();
            foreach (ObjectPool<SkillVfx> pool in _pools.Values) pool.Clear();
            _pools.Clear();
        }
    }

    /// <summary>Scene host of a <see cref="VfxSpawner"/>, ticked with <c>Time.deltaTime</c> until the game clock drives it.</summary>
    public sealed class VfxRunner : MonoBehaviour
    {
        [SerializeField] private VfxCatalog _catalog;

        /// <summary>The spawner (created on Awake).</summary>
        public VfxSpawner Spawner { get; private set; }

        private void Awake() => Spawner = new VfxSpawner(_catalog, transform);

        private void Update() => Spawner?.Tick(Time.deltaTime);

        private void OnDestroy() => Spawner?.Dispose();
    }
}
