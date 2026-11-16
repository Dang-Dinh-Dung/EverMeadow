# Systems Index: Evermeadow

> **Status**: Draft
> **Created**: 2026-10-25
> **Last Updated**: 2026-10-25
> **Source Concept**: design/gdd/game-concept.md (APPROVED 2026-10-25, lean re-review)
> **Gates**: TD-SYSTEM-BOUNDARY: CONCERNS (đã áp dụng A1–A7; hợp đồng B1–B8 ghi ở Dependency Map) · PR-SCOPE: UNREALISTIC → đã áp dụng phương án B (tách MVP, gộp còn 35 hệ thống) · CD-SYSTEMS: CONCERNS → chấp nhận, C1–C6 ghi dưới Priority Tiers

> Ghi chú ngôn ngữ: tiêu đề section giữ tiếng Anh theo template; nội dung tiếng Việt, tên hệ thống tiếng Anh.

---

## Overview

Evermeadow cần hai nửa hệ thống chạy trên một nền chung. **Nửa phiêu lưu** là combat platformer nhanh (Controller → Combat → Weapon Moveset), weapon tree kiểu Monster Hunter và boss nhiều phase; **nửa cozy** là farming trên một lịch luôn trôi, làng với NPC affinity, và lò rèn nối hai nửa (Pillar 3: Two Lives, One Loop). Nền chung là năm hệ Foundation không phụ thuộc gì: **Game Time & Calendar**, **Flags, Conditions & World State** (cả chiều đọc — Conditions — lẫn chiều ghi — Effects), **Game Data & Tuning**, **Save/Load** (chỉ là hợp đồng mà các hệ khác đăng ký vào) và **Input**. Hầu hết mọi luật mở khóa của game — vũ khí là khám phá (Pillar 1), luật đường chính không cần farm/affinity (Pillar 3), affinity mở gameplay thật (Pillar 5) — được biểu diễn bằng điều kiện trên flag, không bằng cạnh phụ thuộc trực tiếp giữa các hệ. Đó là lý do Flags là điểm nghẽn lớn nhất của bản đồ.

**MVP chia làm hai cổng** (quyết định 2026-10-25 sau PR-SCOPE: UNREALISTIC — 36 hệ thống không vừa 4 tháng ở bất kỳ kịch bản nào):
- **Fun-Gate Build** (mục tiêu ~tháng 6, 2027-04-25; 25 hệ thống) — đo H1 (vòng lặp ngày) và H2 (khám phá vũ khí thứ hai) của cổng vui. **Vũ khí thứ hai đến từ một bí mật của vùng hoặc từ NPC, không từ boss**, nên H2 đo được khi chưa có boss.
- **MVP-Complete** (mục tiêu ~tháng 8, 2027-06-25; +3 hệ thống) — thêm Boss Encounter (1 boss 2 phase), Skill Stone (2–3 stone), Quest & Hidden Quest (1 hidden quest mở nhánh bí mật). Kiểm Pillar 4 và Pillar 5.
- Cả hai giai đoạn dùng chung Priority = `MVP` trong bảng dưới (`/gate-check` đếm GDD theo nhãn này); giai đoạn ghi trong ngoặc ở tên hệ.
- **Mốc kiểm tốc độ 2026-12-06 (tuần 6):** controller, combat và pipeline vũ khí 1 chạy trong code production. Nếu tốc độ thực > 1,5× ước tính của producer → chạy lại PR-SCOPE và khôi phục phạm vi.

### Lý do từng nhóm hệ thống (docs.density: thorough)

**Foundation.**
- **Game Time & Calendar** — "lịch luôn trôi" là hợp đồng của cả game (Core Loop › Calendar Contract) và là nguồn của luật "thua boss không hỏng nông trại". Sai ở đây thì mọi hệ có thời gian (farming, processing, schedules, respawn) sai theo. Fun-Gate chỉ cần đồng hồ, số ngày, cờ đêm, ngủ = sang ngày + lưu; tạm dừng thời gian trong arena đến cùng boss; mùa ở VS; mô phỏng ngoài màn hình sau.
- **Flags, Conditions & World State** — gộp Condition/Flag, World State và Boss Progress & Region Gating (PR-SCOPE). Boss progress chỉ là một flag; trần tier toàn cục (Pillar 2) và vùng mở (Pillar 3) là điều kiện trên flag đó. Có bộ **Effect** đóng (SetFlag, Give/TakeItem, AffinityDelta, GiveCurrency, StartQuest) để Dialogue/Quest ghi trạng thái mà không phụ thuộc ngược vào Affinity/Inventory (TD-A4).
- **Game Data & Tuning** — luật "giá trị gameplay phải data-driven" của coding standards; ScriptableObject đơn giản ở MVP. Chỉ là registry + lookup; mỗi hệ sở hữu schema riêng.
- **Save/Load** — "ngủ = lưu" là điểm dừng tự nhiên của phiên (Core Loop › Session-Level); schema có version từ bản save đầu tiên (Technical Risks).
- **Input** — action maps của Input System, buffer có timestamp cho combat; rebinding để VS.

**Core.**
- **Character Controller** — sở hữu **state machine hành động duy nhất** (đất, không, lướt, hitstun, slot tấn công) và phân xử cancel sang né (TD-A3, quyết định 2026-10-25). Không biết họ vũ khí. Đây là nơi cảm giác "ngang hoặc nhanh hơn Hollow Knight" được quyết.
- **Combat** — phân giải trúng đòn: hitbox/hurtbox, pipeline sát thương, hit-stop, áp hitstun, i-frame. Đọc stat block do Loadout tính, không phụ thuộc lên trên.
- **Camera** — pixel-perfect 640×360 (art bible), parallax, sương; timebox 4 ngày ở MVP, quá hạn thì tạm bỏ pixel-perfect.
- **Region Loading** — nạp vùng (MVP chỉ một vùng + hub). Tách khỏi World State theo TD-B5.
- **Interaction & Context Prompts** — một lớp "tương tác" chung cho NPC, cây, trạm, cửa; ở MVP gánh luôn phần onboarding tối thiểu (prompt ngữ cảnh + thẻ điều khiển giấy cho người test).
- **Item & Inventory** — taxonomy vật phẩm (nguyên liệu, tiêu hao, vật phẩm khóa, skill stone; vũ khí là item hay entry của Loadout — TD-D quyết trong GDD).
- **Telemetry** — log cục bộ (số ngày chơi, số chuyến, lúc bỏ) mà cổng vui bắt buộc; phải chạy thử trước buổi test đầu tiên. Thuộc Core, không Polish (TD-A1).

