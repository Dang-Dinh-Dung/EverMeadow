// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do the farm half and the outing half still feed each other when the world is a set of free, daily-regenerated biome maps joined by mid-map branches you must unlock?
// Date: 2026-10-30
//
// Everything the player can stand next to and press E on, plus things hit
// with the weapon. Each cross-half link in the hypothesis lives here:
//   Plot        -- farm half; BONE from the wilds = fertilizer (+1 growth stage)
//   Cauldron    -- crops -> POTION / MEAL; FISH -> GRILLED FISH (round 3)
//   Forge       -- ORE (+ BOSS CORE from tier 3) = weapon upgrade, fast vs strong
//   Market      -- crops and fish -> GOLD -> transport tickets
//   Transporter -- fast travel between villages already reached, costs a ticket
//   BranchGate  -- mid-map side entrance to another map; opened by boss, key,
//                  fished item or by breaking a cracked wall (round 3)
//   FishingSpot -- shore of a river / lake / sea / pond; starts the fishing minigame
//   Chest       -- loot in hand-made landmarks; refills with the daily map
//   Bed         -- ends the day; home bed or the far village's inn
// Map objects are rebuilt every time a map loads, so anything that must
// survive leaving the map (plots, branches, kills) lives in DayLoopGame.

using UnityEngine;

namespace DayLoopProto
{
    public abstract class Station : MonoBehaviour
    {
        public string displayName;
        public abstract string Prompt { get; }
        public abstract void Interact();
        // Non-null while this station's menu is open.
        public virtual string MenuText => null;
        public virtual void MenuKey(int number) { }
        public virtual void CloseMenu() { }
        protected DayLoopGame Game => DayLoopGame.I;
    }

    public class PlotState
    {
        public Item? crop;          // null = empty
        public int stage;           // 0..growDays; ripe at growDays
        public bool fertilizedToday;
    }

    public class Plot : Station
    {
        public PlotState state;
        bool _menu;
        SpriteRenderer _plant;

        void Awake()
        {
            // Parent to the map root (not to this scaled transform) so it dies with the map.
            _plant = Visuals.Box("Plant", transform.position, new Vector2(0.5f, 0.1f), Color.clear, 3, transform.parent).GetComponent<SpriteRenderer>();
        }

        void Start() => Refresh();

        bool Ripe => state.crop != null && state.stage >= Game.growDays;

        public override string Prompt
        {
            get
            {
                if (state.crop == null) return "Gieo hạt";
                string name = Game.ItemName(state.crop.Value);
                if (Ripe) return $"Thu hoạch {name}";
                if (state.fertilizedToday) return $"{name} đang lớn ({state.stage}/{Game.growDays}) — đã bón hôm nay";
                return $"{name} đang lớn ({state.stage}/{Game.growDays}) — bón phân (1 Xương)";
            }
        }

        public override void Interact()
        {
            if (state.crop == null) { _menu = true; Game.OpenMenu = this; return; }

            if (Ripe)
            {
                Game.Add(state.crop.Value, Game.yieldPerHarvest);
                Game.Toast($"+{Game.yieldPerHarvest} {Game.ItemName(state.crop.Value)}");
                Game.LogFarmAction("harvest");
                state.crop = null;
                state.stage = 0;
                Refresh();
                return;
            }

            if (state.fertilizedToday) { Game.Toast("Ô này đã bón phân hôm nay"); return; }
            if (!Game.Spend(Item.Bone, 1)) { Game.Toast("Cần Xương (rơi từ quái) để bón phân"); return; }
            state.stage++;
            state.fertilizedToday = true;
            Game.Toast("Bón phân: cây lớn thêm một bậc");
            Game.LogDropSpentAtHome("fertilizer");
            Refresh();
        }

        public override string MenuText => _menu
            ? "GIEO HẠT\n[1] Healroot (→ potion hồi máu, bán 3 vàng)\n[2] Emberpepper (→ món ăn tăng sát thương, bán 4 vàng)\n[E/Esc] Đóng"
            : null;

        public override void MenuKey(int n)
        {
            if (n != 1 && n != 2) return;
            state.crop = n == 1 ? Item.Healroot : Item.Emberpepper;
            state.stage = 0;
            _menu = false;
            Game.OpenMenu = null;
            Game.LogFarmAction("plant");
            Refresh();
        }

