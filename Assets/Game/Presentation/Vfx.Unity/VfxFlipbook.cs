using UnityEngine;

namespace Evermeadow.Presentation.Vfx
{
    /// <summary>
    /// A sprite flipbook (art bible: VFX are flipbooks of 32–128 px, grayscale + gradient map). Driven by time through
    /// <see cref="SetTime"/> so the game and the editor preview show exactly the same frame (<see cref="VfxTime.FrameAt"/>).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class VfxFlipbook : MonoBehaviour
    {
        [Tooltip("Frames in order.")]
        [SerializeField] private Sprite[] _frames = new Sprite[0];
        [Tooltip("Frames per second.")]
        [SerializeField, Min(1f)] private float _fps = 12f;
        [Tooltip("Loop the frames instead of holding the last one.")]
        [SerializeField] private bool _loop;
        [Tooltip("Hide the renderer once a non-looping flipbook has shown its last frame for one frame time.")]
        [SerializeField] private bool _hideWhenDone = true;

        private SpriteRenderer _renderer;

        /// <summary>Length of one pass in seconds.</summary>
        public double Length => VfxTime.FlipbookLength(_fps, _frames?.Length ?? 0);

        /// <summary>True when the frames loop.</summary>
        public bool Loop => _loop;

        /// <summary>Shows the frame for <paramref name="time"/> seconds since the effect started.</summary>
        public void SetTime(float time)
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            int count = _frames?.Length ?? 0;
            if (count == 0) return;
            _renderer.sprite = _frames[VfxTime.FrameAt(time, _fps, count, _loop)];
            _renderer.enabled = _loop || !_hideWhenDone || time < Length;
        }
    }
}