**Feature — nửa phiêu lưu.**
- **Skill & Moveset** *(trước là Weapon Moveset; GDD `skill-moveset.md`)* — mỗi đòn là một kỹ năng XML xuất từ đồ thị node (Start → End, sự kiện có độ trễ giây), bảng tổ hợp nút theo họ vũ khí, kỹ năng đặc biệt từ Skill Stone, node chuyển động có giới hạn do Controller thực thi. Mô tả gốc: chuỗi trên đất riêng mỗi họ, đòn không/lướt/đáp dùng chung + một đòn đặc trưng mỗi họ. Đây là nơi Pillar 1 và 2 thành cảm giác tay: mỗi họ đổi tầm, nhịp và cách tiếp cận.
- **Weapon Tree** — họ, tier, nhánh sidegrade, nhánh bí mật, trần tier toàn cục, chi phí bắt kịp k. Định nghĩa + luật khả dụng + node đã sở hữu; **không đọc Inventory** (TD-A6) — trả nguyên liệu là việc của Crafting. Fun-Gate: 2 vũ khí × 2 nhánh, chưa có nhánh bí mật.
- **Loadout & Player Stats** — chủ duy nhất của vũ khí đang trang bị, socket skill stone, trang bị và stat block cuối (TD-A2). Fun-Gate: đổi vũ khí + HP.
- **Skill Stone** *(MVP-Complete)* — định nghĩa và hành vi kỹ năng chủ động; socket thuộc Loadout.
- **Enemy AI, Spawning & Drops** — gộp Loot vào dữ liệu quái (PR-SCOPE); sở hữu bảng spawn, quái mạnh hơn về đêm (đọc Game Time, TD-A7). Respawn theo vùng lưu ở World State.
- **Boss Encounter** *(MVP-Complete)* — nhiều phase, arena riêng, màu hazard dành riêng. Luật cửa sổ dễ tổn thương ≥ 1,25× thời gian ra đòn của họ chậm nhất là **ràng buộc kiểm bằng test/validator lúc authoring**, không phải phụ thuộc runtime vào Moveset (TD-A5). Đây là chỗ Pillar 4 (Fewer Bosses, Each an Event) và luật P2↔P4 "đổi boss, không nerf vũ khí" gặp nhau.
- **Defeat & Retry** — Fun-Gate: stub "thua → tỉnh dậy ở nhà, **giờ giữ nguyên**, không chuyển ngày, không save; đồ đã cất, vũ khí và trang bị không bao giờ rơi, còn **nguyên liệu chưa cất rơi thành một túi tại chỗ thua, nhặt lại được, không hết hạn theo thời gian**" (sửa 2026-10-25 theo GDD Game Time Rule 18 — bản "sáng hôm sau" ban đầu đốt phần còn lại của ngày, vi phạm "không mất ngày"; túi đồ rơi thêm 2026-10-26 theo D-2, ràng buộc N3–N4 ở Game Time › Dependencies). Cơ chế arena (đóng băng giờ, checkpoint tạm trong bộ nhớ — TD-B2) đến cùng boss.

**Feature — nửa cozy và làng.**
- **Farming** — 4 cây ở MVP; cây không héo (Calendar Contract). Vụ mùa → potion là nửa "cây" của hook "mỗi vụ mùa góp vào lò rèn".
- **Crafting & Forge** — lò rèn + trạm potion gộp thành một UI có tab công thức (PR-SCOPE); thực thi giao dịch nâng cấp (tiêu từ Inventory rồi gọi Weapon Tree). Processing theo thời gian để sau.
- **NPC Affinity** — chỉ chiều tăng ở MVP (tặng quà là nhịp "trưa: vào làng"); affinity âm + hàn gắn ở EA theo concept. Cung cấp giá trị qua Conditions, nhận thay đổi qua Effect `AffinityDelta`.
- **Dialogue & Discovery Hints** — gộp Dialogue, Dialogue UI và Discovery & Hint (PR-SCOPE): hội thoại tuyến tính có điều kiện, dòng tin đồn + một biển hiệu trong thế giới làm lớp gợi ý. **Nội dung gợi ý không được cắt** — nó gánh H2 và là bắt buộc theo ngân sách khám phá của Pillar 1. Nhật ký đầy đủ lên VS.
- **Quest & Hidden Quest** *(MVP-Complete)* — MVP là một chuỗi flag (một hidden quest), không phải hệ quest tổng quát.

**Feature — VS/Alpha.** Currency & Shops (affinity thấp làm đắt hơn — Pillar 5, TD-A7), NPC Schedules, Cutscene & Scripted Sequence (thay cho "Premise & Lore Delivery": intro boss "each an event" cần cơ chế, lore là nội dung để narrative docs — TD-B6), Elemental & Status (nhánh nguyên tố ở EA theo tier table của concept; đăng ký modifier vào Controller — TD-A7).

**Presentation và Polish.** Menu & Navigation giữ mức M vì điều hướng tay cầm qua menu kho/rèn dày đặc là phần concept tự nhận "hay bị đánh giá thấp". Inventory, Equipment & Forge UI là một màn hình danh sách có tab, **phải hiện "còn thiếu 1 nguyên liệu"** — móc "thêm một chuyến" của Core Loop. HUD gánh luôn đồng hồ/ngày (gộp Farm & Calendar UI). Audio ở MVP vì trọng lượng đòn đánh đến một phần từ âm thanh (Core Loop › Moment-to-Moment). Settings & Accessibility (rebinding, tùy chọn độ khó, tùy chọn truy cập; mỗi GDD Presentation tự mang yêu cầu accessibility của nó — TD-A1), Atmosphere & Lighting (HD-2D, sương — art bible) và Tutorial & Onboarding đầy đủ ở VS.