        public override void CloseMenu() => _menu = false;

        void Refresh()
        {
            if (state.crop == null) { _plant.color = Color.clear; return; }
            Color c = state.crop == Item.Healroot ? new Color(0.3f, 0.9f, 0.5f) : new Color(1f, 0.45f, 0.15f);
            float t = (state.stage + 1f) / (Game.growDays + 1f);
            _plant.transform.localScale = new Vector3(Ripe ? 0.7f : 0.4f, 0.2f + 1.0f * t, 1f);
            _plant.transform.position = transform.position + new Vector3(0f, 0.15f + 0.5f * _plant.transform.localScale.y, 0f);
            _plant.color = Ripe ? c : Color.Lerp(new Color(0.4f, 0.55f, 0.3f), c, 0.4f);
        }
    }

    public class Cauldron : Station
    {
        bool _menu;
        public override string Prompt => "Nồi nấu";
        public override void Interact() { _menu = true; Game.OpenMenu = this; }

        public override string MenuText => _menu
            ? $"NỒI NẤU\n[1] Potion hồi máu (+{Game.potionHeal} HP)  — tốn 1 Healroot (có {Game.Count(Item.Healroot)})\n" +
              $"[2] Món ăn cay (sát thương ×{Game.mealDamageMult:0.#} tới khi ngủ)  — tốn 1 Emberpepper (có {Game.Count(Item.Emberpepper)})\n" +
              $"[3] Cá nướng (+{Game.grilledFishHeal} HP)  — tốn 1 cá (cá thường {Game.Count(Item.Fish)}, cá to {Game.Count(Item.BigFish)} → 2 phần)\n[E/Esc] Đóng"
            : null;

        public override void MenuKey(int n)
        {
            if (n == 1)
            {
                if (!Game.Spend(Item.Healroot, 1)) { Game.Toast("Thiếu Healroot"); return; }
                Game.Add(Item.Potion, 1);
                Game.Toast("+1 Potion");
                Game.LogFarmAction("brew");
            }
            else if (n == 2)
            {
                if (!Game.Spend(Item.Emberpepper, 1)) { Game.Toast("Thiếu Emberpepper"); return; }
                Game.Add(Item.Meal, 1);
                Game.Toast("+1 Món ăn cay");
                Game.LogFarmAction("cook");
            }
            else if (n == 3)
            {
                int portions = Game.Spend(Item.Fish, 1) ? 1 : Game.Spend(Item.BigFish, 1) ? 2 : 0;
                if (portions == 0) { Game.Toast("Không có cá — câu ở sông, hồ, biển"); return; }
                Game.Add(Item.GrilledFish, portions);
                Game.LogFishCooked();
                Game.Toast($"+{portions} Cá nướng");
            }
        }

        public override void CloseMenu() => _menu = false;
    }

    public class Forge : Station
    {
        bool _menu;
        public override string Prompt => Game.WeaponTier >= Game.MaxTier ? "Lò rèn (vũ khí đã tối đa)" : "Lò rèn";

        public override void Interact()
        {
            if (Game.WeaponTier >= Game.MaxTier) { Game.Toast("Vũ khí đã ở tier cao nhất của prototype"); return; }
            _menu = true;
            Game.OpenMenu = this;
        }

        string CostText()
        {
            string s = $"{Game.NextTierOre} Quặng (có {Game.Count(Item.Ore)})";
            if (Game.NextTierCore > 0) s += $" + {Game.NextTierCore} Lõi Boss (có {Game.Count(Item.BossCore)})";
            return s;
        }

        public override string MenuText => !_menu ? null :
            $"LÒ RÈN — vũ khí tier {Game.WeaponTier} (sát thương {Game.WeaponDamage}, nhịp ×{Game.WeaponSpeedMult:0.00})\n" +
            $"Lên tier {Game.WeaponTier + 1}: tốn {CostText()}\n" +
            "[1] Nhánh NHANH: sát thương +1, đòn nhanh hơn (nhịp ×0.8)\n" +
            "[2] Nhánh MẠNH: sát thương +2, đòn chậm hơn (nhịp ×1.1)\n[E/Esc] Đóng";

