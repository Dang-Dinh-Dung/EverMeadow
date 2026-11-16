// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do the farm half and the outing half still feed each other when the world is a set of free, daily-regenerated biome maps joined by mid-map branches you must unlock?
// Date: 2026-10-30
//
// Hypothesis (round 3): exploration maps are long, seeded biome strips that
// regenerate every morning; other maps are reached through mid-map branches
// opened by a boss, a key, a fished item or by breaking a wall; map ends are
// walled off by cliffs or water you can fish in. The farm and the outing
// should still feed each other -- potions/meals/grilled fish for bosses,
// drops and boss cores for the forge, crops and fish for ticket gold -- and
// the player keeps coming home. Signal: from day 2 most days are "linked"
// (>= 1 farm product used on the outing AND >= 1 outing drop spent at home),
// and after reaching the far village the player is home at least every 2nd
// day. Refuted if the farm is skipped 2+ days in a row.
//
// Global state, world state, map loading, clock, inventory, sleep, fishing,
// telemetry and the OnGUI HUD all live here on purpose: one file to tune
// between playtests. Singleton allowed (prototype rules).

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DayLoopProto
{
    public class DayLoopGame : MonoBehaviour
    {
        public static DayLoopGame I;

        // ---- Tuning knobs ----
        [Header("World")]
        public int worldSeed = 20261029;

        [Header("Clock (game minutes; 360 = 06:00)")]
        public int dayStartMinute = 360;
        public int dayEndMinute = 1320;          // 22:00 -> pass out
        public int earliestSleepMinute = 1080;   // 18:00, same as the Game Time GDD
        public float realSecondsPerGameMinute = 0.5f; // 06:00-22:00 = 480 s real

        [Header("Player")]
        public int maxHp = 10;
        public int potionHeal = 5;
        public int grilledFishHeal = 4;
        public float mealDamageMult = 1.5f;
        public int startingPotions = 1;

        [Header("Farm")]
        public int growDays = 2;
        public int yieldPerHarvest = 2;

        [Header("Forge (index = current tier - 1)")]
        public int[] tierOreCost = { 3, 6, 10 };
        public int[] tierCoreCost = { 0, 1, 2 };
        public int baseDamage = 2;

        [Header("Economy")]
        public int priceHealroot = 3;
        public int priceEmber = 4;
        public int priceFish = 2;
        public int priceBigFish = 6;
        public int ticketCost = 8;

        [Header("Fishing (bar units: 0 = bottom, 1 = top)")]
        public float biteWaitMin = 2f, biteWaitMax = 5f, hookWindow = 0.8f;
        public float catchZone = 0.26f, liftAccel = 2.6f, sinkAccel = 2.0f;
        public float progressGain = 0.32f, progressLoss = 0.22f, bigFishChance = 0.25f;
        public int wallHitsToBreak = 3;

        // ---- Runtime state ----
        public int Day { get; private set; } = 1;
        public float ClockMinute { get; private set; }
        public int Hp { get; private set; }
        public bool MealActive { get; private set; }
        public int WeaponTier { get; private set; } = 1;
        public int WeaponDamage { get; private set; }
        public float WeaponSpeedMult { get; private set; } = 1f;
        string _branches = "";

        // World state that must survive a map being rebuilt.
        public readonly HashSet<string> Visited = new HashSet<string>();
        public readonly HashSet<string> BossDefeated = new HashSet<string>();
        public readonly HashSet<string> OpenedBranches = new HashSet<string>();
        public readonly HashSet<string> OpenedChests = new HashSet<string>();
        public readonly HashSet<string> KilledToday = new HashSet<string>();
        public readonly HashSet<string> MinedToday = new HashSet<string>();
        public readonly PlotState[] PlotStates = { new PlotState(), new PlotState(), new PlotState(), new PlotState() };
        public string CurrentMap { get; private set; }
        public MapLayout Map { get; private set; }
        string _bedMap = World.Home;
        GameObject _mapRoot;
        int _farVillageDay = -1;
        readonly Dictionary<string, int> _wallHits = new Dictionary<string, int>();
        readonly HashSet<string> _speciesCaught = new HashSet<string>();

        public DayLoopPlayer Player;
        public CameraFollow Cam;
        public readonly List<DayLoopEnemy> Enemies = new List<DayLoopEnemy>();
        public readonly List<OreNode> OreNodes = new List<OreNode>();
        public readonly List<Station> Stations = new List<Station>();

        public Station OpenMenu;
        readonly Dictionary<Item, int> _inv = new Dictionary<Item, int>();

        string _toast;
        float _toastUntil;
        string _banner;
        float _bannerUntil;
        bool _showStats, _showMap;

        // Fishing minigame.
        enum FishState { None, Waiting, Bite, Reel }
        FishState _fish = FishState.None;
        WaterKind _fishWater;
        float _fishTimer, _zonePos, _zoneVel, _fishPos, _fishTarget, _fishRetarget, _fishProgress, _fishStart;
        int _fishCastFrame;
        FishDef _hooked;
        bool _hookedIsItem;

        public enum DayEnd { Slept, PassedOut, Defeated }

        public bool IsBlackout => Time.unscaledTime < _bannerUntil;
        public bool IsFishing => _fish != FishState.None;
        public bool InputBlocked => OpenMenu != null || IsBlackout || _showMap || IsFishing;
        public int MaxTier => tierOreCost.Length + 1;
        public int NextTierOre => tierOreCost[Mathf.Clamp(WeaponTier - 1, 0, tierOreCost.Length - 1)];
        public int NextTierCore => tierCoreCost[Mathf.Clamp(WeaponTier - 1, 0, tierCoreCost.Length - 1)];
        public bool CanSleep => ClockMinute >= earliestSleepMinute;
        public int CurrentDamage => Mathf.Max(1, Mathf.RoundToInt(WeaponDamage * (MealActive ? mealDamageMult : 1f)));
        MapDef CurDef => World.Maps[CurrentMap];

        // ---- Telemetry ----
        class DayStats
        {
            public int day;
            public string wokeAt, sleptAt;
            public int potionsDrunk, mealsEaten, cropsSold, grilledEaten;   // farm-side product used on the outing
            public int fertilizerUsed, forgeUpgrades;                       // outing drop spent at home
            public int planted, harvested, brewed, cooked;                  // farm-half activity
            public int kills, oreGained, boneGained, goldGained, chestsOpened, branchesUsed, fastTravels;
            public int fishCaught, fishLost, fishCooked, fishSold;
            public float fishSeconds;
            public readonly List<string> bosses = new List<string>();
            public readonly List<string> unlocks = new List<string>();
            public readonly HashSet<string> maps = new HashSet<string>();
            public float minHpInWilds = float.MaxValue;
            public float realSeconds, secondsVillage, secondsWilds;
            public DayEnd end;

            public int FarmProductsUsed => potionsDrunk + mealsEaten + cropsSold;
            public int DropsSpentHome => fertilizerUsed + forgeUpgrades;
            public int FarmActions => planted + harvested + brewed + cooked + fertilizerUsed;
            public bool Linked => FarmProductsUsed > 0 && DropsSpentHome > 0;
            public bool FarmSkipped => FarmActions == 0 && FarmProductsUsed == 0;
            public bool HuntSkipped => !maps.Any(m => World.Maps[m].kind != MapKind.Village);
            public bool WasHome => maps.Contains(World.Home);
        }

        readonly List<DayStats> _history = new List<DayStats>();
        DayStats _today;
        float _dayRealStart;
        string _logPath;
        float _sessionStart;

        void Awake()
        {
            I = this;
            foreach (Item it in System.Enum.GetValues(typeof(Item))) _inv[it] = 0;
            _inv[Item.Potion] = startingPotions;
            Hp = maxHp;
            WeaponDamage = baseDamage;
            ClockMinute = dayStartMinute;
            _sessionStart = Time.unscaledTime;

            string dir = Application.isEditor
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "prototypes", "day-loop-concept", "telemetry"))
                : Application.persistentDataPath;
            Directory.CreateDirectory(dir);
            _logPath = Path.Combine(dir, $"session-{System.DateTime.Now:yyyyMMdd-HHmmss}.txt");
            Debug.Log($"[DayLoop] telemetry -> {_logPath}");
        }

        // Called by the bootstrap once the player and camera exist.
        public void Begin()
        {
            StartDay();
            LoadMap(World.Home, null, Spawn.Bed);
            Banner($"NGÀY {Day}\n{CurDef.name}", 1.5f);
        }

        void OnDestroy()
        {
            if (I == this) { HitStop.Clear(); WriteLog(final: true); I = null; }
        }

        void StartDay()
        {
            _today = new DayStats { day = Day, wokeAt = _bedMap };
            _dayRealStart = Time.unscaledTime;
        }

        void Banner(string text, float seconds)
        {
            _banner = text;
            _bannerUntil = Time.unscaledTime + seconds;
        }

        // ---- Map loading ----
        enum Spawn { Branch, Bed, Transporter, Start }

        void LoadMap(string id, string fromMap, Spawn spawn)
        {
            if (OpenMenu != null) { OpenMenu.CloseMenu(); OpenMenu = null; }
            _fish = FishState.None;
            HitStop.Clear();
            if (_mapRoot != null) { _mapRoot.SetActive(false); Destroy(_mapRoot); }
            Stations.Clear(); Enemies.Clear(); OreNodes.Clear();

            CurrentMap = id;
            Map = MapGen.Generate(World.Maps[id], worldSeed, Day);
            _mapRoot = MapGen.Build(Map, this);

            bool first = Visited.Add(id);
            _today.maps.Add(id);
            if (id == "cv" && _farVillageDay < 0) _farVillageDay = Day;

            float x = Map.playMin + 3f;
            if (spawn == Spawn.Branch) { var g = Stations.OfType<BranchGate>().FirstOrDefault(s => s.edge.Touches(fromMap)); if (g != null) x = g.transform.position.x; }
            else if (spawn == Spawn.Bed) { var b = Stations.OfType<Bed>().FirstOrDefault(); if (b != null) x = b.transform.position.x; }
            else if (spawn == Spawn.Transporter) { var t = Stations.OfType<Transporter>().FirstOrDefault(); if (t != null) x = t.transform.position.x; }

            Player.ResetForNewDay(new Vector2(x, Map.GroundY(x) + 1.2f));
            Cam.mapWidth = Map.width;
            Cam.Snap();

            var def = World.Maps[id];
            string sub = def.kind == MapKind.Village ? "Làng — trạm vận chuyển"
                       : $"{Map.width:0} ô · {string.Join(" → ", Map.zones.Select(z => z.def.name))}";
            if (def.boss != null && def.kind != MapKind.Village) sub += BossDefeated.Contains(id) ? "\nBoss đã hạ" : $"\nBoss: {def.boss.name}";
            if (first && def.kind == MapKind.Village && id != World.Home) sub += "\nTrạm vận chuyển đã mở!";
            Banner($"{def.name}\n<size=20>{sub}</size>", first ? 1.8f : 0.9f);
        }

        // ---- Branches ----
        public bool BranchOpen(EdgeDef e) =>
            e.unlock == Unlock.Open || OpenedBranches.Contains(e.Id) || (e.unlock == Unlock.Boss && BossDefeated.Contains(e.bossMap));

        public string BranchPrompt(EdgeDef e)
        {
            string target = World.Maps[e.Other(CurrentMap)].name;
            if (BranchOpen(e)) return $"Rẽ vào {target}";
            switch (e.unlock)
            {
                case Unlock.Boss: return $"Ngã rẽ tới {target} — bị {World.Maps[e.bossMap].boss.name} canh, hạ nó để mở";
                case Unlock.Key: return Count(e.keyItem) > 0 ? $"Mở cổng tới {target} bằng {ItemName(e.keyItem)}" : $"Cổng khóa tới {target} — cần {ItemName(e.keyItem)} (tìm trong công trình)";
                case Unlock.FishItem: return Count(e.keyItem) > 0 ? $"Đặt {ItemName(e.keyItem)} vào hốc đá — mở lối tới {target}" : $"Hốc đá trống trên cổng tới {target} — cần {ItemName(e.keyItem)}";
                case Unlock.CrackedWall: return $"Tường nứt chắn lối tới {target} — đánh vỡ (cần vũ khí tier {e.wallTier}, bạn tier {WeaponTier})";
            }
            return target;
        }

        public void UseBranch(EdgeDef e)
        {
            string target = e.Other(CurrentMap);
            if (!BranchOpen(e))
            {
                if ((e.unlock == Unlock.Key || e.unlock == Unlock.FishItem) && Spend(e.keyItem, 1))
                {
                    OpenBranch(e, e.unlock == Unlock.Key ? "key" : "fish");
                    return;
                }
                Toast(BranchPrompt(e));
                return;
            }
            string from = CurrentMap;
            _today.branchesUsed++;
            LoadMap(target, from, Spawn.Branch);
        }

        public void HitWall(EdgeDef e)
        {
            if (BranchOpen(e)) return;
            if (WeaponTier < e.wallTier) { Toast($"Keng! Vũ khí chưa đủ mạnh — cần tier {e.wallTier} (rèn ở lò)"); return; }
            _wallHits.TryGetValue(e.Id, out int hits);
            _wallHits[e.Id] = ++hits;
            if (hits < wallHitsToBreak) { Toast($"Tường nứt thêm ({hits}/{wallHitsToBreak})"); return; }
            OpenBranch(e, "wall");
        }

        void OpenBranch(EdgeDef e, string how)
        {
            if (!OpenedBranches.Add(e.Id)) return;
            _today.unlocks.Add($"{e.Id}:{how}");
            Toast($"Lối tới {World.Maps[e.Other(CurrentMap)].name} đã mở!");
        }

        public string[] FastTravelTargets() =>
            World.Maps.Values.Where(m => m.isStation && m.id != CurrentMap && Visited.Contains(m.id)).Select(m => m.id).ToArray();

        public void FastTravel(string target)
        {
            _today.fastTravels++;
            LoadMap(target, null, Spawn.Transporter);
        }

        void Update()
        {
            HandleKeys();
            if (IsBlackout || _showMap) return;

            if (IsFishing) TickFishing(Time.deltaTime);

            // Clock keeps running in menus and while fishing -- both are part of the day.
            ClockMinute += Time.deltaTime / realSecondsPerGameMinute;

            if (CurDef.kind == MapKind.Village) _today.secondsVillage += Time.deltaTime;
            else
            {
                _today.secondsWilds += Time.deltaTime;
                _today.minHpInWilds = Mathf.Min(_today.minHpInWilds, Hp);
            }

            if (ClockMinute >= dayEndMinute) Sleep(DayEnd.PassedOut);
        }

        void HandleKeys()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) _showStats = !_showStats;
            if (IsBlackout) return;
            if (IsFishing) return;   // the minigame reads its own keys
            if (Input.GetKeyDown(KeyCode.M) && OpenMenu == null) _showMap = !_showMap;
            if (_showMap) { if (Input.GetKeyDown(KeyCode.Escape)) _showMap = false; return; }

            // Observer-only: reroll the world seed to compare layouts. Progress is kept.
            if (Input.GetKeyDown(KeyCode.F5))
            {
                worldSeed = Random.Range(1, int.MaxValue);
                LoadMap(CurrentMap, null, Spawn.Start);
                Toast($"Seed mới: {worldSeed}");
                return;
            }

            if (OpenMenu != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) OpenMenu?.MenuKey(1);
                if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) OpenMenu?.MenuKey(2);
                if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) OpenMenu?.MenuKey(3);
                if (OpenMenu != null && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)))
                {
                    OpenMenu.CloseMenu();
                    OpenMenu = null;
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
            {
                var s = NearestStation();
                if (s != null && (Input.GetKeyDown(KeyCode.E) || s is FishingSpot)) s.Interact();
            }
            if (Input.GetKeyDown(KeyCode.Q)) Heal();
            if (Input.GetKeyDown(KeyCode.R)) EatMeal();
        }

        public Station NearestStation()
        {
            if (Player == null) return null;
            Station best = null;
            float bestD = 1.4f;
            Vector2 pp = Player.transform.position;
            foreach (var s in Stations)
            {
                if (s == null) continue;
                Vector2 sp = s.transform.position;
                float d = Mathf.Abs(sp.x - pp.x);
                if (d < bestD && Mathf.Abs(sp.y - pp.y) < 2.2f) { bestD = d; best = s; }
            }
            return best;
        }

        // ---- Fishing: cast, wait for the bite, hook, then keep the fish inside the rising zone ----
        public void StartFishing(WaterKind water)
        {
            _fish = FishState.Waiting;
            _fishWater = water;
            _fishTimer = Random.Range(biteWaitMin, biteWaitMax);
            _fishStart = Time.unscaledTime;
            _fishCastFrame = Time.frameCount;
            Toast("Thả câu... chờ cá cắn (dấu !), rồi bấm F");
        }

        static bool FishKeyDown() => Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E);
        static bool FishKeyHeld() => Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.E) || Input.GetMouseButton(0);

        void TickFishing(float dt)
        {
            if (Time.frameCount == _fishCastFrame) return;   // the key that cast the line is not a strike
            if (Input.GetKeyDown(KeyCode.Escape)) { EndFishing(false, "Thu cần"); return; }

            switch (_fish)
            {
                case FishState.Waiting:
                    if (FishKeyDown()) { EndFishing(false, "Giật sớm quá — cá chạy mất"); return; }
                    if ((_fishTimer -= dt) <= 0f) { _fish = FishState.Bite; _fishTimer = hookWindow; }
                    break;

                case FishState.Bite:
                    if (FishKeyDown()) { Hook(); return; }
                    if ((_fishTimer -= dt) <= 0f) EndFishing(false, "Cá nhả mồi rồi");
                    break;

                case FishState.Reel:
                    _zoneVel += (FishKeyHeld() ? liftAccel : -sinkAccel) * dt;
                    _zonePos += _zoneVel * dt;
                    if (_zonePos < 0f) { _zonePos = 0f; _zoneVel *= -0.3f; }
                    if (_zonePos > 1f - catchZone) { _zonePos = 1f - catchZone; _zoneVel *= -0.3f; }

                    if ((_fishRetarget -= dt) <= 0f)
                    {
                        _fishTarget = Random.Range(0.05f, 0.95f);
                        _fishRetarget = Random.Range(0.35f, 1.2f) / Mathf.Max(0.5f, _hooked.speed * 1.5f);
                    }
                    _fishPos = Mathf.MoveTowards(_fishPos, _fishTarget, _hooked.speed * dt);

                    bool inside = _fishPos >= _zonePos && _fishPos <= _zonePos + catchZone;
                    _fishProgress += (inside ? progressGain : -progressLoss) * dt;
                    if (_fishProgress >= 1f) Land();
                    else if (_fishProgress <= 0f) EndFishing(false, $"{_hooked.name} giãy thoát mất!");
                    break;
            }
        }

        void Hook()
        {
            var table = World.Fish[_fishWater];
            // A branch item can be fished up here while its branch is still closed.
            var item = CurDef.fishItem;
            var itemEdge = item == null ? null : World.Edges.FirstOrDefault(e => e.unlock == Unlock.FishItem && e.keyItem == item.Value);
            _hookedIsItem = item != null && _fishWater == CurDef.pondWater && itemEdge != null && !BranchOpen(itemEdge)
                            && Count(item.Value) == 0 && Random.value < World.FishItemChance;

            if (_hookedIsItem) _hooked = new FishDef { name = ItemName(item.Value), speed = 0.7f };
            else
            {
                var big = table.Where(f => f.big).ToArray();
                var small = table.Where(f => !f.big).ToArray();
                bool pickBig = big.Length > 0 && Random.value < bigFishChance;
                var pool = pickBig ? big : small;
                _hooked = pool[Random.Range(0, pool.Length)];
            }

            _fish = FishState.Reel;
            _zonePos = 0.1f; _zoneVel = 0f;
            _fishPos = 0.3f; _fishTarget = 0.5f; _fishRetarget = 0.5f;
            _fishProgress = 0.3f;
        }

        void Land()
        {
            if (_hookedIsItem)
            {
                var item = CurDef.fishItem.Value;
                Add(item, 1);
                _today.unlocks.Add($"got:{item}");
                EndFishing(true, $"Bạn câu được {ItemName(item)}! Có lẽ nó vừa với một hốc đá nào đó...");
                return;
            }
            Add(_hooked.big ? Item.BigFish : Item.Fish, 1);
            _speciesCaught.Add(_hooked.name);
            EndFishing(true, $"Câu được {_hooked.name}{(_hooked.big ? " (cá to)" : "")}!");
        }

        void EndFishing(bool caught, string msg)
        {
            _today.fishSeconds += Time.unscaledTime - _fishStart;
            if (_fish == FishState.Reel) { if (caught) _today.fishCaught++; else _today.fishLost++; }
            _fish = FishState.None;
            Toast(msg);
        }

        // ---- Inventory ----
        public int Count(Item it) => _inv[it];
        public void Add(Item it, int n) { _inv[it] += n; if (it == Item.Gold && n > 0) _today.goldGained += n; }
        public bool Spend(Item it, int n)
        {
            if (_inv[it] < n) return false;
            _inv[it] -= n;
            return true;
        }

        public string ItemName(Item it)
        {
            switch (it)
            {
                case Item.Healroot: return "Healroot";
                case Item.Emberpepper: return "Emberpepper";
                case Item.Potion: return "Potion";
                case Item.Meal: return "Món ăn cay";
                case Item.Ore: return "Quặng";
                case Item.Bone: return "Xương";
                case Item.Gold: return "vàng";
                case Item.BossCore: return "Lõi Boss";
                case Item.Fish: return "Cá";
                case Item.BigFish: return "Cá to";
                case Item.GrilledFish: return "Cá nướng";
                case Item.SandKey: return "Chìa Khóa Cát";
                default: return "Bùa Đá Cổ";
            }
        }

        // ---- Consumables ----
        void Heal()
        {
            if (Hp >= maxHp) { Toast("Máu đang đầy"); return; }
            if (Spend(Item.Potion, 1))
            {
                Hp = Mathf.Min(maxHp, Hp + potionHeal);
                _today.potionsDrunk++;
                Toast($"Uống potion: +{potionHeal} HP");
            }
            else if (Spend(Item.GrilledFish, 1))
            {
                Hp = Mathf.Min(maxHp, Hp + grilledFishHeal);
                _today.grilledEaten++;
                Toast($"Ăn cá nướng: +{grilledFishHeal} HP");
            }
            else Toast("Hết potion và cá nướng — nấu ở nồi (Healroot hoặc cá)");
        }

        void EatMeal()
        {
            if (MealActive) { Toast("Đã ăn rồi — hiệu lực tới khi ngủ"); return; }
            if (!Spend(Item.Meal, 1)) { Toast("Không có món ăn cay — nấu từ Emberpepper"); return; }
            MealActive = true;
            _today.mealsEaten++;
            Toast($"Ăn món cay: sát thương ×{mealDamageMult:0.#} tới khi ngủ");
        }

        public void DamagePlayer(int dmg)
        {
            Hp -= dmg;
            if (Hp <= 0) { Hp = 0; Sleep(DayEnd.Defeated); }
        }

        // ---- Forge ----
        public void UpgradeWeapon(bool fast)
        {
            WeaponTier++;
            if (fast) { WeaponDamage += 1; WeaponSpeedMult *= 0.8f; _branches += "N"; }
            else { WeaponDamage += 2; WeaponSpeedMult *= 1.1f; _branches += "M"; }
            Toast($"Rèn xong: tier {WeaponTier} ({(fast ? "nhánh Nhanh" : "nhánh Mạnh")})");
        }

        // ---- Telemetry hooks ----
        public void LogFarmAction(string what)
        {
            switch (what)
            {
                case "plant": _today.planted++; break;
                case "harvest": _today.harvested++; break;
                case "brew": _today.brewed++; break;
                case "cook": _today.cooked++; break;
            }
        }

        public void LogDropSpentAtHome(string what)
        {
            if (what == "fertilizer") _today.fertilizerUsed++;
            else if (what == "forge") _today.forgeUpgrades++;
        }

        public void LogCropSold(int price) => _today.cropsSold++;
        public void LogFishCooked() => _today.fishCooked++;
        public void LogFishSold() => _today.fishSold++;

        public void OnEnemyKilled(DayLoopEnemy e)
        {
            var d = e.def;
            _today.kills++;
            if (d.boss)
            {
                BossDefeated.Add(CurrentMap);
                _today.bosses.Add(d.name);
                foreach (var edge in World.Edges.Where(x => x.unlock == Unlock.Boss && x.bossMap == CurrentMap)) OpenBranch(edge, "boss");
            }
            else KilledToday.Add(e.key);

            var parts = new List<string>();
            if (d.bone > 0) { Add(Item.Bone, d.bone); _today.boneGained += d.bone; parts.Add($"+{d.bone} Xương"); }
            if (d.ore > 0) { Add(Item.Ore, d.ore); _today.oreGained += d.ore; parts.Add($"+{d.ore} Quặng"); }
            if (d.gold > 0) { Add(Item.Gold, d.gold); parts.Add($"+{d.gold} vàng"); }
            if (d.core > 0) { Add(Item.BossCore, d.core); parts.Add($"+{d.core} Lõi Boss"); }
            Toast(string.Join("  ", parts));
            if (d.boss)
            {
                bool guards = World.Edges.Any(x => x.unlock == Unlock.Boss && x.bossMap == CurrentMap);
                Banner($"ĐÃ HẠ {d.name.ToUpper()}\n<size=22>{(guards ? "Ngã rẽ nó canh giữ đã mở" : "Nhận Lõi Boss cho lò rèn")}</size>", 1.6f);
            }
        }

        public void OnOreMined(string key, int n)
        {
            MinedToday.Add(key);
            Add(Item.Ore, n);
            _today.oreGained += n;
            Toast($"+{n} Quặng");
        }

        public void OpenChest(string key, Loot l)
        {
            OpenedChests.Add(key);
            _today.chestsOpened++;
            var parts = new List<string>();
            void Give(Item it, int n) { if (n <= 0) return; Add(it, n); parts.Add($"+{n} {ItemName(it)}"); }
            Give(Item.Gold, l.gold); Give(Item.Ore, l.ore); Give(Item.Bone, l.bone); Give(Item.Potion, l.potion);
            Give(Item.Healroot, l.healroot); Give(Item.Emberpepper, l.ember); Give(Item.BossCore, l.core);
            if (l.key != null)
            {
                var edge = World.Edges.FirstOrDefault(e => e.unlock == Unlock.Key && e.keyItem == l.key.Value);
                if (edge != null && !BranchOpen(edge) && Count(l.key.Value) == 0)
                {
                    Give(l.key.Value, 1);
                    _today.unlocks.Add($"got:{l.key.Value}");
                }
            }
            _today.oreGained += l.ore;
            _today.boneGained += l.bone;
            Toast("Rương: " + string.Join("  ", parts));
        }

        // ---- Sleep / day rollover ----
        public void Sleep(DayEnd how)
        {
            if (OpenMenu != null) { OpenMenu.CloseMenu(); OpenMenu = null; }
            if (IsFishing) EndFishing(false, "");
            HitStop.Clear();

            if (how == DayEnd.Slept) _bedMap = CurrentMap;   // the bed you used becomes your wake-up point
            _today.end = how;
            _today.sleptAt = how == DayEnd.Slept ? CurrentMap : $"({CurrentMap})";
            _today.realSeconds = Time.unscaledTime - _dayRealStart;
            _history.Add(_today);
            WriteLog(final: false);

            Day++;
            ClockMinute = dayStartMinute;
            Hp = maxHp;
            MealActive = false;
            foreach (var p in PlotStates)
            {
                p.fertilizedToday = false;
                if (p.crop != null && p.stage < growDays) p.stage++;
            }
            KilledToday.Clear();
            MinedToday.Clear();
            _wallHits.Clear();

            StartDay();
            LoadMap(_bedMap, null, Spawn.Bed);

            string why = how == DayEnd.Defeated ? "Bạn gục ngã — tỉnh dậy ở giường đã ngủ gần nhất, đồ vẫn còn"
                       : how == DayEnd.PassedOut ? "Bạn ngất vì kiệt sức lúc 22:00"
                       : _bedMap == World.Home ? "Ngủ ngon ở nhà" : $"Ngủ ở {World.Maps[_bedMap].name} — cây ở nhà vẫn lớn";
            Banner($"NGÀY {Day}\n<size=22>{why}\nCác vùng hoang đã đổi hình dạng</size>", 2.2f);
        }

        public void Toast(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            _toast = msg;
            _toastUntil = Time.unscaledTime + 2.8f;
        }

        // ---- Hypothesis readout ----
        string SignalLine()
        {
            int linked = 0, eligible = 0, fs = 0, hs = 0, f = 0, h = 0, homeDays = 0, sinceFar = 0;
            foreach (var d in _history)
            {
                if (d.day >= 2) { eligible++; if (d.Linked) linked++; }
                f = d.FarmSkipped ? f + 1 : 0; fs = Mathf.Max(fs, f);
                h = d.HuntSkipped ? h + 1 : 0; hs = Mathf.Max(hs, h);
                if (_farVillageDay > 0 && d.day >= _farVillageDay) { sinceFar++; if (d.WasHome) homeDays++; }
            }
            string far = _farVillageDay > 0 ? $" · về nhà {homeDays}/{sinceFar} ngày từ khi tới Cát Vàng" : "";
            if (fs >= 2) return $"TÍN HIỆU: BÁC BỎ — bỏ nông trại {fs} ngày liền{far}";
            if (hs >= 2) return $"TÍN HIỆU: BÁC BỎ — bỏ đi săn {hs} ngày liền{far}";
            if (eligible == 0) return "TÍN HIỆU: chờ (tính từ ngày 2)";
            return $"TÍN HIỆU: ngày liên kết {linked}/{eligible} (ngày 2+){far}";
        }

        void WriteLog(bool final)
        {
            if (string.IsNullOrEmpty(_logPath)) return;
            var sb = new StringBuilder();
            sb.AppendLine("# PROTOTYPE - NOT FOR PRODUCTION -- day-loop telemetry (round 3: free maps, branches, fishing)");
            sb.AppendLine($"# written {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}  session {(Time.unscaledTime - _sessionStart) / 60f:0.0} min  final={final}  worldSeed={worldSeed}");
            sb.AppendLine("day,end,wokeAt,sleptAt,maps,wasHome,realSec,secVillage,secWilds,minHpWilds,potions,meals,grilledFish,cropsSold,fertilizer,forge,planted,harvested,brewed,cooked," +
                          "kills,bosses,ore,bone,gold,chests,branches,fastTravels,fishCaught,fishLost,fishSec,fishCooked,fishSold,unlocks,linked,farmSkipped,huntSkipped");
            foreach (var d in _history)
            {
                string minHp = d.minHpInWilds == float.MaxValue ? "" : d.minHpInWilds.ToString("0");
                sb.AppendLine($"{d.day},{d.end},{d.wokeAt},{d.sleptAt},{string.Join("|", d.maps)},{d.WasHome},{d.realSeconds:0},{d.secondsVillage:0},{d.secondsWilds:0},{minHp}," +
                              $"{d.potionsDrunk},{d.mealsEaten},{d.grilledEaten},{d.cropsSold},{d.fertilizerUsed},{d.forgeUpgrades},{d.planted},{d.harvested},{d.brewed},{d.cooked}," +
                              $"{d.kills},{string.Join("|", d.bosses)},{d.oreGained},{d.boneGained},{d.goldGained},{d.chestsOpened},{d.branchesUsed},{d.fastTravels}," +
                              $"{d.fishCaught},{d.fishLost},{d.fishSeconds:0},{d.fishCooked},{d.fishSold},{string.Join("|", d.unlocks)},{d.Linked},{d.FarmSkipped},{d.HuntSkipped}");
            }
            sb.AppendLine($"# reached day {Day}; weapon tier {WeaponTier} branches '{_branches}'; bosses down: {string.Join("|", BossDefeated)}; " +
                          $"paths open: {string.Join("|", OpenedBranches)}; fish species: {string.Join("|", _speciesCaught)}; far village found day {_farVillageDay}");
            sb.AppendLine($"# {SignalLine()}");
            File.WriteAllText(_logPath, sb.ToString());
        }

        public static string FormatClock(float minute)
        {
            int m = Mathf.FloorToInt(minute);
            return $"{(m / 60) % 24:00}:{m % 60:00}";
        }

        // ---- HUD (instrumentation, not game UI) ----
        GUIStyle _style, _big, _small, _center;

        void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true };
                _style.normal.textColor = Color.white;
                _big = new GUIStyle(_style) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
                _small = new GUIStyle(_style) { fontSize = 14, alignment = TextAnchor.UpperCenter };
                _center = new GUIStyle(_style) { alignment = TextAnchor.MiddleCenter };
            }
            if (CurrentMap == null) return;

            var cam = Camera.main;
            if (cam != null)
            {
                foreach (var s in Stations)
                {
                    if (s == null) continue;
                    string label = s is BranchGate bg
                        ? $"Ngã rẽ → {World.Maps[bg.Target].name}{(BranchOpen(bg.edge) ? "" : " (đóng)")}"
                        : s.displayName;
                    WorldLabel(cam, s.transform.position + Vector3.up * (s.transform.localScale.y / 2f + 0.6f), label);
                }
                foreach (var e in Enemies) if (e != null && !e.IsDead && !e.IsBoss) WorldLabel(cam, e.transform.position + Vector3.up * (e.def.size.y / 2f + 0.4f), e.HpText);
                if (Player != null)
                    WorldLabel(cam, Player.transform.position + Vector3.up * 1.6f, $"<size=12>{Map.ZoneAt(Player.transform.position.x).def.name}</size>");
            }

            GUI.Box(new Rect(10, 10, 470, 256), "");
            var sb = new StringBuilder();
            sb.AppendLine($"<b>{CurDef.name}</b>   <b>Ngày {Day}   {FormatClock(ClockMinute)}</b>");
            sb.AppendLine($"<size=13>Ngủ được từ {FormatClock(earliestSleepMinute)}, ngất lúc {FormatClock(dayEndMinute)} · giường: {World.Maps[_bedMap].name}</size>");
            sb.AppendLine($"HP {Hp}/{maxHp}   Vũ khí tier {WeaponTier} · sát thương {CurrentDamage}{(MealActive ? " (đã ăn cay)" : "")}");
            sb.AppendLine($"Potion {Count(Item.Potion)}   Cá nướng {Count(Item.GrilledFish)}   Món cay {Count(Item.Meal)}   Vàng {Count(Item.Gold)}");
            sb.AppendLine($"Healroot {Count(Item.Healroot)}   Emberpepper {Count(Item.Emberpepper)}   Cá {Count(Item.Fish)} / to {Count(Item.BigFish)}");
            sb.AppendLine($"Quặng {Count(Item.Ore)}   Xương {Count(Item.Bone)}   Lõi Boss {Count(Item.BossCore)}");
            var keys = new List<string>();
            if (Count(Item.SandKey) > 0) keys.Add(ItemName(Item.SandKey));
            if (Count(Item.StoneCharm) > 0) keys.Add(ItemName(Item.StoneCharm));
            sb.AppendLine(keys.Count > 0 ? $"<color=#ffd76a>Vật phẩm đặc biệt: {string.Join(", ", keys)}</color>" : "");
            sb.AppendLine("<size=13>A/D đi · Space nhảy · J đánh · Shift né · E tương tác · F câu cá · Q hồi máu · R món cay · M bản đồ · Tab số liệu</size>");
            GUI.Label(new Rect(20, 16, 460, 248), sb.ToString(), _style);

            var boss = Enemies.FirstOrDefault(e => e != null && e.IsBoss && e.IsEngaged);
            if (boss != null)
            {
                float w = 500, x = Screen.width / 2f - w / 2f;
                GUI.Box(new Rect(x - 4, 14, w + 8, 46), "");
                GUI.color = new Color(0.8f, 0.15f, 0.2f);
                GUI.DrawTexture(new Rect(x, 38, w * Mathf.Clamp01(boss.Hp / (float)boss.def.hp), 16), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(x, 14, w, 24), $"{boss.def.name}  {Mathf.Max(0, boss.Hp)}/{boss.def.hp}", _center);
            }

            if (IsFishing) DrawFishing();
            else if (OpenMenu != null && OpenMenu.MenuText != null)
            {
                GUI.Box(new Rect(Screen.width / 2f - 360, Screen.height - 210, 720, 190), "");
                GUI.Label(new Rect(Screen.width / 2f - 345, Screen.height - 202, 690, 180), OpenMenu.MenuText, _style);
            }
            else if (!IsBlackout && !_showMap)
            {
                var near = NearestStation();
                if (near != null)
                    GUI.Label(new Rect(Screen.width / 2f - 450, Screen.height - 60, 900, 30), $"[{(near is FishingSpot ? "F" : "E")}] {near.Prompt}", _center);
            }

            if (Time.unscaledTime < _toastUntil && !string.IsNullOrEmpty(_toast))
                GUI.Label(new Rect(Screen.width / 2f - 450, Screen.height - 100, 900, 30), _toast, _center);

            if (_showMap) DrawWorldMap();
            if (_showStats) DrawStats();

            if (IsBlackout)
            {
                GUI.color = new Color(0, 0, 0, 0.85f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), _banner, _big);
            }
        }

        void DrawFishing()
        {
            float bx = Screen.width / 2f + 120f, by = Screen.height / 2f - 170f, bw = 46f, bh = 340f;
            if (_fish == FishState.Waiting || _fish == FishState.Bite)
            {
                string txt = _fish == FishState.Bite ? "<size=60><color=#ffdd44>!</color></size>\nBẤM F!" : "Đang chờ cá cắn...\n<size=13>(Esc thu cần)</size>";
                GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height / 2f - 160, 400, 140), txt, _center);
                return;
            }
            GUI.Box(new Rect(bx - 10, by - 40, bw + 90, bh + 70), "");
            GUI.Label(new Rect(bx - 10, by - 36, bw + 90, 28), $"<size=14>{(_hookedIsItem ? "???" : _hooked.name)}</size>", _center);
            GUI.color = new Color(0.1f, 0.2f, 0.35f);
            GUI.DrawTexture(new Rect(bx, by, bw, bh), Texture2D.whiteTexture);
            bool inside = _fishPos >= _zonePos && _fishPos <= _zonePos + catchZone;
            GUI.color = inside ? new Color(0.35f, 0.9f, 0.45f, 0.85f) : new Color(0.3f, 0.6f, 0.35f, 0.7f);
            GUI.DrawTexture(new Rect(bx, by + bh * (1f - _zonePos - catchZone), bw, bh * catchZone), Texture2D.whiteTexture);
            GUI.color = _hookedIsItem ? new Color(0.9f, 0.8f, 0.4f) : new Color(1f, 0.6f, 0.3f);
            GUI.DrawTexture(new Rect(bx + 8, by + bh * (1f - _fishPos) - 8, bw - 16, 16), Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
            GUI.DrawTexture(new Rect(bx + bw + 14, by, 18, bh), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.85f, 0.3f);
            GUI.DrawTexture(new Rect(bx + bw + 14, by + bh * (1f - _fishProgress), 18, bh * _fishProgress), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(bx - 260, by + bh / 2f - 40, 250, 80), "<size=14>Giữ F (hoặc chuột trái) để nâng vùng xanh,\nthả ra để hạ. Giữ cá trong vùng.\nEsc thu cần.</size>", _style);
        }

        void WorldLabel(Camera cam, Vector3 world, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0 || sp.x < -200 || sp.x > Screen.width + 200) return;
            GUI.Label(new Rect(sp.x - 180, Screen.height - sp.y - 10, 360, 24), text, _small);
        }

        // A map is shown once visited or once it borders a visited map (as in the HTML).
        bool Seen(string id) => Visited.Contains(id) || World.Neighbors(id).Any(Visited.Contains);

        void DrawWorldMap()
        {
            const float cellW = 170f, cellH = 120f, boxW = 150f, boxH = 70f;
            float ox = Screen.width / 2f - 2.5f * cellW, oy = Screen.height / 2f - 110f;
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(ox - 20, oy - 60, 5 * cellW + 40, 2 * cellH + 110), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(ox, oy - 52, 5 * cellW, 30), "<b>BẢN ĐỒ THẾ GIỚI</b>  (M / Esc đóng) — vàng = lối đã mở, xám = còn đóng", _center);

            Vector2 Center(MapDef m) => new Vector2(ox + m.mapCell.x * cellW + cellW / 2f, oy + m.mapCell.y * cellH + boxH / 2f);

            foreach (var e in World.Edges)
            {
                if (!Seen(e.a) || !Seen(e.b) || (!Visited.Contains(e.a) && !Visited.Contains(e.b))) continue;
                Vector2 a = Center(World.Maps[e.a]), b = Center(World.Maps[e.b]);
                GUI.color = BranchOpen(e) ? new Color(0.9f, 0.75f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
                if (Mathf.Abs(a.y - b.y) < 1f) GUI.DrawTexture(new Rect(Mathf.Min(a.x, b.x), a.y - 2, Mathf.Abs(a.x - b.x), 4), Texture2D.whiteTexture);
                else GUI.DrawTexture(new Rect(a.x - 2, Mathf.Min(a.y, b.y), 4, Mathf.Abs(a.y - b.y)), Texture2D.whiteTexture);
                GUI.color = Color.white;
                if (!BranchOpen(e))
                {
                    string how = e.unlock == Unlock.Boss ? "boss" : e.unlock == Unlock.Key ? "chìa" : e.unlock == Unlock.FishItem ? "bùa" : "tường";
                    GUI.Label(new Rect((a.x + b.x) / 2f - 40, (a.y + b.y) / 2f - 22, 80, 20), $"<size=11>[{how}]</size>", _center);
                }
            }

            foreach (var m in World.Maps.Values)
            {
                if (!Seen(m.id)) continue;
                Vector2 c = Center(m);
                var r = new Rect(c.x - boxW / 2f, c.y - boxH / 2f, boxW, boxH);
                bool v = Visited.Contains(m.id);
                GUI.color = m.kind == MapKind.Village ? new Color(0.25f, 0.5f, 0.35f) : v ? new Color(0.3f, 0.3f, 0.38f) : new Color(0.2f, 0.2f, 0.22f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = Color.white;
                string status = m.kind == MapKind.Village ? (v ? "làng · trạm" : "làng (chưa tới)")
                              : !v ? "chưa tới"
                              : m.boss == null ? "" : BossDefeated.Contains(m.id) ? "boss: đã hạ" : "boss: còn";
                string here = m.id == CurrentMap ? "▶ " : "";
                GUI.Label(r, $"<size=15><b>{here}{(v ? m.name : m.name + "?")}</b></size>\n<size=12>{status}{(m.isHome ? " · nhà" : "")}</size>", _center);
            }
        }

        void DrawStats()
        {
            float w = 680, h = 140 + 22 * _history.Count;
            var r = new Rect(Screen.width - w - 10, 10, w, h);
            GUI.Box(r, "");
            var sb = new StringBuilder();
            sb.AppendLine("<b>SỐ LIỆU (dành cho người quan sát)</b>  seed " + worldSeed + "  (F5: seed mới)");
            sb.AppendLine("<size=15>" + SignalLine() + "</size>");
            sb.AppendLine($"<size=13>Hôm nay: dùng đồ farm {_today.FarmProductsUsed} · tiêu đồ rơi ở nhà {_today.DropsSpentHome} · map {_today.maps.Count} · về nhà {(_today.WasHome ? "có" : "chưa")} · cá {_today.fishCaught} ({_today.fishSeconds:0}s)</size>");
            sb.AppendLine("<size=13>Ngày | đồ farm dùng | đồ rơi tiêu | liên kết | bỏ farm | bỏ săn | về nhà | ngủ ở | cá | mở lối</size>");
            foreach (var d in _history)
                sb.AppendLine($"<size=13>{d.day} | {d.FarmProductsUsed} | {d.DropsSpentHome} | {(d.Linked ? "có" : "-")} | {(d.FarmSkipped ? "X" : "")} | {(d.HuntSkipped ? "X" : "")} | {(d.WasHome ? "có" : "-")} | {d.sleptAt} | {d.fishCaught} | {string.Join(",", d.unlocks)}</size>");
            GUI.Label(new Rect(r.x + 10, r.y + 6, w - 20, h), sb.ToString(), _style);
        }
    }
}
