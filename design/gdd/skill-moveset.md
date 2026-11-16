# Skill & Moveset

> **Status**: Designed (chờ `/design-review` trong phiên mới)
> **Author**: Đặng Đình Dũng + Claude
> **Last Updated**: 2026-11-05
> **Last Verified**: 2026-11-05
> **Implements Pillar**: Pillar 1 (Every Weapon Is a Discovery), Pillar 2 (Your Build, Your Way); phục vụ phụ Pillar 3 (Two Lives, One Loop — stone và vũ khí chỉ đổi ở nhà)
> **Creative Director Review (CD-GDD-ALIGN)**: CONCERNS 2026-11-05 → REVISED 2026-11-05 — (C1) thêm hệ số tốc độ theo nhánh `timeline_scale` (Rule 34, F9); (C2) **không đặt trần** `commit_time` cho đòn thường — người dùng chấp nhận rủi ro họ vũ khí chậm nhất kéo nhịp boss (OQ-S10); (C3) sửa ba câu mâu thuẫn "animation quyết định thời gian"; (C4) dọn Quick reference trùng và tên `land_policy`

## Summary

Skill & Moveset biến mọi đòn — từ nhát chém thường tới kỹ năng đặc biệt của Skill Stone — thành dữ liệu: mỗi kỹ năng
là một file XML được xuất từ đồ thị node (Start → End, sự kiện có độ trễ giây) soạn trong Unity Editor, và mỗi họ vũ
khí có một bảng tổ hợp ánh xạ "nút Đánh + hướng / trạng thái / đòn trước" sang kỹ năng. Người chơi chỉ có hai nút
(Đánh, Kỹ năng đặc biệt) nhưng mỗi vũ khí "nói" theo cách riêng (Pillar 1), còn stone gắn ở nhà quyết định nút thứ hai
(Pillar 2); Character Controller thực thi đòn và chuyển động có giới hạn, Combat sở hữu sát thương.

> **Quick reference** — Layer: `Core` (đổi từ Feature 2026-11-06: Controller, tầng Core, phải tham chiếu mô hình kỹ năng; ADR-0002) · Priority: `MVP` · Key deps: `Character Controller, Combat, Input, Game Data & Tuning Config, Loadout & Player Stats`

## Overview

Skill & Moveset quyết định **nhân vật làm gì khi người chơi bấm nút đánh**. Mọi đòn trong game, từ cú chém
thường đầu chuỗi tới kỹ năng đặc biệt của một Skill Stone, đều là **một kỹ năng**: một file XML mô tả trọn vẹn
đòn đó theo thời gian — một chuỗi node từ **Start** tới **End**, mỗi node có độ trễ tính bằng giây: clip hình nào
chạy và khi nào, vùng gây sát thương xuất hiện ở đâu và lúc nào, nhân vật lướt tới hay bật lên bao xa, đòn hủy được bằng gì, hồi chiêu bao lâu. Người
làm game soạn kỹ năng bằng một **đồ thị kéo thả node** trong Unity Editor; khi lưu, đồ thị được xuất thành XML,
và game chỉ đọc XML. Mỗi **họ vũ khí** có một **bảng tổ hợp** ghi "tổ hợp nút nào ra kỹ năng nào": bấm Đánh
thường đứng yên, Đánh khi đang chạy, Lên + Đánh, Đánh trên không, Đánh ngay sau khi lướt, giữ Đánh để tụ lực… Mỗi
dòng trỏ tới một id kỹ năng. Người chơi chỉ có **hai nút**: **Đánh thường** và **Kỹ năng đặc biệt**; kỹ năng
đặc biệt đến từ **Skill Stone** người chơi gắn vào vũ khí, id kỹ năng của mỗi stone do người làm game định sẵn.

