# Character Controller

> **Status**: Designed (pending independent review)
> **Author**: Đặng Đình Dũng + Claude (game-designer, ux-designer, gameplay-programmer)
> **Last Updated**: 2026-10-31
> **Last Verified**: 2026-10-31
> **Implements Pillar**: Core Loop › Moment-to-Moment (né → áp sát → 2–3 đòn → rút; nhịp ngang hoặc nhanh hơn Hollow Knight; không trạng thái nào khóa đòn đánh), Pillar 2 (dải tốc độ theo họ vũ khí; vũ khí không đổi cơ thể), Pillar 3 (cùng controller, cùng số liệu cho hai nửa game), Pillar 4 (né được pattern boss trong arena dọc)
> **Creative Director Review (CD-GDD-ALIGN)**: CONCERNS 2026-10-31 → REVISED 2026-10-31 — (1) C-10 giữ nguyên, thêm ràng buộc cứng cho Combat (root tôn trọng i-frame) và ghi ngoại lệ "hitstun và root do kẻ địch gây ra, luôn có báo trước" vào lời hứa và binding test 2 (người dùng chọn); (2) OQ-C13 có tiêu chí đạt/trượt và giá trị lùi về; (3) header thêm Pillar 3

> **Sửa 2026-11-14 (người dùng — đòn đánh rời Controller):** Controller **không còn đòn đánh**: bỏ các trạng thái
> GroundAttack, AirAttack, DashAttack, Plunge, LandRecovery, chuỗi đòn, `WeaponMoveset`, các sự kiện AttackStarted /
> AttackPhaseChanged / PlungeLanded, knob AttackMoveMult, LandCancelLockout, LandRecovery, ComboReset, DashAttackCarry,
> DashAttackDecay, PlungeSpeed, AttackWindow, PlungeWindow (và F8, validator 2, 13). Controller chỉ còn chạy / đi, nhảy, nhảy
> đôi, coyote, rơi xuyên sàn, né, hitstun, khóa điều khiển, modifier. **Mọi đòn là skill**: người chơi chọn qua moveset (đá
> khảm), bộ chạy skill giữ khóa `MoveOnly` từ lúc tung tới End (GDD Skill & Moveset Rule 35–45). Các Rule / Formula / AC về
> đòn đánh bên dưới chỉ còn để tra cứu; Input vẫn ghi lần bấm Attack / Plunge nhưng Controller không tiêu thụ (OQ-S18).
> Animation: Animator chỉ tự phát di chuyển + phản ứng (né, bị đánh, chết) theo bộ slot chung (Skill & Moveset Rule 45).

## Summary

Character Controller biến ý định của người chơi thành chuyển động và hành động của nhân vật: chạy với vận tốc
tức thì, nhảy cao thấp theo thời gian giữ, nhảy đôi, né trên đất và trên không với i-frame, bốn ngữ cảnh đòn
(trên đất, trên không, khi lướt, Plunge), đi trên dốc, platform một chiều và trần hang. Nó sở hữu máy trạng
thái hành động và quyết định lần bấm trong bộ đệm của Input có hiệu lực hay không, với cửa sổ theo đồng hồ Game
để hit-stop không ăn mất. Fantasy là "Nhẹ chân, nặng tay": cơ thể luôn nhẹ với mọi vũ khí, sức nặng nằm ở cú
va chạm (Combat); cùng một controller dùng cho cả nửa combat lẫn nửa cozy.

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `Input` (dữ liệu từ Weapon Moveset; ADR "Character controller physics" và đồng hồ Game dùng chung chưa có)

## Overview

Character Controller là hệ biến ý định của người chơi thành chuyển động và hành động của nhân vật
trong thế giới 2D đi ngang. Nó sở hữu ba thứ. Thứ nhất là **chuyển động**: chạy (vận tốc đổi tức
thì, quyết định C-1), nhảy
cao thấp theo thời gian giữ, nhảy đôi, rơi, đứng và đi trên dốc, đứng trên và rơi xuyên platform
một chiều, va trần hang. Thứ hai là **máy trạng thái hành động** (systems index, TD-A3): nhân vật
đang đứng, chạy, trên không, đánh (trên đất, trên không, khi lướt, khi đáp), né (trên đất và trên
không), choáng khi trúng đòn, bị khóa điều khiển, hay gục ngã; trạng thái nào cho phép chuyển sang
trạng thái nào, và cửa sổ hủy đòn nằm ở đâu. Thứ ba là **quyết định lần bấm có hiệu lực**: Input giữ
lần bấm trong bộ đệm, Controller khai báo cửa sổ và đồng hồ cho từng hành động, lấy lần bấm ra khi
trạng thái cho phép, và ghi lại lý do khi từ chối.

Người chơi cảm nhận hệ này trực tiếp hơn bất kỳ hệ nào khác: đây là "tay" của họ trong game. Concept
đặt yêu cầu cụ thể ở Core Loop › Moment-to-Moment: nhịp ngang hoặc nhanh hơn Hollow Knight, vẫn di
chuyển khi chém, nhảy được giữa đòn, hủy đòn sang né, **không trạng thái nào khóa đòn đánh** (trên
đất, trên không, khi lướt, khi đáp), và trọng lượng đến từ hit-stop, knockback, âm thanh, phản ứng
của quái chứ không từ việc khóa nhân vật. Prototype combat đã chạy các số liệu này và nhận verdict
PROCEED (~0,20 s mỗi đòn, nhảy 13, né 14 u/s). Không có hệ này, Combat, Weapon Moveset, Camera và
mọi hệ tương tác trong thế giới không có gì để đứng lên.

Phạm vi được giới hạn có chủ ý. Controller **không** tính sát thương, hitbox hay hit-stop (Combat);
**không** chứa thời gian và hình dạng của từng đòn theo từng loại vũ khí (Weapon Moveset cung cấp
dưới dạng dữ liệu, Controller đọc); **không** đọc phím vật lý (Input); **không** điều khiển camera
(Camera). Controller cung cấp cho các hệ đó: trạng thái hiện tại, sự kiện (nhảy, chạm đất, né, trúng
đòn), API dịch chuyển nhân vật, interface để Elemental & Status đăng ký hệ số chuyển động, và yêu cầu
khóa điều khiển cho các hệ như menu, câu cá, chuyển map. Cách hiện thực va chạm (kinematic tự viết
như concept nói, hay Rigidbody2D như prototype) → thành ADR, không quyết ở GDD này.

## Player Fantasy

**"Nhẹ chân, nặng tay."** *(Định khung: trực tiếp, người chơi điều khiển và cảm nhận hệ này. Hướng C
do creative-director đề xuất, người dùng chọn 2026-10-31.)*

Thân người giữ Giao Ước lúc nào cũng nhẹ. Chạy, nhảy, né đều ra ngay, và vũ khí nào cũng vậy. Sức
nặng nằm ở tay, tức là ở cú va chạm: quái khựng lại, bật lùi, tiếng đòn chắc nịch. Sức nặng không
phải cái neo kéo người chơi xuống đất. Búa đánh chậm hơn, nện mạnh hơn, nhưng chân vẫn nhẹ như khi
cầm dao găm. Hit-stop làm khoảnh khắc khựng lại; cơ thể thì không đánh mất nhịp kế tiếp. Đang chém
vẫn chạy, đang chém vẫn nhảy, nhát thứ hai chưa dứt đã lướt né được: mỗi trạng thái đều có lối ra,
và lối ra luôn là một chuyển động.

**Lời hứa nối tiếp Input.** Input hứa "lần bấm của bạn không bao giờ bị mất hay lạc chỗ" (GDD Input,
Player Fantasy). Controller hứa vế còn lại: **khi lần bấm hợp lệ, cơ thể đáp lại ngay, và không có gì
giữ chân người chơi ngoài những gì họ tự chọn — trừ hitstun và hiệu ứng trói (root) do kẻ địch gây ra,
luôn có báo trước, và không bao giờ tước i-frame của một cú né đang chạy** (Rule 19, 25; ràng buộc cho
Combat ở OQ-C8).

**Tham chiếu cảm giác:**
- *Hollow Knight:* nhát chém không làm mất quán tính ngang, nhảy được giữa đòn; phần "nặng" đến từ
  hit-stop và phản ứng của quái.
- *Hades:* vũ khí nặng vẫn hủy đòn sang dash. Khác biệt giữa vũ khí nằm ở nhịp đòn, không ở độ linh
  hoạt của cơ thể.

**Phục vụ pillar nào:**
- *Core Loop › Moment-to-Moment:* "vẫn di chuyển khi chém, nhảy được giữa đòn, cancel sang né"; "trọng
  lượng đến từ hit-stop, knockback, âm thanh và phản ứng của quái — không từ việc khóa nhân vật". Đây
  là bài học của prototype combat: nhịp "nặng hơn Hollow Knight một nấc" ở round 1 bị playtest bác,
  vì trọng lượng bị gán vào cơ thể. Fantasy này viết bài học đó thành luật.
- *Pillar 2 (Your Build, Your Way):* chọn nhanh hay nặng là **chọn nhịp đòn**, không phải chọn mức chịu
  khổ khi di chuyển. Mọi họ vũ khí dùng chung một cơ thể; Weapon Moveset chỉ đổi nhịp, tầm và hình
  dạng đòn.
- *Pillar 4 (Fewer Bosses, Each an Event):* arena boss là sân hội: đạn như pháo hoa, platform như giàn
  tre. Nhảy đôi và né trên không biến khoảng trời thành chỗ của người chơi; họ đổi hướng giữa không
  trung, chui qua khe đạn, đáp xuống đúng bậc mình chọn. Đường bay phải đọc được trước khi bấm, để né
  đạn là kỹ năng chứ không phải đoán mò.
- *Pillar 3 (Two Lives, One Loop):* nửa cozy dùng **cùng một controller, cùng số liệu**; không có chế độ
  "đi bộ chậm" riêng. Đổi số liệu giữa hai nửa sẽ làm hỏng trí nhớ cơ bắp. Cảm giác thư thả đến từ bối
  cảnh (không có mối đe dọa, nhảy đôi để leo ruộng bậc thang cho vui), không từ một nhân vật ì hơn. Ở
  làng, "nhẹ chân" nghĩa là **dừng đúng chỗ**: đứng chính xác trước luống đất, trước cửa nhà.

**Binding test.** Fantasy này bị phản bội nếu xảy ra bất kỳ điều nào sau:
1. Một họ vũ khí làm giảm tốc chạy, độ cao nhảy, thêm khung khóa chân, hoặc **thay thế** điều khiển của người chơi
   bằng chuyển động không do người chơi bấm ra. *(Lực của kỹ năng **cộng thêm** vào điều khiển trong lúc đòn mà người
   chơi bấm ra đang chạy thì được phép — Skill & Moveset Rule 7.)*
2. Trong một trạng thái đánh, giữ hướng mà nhân vật không di chuyển; hoặc bấm né trong cửa sổ hủy đòn
   đã khai báo mà né không ra. *(Ngoại lệ: hitstun và root do kẻ địch gây ra, luôn có báo trước.)*
3. Một lần bấm né hoặc nhảy hợp lệ trong lúc hit-stop ra trễ hơn frame đầu tiên sau hit-stop.
4. Trên dốc, nhảy bật thấp hơn hoặc dính lại so với đất bằng; đi xuống dốc bị nảy khỏi mặt đất; va trần
   hang làm nhân vật dính lại.
5. Đỉnh nhảy treo lâu tới mức né đạn thành đoán mò; né trên không không ra sau nhảy đôi khi luật nói là được.
6. *(Nửa cozy)* Thả hướng ở tốc độ đi bộ mà nhân vật trượt quá ô đất hoặc cửa đang nhắm; bấm xuống để
   tương tác trên platform một chiều mà nhân vật rơi xuyên xuống ngoài ý muốn.

Câu đo playtest (đưa vào Acceptance Criteria): người chơi có bao giờ mô tả nhân vật là "ì" hay "bị khóa"
không, ở cả hai nửa game.

## Detailed Design

*Nguồn: game-designer (luật, trạng thái, giao diện), ux-designer (cảm giác, cửa sổ, khả năng tiếp
cận), gameplay-programmer (khả thi), 2026-10-31. Quyết định của người dùng 2026-10-31: C-1 vận tốc
ngang tức thì; C-2 giữ hitstun 0,25 s khóa toàn bộ; C-3 lượt nhảy đôi và né trên không chỉ hồi khi
chạm đất; C-4 bất tử 0,6 s sau khi trúng đòn (luật của Combat). Sau Formulas (2026-10-31): C-5 trọng
lực rơi ×1,6; C-6 treo nhẹ ở đỉnh (kẹp ~0,04 s); C-7 hồi chiêu né tính từ lúc kết thúc; C-8 nhảy coyote
bù độ cao. Sau Edge Cases (2026-10-31, sửa thẳng vào luật): C-9 né trên đất lướt qua mép tính là lượt
né trên không; C-10 root cắt cú né ngay lập tức; C-11 khóa điều khiển chờ né / dash attack / Plunge /
hitstun chạy xong; cùng các sửa nhỏ ở Rule 3, 12, 15, 21, 23, 24, 26, 30, bảng States, validator 13.
Con số đánh dấu **(F)** là giá trị đề xuất đã chốt ở Formulas; con số đánh dấu **(P)** là giá trị
prototype đã playtest. Mọi
khẳng định về Physics2D là **[giả định]** vì không có tài liệu engine cho Physics2D (Open Questions).*

*Tỉ lệ thế giới (quyết định C-12 của người dùng, 2026-11-01): mọi con số chiều dài, tốc độ và gia tốc trong GDD này
là **đơn vị prototype** (thân cao 1,3 u). Game dùng đơn vị của art bible §8.1: **1 u = 1 tile = 16 px, thân 48 px = 3 u**.
Runtime = số trong GDD × **k = 30/13 ≈ 2,308** (nhảy 13 → 30 u/s, đỉnh 2,727 → 6,293 u, chạy 6,5 → 15 u/s, collider
0,9×1,3 → 2,077×3,0 u, bậc 2,2 → 5,08 u). Thời gian (s) và hệ số nhân không đổi, nên cảm giác tính theo chiều cao thân giữ
nguyên. Bảng quy đổi đầy đủ: Tuning Knobs › Quy đổi tỉ lệ. Code giữ hằng `ControllerConfig.K`.*

*Đi bộ và chạy (quyết định C-13 của người dùng, 2026-11-02): khi thử với sprite thật, 15 u/s trông quá nhanh với dáng
đi bộ. **Đi bộ là dáng mặc định** (`walk_speed` 4 u/s đơn vị game ≈ 1,73 (P)); **giữ Run** (Left Ctrl / B–Circle, GDD
Input I-9) thì chạy bằng `move_speed`. Đã sửa Rule 1, Tuning Knobs (`walk_speed`), F3 (tầm ngang tính khi chạy),
Visual (clip walk), AC-R01, thêm AC-R01a. Né, đánh và nhảy không đổi; tốc ngang trong lúc đánh = tốc dáng hiện tại ×
`attack_move_mult`.*

*Sửa C-13 (2026-11-03, người dùng): Run đổi từ **giữ để chạy** sang **bấm để bật/tắt**. Mỗi lần nhấn Run đổi đi bộ ↔ chạy;
nhả nút không làm gì. Dáng là một thiết lập, không phải hành động: đổi được ở mọi trạng thái (kể cả hitstun, khóa điều
khiển), giữ nguyên qua dừng, nhảy, né, đánh. Lần nhấn lấy từ bộ đệm Input với cửa sổ `run_window` (0,10 s). Đã sửa Rule 1–2,
Tuning Knobs, Visual, AC-R01, AC-R01a.*

*Attack cooldown (2026-11-04, người dùng): mỗi ngữ cảnh đòn (đất, trên không, khi lướt) có thêm `cooldown` trong dữ liệu
vũ khí, mặc định 0 (không đổi hành vi cũ), khoảng 0–2 s. Tính **từ lúc đòn kết thúc** (hết recovery hoặc bị hủy); trong lúc
`cooldown` > 0, đòn kế cùng ngữ cảnh không hủy được recovery của đòn đang chạy mà phải đợi đòn đó xong. Cooldown riêng từng
ngữ cảnh: cooldown đòn đất không chặn đòn trên không. Lần bấm bị chặn nằm lại trong bộ đệm (`attack_window`). `combo_reset`
tính sau khi hết cooldown, để cooldown dài không làm chuỗi luôn về đòn 1. Đã sửa Rule 12, thêm AC-R13a.*

*Đối chiếu tên với ADR-0001 (2026-10-31): "đồng hồ Game" trong GDD này = `FrameClock.ScaledNow` / `ClockKind.Scaled` của
ADR-0001 — thời gian khung hình có scale (hit-stop, pause), không phải đồng hồ lịch của Game Time & Calendar. Hit-stop do
Combat đăng ký qua `ITimeScaleControl` (handle có hạn dùng, gộp bằng min); Controller chỉ đọc `IFrameClock`.*

*Sửa theo GDD Skill & Moveset (2026-11-05, `design/gdd/skill-moveset.md`, quyết định người dùng): **mọi đòn là một
kỹ năng dữ liệu** (XML xuất từ đồ thị node Start → End, sự kiện có độ trễ giây; không còn `windup`/`active`/`recovery`
khai báo — các giai đoạn suy ra từ cửa sổ hit đầu/cuối và node CancelWindow). Controller vẫn sở hữu máy trạng thái đòn,
luật hủy và API chuyển động; Skill & Moveset sở hữu schema kỹ năng, bảng tổ hợp theo họ vũ khí, hàm chọn kỹ năng và
**nhịp đòn** (chuyển quyền sở hữu khỏi Rule 12). Đã sửa Rule 12 (kỹ năng thay ngữ cảnh cứng; cho phép node chuyển động
có giới hạn cộng vào điều khiển), Rule 14 (nhảy giữa đòn: đòn diễn tiếp nhưng mất lực kỹ năng; ngoại lệ hủy), Rule 26
(`AttackStarted` mang `skill_id`), Rule 28 (cửa sổ Special), validator 8, 10, 11, 13, binding test 1, Dependencies,
Cross-References; AC-R13, R13a, R15–R19, X05 chuyển sang AC của Skill & Moveset. Luật Loadout mới: người chơi **chỉ mang
một vũ khí, đổi vũ khí / stone ở nhà**.*

