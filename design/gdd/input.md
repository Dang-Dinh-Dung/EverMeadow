# Input

> **Status**: Designed (pending independent review)
> **Author**: Đặng Đình Dũng + Claude (game-designer, ux-designer, gameplay-programmer, systems-designer, qa-lead, creative-director)
> **Last Updated**: 2026-10-30
> **Last Verified**: 2026-10-30
> **Implements Pillar**: Core Loop › Moment-to-Moment (combat phản hồi tức thì), Pillar 4 (Fewer Bosses, Each an Event — "khó nhưng đọc được"), Pillar 3 (Two Lives, One Loop — nửa cozy không bị lạc chế độ)
> **Creative Director Review (CD-GDD-ALIGN)**: APPROVED 2026-10-30 — 4 ghi chú không chặn đã xử lý (header pillar; OQ-I15 `Locked` ↔ đồng hồ Game Time; ghi chú cho GDD câu cá ở OQ-I14; câu "cầm cần câu" trong Player Fantasy xem lại khi cập nhật concept)

## Summary

Input biến bàn phím, chuột và tay cầm thành các hành động có tên (Nhảy, Đánh, Né, Tương tác…), giữ
những lần bấm gần đây trong một bộ đệm để hệ khác tiêu thụ trễ mà không mất phím, quản lý chế độ
input (Gameplay, Menu, Hội thoại, Câu cá, Bản đồ, Khóa) bằng stack token có rào chống rò phím, và báo
thiết bị đang dùng để hiện đúng biểu tượng phím. Người chơi không thấy hệ này; họ cảm nhận nó qua việc
lần bấm không bao giờ bị mất hay lạc chỗ ("Đôi tay quen việc"), điều kiện để combat nhịp ~0,20 s mỗi đòn
và nửa cozy đều dễ chịu. Input không quyết định lần bấm có hiệu lực (Character Controller quyết) và
không đọc cài đặt (Settings đẩy vào).

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `None` (dùng Input System package; ADR "Input architecture" và ADR TD-B4 chưa viết)

## Overview

Input là lớp nền chuyển tín hiệu từ bàn phím, chuột và tay cầm thành **hành động có tên**
(Nhảy, Đánh, Né, Tương tác…) mà mọi hệ khác đọc. Không hệ gameplay nào được đọc phím
vật lý trực tiếp: Character Controller hỏi "người chơi vừa bấm Nhảy chưa", không hỏi
"phím Space có xuống không". Input sở hữu bốn thứ: **bảng hành động** và cách gán phím
mặc định cho cả bàn phím lẫn tay cầm; **bộ đệm phím có mốc thời gian**, giữ những lần
bấm gần đây để hệ khác tiêu thụ trễ một chút mà không mất phím; **chế độ input**
(Gameplay, Menu, Câu cá, Hội thoại…) quyết định nhóm hành động nào đang nhận; và
**nhận biết thiết bị đang dùng**, để các gợi ý trên màn hình hiện đúng biểu tượng phím.

Người chơi không tương tác với Input như một tính năng. Họ cảm nhận nó gián tiếp, qua
việc nhân vật phản ứng đúng và kịp: một cú né bấm sát cuối đòn chém vẫn ra, một lần
nhảy bấm ngay trước khi chạm đất vẫn bật lên, và chuyển giữa bàn phím với tay cầm giữa
chừng không làm gì bị kẹt. Nếu không có lớp này, mỗi hệ sẽ tự đọc phím theo cách riêng.
Hệ quả: phím bị nuốt khi hit-stop làm chậm thời gian, menu và gameplay giành nhau cùng
một nút, và đổi phím (một yêu cầu bắt buộc khi lên Steam) phải sửa ở hàng chục chỗ.
Concept gọi thẳng rủi ro này: combat feel cần "input buffer, hit-stop, cancel window".
Prototype đã chứng minh nhịp ~0,20 giây mỗi đòn chỉ dễ chịu khi phím không bị rơi.

Phạm vi được giới hạn có chủ ý. Input **không** quyết định một lần bấm có hiệu lực hay
không. Việc đó thuộc hệ tiêu thụ: Character Controller quyết định lần bấm Né trong bộ đệm
có hủy được đòn chém hay không (systems index, TD-A3). Input cũng **không** đọc cài đặt:
Settings & Accessibility đẩy cấu hình phím vào Input lúc khởi động (TD-B8). Màn hình đổi
phím thuộc Settings (tier Vertical Slice); Input chỉ cung cấp API cho nó.

## Player Fantasy

**"Đôi tay quen việc."** *(Định khung: gián tiếp. Người chơi cảm nhận tác dụng, không
thấy hệ thống. Hướng B do creative-director đề xuất, người dùng chọn 2026-10-30.)*

Người giữ Giao Ước Thần Rừng có một đôi tay quen việc. Cầm kiếm, cầm cần câu hay lật
sổ đều không vấp. Ngoài bãi săn, khi boss phủ kín màn hình bằng đạn, nhân vật đáp lại
đúng lúc. Cú né bấm ở khung cuối của nhát chém vẫn ra. Cú nhảy bấm ngay trước khi chạm
đất vẫn bật lên. Trong hit-stop, đòn đánh khựng lại nhưng tay người chơi không khựng
theo. Về làng, nút bấm không bao giờ "lạc chỗ": đang nói chuyện với dân làng thì không
lỡ tay chém, đóng menu thì không bật nhảy, đang câu cá mà cầm tay cầm lên thì không có
gì bị kẹt. Người chơi không bao giờ phải nghĩ tới phím. Họ chỉ nghĩ tới việc mình đang làm.

**Lời hứa là "lần bấm của bạn không bao giờ bị mất hay lạc chỗ".** Lời hứa không phải
"nhân vật luôn làm theo lệnh". Input không quyết định một lần bấm có tác dụng hay không;
bên tiêu thụ quyết định (Overview). Một cú né bấm khi còn hồi chiêu không ra là luật của
Character Controller. Một cú né bấm hợp lệ mà biến mất là lỗi của Input.

**Phục vụ pillar nào:**
- *Core Loop › Moment-to-Moment:* "tốc độ gốc ngang hoặc nhanh hơn Hollow Knight… cancel
  sang né". Ở nhịp ~0,20 giây mỗi đòn, một phím bị rơi là một nhịp hỏng.
- *Pillar 4 (Fewer Bosses, Each an Event):* boss "khó nhưng đọc được". Một lần chết phải
  là bài học về pattern, không phải về phần cứng. Câu đo này của hướng C trở thành tiêu
  chí playtest ở Acceptance Criteria.
- *Pillar 3 (Two Lives, One Loop):* nửa cozy cũng là nửa thật. Ở nửa này input hỏng theo
  kiểu khác combat: không phải thiếu phản xạ mà là **lạc chế độ**, tức lần bấm thừa từ
  hội thoại tràn sang gameplay, hoặc menu nuốt mất lần bấm đầu tiên. Chuyển chế độ là
  trách nhiệm riêng của Input, nên fantasy phải nói tới nó.
- *Anti-pillar "NOT a time-pressure game":* ở nửa cozy, sự dễ tha thứ của input là để
  **thư thả**, không phải để đòi độ chính xác từng khung hình. Tinh thần là "không phạt
  người chơi vì tay". Độ rộng cửa sổ cụ thể do bên tiêu thụ quyết, không ghi ở đây.

**Binding test.** Fantasy này bị phản bội nếu xảy ra bất kỳ điều nào sau:
1. Một lần bấm dành cho chế độ này có tác dụng ở chế độ khác. Ví dụ: Tương tác để đóng
   hội thoại lại kích hoạt Đánh hoặc Nhảy trong Gameplay.
2. Đổi bàn phím ↔ tay cầm giữa chừng làm input bị kẹt, hoặc biểu tượng phím trên màn
   hình hiện sai thiết bị dù chỉ một khung hình hiển thị.
3. Một lần bấm hợp lệ (nằm trong cửa sổ bộ đệm mà bên tiêu thụ khai báo) bị mất trong
   lúc hit-stop, lúc lướt né hay lúc đang tấn công.

## Detailed Design

*Nguồn: game-designer (luật, chế độ, giao diện), ux-designer (phím mặc định, chuyển chế độ,
thiết bị, khả năng tiếp cận), gameplay-programmer (khả thi trên Input System 1.20.1), 2026-10-30.
Quyết định của người dùng 2026-10-30: I-1 câu cá gộp vào Tương tác; I-2 Né tay cầm ở nút vai RB;
I-3 Plunge và Rơi qua platform là hành động riêng; I-4 chế độ là stack theo token.
Mọi khẳng định về API Unity không có trong `docs/engine-reference/unity/modules/input.md`
(chỉ xác minh cho bản 1.11) được đánh dấu **[giả định]** và gom vào Open Questions.*

*Đối chiếu tên với ADR-0001 (2026-10-31): "đồng hồ Game" / `t_game` / `ClockKind.Game` trong GDD này = `FrameClock.ScaledNow` /
`tScaled` / `ClockKind.Scaled` trong ADR-0001 — thời gian khung hình có scale (hit-stop, pause), **không phải** đồng hồ lịch
của Game Time & Calendar. "Đồng hồ thật" = `FrameClock.RealNow`. Đơn vị: tick `long` 1 µs.*

*Sửa sau Edge Cases (2026-10-30, systems-designer; người dùng chọn sửa thẳng vào luật): Rule 4
(Xuống cùng frame), 5 (mốc `double`, đồng hồ đơn điệu), 7 (cảnh báo khi gọi), 8 (`ReadMove` về 0
ngoài Gameplay), 9 (I-6: Toggle gắn với mục trong stack), 10 (timeScale = 0 phải kèm push),
11 (handle có thế hệ, tự gỡ chủ đã chết, `IsTop`/`TopChanged`), 12 (đỉnh = handle; không gộp rào;
hoãn push/pop trong handler), 13 (`DeviceChanged` tối đa một lần mỗi bước), 14 (ghi đè kiểu biểu
tượng), 15 (rút tay cầm, mất focus, frame đầu), 17 (I-7: khóa Esc/Menu cho Pause và Cancel;
`CommitRebind`; đổi phím tạo rào; kiểm tra khi nạp).*

*Sửa khi triển khai (2026-10-31, quyết định I-8 của người dùng): Esc và Menu/Options là Pause chỉ
khi không có menu nào mở (Gameplay); ở Menu, Dialogue, Map và Fishing chúng là Cancel. Pause chỉ
còn thuộc Gameplay; Cancel có thêm Menu/Options. Đã sửa Rule 2, 9, 11, bảng States, Edge Cases,
AC-M04, AC-X06; thêm AC-M06.*

*Sửa (2026-11-02, quyết định I-9 của người dùng, đi kèm C-13 của Character Controller): thêm hành động **Run**
(button, Gameplay, giữ để chạy; Left Ctrl, B / Circle). Không có Run thì nhân vật đi bộ. Đã sửa Rule 2.
Sửa tiếp (2026-11-03, người dùng): Run thành **bấm để bật/tắt**; Character Controller tiêu thụ bản ghi `Pressed` của Run
(`TryConsume`, cửa sổ `run_window`) và tự giữ công tắc. Input không đổi cách ghi.*

*Sửa (2026-11-05, quyết định I-10 của người dùng, theo GDD Skill & Moveset): thêm hành động **Special** (nút Kỹ năng
đặc biệt; button, Gameplay; bàn phím **L** và **chuột phải**, tay cầm **RT / R2**). Né mất phím phụ RT, chỉ còn RB /
R1 (bàn phím giữ Left Shift, K). Character Controller tiêu thụ Special với cửa sổ 0,20 s đồng hồ Game, chung hàng đợi với
Attack (Controller Rule 28). Đã sửa Rule 2, Downstream. Code cần: `InputActionId.Special`, ActionCatalog Gameplay, ô binding L / chuột phải / RT trong asset (bỏ RT khỏi Dodge), dòng Rule 2 trong InputAssetTests.*

*Hiện thực Rule 17 (2026-11-04, người dùng chốt: UI Toolkit, menu Pause tối giản, phím chính + phụ cho bàn phím và tay
cầm, lưu `settings.json`): asset có sẵn **ô trống thứ hai** cho mỗi hành động nút trên mỗi thiết bị (ID binding cố định,
uuid5 "evermeadow:input/<map>/<action>/slot/<group>/<n>"), nên cấu hình lưu theo GUID binding không lệch ô. Plunge /
DropThrough dùng tổ hợp cho tới khi người chơi gán phím riêng (tự gọi `SetChordMode`). `ApplyBindingOverrides` chạy kiểm tra
khi nạp (dữ liệu hỏng → mặc định; trùng phím hoặc chiếm Esc/Menu → hành động đó về mặc định) và trả `false` để Settings lưu
lại. Esc hủy rebind ô bàn phím, Menu/Options hủy rebind ô tay cầm. Settings (`settings.json` trong persistentDataPath) nạp lúc
khởi động và đẩy vào Input (TD-B8). Màn hình Điều khiển đọc Navigate/Confirm/Cancel từ Input; chuột dùng sự kiện con trỏ
của UI Toolkit (cần chốt trong GDD Menu & Navigation). GDD Settings & Accessibility và ADR chọn UI Toolkit chưa viết.*

### Core Rules

**Hành động và phím**

1. **Chỉ đọc hành động có tên.** Không hệ nào ngoài Input đọc phím, nút hay cần vật lý.
   Mọi hệ hỏi Input bằng tên hành động trong bảng ở Rule 2. Chỉ assembly của Input được tham
   chiếu Input System package; luật này được cưỡng chế bằng test tĩnh (AC-R01), không chỉ là quy
   ước. (*Active Input Handling* đang là *Both* nên API cũ vẫn biên dịch được: Open Questions.)

2. **Bảng hành động.** Mỗi hành động thuộc một hoặc nhiều chế độ (Rule 11). Phím mặc định:

   | Hành động | Loại | Chế độ | Bàn phím + chuột | Tay cầm (Xbox / PlayStation) |
   |---|---|---|---|---|
   | Move | value (Vector2) | Gameplay | A/D, W/S; mũi tên làm phụ | Cần trái, D-pad |
   | Jump | button (nhấn + nhả) | Gameplay | Space | A / Cross |
   | Attack | button | Gameplay | J, chuột trái | X / Square |
   | Dodge | button | Gameplay | Left Shift, K | RB / R1 *(RT chuyển cho Special, I-10)* |
   | Plunge | button, tổ hợp mặc định (Rule 4) | Gameplay | Xuống + Attack | Xuống + X |
   | DropThrough | button, tổ hợp mặc định (Rule 4) | Gameplay | Xuống + Jump | Xuống + A |
   | Interact | button | Gameplay | E | Y / Triangle |
   | Heal | button | Gameplay | Q | LB / L1 |
   | EatBuff | button | Gameplay | R | LT / L2 |
   | Run | button, mỗi lần nhấn đổi đi bộ ↔ chạy (I-9) | Gameplay | Left Ctrl | B / Circle |
   | Special | button (I-10) | Gameplay | L, chuột phải | RT / R2 |
   | Map | button | Gameplay, Map | M | View / Touchpad |
   | Pause | button | Gameplay | Esc | Menu / Options |
   | Navigate | value (Vector2), có lặp (Rule 16) | Menu, Dialogue, Map | W/A/S/D, mũi tên | D-pad, cần trái |
   | Confirm | button | Menu, Dialogue | E, Enter, Space | A / Cross |
   | Cancel | button | Menu, Dialogue, Map, Fishing | Esc, Backspace | B / Circle, Menu / Options |
   | TabLeft / TabRight | button | Menu | Q / R | LB / RB |
   | Point / Click | value + button | Menu, Map | chuột | — |
   | Reel | hold (nhấn + nhả + đang giữ) | Fishing | Space, chuột trái, E | A / Cross, Y / Triangle |

   - **Câu cá không có nút riêng (I-1).** Chỗ câu cá là một trạm như lò rèn: Interact ở bờ
     để thả câu, sau đó hệ câu cá đẩy chế độ Fishing và đọc Reel (bấm để giật, giữ để nâng).
   - **Esc và Menu/Options: Pause hay Cancel theo chế độ (I-8).** Không có menu nào mở (Gameplay)
     thì là Pause; ở Menu, Dialogue, Map và Fishing thì là Cancel (đóng menu, đóng bản đồ, bỏ câu).
     Tập chế độ của Pause và Cancel không giao nhau, nên mỗi lần bấm chỉ ra đúng một hành động mà
     không cần luật riêng cho phím. Hệ quả: đang câu cá thì Esc là bỏ câu, không tạm dừng; `Menu`
     vẫn có thể bị đẩy lên trên `Fishing` bởi hệ khác (ví dụ Menu & Navigation khi `DeviceLost`).
   - **Tương tác và Đánh luôn ở hai nút khác nhau** (E ≠ J, Y ≠ X). Gộp chúng theo ngữ cảnh
     là vi phạm binding test 1 ngay từ thiết kế: đang chém mà lại gần dân làng sẽ mở hội thoại.
   - **Run là công tắc (I-9).** Mỗi lần nhấn đổi đi bộ ↔ chạy; nhả nút không làm gì (GDD Character Controller C-13). B / Circle
     ở Gameplay vốn trống (chỉ là Cancel ở các chế độ khác, tập chế độ không giao nhau). Công tắc nằm ở
     Character Controller, không dùng `HoldMode` của Rule 9 (chỉ Reel dùng); muốn có lựa chọn "giữ để chạy" thì thêm
     khi làm phần Accessibility.
   - **Né ở nút vai (I-2)** để ngón cái giữ được A (Nhảy) cùng lúc. Nhảy đôi và né trên không
     (đã quyết cho GDD Character Controller) cần bấm hai nút này gần như đồng thời.
   - Một số phím đổi nghĩa giữa các chế độ (Space là Nhảy ở Gameplay, Xác nhận ở Menu, Reel
     ở Fishing; Q/R là Hồi máu/Ăn buff ở Gameplay, chuyển tab ở Menu). Việc này chấp nhận
     được vì chỉ chế độ ở đỉnh stack nhận input (Rule 11) và có rào chuyển chế độ (Rule 12).
   - Map **Debug** (bảng số liệu, đổi seed…) chỉ có trong development build, không nằm trong
     bảng phát hành và không đổi phím được.