        public override void MenuKey(int n)
        {
            if (n != 1 && n != 2) return;
            if (Game.Count(Item.Ore) < Game.NextTierOre) { Game.Toast("Thiếu Quặng — có ở mỏ, rương và quái mạnh"); return; }
            if (Game.Count(Item.BossCore) < Game.NextTierCore) { Game.Toast("Thiếu Lõi Boss — hạ boss của một vùng"); return; }
            Game.Spend(Item.Ore, Game.NextTierOre);
            Game.Spend(Item.BossCore, Game.NextTierCore);
            Game.UpgradeWeapon(n == 1);
            Game.LogDropSpentAtHome("forge");
            _menu = false;
            Game.OpenMenu = null;
        }

        public override void CloseMenu() => _menu = false;
    }

    public class Market : Station
    {
        bool _menu;
        public override string Prompt => "Quầy chợ (bán nông sản, cá)";
        public override void Interact() { _menu = true; Game.OpenMenu = this; }

        public override string MenuText => _menu
            ? $"QUẦY CHỢ — bạn có {Game.Count(Item.Gold)} vàng\n" +
              $"[1] Bán 1 Healroot: +{Game.priceHealroot} vàng (có {Game.Count(Item.Healroot)})\n" +
              $"[2] Bán 1 Emberpepper: +{Game.priceEmber} vàng (có {Game.Count(Item.Emberpepper)})\n" +
              $"[3] Bán 1 cá: thường +{Game.priceFish}, to +{Game.priceBigFish} vàng (có {Game.Count(Item.Fish)} / {Game.Count(Item.BigFish)})\n" +
              $"Vé dịch chuyển giữa hai làng: {Game.ticketCost} vàng\n[E/Esc] Đóng"
            : null;

        public override void MenuKey(int n)
        {
            if (n == 3)
            {
                int price = Game.Spend(Item.Fish, 1) ? Game.priceFish : Game.Spend(Item.BigFish, 1) ? Game.priceBigFish : 0;
                if (price == 0) { Game.Toast("Không có cá để bán"); return; }
                Game.Add(Item.Gold, price);
                Game.LogFishSold();
                Game.Toast($"Bán 1 cá: +{price} vàng");
                return;
            }
            if (n != 1 && n != 2) return;
            Item it = n == 1 ? Item.Healroot : Item.Emberpepper;
            if (!Game.Spend(it, 1)) { Game.Toast($"Không có {Game.ItemName(it)}"); return; }
            int p = n == 1 ? Game.priceHealroot : Game.priceEmber;
            Game.Add(Item.Gold, p);
            Game.LogCropSold(p);
            Game.Toast($"Bán 1 {Game.ItemName(it)}: +{p} vàng");
        }

        public override void CloseMenu() => _menu = false;
    }

    public class Transporter : Station
    {
        bool _menu;
        string[] _dest = new string[0];

        public override string Prompt => "Người vận chuyển (dịch chuyển nhanh)";

        public override void Interact()
        {
            _dest = Game.FastTravelTargets();
            if (_dest.Length == 0) { Game.Toast("Chưa tới làng nào khác — lần đầu phải đi bộ"); return; }
            _menu = true;
            Game.OpenMenu = this;
        }

        public override string MenuText
        {
            get
            {
                if (!_menu) return null;
                var s = $"NGƯỜI VẬN CHUYỂN — vé {Game.ticketCost} vàng (bạn có {Game.Count(Item.Gold)})\n";
                for (int i = 0; i < _dest.Length && i < 3; i++) s += $"[{i + 1}] Tới {World.Maps[_dest[i]].name}\n";
                return s + "[E/Esc] Đóng";
            }
        }

        public override void MenuKey(int n)
        {
            if (n < 1 || n > _dest.Length) return;
            if (!Game.Spend(Item.Gold, Game.ticketCost)) { Game.Toast($"Cần {Game.ticketCost} vàng — bán nông sản, cá ở quầy chợ hoặc săn quái"); return; }
            string target = _dest[n - 1];
            _menu = false;
            Game.OpenMenu = null;
            Game.FastTravel(target);
        }

        public override void CloseMenu() => _menu = false;
    }

    public class BranchGate : Station
    {
        public EdgeDef edge;
        SpriteRenderer _sr;
        bool _shownOpen;

