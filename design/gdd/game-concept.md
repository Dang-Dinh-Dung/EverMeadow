# Game Concept: Evermeadow

*Created: 2026-10-24*
*Status: Draft*
*Nguồn: Game_Concept_Summary.md của tác giả + phiên /brainstorm 2026-10-24 (review: CD-PILLARS, AD-CONCEPT-VISUAL, TD-FEASIBILITY, PR-SCOPE); cập nhật 2026-10-25 theo prototype combat (CD-PLAYTEST), gate Concept → Systems Design và `/design-review` (NEEDS REVISION — xem `design/gdd/reviews/game-concept-review-log.md`)*

> Ghi chú ngôn ngữ: tiêu đề section giữ tiếng Anh theo template (các skill sau đọc theo heading); nội dung viết tiếng Việt, thuật ngữ hệ thống giữ tiếng Anh.

---

## Elevator Pitch

> Chăm vườn, kết bạn với dân làng, rồi bước vào màn sương. Evermeadow là một
> 2D pixel-art platformer RPG nơi mỗi loại vũ khí bạn tìm được là một bí mật
> của thế giới — và mỗi vụ mùa, mỗi tình bạn đều góp vào lò rèn của bạn.

**10-second test:** "Stardew Valley ban ngày, Monster Hunter khi sương xuống, trong một platformer 2D đi ngang."

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | 2D Platformer Action RPG + Life Sim (farming, crafting, social) |
| **Platform** | PC (Steam / Epic); bàn phím + tay cầm |
| **Target Audience** | Explorer / Achiever thích sưu tập và build (xem Target Player Profile) |
| **Player Count** | Single-player |
| **Session Length** | 30–120 phút (≈ vài ngày trong game; điểm dừng tự nhiên = đi ngủ) |
| **Monetization** | Premium; Demo miễn phí (Steam Next Fest); Early Access tùy chọn. Không bán vũ khí/nhánh bằng tiền thật |
| **Estimated Scope** | Large (24 tháng tới 1.0, solo full-time; full vision 36+ tháng qua cập nhật sau phát hành) |
| **Comparable Titles** | Rune Factory, Terraria, Stardew Valley, Monster Hunter: World (phụ: Sun Haven, Moonlighter, Hollow Knight) |

---

## Core Fantasy

**Có một mái nhà để quay về sau mỗi chuyến phiêu lưu — và là người được các vị thần chọn để trao trả những vũ khí bị thế giới lãng quên.**

Người chơi vừa là người nông dân của một vùng đồng cỏ ấm áp, vừa là thợ săn dần sưu tập được một kho vũ khí do chính mình tìm ra và rèn nên. Sức mạnh không đến từ cấp độ nhân vật mà từ lựa chọn: đi nhánh vũ khí nào, gắn skill stone nào, thân với ai. Không có con đường đúng duy nhất — hành trình của mỗi người khác nhau.

