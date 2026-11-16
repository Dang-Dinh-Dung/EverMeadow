// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do the farm half and the outing half still feed each other when the world is a set of free, daily-regenerated biome maps joined by mid-map branches you must unlock?
// Date: 2026-10-30
//
// The seeded half of the world. Generate() turns (MapDef, world seed, day)
// into a MapLayout -- pure data; exploration maps get a new layout every day,
// villages keep one per world seed. Build() turns a layout into GameObjects
// under one root, destroyed when the player leaves.
//
// Pipeline: seed -> length -> end barriers (cliff / river / lake / sea) ->
// sub-ecology zones -> terrain -> ponds -> branches (mid-map side entrances) ->
// hand-made landmarks at seeded spots (terrain flattened under them) -> boss ->
// cave ceiling -> platforms -> ore -> enemies -> trees.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DayLoopProto
{
    public class MapLayout
    {
        public const float Step = 0.5f;
        public const float Bottom = -14f;

        public class Placed { public LandmarkDef def; public float x, y; public int index; }
        public class Water { public WaterKind kind; public float x0, x1, surface, bottom; public bool barrier; }
        public class Zone { public ZoneDef def; public float x0, x1; }

        public MapDef def;
        public int day;
        public float width, playMin, playMax;
        public EndKind leftEnd, rightEnd;
        public float[] floor;
        public float[] ceil;
        public float bossX = float.NaN;
        public readonly List<Zone> zones = new List<Zone>();
        public readonly List<Water> waters = new List<Water>();
        public readonly List<(float x, WaterKind kind)> fishingSpots = new List<(float, WaterKind)>();
        public readonly List<float> invisibleWalls = new List<float>();
        public readonly List<(EdgeDef edge, float x)> branches = new List<(EdgeDef, float)>();
        public readonly List<Placed> landmarks = new List<Placed>();
        public readonly List<Rect> platforms = new List<Rect>();
        public readonly List<Vector2> ores = new List<Vector2>();
        public readonly List<(EnemyDef def, float x)> enemies = new List<(EnemyDef, float)>();
        public readonly List<(TreeKind kind, float x, float h)> trees = new List<(TreeKind, float, float)>();
        public readonly List<(Building b, float x, float w)> buildings = new List<(Building, float, float)>();

        public float GroundY(float x) => Sample(floor, x);
        public float CeilY(float x) => ceil == null ? 99f : Sample(ceil, x);

        public Zone ZoneAt(float x)
        {
            foreach (var z in zones) if (x >= z.x0 && x < z.x1) return z;
            return x < playMin ? zones[0] : zones[zones.Count - 1];
        }

        static float Sample(float[] a, float x)
        {
            float t = Mathf.Clamp(x / Step, 0f, a.Length - 1.001f);
            int i = Mathf.FloorToInt(t);
            return Mathf.Lerp(a[i], a[i + 1], t - i);
        }
    }

    public static class MapGen
    {
        static readonly Dictionary<Building, float> BuildingWidth = new Dictionary<Building, float>
        {
            { Building.House, 4f }, { Building.Inn, 4f }, { Building.Cauldron, 2f }, { Building.Forge, 3f },
            { Building.Farm, 8f }, { Building.Market, 3f }, { Building.Transporter, 2f },
        };

        const float BranchWidth = 3f;

        static float EndWidth(EndKind k)
        {
            switch (k)
            {
                case EndKind.River: return 14f;
                case EndKind.Lake: return 16f;
                case EndKind.Sea: return 22f;
                case EndKind.UndergroundLake: return 14f;
                default: return 6f;   // Cliff, RockWall
            }
        }

        static WaterKind EndWater(EndKind k) =>
            k == EndKind.River ? WaterKind.River : k == EndKind.Sea ? WaterKind.Sea : k == EndKind.UndergroundLake ? WaterKind.CavePool : WaterKind.Lake;

        static bool IsWater(EndKind k) => k != EndKind.Cliff && k != EndKind.RockWall;

        public static MapLayout Generate(MapDef def, int worldSeed, int day)
        {
            var r = new System.Random(Noise.Seed(def.RerollsDaily ? $"{def.id}#d{day}" : def.id, worldSeed));
            float S = MapLayout.Step;
            float W = Mathf.Round(Noise.Range(r, def.minWidth, def.maxWidth) / S) * S;
            int n = Mathf.RoundToInt(W / S) + 1;
            var L = new MapLayout { def = def, day = day, width = W };

            L.leftEnd = def.leftEnds[r.Next(def.leftEnds.Length)];
            L.rightEnd = def.rightEnds[r.Next(def.rightEnds.Length)];
            L.playMin = EndWidth(L.leftEnd);
            L.playMax = W - EndWidth(L.rightEnd);

            BuildZones(L, r);

            // Terrain: one fbm, roughness and base height follow the zone under each sample (smoothed across zone borders).
            var f = Noise.Fbm(r, n, S);
            var amp = new float[n];
            var bas = new float[n];
            for (int i = 0; i < n; i++) { var z = L.ZoneAt(i * S).def; amp[i] = z.amp; bas[i] = z.baseOffset; }
            amp = Smooth(amp, Mathf.RoundToInt(8f / S));
            bas = Smooth(bas, Mathf.RoundToInt(8f / S));
            L.floor = new float[n];
            for (int i = 0; i < n; i++) L.floor[i] = bas[i] + (f[i] - 0.5f) * 2f * amp[i];

            var reserved = new List<Vector2>();
            bool Free(float a, float b)
            {
                if (a < L.playMin + 2f || b > L.playMax - 2f) return false;
                foreach (var v in reserved) if (a < v.y && b > v.x) return false;
                return true;
            }

            CarveEnd(L, true);
            CarveEnd(L, false);
            reserved.Add(new Vector2(0f, L.playMin + 5f));
            reserved.Add(new Vector2(L.playMax - 5f, W));

            // Ponds in the middle of pond zones: shallow, walkable, fishable.
            foreach (var z in L.zones.Where(z => z.def.pond))
            {
                float c = (z.x0 + z.x1) / 2f, pw = Noise.Range(r, 6f, 9f), a = c - pw / 2f, b = c + pw / 2f;
                Flatten(L, a - 3f, b + 3f);
                float shore = L.GroundY(c);
                int i0 = Mathf.CeilToInt(a / S), i1 = Mathf.FloorToInt(b / S);
                for (int i = i0; i <= i1; i++) L.floor[i] = shore - 1.4f * Mathf.Sin(Mathf.PI * (i * S - a) / pw);
                L.waters.Add(new MapLayout.Water { kind = def.pondWater, x0 = a, x1 = b, surface = shore - 0.15f, bottom = shore - 1.5f });
                L.fishingSpots.Add((a - 1.3f, def.pondWater));
                reserved.Add(new Vector2(a - 4f, b + 4f));
            }

            if (def.kind == MapKind.Village) PlaceVillage(L, r, reserved);
            else
            {
                // Branches: side entrances somewhere mid-map. A boss-guarded one gets an arena around it.
                foreach (var e in World.EdgesOf(def.id))
                {
                    bool guarded = def.bossGuardsEdge == e.Id;
                    float pad = guarded ? 11f : 4f;
                    for (int t = 0; t < 300; t++)
                    {
                        float x = Noise.Range(r, L.playMin + pad + 2f, L.playMax - pad - 2f);
                        if (!Free(x - pad, x + pad)) continue;
                        Flatten(L, x - pad + 1f, x + pad - 1f);
                        L.branches.Add((e, x));
                        reserved.Add(new Vector2(x - pad, x + pad));
                        if (guarded) L.bossX = x + 5f;
                        break;
                    }
                }

                // Hand-made landmarks at seeded positions.
                for (int li = 0; li < def.landmarks.Length; li++)
                {
                    var lm = def.landmarks[li];
                    bool guarded = def.bossGuardsLandmark == li;
                    float pad = guarded ? 10f : 2f;
                    for (int t = 0; t < 300; t++)
                    {
                        float x = Noise.Range(r, L.playMin + pad + 2f, L.playMax - lm.width - pad - 2f);
                        if (!Free(x - pad, x + lm.width + pad)) continue;
                        Flatten(L, x - pad + 1f, x + lm.width + pad - 1f);
                        L.landmarks.Add(new MapLayout.Placed { def = lm, x = x, y = L.GroundY(x), index = li });
                        reserved.Add(new Vector2(x - pad, x + lm.width + pad));
                        if (guarded) L.bossX = x + lm.width + 4f;
                        break;
                    }
                }

                // Optional boss: an arena of its own somewhere free.
                if (def.boss != null && float.IsNaN(L.bossX))
                {
                    for (int t = 0; t < 300; t++)
                    {
                        float x = Noise.Range(r, L.playMin + 12f, L.playMax - 12f);
                        if (!Free(x - 10f, x + 10f)) continue;
                        Flatten(L, x - 9f, x + 9f);
                        L.bossX = x;
                        reserved.Add(new Vector2(x - 10f, x + 10f));
                        break;
                    }
                }
            }

            if (def.kind == MapKind.Cave) BuildCeiling(L, r, n);

            float playLen = L.playMax - L.playMin;

            // One-way platforms, only in zones that want them.
            int platCount = Mathf.RoundToInt(def.platformsPer100 * playLen / 100f);
            for (int k = 0, t = 0; k < platCount && t < 300; t++)
            {
                float w = Noise.Range(r, 2.5f, 4f), x = Noise.Range(r, L.playMin + 4f, L.playMax - w - 4f);
                if (!L.ZoneAt(x).def.platforms || !Free(x - 1f, x + w + 1f)) continue;
                float y = Mathf.Max(L.GroundY(x), L.GroundY(x + w / 2f), L.GroundY(x + w)) + 2.1f;
                if (y > L.CeilY(x) - 1.5f) continue;
                L.platforms.Add(Rect.MinMaxRect(x, y - 0.3f, x + w, y));
                reserved.Add(new Vector2(x - 1f, x + w + 1f));
                k++;
            }

            // Ore nodes.
            int oreCount = def.kind == MapKind.Village ? 0 : Mathf.Max(1, Mathf.RoundToInt(def.orePer100 * playLen / 100f));
            for (int k = 0, t = 0; k < oreCount && t < 300; t++)
            {
                float x = Noise.Range(r, L.playMin + 4f, L.playMax - 4f);
                if (!Free(x - 1.5f, x + 1.5f)) continue;
                L.ores.Add(new Vector2(x, L.GroundY(x)));
                reserved.Add(new Vector2(x - 1.5f, x + 1.5f));
                k++;
            }

            // Enemies, mix chosen by the zone they stand in; never on top of a branch or in a boss arena.
            int enemyCount = Mathf.RoundToInt(def.enemiesPer100 * playLen / 100f);
            var taken = new List<float>();
            for (int k = 0, t = 0; k < enemyCount && t < 600; t++)
            {
                float x = Noise.Range(r, L.playMin + 4f, L.playMax - 4f);
                bool ok = true;
                foreach (var b in L.branches) if (Mathf.Abs(x - b.x) < 8f) ok = false;
                if (!float.IsNaN(L.bossX) && Mathf.Abs(x - L.bossX) < 13f) ok = false;
                foreach (var w in L.waters) if (x > w.x0 - 1f && x < w.x1 + 1f) ok = false;
                foreach (var o in taken) if (Mathf.Abs(x - o) < 5f) ok = false;
                if (!ok) continue;
                var weights = L.ZoneAt(x).def.enemyWeights;
                L.enemies.Add((def.common[Weighted(r, weights)], x));
                taken.Add(x);
                k++;
            }

            // Trees, by zone, kept out of everything placed.
            float tx = L.playMin + 1f;
            while (tx < L.playMax - 1f)
            {
                var z = L.ZoneAt(tx).def;
                bool inside = false;
                foreach (var b in L.buildings) if (tx > b.x - 1f && tx < b.x + b.w + 1f) inside = true;
                foreach (var p in L.landmarks) if (tx > p.x - 1f && tx < p.x + p.def.width + 1f) inside = true;
                foreach (var b in L.branches) if (Mathf.Abs(tx - b.x) < 2.5f) inside = true;
                foreach (var w in L.waters) if (tx > w.x0 - 1f && tx < w.x1 + 1f) inside = true;
                var kind = z.tree != TreeKind.None ? z.tree : def.tree;
                if (!inside && kind != TreeKind.None) L.trees.Add((kind, tx, Noise.Range(r, 2.5f, 5f)));
                tx += Noise.Range(r, z.treeMin, z.treeMax);
            }

            return L;
        }

        static void BuildZones(MapLayout L, System.Random r)
        {
            var def = L.def;
            if (def.kind == MapKind.Village || def.zones.Length == 1)
            {
                L.zones.Add(new MapLayout.Zone { def = def.zones[0], x0 = L.playMin, x1 = L.playMax });
                return;
            }
            float x = L.playMin;
            ZoneDef prev = null;
            while (x < L.playMax - 0.01f)
            {
                float len = Noise.Range(r, 25f, 50f);
                if (L.playMax - (x + len) < 18f) len = L.playMax - x;
                ZoneDef z;
                int guard = 0;
                do z = def.zones[Weighted(r, def.zones.Select(q => q.weight).ToArray())];
                while (z == prev && ++guard < 10);   // no two identical zones in a row
                L.zones.Add(new MapLayout.Zone { def = z, x0 = x, x1 = x + len });
                prev = z;
                x += len;
            }
            if (def.forcedZone != null && L.zones.All(q => q.def.name != def.forcedZone))
            {
                var forced = def.zones.First(q => q.name == def.forcedZone);
                L.zones[r.Next(L.zones.Count)].def = forced;
            }
        }

        // Cliff / rock wall: ground climbs far above jump height. Water: the ground
        // drops into a basin; an invisible wall at the shore keeps the player on
        // land and a fishing spot sits next to it. A river has a far bank you can see but not reach.
        static void CarveEnd(MapLayout L, bool left)
        {
            float S = MapLayout.Step;
            var kind = left ? L.leftEnd : L.rightEnd;
            float sx = left ? L.playMin : L.playMax;
            if (left) Flatten(L, sx, sx + 4f); else Flatten(L, sx - 4f, sx);
            float shoreY = L.GroundY(sx);
            int n = L.floor.Length;

            for (int i = 0; i < n; i++)
            {
                float x = i * S, d = left ? sx - x : x - sx;
                if (d <= 0f) continue;
                if (!IsWater(kind)) L.floor[i] = shoreY + Mathf.Min(14f, d * 6f);
                else if (kind == EndKind.River && d > 8f) L.floor[i] = shoreY - 3f + Mathf.Min(3.5f, (d - 8f) * 1.5f);
                else L.floor[i] = shoreY - Mathf.Min(3f, d * 1.5f);
            }

            if (!IsWater(kind)) return;
            float far = kind == EndKind.River ? 10f : EndWidth(kind);
            L.waters.Add(new MapLayout.Water
            {
                kind = EndWater(kind), barrier = true, surface = shoreY - 0.25f, bottom = shoreY - 3.2f,
                x0 = left ? sx - far : sx, x1 = left ? sx : sx + far,
            });
            L.invisibleWalls.Add(left ? sx - 0.2f : sx + 0.2f);
            L.fishingSpots.Add((left ? sx + 1.3f : sx - 1.3f, EndWater(kind)));
        }

        static void PlaceVillage(MapLayout L, System.Random r, List<Vector2> reserved)
        {
            var def = L.def;
            // Hand-made buildings and the village's branches, in a seeded order with seeded gaps.
            var items = def.buildings.Select(b => ((object)b, BuildingWidth[b])).ToList();
            foreach (var e in World.EdgesOf(def.id)) items.Add(((object)e, BranchWidth));
            for (int i = items.Count - 1; i > 0; i--) { int j = r.Next(i + 1); (items[i], items[j]) = (items[j], items[i]); }

            float x = L.playMin + 5f;
            foreach (var (item, w) in items)
            {
                if (x + w > L.playMax - 4f) { Debug.LogWarning($"[DayLoop] {def.id}: no room for {item}"); continue; }
                if (item is Building b) L.buildings.Add((b, x, w));
                else L.branches.Add(((EdgeDef)item, x + w / 2f));
                reserved.Add(new Vector2(x - 0.5f, x + w + 0.5f));
                x += w + Noise.Range(r, 2f, 4f);
            }
        }

        static void BuildCeiling(MapLayout L, System.Random r, int n)
        {
            float S = MapLayout.Step;
            var f2 = Noise.Fbm(r, n, S);
            var gap = new float[n];
            for (int i = 0; i < n; i++) gap[i] = L.ZoneAt(i * S).def.ceilingGap;
            gap = Smooth(gap, Mathf.RoundToInt(8f / S));
            L.ceil = new float[n];
            for (int i = 0; i < n; i++) L.ceil[i] = L.floor[i] + gap[i] + f2[i] * 3f;

            foreach (var p in L.landmarks) RaiseCeiling(L, p.x - 1f, p.x + p.def.width + 1f, 10f);
            foreach (var b in L.branches) RaiseCeiling(L, b.x - 2f, b.x + 2f, 6f);
            if (!float.IsNaN(L.bossX)) RaiseCeiling(L, L.bossX - 10f, L.bossX + 10f, 8f);

            // Over end barriers: water keeps headroom above the shore, rock walls close up.
            for (int i = 0; i < n; i++)
            {
                float x = i * S;
                if (x < L.playMin || x > L.playMax)
                {
                    bool leftSide = x < L.playMin;
                    var kind = leftSide ? L.leftEnd : L.rightEnd;
                    float shore = L.GroundY(leftSide ? L.playMin : L.playMax);
                    L.ceil[i] = IsWater(kind) ? shore + 7f : L.floor[i] + 0.5f;
                }
                L.ceil[i] = Mathf.Max(L.ceil[i], L.floor[i] + 0.5f);
            }
        }

        static float[] Smooth(float[] a, int radius)
        {
            var o = new float[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                float s = 0f; int c = 0;
                for (int k = Mathf.Max(0, i - radius); k <= Mathf.Min(a.Length - 1, i + radius); k++) { s += a[k]; c++; }
                o[i] = s / c;
            }
            return o;
        }

        static int Weighted(System.Random r, float[] w)
        {
            float total = w.Sum(), roll = (float)r.NextDouble() * total;
            for (int i = 0; i < w.Length; i++) { roll -= w[i]; if (roll <= 0f) return i; }
            return w.Length - 1;
        }

        static void Flatten(MapLayout L, float a, float b)
        {
            float S = MapLayout.Step;
            int n = L.floor.Length;
            int i0 = Mathf.Clamp(Mathf.FloorToInt(a / S), 0, n - 1), i1 = Mathf.Clamp(Mathf.CeilToInt(b / S), 0, n - 1);
            float mean = 0f;
            for (int i = i0; i <= i1; i++) mean += L.floor[i];
            mean /= (i1 - i0 + 1);
            for (int i = i0; i <= i1; i++) L.floor[i] = mean;

            // Blend 3 units on each side so the flat pad meets the hills smoothly.
            int blend = Mathf.RoundToInt(3f / S);
            for (int k = 1; k <= blend; k++)
            {
                float t = k / (float)(blend + 1);
                if (i0 - k >= 0) L.floor[i0 - k] = Mathf.Lerp(mean, L.floor[i0 - k], t);
                if (i1 + k < n) L.floor[i1 + k] = Mathf.Lerp(mean, L.floor[i1 + k], t);
            }
        }

        static void RaiseCeiling(MapLayout L, float a, float b, float clearance)
        {
            float S = MapLayout.Step;
            int n = L.ceil.Length;
            int i0 = Mathf.Clamp(Mathf.FloorToInt((a - 3f) / S), 0, n - 1), i1 = Mathf.Clamp(Mathf.CeilToInt((b + 3f) / S), 0, n - 1);
            for (int i = i0; i <= i1; i++) L.ceil[i] = Mathf.Max(L.ceil[i], L.floor[i] + clearance);
        }

        // ---------------------------------------------------------------------
        static Color WaterColor(WaterKind k)
        {
            switch (k)
            {
                case WaterKind.River: return new Color(0.29f, 0.64f, 0.88f, 0.75f);
                case WaterKind.Sea: return new Color(0.18f, 0.44f, 0.72f, 0.8f);
                case WaterKind.Oasis: return new Color(0.29f, 0.78f, 0.82f, 0.75f);
                case WaterKind.CavePool: return new Color(0.16f, 0.35f, 0.55f, 0.8f);
                default: return new Color(0.23f, 0.53f, 0.79f, 0.75f);
            }
        }

        public static string WaterName(WaterKind k)
        {
            switch (k)
            {
                case WaterKind.River: return "sông";
                case WaterKind.Sea: return "biển";
                case WaterKind.Oasis: return "ốc đảo";
                case WaterKind.CavePool: return "hồ ngầm";
                default: return "hồ";
            }
        }

        public static GameObject Build(MapLayout L, DayLoopGame game)
        {
            var def = L.def;
            var root = new GameObject($"Map_{def.id}_d{L.day}").transform;
            float W = L.width, S = MapLayout.Step, B = MapLayout.Bottom;
            int n = L.floor.Length;
            Color edgeColor = def.body * 0.6f; edgeColor.a = 1f;

            Visuals.Box("Sky", new Vector2(W / 2f, 5f), new Vector2(W + 60f, 70f), def.sky, -20, root);

            // Terrain: thin columns (visual) + one edge collider following the samples (physics).
            for (int i = 0; i < n - 1; i++)
            {
                float x0 = i * S, top = (L.floor[i] + L.floor[i + 1]) / 2f;
                Visuals.Rect("Ground", x0, B, x0 + S, top - 0.3f, def.body, 0, root);
                Visuals.Rect("GroundTop", x0, top - 0.3f, x0 + S, top, def.top, 0, root);
            }
            Visuals.Rect("WallL", -30f, B, 0f, 40f, edgeColor, 0, root);
            Visuals.Rect("WallR", W, B, W + 30f, 40f, edgeColor, 0, root);

            var pts = new List<Vector2> { new Vector2(0f, 40f) };
            for (int i = 0; i < n; i++) pts.Add(new Vector2(i * S, L.floor[i]));
            pts.Add(new Vector2(W, 40f));
            root.gameObject.AddComponent<EdgeCollider2D>().points = pts.ToArray();

            if (L.ceil != null)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    float x0 = i * S, c = (L.ceil[i] + L.ceil[i + 1]) / 2f;
                    Visuals.Rect("Ceiling", x0, c, x0 + S, 40f, def.body * 0.8f, 0, root);
                }
                var cp = new Vector2[n];
                for (int i = 0; i < n; i++) cp[i] = new Vector2(i * S, L.ceil[i]);
                root.gameObject.AddComponent<EdgeCollider2D>().points = cp;
            }

            foreach (var w in L.waters)
                Visuals.Rect("Water", w.x0, w.bottom, w.x1, w.surface, WaterColor(w.kind), 5, root);
            foreach (float x in L.invisibleWalls)
            {
                var wall = new GameObject("ShoreWall");
                wall.transform.SetParent(root, false);
                wall.transform.position = new Vector3(x, L.GroundY(x) + 10f, 0f);
                wall.AddComponent<BoxCollider2D>().size = new Vector2(0.4f, 30f);
            }

            foreach (var t in L.trees) BuildTree(root, t.kind, t.x, L.GroundY(t.x), t.h, def);
            foreach (var p in L.platforms) Platform(root, p, def.body * 0.9f);

            // Landmarks: hand-made parts, offset to their seeded position.
            foreach (var lm in L.landmarks)
            {
                for (int pi = 0; pi < lm.def.parts.Count; pi++)
                {
                    var part = lm.def.parts[pi];
                    var r = new Rect(part.r.x + lm.x, part.r.y + lm.y, part.r.width, part.r.height);
                    string key = $"{def.id}#d{L.day}#{lm.index}#{pi}";
                    switch (part.type)
                    {
                        case Part.T.Solid:
                            Visuals.Rect(lm.def.name, r.xMin, r.yMin, r.xMax, r.yMax, part.color, 1, root).AddComponent<BoxCollider2D>().size = Vector2.one;
                            break;
                        case Part.T.Platform:
                            Platform(root, r, part.color);
                            break;
                        case Part.T.Deco:
                            Visuals.Rect(lm.def.name, r.xMin, r.yMin, r.xMax, r.yMax, part.color, -2, root);
                            break;
                        case Part.T.Chest:
                            var chest = Station<Chest>(game, root, "Rương", r.center.x, r.yMin, r.size, new Color(0.94f, 0.75f, 0.25f));
                            chest.Init(key, part.loot, lm.def.name);
                            break;
                        case Part.T.Ore:
                            Ore(game, root, key, new Vector2(r.center.x, r.yMin), part.oreYield);
                            break;
                    }
                }
            }

            for (int i = 0; i < L.ores.Count; i++) Ore(game, root, $"{def.id}@ore{i}", L.ores[i], def.oreYield);

            for (int i = 0; i < L.enemies.Count; i++)
            {
                string key = $"{def.id}@e{i}";
                if (game.KilledToday.Contains(key)) continue;
                Enemy(game, root, L, L.enemies[i].def, L.enemies[i].x, key);
            }
            if (def.boss != null && !float.IsNaN(L.bossX) && !game.BossDefeated.Contains(def.id))
                Enemy(game, root, L, def.boss, L.bossX, def.id);

            foreach (var (x, kind) in L.fishingSpots)
            {
                var spot = Station<FishingSpot>(game, root, $"Chỗ câu cá ({WaterName(kind)})", x, L.GroundY(x), new Vector2(0.25f, 1.3f), new Color(0.5f, 0.35f, 0.2f));
                spot.water = kind;
            }

            foreach (var (edge, x) in L.branches)
            {
                float g = L.GroundY(x);
                Visuals.Rect("BranchPillar", x - 1.3f, g, x - 0.9f, g + 3.2f, new Color(0.45f, 0.42f, 0.4f), -1, root);
                Visuals.Rect("BranchPillar", x + 0.9f, g, x + 1.3f, g + 3.2f, new Color(0.45f, 0.42f, 0.4f), -1, root);
                Visuals.Rect("BranchLintel", x - 1.4f, g + 3.2f, x + 1.4f, g + 3.6f, new Color(0.45f, 0.42f, 0.4f), -1, root);
                var gate = Station<BranchGate>(game, root, "Ngã rẽ", x, g, new Vector2(1.8f, 3.1f), Color.black);
                gate.Init(edge);
                if (edge.unlock == Unlock.CrackedWall)
                {
                    var col = gate.gameObject.AddComponent<BoxCollider2D>();
                    col.size = Vector2.one;
                    col.isTrigger = true;
                    gate.gameObject.AddComponent<CrackedWall>().gate = gate;
                }
            }

            foreach (var (b, x, w) in L.buildings) BuildBuilding(game, root, L, b, x, w);

            return root.gameObject;
        }

        static void BuildBuilding(DayLoopGame game, Transform root, MapLayout L, Building b, float x, float w)
        {
            float cx = x + w / 2f, g = L.GroundY(cx);
            switch (b)
            {
                case Building.House:
                case Building.Inn:
                    Visuals.Rect("House", x, g, x + w, g + 2.6f, b == Building.Inn ? new Color(0.75f, 0.6f, 0.4f) : new Color(0.62f, 0.45f, 0.3f), -3, root);
                    Visuals.Rect("Roof", x - 0.3f, g + 2.6f, x + w + 0.3f, g + 3.3f, new Color(0.65f, 0.2f, 0.2f), -3, root);
                    var bed = Station<Bed>(game, root, b == Building.Inn ? "Quán trọ (giường)" : "Nhà (giường)", cx, g, new Vector2(1.8f, 0.6f), new Color(0.55f, 0.4f, 0.7f));
                    bed.isInn = b == Building.Inn;
                    break;
                case Building.Cauldron:
                    Station<Cauldron>(game, root, "Nồi nấu", cx, g, new Vector2(1.2f, 1f), new Color(0.3f, 0.3f, 0.35f));
                    break;
                case Building.Forge:
                    Station<Forge>(game, root, "Lò rèn", cx, g, new Vector2(1.6f, 1.4f), new Color(0.8f, 0.4f, 0.15f));
                    break;
                case Building.Farm:
                    for (int i = 0; i < 4; i++)
                    {
                        var plot = Station<Plot>(game, root, $"Ô đất {i + 1}", x + 1f + 2f * i, g, new Vector2(1.5f, 0.3f), new Color(0.45f, 0.3f, 0.18f));
                        plot.state = game.PlotStates[i];
                    }
                    break;
                case Building.Market:
                    Visuals.Rect("Awning", x, g + 1.8f, x + w, g + 2.2f, new Color(0.9f, 0.85f, 0.3f), -3, root);
                    Station<Market>(game, root, "Quầy chợ", cx, g, new Vector2(2.4f, 1f), new Color(0.55f, 0.4f, 0.25f));
                    break;
                case Building.Transporter:
                    Station<Transporter>(game, root, "Người vận chuyển", cx, g, new Vector2(0.8f, 1.6f), new Color(0.3f, 0.6f, 0.95f));
                    break;
            }
        }

        static T Station<T>(DayLoopGame game, Transform root, string name, float x, float groundY, Vector2 size, Color color) where T : Station
        {
            var go = Visuals.Box(name, new Vector2(x, groundY + size.y / 2f), size, color, 1, root);
            var s = go.AddComponent<T>();
            s.displayName = name;
            game.Stations.Add(s);
            return s;
        }

        static void Platform(Transform root, Rect r, Color c)
        {
            var go = Visuals.Rect("Platform", r.xMin, r.yMin, r.xMax, r.yMax, c, 1, root);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            col.usedByEffector = true;
            go.AddComponent<PlatformEffector2D>().useOneWay = true;
        }

        static void Ore(DayLoopGame game, Transform root, string key, Vector2 groundPos, int yield)
        {
            var go = Visuals.Box("OreNode", groundPos + new Vector2(0f, 0.5f), Vector2.one, Color.white, 2, root);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            col.isTrigger = true;
            var ore = go.AddComponent<OreNode>();
            ore.Init(key, yield, game.MinedToday.Contains(key));
            game.OreNodes.Add(ore);
        }

        static void Enemy(DayLoopGame game, Transform root, MapLayout L, EnemyDef def, float x, string key)
        {
            var go = Visuals.Box(def.name, new Vector2(x, L.GroundY(x) + def.size.y / 2f), def.size, def.color, 3, root);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            col.isTrigger = true;
            var e = go.AddComponent<DayLoopEnemy>();
            e.Configure(def, key, L);
            game.Enemies.Add(e);
        }

        static void BuildTree(Transform root, TreeKind kind, float x, float y, float h, MapDef def)
        {
            Color trunk = new Color(0.42f, 0.29f, 0.17f);
            Color leaf = def.top * 0.8f; leaf.a = 1f;
            switch (kind)
            {
                case TreeKind.Oak:
                    Visuals.Rect("Tree", x - 0.15f, y, x + 0.15f, y + h * 0.65f, trunk, -5, root);
                    Visuals.Rect("Tree", x - 1f, y + h * 0.5f, x + 1f, y + h, leaf, -5, root);
                    break;
                case TreeKind.Pine:
                    Visuals.Rect("Tree", x - 0.15f, y, x + 0.15f, y + h * 0.4f, trunk, -5, root);
                    Visuals.Rect("Tree", x - 0.8f, y + h * 0.3f, x + 0.8f, y + h * 0.75f, leaf, -5, root);
                    Visuals.Rect("Tree", x - 0.45f, y + h * 0.75f, x + 0.45f, y + h * 1.2f, leaf, -5, root);
                    break;
                case TreeKind.Palm:
                    Visuals.Rect("Palm", x - 0.15f, y, x + 0.15f, y + h, trunk, -5, root);
                    Visuals.Rect("Palm", x - 1.1f, y + h - 0.3f, x + 1.1f, y + h + 0.2f, new Color(0.3f, 0.65f, 0.3f), -5, root);
                    break;
                case TreeKind.Mushroom:
                    Visuals.Rect("Mushroom", x - 0.2f, y, x + 0.2f, y + h * 0.5f, new Color(0.95f, 0.9f, 0.85f), -5, root);
                    Visuals.Rect("Mushroom", x - 0.9f, y + h * 0.5f, x + 0.9f, y + h * 0.7f, new Color(0.85f, 0.35f, 0.6f), -5, root);
                    break;
                case TreeKind.Cactus:
                    Visuals.Rect("Cactus", x - 0.2f, y, x + 0.2f, y + h * 0.6f, new Color(0.25f, 0.6f, 0.3f), -5, root);
                    Visuals.Rect("Cactus", x + 0.2f, y + h * 0.3f, x + 0.6f, y + h * 0.4f, new Color(0.25f, 0.6f, 0.3f), -5, root);
                    break;
                case TreeKind.Crystal:
                    Visuals.Rect("Crystal", x - 0.2f, y, x + 0.2f, y + h * 0.25f, new Color(0.35f, 0.82f, 0.9f), -5, root);
                    break;
            }
        }
    }
}
