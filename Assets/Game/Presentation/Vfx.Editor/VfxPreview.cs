using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Evermeadow.Presentation.Vfx.Editor
{
    /// <summary>
    /// Renders one effect prefab in an isolated preview scene at a chosen time (<see cref="SkillVfx.SetTime"/>), with an
    /// orthographic camera framed on the effect. Used by the VFX picker; dispose it to free the preview scene.
    /// </summary>
    /// <example>
    /// <code>
    /// var preview = new VfxPreview();
    /// preview.Load(prefab);
    /// preview.Draw(rect, time: 0.2f, scale: 1f, background: Color.black);
    /// preview.Dispose();
    /// </code>
    /// </example>
    public sealed class VfxPreview : IDisposable
    {
        private PreviewRenderUtility _pru;
        private GameObject _instance;
        private SkillVfx _fx;
        private float _framedSize = 2f;

        /// <summary>Lifetime of the loaded effect (∞ when it loops), for the time slider.</summary>
        public double Lifetime { get; private set; }

        /// <summary>The loaded effect component, or null.</summary>
        public SkillVfx Effect => _fx;

        /// <summary>Shows <paramref name="prefab"/> (null clears).</summary>
        public void Load(SkillVfx prefab)
        {
            Ensure();
            if (_instance != null) Object.DestroyImmediate(_instance);
            _instance = null;
            _fx = null;
            Lifetime = 0;
            if (prefab == null) return;
            _instance = _pru.InstantiatePrefabInScene(prefab.gameObject);
            _instance.transform.position = Vector3.zero;
            _fx = _instance.GetComponent<SkillVfx>();
            Lifetime = _fx != null ? _fx.LifetimeFor(0f) : 0;
            Frame();
        }

        /// <summary>Draws the effect at <paramref name="time"/> seconds into <paramref name="rect"/>.</summary>
        public void Draw(Rect rect, float time, float scale, Color background)
        {
            if (Event.current.type != EventType.Repaint || rect.width < 4 || rect.height < 4) return;
            Ensure();
            if (_instance != null)
            {
                _instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
                _fx?.SetTime(time);
            }
            Camera cam = _pru.camera;
            cam.backgroundColor = background;
            cam.orthographicSize = _framedSize * Mathf.Max(0.01f, scale);
            _pru.BeginPreview(rect, GUIStyle.none);
            _pru.Render(true);
            Texture tex = _pru.EndPreview();
            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_instance != null) Object.DestroyImmediate(_instance);
            _instance = null;
            _pru?.Cleanup();
            _pru = null;
        }

        private void Ensure()
        {
            if (_pru != null) return;
            _pru = new PreviewRenderUtility();
            Camera cam = _pru.camera;
            cam.orthographic = true;
            cam.orthographicSize = 2f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
        }

        // Frames the camera on the effect's renderers over its first second (particles spread after the start).
        private void Frame()
        {
            if (_fx == null) return;
            var bounds = new Bounds(Vector3.zero, Vector3.one * 0.5f);
            foreach (float t in new[] { 0.05f, 0.2f, 0.5f, 1f })
            {
                _fx.SetTime(t);
                foreach (Renderer r in _instance.GetComponentsInChildren<Renderer>(true))
                    if (r.bounds.size.sqrMagnitude > 0) bounds.Encapsulate(r.bounds);
            }
            _framedSize = Mathf.Max(0.5f, Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.25f);
            _pru.camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
        }
    }
}