### Phân tích quan hệ

- **Điểm nghẽn** (nhiều hệ phụ thuộc): Flags, Conditions & World State (~14), Game Time & Calendar (~12), Game Data & Tuning (~12), Item & Inventory (~8), Combat (~8), Save/Load (~8), Character Controller (~6), Input, Menu & Navigation. Bốn hệ đầu là bốn GDD đầu tiên — TD nhận định Game Time, Flags, Game Data và Inventory là các điểm nghẽn đang bị đặc tả thiếu.
- **"Quan sát" chứ không "gọi"**: Telemetry, Audio, HUD, Loot (trong Enemy AI), Tutorial nhận sự kiện gameplay; mục Dependencies trong mỗi GDD phải đánh dấu từng cạnh là *calls* hay *observes* (TD-C). Cần một ADR messaging trước kiến trúc.
- **Lá** (an toàn để thiết kế muộn): Tutorial & Onboarding, Atmosphere & Lighting, Audio, Settings & Accessibility, Currency & Shops, Cutscene, Elemental & Status.

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | Input (inferred) | Core | MVP | Designed | design/gdd/input.md | — |
| 2 | Game Time & Calendar | Core | MVP | Approved | design/gdd/game-time-calendar.md | Game Data & Tuning Config (tuning constants only) |
| 3 | Flags, Conditions & World State | Core | MVP | Not Started | — | — (providers & effect handlers register) |
| 4 | Game Data & Tuning Config (inferred) | Core | MVP | Not Started | — | — |
| 5 | Save/Load | Persistence | MVP | Not Started | — | — (saveables register) |
| 6 | Character Controller | Core | MVP | Designed (sửa 2026-11-14: đòn đánh rời Controller, thành skill) | design/gdd/character-controller.md | Input |
| 7 | Camera | Core | MVP | Not Started | — | Character Controller, Region Loading |
| 8 | Region Loading (inferred) | Core | MVP | Not Started | — | Game Data & Tuning Config, Input (mode `Locked`), Character Controller (Teleport) |
| 9 | Combat | Gameplay | MVP | Not Started | — | Character Controller, Game Data & Tuning Config |
| 10 | Interaction & Context Prompts (inferred) | Gameplay | MVP | Not Started | — | Input, Character Controller, Flags, Conditions & World State |
| 11 | Item & Inventory (inferred) | Economy | MVP | Not Started | — | Game Data & Tuning Config, Save/Load |
| 12 | Telemetry | Meta | MVP | Not Started | — | Game Time & Calendar (observes gameplay events), Input (observes), Character Controller (observes) |
| 13 | Settings & Accessibility (inferred) | Persistence | Vertical Slice | Not Started | — | Input, Save/Load, Menu & Navigation |
| 14 | Skill & Moveset (gộp Weapon Moveset + phần định nghĩa kỹ năng của Skill Stone, 2026-11-05) | Gameplay | MVP | Designed | design/gdd/skill-moveset.md | Combat, Character Controller, Input, Game Data & Tuning Config, Loadout & Player Stats |
| 15 | Weapon Tree | Progression | MVP | Not Started | — | Skill & Moveset (bảng tổ hợp, `timeline_scale` theo nhánh), Flags, Conditions & World State, Game Data & Tuning Config |
| 16 | Loadout & Player Stats (inferred) | Progression | MVP | Not Started | — | Game Data & Tuning Config, Item & Inventory, Weapon Tree, Skill Stone |
| 17 | Skill Stone (MVP-Complete) | Gameplay | MVP | Not Started | — | Combat, Skill & Moveset (**từ 2026-11-13: mỗi đá trỏ tới một moveset id; mọi đòn đến từ đá khảm, vũ khí 2–4 ô** — `skill-moveset.md` Rule 35–44), Game Data & Tuning Config |
| 18 | Enemy AI, Spawning & Drops (inferred) | Gameplay | MVP | Not Started | — | Combat, Character Controller, Skill & Moveset (kỹ năng `owner: creature`, 2026-11-07), Flags, Conditions & World State, Game Time & Calendar, Item & Inventory |
| 19 | Boss Encounter (MVP-Complete) | Gameplay | MVP | Not Started | — | Combat, Enemy AI, Spawning & Drops, Camera, Game Time & Calendar, Flags, Conditions & World State (Weapon Moveset: test-time only) |
| 20 | Defeat & Retry (inferred) | Gameplay | MVP | Not Started | — | Game Time & Calendar, Flags, Conditions & World State, Combat, Save/Load, Character Controller (Respawn, EnterDefeated) |
| 21 | Farming | Gameplay | MVP | Not Started | — | Game Time & Calendar, Interaction & Context Prompts, Item & Inventory, Save/Load |
| 22 | Crafting & Forge | Economy | MVP | Not Started | — | Item & Inventory, Weapon Tree, Game Data & Tuning Config, Game Time & Calendar |
| 23 | NPC Affinity | Progression | MVP | Not Started | — | Flags, Conditions & World State, Interaction & Context Prompts, Item & Inventory, Save/Load |
| 24 | Dialogue & Discovery Hints | Narrative | MVP | Not Started | — | Flags, Conditions & World State, Interaction & Context Prompts, Game Data & Tuning Config, Input (mode `Dialogue`) |
| 25 | Quest & Hidden Quest (MVP-Complete) | Narrative | MVP | Not Started | — | Dialogue & Discovery Hints, Flags, Conditions & World State, Save/Load |
| 26 | Elemental & Status | Gameplay | Alpha | Not Started | — | Combat, Character Controller, Game Data & Tuning Config |
| 27 | Currency & Shops (inferred) | Economy | Vertical Slice | Not Started | — | Item & Inventory, Game Data & Tuning Config, Game Time & Calendar, NPC Affinity, NPC Schedules |
| 28 | NPC Schedules (inferred) | Narrative | Vertical Slice | Not Started | — | Game Time & Calendar, Region Loading, NPC Affinity |
| 29 | Cutscene & Scripted Sequence (inferred) | Narrative | Vertical Slice | Not Started | — | Game Time & Calendar, Camera, Dialogue & Discovery Hints, Flags, Conditions & World State, Input (mode `Locked`) |
| 30 | HUD (inferred) | UI | MVP | Not Started | — | Combat, Game Time & Calendar, Input (glyphs) (Boss Encounter, Skill Stone at MVP-Complete; Elemental & Status at Alpha) |
| 31 | Menu & Navigation Framework (inferred) | UI | MVP | Not Started | — | Input |
| 32 | Inventory, Equipment & Forge UI (inferred) | UI | MVP | Not Started | — | Menu & Navigation Framework, Item & Inventory, Loadout & Player Stats, Weapon Tree, Crafting & Forge |
| 33 | Audio | Audio | MVP | Not Started | — | Game Time & Calendar, Region Loading, Combat (observes), Character Controller (observes) |
| 34 | Atmosphere & Lighting (inferred) | Core | Vertical Slice | Not Started | — | Camera, Game Time & Calendar, Region Loading, Boss Encounter |
| 35 | Tutorial & Onboarding (inferred) | Meta | Vertical Slice | Not Started | — | Flags, Conditions & World State, HUD, Interaction & Context Prompts |