### Core Rules

**Di chuyển**

1. **Vận tốc ngang tức thì (C-1).** Trên mặt đất, `vx = move.x × gait_speed × speed_mult`, với
   `gait_speed` = `move_speed` (chạy, 6,5 u/s (P)) khi công tắc Run đang bật (mỗi lần nhấn Run đổi bật/tắt), ngược lại `walk_speed` (đi bộ, 1,73 (P) =
   4 u/s đơn vị game; C-13); `speed_mult` từ modifier, Rule 25. Lần nhấn Run đổi tốc ngay trong frame đó; nhả nút không làm gì. Thả hướng là dừng ngay trong frame
   đó; quay đầu đổi dấu ngay. Hai knob `accel_time` và `decel_time` tồn tại để thử về sau, mặc định 0.
   Lý do: prototype đạt PROCEED với vận tốc tức thì, và nửa cozy cần dừng đúng trước luống đất
   rộng 1,5 u (binding test 6). Cần analog chỉ có một tốc độ (không có "đi bộ khi nghiêng một phần"),
   để giữ trí nhớ cơ bắp.
2. **Trên không** điều khiển ngang đầy đủ (hệ số 1,0), cũng tức thì, theo cùng dáng (đang chạy thì bay với tốc chạy).
3. **Trọng lực.** `g = gravity_scale × 9,81` (`gravity_scale` 3,2 (P) → ~31,4 u/s²). Khi đang rơi
   (`vy < 0`), trọng lực nhân `fall_gravity_mult` = 1,6 (quyết định C-5) để phần rơi nhanh hơn phần lên.
   **Treo nhẹ ở đỉnh (C-6):** trọng lực nhân `apex_hang_mult` (0,5) khi `|vy| < apex_hang_threshold`
   (1,5 u/s) **và** đang giữ Jump, chỉ trong cú bay sau Jump hoặc nhảy đôi (không sau né, knockback hay
   rơi khỏi mép), tối đa `apex_hang_max_time` (0,08 s), tức chậm thêm đúng ~0,04 s: đủ làm mềm cú
   chuyển sang rơi nhanh, không đủ làm đường bay khó đoán (binding test 5; F1). Khi bật tùy chọn "luôn
   nhảy hết độ cao" (Rule 29), điều kiện "đang giữ Jump" được coi là luôn đúng. Bước tích phân chia bước
   con tại điểm `vy` cắt `±apex_hang_threshold` và `vy = 0`, để thời điểm bắt đầu treo không phụ thuộc pha
   frame; khi đang treo mà `vy < 0`, trọng lực = `g × apex_hang_mult` (không nhân thêm `fall_gravity_mult`).
   Tốc độ rơi bị kẹp ở
   `max_fall_speed` = 16 u/s; Plunge (18 u/s) miễn giới hạn này và luôn nhanh hơn rơi thường.
4. **Cùng một bộ số cho cả hai nửa game.** Không có chế độ di chuyển riêng cho làng hay nông trại
   (Player Fantasy, Pillar 3).

**Nhảy**

5. **Nhảy từ đất.** Đặt thẳng `vy = jump_speed` (13 (P), đỉnh ~2,7 u), không cộng thành phần vận tốc
   theo dốc, nên nhảy từ dốc cao bằng nhảy từ đất bằng (binding test 4).
6. **Nhảy cao thấp.** Nếu `vy > 0` và `WasReleasedAfter(Jump, seq)` (Input) trả đúng, thì
   `vy ×= jump_cut_mult` (0,5 (F)), một lần cho mỗi cú nhảy. Nhả nút trong lúc hit-stop vẫn được tính.
7. **Coyote time.** Trong `coyote_time` (0,10 s (F), đồng hồ Game) sau khi rời mép mà không nhảy,
   một lần bấm Jump vẫn là nhảy từ đất và không tốn lượt nhảy đôi. Coyote là bộ đếm riêng của
   Controller, không phải bộ đệm của Input. Nhảy coyote thành công không có phản hồi riêng: nó phải
   cảm thấy như một lần nhảy bình thường. **Bù độ cao (C-8):** vì nhân vật đã tụt `Δy` kể từ mép, vận
   tốc nhảy coyote được tăng để đỉnh nhảy luôn bằng mép + chiều cao nhảy thường (F9), kể cả khi Settings
   nới coyote time.
8. **Nhảy đôi.** Một lượt trên không. **Đặt** `vy = double_jump_speed` (12; ~2,33 u, ~85% cú nhảy đầu,
   đủ tự leo lại một bậc 2,2 u, F2), không cộng dồn, nên độ
   cao luôn như nhau dù bấm lúc đang lên hay đang rơi; cũng cắt được bằng cách nhả (Rule 6). Rơi khỏi
   mép sau khi hết coyote thì vẫn còn lượt nhảy đôi. **Lượt chỉ hồi khi chạm đất** (C-3), khi dịch
   chuyển hoặc hồi sinh (Rule 24).

**Né**

9. **Né trên đất.** Hướng lấy từ `move.x` trong bản ghi lúc bấm (Input Rule 5); `move.x = 0` thì né
   theo hướng đang quay mặt. Lướt phẳng `dodge_speed` 14 u/s (P) trong `dodge_duration` 0,20 s (P)
   (~2,8 u), `vy = 0`, không trọng lực. **I-frame phủ trọn cú né** (0,20 s (P)): hình ảnh trùng với luật,
   không có "frame bị trúng khi đang né" mà người chơi không thấy được. Controller giữ cờ i-frame và phát
   `IFrameStarted/Ended`; Combat đọc cờ. Modifier tốc độ (Rule 25) không làm ngắn quãng né. Khi
   `iframe_scale` > 1 (trợ giúp, Rule 29), i-frame kéo dài thêm `t_d·(iframe_scale − 1)` sau `DodgeEnded`;
   Visual tắt hiệu ứng i-frame theo `IFrameEnded`, không theo `DodgeEnded`.
10. **Né trên không.** Như né trên đất, **chỉ theo phương ngang**: đường bay phẳng nên đọc được trước
    khi bấm (Pillar 4, binding test 5); phương dọc đã có nhảy đôi lo. Một lượt mỗi lần ở trên không.
    **Nhảy đôi và né trên không là hai lượt độc lập**, dùng theo thứ tự nào cũng được (nhảy → nhảy
    đôi → né, hoặc nhảy → né → nhảy đôi). Lượt chỉ hồi khi chạm đất (C-3), dịch chuyển hoặc hồi sinh.
    Sau cú né trên không, trọng lực chạy lại từ `vy = 0`. **Né trên đất lướt qua mép (C-9):** cú né đi
    tiếp theo phương ngang; nếu kết thúc khi đang ở trên không thì nó tính là lượt né trên không của lần
    bay đó, và coyote bắt đầu đếm từ lúc cú né kết thúc. Nhờ vậy tầm ngang tối đa vẫn đúng F3 (`n_d` ≤ 1).
11. **Hồi chiêu né** `dodge_cooldown` 0,35 s (P), dùng chung cho né trên đất và trên không, **tính từ lúc
    kết thúc cú né** (quyết định C-7). Chu kỳ né liên tiếp 0,55 s; spam né chỉ đi được ~5,1 u/s, chậm hơn
    chạy (6,5), và bất tử tối đa ~36% thời gian (F4). Né không bao giờ thành cách di chuyển mặc định, kể
    cả ở làng. Đây là thay đổi so với prototype (tính từ lúc bắt đầu, nhịp 0,35 s): cần playtest lại.

**Đánh: hợp đồng với Weapon Moveset**

12. **Controller chạy kỹ năng; Skill & Moveset cung cấp kỹ năng dưới dạng dữ liệu** *(sửa 2026-11-05)*. Khi một lần
    bấm Attack / Plunge / Special được tiêu thụ, Controller hỏi hàm chọn của Skill & Moveset (`context`, `moving`, `dir`,
    `chain_ref`) và chạy dòng thời gian của kỹ năng nhận về (cửa sổ hit, CancelWindow, node chuyển động, `land_policy`,
    `cooldown`). **Dữ liệu kỹ năng không được chứa hệ số** (tốc chạy, nhảy, trọng lực, `attack_move_mult`) **hay khung
    khóa chân** (binding test 1); **được** chứa node chuyển động có giới hạn (`lunge`, `launch` từ mặt đất, `halt`,
    `dive`) mà Controller thực thi và **cộng** vào điều khiển (Skill & Moveset Rule 7, F3, F5). Chỉ số chuỗi và
    `combo_reset` thay bằng liên kết nối đòn và `link_window` của bảng tổ hợp (`combo_reset` giữ làm `link_window` mặc
    định). Các ngữ cảnh bên dưới giữ nguyên ý nghĩa, nay là `context` của bảng tổ hợp:
    - **Trên đất:** chuỗi tối đa `string_length` đòn (mặc định 3). Chỉ số đòn về 0 nếu sau khi hết
      recovery mà không có Attack nào được tiêu thụ trong `combo_reset` (0,30 s); và về 0 ngay khi chuỗi bị
      ngắt bởi Hitstun, Teleport, Respawn, Defeated hoặc đổi vũ khí. Hủy đòn bằng né hoặc nhảy thì giữ chỉ
      số; `combo_reset` tính từ lúc đòn bị hủy.
    - Mỗi đòn chụp bộ số của vũ khí lúc `AttackStarted`; đổi vũ khí giữa đòn không đổi đòn đang chạy.
      Dữ liệu vũ khí không qua validator lúc runtime → ngữ cảnh đó dùng bộ số dự phòng tích hợp sẵn (số
      prototype ở cuối luật này), log lỗi một lần; di chuyển không bao giờ bị chặn.
    - **Trên không:** một đòn mỗi lần bấm; đáp đất giữa đòn thì đòn bị hủy (không có khóa khi đáp).
    - **Dash attack:** Attack được tiêu thụ trong lúc né. Giữ `dash_attack_carry` (0,8 (P)) × tốc độ né
      theo hướng né, giảm tuyến tính với `dash_attack_decay` (6 (P)) (F8); khi quán tính xuống dưới vận
      tốc đi bằng tay ở ×0,6, input điều khiển tiếp mà không giật. **I-frame tắt khi dash attack bắt
      đầu**, để không có đòn đánh bất tử chiếm ưu thế tuyệt đối.
    - **Plunge:** chỉ ở trên không. `vx = 0`, `vy = −plunge_speed` (18 (P)). Chạm đất thì phát
      `PlungeLanded(pos)`; sóng xung kích và sát thương là việc của Combat. Sau đó vào `LandRecovery`
      (`land_recovery` 0,16 s (P)).
    - Số liệu prototype (P) nay là ba **kỹ năng dự phòng** tích hợp sẵn của Skill & Moveset Rule 22 (trên đất: hit
      0,04 → 0,10, CancelWindow 0,13, End 0,20; trên không: 0,03 → 0,10, 0,13, 0,18; khi lướt: 0,02 → 0,12, 0,18, 0,26).
      GDD Skill & Moveset sở hữu các con số này.
13. **Di chuyển khi đánh** dùng hằng số của Controller, **giống nhau cho mọi vũ khí**: ×`attack_move_mult`
    (0,6 (P)) khi đánh trên đất và trong LandRecovery; ×1,0 khi đánh trên không (binding test 2).
14. **Hủy đòn.**
    - **Nhảy được ở bất kỳ pha nào** của đòn trên đất. Đòn vẫn diễn tiếp, hệ số di chuyển đổi sang ×1,0; lực của kỹ
      năng bị xóa và các node chuyển động còn lại bị bỏ qua; từ đó đòn chịu `land_policy` *(Skill & Moveset Rule 9)*.
    - *Ngoại lệ của Skill & Moveset Rule 9:* pha tụ lực (`charging`) bị hủy hẳn bằng nhảy hoặc né; kỹ năng đặc biệt hủy
      được `recovery.open` của đòn thường và pha tụ lực; kỹ năng có `dive` hủy được bằng né trên không ở mọi pha. Mốc
      "né / đòn kế" là node CancelWindow của kỹ năng (mặc định `recovery_start` + 0,03 s).
    - Né, đòn kế và Plunge chỉ được kể từ `recovery_start + cancel_lockout`. Bấm sớm hơn thì lần bấm
      nằm trong bộ đệm và ra ngay ở frame đầu tiên được phép.
    - Plunge hủy được bằng né trên không nếu còn lượt.
    - LandRecovery hủy được bằng nhảy, né hoặc đánh sau `land_cancel_lockout` (0,08 s (P)).
15. **Thứ tự khi nhiều lần bấm cùng hợp lệ trong một frame:** **mỗi bước chỉ bắt đầu một hành động**,
    theo `seq` nhỏ nhất (lần bấm trước ra trước). Lần bấm còn lại nằm trong bộ đệm và được xét ở bước sau
    theo trạng thái mới (ví dụ Dodge rồi Attack: bước này né, bước sau ra dash attack). Không có luật này,
    Jump rồi Dodge cùng bước sẽ đặt `vy` = 13 rồi `vy` = 0 và cú nhảy biến mất. Controller dùng `Peek` của
    Input để so `seq`.
16. **Hướng mặt** theo dấu của `move.x` khi không đánh. Hướng mặt bị chốt trong suốt một đòn (vẫn lùi
    được ở ×0,6 nhưng hitbox không lật), và chỉ đổi khi đòn kế bắt đầu, theo `move` của bản ghi đó.
    Dash attack quay theo hướng né; Plunge giữ hướng lúc bắt đầu.

**Tổ hợp phím (trả lời Input OQ-I12)**

17. **Plunge khi đang đứng trên đất** được tiêu thụ thành **Attack** (đòn trên đất) qua `baseAction` của
    bản ghi (Input Rule 4). Plunge buffer trên không mà chạm đất trước khi được tiêu thụ cũng ra đòn trên đất.
18. **DropThrough:**
    - Trên platform một chiều (mọi mặt đỡ chân đều là một chiều): rơi xuyên platform đó; bỏ qua nó cho
      tới khi chân đã xuống thấp hơn mặt platform, với sàn thời gian `drop_ignore_time` (0,12 s).
    - Đứng vắt giữa platform một chiều và đất cứng: không rơi; lần bấm được tiêu thụ thành Jump.
    - **Trên đất cứng: tiêu thụ thành Jump** qua `baseAction`, không nuốt mất lần bấm (giống Hollow Knight).
    - Trên không: tiêu thụ thành Jump (nhảy đôi nếu còn lượt).
    - Bấm Xuống đơn thuần không bao giờ rơi xuyên platform (binding test 6).

**Trúng đòn**

19. **Hitstun (C-2).** Combat gửi `HitReceived(knockback_vec, stun_time)`; Controller đặt vận tốc theo
    knockback (7 (P)) và vào Hitstun trong `stun_time` (0,25 s (P), Combat sở hữu). Trong Hitstun không di
    chuyển, không hành động; bộ đệm của Input vẫn giữ lần bấm (lần bấm còn trong cửa sổ khi hết choáng
    thì ra ngay). Đây là ngoại lệ duy nhất của luật "không trạng thái nào khóa đòn đánh" (concept Core
    Loop), và chỉ do kẻ địch gây ra, không do lựa chọn của người chơi.
20. **Bất tử sau khi trúng đòn (C-4)** là luật của Combat (đề xuất 0,6 s tính từ lúc trúng). Controller
    không giữ bộ đếm này; nó chỉ đọc cờ từ Combat nếu cần (ví dụ để Visual nhấp nháy).

**Địa hình**

21. **Dốc.** Mặt có góc ≤ `max_walk_angle` (50°; map sinh và art giới hạn dốc ở 45°, phần dư hấp thụ
    pháp tuyến ở mép collider) là đất đi được; dốc hơn coi như tường (F6).
    - Đứng yên trên dốc không trượt; tốc độ ngang trên dốc bằng trên đất bằng.
    - Đi xuống dốc: nhân vật bám vào mặt đất (snap) trong khoảng tính theo F6 để không nảy khỏi mặt
      đất (binding test 4). **Snap tắt từ frame nhảy cho tới khi cờ `jump_rising` tắt** (cờ bật khi Jump
      hoặc nhảy đôi, tắt khi `vy ≤ 0` lần đầu); không dùng `vy` thô vì đi lên dốc `vy` cũng dương.
      Thiếu điều này, snap kéo nhân vật xuống ngay sau khi nhảy và cảm giác như dính.
    - **Rời đất mà không nhảy** (qua đỉnh dốc lồi, quá `snap`): đặt `vy = min(vy, 0)` để đầu dốc không
      thành bệ phóng; coyote bắt đầu đếm.
    - Né trên dốc đi theo mặt dốc khi đang chạm đất; lướt ra khỏi mép thì theo Rule 10 (C-9). Gặp dốc vượt
      `max_walk_angle` hoặc tường khi đang né: `vx` bị cắt, cú né và i-frame vẫn chạy đủ thời gian (i-frame
      chỉ phụ thuộc thời gian, không phụ thuộc quãng đường).
22. **Platform một chiều.** Chỉ đỡ nhân vật khi đang đi xuống (`vy ≤ 0`) và chân đã ở trên mặt platform
    từ trước bước di chuyển. Từ dưới nhảy xuyên lên được; vừa rơi xuyên xuống thì không đáp lại.
23. **Trần và tường.**
    - Va trần: `vy = min(vy, 0)` ngay, giữ `vx`, không dính lại (binding test 4). Có `corner_correction`
      (0,15 u): nếu chỉ phần mép đầu chạm mép trần, nhân vật được đẩy ngang để đi qua, **chỉ khi** vị trí
      mới không vướng gì (kiểm bằng cast); nếu vướng thì xử lý như va trần bình thường. Nhảy đơn không bao
      giờ va trần hang thấp nhất (5 u); nhảy đôi trong hang thì có thể va (F2).
    - Vách đá và tường bờ nước (map sinh từ seed): cắt `vx`. Không bám tường, không nhảy tường (concept
      không có hai thứ này).
    - Controller ghi `last_safe_ground`. Rơi dưới `kill_y` (= `world_bounds.min.y − kill_margin`, do Region
      Loading cung cấp) thì phát `FellOutOfWorld`; Defeat & Retry quyết hậu quả.

**API cho các hệ khác**

