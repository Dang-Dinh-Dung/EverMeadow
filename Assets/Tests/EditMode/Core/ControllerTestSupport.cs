using System;
using System.Collections.Generic;
using Evermeadow.Core.CharacterController;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Time;
using Evermeadow.Tests.EditMode.Foundation;

namespace Evermeadow.Tests.EditMode.Core
{
    /// <summary>
    /// Fake collision world from analytic geometry (hook H-3): convex polygons, box casts by ray-casting the Minkowski sum with
    /// Cyrus–Beck clipping. Casts that start strictly inside a shape ignore it, like Physics2D with queriesStartInColliders off.
    /// </summary>
    public sealed class ConvexWorld : ICollisionWorld
    {
        private sealed class Shape
        {
            public Vec2[] Points; // CCW
            public bool OneWay;
            public int Id;
            public float Top;
        }

        private readonly List<Shape> _shapes = new List<Shape>();

        /// <summary>Adds a convex polygon given counter-clockwise.</summary>
        public int AddPolygon(bool oneWay, params Vec2[] ccw)
        {
            float top = float.MinValue;
            foreach (Vec2 p in ccw) top = Math.Max(top, p.Y);
            int id = _shapes.Count + 1;
            _shapes.Add(new Shape { Points = ccw, OneWay = oneWay, Id = id, Top = top });
            return id;
        }

        /// <summary>Axis-aligned box.</summary>
        public int AddBox(float minX, float minY, float maxX, float maxY, bool oneWay = false) =>
            AddPolygon(oneWay, new Vec2(minX, minY), new Vec2(maxX, minY), new Vec2(maxX, maxY), new Vec2(minX, maxY));

        public int Cast(Vec2 center, Vec2 size, Vec2 direction, float distance, CastHit[] results)
        {
            int w = 0;
            foreach (Shape s in _shapes)
            {
                if (w >= results.Length) break;
                Vec2[] hull = Minkowski(s.Points, size);
                if (!RayHull(hull, center, direction, out double t, out Vec2 n)) continue;
                if (t > distance) continue;
                results[w++] = new CastHit { Distance = (float)Math.Max(0.0, t), Normal = n, OneWay = s.OneWay, Top = s.Top, ColliderId = s.Id };
            }
            return w;
        }

        public bool ComputePenetration(Vec2 center, Vec2 size, out Vec2 push)
        {
            push = Vec2.Zero;
            double best = 0;
            foreach (Shape s in _shapes)
            {
                if (s.OneWay) continue;
                Vec2[] hull = Minkowski(s.Points, size);
                double minDepth = double.MaxValue;
                Vec2 minN = Vec2.Zero;
                bool inside = true;
                for (int i = 0; i < hull.Length; i++)
                {
                    Vec2 a = hull[i], b = hull[(i + 1) % hull.Length];
                    Vec2 n = Normal(a, b);
                    double depth = n.X * (a.X - center.X) + n.Y * (a.Y - center.Y);
                    if (depth <= 1e-7) { inside = false; break; }
                    if (depth < minDepth) { minDepth = depth; minN = n; }
                }
                if (!inside || minDepth <= best) continue;
                best = minDepth;
                push = new Vec2((float)(minN.X * minDepth), (float)(minN.Y * minDepth));
            }
            return best > 0;
        }

        private static Vec2 Normal(Vec2 a, Vec2 b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, len = Math.Sqrt(dx * dx + dy * dy);
            return new Vec2((float)(dy / len), (float)(-dx / len));
        }

        private static bool RayHull(Vec2[] hull, Vec2 o, Vec2 d, out double tEnter, out Vec2 normal)
        {
            tEnter = double.NegativeInfinity;
            double tExit = double.PositiveInfinity;
            normal = Vec2.Zero;
            for (int i = 0; i < hull.Length; i++)
            {
                Vec2 a = hull[i], b = hull[(i + 1) % hull.Length];
                Vec2 n = Normal(a, b);
                double num = n.X * (a.X - o.X) + n.Y * (a.Y - o.Y); // > 0 when o is inside of this edge
                double den = n.X * d.X + n.Y * d.Y;
                if (Math.Abs(den) < 1e-12)
                {
                    if (num < 0) return false;
                    continue;
                }
                double t = num / den;
                if (den < 0)
                {
                    if (t > tEnter) { tEnter = t; normal = n; }
                }
                else if (t < tExit) tExit = t;
            }
            if (tEnter > tExit || tExit < 0) return false;
            return tEnter >= -1e-6; // started strictly inside → ignored
        }