3. **Nhiều phím cho một hành động, mọi thiết bị luôn hoạt động.** Một hành động có thể gán
   nhiều phím trên nhiều thiết bị cùng lúc (WASD + chuột và WASD + J/K đều dùng được, không
   phải chọn "kiểu điều khiển"). Bàn phím, chuột và tay cầm luôn được đọc song song; "thiết
   bị đang dùng" (Rule 13) chỉ quyết định biểu tượng phím, **không bao giờ chặn input**.
   Nhờ vậy vế "kẹt input khi đổi thiết bị" của binding test 2 bị loại trừ từ thiết kế.

4. **Hành động tổ hợp (I-3).** Plunge và DropThrough là hành động riêng, có ô gán phím riêng.
   Mặc định chúng được gán theo tổ hợp "Xuống + nút gốc" (Attack, Jump). Người chơi có thể
   đổi chúng thành một nút đơn.
   - **Điều kiện "Xuống"**: xét trên Move **sau dead zone** (F4): trục dọc ≤ `−down_threshold`
     **và** hướng nằm trong ±góc của hành động quanh hướng thẳng xuống (Formulas F3). Mỗi hành
     động một góc (quyết định I-5, 2026-10-30): `down_angle_drop` = 35° cho DropThrough, để chạy
     chéo xuống (kể cả S+D trên bàn phím, 45°) không vô tình rơi qua platform; `down_angle_plunge`
     = 50° cho Plunge, để người chơi đang trôi ngang trên không (giữ S+D) vẫn ra plunge.
   - **Một lần bấm, một bản ghi.** Khi nút gốc được bấm lúc điều kiện "Xuống" đang đúng, Input
     ghi **một** bản ghi với `action = Plunge` (hoặc `DropThrough`) và `baseAction = Attack`
     (hoặc `Jump`). Bên tiêu thụ có thể lấy bản ghi đó bằng tên nào cũng được (Rule 8); lấy
     bằng tên này thì bản ghi coi như đã tiêu thụ cho cả tên kia. Ví dụ: Character Controller
     đang ở trên không thì tiêu thụ Plunge; đứng trên đất thì tiêu thụ Attack từ chính bản ghi
     đó. Plunge trên mặt đất có nghĩa gì là luật của Controller, không phải của Input.
   - Khi Plunge hoặc DropThrough bị gán thành nút đơn, bản ghi của nó không có `baseAction`, và
     bấm nút gốc lúc đang Xuống chỉ ghi hành động gốc (Attack, Jump) thường.
   - **Xuống và nút gốc trong cùng một frame được coi là đồng thời:** điều kiện "Xuống" đúng nếu
     nó đúng tại lúc xử lý sự kiện nút gốc **hoặc** tại cuối bước xử lý input của frame đó, không
     phụ thuộc thứ tự callback.
   - Bản ghi được chốt tại lúc bấm: Xuống được nhả hay nhấn sau đó không đổi một bản ghi Plunge
     thành Attack hay ngược lại. Khi bản ghi tổ hợp thành lập nhờ Xuống ở cuối bước (xem trên),
     trường `move` (Rule 5) lấy giá trị cuối bước.

**Bộ đệm**

5. **Ghi sự kiện.** Mỗi lần nhấn **và** mỗi lần nhả của một hành động thuộc chế độ đang ở
   đỉnh stack được ghi một bản ghi:

   | Trường | Ý nghĩa |
   |---|---|
   | `seq` | số thứ tự tăng dần, không lặp trong một phiên |
   | `action`, `baseAction` | tên hành động; `baseAction` chỉ có ở bản ghi tổ hợp (Rule 4) |
   | `phase` | `Pressed` hoặc `Released` |
   | `t_real` | thời gian thật lúc Input xử lý sự kiện (không bị timeScale ảnh hưởng) |
   | `t_game` | thời gian có scale lúc Input xử lý sự kiện (bị hit-stop làm chậm) |
   | `device` | `KeyboardMouse` hoặc `Gamepad` |
   | `epoch` | số thứ tự rào lúc ghi: tăng mỗi khi handle ở đỉnh stack đổi (Rule 12) và mỗi lần đổi phím (Rule 17) |
   | `move` | giá trị Move lúc bấm, đã qua dead zone |
   | `consumed` | đã bị tiêu thụ chưa |

   - `move` giữ **ý định hướng** tại lúc bấm: Xuống + Đánh được buffer vẫn ra plunge dù lúc
     tiêu thụ người chơi đã nhả Xuống; một cú né buffer vẫn lướt theo hướng lúc bấm.
   - Cả hai mốc thời gian đọc từ **một đồng hồ được inject vào** (một nguồn thời gian thật,
     một nguồn có scale). Input đóng dấu lúc xử lý callback, không dùng mốc thời gian riêng
     của sự kiện thiết bị. Sai số ≤ độ dài frame đang xử lý (~16,7 ms ở 60 fps đều; bằng đúng
     độ dài frame khi frame khựng), luôn theo hướng dễ dãi (F1). Đổi lại hai mốc luôn cùng trục
     và test cho kết quả xác định.
   - Mốc thời gian là **tick nguyên 64 bit** tính từ đầu phiên (đề xuất 1 tick = 1 µs); `double`
     chỉ dùng để hiển thị. `seq` và `epoch` là số nguyên 64 bit. Lý do: so sánh biên ≤ (F1) và phép
     `floor` (F5) hỏng với số thực, ví dụ (0,98 − 0,35)/0,09 ra 6,999… trong `double`; còn `float`
     32 bit mất độ chính xác tới ~7,8 ms sau 18 giờ chơi.
   - Mỗi đồng hồ được giữ đơn điệu: `now = max(now, last_now)`; nếu nguồn chạy lùi thì log lỗi một
     lần mỗi phiên. Không có chốt này, tuổi âm luôn ≤ cửa sổ và bản ghi cũ đủ điều kiện mãi mãi.
   - Có nên dùng mốc thời gian riêng của sự kiện để chính xác dưới một frame hay không: OQ-I4.
   - Input nhận sự kiện qua callback của từng action (nhấn, nhả), không thăm dò trạng thái mỗi
     frame, để chuỗi nhấn–nhả–nhấn trong cùng một frame không bị gộp thành một bit. **[giả định:
     callback bắn riêng cho từng sự kiện trong một frame]**

6. **Giữ lại và sức chứa.** Bộ đệm là vòng tròn cố định `buffer_capacity` bản ghi (mặc định 32),
   cấp phát một lần, không tạo rác mỗi frame. Bản ghi cũ hơn `buffer_retention` giây thời gian
   thật (mặc định 1,0 s) bị coi là hết hạn. Khi đầy, bản ghi cũ nhất bị ghi đè. Hai giá trị này
   là giới hạn của riêng Input, không phải cửa sổ của bên tiêu thụ.

7. **Khai báo cửa sổ.** Bên tiêu thụ đăng ký với Input bằng cửa sổ lớn nhất nó sẽ dùng và loại
   đồng hồ của cửa sổ đó. Input cảnh báo (log, không chặn) nếu cửa sổ khai báo vượt quá thời gian
   giữ lại (Formulas F2). Độ dài cửa sổ của từng hành động (đệm nhảy, đệm né, đệm đòn kế) do bên
   tiêu thụ quyết và nằm trong GDD của bên tiêu thụ. Khi gọi với `window` lớn hơn cửa sổ đã đăng
   ký hoặc lớn hơn `buffer_retention`, cửa sổ hiệu dụng vẫn bị `buffer_retention` cắt (F1); development
   build và Editor log cảnh báo một lần cho mỗi cặp (bên tiêu thụ, hành động). `window` < 0 được coi là 0.

8. **Tiêu thụ.** Input cung cấp cho bên tiêu thụ:
   - `TryConsume(action, window, clock)`: tìm bản ghi `Pressed` **cũ nhất** chưa tiêu thụ, cùng
     `epoch` hiện tại, có `action` hoặc `baseAction` trùng tên, nằm trong cửa sổ (F1). Nếu có thì
     đánh dấu đã tiêu thụ và trả về nó; nếu không thì trả về "không có". `clock` là `Real` hoặc
     `Game`. Chọn `Game` nghĩa là hit-stop làm cửa sổ gần như đứng lại; chọn `Real` nghĩa là
     hit-stop ăn vào cửa sổ. Lựa chọn thuộc về bên tiêu thụ.
   - **Một lần bấm chỉ tạo ra một hiệu ứng.** Bấm dồn ba lần Đánh thì chuỗi ba đòn ra đúng ba
     nhát, không hơn: mỗi lần `TryConsume` lấy một bản ghi.
   - `Peek(action, window, clock)`: như `TryConsume` nhưng không tiêu thụ.
   - `WasReleasedAfter(action, seq)`: nút đã được nhả sau bản ghi `seq` chưa (kể cả khi nhả trong
     lúc hit-stop). Character Controller cần nó cho nhảy cao/thấp. Input lưu riêng `last_release_seq`
     cho từng hành động, nên câu trả lời không mất khi bản ghi `Released` hết hạn hay bị ghi đè.
   - `IsHeld(action)`: hành động đang được giữ, đã tính chế độ giữ/bật-tắt (Rule 9) và chốt (Rule 12).
   - `ReadMove()` / `ReadNavigate()`: giá trị trục hiện tại, đã qua dead zone. Trục không bị chốt.
     `ReadMove()` trả (0, 0) khi `Gameplay` không ở đỉnh stack; `ReadNavigate()` trả (0, 0) khi chế
     độ ở đỉnh không có Navigate. Nhờ vậy nhân vật không đi tiếp trong lúc cửa hàng đang mở.
   - Hỏi một hành động không thuộc chế độ ở đỉnh thì `TryConsume`/`Peek` trả "không có", không log
     (Character Controller hỏi Jump lúc menu đang mở là chuyện hợp lệ).

9. **Giữ hay bật/tắt.** Mỗi hành động loại hold (hiện có Reel; về sau có thể có hành động khác)
   có chế độ `Hold` (mặc định) hoặc `Toggle`, do Settings đẩy vào. Ở `Toggle`, một lần bấm bật
   trạng thái giữ, lần bấm sau tắt. `IsHeld` trả giá trị đã tính sẵn, nên hệ câu cá không phải
   tự làm chế độ bật/tắt. **Trạng thái bật của `Toggle` gắn với mục trong stack** (quyết định I-6,
   2026-10-30): khi mục đó bị chế độ khác che (ví dụ `Menu` đè lên Fishing khi rút tay cầm), trạng thái tạm ẩn
   (`IsHeld` trả sai); khi mục về lại đỉnh, trạng thái được khôi phục; trạng thái chỉ bị xóa khi
   chính mục đó bị `Pop`. Người chơi dùng tùy chọn trợ năng không bị thả dây chỉ vì tạm dừng.

10. **Thời gian và hit-stop.** Input xử lý sự kiện mỗi frame được vẽ, bất kể `Time.timeScale`.
    Hit-stop (timeScale ~0,05–0,08) không làm mất hay làm trễ việc ghi một lần bấm nào. Cổng duy
    nhất quyết định lần bấm còn hiệu lực là cửa sổ của bên tiêu thụ (Rule 8) và thời gian giữ lại
    (Rule 6). Input xử lý trong bước cập nhật theo frame (Dynamic Update), không theo bước vật lý,
    vì ở timeScale nhỏ bước vật lý gần như không chạy. **[giả định: ở chế độ xử lý theo
    FixedUpdate, sự kiện bị dồn hoặc trễ khi timeScale nhỏ]** Input không dùng đồng hồ Game Time:
    đồng hồ đó dừng khi mở menu, còn Input vẫn phải chạy.
    - **Dừng hẳn bằng timeScale = 0 phải kèm một push** (`Menu` hoặc `Locked`). Nếu không, đồng hồ
      `Game` đứng yên, mọi cửa sổ theo `Game` luôn đúng và chỉ `buffer_retention` cắt được; một lần
      bấm trong lúc dừng sẽ bung ra ngay khi chạy lại. F2 chỉ định cỡ cho timeScale ≥ `s_min`.
      Input phát hiện vi phạm theo đồng hồ inject, không đọc `Time.timeScale`: đồng hồ `Game` không
      tiến trong hơn `buffer_retention` giây thật khi đỉnh stack là `Gameplay` → development build và
      Editor log cảnh báo.

**Chế độ**

11. **Stack chế độ theo token (I-4).** Các chế độ: `Gameplay`, `Menu`, `Dialogue`, `Fishing`,
    `Map`, `Locked`.
    - `Gameplay` luôn nằm ở đáy stack và không bị gỡ.
    - Hệ cần một chế độ gọi `Push(mode, owner)` và nhận một **handle**. Chỉ handle đó gỡ được
      chế độ ấy (`Pop(handle)`), giống token tạm dừng của Game Time. Không hệ nào gỡ nhầm chế độ
      của hệ khác.
    - **Chỉ chế độ ở đỉnh stack nhận input.** Không có chuyện input "rơi xuống" chế độ bên dưới.
    - Gỡ một handle không ở đỉnh thì chế độ đó biến khỏi stack nhưng đỉnh không đổi, nên không
      có rào (Rule 12).
    - Handle mang **số thế hệ**: `Pop` với handle không hợp lệ, đã gỡ, hoặc của một instance Input
      khác thì bị bỏ qua, trả `false`, log cảnh báo, không có rào. Handle cũ không thể gỡ nhầm một
      chế độ mới dùng lại cùng ô.
    - **Chủ đã chết thì tự gỡ.** `Push` nhận kèm cách kiểm tra chủ còn sống (adapter cung cấp). Mỗi
      bước input, mục nào có chủ đã bị hủy mà chưa `Pop` (cutscene bị destroy, map bị unload) thì
      bị tự gỡ kèm log lỗi; nếu nó ở đỉnh thì có rào. Một `Locked` bị bỏ rơi sẽ khóa cả Pause, tức
      kẹt game vĩnh viễn.
    - Hai chủ có thể cùng đẩy một loại chế độ; stack giữ hai mục riêng.
    - `IsTop(handle)` và sự kiện `TopChanged` cho chủ biết chế độ của mình đang ở đỉnh hay bị che,
      ví dụ để hệ câu cá dừng tính lực căng dây khi `Menu` đè lên.
    - `Locked` là một chế độ như các chế độ khác: không hành động nào được ghi (kể cả Pause).
      Day banner, cutscene, nạp vùng đẩy `Locked`; "trả input" (Game Time GDD) chính là lúc
      Region Loading gỡ handle `Locked` của nó.