24. **Khóa điều khiển, dịch chuyển, hồi sinh.**
    - `RequestControlLock(owner, Full | MoveOnly)` trả về handle ngay; chỉ handle đó gỡ được khóa; khóa có
      chủ đã chết thì tự gỡ ở bước kế và log (giống Input Rule 11). Dùng khi vẫn ở chế độ Gameplay nhưng cần
      giữ nhân vật (đi vào cửa, dùng công cụ). Khóa thay cho cả màn hình (menu, câu cá, chuyển map) thì dùng
      chế độ của Input.
    - **Khóa chờ động tác (C-11):** đòn đánh thường bị hủy ngay; Dodge, DashAttack, Plunge và Hitstun chạy
      xong rồi khóa mới có hiệu lực, kèm sự kiện `ControlLockApplied`. Khóa khi đang ở trên không thì vẫn
      rơi. Nhiều khóa cùng lúc: khóa hiệu dụng là khóa mạnh nhất (`Full` > `MoveOnly`), mỗi handle độc lập.
      Gỡ khóa cuối cùng thì về Grounded hoặc Airborne tùy tiếp xúc.
    - `Teleport(pos, facing, reason)`: hủy hành động đang làm, đặt vận tốc về 0, hồi lượt nhảy đôi và né
      trên không, phát `Teleported`.
    - `Respawn(pos)`: như Teleport, thêm xóa Hitstun, xóa khóa của chính Controller, về Idle.
    - `EnterDefeated()`: Defeat & Retry gọi khi Combat báo hết HP; vào Defeated từ bất kỳ trạng thái nào.
25. **Modifier chuyển động.** `AddMovementModifier(owner, speed_mult, jump_mult, root)` trả về handle.
    Các hệ số nhân với nhau rồi kẹp: tốc độ trong [0,3; 1,5], nhảy trong [0,5; 1,2] tính theo **độ cao**
    (F5). `root` chặn di chuyển, nhảy và né, **không chặn đánh**. Hệ số tốc độ không làm ngắn quãng né
    (Rule 9). **Root cắt cú né đang chạy ngay lập tức (C-10):** cú né kết thúc, i-frame tắt
    (`IFrameEnded`), quán tính dash attack về 0. **Ràng buộc cứng cho Combat (CD-GDD-ALIGN):** hiệu ứng trói
    tôn trọng i-frame giống sát thương, nên trên thực tế root chỉ cắt được quán tính dash attack (i-frame đã
    tắt khi dash attack bắt đầu, Rule 12) và phần né sau i-frame (khi `iframe_scale` < 1, nếu có). Root luôn
    có báo trước và có trần thời lượng do Elemental & Status đặt (OQ-C8). Root giữa cú nhảy: `vx` = 0, phương dọc vẫn rơi, nhả nút
    vẫn cắt được cú nhảy. Hiệu ứng trạng thái (Elemental & Status) được làm chậm cơ thể;
    binding test 1 chỉ cấm vũ khí làm việc đó.
26. **Sự kiện phát ra:** `Jumped(kind: Ground | Coyote | Air)`, `LeftGround`, `Landed(fall_speed)`,
    `DodgeStarted(dir, air)`, `DodgeEnded`, `DodgeReady`, `IFrameStarted/Ended`,
    `AttackStarted(skill_id, context)` *(sửa 2026-11-05: mang id kỹ năng thay chỉ số chuỗi)*, `AttackPhaseChanged`, `PlungeLanded`, `HitstunStarted/Ended`,
    `Bonked`, `FacingChanged`, `Teleported`, `FellOutOfWorld`, `ControlLockApplied`, `StateChanged`,
    `AirResourcesChanged`. Lượt nhảy đôi và né trên không còn lại được công bố qua thuộc tính
    `AirJumpsLeft` / `AirDodgesLeft` và sự kiện `AirResourcesChanged`, để Visual hiện ngay trên thân nhân
    vật (không ở HUD, vì mắt người chơi đang dán vào đạn).
27. **Log lý do từ chối (Input H-14).**
    - Mỗi frame, nếu `Peek` thấy một bản ghi mà trạng thái hiện tại không cho tiêu thụ, Controller gắn
      lý do mới nhất vào `seq` của bản ghi đó.
    - Khi bản ghi hết hạn hoặc bị rào xóa mà chưa được tiêu thụ, Controller ghi một dòng
      `{seq, action, reason, state, t_press, t_expire}`.
    - Lý do: `Cooldown`, `CancelLockout`, `NoAirJump`, `NoAirDodge`, `Hitstun`, `ControlLocked`, `Rooted`,
      `WrongState`, `Defeated`, `NeverPolled`.
    - Ghi trong development build và Editor, có kênh gửi sang Telemetry. Đây là dữ liệu để phân loại
      "xác nhận do bên tiêu thụ" ở Input AC-T04.

**Bộ đệm: cửa sổ khai báo với Input**

28. Controller đăng ký với Input các cửa sổ sau, **tất cả theo đồng hồ Game** để hit-stop làm cửa sổ gần
    như đứng lại (binding test 3). Trường hợp xấu nhất: 0,20 s + `hitstop_budget` 0,21 s × 0,95 ≈ 0,40 s
    thời gian thật, dưới `buffer_retention` 1,0 s của Input (Input F2).

    | Hành động | Cửa sổ (F) | Đồng hồ | Lý do |
    |---|---|---|---|
    | Jump | 0,12 s | Game | Thể loại dùng 0,08–0,15 (Celeste ~0,08); hơi rộng vì có nhảy giữa đòn |
    | DropThrough | 0,12 s | Game | Bằng Jump, vì có thể được tiêu thụ thành Jump |
    | Attack | 0,20 s | Game | Khoảng một đòn: bấm trong lúc đòn trước active/recovery là đòn kế ra |
    | Dodge | 0,15 s | Game | Phủ `cancel_lockout` và phần cuối hồi chiêu |
    | Plunge | 0,15 s | Game | Được tiêu thụ thành Attack nếu đã chạm đất |
    | Special | 0,20 s | Game | Nút Kỹ năng đặc biệt (Skill & Moveset Rule 20); chung hàng đợi và `seq` với Attack; LandRecovery cho phép Special sau `land_cancel_lockout` như Attack |

**Khả năng tiếp cận (hook; Settings sở hữu màn hình và lưu trữ)**

29. Controller nhận từ Settings (đẩy vào, như Input TD-B8): `buffer_scale` và `coyote_scale` (1,0–2,0)
    nhân vào mọi cửa sổ và coyote time; tùy chọn **luôn nhảy hết độ cao** (tắt Rule 6) cho người khó giữ
    nút; `drop_through_hold` (0 hoặc ~0,15 s giữ Xuống trước khi DropThrough có hiệu lực) chống rơi ngoài
    ý muốn; `iframe_scale` cho chế độ trợ giúp (ngưỡng do hệ độ khó quyết, nếu có). Không có auto-dodge:
    né là kỹ năng (Pillar 4). Chế độ chậm (nếu có) không thuộc Controller; Controller chỉ cần dùng đồng
    hồ Game ở mọi chỗ để time scale có hiệu lực.

**Hiện thực (định hướng; chi tiết → ADR "Character controller physics")**

30. Controller cần các tính chất sau, bất kể cách hiện thực: kết quả tất định và unit test được (lõi C#
    thuần, va chạm qua một interface inject được); dốc không nảy và không trượt; tự lọc platform một
    chiều; phản ứng ngay ở frame đầu tiên sau hit-stop (máy trạng thái và bước tích phân chạy mỗi frame
    được vẽ, theo đồng hồ Game, sau bước xử lý của Input); không singleton tĩnh (TD-C). Hướng được
    gameplay-programmer đề xuất: Rigidbody2D Kinematic kèm bước "va chạm rồi trượt dọc bề mặt" tự viết
    bằng cast. Concept nói "kinematic controller tự viết"; prototype dùng Rigidbody2D động và bị nảy,
    trượt trên dốc. Quyết định cuối thuộc ADR (Open Questions).
    - **Tích phân chính xác:** vị trí dọc dùng `y += vy·dt − ½·g·dt²` (rồi `vy −= g·dt`), khớp đúng công
      thức F1–F2. Euler thường làm đỉnh nhảy thấp đi ~0,1 u ở 60 fps, khiến nhảy đôi (2,19 u) trượt bậc 2,2 u.
    - **Kẹp và chia nhỏ bước khi khựng:** `dt` của frame bị kẹp ở `max_frame_dt` (0,10 s), rồi chia thành
      các bước con ≤ 1/30 s, để snap (F6) và cast không hỏng và người chơi không bị mô phỏng rơi xuống hố
      trong lúc không thể phản ứng.
    - **Thứ tự trong mỗi bước con:** (1) tăng bộ đếm và chuyển trạng thái theo thời gian (hết né, hết
      choáng, hết lockout); (2) tiêu thụ input theo trạng thái mới (Rule 15: tối đa một hành động);
      (3) tích phân và va chạm; (4) chuyển trạng thái theo tiếp xúc (`Landed`, `LeftGround`, `Bonked`).
      "Frame chạm đất" là frame đầu tiên bắt đầu với trạng thái Grounded.
    - **Kẹt trong địa hình** (xuất hiện, dịch chuyển, hồi sinh): đẩy ra theo hướng ngắn nhất, tối đa
      `depen_max` (1,0 u); không được thì về `last_safe_ground`; vẫn không được thì log lỗi.
    - **Bộ đếm theo tick:** mọi bộ đếm của Controller (né, hồi chiêu, coyote, lockout, treo, choáng) dùng
      tick số nguyên của đồng hồ Game (cùng nguồn với Input), so sánh `≤` ở biên; không cộng dồn số thực
      (0,35 s ≠ 21 × 1/60 trong số thực).
    - **Ngân sách CPU:** `Controller.Step` ≤ [technical-director chốt] ms p95 trên máy tối thiểu (Open Questions).

### States and Transitions

| Trạng thái | Di chuyển | Nhảy | Né | Đánh | Chuyển sang |
|---|---|---|---|---|---|
| Grounded (Idle / Run) | ×1 | từ đất | trên đất | chuỗi trên đất | Airborne, GroundAttack, Dodge, Hitstun, ControlLocked, Defeated |
| Airborne (Rise / Fall) | ×1 | nhảy đôi nếu còn (coyote: từ đất) | trên không nếu còn | trên không / Plunge | Grounded, AirAttack, Plunge, Dodge, Hitstun |
| GroundAttack | ×0,6 | mọi pha | sau lockout | đòn kế sau lockout | Grounded, AirAttack (nhảy giữa đòn), Dodge, Hitstun |
| AirAttack | ×1 | nhảy đôi nếu còn | sau lockout | đòn trên không kế | Airborne, Grounded (đòn hủy khi đáp), Plunge, Dodge, Hitstun |
| Dodge (đất / không) | khóa theo hướng né | không | không | dash attack | Grounded / Airborne, DashAttack, Hitstun (chỉ khi hết i-frame) |
| DashAttack | quán tính 0,8 giảm dần | sau lockout | sau lockout | không | Grounded / Airborne, Dodge, Hitstun |
| Plunge | rơi thẳng | không | trên không nếu còn | không | LandRecovery, Dodge, Hitstun |
| LandRecovery | ×0,6 | sau `land_cancel_lockout` | sau `land_cancel_lockout` | sau `land_cancel_lockout` | Grounded, Airborne, Dodge, GroundAttack, Hitstun |
| Hitstun | không (bị đẩy lùi) | không | không | không | Grounded / Airborne |
| ControlLocked (Full / MoveOnly) | không | không | không | Full: không; MoveOnly: có | Grounded / Airborne tùy tiếp xúc khi gỡ khóa cuối |
| Defeated | không | không | không | không | Respawn → Grounded |

- Mọi trạng thái trừ Hitstun, ControlLocked (Full) và Defeated đều có ít nhất một lối ra bằng đánh hoặc
  né: đây là luật "không trạng thái nào khóa đòn đánh" ở dạng kiểm được.
- `root` (Rule 25) chặn cột Di chuyển, Nhảy, Né ở mọi trạng thái nhưng không chặn cột Đánh.
- Bảng chuyển trạng thái là dữ liệu tường minh trong lõi; mọi lần từ chối có lý do (Rule 27).
- **Chuyển trạng thái chung (từ mọi trạng thái):** → Defeated (`EnterDefeated`, Rule 24); Teleport / Respawn
  → Grounded hoặc Airborne theo cast ở điểm đến; → ControlLocked khi động tác hiện tại chạy xong (C-11).

### Interactions with Other Systems

| Hệ | Hướng | Hợp đồng | Chủ giao diện |
|---|---|---|---|
| Input | Controller đọc | Đăng ký cửa sổ + đồng hồ (Rule 28); `TryConsume`/`Peek` cho Jump, Attack, Dodge, Plunge, DropThrough; `WasReleasedAfter`; `ReadMove` | Input sở hữu API; Controller quyết hiệu lực |
| Weapon Moveset | Controller đọc dữ liệu | Nhịp đòn theo ngữ cảnh (Rule 12); không có trường chuyển động; Moveset xuất thời gian cam kết tối đa mỗi họ cho Boss | Weapon Moveset sở hữu dữ liệu; Controller sở hữu schema ngữ cảnh |
| Combat | hai chiều | Controller phát `AttackStarted`/phase, hướng mặt, `PlungeLanded`, cờ i-frame. Combat gửi `HitReceived(knockback, stun)`; sở hữu hitbox, sát thương, hit-stop, bất tử sau khi trúng đòn, **HP hiện tại** | Combat |
| Loadout & Player Stats | — | Giữ HP tối đa trong khối chỉ số; Controller không giữ HP | Loadout |
| Camera | Camera đọc | Vị trí, vận tốc, hướng mặt, Grounded; `Teleported` để snap | Controller |
| Elemental & Status | ghi | Handle modifier (Rule 25) | Controller sở hữu API |
| Defeat & Retry | hai chiều | Nghe `FellOutOfWorld`, Combat báo hết HP; gọi `Respawn`; Controller vào Defeated | Defeat & Retry |
| Interaction & Context Prompts | đọc + khóa | Grounded / hướng mặt để biết có hiện prompt; khóa `MoveOnly`/`Full` khi dùng công cụ | Controller sở hữu API |
| Region Loading | gọi | `Teleport` khi chuyển map (input bị chế độ `Locked` của Input chặn) | Region Loading |
| Enemy AI, Spawning & Drops | chỉ đọc | Vị trí, trạng thái, cờ né / i-frame, hướng mặt | Controller |
| Audio, VFX | nghe | Sự kiện Rule 26 | Controller phát |
| Telemetry | nghe | Sự kiện + log từ chối (Rule 27) | Telemetry quan sát |
| HUD | đọc | Hồi chiêu né (nếu hiện) | Controller |
| Settings & Accessibility | đẩy vào | Hook Rule 29 | Controller sở hữu API |
| Game Time & Calendar | — | Không trực tiếp. Controller dùng đồng hồ Game (thời gian có scale của frame), không dùng đồng hồ lịch | — |

## Formulas

*Nguồn: systems-designer, 2026-10-31, dựa trên Section C và mã prototype combat. Quyết định của người
dùng: C-5 (rơi ×1,6), C-6 (treo ở đỉnh), C-7 (hồi chiêu né từ lúc kết thúc), C-8 (bù độ cao coyote).
Hằng chung: `g = 3,2 × 9,81 = 31,39 u/s²`; `g_fall = g × 1,6 = 50,22 u/s²`; 60 fps. Mọi công thức
liên tục và giả định tích phân chính xác (Rule 30).*

### F1. Đường nhảy

The jump_arc formula is defined as:

`h_apex = v_j² / (2g) + Δh_hang` ; `t_up = v_j / g + T_hang·(1 − k_hang)` ; `t_down = fall(h_apex)` ;
`T_air = t_up + t_down` ; `h_cut = y(t_r) + (c·v_r)² / (2g)`

với `fall(h) = √(2h / g_fall)` nếu `h ≤ v_max² / (2·g_fall)`, ngược lại
`fall(h) = v_max / g_fall + (h − v_max² / (2·g_fall)) / v_max`.

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `v_j` | float (u/s) | 6,5–14,3 | calculated (F5) | `jump_speed` hiệu dụng, mặc định 13 (P) |
| `g` | float (u/s²) | 31,39 | constant | `gravity_scale` 3,2 × 9,81 |
| `g_fall` | float (u/s²) | 31,4–62,8 | calculated | `g × fall_gravity_mult` (1,6) |
| `v_max` | float (u/s) | 12–17 | data file | `max_fall_speed` 16 |
| `k_hang` | float | 0,3–1,0 | data file | `apex_hang_mult` 0,5 (1,0 = tắt) |
| `T_hang` | float (s) | 0–0,10 | calculated | thời gian dưới hang, ≤ `apex_hang_max_time` 0,08; hang chỉ chạy khi `|vy| < 1,5` **và** đang giữ Jump |
| `Δh_hang` | float (u) | 0–0,05 | calculated | độ cao thêm nhờ hang (~0,035) |
| `c` | float | 0,3–0,7 | data file | `jump_cut_mult` 0,5 |
| `t_r`, `v_r` | float | 0–`t_up` | calculated | thời điểm nhả và `vy` lúc nhả (`v_r > 0`) |

**Output Range:** `h_apex` trong [0,673; 2,727] u (nhả ngay ở frame đầu → giữ hết). `T_air` trong
[0,371; 0,784] s. Có chặn trên vì `v_max` và `T_hang` đều bị kẹp.

**Example:**
- Giữ hết: `t_up` = 0,414 + 0,04 = 0,454 s; `h` = 2,692 + 0,035 = **2,727 u**. Rơi: chạm 16 u/s sau
  2,548 u, rồi 0,179/16 → `t_down` = 0,330 s. **`T_air` = 0,784 s.**
- Nhả ngay: `vy` = 6,5 → `h` = **0,673 u**, `T_air` = 0,371 s.
- Nhả ở `t_r` = 0,2 s: `y` = 1,972, `v_r` = 6,72 → `h_cut` = **2,152 u**.

### F2. Nhảy đôi và độ cao tối đa

The air_height formula is defined as:

