// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do the farm half and the outing half still feed each other when the world is a set of free, daily-regenerated biome maps joined by mid-map branches you must unlock?
// Date: 2026-10-30
//
// The hand-authored half of the world: maps, their sub-ecology zones, how
// they connect (branches) and how each branch opens, enemies, fish and the
// landmark templates. The seeded half (length, zones, terrain, positions) is
// in DayLoopMapGen.cs.
//
// Round 3 world (same graph as round 2, joined differently):
//
//   Rừng Sương ─(tường nứt)─ Làng Đồng Xanh (nhà) ─(mở sẵn)─ Đồng Cỏ ─(boss)─ Sa Mạc Cổ ─(chìa khóa)─ Làng Cát Vàng
//                                                                               └─(bùa câu được)─ Hang Động
//
// A branch is a side entrance somewhere in the MIDDLE of a map, not the map's
// end. Map ends are walled off by terrain: a cliff too high to jump, or a
// river / lake / sea you cannot cross -- and can fish in.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DayLoopProto
{
    public enum MapKind { Village, Surface, Cave }
    public enum TreeKind { None, Oak, Pine, Cactus, Crystal, Mushroom, Palm }
    public enum Building { House, Inn, Cauldron, Forge, Farm, Market, Transporter }
    public enum EndKind { Cliff, River, Lake, Sea, RockWall, UndergroundLake }
    public enum WaterKind { River, Lake, Sea, Oasis, CavePool }
    public enum Unlock { Open, Boss, Key, FishItem, CrackedWall }

    public class EnemyDef
    {
        public string name;
        public int hp, damage, bone, ore, gold, core;
        public float speed, telegraph = 0.5f, active = 0.2f, recover = 0.6f, range = 1.5f, knock = 4f;
        public Vector2 size = new Vector2(0.9f, 0.8f);
        public Color color;
        public bool boss;
    }

    public class Loot
    {
        public int gold, ore, bone, potion, healroot, ember, core;
        public Item? key;   // a branch key: only given while that branch is still closed
    }

    public class Part
    {
        public enum T { Solid, Platform, Deco, Chest, Ore }
        public T type;
        public Rect r;          // relative to the landmark's left edge and ground height
        public Color color;
        public Loot loot;
        public int oreYield = 1;
    }

    public class LandmarkDef
    {
        public string name;
        public float width;
        public readonly List<Part> parts = new List<Part>();

        public LandmarkDef(string name, float width) { this.name = name; this.width = width; }

        public LandmarkDef Solid(float x0, float y0, float x1, float y1, Color c) { parts.Add(new Part { type = Part.T.Solid, r = Rect.MinMaxRect(x0, y0, x1, y1), color = c }); return this; }
        public LandmarkDef Plat(float x0, float y, float x1, Color c) { parts.Add(new Part { type = Part.T.Platform, r = Rect.MinMaxRect(x0, y - 0.3f, x1, y), color = c }); return this; }
        public LandmarkDef Deco(float x0, float y0, float x1, float y1, Color c) { parts.Add(new Part { type = Part.T.Deco, r = Rect.MinMaxRect(x0, y0, x1, y1), color = c }); return this; }
        public LandmarkDef Chest(float x, float y, Loot loot) { parts.Add(new Part { type = Part.T.Chest, r = new Rect(x - 0.35f, y, 0.7f, 0.55f), loot = loot }); return this; }
        public LandmarkDef Ore(float x, float y, int yield) { parts.Add(new Part { type = Part.T.Ore, r = new Rect(x - 0.5f, y, 1f, 1f), oreYield = yield }); return this; }
    }

    // A sub-ecology inside a map: its own terrain roughness, plants and enemy mix.
    public class ZoneDef
    {
        public string name;
        public float weight = 1f, amp = 2f, baseOffset;
        public float treeMin = 5f, treeMax = 10f;
        public TreeKind tree = TreeKind.None;   // None = the map's default tree
        public bool pond;
        public float ceilingGap = 6f;           // caves only
        public float[] enemyWeights;            // one per MapDef.common
        public bool platforms;
    }

    public class FishDef
    {
        public string name;
        public float speed;    // how hard it darts around the bar (0.3 easy .. 1.1 hard)
        public bool big;
    }

    public class EdgeDef
    {
        public string a, b;
        public Unlock unlock;
        public int wallTier;      // CrackedWall: weapon tier needed to break it
        public Item keyItem;      // Key / FishItem
        public string bossMap;    // Boss: whose boss must be down
        public string Id => a + "-" + b;
        public string Other(string id) => id == a ? b : a;
        public bool Touches(string id) => id == a || id == b;
    }

    public class MapDef
    {
        public string id, name;
        public MapKind kind;
        public float minWidth = 150f, maxWidth = 250f;
        public Color sky, top, body;
        public TreeKind tree;
        public ZoneDef[] zones;
        public string forcedZone;                 // always include one zone of this name
        public EndKind[] leftEnds, rightEnds;     // seeded choice per side
        public WaterKind pondWater = WaterKind.Lake;
        public EnemyDef[] common = new EnemyDef[0];
        public float enemiesPer100;
        public EnemyDef boss;
        public string bossGuardsEdge;             // edge id, or
        public int bossGuardsLandmark = -1;       // landmark index; neither = optional boss
        public LandmarkDef[] landmarks = new LandmarkDef[0];
        public Building[] buildings = new Building[0];
        public float orePer100, platformsPer100;
        public int oreYield = 1;
        public Item? fishItem;                    // a branch item that can be fished from pondWater here
        public bool isHome, isStation;
        public Vector2Int mapCell;                // position on the M overlay grid

        public bool RerollsDaily => kind != MapKind.Village;
    }

    public static class World
    {
        public static readonly Dictionary<string, MapDef> Maps = new Dictionary<string, MapDef>();
        public static readonly List<EdgeDef> Edges = new List<EdgeDef>();
        public static readonly Dictionary<WaterKind, FishDef[]> Fish = new Dictionary<WaterKind, FishDef[]>();
        public const string Home = "hub";
        public const float FishItemChance = 0.3f;

        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static World()
        {
            // ---------- Enemies ----------
            var slime = new EnemyDef { name = "Slime", hp = 4, damage = 1, speed = 2f, telegraph = 0.55f, recover = 0.7f, bone = 1, knock = 6f, size = new Vector2(0.9f, 0.7f), color = C("#73cc59") };
            var wolf = new EnemyDef { name = "Sói đồng", hp = 6, damage = 2, speed = 3.6f, telegraph = 0.4f, recover = 0.6f, bone = 1, gold = 2, size = new Vector2(1.2f, 0.8f), color = C("#9a9488") };
            var spider = new EnemyDef { name = "Nhện rừng", hp = 6, damage = 2, speed = 3f, telegraph = 0.45f, recover = 0.6f, bone = 1, gold = 1, size = new Vector2(1.1f, 0.6f), color = C("#4a3b52") };
            var bear = new EnemyDef { name = "Gấu", hp = 16, damage = 3, speed = 2.6f, telegraph = 0.5f, recover = 0.5f, range = 1.8f, bone = 1, ore = 2, knock = 2.5f, size = new Vector2(1.4f, 1.5f), color = C("#6b4a2b") };
            var scorpion = new EnemyDef { name = "Bọ cạp", hp = 8, damage = 2, speed = 3f, telegraph = 0.4f, recover = 0.6f, bone = 1, gold = 2, size = new Vector2(1.2f, 0.6f), color = C("#c08a3a") };
            var mummy = new EnemyDef { name = "Xác ướp", hp = 18, damage = 3, speed = 2.2f, telegraph = 0.5f, recover = 0.5f, range = 1.8f, ore = 2, gold = 2, knock = 2.5f, size = new Vector2(1.1f, 1.7f), color = C("#d8cfae") };
            var stoneSlime = new EnemyDef { name = "Slime đá", hp = 10, damage = 2, speed = 1.8f, telegraph = 0.55f, recover = 0.7f, bone = 1, ore = 1, knock = 3f, size = new Vector2(1f, 0.8f), color = C("#7a7f88") };
            var caveSpider = new EnemyDef { name = "Nhện hang", hp = 10, damage = 3, speed = 3.2f, telegraph = 0.4f, recover = 0.55f, bone = 1, gold = 2, size = new Vector2(1.2f, 0.7f), color = C("#3a3046") };

            EnemyDef Boss(string n, int hp, int dmg, float spd, Color c) => new EnemyDef
            {
                name = n, hp = hp, damage = dmg, speed = spd, telegraph = 0.45f, active = 0.25f, recover = 0.7f, range = 2.4f,
                gold = 15, ore = 3, core = 1, knock = 0.8f, size = new Vector2(2.2f, 2.4f), color = c, boss = true
            };

            // ---------- Fish (by kind of water) ----------
            Fish[WaterKind.River] = new[] { new FishDef { name = "Cá chép", speed = 0.35f }, new FishDef { name = "Cá trê", speed = 0.6f }, new FishDef { name = "Cá lăng", speed = 0.9f, big = true } };
            Fish[WaterKind.Lake] = new[] { new FishDef { name = "Cá rô", speed = 0.35f }, new FishDef { name = "Cá chình", speed = 0.95f, big = true } };
            Fish[WaterKind.Sea] = new[] { new FishDef { name = "Cá thu", speed = 0.6f }, new FishDef { name = "Cá ngừ", speed = 1.1f, big = true } };
            Fish[WaterKind.Oasis] = new[] { new FishDef { name = "Cá cát", speed = 0.55f }, new FishDef { name = "Cá vàng sa mạc", speed = 0.9f, big = true } };
            Fish[WaterKind.CavePool] = new[] { new FishDef { name = "Cá mù", speed = 0.5f }, new FishDef { name = "Cá đèn", speed = 1.0f, big = true } };

            // ---------- Landmarks (hand-made, placed at seeded positions) ----------
            Color stone = C("#8a8f98"), darkStone = C("#5a5e66"), wood = C("#7a5534"), sand = C("#d9b56a"), leaf = C("#3f8a52");

            var watchRuin = new LandmarkDef("Tàn tích trạm gác", 7f)
                .Deco(0.3f, 0f, 1f, 4.5f, stone).Deco(6f, 0f, 6.7f, 3.4f, stone)
                .Plat(0f, 2.2f, 7f, darkStone)
                .Chest(3.5f, 2.2f, new Loot { gold = 6, ore = 2 });
            var oldWell = new LandmarkDef("Giếng cổ", 5f)
                .Solid(0.5f, 0f, 3f, 1.1f, stone).Deco(0.5f, 1.1f, 0.8f, 2.6f, wood).Deco(2.7f, 1.1f, 3f, 2.6f, wood).Deco(0.3f, 2.6f, 3.2f, 2.9f, wood)
                .Chest(4.2f, 0f, new Loot { bone = 3, healroot = 1 });
            var forestShrine = new LandmarkDef("Đền rừng", 9f)
                .Solid(0f, 0f, 9f, 1f, darkStone).Solid(1.5f, 1f, 7.5f, 2f, darkStone).Solid(3f, 2f, 6f, 3f, darkStone)
                .Deco(3.2f, 3f, 3.6f, 5f, leaf).Deco(5.4f, 3f, 5.8f, 5f, leaf)
                .Chest(4.5f, 3f, new Loot { ore = 3, potion = 1 });
            var treeHouse = new LandmarkDef("Nhà cây", 6f)
                .Deco(2.6f, 0f, 3.4f, 8f, wood).Deco(1f, 7.6f, 5f, 9f, leaf)
                .Plat(0f, 2.1f, 2.8f, wood).Plat(3.2f, 4.2f, 6f, wood).Plat(0.3f, 6.3f, 3f, wood)
                .Chest(1.5f, 6.3f, new Loot { gold = 8, ember = 1 });
            var pyramid = new LandmarkDef("Kim tự tháp", 12f)
                .Solid(0f, 0f, 12f, 1.4f, sand).Solid(1.5f, 1.4f, 10.5f, 2.8f, sand).Solid(3f, 2.8f, 9f, 4.2f, sand).Solid(4.5f, 4.2f, 7.5f, 5.6f, sand)
                .Chest(6f, 5.6f, new Loot { gold = 12, ore = 3, key = Item.SandKey });
            var ruinedCamp = new LandmarkDef("Trại đổ nát", 8f)
                .Deco(0.5f, 0f, 3.5f, 2f, C("#a8884f")).Deco(0.3f, 2f, 3.7f, 2.4f, C("#7a3a2a"))
                .Plat(4.5f, 1.8f, 8f, wood)
                .Chest(6.2f, 1.8f, new Loot { potion = 1, gold = 5 });
            var minersRoom = new LandmarkDef("Phòng thợ mỏ", 10f)
                .Deco(0f, 0f, 0.4f, 5f, wood).Deco(9.6f, 0f, 10f, 5f, wood).Deco(0f, 5f, 10f, 5.4f, wood)
                .Plat(0.4f, 2.2f, 9.6f, wood)
                .Ore(2f, 0f, 2).Ore(8f, 0f, 2)
                .Chest(5f, 2.2f, new Loot { ore = 2, gold = 5 });

            // ---------- Zones ----------
            ZoneDef Z(string n, float w, float amp, float tMin, float tMax, float[] ew, bool pond = false, TreeKind tree = TreeKind.None, float gap = 6f, bool plats = false, float baseOff = 0f) =>
                new ZoneDef { name = n, weight = w, amp = amp, treeMin = tMin, treeMax = tMax, enemyWeights = ew, pond = pond, tree = tree, ceilingGap = gap, platforms = plats, baseOffset = baseOff };

            // ---------- Maps ----------
            Add(new MapDef
            {
                id = "hub", name = "Làng Đồng Xanh", kind = MapKind.Village, minWidth = 86f, maxWidth = 86f,
                sky = C("#8fd0ff"), top = C("#6bbf59"), body = C("#8b5a2b"), tree = TreeKind.Oak,
                zones = new[] { Z("Làng", 1f, 0.4f, 7f, 14f, new float[0]) },
                leftEnds = new[] { EndKind.River }, rightEnds = new[] { EndKind.Cliff }, pondWater = WaterKind.River,
                buildings = new[] { Building.House, Building.Cauldron, Building.Forge, Building.Farm, Building.Market, Building.Transporter },
                isHome = true, isStation = true, mapCell = new Vector2Int(1, 0)
            });
            Add(new MapDef
            {
                id = "forest", name = "Rừng Sương", kind = MapKind.Surface, minWidth = 150f, maxWidth = 240f,
                sky = C("#2c4a3e"), top = C("#3f8a52"), body = C("#5b4631"), tree = TreeKind.Pine,
                zones = new[]
                {
                    Z("Rừng rậm", 3f, 3f, 2f, 4f, new[] { 2f, 1f }, plats: true),
                    Z("Trảng trống", 2f, 1.5f, 8f, 14f, new[] { 3f, 0.5f }),
                    Z("Lùm nấm", 1.5f, 2.5f, 3f, 6f, new[] { 1f, 1f }, tree: TreeKind.Mushroom),
                    Z("Ao rừng", 1f, 1f, 5f, 9f, new[] { 2f, 0.5f }, pond: true),
                },
                leftEnds = new[] { EndKind.Cliff, EndKind.Lake }, rightEnds = new[] { EndKind.Cliff, EndKind.Lake }, pondWater = WaterKind.Lake,
                common = new[] { spider, bear }, enemiesPer100 = 5f,
                boss = Boss("Hươu Sương Cổ Thụ", 36, 3, 3.5f, C("#a8c8b8")),
                landmarks = new[] { forestShrine, treeHouse }, orePer100 = 1.5f, platformsPer100 = 4f,
                mapCell = new Vector2Int(0, 0)
            });
            Add(new MapDef
            {
                id = "grass", name = "Đồng Cỏ", kind = MapKind.Surface, minWidth = 150f, maxWidth = 260f,
                sky = C("#7ec8f5"), top = C("#7acb5a"), body = C("#94643a"), tree = TreeKind.Oak,
                zones = new[]
                {
                    Z("Đồng bằng", 3f, 1.2f, 8f, 16f, new[] { 3f, 1f }),
                    Z("Đồi", 2f, 4f, 6f, 12f, new[] { 1f, 2f }),
                    Z("Rừng thưa", 2f, 2f, 2.5f, 5f, new[] { 1f, 2f }),
                    Z("Ao", 1.2f, 1f, 6f, 10f, new[] { 2f, 1f }, pond: true),
                },
                leftEnds = new[] { EndKind.River, EndKind.Cliff, EndKind.Lake }, rightEnds = new[] { EndKind.River, EndKind.Cliff, EndKind.Lake },
                pondWater = WaterKind.Lake,
                common = new[] { slime, wolf }, enemiesPer100 = 5f,
                boss = Boss("Sói Chúa Đồng Cỏ", 30, 3, 4f, C("#c9c2b4")), bossGuardsEdge = "grass-desert",
                landmarks = new[] { watchRuin, oldWell }, orePer100 = 1.5f,
                mapCell = new Vector2Int(2, 0)
            });
            Add(new MapDef
            {
                id = "desert", name = "Sa Mạc Cổ", kind = MapKind.Surface, minWidth = 180f, maxWidth = 300f,
                sky = C("#f2b66d"), top = C("#e3c27a"), body = C("#cfa55c"), tree = TreeKind.Cactus,
                zones = new[]
                {
                    Z("Cồn cát", 3f, 6f, 8f, 16f, new[] { 2f, 1f }),
                    Z("Đá phẳng", 2f, 1.5f, 12f, 20f, new[] { 1f, 2f }),
                    Z("Ốc đảo", 1f, 1f, 3f, 6f, new[] { 2f, 0f }, pond: true, tree: TreeKind.Palm),
                },
                forcedZone = "Ốc đảo",   // the Stone Charm is fished here -- must exist every day
                leftEnds = new[] { EndKind.Cliff, EndKind.Sea }, rightEnds = new[] { EndKind.Cliff, EndKind.Sea }, pondWater = WaterKind.Oasis,
                common = new[] { scorpion, mummy }, enemiesPer100 = 5f,
                boss = Boss("Pharaoh Cát", 50, 4, 3f, C("#e0b84a")), bossGuardsLandmark = 0,
                landmarks = new[] { pyramid, ruinedCamp }, orePer100 = 1.5f, oreYield = 2,
                fishItem = Item.StoneCharm,
                mapCell = new Vector2Int(3, 0)
            });
            Add(new MapDef
            {
                id = "cv", name = "Làng Cát Vàng", kind = MapKind.Village, minWidth = 66f, maxWidth = 66f,
                sky = C("#f2b66d"), top = C("#e3c27a"), body = C("#cfa55c"), tree = TreeKind.Palm,
                zones = new[] { Z("Làng", 1f, 0.4f, 8f, 14f, new float[0]) },
                leftEnds = new[] { EndKind.Cliff }, rightEnds = new[] { EndKind.Sea }, pondWater = WaterKind.Sea,
                buildings = new[] { Building.Inn, Building.Market, Building.Transporter },
                isStation = true, mapCell = new Vector2Int(4, 0)
            });
            Add(new MapDef
            {
                id = "cave", name = "Hang Động", kind = MapKind.Cave, minWidth = 150f, maxWidth = 240f,
                sky = C("#1c1a22"), top = C("#6b7a4a"), body = C("#4a4038"), tree = TreeKind.Crystal,
                zones = new[]
                {
                    Z("Đường hầm", 3f, 2f, 5f, 11f, new[] { 1f, 1f }, gap: 5f),
                    Z("Hang lớn", 2f, 4f, 4f, 9f, new[] { 1f, 2f }, gap: 9f),
                    Z("Hồ ngầm", 1f, 1f, 6f, 12f, new[] { 2f, 0.5f }, pond: true, gap: 8f),
                },
                leftEnds = new[] { EndKind.RockWall, EndKind.UndergroundLake }, rightEnds = new[] { EndKind.RockWall, EndKind.UndergroundLake },
                pondWater = WaterKind.CavePool,
                common = new[] { stoneSlime, caveSpider }, enemiesPer100 = 6f,
                boss = Boss("Nhện Mẹ Khổng Lồ", 60, 4, 3.5f, C("#5a3a6a")),
                landmarks = new[] { minersRoom, minersRoom }, orePer100 = 3f, oreYield = 2,
                mapCell = new Vector2Int(3, 1)
            });

            // ---------- Branches ----------
            Edges.Add(new EdgeDef { a = "hub", b = "grass", unlock = Unlock.Open });
            Edges.Add(new EdgeDef { a = "hub", b = "forest", unlock = Unlock.CrackedWall, wallTier = 2 });
            Edges.Add(new EdgeDef { a = "grass", b = "desert", unlock = Unlock.Boss, bossMap = "grass" });
            Edges.Add(new EdgeDef { a = "desert", b = "cv", unlock = Unlock.Key, keyItem = Item.SandKey });
            Edges.Add(new EdgeDef { a = "desert", b = "cave", unlock = Unlock.FishItem, keyItem = Item.StoneCharm });
        }

        static void Add(MapDef d) => Maps[d.id] = d;

        public static IEnumerable<EdgeDef> EdgesOf(string id) => Edges.Where(e => e.Touches(id));
        public static IEnumerable<string> Neighbors(string id) => EdgesOf(id).Select(e => e.Other(id));
        public static EdgeDef Edge(string id) => Edges.First(e => e.Id == id);
    }
}