12. **Rào chuyển chế độ.** Mỗi khi **handle** ở đỉnh stack đổi (do push hay pop; gỡ một Menu con
    về Menu cha cũng tính, dù loại chế độ không đổi). Mỗi lần đổi là một rào riêng, không gộp:
    push rồi pop trong cùng frame vẫn tăng `epoch` hai lần. `Push`/`Pop` gọi từ bên trong handler
    sự kiện của Input (ví dụ handler `DeviceLost`) được hoãn tới cuối bước input. Một lần bấm đến
    trong cùng frame với một push mang epoch cũ và bị rào xóa:
    - `epoch` tăng một. Mọi bản ghi của epoch cũ coi như đã tiêu thụ: bộ đệm của chế độ cũ
      không bao giờ chảy sang chế độ mới.
    - Mọi nút đang được giữ bị **chốt**. Chế độ mới bỏ qua nút đó (không ghi lần nhả, `IsHeld`
      trả sai) cho tới khi người chơi nhả nó ra. Ví dụ: bấm E để đóng hội thoại thì lần nhả E
      không lọt xuống Gameplay; giữ Space lúc đóng menu không thành cú nhảy; lần bấm Interact
      để thả câu không được tính là lần giật ở dấu "!".
    - **Trục Move là ngoại lệ:** được đọc lại ngay. Move là tín hiệu liên tục, không phải lần
      bấm; bắt nhả thì người chơi đang giữ → lúc đóng cửa hàng sẽ thấy nhân vật bị kẹt.
    - Trạng thái lặp của Navigate (Rule 16) bị xóa. Trạng thái bật của Toggle theo Rule 9 (tạm ẩn
      khi bị che, không bị xóa).
    - Nút bị chốt được nhả rồi nhấn lại trong cùng một frame: lần nhả gỡ chốt, lần nhấn được ghi
      thành `Pressed` của epoch mới.
    - Rào không dùng khoảng ân hạn theo thời gian: con số nào cũng hoặc quá ngắn (vẫn rò phím)
      hoặc quá dài (nuốt cú nhảy cố ý).

**Thiết bị và biểu tượng phím**

13. **Thiết bị đang dùng.** Là thiết bị cuối cùng có **tín hiệu có nghĩa** (F4): nhấn một nút
    (không tính nhả); cò vượt `trigger_threshold`; cần vượt ngưỡng hiệu dụng
    `max(stick_switch_threshold, dead_zone + 0,15)`, luôn cao hơn dead zone kể cả khi người chơi
    tăng dead zone, để chống drift; chuột click. **Di chuột chỉ tính ở chế độ Menu và Map**
    (quãng di quy về độ phân giải tham chiếu: `Δp_ref = Δp × 1080 / chiều_cao_màn_hình`), để người
    cầm tay cầm lỡ chạm chuột không làm biểu tượng nhấp nháy. RT làm phím Né phụ được coi là "nhấn"
    ở cùng `trigger_threshold`. Click bị bỏ qua lúc có lại focus (Rule 15) không tính là tín hiệu.
    - Lúc khởi động chưa có tín hiệu: mặc định `Gamepad` nếu đang cắm tay cầm, ngược lại
      `KeyboardMouse`.
    - `DeviceChanged` phát **tối đa một lần mỗi bước input**, ở cuối bước, và chỉ khi giá trị cuối
      bước khác giá trị đầu bước. Bàn phím và tay cầm cùng có tín hiệu trong một frame: thiết bị
      của tín hiệu xử lý sau cùng thắng.
    - Có hai tay cầm: cả hai đều điều khiển được (Rule 3); kiểu biểu tượng theo tay cầm có tín hiệu
      có nghĩa gần nhất. Cắm tay cầm vào không phải là tín hiệu: thiết bị đang dùng chỉ đổi ở lần
      bấm có nghĩa đầu tiên.

14. **Biểu tượng phím không sai dù một khung hình**, đo được là: frame đầu tiên UI vẽ biểu tượng
    mới bằng đúng frame Input phát `DeviceChanged`. `DeviceChanged` được phát trong bước xử lý
    input, **trước** `Update` của gameplay và trước khi UI vẽ, nên prompt và HUD đổi biểu tượng
    ngay trong frame đó. **[giả định: bước cập nhật input chạy trước `MonoBehaviour.Update`]**
    UI cập nhật theo sự kiện, không tự đi hỏi thiết bị. Input cung cấp `GetGlyph(action)`: phím
    đang gán cho hành động đó trên thiết bị đang dùng, dạng dữ liệu hiển thị; UI tự vẽ. Prompt
    đang chạy hiệu ứng thì chỉ thay biểu tượng, không crossfade (crossfade là đúng một khung hình
    hiện sai). Con trỏ chuột chỉ hiện khi thiết bị đang dùng là `KeyboardMouse`.
    - **Kiểu biểu tượng tay cầm:** theo layout thiết bị báo; layout chung hoặc không rõ thì dùng
      kiểu Xbox. Settings có thể ghi đè (`SetGlyphFamily(Auto | Xbox | PlayStation)`), vì Steam Input
      có thể báo tay cầm PlayStation là Xbox **[giả định]**.

15. **Rút tay cầm, mất focus, frame đầu.**
    - **Rút tay cầm:** mọi nút đang giữ từ tay cầm đó bị chốt và được đặt `last_release_seq` (để
      `WasReleasedAfter` trả đúng, ví dụ cú nhảy bị cắt), không ghi `Released`. Nếu cùng hành động
      đang được giữ bằng thiết bị khác thì `IsHeld` vẫn đúng. Nếu đó là tay cầm dùng gần nhất: thiết
      bị đang dùng chuyển về `KeyboardMouse` và Input phát `DeviceLost`; rút tay cầm kia thì chỉ chốt
      nút của nó. Input **không** tự tạm dừng game; hệ đăng ký `DeviceLost` (Menu & Navigation)
      quyết định có mở menu tạm dừng hay không.
    - **Mất focus (Alt-Tab):** mọi nút đang giữ bị chốt và được đặt `last_release_seq`, `IsHeld` trả
      sai, trạng thái lặp Navigate bị xóa; không ghi `Released`, không đổi chế độ, không tự tạm dừng.
      **[giả định: khi mất focus có thể không nhận được sự kiện nhả]**
    - **Có lại focus:** phím vẫn đang giữ tiếp tục bị chốt tới khi nhả; Move đọc lại ngay. Lần click
      chuột trong bước input có lại focus bị bỏ qua, để click vào cửa sổ không thành một nhát Attack.
    - **Frame đầu tiên:** bộ đệm rỗng, `epoch` = 0; mọi nút đang giữ lúc khởi tạo bị chốt như qua
      một rào.

**Menu, đổi phím, khởi tạo**

16. **Lặp phím khi điều hướng menu.** Giữ Navigate theo một hướng: phát một bước ngay, rồi lặp
    sau `nav_repeat_delay`, sau đó mỗi `nav_repeat_interval` (F5), theo thời gian thật. Với cần
    analog, "một hướng" nghĩa là độ lớn sau dead zone ≥ `nav_threshold` và hướng rơi vào một trong
    bốn góc phần tư 90° quanh lên/xuống/trái/phải; đổi phần tư thì đếm lại từ đầu. Mỗi frame phát
    **tối đa một bước** (`nav_max_steps_per_frame`), để một frame khựng dài không làm con trỏ nhảy
    nhiều ô cùng lúc.

17. **API đổi phím.** Input không lưu gì xuống đĩa và không đọc Settings (TD-B8).
    - `ApplyBindingOverrides(data)`: Settings gọi lúc khởi động, đẩy cấu hình đã lưu vào.
    - `ExportBindingOverrides()`: trả dữ liệu để Settings tự lưu.
    - `StartRebind(action, slot)`: chờ người chơi bấm phím mới cho một ô gán. Tập xét xung đột là
      mọi hành động có chung ít nhất một chế độ với hành động đang đổi (kể cả từng phím của Move).
      Không xung đột → áp dụng ngay. Có xung đột → trả `Conflict` kèm danh sách, chưa áp dụng;
      Settings gọi `CommitRebind(Swap | Cancel)`. Không cho phép hai hành động cùng chế độ giữ một
      phím. Phím trùng giữa hai chế độ khác nhau không phải xung đột (Rule 11).
    - Mọi hành động, kể cả hành động menu, Plunge và DropThrough, đều đổi được, **trừ ô cố định của
      Pause và Cancel** (quyết định I-7, 2026-10-30): Esc và Menu/Options luôn gán cho Pause/Cancel,
      không hành động nào khác lấy được hai phím này (kể cả bằng Swap); người chơi vẫn thêm được phím
      phụ cho Pause/Cancel. Esc trong lúc rebind là hủy rebind. Rebind làm Navigate, Confirm hoặc
      Cancel không còn phím nào trên một thiết bị bị từ chối. Người chơi không bao giờ tự khóa mình
      khỏi menu.
    - Nút đơn của Plunge trùng nút Attack là xung đột (cùng Gameplay). Dạng tổ hợp tham chiếu tới
      hành động gốc chứ không tới phím, nên đổi phím Attack thì Plunge dạng tổ hợp đi theo.
    - **Đổi phím tạo rào:** mọi thay đổi gán phím (`ApplyBindingOverrides`, `ResetBindings`, rebind
      hoàn tất) tăng `epoch` và chốt nút đang giữ, nên không bản ghi nào trong bộ đệm mang nghĩa cũ.
    - Rebind bị hủy với `Cancelled(ModeChanged)` nếu đỉnh stack đổi giữa chừng, và `Cancelled(DeviceLost)`
      nếu tay cầm của ô đang đổi bị rút. Phím dùng để chọn khi rebind hoàn tất bị chốt tới khi nhả,
      để lần nhả không kích hoạt Confirm.
    - **Kiểm tra khi nạp:** ID hành động không biết → bỏ qua, log; đường dẫn phím sai → ô đó về mặc
      định; dữ liệu không đọc được → dùng toàn bộ mặc định và báo lỗi cho Settings. Sau khi nạp, chạy
      kiểm tra xung đột và ô cố định Pause/Cancel; hành động vi phạm về mặc định và có trong danh
      sách trả về.
    - `ResetBindings(action | all)`.
    - Cấu hình gán phím được khóa theo **ID ổn định của hành động**, không theo tên hiển thị, để
      đổi tên hành động không làm hỏng cấu hình đã lưu (phụ thuộc ADR TD-B4 về ID ổn định).
    - `SetDeadZone(value)`, `SetHoldMode(action, mode)`, `SetGlyphFamily(family)`: Settings đẩy vào.

18. **Một instance, không singleton tĩnh (TD-C).** Input là một instance do composition root tạo
    và inject vào các hệ dùng nó. Khi bị hủy, nó gỡ mọi callback đã đăng ký (kể cả sự kiện tĩnh
    của engine như thay đổi thiết bị và focus), tắt các action và xóa cấu hình gán phím đã áp lên
    asset, vì Enter Play Mode tắt domain reload sẽ giữ trạng thái cũ giữa các lần Play. Instance
    mới đặt lại `seq` và `epoch`, xóa cấu hình cũ trước khi áp cấu hình mới; handle của instance cũ
    bị từ chối (Rule 11). Phần lõi (bộ đệm, stack chế độ, theo dõi thiết bị) không phụ thuộc kiểu Unity và
    nhận sự kiện qua một nguồn sự kiện trừu tượng, để unit test chạy không cần thiết bị. Cấu trúc
    cụ thể (adapter, asset hành động, update mode) → ADR "Input architecture".

### States and Transitions

**Chế độ input** (đỉnh stack quyết định chế độ hiệu lực):

| Chế độ | Ai đẩy vào | Ai gỡ ra | Hành động nhận | Ghi chú |
|---|---|---|---|---|
| Gameplay | luôn có ở đáy | không bao giờ | bảng Gameplay | |
| Menu | Menu & Navigation (Pause; mở kho, lò rèn, chợ qua Interact) | chính hệ đó, khi Cancel hoặc chọn xong | Navigate, Confirm, Cancel, Tab, Point/Click | |
| Dialogue | Dialogue & Discovery Hints | chính hệ đó | Navigate, Confirm, Cancel | dùng chung hành động với Menu |
| Fishing | hệ câu cá, sau Interact hợp lệ ở chỗ câu | hệ câu cá, khi câu xong, sổng cá hoặc Cancel | Reel, Cancel | Esc/Menu = Cancel = bỏ câu (I-8); `Menu` có thể bị đẩy lên trên (ví dụ `DeviceLost`) |
| Map | hệ hiển thị bản đồ thế giới, khi Map | chính hệ đó, khi Map hoặc Cancel | Map, Cancel, Navigate, Point/Click | Esc/Menu = Cancel (I-8) |
| Locked | Region Loading, Cutscene, day banner | chủ handle | không gì | gỡ `Locked` của Region Loading = "trả input" |

| Sự kiện | Kết quả |
|---|---|
| `Push(mode)` | chế độ mới thành đỉnh → rào (Rule 12) |
| `Pop(handle)` của đỉnh | chế độ bên dưới thành đỉnh → rào |
| `Pop(handle)` không ở đỉnh | chế độ đó rời stack, đỉnh không đổi, không rào |
| `Pop(handle)` không hợp lệ hoặc đã gỡ | bỏ qua, log cảnh báo (Edge Cases) |

**Thiết bị đang dùng:** `KeyboardMouse` ⇄ `Gamepad`, chuyển theo Rule 13; `Gamepad` → `KeyboardMouse`
khi rút tay cầm (Rule 15). Thiết bị đang dùng không ảnh hưởng chế độ và không chặn input.

### Interactions with Other Systems

| Hệ | Gửi vào Input | Nhận từ Input | Chủ giao diện |
|---|---|---|---|
| Character Controller | đăng ký cửa sổ lớn nhất + loại đồng hồ | `TryConsume`/`Peek` cho Jump, Attack, Dodge, Plunge, DropThrough; `WasReleasedAfter`; `ReadMove` | Input sở hữu API. Controller quyết lần bấm có hiệu lực, độ dài cửa sổ, hủy đòn (TD-A3), và Plunge/DropThrough trên mặt đất nghĩa là gì |
| Interaction & Context Prompts | — | `TryConsume(Interact)`; `GetGlyph`, `DeviceChanged` | Input |
| Menu & Navigation Framework | Push/Pop `Menu`; đăng ký `DeviceLost` | Navigate (có lặp), Confirm, Cancel, Tab, Point/Click | Input; Menu quyết có tạm dừng khi rút tay cầm |
| Settings & Accessibility | lúc khởi động: `ApplyBindingOverrides`, `SetDeadZone`, `SetHoldMode` (TD-B8); `StartRebind`, `ResetBindings` | dữ liệu gán phím để lưu; danh sách xung đột | Input sở hữu API; Settings lưu và dựng màn hình |
| Region Loading | Push/Pop `Locked` | — | Region Loading giữ handle |
| Câu cá *(hệ chưa có trong systems index)* | Push/Pop `Fishing` | Reel (nhấn, nhả, `IsHeld`), Cancel | Input; hệ câu cá quyết cửa sổ cắn câu |
| Dialogue & Discovery Hints | Push/Pop `Dialogue` | Navigate, Confirm, Cancel | Input |
| Cutscene & Scripted Sequence | Push/Pop `Locked` | — | Cutscene; có cho bỏ qua cutscene hay không: Open Questions |
| Hiển thị bản đồ thế giới *(chưa có chủ trong systems index)* | Push/Pop `Map` | Map, Cancel, Navigate, Point/Click | Input |
| Dùng vật phẩm tiêu hao *(chưa có chủ: Item & Inventory hoặc Combat)* | — | `TryConsume(Heal)`, `TryConsume(EatBuff)` | Input; chủ thật xác định khi viết GDD tương ứng |
| HUD | — | `GetGlyph`, `DeviceChanged` | Input |
| Telemetry | — | quan sát, bộ đếm tách riêng: đã ghi / đã tiêu thụ / hết hạn chưa tiêu thụ / bị ghi đè trước hạn / bị rào xóa; histogram độ trễ từ lúc bấm tới lúc tiêu thụ; số lần đổi chế độ, đổi thiết bị (kèm chỉ số frame) | Telemetry quan sát; Input không phụ thuộc Telemetry |
| Game Time & Calendar | — | — | Không liên quan trực tiếp (Game Time GDD, bảng phụ thuộc). Input không dùng đồng hồ game |