`h_dj = v_dj² / (2g) + Δh_hang` ; `H_max = h_apex + h_dj` ; `H_head = H_max + collider_h`

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `v_dj` | float (u/s) | 10,5–12,5 | data file | `double_jump_speed` 12; đi qua hệ số nhảy của F5 |
| `collider_h` | float (u) | 1,2–1,4 | data file | chiều cao collider nhân vật, 1,3 (prototype) |
| `cave_gap_min` | float (u) | ≥ 5 | consumer-declared (map sinh) | khoảng hở trần hang nhỏ nhất |
| `step_max` | float (u) | 2,1–2,2 | consumer-declared (map sinh, landmark) | độ cao platform / bậc cao nhất |

**Output Range:** `h_dj` = 2,329 u (85% cú nhảy đầu); `H_max` = 5,056 u; đỉnh đầu ở 6,356 u.
Trong hang 5 u, nhảy đơn chạm đầu ở 4,03 u nên **không bao giờ va trần**; nhảy đôi trong hang **va
trần** và xử lý theo Rule 23 (không dính).

**Example:** nhảy đơn thừa 2,727 − 2,2 = **0,53 u** so với bậc cao nhất; chân ở trên mốc 2,2 trong
0,357 s (≈ 2,32 u ngang). Nhảy đôi riêng lẻ từ cùng mặt đứng: 2,329 > 2,2, thừa 0,13 u. Arena không
muốn nhảy đôi va trần cần trần ≥ ~6,9 u (level design quyết).

### F3. Tầm ngang (dành cho level design)

The horizontal_reach formula is defined as:

`R = v_x·(T_total − n_d·t_d) + n_d·d_dodge`

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `v_x` | float (u/s) | 6,5 | data file | `move_speed` (đang chạy), giá trị gốc không modifier. Nhảy khi đi bộ chỉ được `walk_speed`/`move_speed` ≈ 0,27 tầm này; level đo khe theo tầm chạy (C-13) |
| `T_total` | float (s) | 0,37–1,7 | calculated | tổng thời gian bay của chuỗi động tác (F1, F2) |
| `n_d` | int | 0–1 | constant | số lần né trên không (C-3) |
| `t_d`, `d_dodge` | float | 0,20 s; 2,8 u | data file / F4 | thời gian và quãng né |

**Output Range:** 0 (nhảy tại chỗ) đến ~12,6 u (trần kỹ năng).

**Example:**
- Nhảy đơn: 6,5 × 0,784 = **5,10 u**. Nhảy rồi né ở đỉnh: **7,90 u**.
- Chuỗi chuẩn (nhảy → nhảy đôi ở đỉnh → né ở đỉnh nhảy đôi → rơi từ 5,056): `T` = 0,454 + 0,422 + 0,20
  + 0,475 = 1,551 s → `R` = 6,5 × 1,351 + 2,8 = **11,58 u**. Trần kỹ năng (nhảy đôi ngay trước khi chạm
  đất): **12,62 u**.
- **Quy tắc thiết kế:** khe bắt buộc ≤ 0,85 × chuỗi tương ứng: ≤ 4,3 u nếu chỉ dùng nhảy; ≤ 9,8 u nếu
  cần cả nhảy đôi lẫn né. Không bao giờ bắt buộc dùng coyote.

### F4. Chu kỳ né

The dodge_cycle formula is defined as:

`d_dodge = v_d·t_d` ; `t_iframe = t_d·s_if` ; `T_cycle = t_d + cd` ; `f_inv = t_iframe / T_cycle` ;
`v_spam = d_dodge / T_cycle`

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `v_d` | float (u/s) | 10–16 | data file | `dodge_speed` 14 (P) |
| `t_d` | float (s) | 0,15–0,25 | data file | `dodge_duration` 0,20 (P) |
| `cd` | float (s) | 0,25–0,50 | data file | `dodge_cooldown` 0,35 (P), tính từ lúc kết thúc (C-7) |
| `s_if` | float | 1,0–1,5 | consumer-declared (Settings / độ khó) | `iframe_scale`, mặc định 1,0 |

**Output Range:** `d_dodge` = 2,8 u; `T_cycle` trong [0,40; 0,75] s; `f_inv` trong [0,20; 0,75]
(0,55 khi `s_if` = 1,5; ngưỡng do hệ độ khó quyết).

**Example:** mặc định `T_cycle` = 0,55 s, **`f_inv` = 36,4%**, **`v_spam` = 5,09 u/s < 6,5** (chạy).
Đối chiếu nếu tính hồi chiêu từ lúc bắt đầu: `T` = 0,35 s, `f_inv` = 57%, `v_spam` = 8,0 u/s (nhanh hơn
chạy). Né trên không chỉ một lượt mỗi lần bay (C-3), nên spam né chỉ xảy ra trên đất.

### F5. Di chuyển hiệu dụng

The effective_movement formula is defined as:

`vx = root ? 0 : move.x · move_speed · clamp(Π s_i, s_lo, s_hi) · m_ctx`

`v_j_eff = root ? 0 : jump_speed · √clamp(Π j_i, j_lo, j_hi)` (tương tự cho `v_dj`)

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `move.x` | float | −1–1 | engine (Input `ReadMove`) | trục ngang sau dead zone |
| `s_i`, `j_i` | float | > 0 | consumer-declared (Elemental & Status) | hệ số của từng handle modifier |
| `s_lo`, `s_hi` | float | 0,3; 1,5 | data file | `modifier_speed_min` / `max` |
| `j_lo`, `j_hi` | float | 0,5; 1,2 | data file | `modifier_jump_min` / `max`, tính theo **độ cao** (nên vận tốc nhân căn bậc hai) |
| `m_ctx` | float | 0,6 / 1,0 | constant | `attack_move_mult` khi đánh trên đất và LandRecovery; 1,0 cho trường hợp còn lại |
| `root` | bool | — | calculated | OR của mọi handle |

**Output Range:** `|vx|` trong [1,17; 9,75] u/s khi không bị root (đáy 0,3 × 0,6 × 6,5 = 1,17 > 0, nên
binding test 2 giữ được). Nhảy đơn từ 1,35 đến 3,27 u. Với `j` = 0,5, nhảy + nhảy đôi còn 2,56 u (cả hai
cú có treo; 2,49 u nếu không treo), vẫn trên bậc 2,2.

**Example:** lạnh 0,7 × ướt 0,8 = 0,56 → chạy 3,64 u/s; chém trên đất 2,18 u/s.

### F6. Dốc và bám đất

The ground_snap formula is defined as:

`walkable = (n.y ≥ cos(θ_max) − ε)` ; `vy_slope = −vx · n.x / n.y` ;
`snap = max(snap_min, |vx|·dt·tan(θ_max) + skin)`; bám đất chỉ chạy khi frame trước đã chạm đất **và**
`jump_rising = false` (Rule 21).

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `n` | Vector2 | đơn vị | engine | pháp tuyến bề mặt (nhiều hit → chọn pháp tuyến hướng lên nhiều nhất) |
| `θ_max` | float (°) | 30–55 | data file | `max_walk_angle` 50 |
| `ε` | float | 1e-4 | constant | dung sai số thực |
| `dt` | float (s) | ≤ 1/30 | engine (đồng hồ Game, đã chia bước con, Rule 30) | |
| `skin` | float (u) | 0,005–0,02 | data file | 0,01 |
| `snap_min` | float (u) | 0,05–0,15 | data file | 0,10 |

**Output Range:** `snap` trong [0,10; 0,29] u ở 60 fps; có chặn trên vì `dt` bị chia bước ≤ 1/30.

**Example:** chạy 6,5 u/s: 0,108 × 1,192 + 0,01 = **0,139 u**. Né 14 u/s: **0,288 u**. Đi lên dốc 45°:
`vy_slope` = 6,5, tốc độ dọc mặt dốc 9,19 u/s (tốc độ ngang giữ nguyên, Rule 21).

### F7. Cửa sổ đệm, trường hợp xấu nhất theo thời gian thật

Dùng **Input F2**, không định nghĩa lại: `W_real_worst = W + hitstop_budget × (1 − s_min)`, với
`W = window × buffer_scale`.

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `window` | float (s) | 0,12–0,20 | data file | bảng Rule 28 |
| `buffer_scale` | float | 1,0–2,0 | consumer-declared (Settings) | hook khả năng tiếp cận (Rule 29) |
| `hitstop_budget`, `s_min` | float | 0,21 s; 0,05 | consumer-declared (Input, Combat) | |
| `buffer_retention` | float (s) | 1,0 | consumer-declared (Input, registry) | |

**Output Range:** 0,32 s (Jump, scale 1) đến 0,60 s (Attack, scale 2); luôn ≤ 1,0 s.

**Example:** Attack ở scale 2: 0,40 + 0,1995 = **0,5995 s**. Điều kiện để luôn hợp lệ ở scale tối đa:
mọi cửa sổ ≤ 0,40 s.

### F8. Quán tính dash attack

The dash_carry formula is defined as:

`v_c(t) = max(0, carry·v_d − decay·v_d·t)` ; `vx = (v_c > dir·v_in) ? dir·v_c : v_in` (với `v_in` là
`vx` của F5 khi `m_ctx` = 0,6) ; `d_carry = ((carry·v_d)² − max(0, dir·v_in)²) / (2·decay·v_d)`

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `carry` | float | 0,5–1,0 | data file | `dash_attack_carry` 0,8 (P) |
| `decay` | float (1/s) | 3–10 | data file | `dash_attack_decay` 6 (P) |
| `dir` | int | ±1 | calculated | hướng né |

**Output Range:** `v_c` từ 11,2 về 0 trong 0,133 s; `d_carry` trong [0,66; 0,75] u; quán tính luôn
tắt trước khi đòn dash (0,26 s) hết recovery.

**Example:** không giữ hướng: 125,4/168 = **0,747 u**. Giữ hướng tiến: quán tính xuống 3,9 u/s ở
t = 0,087 s sau 0,656 u, rồi đi tiếp 3,9 u/s bằng tay mà không giật.

### F9. Coyote, bù độ cao và khả năng tiếp cận

The coyote formula is defined as:

`t_coy = coyote_time · coyote_scale` ; `Δy = y_mép − y_lúc_nhảy` (độ tụt đo thực tế kể từ mép; xấp xỉ
`½ · g_fall · t²`, t ≤ `t_coy`) ; `v_coy = √(v_j_eff² + 2·g·Δy)` (C-8)

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `coyote_time` | float (s) | 0,06–0,15 | data file | 0,10, đồng hồ Game |
| `coyote_scale` | float | 1,0–2,0 | consumer-declared (Settings) | hook khả năng tiếp cận |
| `Δy` | float (u) | 0–1,0 | calculated | độ tụt kể từ mép |

**Output Range:** `t_coy` trong [0,10; 0,20] s; `Δy` tối đa trong [0,25; 1,00] u; `v_coy` trong [13,0;
15,2] u/s. Đỉnh nhảy coyote luôn bằng mép + 2,727 u.

**Example:** `coyote_scale` 2, nhảy ở cuối cửa sổ: `Δy` = ½ × 50,22 × 0,04 = 1,00 u →
`v_coy` = √(169 + 62,8) = **15,2 u/s**. Nếu không bù, đỉnh chỉ còn mép + 1,69 u, không lên nổi bậc 2,2.

### Ràng buộc kiểm tra khi nạp dữ liệu (validator)

1. `double_jump_speed < jump_speed`.
2. `max_fall_speed × 1,1 ≤ plunge_speed` (Plunge luôn nhanh hơn rơi thường).
3. `h_apex(j_hi) + collider_h ≤ cave_gap_min − 0,2` (nhảy đơn không va trần hang; hiện 4,57 ≤ 4,8).
4. `h_apex ≥ step_max + 0,3` và `h_dj ≥ step_max + 0,05`.
5. `h_apex(j_lo) + h_dj(j_lo) ≥ step_max` (một hiệu ứng trạng thái không được nhốt người chơi dưới bậc).
6. `dodge_duration + dodge_cooldown > dodge_distance / move_speed` (né không nhanh hơn chạy; hiện cần
   `dodge_cooldown` > 0,231 s).
7. `apex_hang_max_time × (1 − apex_hang_mult) ≤ 0,05 s` (binding test 5).
8. `max(window) × buffer_scale_max + hitstop_budget × (1 − s_min) ≤ buffer_retention` (Input F2). Nếu
   về sau có chế độ chậm, chia thêm cho time scale nhỏ nhất. *(2026-11-05)* Thêm: cửa sổ đệm Attack ≥ thời gian
   `cancel_lockout` dài nhất trong dữ liệu kỹ năng (`max_cancel_lockout_in_data`), để lần bấm trong lockout còn sống
   khi lockout hết.
9. `corner_correction ≤ collider_w / 4`.
10. `land_cancel_lockout` ≥ 1/60 s. (`cancel_lockout` của kỹ năng: Skill & Moveset Rule 9, 23.)
11. Dữ liệu kỹ năng không có trường hệ số di chuyển, hệ số nhảy hay thời gian khóa chân (Rule 12); node chuyển động
    có giới hạn được phép và do validator của Skill & Moveset kiểm (F3, F5).
12. *(Gửi Combat, thông báo, không chặn)* khoảng cách giữa hai làn đạn "bắt buộc né" ≥ `T_cycle`
    (0,55 s); nếu ngắn hơn, pattern phải giải được bằng nhảy hoặc nhảy đôi.
13. `land_cancel_lockout < land_recovery`; nếu lọt tới runtime thì kẹp về `land_recovery − 1/60`. (Biên CancelWindow
    của kỹ năng và luật kẹp: Skill & Moveset Rule 9, 22, 24.)

## Edge Cases

*Nguồn: systems-designer, 2026-10-31. Mục nào cần đổi luật đã được sửa thẳng vào Section C/D (ghi chú
ở đầu Detailed Design); ở đây ghi kết quả và trỏ về luật.*

### Thứ tự trong một bước

- **If nhiều sự kiện rơi vào cùng một frame**: mỗi bước con chạy theo thứ tự thời gian → input → tích
  phân → tiếp xúc (Rule 30). Frame "chạm đất" là frame đầu tiên bắt đầu với trạng thái Grounded.
- **If hai lần bấm cùng hợp lệ trong một bước**: chỉ một hành động bắt đầu, theo `seq` nhỏ nhất; lần bấm
  kia được xét ở bước sau (Rule 15).

### Bộ đệm và hủy đòn

- **If Dodge được bấm trong Hitstun**: lần bấm nằm trong bộ đệm (0,15 s, đồng hồ Game). Nó ra ở frame đầu
  tiên sau khi hết choáng nếu được bấm trong 0,15 s cuối của 0,25 s choáng; bấm sớm hơn thì hết hạn và log
  `Hitstun` (Rule 27). Hồi chiêu né vẫn áp dụng.
- **If Attack được bấm khi trạng thái vẫn là Dodge ở bước tiêu thụ input**: ra dash attack, i-frame tắt
  ngay (Rule 12). Nếu bước thời gian đã kết thúc cú né thì ra đòn trên đất hoặc trên không bình thường.
- **If Jump được bấm đúng frame chạm đất sau Plunge**: lần bấm nằm trong bộ đệm; cửa sổ 0,12 s phủ được
  `land_cancel_lockout` 0,08 s, nên nhảy từ đất ra ở 0,08 s.
- **If Jump được bấm ngay trước frame chạm đất mà còn lượt nhảy đôi**: ra nhảy đôi, vì nhân vật vẫn ở trên
  không ở bước tiêu thụ input. Đây là quy ước của thể loại (Hollow Knight).
- **If coyote còn hiệu lực và một Jump trong bộ đệm được tiêu thụ cùng frame**: nhảy từ đất (coyote, có bù
  độ cao F9), không tốn lượt nhảy đôi. Thứ tự ưu tiên: đứng trên đất > coyote > nhảy đôi.
- **If Jump rơi đúng frame cuối của coyote (`t = t_coy`)**: vẫn là nhảy coyote (so sánh ≤, cùng quy ước
  biên với Input F1).
- **If Plunge được bấm trên không khi còn 0,02 s nữa thì chạm đất**: Plunge vẫn ra, phát `PlungeLanded` ở
  frame kế, rồi vào LandRecovery 0,16 s. Plunge từ độ cao thấp có gây sát thương đầy đủ không là việc của
  Combat. Plunge còn trong bộ đệm khi đã chạm đất thì ra đòn trên đất (Rule 17).
- **If DropThrough trên một platform một chiều nằm ngay trên một platform một chiều khác**: chỉ platform
  đang đứng bị bỏ qua (theo định danh collider); platform bên dưới vẫn đỡ nếu điều kiện Rule 22 đúng.
  Trong `drop_ignore_time` nhân vật tụt ~0,36 u, nên level phải đặt hai platform một chiều chồng nhau cách
  nhau ít nhất 1,5 u theo phương dọc.
- **If Jump và Dodge cùng nằm trong bộ đệm ở một frame**: Jump có `seq` nhỏ hơn → nhảy trước, bước sau ra né
  trên không (cú nhảy bị thấp vì né phẳng). Dodge có `seq` nhỏ hơn → né trước; Jump hết hạn sau 0,12 s,
  trước khi cú né 0,20 s kết thúc, và log `WrongState`.
- **If chuỗi đòn bị ngắt bởi Hitstun, Teleport, Respawn, Defeated hoặc đổi vũ khí**: chỉ số đòn về 0; hủy
  bằng né hoặc nhảy thì giữ (Rule 12).

### Lượt trên không

- **If đã dùng nhảy đôi và né trên không, rồi trúng knockback bị hất lên**: không hồi lượt nào (C-3), không
  có coyote khi bị hất ra khỏi mép. Bị hất xuống hố thì theo `FellOutOfWorld`.
- **If đáp xuống platform một chiều**: hồi cả hai lượt (mọi mặt đi được có phát `Landed`). Nhảy xuyên từ dưới
  lên qua platform thì không hồi.
- **If chạm đất trong lúc Plunge**: hồi cả hai lượt ngay ở frame chạm đất, dù đang ở LandRecovery.
- **If Teleport khi đang ở trên không**: hồi cả hai lượt, vận tốc về 0, trọng lực chạy lại từ `vy = 0`,
  không có coyote; chạm đất hay không xác định bằng một lần cast ở điểm đến.

### Địa hình

- **If chạy qua đỉnh dốc lồi**: bám đất nếu độ tụt trong frame ≤ `snap` (F6; dốc 45°: chạy tụt 0,108 u ≤
  0,139 u, né tụt 0,233 u ≤ 0,288 u). Tụt nhiều hơn thì rời đất, `vy = min(vy, 0)`, coyote bắt đầu đếm (Rule 21).