*Lựa chọn thay thế đã cân nhắc:* fantasy "bậc thầy vũ khí" thuần (Concept 1 — Hunter's Meadow) bị bỏ vì làm nửa cozy thành phụ; fantasy "người giải mã bí mật" thuần (Concept 3 — Secrets of the Mist) được giữ lại như *cách* vũ khí xuất hiện, không phải toàn bộ fantasy.

---

## Unique Hook

**"Giống Stardew Valley và Monster Hunter trong cùng một ngày — VÀ mỗi loại vũ khí là một bí mật được tìm thấy trong thế giới (một vùng đất, một boss, lòng tin của một NPC), không phải món mua ở cửa hàng."**

- Hook này ảnh hưởng gameplay trực tiếp: số lượng vũ khí lớn (~14 loại) trở thành số lượng *khám phá*, là động lực để vào vùng mới và quay lại vùng cũ.
- Nó nối với core fantasy: vụ mùa → potion cho chuyến săn; quái vật → nguyên liệu rèn; tình bạn → nhánh vũ khí bí mật.
- Khoảng trống thị trường: Rune Factory đã kết hợp farming + combat + quan hệ + nhiều vũ khí, nhưng là 3D/top-down và không có weapon tree kiểu Monster Hunter. Side-scroll 2D + weapon tree sâu là phần chưa ai lấp.

---

## World Premise

**Hướng: Giao Ước Thần Rừng** (chọn 2026-10-25 từ 3 phương án của narrative-director). Đây là bản sơ bộ; lore chi tiết để GDD Narrative.

**Tiền đề.** Sương là hơi thở của các vị thần đất và rừng. Sau một cuộc chiến cổ xưa, con người giao vũ khí của mình cho thần, và sương phủ xuống niêm phong chúng. Vũ khí bị "lãng quên" có chủ đích: tổ tiên Green Field Village *chọn* quên để giữ hòa bình. Thần chỉ thử thách người chăm đất thật lòng, nên người nông dân — người mới đến cày lại mảnh đất bỏ hoang — là người các vị thần bắt đầu để ý. Boss là các linh thú hộ vệ ra đề thử; thắng thì được thần công nhận và vũ khí được trao lại. Thần không chết; boss luôn có thể thử lại.

**Giọng điệu (tone contract).** *"Lễ hội và thử thách là một: ngày dâng lễ, đêm đấu với thần — và thần nhớ tên người thắng."* Boss hoành tráng kiểu Calamity là một nghi lễ của thế giới, không phải tai họa từ bên ngoài.

**Ràng buộc với các pillar.**
- **Lễ dâng mùa màng ở các miếu nhỏ là đường tắt hoặc đường phụ** (mở gợi ý, nhánh phụ, thử thách tùy chọn), **không bao giờ là cửa bắt buộc** — đường chính vẫn mở được bằng chiến đấu (Pillar 3 › Luật đường chính).
- Không có đồng hồ tận thế: sương không lan, thần không giận theo thời gian (anti-pillar *NOT a time-pressure game*).
- "Người duy nhất tìm ra" là cảm giác trong truyện — thần chọn *nhân vật này* — không phải điều cộng đồng người chơi không được chia sẻ.

**Lớp NPC / hidden quest.** Dân làng là hậu duệ của các dòng họ đã lập giao ước; mỗi nhà giữ một mảnh nghi lễ, và lòng tin của họ mở ra mảnh đó (Pillar 5).

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics (What the player FEELS)

Expression và Discovery là **đồng chính** (theo review CD-PILLARS: người chơi chọn Expression, nhưng hook và phần lớn pillar phục vụ Discovery).

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Expression** (self-expression, creativity) | 1 | Loại vũ khí × nhánh × skill stone = lối chơi riêng; sidegrade trong cùng tier; không có build bắt buộc |
| **Discovery** (exploration, secrets) | 1 | Mỗi loại vũ khí là một khám phá; hidden quest; secret recipe; nhánh bí mật; vùng đất có bí mật riêng |
| **Challenge** (obstacle course, mastery) | 3 | Combat nhanh, nhạy; boss nhiều phase kiểu Calamity, khó nhưng đọc được |
| **Fantasy** (make-believe, role-playing) | 4 | Hai vai: nông dân + thợ săn; thế giới fantasy handcrafted |
| **Submission** (relaxation, comfort zone) | 5 | Farming, sinh hoạt làng, nhịp ngày/mùa, mái nhà ấm áp (Lantern & Mist) |
| **Narrative** (drama, story arc) | 6 | Cốt truyện chính là chuỗi boss các vùng, kết thúc ở boss cuối; câu chuyện từng vùng và quest chain giữa NPC nằm quanh nó. Phần lớn thời gian chơi không nằm trên đường chính |
| **Sensation** (sensory pleasure) | 7 | Hit-stop, VFX, phản hồi đòn đánh; pixel art có hướng hình ảnh rõ |
| **Fellowship** (social connection) | N/A | Không multiplayer. Quan hệ với NPC được phục vụ qua Fantasy/Narrative, không phải Fellowship |

### Key Dynamics (Emergent player behaviors)

- Người chơi **thử vũ khí mới với boss cũ** để cảm nhận sự khác biệt giữa các loại — vì mọi loại đều hạ được mọi boss.
- Người chơi **lên kế hoạch cho từng ngày**: hôm nay trồng, gặp NPC hay đi săn — vì lịch luôn trôi.
- Người chơi **quay lại vùng cũ** khi nghe tin đồn về một loại vũ khí hay một NPC lạ.
- Người chơi **đầu tư vào một NPC cụ thể** vì nghi ngờ người đó giữ một nhánh vũ khí bí mật.
- Người chơi **chia sẻ khám phá** (vị trí vũ khí, điều kiện mở nhánh bí mật) với cộng đồng.
- Người chơi **farm có mục đích**: trồng cây cụ thể để chế potion cho một boss cụ thể.

### Core Mechanics (Systems we build)

1. **Real-time platformer combat** — nhanh, phản hồi tức thì; tốc độ gốc ngang hoặc nhanh hơn Hollow Knight; không trạng thái nào khóa đòn đánh; cancel sang né; boss nhiều phase trong arena thiết kế riêng.
2. **Weapon Tree** — ~14 loại vũ khí, mỗi loại có cây nhánh kiểu Monster Hunter (sidegrade trong tier, tier sau mạnh hơn), nhánh nguyên tố, Skill Stone socket cho kỹ năng chủ động, nhánh bí mật.
3. **Farming + Calendar** — cây trồng theo mùa, lịch ngày/mùa luôn trôi kể cả khi phiêu lưu.
4. **Crafting + Forge** — Equipment, Consumables, Utility, Processing (theo thời gian), Special/Secret recipes; chuỗi nguyên liệu nối farming, quái vật, boss và NPC.
5. **NPC Affinity + Hidden Quests** — affinity tăng/giảm có hậu quả (nhưng luôn hàn gắn được); hidden quest chain giữa nhiều NPC; mở công thức và nhánh vũ khí bí mật.

---

## Player Motivation Profile

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** (freedom, meaningful choice) | Chọn vũ khí, nhánh, skill stone; tự quyết cách dùng mỗi ngày; không nửa game nào khóa cứng nửa kia | Core |
| **Competence** (mastery, skill growth) | Làm chủ từng loại vũ khí; boss khó nhưng đọc được; thấy rõ build tiến bộ qua tier | Core |
| **Relatedness** (connection, belonging) | Dân làng nhớ bạn; affinity mở ra câu chuyện và phần thưởng thật; làng là "nhà" | Supporting |

### Player Type Appeal (Bartle Taxonomy)

- [x] **Achievers** (goal completion, collection, progression) — How: sưu tập đủ bộ vũ khí và nhánh bí mật; hạ mọi boss; hoàn thành câu chuyện từng vùng.
- [x] **Explorers** (discovery, understanding systems, finding secrets) — How: **người chơi chính.** Loại vũ khí giấu trong thế giới, hidden quest, secret recipe, điều kiện mở khóa ẩn.
- [x] **Socializers** (relationships, cooperation, community) — How: phụ, qua NPC (không phải người chơi khác) — affinity, quest chain giữa NPC.
- [ ] **Killers/Competitors** (domination, PvP, leaderboards) — How: không phục vụ.

Quantic Foundry: chính là **Creativity → Discovery** và **Achievement → Completion/Power**; phụ là **Mastery → Challenge** và **Immersion → Fantasy**.

### Flow State Design

- **Onboarding curve**: 10 phút đầu ở Farm Area — di chuyển, đòn đánh cơ bản với một vũ khí khởi đầu, trồng cây đầu tiên, gặp NPC đầu tiên ở Green Field Village. Vũ khí thứ hai được *tìm thấy* sớm (trong ngày đầu–ngày hai) để dạy ngay hook "vũ khí là khám phá".
- **Difficulty scaling**: theo vùng và tier vũ khí. Trong một tier, các nhánh là sidegrade nên độ khó đến từ boss và quái mới, không từ việc thiếu stat. Mỗi vùng có một boss là đỉnh độ khó của vùng đó.
- **Feedback clarity**: hit-stop, flash, knockback, âm thanh rõ cho từng đòn; boss có màu hazard dành riêng; tiến bộ build thấy được qua cây vũ khí và skill stone.
- **Recovery from failure**: thua boss → thử lại nhanh từ trước arena. **Luật: thua boss không được làm hỏng nửa nông trại** (không mất ngày, không mất mùa vụ vì một lần thua). Cơ chế cụ thể là Open Question.

---

## Core Loop

### Moment-to-Moment (30 seconds)

Né → áp sát → 2–3 đòn → rút → đọc pattern → lặp lại. Thỏa mãn đến từ:
- **Phản hồi:** hit-stop ngắn, knockback, flash, âm thanh từng đòn trúng.
- **Nhịp:** tốc độ gốc **ngang hoặc nhanh hơn Hollow Knight** (~0.20s/đòn trong prototype); vẫn di chuyển khi chém, nhảy được giữa đòn, cancel sang né. Nặng hay nhanh là **lựa chọn build**, không phải nhịp gốc.
- **Không khóa đòn đánh:** tấn công được ở mọi trạng thái — trên đất (chuỗi 3 hit), trên không, khi lướt (đòn lướt), khi đáp đất (plunge). Hitstun là ngoại lệ duy nhất còn mở — GDD Combat quyết.
- **Trọng lượng** đến từ hit-stop, knockback, âm thanh và phản ứng của quái — không từ việc khóa nhân vật. GDD Combat định nghĩa cụ thể.
- **Chiều sâu:** mỗi loại vũ khí đổi tầm, nhịp và cách tiếp cận — cùng một quái, cảm giác khác nhau tùy vũ khí.

Boss kiểu Terraria Calamity: nhiều phase, pattern/đạn dày, đòi hỏi di chuyển — nên mỗi boss có arena được thiết kế riêng (không gian dọc, platform để né), khác với phần thế giới đi ngang.

### Short-Term (5-15 minutes)

Một chuyến ra ngoài: vào một vùng (vd. Mist Forest) → đánh quái, nhặt nguyên liệu → phát hiện một dấu hiệu bí mật (lối đi, NPC lạ, mảnh của một loại vũ khí chưa biết) → quyết định đi sâu tiếp hay về nhà. Ở lại qua đêm thì đồ rơi tốt hơn nhưng quái mạnh hơn; không có gì buộc phải về. "Thêm một chuyến" bắt đầu khi chỉ thiếu đúng một nguyên liệu để rèn nhánh kế tiếp.

### Session-Level (30-120 minutes)

Một phiên = vài ngày trong game (~4 ngày mỗi 60 phút). **Mỗi ngày một chuyến:** sáng làm việc nhà (thu hoạch, tưới) và ghé làng (tặng quà, nhận quest); rồi **một chuyến ra ngoài** (Short-Term, 5–15 phút) từ trưa hoặc chiều, kéo tới hoàng hôn — hoặc ở lại qua đêm (đồ rơi tốt hơn, quái mạnh hơn) nếu muốn; về nhà rèn, chế potion, xử lý nguyên liệu, rồi ngủ. Khoảng giờ cho ngày *hình dạng* (ánh sáng, ai ở làng, quái đêm), không phải lịch việc phải làm. **Điểm dừng tự nhiên là đi ngủ (cũng là điểm lưu).** Lý do quay lại: cây sắp chín, NPC hẹn ngày mai, một vũ khí chỉ còn thiếu một nguyên liệu. *(Sửa 2026-10-26 theo review GDD Game Time & Calendar, quyết định D-1: một ngày thức ≈ 12–15 phút thực, không đủ cho "mỗi khoảng giờ một việc".)*

### Calendar Contract

Thời gian tạo nhịp cho cuộc sống, không bao giờ là hạn chót (anti-pillar *NOT a time-pressure game*):
- **Ngày/đêm và 4 mùa luôn trôi; năm lặp lại.** Nội dung theo mùa luôn quay lại năm sau — không có gì bị bỏ lỡ vĩnh viễn vì lịch.
- **Cây không bao giờ héo hay chết.** Ảnh hưởng của việc chăm sóc (tưới, phân) lên tốc độ lớn để GDD Farming quyết.
- **Đêm là cơ hội, không phải hạn chót:** đồ rơi tốt hơn, quái mạnh hơn; không có gì buộc người chơi phải về nhà. Ngủ là điểm lưu.
- **Thua boss không làm hỏng nông trại.** Cơ chế cụ thể (phương án systems-designer đề xuất: thời gian đóng băng trong arena) → GDD Calendar/Time.

### Long-Term Progression

Tìm loại vũ khí mới → đi sâu nhánh → gắn skill stone → hạ boss → nguyên liệu boss mở nhánh tier cao hơn và mở vùng mới → NPC mới, bí mật mới. Mục tiêu dài hạn: sưu tập các loại vũ khí và nhánh bí mật, hạ boss cuối, hoàn thành câu chuyện từng vùng. Câu chuyện chính kết thúc ở boss cuối (credits). Boss cuối là linh thú hộ vệ cuối cùng; hạ nó nghĩa là được thần công nhận, không phải giết thần. Mọi boss, kể cả boss cuối, đánh lại được sau credits. Sau đó lịch, nông trại và việc sưu tập vũ khí, bí mật vẫn tiếp tục.

Không có skill tree nhân vật truyền thống — tiến trình sức mạnh nằm ở vũ khí, skill stone và trang bị.

### Retention Hooks

- **Curiosity**: những loại vũ khí chưa tìm thấy; tin đồn về nhánh bí mật; NPC có hidden quest chưa mở; vùng đất chưa vào.
- **Investment**: nông trại đang lớn; kho vũ khí đã rèn; quan hệ với NPC đã xây.
- **Social**: không có social giữa người chơi trong game; cộng đồng chia sẻ khám phá bên ngoài game.
- **Mastery**: làm chủ một loại vũ khí mới; hạ boss bằng build khác; nhánh tier cao hơn.

---

## Game Pillars

Đã chốt sau review CD-PILLARS (verdict CONCERNS → cả 5 chỉnh sửa được áp dụng).

### Pillar 1: Every Weapon Is a Discovery
Các **loại vũ khí và nhánh bí mật** luôn được tìm thấy trong thế giới — từ một vùng đất, một boss, hoặc lòng tin của một NPC. Nâng cấp nhánh thường và nguyên liệu có thể chế tạo hoặc mua.

*Design test*: Nếu phân vân giữa "bán loại vũ khí này ở shop" và "giấu nó trong thế giới", pillar này chọn **giấu trong thế giới**.

**Ngân sách khám phá (1.0):** 12–14 loại vũ khí + 12–14 nhánh bí mật ≈ 24–28 thứ cần tìm, trong khi chỉ có ~16–19 nguồn (3 vùng, 3–4 boss, ~10–12 NPC). Luật phân bổ: khoảng 4–5 thứ đến từ NPC (mỗi NPC có mong muốn riêng ngoài vũ khí), phần còn lại từ vùng và boss; một nguồn có thể giữ nhiều thứ, nhưng mỗi thứ có điều kiện mở riêng. Vì vậy **hệ thống gợi ý (tin đồn, nhật ký) là bắt buộc**, không phải tùy chọn.

### Pillar 2: Your Build, Your Way
Loại vũ khí + nhánh + skill stone tạo nên lối chơi riêng; không có con đường đúng duy nhất. Các nhánh **trong cùng một tier là sidegrade**; tier sau mạnh hơn tier trước. **Mọi loại vũ khí đều có thể hạ mọi boss.**

Trục sidegrade cơ bản: **tốc đánh ↔ sát thương**. Mỗi họ vũ khí có một **dải tốc độ** làm bản sắc; các nhánh dịch chuyển trong dải đó, để 14 loại vũ khí không thành một kiểu chém ở 14 tốc độ.

**Bắt kịp khi đổi vũ khí:** loại vũ khí mới bắt đầu ở tier 1, nhưng nâng lên tới **tier trần chung** rất rẻ — chỉ cần nguyên liệu dễ kiếm từ các vùng đã qua, không phải đánh lại boss. Tier trần chung là một giá trị toàn cục do tiến độ boss mở ra (không theo từng vũ khí); chi phí leo là một phần giới hạn **k** của chi phí leo từ đầu (k chốt ở GDD Weapon Tree). Đổi vũ khí là thử một lối chơi mới, không phải leo lại từ đầu.

**"Hạ được mọi boss" nghĩa là:** mỗi phase boss có cửa sổ dễ tổn thương rộng hơn thời gian ra đòn của họ vũ khí chậm nhất một biên an toàn (đề xuất ≥ 1,25×, chốt ở GDD Combat) để vũ khí chậm không phải đánh hoàn hảo. Vũ khí tầm xa / catalyst có cơ hội tương đương: một khoảng hở boss lộ mình ngoài tầm cận chiến, không bắt buộc áp sát. Thời gian hạ boss giữa các họ vũ khí cùng tier chênh nhau trong một khoảng đã thống nhất (đề xuất ≤ 1,5× — chốt ở GDD Combat).

**Luật xung đột P2 ↔ P4:** khi một boss không công bằng với một họ vũ khí, **đổi boss, không nerf vũ khí**. Boss được chỉnh theo họ vũ khí có thời gian hạ trung vị.

*Design test*: Nếu phân vân giữa một nâng cấp "tốt hơn hẳn" và một nâng cấp "khác hướng" trong cùng tier, pillar này chọn **khác hướng (sidegrade)**.

### Pillar 3: Two Lives, One Loop
Nông trại, làng và phiêu lưu nuôi lẫn nhau: mỗi nửa làm nửa kia dễ hơn hoặc giàu hơn, nhưng không nửa nào khóa cứng nửa kia. **Thua boss không được làm hỏng nửa nông trại.**

**Luật đường chính:** mọi thứ đường chính bắt buộc (vũ khí đủ tier để hạ boss kế tiếp, mở vùng mới) đều có một đường lấy được không cần farm và không cần affinity. Farm và affinity chỉ thêm tiện lợi hoặc thêm lựa chọn sidegrade.

*Design test*: Nếu một tính năng làm cho nửa này cần một input mà chỉ nửa kia mới có, pillar này **bác nó, hoặc buộc thêm một đường thay thế**.

### Pillar 4: Fewer Bosses, Each an Event
Ít boss hơn, nhưng mỗi boss là một sự kiện: nhiều phase, hoành tráng, khó nhưng đọc được, trong arena thiết kế riêng.

*Design test*: Nếu phân vân giữa thêm một boss và làm một boss hiện có thêm một phase đáng nhớ, pillar này chọn **thêm phase**. (Số vũ khí không bị pillar này giới hạn.)

*(Tiêu chí combat feel nằm ở Core Loop › Moment-to-Moment, vì nó áp dụng cho mọi trận, không chỉ boss.)*

### Pillar 5: Relationships Change the World
Affinity mở ra gameplay thật — quest, công thức, nhánh vũ khí bí mật — và có thể giảm, có hậu quả. Affinity giảm chỉ **làm chậm hoặc làm đắt hơn**, không bao giờ khóa nội dung vĩnh viễn; **mọi mối quan hệ đều có thể hàn gắn.** Nội dung đã mở nhờ affinity (nhánh bí mật, công thức) không bao giờ khóa lại khi affinity giảm. Hàn gắn chỉ đưa quan hệ về mức cũ, không cho thêm thưởng (không cày được); đường hàn gắn không bao giờ phụ thuộc vào quest đang bị chặn.

*Design test*: Nếu phân vân giữa phần thưởng chỉ là hội thoại và phần thưởng là gameplay, pillar này chọn **gameplay**.

**Lực kéo có chủ ý giữa các pillar:**
- P4 (thử thách epic) ↔ P3 (cozy) — được điều hòa bởi luật "thua boss không hỏng nông trại".
- P1 (giấu kín) ↔ P2 (tự do chọn build) — được điều hòa bởi luật hàn gắn của P5.
- P1 ↔ P5 — nhánh bí mật sau affinity không bao giờ mất vĩnh viễn.
- P2 ↔ P4 — pattern boss phải công bằng với mọi loại vũ khí (chi phí cân bằng lớn nhất, xem Scope Risks).

### Anti-Pillars (What This Game Is NOT)

- **NOT a dig/build sandbox**: không đào hay xây địa hình tự do — nó phá thế giới handcrafted mà Pillar 1 dựa vào.
- **NOT an underground / depth-first Metroidvania**: không hang ngầm, không khám phá chiều sâu — thế giới đi ngang theo vùng là bản sắc của khám phá.
- **NOT a game with a mandatory best weapon or build**: phá Pillar 2.
- **NOT a game that forces farming to beat bosses**: biến Pillar 3 thành nghĩa vụ và lấy mất lựa chọn của Pillar 2.
- **NOT selling weapons or branches for real money**: phá Pillar 1.
- **NOT a time-pressure game**: lịch tạo nhịp cho cuộc sống, không bao giờ phạt việc phiêu lưu.

---

## Visual Identity Anchor

**Hướng: Hybrid "Lantern & Mist" + "Relic Silhouette"** (AD-CONCEPT-VISUAL: CONCEPTS; người dùng chọn hybrid, đúng khuyến nghị của art-director).

**Luật hình ảnh một dòng:** *"Hơi ấm là nhà; sương mù rút cạn màu sắc — và người chơi mang màu trở lại."*

**Nguyên tắc hỗ trợ:**

1. **Màu bão hòa = thứ quan trọng.** Trong vùng sương, màu đậm chỉ xuất hiện ở tài nguyên, quái, bí mật và thứ tương tác được.
   *Design test*: Nếu một vật trang trí trong Mist Forest có màu bão hòa, nó phải tương tác được — nếu không, giảm màu.
2. **Mỗi vũ khí đọc được qua một bóng dáng** (từ Relic Silhouette). Mỗi loại trong ~14 loại có một hình dạng chủ đạo riêng (lưỡi liềm, phiến, kim, vòng…), phân biệt được ở kích thước gốc; vũ khí phóng đại ~1.3–1.5×.
   *Design test*: Nếu hai loại vũ khí không phân biệt được ở 1× chỉ bằng silhouette, một trong hai phải đổi hình dạng.
3. **Hình khối theo khoảng cách từ nhà.** Vùng nhà tròn, mềm (mái tranh, hàng rào tròn, nhân vật tròn trịa); càng vào sương càng hữu cơ và gai góc; arena boss sắc cạnh, hình học.
   *Design test*: Nếu một asset mới không rõ thuộc vùng nào, độ tròn/sắc của nó phải cho biết khoảng cách từ nhà.

**Triết lý màu:**
- Nhà (Farm, Green Field Village): hổ phách ấm, xanh lá, ánh lửa, đom đóm, giờ vàng.
- Mist Village: tĩnh, ánh đèn lồng giữa tông lạnh.
- Mist Forest: teal → tím, giảm giá trị màu theo lớp sâu.
- Chu kỳ ngày/mùa đổi dải màu ấm; vùng sương "kháng" mùa một phần — tạo cảm giác xa lạ.
- **Mỗi nguyên tố sở hữu một họ màu** mà môi trường không dùng.
- **Một màu dành riêng cho hazard của boss** — môi trường và VFX của người chơi không bao giờ dùng; VFX người chơi được làm mờ khi đánh boss.

**Lưu ý sản xuất (mang sang /art-bible):**
- Vũ khí là **layer riêng** chồng lên sprite nhân vật (paper-doll); mỗi **họ vũ khí** có chuỗi đòn trên đất riêng; đòn trên không, lướt và đáp đất dùng chung, đổi diện mạo/hiệu ứng theo họ, cộng **một đòn ngữ cảnh đặc trưng** mỗi họ; nhánh nguyên tố = palette swap + VFX, không vẽ lại.
- Độ phân giải nội bộ 640×360 hoặc 480×270. 640×360 scale nguyên lên 1080p (×3), 1440p (×4) và 4K (×6); 480×270 chỉ scale nguyên lên 1080p (×4) và 4K (×8), lên 1440p là ×5,33 nên cần letterbox; nhân vật cao ~32–48 px. Xác nhận silhouette vũ khí ở kích thước này trước khi sản xuất.
- Một tileset mỗi vùng mang dải màu thay vì nhiều bộ art — giữ chi phí vừa phải.
- Hướng "Breathing Meadow" (thế giới sống động theo mùa) để dành làm lớp polish sau 1.0, không phải bản sắc.

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| **Terraria** (+ Calamity mod) | Khám phá, boss, trang bị, tiến trình sức mạnh; boss nhiều phase, pattern dày | Không sandbox đào/xây, không thế giới ngầm; thế giới handcrafted đi ngang | Chứng minh vòng lặp khám phá–chế tạo–boss giữ chân người chơi hàng trăm giờ |
| **Stardew Valley** | Farming theo mùa, lịch, NPC relationship, cảm giác "nhà" | Affinity mở gameplay thật (nhánh vũ khí bí mật), có thể giảm | Chứng minh nhu cầu lớn của khán giả cozy với quan hệ NPC |
| **Monster Hunter: World** | Weapon tree nhiều nhánh, nguyên liệu từ quái/boss, build theo vũ khí | 2D side-scroll; loại vũ khí là thứ phải *tìm*, không có sẵn từ đầu | Chứng minh weapon tree sâu tự nó là động lực dài hạn |
| **Hollow Knight** | Cảm giác combat nhanh, phản hồi tức thì | Tốc độ gốc ngang hoặc nhanh hơn; nặng/nhanh là lựa chọn build; thêm đòn lướt và đòn đáp đất; không Metroidvania | Chuẩn tham chiếu cho combat feel |
| **Rune Factory** | Farming + combat + quan hệ + nhiều loại vũ khí | 2D side-scroll + weapon tree kiểu MH + vũ khí là khám phá | Đối thủ gần nhất — xác nhận thị trường, cũng là áp lực cạnh tranh |
| **Moonlighter** | "Hai cuộc sống" (cửa hàng ↔ dungeon) nuôi nhau | Nông trại + làng thay cửa hàng; thế giới handcrafted thay dungeon | Chứng minh cấu trúc hai nửa hoạt động |

**Non-game inspirations**: truyện dân gian và tranh khắc gỗ (cách dàn dựng vũ khí như "di vật huyền thoại"); hình ảnh làng quê lúc giờ vàng và rừng sương mù (trục ấm ↔ sương của Lantern & Mist).

---

## Target Player Profile

| Attribute | Detail |
| ---- | ---- |
| **Age range** | 18–35 |
| **Gaming experience** | Mid-core (đủ quen platformer/action để chịu được boss khó, nhưng cũng thích cozy) |
| **Time availability** | 30–60 phút buổi tối; phiên dài 2+ giờ cuối tuần |
| **Platform preference** | PC (Steam) |
| **Current games they play** | Terraria, Stardew Valley, Monster Hunter: World/Rise (thêm: Rune Factory, Hollow Knight) |
| **What they're looking for** | Một game vừa có chiều sâu build/vũ khí vừa có mái nhà ấm áp để quay về, trong 2D |
| **What would turn them away** | Build bắt buộc/meta duy nhất; bị phạt vì thời gian; mất nội dung vĩnh viễn vì một lựa chọn; grind farming bắt buộc |

**Không dành cho:** người muốn multiplayer/competitive; người muốn sandbox đào/xây; fan Metroidvania chiều sâu; người chơi cozy thuần không muốn đánh boss khó (có thể giữ qua tùy chọn độ khó, nhưng không thiết kế xoay quanh).

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | **Unity 6.3 LTS** (6000.3.27f1, ghim 2026-10-24) — lựa chọn của tác giả (nền tảng Unity/C#) |
| **Key Technical Challenges** | Combat feel (input buffer, hit-stop, cancel window, kinematic 2D controller tự viết); đồng bộ layer vũ khí với animation qua ~14 loại; lịch luôn trôi + mô phỏng thế giới ngoài màn hình (tick-on-load); save/load nhiều trạng thái có schema versioning; hệ condition/flag data-driven; camera pixel-perfect + parallax + fog |
| **Art Style** | Pixel art 2D, side-scroll, parallax nhiều lớp + sương |
| **Art Pipeline Complexity** | Medium — asset pack làm nền + tự vẽ thêm; nặng nhất là moveset theo họ vũ khí và sprite boss |
| **Audio Needs** | Moderate — nhạc theo vùng và ngày/đêm; SFX đòn đánh là trọng tâm combat feel |
| **Networking** | None |
| **Content Volume** | 1.0: 3 vùng phiêu lưu + hub (Farm, Green Field Village), 12–14 loại vũ khí (khoảng 12–14 là vùng đệm cắt giảm của producer; mục tiêu thiết kế ~14) × 2 nhánh + 1 nhánh bí mật, 3–4 boss, ~10–12 NPC, 4 mùa, ~20–30 giờ. Full vision: 14 loại × 3–5 nhánh, 6–8 vùng, ~8 boss, ~25 NPC, 30–50 giờ |
| **Procedural Systems** | Không — thế giới handcrafted |

**TD-FEASIBILITY: HIGH RISK** (cho toàn bộ phạm vi, trước khi áp dụng tier của producer). Không có gì bất khả thi; kiến trúc MVP khả thi trên Unity. Rủi ro đến từ phạm vi × game đầu tiên × solo × 1–2 năm — gỡ bằng phạm vi/timeline, không cần làm lại thiết kế kỹ thuật. Đã ghim Unity 6.3 LTS; prototype combat chạy đúng trên editor 6000.3.27f1 (TD-PHASE-GATE 2026-10-25). Mọi API Unity vẫn phải đối chiếu `docs/engine-reference/unity/`.

---

## Risks and Open Questions

### Design Risks
- **Hai nửa kéo nhịp về hai phía**: người chơi combat thấy farming chậm, người chơi cozy thấy boss nặng. Điều hòa bằng Pillar 3 và luật thua boss.
- **Lịch luôn trôi × boss khó**: nguy cơ cảm giác bị phạt thời gian. Hợp đồng đã chốt (Core Loop › Calendar Contract); cơ chế trong arena chưa chốt.
- **Bí mật quá khó tìm**: người chơi bỏ lỡ phần hay nhất (vũ khí, nhánh bí mật). Cần hệ thống tin đồn/gợi ý.
- **Expression yếu ngoài vũ khí**: chỉ Pillar 2 phục vụ Expression trực tiếp; trang trí nhà/nông trại chưa thuộc phạm vi.

### Technical Risks
- **Combat feel** là phần cần lặp nhiều nhất (thường 1–2 tháng với người làm lần đầu).
- **Hệ condition/flag** dễ phình thành ngôn ngữ scripting ngoài ý muốn — giữ tập predicate nhỏ (flag, affinity ≥ N, có item, ngày/mùa, trạng thái quest), data-driven, có unit test từ ngày đầu.
- **Save schema versioning** — cần trường version và đường migration ngay từ bản save đầu tiên; save data tách khỏi runtime object.
- **Camera pixel-perfect + parallax**: jitter sub-pixel là "hố thời gian" kinh điển — prototype sớm.
- **Hay bị đánh giá thấp**: điều hướng UI bằng tay cầm qua menu kho/rèn/cây vũ khí dày đặc; rebinding; tích hợp Steam/Epic SDK; processing theo thời gian qua ngủ/lưu; test ma trận boss × vũ khí.
- **Code prototype trong `Assets/_Prototypes/`** đang được compile vào game — cần asmdef hoặc cách loại trừ trước mọi production build.

### Market Risks
- **Rune Factory, Sun Haven** chiếm khán giả farming + combat; khác biệt phải rõ ngay trong trailer (side-scroll + weapon tree + vũ khí là khám phá).
- **Kỳ vọng của hai khán giả** (cozy và action) có thể không trùng; demo phải cho thấy cả hai nửa trong 20 phút.
- Tên "Evermeadow" chưa kiểm tra trademark/khả dụng trên store.

### Scope Risks
- **PR-SCOPE: OPTIMISTIC** — mọi ước tính ban đầu quá lạc quan với solo, game đầu tiên; đã áp dụng tier điều chỉnh của producer.
- **Hệ số nhân ẩn**: "mọi vũ khí hạ được mọi boss" → cân bằng tăng theo vũ khí × boss × phase × ngữ cảnh đòn × nhánh. Với số 1.0 (12–14 vũ khí × 3–4 boss × 2–3 phase × 4 ngữ cảnh đòn) là **288–672 ô**, chưa tính nhánh; tính 3 nhánh mỗi vũ khí (2 nhánh + 1 bí mật) là **864–2.016 ô** — không test tay được. Full Vision (14 loại × 3–5 nhánh, ~8 boss) chưa được tính ở đây. Cách kiểm: mô phỏng theo họ vũ khí (4–5 họ) + kiểm tay một vũ khí đại diện mỗi họ × mỗi boss. Mỗi boss mới phải kiểm lại mọi họ (hệ số chéo). Dự trù ~1 tuần cân bằng mỗi boss mới, ~3 ngày mỗi loại vũ khí mới, ngoài thời gian làm.
- **Moveset 4 ngữ cảnh** (đất, trên không, lướt, đáp đất) cho mỗi họ vũ khí — nhân chi phí moveset. Hướng đề xuất: đòn ngữ cảnh dùng chung, đổi diện mạo theo họ, cộng **một đòn đặc trưng** mỗi họ. TD + producer tính chi phí trước `/map-systems`.
- **Chi phí vũ khí**: loại đầu tiên 3–4 tuần (dựng pipeline), mỗi loại sau ~1,5–2 tuần. Loại thứ hai ≤ 2 tuần: đạt; > 2 và ≤ 3 tuần: cảnh báo; > 3 tuần: ước tính lại ngân sách vũ khí và xem lại số loại vũ khí 1.0 trước khi cam kết tier Demo.
- **Art là đường găng**: asset pack + tự vẽ — moveset theo họ vũ khí và sprite boss là phần pack khó phủ.
- **MVP làm song song** (combat + day loop cùng lúc) — lựa chọn của tác giả; TD khuyến nghị combat slice trước. Rủi ro: cảm giác combat chưa được chứng minh trong khi đã đầu tư vào day loop. Theo dõi: nếu combat feel chưa đạt sau tháng 2, dồn ưu tiên về combat.
- **Điểm cắt dự kiến** quanh tháng 18–22 (cắt một vùng + một boss). **Luật phụ thuộc:** một nhánh vũ khí không bao giờ phụ thuộc nội dung của tier sau; mỗi vùng là một gói kín (vùng + boss + NPC + nguyên liệu).

### Open Questions
- **Cơ chế "thua boss không hỏng nông trại"** với lịch luôn trôi: đóng băng giờ trong arena? chết thì tua về trước arena? chi phí thời gian cố định nhỏ? → `/design-system` cho Calendar/Time, trước khi kiến trúc hệ thời gian.
- **Danh sách ~14 loại vũ khí và cách gom vào 4–5 họ animation** (light blade, heavy, polearm, ranged, catalyst…) → `/map-systems` rồi GDD Weapon Tree.
- **4 social stats** — để sau 1.0 (quyết định 2026-10-25), trừ khi một GDD tìm được vai trò pillar cho chúng.
- **Hitstun có khóa đòn đánh không** (prototype: 0.25s, ngoại lệ duy nhất của luật "không khóa đòn đánh") → GDD Combat.
- **Đòn lướt có giữ i-frame không** (prototype: không) → GDD Combat.
- **Dải tốc độ của từng họ vũ khí** → GDD Weapon Tree.
- **Trọng lượng đòn đánh đến từ đâu** — hit-stop, knockback, âm thanh, phản ứng quái, với số cụ thể → GDD Combat.
- **Độ phân giải nội bộ** 640×360 hay 480×270 (ảnh hưởng silhouette vũ khí và quy đổi số tuning) → `/art-bible`, trước `/design-system combat`.
- **Ba vùng phiêu lưu của 1.0** là vùng nào (Mist Forest + Mist Village/vùng kế tiếp + ?) → `/map-systems` / level design.
- **Tùy chọn độ khó** cho người chơi cozy: có hay không → GDD Combat.
- **Hệ thống gợi ý bí mật** (tin đồn, nhật ký) — **bắt buộc** theo ngân sách khám phá ở Pillar 1; thiết kế cụ thể → GDD Discovery/Quest.
- **Tên game** — Evermeadow là tên làm việc; kiểm trademark trước khi công bố.

---

## MVP Definition

**Core hypothesis**: Vòng lặp *nông trại → lò rèn → đi săn* trên một lịch luôn trôi khiến người chơi muốn "thêm một ngày nữa", và việc tìm thấy một loại vũ khí mới cảm giác như một khám phá.

**Cổng vui (fun gate)** — ngưỡng đã đóng băng, không chỉnh sau khi buổi test bắt đầu. **Mẫu: một nhóm n = 5 người test bên ngoài**, không phải người thiết kế. Đạt khi **cả H1 và H2 đều ≥ 4/5**:
- **H1 (vòng lặp ngày):** ít nhất 4/5 người test bên ngoài tự chọn chơi sang ngày thứ 4 trở đi, sau điểm dừng tự nhiên đầu tiên (đi ngủ). Ghi thêm số ngày chơi trung vị.
- **H2 (khám phá):** ít nhất 4/5 người tìm thấy vũ khí thứ hai nói được nó đến từ đâu, và nói muốn tìm thêm.
- **Nếu không đạt** (H1 hoặc H2 < 4/5): một vòng sửa, rồi test lại với nhóm 5 người mới; không đạt lần hai → quay lại `/prototype` và quyết định pivot.
- **Điều kiện bằng chứng:** ngưỡng và cách đo được ghi vào `/playtest-report` trước buổi test đầu tiên; log tự động (số ngày chơi, số chuyến, lúc bỏ), chạy thử phần đo trước khi họ chơi; kết quả ghi bằng `/playtest-report`. Áp dụng cho prototype vòng lặp ngày và vertical slice (tiêu chí bắt buộc của CD-PLAYTEST).

**Required for MVP** (bản rút gọn theo PR-SCOPE; làm song song combat + day loop):
1. Combat: kinematic controller, input buffer, hit-stop, cancel sang né — đạt combat feel mục tiêu.
2. **2 loại vũ khí** × 2 nhánh sidegrade + **1 nhánh bí mật**; 2–3 skill stone.
3. **1 boss 2 phase** trong arena thiết kế riêng.
4. Mist Forest (1 vùng phiêu lưu) + Farm hub + Green Field Village (1 màn hình).
5. Lịch ngày/đêm, 4 loại cây trồng, ngủ = lưu.
6. Lò rèn + 1 trạm chế tạo; chuỗi nguyên liệu quái → vũ khí, cây → potion.
7. **2 NPC** có affinity, 1 hidden quest mở nhánh bí mật.
8. Art placeholder / asset pack.

**Explicitly NOT in MVP** (defer to later):
- Mùa (seasons) đầy đủ — MVP chỉ ngày/đêm.
- Mist Village và các vùng khác.
- Affinity âm và cơ chế hàn gắn (chỉ cần chiều tăng để test hook).
- 4 social stats.
- Processing theo thời gian, Utility crafting.
- Art cuối cùng, nhạc cuối cùng.
- Steam SDK, rebinding, UI polish.

### Scope Tiers (if budget/time shrinks)

Đã áp dụng điều chỉnh PR-SCOPE (solo full-time). Nguyên tắc: **cắt vùng đất, boss và độ dài nhánh trước — giữ số loại vũ khí.**

| Tier | Content | Features | Timeline (tích lũy) |
| ---- | ---- | ---- | ---- |
| **MVP (rút gọn)** | Mist Forest + Farm hub + làng 1 màn hình; 2 loại vũ khí; 1 boss 2 phase; 2 NPC | Combat, weapon tree cơ bản, farming ngày/đêm, forge, affinity tăng, 1 hidden quest | 3–4 tháng (4–6 nếu không rút gọn) |
| **Free Demo** (Steam Next Fest) | 2 vùng, 6 loại vũ khí (từ 3 họ animation), 2 boss, 6–8 NPC, 2 mùa | Core + tiến trình qua tier đầu, crafting đầy đủ hơn | 12–14 tháng |
| **Early Access** (tùy chọn) | 3 vùng, ~10 loại vũ khí, 3 boss, ~10 NPC, 4 mùa | Mọi hệ thống chính, affinity âm + hàn gắn, nhánh nguyên tố | ~18 tháng |
| **1.0** (mục tiêu 24 tháng) | 3 vùng, **12–14 loại vũ khí** × 2 nhánh + 1 nhánh bí mật mỗi loại, 3–4 boss, ~10–12 NPC, 4 mùa | Mọi tính năng, polish, Steam/Epic | 24 tháng |
| **Full Vision** | 14 loại × 3–5 nhánh, 6–8 vùng, ~8 boss, ~25 NPC | Mọi tính năng + lớp polish "Breathing Meadow" | 36+ tháng (cập nhật sau phát hành) |

*Nếu giả định thay đổi:* part-time → nhân đôi mọi mốc (1.0 trong 2 năm trở thành không thực tế); có họa sĩ pixel riêng → art rời khỏi đường găng, MVP gốc 2–3 tháng và 1.0 20–24 tháng trở nên khả thi.

**Cổng tiến độ (schedule gate)** — tách riêng với cổng vui: MVP rút gọn xong trong 4 tháng, và loại vũ khí thứ hai mất ≤ 2 tuần (đạt) — 2–3 tuần là cảnh báo, > 3 tuần kích hoạt ước tính lại (xem Scope Risks › Chi phí vũ khí).

---

## Next Steps

- [x] Configure Unity (`/setup-engine`) — Unity 6.3 LTS ghim 2026-10-24
- [x] Prototype combat feel (`/prototype`) — PROCEED 2026-10-25 (`prototypes/combat-feel-concept/REPORT.md`)
- [ ] Prototype vòng lặp một ngày (`/prototype`) — farm → làng → rèn → săn → ngủ
- [ ] Visual identity (`/art-bible`) — từ Visual Identity Anchor ở trên
- [ ] Validate this concept (`/design-review design/gdd/game-concept.md`)
- [ ] Decompose into systems (`/map-systems`)
- [ ] Design each system (`/design-system [system-name]`) + `/design-review` sau mỗi GDD
- [ ] Architecture (`/create-architecture` → `/architecture-decision` → `/architecture-review`)
- [ ] Vertical slice in Pre-Production (`/vertical-slice`) và `/playtest-report`
- [ ] Plan first milestone (`/sprint-plan new`)