        private static Vec2[] Minkowski(Vec2[] poly, Vec2 size)
        {
            float hx = size.X * 0.5f, hy = size.Y * 0.5f;
            var pts = new List<Vec2>(poly.Length * 4);
            foreach (Vec2 p in poly)
            {
                pts.Add(new Vec2(p.X - hx, p.Y - hy));
                pts.Add(new Vec2(p.X + hx, p.Y - hy));
                pts.Add(new Vec2(p.X + hx, p.Y + hy));
                pts.Add(new Vec2(p.X - hx, p.Y + hy));
            }
            return Hull(pts);
        }

        // Andrew's monotone chain, counter-clockwise, collinear points removed.
        private static Vec2[] Hull(List<Vec2> pts)
        {
            pts.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
            var h = new List<Vec2>();
            for (int pass = 0; pass < 2; pass++)
            {
                int start = h.Count;
                for (int i = 0; i < pts.Count; i++)
                {
                    Vec2 p = pass == 0 ? pts[i] : pts[pts.Count - 1 - i];
                    while (h.Count >= start + 2 && Cross(h[h.Count - 2], h[h.Count - 1], p) <= 1e-9) h.RemoveAt(h.Count - 1);
                    h.Add(p);
                }
                h.RemoveAt(h.Count - 1);
            }
            return h.ToArray();
        }

        private static double Cross(Vec2 o, Vec2 a, Vec2 b) => (a.X - o.X) * (double)(b.Y - o.Y) - (a.Y - o.Y) * (double)(b.X - o.X);
    }

    /// <summary>Runs the real Input core and the controller frame by frame on an injected clock.</summary>
    public sealed class ControllerSim
    {
        public const long Frame = 16_667;
        public readonly FakeClock Clock = new FakeClock();
        public readonly RecordingLogSink Log = new RecordingLogSink();
        public readonly InputService Input;
        public readonly ConvexWorld World;
        public PlayerController Ctl;
        public readonly List<string> Events = new List<string>();

        public ControllerSim(ConvexWorld world)
        {
            World = world;
            Input = new InputService(Clock, new InputConfig(), Log, false);
        }

        public static float HalfHeight => new ControllerConfig().ColliderHeight * 0.5f;

        /// <summary>Flat floor from x −100 to 100 with its top at y = 0.</summary>
        public static ConvexWorld Floor()
        {
            var w = new ConvexWorld();
            w.AddBox(-100f, -1f, 100f, 0f);
            return w;
        }

        public ControllerSim Spawn(float x, float feetY, ControllerConfig config = null)
        {
            Ctl = new PlayerController(config ?? new ControllerConfig(), Input, World, Log,
                new Vec2(x, feetY + HalfHeight + 0.05f));
            Ctl.Jumped += k => Events.Add("Jumped:" + k);
            Ctl.Landed += _ => Events.Add("Landed");
            Ctl.LeftGround += () => Events.Add("LeftGround");
            Ctl.DodgeStarted += (d, air) => Events.Add(air ? "DodgeAir" : "DodgeGround");
            Ctl.DodgeEnded += () => Events.Add("DodgeEnded");
            Ctl.Bonked += () => Events.Add("Bonked");
            Ctl.ControlLockApplied += () => Events.Add("LockApplied");
            Ctl.FellOutOfWorld += () => Events.Add("FellOut");
            Ctl.IFrameEnded += () => Events.Add("IFrameEnded");
            return this;
        }

        public float Feet => Ctl.Position.Y - HalfHeight;

        /// <summary>One frame: clock, input step with <paramref name="inputEvents"/>, controller step.</summary>
        public void Step(Action<InputService> inputEvents = null, long dt = Frame)
        {
            Clock.Advance(dt);
            Input.BeginStep();
            inputEvents?.Invoke(Input);
            Input.EndStep();
            Ctl.Step(dt);
        }

        public void Run(int frames, Action<InputService> everyFrame = null)
        {
            for (int i = 0; i < frames; i++) Step(everyFrame);
        }

        public void Hold(Vec2 move) => Step(i => i.OnAxisSample(AxisId.Move, move, DeviceKind.KeyboardMouse));

        public int Count(string evt) => Events.FindAll(e => e == evt).Count;
    }
}
