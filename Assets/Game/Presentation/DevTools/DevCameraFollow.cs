using UnityEngine;

namespace Evermeadow.Presentation.DevTools
{
    /// <summary>
    /// Development-only camera follow for testbeds, until the Camera system is designed (systems index #7). Follows the target
    /// with a dead zone and snaps to the 1/16-unit pixel grid (PPU 16, art bible §8.1) so pixel art does not swim.
    /// </summary>
    public sealed class DevCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset = new Vector2(0f, 3f);
        [SerializeField] private Vector2 deadZone = new Vector2(2f, 1.5f);
        [SerializeField, Range(1f, 20f)] private float sharpness = 8f;
        [SerializeField] private float pixelsPerUnit = 16f;

        private Vector3 _focus;

        /// <summary>Sets the followed transform and jumps to it.</summary>
        public void SetTarget(Transform t)
        {
            target = t;
            if (t != null) _focus = (Vector2)t.position + offset;
        }

        private void Start()
        {
            if (target != null) _focus = (Vector2)target.position + offset;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector2 goal = (Vector2)target.position + offset;
            Vector2 d = goal - (Vector2)_focus;
            if (Mathf.Abs(d.x) > deadZone.x) _focus.x += d.x - Mathf.Sign(d.x) * deadZone.x;
            if (Mathf.Abs(d.y) > deadZone.y) _focus.y += d.y - Mathf.Sign(d.y) * deadZone.y;

            Vector3 p = transform.position;
            float k = 1f - Mathf.Exp(-sharpness * Time.unscaledDeltaTime);
            p.x = Mathf.Lerp(p.x, _focus.x, k);
            p.y = Mathf.Lerp(p.y, _focus.y, k);
            transform.position = new Vector3(Mathf.Round(p.x * pixelsPerUnit) / pixelsPerUnit, Mathf.Round(p.y * pixelsPerUnit) / pixelsPerUnit, p.z);
        }
    }
}