## Formulas

*Nguồn: systems-designer, 2026-10-30, dựa trên Section C. Quyết định I-5 (người dùng, 2026-10-30):
mỗi hành động tổ hợp một góc "Xuống" riêng. Mọi con số thuộc cửa sổ của bên tiêu thụ chỉ là
**tham chiếu** (prototype: hủy đòn ~0,03–0,10 s trong đòn ~0,20 s; đệm nhảy/né phổ biến trong
thể loại ~0,10–0,15 s), không phải quyết định của GDD này.*

### F1. Thuộc cửa sổ đệm

The buffer_window_membership formula is defined as:

`eligible(e) = (e.phase = Pressed) ∧ ¬e.consumed ∧ (e.epoch = epoch_now) ∧ (e.action = a ∨ e.baseAction = a) ∧ (now_clock − e.t_clock ≤ window) ∧ (now_real − e.t_real ≤ buffer_retention)`

`selected = argmin_seq { e | eligible(e) }` (bản ghi cũ nhất đủ điều kiện)

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `a` | enum | bảng Rule 2 | consumer-declared | tên hành động được hỏi |
| `window` | float (s) | 0 – `buffer_retention` | consumer-declared | cửa sổ của lần gọi |
| `clock` | enum | `Real` / `Game` | consumer-declared | chọn cặp (`now_clock`, `e.t_clock`) |
| `now_real`, `e.t_real` | int64 tick (1 µs) | ≥ 0, đơn điệu | engine (đồng hồ inject) | thời gian thật |
| `now_game`, `e.t_game` | int64 tick (1 µs) | ≥ 0, đơn điệu | engine (đồng hồ inject) | thời gian có scale |
| `epoch_now` | int64 | ≥ 0 | calculated | số lần đổi handle ở đỉnh stack và đổi phím (Rule 12, 17) |
| `buffer_retention` | float (s) | 0,5–2,0 | data file | hạn giữ của Input, mặc định 1,0 |
| `selected` | bản ghi / không có | — | calculated | bản ghi được trả về và đánh dấu đã tiêu thụ |

**Output Range:** đúng một bản ghi hoặc "không có"; không có bản ghi đủ điều kiện thì trả "không
có", không lỗi. Cả hai so sánh thời gian bao gồm biên (≤).

**Hit-stop:** trong một hit-stop dài `d` giây thật với timeScale `s`, đồng hồ `Real` đi `d` còn
đồng hồ `Game` chỉ đi `d·s`.

**Example:** bấm ngay đầu một hit-stop 0,07 s ở `s` = 0,05, cửa sổ 0,12 s.
- `Real`: hết hit-stop còn 0,12 − 0,07 = **0,05 s** (khoảng 3 frame).
- `Game`: trong hit-stop đồng hồ chỉ đi 0,07 × 0,05 = 0,0035 s, nên hết hit-stop còn **0,1165 s**.
  Tổng thời gian thật lần bấm còn sống là 0,1865 s < 1,0 s, nên `buffer_retention` không cắt.

**Sai số đóng dấu:** lần bấm được đóng dấu ở lần xử lý input kế tiếp, tức chậm tối đa `Δframe`
so với lúc bấm thật. Lần bấm trông "trẻ" hơn tuổi thật, nên cửa sổ hiệu dụng tính từ lúc bấm thật
nằm trong `[window, window + Δframe]`. Sai lệch luôn theo hướng dễ dãi, đúng tinh thần "không
phạt người chơi vì tay" (Player Fantasy). Ở đồng hồ `Game` trong lúc hit-stop, sai số chỉ còn
`Δframe·s` ≈ 0,8 ms.

### F2. Cảnh báo thời gian giữ lại, và sức chứa

The retention_warning formula is defined as:

`warn = (W_real_worst > buffer_retention)`, với
`W_real_worst = W` nếu `clock = Real`;
`W_real_worst = W + hitstop_budget × (1 − s_min)` nếu `clock = Game`

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `W` | float (s) | > 0 | consumer-declared | cửa sổ lớn nhất bên tiêu thụ đã đăng ký |
| `hitstop_budget` | float (s) | 0–0,5 | data file | tổng thời gian thật của hit-stop giả định rơi vào một cửa sổ; mặc định 0,21 (3 × 0,07) |
| `s_min` | float | 0,05 | constant (giá trị prototype; Combat xác nhận) | timeScale nhỏ nhất của hit-stop |
| `W_real_worst` | float (s) | `W` – `W/s_min` | calculated | thời gian thật dài nhất cửa sổ có thể kéo dài |

**Output Range:** bool; chỉ log, không chặn đăng ký. Nếu không chặn bằng `hitstop_budget`, chuỗi
hit-stop liền nhau đẩy `W_real_worst` tới `W/s_min` (0,12/0,05 = 2,4 s). Nếu thực tế vượt mức giả
định, `buffer_retention` cắt bản ghi sớm hơn cửa sổ khai báo; cảnh báo đã được log từ lúc đăng ký.

**Example:** `W` = 0,15 s theo `Game` → 0,15 + 0,21 × 0,95 = **0,35 s** ≤ 1,0 s, không cảnh báo.

The buffer_capacity_bound formula is defined as:

`buffer_capacity ≥ ceil(2 × r_press_max × buffer_retention)`, làm tròn lên lũy thừa của 2

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `r_press_max` | float (lần/s) | 15 | constant (định cỡ) | tốc độ bấm dồn tối đa của người (~10–15 lần/s) |
| hệ số 2 | int | 2 | constant | mỗi lần bấm sinh một bản ghi nhấn và một bản ghi nhả |
| `buffer_retention` | float (s) | 0,5–2,0 | data file | như F1 |
| `buffer_capacity` | int | 32–128 | data file | sức chứa vòng đệm, mặc định 32 |

**Output Range:** 2 × 15 × 1,0 = 30 → **32**. Bản ghi tổ hợp tính là một. Tăng `buffer_retention`
lên 2,0 s thì `buffer_capacity` phải lên 64. Vi phạm ràng buộc này thì bản ghi bị ghi đè trước khi
hết hạn.

**Example:** người chơi bấm dồn Attack 15 lần trong 1 giây → 30 bản ghi; vòng 32 vẫn giữ đủ, bản
ghi cũ nhất chỉ bị ghi đè khi đã quá 1,0 s.

### F3. Điều kiện "Xuống"

The down_qualifier formula is defined as:

`down(action) = (|m| > 0) ∧ (m.y ≤ −down_threshold) ∧ (−m.y / |m| ≥ cos(angle_action))`,
với `m` = Move sau dead zone (F4); `angle_action` = `down_angle_plunge` cho Plunge,
`down_angle_drop` cho DropThrough

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `m` | Vector2 | ‖m‖ ≤ 1 | calculated (F4) | Move sau dead zone |
| `down_threshold` | float | 0,3–0,7 | data file | mặc định 0,5 |
| `down_angle_drop` | float (°) | 25–44 | data file | mặc định 35; dưới 45° nên đường chéo bàn phím không tính là Xuống |
| `down_angle_plunge` | float (°) | 45–60 | data file | mặc định 50; từ 45° trở lên nên đường chéo bàn phím vẫn tính là Xuống |

**Output Range:** bool; `m = 0` → `false` (không chia cho 0). Ở góc θ, độ lớn sau dead zone cần
đạt `down_threshold / cos θ` (0,61 ở 35°, 0,78 ở 50°).

**Example:**
- Cần thô lệch 30° khỏi hướng xuống, độ lớn 0,8 → sau dead zone (0,8 − 0,2)/0,8 = 0,75 → y = −0,65
  ≤ −0,5, góc 30° → Xuống với cả hai hành động.
- Bàn phím S → (0, −1), góc 0° → Xuống với cả hai.
- Bàn phím S+D → (0,707; −0,707), góc 45° → **không** Xuống với DropThrough (35°), **có** Xuống
  với Plunge (50°).
- Giả định composite bàn phím đã chuẩn hóa về độ dài 1 **[giả định]**; nếu không, kẹp ‖v‖ ≤ 1 trước.

### F4. Dead zone và tín hiệu đổi thiết bị

The move_dead_zone formula is defined as:

`out = 0` nếu `r < dead_zone`; ngược lại `out = (v / r) × (r − dead_zone) / (1 − dead_zone)`,
với `r = min(|v|, 1)` (dead zone tròn, có rescale)

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `v` | Vector2 | ‖v‖ 0–~1,4 | engine | giá trị thô của cần (hoặc composite bàn phím) |
| `dead_zone` | float | 0,05–0,4 | data file (Settings đẩy vào) | mặc định 0,2 |
| `out` | Vector2 | ‖out‖ 0–1 | calculated | Move / Navigate sau dead zone |

**Output Range:** ‖out‖ ∈ [0, 1], liên tục tại `r = dead_zone`. `dead_zone` bị kẹp ≤ 0,4 khi
Settings đẩy vào (tránh chia cho số gần 0).

**Example:** `v` = (0,3; −0,4) → `r` = 0,5; hướng `v/r` = (0,6; −0,8); hệ số (0,5 − 0,2)/0,8 =
0,375 → `out` = (0,6; −0,8) × 0,375 = **(0,225; −0,3)**. Cần ở `r` = 0,15 < 0,2 → `out` = (0; 0).

The device_switch_signal formula is defined as:

`signal = buttonDown ∨ (trigger ≥ trigger_threshold) ∨ (|stick_raw| ≥ stick_switch_eff) ∨ mouseClick ∨ (mode ∈ {Menu, Map} ∧ Σ|Δp| trong mouse_switch_time > mouse_switch_pixels)`,
với `stick_switch_eff = max(stick_switch_threshold, dead_zone + 0,15)`

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `stick_raw` | Vector2 | ‖·‖ 0–~1,4 | engine | giá trị thô của cần (trước dead zone) |
| `trigger_threshold` | float | 0,15–0,5 | data file | mặc định 0,3; cũng là điểm "nhấn" của RT khi làm Né phụ |
| `stick_switch_threshold` | float | 0,4–0,8 | data file | mặc định 0,5 |
| hằng 0,15 | float | 0,15 | constant | khoảng cách tối thiểu giữa ngưỡng đổi thiết bị và dead zone |
| `mouse_switch_pixels` | float (px) | 4–24 | data file | mặc định 8, ở độ phân giải tham chiếu 1080p (quy đổi theo chiều cao màn hình) |
| `mouse_switch_time` | float (s) | 0,1–0,5 | data file | mặc định 0,25, thời gian thật |

**Output Range:** bool. Một tín hiệu đúng từ thiết bị khác thiết bị đang dùng → đổi thiết bị và
phát `DeviceChanged` (Rule 13).

**Example:** người chơi đặt `dead_zone` = 0,4 → `stick_switch_eff` = max(0,5; 0,55) = 0,55. Cần
drift ở 0,35 không đổi thiết bị; đẩy cần tới 0,6 thì đổi.

### F5. Lặp Navigate

The nav_repeat_steps formula is defined as:

`steps(t) = 1 + max(0, floor((t − t0 − nav_repeat_delay) / nav_repeat_interval) + 1)` khi
`t ≥ t0 + nav_repeat_delay`; `steps(t) = 1` khi `t0 ≤ t < t0 + nav_repeat_delay`

**Variables:**

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `t`, `t0` | float (s) | ≥ 0 | engine (thời gian thật) | bây giờ / lúc bắt đầu giữ một hướng |
| `nav_repeat_delay` | float (s) | 0,25–0,6 | data file | mặc định 0,35 |
| `nav_repeat_interval` | float (s) | 0,05–0,15 | data file | mặc định 0,09 |
| `nav_threshold` | float | 0,3–0,7 | data file | độ lớn sau dead zone để tính là "đang giữ một hướng"; mặc định 0,5 |
| `nav_max_steps_per_frame` | int | 1 | constant | số bước tối đa phát trong một frame |

**Output Range:** số nguyên ≥ 1, không chặn trên. Tốc độ lặp ổn định 1/`nav_repeat_interval` ≈ 11
bước/s. Mỗi frame phát `min(steps(t) − steps_đã_phát, nav_max_steps_per_frame)`, rồi đặt
`steps_đã_phát := steps(t)`: **bước dư trong một frame bị bỏ**, không dồn sang các frame sau, nên sau
một frame khựng con trỏ không chạy tiếp nhiều ô. Phép chia tính trên tick nguyên (Rule 5). Đổi phần
tư hướng hoặc qua rào chuyển chế độ thì `t0` đặt lại.

**Example:** giữ 1,0 s → 1 + (floor(0,65/0,09) + 1) = 1 + 8 = **9 bước**, ở các mốc 0; 0,35; 0,44;
0,53; 0,62; 0,71; 0,80; 0,89; 0,98 s.

### Ràng buộc kiểm tra khi nạp dữ liệu (validator)

- `buffer_capacity ≥ ceil(2 × 15 × buffer_retention)` (F2).
- `down_angle_drop < 45° ≤ down_angle_plunge` (giữ đúng ý I-5 với đường chéo bàn phím).
- `stick_switch_eff > dead_zone` luôn đúng nhờ công thức `max` (F4); `dead_zone ≤ 0,4`.
- `buffer_capacity` phải là lũy thừa của 2; nếu không → mặc định.
- Mọi giá trị nằm trong khoảng an toàn ở Tuning Knobs; ngoài khoảng → dùng mặc định, mỗi giá trị một
  log lỗi.
- Ràng buộc chéo bị vi phạm dù từng giá trị vẫn trong khoảng (ví dụ `buffer_retention` 2,0 với
  `buffer_capacity` 32) → **mọi knob tham gia ràng buộc đó** về mặc định, một log lỗi.
- Validator là hàm thuần: dữ liệu → cấu hình hiệu dụng + danh sách vi phạm (test hook H-10).

## Edge Cases

*Nguồn: systems-designer, 2026-10-30. Mục nào cần đổi luật đã được sửa thẳng vào Section C/D
(ghi chú ở đầu Detailed Design); ở đây ghi kết quả và trỏ về luật.*

### Bộ đệm

- **If `Time.timeScale` = 0 mà không có push nào**: vi phạm Rule 10 (hệ dừng game phải push `Menu`
  hoặc `Locked`). Nếu vẫn xảy ra: Input vẫn ghi; cửa sổ theo `Game` luôn đúng, chỉ `buffer_retention`
  cắt; lần bấm trong ≤ 1,0 s đóng băng bung ra khi chạy lại. Development build và Editor log cảnh
  báo khi đồng hồ `Game` không tiến quá `buffer_retention` giây thật mà đỉnh stack vẫn là `Gameplay`
  (Rule 10, AC-R20).
- **If một frame khựng lâu hơn `buffer_retention` (ví dụ 1,5 s nạp tài nguyên)**: mọi lần bấm trong
  lúc khựng được xử lý ở frame kế, cùng một mốc thời gian (tuổi 0), `seq` theo thứ tự sự kiện; chúng
  còn hiệu lực. Bản ghi có từ trước lúc khựng thì hết hạn. Mỗi `TryConsume` vẫn chỉ lấy một bản ghi.
  Sai lệch theo hướng dễ dãi (F1).
