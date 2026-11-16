using UnityEngine;

namespace Evermeadow.Presentation.Vfx
{
    /// <summary>
    /// Root component of every skill effect prefab (folder <c>Assets/Game/Art/VFX/&lt;Category&gt;/</c>, name
    /// <c>vfx_&lt;region&gt;_&lt;name&gt;</c>). Plays its flipbooks (<see cref="VfxFlipbook"/>) and particle systems as one
    /// effect, knows its lifetime, and reports when it is finished so <see cref="VfxSpawner"/> can return it to the pool.
    /// Time is pushed in (<see cref="Advance"/>) so hit-stop / pause can be honoured by the caller; the editor preview uses
    /// <see cref="SetTime"/>. Particles are decorative only (art bible §VFX budget).
    /// </summary>
    /// <example>
    /// <code>
    /// SkillVfx fx = spawner.Spawn("vfx_cmn_smear_spear", pos, rot);   // pooled
    /// // spawner.Tick(dt) advances it and releases it when finished
    /// </code>
    /// </example>
    [DisallowMultipleComponent]
    public sealed class SkillVfx : MonoBehaviour
    {
        [Tooltip("Lifetime in seconds. 0 = automatic (longest flipbook / particle system; looping effects live until stopped).")]
        [SerializeField, Min(0f)] private float _duration;
        [Tooltip("Loop until stopped (auras, charge glows).")]
        [SerializeField] private bool _loop;
        [Tooltip("Short note for the effect browser (what it is, where it is used).")]
        [SerializeField, TextArea(1, 3)] private string _note = "";

        private VfxFlipbook[] _flipbooks;
        private ParticleSystem[] _particles;
        private float _time;
        private double _lifetime;
        private bool _stopping;

        /// <summary>Seconds since <see cref="Play"/>.</summary>
        public float Time => _time;
        /// <summary>True while playing (not yet finished).</summary>
        public bool IsPlaying { get; private set; }
        /// <summary>Browser note.</summary>
        public string Note => _note;
        /// <summary>True when the effect loops until stopped.</summary>
        public bool Loop => _loop;
        /// <summary>Key in the catalog (set by the spawner).</summary>
        public string Key { get; internal set; }

        /// <summary>Pool generation of the key when this instance was made; an older one is destroyed, not pooled (hot reload).</summary>
        internal int Generation { get; set; }

        /// <summary>Lifetime with <paramref name="durationOverride"/> (&gt; 0 wins, e.g. Effect.Duration of a skill node).</summary>
        public double LifetimeFor(float durationOverride)
        {
            Collect();
            double flip = 0, part = 0;
            bool loop = _loop;
            foreach (VfxFlipbook f in _flipbooks)
            {
                flip = System.Math.Max(flip, f.Length);
                loop |= f.Loop;
            }
            foreach (ParticleSystem p in _particles)
            {
                ParticleSystem.MainModule m = p.main;
                part = System.Math.Max(part, m.duration + m.startLifetime.constantMax);
                loop |= m.loop;
            }
            return VfxTime.Resolve(durationOverride > 0 ? durationOverride : _duration, flip, part, loop && _loop);
        }

        /// <summary>Starts (or restarts) the effect.</summary>
        public void Play(float durationOverride = 0f)
        {
            Collect();
            _time = 0f;
            _stopping = false;
            _lifetime = LifetimeFor(durationOverride);
            IsPlaying = true;
            foreach (VfxFlipbook f in _flipbooks) f.SetTime(0f);
            foreach (ParticleSystem p in _particles)
            {
                p.Clear(true);
                p.Play(true);
            }
        }

        /// <summary>Stops emitting; the effect finishes once remaining particles die (or at once with <paramref name="immediate"/>).</summary>
        public void Stop(bool immediate = false)
        {
            Collect();
            _stopping = true;
            foreach (ParticleSystem p in _particles)
                p.Stop(true, immediate ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
            foreach (VfxFlipbook f in _flipbooks)
                if (immediate || f.Loop) f.gameObject.SetActive(false);
            if (immediate) IsPlaying = false;
        }

        /// <summary>Advances by <paramref name="dt"/> seconds (game clock); returns false once finished.</summary>
        public bool Advance(float dt)
        {
            if (!IsPlaying) return false;
            _time += dt;
            foreach (VfxFlipbook f in _flipbooks) f.SetTime(_time);
            if (!_stopping && _time >= _lifetime) Stop();
            if (_stopping)
            {
                bool alive = false;
                foreach (ParticleSystem p in _particles) alive |= p.IsAlive(true);
                if (!alive) IsPlaying = false;
            }
            return IsPlaying;
        }

        /// <summary>
        /// Editor preview: shows the effect as it looks <paramref name="time"/> seconds after starting (particles are
        /// re-simulated from the start, flipbooks jump to their frame). Not for gameplay.
        /// </summary>
        public void SetTime(float time)
        {
            Collect();
            _time = time;
            foreach (VfxFlipbook f in _flipbooks) f.SetTime(time);
            foreach (ParticleSystem p in _particles)
                if (p.transform.parent == null || p.transform.parent.GetComponentInParent<ParticleSystem>() == null)
                    p.Simulate(Mathf.Max(0f, time), true, true, false);
        }

        private void Collect()
        {
            if (_flipbooks == null) _flipbooks = GetComponentsInChildren<VfxFlipbook>(true);
            if (_particles == null) _particles = GetComponentsInChildren<ParticleSystem>(true);
        }

        private void OnValidate()
        {
            _flipbooks = null;
            _particles = null;
        }
    }
}
