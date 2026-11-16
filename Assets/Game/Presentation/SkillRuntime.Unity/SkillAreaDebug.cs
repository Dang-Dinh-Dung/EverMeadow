using System.Collections.Generic;
using UnityEngine;

namespace Evermeadow.Presentation.SkillRuntime.Unity
{
    /// <summary>
    /// Draws skill areas in the game view (dev aid, switched on from Evermeadow ▸ Project Settings): an arc (angle, radius) or a
    /// box (length along the facing, height) as a filled translucent shape with an outline, shown for exactly the node's
    /// active time (at least <see cref="MinSeconds"/> so an instant hit can be seen), optionally following a transform.
    /// Pooled meshes and line renderers; no allocation per area once warm. Single-thread only.
    /// </summary>
    /// <example>
    /// <code>
    /// areas.ShowArc(center, facing: 1, angleDegrees: 180, radius: 1.5f, seconds: 0.2f, SkillAreaDebug.HitColour, follow: owner);
    /// </code>
    /// </example>
    public sealed class SkillAreaDebug : MonoBehaviour
    {
        /// <summary>Shortest time an area stays on screen (an instant hit lasts 0 s).</summary>
        public const float MinSeconds = 0.1f;

        /// <summary>Colour of Hit areas (green, user request 2026-11-14).</summary>
        public static readonly Color HitColour = new Color(0.2f, 0.95f, 0.35f, 1f);

        /// <summary>Colour of Search areas.</summary>
        public static readonly Color SearchColour = new Color(1f, 0.85f, 0.2f, 1f);

        private const int ArcSegments = 32;
        private const float FillAlpha = 0.28f;
        private const int SortingOrder = 500;

        private sealed class Shape
        {
            public GameObject Root;
            public Mesh Mesh;
            public LineRenderer Line;
            public float Left;
            public Transform Follow;
            public Vector3 Offset;
            public readonly List<Vector3> Vertices = new List<Vector3>(ArcSegments + 2);
            public readonly List<Color> Colours = new List<Color>(ArcSegments + 2);
            public readonly List<int> Triangles = new List<int>(ArcSegments * 3);
        }

        private readonly List<Shape> _shapes = new List<Shape>();
        private Material _material;

        /// <summary>Filled arc centred on <paramref name="center"/>, opening toward <paramref name="facing"/> (±1).</summary>
        public void ShowArc(Vector3 center, int facing, float angleDegrees, float radius, float seconds, Color colour, Transform follow = null)
        {
            Shape s = Take(center, seconds, follow);
            float angle = Mathf.Clamp(angleDegrees, 0f, 360f) * Mathf.Deg2Rad;
            float baseAngle = facing >= 0 ? 0f : Mathf.PI;
            bool full = angleDegrees >= 359.9f;
            s.Vertices.Add(Vector3.zero);
            for (int k = 0; k <= ArcSegments; k++)
            {
                float a = baseAngle - angle / 2f + angle * k / ArcSegments;
                s.Vertices.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Mathf.Max(0f, radius));
            }
            for (int k = 1; k <= ArcSegments; k++)
            {
                s.Triangles.Add(0);
                s.Triangles.Add(k + 1);
                s.Triangles.Add(k);
            }
            // Outline: rim, plus the two straight edges when it is not a full circle.
            s.Line.positionCount = ArcSegments + 1 + (full ? 0 : 2);
            int i = 0;
            if (!full) s.Line.SetPosition(i++, Vector3.zero);
            for (int k = 1; k <= ArcSegments + 1; k++) s.Line.SetPosition(i++, s.Vertices[k]);
            if (!full) s.Line.SetPosition(i, Vector3.zero);
            s.Line.loop = full;
            Finish(s, colour);
        }

        /// <summary>Filled box from <paramref name="center"/> forward by <paramref name="length"/>, <paramref name="height"/> tall.</summary>
        public void ShowBox(Vector3 center, int facing, float length, float height, float seconds, Color colour, Transform follow = null)
        {
            Shape s = Take(center, seconds, follow);
            float dir = facing >= 0 ? 1f : -1f;
            float h = Mathf.Max(0f, height) / 2f, l = Mathf.Max(0f, length) * dir;
            s.Vertices.Add(new Vector3(0f, -h, 0f));
            s.Vertices.Add(new Vector3(l, -h, 0f));
            s.Vertices.Add(new Vector3(l, h, 0f));
            s.Vertices.Add(new Vector3(0f, h, 0f));
            if (dir > 0) s.Triangles.AddRange(new[] { 0, 2, 1, 0, 3, 2 });
            else s.Triangles.AddRange(new[] { 0, 1, 2, 0, 2, 3 });
            s.Line.positionCount = 4;
            for (int k = 0; k < 4; k++) s.Line.SetPosition(k, s.Vertices[k]);
            s.Line.loop = true;
            Finish(s, colour);
        }

        /// <summary>Hides every area now.</summary>
        public void Clear()
        {
            foreach (Shape s in _shapes)
            {
                s.Left = 0f;
                s.Root.SetActive(false);
            }
        }

        private Shape Take(Vector3 center, float seconds, Transform follow)
        {
            Shape free = null;
            foreach (Shape s in _shapes)
                if (s.Left <= 0f) { free = s; break; }
            if (free == null)
            {
                if (_material == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                    _material = new Material(shader);
                }
                var root = new GameObject("Skill area");
                root.transform.SetParent(transform, false);
                var mf = root.AddComponent<MeshFilter>();
                var mr = root.AddComponent<MeshRenderer>();
                mr.sharedMaterial = _material;
                mr.sortingOrder = SortingOrder;
                var mesh = new Mesh { name = "Skill area" };
                mesh.MarkDynamic();
                mf.sharedMesh = mesh;
                var outline = new GameObject("Outline");
                outline.transform.SetParent(root.transform, false);
                var line = outline.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.widthMultiplier = 0.05f;
                line.sharedMaterial = _material;
                line.sortingOrder = SortingOrder + 1;
                free = new Shape { Root = root, Mesh = mesh, Line = line };
                _shapes.Add(free);
            }
            free.Vertices.Clear();
            free.Colours.Clear();
            free.Triangles.Clear();
            free.Left = Mathf.Max(MinSeconds, seconds);
            free.Follow = follow;
            free.Offset = follow != null ? center - follow.position : Vector3.zero;
            free.Root.transform.position = center;
            free.Root.SetActive(true);
            return free;
        }

        private static void Finish(Shape s, Color colour)
        {
            var fill = new Color(colour.r, colour.g, colour.b, FillAlpha);
            for (int k = 0; k < s.Vertices.Count; k++) s.Colours.Add(fill);
            s.Mesh.Clear();
            s.Mesh.SetVertices(s.Vertices);
            s.Mesh.SetColors(s.Colours);
            s.Mesh.SetTriangles(s.Triangles, 0);
            s.Mesh.RecalculateBounds();
            s.Line.startColor = s.Line.endColor = new Color(colour.r, colour.g, colour.b, 0.95f);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            foreach (Shape s in _shapes)
            {
                if (s.Left <= 0f) continue;
                if (s.Follow != null) s.Root.transform.position = s.Follow.position + s.Offset;
                s.Left -= dt;
                if (s.Left <= 0f) s.Root.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            foreach (Shape s in _shapes)
                if (s.Mesh != null) Destroy(s.Mesh);
            if (_material != null) Destroy(_material);
        }
    }
}