- **If vòng đệm đầy vì bấm dồn**: bản ghi cũ nhất bị ghi đè, kể cả khi chưa tiêu thụ và chưa hết hạn
  (Rule 6). `last_release_seq` không mất (Rule 8). Telemetry đếm "ghi đè trước hạn"; khác 0 nghĩa là
  `r_press_max` của F2 đặt thấp.
- **If một hành động được nhấn rồi nhả trong cùng một frame**: hai bản ghi, `Pressed` (n) và
  `Released` (n+1), cùng mốc. `TryConsume` trả bản `Pressed`; `WasReleasedAfter(n)` đúng (nhảy thấp);
  `IsHeld` sai.
- **If `TryConsume` được gọi hai lần trong một frame**: lần hai trả bản ghi đủ điều kiện kế tiếp hoặc
  "không có", không bao giờ trả lại cùng bản ghi. `Peek` rồi `TryConsume` trong cùng frame trả cùng
  một bản ghi.
- **If bên tiêu thụ hỏi hành động không thuộc chế độ ở đỉnh**: "không có", không log; `ReadMove()`
  trả (0, 0) ngoài Gameplay (Rule 8).
- **If `window` lúc gọi vượt cửa sổ đã đăng ký hoặc `buffer_retention`, hoặc âm**: bị
  `buffer_retention` cắt / coi là 0; development build cảnh báo một lần mỗi cặp (Rule 7).
- **If Xuống được nhấn sau Attack trong cùng một frame**: tính là đồng thời, ghi Plunge (Rule 4).
- **If Xuống được nhả hay nhấn giữa lúc bấm và lúc tiêu thụ**: bản ghi giữ nguyên như lúc bấm; Plunge
  vẫn là Plunge, Attack vẫn là Attack (Rule 4); `move` là ảnh chụp lúc bấm (Rule 5).
- **If Plunge bị đổi thành nút đơn khi còn bản ghi tổ hợp trong bộ đệm**: đổi phím tạo rào (Rule 17),
  bản ghi cũ bị vô hiệu. Luồng bình thường không xảy ra vì đổi phím diễn ra ở `Menu`.
- **If Plunge đang gán nút đơn mà người chơi bấm Attack lúc giữ Xuống**: ghi Attack thường (Rule 4).

### Chế độ

- **If `Pop` nhận handle không hợp lệ, đã gỡ, hoặc của instance khác**: bỏ qua, trả `false`, log cảnh
  báo, không rào (Rule 11).
- **If chủ handle bị hủy mà chưa `Pop`**: mục bị tự gỡ ở bước input kế tiếp, log lỗi; rào nếu nó ở
  đỉnh (Rule 11). Không có luật này, một `Locked` bị bỏ rơi làm kẹt game vĩnh viễn.
- **If `Push` và `Pop` trong cùng một frame**: hai rào, `epoch` tăng 2, kể cả khi stack quay về đúng
  trạng thái cũ (Rule 12). Kết quả luôn xác định.
- **If hai chủ cùng đẩy một loại chế độ**: hai mục riêng; gỡ mục trên dựng rào vì handle ở đỉnh đổi
  (Rule 12).
- **If một lần bấm đến cùng frame với một `Push`**: bước input chạy trước `Update`, lần bấm mang epoch
  cũ và bị rào xóa. Push/pop gọi từ handler sự kiện của Input được hoãn tới cuối bước input (Rule 12).
- **If `Locked` được đẩy lên khi đang giữ Reel**: Reel bị chốt, `IsHeld(Reel)` sai ngay, không ghi
  `Released`; sau khi gỡ `Locked` vẫn chốt tới khi nhả. Hệ câu cá biết qua `TopChanged` để dừng tính
  lực căng (Rule 11); GDD câu cá phải định nghĩa cá làm gì trong lúc đó.
- **If `Menu` bị đẩy lên trên `Fishing` (ví dụ Menu & Navigation xử lý `DeviceLost`) rồi gỡ ra**: hai rào; hệ câu cá không nhận gì khi `Menu` ở
  đỉnh. Toggle của Reel tạm ẩn rồi được khôi phục (Rule 9, I-6); Reel giữ bằng tay (chế độ `Hold`) bị
  chốt tới khi nhả rồi bấm lại.
- **If nút bị chốt được nhả rồi nhấn lại trong cùng một frame**: lần nhả gỡ chốt, lần nhấn được ghi
  ở epoch mới (Rule 12).

### Thiết bị

- **If tay cầm bị rút khi đang giữ nút**: nút bị chốt, `last_release_seq` được đặt, không ghi
  `Released`; giữ cùng hành động bằng bàn phím thì `IsHeld` vẫn đúng (Rule 15).
- **If tay cầm bị rút giữa lúc rebind**: rebind ô của tay cầm bị hủy `Cancelled(DeviceLost)`, không
  đổi phím nào; rebind ô bàn phím chạy tiếp (Rule 17).
- **If có hai tay cầm**: cả hai điều khiển được; biểu tượng theo tay cầm dùng gần nhất; `DeviceLost`
  chỉ phát khi rút tay cầm dùng gần nhất (Rule 13, 15).
- **If bàn phím và tay cầm có tín hiệu trong cùng một frame**: tín hiệu xử lý sau cùng thắng;
  `DeviceChanged` tối đa một lần, ở cuối bước (Rule 13).
- **If Steam Input báo tay cầm PlayStation là Xbox**: hiện kiểu Xbox, trừ khi Settings ghi đè kiểu
  biểu tượng (Rule 14).
- **If tay cầm được cắm sau frame đầu tiên**: không đổi thiết bị cho tới lần bấm có nghĩa đầu tiên
  (Rule 13).
- **If ứng dụng mất focus khi đang giữ phím**: mọi nút bị chốt, `last_release_seq` được đặt, `IsHeld`
  sai, lặp Navigate bị xóa; không đổi chế độ, không tự tạm dừng (Rule 15).
- **If ứng dụng có lại focus**: phím đang giữ vẫn chốt tới khi nhả; Move đọc lại ngay; click lấy lại
  focus bị bỏ qua (Rule 15).

### Đổi phím

- **If người chơi chọn phím đang dùng cho hành động khác cùng chế độ**: `Conflict`, chưa áp dụng;
  Settings chọn `Swap` hoặc `Cancel` (Rule 17).
- **If chọn phím chỉ dùng ở chế độ khác (ví dụ Enter của Menu cho một hành động Gameplay)**: được
  phép, không phải xung đột.
- **If người chơi cố gán Esc hoặc Menu/Options cho hành động khác**: bị từ chối, kể cả bằng Swap; Esc
  trong lúc rebind là hủy (Rule 17, I-7).
- **If một rebind làm Navigate, Confirm hoặc Cancel không còn phím nào trên một thiết bị**: bị từ chối
  (Rule 17).
- **If nút đơn của Plunge trùng nút Attack**: xung đột (Rule 17).
- **If dữ liệu gán phím có ID không biết, phím sai, hoặc hỏng**: bỏ qua / về mặc định / toàn bộ mặc
  định kèm báo lỗi; sau đó kiểm tra xung đột và ô cố định (Rule 17).
- **If đang rebind mà đỉnh stack đổi**: `Cancelled(ModeChanged)`; phím vừa chọn bị chốt tới khi nhả
  (Rule 17).

### Đồng hồ và vòng đời

- **If đồng hồ được inject chạy lùi**: kẹp đơn điệu, log lỗi một lần mỗi phiên (Rule 5).
- **If phiên chơi kéo dài nhiều giờ**: mốc thời gian `double`, `seq`/`epoch` 64 bit; không mất độ
  chính xác trong phạm vi một phiên (Rule 5).
- **If là frame đầu tiên**: bộ đệm rỗng, `epoch` = 0, nút đang giữ bị chốt; frame đầu thường dài nên
  áp dụng mục "frame khựng" ở trên (Rule 15).
- **If vào lại Play Mode khi tắt domain reload**: instance mới, `seq`/`epoch` đặt lại; handle cũ bị từ
  chối; đăng ký tĩnh và cấu hình gán phím cũ đã bị gỡ khi hủy instance cũ (Rule 18).

## Dependencies

### Upstream (Input phụ thuộc vào)

| Phụ thuộc | Loại | Giao diện | Ghi chú |
|---|---|---|---|
| Unity Input System package (`com.unity.inputsystem` 1.20.1) | cứng (engine) | action, binding, callback, sự kiện thiết bị | Tài liệu engine chỉ xác minh cho 1.11 → Open Questions; *Active Input Handling* đang là *Both* |
| ADR TD-B4 (sơ đồ ID ổn định) | mềm | ID ổn định của hành động, dùng làm khóa của cấu hình gán phím | Không có ADR này thì đổi tên hành động làm hỏng phím đã lưu (Rule 17). Chưa viết |
| ADR "Input architecture" (đề xuất) | mềm | update mode, nguồn đồng hồ, cấu trúc adapter/lõi, chủ của asset hành động | Chưa viết; chặn story đầu tiên của Input (Open Questions) |

Input **không phụ thuộc hệ thiết kế nào** (systems index: Foundation, "Depends On: —"). Nó không đọc
Settings (TD-B8), không đọc Game Time (Rule 10), không biết hệ nào đẩy chế độ ngoài handle nhận được.

### Downstream (hệ phụ thuộc vào Input)

