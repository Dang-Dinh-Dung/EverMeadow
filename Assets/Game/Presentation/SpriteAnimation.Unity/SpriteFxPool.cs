using UnityEngine;

namespace Evermeadow.Presentation.SpriteAnimation.Unity
{
    /// <summary>
    /// Fixed pool of sprite effects (flipbooks and fading ghosts) stepped on the scaled clock. Allocates only when created.
    /// Decorative only: nothing reads it back into gameplay (art bible §8.10).
    /// </summary>
    public sealed class SpriteFxPool
    {
        private struct Fx
        {
            public bool Active;
            public Sprite[] Frames;
            public int TicksPerFrame;
            public float FadeTicks; // > 0: ghost mode, fades over this many ticks
            public float Age;       // in 60 Hz ticks
            public Color Tint;
        }

        private readonly SpriteRenderer[] _renderers;
        private readonly Fx[] _fx;

        /// <summary>Creates <paramref name="size"/> renderers under <paramref name="parent"/>.</summary>
        public SpriteFxPool(Transform parent, int size, int sortingOrder)
        {
            _renderers = new SpriteRenderer[size];
            _fx = new Fx[size];
            for (int i = 0; i < size; i++)
            {
                var go = new GameObject("fx");
                go.transform.SetParent(parent, false);
                go.SetActive(false);
                _renderers[i] = go.AddComponent<SpriteRenderer>();
                _renderers[i].sortingOrder = sortingOrder;
            }
        }

        /// <summary>Plays a flipbook at <paramref name="position"/> (world).</summary>
        public void Play(Sprite[] frames, Vector3 position, int ticksPerFrame, Color tint, float scale = 1f)
        {
            if (frames == null || frames.Length == 0) return;
            int i = Take();
            _fx[i] = new Fx { Active = true, Frames = frames, TicksPerFrame = Mathf.Max(1, ticksPerFrame), Tint = tint };
            Show(i, frames[0], position, false, tint, scale);
        }

        /// <summary>Leaves a fading copy of <paramref name="sprite"/> (dodge afterimage).</summary>
        public void Ghost(Sprite sprite, Vector3 position, bool flipX, Color tint, float fadeTicks)
        {
            if (sprite == null) return;
            int i = Take();
            _fx[i] = new Fx { Active = true, FadeTicks = Mathf.Max(1f, fadeTicks), Tint = tint };
            Show(i, sprite, position, flipX, tint, 1f);
        }

        /// <summary>Advances every effect by <paramref name="animTicks"/> (60 Hz ticks, fractional under hit-stop).</summary>
        public void Advance(float animTicks)
        {
            for (int i = 0; i < _fx.Length; i++)
            {
                if (!_fx[i].Active) continue;
                ref Fx f = ref _fx[i];
                f.Age += animTicks;
                SpriteRenderer r = _renderers[i];
                if (f.FadeTicks > 0f)
                {
                    float a = 1f - f.Age / f.FadeTicks;
                    if (a <= 0f) { Stop(i); continue; }
                    r.color = new Color(f.Tint.r, f.Tint.g, f.Tint.b, f.Tint.a * a);
                }
                else
                {
                    int frame = (int)(f.Age / f.TicksPerFrame);
                    if (frame >= f.Frames.Length) { Stop(i); continue; }
                    r.sprite = f.Frames[frame];
                }
            }
        }

        private void Show(int i, Sprite sprite, Vector3 position, bool flipX, Color tint, float scale)
        {
            SpriteRenderer r = _renderers[i];
            r.sprite = sprite;
            r.flipX = flipX;
            r.color = tint;
            r.transform.position = position;
            r.transform.localScale = new Vector3(scale, scale, 1f);
            r.gameObject.SetActive(true);
        }

        private void Stop(int i)
        {
            _fx[i].Active = false;
            _renderers[i].gameObject.SetActive(false);
        }

        private int Take()
        {
            int oldest = 0;
            for (int i = 0; i < _fx.Length; i++)
            {
                if (!_fx[i].Active) return i;
                if (_fx[i].Age > _fx[oldest].Age) oldest = i;
            }
            return oldest;
        }
    }
}