- **If đang né mà gặp dốc vượt `max_walk_angle` hoặc lao vào tường**: `vx` bị cắt; cú né và i-frame vẫn chạy
  đủ 0,20 s (Rule 21).
- **If né trên đất lướt khỏi mép**: tính là lượt né trên không của lần bay đó; coyote đếm từ lúc né kết
  thúc (Rule 10, C-9).
- **If va trần trong lúc né**: cú né trượt dọc bề mặt trần, không phát `Bonked` (vì `vy = 0`).
- **If `corner_correction` sẽ đẩy nhân vật vào tường**: không đẩy; xử lý như va trần (Rule 23).
- **If knockback đẩy nhân vật vào vách đá hoặc tường bờ nước**: `vx` = 0, không nảy lại, không có sát thương
  va tường, Hitstun chạy tiếp.
- **If xuất hiện, dịch chuyển hoặc hồi sinh bên trong địa hình**: đẩy ra tối đa `depen_max`; không được thì
  về `last_safe_ground`; vẫn không được thì log lỗi (Rule 30).
- **If đứng vắt giữa mép platform một chiều và đất cứng**: được đỡ nếu một trong hai mặt đỡ được, `Landed`
  chỉ phát một lần; DropThrough ra Jump (Rule 18).
- **If đứng sát tường vô hình ở bờ, chỗ câu cá**: tường cắt `vx`, không rung, không đẩy lùi, không bao giờ
  rơi. Ở chế độ Fishing, `ReadMove` trả (0, 0) (Input Rule 8) nên nhân vật không trôi. Level phải đặt vùng
  Interact phủ tới vị trí đứng sát tường.

### Modifier và khóa

- **If bị root giữa cú nhảy**: `vx` = 0 ngay; phương dọc vẫn rơi; nhả nút vẫn cắt được cú nhảy; nhảy đôi và
  né bị chặn, đánh vẫn được (Rule 25).
- **If bị root giữa cú né**: cú né kết thúc ngay, i-frame tắt, quán tính dash attack về 0 (Rule 25, C-10).
  Việc hiệu ứng trói có được áp lên nhân vật đang có i-frame hay không là luật của Combat (Open Questions).
- **If modifier tốc độ đổi giữa cú né**: không ảnh hưởng cú né hay quán tính dash attack; áp dụng từ bước
  đầu tiên sau khi né kết thúc (Rule 9).
- **If có yêu cầu khóa trong lúc đánh hoặc né**: đòn đánh thường bị hủy ngay; Dodge, DashAttack, Plunge,
  Hitstun chạy xong rồi khóa mới có hiệu lực, kèm `ControlLockApplied` (Rule 24, C-11).
- **If chủ của khóa bị hủy**: khóa tự gỡ ở bước kế, có log; gỡ khóa cuối thì về Grounded hoặc Airborne tùy
  tiếp xúc (Rule 24).
- **If cùng lúc có nhiều khóa MoveOnly và Full**: khóa hiệu dụng là khóa mạnh nhất; gỡ theo thứ tự nào cũng được.

### Hit-stop và thời gian

- **If timeScale đổi giữa cú né**: thời lượng và i-frame tính theo đồng hồ Game, nên kéo dài theo thời gian
  thật nhưng quãng né vẫn 2,8 u. Combat phải đọc cờ i-frame theo cùng đồng hồ.
- **If hit-stop bắt đầu đúng frame chạm đất**: `Landed` và việc hồi lượt vẫn xảy ra trong frame đó. Bấm
  Jump trong lúc hit-stop thì nhảy ra ngay, chuyển động chậm theo hit-stop (không vi phạm binding test 3).
- **If một frame khựng 1,5 s khi đang ở trên không gần trần**: `dt` bị kẹp ở `max_frame_dt` 0,10 s rồi chia
  bước con ≤ 1/30 s, mỗi bước con đều cast nên không xuyên trần (Rule 30). Lần bấm trong lúc khựng mang
  tuổi 0 (Input Edge Cases) nên vẫn còn hiệu lực.
- **If mở menu (push `Menu`, timeScale 0) khi đang ở trên không**: mọi thứ đứng yên, coyote và hồi chiêu
  không chạy. Khi chạy lại: `vy` được giữ, Move đọc lại ngay, bộ đệm đã bị rào của Input xóa; Jump đang giữ
  bị chốt nên treo ở đỉnh tắt (Open Questions về `last_release_seq`).

### Gục ngã và hồi sinh

- **If HP về 0 trong lúc có i-frame (sát thương không phải đòn đánh)**: vào Defeated ngay từ bất kỳ trạng
  thái nào, phát `IFrameEnded`. Loại sát thương nào xuyên i-frame là luật của Combat.
- **If rơi khỏi thế giới trong lúc Hitstun**: `FellOutOfWorld` phát ở mọi trạng thái, một lần mỗi lần rơi;
  Controller dừng mô phỏng tới khi có Teleport hoặc Respawn.
- **If hồi sinh khi hệ khác đang giữ khóa**: khóa của hệ khác vẫn giữ (Rule 24); development build cảnh báo
  nếu một khóa sống sót qua lần hồi sinh lâu hơn 5 s.

### Dữ liệu vũ khí

- **If đổi vũ khí giữa chuỗi đòn**: đòn đang chạy dùng nốt bộ số đã chụp; đòn kế dùng vũ khí mới, chỉ số về
  0 (Rule 12).
- **If dữ liệu vũ khí không qua validator lúc runtime**: dùng bộ số dự phòng, log lỗi một lần; di chuyển
  không bị chặn (Rule 12).
- **If `cancel_lockout ≥ recovery`**: validator từ chối; lọt tới runtime thì kẹp về `recovery − 1/60`
  (validator 13).

### Khả năng tiếp cận

- **If `buffer_scale` = 2 làm các cửa sổ chồng lên nhau**: Rule 15 phân xử; lần bấm cũ có thể ra muộn (ví
  dụ Jump bấm từ 0,24 s trước). Đây là cái giá có chủ đích của tùy chọn; validator 8 vẫn đạt (0,5995 s ≤ 1,0 s).
- **If bật "luôn nhảy hết độ cao"**: không còn cắt nhảy; treo ở đỉnh coi như luôn giữ Jump (Rule 3).
- **If bật `drop_through_hold` và có DropThrough trong bộ đệm**: bản ghi được tiêu thụ ngay và bắt đầu đếm giờ
  giữ (đồng hồ Game). Giữ Xuống đủ thời gian (điều kiện Input F3, 35°) thì rơi xuyên; nhả sớm thì ra Jump từ
  đất, không nuốt lần bấm. Lý do: cửa sổ 0,12 s ngắn hơn thời gian giữ 0,15 s, nên chờ đủ giờ giữ rồi mới
  tiêu thụ thì bản ghi đã hết hạn.

## Dependencies

### Upstream (Controller phụ thuộc vào)