| Hệ | Loại | Dùng gì | GDD | Có ghi Input trong systems index? |
|---|---|---|---|---|
| Character Controller (#6) | cứng | bộ đệm (`TryConsume`, `Peek`, `WasReleasedAfter`), `ReadMove`, đăng ký cửa sổ | chưa có (viết tiếp theo) | có |
| Skill & Moveset (#14) | gián tiếp qua Controller | Attack, Plunge, **Special** (I-10), `IsHeld` / `WasReleasedAfter` cho tụ lực (HoldGate) | `skill-moveset.md` (Designed) | có (2026-11-05) |
| Interaction & Context Prompts (#10) | cứng | `TryConsume(Interact)`, `GetGlyph`, `DeviceChanged` | chưa có | có |
| Menu & Navigation Framework (#31) | cứng | stack chế độ (`Menu`), Navigate có lặp, Confirm, Cancel, Tab, chuột; `DeviceLost` | chưa có | có |
| Settings & Accessibility (#13) | cứng | API đổi phím, `ApplyBindingOverrides`, `SetDeadZone`, `SetHoldMode`, `SetGlyphFamily` | chưa có | có |
| Region Loading (#8) | cứng | stack chế độ (`Locked`) để "trả input" sau khi chuyển vùng | chưa có | **không** → bổ sung |
| Cutscene & Scripted Sequence (#29) | cứng | stack chế độ (`Locked`) | chưa có | **không** → bổ sung |
| Dialogue & Discovery Hints (#24) | cứng | stack chế độ (`Dialogue`), Navigate, Confirm, Cancel | chưa có | **không** → bổ sung |
| HUD (#30) | mềm | `GetGlyph`, `DeviceChanged` | chưa có | **không** → bổ sung |
| Telemetry (#12) | mềm (quan sát) | sự kiện đổi chế độ, đổi thiết bị, độ trễ bấm → tiêu thụ, ghi đè trước hạn | chưa có | **không** → bổ sung |
| Câu cá *(chưa có trong systems index)* | cứng | stack chế độ (`Fishing`), Reel (`IsHeld`, nhấn/nhả), Cancel, `TopChanged` | chưa có | hệ chưa tồn tại |
| Hiển thị bản đồ thế giới *(chưa có chủ)* | cứng | stack chế độ (`Map`), Map, Navigate, chuột | chưa có | chưa có chủ |
| Dùng vật phẩm tiêu hao *(chưa có chủ: Item & Inventory hoặc Combat)* | cứng | `TryConsume(Heal)`, `TryConsume(EatBuff)` | chưa có | chưa có chủ |

### Kiểm tra hai chiều

- **Game Time & Calendar** (Approved): bảng phụ thuộc của nó ghi "Input — không trực tiếp". Khớp: Input
  không dùng đồng hồ game. Cụm "trả input" trong Game Time (Region Loading, Rule 6 và AC-C04) nay
  được định nghĩa là lúc Region Loading gỡ handle `Locked` (Rule 11). Không cần sửa GDD Game Time.
- **Flags, Conditions & World State** (đang viết): không liên quan.
- Mọi hệ downstream đều chưa có GDD. **Khi viết GDD của chúng, mục Dependencies phải ghi Input** và
  dùng đúng giao diện ở bảng trên.
- **Systems index cần cập nhật (Phase 5, cần duyệt):** thêm Input vào cột *Depends On* của Region
  Loading, Cutscene, Dialogue, HUD, Telemetry; ghi nhận ba hệ chưa có chủ (câu cá, hiển thị bản đồ thế
  giới, dùng vật phẩm tiêu hao). Ba hệ này cũng thuộc việc cập nhật concept theo thế giới mới đang
  còn mở.

## Tuning Knobs

*Nguồn: danh sách knob systems-designer đề xuất cùng Section D (2026-10-30). Mọi giá trị nằm trong
file dữ liệu cấu hình của Input (định dạng theo ADR TD-B4), không viết cứng trong code. Giá trị ngoài
khoảng an toàn → dùng mặc định và log lỗi (validator, Formulas).*

### Knob của designer

| Knob | Mặc định | Khoảng an toàn | Ảnh hưởng gameplay | Quá cao | Quá thấp | Nguồn |
|---|---|---|---|---|---|---|
| `buffer_retention` | 1,0 s | 0,5–2,0 | trần tuổi của mọi lần bấm, bất kể cửa sổ bên tiêu thụ | bộ nhớ đệm lớn hơn; vô hại vì cửa sổ bên tiêu thụ vẫn quyết | cắt cửa sổ của bên tiêu thụ trước hạn (nhất là cửa sổ theo `Game` qua chuỗi hit-stop) | F1, F2 |
| `buffer_capacity` | 32 | 32–128, lũy thừa 2, ≥ F2 | số bản ghi giữ cùng lúc | tốn bộ nhớ (không đáng kể) | bấm dồn làm ghi đè bản ghi chưa hết hạn | F2 |
| `hitstop_budget` | 0,21 s | 0–0,5 | chỉ dùng để cảnh báo lúc đăng ký cửa sổ | cảnh báo giả với cửa sổ hợp lệ | không cảnh báo khi chuỗi hit-stop thực sự vượt retention | F2 (Combat xác nhận) |
| `down_threshold` | 0,5 | 0,3–0,7 | cần đẩy xuống bao nhiêu thì tính là Xuống | khó ra plunge / rơi qua platform bằng cần | chạm nhẹ cần cũng rơi qua platform | F3 |
| `down_angle_drop` | 35° | 25–44° | độ lệch cho phép quanh hướng xuống, DropThrough | ≥ 45°: S+D trên bàn phím rơi qua platform khi đang chạy | phải đẩy cần gần như thẳng đứng | F3, I-5 |
| `down_angle_plunge` | 50° | 45–60° | như trên, cho Plunge | đẩy chéo ngang cũng ra plunge ngoài ý muốn | < 45°: S+D trên không không ra plunge | F3, I-5 |
| `dead_zone` | 0,2 | 0,05–0,4 | vùng chết tròn của cần (Settings cho người chơi chỉnh) | phải đẩy mạnh mới đi, mất điều khiển tinh | cần drift làm nhân vật tự trôi | F4 |
| `trigger_threshold` | 0,3 | 0,15–0,5 | điểm "nhấn" của cò, gồm RT khi làm Né phụ, và ngưỡng đổi thiết bị | phải bóp sâu mới né | chạm nhẹ cò cũng né | F4 |
| `stick_switch_threshold` | 0,5 | 0,4–0,8 | cần đẩy bao nhiêu thì đổi biểu tượng sang tay cầm | đẩy cần vẫn hiện biểu tượng bàn phím một lúc | drift làm biểu tượng nhấp nháy (được chặn dưới bởi `dead_zone + 0,15`) | F4 |
| `mouse_switch_pixels` | 8 px @1080p | 4–24 | quãng di chuột để đổi sang bàn phím-chuột (chỉ Menu/Map) | phải di chuột xa mới hiện con trỏ | rung tay trên chuột làm biểu tượng lật | F4 |
| `mouse_switch_time` | 0,25 s | 0,1–0,5 | khung thời gian tính quãng di chuột | di chậm lâu cũng đổi | phải di thật nhanh mới đổi | F4 |
| `nav_threshold` | 0,5 | 0,3–0,7 | cần đẩy bao nhiêu thì tính là đang giữ một hướng trong menu | khó cuộn bằng cần | menu tự cuộn vì drift | F5 |
| `nav_repeat_delay` | 0,35 s | 0,25–0,6 | trễ trước khi bắt đầu lặp | cuộn danh sách dài chậm chạp | bấm một lần nhảy hai ô | F5 |
| `nav_repeat_interval` | 0,09 s | 0,05–0,15 | nhịp lặp (≈ 11 bước/s) | cuộn chậm | vượt quá ô muốn chọn | F5 |

### Không phải knob (hằng số có chủ đích)

| Hằng | Giá trị | Lý do |
|---|---|---|
| `nav_max_steps_per_frame` | 1 | Frame khựng không được làm con trỏ nhảy nhiều ô (Rule 16) |
| Khoảng cách tối thiểu ngưỡng đổi thiết bị – dead zone | 0,15 | Bảo đảm drift không đổi thiết bị dù người chơi tăng dead zone (F4) |
| `r_press_max` | 15 lần/s | Chỉ dùng để định cỡ `buffer_capacity` (F2) |
| `s_min` | 0,05 | Giá trị prototype của Combat; GDD Combat sở hữu, Input chỉ tham chiếu (F2) |
| Ô cố định Pause/Cancel | Esc, Menu/Options | Quyết định I-7, không phải tinh chỉnh |

### Knob ảnh hưởng lẫn nhau

- **`buffer_capacity` ↔ `buffer_retention`**: tăng retention phải tăng capacity theo F2 (2,0 s → 64).
  Validator chặn cấu hình vi phạm.
- **`stick_switch_threshold` ↔ `dead_zone`**: khi người chơi tăng dead zone quá 0,35, ngưỡng hiệu dụng
  bị đẩy lên `dead_zone + 0,15` và `stick_switch_threshold` không còn tác dụng.
- **`down_threshold` ↔ hai góc**: ở góc lớn, độ lớn cần đạt `down_threshold / cos θ` (0,78 ở 50°); tăng
  `down_threshold` lên 0,7 làm plunge chéo gần như bất khả (0,7/cos 50° > 1).
- **`down_angle_drop` < 45° ≤ `down_angle_plunge`**: giữ đúng ý I-5 với đường chéo bàn phím. Validator
  chặn cấu hình vi phạm.
- **`hitstop_budget` ↔ `buffer_retention` ↔ cửa sổ bên tiêu thụ**: chỉ ảnh hưởng cảnh báo. Khi Combat
  chốt hit-stop thật, cập nhật `hitstop_budget` và `s_min`.

### Knob thuộc hệ khác (Input chỉ tham chiếu, không sở hữu)

- Cửa sổ đệm nhảy, đệm né, đệm đòn kế và loại đồng hồ của chúng → GDD Character Controller.
- Cửa sổ móc cá (~0,8 s trong prototype) → GDD câu cá.
- Độ dài và timeScale của hit-stop → GDD Combat.

## Visual/Audio Requirements

Input không tự vẽ gì và không phát âm thanh. Yêu cầu hình ảnh duy nhất của nó là **bộ biểu tượng
phím** mà UI dùng khi hiện `GetGlyph(action)`:

| Bộ biểu tượng | Phạm vi | Ghi chú |
|---|---|---|
| Bàn phím + chuột | mọi phím có thể gán (chữ, số, F1–F12, Space, Shift, Ctrl, Alt, Esc, Enter, Backspace, Tab, mũi tên, chuột trái/phải/giữa) | Phím không có biểu tượng riêng → khung phím chung + nhãn chữ |
| Tay cầm Xbox | A, B, X, Y, LB, RB, LT, RT, View, Menu, cần trái/phải (kể cả nhấn cần), D-pad | Cũng là kiểu dự phòng cho tay cầm chung (Rule 14) |
| Tay cầm PlayStation | Cross, Circle, Square, Triangle, L1, R1, L2, R2, Create/Touchpad, Options, cần, D-pad | Settings ghi đè được kiểu biểu tượng |
| Biểu tượng tổ hợp | "Xuống + [nút]" cho Plunge và DropThrough mặc định | UI ghép hai biểu tượng; không vẽ riêng |

- Biểu tượng phải đọc rõ ở độ phân giải thấp nhất game hỗ trợ và hợp phong cách pixel của art bible
  (`design/art/art-bible.md`). Kích thước, bảng màu cụ thể → `/asset-spec`.
- Âm thanh xác nhận hay hủy trong menu thuộc Menu & Navigation, không thuộc Input.
- Chi phí art: ba bộ biểu tượng là việc của Vertical Slice (khi có màn hình đổi phím); Fun-Gate
  dùng được bộ tạm bằng chữ.

> 📌 **Asset Spec** — Visual/Audio requirements are defined. After the art bible is approved, run
> `/asset-spec system:input` to produce per-asset visual descriptions, dimensions, and generation
> prompts from this section.

## Game Feel

Input không tạo ra "cảm giác" của đòn đánh hay cú nhảy; việc đó thuộc Character Controller và Combat.
Phần của Input là **không làm hỏng** cảm giác đó:

- **Không thêm độ trễ.** Lần bấm được ghi ngay trong frame nó được xử lý, trước `Update` của gameplay
  (Rule 10, 14). Độ trễ Input thêm vào chuỗi "bấm → nhân vật phản ứng" là 0 frame ngoài độ trễ của
  engine và phần cứng. Đo được bằng AC-R19 và histogram độ trễ bấm → tiêu thụ (Telemetry).
- **Dễ dãi theo hướng có lợi cho người chơi.** Sai số đóng dấu luôn làm cửa sổ rộng ra, không hẹp đi
  (F1). Bên tiêu thụ chọn được đồng hồ `Game` để hit-stop không ăn mất cửa sổ (Rule 8).
- **Ý định hướng được giữ.** `move` chụp lúc bấm (Rule 5): cú né và plunge đã buffer đi theo hướng
  người chơi muốn lúc bấm, không theo hướng lúc tiêu thụ.
- **Không có gì bị "lạc chỗ".** Rào chuyển chế độ (Rule 12) là điều kiện để nửa cozy cảm thấy thư thả:
  đóng hội thoại không bao giờ thành một nhát chém.
- **Tham chiếu cảm giác:** nhịp ngang hoặc nhanh hơn Hollow Knight (Core Loop › Moment-to-Moment);
  prototype combat đạt PROCEED với ~0,20 s mỗi đòn. Độ dài cửa sổ cụ thể để đạt cảm giác này nằm ở
  GDD Character Controller.

## UI Requirements

Input không sở hữu màn hình nào. Nó cung cấp dữ liệu và API cho các màn hình sau:

| Màn hình / phần tử | Chủ | Input cung cấp | Tier |
|---|---|---|---|
| Gợi ý "[phím] Hành động" ở trạm, ngã rẽ, chỗ câu cá | Interaction & Context Prompts | `GetGlyph`, `DeviceChanged` | MVP (Fun-Gate) |
| Biểu tượng phím trên HUD (hồi máu, ăn buff…) | HUD | `GetGlyph`, `DeviceChanged` | MVP |
| Màn hình đổi phím: danh sách hành động theo chế độ, ô gán theo thiết bị, chờ bấm phím, hộp thoại xung đột (Đổi chỗ / Hủy), đặt lại mặc định | Settings & Accessibility | `StartRebind`, `CommitRebind`, `ResetBindings`, danh sách xung đột, ô cố định Pause/Cancel không cho chọn | Vertical Slice |
| Tùy chọn trợ năng: dead zone, giữ/bật-tắt theo hành động, kiểu biểu tượng tay cầm | Settings & Accessibility | `SetDeadZone`, `SetHoldMode`, `SetGlyphFamily` | Vertical Slice |
| Thông báo khi rút tay cầm | Menu & Navigation | `DeviceLost` | MVP |
| Con trỏ chuột chỉ hiện khi đang dùng bàn phím-chuột | Menu & Navigation | thiết bị đang dùng | MVP |

> **📌 UX Flag — Input**: This system has UI requirements. In Phase 4 (Pre-Production), run
> `/ux-design` to create a UX spec for each screen or HUD element this system contributes to
> **before** writing epics — at minimum the rebinding screen (Settings) and the interaction-prompt
> glyph pattern. Stories that reference UI should cite `design/ux/[screen].md`, not the GDD directly.

## Cross-References

| GDD / tài liệu | Phần được tham chiếu | Tham chiếu ở đâu trong GDD này | Trạng thái |
|---|---|---|---|
| `design/gdd/game-concept.md` | Core Loop › Moment-to-Moment (nhịp HK-or-faster, cancel sang né, không khóa đòn đánh); Pillar 2, 3, 4; anti-pillar *NOT a time-pressure game*; Technical Considerations (input buffer, hit-stop) | Overview, Player Fantasy, Game Feel | Approved |
| `design/gdd/systems-index.md` | Input (#1, Foundation, MVP); TD-A3 (Controller sở hữu máy trạng thái), TD-B8 (Settings đẩy vào Input), TD-C (không singleton tĩnh), TD-B4 (ID ổn định) | Overview, Rule 17–18, Dependencies | — |
| `design/gdd/game-time-calendar.md` | Bảng phụ thuộc ("Input — không trực tiếp"); Rule 6 và AC-C04 ("trả input" sau khi Region Loading chuyển vùng) | Rule 10, 11; Dependencies | Approved; không cần sửa |
| `design/gdd/flags-conditions-world-state.md` | — | — | Không liên quan |
| `prototypes/combat-feel-concept/REPORT.md` | Nhịp ~0,20 s/đòn, cửa sổ hủy đòn 0,03–0,10 s, hit-stop 0,05–0,07 s ở timeScale 0,05–0,08 | Formulas (tham chiếu), Game Feel | PROCEED |
| `prototypes/day-loop-concept/README.md` | Minigame câu cá (thả câu, cắn câu "!", giữ để nâng), phím prototype | Rule 2 (I-1), States (Fishing) | Chưa đóng (không có REPORT.md) |
| `docs/engine-reference/unity/modules/input.md` | Input Actions, control scheme, rebinding, callback context, dead zone processor | Section C (khả thi), Open Questions | Xác minh cho 1.11; dự án dùng 1.20.1 |
| GDD Character Controller *(chưa viết)* | Cửa sổ đệm, loại đồng hồ, nghĩa của Plunge/DropThrough trên mặt đất, log lý do từ chối tiêu thụ (H-14) | Rule 4, 7, 8; AC-X01, AC-T04 | Viết tiếp theo |
| GDD Combat *(chưa viết)* | Độ dài và timeScale của hit-stop (`s_min`, `hitstop_budget`) | F2, Tuning Knobs | Chưa viết |
| GDD câu cá *(hệ chưa có trong index)* | Cửa sổ móc cá; cá làm gì khi chế độ Fishing bị che | States, Edge Cases | Chưa có |

## Acceptance Criteria

*Nguồn: qa-lead, 2026-10-30 (78 tiêu chí). Mười chỗ sửa chữ qa-lead đề xuất đã được áp thẳng vào
Section C/D (Rule 1, 4, 5, 7, 10, 13, 14, F5, validator, Telemetry). Gate dùng mức mặc định vì
`project.yaml` chưa đặt `testing.strict`: Logic/Integration/Visual/UI là BLOCKING.*

**Quy ước:** test lõi ở `Assets/Tests/EditMode/Foundation/`, test adapter ở `Assets/Tests/PlayMode/Foundation/` (ADR-0001, sửa đổi đợt 1).
Mọi test dùng đồng hồ inject (H-1) và nguồn sự kiện giả (H-2); giá trị mặc định lấy từ fixture cấu
hình, không viết cứng; con số trong tiêu chí chỉ là giá trị biên hoặc ví dụ có sẵn ở Formulas.
Lưu ý: `commands.test` hiện chỉ chạy EditMode, và test PlayMode kiểm chứng giả định cần
`InputTestFixture` (cần `testables` trong manifest): Open Questions OQ-I10.

### H.1 Luật lõi

- **GIVEN** mọi assembly game ngoài assembly của Input, **WHEN** test tĩnh quét tham chiếu asmdef và mã nguồn, **THEN** có 0 tham chiếu tới Input System package, `UnityEngine.Input`, `Keyboard/Gamepad/Mouse.current`. (AC-R01) `[Unit/EditMode — BLOCKING]`
- **GIVEN** asset hành động mặc định, **WHEN** liệt kê binding, **THEN** mỗi hành động khớp đúng chế độ và phím của cả hai thiết bị theo bảng Rule 2 (bảng là fixture), và Interact ≠ Attack trên từng thiết bị. (AC-R02) `[Unit/EditMode — BLOCKING]`
- **GIVEN** một release build, **WHEN** liệt kê các action map, **THEN** không có map Debug. (AC-R03) `[Manual — ADVISORY]`
- **GIVEN** thiết bị đang dùng là `Gamepad`, **WHEN** bấm J và giữ D trên bàn phím, **THEN** `TryConsume(Attack)` trả bản ghi có `device = KeyboardMouse` và `ReadMove().x > 0`. (AC-R04) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Move sau dead zone = (0, −1), **WHEN** bấm Attack, **THEN** có đúng 1 bản ghi `Plunge` với `baseAction = Attack`; sau khi `TryConsume(Attack)` trả bản ghi đó, `TryConsume(Plunge)` trả "không có". (AC-R05) `[Unit/EditMode — BLOCKING]`
- **GIVEN** một bước input, **WHEN** sự kiện Attack và sự kiện Xuống đến theo hai thứ tự ngược nhau, **THEN** cả hai thứ tự cho cùng một bản ghi `Plunge`, với `move` là giá trị cuối bước. (AC-R06) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Plunge được gán thành nút đơn, **WHEN** bấm Attack lúc đang Xuống, **THEN** ghi `Attack` không có `baseAction`; và khi Attack đã đổi sang phím L (Plunge dạng tổ hợp) thì Xuống + L ra `Plunge`. (AC-R07) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đã có bản ghi `Plunge`, **WHEN** nhả Xuống trước khi tiêu thụ, **THEN** `TryConsume(Plunge)` vẫn trả bản ghi đó, với `move.y ≤ −down_threshold`. (AC-R08) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đồng hồ giả ở (t_real, t_game), **WHEN** bấm rồi nhả Jump, **THEN** có 2 bản ghi với `seq` liên tiếp, và `t_real`, `t_game`, `epoch`, `device`, `move` khớp chính xác giá trị đã inject. (AC-R09) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `InputTestFixture`, **WHEN** xếp hàng chuỗi nhấn–nhả–nhấn Attack trong 1 frame, **THEN** có 3 bản ghi (kiểm chứng giả định ở Rule 5). (AC-R10) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** đồng hồ giả lùi 5 ms, **WHEN** bước kế tiếp chạy, **THEN** `now` không giảm, và có đúng 1 log lỗi trong cả phiên dù đồng hồ lùi lần hai. (AC-R11) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đã ghi 100 sự kiện, **WHEN** lấy snapshot bộ đệm, **THEN** số bản ghi ≤ `buffer_capacity`, và mọi bản ghi có tuổi thật > `buffer_retention` không lấy được bằng bất kỳ `window` nào. (AC-R12) `[Unit/EditMode — BLOCKING]`
- **GIVEN** một bên tiêu thụ đăng ký `W` vượt ngưỡng F2, **WHEN** đăng ký, **THEN** có 1 cảnh báo và đăng ký vẫn thành công; gọi 2 lần với `window > W` chỉ có 1 cảnh báo cho cặp (bên tiêu thụ, hành động); `window = −0,1` hoạt động như 0. (AC-R13) `[Unit/EditMode — BLOCKING]`
- **GIVEN** bấm Attack 3 lần, **WHEN** gọi `TryConsume(Attack)` 4 lần, **THEN** 3 lần đầu trả bản ghi theo `seq` tăng dần, lần thứ 4 trả "không có". (AC-R14) `[Unit/EditMode — BLOCKING]`
- **GIVEN** có 1 bản ghi đủ điều kiện, **WHEN** gọi `Peek` 2 lần rồi `TryConsume`, **THEN** cả 3 lần trả cùng `seq`, và `Peek` không đánh dấu tiêu thụ. (AC-R15) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Jump được nhả trong lúc hit-stop, sau đó bản ghi `Released` bị ghi đè, **WHEN** gọi `WasReleasedAfter(Jump, seq_nhấn)`, **THEN** kết quả là `true`. (AC-R16) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đỉnh stack là `Menu` (hoặc `Fishing`), **WHEN** đọc `ReadMove()` (hoặc `ReadNavigate()`) và gọi `TryConsume(Jump)`, **THEN** trục trả (0, 0), Jump trả "không có", và có 0 log. (AC-R17) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Reel ở `Toggle`, **WHEN** nhấn, nhả, rồi nhấn lại, **THEN** `IsHeld` lần lượt là true, true, false; ở `Hold`, `IsHeld` bám theo trạng thái nút thật. (AC-R18) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `Time.timeScale = 0,05`, **WHEN** bấm Dodge, **THEN** bản ghi xuất hiện ngay trong frame đó, với `t_real` bằng thời gian thật (kiểm chứng giả định Dynamic Update ở Rule 10). (AC-R19) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** đồng hồ `Game` đứng yên lâu hơn `buffer_retention` giây thật, **WHEN** đỉnh là `Gameplay`, **THEN** có 1 cảnh báo; khi `Menu` đã được push thì có 0 cảnh báo. (AC-R20) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `Dialogue` đè lên `Gameplay`, **WHEN** bấm J, gọi `Pop` với handle của chủ khác, rồi thử gỡ `Gameplay`, **THEN** không có bản ghi Attack, hai lần gỡ đều trả `false`, stack không đổi. (AC-R21) `[Unit/EditMode — BLOCKING]`
- **GIVEN** một handle đã gỡ mà ô của nó đã được dùng lại, hoặc một handle của instance khác, **WHEN** gọi `Pop`, **THEN** kết quả `false`, có 1 cảnh báo, `epoch` không đổi. (AC-R22) `[Unit/EditMode — BLOCKING]`
- **GIVEN** chủ của một mục `Locked` ở đỉnh đã chết (predicate liveness trả false), **WHEN** bước kế tiếp chạy, **THEN** mục bị tự gỡ, có 1 log lỗi, `epoch` tăng 1, và Pause lại được ghi. (AC-R23) `[Unit/EditMode — BLOCKING]`
- **GIVEN** chủ của `Fishing` đã đăng ký `TopChanged`, **WHEN** push rồi pop `Menu`, **THEN** chủ nhận đúng 2 sự kiện, `false` rồi `true`, và `IsTop` khớp với từng sự kiện. (AC-R24) `[Unit/EditMode — BLOCKING]`
- **GIVEN** có bản ghi Attack chưa tiêu thụ, **WHEN** push `Menu`, **THEN** `epoch` tăng 1, và sau khi pop về `Gameplay` thì `TryConsume(Attack)` vẫn trả "không có". (AC-R25) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang giữ E (hoặc Space) lúc `Dialogue` (hoặc `Menu`) bị pop, **WHEN** nhả phím, **THEN** không có bản ghi `Released`, `IsHeld` là false, không có bản ghi Jump; lần nhấn tiếp theo được ghi ở epoch mới. (AC-R26) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang giữ D lúc `Menu` bị pop, **WHEN** bước đó chạy, **THEN** `ReadMove().x > 0` ngay trong bước đó. (AC-R27) `[Unit/EditMode — BLOCKING]`
- **GIVEN** handler của `DeviceLost` gọi `Push(Menu)`, **WHEN** trong cùng bước có thêm một lần bấm Attack, **THEN** push chỉ được áp ở cuối bước, và lần bấm mang epoch cũ nên bị rào xóa. (AC-R28) `[Unit/EditMode — BLOCKING]`
- **GIVEN** các tín hiệu dưới ngưỡng (di chuột ở `Gameplay`, cắm tay cầm, cần thô < `stick_switch_eff`), **WHEN** bước chạy, **THEN** có 0 `DeviceChanged`; khi bàn phím và tay cầm cùng có tín hiệu trong 1 bước, có đúng 1 `DeviceChanged` và thiết bị xử lý sau cùng thắng. (AC-R29) `[Unit/EditMode — BLOCKING]`
- **GIVEN** lúc khởi động chưa có tín hiệu nào, **WHEN** có (hoặc không có) tay cầm đang cắm, **THEN** thiết bị đang dùng là `Gamepad` (hoặc `KeyboardMouse`). (AC-R30) `[Unit/EditMode — BLOCKING]`
- **GIVEN** thiết bị ở từng layout (bàn phím-chuột, Xbox, DualSense, tay cầm chung) và trường hợp `SetGlyphFamily(PlayStation)` trên layout Xbox, **WHEN** gọi `GetGlyph(Jump)`, **THEN** lần lượt nhận Space / A / Cross / A, và Cross khi đã ghi đè. (AC-R31) `[Unit/EditMode — BLOCKING]`
- **GIVEN** một probe chạy sớm nhất theo execution order, **WHEN** bấm nút tay cầm ở frame N, **THEN** `DeviceChanged` đã phát ở frame N, trước mọi `Update` của frame đó (kiểm chứng giả định ở Rule 14). (AC-R32) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** đang giữ Jump trên tay cầm dùng gần nhất, **WHEN** rút tay cầm, **THEN** `IsHeld` là false (vẫn true nếu Space cũng đang giữ), `WasReleasedAfter` là true, không có bản ghi `Released`, thiết bị chuyển về `KeyboardMouse`, có 1 `DeviceLost`, stack không đổi. (AC-R33) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang giữ phím và Navigate đang lặp, **WHEN** mất focus rồi có lại focus kèm một click, **THEN** phím bị chốt, trạng thái lặp bị xóa, chế độ không đổi, click không sinh bản ghi Attack và không đổi thiết bị. (AC-R34) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang giữ Space lúc khởi tạo, **WHEN** chạy frame đầu tiên, **THEN** bộ đệm rỗng, `epoch = 0`, và Space bị chốt tới khi nhả. (AC-R35) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đổi phím Jump sang J (J đang là Attack, cùng Gameplay), **WHEN** gọi `StartRebind`, **THEN** nhận `Conflict[Attack]` và chưa áp dụng gì; `CommitRebind(Swap)` cho Jump = J, Attack = Space; `Cancel` giữ nguyên; đổi Jump sang Enter (chỉ Menu dùng) không phải xung đột. (AC-R36) `[Unit/EditMode — BLOCKING]`
- **GIVEN** các rebind thử: Esc hoặc Menu cho Map (kể cả qua Swap), bấm Esc giữa lúc rebind, xóa phím Confirm cuối cùng trên tay cầm, thêm phím phụ cho Pause, **WHEN** thực hiện lần lượt, **THEN** kết quả là từ chối / `Cancelled` / từ chối / chấp nhận. (AC-R37) `[Unit/EditMode — BLOCKING]`
- **GIVEN** cấu hình đã xuất ra, **WHEN** `ApplyBindingOverrides` trên một instance mới mà tên hiển thị của hành động đã đổi, **THEN** binding khớp hoàn toàn (khóa theo ID ổn định), `epoch` tăng, nút đang giữ bị chốt. (AC-R38) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang rebind, **WHEN** đỉnh stack đổi, hoặc tay cầm của ô đang đổi bị rút, **THEN** nhận `Cancelled(ModeChanged)` hoặc `Cancelled(DeviceLost)` và 0 phím bị đổi; khi rebind hoàn tất bình thường, phím vừa chọn bị chốt nên lần nhả không kích hoạt Confirm. (AC-R39) `[Unit/EditMode — BLOCKING]`
- **GIVEN** một instance Input, **WHEN** hủy rồi tạo instance mới, **THEN** còn 0 callback đăng ký, action bị tắt, cấu hình gán phím đã áp bị xóa, instance mới có `seq = epoch = 0`, handle cũ bị từ chối; asmdef của lõi không tham chiếu engine. (AC-R40) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Enter Play Mode đã tắt domain reload, **WHEN** vào Play 2 lần, **THEN** mỗi sự kiện đổi thiết bị chỉ được xử lý 1 lần. (AC-R41) `[Integration/PlayMode — BLOCKING]`

### H.2 Chế độ và trạng thái

- **GIVEN** từng chế độ trong bảng States, **WHEN** bơm lần bấm của mọi hành động trong bảng Rule 2 (test theo bảng dữ liệu), **THEN** chỉ hành động thuộc chế độ ở đỉnh có bản ghi. (AC-M01) `[Unit/EditMode — BLOCKING]`
- **GIVEN** chế độ `Locked`, **WHEN** bấm Esc, Pause và Attack, **THEN** có 0 bản ghi. (AC-M02) `[Unit/EditMode — BLOCKING]`
- **GIVEN** chế độ `Dialogue`, **WHEN** bấm E để Confirm đóng hội thoại và hệ Dialogue pop, **THEN** trong 10 frame sau đó có 0 bản ghi Interact và 0 bản ghi Attack. (AC-M03) `[Unit/EditMode — BLOCKING]`
- **GIVEN** chế độ `Fishing`, **WHEN** bấm Esc (Pause và Cancel cùng bắn từ phím), **THEN** chỉ có bản ghi Cancel, không có Pause; và khi `Menu` được push lên trên `Fishing`, hệ câu cá không nhận Reel hay Cancel nào cho tới khi pop. (AC-M04) `[Unit/EditMode — BLOCKING]`
- **GIVEN** từng chế độ, **WHEN** bấm Esc hoặc Menu/Options, **THEN** Gameplay ra đúng 1 bản ghi Pause; Menu, Dialogue, Map, Fishing ra đúng 1 bản ghi Cancel; Locked ra 0 (I-8). (AC-M06) `[Unit/EditMode + Integration/PlayMode — BLOCKING]`
- **GIVEN** pop một handle không ở đỉnh, **WHEN** bước chạy, **THEN** mục đó rời stack, `epoch` không đổi, nút đang giữ không bị chốt. (AC-M05) `[Unit/EditMode — BLOCKING]`

### H.3 Công thức

- **GIVEN** một lần bấm ở t = 0, `window` = 0,125 s, **WHEN** `now` = 0,125 s rồi 0,125 s + 1 tick, **THEN** lần lượt đủ điều kiện rồi không (biên ≤ bao gồm). (AC-F01) `[Unit/EditMode — BLOCKING]`
- **GIVEN** bấm ngay đầu hit-stop 0,07 s ở s = 0,05, `window` = 0,12 s, **WHEN** gọi với `Real`, **THEN** còn đủ điều kiện tới thời gian thật 0,12 s (còn 0,05 s sau hit-stop), quá mốc đó thì không. (AC-F02) `[Unit/EditMode — BLOCKING]`
- **GIVEN** cùng tình huống AC-F02, **WHEN** gọi với `Game`, **THEN** còn đủ điều kiện tới thời gian thật 0,1865 s (game = 0,12 s, còn 0,1165 s sau hit-stop), quá mốc đó thì không. (AC-F03) `[Unit/EditMode — BLOCKING]`
- **GIVEN** cửa sổ `Game` 0,15 s và đồng hồ Game chạy ở s = 0,05 suốt 1,0 s thật, **WHEN** thời gian thật = 1,0 s rồi 1,0 s + 1 tick, **THEN** đủ điều kiện rồi không; lần sau bị `buffer_retention` cắt dù tuổi game chỉ 0,05 s. (AC-F04) `[Unit/EditMode — BLOCKING]`
- **GIVEN** 3 bản ghi đủ điều kiện, trong đó bản cũ nhất thuộc epoch cũ, **WHEN** gọi `TryConsume`, **THEN** trả bản ghi cũ nhất trong số bản ghi thuộc epoch hiện tại. (AC-F05) `[Unit/EditMode — BLOCKING]`
- **GIVEN** các đăng ký `W`, **WHEN** `W` = 0,15 s theo `Game` (ra 0,35), 1,0 s theo `Real`, 1,0 s + 1 tick theo `Real`, 0,81 s theo `Game` (ra 1,0095), **THEN** cảnh báo lần lượt: không / không / có / có. (AC-F06) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `buffer_capacity` = 32, **WHEN** bấm dồn 15 lần, rồi 17 lần, trong 1,0 s, **THEN** bộ đếm "ghi đè trước hạn" lần lượt là 0 và 2. (AC-F07) `[Unit/EditMode — BLOCKING]`
- **GIVEN** bàn phím S+D cho m = (0,707; −0,707), **WHEN** tính `down()`, **THEN** DropThrough (35°) false, Plunge (50°) true; bấm Jump ra Jump, bấm Attack ra Plunge. (AC-F08) `[Unit/EditMode — BLOCKING]`
- **GIVEN** m = (0; −0,5), (0; −0,49), (0; 0), và độ lớn 0,60 ở 35°, **WHEN** tính `down(DropThrough)`, **THEN** lần lượt true, false, false (không chia cho 0), false. (AC-F09) `[Unit/EditMode — BLOCKING]`
- **GIVEN** cần thô lệch 30° khỏi hướng xuống, độ lớn 0,8, `dead_zone` = 0,2, **WHEN** đi qua F4 rồi F3, **THEN** độ lớn sau dead zone = 0,75 (±1e-6), cả hai hành động đều tính là Xuống. (AC-F10) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `dead_zone` = 0,2, **WHEN** r = 0,15; r = 0,2; r = 0,201; v = (0,3; −0,4); ‖v‖ = 1,41, **THEN** ‖out‖ lần lượt = 0; 0; 0,00125; out = (0,225; −0,3); ‖out‖ = 1 (±1e-6). (AC-F11) `[Unit/EditMode — BLOCKING]`
- **GIVEN** Settings đẩy `SetDeadZone(0,5)`, **WHEN** áp dụng, **THEN** dead zone hiệu dụng = 0,4 và `stick_switch_eff` = 0,55; cần thô 0,35 không đổi thiết bị, 0,55 và 0,6 thì đổi. (AC-F12) `[Unit/EditMode — BLOCKING]`
- **GIVEN** RT là phím Né phụ, **WHEN** cò ở 0,29 rồi 0,30, **THEN** lúc 0,29 không có Dodge và không đổi thiết bị; lúc 0,30 có `Dodge Pressed` và đổi thiết bị. (AC-F13) `[Unit/EditMode — BLOCKING]`
- **GIVEN** chuột ở `Menu`, cửa sổ 0,25 s, **WHEN** di 8 px và 9 px ở 1080p, 16 px và 18 px ở 2160p, **THEN** đổi thiết bị lần lượt: không / có / không / có; ở `Gameplay`, di 100 px cũng không đổi. (AC-F14) `[Unit/EditMode — BLOCKING]`
- **GIVEN** giữ Navigate một hướng từ t0 = 0 với giá trị mặc định, **WHEN** chạy 60 fps suốt 1,0 s, **THEN** có 9 bước ở các mốc 0; 0,35; 0,44; …; 0,98 s (±1 frame); ở t = 0,35 s − 1 tick là 1 bước, ở t = 0,35 s là 2 bước. (AC-F15) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang giữ Navigate, **WHEN** một frame kéo dài 1,0 s, hoặc hướng đổi sang phần tư khác, **THEN** frame dài phát đúng 1 bước và không có bước dồn ở các frame sau; đổi phần tư thì phát 1 bước ngay và `t0` đặt lại. (AC-F16) `[Unit/EditMode — BLOCKING]`

### H.4 Validator

- **GIVEN** `buffer_retention` = 2,0 với `buffer_capacity` = 32, rồi với 64, **WHEN** nạp cấu hình, **THEN** cấu hình thứ nhất đưa cả hai knob về mặc định kèm 1 log lỗi; cấu hình thứ hai được nhận. (AC-V01) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `buffer_capacity` = 48, **WHEN** nạp, **THEN** dùng 32 và có 1 log lỗi. (AC-V02) `[Unit/EditMode — BLOCKING]`
- **GIVEN** `down_angle_drop` = 45, **WHEN** nạp, **THEN** dùng 35, có log lỗi, và `drop < 45 ≤ plunge` vẫn đúng. (AC-V03) `[Unit/EditMode — BLOCKING]`
- **GIVEN** mỗi knob trong bảng Tuning Knobs (test theo bảng dữ liệu), **WHEN** giá trị đúng bằng min và max, rồi min − ε và max + ε, **THEN** hai biên được nhận; hai giá trị ngoài biên về mặc định, mỗi giá trị đúng 1 log lỗi. (AC-V04) `[Unit/EditMode — BLOCKING]`

### H.5 Edge Cases

- **GIVEN** có bản ghi từ trước, **WHEN** một frame khựng 1,5 s và có 3 lần bấm trong lúc khựng, **THEN** 3 lần bấm đủ điều kiện với tuổi 0 và `seq` đúng thứ tự; bản ghi có từ trước hết hạn. (AC-E01) `[Unit/EditMode — BLOCKING]`
- **GIVEN** nhấn rồi nhả Jump trong 1 frame, **WHEN** tiêu thụ, **THEN** `TryConsume` trả bản `Pressed`, `WasReleasedAfter` true, `IsHeld` false. (AC-E02) `[Unit/EditMode — BLOCKING]`
- **GIVEN** push rồi pop trong 1 frame, **WHEN** bước kết thúc, **THEN** `epoch` tăng 2; khi 2 chủ cùng có `Menu`, gỡ mục trên tạo rào. (AC-E03) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đang giữ Reel, **WHEN** push rồi pop `Locked`, **THEN** `IsHeld` false ngay khi push, không có bản ghi `Released`, và Reel còn chốt sau khi pop cho tới khi nhả. (AC-E04) `[Unit/EditMode — BLOCKING]`
- **GIVEN** có bản ghi Plunge tổ hợp trong bộ đệm, **WHEN** Plunge bị đổi sang nút đơn, **THEN** bản ghi cũ không tiêu thụ được nữa. (AC-E05) `[Unit/EditMode — BLOCKING]`
- **GIVEN** đồng hồ đang ở 64 800 s (18 giờ), **WHEN** lặp lại AC-F01 với mốc gốc này, **THEN** kết quả biên y hệt AC-F01. (AC-E06) `[Unit/EditMode — BLOCKING]`
- **GIVEN** có 2 tay cầm, **WHEN** rút tay cầm không phải tay cầm dùng gần nhất, **THEN** có 0 `DeviceLost`, chỉ nút của tay cầm đó bị chốt, biểu tượng không đổi. (AC-E07) `[Unit/EditMode — BLOCKING]`
- **GIVEN** dữ liệu gán phím có ID lạ, đường dẫn phím sai, dữ liệu hỏng, hoặc chứa xung đột, **WHEN** gọi `ApplyBindingOverrides`, **THEN** lần lượt: bỏ qua kèm log / ô đó về mặc định / toàn bộ về mặc định và báo lỗi cho Settings / hành động vi phạm về mặc định và có trong danh sách trả về. (AC-E08) `[Unit/EditMode — BLOCKING]`

### H.6 Liên hệ với hệ khác *(thuộc story tích hợp của từng hệ)*

- **GIVEN** Character Controller với cửa sổ đã đăng ký, **WHEN** bấm Jump trước lúc chạm đất trong cửa sổ, bấm Dodge giữa đòn chém trong lúc hit-stop, và bấm Xuống + Attack trên không, **THEN** nhảy ra ở frame chạm đất, né ra ở cửa sổ hủy đòn, plunge ra; mỗi lần bấm sinh đúng 1 hiệu ứng. (AC-X01) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** Region Loading đang giữ `Locked`, **WHEN** người chơi bấm phím trong lúc nạp vùng rồi handle được gỡ, **THEN** có 0 bản ghi trong lúc nạp; lần bấm đầu tiên sau khi gỡ được tiêu thụ; phím đang giữ lúc gỡ bị chốt. (AC-X02) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** một Interaction prompt đang hiện, **WHEN** đổi thiết bị ở frame N, **THEN** frame đầu tiên prompt vẽ biểu tượng mới = N, không có crossfade. (AC-X03) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** cùng tình huống AC-X03, **WHEN** chụp lại, **THEN** có ảnh retained của prompt và HUD ở 3 kiểu biểu tượng (bàn phím-chuột, Xbox, PlayStation) và ảnh của frame đổi, lưu ở `production/qa/evidence/`. (AC-X04) `[Visual — BLOCKING]`
- **GIVEN** màn hình đổi phím của Settings, **WHEN** đổi Jump sang J, chọn Swap rồi khởi động lại game, **THEN** cấu hình đã swap còn nguyên; có ảnh retained của hộp thoại xung đột. (AC-X05) `[Integration/PlayMode + Visual — BLOCKING]`
- **GIVEN** Reel ở `Toggle` đang bật trong `Fishing`, **WHEN** `Menu` được push lên trên `Fishing` (ví dụ `DeviceLost`) rồi gỡ, **THEN** `IsHeld` false trong lúc `Menu` ở đỉnh, hệ câu cá nhận 0 input, và sau khi tiếp tục `IsHeld` true mà không cần bấm lại. (AC-X06) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** Menu & Navigation đã đăng ký `DeviceLost`, **WHEN** rút tay cầm giữa gameplay, **THEN** Menu nhận đúng 1 `DeviceLost`, Input không tự push gì. (AC-X07) `[Integration/PlayMode — BLOCKING]`

### H.7 Hiệu năng

- **GIVEN** lõi đã chạy warm-up 100 bước, **WHEN** chạy 10 000 bước theo kịch bản (15 lần bấm/s, consume, peek, push/pop, đổi thiết bị), **THEN** bộ nhớ cấp phát của luồng tăng đúng 0 byte. (AC-P01) `[Performance — BLOCKING]`
- **GIVEN** PlayMode ở trạng thái ổn định (không rebind, không cắm/rút thiết bị), **WHEN** đo 600 frame bằng profiler, **THEN** cấp phát quy cho marker `Input.*` = 0 B mỗi frame. ADVISORY tới khi xác minh package 1.20.1. (AC-P02) `[Performance — ADVISORY]`
- **GIVEN** máy PC mục tiêu, bộ đệm đầy 32 bản ghi, 2 thiết bị, 20 lần query mỗi frame, **WHEN** đo marker `Input.Step` cộng thời gian query, **THEN** p95 ≤ 0,1 ms. ADVISORY tới khi có spec máy tối thiểu. (AC-P03) `[Performance — ADVISORY]`

### H.8 Playtest và cảm giác *(gate ở milestone Vertical Slice, không phải gate từng story)*

- **GIVEN** n ≥ 5 tester, mỗi người 20 phút nửa cozy với ≥ 30 lần chuyển chế độ, **WHEN** phân tích log và báo cáo, **THEN** 0 báo cáo "lạc chế độ" và log ghi 0 lần tiêu thụ bản ghi khác epoch (binding test 1). (AC-T01) `[Playtest — BLOCKING]`
- **GIVEN** mỗi tester đổi thiết bị ≥ 10 lần qua Gameplay, Menu, Fishing, Dialogue, **WHEN** phân tích log theo frame, **THEN** 0 báo cáo kẹt input và 0 frame mà thiết bị của biểu tượng ≠ thiết bị đang dùng (binding test 2). (AC-T02) `[Playtest — BLOCKING]`
- **GIVEN** mỗi tester ≥ 15 phút combat có hit-stop, **WHEN** đọc bộ đếm, **THEN** "lần bấm thô không có bản ghi" = 0 và "ghi đè trước hạn" = 0 (binding test 3). (AC-T03) `[Playtest — BLOCKING]`
- **GIVEN** n ≥ 5 tester, mỗi người ≥ 10 lần chết trước boss hoặc elite, tự gắn nhãn nguyên nhân sau mỗi lần chết (pattern / input / khác), **WHEN** người phân tích rà log 2,0 s trước mỗi lần chết gắn nhãn "input", **THEN** **0 lần chết được xác nhận do Input**. "Xác nhận do Input" = có ít nhất một: lần bấm thô không có bản ghi; bản ghi bị ghi đè trước hạn; bản ghi bị rào xóa mà không có đổi chế độ người chơi nhìn thấy; biểu tượng sai thiết bị. Trường hợp "xác nhận do bên tiêu thụ" (bản ghi đủ điều kiện trong cửa sổ đã khai báo, Controller ở trạng thái cho phép, mà không tiêu thụ) được báo cho chủ Character Controller, mục tiêu 0. Tỷ lệ nhãn "input" không xác nhận được ghi lại để hiệu chỉnh. (AC-T04) `[Playtest — BLOCKING]`

## Open Questions

| ID | Câu hỏi | Chủ | Hạn |
|---|---|---|---|
| OQ-I1 | **ADR "Input architecture":** update mode (Dynamic Update, Rule 10), nguồn đồng hồ thật để inject (`unscaledTimeAsDouble` cố định trong frame hay `realtimeSinceStartupAsDouble`; quy về tick 1 µs), cách hiện thực chế độ (bật/tắt action map hay mọi map luôn bật + lọc trong bộ đệm), phân lớp adapter/lõi, chủ của asset hành động, không dùng `PlayerInput` và project-wide actions | technical-director, unity-specialist | Trước story đầu tiên của Input |
| OQ-I2 | **ADR TD-B4 (ID ổn định)**: định dạng ID của hành động làm khóa cấu hình gán phím (Rule 17) và định dạng file cấu hình knob | technical-director, lead-programmer | Trước story đổi phím; trước Game Data GDD |
| OQ-I3 | Xác minh trên Input System 1.20.1 (tài liệu engine chỉ có 1.11): callback bắn riêng cho từng sự kiện trong một frame (Rule 5, AC-R10); giá trị Move đọc được bên trong callback của nút gốc (Rule 4); bước input chạy trước `MonoBehaviour.Update` và vẫn chạy khi timeScale = 0 (Rule 10, 14; AC-R19, AC-R32); ở chế độ FixedUpdate sự kiện có bị dồn khi timeScale nhỏ không | unity-specialist | Trước story đầu tiên (ghi vào ADR OQ-I1) |
| OQ-I4 | Có dùng mốc thời gian riêng của sự kiện (`CallbackContext.time`) để chính xác dưới một frame không, và nó dùng trục thời gian nào | unity-specialist | Cùng OQ-I1 |
| OQ-I5 | Hành vi engine khi: mất focus (sự kiện nhả có bị mất, background behavior), click lấy lại focus có tới action không (Rule 15); rút thiết bị có bắn `canceled` không (nếu có, phải chặn để không sinh `Released`); bật lại map khi phím đang giữ có bắn `performed` không (Initial State Check); event buffer có làm rơi sự kiện khi khựng dài không | unity-specialist | Trước story thiết bị |
| OQ-I6 | Trạng thái enabled và cấu hình gán phím trên asset hành động có sót lại giữa các lần Play khi tắt domain reload không (Rule 18, AC-R41) | unity-specialist | Trước story đầu tiên |
| OQ-I7 | Rebind tương tác có cần tắt action trong lúc chạy không; có sẵn phát hiện trùng phím không (nếu không, Input tự làm theo Rule 17); `GetBindingDisplayString` có dùng được cho `GetGlyph` không; binding composite (Move/WASD) đổi phím thế nào | unity-specialist, ui-programmer | Trước story đổi phím (Vertical Slice) |
| OQ-I8 | Steam Input / Steam Deck: có nhận ra loại tay cầm thật khi Steam báo tay cầm PlayStation là Xbox không; có hỗ trợ biểu tượng Switch / Steam Deck không (chi phí art) | unity-specialist, producer | Trước Vertical Slice |
| OQ-I9 | Chuyển *Active Input Handling* từ *Both* sang chỉ Input System sau khi xóa hai prototype (đang dùng API cũ), để Rule 1 được engine cưỡng chế | lead-programmer | Trước Vertical Slice |
| OQ-I10 | **Hạ tầng test (cần người dùng duyệt riêng, chưa áp dụng):** thêm `"testables": ["com.unity.inputsystem"]` vào `Packages/manifest.json`; thêm lệnh chạy PlayMode vào `commands.test` của `project.yaml` (hiện chỉ EditMode); xác minh `InputTestFixture` có trong 1.20.1 | qa-lead, devops-engineer | Trước story có AC PlayMode (`/test-setup`) |
| OQ-I11 | **Test hook** phần hiện thực phải có (đưa vào ADR OQ-I1): H-1 đồng hồ inject `Real`/`Game` tick int64, đặt và tiến tay được; H-2 nguồn sự kiện giả (nhấn, nhả, trục, cắm/rút, focus, layout; thứ tự tường minh; `BeginStep`/`EndStep`); H-3 snapshot bộ đệm + `last_release_seq`; H-4 snapshot stack (handle, thế hệ, chủ, đỉnh), `epoch`, tập nút bị chốt; H-5 predicate liveness inject được; H-6 log sink inject được, đếm theo khóa cảnh báo; H-7 bộ đếm telemetry tách riêng + histogram độ trễ, kèm chỉ số frame; H-8 tap sự kiện thiết bị thô (development build); H-9 log playtest theo frame (`DeviceChanged` và biểu tượng UI thực vẽ); H-10 validator là hàm thuần; H-11 rebind chạy không cần UI; H-12 đếm subscription + cờ đã hủy; H-13 profiler marker `Input.Step` | lead-programmer, qa-lead | Cùng OQ-I1 |
| OQ-I12 | GDD Character Controller phải định nghĩa: độ dài và loại đồng hồ của từng cửa sổ đệm; Plunge/DropThrough trên mặt đất nghĩa là gì; **log lý do từ chối tiêu thụ** (H-14) để phân loại AC-T04 | game-designer (GDD Character Controller) | Khi viết GDD Character Controller (tiếp theo) |
| OQ-I13 | GDD Combat xác nhận `s_min` (0,05) và `hitstop_budget` (0,21 s) của F2 | game-designer (GDD Combat) | Khi viết GDD Combat |
| OQ-I14 | Chủ của ba nhóm hành động chưa có chủ trong systems index: dùng vật phẩm tiêu hao (Heal, EatBuff: Item & Inventory hay Combat), hiển thị bản đồ thế giới (Map), hệ câu cá (Reel, chế độ Fishing). Gắn với việc cập nhật concept theo thế giới mới. **Ghi chú cho GDD câu cá (CD-GDD-ALIGN):** cửa sổ móc cá nên đo theo đồng hồ `Game` và tôn trọng chế độ `Toggle`, để nửa cozy không thành bài kiểm tra phản xạ (anti-pillar *NOT a time-pressure game*). Câu "cầm cần câu" trong Player Fantasy cần xem lại khi cập nhật concept | producer, game-designer | Trước `/gate-check` Systems Design |
| OQ-I15 | Cutscene có cho bỏ qua không; nếu có, `Locked` cần một ngoại lệ cho hành động Skip (hiện `Locked` không ghi gì, kể cả Pause). **Xác nhận chéo với GDD Game Time (CD-GDD-ALIGN):** `Locked` chặn cả Pause, nên chỉ ổn nếu đồng hồ game cũng dừng trong **mọi** khoảng `Locked` (day banner, cutscene, nạp vùng); nếu lịch vẫn chạy khi người chơi không tạm dừng được, đó là mầm áp lực thời gian | narrative-director, ux-designer, game-designer (Game Time) | Khi viết GDD Cutscene (Vertical Slice); xác nhận Game Time trước story đầu tiên của Input |