Với người chơi, hệ này là thứ làm **mỗi vũ khí có một cách đánh riêng**: cùng hai nút, nhưng cầm thương thì Lên +
Đánh là cú hất lên trời, cầm búa thì giữ Đánh là đòn nện tụ lực. Học tổ hợp của một vũ khí mới là một phần của
khám phá (Pillar 1); chọn vũ khí và stone nào là chọn lối đánh (Pillar 2). Không có hệ này, mọi vũ khí sẽ là cùng
một chuỗi ba nhát ở những tốc độ khác nhau — đúng điều concept muốn tránh ("để 14 loại vũ khí không thành một kiểu
chém ở 14 tốc độ").

Hệ này **không** quyết định sát thương bao nhiêu hay trúng ai (đó là Combat), không di chuyển nhân vật trực tiếp
(nó yêu cầu Controller thực hiện các chuyển động có giới hạn), và không quyết định vũ khí nào đang cầm hay stone
nào đang gắn (đó là Loadout). Công cụ soạn đồ thị → ADR-0003; định dạng XML, lõi và cách nạp → ADR-0002.

## Player Fantasy

**"Hai nút, mười bốn tiếng nói."**

Lần đầu cầm cây thương vừa được thần trao lại, người chơi bấm Lên + Đánh như đã bấm cả trăm lần với thanh kiếm,
và cây thương hất bổng con quái lên trời. Khoảnh khắc "ồ, cái này làm được vậy à" là trái tim của hệ này. Vẫn hai
nút, vẫn những tổ hợp tay đã quen, nhưng mỗi họ vũ khí trả lời bằng tiếng nói riêng: thương vươn lên trời, búa nện
xuống đất, dao găm lướt xuyên qua. Người chơi không phải học lại cách bấm, chỉ học lại vũ khí. **Bất ngờ chỉ đến
một lần:** từ lần thứ hai, cùng tổ hợp luôn ra cùng đòn, để bất ngờ hôm nay thành công cụ ngày mai.

Nút thứ hai thuộc về người chơi: **Skill Stone là câu cuối người chơi tự viết.** Bảng tổ hợp là vốn từ chung của
họ vũ khí; viên stone gắn ở lò rèn tối qua quyết định Kỹ năng đặc biệt nói gì trong trận hôm nay.

Hệ này phục vụ trực tiếp **Pillar 1 (Every Weapon Is a Discovery)** — khám phá nằm ngay trong tay, ở lần bấm đầu
tiên — và nửa sau của **Pillar 2 (Your Build, Your Way)** qua Skill Stone. Phần "tay dần quen nhịp vũ khí" (làm
chủ) không phải tiêu đề của hệ này; nó được giữ ở Game Feel và Acceptance Criteria (đòn đọc được trước, quãng di
chuyển giống nhau mọi lần).

**Nối tiếp lời hứa của Character Controller ("Nhẹ chân, nặng tay"; không bao giờ "ì" hay "bị khóa").** Đòn chỉ
được kéo nhân vật đi khi chính người chơi đã bấm ra đòn đó, chỉ trong lúc đòn chạy, và luôn **cộng thêm** vào
điều khiển của người chơi chứ không thay thế: giữ hướng thì nhân vật vẫn đi theo hướng đó. Mọi đòn đều có lối ra,
và lối ra luôn là một chuyển động (nhảy ở mọi pha; né trong cửa sổ đã khai báo). Sức nặng của vũ khí nặng nằm ở
chính đòn — lấy đà, thu đòn — không ở tốc chạy hay độ cao nhảy.

**Binding test.** Fantasy này bị phản bội nếu xảy ra bất kỳ điều nào sau:
1. Sau khi thử hai họ vũ khí, playtester tả cùng một ô tổ hợp (ví dụ Lên + Đánh) là "giống nhau, chỉ nhanh hay
   chậm hơn".
2. Cùng họ vũ khí, cùng tổ hợp, cùng trạng thái mà ra hai đòn khác nhau.
3. Một vũ khí bắt người chơi học kiểu bấm mới: nút thứ ba, hoặc chuỗi lệnh kiểu game đối kháng (↓↘→).
4. Sau 5 phút tự do với một vũ khí mới, người chơi không tự tìm ra được đòn nào ngoài chuỗi Đánh thường.
5. Trong lúc đòn chạy, **phần điều khiển của người chơi** (vận tốc trừ lực của kỹ năng) lệch khỏi hướng đang giữ ×
   tốc dáng × hệ số Controller × `speed_mult` (trừ khi bị trói hoặc chạm tường); hoặc bấm né / nhảy trong cửa sổ đã
   khai báo mà không ra.
6. Trên một họ vũ khí, một Skill Stone thành lựa chọn mặc định của đa số playtester (anti-pillar "không có build
   bắt buộc").

Câu đo playtest (đưa vào Acceptance Criteria): sau khi cầm vũ khí mới, người chơi có kể lại được ít nhất một đòn
"không ngờ" bằng lời của họ không.

*Ý kiến creative-director (2026-11-05): chọn hướng A trong ba hướng (A "Hai nút, mười bốn tiếng nói", B "Bàn tay
quen việc", C "Lối đánh mang tên mình"), ghép câu Skill Stone từ C. Người dùng chọn bảng tổ hợp **tự do hoàn
toàn** (không có "ngữ pháp" chung theo dòng) — rủi ro một họ vũ khí thiếu công cụ cho pha boss trên không được
chuyển sang kiểm tra của Boss Encounter (Open Questions).*

## Detailed Design

*Tổng hợp từ game-designer (bảng tổ hợp, vòng đời, Skill Stone), systems-designer (cấu trúc dữ liệu, kiểm tra hợp
lệ, giới hạn) và gameplay-programmer (khả thi), 2026-11-05. Các giới hạn số được đặt tên ở đây và định nghĩa ở
Formulas / Tuning Knobs. Cách hiện thực: định dạng XML, lõi, quy về bước, cách nạp, kiểm tra → ADR-0002 (Proposed); công cụ đồ thị, xuất, chống lệch → ADR-0003 (Proposed, chờ spike).*

### Core Rules

**Kỹ năng là dữ liệu**

1. **Mọi đòn là một kỹ năng.** Đòn đánh thường (từng nhát của chuỗi, đòn trên không, đòn khi lướt, cắm xuống) và
   kỹ năng đặc biệt đều cùng một dạng dữ liệu: một file kỹ năng. Không có loại đòn nào viết cứng trong code, trừ các
   **kỹ năng dự phòng** tích hợp sẵn (Rule 22).
2. **Nguồn và bản chạy.** Mỗi kỹ năng là **một file XML** (một kỹ năng, một file) trong `Assets/StreamingAssets/Skills/`;
   XML là **nguồn duy nhất** và game **chỉ đọc XML**. Người làm game mở và tạo file đó bằng **Skill Tool** — cửa sổ đồ
   thị node trong Unity Editor (menu `Evermeadow ▸ Skills ▸ Skill Tool`, cạnh công cụ animation): mở XML thành luồng node,
   sửa, lưu lại XML. Đồ thị chỉ là cách xem và sửa, không lưu thành file trong project (quyết định người dùng 2026-11-06).
   Không sửa XML bằng tay: file ghi kèm `payload_hash`, và bước kiểm tra (Rule 21) báo file bị sửa ngoài Skill Tool.
3. **Định danh.** `id` của kỹ năng **là tên file XML**: một số **6 chữ số, từ 100000** (ví dụ `100000.xml` → `100000`;
   quyết định người dùng 2026-11-06). Skill Tool cấp id kế tiếp còn trống khi tạo kỹ năng mới; trong file không có
   thuộc tính `id`. `id` là khóa vĩnh viễn mà người làm game gán vào **đòn đánh thường** (dòng của bảng tổ hợp), **bùa
   chú / Skill Stone** và dữ liệu lưu; tên node và tên hiển thị không phải khóa. Tên hiển thị là một khóa chuỗi (`name_key`), đổi tự do. Đổi `id` chỉ qua **bảng đổi tên**
   (`id_cũ → id_mới`); xóa kỹ năng còn bị tham chiếu thì giữ một bản thay thế đánh dấu `deprecated` trỏ tới kỹ năng
   mới. Mỗi file có `schema_version`; game từ chối phiên bản mới hơn nó biết và chạy bước chuyển đổi cho phiên bản cũ.

**Một kỹ năng gồm gì**

4. **Phần đầu (header) — chỉ còn `owner`** (quyết định người dùng 2026-11-08). File kỹ năng và Skill Tool **chỉ thể hiện
   kỹ năng trông và tác động thế nào**: animation, hiệu ứng, vùng chạm, chuyển động, dòng thời gian. Header chỉ có
   `schema_version` và `owner` (`player` | `creature`, mặc định `player` — Rule 4a); `id` là tên file (Rule 3).
   **Logic chi tiết của kỹ năng do hệ thống gán id quản lý, không nằm trong file kỹ năng:** combo và liên kết nối đòn
   (bảng tổ hợp), loại đòn thường / đặc biệt, tình huống được dùng (đất / trên không / khi né), họ vũ khí, hồi chiêu,
   hành vi khi chạm đất, tên hiển thị, nhãn. Các trường cũ `name_key`, `kind`, `contexts`, `weapon_families`, `tags`,
   `cooldown`, `land_policy` bị bộ đọc từ chối (`UnknownAttribute`).

   **Luật trong GDD này chuyển sang hệ thống gán id** (OQ-S15 chốt hệ nào sở hữu): `kind` và hồi chiêu của kỹ năng đặc
   biệt (Rule 4, 16–19 → Skill Stone / Loadout); `contexts` của kỹ năng (Rule 4; bảng tổ hợp giữ `context` của **dòng**);
   `land_policy` (Rule 4 → Controller / hệ logic kỹ năng); `weapon_families` (Rule 4, 16 → thành viên của bảng tổ hợp /
   Skill Stone); `tags`, tag `utility` và liên kết `after: tag:` (Rule 5, 6, 11, 13 — bỏ; `after` chỉ nhận id). 

   **Node logic và bảng tổ hợp cũng rời Skill Tool** (quyết định người dùng 2026-11-08, cùng ngày): node **CancelWindow,
   HoldGate, Move, Armor** không còn là node kỹ năng (bộ đọc từ chối `UnknownNode`) — **hệ thống tạo combo** riêng sẽ sở
   hữu cửa sổ hủy, giữ để tụ lực, chuyển động của đòn và giáp; tab **Command Table** bị bỏ khỏi Skill Tool (lõi đọc và
   chọn bảng tổ hợp vẫn giữ để hệ combo dùng). Vì vậy **Rule 7 (node chuyển động), 8 (giáp), 9 (cửa sổ hủy), 12 (HoldGate),
   phần dòng `dive` của Rule 15, F3–F6, F8, và AC nhóm H.3, H.4, H.5 phần giáp** chuyển sang hệ combo; trong GDD này chúng là
   **tham khảo cho hệ đó**, không còn ràng buộc file kỹ năng. Node còn lại của Skill Tool: **PlayAnimation, Hit, Cue,
   Spawn, LookAt, Search, End**. AC liên quan tới các trường / node đã bỏ (AC-D12 phần header, AC-C15, phần `utility` của
   AC trong H.6, AC về HoldGate / Move / Armor / CancelWindow) **chờ viết lại** theo hệ thống mới.
4d. **Skill Graph — cây sự kiện theo tool tham khảo** (quyết định người dùng 2026-11-09; **thay Rule 4b, 4c, 5, 5a, 6** và
    đảo một phần quyết định 2026-11-08). File kỹ năng (`schema_version="2"`) là một **đồ thị sự kiện**, không còn dòng thời
    gian phẳng hay node End:
    - **Node** có số thứ tự (`11:Animation`; Start luôn là `0`) và một trong các loại: **Start, Animation, Effect, Search,
      Hit, Move, MoveControl, LookAt, CameraAction, Knockback, Buff**. Mọi node (trừ Start) có ba trường chung
      **ActionDelay** (giây chờ sau điều kiện của dây vào), **CanCancel** (bị dừng khi kỹ năng bị hủy), **UseAttackSpeed**
      (co giãn theo tốc đánh), và cổng ra **NodeID**. Trường và cổng ra của từng loại khai báo trong một bảng schema
      (`SkillSchema`): Effect (prefab, pivot, xoay, bám / quay quanh), Search (cờ đối tượng, `TargetTID`, MaxCount, vùng
      Arc / Box; ra Count, CreatureGIDs, CreaturePositions), Hit (đánh một lần hoặc kéo dài Steady, hệ số, hiệu ứng trúng,
      vùng, giết ngay; ra LastHitCreatureGID…), Move (đích theo pivot, Straight / Jump, Time / Speed), MoveControl, LookAt,
      CameraAction, Knockback, Buff.
    - **Dây luồng** cha → con mang **điều kiện**: khi node cha bắt đầu / kết thúc; **mỗi lần trúng một creature** (chỉ từ
      Hit); khi tìm thấy / không tìm thấy (chỉ từ Search). Một node có **một** cha, một cha có **nhiều** con; không vòng lặp.
      Kỹ năng kết thúc khi mọi node đã xong.
    - **Mọi trường** gõ giá trị hoặc **nối dây** từ cổng ra của **Start hoặc một node tổ tiên**, cùng kiểu (Int được vào
      Float / Long). Start ra: SkillID, SkillLevel, CastRatio, SkillInputType, IsChanneling, OwnerGID, OwnerTID,
      OwnerPosition, OwnerDirection, OwnerLevel, ExternalTargetGID, ExternalPosition.
    - **Vec3** cho vị trí / hướng (game 2D: x, y mặt chơi; z chiều sâu). **GID** (Long) = id thực thể lúc chạy (-1 = chủ kỹ
      năng); **TID** (Int) = mã loại (Creature 100–999).
    - **Memo** (ghi chú vàng) và vị trí node lưu cùng file; tool có Copy / Paste / Undo / Redo.
    - Quay lại Skill Tool: **Move, MoveControl, Knockback, Buff, CameraAction**. Vẫn **không** trong Skill Tool: bảng tổ hợp
      (combo), CancelWindow, HoldGate, Armor (hệ combo). Ngữ nghĩa lúc chạy của từng node (khi nào "kết thúc", hệ số Hit…)
      → OQ-S16.
    - **Node End** *(thêm lại 2026-11-14 — quyết định người dùng)*: tới End (sau ActionDelay) thì **kỹ năng kết thúc**:
      animation của chủ kỹ năng trở về idle, nhân vật hết đòn (được điều khiển lại; cửa sổ nối đòn tính từ đây); node **chưa
      bắt đầu** bị bỏ (kể cả con `end` của node còn chạy); node **đang chạy** (Hit, Effect…) chạy hết thời lượng của nó.
      End không có node con (`EndHasChildren`); nhiều End được (mỗi nhánh một End), End tới trước thắng. **Không có End**:
      kỹ năng kết thúc khi không còn node chờ và chỉ còn node không có hạn (animation lặp), những node đó dừng theo.
    - **Thời lượng node lúc chạy** (bộ chạy tối thiểu 2026-11-14, `SkillRun`): Animation = độ dài clip / `TimeScale`
      (lặp = tới khi kỹ năng kết thúc; clip không lặp giữ frame cuối); Effect = `Duration` (0 = theo prefab); Hit =
      `SteadyDuration` khi Steady, không thì tức thời; Move = `Value` khi `MoveStandard = Time`; MoveControl /
      CameraAction / Knockback = `Duration`; còn lại tức thời. Mọi thời gian theo luật bước 1/60 s (Rule 23).
4a. **Kỹ năng cho Player và cho quái** (quyết định người dùng 2026-11-07). Skill Tool soạn được kỹ năng cho cả người
    chơi lẫn quái; node Start khai báo `owner`. Kỹ năng `player` gán vào đòn đánh thường (dòng bảng tổ hợp) hoặc Skill
    Stone; kỹ năng `creature` do **Enemy AI** gán và chạy. Bảng tổ hợp chỉ chứa kỹ năng `player` (validator báo
    `RowCreatureSkill`). Mọi luật về dòng thời gian, node và kiểm tra trong GDD này áp cho cả hai, trừ chỗ ghi riêng.
4b. **Giá trị toàn cục của node Start.** Lúc kỹ năng bắt đầu, Start **chụp một lần** các giá trị và các node khác dùng
    chúng qua **dây dữ liệu** nối từ Start (giá trị không cập nhật khi Owner hay Target di chuyển sau đó — người chơi né
    được đòn nhắm vào vị trí cũ):
    - **Owner Position**, **Owner ID** — vị trí và id thực thể của chủ kỹ năng (người chơi hoặc quái);
    - **Target Position**, **Target ID** — **chỉ kỹ năng `creature`**; Target là **người chơi**. Kỹ năng `player` dùng
      giá trị Target bị từ chối (`TargetInPlayerSkill`); trên Skill Tool hai cổng này ẩn khi Owner = Player.

    Mỗi loại node khai báo các **cổng vào dữ liệu** của nó (một bảng chung, nên thêm node mới như Effect, Projectile chỉ
    là thêm một dòng): `origin` (vị trí, nơi hiệu ứng xảy ra), `toward` (vị trí, hướng nhắm / lướt / quay mặt), `only`
    (id, chỉ tác động lên thực thể đó). Dây nối sai kiểu (vị trí vào cổng id hay ngược lại) bị từ chối (`BadInput`).
    Cổng không nối dùng mặc định của node (gắn theo Owner, theo hướng mặt). XML ghi cổng là thuộc tính, ví dụ
    `origin="target.position"`, `only="target.id"`.
4c. **Node Search — tìm Creature hoặc Player** (quyết định người dùng 2026-11-07). Search là node **trên luồng** (có
    độ trễ như node khác), chạy đúng lúc luồng tới nó, và **rẽ luồng** thành hai nhánh: **Found** (tìm được ít nhất một)
    và **Not Found**, mỗi nhánh có End riêng (như HoldGate). Tham số: `for` (`creature` | `player` | `any`), `gid` (tùy
    chọn — **GID là mã của Creature, 100–999**, mỗi loại Monster / NPC một mã; mọi con cùng loại chung GID nên kết quả có
    thể là **danh sách**), `radius` (ô tile, > 0) quanh `center` (cổng vào vị trí; không nối = vị trí Owner). Kết quả
    xếp từ gần tới xa, chụp lúc Search chạy, và ra ba cổng dữ liệu: **Result** (danh sách thực thể), **Nearest** (thực thể
    gần nhất), **Nearest Position** (vị trí của nó). Kết quả **chỉ dùng được trong nhánh Found** (dùng trước Search
    hay trong Not Found bị từ chối, `SearchValueOutsideFound`). Kỹ năng `player` cũng dùng Search được (ví dụ khóa mục
    tiêu quái gần nhất). **Mỗi kỹ năng có tối đa một Search**, không lồng trong nhánh (`OneSplit`). Mã GID thuộc dữ liệu quái của Enemy AI.
5. **Dòng thời gian: từ node Start tới node End** (quyết định người dùng 2026-11-05 — bỏ thời lượng pha khai báo).
   Đồ thị của một kỹ năng là một **luồng node** bắt đầu ở **Start** và kết thúc ở **End**; mỗi node có **độ trễ tính
   bằng giây** so với node trước trên luồng. Kỹ năng kết thúc khi luồng tới End. Thời gian chạy trên **đồng hồ Game**
   (có hit-stop và pause); một sự kiện xảy ra ở bước 1/60 s đầu tiên có thời điểm ≥ thời điểm khai báo của nó. Khi
   xuất, công cụ **trải phẳng luồng thành một dòng thời gian** (mỗi sự kiện một mốc giây tính từ Start; một dòng thời
   gian cho mỗi nhánh của HoldGate), nên game không phải diễn giải đồ thị lúc chạy. **Animation không quyết định thời
   gian**: node `PlayAnimation(anim, delay)` chỉ phát clip của khóa `anim` (Presentation ánh xạ khóa sang clip của từng
   look vũ khí); độ dài đòn do vị trí của End quyết định.

   **Các giai đoạn được suy ra từ dòng thời gian, không khai báo**, và các luật khác dùng chúng:
   - `windup` (lấy đà) = từ Start tới lúc cửa sổ hit đầu tiên mở;
   - `charging` (tụ lực) = thời gian chờ ở node HoldGate (Rule 12), nếu có;
   - `active` (chạm) = từ lúc cửa sổ hit đầu tiên mở tới lúc cửa sổ hit cuối cùng đóng;
   - `recovery` (thu đòn) = từ đó tới End; chia thành `locked` (tới lúc **cửa sổ hủy** cho né / đòn kế mở — Rule 9) và
     `open`. `cancel_lockout` = thời điểm cửa sổ đó mở − đầu `recovery`.
   - Kỹ năng không có cửa sổ hit (tag `utility`): `windup` = 0, `active` = 0, cả đòn là `recovery`.
   - `total` = thời điểm của End (với HoldGate tính `charge = hold_max` khi so trần; node `dive` tính tới lúc chạm đất).
6. **Các loại node sự kiện** (mỗi node có độ trễ giây; node có thời lượng ghi thêm `duration`). *Từ 2026-11-08 Skill
   Tool chỉ còn PlayAnimation, Hit, Cue, Spawn, LookAt, Search, End; các mục Move, CancelWindow, Armor, HoldGate dưới đây
   là tham khảo cho hệ combo (Rule 4).*
   - **PlayAnimation** — phát clip của khóa `anim` (Rule 5). Mỗi kỹ năng có ít nhất một node này ở độ trễ 0.
   - **Hit** — cửa sổ gây sát thương `duration` giây, kèm `hit_profile_id` do **Combat** định nghĩa (hình dạng vùng
     chạm, sát thương, hit-stop, knockback). Kỹ năng **không chứa con số sát thương**. Một kỹ năng có thể có nhiều cửa
     sổ hit; kỹ năng không có cửa sổ hit nào phải mang tag `utility`. Cổng vào: `origin` (đặt tâm vùng chạm tại vị trí
     đó, thay vì gắn theo Owner), `only` (chỉ gây sát thương lên id đó).
   - **Spawn** — tạo projectile / vùng hiệu ứng do Combat sở hữu (tham chiếu id). Cổng vào: `origin` (vị trí tạo),
     `toward` (hướng bắn).
   - **Move** — yêu cầu chuyển động (Rule 7). Cổng vào: `toward` (chỉ `lunge`: lướt về phía vị trí đó thay vì theo hướng
     mặt).
   - **LookAt** (thay Face, 2026-11-07) — Owner quay nhìn vào đối tượng kỹ năng nhắm tới: cổng `toward` (bắt buộc nối)
     nhận **vị trí, một thực thể hoặc danh sách** (danh sách → thực thể gần Owner nhất); thường lấy từ Search. Dùng cho
     **cả kỹ năng `player` lẫn `creature`** (ngoại lệ có tên của Rule 10).
   - **Search** — điểm rẽ tìm thực thể (Rule 4c).
   - **CancelWindow** — mở cửa sổ hủy cho né / đòn kế / cắm xuống (Rule 9).
   - **Cue** — gọi hiệu ứng hình (VFX), âm thanh, gợi ý hit-stop; độ trễ riêng cho phép coder và người làm hiệu ứng
     canh hiệu ứng theo hình; cue thiếu chỉ cảnh báo, không chặn. Cổng vào: `origin` (phát tại vị trí đó).
   - **Armor** — cửa sổ giáp `duration` giây, chỉ cho `special` (Rule 8).
   - **HoldGate** — điểm rẽ nhánh tụ lực (Rule 12).
   - **Start**, **End** — mỗi luồng có đúng một Start; mỗi nhánh kết thúc ở đúng một End. Node có thời lượng (Hit,
     Armor, Move) phải kết thúc trước hoặc đúng End.
7. **Node chuyển động — yêu cầu có giới hạn, thực thi bởi Controller.** Bốn loại node:
   - `lunge(quãng_đường, thời_lượng, hướng)` — lướt tới; `hướng` là `facing` hoặc `opposite` (chỉ đổi hướng lực,
     không lật vùng chạm).
   - `launch(độ_cao)` — bật lên **từ mặt đất**; đặt vận tốc lên, **không tiêu lượt nhảy**. Node `launch` bắt đầu
     khi nhân vật đang trên không thì bị bỏ qua (phần còn lại của kỹ năng chạy bình thường), để bật lên không cộng
     dồn với nhảy và nhảy đôi (quyết định người dùng 2026-11-05).
   - `halt()` — xóa lực của chính kỹ năng này (không khóa người chơi).
   - `dive()` — lao thẳng xuống với `plunge_speed` của Controller (dùng cho cắm xuống). **Ngoại lệ có tên** của
     luật cộng bên dưới: `dive` đặt vận tốc ngang = 0 như Plunge của Controller Rule 12, và hủy được bằng né trên
     không ở mọi pha (Rule 9 (d)).

   **Luật chung:** vận tốc của nhân vật = **phần điều khiển của người chơi** (hướng đang giữ × hệ số của Controller
   Rule 13) **+ lực của kỹ năng**. Hai phần **cộng** với nhau, không thay thế: giữ hướng thì nhân vật vẫn đi theo
   hướng đó (Controller binding test 2), giữ ngược hướng thì cú lướt ngắn lại. Khi nhân vật bị làm chậm (`speed_mult`
   < 1 của Controller Rule 25), lực của kỹ năng cũng nhân `speed_mult`, để đòn không thành cách lách hiệu ứng chậm.
   Lực của kỹ năng mất khi: người chơi **nhảy** (đòn vẫn diễn tiếp — Rule 9), kỹ năng bị hủy bằng né, hitstun, bị trói
   (root), kỹ năng kết thúc, đụng tường, hết thời lượng của node. Lướt khỏi mép thì lực đi tiếp tới hết node, trọng lực
   vẫn chạy (lunge không đặt `vy = 0` như né). Va chạm, dốc, mép vực vẫn do
   Controller xử lý; lực bị tường chặn thì dừng, không đẩy xuyên. Node chuyển động phải **bắt đầu trước khi cửa sổ hit
   cuối cùng đóng** (không lướt trong `recovery` của đòn thường — thu đòn luôn là lúc cam kết). Giới hạn (Formulas): quãng lướt mỗi node và tổng mỗi kỹ năng (theo tỉ lệ quãng né), tốc độ lướt cộng
   điều khiển không vượt tốc né (F3), tổng quãng lướt mỗi lần lên không (`air_lunge_budget`, F3), độ cao bật ≤ độ cao
   nhảy đơn (F5).
8. **Không có bất tử từ kỹ năng.** Bất tử (i-frame) chỉ đến từ né của Controller; đòn khi lướt vẫn tắt bất tử như
   Controller Rule 12. Kỹ năng `special` được khai báo **giáp** (`armor`): trong cửa sổ đó nhân vật vẫn mất máu nhưng
   không bị ngắt đòn. Combat phân giải giáp và chốt trần (số đòn chịu được, thời lượng tối đa), nên Controller không
   thấy hitstun trong lúc có giáp và không phải đổi.
9. **Cửa sổ hủy.** Mặc định theo Controller Rule 14: **nhảy được ở mọi pha và đòn vẫn diễn tiếp** — cửa sổ hit vẫn
   chạy, lực của kỹ năng bị xóa, các node chuyển động còn lại bị bỏ qua, hệ số di chuyển chuyển ×1,0, và từ lúc đó kỹ
   năng chịu `land_policy`. Né, đòn kế và cắm xuống mở từ node **CancelWindow**; kỹ năng không có node này thì mở ở
   `recovery_start + default_cancel_lockout` (knob chung, mặc định 0,03 s). CancelWindow **không được mở trước khi cửa
   sổ hit đầu tiên đóng** (mỗi đòn cam kết ít nhất một nhát) và, với đòn thường, **không được mở muộn hơn**
   `recovery_start + dodge_cancel_latest` (F6). Ngoại lệ đã định: (a) pha `charging` bị **hủy hẳn** bằng
   nhảy hoặc né bất kỳ lúc nào; (b) **Kỹ năng đặc biệt** hủy được `recovery.open` của mọi đòn thường và pha `charging`;
   (c) nối đòn theo bảng tổ hợp (Rule 11) dùng cửa sổ `open`; (d) kỹ năng có node `dive` hủy được bằng né trên không ở
   mọi pha (như Plunge của Controller Rule 14).
   Kỹ năng `normal` phải hủy được bằng né không muộn hơn `recovery_start + dodge_cancel_latest` (Formulas).
10. **Kỹ năng không được:** đổi tốc chạy, độ cao nhảy, trọng lực hay hệ số `attack_move_mult` của Controller; thêm
    khung khóa chân; bỏ hủy-bằng-nhảy; đặt thẳng vận tốc ghi đè điều khiển của người chơi; đổi hướng mặt giữa đòn
    (Controller Rule 16; ngoại lệ: node LookAt, cho cả người chơi và quái — quyết định người dùng 2026-11-07); chứa biểu thức, script hay điều kiện tùy ý (XML thuần dữ liệu); ghi cờ, dữ liệu lưu hay
    trạng thái thế giới; gọi kỹ năng khác ngoài các liên kết đã khai báo trong bảng tổ hợp. Validator từ chối file
    vi phạm.

**Bảng tổ hợp (Đánh thường)**

> **Thay thế 2026-11-13 (người dùng):** Rule 11–19 (bảng tổ hợp cố định theo họ vũ khí, một kỹ năng đặc biệt từ Skill
> Stone) **bị thay bằng Rule 35–44 "Moveset và đá khảm"** ở cuối Core Rules. Giữ lại bên dưới để tra cứu; code bảng tổ hợp
> (`CommandTable`, `SkillLibrary.Select`) được giữ trong giai đoạn chuyển, sẽ gỡ khi Controller dùng Moveset.

11. **Mỗi họ vũ khí có một bảng tổ hợp** do người làm game soạn; người chơi không sửa. Mỗi dòng gồm:
    - `skill` — id kỹ năng.
    - `context` — `ground` | `air` | `dash` (Attack được tiêu thụ trong lúc né) | `any`. Controller cấp `context` lúc
      tiêu thụ lần bấm; `LandRecovery` tính là `ground`.
    - `moving` — `still` | `moving` | `any`: nhân vật đang đứng hay đang đi/chạy, lấy từ hướng ngang của **bản ghi
      lần bấm** (chỉ có nghĩa ở `ground`).
    - `dir` — `neutral` | `up` | `down` | `forward` | `back` | `any`, lấy từ hướng của **bản ghi lần bấm** (không lấy
      lúc xử lý, đúng Controller Rule 15/17); `forward`/`back` so với hướng mặt lúc tiêu thụ.
    - `after` — danh sách id kỹ năng mà đòn vừa chạy phải thuộc về (liên kết theo tag đã bỏ 2026-11-08); rỗng = đòn mở đầu. Đây là **liên kết
      nối đòn**: chuỗi 1 → 2 → 3 là `g1` (không `after`), `g2` (`after: g1`), `g3` (`after: g2`), và có thể rẽ nhánh
      (sau `g1`, Tiến + Đánh ra một đòn khác). Độ dài chuỗi suy ra từ bảng.
    - `link_window` — thời gian còn nối được tính từ lúc đòn trước kết thúc hoặc bị hủy; mặc định `combo_reset`
      của Controller.
    - `priority` — số nguyên, mặc định 0.

    Bảng **tự do hoàn toàn**: không có ý nghĩa chung bắt buộc cho từng loại tổ hợp giữa các họ vũ khí (quyết định
    người dùng 2026-11-05).
5a. **Nối từ lúc bắt đầu hay lúc kết thúc của node trước** (quyết định người dùng 2026-11-07). Node có cửa sổ thời
    gian (Hit) có hai cổng ra: **On Start** và **On End**. Nối từ On Start thì độ trễ của node kế tính
    từ lúc node trước **bắt đầu** (như Rule 5); nối từ On End thì tính từ lúc cửa sổ của node trước **đóng**
    (`mốc = mốc_trước + duration_trước + độ_trễ`). Chỉ nối một trong hai cổng. Khi xuất XML, mốc đã cộng sẵn; node ghi thêm
    `after="end"` để Skill Tool mở lại đúng cách nối, và node có `after="end"` không được bắt đầu trước khi cửa sổ của node
    trước đóng (`AfterEndTooEarly`). Điểm rẽ (HoldGate, Search) có hai cổng nhánh thay cho On Start / On End.
12. **Giữ để tụ lực không nằm trong bảng.** Một kỹ năng đặt node `HoldGate(hold_max)` trên luồng, **trước** cửa sổ hit
    đầu tiên, ở độ trễ ≥ `hold_threshold` (knob chung, F8). Tới node đó mà nút vẫn được giữ liên tục từ lần bấm đã mở
    kỹ năng, kỹ năng chờ ở đó (`charging`), thoát khi nhả nút hoặc chạm `hold_max`, rồi đi nhánh `charged`; nếu nút đã
    nhả, đi nhánh `tap` ngay. Hai nhánh nằm trong cùng file, mỗi nhánh có End riêng. Nhờ vậy **bấm nhanh luôn
    ra đòn ngay**, không chờ biết là chạm hay giữ (lời hứa của Input).
13. **Chọn kỹ năng cho một lần bấm Đánh** (hàm thuần, một lượt):
    1. Giữ các dòng khớp **mọi** điều kiện: `context` (giao với `contexts` của kỹ năng), `moving`, `dir` (`dir: any`
       khớp mọi hướng — Xuống + Đánh trên đất không có dòng riêng thì rơi vào dòng `any`, đúng Controller Rule 17), và
       `after` (dòng có `after` chỉ khớp khi `chain_ref` thuộc danh sách và lần tiêu thụ ≤ `link_window` của **chính
       dòng đó** tính từ lúc đòn trước kết thúc hoặc bị hủy, so ≤ theo tick). Dòng trỏ tới kỹ năng đang hồi chiêu hoặc
       bị tắt lúc nạp (Rule 22) bị loại.
    2. Chọn theo khóa so sánh lần lượt: `priority` cao hơn → dòng **có `after` khớp** thắng dòng không có (đòn nối
       luôn thắng đòn mở đầu, kể cả khi người chơi giữ hướng lúc bấm liên tục) → dòng **cụ thể hơn** (số trường bị
       ràng buộc; `after` theo id cụ thể hơn theo tag) → thứ tự khai báo.
    3. Không dòng nào khớp → kỹ năng dự phòng theo `context` (Rule 22). **Lần bấm Đánh luôn ra một đòn**, không bao
       giờ bị nuốt.

    Bản ghi của hành động **Plunge** (Input Rule 4; mặc định Xuống + Đánh) tra cùng bảng với `dir` ép = `down`,
    `context` = `air` (hoặc `ground` khi đã chạm đất, Controller Rule 17) và giữ cửa sổ đệm 0,15 s của Controller.
    Lùi + Đánh (`dir: back`) **giữ nguyên hướng mặt cũ**; mọi dòng khác đổi hướng mặt theo bản ghi (Controller Rule 16).

    Việc chọn xảy ra **lúc Controller tiêu thụ lần bấm**, theo trạng thái lúc đó: lần bấm nằm trong bộ đệm khi đang
    thu đòn sẽ tra bảng theo trạng thái ở frame đầu tiên được phép (Controller Rule 14).
14. **Trạng thái chuỗi.** Controller giữ `chain_ref` = id đòn vừa kết thúc và **tick kết thúc** (mỗi dòng tự so với
    `link_window` của nó). `chain_ref` về rỗng khi: quá `link_window` lớn nhất trong các dòng trỏ tới nó; Hitstun,
    Teleport, Respawn, Defeated; đòn kết thúc mà không dòng nào có `after` trỏ tới nó. Hủy bằng né, và đòn trên không bị
    hủy vì chạm đất, thì **giữ** `chain_ref` (đếm từ lúc hủy; lần bấm kế mang ngữ cảnh mới). **Kỹ năng đặc biệt không
    ghi `chain_ref`**: hủy một đòn thường bằng kỹ năng đặc biệt thì `link_window` của đòn đó tiếp tục đếm.
15. **Dòng bắt buộc.** Mỗi họ vũ khí phải có ít nhất một dòng `dir: any` không `after` cho mỗi `context` `ground`,
    `air` và `dash`, và một dòng `air` + `down` có kỹ năng chứa `dive` cho cắm xuống. Validator từ chối bảng thiếu, và
    từ chối dòng `air`/`dash` có `moving` khác `any` (`moving` chỉ có nghĩa ở `ground`).

**Kỹ năng đặc biệt**

16. **Một nút, kỹ năng từ Skill Stone.** Mỗi Skill Stone mang `special_skill_id` và tùy chọn
    `special_skill_air_id` (id do người làm game định; người chơi chọn stone nào gắn — Loadout sở hữu socket). Kỹ năng
    của stone phải có `kind: special` và `weapon_families` chứa họ vũ khí đang cầm (hoặc `any`); không khớp thì stone
    không kích hoạt được và UI báo (điều kiện của Loadout).
17. **Chỉ có hồi chiêu, không có tài nguyên** ở MVP. **Người chơi chỉ mang một vũ khí khi ra ngoài; đổi vũ khí và
    đổi stone chỉ làm được ở nhà** (quyết định người dùng 2026-11-05; Loadout sở hữu luật này). Vì vậy tại một thời
    điểm chỉ có **một** kỹ năng đặc biệt và **một** hồi chiêu; không có cách đổi đồ giữa trận để làm mới hồi chiêu hay
    chuỗi đòn. Hồi chiêu bắt đầu khi cửa sổ hit đầu tiên mở (đầu `active`; kỹ năng
    `utility`: ở Start), nên bị hủy trong `windup` thì không tốn hồi chiêu; đã qua mốc đó thì hồi chiêu vẫn tính dù kỹ
    năng bị hủy sau đó.
18. **Khi không dùng được:** chưa gắn stone → nút không làm gì, HUD hiện ô trống; đang hồi chiêu → lần bấm bị từ chối
    kèm tín hiệu "chưa sẵn sàng", và nằm lại bộ đệm nếu thời gian hồi còn lại ≤ cửa sổ đệm; bấm trên không mà stone
    không có `special_skill_air_id` → lần bấm **nằm lại bộ đệm**, chạm đất trong cửa sổ đệm thì ra biến thể trên đất,
    hết hạn mới báo "không có biến thể" (không tốn hồi chiêu); kỹ năng đặc biệt thiếu hoặc hỏng (Rule 22) → HUD hiện
    trạng thái "lỗi" riêng, khác ô trống và khác "chưa sẵn sàng". Không bao giờ thay bằng Đánh thường.
19. **Kỹ năng đặc biệt không hủy được kỹ năng đặc biệt** và không ra trong lúc né (nằm lại bộ đệm, ra khi hết né).

**Tiêu thụ input**

20. Hệ này **không đọc input**. Controller xin Input bản ghi lần bấm (Attack, Plunge hoặc Special) theo luật "mỗi bước
    chỉ bắt đầu một hành động, lần bấm trước ra trước" (Controller Rule 15), rồi hỏi bảng tổ hợp / stone "lần bấm này
    ra kỹ năng nào". Kỹ năng đặc biệt dùng nút riêng nhưng đi chung hàng đợi và cùng luật thứ tự; cửa sổ đệm của
    Special là **0,20 s theo đồng hồ Game** (thêm vào Controller Rule 28; validator 8: 0,40 + 0,1995 ≤ 1,0 vẫn đạt), và
    LandRecovery cho phép Special sau `land_cancel_lockout` như Attack.

**Kiểm tra hợp lệ và dữ liệu hỏng**

21. **Kiểm tra lúc soạn và lúc build là chặn cứng:** đồ thị không hợp lệ thì không xuất được XML; XML lệch mã băm
    đồ thị, kỹ năng hay bảng sai luật thì **build lỗi**. Danh sách kiểm tra: các ràng buộc ở Rule 3–15 và giới hạn ở
    Formulas (đúng một Start, mỗi nhánh tới đúng một End, không có node treo không nối, node có thời lượng kết thúc trước End,
    CancelWindow trong biên Rule 9, ít nhất một PlayAnimation, node chuyển động trong giới hạn,
    không trùng dòng trong bảng, mọi `id` tham chiếu tồn tại và đúng `context` / họ vũ khí, đủ dòng bắt buộc, vòng
    nối đòn có thời gian cam kết ≥ `min_loop_commit`).
22. **Lúc chạy là lưới an toàn:** lỗi **số có biên** (CancelWindow ngoài biên Rule 9, quãng lướt hoặc độ cao bật
    vượt trần, node có thời lượng vượt quá End) → **kẹp** về biên (như Controller validator 13) và ghi một lỗi; lỗi **cấu trúc**
    (id thiếu, `schema_version` mới hơn, `hit_profile_id` Combat không biết, file không đọc được) → dùng **kỹ năng dự
    phòng** tích hợp sẵn theo `context` (giữ nhịp prototype của Controller Rule 12, viết thành mốc sự kiện tính từ Start:
    trên đất — hit 0,04 → 0,10, CancelWindow 0,13, End 0,20; trên không — hit 0,03 → 0,10, CancelWindow 0,13, End 0,18;
    khi lướt — hit 0,02 → 0,12, CancelWindow 0,18, End 0,26), ghi **một** lỗi cho mỗi (id, lý do) trong
    phiên, không dừng game, không chặn di chuyển. Kỹ năng đặc biệt thiếu hoặc hỏng → nút không làm gì (không dùng dự
    phòng, để không đánh lừa người chơi về viên stone của họ). Kỹ năng đã bắt đầu thì chạy hết bằng bộ số đã chụp
    lúc bắt đầu.

**Quy ước chi tiết** (làm rõ theo rà soát của qa-lead, 2026-11-05)

23. **Thời gian:** mọi mốc và `duration` (kể cả của node chuyển động) quy về bước 1/60 s theo cùng luật Rule 5 (bước đầu
    tiên ≥ mốc); node có `duration` > 0 kéo dài ít nhất 1 bước. Các biên (Rule 9, F1, F3, F6) được kiểm **sau khi quy về
    bước**.
24. **Kẹp lúc chạy (Rule 22):** quãng lướt vượt trần → kẹp quãng; tốc độ lướt vượt trần F3 → giữ quãng, kéo dài
    `duration` tới bước nhỏ nhất thỏa trần; tổng lướt của kỹ năng vượt trần → co đều các node theo tỉ lệ; CancelWindow
    ngoài biên → kẹp về biên gần nhất; `cooldown` của kỹ năng đặc biệt ngoài khoảng → kẹp về khoảng.
25. **Ngân sách lướt trên không (F3):** chỉ tính node **bắt đầu** khi nhân vật đang trên không; node vượt phần còn lại thì
    bị cắt quãng theo phần còn lại (giữ `duration`). Node bắt đầu trên đất rồi lướt khỏi mép không tính.
26. **Kỹ năng nhiều ngữ cảnh:** trần tốc độ F3 dùng hệ số `attack_move_mult` **lớn nhất** trong `contexts` (chặt nhất).
27. **Phân loại hướng của bản ghi:** theo vùng chết và trục trội của Input (|y| > |x| → `up`/`down`, ngược lại
    `forward`/`back`, cả hai trong vùng chết → `neutral`). Validator cảnh báo dòng `moving: still` kèm `dir`
    `forward`/`back` (không bao giờ khớp, vì hướng ngang đã kéo theo `moving`).
28. **So độ cụ thể (Rule 13):** trường đặt `any` không tính là ràng buộc; "khớp `after` theo id" thắng "khớp `after` theo
    tag" ở một bước so riêng, trước khi đếm số trường.
29. **Vòng nối đòn (F6):** chỉ xét vòng đi qua liên kết `after`. Đòn mở đầu hủy `recovery.open` của chính nó (bấm liên
    tục) không phải vòng; nhịp tối thiểu của nó chính là `commit_time` của đòn đó.
30. **Hủy `charging` bằng nhảy** giữ `chain_ref` như hủy bằng né.
31. **Mã băm và lưu XML:** Skill Tool là bộ ghi duy nhất; mỗi file mang `payload_hash` (SHA-256 của chính file với trường
    này để trống). Skill Tool **không lưu** file mà game sẽ từ chối. Bước build nạp lại cả thư mục như game: mọi lỗi nạp,
    kể cả `payload_hash` lệch (file bị sửa ngoài Skill Tool), → build lỗi. Mở file đó bằng Skill Tool rồi lưu lại là sửa được.
32. **Dữ liệu lạ:** phần tử hoặc thuộc tính XML không biết → build từ chối (XML thuần dữ liệu, Rule 10); ở dev build thì
    bỏ qua kèm một cảnh báo (tương thích xuôi khi đang phát triển). Chuỗi đổi tên dài hơn 8 bước → coi là thiếu.
33. **Validator cảnh báo knob mâu thuẫn:** `r_node > r_total`, `r_air < r_node`, `hold_threshold` lớn hơn độ trễ của một
    HoldGate hiện có.

34. **Hệ số tốc độ theo nhánh (`timeline_scale`)** — trục sidegrade "tốc đánh ↔ sát thương" của Pillar 2 (quyết định
    người dùng 2026-11-05). Mỗi nhánh vũ khí (Weapon Tree) mang một `timeline_scale` nằm trong **dải tốc độ của họ vũ khí**
    (Weapon Tree sở hữu dải và giá trị). Khi kỹ năng chạy, **mọi mốc và `duration`** của dòng thời gian nhân với
    `timeline_scale` (< 1 = nhanh hơn), và Presentation phát clip với tốc độ × 1 / `timeline_scale`, nên nhánh nhanh và
    nhánh mạnh của cùng họ **dùng chung XML**. Không co giãn: `hold_max` và `hold_threshold` (thời gian người chơi giữ
    nút), `link_window`, `cooldown` của kỹ năng đặc biệt (thuộc stone, không thuộc nhánh), và **quãng** của node chuyển động
    (chỉ `duration` co giãn — tốc độ lướt tăng tương ứng). Validator kiểm các biên (Rule 9, F1, F3, F6) với **cả hai đầu
    của dải** của họ; `commit_time_max(họ)` (F2) tính ở `timeline_scale` **chậm nhất** của dải. Sát thương theo nhánh là
    việc của Combat / Weapon Tree.

**Moveset và đá khảm** *(thêm 2026-11-13 — quyết định người dùng; thay Rule 11–19)*

35. **Moveset của nhân vật đến từ đá khảm.** Mỗi vũ khí có **2–4 ô khảm**. Mỗi đá khảm trỏ tới **một moveset id**; khảm
    đá vào vũ khí = thêm các đòn của moveset đó. **Mọi đòn đều đến từ đá**: vũ khí chưa khảm đá đánh thường thì chuột
    trái không làm gì (không có moveset gốc, không có đòn dự phòng; dự phòng của Rule 22 chỉ còn cho kỹ năng hỏng khi chạy).
36. **Moveset = một file XML, nhiều dòng.** `Assets/StreamingAssets/Movesets/<id>.xml`, id = tên file, 6 chữ số từ
    100000 (vùng id riêng, không trùng nghĩa với id kỹ năng). Phần đầu: `name` (nhãn cho người làm game), `weapons`
    (`any` hoặc danh sách họ vũ khí dùng được — **chọn từ danh sách** `Assets/StreamingAssets/Weapons/weapon_families.txt`, mỗi dòng một họ; mặc định 5 họ animation của art bible `crescent`, `slab`, `needle`, `ring`, `chain` cho tới khi GDD Weapon Tree chốt danh sách thật; họ không có trong danh sách thì tool không cho lưu — `UnknownWeapon`), `note`. Mỗi dòng:
    - `input` — **hành động logic + cử chỉ**, không phải phím vật lý (đổi phím / tay cầm vẫn chạy): nút `Button1`–`Button5`
      (mặc định chuột trái, phải, giữa, hai nút bên) và `Key1`–`Key4` (mặc định Q E R F); cử chỉ **nhấn** (`Button1`),
      **nhấn đúp** (`Button1:double`), **giữ** (`Key1:hold`), **nhấn cùng lúc** (`Button1+Button2`).
    - `context` — một hoặc nhiều trong `ground` / `air` / `dash` (hoặc `any`); `moving` (`still` / `moving`, chỉ dòng
      chỉ-ground); `dir` (`neutral` / `up` / `down` / `forward` / `back` / `any`, so với hướng mặt, lấy từ bản ghi lần bấm).
    - `skill` — id kỹ năng (file Skill Tool) thể hiện đòn.
    - `after` + `link_window` — đòn nối: đòn ngay trước phải là một trong các skill id này và lần bấm còn trong cửa sổ;
      trống = đòn mở đầu. Chuỗi 1 → 2 → 3 là ba dòng cùng input. Đòn nối có thể trỏ tới skill của **đá khác** (nối đá).
    - `priority`, `cooldown` (giây; hồi chiêu theo dòng, bắt đầu khi đòn bắt đầu — chi tiết chờ OQ-S20).
37. **Chọn đòn cho một input** (hàm thuần `MoveSelector`): lấy mọi dòng của mọi ô đã khảm mà moveset cho phép họ vũ khí
    đang cầm, khớp input, `context`, `moving`, `dir`, `after` + cửa sổ; bỏ dòng đang hồi chiêu và dòng trỏ tới kỹ năng
    không nạp được. Thắng theo: `priority` → đòn nối đã khớp thắng đòn mở đầu → cụ thể hơn → ô khảm đứng trước → dòng
    đứng trước. Không dòng nào khớp → **không có đòn** (lần bấm không làm gì).
38. **Khảm bị chặn** (Loadout, lúc khảm ở nhà): moveset không cho phép họ vũ khí đang cầm; cùng moveset đã ở ô khác;
    hoặc **trùng**: một dòng của đá mới và một dòng của đá đã khảm có cùng input, chung ít nhất một `context`, `moving`
    và `dir` không loại trừ nhau, và cả hai là đòn mở đầu (hoặc cả hai nối từ một skill chung). `priority` **không** gỡ
    trùng giữa hai đá (người chơi không biết đòn nào thắng). Đòn nối không bao giờ trùng với đòn mở đầu (đòn nối luôn
    thắng trong cửa sổ). UI chỉ rõ dòng nào trùng với ô nào.
39. **Trong một moveset**, hai dòng giống hệt về input + điều kiện + đòn nối + priority là lỗi (`DuplicateRow`, dòng sau
    không bao giờ được chọn). Dòng khác điều kiện thì được, `priority` / thứ tự phân xử.
40. **Nhận diện cử chỉ** (`MoveGestureRecognizer`, biết cử chỉ nào đang được các đá dùng): nút chỉ có dòng nhấn → ra
    **ngay lúc nhấn** (giữ lời hứa "bấm nhanh luôn ra đòn ngay"); nút có thêm dòng giữ → nhả trước `hold_time` ra nhấn,
    tới `hold_time` ra giữ; nút thuộc một tổ hợp nhấn cùng lúc → chờ tối đa `chord_window` cho nút kia (nhả sớm hoặc hết
    cửa sổ thì ra nhấn); nhấn đúp → lần nhấn thứ hai trong `double_tap_window` ra nhấn đúp (lần đầu **vẫn ra nhấn**), lần
    thứ ba bắt đầu lại. Knob: `double_tap_window` 0,25 s, `hold_time` 0,30 s, `chord_window` 0,05 s (3 frame); tất cả quy
    về bước 1/60 s. Người làm moveset cần biết: thêm dòng giữ / tổ hợp cho một nút làm lần nhấn của nút đó trễ tới lúc
    nhả / hết cửa sổ.
41. **Giữ để tụ lực** vẫn nằm trong kỹ năng (HoldGate, Rule 12) và khác cử chỉ giữ: cử chỉ giữ **chọn** đòn, HoldGate
    **kéo dài** đòn đã chọn khi nút còn được giữ.
42. **Soạn:** Moveset Tool (`Evermeadow ▸ Skills ▸ Moveset Tool`) đọc / ghi XML, chọn skill từ thư mục Skill Tool (mở thẳng
    sang Skill Tool), chỉ lưu file hợp lệ với mọi skill id tồn tại, có khung "Socket test" để thử khảm vào một vũ khí và xem
    bảng input → đòn. Sửa và Save khi đang Play thì lần bấm kế dùng bản mới (như kỹ năng). **Thử trong Play Mode (tạm,
    2026-11-14):** bảng dev *Moveset Tester* (F9, chỉ Editor / bản dev) chọn vũ khí, số ô, moveset từng ô, đọc trạng thái
    thật của nhân vật và in "cử chỉ → skill" khi bấm chuột trái / phải / giữa / bên, Q E R F; nút dev dựng bằng code
    (`DevButtonPoller`), không nằm trong asset Input, nên tạm trùng Attack / Heal / Interact / EatBuff cho tới OQ-S18.
43. **Hệ thống khác sở hữu:** đá khảm là vật phẩm của Skill Stone / Item (đá → moveset id, tên, độ hiếm); số ô và luật
    "đổi đá ở nhà" (Rule 17) thuộc Loadout; Input sở hữu các hành động `Button1`–`Button5`, `Key1`–`Key4` và phím mặc định;
    Controller tiêu thụ input đã nhận diện, cấp `context` / `moving` / `dir` / `chain_ref` rồi hỏi `MoveSelector`
    (OQ-S18…S20).
44. **Kỹ năng đặc biệt** không còn là một nút riêng: một dòng có `cooldown` của một đá bất kỳ đóng vai đó. Luật Rule 18
    (từ chối khi hồi chiêu, tín hiệu "chưa sẵn sàng") áp cho mọi dòng có hồi chiêu; HUD hiện hồi chiêu theo dòng (OQ-S20).

**Animation slot chung, quái đánh bằng skill** *(thêm 2026-11-14 — quyết định người dùng)*

45. **Một bộ tên animation cho mọi rig** (player, mọi creature): danh sách slot là dữ liệu
    (`Assets/StreamingAssets/Animation/animation_slots.txt`, mỗi dòng một slot, `#` ghi chú; mặc định: idle1–idle3, walk,
    run, jump_rise, jump_apex, jump_fall, land, bonk, dodge, hurt, die, attack1–attack12, skill1–skill8). Mỗi rig vẽ clip
    **đúng tên slot** (Base và mỗi look vũ khí); nhờ vậy skill gán cho ai cũng chạy được.
    - **Tự phát** (state machine của Animator, theo trạng thái): idle1, walk, run, jump_rise / jump_apex / jump_fall, land,
      bonk (di chuyển) và dodge, hurt, die (phản ứng). Creature tự phát idle1 ↔ walk (+ hurt, die khi có Combat).
    - **Chỉ qua skill**: mọi slot còn lại (idle2, attack1…, skill1…). Không transition nào vào chúng; node Animation của skill
      phát clip lên trên Animator, tới End thì về idle. Player: moveset (đá khảm) chọn skill. **Creature: không dùng moveset**
      — Behaviour Tree của nó (`StreamingAssets/AI/<tên>.xml`) chọn và tung skill. Không có skill thì quái chỉ đi qua lại.
    - Node Animation trong Skill Tool **chọn slot từ danh sách** (không gõ tay); slot không có trong danh sách hiện màu cam;
      rig thiếu clip cho slot → bỏ qua animation, báo ở Console và log của Moveset Tester.
    - Đổi rig cũ: menu `Evermeadow ▸ Animation ▸ Migrate Rigs To Animation Slots` (idle → idle1, rise/apex/fall → jump_*,
      hitstun → hurt, defeated → die, attack_ground/air/dash → attack1/2/3, plunge/plunge_land → attack4/5; creature
      move → walk, attack → attack1) rồi dựng lại controller; `Rebuild Animator From Clips` dựng lại một controller từ các clip
      trong `Base/` (giữ GUID, look vẫn chạy).
46. **Behaviour Tree tối thiểu cho creature** (trước GDD Enemy AI): node `selector`, `sequence`, `in_range min max`,
    `cast skill cooldown` (Running tới End của skill; hồi chiêu tính từ lúc tung), `chase stop speed`, `patrol`, `wait seconds`,
    `face`. Creature đứng yên khi đang tung skill; ghi Console mỗi lần đổi nhánh; gizmo vẽ vùng tuần tra và dây tới mục tiêu.
    Thử nhanh: `Evermeadow ▸ Dev ▸ Add Test Goblin`. Thiết kế đầy đủ (cảm nhận, telegraph, nhóm, ngân sách 2 ms) → OQ-S21.

### States and Transitions

Mỗi bước chỉ có một chuyển (Controller Rule 15). "Hủy" luôn xóa lực của kỹ năng và đóng các cửa sổ hit còn mở.

| Từ | Sự kiện | Đến | Ghi chú |
|---|---|---|---|
| Rảnh (Idle) | Đánh khớp một dòng bảng / Đặc biệt dùng được | `windup` | chụp bộ số kỹ năng lúc bắt đầu |
| `windup` | luồng tới HoldGate, nút đang giữ liên tục | `charging` | |
| `windup` | cửa sổ hit đầu tiên mở | `active` | hồi chiêu của kỹ năng đặc biệt bắt đầu ở đây |
| `charging` | nhả nút hoặc chạm `hold_max` | tiếp luồng nhánh `charged` | |
| `active` | cửa sổ hit cuối cùng đóng | `recovery.locked` | |
| `recovery.locked` | CancelWindow mở (hoặc hết `default_cancel_lockout`) | `recovery.open` | |
| `recovery.open` | Đánh khớp dòng có `after` trỏ tới đòn này | `windup` của đòn kế | `chain_ref` = đòn này |
| `recovery.open` | Đánh chỉ khớp dòng mở đầu (cuối chuỗi, hoặc `chain_ref` rỗng) | `windup` của đòn mở đầu | đòn hiện tại bị hủy |
| `recovery.open` | né / cắm xuống / kỹ năng đặc biệt | Hủy | giữ `chain_ref` |
| `recovery.open` | luồng tới End | Kết thúc → Rảnh | `chain_ref` bắt đầu đếm `link_window` |
| mọi pha trừ `charging` | nhảy | **không đổi pha** — đòn diễn tiếp | lực kỹ năng xóa, node chuyển động còn lại bỏ qua, ×1,0, từ đó chịu `land_policy` (Controller Rule 14) |
| `charging` | nhảy / né / kỹ năng đặc biệt | Hủy | ngoại lệ Rule 9 (a), (b) |
| mọi pha (kỹ năng có `dive`) | né trên không | Hủy | ngoại lệ Rule 9 (d) |
| mọi pha | Hitstun (không có giáp), Teleport, Respawn, Defeated | Hủy (ngắt) | xóa `chain_ref` |
| mọi pha (kỹ năng trên không) | chạm đất | Hủy (`cancel_on_land`), hoặc tiếp tục (`continue`) | cắm xuống: Controller phát `PlungeLanded` rồi vào `LandRecovery` |
| mọi pha | hit-stop của Combat | **chậm lại** | toàn bộ dòng thời gian (kể cả node chuyển động) chạy theo đồng hồ Game (time scale `s_min` = 0,05 của Input/Controller); tổng quãng đường không đổi |

Projectile đã tạo ra tồn tại độc lập khi kỹ năng bị hủy (Combat sở hữu vòng đời projectile). Không có chuyển "đổi vũ
khí giữa đòn": người chơi chỉ đổi vũ khí ở nhà (Rule 17), và đổi vũ khí xóa `chain_ref`.

### Interactions with Other Systems

| Hệ | Hướng | Dữ liệu / giao diện | Chủ giao diện |
|---|---|---|---|
| **Character Controller** (#6) | Controller → hệ này: hỏi "lần bấm này ra kỹ năng nào" với `context`, `moving`, `dir`, `chain_ref`; hệ này → Controller: kỹ năng (dòng thời gian, cửa sổ hủy, node chuyển động, `land_policy`) | Controller chạy máy trạng thái đòn và thực thi chuyển động; hệ này chỉ cung cấp dữ liệu và hàm chọn | Controller sở hữu máy trạng thái, API chuyển động và luật hủy; hệ này sở hữu schema kỹ năng và bảng tổ hợp |
| **Combat** (#9, chưa có GDD) | hệ này → Combat: cửa sổ hit kèm `hit_profile_id`, spawn, cửa sổ giáp; Combat → hệ này: hit-stop (qua đồng hồ Game) | Combat định nghĩa vùng chạm, sát thương, knockback, giáp, projectile | Combat |
| **Input** (#1) | gián tiếp qua Controller | thêm hành động **Special** (nút Kỹ năng đặc biệt); Attack giữ nguyên; `IsHeld(Attack)` cho HoldGate | Input |
| **Loadout & Player Stats** (#16) | Loadout → hệ này: họ vũ khí đang cầm, stone đang gắn | chọn bảng tổ hợp; lấy `special_skill_id`. **Một vũ khí khi ra ngoài; đổi vũ khí / stone chỉ ở nhà** (quyết định người dùng 2026-11-05 — chuyển vào GDD Loadout khi viết) | Loadout |
| **Skill Stone** (#17) / **Item & Inventory** (#11) | stone mang `special_skill_id`, `special_skill_air_id`; hồi chiêu theo viên | | Skill Stone định nghĩa stone; hệ này định nghĩa kỹ năng |
| **Weapon Tree** (#15) | họ vũ khí và nhánh trỏ tới bảng tổ hợp; nhánh mang `timeline_scale` trong `scale_band` của họ (Rule 34) — trục tốc đánh của sidegrade; nhánh có thể đổi look (palette swap) | | Weapon Tree |
| **Animation (Presentation, đã có code)** | hệ này → Presentation: khóa `anim` và mốc của từng node PlayAnimation, `timeline_scale` | Presentation ánh xạ khóa sang clip theo look vũ khí và phát clip **theo thời gian** từ mốc của node (tốc độ phát × 1 / `timeline_scale`); clip không bao giờ điều khiển dòng thời gian | Presentation |
| **Game Data & Tuning Config** (#4) | định dạng file, nơi nạp XML, schema version | → ADR-0002 (định dạng, nạp qua `ISkillTextSource` / `FolderSkillTextSource`), ADR-0003 (xuất) | Game Data |
| **Enemy AI, Spawning & Drops** (#18, chưa có GDD) | Enemy AI → hệ này: chọn và chạy kỹ năng `owner: creature` theo id; cấp `SkillStartValues` (vị trí / id của quái và của người chơi) lúc bắt đầu | hệ này cung cấp dữ liệu kỹ năng quái và giá trị Start (Rule 4a/4b); Enemy AI quyết định lúc nào dùng kỹ năng nào và thực thi dòng thời gian cho quái | Enemy AI sở hữu luật chọn kỹ năng của quái; hệ này sở hữu schema |
| **Boss Encounter** (#19) | hệ này xuất `commit_time` của từng kỹ năng (từ lúc bắt đầu tới cửa sổ né sớm nhất) và tối đa theo họ vũ khí | dùng để chỉnh cửa sổ dễ tổn thương của boss ("mọi vũ khí hạ được mọi boss") | Boss Encounter |
| **HUD** (#30) | hồi chiêu của kỹ năng đặc biệt, ô trống, "chưa sẵn sàng" | | HUD |
| **Audio** (#33) / VFX | cue của kỹ năng | | Audio / Presentation |
| **Telemetry** (#12) | kỹ năng đã dùng, lỗi dữ liệu (một lần mỗi id, lý do) | | Telemetry |

**Thay đổi cần lan sang GDD Character Controller** (sau khi GDD này được duyệt, chạy `/propagate-design-change`):
- **Rule 12 và validator 11:** dữ liệu vũ khí vẫn **cấm hệ số** (tốc chạy, nhảy, khóa chân), nhưng **cho phép node
  chuyển động có giới hạn** của Rule 7 (cộng vào điều khiển, không thay thế). Các con số nhịp đòn chuyển quyền sở hữu
  sang GDD này (Controller đã ghi "GDD Weapon Moveset sở hữu các con số này"). Ngữ cảnh đòn cứng (Ground/Air/Dash +
  chỉ số chuỗi) thay bằng hàm chọn kỹ năng của Rule 13.
- **Rule 14:** thêm ngoại lệ hủy `charging` bằng nhảy / né và hủy `recovery.open` bằng kỹ năng đặc biệt.
- **Binding test 1:** đổi câu chữ thành "một họ vũ khí làm giảm tốc chạy, độ cao nhảy, hoặc **thay thế** điều khiển
  của người chơi bằng chuyển động không do người chơi bấm ra" — lực cộng thêm trong lúc đòn chạy được phép.
- Đòn khi lướt giữ quán tính F8 của Controller; kỹ năng có `dash` trong `contexts` không được có node `lunge`.
- **Plunge:** Controller giữ hành động Plunge (cửa sổ 0,15 s, Rule 17); kỹ năng `dive` đặt vận tốc ngang = 0, hủy
  được bằng né trên không ở mọi pha, và khóa điều khiển chờ nó chạm đất như C-11.
- **Rule 28:** thêm cửa sổ đệm Special 0,20 s; bảng hủy LandRecovery thêm Special. **Sự kiện** `AttackStarted(context,
  index)` đổi thành mang `skill_id`.
- **Launch:** Controller bật cờ `jump_rising` (tắt snap), **không** mở coyote, không treo ở đỉnh; nhả Jump không cắt
  vận tốc của launch.

## Formulas

*Đề xuất số từ systems-designer (2026-11-05); quy đổi sang đơn vị game theo Character Controller C-12 (× k = 30/13).
Các số chưa qua playtest, đánh dấu **(đề xuất)**. Thời lượng không còn là trường khai báo: mọi giai đoạn suy ra
từ dòng thời gian Start → End (Rule 5). Mốc của ba kỹ năng dự phòng giữ nhịp prototype của Controller Rule 12 **(P)**.
Mọi giới hạn dưới đây là tỉ lệ so với số của Controller, nên chỉnh Controller thì giới hạn đi theo.*

### F1. Tổng thời lượng kỹ năng

The skill_total formula is defined as:

`skill_total = t_End + charge`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `t_End` | float (s) | > 0 | calculated (dòng thời gian đã trải phẳng) | thời điểm node End tính từ Start, không tính thời gian chờ ở HoldGate |
| `charge` | float (s) | 0–`hold_max` | calculated | thời gian chờ thực tế ở HoldGate; 0 nếu không giữ |
| `skill_total_max` | float (s) | 1,5 (`normal`) / 3,0 (`special`) | tuning knob | trần, tính với `charge = hold_max` |

**Output Range:** 1/60 s đến 1,5 s (`normal`) / 3,0 s (`special`). Validator từ chối kỹ năng có `skill_total` (với
`charge = hold_max`) vượt trần; node `dive` không tính (thời gian rơi phụ thuộc độ cao).
**Example:** đòn đất dự phòng (P): End ở 0,20 s → **0,20 s**. Đòn nện tụ lực của búa (đề xuất): HoldGate ở 0,30 s,
hold_max 0,60, End ở 0,75 s → 0,75 + 0,60 = **1,35 s** ≤ 1,5 s.

### F2. Thời gian cam kết (dùng cho Boss Encounter)

The commit_time formula is defined as:

`commit_time = t_CancelWindow` (thời điểm cửa sổ hủy cho né mở, tính từ Start, nhánh `tap`)
(không có CancelWindow: `recovery_start + default_cancel_lockout`; kỹ năng có `dive`: thời điểm bắt đầu node `dive`,
vì từ đó né hủy được mọi pha — Rule 9 (d))

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `t_CancelWindow` | float (s) | trong biên Rule 9 | data file (XML) | mốc của node CancelWindow |
| `default_cancel_lockout` | float (s) | 1/60–0,10; mặc định 0,03 (P) | tuning knob | dùng khi kỹ năng không đặt CancelWindow |
| `commit_time_max(họ)` | float (s) | calculated | calculated | lớn nhất trên mọi kỹ năng `normal` của bảng tổ hợp một họ vũ khí |

**Output Range:** 0,05 s đến ~1,2 s. Kỹ năng có HoldGate tính với nhánh `tap` (`charge = 0`), vì tụ lực là tùy
chọn, không bắt buộc để hạ boss. Boss Encounter dùng `commit_time_max(họ)` chậm nhất để đặt cửa sổ dễ tổn thương
≥ 1,25 × giá trị đó (concept: "mọi vũ khí hạ được mọi boss", chốt ở GDD Combat).
**Example:** đòn đất dự phòng (P): CancelWindow ở **0,13 s**; đòn khi lướt (P): **0,18 s**.

### F3. Giới hạn node lướt tới (lunge)

The lunge_limits formula is defined as:

`d_node ≤ r_node × D_dodge` ; `Σ d_node ≤ r_total(kind) × D_dodge` ;
`d_node / t_node + gait_max × attack_move_mult ≤ V_dodge` ; `Σ d_node (mỗi lần lên không) ≤ r_air × D_dodge`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `D_dodge` | float (u) | 6,46 | calculated (Controller) | quãng né = `dodge_speed × dodge_duration` = 14 × 0,20 × k (registry `horizontal_reach` dùng 2,8 u prototype) |
| `V_dodge` | float (u/s) | 32,3 | constant (Controller `dodge_speed`) | tốc độ né, đơn vị game |
| `r_node` | float | 0,3–0,7; mặc định 0,5 | tuning knob | trần một node, theo tỉ lệ quãng né |
| `r_total(normal)` | float | 0,5–0,9; mặc định 0,75 | tuning knob | trần tổng một kỹ năng thường |
| `r_total(special)` | float | 0,75–1,0; mặc định 1,0 | tuning knob | trần tổng một kỹ năng đặc biệt |
| `d_node`, `t_node` | float (u, s) | ≥ 0, > 0 | data file (XML) | quãng và thời lượng của một node |
| `gait_max` | float (u/s) | 15 | Controller `move_speed` | tốc chạy (dáng nhanh nhất) |
| `attack_move_mult` | float | 0,6 trên đất / 1,0 trên không | Controller Rule 13 | lấy theo ngữ cảnh của node |
| `r_air` | float | 0,25–0,75; mặc định 0,5 | tuning knob | ngân sách lướt mỗi lần lên không (`air_lunge_budget` = `r_air × D_dodge` = 3,23 u), hồi khi `Landed`, Teleport, Respawn; không hồi khi nhảy đôi hay né |

**Output Range:** một node ≤ 3,23 u (~3 ô); tổng kỹ năng thường ≤ 4,85 u; kỹ năng đặc biệt ≤ 6,46 u; tốc độ trung
bình của node ≤ 23,3 u/s trên đất (32,3 − 15 × 0,6) và ≤ 17,3 u/s trên không (32,3 − 15 × 1,0), để **lực lướt cộng
điều khiển của người chơi cũng không nhanh hơn một cú né** (giữ ý Controller validator 6, và giữ dưới ngưỡng bám đất
F6 của Controller trên dốc lồi). Trên không, tổng lướt của mọi kỹ năng trong một lần bay ≤ 3,23 u: không thể chém liên
tục để kéo dài tầm nhảy quá Controller F3 thêm một quãng lớn hơn nửa cú né.
**Example:** đòn đâm của thương: `lunge(2,0 u, 0,10 s)` → 2,0 ≤ 3,23 ✓; 20 + 9 = 29 u/s ≤ 32,3 ✓. Với `lunge(2,5 u,
0,10 s)`: 25 + 9 = 34 > 32,3 ✗ → validator kẹp thời lượng lên 0,11 s.

### F4. Quãng dịch chuyển thực khi lực cộng vào điều khiển

The lunge_displacement formula is defined as:

`Δx = facing × d_node + Σ_t (input_x × gait_speed × attack_move_mult × dt)`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `input_x` | float | −1–1 | Input | hướng ngang người chơi đang giữ |
| `gait_speed` | float (u/s) | 4 (đi bộ) / 15 (chạy) | Controller `walk_speed` / `move_speed` | dáng đang dùng (C-13) |
| `attack_move_mult` | float | 0,6 trên đất / 1,0 trên không | Controller Rule 13 | |

**Output Range:** quãng danh nghĩa `d_node` ± phần điều khiển trong thời lượng node. Trên không (hệ số 1,0), giữ ngược
hướng có thể cho dịch chuyển ròng âm (ví dụ `lunge(2,0 u, 0,15 s)`: 2,0 − 15 × 0,15 = −0,25 u) — đó là lựa chọn của
người chơi, được phép.
**Example:** `lunge(2,0 u, 0,10 s)` trên đất, người chơi chạy và giữ **ngược** hướng: phần điều khiển = −1 × 15 × 0,6 ×
0,10 = −0,9 u → Δx = **1,1 u**; giữ **cùng** hướng: **2,9 u**; không giữ: **2,0 u**. Đi bộ (4 u/s): 2,24 / 1,76 u.

### F5. Bật lên (launch)

The launch_speed formula is defined as:

`v_launch = √(2 × g × h_launch)` với `h_launch ≤ r_launch × H_jump`; chỉ khi node bắt đầu trên mặt đất

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `g` | float (u/s²) | 72,4 | Controller (`gravity_scale` × 9,81 × k) | trọng lực đơn vị game |
| `H_jump` | float (u) | 6,29 | registry `jump_apex_height` (2,727 P × k) | độ cao nhảy đơn |
| `r_launch` | float | 0,25–1,0; mặc định 1,0 | tuning knob | trần độ cao bật theo tỉ lệ nhảy đơn |
| `h_launch` | float (u) | 0–`r_launch × H_jump` | data file (XML) | độ cao khai báo trong node |

**Output Range:** 0 đến 30,2 u/s (bật cao bằng nhảy đơn). Không tiêu lượt nhảy. Bật chỉ từ mặt đất, nên độ cao tối
đa của nhân vật vẫn là nhảy + nhảy đôi của Controller F2 (không có vòng "bật → đánh trên không → bật"); launch không mở
coyote, nên không có "bật rồi nhảy từ đất".
**Example:** hất lên của thương `launch(3 u)` → v = √(2 × 72,4 × 3) = **20,8 u/s**.

### F6. Cửa sổ né muộn nhất và vòng nối đòn

The cancel_bounds formula is defined as:

`dodge_window_start ≤ recovery_start + dodge_cancel_latest` ; `Σ commit_time(vòng) ≥ min_loop_commit`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `recovery_start` | float (s) | calculated | calculated | thời điểm cửa sổ hit cuối cùng đóng (Rule 5) |
| `dodge_cancel_latest` | float (s) | 0,05–0,25; mặc định 0,15 | tuning knob | né phải mở không muộn hơn mức này sau khi bắt đầu thu đòn (kỹ năng `normal`) |
| `min_loop_commit` | float (s) | 0,15–0,40; mặc định 0,20 | tuning knob | tổng thời gian cam kết tối thiểu của một vòng nối đòn A → … → A |

**Output Range:** validator từ chối kỹ năng `normal` có cửa sổ né mở muộn hơn `recovery_start + 0,15 s` (khóa người
chơi quá lâu, phạm binding test 5) và bảng có vòng nối đòn ngắn hơn 0,20 s (vòng hủy gần như không tốn gì).
**Example:** chuỗi g1 → g2 → g1 với commit 0,13 + 0,13 = 0,26 s ≥ 0,20 ✓.

### F7. Hồi chiêu kỹ năng đặc biệt

The special_cooldown_remaining formula is defined as:

`remaining = max(0, cooldown − (t_now − t_active_start))`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `cooldown` | float (s) | `special_cooldown_min`–30; mặc định theo stone | data file (XML) | |
| `special_cooldown_min` | float (s) | 4–20; mặc định 8 | tuning knob | trần dưới cho mọi kỹ năng đặc biệt |
| `t_active_start` | float (s) | đồng hồ Game | calculated | lúc cửa sổ hit đầu tiên mở trong lần dùng gần nhất của kỹ năng đặc biệt (chỉ có một, Rule 17) |

**Output Range:** 0 đến `cooldown` (kẹp vào [0, `cooldown`]). Lưu game ghi `remaining`; nạp lại đặt `t_active_start
= t_now − (cooldown − remaining)`. Tính trên đồng hồ Game (hit-stop và pause làm chậm / dừng hồi chiêu như mọi thứ
khác). HUD hiển thị `remaining / cooldown`.
**Example:** stone gió, cooldown 10 s, `active` bắt đầu ở 42,0 s; ở 47,5 s: remaining = 10 − 5,5 = **4,5 s**.

### F8. Tụ lực

The charge_time formula is defined as:

`charge = clamp(t_release − t_gate, 0, hold_max)` ; nhánh `charged` nếu nút còn giữ liên tục tại `t_gate` (mốc node HoldGate)

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `hold_threshold` | float (s) | 0,15–0,40; mặc định 0,25 | tuning knob (chung mọi họ) | thời gian giữ tối thiểu để tính là "giữ"; node HoldGate phải ở độ trễ ≥ `hold_threshold` |
| `hold_max` | float (s) | 0,20–1,0 | data file (XML) | tụ lực tối đa; bắt buộc có (không giữ vô hạn) |
| `t_release` | float (s) | đồng hồ Game | Input (`IsHeld` chuyển sang false) | |

**Output Range:** `charge` từ 0 đến `hold_max`. Một hằng `hold_threshold` chung cho mọi họ để người chơi không phải học
lại ngưỡng giữ theo vũ khí. Bấm nhanh (nhả trước `t_gate`) luôn đi tiếp nhánh `tap` ở `t_gate`, không bị trễ.
**Example:** búa, HoldGate ở 0,30 s, hold_max 0,60 s; giữ 0,70 s rồi nhả → tại `t_gate` = 0,30 nút còn giữ →
`charging`; charge = 0,70 − 0,30 = **0,40 s**.

### F9. Co giãn theo nhánh

The scaled_timeline formula is defined as:

`t' = t × timeline_scale` (mọi mốc và `duration`) ; `v_lunge' = d_node / (t_node × timeline_scale)` ; `clip_speed = 1 / timeline_scale`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `timeline_scale` | float | trong dải của họ, toàn cục 0,7–1,3 (đề xuất) | data file (Weapon Tree, theo nhánh) | < 1 nhanh hơn, > 1 chậm hơn; 1,0 = đúng như XML |
| `scale_band(họ)` | [float, float] | ⊂ 0,7–1,3 | data file (Weapon Tree) | dải tốc độ làm bản sắc của họ vũ khí (concept: "mỗi họ vũ khí có một dải tốc độ") |

**Output Range:** dòng thời gian co giãn trong ±30% (đề xuất). Ở đầu nhanh của dải, tốc độ lướt tăng nên có thể vượt trần
F3 — validator kiểm F3 với `timeline_scale` nhỏ nhất của dải; ở đầu chậm, F1 và F6 kiểm với `timeline_scale` lớn nhất.
**Example:** đòn đất dự phòng (End 0,20, CW 0,13) ở nhánh nhanh 0,85 → End **0,17 s**, CW **0,11 s**, clip phát × 1,18;
ở nhánh mạnh 1,15 → End **0,23 s**, CW **0,15 s**. Một XML, hai nhánh.

## Edge Cases

*Từ systems-designer (2026-11-05), đã bỏ các trường hợp đổi vũ khí / đổi stone giữa trận (không còn xảy ra: Rule 17).
Đơn vị game (× 30/13) trừ khi ghi (P).*

### Nhiều lần bấm cùng lúc và bộ đệm

- **If Attack và Special được bấm cùng frame**: lần bấm có `seq` nhỏ hơn ra trước (Controller Rule 15), lần kia nằm lại
  bộ đệm. Attack trước → Special ra ở frame đầu `recovery.open` của đòn đó nếu còn trong 0,20 s. Special trước →
  Attack chỉ ra từ `recovery_start + cancel_lockout` của Special, thường đã quá cửa sổ 0,20 s nên hết hạn (log
  `CancelLockout`). Một hàng đợi, một luật thứ tự, không có ưu tiên ngầm.
- **If cả Attack và Special cùng nằm trong bộ đệm khi đang `recovery.locked`**: frame đầu `recovery.open` tiêu thụ lần
  bấm có `seq` nhỏ nhất **trong số những lần tiêu thụ được** (cùng mẫu "Jump và Dodge trong bộ đệm" của Controller).
- **If Special được bấm trong hit-stop**: trạng thái cho phép thì kỹ năng bắt đầu ngay, dòng thời gian chạy theo time
  scale 0,05; hồi chiêu bắt đầu ở đầu `active` theo đồng hồ Game. Chưa cho phép thì cửa sổ 0,20 s gần như đứng lại.
- **If Special được bấm trong lúc né và né kết thúc ở trên không** (né trên đất lướt khỏi mép): xét theo ngữ cảnh
  `air`; không có biến thể trên không thì nằm lại bộ đệm chờ chạm đất (Rule 18).

### Chuỗi đòn và bảng tổ hợp

- **If lần bấm được tiêu thụ đúng tick `t_end + link_window`**: vẫn nối được (so ≤ theo tick, như coyote của Controller
  Rule 30). Mốc so là **lúc tiêu thụ**, không phải lúc bấm.
- **If dòng nối khớp nhưng kỹ năng của nó bị tắt lúc nạp**: hàm chọn bỏ qua dòng đó và xét dòng kế; kỹ năng dự phòng chỉ
  dùng khi không còn dòng nào; kỹ năng dự phòng kết thúc thì xóa `chain_ref`. Giữ chuỗi gần đúng nhất thay vì nhảy sang
  đòn dự phòng.
- **If kỹ năng `normal` có `cooldown` > 0 đang hồi**: dòng của nó bị loại ở bước lọc; Rule 13 vẫn bảo đảm luôn ra một đòn.
- **If hai dòng hòa cả `priority`, `after` lẫn độ cụ thể mà điều kiện chồng nhau một phần**: chọn theo thứ tự khai báo;
  validator **cảnh báo** (không chặn), vì "không trùng dòng" chỉ bắt được dòng trùng hoàn toàn.

### Giữ để tụ lực (HoldGate)

- **If nút được nhả đúng frame luồng tới HoldGate**: nhánh `tap`. Input xử lý trước Controller trong frame (Controller Rule
  30), nên `IsHeld` đã là false — đúng lời hứa "bấm nhanh luôn ra đòn ngay".
- **If người chơi nhả rồi bấm Đánh lần nữa và giữ trước `t_gate`**: nhánh `tap`. HoldGate kiểm "nút giữ **liên tục** từ
  lần bấm đã mở kỹ năng" bằng `WasReleasedAfter(nút, seq_mở)` (như cắt nhảy của Controller); lần bấm thứ hai nằm lại bộ
  đệm làm đòn nối. Không có kiểm tra này, bấm liên tục sẽ vô tình tụ lực.
- **If kỹ năng `special` có HoldGate**: nút được xét là **Special** (nút đã mở kỹ năng), không phải Attack.
- **If bị đánh trúng khi đang `charging` mà không có giáp**: Hitstun hủy kỹ năng, xóa `chain_ref`. Hết choáng mà vẫn giữ
  nút thì không có gì xảy ra — giữ không phải là bấm.
- **If đang `charging` một đòn trên không rồi chạm đất**: `cancel_on_land` thì hủy (đặc biệt chưa tới `active` nên không
  tốn hồi chiêu); trọng lực vẫn chạy khi tụ lực, không lơ lửng. Validator **cảnh báo** đòn trên không có HoldGate dùng
  `cancel_on_land`.
- **If nhả nút đúng tick đạt `hold_max`**: nhánh `charged`, `charge = hold_max`.
- **If nhánh `charged` ra với `charge` rất ngắn (1/60 s)**: cùng nhánh, cùng `hit_profile` với lúc tụ đủ. Nhánh `charged`
  là **nhị phân**; `charge` chỉ đổi thời lượng. Mạnh dần theo thời gian giữ (nhiều nấc) là mở rộng sau (Open Questions).

### Node chuyển động

- **If lướt khỏi mép (kể cả platform một chiều)**: lực đi tiếp tới hết node, trọng lực vẫn chạy, coyote bắt đầu đếm
  ("rời đất không do nhảy" của Controller); lướt không bao giờ kích hoạt DropThrough.
- **If đã sát tường khi node lướt bắt đầu**: lực bị bỏ ở bước đầu, node coi như **đã dùng**, không thử lại; node sau vẫn
  chạy; cửa sổ hit vẫn mở. Dốc dựng quá `max_walk_angle` tính là tường.
- **If lướt trong hit-stop** (kể cả hit-stop do chính đòn gây ra giữa node): lực nhân `dt` của đồng hồ Game, tổng quãng
  giữ nguyên, chỉ kéo dài theo thời gian thật.
- **If bị trói (root) giữa kỹ năng**: lực kỹ năng về 0 (cả lunge lẫn launch); kỹ năng và cửa sổ hit vẫn chạy (root không
  chặn đánh). Không có ngoại lệ này, lướt thành cách thoát trói.
- **If kỹ năng trên đất chuyển sang trên không giữa node** (lướt khỏi mép): `attack_move_mult` đổi theo tiếp xúc ở từng
  bước (0,6 trên đất, 1,0 trên không); F4 tính theo từng đoạn. Nếu là nhảy: lực kỹ năng bị xóa (Rule 9).
- **If node `launch` gặp trần ngay phía trên**: vẫn đặt vận tốc lên; chạm trần thì `vy = min(vy, 0)` và phát `Bonked`
  (Controller Rule 23, có `corner_correction`). Tất định, không có kiểu "đứng dưới trần cho khỏi bật".
- **If node `launch` bắt đầu khi đang trên không**: node bị bỏ qua (Rule 7), log ở dev build; phần còn lại của kỹ năng
  chạy bình thường.
- **If launch làm nhân vật rời đất**: không mở coyote; bật cờ `jump_rising` (tắt snap — nếu không, bật trên dốc bị snap
  kéo xuống); không treo ở đỉnh; nhả Jump không cắt vận tốc của launch.

### Chạm đất (`land_policy`)

- **If kỹ năng ngữ cảnh `ground` hoặc `dash` đang chạy mà nhân vật rời đất không do nhảy**: kỹ năng tiếp tục; đáp lại thì
  không hủy (`continue` là mặc định của kỹ năng `ground`/`dash`; `cancel_on_land` chỉ là mặc định của kỹ năng `air`).
  Đòn khi lướt đáp đất thì quán tính F8 của Controller chạy tiếp với ×0,6.
- **If cửa sổ hit của đòn trên không (`cancel_on_land`) đang mở đúng frame chạm đất**: cửa sổ đóng ở bước va chạm của
  frame đó, Combat không thấy hit trong frame đó; không có LandRecovery (Controller Rule 12); Attack trong bộ đệm ra
  thành đòn trên đất ở bước kế. Không có hit "ma" trên mặt đất.
- **If kỹ năng đặc biệt trên không bị hủy vì chạm đất sau khi đã vào `active`**: hồi chiêu vẫn tính. Validator cảnh báo
  kỹ năng đặc biệt trên không dùng `cancel_on_land`.
- **If kỹ năng có `dive` chạm platform một chiều**: đáp xuống đó (đang đi xuống, chân ở trên mặt), phát `PlungeLanded` và
  `Landed`, hồi lượt nhảy đôi, né trên không và `air_lunge_budget`, rồi vào LandRecovery. Platform đang trong
  `drop_ignore_time` (vừa DropThrough) thì đi xuyên, đáp lớp dưới.
- **If kỹ năng có `dive` chạm đất**: kỹ năng **kết thúc** ở frame chạm đất; LandRecovery dùng `land_recovery` của
  Controller, trường `recovery` của kỹ năng bị bỏ qua. Đồng hồ pha **đứng ở node `dive`** cho tới khi chạm đất, nên không
  tính vào `skill_total`. Rơi mãi không chạm đất thì `FellOutOfWorld` xử lý; Teleport/Respawn hủy kỹ năng.

### Kỹ năng đặc biệt

- **If một đòn của kẻ địch tới đúng tick giáp kết thúc**: vẫn được giáp (cửa sổ tính cả tick cuối, so ≤). Đòn nhiều nhát
  xét từng nhát theo tick của nó; nhát tới sau khi giáp hết hoặc sau khi đạt trần số đòn của Combat gây Hitstun và hủy
  kỹ năng (hồi chiêu vẫn tính nếu đã qua `active`). Trong lúc có giáp, Combat không gửi `HitReceived`, nên không có
  knockback.
- **If có yêu cầu khóa điều khiển giữa kỹ năng đặc biệt**: hủy ngay như đòn thường (Controller C-11); hồi chiêu giữ nếu đã
  qua `active`. Kỹ năng có `dive` thì chạy tới khi chạm đất.
- **If bấm Special khi chính kỹ năng đặc biệt đang chạy**: từ chối (Rule 19). Hồi chiêu còn ≥ 8 s, lớn hơn cửa sổ đệm, nên
  lần bấm không nằm lại bộ đệm.
- **If lưu game giữa lúc đang hồi chiêu**: lưu `remaining`, nạp lại theo F7. Respawn → Open Questions (đề xuất: hồi chiêu
  về 0, để đánh lại boss công bằng).

### Dữ liệu

- **If hai họ vũ khí dùng chung một id kỹ năng với `context` khác nhau**: ngữ cảnh hiệu dụng của dòng = `context` của dòng
  giao với `contexts` của kỹ năng; giao rỗng thì validator từ chối; `any` chỉ phủ một phần thì cảnh báo. Kỹ năng có `dash`
  trong `contexts` mà chứa `lunge` bị từ chối dù dòng đang dùng là `ground`. Hệ số sát thương theo vũ khí là việc của
  Combat.
- **If bảng đổi tên tạo chuỗi, hoặc kỹ năng `deprecated` trỏ tới một kỹ năng `deprecated` khác**: lúc nạp đi theo chuỗi
  tới id cuối, tối đa 8 bước; vòng (a → b → a) hoặc `id_cũ` vẫn còn file thật thì build lỗi; lọt tới dev build thì coi
  là thiếu (dự phòng; Special: không làm gì), log một lần. Dữ liệu lưu mang id cũ được ghi lại bằng id cuối.
- **If `schema_version` mới hơn bản game biết**: file bị từ chối, coi như thiếu (`SchemaTooNew`). Nếu là **file bảng tổ
  hợp** thì cả họ vũ khí dùng ba kỹ năng dự phòng: mỗi ngữ cảnh một đòn, không có chuỗi.
- **If XML lệch mã băm ở dev build hoặc Editor**: vẫn nạp nếu nội dung qua kiểm tra lúc chạy, kèm cảnh báo một lần và dấu
  hiệu trên màn hình dev. Bản release không gặp được vì build đã chặn (Rule 21). Lệch mã băm là lỗi quy trình, không
  phải dữ liệu nguy hiểm.
- **If file kỹ năng được lưu khi game đang chạy** (thêm 2026-11-11 — người dùng: sửa trong Play Mode, không dừng / chạy
  lại): lần ra đòn **kế tiếp** dùng bản mới; đòn đang chạy chạy nốt bản cũ. File mới có lỗi thì game giữ bản cũ và báo
  lỗi (không bao giờ mất kỹ năng vì một lần lưu hỏng). Xóa file thì kỹ năng biến mất như thiếu (dự phòng). Prefab VFX sửa
  khi đang chạy: lần spawn kế dùng prefab mới, effect đang hiện chạy nốt. Chỉ có ở Editor (ADR-0002, ADR-0003 §1f).
- **If thiếu khóa `anim`**: gameplay vẫn chạy theo dòng thời gian; Presentation phát clip chung theo ngữ cảnh, log một lần.
  Lúc build: thiếu ở look mặc định là lỗi, thiếu ở look khác là cảnh báo.
- **If Combat không biết `hit_profile_id` hoặc id spawn**: lúc build là lỗi chặn; lúc nạp runtime thì kỹ năng coi như
  không hợp lệ (đòn thường: dự phòng; Special: không làm gì). Một đòn chạy hình mà không gây sát thương sẽ đánh lừa người
  chơi.
- **If mốc giây của hai sự kiện rơi vào cùng một bước 1/60 s**: xảy ra trong cùng bước theo thứ tự trên luồng. Cửa sổ
  hit hoặc giáp có `duration` < 1/60 s vẫn kéo dài đúng 1 bước. Validator kiểm các biên (Rule 9, F1) sau khi quy về bước.
- **If node PlayAnimation thứ hai chạy khi clip trước chưa hết**: clip mới thay clip cũ ngay ở mốc của nó (một kỹ năng
  có thể ghép nhiều clip). Clip hết trước End thì giữ frame cuối tới End; End tới trước khi clip hết thì clip bị cắt —
  validator cảnh báo nếu End sớm hơn độ dài clip của look mặc định quá 0,1 s (hình bị cắt rõ).

### Chiến thuật suy biến (đã chặn hoặc chấp nhận có chủ đích)

- **Chém liên tục trên không để kéo dài tầm nhảy** (không chặn: khoảng +34 u so với tầm 26,7 u của Controller F3): chặn bằng
  `air_lunge_budget` 3,23 u mỗi lần bay (F3). Controller F3 vẫn cần thêm hạng `R_skill` ≤ 3,23 u (hoặc 6,46 u với kỹ
  năng đặc biệt) để luật khe của khu bí mật / lối tắt tính cả phần này.
- **Bật lên cộng nhảy đôi** (không chặn: ~17,9 u so với 11,67 u, +54%): chặn bằng "bật chỉ từ mặt đất" (Rule 7, F5).
- **Đổi vũ khí để làm mới chuỗi / hồi chiêu**: không thể — chỉ đổi ở nhà (Rule 17).
- **Vòng hủy dưới `min_loop_commit` có dùng Special**: bị chặn tự nhiên vì hồi chiêu ≥ 8 s; validator không cần xét
  Special trong vòng. Vòng hủy bằng nhảy (đòn đất → nhảy → đòn trên không → chạm đất hủy → lặp) mất ~0,37 s cho 2 nhát,
  chậm hơn chuỗi đất: chấp nhận có chủ đích.
- **Kỹ năng đặc biệt thay né**: lướt 6,46 u có giáp nhưng không bất tử (vẫn mất máu), hồi chiêu ≥ 8 s, không nhanh hơn né
  (F3): chấp nhận. Gửi Combat ràng buộc: giáp không chặn sát thương của đòn boss "bắt buộc né".
- **Leo platform một chiều chồng nhau bằng launch từ mặt đất**: hợp lệ, như leo bằng nhảy.

## Dependencies

### Upstream (hệ này phụ thuộc vào)

| Phụ thuộc | Loại | Giao diện | Ghi chú |
|---|---|---|---|
| Character Controller (#6) | cứng | máy trạng thái đòn, API node chuyển động (Rule 7), luật hủy (Rule 14), `chain_ref`, `context`/`moving`/`dir` của bản ghi lần bấm | GDD Designed; **phải sửa Rule 12, 14, validator 11, binding test 1** (Detailed Design › Interactions) |
| Combat (#9) | cứng | `hit_profile_id`, spawn, giáp, hit-stop qua đồng hồ Game | **Chưa có GDD.** Hợp đồng tạm: kỹ năng chỉ tham chiếu id, Combat định nghĩa mọi con số; thiếu id → cảnh báo lúc build, kỹ năng chạy không gây sát thương |
| Input (#1) | cứng | Attack (có sẵn), **Special (mới)**, `IsHeld(Attack)` cho HoldGate, bộ đệm và `seq` | GDD Designed; cần thêm hành động Special vào Rule 2 (I-10 mới) |
| Game Data & Tuning Config (#4) | cứng cho hiện thực | định dạng XML, nơi nạp, `schema_version`, mã băm | **Chưa có GDD**; quyết định kỹ thuật → ADR-0002 (runtime) và ADR-0003 (soạn / xuất) |
| Loadout & Player Stats (#16) | mềm | họ vũ khí đang cầm, stone đang gắn | Chưa có GDD; thiếu thì dùng bảng của vũ khí khởi đầu, không có stone |

### Downstream (hệ phụ thuộc vào hệ này)

| Hệ | Loại | Dùng gì | GDD |
|---|---|---|---|
| Skill Stone (#17) | cứng | schema kỹ năng `kind: special`, luật hồi chiêu theo viên (Rule 16–19) | chưa có |
| Weapon Tree (#15) | cứng | bảng tổ hợp theo họ vũ khí; nhánh trỏ tới bảng | chưa có |
| Loadout & Player Stats (#16) | mềm | validator "stone dùng được với họ vũ khí này" | chưa có |
| Enemy AI, Spawning & Drops (#18) | cứng | kỹ năng `owner: creature`, giá trị Start (Owner / Target), Search theo GID, LookAt và cổng dữ liệu (Rule 4a–4c) | chưa có |
| Boss Encounter (#19) | mềm | `commit_time`, `commit_time_max(họ)` (F2) | chưa có |
| Animation / Presentation (đã có code) | cứng | khóa `anim`, mốc node PlayAnimation, `timeline_scale` | không có GDD riêng; ghi ở Character Controller › Visual |
| HUD (#30) | mềm | hồi chiêu đặc biệt, ô trống, "chưa sẵn sàng" | chưa có |
| Audio (#33) | mềm (quan sát) | cue | chưa có |
| Telemetry (#12) | mềm (quan sát) | kỹ năng đã dùng, lỗi dữ liệu | chưa có |
| Level design | ràng buộc | lunge / launch không vượt quá tầm của Controller F3 (Edge Cases) | — |

### Kiểm tra hai chiều

- **Character Controller Rule 16 (khóa hướng mặt giữa đòn):** LookAt dùng được cho kỹ năng người chơi (2026-11-07) —
  khi lan thay đổi, Rule 16 của Controller cần ghi ngoại lệ "node LookAt của kỹ năng đổi hướng mặt ở mốc của nó".

- **Character Controller:** bảng Upstream của Controller ghi "Weapon Moveset (#14) — nhịp đòn theo bốn ngữ cảnh"; cần
  đổi thành "Skill & Moveset — kỹ năng XML và bảng tổ hợp" khi lan thay đổi.
- **Input:** Downstream của Input chưa có hệ này; thêm khi bổ sung hành động Special.
- **Enemy AI (#18):** chưa có GDD; systems index đã thêm *Depends On* Skill & Moveset (2026-11-07). GDD Enemy AI phải
  ghi cạnh này (calls: chọn và chạy kỹ năng quái, cấp `SkillStartValues`).
- **Systems index (Phase 5):** gộp Weapon Moveset (#14) thành "Skill & Moveset", trỏ `design/gdd/skill-moveset.md`;
  Skill Stone (#17) giữ hàng riêng nhưng *Depends On* thêm Skill & Moveset; Weapon Moveset thêm *Depends On* Input,
  Loadout.

## Tuning Knobs

*Knob lấy từ biến của Formulas. Knob "chung" nằm trong file cấu hình của hệ; knob "theo kỹ năng" nằm trong XML của
từng kỹ năng. Số (P) là prototype, số (đề xuất) chưa playtest.*

### Knob chung của hệ

| Knob | Mặc định | Khoảng an toàn | Ảnh hưởng | Quá cao | Quá thấp |
|---|---|---|---|---|---|
| `default_cancel_lockout` | 0,03 s (P) | 1/60–0,10 | cửa sổ hủy khi kỹ năng không đặt CancelWindow (Rule 9, F2) | đòn cứng nhắc | đòn hủy gần như tức thì sau nhát cuối |
| `hold_threshold` | 0,25 s (đề xuất) | 0,15–0,40 | thời gian giữ để tính là tụ lực (F8) | tụ lực chậm, khó dùng | lần bấm hơi lâu thành tụ lực ngoài ý muốn |
| `r_node` | 0,5 (đề xuất) | 0,3–0,7 | trần một node lướt, × quãng né (F3) | đòn thay né | lướt không cảm thấy được |
| `r_total(normal)` | 0,75 (đề xuất) | 0,5–0,9 | trần tổng lướt của đòn thường | đòn thường thành cách di chuyển | họ vũ khí áp sát mất bản sắc |
| `r_total(special)` | 1,0 (đề xuất) | 0,75–1,0 | trần tổng lướt của kỹ năng đặc biệt | (trần cứng 1,0 — không vượt né) | stone di chuyển kém hấp dẫn |
| `r_launch` | 1,0 (đề xuất) | 0,25–1,0 | trần độ cao bật × nhảy đơn (F5) | vượt tầm leo của level | hất lên không đuổi theo được |
| `r_air` (`air_lunge_budget`) | 0,5 × quãng né = 3,23 u (đề xuất) | 0,25–0,75 | tổng lướt mỗi lần lên không (F3) | chém trên không kéo dài tầm nhảy, phá luật khe của level | họ vũ khí trên không mất bản sắc |
| `skill_total_max(normal)` | 1,5 s (đề xuất) | 0,8–2,0 | trần thời lượng đòn thường (F1) | đòn khóa người chơi lâu | không có chỗ cho đòn tụ lực |
| `skill_total_max(special)` | 3,0 s (đề xuất) | 1,5–4,0 | trần thời lượng kỹ năng đặc biệt | | |
| `dodge_cancel_latest` | 0,15 s (đề xuất) | 0,05–0,25 | né phải mở trong khoảng này sau khi bắt đầu thu đòn (F6) | phạm binding test 5 ("bị khóa") | mọi đòn nhẹ như nhau, mất sức nặng |
| `min_loop_commit` | 0,20 s (đề xuất) | 0,15–0,40 | vòng nối đòn tối thiểu (F6) | khó nối chuỗi tự nhiên | vòng hủy gần như miễn phí |
| `special_cooldown_min` | 8 s (đề xuất) | 4–20 | trần dưới hồi chiêu đặc biệt (F7) | stone ít được dùng, nút thứ hai "chết" | đặc biệt thành đòn thường |

### Knob theo kỹ năng (trong XML)

| Knob | Mặc định / vũ khí khởi đầu | Khoảng an toàn | Ghi chú |
|---|---|---|---|
| độ trễ của mọi node | — | ≥ 0; node có thời lượng kết thúc trước End | **thay cho thời lượng pha** (quyết định 2026-11-05); nhịp đòn của từng họ vũ khí → GDD Weapon Tree |
| mốc CancelWindow | không đặt = `recovery_start` + 0,03 (P) | sau cửa sổ hit đầu tiên; đòn thường ≤ `recovery_start` + `dodge_cancel_latest` | Rule 9 |
| `cooldown` | 0 (`normal`); theo stone (`special`) | 0–2 (`normal`, như Controller hiện có); `special_cooldown_min`–30 (`special`) | |
| `hold_max` | — | 0,20–1,0 | bắt buộc khi có HoldGate |
| `link_window` | `combo_reset` của Controller (0,30 s (P)) | 0,20–0,50 | theo dòng của bảng tổ hợp |
| node `lunge` / `launch` | — | trong trần F3, F5 | |
| cửa sổ `armor` | — | Combat chốt trần | chỉ `special` |

### Knob thuộc hệ khác (hệ này chỉ đọc)

- Character Controller: `attack_move_mult`, `combo_reset`, `dodge_speed`, `dodge_duration`, `plunge_speed`,
  `dash_attack_carry` / `dash_attack_decay` (F8 của Controller), `walk_speed`, `move_speed`.
- Input: `attack_window` (bộ đệm) — phải ≥ `cancel_lockout` dài nhất trong dữ liệu để lần bấm trong lockout còn sống
  khi lockout hết (đưa `max_cancel_lockout_in_data` vào validator 8 của Controller).
- Combat: mọi con số sát thương, hit-stop, knockback, trần giáp.
- Weapon Tree: `timeline_scale` theo nhánh và `scale_band` theo họ (F9).

### Knob ảnh hưởng lẫn nhau

- `hold_threshold` ≤ độ trễ của mọi node HoldGate — tăng `hold_threshold` có thể làm các kỹ năng tụ lực hiện có không qua
  kiểm tra.
- `r_total(normal)` và `r_node`: nếu `r_node > r_total`, trần một node không còn tác dụng.
- `r_air` < `r_node` làm một node lướt trên không không thể dùng hết trần của nó.
- `special_cooldown_min` và `dodge` của Controller: hồi chiêu đặc biệt thấp + lướt đặc biệt dài → đặc biệt thành né
  thứ hai (Edge Cases).

## Visual/Audio Requirements

*Từ art-director (2026-11-05); người dùng chốt D1–D3 và ngân sách cùng ngày. Phân chia: **hệ này sở hữu hình ảnh của
người ra đòn** (tư thế, smear, VFX của node chuyển động, cue); **Combat sở hữu hình ảnh của mục tiêu** (hit flash, tia
va chạm, hit-stop, knockback, sóng xung kích). Mọi VFX chạy theo **đồng hồ Game**, để hit-stop giữ smear đứng cùng
frame chạm.*

### Theo từng pha

| Pha / sự kiện | Hình ảnh | Âm thanh (mức sự kiện) |
|---|---|---|
| `windup` | Thân co ngược hướng đòn, squash 10–15%, lệch ≥ 2 px (art bible §5); 2–3 frame; key pose đọc được bằng silhouette trên 3 nền. Theo họ: Slab squash mạnh nhất, Needle rút theo trục đâm, Crescent/Chain xoay vai, Ring nâng vòng. Khăn cuộn ngược hướng. **Không** flash, bloom hay đổi màu (flash trắng là telegraph của quái, art bible §4). | tiếng vút lấy đà, nặng nhẹ theo họ |
| Vào `charging` | **Ánh lóe xác nhận** ở đầu vũ khí: 1 frame sáng + 4 tick tắt, ≤ 8×8 px, màu Hearth Amber. Cue quan trọng nhất của pha: "chắc chắn ra nhánh charged". | tiếng xác nhận ngắn, trùng ánh lóe |
| Trong `charging` | Clip lặp 2–3 frame × 6 tick; rung 1 px bằng code mỗi 4 tick; hạt hổ phách hút vào vũ khí, mật độ theo `charge / hold_max` (đồng hồ tới lúc tự nhả, không phải nấc sức mạnh). Bị hủy → hạt bắn tung, tan trong 4–6 tick. Không bloom trên nhân vật. | tiếng kéo dài căng dần; dừng ngay khi hủy |
| `active` | 1 frame chạm giữ, stretch theo hướng đòn, hình chủ đạo của vũ khí duỗi ≥ 60%. Smear `vfx_cmn_smear_[họ]` (5 bộ) gọi qua track Cue ở đầu `active`, **chỉ phủ đúng vùng chạm thật** của `hit_profile`. Đòn nhiều nhát: mỗi cửa sổ hit một smear; `active` > 0,15 s thêm 1 frame follow-through. Nhánh `charged` dùng chung frame chạm, smear dày hơn (`_charged`) + bụi dưới chân. Người ra đòn **không** flash. | tiếng vung khớp smear; `charged` dày hơn; tiếng va chạm thuộc Combat |
| `recovery` | 2–4 frame, khăn rơi trễ 1–2 frame. **Người làm đòn đặt CancelWindow trùng lúc clip chuyển từ tư thế còn duỗi sang thân thả lỏng** — người chơi đọc cửa sổ hủy bằng tư thế, không có icon hay glow. | — |
| Hủy bằng né | Cắt sang clip `dodge` **ngay frame đó**; smear và VFX của kỹ năng bị xóa; tint bất tử theo cờ của Controller. | |
| Nhảy giữa đòn | Bụi cất cánh. `windup` và `active` vẫn chạy clip đánh (hình khớp vùng chạm); **tới `recovery` thì cắt sang clip bay** (rise / apex / fall) — quyết định D2. | |
| Hitstun (bị ngắt) | Clip `hitstun` (dùng chung giữa các look); xóa smear và VFX tụ lực. | |

### Node chuyển động

**Không dùng bóng mờ (after-image) cho node chuyển động** — bóng mờ và độ trong là ngôn ngữ của né (Controller AC-VI03);
đòn lướt bị nhầm là né thì người chơi tưởng mình đang bất tử.

| Node | Hình ảnh | Âm thanh |
|---|---|---|
| `lunge` trên đất | bụi đá chân ở frame đầu node (2–3 frame flipbook, giá trị 30–70%); 2–3 vạch tốc độ mực 1 px sau lưng, tắt khi lực mất | cọ đất |
| `lunge` trên không | chỉ vạch tốc độ | |
| `launch` | vòng bụi nổ **từ mặt đất** + vệt dọc ngắn (khác vòng gió nhảy đôi giữa không của Controller); node bị bỏ qua thì không có VFX | nện đất bật lên |
| `dive` | vạch tốc độ dọc phía trên thân; tư thế "đóng ấn triện" (art bible §5); chạm đất thuộc `PlungeLanded` | tiếng rít khi lao |
| `halt` | không có | |

### Kỹ năng đặc biệt

- **Mở đầu — ấn triện mực (D3):** frame đầu `windup`, một dấu triện tròn hoặc vuông sau lưng (nền giấy `#F4E6C8`, nét mực
  `#2A1E1A`, ~24–32 px), squash 1 frame rồi mờ trong 120 ms (cùng nhịp "đóng triện" của UI, art bible §7); khăn bung 1
  frame; key pose lấy đà giữ 3–4 frame. **Không phát sáng, không dùng đỏ son** (an toàn trong arena, art bible §4). Stone
  có nguyên tố: VFX của pha `active` mang dải màu và chữ ký chuyển động của nguyên tố; pha mở đầu vẫn trung tính. Âm thanh:
  tiếng "đóng ấn" riêng, không đòn thường nào dùng.
- **Giáp — viền màu giấy (D1):** khi cửa sổ giáp mở, viền mực 1 px đổi sang `#F4E6C8` (màu khóa viền của shader, art bible
  §8.6); sprite **đục hoàn toàn**. Khác tint bất tử của né (xanh nhạt, trong mờ 55%, phủ toàn thân) ở cả màu, diện tích và
  độ trong; qua được grayscale test. Bị đánh trúng khi có giáp: viền chớp trắng 1 frame + mảnh mực văng + thanh máu vẫn tụt
  (bắt buộc — không có knockback, thiếu cue này người chơi tưởng giáp là bất tử); tiếng va chạm đục khác tiếng đau thường.
  Giáp hết: viền về màu mực + 2–3 mảnh vỡ nhỏ + tiếng vỡ nhỏ. Hit flash viền của Combat và giáp dùng chung kênh màu khóa:
  hit flash ưu tiên 1–2 frame rồi trả về màu giáp; Reduce Flashing cần trạng thái kết hợp (technical-artist).
- **Hồi chiêu (HUD):** xem UI Requirements; hồi xong có tiếng ping nhẹ, nghe được giữa trận nhưng không lấn tiếng hazard
  của boss.

### Hệ VFX kỹ năng (prefab — thêm 2026-11-11)

Quyết định người dùng 2026-11-11: hiệu ứng kỹ năng (vệt chém, tia va chạm, hiệu ứng đặc biệt, hào quang, đạn…) là
**prefab**, Skill Tool chọn chúng từ danh sách có preview.

- **Nơi đặt:** `Assets/Game/Art/VFX/<Loại>/vfx_<vùng|cmn>_<tên>.prefab` — Loại = thư mục: `Slash`, `Hit`, `Special`, `Aura`,
  `Projectile`, `Buff`, `Environment`, `Other`. **Key = tên file** (vd `vfx_cmn_smear_spear`); key là thứ file kỹ năng lưu
  (Effect.PrefabName, Hit.HitEffectAddressable). Tên phải theo luật art bible (chữ thường, số, `_`, ≥ 3 phần); trùng key →
  cảnh báo, bản đầu thắng.
- **Cấu tạo:** gốc prefab có `SkillVfx` (Duration 0 = tự tính, Loop, ghi chú), con là `VfxFlipbook` (flipbook sprite
  32–128 px, fps, loop) và/hoặc `ParticleSystem` (hạt trang trí, ≤ 2000 hạt toàn cảnh). Thời lượng: Duration > 0 thắng →
  effect lặp sống tới khi bị dừng → còn lại lấy cái dài hơn giữa flipbook và hạt (rỗng = 1 s). Effect node `Duration` > 0 ghi
  đè thời lượng của prefab.
- **VFX Catalog** (`Assets/Game/Art/VFX/VfxCatalog.asset`) tự dựng lại khi prefab trong thư mục đổi; game tìm prefab theo
  key qua catalog, **pool theo key** (không Instantiate / Destroy mỗi đòn), thời gian được đẩy bằng đồng hồ Game nên
  hit-stop giữ smear đứng hình như mục trên yêu cầu.
- **Skill Tool:** bấm trường PrefabName / HitEffectAddressable → Effect Browser: lọc loại, tìm chữ, thumbnail, preview chạy
  được (Play / Pause / Restart, kéo thời gian, Loop, Scale, nền), Select / Clear. Key không tồn tại → chữ cam.
- Tạo prefab mẫu: `Assets ▸ Create ▸ Evermeadow ▸ Skill VFX (Flipbook | Particle)` hoặc nút New ▾ trong browser.
- Chưa có: game chưa gọi VFX khi chạy kỹ năng (OQ-S16, OQ-S17).

### Ràng buộc animation

- **Luật pixel** (art bible §5, §8; sprite-sheet-spec): thân 48 px trong ô 96×96, pivot chân (48, 16) ở mọi frame; squash
  và stretch trong ô, smear là VFX nên được ra ngoài ô; không khử răng cưa, alpha 0/255, ≤ 16 màu, viền `#2A1E1A` 1 px là
  màu khóa; vũ khí phóng đại 1,3–1,5×; silhouette thân riêng cho 4 ngữ cảnh (đất / không / lướt / cắm xuống).
- **Clip được phát theo thời gian từ mốc của node PlayAnimation, ở tốc độ của chính nó** (× 1 / `timeline_scale` của
  nhánh, Rule 34); **clip không bao giờ điều khiển dòng thời gian** — độ dài đòn do End quyết định (Rule 5). Người làm đòn canh độ trễ của Hit, CancelWindow và Cue theo frame của clip: **Skill tool hiển thị các
  mốc frame của clip trên trục thời gian** để node Hit trùng frame chạm và Cue trùng hình. Số frame gợi ý: lấy đà 2–3
  (đặc biệt 3–4), chạm 1 (+1 follow-through), thu đòn 2–4; tụ lực 2–3 frame lặp. *(Code hiện tại điều khiển clip đánh
  bằng `AttackProgress` — phải đổi sang phát clip theo thời gian từ node PlayAnimation.)*
- **Các look của cùng một khóa `anim`** nên có cùng độ dài và cùng frame chạm với look mặc định; validator cảnh báo khi
  frame chạm của một look lệch quá 1 frame so với mốc Hit (hình chém lệch lúc gây sát thương).
- **Clip thiếu — thứ tự dự phòng:** (1) clip của khóa `anim` trong look hiện tại → (2) clip chung theo ngữ cảnh của cùng
  look (`attack_ground` / `attack_air` / `attack_dash` / `plunge`) → (3) clip chung của look gốc. Không bao giờ đứng hình ở
  sprite cuối, không bao giờ dùng clip không vũ khí khi đang cầm vũ khí. Bản dev hiện icon "?" nhỏ trên đầu (mực trên
  giấy). *(sprite-sheet-spec §4 "giữ sprite cuối" phải sửa cho clip đánh.)*

### Ngân sách hình

- Không dùng chung clip có vũ khí giữa các look (sprite toàn thân, quyết định 2026-11-02); chỉ `hitstun`, `defeated`,
  `bonk` dùng chung.
- **Bộ khóa `anim` cố định** (danh sách đóng, ví dụ `g1 g2 g3 run up sig air dash plunge charge` + các mẫu đặc biệt); mỗi
  look phải phủ đủ, thiếu thì dự phòng như trên.
- **Kỹ năng đặc biệt không có clip riêng theo stone:** mỗi stone trỏ vào một trong **~5 mẫu đặc biệt** của họ vũ khí
  (ví dụ đâm, xoay, nện, bật lên, phóng); bản sắc stone đến từ VFX, node chuyển động và màu nguyên tố (quyết định người
  dùng 2026-11-05).
- Trong cùng một look, nhiều khóa được trỏ về cùng clip (binding test 1 chỉ cấm "giống nhau" giữa các họ). Nhánh nguyên tố
  và nhánh Weapon Tree "chỉ đổi look" dùng palette swap, không vẽ bộ mới.
- **Ước lượng:** ~80 frame đánh mỗi look vũ khí (chuỗi đất ~16, đánh khi chạy / Lên + Đánh / đòn đặc trưng ~15, trên không
  và khi lướt ~8, cắm xuống 4, tụ lực ~5, 5 mẫu đặc biệt ~35); 5 look ≈ **400 frame đánh thêm**, trên ~110–130 frame cơ bản
  của art bible. Người dùng chấp nhận 2026-11-05; art bible cần cập nhật tổng ngân sách.

### Việc kéo theo ở tài liệu khác

- `design/art/specs/sprite-sheet-spec.md` lên v2.1: danh sách khóa `anim`, đánh dấu pha cho từng frame, sửa §4 (clip thiếu).
- `design/art/art-bible.md` §5 / §8.7: dọn câu cũ "đòn trên không, lướt, cắm xuống dùng chung, chỉ đổi VFX theo họ" và
  đoạn "vũ khí socket"; cập nhật tổng ngân sách frame.
- Mọi VFX kỹ năng không dùng đỏ son (art bible §4, người mù màu tritan lẫn với magenta của boss).

> 📌 **Asset Spec** — Visual/Audio requirements are defined. After the art bible is approved, run `/asset-spec system:skill-moveset` to produce per-asset visual descriptions, dimensions, and generation prompts from this section.

## Game Feel

Cảm giác của hệ này là **"tay hỏi, vũ khí đáp ngay — và đáp theo cách riêng"**. Mục tiêu đo được:

| Mục tiêu | Chỉ số | Nguồn |
|---|---|---|
| Lần bấm Đánh ra đòn ở frame đầu tiên được phép | 0 frame trễ do hệ chọn kỹ năng (hàm thuần, cùng bước với Controller) | Rule 13, 20; Controller Rule 15 |
| Bấm nhanh không bao giờ chờ để biết là chạm hay giữ | nhánh `tap` đi tiếp ngay ở HoldGate, không thêm độ trễ | Rule 12, F8 |
| Đòn đọc được trước khi trúng | clip hiện ít nhất một tư thế lấy đà trước khi cửa sổ hit đầu tiên mở (art bible §5: anticipation 2–3 frame); tụ lực có dấu hiệu nhìn thấy | Visual/Audio |
| Quãng di chuyển của một đòn giống nhau mọi lần | cùng kỹ năng, cùng input → cùng quãng (sai số ≤ 1 px khi không có va chạm) | Rule 7, F4 |
| Không bao giờ "bị khóa" | nhảy ra ở mọi pha; né mở không muộn hơn `recovery_start + 0,15 s` ở đòn thường | Rule 9, F6 |
| Sức nặng nằm ở đòn, không ở chân | vũ khí nặng = `windup`/`recovery` dài hơn, **không** đổi tốc chạy hay nhảy | Player Fantasy; Controller binding test 1 |
| Mỗi họ vũ khí khác nhau rõ ràng | trong playtest, cùng một ô tổ hợp được mô tả khác nhau giữa hai họ (binding test 1 của hệ này) | Acceptance Criteria |

**Nhịp tham chiếu (vũ khí khởi đầu, P):** đòn đất 0,20 s, commit 0,13 s; chuỗi 3 nhát ~0,6 s nếu bấm đều trong
`link_window`. Họ vũ khí nhanh nên ở dưới mốc này, họ vũ khí nặng có thể lên tới ~0,5 s mỗi đòn (dải tốc độ thuộc GDD
Weapon Tree).

**Hit-stop** chậm cả dòng thời gian kỹ năng và node chuyển động theo đồng hồ Game, nên cú đâm "ăn" vào mục tiêu mà
không làm lệch quãng. Hit-stop do Combat quyết; hệ này chỉ có thể gửi gợi ý qua cue.

## UI Requirements

Hệ này cung cấp dữ liệu; HUD và menu hiển thị:

| Thông tin | Ở đâu | Yêu cầu |
|---|---|---|
| Hồi chiêu kỹ năng đặc biệt | HUD, cạnh biểu tượng stone | hiển thị `remaining / cooldown` (F7); tín hiệu "sẵn sàng" khi về 0 (hình + tiếng nhẹ) |
| Trạng thái nút Kỹ năng đặc biệt | HUD | bốn trạng thái phân biệt được không cần màu: **sẵn sàng**, **đang hồi**, **ô trống** (chưa gắn stone), **lỗi** (kỹ năng hỏng, Rule 18) |
| Bấm Special bị từ chối | HUD | tín hiệu "chưa sẵn sàng" ngắn, không chặn màn hình |
| Danh sách đòn (move list) của vũ khí đang cầm | menu Tạm dừng / lò rèn | sinh tự động từ bảng tổ hợp: mỗi dòng = tổ hợp nút (glyph theo thiết bị, Input Rule 14) + tên đòn (`name_key`); đòn tụ lực ghi "giữ"; đòn nối ghi "sau đòn X". Không viết tay danh sách này |
| Kỹ năng của Skill Stone | lò rèn / màn hình gắn stone | tên, mô tả, hồi chiêu, có biến thể trên không hay không, họ vũ khí dùng được |

Pillar 1 cần người chơi **tự** tìm ra đòn: move list có thể ẩn các đòn chưa từng dùng (hiện "???") cho tới lần đầu ra
đòn đó (Open Questions).

## Cross-References

| Tài liệu | Phần liên quan | Quan hệ |
|---|---|---|
| `design/gdd/character-controller.md` | Rule 12–17, 23–28; F2, F3, F6, F8; binding test 1–2, 5; validator 6, 8, 11, 13; Edge Cases | hệ này thay ngữ cảnh đòn cứng bằng kỹ năng dữ liệu; **cần lan thay đổi** (Detailed Design › Interactions) |
| `design/gdd/input.md` | Rule 2 (bảng hành động), Rule 4 (Plunge tổ hợp), Rule 14 (glyph), I-9 | thêm hành động **Special** (I-10 mới) |
| `design/gdd/game-concept.md` | Pillar 1, Pillar 2, anti-pillar "build bắt buộc", ghi chú moveset theo họ vũ khí | nguồn của Player Fantasy |
| `design/gdd/systems-index.md` | #14 Weapon Moveset, #15 Weapon Tree, #16 Loadout, #17 Skill Stone, #9 Combat, #19 Boss | gộp #14 thành "Skill & Moveset" (Phase 5) |
| `design/art/art-bible.md` | §5 nhân vật / vũ khí; quyết định 2026-11-02 "đổi vũ khí là đổi sprite nhân vật" | Visual/Audio |
| `design/art/specs/sprite-sheet-spec.md` | look theo vũ khí; §6 "clip đánh chạy theo tiến độ đòn" phải sửa thành "phát theo thời gian từ node PlayAnimation" | Visual/Audio, Presentation |
| `design/registry/entities.yaml` | `jump_apex_height`, `horizontal_reach`, `dodge_cycle` | giá trị đọc vào F3, F5 |
| `docs/architecture/adr-0002-skill-runtime-data-contract.md` (Proposed) | định dạng XML, nhánh HoldGate, lõi `Core.Skills`, quy về bước (`SkillTime`), nạp, kiểm tra ba tầng | Rule 1–6, 12, 21–24, 32, 34 |
| `docs/architecture/adr-0003-skill-authoring-pipeline.md` (Proposed) | Skill Tool (GraphView, Editor-only) mở/lưu XML trực tiếp, XML là nguồn duy nhất, id = tên file 6 số, build chỉ kiểm tra | Rule 2, 3, 21, 31 |

## Acceptance Criteria

*Bản nháp của qa-lead (2026-11-05), chuyển sang mô hình Start → End với độ trễ giây. Quy ước:*
- *Vị trí test: lõi `Assets/Tests/EditMode/SkillMoveset/`; công cụ đồ thị và bộ xuất `Assets/Tests/EditMode/SkillMoveset/Editor/`;
  tích hợp `Assets/Tests/PlayMode/SkillMoveset/`; build: script batchmode trong `tools/ci/` (`[Integration/Build]`).*
- *Harness: đồng hồ Game inject (bước 1/60, chỉnh được scale và pause); bản ghi lần bấm giả `{seq, action, dir, move.x,
  t_press}` kèm `IsHeld`/`WasReleasedAfter`; lõi Controller + thế giới va chạm giả; fixture XML hằng; sink log `{id, lý do}`;
  Combat giả (registry `hit_profile_id`, ghi cửa sổ hit theo bước); Loadout / stone giả.*
- *Đơn vị game; dung sai vị trí ±0,01 u, vận tốc ±0,01 u/s; "ở mốc t" = bước đầu tiên có thời điểm ≥ t (Rule 5, 23).*
- *Fixture: **F-G** (đất): PlayAnimation 0; Hit 0,04 → 0,10; CancelWindow 0,13; End 0,20. **F-A** (không): Hit 0,03 →
  0,10; CW 0,13; End 0,18. **F-D** (lướt): Hit 0,02 → 0,12; CW 0,18; End 0,26. **T-SPEAR** (thứ tự khai báo, `link_window`
  0,30, `priority` 0): `g1` ground/any; `g2` after g1; `g3` after g2; `up` ground/up; `fwd` ground/forward after g1; `run`
  ground/moving; `back` ground/moving/back; `air1` air/any; `plunge` air/down (`dive` ở 0,05); `dash1` dash/any; mọi đòn
  đất dùng nhịp F-G. **HAMMER-C**: HoldGate ở 0,30 (`hold_max` 0,60); nhánh tap: Hit 0,35 → 0,45, End 0,80; nhánh
  charged: cùng mốc sau khi chờ. **THRUST** (đất): lunge A(1,0 u, 0,05 s) ở 0; Hit 0,05 → 0,15; lunge B(2,0 u, 0,10 s) ở
  0,05; CW 0,18; End 0,25. **S-WIND** (đặc biệt): Hit 0,10 → 0,20; Armor 0,10 → 0,20; CW 0,25; End 0,40; cooldown 10 s.*
- *Gate theo tiền lệ Character Controller: Playtest BLOCKING ở milestone Vertical Slice; ngưỡng % và ngân sách hiệu năng là
  đề xuất, game-designer / technical-director xác nhận.*

### H.1 Dữ liệu, dòng thời gian và pipeline

- **GIVEN** họ `fam.test` không có trong code, bảng 4 dòng bắt buộc và 4 XML fixture, **WHEN** bấm khớp từng dòng, **THEN** `AttackStarted.skill_id` đúng id của dòng, không sửa code. (AC-D01, R1) `[Unit/EditMode — BLOCKING]`
- **GIVEN** 0 file kỹ năng được nạp, **WHEN** liệt kê kỹ năng tích hợp, **THEN** có đúng 3 kỹ năng dự phòng với mốc của Rule 22 (F-G, F-A, F-D). (AC-D02, R1/R22) `[Unit/EditMode — BLOCKING]`
- **GIVEN** các asmdef runtime của hệ, **WHEN** quét tham chiếu, **THEN** không tham chiếu `UnityEditor` hay kiểu của Skill Tool (GraphView); fixture XML nạp được trong test engine-free. (AC-D03, R2) `[Unit/EditMode — BLOCKING]`
- **GIVEN** một player build test, **WHEN** đọc build report, **THEN** 0 asset đồ thị trong build, mọi XML kỹ năng đều có. (AC-D04, R2) `[Integration/Build — BLOCKING]`
- **GIVEN** đồ thị hợp lệ, **WHEN** xuất XML hai lần, rồi một lần với `CurrentCulture` vi-VN và một lần en-US, **THEN** bốn file giống từng byte; số thập phân dùng `.`; UTF-8 không BOM; xuống dòng LF. (AC-D05, R2) `[Unit/EditMode (Editor) — BLOCKING]`
- **GIVEN** XML đã xuất, **WHEN** mở đồ thị rồi lưu lại không sửa gì, **THEN** XML giống từng byte. (AC-D06, R2) `[Unit/EditMode (Editor) — BLOCKING]`
- **GIVEN** XML do Skill Tool lưu, **WHEN** sửa tay mốc một node Hit trong XML rồi build, **THEN** build lỗi (exit ≠ 0), báo `PayloadHashMismatch` kèm id và đường dẫn; trả XML về bản gốc, hoặc mở bằng Skill Tool và lưu lại, thì build qua. (AC-D07, R2/R31) `[Integration/Build — BLOCKING]`
- **GIVEN** đồ thị có một lỗi trong số: không có End trên một nhánh, hai Start, node treo không nối, Hit kết thúc sau End, CancelWindow trước khi cửa sổ hit đầu tiên đóng, không có PlayAnimation, **WHEN** lưu, **THEN** XML trên đĩa không đổi (SHA-256 như cũ), Editor báo lỗi kèm ID ràng buộc và node; build kế tiếp lỗi. (AC-D08, R5/R6/R21) `[Unit/EditMode (Editor) — BLOCKING]`
- **GIVEN** file `100000.xml`, `099999.xml`, `10000.xml`, `skl.spear.xml`, và hai file cùng tên ở hai thư mục, **WHEN** validate, **THEN** file đầu đạt; ba file kế bị từ chối `IdFormat`; hai file trùng bị từ chối `DuplicateId` kèm cả hai đường dẫn. (AC-D09, R3) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đổi tên a→b, chuỗi a→b→c, vòng a→b→a, `a` còn file thật, chuỗi 9 bước, dữ liệu lưu chứa `a`, **WHEN** nạp hoặc build, **THEN** dòng trỏ `a` dùng `b`; chuỗi 2 bước tới `c`; vòng và "id cũ còn file" làm build lỗi; chuỗi 9 bước coi là thiếu; dữ liệu lưu được ghi lại bằng id cuối. (AC-D10, R3/R32) `[Unit/EditMode — BLOCKING]`
- **GIVEN** fixture `schema_version` N−1 và bản tương đương N, **WHEN** nạp, **THEN** bước chuyển đổi chạy và hai dòng thời gian bằng nhau từng mốc. (AC-D11, R3) `[Unit/EditMode — BLOCKING]`
- **GIVEN** header lỗi (`special` với `cooldown` 0; `weapon_families` rỗng; `contexts` rỗng; `kind: other`), **WHEN** validate, **THEN** mỗi trường hợp bị từ chối với ID riêng; `normal` không khai `cooldown` nhận 0; kỹ năng chỉ `air` không khai `land_policy` nhận `cancel_on_land`, có `ground` nhận `continue`. (AC-D12, R4) `[Unit/EditMode — BLOCKING]`
- **GIVEN** F-G, **WHEN** trải phẳng dòng thời gian, **THEN** `windup` = bước 0–2, cửa sổ hit mở ở bước 3 (0,05 s ≥ 0,04) và đóng ở bước 6, `recovery.locked` bước 6–7, `recovery.open` từ bước 8 (0,133 ≥ 0,13), End ở bước 12. (AC-D13, R5/R23) `[Unit/EditMode — BLOCKING]`
- **GIVEN** F-G đang chạy, **WHEN** đồng hồ Game đứng 2 s (pause), rồi chạy ở scale 0,05, **THEN** không mốc nào tiến khi pause; cửa sổ hit (3 bước game) kéo dài 60 frame thật ở scale 0,05. (AC-D14, R5) `[Unit/EditMode — BLOCKING]`
- **GIVEN** F-G với hai node PlayAnimation (0 và 0,10), **WHEN** chạy, **THEN** Presentation nhận khóa thứ nhất ở bước 0 và khóa thứ hai ở bước 6; End ở 0,20 không phụ thuộc độ dài clip. (AC-D15, R5/R6) `[Unit/EditMode — BLOCKING]`
- **GIVEN** các file: không có Hit và không tag `utility`; có tag `utility`; Cue trỏ VFX không tồn tại; Armor trong `normal`; Armor trong `special`, **WHEN** validate, **THEN** lần lượt: từ chối / đạt / chỉ cảnh báo / từ chối / đạt. (AC-D16, R6/R8) `[Unit/EditMode — BLOCKING]`
- **GIVEN** mỗi fixture chứa đúng một thứ cấm (đổi `move_speed`/`jump_speed`/`gravity`/`attack_move_mult`; khóa chân; cửa sổ hủy loại trừ nhảy; đặt thẳng vận tốc; đổi hướng mặt giữa đòn; biểu thức / script; ghi cờ / dữ liệu lưu; gọi kỹ năng khác; phần tử XML lạ), **WHEN** validate, **THEN** mỗi fixture bị từ chối với đúng ID ràng buộc Rule 10 / 32. (AC-D17, R10/R32) `[Unit/EditMode — BLOCKING]`
- **GIVEN** bộ fixture mỗi file vi phạm một ràng buộc Rule 21, **WHEN** chạy bước kiểm tra build, **THEN** exit ≠ 0 và báo cáo liệt kê **mọi** lỗi; fixture đúng biên thì đạt. (AC-D18, R21) `[Integration/Build — BLOCKING]`
- **GIVEN** XML lọt tới dev build có (a) CancelWindow trước khi cửa sổ hit đầu tiên đóng, (b) `lunge` 4,0 u, (c) `launch` 7,0 u, (d) Hit kết thúc sau End, (e) cooldown đặc biệt 3 s, **WHEN** chạy, **THEN** lần lượt kẹp về: (a) lúc cửa sổ hit đầu đóng; (b) 3,23 u; (c) 6,29 u; (d) End; (e) 8 s — kỹ năng vẫn chạy, mỗi trường hợp 1 log `Clamped:<trường>`. (AC-D19, R22/R24) `[Unit/EditMode — BLOCKING]`
- **GIVEN** lỗi cấu trúc (id thiếu, `schema_version` mới hơn, `hit_profile_id` lạ, file không đọc được), **WHEN** bấm Đánh ở `ground` / `air` / `dash`, **THEN** ra kỹ năng dự phòng đúng ngữ cảnh; game không dừng; di chuyển và nhảy vẫn chạy cùng bước. (AC-D20, R22) `[Unit/EditMode — BLOCKING]`
- **GIVEN** một lỗi dữ liệu, **WHEN** dùng kỹ năng đó 50 lần trong phiên, **THEN** sink nhận đúng 1 dòng cho mỗi (id, lý do). (AC-D21, R22) `[Unit/EditMode — BLOCKING]`
- **GIVEN** kỹ năng `owner: creature` có LookAt(`toward=target.position`), Move lunge(`toward=target.position`), Hit(`origin=owner.position`, `only=target.id`), Spawn và Cue nối dây từ Start, **WHEN** đọc, lưu bằng Skill Tool rồi đọc lại, **THEN** owner và mọi dây giữ nguyên; `weapon_families` không bắt buộc. (AC-D23, R4a/R4b) `[Unit/EditMode — BLOCKING]`
- **GIVEN** kỹ năng `player` nối `target.position` vào một node, và kỹ năng `player` có LookAt nối `owner.position`, **WHEN** validate, **THEN** cái đầu bị từ chối `TargetInPlayerSkill`; cái sau đạt (LookAt dùng được cho người chơi). (AC-D24, R4b/R6) `[Unit/EditMode — BLOCKING]`
- **GIVEN** dây sai kiểu (`only=target.position`, `origin=target.id`), tên giá trị lạ, `toward` trên `launch`, cổng trên node không có cổng đó, LookAt không nối `toward`, **WHEN** validate, **THEN** lần lượt `BadInput`, `BadInput`, `BadInput`, `BadInput`, `UnknownAttribute`, `MissingAttribute`. (AC-D25, R4b) `[Unit/EditMode — BLOCKING]`
- **GIVEN** bảng tổ hợp có dòng trỏ tới kỹ năng `creature`, **WHEN** validate thư viện, **THEN** báo `RowCreatureSkill`. (AC-D26, R4a) `[Unit/EditMode — BLOCKING]`
- **GIVEN** kỹ năng quái: Search(`for=creature`, `gid=105`, `radius=8`, `center=owner.position`) → Found: LookAt(`toward=search.list`), Hit(`only=search.nearest`), Cue(`after=end`, `origin=search.nearest_position`); Not Found: Cue, End, **WHEN** đọc, lưu bằng Skill Tool rồi đọc lại, **THEN** tham số Search, mọi dây và cách nối On End giữ nguyên; nhánh Not Found không có Hit vẫn hợp lệ. (AC-D28, R4c/R5a) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `gid` 99 / 1050, `gid` với `for=player`, `radius` 0, `for` lạ, `center` là id hoặc chính kết quả Search, thiếu nhánh, hai điểm rẽ (Search lồng trong nhánh), **WHEN** validate, **THEN** lần lượt `GidFormat` ×3, `BadNumber`, `BadEnum`, `BadInput` ×2, `SearchBranches`, `OneSplit`. (AC-D29, R4c) `[Unit/EditMode — BLOCKING]`
- **GIVEN** kết quả Search được nối vào node trước Search hoặc trong nhánh Not Found, **WHEN** validate, **THEN** `SearchValueOutsideFound`. (AC-D30, R4c) `[Unit/EditMode — BLOCKING]`
- **GIVEN** LookAt nhận `search.list`, `search.nearest`, `search.nearest_position`; Hit `only` nhận `search.list` và `search.nearest_position`, **WHEN** validate, **THEN** LookAt đạt cả ba; Hit đạt với danh sách, từ chối vị trí (`BadInput`). (AC-D31, R4c/R6) `[Unit/EditMode — BLOCKING]`
- **GIVEN** CancelWindow nối từ On End của Hit (Hit 0,04 s dài 0,06 s) với độ trễ 0,03, **WHEN** xuất, **THEN** mốc 0,13 và `after="end"`; mốc nhỏ hơn mốc đóng của node trước → `AfterEndTooEarly`; `after="end"` ở node đầu đoạn → `AfterEndNoPrevious`. (AC-D32, R5a) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `SkillStartValues` chụp lúc bắt đầu (quái ở (1,2), người chơi ở (5,2)), **WHEN** người chơi di chuyển rồi node đọc `target.position`, **THEN** node nhận (5,2); ảnh chụp của kỹ năng `player` không có Target. (AC-D27, R4b) `[Unit/EditMode — BLOCKING]`
- **GIVEN** THRUST đang chạy ở 0,07 s, **WHEN** nạp lại dữ liệu với End 0,40, **THEN** lần chạy này kết thúc theo số cũ (End 0,25); lần bấm sau dùng số mới. (AC-D22, R22) `[Unit/EditMode — BLOCKING]`

### H.2 Bảng tổ hợp và chuỗi đòn

- **GIVEN** T-SPEAR, Rảnh, hướng mặt phải, **WHEN** bấm Đánh với các bản ghi: đứng yên trung tính / `move.x` +1 / thẳng lên / `move.x` −1 / Xuống trên đất / trên không / tiêu thụ trong lúc né / trong LandRecovery, **THEN** lần lượt ra `g1` / `run` / `up` / `back` / `g1` / `air1` / `dash1` / xét theo `ground`. (AC-C01, R11/R13) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang `recovery.locked` của `g3`, **WHEN** bản ghi mang `dir` up nhưng lúc tiêu thụ cần gạt đã về trung tính, **THEN** ra `up` (hướng lấy từ bản ghi). (AC-C02, R11) `[Unit/EditMode — BLOCKING]`
- **GIVEN** hướng mặt phải, **WHEN** ra `back`, và trong lần chạy khác ra `g1` bằng bản ghi hướng trái qua bảng không có dòng `back`, **THEN** `back` giữ hướng mặt phải (0 `FacingChanged`); `g1` quay mặt theo bản ghi. (AC-C03, R13) `[Unit/EditMode — BLOCKING]`
- **GIVEN** T-SPEAR, **WHEN** bấm Đánh ở bước đầu `recovery.open` mỗi đòn, **THEN** ra `g1` → `g2` → `g3`, mỗi đòn bắt đầu ở bước 8 của đòn trước; sau `g3` kết thúc, `chain_ref` rỗng ngay. (AC-C04, R11/R14) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `recovery.open` của `g1`, **WHEN** giữ Lên và bấm Đánh, **THEN** ra `g2`, không ra `up`; đổi `up` sang `priority` 1 thì ra `up`. (AC-C05, R13) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `recovery.open` của `g1`, **WHEN** giữ Tiến và bấm Đánh, **THEN** ra `fwd` (cùng có `after`, cụ thể hơn `g2`). (AC-C06, R13/R28) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `g1` kết thúc ở bước T, **WHEN** lần bấm được tiêu thụ ở T+18, và trong lần chạy khác ở T+19, **THEN** T+18 ra `g2` (so ≤); T+19 ra `g1`, `chain_ref` rỗng. (AC-C07, R13/R14) `[Unit/EditMode — BLOCKING]`
- **GIVEN** dòng `fwd` có `link_window` 0,45 s, dòng `g2` 0,30 s, **WHEN** bấm ở T+25, **THEN** giữ Tiến ra `fwd`, trung tính ra `g1`; `chain_ref` còn tới T+27, rỗng từ T+28. (AC-C08, R14) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `chain_ref` = `g1`, **WHEN** lần lượt Hitstun, Teleport, Respawn, Defeated (mỗi trường hợp một lần chạy), **THEN** `chain_ref` rỗng ngay ở bước xảy ra. (AC-C09, R14) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `g1` bị hủy bằng né; `air1` bị hủy vì chạm đất; HAMMER-C bị hủy bằng nhảy khi đang `charging`, **WHEN** bấm Đánh 0,25 s sau lúc hủy, **THEN** cả ba còn giữ `chain_ref`, đếm từ lúc hủy; lần bấm kế mang ngữ cảnh mới. (AC-C10, R14/R30) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `g2` có `cooldown` 0,5 s đang hồi, hoặc `g2` bị tắt lúc nạp, **WHEN** bấm ở `recovery.open` của `g1`, **THEN** ra `g1` (dòng khớp kế tiếp), không ra dự phòng; khi cả `g1` và `g2` đều tắt thì ra dự phòng `ground`, dự phòng kết thúc thì xóa `chain_ref`. (AC-C11, R13/R22) `[Unit/EditMode — BLOCKING]`
- **GIVEN** bảng chỉ có các dòng bắt buộc, **WHEN** duyệt **toàn bộ** tổ hợp {context × moving × dir × `chain_ref` ∈ {rỗng, g1, id lạ} × trong/ngoài `link_window`} trên hai instance, **THEN** mọi lần trả một kỹ năng khác null và hai instance cho kết quả giống nhau (liệt kê hết, không dùng seed ngẫu nhiên). (AC-C12, R13, binding test 2) `[Unit/EditMode — BLOCKING]`
- **GIVEN** bản ghi Plunge, **WHEN** tiêu thụ trên không, và lần khác sau khi đã chạm đất trong 0,15 s, **THEN** trên không ra `plunge` (`dir` ép down); trên đất ra `g1`; tiêu thụ ở 0,15 s + 1 bước thì đã hết hạn. (AC-C13, R13) `[Unit/EditMode — BLOCKING]`
- **GIVEN** hai dòng hòa khóa và tồn tại một bản ghi khớp cả hai; và hai dòng trùng hoàn toàn, **WHEN** validate rồi chọn, **THEN** cặp đầu cho 1 cảnh báo, hàm chọn lấy dòng khai báo trước; cặp sau bị từ chối `DuplicateRow`. (AC-C14, R13/R21) `[Unit/EditMode — BLOCKING]`
- **GIVEN** dòng A `after: [g1]`, dòng B `after: [tag:light]`, `g1` mang tag `light`, **WHEN** bấm sau `g1`, **THEN** ra A. (AC-C15, R28) `[Unit/EditMode — BLOCKING]`
- **GIVEN** bảng thiếu dòng `air` + `any`; bảng thiếu dòng `air` + `down` có `dive`; bảng có dòng `dash` với `moving: still`; dòng `ground` `still` + `forward`, **WHEN** validate, **THEN** ba bảng đầu bị từ chối với ID riêng; dòng cuối cho cảnh báo. (AC-C16, R15/R27) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `recovery.open` của `g3`, **WHEN** bấm Đánh, **THEN** cùng bước đó `g3` bị hủy và `g1` bắt đầu. (AC-C17, States) `[Unit/EditMode — BLOCKING]`
- **GIVEN** bảng có `g1` after `g1` (vòng 0,13 s) và bảng `g1 → g2 → g1` (0,26 s), **WHEN** validate, **THEN** bảng đầu bị từ chối `MinLoopCommit`; bảng sau đạt; bấm liên tục `g1` (đòn mở đầu tự hủy) không bị coi là vòng. (AC-C18, F6/R29) `[Unit/EditMode — BLOCKING]`

### H.3 Giữ để tụ lực

- **GIVEN** HAMMER-C, **WHEN** nhả nút ở 0,08 s, **THEN** nhánh `tap`, cửa sổ hit mở ở mốc 0,35 như bản không có HoldGate (không trễ). (AC-H01, R12) `[Unit/EditMode — BLOCKING]`
- **GIVEN** HAMMER-C, **WHEN** giữ 0,70 s rồi nhả, **THEN** vào `charging` ở 0,30; `charge` = 0,40; cửa sổ hit nhánh `charged` mở ở 0,35 + 0,40 = 0,75. (AC-H02, R12/F8) `[Unit/EditMode — BLOCKING]`
- **GIVEN** HAMMER-C, **WHEN** giữ liên tục 1,5 s, **THEN** `charging` thoát ở 0,90, `charge` = 0,60; nhả muộn hơn không có tác dụng. (AC-H03, F8) `[Unit/EditMode — BLOCKING]`
- **GIVEN** HAMMER-C, **WHEN** nhả đúng bước luồng tới HoldGate (Input xử lý trước, `IsHeld` = false), **THEN** nhánh `tap`. (AC-H04) `[Unit/EditMode — BLOCKING]`
- **GIVEN** HAMMER-C, **WHEN** nhả ở 0,07, bấm lại và giữ từ 0,13, **THEN** nhánh `tap` ở 0,30 (`WasReleasedAfter(seq_mở)` = true); lần bấm thứ hai nằm lại bộ đệm làm đòn nối. (AC-H05) `[Unit/EditMode — BLOCKING]`
- **GIVEN** kỹ năng `special` có HoldGate, **WHEN** nhả Special nhưng vẫn giữ Đánh qua HoldGate, **THEN** nhánh `tap` (nút xét là Special). (AC-H06) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang `charging` không có giáp, **WHEN** trúng đòn, rồi hết choáng mà vẫn giữ nút, **THEN** Hitstun hủy kỹ năng, `chain_ref` rỗng; sau Hitstun không kỹ năng nào bắt đầu. (AC-H07) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đòn trên không có HoldGate và `cancel_on_land`, **WHEN** đang `charging` thì chạm đất, **THEN** `vy` giảm theo trọng lực mỗi bước khi tụ lực; kỹ năng bị hủy ở bước chạm đất; validator cho 1 cảnh báo cho file này. (AC-H08) `[Unit/EditMode — BLOCKING]`
- **GIVEN** HoldGate ở 0,20 khi `hold_threshold` 0,25; HoldGate thiếu `hold_max`; `hold_max` 0,15; `hold_max` 1,05; HoldGate sau cửa sổ hit đầu tiên; HoldGate ở 0,25, **WHEN** validate, **THEN** năm file đầu bị từ chối với ID riêng; file cuối đạt. (AC-H09, R12/F8) `[Unit/EditMode — BLOCKING]`

### H.4 Node chuyển động

- **GIVEN** `lunge(2,0 u, 0,10 s, opposite)`, hướng mặt phải, **WHEN** chạy, **THEN** lực đẩy sang trái 2,00 u; hướng mặt và vùng chạm vẫn bên phải; 0 `FacingChanged`. (AC-M01, R7) `[Unit/EditMode — BLOCKING]`
- **GIVEN** THRUST trong node B, **WHEN** lần lượt nhảy, hủy bằng né, Hitstun, root, kỹ năng kết thúc, đụng tường, hết node (mỗi trường hợp một lần chạy), **THEN** lực kỹ năng = 0 ngay bước đó; `vx` chỉ còn phần điều khiển; khi nhảy, cửa sổ hit vẫn mở tới 0,15 và End vẫn ở 0,25. (AC-M02, R7/R9) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đứng cách mép 0,5 u (đất cứng và platform một chiều), **WHEN** `lunge(2,0, 0,10)` qua mép, **THEN** lực đi đủ node; `vy` giảm theo trọng lực từ bước rời đất; có `LeftGround`, coyote bắt đầu đếm; 0 DropThrough; quãng này không trừ `air_lunge_budget`. (AC-M03, R7/R25) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đã sát tường (hoặc trước dốc dựng hơn `max_walk_angle`) khi node A bắt đầu, **WHEN** chạy THRUST, **THEN** A bị bỏ ngay và không thử lại; node B vẫn chạy; cửa sổ hit vẫn mở 0,05 → 0,15. (AC-M04) `[Unit/EditMode — BLOCKING]`
- **GIVEN** tường cách 1,0 u, **WHEN** `lunge(2,0, 0,10)`, **THEN** dừng ở tường, không xuyên (mép collider ≤ mặt tường + `skin`). (AC-M05, R7) `[Unit/EditMode — BLOCKING]`
- **GIVEN** hit-stop scale 0,05 trong 0,21 s thật bắt đầu giữa `lunge(2,0, 0,10)`, **WHEN** chạy hết node, **THEN** tổng quãng do lực = 2,00 ±0,01 u; thời gian thật dài thêm đúng phần hit-stop. (AC-M06) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `lunge(2,0, 0,10)` trên đất, giữ cùng chiều, rời mép sau 3 bước, **WHEN** đo, **THEN** phần điều khiển dùng 0,6 trong 3 bước đầu và 1,0 sau đó: Δx = 2,0 + 15 × (0,6 × 3 + 1,0 × 3)/60 = 3,20 ±0,01 u. (AC-M07) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `launch(3,0)` mà trần cách 1,0 u, **WHEN** chạy, **THEN** có vận tốc lên; chạm trần thì `Bonked` và `vy` = min(`vy`, 0). (AC-M08) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `launch` làm nhân vật rời đất, **WHEN** bấm Jump trong `t_coy`, và nhả Jump lúc đang lên, **THEN** ra `Jumped(Air)` (không coyote); `jump_rising` bật (snap tắt); không treo ở đỉnh; nhả Jump không nhân 0,5 vào vận tốc launch. (AC-M09) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang trên không, **WHEN** kỹ năng có `launch(3,0)` chạy, **THEN** `vy` không đổi ở mốc đó; phần còn lại chạy bình thường; dev build có 1 log. (AC-M10, R7) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `halt()` sau một `lunge`, người chơi giữ hướng, **WHEN** chạy, **THEN** từ mốc đó lực kỹ năng = 0, phần điều khiển vẫn = `input_x` × 15 × 0,6. (AC-M11, R7) `[Unit/EditMode — BLOCKING]`
- **GIVEN** node lướt bắt đầu sau khi cửa sổ hit cuối đóng (đòn thường), và kỹ năng có `dash` trong `contexts` chứa `lunge`, **WHEN** validate, **THEN** cả hai bị từ chối. (AC-M12, R7) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang bị làm chậm `speed_mult` 0,5, **WHEN** `lunge(2,0, 0,10)`, **THEN** quãng do lực = 1,00 u. (AC-M13, R7) `[Unit/EditMode — BLOCKING]`
- **GIVEN** S-WIND có giáp và `dash1` đang chạy, **WHEN** đọc cờ i-frame mỗi bước, **THEN** cờ = false ở mọi bước của mọi kỹ năng; `dash1` tắt i-frame ngay khi bắt đầu. (AC-M14, R8) `[Unit/EditMode — BLOCKING]`

### H.5 Kỹ năng đặc biệt

- **GIVEN** stone trỏ kỹ năng `special` họ `spear`; stone `any`; stone họ `hammer` khi cầm thương; stone trỏ kỹ năng `normal`, **WHEN** kích hoạt hoặc validate, **THEN** hai stone đầu ra kỹ năng; stone `hammer` không kích hoạt, phát `StoneIncompatible`; stone trỏ `normal` bị từ chối. (AC-S01, R16) `[Unit/EditMode — BLOCKING]`
- **GIVEN** S-WIND, **WHEN** bị Hitstun ở 0,05 (trước cửa sổ hit), và lần khác ở 0,15, **THEN** lần đầu hồi chiêu còn 0; lần sau hồi chiêu đã bắt đầu từ 0,10. (AC-S02, R17) `[Unit/EditMode — BLOCKING]`
- **GIVEN** chưa gắn stone, **WHEN** bấm Special, **THEN** 0 kỹ năng bắt đầu, HUD nhận `Empty`. (AC-S03, R18) `[Unit/EditMode — BLOCKING]`
- **GIVEN** hồi chiêu còn 3,0 / 0,15 / 0,20 s, **WHEN** bấm Special, **THEN** 3,0 → từ chối `NotReady`, bản ghi hết hạn sau 0,20; 0,15 → nằm lại bộ đệm, ra khi hồi chiêu về 0; 0,20 → ra sau 0,20 s. (AC-S04, R18) `[Unit/EditMode — BLOCKING]`
- **GIVEN** stone không có biến thể trên không, đang trên không, **WHEN** bấm Special 0,05 s trước khi chạm đất, và lần khác 0,25 s trước, **THEN** lần đầu ra biến thể trên đất ở bước chạm đất; lần sau hết hạn ở 0,20 s, phát `NoVariant`, hồi chiêu vẫn 0. (AC-S05, R18) `[Unit/EditMode — BLOCKING]`
- **GIVEN** kỹ năng đặc biệt thiếu file, `hit_profile_id` lạ, hoặc `schema_version` mới hơn, **WHEN** bấm Special, **THEN** không làm gì, HUD nhận `Error`, 0 kỹ năng dự phòng, 0 đòn thường. (AC-S06, R18/R22) `[Unit/EditMode — BLOCKING]`
- **GIVEN** S-WIND đang chạy, **WHEN** bấm Special, **THEN** từ chối, không nằm lại bộ đệm. (AC-S07, R19) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang né, **WHEN** bấm Special giữa cú né, **THEN** Special ra ở bước đầu tiên sau `DodgeEnded`. (AC-S08, R19) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `g1`, **WHEN** bấm Special ở 0, **THEN** Special không hủy trước `recovery.open`; ra ở bước 8. Bấm Special khi HAMMER-C đang `charging` thì hủy ngay. (AC-S09, R9b) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Special ngắn hủy `g1` ở `recovery.open`, **WHEN** bấm Đánh ở `recovery.open` của Special, **THEN** ra `g2`; sau khi Special kết thúc `chain_ref` = `g1`. (AC-S10, R14) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Input giả ghi lại đăng ký, **WHEN** khởi tạo, **THEN** Special có cửa sổ 0,20 s đồng hồ Game, chung hàng đợi và `seq` với Attack; trong LandRecovery Special được chấp nhận từ 0,08 s. (AC-S11, R20) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Combat giả, S-WIND giáp 0,10 → 0,20, **WHEN** đòn địch tới ở bước chứa 0,20 và ở bước kế, **THEN** bước đầu không có `HitReceived`, kỹ năng tiếp tục; bước sau Hitstun, kỹ năng bị hủy, hồi chiêu vẫn tính. (AC-S12, R8) `[Unit/EditMode — BLOCKING]`
- **GIVEN** S-WIND ở 0,15 và một Special có `dive`, **WHEN** có yêu cầu khóa điều khiển, **THEN** S-WIND bị hủy cùng bước, hồi chiêu giữ; kỹ năng `dive` chạy tới khi chạm đất. (AC-S13) `[Unit/EditMode — BLOCKING]`
- **GIVEN** asmdef của hệ, **WHEN** quét, **THEN** không tham chiếu Input System hay module Input (hệ không tự đọc input). (AC-S14, R20) `[Unit/EditMode — BLOCKING]`

### H.6 Công thức và validator

- **GIVEN** F-G và HAMMER-C (giữ đủ), **WHEN** tính `skill_total`, **THEN** F-G = 0,20 s; HAMMER-C = 0,80 + 0,60 = 1,40 s. (AC-F01, F1) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đòn `normal` End 0,95 + `hold_max` 0,60 = 1,55 s, và bản End 0,90 (1,50 s), **WHEN** validate, **THEN** 1,55 bị từ chối `SkillTotalMax`; 1,50 đạt. (AC-F02, F1) `[Unit/EditMode — BLOCKING]`
- **GIVEN** API `commit_time` cho Boss Encounter, **WHEN** tính, **THEN** F-G = 0,13; F-D = 0,18; HAMMER-C (nhánh tap, không CW) = 0,45 + 0,03 = 0,48; `plunge` = 0,05; `commit_time_max(spear)` = lớn nhất trong các kỹ năng `normal` của bảng. (AC-F03, F2) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `r_node` 0,5, `r_total` 0,75 / 1,0, **WHEN** validate, **THEN** `lunge(3,23, 0,20)` đạt, `(3,25, 0,20)` từ chối `LungeNode`; hai node 2,4 + 2,4 trong `normal` đạt, 2,5 + 2,5 từ chối `LungeTotal`; 3,23 + 3,23 trong `special` đạt. (AC-F04, F3) `[Unit/EditMode — BLOCKING]`
- **GIVEN** trần tốc độ, **WHEN** validate, **THEN** trên đất `lunge(2,33, 0,10)` đạt (23,3 + 9 ≤ 32,31), `(2,34, 0,10)` và `(2,5, 0,10)` từ chối; trên không `(2,0, 0,10)` từ chối (35), `(2,0, 0,15)` đạt; kỹ năng {ground, air} dùng trần trên không. (AC-F05, F3/R26) `[Unit/EditMode — BLOCKING]`
- **GIVEN** dev build có `lunge(2,5, 0,10)` trên đất, **WHEN** chạy, **THEN** `duration` kéo dài tới 7 bước (0,1167 s), quãng giữ 2,5 u, 1 log. (AC-F06, F3/R24) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `lunge(2,0, 0,10)` trên đất, **WHEN** chạy không giữ / giữ cùng chiều / ngược chiều ở dáng chạy, **THEN** Δx = 2,00 / 2,90 / 1,10 u; dáng đi bộ cùng chiều 2,24, ngược 1,76. (AC-F07, F4) `[Unit/EditMode — BLOCKING]`
- **GIVEN** trên đất, `launch(3,0)`, **WHEN** chạy, **THEN** `vy` = 20,84 ±0,01 u/s; đỉnh cao hơn điểm bật 3,00 ±0,01 u; `AirJumpsLeft` không đổi. (AC-F08, F5) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `launch(7,0)` với `r_launch` 1,0 và `launch(3,2)` với `r_launch` 0,5, **WHEN** validate, **THEN** cả hai bị từ chối; `launch(7,0)` lọt tới runtime thì kẹp về 6,29 u (`vy` 30,18 ±0,02), 1 log. (AC-F09, F5) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đòn `normal` có cửa sổ hit cuối đóng ở 0,10, **WHEN** CancelWindow ở 0,25 và ở 0,26, **THEN** 0,25 đạt (= 0,10 + 0,15); 0,26 bị từ chối `DodgeCancelLatest`. (AC-F10, F6/R9) `[Unit/EditMode — BLOCKING]`
- **GIVEN** cooldown 10 s, cửa sổ hit đầu mở ở 42,0 s, **WHEN** đo, **THEN** ở 47,5 s `remaining` = 4,50 (HUD 0,45); hit-stop 0,05 trong 0,21 s thật làm `remaining` giảm 0,0105 ±1e-4; pause 2 s không đổi; không bao giờ < 0. (AC-F11, F7) `[Unit/EditMode — BLOCKING]`
- **GIVEN** dữ liệu lưu `remaining` 4,5, và bản lưu hỏng `remaining` 12 (cooldown 10), **WHEN** nạp ở 100,0 s, **THEN** bản thường `t_active_start` = 94,5; bản hỏng kẹp về 10. (AC-F12, F7) `[Unit/EditMode — BLOCKING]`
- **GIVEN** cooldown `special` 7,9 / 8,0 / 30,0 / 30,1 và `normal` −0,1 / 2,0 / 2,1, **WHEN** validate, **THEN** chỉ 8,0, 30,0 và 2,0 đạt. (AC-F13, F7) `[Unit/EditMode — BLOCKING]`
- **GIVEN** HAMMER-C, **WHEN** `t_release` = 0,20 / 0,70 / 1,50, **THEN** `charge` = 0 (tap) / 0,40 / 0,60. (AC-F14, F8) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Special có CancelWindow muộn hơn cửa sổ đệm Attack cho phép (lockout 0,25 > 0,20), **WHEN** validate chéo, **THEN** từ chối `MaxLockoutVsBuffer` (Controller validator 8). (AC-V01) `[Unit/EditMode — BLOCKING]`
- **GIVEN** knob chung ngoài khoảng (`hold_threshold` 0,5; `r_node` 0,8; `special_cooldown_min` 3) và cặp knob mâu thuẫn (`r_node` 0,7 > `r_total` 0,6), **WHEN** nạp, **THEN** knob ngoài khoảng về mặc định kèm 1 log, knob khác giữ nguyên; cặp mâu thuẫn cho 1 cảnh báo. (AC-V02, Tuning/R33) `[Unit/EditMode — BLOCKING]`

### H.7 Edge Cases

- **GIVEN** THRUST, giữ phải, **WHEN** nhảy sau 1 bước, **THEN** `Jumped` cùng bước; lực kỹ năng = 0, node B bị bỏ; cửa sổ hit vẫn mở 0,05 → 0,15; hệ số ×1,0; End vẫn 0,25; Δx trong lúc đòn chạy chỉ gồm phần điều khiển. (AC-E01, R9) `[Unit/EditMode — BLOCKING]`
- **GIVEN** HAMMER-C đang `charging`, **WHEN** bấm Jump, và lần khác Dodge, **THEN** kỹ năng bị hủy cùng bước, `Jumped`/`DodgeStarted` cùng bước; không có cửa sổ hit nào; bản Special bị hủy khi `charging` không tốn hồi chiêu. (AC-E02, R9a) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `plunge`, giữ phải, **WHEN** chạy; rồi ba lần khác bấm né trên không ở 0,02, 0,05 và 0,15, **THEN** khi `dive`: `vx` = 0 mỗi bước, `vy` = −`plunge_speed`; cả ba lần né hủy kỹ năng cùng bước. (AC-E03, R7/R9d) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `plunge` chạm đất cứng / platform một chiều / platform đang trong `drop_ignore_time`, **WHEN** chạm, **THEN** kỹ năng kết thúc ở bước chạm đất, `PlungeLanded` và `Landed` mỗi thứ 1 lần, LandRecovery 0,16 s, hồi lượt nhảy đôi, né trên không và `air_lunge_budget`; platform đang `drop_ignore_time` thì đi xuyên. (AC-E04) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `air_lunge_budget` 3,23 u, đòn trên không có `lunge(2,0, 0,15)` nối với chính nó, **WHEN** chạy hai đòn trong một lần bay, **THEN** tổng quãng do lực = 3,23 ±0,01 u (đòn hai được 1,23 u, giữ `duration`); sau nhảy đôi hoặc né trên không ngân sách không hồi; sau `Landed`, Teleport, Respawn về 3,23. (AC-E05, F3/R25) `[Unit/EditMode — BLOCKING]`
- **GIVEN** cùng một id, bản (a) CancelWindow trước khi cửa sổ hit đầu đóng và bản (b) `hit_profile_id` lạ, **WHEN** bấm Đánh ở `ground`, **THEN** (a) chạy chính kỹ năng với CancelWindow bị kẹp, log `Clamped`; (b) chạy dự phòng `ground`, log `Fallback:UnknownHitProfile`. (AC-E06, R22) `[Unit/EditMode — BLOCKING]`
- **GIVEN** XML có `payload_hash` lệch nhưng nội dung hợp lệ, **WHEN** nạp trong Editor / dev build, và khi build, **THEN** Editor/dev nạp được kèm 1 cảnh báo `PayloadHashMismatch`; build lỗi. (AC-E07) `[Unit/EditMode + Integration/Build — BLOCKING]`
- **GIVEN** file kỹ năng và file bảng có `schema_version` N+1, **WHEN** nạp, **THEN** kỹ năng bị từ chối `SchemaTooNew` (đòn thường: dự phòng; Special: không làm gì); cả họ vũ khí của file bảng dùng 3 dự phòng, bấm 5 lần trên đất ra 5 lần dự phòng `ground`, `chain_ref` rỗng sau mỗi đòn. (AC-E08) `[Unit/EditMode — BLOCKING]`
- **GIVEN** kỹ năng thiếu khóa `anim`, **WHEN** chạy, **THEN** dòng thời gian (mốc, hit, lực) giống từng bước với bản có `anim`; Presentation nhận clip chung theo ngữ cảnh; 1 log; build: thiếu ở look mặc định là lỗi, look khác là cảnh báo. (AC-E09) `[Unit/EditMode + Integration/Build — BLOCKING]`
- **GIVEN** Attack (`seq` nhỏ) và Special cùng frame, rồi thứ tự ngược với S-WIND, **WHEN** chạy, **THEN** thứ tự thường: `g1`, Special ra ở bước 8; thứ tự ngược: S-WIND ra, Attack chỉ được phép từ 0,25 s nên hết hạn, log `CancelLockout`. (AC-E10) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Attack (`seq` 5) và Special (`seq` 6) cùng trong bộ đệm khi `g1` đang `recovery.locked`, **WHEN** tới bước 8, **THEN** Attack được tiêu thụ trước, ra `g2`. (AC-E11) `[Unit/EditMode — BLOCKING]`
- **GIVEN** cửa sổ hit của `air1` đang mở đúng frame chạm đất và có Attack trong bộ đệm, **WHEN** chạy, **THEN** cửa sổ đóng ở bước va chạm; Combat giả ghi 0 hit ở frame đó; không có LandRecovery; bước kế ra `g1`. (AC-E12) `[Unit/EditMode — BLOCKING]`
- **GIVEN** kỹ năng `ground` đi khỏi mép rồi đáp lại, và `dash1` đáp đất, **WHEN** chạy, **THEN** kỹ năng `ground` không bị hủy; `dash1` tiếp quán tính F8 với ×0,6. (AC-E13) `[Unit/EditMode — BLOCKING]`
- **GIVEN** hai node PlayAnimation trong một kỹ năng và End sớm hơn clip của look mặc định 0,15 s, **WHEN** validate, **THEN** 1 cảnh báo "clip bị cắt"; khi chạy, clip thứ hai thay clip thứ nhất ở mốc của nó. (AC-E14) `[Unit/EditMode — BLOCKING]`

### H.8 Liên hệ với hệ khác

- **GIVEN** Input thật (`InputTestFixture`), Controller và XML thật của thương, **WHEN** bấm Đánh 3 lần nhịp đều, **THEN** `AttackStarted.skill_id` lần lượt `g1`, `g2`, `g3`, không còn payload `(context, index)` cũ. (AC-X01) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** Input thật, **WHEN** giữ phím Đánh 0,70 s với HAMMER-C, **THEN** HoldGate cho kết quả như AC-H02. (AC-X02) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** Combat giả, **WHEN** THRUST trúng mục tiêu, Combat gây hit-stop 0,21 s, và một kỹ năng có Spawn bị hủy sau khi spawn, **THEN** Combat nhận cửa sổ hit kèm `hit_profile_id` từ 0,05 tới 0,15; dòng thời gian chậm lại; projectile còn sau khi kỹ năng bị hủy. (AC-X03) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** Presentation, **WHEN** chạy F-G, **THEN** nhận khóa `anim` ở bước 0 và clip phát theo thời gian; khóa thiếu thì clip chung chạy. (AC-X04) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** API cho Boss Encounter, **WHEN** đọc, **THEN** có `commit_time` của mọi kỹ năng `normal` và `commit_time_max` theo họ, khớp AC-F03. (AC-X05) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** không có Loadout, rồi gắn một stone, **WHEN** khởi tạo, **THEN** không Loadout dùng bảng của vũ khí khởi đầu, không stone; gắn stone thì Special lấy đúng `special_skill_id`. (AC-X06) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** HUD giả, **WHEN** đi qua các trạng thái, **THEN** HUD nhận 5 giá trị phân biệt (`Ready`, `Empty`, `NotReady`, `NoVariant`, `Error`) và `remaining / cooldown` mỗi frame khi đang hồi. (AC-X07) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** kênh Telemetry, **WHEN** dùng kỹ năng và gặp lỗi dữ liệu, **THEN** mỗi lần dùng 1 sự kiện kèm `skill_id`; lỗi dữ liệu 1 sự kiện mỗi (id, lý do) trong phiên. (AC-X08) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** lưu game khi `remaining` = 4,5, **WHEN** thoát rồi nạp lại, **THEN** `remaining` = 4,5 ±1 bước; id cũ trong dữ liệu lưu được ghi lại bằng id mới. (AC-X09) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** adapter Physics2D thật, **WHEN** `lunge(2,0, 0,10)` trên đất bằng và lao vào tường, **THEN** vị trí body khớp dự đoán của lõi ±`skin`; không xuyên tường. (AC-X10) `[Integration/PlayMode — BLOCKING]`

### H.9 Hiệu năng

- **GIVEN** lõi đã warm-up, **WHEN** StartSkill rồi chạy 600 bước (lunge, launch, halt, dive, 3 cửa sổ hit, cue, HoldGate tới `charging`, hủy bằng nhảy, hủy bằng né, chọn kỹ năng ở mỗi lần bấm, phát sự kiện), **THEN** cấp phát tăng **0 byte** (`GC.GetAllocatedBytesForCurrentThread()` ngoài Unity; `Is.Not.AllocatingGCMemory()` trong UTF). (AC-P01) `[Performance — BLOCKING]`
- **GIVEN** T-SPEAR, **WHEN** gọi hàm chọn 10 000 lần qua mọi tổ hợp của AC-C12, **THEN** 0 byte cấp phát. (AC-P02) `[Performance — BLOCKING]`
- **GIVEN** bảng 32 dòng, PC mục tiêu, IL2CPP release, **WHEN** đo 10 000 lần gọi hàm chọn, **THEN** trung vị ≤ 1 µs, p99 ≤ 5 µs (đề xuất). (AC-P03) `[Performance — ADVISORY]`
- **GIVEN** bộ dữ liệu đầy đủ ước tính (~300 XML + 14 bảng, ~1,2 MB), SSD, IL2CPP release, **WHEN** nạp nguội (parse, chuyển đổi, kiểm tra, dựng dòng thời gian), **THEN** tổng ≤ 150 ms (đề xuất; ADVISORY tới khi ADR chốt lúc nạp). (AC-P04) `[Performance — ADVISORY]`
- **GIVEN** đã nạp xong, **WHEN** chạy 600 frame combat liên tục, **THEN** bộ đếm parse XML = 0; StartSkill không bao giờ đọc file. (AC-P05) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** bộ dữ liệu đầy đủ, **WHEN** chạy validator lúc build và xuất khi lưu một đồ thị, **THEN** validator ≤ 30 s; xuất ≤ 200 ms mỗi đồ thị (đề xuất). (AC-P06) `[Performance — ADVISORY]`

### H.10 Playtest, cảm giác, hình ảnh và UI *(gate ở milestone Vertical Slice)*

- **GIVEN** n ≥ 5 tester, mỗi người ≥ 10 phút với ≥ 2 họ vũ khí, **WHEN** hỏi mở về cùng ô Lên + Đánh, **THEN** 0 tester tả là "giống nhau, chỉ nhanh hay chậm hơn" (2 người mã hóa độc lập). (AC-T01, binding test 1) `[Playtest — BLOCKING]`
- **GIVEN** log theo frame của mọi buổi, **WHEN** nhóm theo (họ, ô tổ hợp, `context`, `chain_ref`), **THEN** mỗi nhóm chỉ ra 1 `skill_id`. (AC-T02, binding test 2) `[Playtest — BLOCKING]`
- **GIVEN** action map và schema bảng tổ hợp, **WHEN** kiểm tra, **THEN** chỉ Attack, Plunge và Special kích hoạt đòn; schema không có trường chuỗi lệnh; validator từ chối trường lạ `sequence`. (AC-T03, binding test 3) `[Unit/EditMode — BLOCKING]`
- **GIVEN** tester cầm vũ khí chưa dùng, 5 phút tự do, **WHEN** đọc telemetry, **THEN** ≥ 80% tester dùng ít nhất 1 `skill_id` ngoài chuỗi `g*` (đề xuất). (AC-T04, binding test 4) `[Playtest — BLOCKING]`
- **GIVEN** log theo frame, **WHEN** đếm, **THEN** 0 frame có kỹ năng chạy mà (`vx` − lực kỹ năng) lệch khỏi `input_x` × tốc dáng × hệ số × `speed_mult` quá 1e-3 (trừ khi bị trói hoặc chạm tường); 0 lần Jump / Dodge hợp lệ trong cửa sổ đã khai báo không ra ở frame được phép. (AC-T05, binding test 5) `[Playtest — BLOCKING]`
- **GIVEN** mỗi họ ≥ 2 stone, n ≥ 8 tester mỗi họ, **WHEN** hỏi stone nào hay dùng nhất, **THEN** không stone nào được > 50% chọn. (AC-T06, binding test 6) `[Playtest — ADVISORY, BLOCKING khi đủ stone]`
- **GIVEN** phỏng vấn sau buổi chơi, **WHEN** hỏi "có đòn nào làm bạn bất ngờ không", **THEN** ≥ 80% tester kể được ít nhất 1 đòn cụ thể (đề xuất). (AC-T07, câu đo playtest) `[Playtest — BLOCKING]`
- **GIVEN** cùng kỹ năng, cùng input, 100 lần lặp, **WHEN** đo Δx và mốc cửa sổ hit, **THEN** giống từng bước. (AC-T08) `[Unit/EditMode — BLOCKING]`
- **GIVEN** cảnh có lưới 0,5 u, **WHEN** quay THRUST (không giữ / cùng chiều / ngược chiều) và `launch(3,0)`, **THEN** ảnh onion-skin lưu trong `production/qa/evidence/`; quãng lệch ≤ 0,05 u so với AC-F07, AC-F08; lead ký duyệt. (AC-VI01) `[Visual — BLOCKING]`
- **GIVEN** cùng đòn ở 2 look vũ khí, **WHEN** quay frame tại mốc Hit, **THEN** screenshot lưu lại cho thấy frame chạm của cả hai look trùng mốc (lệch ≤ 1 frame). (AC-VI02) `[Visual — BLOCKING]`
- **GIVEN** S-WIND có giáp và một cú né, **WHEN** quay ở grayscale, **THEN** screenshot lưu lại phân biệt được viền giấy (giáp) với tint bất tử; có ảnh ấn triện mở đầu đặc biệt. (AC-VI03) `[Visual — BLOCKING]`
- **GIVEN** HUD, **WHEN** chụp ô Special ở `Ready`, `Empty`, `NotReady`, `NoVariant`, `Error`, **THEN** 5 screenshot lưu lại, phân biệt được bằng hình (chặn tới khi có GDD HUD). (AC-VI04) `[UI — BLOCKING]`
- **GIVEN** kỹ năng thiếu `anim` và dev build có XML lệch đồ thị, **WHEN** chạy, **THEN** screenshot lưu lại của clip chung thay thế và của dấu hiệu trên màn hình dev. (AC-VI05) `[Visual/UI — BLOCKING]`

**Độ phủ:** R1 D01–02 · R2 D03–07 · R3 D09–11 · R4 D12 · R5 D13–15 · R6 D15–16 · R7 M01–13, E03, E05 · R8 M14, S12 ·
R9 E01–03, S09, F10 · R10 D17 · R11 C01–03 · R12 H01–09 · R13 C05–15 · R14 C04, C07–10, S10 · R15 C16 · R16 S01 ·
R17 S02 · R18 S03–06 · R19 S07–08 · R20 S11, S14 · R21 D07–08, D18 · R22 D19–22, E06 · R23–33 D13, D19, M03, F05,
C06, C15, C16, C18, C10, D07, D17, V02 · F1 F01–02 · F2 F03 · F3 F04–06, E05 · F4 F07 · F5 F08–09 · F6 F10, C18 · F7 F11–13 ·
F8 F14, H02–03.

*Khi lan thay đổi sang Character Controller: viết lại AC-R13, R13a, R15–R19, X05 của Controller cho khớp hàm chọn kỹ năng.*

## Open Questions

| # | Câu hỏi | Người quyết | Hạn |
|---|---|---|---|
| OQ-S1 | ~~**Graph Toolkit trên Unity 6.3**~~ **Đã chốt 2026-11-06:** Graph Toolkit 0.4 không có API công khai để tạo node từ code, nên không mở được XML thành đồ thị; Skill Tool dựng bằng GraphView (có sẵn trong Editor), XML là nguồn duy nhất (ADR-0003). Ghi chép cũ: (package thử nghiệm 0.4.0-exp.2): spike S1–S7 của ADR-0003 — đồ thị 3 node → postprocessor (`GraphDatabase.LoadGraph` ngoài importer) → file XML thật; xuất hai lần ra byte giống nhau; chạy batchmode; mở lại graph; player build không chứa graph. Thất bại → công cụ soạn thay thế (UI Toolkit tự viết hoặc XML có schema + validator); runtime không đổi vì chỉ đọc XML | technical-director, tools-programmer | trước story đầu tiên của hệ này (ADR) |
| OQ-S2 | Hồi chiêu kỹ năng đặc biệt khi **Respawn**: về 0 (đề xuất, đánh lại boss công bằng) hay giữ | game-designer + GDD Defeat & Retry | khi viết GDD Defeat & Retry |
| OQ-S3 | **Combat**: định nghĩa `hit_profile` (hình vùng chạm, sát thương, hit-stop, knockback), trần giáp (số đòn, thời lượng), giáp không chặn sát thương của đòn boss "bắt buộc né" | game-designer (GDD Combat) | GDD Combat |
| OQ-S4 | **Loadout**: luật "một vũ khí khi ra ngoài, đổi ở nhà" và socket stone (bao nhiêu ô, gắn ở đâu) — ghi vào GDD Loadout | game-designer | GDD Loadout |
| OQ-S5 | Bảng tổ hợp **tự do hoàn toàn**: ai bảo đảm mỗi họ vũ khí có công cụ cho pha boss trên không / nhiều tầng ("mọi vũ khí hạ được mọi boss")? Đề xuất: Boss Encounter kiểm theo `commit_time` và một checklist "có đòn hướng lên" theo họ | game-designer + Boss Encounter | GDD Boss Encounter |
| OQ-S6 | Tụ lực **nhiều nấc** (mạnh dần theo thời gian giữ) thay cho nhánh `charged` nhị phân | game-designer | sau MVP |
| OQ-S7 | Move list ẩn đòn chưa dùng ("???") để giữ khám phá (Pillar 1), hay hiện đủ. Lập trường creative-director: ẩn nhưng **hiện số lượng** ("3/7 đòn") | game-designer + ux-designer | GDD HUD / Menu |
| OQ-S8 | **Đơn vị** trong registry: `horizontal_reach` ghi 2,8 u prototype; GDD này dùng đơn vị game. Chuẩn hóa registry và thêm hạng `R_skill` vào Controller F3 | systems-designer | khi lan thay đổi sang Controller |
| OQ-S9 | ~~Nơi nạp XML~~ **Đã chốt (ADR-0002, 2026-11-06):** file `.xml` thật trong thư mục riêng `Assets/StreamingAssets/Skills/`, đọc bằng `FolderSkillTextSource` sau `ISkillTextSource` (sửa 2026-11-06 theo người dùng: bỏ `SkillCatalog`, không Addressables); nguồn khác thêm sau mà không sửa lõi | technical-director | — |
| OQ-S10 | **Không có trần `commit_time` cho đòn thường** (người dùng chọn 2026-11-05, rủi ro đã chấp nhận): họ vũ khí chậm nhất quyết định cửa sổ dễ tổn thương tối thiểu của mọi boss (≥ 1,25 × `commit_time_max`), có thể làm loãng boss "pattern dày" (Pillar 4). Theo dõi `commit_time_max(họ)` mỗi khi thêm họ; xem lại khi viết GDD Boss Encounter | game-designer + Boss Encounter | GDD Boss Encounter |
| OQ-S11 | Đề xuất của creative-director cho OQ-S5: validator **cảnh báo** (không chặn) khi một bảng thiếu đòn hướng lên / trên không, chạy từ khi soạn bảng thứ hai — tránh phải sửa lại 14 bảng khi có GDD Boss | game-designer | trước bảng thứ hai |
| OQ-S13 | **Ngữ nghĩa thực thi của cổng dữ liệu** (thêm 2026-11-07): Hit `origin` đặt tâm vùng chạm tại vị trí chụp (vùng không đi theo Owner?); Move `toward` cho người chơi (side-view) chỉ lấy dấu ngang hay cả hướng 2D cho quái bay; Spawn `toward` khi Owner trùng vị trí Target. Combat và Enemy AI chốt khi viết GDD | game-designer, ai-programmer | trước story chạy kỹ năng quái |
| OQ-S14 | **Truy vấn thế giới cho Search** (thêm 2026-11-07): hệ nào trả danh sách thực thể theo loại / GID / bán kính lúc chạy (Enemy AI, một registry thực thể chung?), người chơi có GID không, và Search có thấy thực thể đã chết / đang ẩn không. Runtime chưa hiện thực; lõi chỉ có dữ liệu `SkillSearch` | game-designer, ai-programmer, technical-director | trước story chạy kỹ năng quái |
| OQ-S15 | **Hệ thống nào sở hữu logic kỹ năng** (thêm 2026-11-08, sau khi file kỹ năng chỉ còn phần thể hiện): loại đòn thường / đặc biệt, tình huống dùng (đất / trên không / khi né), họ vũ khí, hồi chiêu, hành vi chạm đất, tên hiển thị — nằm ở bảng tổ hợp, Skill Stone, Loadout, Controller, hay một hệ "Skill Logic" riêng? ~~Bảng tổ hợp có soạn trong Skill Tool không?~~ Đã chốt 2026-11-08: không — hệ thống tạo combo riêng soạn bảng tổ hợp và sở hữu cửa sổ hủy, tụ lực, chuyển động, giáp. | game-designer, systems-designer, user | trước story gán kỹ năng vào đòn / stone |
| OQ-S16 | **Bộ thực thi Skill Graph lúc chạy** (thêm 2026-11-09): khi nào mỗi loại node "kết thúc" (Animation hết clip / loop; Effect hết Duration; Hit một lần vs Steady; Move tới đích), CanCancel khi kỹ năng bị né / ngắt, UseAttackSpeed co giãn những gì, GID -1 = chủ kỹ năng, FactorType / SkillMaterialType do Combat định nghĩa, Search / Hit truy vấn creature qua hệ nào. **Đã có bộ chạy tối thiểu (2026-11-14):** `SkillRun` (lõi, chạy cây sự kiện, End) + `SkillCaster` / `PlayerSkillPerformer` (phát clip theo `AnimationType` bằng PlayableGraph, VFX từ catalog, vẽ vùng Hit; khóa di chuyển tới End); **chưa có**: sát thương / trúng (`hit` không bao giờ bắn), Search thật (luôn không thấy), Move, LookAt, Camera, Knockback, Buff, CanCancel, UseAttackSpeed, dữ liệu nối dây lúc chạy. **Phải tra kỹ năng theo id ở mỗi lần ra đòn** (nạp lại nóng, ADR-0002 sửa 2026-11-11). Chưa hiện thực | technical-director, gameplay-programmer, game-designer | trước story chạy kỹ năng |
| OQ-S17 | **VFX lúc chạy** (thêm 2026-11-11): bộ thực thi Skill Graph gọi `VfxSpawner.Spawn` cho node Effect (Pivot / Offset / Space / BindType → vị trí, xoay, gắn theo) và hiệu ứng của Hit (chỗ trúng, HitEffectDuration); dừng VFX khi kỹ năng bị né / ngắt (mục "Hủy bằng né", "Hitstun"); đồng hồ Game đẩy `Tick` (hit-stop); catalog vẫn là tham chiếu trực tiếp hay chuyển sang Addressables khi số effect lớn. Chưa hiện thực | technical-director, gameplay-programmer, technical-artist | cùng OQ-S16 |
| OQ-S18 | **Input cho moveset** (thêm 2026-11-13): thêm hành động `Button1`–`Button5`, `Key1`–`Key4` vào Input (bảng Rule 2, asset, catalog, test), phím mặc định cho tay cầm (9 nút: tổ hợp với nút vai?), số phận của Attack / Special / Plunge (gộp vào Button1 / Button2 / dòng `air` + `down`?), xung đột phím với Interact / Dodge | game-designer, ux-designer (Input GDD) | trước story Controller dùng Moveset |
| OQ-S19 | **Đá và vũ khí** (thêm 2026-11-13): Skill Stone GDD định nghĩa đá → moveset id (một đá nhiều moveset theo họ vũ khí?), độ hiếm, cách có được; Loadout định nghĩa số ô từng vũ khí (2–4, theo tier / nhánh?), đổi đá chỉ ở nhà, lưu save; có cần bộ đá khởi đầu để người chơi mới không "tay không" | game-designer, systems-designer, economy-designer | GDD Loadout / Skill Stone |
| OQ-S20 | **Controller + Moveset lúc chạy** (thêm 2026-11-13): Controller nhận `MoveInputEvent`, cấp context / moving / dir / chain_ref, gọi `MoveSelector`, chạy kỹ năng (OQ-S16); hồi chiêu theo dòng (khi nào bắt đầu, HUD); bộ đệm cho input đã nhận diện; gỡ `CommandTable` | technical-director, gameplay-programmer | cùng OQ-S16 |
| OQ-S21 | **Enemy AI đầy đủ** (thêm 2026-11-14): GDD Enemy AI thay BT tối thiểu — cảm nhận (tầm nhìn, nghe), telegraph trước đòn, va chạm / địa hình cho creature (hiện chỉ đi ngang, không trọng lực), hurt / die khi có Combat, ngân sách 2 ms / khung, công cụ soạn / xem BT, dữ liệu creature (TID, chỉ số, rig, BT) | game-designer, ai-programmer | trước story quái thật |
| OQ-S12 | Luật "một vũ khí, đổi ở nhà" + "thử lại nhanh trước arena": bảo đảm có đường về nhà đổi đồ không bị phạt khi kẹt trước một boss (tinh thần Pillar 3) | game-designer (GDD Loadout, Defeat & Retry) | GDD Loadout |