        public void Init(EdgeDef e)
        {
            edge = e;
            _sr = GetComponent<SpriteRenderer>();
            _sr.sortingOrder = -1;   // behind the player
            Refresh();
        }

        public string Target => edge.Other(Game.CurrentMap);
        public override string Prompt => Game.BranchPrompt(edge);
        public override void Interact() => Game.UseBranch(edge);

        void Update()
        {
            if (Game != null && Game.BranchOpen(edge) != _shownOpen) Refresh();
        }

        void Refresh()
        {
            _shownOpen = Game.BranchOpen(edge);
            if (_shownOpen) { _sr.color = new Color(0.1f, 0.08f, 0.12f); return; }
            switch (edge.unlock)
            {
                case Unlock.CrackedWall: _sr.color = new Color(0.55f, 0.5f, 0.45f); break;
                case Unlock.Key: _sr.color = new Color(0.6f, 0.45f, 0.15f); break;
                case Unlock.FishItem: _sr.color = new Color(0.35f, 0.5f, 0.55f); break;
                default: _sr.color = new Color(0.5f, 0.15f, 0.15f); break;
            }
        }
    }

    // The cracked wall IS the closed branch: hit it with a strong enough weapon.
    public class CrackedWall : MonoBehaviour, IHittable
    {
        public BranchGate gate;
        public void TakeHit(int damage, Vector2 from) => DayLoopGame.I.HitWall(gate.edge);
    }

    public class FishingSpot : Station
    {
        public WaterKind water;
        public override string Prompt => $"Câu cá ({MapGen.WaterName(water)})";
        public override void Interact() => Game.StartFishing(water);
    }

    public class Chest : Station
    {
        string _key, _where;
        Loot _loot;
        SpriteRenderer _sr;

        public void Init(string key, Loot loot, string where)
        {
            _key = key; _loot = loot; _where = where;
            _sr = GetComponent<SpriteRenderer>();
            if (Game.OpenedChests.Contains(_key)) _sr.color = new Color(0.35f, 0.3f, 0.2f);
        }

        public override string Prompt => Game.OpenedChests.Contains(_key) ? $"Rương ({_where}) — đã mở" : $"Mở rương ({_where})";

        public override void Interact()
        {
            if (Game.OpenedChests.Contains(_key)) { Game.Toast("Rương trống"); return; }
            Game.OpenChest(_key, _loot);
            _sr.color = new Color(0.35f, 0.3f, 0.2f);
        }
    }

    public class Bed : Station
    {
        public bool isInn;
        public override string Prompt => Game.CanSleep
            ? (isInn ? "Ngủ ở quán trọ (sang ngày mới — cây ở nhà vẫn lớn)" : "Đi ngủ (sang ngày mới)")
            : $"Giường — ngủ được từ {DayLoopGame.FormatClock(Game.earliestSleepMinute)}";

        public override void Interact()
        {
            if (!Game.CanSleep) { Game.Toast($"Còn sớm — ngủ được từ {DayLoopGame.FormatClock(Game.earliestSleepMinute)}"); return; }
            Game.Sleep(DayLoopGame.DayEnd.Slept);
        }
    }

    public class OreNode : MonoBehaviour, IHittable
    {
        string _key;
        int _yield = 1;
        int _hits;
        bool _depleted;
        SpriteRenderer _sr;
        const int HitsToBreak = 3;
        readonly Color _full = new Color(0.6f, 0.6f, 0.75f);
        readonly Color _empty = new Color(0.2f, 0.2f, 0.22f);

        public void Init(string key, int yield, bool mined)
        {
            _key = key;
            _yield = yield;
            _depleted = mined;
            _sr = GetComponent<SpriteRenderer>();
            _sr.color = mined ? _empty : _full;
        }

        public void TakeHit(int damage, Vector2 from)
        {
            if (_depleted) return;
            _hits++;
            _sr.color = Color.Lerp(_full, Color.white, 0.6f);
            Invoke(nameof(Unflash), 0.08f);
            if (_hits < HitsToBreak) return;
            _depleted = true;
            DayLoopGame.I.OnOreMined(_key, _yield);
        }

        void Unflash() => _sr.color = _depleted ? _empty : _full;
    }
}