| Phụ thuộc | Loại | Giao diện | Ghi chú |
|---|---|---|---|
| Input (#1) | cứng | `TryConsume`/`Peek`/`WasReleasedAfter`/`ReadMove`; đăng ký cửa sổ + đồng hồ; bản ghi tổ hợp có `baseAction` | GDD Input (Designed). Controller trả lời OQ-I12 ở Rule 17, 18, 27, 28 |
| Skill & Moveset (#14, `skill-moveset.md`) | cứng (dữ liệu + hàm chọn) | Kỹ năng XML (dòng thời gian Start → End), bảng tổ hợp theo họ vũ khí, hàm chọn kỹ năng, kỹ năng dự phòng (Rule 12) | Đảo phụ thuộc: Skill & Moveset cung cấp dữ liệu và hàm thuần; Controller chạy máy trạng thái và chuyển động. Thiếu hoặc lỗi dữ liệu → kỹ năng dự phòng, nên Controller chạy được trước khi có dữ liệu |
| Game Data & Tuning Config (#4) | mềm | Định dạng file knob (Tuning Knobs) và schema dữ liệu vũ khí | Phụ thuộc ADR TD-B4 (chưa viết) |
| ADR "Character controller physics" (đề xuất) | cứng cho hiện thực | Loại body, hàm cast, skin, layer, thứ tự frame, chế độ mô phỏng 2D | Chưa viết; chặn story đầu tiên (Open Questions) |
| Dịch vụ đồng hồ Game / hit-stop dùng chung (đề xuất, gắn ADR Input OQ-I1) | cứng cho hiện thực | `dt` có scale từ một nguồn duy nhất cho Input, Combat, Controller | Chưa có chủ; tránh lỗi trễ một frame khi hit-stop kết thúc |

Controller **không** phụ thuộc Combat, Camera hay hệ nào ở trên nó: Combat gửi `HitReceived` qua giao
diện Controller sở hữu, Elemental & Status đăng ký modifier, Settings đẩy hook vào (đảo phụ thuộc như
systems index quy định).

### Downstream (hệ phụ thuộc vào Controller)

| Hệ | Loại | Dùng gì | GDD | Systems index ghi Controller? |
|---|---|---|---|---|
| Combat (#9) | cứng | `AttackStarted`/phase, hướng mặt, `PlungeLanded`, cờ i-frame; gửi `HitReceived` | chưa có | có |
| Skill & Moveset (#14) | cứng | máy trạng thái đòn, API node chuyển động, luật hủy, `chain_ref`, `context`/`moving`/`dir` của bản ghi (Rule 12, 14, validator 11) | `skill-moveset.md` (Designed) | có |
| Camera (#7) | cứng | vị trí, vận tốc, hướng mặt, Grounded, `Teleported` | chưa có | có |
| Interaction & Context Prompts (#10) | cứng | Grounded, hướng mặt; khóa `MoveOnly`/`Full` | chưa có | có |
| Enemy AI, Spawning & Drops (#18) | cứng | vị trí, trạng thái, cờ né / i-frame | chưa có | có |
| Elemental & Status (#26) | cứng | API modifier (Rule 25) | chưa có | có |
| Defeat & Retry (#20) | cứng | `FellOutOfWorld`, `Respawn`, trạng thái Defeated | chưa có | **không** → bổ sung |
| Region Loading (#8) | cứng | `Teleport` khi chuyển map | chưa có | **không** → bổ sung |
| Boss Encounter (#19) | mềm | F3, F4 (tầm, chu kỳ né) để thiết kế pattern; validator 12 | chưa có | qua Combat |
| Audio (#33) | mềm (quan sát) | sự kiện Rule 26 | chưa có | **không** → bổ sung |
| HUD (#30) | mềm | hồi chiêu né (nếu hiện) | chưa có | không cần (tùy chọn) |
| Telemetry (#12) | mềm (quan sát) | sự kiện + log từ chối (Rule 27) | chưa có | **không** → bổ sung |
| Settings & Accessibility (#13) | mềm | đẩy hook Rule 29 | chưa có | không (đảo phụ thuộc) |
| Level design (map sinh, landmark, arena) | ràng buộc | F2, F3, Edge Cases (khe ≤ 0,85 × tầm; bậc ≤ 2,2 u; one-way chồng nhau ≥ 1,5 u; trần hang ≥ 5 u) | Map Gen chưa có trong index | — |

### Kiểm tra hai chiều

- **Input** (Designed): bảng Downstream của Input ghi Character Controller (cứng, bộ đệm, `ReadMove`, đăng ký
  cửa sổ). Khớp. Input OQ-I12 được trả lời ở đây: cửa sổ + đồng hồ (Rule 28), Plunge/DropThrough trên mặt đất
  (Rule 17–18), log lý do từ chối (Rule 27).
- **Game Time & Calendar** (Approved): không liên quan trực tiếp; Controller dùng đồng hồ Game của frame,
  không dùng đồng hồ lịch.
- Mọi hệ downstream khác chưa có GDD: khi viết, mục Dependencies phải ghi Character Controller với đúng giao
  diện trên.
- **Systems index cần cập nhật (Phase 5):** thêm Character Controller vào *Depends On* của Defeat & Retry (#20),
  Region Loading (#8), Audio (#33), Telemetry (#12).

## Tuning Knobs

*Nguồn: danh sách knob systems-designer đề xuất cùng Section D và Edge Cases (2026-10-31). Mọi giá trị nằm
trong file dữ liệu cấu hình của Controller (định dạng theo ADR TD-B4), không viết cứng. Ngoài khoảng an
toàn → mặc định + log lỗi; vi phạm ràng buộc chéo → mọi knob tham gia về mặc định (validator, Formulas).
**(P)** = giá trị prototype đã playtest.*

### Nhảy và rơi

| Knob | Mặc định | Khoảng an toàn | Ảnh hưởng | Quá cao | Quá thấp |
|---|---|---|---|---|---|
| `jump_speed` | 13 (P) | 12,5–13,4 | độ cao nhảy (F1) | va trần hang (validator 3) | không lên bậc 2,2 u (validator 4) |
| `gravity_scale` | 3,2 (P) | 2,5–4 | độ nặng toàn cục | nhảy cụt, gắt | lơ lửng, chậm (bài học round 1) |
| `fall_gravity_mult` | 1,6 (C-5) | 1,0–2,0 | độ dứt khoát khi rơi | khó canh chỗ đáp | lơ lửng như bay |
| `max_fall_speed` | 16 | 12–17 | tốc độ rơi tối đa | mắt không đọc kịp; vượt Plunge (validator 2) | rơi trôi |
| `apex_hang_mult` | 0,5 (C-6) | 0,3–1,0 | độ mềm ở đỉnh (1,0 = tắt) | — | treo lâu, khó đoán |
| `apex_hang_threshold` | 1,5 u/s | 0,5–2,5 | vùng vận tốc coi là "đỉnh" | treo quá rộng | không cảm nhận được |
| `apex_hang_max_time` | 0,08 s | 0–0,10 | giới hạn thời gian treo | vi phạm binding test 5 (validator 7) | — |
| `jump_cut_mult` | 0,5 | 0,3–0,7 | độ thấp khi nhả sớm | nhảy thấp khó làm | nhảy thấp quá cụt |
| `double_jump_speed` | 12 | 11,8–12,4 (và < `jump_speed`) | độ cao nhảy đôi (F2) | cao hơn nhảy đầu (validator 1) | không tự leo lại bậc 2,2 u (validator 4) |
| `coyote_time` | 0,10 s | 0,06–0,15 | dễ tha thứ khi rời mép | nhảy "từ không khí" lộ liễu | rơi khỏi mép khi tưởng còn đất |

### Di chuyển và né

| Knob | Mặc định | Khoảng an toàn | Ảnh hưởng | Quá cao | Quá thấp |
|---|---|---|---|---|---|
| `move_speed` | 6,5 (P) | 5,5–8 | tốc chạy khi bật Run, tầm nhảy (F3) | khó dừng đúng ô đất | ì, trái "nhẹ chân" |
| `walk_speed` (C-13) | 4 u/s **đơn vị game** (≈ 1,73 (P)) | 3–6 u/s game (1,3–2,6 (P)) | tốc đi bộ mặc định | dáng đi trượt, gần như chạy | sốt ruột khi đi quanh nông trại |
| `run_window` (C-13) | 0,10 s | 0–0,40 | lần nhấn Run cũ hơn mức này bị bỏ qua | đổi dáng trễ sau khi bấm | bấm trong hit-stop dài có thể trượt |
| `accel_time`, `decel_time` | 0, 0 (C-1) | 0–0,08 | độ trễ khởi động / dừng | trượt qua ô đất (binding test 6) | — |
| `dodge_speed` | 14 (P) | 10–16 | quãng né | né thành cách di chuyển | né không thoát được đạn |
| `dodge_duration` | 0,20 s (P) | 0,15–0,25 | thời gian né và i-frame | bất tử quá nhiều (F4) | né khó canh |
| `dodge_cooldown` | 0,35 s (P), từ lúc kết thúc (C-7) | 0,25–0,50 | nhịp né liên tiếp (F4) | thiếu lối thoát trong pattern dày | spam né nhanh hơn chạy (validator 6) |

### Đánh và hủy đòn *(hằng của Controller, giống nhau cho mọi vũ khí)*

| Knob | Mặc định | Khoảng an toàn | Ảnh hưởng | Quá cao | Quá thấp |
|---|---|---|---|---|---|
| `attack_move_mult` | 0,6 (P) | 0,4–1,0 | di chuyển khi chém trên đất | đánh và chạy như nhau, mất cam kết | gần như đứng im (round 1 bị bác) |
| `land_cancel_lockout` | 0,08 s (P) | 0,03–0,10 | khóa ngắn sau khi đáp Plunge | cảm giác dính sau plunge | plunge spam không cam kết |
| `land_recovery` | 0,16 s (P) | 0,10–0,25 | thời gian hồi sau Plunge | — | — |
| `combo_reset` | 0,30 s | 0,20–0,50 | thời gian giữ chỉ số chuỗi | chuỗi "nhớ" quá lâu | khó nối đòn 3 |
| `dash_attack_carry` | 0,8 (P) | 0,5–1,0 | quán tính đòn lướt (F8) | đòn lướt bay quá xa | đòn lướt không khác đòn thường |
| `dash_attack_decay` | 6 (P) | 3–10 | tốc độ tắt quán tính | — | trượt dài |
| `plunge_speed` | 18 (P) | 17–22 | tốc độ Plunge | khó né lúc rơi | chậm hơn rơi thường (validator 2) |

### Địa hình và hiện thực

| Knob | Mặc định | Khoảng an toàn | Ảnh hưởng |
|---|---|---|---|
| `max_walk_angle` | 50° | 30–55° | dốc đi được (F6); phải > dốc lớn nhất map sinh (45°) |
| `snap_min` | 0,10 u | 0,05–0,15 | khoảng bám đất tối thiểu (F6) |
| `skin` | 0,01 u | 0,005–0,02 | khoảng đệm của cast |
| `corner_correction` | 0,15 u | 0,05–0,225 | đẩy qua mép trần (≤ collider_w / 4) |
| `drop_ignore_time` | 0,12 s | 0,10–0,20 | sàn thời gian bỏ qua platform khi rơi xuyên |
| `depen_max` | 1,0 u | 0,5–2,0 | khoảng đẩy ra tối đa khi kẹt trong địa hình |
| `max_frame_dt` | 0,10 s | 0,05–0,20 | kẹp `dt` khi frame khựng (Rule 30) |
| `collider_w`, `collider_h` | 0,9 u, 1,3 u (prototype) | 0,8–1,0; 1,2–1,4 | kích thước collider nhân vật; F2 (va trần), validator 3 và 9 |

### Modifier, bộ đệm, khả năng tiếp cận

| Knob | Mặc định | Khoảng an toàn | Ghi chú |
|---|---|---|---|
| Kẹp modifier tốc độ | [0,3; 1,5] | — | F5; đáy > 0 nên vẫn di chuyển được khi chém (binding test 2) |
| Kẹp modifier nhảy (độ cao) | [0,5; 1,2] | — | F5; đáy vẫn cho nhảy + nhảy đôi ≥ 2,2 u (validator 5) |
| Cửa sổ Jump / DropThrough / Attack / Dodge / Plunge | 0,12 / 0,12 / 0,20 / 0,15 / 0,15 s | mỗi cửa sổ ≤ 0,40 s | Rule 28; validator 8 |
| `buffer_scale`, `coyote_scale` | 1,0 | 1,0–2,0 | Settings đẩy vào (Rule 29) |
| `iframe_scale` | 1,0 | 1,0–1,5 | Settings / hệ độ khó (F4) |
| `drop_through_hold` | 0 | 0 hoặc 0,10–0,20 s | Settings (Rule 29) |
| "Luôn nhảy hết độ cao" | tắt | bật / tắt | Settings (Rule 29) |

### Quy đổi tỉ lệ (C-12, 2026-11-01)

Runtime dùng 1 u = 1 tile = 16 px (art bible §8.1). Knob chiều dài/tốc độ/gia tốc = giá trị GDD × 30/13; khoảng an toàn
nhân cùng hệ số. Knob thời gian và hệ số nhân (`fall_gravity_mult`, `apex_hang_mult`, `jump_cut_mult`, `attack_move_mult`,
`dash_attack_carry`, `dash_attack_decay`, modifier, cửa sổ đệm) không đổi.

| Knob / đại lượng | GDD (prototype) | Runtime (tile) |
|---|---|---|
| `jump_speed` | 13 u/s | 30 u/s |
| `double_jump_speed` | 12 u/s | 27,69 u/s |
| `gravity_scale` (g) | 3,2 (31,39 u/s²) | 7,385 (72,44 u/s²) |
| `max_fall_speed` / `plunge_speed` | 16 / 18 u/s | 36,92 / 41,54 u/s |
| `apex_hang_threshold` | 1,5 u/s | 3,46 u/s |
| `move_speed` / `dodge_speed` | 6,5 / 14 u/s | 15 / 32,31 u/s |
| `collider_w` × `collider_h` | 0,9 × 1,3 u | 2,077 × 3,0 u (≈ 33 × 48 px) |
| `snap_min` / `skin` / `corner_correction` / `depen_max` | 0,10 / 0,01 / 0,15 / 1,0 u | 0,231 / 0,023 / 0,346 / 2,31 u |
| Đỉnh nhảy (F1) / nhảy + nhảy đôi (F2) | 2,727 / 5,056 u | 6,293 / 11,668 u |
| Quãng né (F4) / tầm chuỗi chuẩn (F3) | 2,8 / 11,58 u | 6,46 / 26,72 u |
| Ràng buộc level: bậc tối đa / trần hang / one-way chồng nhau | 2,2 / 5 / 1,5 u | 5,08 / 11,54 / 3,46 u (≈ 5 / 12 / 4 tile) |

### Knob ảnh hưởng lẫn nhau

- `jump_speed` ↔ `gravity_scale` ↔ `double_jump_speed`: cả ba quyết `h_apex`, `h_dj`; phải giữ validator 1, 3,
  4, 5. Đổi một knob → tính lại F1–F3 và báo level design.
- `fall_gravity_mult` ↔ `max_fall_speed` ↔ `plunge_speed`: validator 2.
- `dodge_speed` ↔ `dodge_duration` ↔ `dodge_cooldown` ↔ `move_speed`: validator 6 (né không nhanh hơn chạy);
  thay đổi `T_cycle` phải báo Combat (validator 12).
- Cửa sổ đệm ↔ `buffer_scale` ↔ Input `buffer_retention`, `hitstop_budget`: validator 8.
- `apex_hang_mult` ↔ `apex_hang_max_time`: validator 7.

### Knob thuộc hệ khác (Controller chỉ tham chiếu)

- Nhịp đòn từng vũ khí (`windup`, `active`, `recovery`, `cancel_lockout`, `string_length`) → GDD Weapon Moveset
  (bộ số dự phòng ở Rule 12).
- `stun_time`, knockback, hit-stop, bất tử sau khi trúng đòn (0,6 s, C-4) → GDD Combat.
- Góc "Xuống", dead zone, `buffer_retention` → GDD Input.

## Visual/Audio Requirements

*`art-director` và `audio-director` chưa được hỏi cho section này (category Core, không bắt buộc). Cần họ
xem lại trước `/asset-spec`.*

Controller không tự vẽ hay phát âm thanh; nó phát sự kiện (Rule 26) và Visual/Audio thể hiện. Hiện thực (2026-11-02, người dùng):
**Animator của Unity**. Driver chỉ đặt tham số (`Speed`, `Running`, `Grounded`, `VelocityY`, `Action`, `AttackProgress`,
trigger `Land`/`Bonk`); transition trong Animator Controller chọn clip; clip đánh chạy theo `AttackProgress` (motion time) nên
khớp `windup/active/recovery` của vũ khí. **Đổi vũ khí đổi toàn bộ sprite** (look = Animator Override Controller), giữ nguyên
clip và tiến độ đang chạy. Hit-stop và pause làm chậm Animator qua `Time.timeScale` do frame driver ghi (ADR-0001). Yêu cầu:

| Sự kiện / trạng thái | Visual cần có | Audio cần có |
|---|---|---|
| Idle, Walk, Run, Rise, Fall, apex | bộ animation tối thiểu; Walk khi đi bộ, Run khi bật Run (C-13), thiếu một trong hai thì dùng clip còn lại; animation chạy không được làm chậm cảm giác tốc độ tức thì (C-1) | bước chân theo bề mặt (đọc loại bề mặt từ va chạm) |
| `Jumped(Ground / Coyote / Air)` | cất cánh; nhảy đôi có dấu hiệu riêng (vệt, vòng gió) | tiếng nhảy; nhảy đôi khác tiếng |
| `Landed(fall_speed)` | bụi đáp, mạnh nhẹ theo `fall_speed` | tiếng đáp theo `fall_speed` |
| `DodgeStarted/Ended`, `IFrameStarted/Ended` | vệt mờ hoặc đổi độ trong suốt **khớp từng frame với cờ i-frame** (lệch 0 frame, AC-VI03); có dấu hiệu rõ lúc i-frame tắt | tiếng lướt |
| `DodgeReady` | tín hiệu rất nhẹ | tiếng "tick" rất nhỏ (tùy chọn) |
| `AirResourcesChanged` | **chỉ báo ngay trên thân nhân vật** (không ở HUD): còn nhảy đôi / còn né trên không (AC-VI04) | — |
| `PlungeLanded` | sóng xung kích (Combat sở hữu sát thương; hình ảnh cùng nhịp) | tiếng nện |
| `Bonked` | đầu khựng nhẹ | tiếng cụng nhỏ |
| `HitstunStarted/Ended` | tư thế choáng, nhấp nháy khi có bất tử của Combat | do Combat |
| Bị từ chối (`Rejected`) | không có (chỉ debug overlay) | né bị từ chối vì hồi chiêu: "tick" nhỏ tùy chọn; nhảy bị từ chối: im lặng |

- Nhảy coyote **không** có phản hồi riêng (Rule 7).
- Tư thế đánh của từng vũ khí thuộc Weapon Moveset; Controller chỉ cung cấp ngữ cảnh và pha.
- Phong cách pixel và bảng màu theo art bible (`design/art/art-bible.md`).

> 📌 **Asset Spec** — Visual/Audio requirements are defined. After the art bible is approved, run
> `/asset-spec system:character-controller` to produce per-asset visual descriptions, dimensions, and
> generation prompts from this section.

## Game Feel

Mục tiêu cảm giác là Player Fantasy "Nhẹ chân, nặng tay", đo bằng các con số sau:

- **Phản hồi:** Input thêm 0 frame (GDD Input); Controller tiêu thụ lần bấm và đổi trạng thái **trong cùng
  frame** nó được phép (Rule 14, 30). Lần bấm hợp lệ trong hit-stop ra ở frame đầu tiên sau hit-stop (AC-X01).
- **Nhịp:** đòn trên đất ~0,20 s (P); nhảy giữa đòn ở mọi pha; hủy sang né sau 0,03 s recovery (P).
- **Đường bay:** lên 0,454 s, rơi 0,330 s (rơi ×1,6), treo ở đỉnh tối đa ~0,04 s; tổng 0,784 s, gần Hollow
  Knight; đường bay đọc được trước khi bấm (F1, binding test 5).
- **Dừng:** vận tốc tức thì, quãng trượt khi thả phím 0 u (yêu cầu playtest ≤ 0,13 u trong 95% lần dừng, AC-T07).
- **Trọng lượng** đến từ hit-stop, knockback, âm thanh, phản ứng của quái (Combat), **không** từ khóa chân. Vũ khí
  không đổi cơ thể (binding test 1).
- **Dễ tha thứ:** coyote 0,10 s có bù độ cao; cửa sổ đệm theo đồng hồ Game; bắt góc trần 0,15 u; bám đất khi
  xuống dốc.
- **Cần playtest lại** (lệch so với prototype đã PROCEED): rơi ×1,6 (prototype ×1,0), chu kỳ né 0,55 s (prototype
  0,35 s), treo ở đỉnh (prototype không có), nhảy đôi và né trên không (mới).

## UI Requirements

Controller không sở hữu màn hình nào.

| Phần tử | Chủ | Controller cung cấp | Tier |
|---|---|---|---|
| Chỉ báo lượt nhảy đôi / né trên không (trên thân nhân vật, không phải HUD) | Visual nhân vật | `AirJumpsLeft`, `AirDodgesLeft`, `AirResourcesChanged` | MVP (Fun-Gate) |
| Hồi chiêu né trên HUD (tùy chọn; mặc định không hiện) | HUD | trạng thái hồi chiêu, `DodgeReady` | MVP (nếu cần) |
| Tùy chọn trợ năng: nới cửa sổ, nới coyote, luôn nhảy hết độ cao, giữ Xuống để rơi xuyên, kéo dài i-frame | Settings & Accessibility | hook Rule 29 | Vertical Slice |
| Debug overlay (cờ i-frame, lượt trên không, lý do từ chối) | Công cụ dev | H-13, H-14 | Development build |

> **📌 UX Flag — Character Controller**: This system has UI requirements. In Phase 4 (Pre-Production), run
> `/ux-design` for the on-body air-resource indicator and the accessibility options it feeds **before**
> writing epics. Stories that reference UI should cite `design/ux/[screen].md`, not the GDD directly.

## Cross-References

| GDD / tài liệu | Phần được tham chiếu | Tham chiếu ở đâu | Trạng thái |
|---|---|---|---|
| `design/gdd/game-concept.md` | Core Loop › Moment-to-Moment (nhịp HK-or-faster, di chuyển khi chém, nhảy giữa đòn, hủy sang né, không khóa đòn đánh, trọng lượng từ hit-stop); Pillar 2, 3, 4; Technical Considerations ("kinematic 2D controller tự viết") | Overview, Player Fantasy, Rule 1–30, Game Feel | Approved |
| `design/gdd/input.md` | Rule 4 (tổ hợp, `baseAction`), 5 (bản ghi, `move`), 7–8 (đăng ký cửa sổ, `TryConsume`, `WasReleasedAfter`, `ReadMove`), 11–12 (chế độ, rào), F2 (`buffer_retention`, `hitstop_budget`), F3 (góc Xuống), OQ-I12 | Rule 6, 9, 15, 17, 18, 27, 28; F7 | Designed |
| `design/gdd/systems-index.md` | Character Controller (#6), TD-A3 (Controller sở hữu máy trạng thái), TD-C (không singleton tĩnh), TD-B4 (ID ổn định) | Overview, Rule 30, Dependencies | — |
| `design/gdd/game-time-calendar.md` | — (không dùng đồng hồ lịch) | Interactions | Approved |
| `prototypes/combat-feel-concept/REPORT.md` + `CombatFeelPlayer.cs` | Mọi số (P): tốc độ, nhảy, trọng lực, nhịp đòn, né, hitstun, knockback, plunge, land recovery; bài học "nặng hơn HK" bị bác | Rule 1–19, Formulas, Tuning Knobs | PROCEED |
| `prototypes/day-loop-concept/` (map sinh) | Dốc ≤ 45°, platform một chiều ~2,1 u, bậc landmark ≤ 2,2 u, trần hang ≥ 5 u, tường bờ nước | Rule 21–23, F2, F6, Edge Cases | Chưa đóng |
| `design/gdd/skill-moveset.md` | Kỹ năng dữ liệu, bảng tổ hợp, node chuyển động có giới hạn, kỹ năng dự phòng, cửa sổ Special | Rule 12, 14, 26, 28; validator 8, 10, 11, 13; binding test 1 | Designed 2026-11-05 |
| GDD Combat *(chưa viết)* | `HitReceived`, `stun_time` 0,25, knockback 7, bất tử 0,6 s (C-4), HP hiện tại, hit-stop, đọc cờ i-frame, validator 12 | Rule 19–20, Interactions | — |
| GDD Defeat & Retry, Region Loading, Camera, Elemental & Status *(chưa viết)* | `EnterDefeated`, `Respawn`, `FellOutOfWorld`, `kill_y`, `Teleport`, modifier | Rule 23–25 | — |

## Acceptance Criteria

*Nguồn: qa-lead, 2026-10-31 (84 tiêu chí). 13 chỗ sửa chữ qa-lead đề xuất đã áp thẳng vào Section C/D/G
(FIX-1 chia bước con ở ngưỡng treo; FIX-2 bộ đếm theo tick; FIX-3 ví dụ F5; FIX-4 `collider_w`; FIX-5 khoảng
an toàn khớp validator; FIX-6 `iframe_scale`; FIX-7 chuyển trạng thái chung; FIX-8 `EnterDefeated`; FIX-9 số
validator; FIX-10 `Δy` của F9; FIX-11 ngân sách CPU → Open Questions; FIX-12 `kill_y`; FIX-13
`AirResourcesChanged`). Gate dùng mức mặc định vì `project.yaml` chưa đặt `testing.strict`.*

**Quy ước:** test lõi ở `Assets/Tests/EditMode/CharacterController/`; test adapter và kiểm chứng giả định
Physics2D ở `Assets/Tests/PlayMode/CharacterController/`. Test EditMode dùng đồng hồ Game inject (H-1), bộ đệm
input giả (H-2), thế giới va chạm giả dựng từ hình học giải tích (H-3); bước mặc định 1/60 s; giá trị mặc định
từ fixture cấu hình (H-7). Giá trị kỳ vọng tính bằng hàm thuần F1–F9 trên fixture; con số ghi ở đây là kết quả
với fixture mặc định. **Dung sai:** vị trí ±0,01 u; đỉnh nhảy có treo ±0,03 u; thời gian ±1 frame; vận tốc
±1e-3 u/s. `commands.test` hiện chỉ chạy EditMode (Open Questions).

### H.1 Luật lõi

- **GIVEN** Grounded trên đất bằng, đang bật Run, **WHEN** `move.x` đổi 1 → 0 ở bước N rồi −1 ở bước N+1, **THEN** `vx` = `move_speed` ở N−1, `vx` = 0 và Δx = 0 ở N, `vx` = −`move_speed` ở N+1. (AC-R01) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Grounded, `move.x` = 1, **WHEN** chưa bấm Run, rồi nhấn Run, rồi nhả Run và chờ 0,5 s, rồi nhấn Run lần nữa, **THEN** `vx` = `walk_speed`, rồi `move_speed` (ngay bước nhấn), vẫn `move_speed`, rồi `walk_speed`; công tắc giữ nguyên qua dừng, nhảy và hitstun, và một lần nhấn trong hitstun vẫn đổi được; `walk_speed` ngoài 3–6 u/s về mặc định kèm 1 lỗi. (AC-R01a, C-13) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Airborne, **WHEN** `move.x` đổi 1 → −1, **THEN** `vx` = −`move_speed` ngay trong bước đó. (AC-R02) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang bay, không giữ Jump, **WHEN** tích phân, **THEN** Δvy mỗi bước = −g·dt khi `vy` > 0 và −1,6·g·dt khi `vy` < 0; `vy` ≥ −16 trừ khi Plunge (`vy` = −18). (AC-R03) `[Unit/EditMode — BLOCKING]`
- **GIVEN** giữ Jump sau Jump, **WHEN** qua vùng `|vy|` < 1,5, **THEN** tổng thời gian treo ≤ 0,08 s; thời gian treo = 0 sau né, knockback hoặc rơi khỏi mép; khi bật "luôn nhảy hết độ cao" thì vẫn treo mà không cần giữ Jump. (AC-R04) `[Unit/EditMode — BLOCKING]`
- **GIVEN** API khởi tạo Controller, **WHEN** chạy cùng một kịch bản input hai lần (gắn nhãn làng và arena), **THEN** API không có tham số vùng/chế độ và hai trace trạng thái giống nhau từng bit. (AC-R05) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang chạy lên dốc 45°, **WHEN** bấm Jump, **THEN** `vy` = `jump_speed` đúng bằng ở bước nhảy, và độ cao đỉnh so với điểm bật bằng trên đất bằng. (AC-R06) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang lên sau Jump, **WHEN** nhả Jump hai lần (lần hai giữa hit-stop, scale 0,05), **THEN** `vy` nhân 0,5 đúng một lần; nếu lần nhả đầu giữa hit-stop thì vẫn được tính. (AC-R07) `[Unit/EditMode — BLOCKING]`
- **GIVEN** rời mép không nhảy, **WHEN** bấm Jump ở t = `t_coy` và ở `t_coy` + 1 tick, **THEN** lần đầu cho `Jumped(Coyote)` và lượt nhảy đôi giữ nguyên; lần sau cho `Jumped(Air)`. (AC-R08) `[Unit/EditMode — BLOCKING]`
- **GIVEN** còn lượt nhảy đôi, **WHEN** nhảy đôi lúc `vy` = +8 và lúc `vy` = −10, **THEN** cả hai lần `vy` = 12 và độ cao tăng thêm bằng nhau; rơi khỏi mép sau khi hết coyote vẫn còn lượt. (AC-R09) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Grounded, `move.x` = 0, `speed_mult` 0,5, **WHEN** bấm Dodge, **THEN** né theo hướng mặt trong 12 frame, Δx = 2,8, `vy` = 0, cờ i-frame bật cả 12 bước, `IFrameStarted`/`Ended` thành cặp. (AC-R10) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Airborne, **WHEN** chạy chuỗi nhảy → nhảy đôi → né và chuỗi nhảy → né → nhảy đôi, rồi bấm né lần hai, **THEN** cả hai chuỗi thành công; né trên không có Δy = 0; sau né `vy` bắt đầu từ 0; lần né thứ hai bị từ chối với lý do `NoAirDodge`. (AC-R11) `[Unit/EditMode — BLOCKING]`
- **GIVEN** giữ spam Dodge mỗi frame, **WHEN** chạy 3 chu kỳ, **THEN** `DodgeStarted` ở frame 0, 33, 66; `DodgeReady` ở 32, 65; né trên đất và trên không dùng chung hồi chiêu. (AC-R12) `[Unit/EditMode — BLOCKING]`
- *(Chuyển sang AC của Skill & Moveset 2026-11-05 — giữ để tra cứu)* **GIVEN** fixture vũ khí, **WHEN** đánh chuỗi trên đất, **THEN** `AttackPhaseChanged` khớp `windup`/`active`/`recovery`; chỉ số đòn về 0 sau `combo_reset` không có Attack, và khi Hitstun, Teleport, Respawn, Defeated, đổi vũ khí; hủy bằng né hoặc nhảy thì giữ chỉ số. (AC-R13) `[Unit/EditMode — BLOCKING]`
- *(Chuyển sang AC của Skill & Moveset 2026-11-05 — giữ để tra cứu)* **GIVEN** đòn đất có `cooldown` 0,30 s (đòn 0,20 s), **WHEN** bấm Attack trong recovery rồi bấm lại ở 0,47 s và 0,52 s, **THEN** không nối đòn trong recovery, không có đòn trước 0,50 s, đòn ở 0,52 s là đòn 1 của chuỗi; cooldown đòn đất không chặn đòn trên không; `cooldown` của đòn trên không và đòn khi lướt chặn đòn cùng loại kế tiếp; `cooldown` âm hoặc > 2 s → dữ liệu vũ khí không hợp lệ, dùng số dự phòng kèm 1 lỗi. (AC-R13a) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đòn đang chạy, **WHEN** đổi vũ khí, hoặc nạp dữ liệu vũ khí lỗi, **THEN** đòn đang chạy dùng bộ số đã chụp; dữ liệu lỗi dùng bộ số dự phòng, 1 log lỗi mỗi cặp (vũ khí, ngữ cảnh), di chuyển không bị chặn. (AC-R14) `[Unit/EditMode — BLOCKING]`
- *(Chuyển sang AC của Skill & Moveset 2026-11-05 — giữ để tra cứu)* **GIVEN** AirAttack, **WHEN** chạm đất giữa đòn, **THEN** đòn bị hủy ở bước có `Landed`, trạng thái Grounded, không lockout. (AC-R15) `[Unit/EditMode — BLOCKING]`
- *(Chuyển sang AC của Skill & Moveset 2026-11-05 — giữ để tra cứu)* **GIVEN** đang Dodge, **WHEN** Attack được tiêu thụ, **THEN** vào DashAttack, `IFrameEnded` cùng bước, `vx` đầu = 11,2. (AC-R16) `[Unit/EditMode — BLOCKING]`
- *(Chuyển sang AC của Skill & Moveset 2026-11-05 — giữ để tra cứu)* **GIVEN** Airborne, **WHEN** Plunge, **THEN** `vx` = 0, `vy` = −18, `PlungeLanded(pos)` đúng 1 lần, LandRecovery 0,16 s. (AC-R17) `[Unit/EditMode — BLOCKING]`
- *(Chuyển sang AC của Skill & Moveset 2026-11-05 — giữ để tra cứu)* **GIVEN** hai fixture vũ khí có nhịp khác nhau, **WHEN** giữ hướng khi đánh, **THEN** hệ số di chuyển ×0,6 ở GroundAttack và LandRecovery, ×1,0 ở AirAttack, giống hệt nhau giữa hai vũ khí. (AC-R18) `[Unit/EditMode — BLOCKING]`
- *(Chuyển sang AC của Skill & Moveset 2026-11-05 — giữ để tra cứu)* **GIVEN** GroundAttack, **WHEN** bấm Jump ở từng pha, và bấm Dodge trước `recovery_start + cancel_lockout`, **THEN** `Jumped` ra cùng bước và hệ số đổi sang ×1,0; Dodge ra đúng frame đầu tiên được phép; Plunge hủy được bằng né trên không; LandRecovery hủy được ở 0,08 s. (AC-R19) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Jump (`seq` nhỏ) và Dodge cùng bước, **WHEN** chạy bước đó, **THEN** bước N có `Jumped`, bước N+1 có `DodgeStarted(air)`; thứ tự ngược: né trước, Jump hết hạn với log `WrongState`; kịch bản cố định 10 000 bước: mỗi bước bắt đầu ≤ 1 hành động. (AC-R20) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang đánh, **WHEN** giữ hướng ngược lại, **THEN** lùi ×0,6, hướng mặt và hitbox không lật, 0 `FacingChanged`; đòn kế quay theo `move` của bản ghi; dash attack quay theo hướng né. (AC-R21) `[Unit/EditMode — BLOCKING]`
- **GIVEN** bản ghi Plunge có `baseAction` = Attack, **WHEN** tiêu thụ trên đất (kể cả Plunge bấm trên không còn trong bộ đệm khi đã chạm đất), **THEN** ra đòn trên đất. (AC-R22) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đứng trên platform một chiều / vắt giữa một chiều và đất cứng / đất cứng / trên không, **WHEN** DropThrough, **THEN** lần lượt: rơi xuyên (bỏ qua đúng collider đó tới khi chân thấp hơn mặt platform và đã qua ≥ 0,12 s) / Jump / Jump / nhảy đôi; chỉ giữ Xuống 600 frame thì 0 lần rơi xuyên. (AC-R23) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Grounded, **WHEN** nhận `HitReceived(kb, 0,25)`, **THEN** vận tốc = kb, Hitstun 15 frame, mọi hành động bị từ chối với `Hitstun`, bản ghi trong bộ đệm còn nguyên. (AC-R24) `[Unit/EditMode — BLOCKING]`
- **GIVEN** vừa nhận `HitReceived`, **WHEN** nhận `HitReceived` thứ hai sau 0,1 s, **THEN** Controller vẫn áp lần hai (lọc bất tử là việc của Combat), không có bộ đếm bất tử riêng. (AC-R25) `[Unit/EditMode — BLOCKING]`
- **GIVEN** dốc 30° và 45°, **WHEN** đứng yên 600 frame, chạy lên và xuống, **THEN** đứng yên trôi ≤ 0,001 u; `|vx|` = `move_speed`; chạy xuống 45° có 0 `LeftGround`; mặt 51° là tường. (AC-R26) `[Unit/EditMode — BLOCKING]`
- **GIVEN** chạy lên dốc 45°, **WHEN** Jump, **THEN** snap tắt tới khi `jump_rising` tắt (đỉnh bằng trên đất bằng); chạy lên dốc không nhảy thì vẫn snap dù `vy` > 0. (AC-R27) `[Unit/EditMode — BLOCKING]`
- **GIVEN** platform một chiều, **WHEN** nhảy từ dưới xuyên lên, **THEN** 0 `Landed` khi đang lên; chỉ đáp khi `vy` ≤ 0 và chân ở trên mặt platform từ trước bước đó. (AC-R28) `[Unit/EditMode — BLOCKING]`
- **GIVEN** trần, **WHEN** va thẳng / mép đầu chạm mép trần với chồng ≤ 0,15 u (vị trí mới trống) / như vậy nhưng bị chặn, **THEN** lần lượt: `Bonked`, `vy` = min(`vy`, 0), `vx` giữ, bước sau `vy` < 0 / đẩy ngang rồi bay tiếp / như va trần; hang 5 u: nhảy đơn 0 `Bonked`, nhảy đôi có `Bonked`. (AC-R29) `[Unit/EditMode — BLOCKING]`
- **GIVEN** tường, **WHEN** lao vào khi đang rơi, **THEN** `vx` = 0, `vy` tiếp tục giảm (không bám tường); rơi dưới `kill_y`: `FellOutOfWorld` đúng 1 lần, `last_safe_ground` là điểm Grounded cuối. (AC-R30) `[Unit/EditMode — BLOCKING]`
- **GIVEN** khóa `Full` (A) và `MoveOnly` (B), **WHEN** gỡ bằng handle sai, gỡ B, gỡ A, và chủ một khóa chết (H-9), **THEN** gỡ sai trả `false`; khóa hiệu dụng `Full` tới khi gỡ A; MoveOnly cho đánh; khóa của chủ chết tự gỡ bước kế với 1 log; gỡ khóa cuối → Grounded/Airborne theo tiếp xúc. (AC-R31) `[Unit/EditMode — BLOCKING]`
- **GIVEN** trên không và còn Hitstun, **WHEN** `Teleport` rồi `Respawn`, **THEN** Teleport hủy hành động, vận tốc = 0, hồi hai lượt, `Teleported` đúng 1 lần; Respawn như Teleport, thêm xóa Hitstun, xóa khóa của chính Controller, về Idle; khóa của hệ khác giữ nguyên. (AC-R32) `[Unit/EditMode — BLOCKING]`
- **GIVEN** modifier root, **WHEN** ở giữa cú nhảy, bấm Jump, Dodge, Attack, **THEN** `vx` = 0, `vy` vẫn rơi, nhả nút vẫn cắt được nhảy; Jump và Dodge bị từ chối với `Rooted`; Attack ra. (AC-R33) `[Unit/EditMode — BLOCKING]`
- **GIVEN** kịch bản phủ mọi sự kiện của Rule 26, **WHEN** chạy, **THEN** mỗi loại sự kiện phát ≥ 1 lần với payload đúng, và `AirJumpsLeft`/`AirDodgesLeft` khớp snapshot ở mọi bước. (AC-R34) `[Unit/EditMode — BLOCKING]`
- **GIVEN** 10 kịch bản, mỗi kịch bản sinh một lý do từ chối, **WHEN** bản ghi hết hạn hoặc bị rào xóa, **THEN** sink (H-6) nhận đúng 1 dòng `{seq, action, reason, state, t_press, t_expire}` với lý do mới nhất. (AC-R35) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Input giả ghi lại đăng ký, **WHEN** Controller khởi tạo, **THEN** cửa sổ là Jump 0,12, DropThrough 0,12, Attack 0,20, Dodge 0,15, Plunge 0,15 s, tất cả theo đồng hồ Game. (AC-R36) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Settings đẩy `buffer_scale` 2, `coyote_scale` 2, "luôn nhảy hết độ cao", `drop_through_hold` 0,15, `iframe_scale` 1,5, **WHEN** đo, **THEN** mọi cửa sổ và coyote ×2; không còn cắt nhảy; DropThrough chỉ có hiệu lực sau 0,15 s giữ Xuống (nhả sớm ra Jump); i-frame kéo dài 0,30 s, `IFrameEnded` sau `DodgeEnded` 0,10 s. (AC-R37) `[Unit/EditMode — BLOCKING]`
- **GIVEN** asmdef lõi, **WHEN** quét tham chiếu và tạo 2 instance trong cùng test, **THEN** lõi không tham chiếu `UnityEngine.Physics2D` hay singleton tĩnh nào, hai instance độc lập. (AC-R38) `[Unit/EditMode — BLOCKING]`
- **GIVEN** cú bay không treo, **WHEN** so `y` từng bước với nghiệm dạng đóng, **THEN** lệch ≤ 1e-4 u (tích phân chính xác, Rule 30). (AC-R39) `[Unit/EditMode — BLOCKING]`
- **GIVEN** bước con mà cú né hết đúng trong bước đó và có Attack trong bộ đệm, **WHEN** chạy, **THEN** ra đòn thường, không phải dash attack (thứ tự thời gian → input → tích phân → tiếp xúc). (AC-R40) `[Unit/EditMode — BLOCKING]`

### H.2 Trạng thái

- **GIVEN** bảng States dưới dạng dữ liệu (H-10), **WHEN** test theo bảng bơm từng hành động vào từng trạng thái, **THEN** chấp nhận/từ chối khớp từng ô; mọi lần từ chối có lý do. (AC-S01) `[Unit/EditMode — BLOCKING]`
- **GIVEN** mọi trạng thái trừ Hitstun, ControlLocked (Full), Defeated, **WHEN** duyệt bảng và chạy runtime, **THEN** mỗi trạng thái có ≥ 1 Attack hoặc Dodge được chấp nhận trong lúc nó kéo dài. (AC-S02) `[Unit/EditMode — BLOCKING]`
- **GIVEN** root ở từng trạng thái, **WHEN** bơm Move, Jump, Dodge, Attack, **THEN** ba hành động đầu bị từ chối với `Rooted`; Attack giữ đúng như ô của bảng. (AC-S03) `[Unit/EditMode — BLOCKING]`
- **GIVEN** kịch bản cố định 10 000 bước, **WHEN** ghi `StateChanged(from, to)`, **THEN** mọi cặp nằm trong cột "Chuyển sang" hoặc trong các chuyển trạng thái chung. (AC-S04) `[Unit/EditMode — BLOCKING]`

### H.3 Công thức

- **GIVEN** đất bằng, giữ Jump suốt, **WHEN** nhảy, **THEN** đỉnh 2,727 ±0,03 u, từ `Jumped` tới `Landed` 47 ±1 frame (0,784 s). (AC-F01) `[Unit/EditMode — BLOCKING]`
- **GIVEN** nhả ở frame đầu / ở t_r = 0,2 s, **WHEN** nhảy, **THEN** đỉnh lần lượt 0,673 / 2,152 ±0,01 u; nhả frame đầu bay 0,371 s ±1 frame. (AC-F02) `[Unit/EditMode — BLOCKING]`
- **GIVEN** nhảy đôi ở đỉnh cú nhảy đầu, **WHEN** đo, **THEN** `h_dj` = 2,329 ±0,03, `H_max` = 5,056 ±0,04, đỉnh đầu 6,356 ±0,04 u. (AC-F03) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đất bằng vô hạn, giữ hướng, **WHEN** nhảy đơn / nhảy rồi né ở đỉnh / chuỗi chuẩn F3, **THEN** Δx lần lượt 5,10 ±0,11 / 7,90 ±0,11 / 11,58 ±0,15 u. (AC-F04) `[Unit/EditMode — BLOCKING]`
- **GIVEN** spam né 10 chu kỳ, **WHEN** đo, **THEN** `T_cycle` = 33 frame, `f_inv` = 12/33 (36,4%), `v_spam` = 5,09 ±0,05 u/s < `move_speed`. (AC-F05) `[Unit/EditMode — BLOCKING]`
- **GIVEN** modifier 0,7×0,8 / 0,5³ / 2,0 / nhảy 0,5 / nhảy 2,0, **WHEN** chạy, chém trên đất và nhảy, **THEN** lần lượt: chạy 3,64, chém 2,184 u/s / kẹp 0,3 → 1,95 u/s / kẹp 1,5 → 9,75 u/s / `v_j` = 9,19, đỉnh 1,38 ±0,03 / kẹp 1,2 → đỉnh 3,27 ±0,03. (AC-F06) `[Unit/EditMode — BLOCKING]`
- **GIVEN** dt = 1/60, **WHEN** tính snap, **THEN** chạy 0,139 ±0,001, né 0,288 ±0,001; qua đỉnh dốc lồi 45° cả chạy lẫn né 0 `LeftGround`; lên dốc 45° `vy_slope` = 6,5. (AC-F07) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `buffer_scale` 2, **WHEN** tính F7 cho Attack, **THEN** 0,5995 s ±1e-4 ≤ `buffer_retention`. (AC-F08) `[Unit/EditMode — BLOCKING]`
- **GIVEN** dash attack không giữ hướng / giữ hướng tiến, **WHEN** đo, **THEN** quán tính đi 0,747 ±0,01 u, tắt sau 8 ±1 frame / chuyển sang 3,9 u/s sau 0,656 ±0,01 u, `|Δvx|` giữa hai frame ≤ 1,4 u/s. (AC-F09) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `coyote_scale` 2 và 1, **WHEN** nhảy coyote ở cuối cửa sổ, **THEN** Δy = 1,00 ±0,02, `v_coy` = 15,2 ±0,05 (scale 2); cả hai trường hợp đỉnh = mép + 2,727 ±0,03 u. (AC-F10) `[Unit/EditMode — BLOCKING]`

### H.4 Validator (hàm thuần, H-7)

- **GIVEN** mỗi fixture vừa vượt biên một ràng buộc — (1) `double_jump_speed` = `jump_speed`; (2) Plunge 17,5 với rơi 16; (3) `jump_speed` 14 với `j_hi` 1,2; (4) `jump_speed` 12,4, rồi `double_jump_speed` 11,7; (5) `modifier_jump_min` 0,4; (6) hồi chiêu 0,23; (7) treo 0,10 × (1 − 0,4); (8) cửa sổ 0,41; (9) `corner_correction` 0,23 với `collider_w` 0,9; (10) lockout 0,01; (11) trường `move_mult` trong dữ liệu vũ khí; (13) `cancel_lockout` = `recovery` — **WHEN** validate, **THEN** mỗi fixture bị từ chối với đúng ID ràng buộc; fixture nằm đúng biên (6: 0,24; 8: 0,40; 2: 17,6) thì đạt. (AC-V01) `[Unit/EditMode — BLOCKING]`
- **GIVEN** knob ngoài khoảng an toàn / vi phạm ràng buộc chéo, **WHEN** nạp, **THEN** chỉ knob đó về mặc định + 1 log lỗi / mọi knob tham gia về mặc định, knob khác giữ nguyên. (AC-V02) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `cancel_lockout` ≥ `recovery` lọt tới runtime / pattern Combat có khoảng giữa hai làn đạn 0,5 s, **WHEN** chạy hoặc nạp, **THEN** lockout bị kẹp về `recovery − 1/60` / 1 cảnh báo, việc nạp vẫn thành công (validator 12 chỉ thông báo). (AC-V03) `[Unit/EditMode — BLOCKING]`

### H.5 Edge Cases

- **GIVEN** né trên đất lướt qua mép và kết thúc trên không, **WHEN** bấm Dodge, rồi Jump trong 0,10 s sau `DodgeEnded`, **THEN** lượt né trên không = 0; Jump ra `Jumped(Coyote)` (C-9). (AC-E01) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang né hoặc dash attack, **WHEN** bị root, **THEN** cùng bước có `DodgeEnded` + `IFrameEnded`, quán tính = 0 (C-10). (AC-E02) `[Unit/EditMode — BLOCKING]`
- **GIVEN** né đã chạy 0,05 s / GroundAttack, **WHEN** có yêu cầu khóa, **THEN** khóa có hiệu lực ở bước cú né kết thúc kèm đúng 1 `ControlLockApplied` / đòn bị hủy ngay (C-11); khóa khi đang trên không thì vẫn rơi. (AC-E03) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Hitstun 0,25 s, **WHEN** bấm Dodge ở 0,12 s / 0,05 s, **THEN** né ra ở bước đầu tiên sau `HitstunEnded` / bản ghi hết hạn với log `Hitstun`. (AC-E04) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Jump bấm 1 frame trước bước chạm đất khi còn lượt / Jump bấm đúng frame Plunge chạm đất, **WHEN** chạy, **THEN** ra nhảy đôi / ra nhảy từ đất ở 0,08 s (5 frame). (AC-E05) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang lên, trần cách 0,3 u, **WHEN** một frame có dt 1,5 s, **THEN** chỉ mô phỏng 0,10 s qua 3 bước con ≤ 1/30 s, không xuyên trần, có `Bonked`. (AC-E06) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Teleport vào trong tường sâu 0,6 u / 1,5 u / không có `last_safe_ground` hợp lệ, **WHEN** depenetration chạy, **THEN** đẩy ra theo hướng ngắn nhất ≤ `depen_max` / về `last_safe_ground` / 1 log lỗi. (AC-E07) `[Unit/EditMode — BLOCKING]`
- **GIVEN** hai platform một chiều chồng nhau cách 1,5 u, **WHEN** DropThrough từ platform trên, **THEN** đáp xuống platform dưới. (AC-E08) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đã dùng cả hai lượt, **WHEN** bị knockback hất lên / đáp xuống platform một chiều / nhảy xuyên lên qua platform một chiều, **THEN** hồi 0 lượt, không coyote / hồi cả hai / hồi 0. (AC-E09) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang có i-frame / đang Hitstun, **WHEN** `EnterDefeated` / rơi khỏi thế giới, **THEN** vào Defeated kèm `IFrameEnded` / `FellOutOfWorld` đúng 1 lần, mô phỏng dừng tới khi có Teleport hoặc Respawn. (AC-E10) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang trên không, **WHEN** đồng hồ Game đứng 2 s (menu), **THEN** coyote, hồi chiêu, `vy` giữ nguyên; khi chạy lại tiếp tục từ đúng giá trị đó. (AC-E11) `[Unit/EditMode — BLOCKING]`
- **GIVEN** khóa của hệ khác đang giữ, **WHEN** Respawn, **THEN** khóa vẫn giữ; development build có 1 cảnh báo khi khóa sống quá 5 s sau Respawn. (AC-E12) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đứng vắt giữa một chiều và đất cứng / đang né mà va trần, **WHEN** đáp / lướt, **THEN** `Landed` đúng 1 lần / 0 `Bonked`. (AC-E13) `[Unit/EditMode — BLOCKING]`

### H.6 Liên hệ với hệ khác và giả định Physics2D

- **GIVEN** Input thật + Controller, `timeScale` 0,05 trong 0,21 s, **WHEN** bấm Dodge giữa hit-stop, **THEN** `DodgeStarted` ở frame đầu tiên sau hit-stop; thứ tự trong frame Input → đồng hồ → Controller (binding test 3). (AC-X01) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** Grounded, **WHEN** Xuống + Attack, **THEN** ra đòn trên đất qua `baseAction`; sau đó `TryConsume(Plunge)` của Input trả "không có". (AC-X02) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** kênh Telemetry, **WHEN** một bản ghi hết hạn chưa tiêu thụ, **THEN** Telemetry nhận dòng log từ chối đủ trường để phân loại theo Input AC-T04. (AC-X03) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** Combat gửi `HitReceived` với `stun` 0,25 / trúng đòn giữa cú né, **WHEN** chạy, **THEN** `HitstunStarted` và `HitstunEnded` cách nhau 15 frame / Combat đọc cờ i-frame theo cùng đồng hồ và bỏ qua đòn. (AC-X04) `[Integration/PlayMode — BLOCKING]`
- *(Chuyển sang AC của Skill & Moveset 2026-11-05 — giữ để tra cứu)* **GIVEN** asset Weapon Moveset thật / thiếu asset, **WHEN** nạp, **THEN** dùng số của asset / dùng bộ số dự phòng Rule 12. (AC-X05) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** Camera đăng ký `Teleported`, **WHEN** Region Loading gọi Teleport khi Input ở `Locked`, **THEN** Camera snap cùng frame; Grounded ở điểm đến xác định bằng cast; hai lượt trên không đầy. (AC-X06) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** Defeat & Retry, **WHEN** có `FellOutOfWorld`, **THEN** gọi `Respawn(last_safe_ground)`, trạng thái Idle. (AC-X07) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** Elemental đăng ký lạnh 0,7 và ướt 0,8, **WHEN** gỡ handle ướt, rồi thêm root, **THEN** tốc chạy 3,64 → 4,55 u/s; khi root thì Attack vẫn ra. (AC-X08) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** EdgeCollider2D gồm đoạn bằng nối dốc 45°, **WHEN** cast tại đỉnh nối, **THEN** pháp tuyến chọn được là pháp tuyến hướng lên nhiều nhất, đi qua đỉnh không nảy (kiểm chứng giả định). (AC-X09) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** platform một chiều thật, **WHEN** nhảy xuyên lên rồi DropThrough, **THEN** lọc khớp kết quả lõi; vị trí body sau mỗi bước bằng dự đoán của lõi ±`skin`. (AC-X10) `[Integration/PlayMode — BLOCKING]`

### H.7 Hiệu năng

- **GIVEN** lõi đã warm-up 100 bước, **WHEN** chạy 10 000 bước theo kịch bản, **THEN** bộ nhớ cấp phát tăng 0 byte. (AC-P01) `[Performance — BLOCKING]`
- **GIVEN** PlayMode ổn định, **WHEN** đo 600 frame, **THEN** marker `Controller.*` cấp phát 0 B mỗi frame. ADVISORY tới khi có ADR physics. (AC-P02) `[Performance — ADVISORY]`
- **GIVEN** PC mục tiêu, 3 bước con mỗi frame, **WHEN** đo `Controller.Step`, **THEN** p95 ≤ 0,1 ms (đề xuất). ADVISORY tới khi technical-director chốt ngân sách và có spec máy tối thiểu. (AC-P03) `[Performance — ADVISORY]`

### H.8 Playtest, cảm giác và Visual *(gate ở milestone Vertical Slice)*

- **GIVEN** n ≥ 5 tester, mỗi nửa game ≥ 20 phút, hỏi mở rồi hỏi gợi theo danh sách từ chốt trước ("ì", "bị khóa", "nặng chân", "dính", "trượt"), **WHEN** phân tích, **THEN** 0 tester dùng các từ đó để mô tả cơ thể nhân vật ở bất kỳ nửa nào (câu đo playtest). (AC-T01) `[Playtest — BLOCKING]`
- **GIVEN** mỗi tester dùng ≥ 2 họ vũ khí, **WHEN** rà log, **THEN** `move_speed`, đỉnh nhảy, quãng né giống hệt nhau giữa các họ; 0 báo cáo vũ khí làm chậm người (binding test 1). (AC-T02) `[Playtest — BLOCKING]`
- **GIVEN** log theo frame (H-13), **WHEN** đếm, **THEN** 0 frame đánh mà `|move.x|` vượt dead zone mà `vx` = 0 (trừ root hoặc tường); 0 lần Dodge trong cửa sổ hủy đòn không ra ở frame được phép, trừ khi đang Hitstun hoặc bị root (binding test 2 và ngoại lệ của nó); 0 lần root được áp trong lúc cờ i-frame bật (ràng buộc OQ-C8). (AC-T03) `[Playtest — BLOCKING]`
- **GIVEN** ≥ 15 phút combat có hit-stop, **WHEN** đếm, **THEN** 100% lần bấm Jump/Dodge hợp lệ giữa hit-stop ra ở frame đầu tiên sau hit-stop (binding test 3). (AC-T04) `[Playtest — BLOCKING]`
- **GIVEN** map có dốc ≤ 45°, **WHEN** đếm, **THEN** 0 `LeftGround` khi chạy xuống dốc không nhảy; 0 `Bonked` mà bước sau `vy` ≥ 0 (binding test 4). (AC-T05) `[Playtest — BLOCKING]`
- **GIVEN** arena boss, **WHEN** đo, **THEN** trung vị thời gian tới lần đầu dùng nhảy đôi + né trên không trong cùng lần bay ≤ 5 phút (mọi tester ≤ 10 phút); 0 lý do `NoAirDodge` sau nhảy đôi khi còn lượt (binding test 5). (AC-T06) `[Playtest — BLOCKING]`
- **GIVEN** nửa cozy, **WHEN** đo, **THEN** ≥ 95% lần dừng có quãng từ lúc nhả phím thô tới `vx` = 0 ≤ 0,13 u; ≥ 90% lần dừng đầu tiên có tâm nhân vật trong ô đất 1,5 u đang nhắm; 0 lần rơi xuyên platform khi không có bản ghi DropThrough (binding test 6). (AC-T07) `[Playtest — BLOCKING]`
- **GIVEN** lần chết gắn nhãn "input", **WHEN** rà log từ chối, **THEN** 0 lần "xác nhận do bên tiêu thụ" quy cho Controller (bổ sung Input AC-T04). (AC-T08) `[Playtest — BLOCKING]`
- **GIVEN** cảnh lưới 0,5 u, **WHEN** quay chuỗi frame của cú nhảy giữ hết, cú nhảy bị cắt, cú nhảy đôi, **THEN** có ảnh onion-skin retained trong `production/qa/evidence/`, đỉnh lệch ≤ 0,05 u so với F1/F2, có lead ký duyệt. (AC-VI01) `[Visual — BLOCKING]`
- **GIVEN** dốc 30°/45° và đỉnh dốc lồi, **WHEN** quay chạy lên, chạy xuống, né, **THEN** có ảnh retained cho thấy chân bám mặt đất mọi frame, có lead ký duyệt. (AC-VI02) `[Visual — BLOCKING]`
- **GIVEN** overlay cờ i-frame (H-14), **WHEN** quay cú né, dash attack, cú né bị root cắt, `iframe_scale` 1,5, **THEN** ảnh retained cho thấy hiệu ứng i-frame bật/tắt đúng frame với cờ (lệch 0 frame), có lead ký duyệt. (AC-VI03) `[Visual — BLOCKING]`
- **GIVEN** chỉ báo lượt trên không trên thân nhân vật, **WHEN** chụp các trạng thái 2/1/0 lượt, **THEN** có screenshot retained từng trạng thái. (AC-VI04) `[Visual — BLOCKING]`

## Open Questions

| ID | Câu hỏi | Chủ | Hạn |
|---|---|---|---|
| OQ-C1 | **ADR "Character controller physics":** loại body (đề xuất Rigidbody2D Kinematic + cast tự viết), hàm cast và `ContactFilter2D`, skin, số lần lặp, layer, cách ghi vị trí (gán `position` hay `MovePosition`), `queriesStartInColliders` cho cả project (đang bật; từng làm `IsGrounded` của prototype luôn đúng), chế độ mô phỏng Physics2D (đang FixedUpdate 50 Hz trong project) và việc trigger/hurtbox của Combat chạy thế nào khi hit-stop, tách lõi C# / adapter | technical-director, unity-specialist, gameplay-programmer | Trước story đầu tiên |
| OQ-C2 | **Không có tài liệu engine cho Physics2D.** Đề xuất unity-specialist viết `docs/engine-reference/unity/modules/physics2d.md` cho 6000.3: cast, pháp tuyến ở đỉnh EdgeCollider2D, query có tôn trọng PlatformEffector2D không, `position` gán xong có được query thấy ngay không, `useFullKinematicContacts`, `Physics2D.Simulate` ở chế độ Script | unity-specialist | Trước ADR OQ-C1 |
| OQ-C3 | **Đồng hồ Game / hit-stop dùng chung:** một dịch vụ duy nhất cấp `dt` có scale (tick) cho Input, Combat, Controller, chạy đầu frame, để tránh lỗi trễ một frame khi hit-stop kết thúc (`Time.deltaTime` chốt đầu frame — giả định). Gắn với ADR Input OQ-I1 | technical-director | Trước story đầu tiên |
| OQ-C4 | `Time.maximumDeltaTime` của project có kẹp `deltaTime` không và ở mức nào; phải khớp `max_frame_dt` (0,10 s) | unity-specialist | Cùng OQ-C1 |
| OQ-C5 | **Ngân sách CPU** cho `Controller.Step` (AC-P03 đang dùng 0,1 ms p95 làm đề xuất) và spec máy tối thiểu | technical-director | Trước Vertical Slice |
| OQ-C6 | **Hạ tầng test:** thêm lệnh chạy PlayMode vào `commands.test` (chung với Input OQ-I10); fake collision world là hạ tầng test dùng chung | qa-lead, devops-engineer | Trước story có AC PlayMode |
| OQ-C7 | **Test hook** phần hiện thực phải có (đưa vào ADR OQ-C1): H-1 đồng hồ Game inject (tick, chung nguồn với Input); H-2 Input giả; H-3 thế giới va chạm giả dựng từ hình học giải tích; H-4 snapshot trạng thái; H-5 bộ ghi sự kiện; H-6 sink log từ chối / log lỗi; H-7 fixture cấu hình + validator hàm thuần; H-8 fixture dữ liệu vũ khí; H-9 predicate liveness; H-10 bảng chuyển trạng thái dạng dữ liệu; H-11 `Step(dt)` quan sát được bước con; H-12 profiler marker `Controller.Step`; H-13 log playtest theo frame; H-14 overlay debug | lead-programmer, qa-lead | Cùng OQ-C1 |
| OQ-C8 | **Combat / Elemental & Status:** **ràng buộc cứng (CD-GDD-ALIGN, người dùng chọn 2026-10-31): hiệu ứng trói (root) tôn trọng i-frame giống sát thương** và luôn có báo trước; Elemental & Status đặt **trần thời lượng root**. Còn mở: Plunge từ độ cao thấp có cần độ cao tối thiểu để có sóng xung kích không; loại sát thương nào xuyên i-frame; không có va tường (wall-splat); xác nhận `stun_time` 0,25, bất tử 0,6 s, validator 12 (khoảng giữa làn đạn ≥ 0,55 s) | game-designer (GDD Combat, GDD Elemental & Status) | Khi viết GDD Combat |
| OQ-C9 | ~~Weapon Moveset: xác nhận schema~~ **Đã trả lời** bởi GDD Skill & Moveset (2026-11-05): kỹ năng dữ liệu, node chuyển động có giới hạn, kỹ năng dự phòng, chụp số lúc bắt đầu. Còn mở: hạng `R_skill` trong F3 (tầm lướt của kỹ năng cộng vào tầm ngang, `air_lunge_budget` 3,23 u, đặc biệt 6,46 u) — Skill & Moveset OQ-S8 | systems-designer | khi chuẩn hóa đơn vị registry |
| OQ-C10 | **Defeat & Retry:** quy ước khóa sống sót qua hồi sinh; hậu quả khi bị hất khỏi mép đã hết lượt trên không; luồng `EnterDefeated` / `Respawn` | game-designer | Khi viết GDD Defeat & Retry |
| OQ-C11 | **Level design / map sinh:** khe bắt buộc ≤ 0,85 × tầm chuỗi (F3); bậc ≤ 2,2 u; hai platform một chiều chồng nhau cách ≥ 1,5 u; trần hang ≥ 5 u (arena không muốn nhảy đôi va trần ≥ ~6,9 u); vùng Interact chỗ câu cá phủ tới tường bờ; điểm hồi sinh không nằm trong collider; `kill_y` do Region Loading cung cấp | level-designer, chủ hệ sinh map (chưa có trong index) | Trước khi viết GDD Region Loading / map sinh |
| OQ-C12 | **Input:** khi nút bị chốt qua rào rồi được nhả thật, `last_release_seq` có được cập nhật không (quyết định cú nhảy có bị cắt sau khi đóng menu giữa không trung) | game-designer (GDD Input) | Trước story Controller–Input |
| OQ-C13 | **Playtest lại các số lệch prototype**, mỗi số có tiêu chí và giá trị lùi về (CD-GDD-ALIGN). **Tiêu chí đạt chung:** AC-T01 (0 tester mô tả nhân vật là "ì" / "bị khóa" / "nặng chân") và AC-T03, AC-T06 đạt. **Nếu trượt, lùi về số đã PROCEED:** chu kỳ né 0,55 s → `dodge_cooldown` 0,35 s **tính từ lúc bắt đầu** (đánh đổi: spam né 8 u/s, bất tử 57%); rơi ×1,6 → `fall_gravity_mult` 1,0; treo ở đỉnh → `apex_hang_mult` 1,0 (tắt); root cắt cú né (C-10) → né chạy hết rồi mới root. Nhảy đôi và né trên không là mới: trượt thì giảm `double_jump_speed` trong khoảng an toàn hoặc bỏ né trên không (cần người dùng quyết). Chỉ đổi một số mỗi vòng để biết số nào gây ra cảm giác | game-designer, qa-lead | Vòng playtest đầu tiên sau khi có bản build Controller |