**Gộp/tách so với bản liệt kê 40 hệ (2026-10-25):** Telemetry & Accessibility tách (TD-A1; Accessibility vào #13); World State & Region Management tách (TD-B5) rồi World State gộp với Condition/Flag và Boss Progress & Region Gating thành #3 (PR-SCOPE); thêm Loadout & Player Stats (TD-A2); Premise & Lore Delivery → Cutscene & Scripted Sequence (TD-B6); Loot & Drops vào #18; Dialogue + Dialogue & Journal UI + Discovery & Hint → #24; Farm & Calendar UI vào #30; Inventory UI + Forge & Weapon Tree UI → #32; onboarding tối thiểu vào #10 (Tutorial đầy đủ #35 ở VS). Steam/Epic SDK và achievements không phải hệ thiết kế — việc nền tảng của 1.0, theo dõi ở release checklist.

---

## Categories

| Category | Description | Systems in Evermeadow |
|----------|-------------|-----------------|
| **Core** | Foundation systems everything depends on | Input, Game Time & Calendar, Flags, Conditions & World State, Game Data & Tuning Config, Character Controller, Camera, Region Loading, Atmosphere & Lighting (rendering domain — không có category riêng) |
| **Gameplay** | The systems that make the game fun | Combat, Interaction & Context Prompts, Weapon Moveset, Skill Stone, Enemy AI, Spawning & Drops, Boss Encounter, Defeat & Retry, Farming, Elemental & Status |
| **Progression** | How the player grows over time | Weapon Tree, Loadout & Player Stats, NPC Affinity |
| **Economy** | Resource creation and consumption | Item & Inventory, Crafting & Forge, Currency & Shops |
| **Persistence** | Save state and continuity | Save/Load, Settings & Accessibility |
| **UI** | Player-facing information displays | HUD, Menu & Navigation Framework, Inventory, Equipment & Forge UI |
| **Audio** | Sound and music systems | Audio |
| **Narrative** | Story and dialogue delivery | Dialogue & Discovery Hints, Quest & Hidden Quest, NPC Schedules, Cutscene & Scripted Sequence |
| **Meta** | Systems outside the core game loop | Telemetry, Tutorial & Onboarding |

---

## Priority Tiers

| Tier | Definition | Target Milestone | Design Urgency |
|------|------------|------------------|----------------|
| **MVP** | Required for the core loop to function. Without these, you can't test "is this fun?" Chia hai giai đoạn: **Fun-Gate Build** (25 hệ) và **MVP-Complete** (+3 hệ, đánh dấu "(MVP-Complete)" ở tên). | Fun-Gate Build ~2027-04-25 (tháng 6); MVP-Complete ~2027-06-25 (tháng 8) | Design FIRST |
| **Vertical Slice** | Required for one complete, polished area. Demonstrates the full experience. Ở dự án này tương ứng tier Free Demo của concept. | Free Demo ~tháng 17–19 (Next Fest tháng 6/2028) | Design SECOND |
| **Alpha** | All features present in rough form. Complete mechanical scope, placeholder content OK. | Early Access (tùy chọn) | Design THIRD |
| **Full Vision** | Polish, edge cases, nice-to-haves, and content-complete features. Chưa có hệ thống riêng — Full Vision của concept là tăng nội dung và lớp polish "Breathing Meadow". | 1.0 ~tháng 30–36 (đặt lại 2026-10-25) | Design as needed |

> **Creative Director Note** (CD-SYSTEMS: CONCERNS, 2026-10-25 — người dùng chấp nhận, ghi tại đây):
>
> - **(MVP — Fun-Gate) C1 · Tình bạn phải nuôi lò rèn.** Ít nhất một ngưỡng affinity mở một phần thưởng gameplay thật qua Conditions/Effects. **Quyết định:** vũ khí thứ hai là **bí mật của vùng**, và một **ngưỡng lòng tin NPC thấp (đạt được ngày 2–3) làm gợi ý sâu hơn**; biển hiệu/tin đồn trong thế giới là đường thay thế (Pillar 3 › Luật đường chính). Một nhịp test cả Pillar 1 và Pillar 5. Chủ: NPC Affinity, Dialogue & Discovery Hints.
> - **(MVP — Fun-Gate) C2 · "Muốn tìm thêm" phải có hệ gánh.** Inventory, Equipment & Forge UI hiện họ vũ khí chưa khám phá dưới dạng silhouette khóa / ô "???"; HUD + Audio có khoảnh khắc reveal tối thiểu khi tìm thấy loại vũ khí mới (banner, sting, ô cây được lấp); Dialogue & Discovery Hints có ít nhất một tin đồn chỉ xa hơn vũ khí 2; Telemetry log sự kiện khám phá (thời gian tới vũ khí 2, nguồn nào).
> - **(MVP — Fun-Gate) C3 · Bí mật vùng có chủ.** Interaction & Context Prompts sở hữu trigger; Flags, Conditions & World State giữ trạng thái đã tìm (giữ mở vĩnh viễn); Combat sở hữu đánh vào môi trường nếu bí mật phá được; level design sở hữu vị trí đặt.
> - **(MVP — Fun-Gate / MVP-Complete) C4 · Phụ thuộc vắt ngang tier.** Loadout & Player Stats (Fun-Gate) phụ thuộc Skill Stone (MVP-Complete): GDD Loadout định nghĩa hợp đồng socket dạng stub; GDD Skill Stone có thể viết cùng đợt, xây sau.
> - **(Alpha) C5 · Chiều giảm + hàn gắn affinity** của Pillar 5 thuộc tier Alpha (Early Access), như tier table của concept. NPC Affinity ở MVP chỉ chiều tăng.
> - **(Full Vision / 1.0) C6 · Một luật cắt giảm duy nhất (người dùng chọn 2026-10-25):** **sàn 1.0 = 10 loại vũ khí**, mỗi loại giữ nhánh bí mật riêng (~20 khám phá / ~16–19 nguồn), 10 loại phủ mọi họ animation với dải tốc độ riêng (gồm ranged và catalyst), mọi loại vẫn được tìm thấy trong thế giới (trừ vũ khí khởi đầu). Thứ tự cắt: **loại 11–14 cắt trước**; khi đã ở 10 thì cắt vùng/boss trước khi cắt loại vũ khí; dưới 10 cần creative-director ký. Không công bố "~14 vũ khí" cho tới khi biết chi phí mỗi vũ khí. Chốt chính thức ở cổng vũ khí thứ hai bằng số liệu thật. Thay luật "giữ số loại vũ khí" của concept.
> - **Điều kiện của các quyết định đã duyệt:** (D1) boss MVP-Complete phải qua một tiêu chí Pillar 4 đóng băng trước buổi test, trước khi cam kết sản xuất nội dung VS; boss không được trượt khỏi MVP-Complete. (D2) boss MVP-Complete vẫn trao thứ liên quan vũ khí (tăng trần tier hoặc nguyên liệu nhánh bí mật). (D3) hai nhánh Fun-Gate phải là sidegrade thật trên trục tốc đánh ↔ sát thương, không phải tăng stat phẳng. (D4) lịch dài hơn → soát anti-pillar chặt hơn ở mỗi mốc.
> - **Khuyến nghị (không chặn):** potion phải thật sự quan trọng trong combat Fun-Gate, hoặc cho một vật phẩm từ cây làm nguyên liệu rèn *tùy chọn* — nếu không nửa farming của hook rỗng và H1 trượt vì lý do sai. Hiện **miếu boss bị niêm phong** trong Fun-Gate như một lời hứa ở chân trời.

> **Producer Note (PR-SCOPE: UNREALISTIC, 2026-10-25)** (MVP): 36 hệ thống MVP như bản nháp ≈ 200 ngày kỳ vọng (~11–12 tháng) so với ~64 ngày lên kế hoạch được trong 4 tháng; kịch bản lạc quan cũng vượt. Đã áp dụng phương án B: gộp/hạ cấp còn 28 hệ MVP, tách Fun-Gate Build (~6 tháng, REALISTIC) và MVP-Complete (~8 tháng, REALISTIC). Ước tính chưa hiệu chỉnh theo tốc độ thực (chỉ có một prototype) — độ tin cậy Medium-Low; kiểm lại ở mốc 2026-12-06 và sau sprint 2 (±30%).
>
> **Producer Note** (Vertical Slice / Full Vision): mốc Demo 12–14 tháng và 1.0 24 tháng của concept không khả thi — đặt lại Demo ~tháng 17–19, 1.0 ~tháng 30–36 (người dùng chọn 2026-10-25). Khi thiếu thời gian, **số loại vũ khí** mới là thứ vỡ trước (không phải vùng/boss như concept giả định); producer đề xuất một **sàn vũ khí 1.0** (vd. 10, mức EA) làm van xả, quyết ở cổng vũ khí thứ hai — cần creative-director vì ảnh hưởng ngân sách khám phá của Pillar 1.

---

## Dependency Map

Hướng: mỗi hệ chỉ phụ thuộc xuống dưới. Hai hệ Foundation trông như "ngược" (Flags, Save/Load) là đảo phụ thuộc hợp lệ: hệ Feature đăng ký provider/handler/saveable vào chúng, chúng không bao giờ tham chiếu lên (TD xác nhận). Đề xuất thực thi bằng asmdef theo layer để cạnh ngược thành lỗi biên dịch (TD-C).

### Foundation Layer (no dependencies)

1. **Game Time & Calendar** — mọi hệ có thời gian đọc từ đây. **Hợp đồng TD-B1 (GDD này viết đầu tiên):** đồng hồ game riêng, tách khỏi `Time.timeScale` (hit-stop/pause không được đẩy lịch); **token tạm dừng** do các hệ yêu cầu (Boss arena, Defeat & Retry, Dialogue, Menu, Cutscene) — họ phụ thuộc Time, Time không biết họ; **timestamp tuyệt đối + tính trạng thái lười** từ thời gian đã trôi (growth, processing, restock, schedules, respawn) — không tick nội dung chưa nạp mỗi frame; **pipeline chuyển ngày có thứ tự, các phase đăng ký** (vd. growth → processing → restock → affinity → quests → **save cuối cùng**). Cũng gỡ Open Question "thua boss không hỏng nông trại" của concept.
2. **Flags, Conditions & World State** — tập predicate đóng của concept (flag, affinity ≥ N, có item, ngày/mùa, trạng thái quest) + tập Effect đóng (TD-A4), mỗi loại một unit test. **Hợp đồng TD-B3:** mỗi namespace flag chỉ một hệ được ghi (`boss.*` chỉ Boss Encounter, `quest.*` chỉ Quest, `rumor.*` chỉ Dialogue & Discovery Hints…); luật thứ tự giữa lúc provider đăng ký và lúc được phép đánh giá điều kiện (boot order). Mở khóa (công thức, nhánh bí mật) là flag mà Weapon Tree/Crafting đọc qua Conditions — một chiều. **TD-B5:** trạng thái vùng (rương, node, respawn) sống ở đây; Region Loading không bao giờ tham chiếu trực tiếp tiến độ boss — truy vấn qua Conditions.
3. **Game Data & Tuning Config** — **TD-B4: cần ADR trước khi bất kỳ GDD nào ghi ID** — định dạng authoring và **sơ đồ ID ổn định** (save và flag tham chiếu ID; đổi tên không được làm vỡ save). Chỉ registry + lookup; mỗi hệ sở hữu schema của mình (tránh God config).
4. **Save/Load** — **Hợp đồng TD-B2:** khóa ổn định + DTO có version cho mỗi saveable, đường migration từ save v1; **thứ tự restore** rõ ràng (vd. Flags → Inventory → Loadout → Weapon Tree); **checkpoint tạm trong bộ nhớ** cho retry trước arena (Defeat & Retry không được nạp lại save lúc ngủ — mất một ngày, phá Pillar 3); **Settings lưu theo máy, ngoài slot save**. Quy ước: các hệ có trạng thái ngầm phụ thuộc Save/Load; GDD Save/Load phải liệt kê **mọi** hệ saveable (gồm Weapon Tree, Crafting, Quest, Shops, Schedules, Dialogue & Discovery Hints dù bảng trên không ghi cạnh).
5. **Input** — action map + buffer có timestamp; việc tiêu thụ buffer do Character Controller quyết. **TD-B8:** Settings đẩy binding vào API của Input lúc boot; Input không bao giờ đọc Settings.

### Core Layer (depends on foundation)

1. **Character Controller** — depends on: Input. Chủ state machine hành động (TD-A3); cung cấp interface modifier chuyển động cho Elemental & Status đăng ký vào.
2. **Region Loading** — depends on: Game Data & Tuning Config.
3. **Camera** — depends on: Character Controller, Region Loading (biên vùng).
4. **Combat** — depends on: Character Controller, Game Data & Tuning Config.
5. **Interaction & Context Prompts** — depends on: Input, Character Controller, Flags, Conditions & World State.
6. **Item & Inventory** — depends on: Game Data & Tuning Config, Save/Load. GDD phải định nghĩa taxonomy vật phẩm (TD-D).
7. **Telemetry** — depends on: Game Time & Calendar; quan sát sự kiện gameplay, chỉ ghi file cục bộ, không mạng.

### Feature Layer (depends on core)

1. **Weapon Moveset** — depends on: Combat, Character Controller, Game Data & Tuning Config. Xuất **thời gian cam kết tối đa mỗi họ** dưới dạng dữ liệu cho validator của Boss.
2. **Weapon Tree** — depends on: Weapon Moveset, Flags, Conditions & World State, Game Data & Tuning Config. Đọc trần tier qua Conditions; không đọc Inventory (TD-A6).
3. **Skill Stone** — depends on: Combat, Weapon Moveset, Game Data & Tuning Config.
4. **Loadout & Player Stats** — depends on: Game Data & Tuning Config, Item & Inventory, Weapon Tree, Skill Stone (định nghĩa). Combat đọc stat block đã tính.
5. **Enemy AI, Spawning & Drops** — depends on: Combat, Character Controller, Flags, Conditions & World State, Game Time & Calendar, Item & Inventory.
6. **Boss Encounter** — depends on: Combat, Enemy AI, Spawning & Drops, Camera, Game Time & Calendar (token tạm dừng), Flags, Conditions & World State. Cạnh tới Weapon Moveset **chỉ lúc authoring/test** (TD-A5): EditMode test hoặc editor validator kiểm tỷ lệ cửa sổ ≥ 1,25× và ma trận họ × boss. Luật nằm ở GDD Combat như concept nói.
7. **Defeat & Retry** — depends on: Game Time & Calendar, Flags, Conditions & World State, Combat, Save/Load.
8. **Farming** — depends on: Game Time & Calendar, Interaction & Context Prompts, Item & Inventory, Save/Load.
9. **Crafting & Forge** — depends on: Item & Inventory, Weapon Tree, Game Data & Tuning Config, Game Time & Calendar.
10. **NPC Affinity** — depends on: Flags, Conditions & World State, Interaction & Context Prompts, Item & Inventory, Save/Load.
11. **Dialogue & Discovery Hints** — depends on: Flags, Conditions & World State, Interaction & Context Prompts, Game Data & Tuning Config. Gợi ý được author bằng predicate (TD-B7); sở hữu trạng thái hiểu biết/nhật ký. Ghi affinity/item qua Effect.
12. **Quest & Hidden Quest** — depends on: Dialogue & Discovery Hints, Flags, Conditions & World State, Save/Load.
13. **Elemental & Status** — depends on: Combat, Character Controller, Game Data & Tuning Config.
14. **NPC Schedules** — depends on: Game Time & Calendar, Region Loading, NPC Affinity.
15. **Currency & Shops** — depends on: Item & Inventory, Game Data & Tuning Config, Game Time & Calendar, NPC Affinity, NPC Schedules.
16. **Cutscene & Scripted Sequence** — depends on: Game Time & Calendar (token tạm dừng), Camera, Dialogue & Discovery Hints, Flags, Conditions & World State.

### Presentation Layer (depends on features)

1. **Menu & Navigation Framework** — depends on: Input.
2. **HUD** — depends on: Combat, Game Time & Calendar; + Boss Encounter, Skill Stone (MVP-Complete), Elemental & Status (Alpha) — icon trạng thái, cooldown (TD-A7).
3. **Inventory, Equipment & Forge UI** — depends on: Menu & Navigation Framework, Item & Inventory, Loadout & Player Stats, Weapon Tree, Crafting & Forge.
4. **Audio** — depends on: Game Time & Calendar, Region Loading, Combat (observes).
5. **Settings & Accessibility** — depends on: Input, Save/Load, Menu & Navigation Framework. Đặt ở Presentation vì màn hình settings dựng trên Menu framework; hệ khác nhận giá trị settings bằng cách Settings đẩy vào (TD-B8), không đọc ngược.
6. **Atmosphere & Lighting** — depends on: Camera, Game Time & Calendar, Region Loading, Boss Encounter.

### Polish Layer (depends on everything)

1. **Tutorial & Onboarding** — depends on: Flags, Conditions & World State, HUD, Interaction & Context Prompts, và hợp đồng sự kiện (TD-B7) — không phụ thuộc sâu vào Combat/Farming/Weapon Tree. Lớp prompt tối thiểu ở MVP sống trong Interaction & Context Prompts.

---

## Recommended Design Order

| Order | System | Priority | Layer | Agent(s) | Est. Effort |
|-------|--------|----------|-------|----------|-------------|
| 1 | Game Time & Calendar | MVP | Foundation | systems-designer, game-designer | M |
| 2 | Flags, Conditions & World State | MVP | Foundation | systems-designer, lead-programmer | M |
| 3 | Save/Load | MVP | Foundation | lead-programmer, unity-specialist | M |
| 4 | Game Data & Tuning Config (sau ADR ID ổn định) | MVP | Foundation | lead-programmer, systems-designer | S |
| 5 | Input | MVP | Foundation | gameplay-programmer, unity-specialist | S |
| 6 | Character Controller | MVP | Core | game-designer, gameplay-programmer | L |
| 7 | Combat | MVP | Core | game-designer, systems-designer, gameplay-programmer | L |
| 8 | Skill & Moveset (trước là Weapon Moveset) | MVP | Core *(đổi từ Feature 2026-11-06, ADR-0002: Controller phải tham chiếu mô hình kỹ năng)* | game-designer, gameplay-programmer, technical-artist | L |
| 9 | Camera | MVP | Core | gameplay-programmer, unity-specialist | M |
| 10 | Region Loading | MVP | Core | engine-programmer, unity-addressables-specialist | S |
| 11 | Interaction & Context Prompts | MVP | Core | game-designer, ux-designer | S |
| 12 | Item & Inventory | MVP | Core | systems-designer, economy-designer | M |
| 13 | Telemetry | MVP | Core | analytics-engineer, qa-lead | S |
| 14 | Weapon Tree | MVP | Feature | systems-designer, economy-designer, game-designer | M |
| 15 | Loadout & Player Stats | MVP | Feature | systems-designer | S |
| 16 | Enemy AI, Spawning & Drops | MVP | Feature | ai-programmer, game-designer, economy-designer | M |
| 17 | Defeat & Retry | MVP | Feature | game-designer | S |
| 18 | Farming | MVP | Feature | game-designer, systems-designer | M |
| 19 | Crafting & Forge | MVP | Feature | economy-designer, systems-designer | M |
| 20 | NPC Affinity | MVP | Feature | game-designer, narrative-director | S |
| 21 | Dialogue & Discovery Hints | MVP | Feature | narrative-director, game-designer | S |
| 22 | Menu & Navigation Framework | MVP | Presentation | ux-designer, unity-ui-specialist | M |
| 23 | HUD | MVP | Presentation | ux-designer, ui-programmer | S |
| 24 | Inventory, Equipment & Forge UI | MVP | Presentation | ux-designer, ui-programmer | M |
| 25 | Audio | MVP | Presentation | audio-director, sound-designer | S |
| 26 | Boss Encounter (MVP-Complete) | MVP | Feature | game-designer, level-designer, systems-designer | L |
| 27 | Skill Stone (MVP-Complete) | MVP | Feature | systems-designer, game-designer | S |
| 28 | Quest & Hidden Quest (MVP-Complete) | MVP | Feature | narrative-director, game-designer | S |
| 29 | Settings & Accessibility | Vertical Slice | Presentation | ux-designer, accessibility-specialist | M |
| 30 | NPC Schedules | Vertical Slice | Feature | game-designer, narrative-director | M |
| 31 | Currency & Shops | Vertical Slice | Feature | economy-designer | M |
| 32 | Cutscene & Scripted Sequence | Vertical Slice | Feature | narrative-director, gameplay-programmer | M |
| 33 | Atmosphere & Lighting | Vertical Slice | Presentation | art-director, technical-artist, unity-shader-specialist | M |
| 34 | Tutorial & Onboarding | Vertical Slice | Polish | ux-designer, game-designer | S |
| 35 | Elemental & Status | Alpha | Feature | systems-designer, game-designer | M |

Ghi chú thứ tự: Skill & Moveset (8) ở layer Core (trước ghi Feature) nhưng thiết kế ngay sau Combat vì state machine của Controller (TD-A3) phải chốt cùng lúc với dữ liệu đòn mà nó tiêu thụ. Boss Encounter thiết kế sau Fun-Gate Build nhưng **phải đọc lại GDD Combat và Weapon Moveset** (luật cửa sổ ≥ 1,25×).

---

## Circular Dependencies

Không còn vòng cứng. Năm nguy cơ vòng đã được gỡ (TD-SYSTEM-BOUNDARY xác nhận đúng chiều):

- **Weapon Tree ↔ Crafting & Forge** — Weapon Tree là dữ liệu cây + node đã sở hữu; Crafting thực thi giao dịch (tiêu nguyên liệu rồi gọi mở node). Một chiều: Crafting → Weapon Tree.
- **Quest ↔ NPC Affinity ↔ Dialogue** — Affinity cung cấp giá trị qua Conditions và nhận thay đổi qua Effect `AffinityDelta`; Dialogue/Quest không phụ thuộc trực tiếp Affinity (TD-A4, người dùng chọn 2026-10-25).
- **Boss Encounter ↔ Weapon Moveset** — chỉ ràng buộc lúc authoring/test, không phụ thuộc runtime (TD-A5).
- **Save/Load ↔ mọi hệ** — Save/Load là hợp đồng Foundation; các hệ đăng ký saveable.
- **Boss progress ↔ Weapon Tree** — Boss ghi flag `boss.*` → điều kiện trần tier → Weapon Tree đọc. Boss không bao giờ gọi Weapon Tree. (Boss Progress & Region Gating nay nằm trong #3.)

Ràng buộc thêm: các registry (condition provider, effect handler, saveable, token tạm dừng thời gian) **không được là static singleton** — theo chuẩn DI của coding standards, và vì Enter Play Mode Options tắt domain reload sẽ giữ đăng ký cũ giữa các lần Play (TD-C).

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| Character Controller + Combat + Weapon Moveset | Technical / Design | Combat feel là phần cần lặp nhiều nhất (1–2 tháng với người làm lần đầu); moveset 4 ngữ cảnh nhân chi phí theo họ vũ khí | Prototype combat đã PROCEED; chốt chủ state machine (TD-A3) trước GDD Combat; mốc kiểm 2026-12-06; vũ khí thứ hai ≤ 2 tuần |
| Game Time & Calendar | Technical / Design | Trộn đồng hồ game với `timeScale`; không có token tạm dừng → lịch bị đẩy bởi hit-stop/menu; thứ tự chuyển ngày ngầm giữa ≥ 6 hệ | GDD đầu tiên; hợp đồng TD-B1; ADR "time domains" |
| Flags, Conditions & World State | Technical | Phình thành ngôn ngữ scripting; flag thành trạng thái toàn cục khả biến | Tập predicate + effect đóng, unit test từng loại; luật một-hệ-ghi mỗi namespace (TD-B3) |
| Save/Load | Technical | Schema thay đổi làm vỡ save; retry boss nạp lại save ngủ làm mất ngày | DTO có version + migration từ v1; checkpoint tạm trong bộ nhớ (TD-B2); ADR ID ổn định (TD-B4) |
| Camera | Technical | Jitter sub-pixel với pixel-perfect + parallax là "hố thời gian" kinh điển | Timebox 4 ngày ở MVP; quá hạn tạm bỏ pixel-perfect (PR-SCOPE) |
| Boss Encounter | Design / Scope | "Mọi vũ khí hạ được mọi boss" = 288–2.016 ô cân bằng; mỗi boss mới kiểm lại mọi họ | Validator/EditMode test cửa sổ ≥ 1,25× và ma trận họ × boss (TD-A5); ~1 tuần cân bằng mỗi boss |
| Menu & Navigation Framework | Technical | Điều hướng tay cầm qua menu kho/rèn dày đặc bị đánh giá thấp | Giữ mức M ở MVP; quyết UI Toolkit vs UGUI bằng ADR |
| Dialogue & Discovery Hints | Design | Gánh H2: người test không tìm ra vũ khí thứ hai nếu gợi ý yếu → cổng vui trượt vì lý do sai | Không cắt nội dung gợi ý; vũ khí 2 từ bí mật vùng/NPC; kiểm khả năng tìm thấy trước buổi test |
| Weapon Tree (nội dung 1.0) | Scope | Số loại vũ khí (12–14) là thứ thực sự vỡ khi thiếu thời gian | Sàn vũ khí 1.0 quyết ở cổng vũ khí thứ hai (cần creative-director) |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 35 |
| Design docs started | 5 (Game Time & Calendar, Flags, Conditions & World State, Input, Character Controller, Skill & Moveset) |
| Design docs reviewed | 1 (Game Time & Calendar) |
| Design docs approved | 1 (Game Time & Calendar) |
| MVP systems designed | 4/28 (Fun-Gate 4/25 · MVP-Complete 0/3) — Game Time (Approved), Input (Designed, chờ review), Character Controller (Designed, chờ review), Skill & Moveset (Designed 2026-11-05, chờ review; gộp #14 + phần kỹ năng của #17); Flags đang viết (tạm dừng) |
| Vertical Slice systems designed | 0/6 |

---

## Next Steps

- [x] Creative-director duyệt bản index này (CD-SYSTEMS: CONCERNS, chấp nhận 2026-10-25)
- [ ] Cập nhật `design/gdd/game-concept.md` theo quyết định 2026-10-25 (cần duyệt riêng): tách MVP thành Fun-Gate Build + MVP-Complete; vũ khí thứ hai = bí mật vùng + gợi ý sâu hơn nhờ lòng tin NPC; skill stone hoãn sang MVP-Complete; schedule gate "MVP xong trong 4 tháng" → Fun-Gate ~tháng 6 / MVP-Complete ~tháng 8 + mốc tốc độ 2026-12-06; Demo ~tháng 17–19, 1.0 ~tháng 30–36; hệ thống gợi ý vào MVP; sàn 1.0 = 10 loại vũ khí và luật cắt "loại 11–14 trước" thay luật "giữ số loại vũ khí" (C6); tiêu chí Pillar 4 cho boss MVP-Complete
- [ ] Chạy prototype vòng lặp một ngày (`/prototype`, 1–3 ngày) — cách rẻ nhất để giảm rủi ro H1 (producer)
- [ ] ADR trước GDD #4: định dạng dữ liệu + sơ đồ ID ổn định (TD-B4); sau đó ADR messaging (calls vs observes) và time domains trước `/create-architecture`
- [ ] Design MVP-tier systems first (use `/design-system [system-name]`) — bắt đầu từ Game Time & Calendar
- [ ] Run `/design-review` on each completed GDD
- [ ] Run `/gate-check technical-setup` when MVP systems are designed
- [ ] Validate the highest-risk systems with `/vertical-slice` before committing to Production
- [ ] Quyết của người dùng (producer nêu): `rigor: full` (28 GDD MVP × 8 section) có đáng chi phí cho MVP không
- [ ] (2026-10-30, từ GDD Input) Ba nhóm chức năng chưa có hệ sở hữu: **câu cá** (chế độ Fishing, Reel), **hiển thị bản đồ thế giới** (chế độ Map), **dùng vật phẩm tiêu hao** (Heal, EatBuff: Item & Inventory hay Combat). Gắn với việc cập nhật concept theo thế giới từ prototype day-loop (map sinh theo ngày, ngã rẽ, câu cá) — cần creative-director và `/map-systems` cập nhật
- [ ] (2026-10-30) ADR "Input architecture" trước story đầu tiên của Input (GDD Input OQ-I1)
