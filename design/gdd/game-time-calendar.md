# Game Time & Calendar

> **Status**: Approved (design-review lần 6 2026-10-27, lean: 5 mục chặn duyệt của lần 5 đã kiểm đạt, theo phán quyết CD; còn mục BLOCKING-CODE và pre-playtest trong review log lần 5)
> **Author**: Đặng Đình Dũng + agents
> **Last Updated**: 2026-10-27
> **Last Verified**: 2026-10-25
> **Revision 2026-10-26**: theo `design/gdd/reviews/game-time-calendar-review-log.md` — 13 blocking + 14 recommended; quyết định người dùng **D-1** mỗi ngày một chuyến (s 0,75–0,9, trần H1 1,0), **D-2** thua rơi túi đồ nhặt lại được, **D-3** crash mất tối đa một chuyến (lưu ở điểm an toàn), **D-4** AC-PT1 là chẩn đoán + chỉ số hành vi. D3 (ngủ từ 18:00) giữ nguyên theo CD.
> **Revision 2 2026-10-26** (re-review, 9 blocking): sự kiện ranh giới mang payload + gộp khi ngủ, `AdvanceBy` phát ranh giới (Rule 11); `MIN_SEGMENT_MINUTES` = 120 là hằng, mốc Night 20:00–22:00; `MAX_GAME_MINUTE` = 2.147.480.767 (Rule 16); tập `TokenKind` đóng + bảng trạng thái × lệnh (States); bảo đảm Rule 6 theo loại chủ; `world_seed` thuộc World State; ràng buộc N1–N4 cho GDD phụ thuộc (Dependencies); bất biến stage 7 thay cho "một khung hình" (Rule 12, AC-PF4); tách AC xuyên hệ (Acceptance Criteria).
> **Revision 3 2026-10-26** (review lần 3): AC-R12.8 dùng giờ được ngủ; epoch chỉ tăng ở T1/T2/T10/T11, cấp lại `Focus` theo trạng thái focus thật (Rule 6); định nghĩa đếm token (States); cô lập handler + nhả token chuyển ngày kiểu `finally` (Rule 11, 12); Rule 15a thu hẹp về lệnh thoát engine cho hoãn, thoát khi catch-up thoát ngay; stage 7 không ghi khi có token `RegionLoad`/`Restore`; rollover không đổi thực thể vùng active (Rule 12); bỏ ScriptableObject khỏi liveness, `Focus` là best-effort (Rule 6, 8); OQ-2/3/11, hook H-3b/H-3c, phân loại lại AC, H.6. **Phạm vi GDD đóng băng: từ nay chỉ sửa lỗi** (CD).
> **Revision 4 2026-10-27** (review lần 4): F1 chỉ kích hoạt T7 khi `minute_advance = 1` (AC-S19); bảng trạng thái × lệnh thêm cột "catch-up + cờ chờ" và luật ưu tiên (catch-up thắng cho thoát/menu/game mới/nạp, cờ chờ thắng cho ngủ/`AdvanceBy`), AC-S17 thành 49 ô; AC-S06/E25 Restoring có n = 1; driver test `Step()`, sửa AC-R14.1, E08, E29, E30, R11.2, S04, tách AC-R12.8 thành R12.8a/b.
> **Revision 5 2026-10-27** (review lean, 4 blocking): thêm Interaction & Context Prompts vào Interactions/Dependencies; bất biến cờ chờ sửa `⇔` → `⇒` (Rule 12, States); `AdvanceBy` từ chối > 30 phút (Rule 2, AC-R02.6); `systems-index.md` dòng Defeat & Retry khớp Rule 18 / D-2.
> **Revision 6 2026-10-27** (review lần 5, full, 5 blocking — CD: lần 6 chỉ kiểm 5 mục này): F1 thêm điều kiện `lifecycle ≠ Active`; "chờ T9" = xếp hàng, không chờ chặn (bảng trạng thái × lệnh); AC-F02.1 khẳng định trên hàm F6 thuần ở `MAX_GAME_MINUTE`; fixture tiêm `is_focused = true` (quy ước test); F12 `u_max ≤ 0` ⇒ H1 không đạt được. Mục BLOCKING-CODE và pre-playtest: review log.
> **Implements Pillar**: Pillar 3 (Two Lives, One Loop); anti-pillar *NOT a time-pressure game*
> **Creative Director Review (CD-GDD-ALIGN)**: REVISED 2026-10-25 — C1 slot thoát game (Rule 15a; nay là slot tiếp tục, D-3), C2 cost 0–30 + biện pháp phi thời gian trước, C3 nạp không vào giữa trận, C4 Noon không phải hạn chót mềm; ghi chú nhỏ đã áp dụng

## Summary

Game Time & Calendar là đồng hồ duy nhất của Evermeadow: một số nguyên phút game từ 06:00 Ngày 1, từ đó suy ra giờ, khoảng giờ, đêm, ngày, mùa và năm; mọi hệ có thời gian (farming, chế tạo, quái về đêm, lịch NPC, HUD) đọc từ đây. Nó giữ lời hứa "thời gian chờ bạn" của game — không hạn chót, không phạt khi thức qua đêm, thua boss không tốn phút nào — bằng ba hợp đồng: token tạm dừng (menu, hội thoại, arena, nạp vùng), pipeline chuyển ngày có thứ tự kết thúc bằng save, và đồng hồ tách khỏi `timeScale` của engine.

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `Game Data & Tuning Config`

## Overview

Game Time & Calendar là **đồng hồ duy nhất của thế giới Evermeadow**: nó giữ giờ trong ngày, số ngày, mùa và năm, và là nguồn sự thật mà mọi hệ có thời gian đọc từ đó. Farming đọc thời gian để biết cây lớn đến đâu, Crafting để biết mẻ xử lý xong chưa, Enemy AI để biết đã sang đêm, NPC Schedules để biết ai đang ở đâu, HUD và Audio để hiện và nghe nhịp ngày. Hệ này không tự biết các hệ đó: nó công bố một **dấu thời gian tuyệt đối** và các sự kiện ranh giới (sang đêm, sang ngày, sang mùa), còn mỗi hệ tự tính trạng thái của mình từ thời gian đã trôi. Hệ cũng sở hữu ba hợp đồng mà các hệ khác cắm vào: **token tạm dừng** (boss arena, menu, hội thoại, cutscene yêu cầu dừng giờ mà Time không cần biết họ là ai), **pipeline chuyển ngày có thứ tự** (ngủ → cây lớn → xử lý → … → lưu cuối cùng), và **tách đồng hồ game khỏi `Time.timeScale` của engine** để hit-stop hay pause không bao giờ đẩy lịch.

Người chơi không "dùng" hệ này — họ sống trong nó. Cái họ cảm nhận là nhịp **mỗi ngày một chuyến** (D-1): sáng việc nhà và ghé làng, rồi một chuyến ra ngoài kéo tới hoàng hôn — hoặc qua đêm nếu họ muốn — rồi về rèn và ngủ (Core Loop › Session-Level). Hệ tồn tại để giữ **Calendar Contract** của concept: thời gian tạo nhịp cho cuộc sống, **không bao giờ là hạn chót** (anti-pillar *NOT a time-pressure game*), và thua boss không bao giờ làm hỏng nông trại (Pillar 3). Không có hệ này, nửa cozy không có nhịp, nửa phiêu lưu không có ngày/đêm, và mỗi hệ sẽ tự đếm giờ theo cách riêng — nguồn lỗi lệch lịch kinh điển.

*Phạm vi theo tier:* Fun-Gate Build — giờ trong ngày, số ngày, cờ ngày/đêm, ngủ = sang ngày + lưu. MVP-Complete — token tạm dừng cho boss arena. Vertical Slice — 4 mùa, năm lặp lại. Mô phỏng ngoài màn hình theo timestamp là hợp đồng từ đầu (chi phí bằng không khi chỉ có một vùng).

*Phạm vi xây dựng ở Fun-Gate* (review 2026-10-26; ước tính công do producer chốt — review lần 2 đánh giá slice này ≈ 7–9 ngày công, không phải 4–5):

| Nhóm | Nội dung |
|---|---|
| **Build now** | Rule 1–7, 9–14, 15, 15a (slot tiếp tục), 16–18; F1–F7, F12 (chỉ phần kiểm H1); token đếm + handle + quét liveness; pipeline **đồng bộ** (Rule 12); restore handle; validator cho các knob đang dùng; HUD giờ/ngày/khoảng giờ |
| **Stub** | Rule 8 dòng Boss/Cutscene (API token có sẵn, chưa có người gọi); `token_watchdog_seconds` (chỉ log); Rule 21 (API trả `Unsupported` khi tắt mùa) |
| **Defer** | F8–F11, F13 (mùa, năm, thứ, "lần kế tiếp" — VS); thứ tự `SeasonChanged`/`YearChanged`; chi tiết Rule 19 (MVP-Complete); `phase_timeout_seconds` và mọi edge case chỉ có khi phase bất đồng bộ (đánh dấu *[async]*); migration U3 (sau phát hành) |

## Player Fantasy

*Framing: gián tiếp — người chơi cảm nhận hiệu ứng của hệ, không thao tác với nó. Hướng chọn 2026-10-25 (creative-director đề xuất 3 hướng; người dùng chọn C làm xương sống, dùng khoảnh khắc của B làm bằng chứng trong phiên).*

**"Ngày mai luôn đến, và mọi thứ vẫn chờ bạn."**

Thời gian ở Evermeadow là **người giữ nhà, không phải người đốc công**. Ngày trôi, mùa đổi, năm quay vòng — nhưng không có gì bị lấy mất khi người chơi vắng mặt. Cây vẫn lớn trong lúc họ đấu với thần; một trận thua boss không ăn mất buổi sáng nào; lễ hội lỡ năm nay sẽ quay lại năm sau. Mỗi tối đi ngủ là một dấu chấm êm: hôm nay đã được giữ lại, ngày mai đã sẵn sàng. Lịch có nhịp và có hình dạng — nhưng nó *hứa*, không *đe dọa*.

Cảm xúc lõi là **an tâm**: thời gian luôn đứng về phía người chơi. Nó phục vụ Autonomy (tự quyết dùng ngày thế nào), Submission (nhịp ngày, mái nhà — Lantern & Mist), và mặt cảm xúc của **Pillar 3: Two Lives, One Loop** — *"mỗi nửa làm nửa kia dễ hơn hoặc giàu hơn, nhưng không nửa nào khóa cứng nửa kia. Thua boss không được làm hỏng nửa nông trại."* Nó biến anti-pillar *NOT a time-pressure game* thành một cảm xúc tích cực: không chỉ "không có hạn chót", mà "thời gian chờ mình". Đây cũng là câu trả lời trực tiếp cho lý do bỏ game mà Target Player Profile ghi: *"bị phạt vì thời gian"*.

**Thời gian có ý nghĩa vì nó cho hình dạng, không phải vì nó lấy đi.** Mỗi khoảng giờ phải *khác nhau* — sáng hợp với ruộng, trưa làng đông, đêm sương trả công hậu hơn, mùa đổi nội dung — chỉ là không khoảng nào phạt. Nếu viết lỏng, fantasy này trượt thành "thời gian chẳng quan trọng"; câu này là lực kéo giữ nó lại.

**Khoảnh khắc neo.**
- *Trong phiên (kiểm được từ Fun-Gate):* sâu trong Mist Forest, túi gần đầy, chỉ thiếu đúng một nguyên liệu; trời ngả tím, quái bắt đầu mạnh lên — người chơi *muốn* ở lại, không *phải* về. Họ dám ở lại chính vì biết ở nhà không mất gì. Rồi về muộn, ngủ, thức dậy trong nắng sáng: cây đã lớn thêm, và họ nhận ra mình đã ở ngoài cả đêm mà không mất gì — chỉ có thêm.
- *Tiếng vọng (Vertical Slice):* mùa xuân năm thứ hai quay lại, và người chơi nhận ra điều mình lỡ năm ngoái đang chờ họ.

*Design test*: Nếu phân vân giữa để người chơi lỡ một thứ vì lịch và để nó quay lại / chờ họ, hệ này chọn **quay lại / chờ**.

*Design test (phụ, cho cách thể hiện)*: Nếu phân vân giữa báo thời gian bằng con số/cảnh báo và báo bằng ánh sáng, âm thanh, hành vi của thế giới, hệ này chọn **thế giới** — con số là phụ.

**Anti-fantasy: "ngày bị phí".** Cách lớn nhất để hệ này phản bội fantasy không phải hạn chót (concept đã cấm, dễ thấy) mà là cảm giác âm thầm rằng **một giờ phiêu lưu là một giờ bị lấy khỏi nông trại** — ngày trở thành đơn vị hiệu suất, và mỗi chuyến săn dài, mỗi lần thử lại boss, mỗi đêm ở lại sương mang một chi phí cơ hội mà người chơi tự đo. Các dạng cụ thể hệ này phải chặn (luật ở Detailed Design, Edge Cases, Tuning Knobs):
- Kết thúc đêm có hình phạt (ngất kiểu 2 giờ sáng mất tiền/đồ). Đêm vẫn phải kết thúc — nhưng kết thúc **không phạt**.
- Đồng hồ HUD đếm ngược hay cảnh báo "sắp hết ngày".
- Thua boss / ở lâu trong arena đẩy lịch — token tạm dừng là hợp đồng cứng.
- "Mất mềm" vì năm quá dài: ở mặc định `D = 14` một năm là 56 ngày ≈ 14 phiên 60 phút (~4 ngày/phiên, F12) — với người chơi 30–60 phút/tối là 2–4 tuần ngoài đời. Nếu `D = 28` thì gấp đôi, và "năm sau" bắt đầu nghe như "mãi mãi". Độ dài mùa so với độ dài phiên là Tuning Knob cần chốt trước Vertical Slice (OQ-10).
- Nghịch lý ngược: nếu mọi tiến trình chỉ tính theo ngày (không theo giờ), người chơi ngủ sớm để "tua" — cũng là coi ngày như đơn vị hiệu suất. Cơ chế cụ thể thuộc Farming/Crafting; hệ này ghi nhận ở Interactions.

**Ngôn ngữ.** Dùng: *nhịp, ngày mai, về nhà, chờ, quay lại, giữ, dấu chấm* (cho giấc ngủ), *lời mời* (cho đêm). Tránh trong GDD, UI copy và hội thoại NPC: *hết giờ, trễ, lãng phí, bỏ lỡ, deadline, tối ưu ngày, "chỉ còn X giờ"*.

**Kiểm fantasy (đưa vào Acceptance Criteria và `/playtest-report`) — chẩn đoán, không phải cổng (D-4).** Sau buổi test Fun-Gate, hỏi **hai câu riêng**: (a) *"Có lúc nào bạn phải về nhà hoặc đi ngủ dù chưa muốn không? Vì sao?"* và (b) *"Có lúc nào bạn thấy phải chơi tiếp hoặc ở lại ngoài kia dù muốn nghỉ không? Vì sao?"* — một câu "có" với lý do là thời gian (không phải chán hay hết máu) là **dấu hiệu fantasy đang hỏng**, ghi thành phát hiện thiết kế. Câu trả lời tự khai đi kèm **chỉ số hành vi** từ Telemetry (ở ngoài sau 21:00, thời gian chờ giường, thua gần nhà, quay về trước hoàng hôn khi túi chưa đầy) và một **kịch bản ép hoàng hôn** (AC-PT1, AC-PT4, AC-PT5). Cổng của Fun-Gate vẫn là H1/H2 của concept.

## Detailed Design

*Nguồn: systems-designer (luật lõi, trạng thái, tương tác), gameplay-programmer (khả thi, testability), engine-programmer (tích hợp engine), 2026-10-25. Quyết định của người dùng D1–D4 cùng ngày. Mọi hành vi engine được nhắc ở đây là **NOT SOURCEABLE** từ `docs/engine-reference/unity/` (không có module core/time) — GDD chỉ nêu hợp đồng hành vi; cơ chế để ADR (xem Open Questions).*

### Core Rules

**Đơn vị và nguồn thời gian**

1. **Đồng hồ chuẩn.** Trạng thái duy nhất của hệ là một số nguyên `game_minute` ≥ 0 — số phút game kể từ **mốc 0 = Ngày 1, 06:00**. Mọi trường khác (ngày, giờ trong ngày, khoảng giờ, mùa, năm, thứ) là **hàm thuần** của `game_minute` (Formulas) và **không bao giờ được lưu**. Đơn vị nhỏ nhất của thời gian lịch là 1 phút game.
2. **Đơn điệu và danh sách đường ghi đóng.** `game_minute` chỉ tăng ngoài Restoring. **Chỉ năm đường** được ghi `game_minute`; mọi đường khác là lỗi (AC-R02.2):
   1. **Tick** (Rule 4, F1) — +0 hoặc +1 mỗi khung hình;
   2. **Stage 2 của pipeline** (Rule 12) — đặt tới mốc bình minh;
   3. **`AdvanceBy` của chuỗi thua** (Rule 18, F7) — chỉ Defeat & Retry gọi, chỉ tiến, **số phút ∈ [0, 30]** (lớn hơn 30 bị **từ chối** + ghi error, không đổi gì — Time tự bảo đảm "tối đa một ranh giới" của Rule 11 thay vì tin vào knob), kẹp ở `next_dawn − 1` (nên **không bao giờ vượt bình minh**); phát ranh giới khoảng giờ nó vượt qua (Rule 11); **bị từ chối (`Busy`)** khi có cờ chờ chuyển ngày hoặc đang DayTransition;
   4. **Restore / migration** (Rule 16, Rule 22) — chỉ trong Restoring;
   5. **Lệnh debug** — chỉ bản dev, bị loại khỏi bản phát hành (Edge Cases › Data/tuning).
   Không có hệ nào được lùi đồng hồ ngoài đường 4 và 5.
3. **Hai loại thời gian — phải gọi đúng tên.**
   - **Thời gian lịch** đi qua hệ này: cây lớn, xử lý nguyên liệu, restock cửa hàng, lịch NPC, hẹn ngày mai, respawn quái theo vùng.
   - **Thời gian khung hình** *không* đi qua hệ này: cooldown đòn đánh, animation, hit-stop, i-frame, VFX, đếm giờ trong combat.
   Một hệ dùng thời gian khung hình cho thứ thuộc lịch (hoặc ngược lại) là lỗi thiết kế, không phải lựa chọn triển khai.
   - **Ngoại lệ chỉ-đọc cho trình bày:** Atmosphere/Lighting và Audio được đọc **thời gian lẻ** (`game_minute + acc`, hoặc `segment_progress` mượt) để ánh sáng không giật theo từng phút. Không hệ nào khác được đọc `acc`, và không ai dùng giá trị lẻ cho logic lịch.
4. **Cách đồng hồ tiến.** Mỗi khung hình, hệ nhận **số giây thực đã trôi, không bị scale** (không nhân `Time.timeScale`):
   - kẹp ở `max_real_delta_per_frame`; phần vượt **bị bỏ, không bao giờ cộng dồn** (một lần giật, alt-tab hay resume từ sleep không được làm ngày trôi); giá trị âm hoặc NaN tính là 0 (F1) — đồng hồ không bao giờ nhận một giá trị không hữu hạn;
   - đổi sang phút game theo `real_seconds_per_game_minute`; phần lẻ giữ trong một bộ nhớ đệm nội bộ (không lưu, không lộ ra ngoài);
   - cộng số phút nguyên vào `game_minute`.
   Đồng hồ **không bao giờ** đọc giờ hệ điều hành (ngày giờ máy có thể nhảy hoặc lùi). Giờ thực chỉ được ghi làm metadata "lần chơi cuối" trong header save.
5. **Hit-stop và `timeScale` không chạm vào lịch.** Hit-stop, slow-motion hay bất kỳ thay đổi `timeScale` nào không đổi lịch và **không giữ token**. Combat tự chọn cơ chế hit-stop (global `timeScale` hay đóng băng cục bộ); hợp đồng ở đây chỉ là bất biến "hit-stop không bao giờ đổi lịch".

**Tạm dừng**

6. **Token tạm dừng.** Khi có **ít nhất một** token đang giữ, đồng hồ không tiến và bộ đệm phần lẻ không tích lũy. Token:
   - được **đếm** (giữ hai, nhả một → vẫn dừng); nhả hai lần là vô hại, không bao giờ đếm âm;
   - mang **loại**, **chủ sở hữu** và **lý do** (liệt kê được trong debug). **Loại** (`TokenKind`) là một **tập đóng**: `Menu`, `Dialogue`, `RegionLoad`, `Restore`, `Defeat`, `DayTransition`, `Focus`, `Boss`, `Cutscene` — đúng các hàng của Rule 8; thêm loại mới là sửa GDD này. Time quyết mọi luật phụ thuộc người giữ (vd. `Busy` khi có `RegionLoad` / `Restore`, States) **theo loại**, không theo chủ hay chuỗi lý do;
   - là một **handle** người giữ tự nhả (dạng `IDisposable`); handle mang **epoch** của tập token lúc được cấp. Khi tập token bị xóa (T10, về menu chính), epoch tăng; nhả một handle của epoch cũ là no-op;
   - **tự nhả khi chủ sở hữu bị hủy — chỉ với chủ là Unity object.** Bảo đảm phụ thuộc loại chủ:
     - **Chủ là `UnityEngine.Object` có vòng đời scene** (MonoBehaviour, component, GameObject — tham chiếu chủ giữ với kiểu `UnityEngine.Object` để phép so sánh null của engine nhận ra đối tượng đã hủy; OQ-2). **ScriptableObject không thuộc nhóm này**: nó gần như không bao giờ bị hủy, nên chủ là ScriptableObject được xử lý như chủ C# thuần (mục dưới). Với nhóm này: không phải cùng khung hình mà **chậm nhất ở tick kế tiếp của Time** — đầu mỗi tick, Time quét các token có chủ đã bị hủy (unload scene, `Destroy`, vốn hoãn tới cuối khung hình) và nhả chúng **trước** khi tính F1. Bước quét chạy **cả khi `token_count ≥ 1`**, trước kiểm tra "đồng hồ đang dừng". Một token rò rỉ vì chủ loại này bị hủy không khóa thế giới quá một tick.
     - **Chủ là đối tượng C# thuần hoặc ScriptableObject** (dịch vụ, không có vòng đời scene): **không có liveness** — Time không biết nó còn sống hay không, nên bảo đảm một tick **không áp dụng**. Token chỉ được nhả bởi scope (`using`/`finally`) hoặc lời gọi nhả của chủ; lưới an toàn duy nhất là watchdog (chỉ log, Edge Cases) và lối thoát nạp save / về menu chính.
     Exception **không** hủy chủ — an toàn với exception là việc của người giữ (giữ handle trong một scope `using`/`finally`, AC-R06.5);
   - **không bao giờ được lưu**; khi nạp save, tập token bắt đầu rỗng (epoch mới) và chỉ chứa **restore handle** do Time cấp (Rule 16); các hệ khác xin lại token khi cần. **Epoch chỉ tăng khi tập token bị xóa:** T1 (game mới), T2 và T10 (nạp save), T11 (về menu chính). T3 (Restoring → Active) **không** tăng epoch — restore handle cấp ở T2/T10 vẫn hợp lệ qua T3. Xin token trong Restoring được đếm trong epoch hiện tại. **Ngoại lệ — token `Focus`:** Time giữ cờ `is_focused` **độc lập với tập token** (đọc trạng thái focus hiện tại của ứng dụng, không chỉ dựa vào callback). Sau T1, T2, T10, nếu `is_focused = false`, Time **cấp lại** token `Focus` trong epoch mới; lấy lại focus nhả đúng token `Focus` của epoch hiện tại. T11 không cấp lại (Uninitialized không giữ token); lần T1/T2 kế tiếp đọc `is_focused` (Rule 8).
7. **Token chỉ dừng đồng hồ, không dừng thế giới.** Giữ token không ngụ ý `timeScale = 0`. Việc đóng băng vật lý, AI hay animation là trách nhiệm của hệ yêu cầu. Ngược lại, đặt `timeScale = 0` (vd. menu pause) **không** dừng đồng hồ — hệ đó phải xin token riêng.
8. **Ai giữ token** (danh sách ban đầu; hệ mới thêm vào bằng cách xin token, Time không cần biết họ):

   | Người yêu cầu | Giữ từ … đến … | Tier |
   |---|---|---|
   | Menu & Navigation | mọi menu/màn hình toàn phần (pause, kho, lò rèn, cây vũ khí, settings) mở → đóng | Fun-Gate |
   | Dialogue & Discovery Hints | hội thoại bắt đầu → kết thúc | Fun-Gate |
   | Region Loading | yêu cầu chuyển vùng → vùng mới active, catch-up xong và input trả lại | Fun-Gate |
   | Defeat & Retry | chuỗi thua bắt đầu → người chơi điều khiển lại ở nhà | Fun-Gate |
   | Game Time (chính nó) | ứng dụng mất focus / bị pause bởi OS → lấy lại (bản dev có toggle tắt riêng token này để Editor không nhiễu khi click qua cửa sổ khác; bản phát hành luôn bật) | Fun-Gate |
   | Game Time (chính nó) | restore / game mới: cấp **restore handle** cho người gọi (Region Loading) → người gọi nhả khi vùng active (Rule 16) | Fun-Gate |
   | Game Time (chính nó) | cờ chờ chuyển ngày được đặt (T7 / lệnh ngủ) → stage 8 phát xong sự kiện (Rule 12) | Fun-Gate |
   | Boss Encounter | bước vào arena → rời arena (thắng, thua hay bỏ) | MVP-Complete |
   | Cutscene & Scripted Sequence | bắt đầu → kết thúc | Vertical Slice |

   **Token `Focus` là lớp best-effort.** Nó phụ thuộc vào việc engine báo mất focus; vài đường có thể không báo (khởi động khi cửa sổ không có focus — xử lý bằng `is_focused`, Rule 6; overlay nền tảng; máy ngủ/thức trên desktop — OQ-11). **Lưới an toàn thật là kẹp `max_real_delta_per_frame`** (Rule 4): dù không có token, một lần mất focus hay ngủ máy chỉ "rò" tối đa một kẹp mỗi khung hình đồng hồ chạy. Cài đặt *Run In Background* được ghim cố định trong ProjectSettings để hành vi xác định (OQ-2); thiết kế đúng ở cả hai giá trị.

**Ngày và đêm**

9. **Ranh giới ngày là 06:00, không phải nửa đêm.** Ngày N là khoảng [06:00 ngày N, 06:00 ngày N+1). Phần 00:00–06:00 thuộc **đêm của ngày N**. Ngày chia năm khoảng giờ (ranh giới là dữ liệu tuning, mặc định):

   | Khoảng | Giờ | Ý nghĩa (cho hệ khác) | Người tiêu thụ tối thiểu |
   |---|---|---|---|
   | Morning | 06:00–11:00 | việc nhà: ruộng, tưới, chuẩn bị chuyến | Atmosphere/Lighting, Audio |
   | Noon | 11:00–14:00 | làng đông nhất (không phải lúc *duy nhất*) | NPC Schedules (VS); Fun-Gate: Atmosphere, Audio |
   | Afternoon | 14:00–18:00 | phần đầu của chuyến đi | Atmosphere/Lighting, Audio |
   | Evening | 18:00–21:00 | hoàng hôn: quay về hoặc ở lại; rèn; được phép ngủ từ đầu khoảng này | Lighting (đèn lồng), Interaction (giường) |
   | Night | 21:00–06:00 | `IsNight = true`: đồ rơi tốt hơn, quái mạnh hơn — phần mở rộng tùy chọn của chuyến | Enemy AI, Drops, Lighting, Audio |

   **Nhịp mỗi ngày một chuyến (D-1, 2026-10-26).** Một ngày thức ≈ 12–15 phút thực ở `s = 0,75` (F12) — không đủ cho mô hình "mỗi khoảng giờ một việc". Đơn vị nhịp là **một chuyến mỗi ngày**: sáng việc nhà và ghé làng (≈ 4–6 phút thực), rồi **một chuyến** (concept › Short-Term, 5–15 phút) bắt đầu từ trưa hoặc chiều và kéo tới hoàng hôn (≈ 5–7 phút thực) hoặc qua đêm (tới ≈ 12 phút thực). Khoảng giờ là **hình dạng không khí và cơ hội** (ánh sáng, ai ở làng, quái mạnh hơn về đêm), không phải lịch việc. Chuyến dài hơn một ngày là hợp lệ — rollover tại chỗ (Rule 14) không cắt chuyến.

   **Ràng buộc dữ liệu khoảng giờ:** mỗi khoảng phải có ít nhất một người tiêu thụ (cột trên) — khoảng không ai đọc là thừa và phải gộp; mỗi khoảng không phải Night dài **≥ `MIN_SEGMENT_MINUTES` = 120 phút game** (hằng của validator, không phải knob); mốc của từng khoảng nằm trong dải ở Tuning Knobs (validator, F4).

10. **Đêm là dữ liệu, không phải hạn chót.** Hệ công bố `IsNight` và sự kiện ranh giới khoảng giờ. **Không hệ nào được dùng đêm hay hết ngày làm hạn chót hoặc hình phạt** (anti-pillar *NOT a time-pressure game*): không đếm ngược, không cảnh báo "sắp hết ngày", không mất tiền/đồ khi thức qua đêm.
11. **Sự kiện ranh giới.** Sự kiện đổi khoảng giờ mang **payload** `(from, to, boundary_minute)` — khoảng giờ trước, khoảng giờ sau, và `game_minute` của ranh giới. **Handler phải dùng payload, không đọc lại `segment` / `IsNight` trực tiếp** để quyết định phản ứng: khi đồng hồ nhảy (ngủ), trạng thái trực tiếp đã là của thời điểm sau ranh giới. **Ai phát — mỗi ranh giới đúng một lần, từ đúng một nơi:**
    - **Tick** (F1) phát ranh giới trong ngày (Morning→Noon … Evening→Night), mỗi lần một ranh giới (F1 tiến tối đa 1 phút/khung hình). Tick chạm `next_dawn` **không** phát sự kiện; nó chỉ đặt cờ chờ chuyển ngày (T7).
    - **`AdvanceBy`** (Rule 2 đường 3) phát ranh giới trong ngày nó vượt qua — **tối đa một**, vì `defeat_time_cost_minutes ≤ 30 < MIN_SEGMENT_MINUTES`; nó không bao giờ vượt bình minh (F7 kẹp ở `next_dawn − 1`) và bị từ chối khi có cờ chờ.
    - **Stage 2 của pipeline** phát **ranh giới bình minh** trên cả hai đường. Rollover tại chỗ: đúng một sự kiện `Night → Morning`. **Ngủ: các ranh giới bị nhảy qua được gộp thành một sự kiện** `(from = khoảng giờ lúc ngủ, to = Morning, boundary_minute = sleep_target)` — vd. ngủ 19:00 phát `Evening → Morning`, không phát `Evening → Night` — để không hệ nào "bật chế độ đêm" trong lúc người chơi ngủ.
    Sự kiện chỉ là **thông báo cho nội dung đang nạp**; nguồn sự thật luôn là timestamp. Hệ nào cần biết "đã qua bao nhiêu đêm" khi vùng chưa nạp phải tính từ timestamp, không đếm sự kiện đã nhận.

    **Cô lập handler (mọi sự kiện của Time — ranh giới từ tick, `AdvanceBy`, stage 2; `DayStarted` / `SeasonChanged` / `YearChanged` ở stage 8).** `game_minute` **đã được ghi trước** khi sự kiện phát. Mỗi handler chạy trong một lớp bắt lỗi riêng: handler ném exception → ghi log (sự kiện, handler, lỗi), các handler còn lại **vẫn được gọi**. Danh sách handler được chụp trước khi phát; đăng ký / hủy đăng ký trong lúc phát (vd. `OnDisable` khi unload scene) có hiệu lực từ lần phát sau. Một handler lỗi **không bao giờ** làm đồng hồ đứng vĩnh viễn (Rule 12 stage 8). Lệnh gọi ngược vào Time từ trong handler (ngủ, `AdvanceBy`, token): luật cụ thể thuộc ADR pipeline + sự kiện (OQ-3).

**Chuyển ngày**

12. **Pipeline chuyển ngày.** Chuyển ngày chạy một pipeline có thứ tự, **không tái nhập**, giữ token "chuyển ngày" suốt thời gian chạy, chờ mỗi phase xong rồi mới sang phase sau.

    **Kích hoạt bằng cờ chờ.** T7 (tick chạm `next_dawn`) và lệnh ngủ được chấp nhận (T8) **không chạy pipeline ngay** trong tick hay trong callback của người gọi: chúng đặt cờ `pending_day_transition` (kèm loại: Sleep / Rollover) và giữ token chuyển ngày ngay lúc đó, nên đồng hồ đứng từ khung hình đặt cờ. **Bất biến: có cờ chờ ⇒ đang giữ token `DayTransition`**; token đó được giữ từ lúc đặt cờ **tới khi stage 8 nhả** (stage 1 xóa cờ nhưng token vẫn giữ, nên chiều ngược lại không đúng trong lúc pipeline chạy); mọi thao tác xóa tập token (T10, T11, restore) xóa cả cờ. Pipeline bắt đầu ở bước cập nhật kế tiếp của Time — bước này chạy **bất kể `token_count`** (lệnh ngủ chấp nhận từ Suspended(n) vẫn chạy pipeline ngay bước sau), **trừ khi có token loại `RegionLoad` hoặc `Restore`**: khi đó pipeline **chờ** (cờ vẫn đặt, đồng hồ vẫn đứng) tới khi các token đó được nhả rồi mới bắt đầu stage 1. Thứ tự các bước trong một khung hình (quét liveness, pipeline, tick): OQ-3. Lệnh ngủ trả `Accepted` / `NotSleepy` / `Busy` (Busy = đang có cờ chờ, đang DayTransition, hoặc đang Restoring / catch-up — States) đồng bộ cho người gọi.

    **Bất biến lưu (mọi tier).** Snapshot trạng thái được **niêm phong ở stage 7**; token chuyển ngày **giữ tới khi lần ghi có kết quả** (thành công hoặc thất bại sau `save_retry_count` lần thử); sự kiện stage 8 phát **sau** kết quả đó. **Stage 7 không bao giờ ghi khi một token `RegionLoad` hoặc `Restore` đang giữ** (không lưu trạng thái catch-up dở; bảo đảm bằng luật chờ ở trên). GDD này không quy định số khung hình: ghi trên luồng chính hay luồng nền là quyết định của ADR (OQ-3, OQ-6), miễn giữ bất biến.

    **Fun-Gate: pipeline đồng bộ.** Ở Fun-Gate mọi phase là hàm đồng bộ, không phase nào được `await`, và cho phép ghi save đồng bộ — cả pipeline chạy trong **khung hình chuyển ngày** (che bằng fade khi ngủ). Khung hình này có ngân sách riêng, không tính vào ngưỡng mỗi khung hình (AC-PF4, OQ-5). Phase bất đồng bộ, `phase_timeout_seconds` và các edge case đánh dấu *[async]* chỉ có hiệu lực khi ADR pipeline (OQ-3) cho phép phase bất đồng bộ.

    **Ngày đã là N+1 trước mọi phase.** Từ stage 2 trở đi, `game_minute` = mốc 06:00 của ngày mới, nên mọi phase (stage 3–7) đọc `day_index`, khoảng giờ (Morning) và `IsNight = false` của **ngày mới**.

    | Stage | Việc | Ghi chú |
    |---|---|---|
    | 1. Begin | xóa cờ chờ, khóa tái nhập (token chuyển ngày đã giữ từ lúc đặt cờ) | |
    | 2. Clock | đặt `game_minute` tới **mốc bình minh của lần chuyển này**: khi ngủ = `sleep_target` (F6); khi rollover tại chỗ = giá trị hiện tại (tick đã dừng đúng ở `next_dawn` — không cộng thêm ngày). Đặt `acc = 0`. Rồi phát **một** sự kiện bình minh có payload: rollover `Night → Morning`; ngủ `(khoảng giờ lúc ngủ) → Morning`, gộp mọi ranh giới bị nhảy qua (Rule 11) | đúng +1 ranh giới ngày mỗi lần chạy; **nơi duy nhất** phát ranh giới bình minh |
    | 3. World | làm tươi trạng thái vùng, respawn | phase của World State / Enemy; **rollover tại chỗ: không đụng thực thể của vùng active** (luật dưới) |
    | 4. Economy | restock, làm tươi hiển thị xử lý | growth và processing **tính lười từ timestamp**; phase chỉ làm tươi phần hiển thị |
    | 5. Social | thay đổi affinity theo ngày, hẹn NPC | |
    | 6. Narrative | trạng thái quest theo ngày, tin đồn | |
    | 7. Save | lưu (niêm phong); thử lại tại chỗ tối đa `save_retry_count` lần (Rule 15) | **cấu trúc bắt buộc là phase cuối có ghi trạng thái**, không phải quy ước đăng ký |
    | 8. End | phát `DayStarted` (rồi `SeasonChanged`, `YearChanged` khi có — Edge Cases), **rồi mới** nhả token chuyển ngày — **nhả kể cả khi handler ném exception** (bảo đảm kiểu `finally`, Rule 11) | sự kiện **chỉ để thông báo** |

    - **Sự kiện stage 8 chỉ để thông báo.** Save đã niêm phong ở stage 7, nên handler của `DayStarted` / `SeasonChanged` / `YearChanged` **không được thay đổi trạng thái được lưu** (kho đồ, flag, cây, affinity…) — thay đổi theo ngày thuộc một phase ở stage 3–6. Handler chỉ làm việc trình bày (HUD, âm thanh, VFX, telemetry). Phát trước khi nhả token để mọi handler chạy khi đồng hồ còn đứng ở 06:00.

    - Phase **đăng ký** vào một trong bốn stage World, Economy, Social, Narrative, kèm khóa thứ tự trong stage; **hai phase trùng khóa bị từ chối lúc đăng ký**. Stage Begin, Clock, End không nhận đăng ký; **chỉ Save/Load được đăng ký vào stage 7**. Đăng ký trong lúc pipeline đang chạy có hiệu lực từ lần chuyển ngày sau.
    - Pipeline chỉ xử lý **trạng thái luôn được nạp**; trạng thái ngoài màn hình được tính từ timestamp khi vùng nạp (Rule 17). Đó là lý do pipeline giữ ngắn.
    - **Rollover tại chỗ không đổi vùng đang active.** Khi loại chuyển ngày là Rollover, phase stage 3–6 **không tạo, xóa, reset hay respawn thực thể** trong vùng người chơi đang đứng (quái, điểm thu hoạch, đồ rơi, túi đồ); phần làm tươi theo ngày của vùng đó áp dụng ở **lần nạp vùng kế tiếp**, tính từ timestamp (Rule 17) — chuyến đi không bị cắt và trận đang đánh không bị reset (Rule 14). Làm tươi phần hiển thị của trạng thái tính lười (cây lớn) vẫn được phép vì không đổi thực thể. Khi ngủ, các phase áp dụng bình thường (che bằng fade). Time cấp loại chuyển ngày cho mọi phase; tuân thủ là mục review bắt buộc của GDD World State, Enemy AI, Farming.
    - **Phase lỗi:** ghi log, bỏ qua, pipeline chạy tiếp, **save vẫn được thử**. Một phase lỗi không được làm mất ngày của người chơi.
    - **Bất biến:** mỗi lần pipeline chạy, ngày tăng đúng 1 và sự kiện `DayStarted` phát đúng 1 lần.
13. **Ngủ.** Người chơi được ngủ từ `earliest_sleep_clock` (**18:00**) tới trước 06:00 — **không có giờ ngủ muộn nhất**, không ngất, không chi phí. Ngủ lúc 23:00 hay 03:00 đều chạy pipeline và **luôn thức dậy 06:00 ngày kế tiếp**. Save ghi vào slot ngủ. Giường có thêm lựa chọn **"Ngủ rồi thoát"**: chạy pipeline như ngủ thường, rồi thoát game sau stage 8 — điểm dừng tự nhiên của concept thành một thao tác. *(D3: chặn "ngủ sớm để tua" buổi sáng/chiều — anti-fantasy ở Player Fantasy.)*
14. **Không ngủ mà chạm 06:00.** Pipeline chạy **tại chỗ**: không dịch chuyển người chơi, không phạt, chuyến đi không bị cắt. Save ghi vào **slot autosave riêng**, không ghi đè slot ngủ. **Rollover tại chỗ và ngủ chạy cùng một pipeline với cùng các phase** — Time không cho giấc ngủ lợi ích lịch nào mà rollover không có. Hệ khác muốn thưởng riêng cho giấc ngủ (vd. hồi máu) được đọc loại chuyển ngày, nhưng không được biến rollover thành bất lợi (không trừ gì). *(D2.)*
15. **Lưu thất bại.** Stage 7 thử lại tại chỗ tối đa `save_retry_count` lần (mặc định 1). Vẫn thất bại thì ngày trong bộ nhớ **vẫn đã sang**; người chơi được báo (không chặn); slot đó được thử lại ở **stage 7 của lần chuyển ngày kế tiếp** (ngủ → slot ngủ; rollover tại chỗ → slot autosave). Không có lần thử nền giữa ngày; các lần lưu ở điểm an toàn và khi thoát (Rule 15a) là ghi độc lập vào slot tiếp tục, không phải thử lại. *(Tạm thời — chờ hợp đồng nguyên tử của GDD Save/Load, OQ-6.)*
15a. **Thoát game.** Hợp đồng chia hai mức (D-3, 2026-10-26):
    - **Thoát có chủ đích** — **mọi lệnh thoát mà engine cho phép hoãn** (menu "Lưu & thoát", "Ngủ rồi thoát", đóng cửa sổ đi qua cơ chế hoãn thoát của engine — OQ-1) — **không bao giờ mất tiến độ**: trạng thái hiện tại lưu vào **slot tiếp tục** — **không chạy pipeline, không sang ngày, không phát sự kiện**; `game_minute` lưu đúng giá trị hiện tại. Khi nạp, slot này khôi phục im lặng như mọi save (Rule 16). Hợp đồng: **dừng chơi lúc 15:00 không mất buổi chiều**. Nếu lệnh thoát đến khi có cờ chờ hoặc đang DayTransition, thoát **chờ pipeline chạy xong stage 8** rồi mới lưu slot tiếp tục — pipeline đồng bộ nên chờ không quá một khung hình (Fun-Gate); việc hoãn đi qua cơ chế hoãn thoát của engine, lần ghi là đồng bộ trên luồng chính (ghi nền bị giết khi thoát).
      **Thoát khi đang catch-up** (token `RegionLoad` / `Restore` đang giữ, **kể cả khi đồng thời có cờ chờ** — luật ưu tiên ở bảng trạng thái × lệnh): **thoát ngay, không lưu slot tiếp tục, không chờ** — không thể treo vì một token rò rỉ. Không mất gì kể từ lúc chuyển vùng, vì **lần lưu ở điểm an toàn "chuyển vùng" được ghi ngay trước khi Region Loading giữ token `RegionLoad`** (ràng buộc cho Region Loading và Save/Load). "Về menu chính" trong catch-up cũng vậy (T11 ngay, không lưu); "Game mới" trong catch-up hủy nạp, không lưu, rồi T1.
    - **Lệnh thoát engine không cho hoãn** (tắt máy / đăng xuất OS, process bị kill, thoát từ overlay nền tảng chưa được xác minh — OQ-11): rơi xuống mức thoát bất thường dưới đây.
    - **Thoát bất thường** (crash, mất điện, bị kill process): **mất tối đa một chuyến đi.** Save/Load tự lưu vào slot tiếp tục ở các **điểm an toàn** — vào nhà, chuyển vùng, điểm nghỉ trong vùng (danh sách do GDD Save/Load và Region Loading chốt) — cùng các save ở stage 7. Một lần lưu ở điểm an toàn **không** chạy pipeline, không sang ngày, không phát sự kiện; nó chỉ ghi `game_minute` hiện tại.
    Cơ chế ghi (nguyên tử, bắt tín hiệu thoát) thuộc GDD Save/Load (OQ-6). *(CD-GDD-ALIGN C1, 2026-10-25; D-3, 2026-10-26.)*

**Khôi phục, thua và arena**

16. **Khôi phục im lặng.** Khi nạp save, đồng hồ được khôi phục **đầu tiên** trong thứ tự restore (trước Flags, Inventory, Farming…), **không phát sự kiện ranh giới và không chạy pipeline** (nếu không, nạp save sẽ làm cây lớn hai lần).
    - **Restore handle.** Lời gọi restore (và lời gọi tạo game mới, T1) **xóa tập token** (epoch mới) rồi **trả về một restore handle** — một token do Time cấp trong epoch mới, chủ là người gọi (Region Loading). Đồng hồ đứng tới khi người gọi nhả handle, tức khi vùng active, catch-up xong và input được trả lại. Vì handle được cấp **sau** khi tập token bị xóa, T10 không thể xóa mất nó; Region Loading không xin token riêng cho luồng nạp save.
    - **Kiểm giá trị.** Restore **từ chối** `game_minute` âm, không đọc được, hoặc lớn hơn **`MAX_GAME_MINUTE` = 2.147.480.767** (= int32 max − 2·1440 ≈ 1,49 triệu ngày): nạp thất bại, Restoring → Uninitialized (Edge Cases › Restore/save). Không có giá trị nào bị kẹp im lặng. `MAX_GAME_MINUTE` là **giá trị thiết kế**, độc lập với kiểu số ADR chọn (OQ-1); khoảng dự trữ 2·1440 bảo đảm `next_dawn`, `sleep_target` (F6) và F13c (`cand + 1440`) không tràn int32. Tick không bao giờ đẩy đồng hồ quá `MAX_GAME_MINUTE` (bão hòa; không đạt được trong thực tế).
17. **Catch-up ngoài màn hình.** Hệ phụ thuộc lưu timestamp "cập nhật lần cuối" của riêng mình và, khi vùng nạp, tính trạng thái từ `game_minute − last_updated`:
    - tính bằng công thức đóng hoặc có giới hạn, **không bao giờ lặp qua từng ngày/phút đã trôi** (một khoảng trống 100 ngày phải tốn như 1 ngày);
    - chạy dưới token của Region Loading (hoặc restore handle khi nạp save), **trước khung hình đầu tiên** hiển thị nội dung; lệnh ngủ đến trong lúc này trả `Busy` (States);
    - độ trễ âm (đồng hồ khôi phục mà timestamp của hệ thì chưa) được coi là 0 và ghi log.
18. **Thua (không phải boss) — Fun-Gate.** Người chơi tỉnh dậy ở nhà, **giờ giữ nguyên** (cộng `defeat_time_cost_minutes` qua `AdvanceBy`, mặc định **0**), **không chuyển ngày, không save**. Chuỗi thua giữ token. *(D1 — thay stub "sáng hôm sau" của PR-SCOPE, vốn đốt phần còn lại của ngày và vi phạm "không mất ngày".)*
    **Chi phí phi thời gian bắt buộc ở Fun-Gate (D-2, 2026-10-26).** Thua không được là một lối về nhà miễn phí, nếu không phép đo "ở lại qua đêm" của Fun-Gate vô nghĩa. Defeat & Retry phải ship ở Fun-Gate: **nguyên liệu chưa cất rơi thành một túi đồ tại chỗ thua**; quay lại chạm vào thì nhặt lại đủ. Chỉ có **một túi** — thua lần nữa trước khi nhặt thì túi cũ mất. Đồ đã cất ở nhà, vũ khí và trang bị không bao giờ rơi. **Túi không hết hạn theo thời gian** — không biến mất khi sang ngày, qua đêm hay qua mùa (Rule 10, design test "chờ"). Chi tiết (cái gì tính là "chưa cất", túi hiện thế nào, vùng chưa nạp) thuộc GDD Defeat & Retry; hợp đồng của Time chỉ là: chi phí của việc thua **không bao giờ đo bằng thời gian lịch** trừ `defeat_time_cost_minutes` (biện pháp cuối, ≤ 30). Ràng buộc tối thiểu mà GDD Defeat & Retry phải thỏa (định nghĩa "chưa cất", chết không rẻ hơn đi về) ở Dependencies › N3–N4.
19. **Boss arena — MVP-Complete.** Token giữ từ lúc vào tới lúc rời arena, nên **giờ lúc rời = giờ lúc vào**. Checkpoint tạm trong bộ nhớ (Save/Load, TD-B2) **không cần khôi phục đồng hồ**: **retry trong arena không đi qua Restoring và không đổi trạng thái của Time** — token arena giữ liên tục qua mọi lần retry (Boss Encounter không nhả rồi xin lại), nên không có tick nào giữa các lần thử. Bản dev kiểm `game_minute` lúc retry bằng giá trị lúc vào và ghi error nếu khác. Thua boss không tốn phút game nào — luật Pillar 3 "thua boss không hỏng nông trại" là hệ quả của token, không cần mã riêng. Hệ quả đã chấp nhận: một trận boss không thể kéo qua hoàng hôn — arena muốn đổi không khí giữa các phase tự sở hữu ánh sáng, không đọc lịch.

**Lịch**

20. **Mùa và năm.** Năm = 4 mùa × `days_per_season` ngày (mặc định **14** → năm 56 ngày), lặp vô hạn. Thứ trong tuần = chu kỳ 7 ngày (cho giờ mở cửa và hẹn NPC). **Ở Fun-Gate và MVP-Complete, `seasons_enabled = false`**: mùa trả về *không có*, số ngày vẫn đếm; khi bật mùa ở Vertical Slice, timeline số nguyên không đổi. Đổi mùa **không bao giờ giết cây** (Calendar Contract).
21. **Không có gì bị lỡ vĩnh viễn.** Mọi nội dung gắn với ngày/mùa phải lặp lại ít nhất mỗi năm. Hệ cung cấp truy vấn "lần xuất hiện kế tiếp của [ngày/mùa/thứ]" để hệ khác (lịch hẹn, lễ hội, nhật ký) nói "quay lại sau", không nói "đã lỡ".
22. **Độ dài lịch là dữ liệu có version.** Vì timestamp nằm trong save, đổi `days_per_season` hay ranh giới ngày sau khi phát hành sẽ dịch ngày của save cũ. Các hằng này thuộc Game Data & Tuning và đi kèm version trong DTO save (TD-B4).

### States and Transitions

Trạng thái của Time là **hai trường độc lập**: **lifecycle** (bảng dưới) và **`token_count`** (n ≥ 0), cộng cờ `pending_day_transition`. Đồng hồ tiến **khi và chỉ khi** lifecycle = Active, n = 0 và không có cờ chờ. Hai tên quen thuộc là tên dẫn xuất: **Running** = Active ∧ n = 0; **Suspended(n)** = Active ∧ n ≥ 1.

**Đếm token.** `token_count` đếm **mọi** token đang giữ trong epoch hiện tại, **kể cả token do Time tự cấp** (`DayTransition`, `Focus`, restore handle). Vì có cờ chờ ⇒ giữ token `DayTransition` (Rule 12), **có cờ chờ ⇒ n ≥ 1**; điều kiện "không có cờ chờ" trong F1 chỉ là hàng rào phòng thủ. Từ lúc đặt cờ tới stage 1, lifecycle **vẫn là Active** (cột "Active — có cờ chờ"); lifecycle = DayTransition từ stage 1 tới T9. `CanSaveContinue` và vị từ catch-up xét **loại** token (Rule 6), không xét n.

| Lifecycle | Đồng hồ tiến? | Mô tả |
|---|---|---|
| **Uninitialized** | Không | Trước khi game mới hay save được nạp; sau khi về menu chính; sau khi nạp thất bại |
| **Restoring** | Không | Đang đặt `game_minute` từ save; không phát sự kiện; lệnh ngủ trả `Busy` |
| **Active** | Có, nếu n = 0 và không có cờ chờ | Trạng thái chơi bình thường |
| **DayTransition** | Không (đặt một lần ở stage 2) | Pipeline đang chạy; giữ token chuyển ngày; không tái nhập |

| # | Từ | Sang | Kích hoạt |
|---|---|---|---|
| T1 | Uninitialized | Active, n = 1 (n = 2 nếu `is_focused = false`: + token `Focus`) | Game mới: `game_minute = 0` (Ngày 1, 06:00), tập token rỗng (epoch mới) + **restore handle** cấp cho Region Loading (Rule 16) — game mới cũng đi qua nạp vùng; 0 sự kiện |
| T2 | Uninitialized | Restoring | Nạp save |
| T3 | Restoring | Active, n = 1 (+1 nếu `is_focused = false`; + token xin trong Restoring) | Khôi phục xong; restore handle (chủ: Region Loading) cấp ở T2/T10 — **T3 không tăng epoch** (Rule 6) |
| T3b | Restoring | Uninitialized | Nạp thất bại: DTO không đọc được, `game_minute` âm hoặc > `MAX_GAME_MINUTE` (Rule 16); tập token rỗng |
| T4/T5/T6 | Active, n | Active, n ± 1 | Giữ / nhả token (kể cả tự nhả khi chủ bị hủy, Rule 6). n = 0 ↔ Running |
| T7 | Active, n = 0 | Active, n = 1, cờ chờ → DayTransition (stage 1) | Tick chạm `next_dawn` khi người chơi thức (Rule 14): đặt cờ Rollover + token chuyển ngày; pipeline bắt đầu ở bước cập nhật kế tiếp (Rule 12) |
| T8 | Active, n ≥ 0 | Active, n + 1, cờ chờ → DayTransition (stage 1) | Lệnh ngủ khi `sleep_allowed` (F6: `minute_of_day ≥ offset(earliest_sleep_clock)` — 02:30 được phép) (Rule 13): đặt cờ Sleep. Token của menu xác nhận vẫn được đếm và nhả sau. **Bị từ chối (`Busy`)** khi đã có cờ chờ, hoặc đang có token loại `RegionLoad` / `Restore` (catch-up chưa xong) |
| T9 | DayTransition | Active, n | Stage 8 phát xong sự kiện và nhả token chuyển ngày (kể cả khi handler ném — Rule 11); n = số token còn lại sau khi nhả |
| T10 | Active (kể cả khi có cờ chờ) / DayTransition *[async]* | Restoring | Nạp save từ menu: xóa tập token **và cờ chờ** (epoch mới) — pipeline đang chờ không bao giờ chạy; nếu đang DayTransition thì pipeline bị hủy (không chạy tiếp, không save). **Retry trong boss arena không phải T10** (Rule 19) |
| T11 | Active | Uninitialized | Về menu chính (sau khi lưu slot tiếp tục, Rule 15a — **trừ khi đang catch-up: không lưu**): xóa tập token (epoch mới). Lệnh này đến khi có cờ chờ hoặc DayTransition thì chờ T9 — **trừ khi đang catch-up**: T11 ngay, xóa cờ (bảng trạng thái × lệnh) |

**Chuyển tiếp bị cấm:** DayTransition → DayTransition (tái nhập); bất kỳ trạng thái nào → giảm `game_minute` ngoài Restoring và lệnh debug (Rule 2); T7 khi n ≥ 1 (đồng hồ không thể chạm 06:00 khi đang dừng); ngủ khi `sleep_allowed = false` (F6) (trả `NotSleepy`, giường nói "chưa buồn ngủ" — không phải lỗi); ngủ khi đang Restoring hoặc catch-up (trả `Busy`).

**Bảng trạng thái × lệnh (đóng).** Mọi lệnh vào Time ở mọi trạng thái; ô không phải chuyển tiếp ở bảng trên là kết quả trả về. *Catch-up* = Active với token loại `RegionLoad` hoặc `Restore` đang giữ. *Lưu slot tiếp tục* gồm cả lưu ở điểm an toàn (Rule 15a); Time công bố `CanSaveContinue` = Active ∧ không cờ chờ ∧ không catch-up.

**Các cột rời nhau.** Catch-up và cờ chờ có thể đồng thời (cờ đặt rồi Region Loading giữ `RegionLoad`, hoặc ngược lại — AC-R12.11), nên trạng thái đó có cột riêng. **Luật ưu tiên khi cả hai:** Game mới, Nạp save, Về menu chính, Lưu slot tiếp tục / thoát có chủ đích theo **catch-up** — làm ngay, không chờ T9 (T9 không thể đến khi pipeline còn chờ token `RegionLoad` / `Restore`, nên chờ sẽ treo); thao tác xóa tập token cũng xóa cờ (Rule 12), pipeline không chạy. Ngủ và `AdvanceBy` theo **cờ chờ** (`Busy`).

**"Chờ T9" nghĩa là xếp hàng, không bao giờ là chờ chặn.** Lệnh ở ô "chờ T9" được Time ghi nhận và chạy **ngay sau T9** (cùng bước cập nhật, sau khi stage 8 nhả token); lời gọi trả về ngay cho người gọi. Vì pipeline Fun-Gate đồng bộ, chờ chặn từ trong một phase hay handler sẽ deadlock. "Nạp save" từ trong phase/handler vẫn **cấm** như ô DayTransition ghi. Luật tái nhập đầy đủ và thứ tự trong khung hình: OQ-3.

| Lệnh ↓ / Trạng thái → | Uninitialized | Restoring | Active — catch-up (không cờ) | Active — bình thường | Active — có cờ chờ (không catch-up) | Active — catch-up + cờ chờ | DayTransition |
|---|---|---|---|---|---|---|---|
| Game mới | T1 | `Busy` | hủy nạp (không lưu), T11 rồi T1 | T11 rồi T1 | chờ T9, rồi T11, T1 | hủy nạp (không lưu, xóa cờ), T11 rồi T1 | chờ T9, rồi T11, T1 |
| Nạp save | T2 | `Busy` (không nạp chồng) | T10 | T10 | T10 (xóa cờ, pipeline không chạy) | T10 (xóa cờ, pipeline không chạy) | Fun-Gate: **cấm** từ phase/handler (từ chối + error); *[async]*: T10 |
| Ngủ | `Busy` | `Busy` | `Busy` | T8 hoặc `NotSleepy` | `Busy` | `Busy` | `Busy` |
| `AdvanceBy` | từ chối + log | từ chối + log | chấp nhận | chấp nhận | `Busy` | `Busy` | `Busy` |
| Xin token | handle rỗng (no-op) | đếm (epoch của lần nạp này) | đếm | đếm | đếm | đếm | đếm |
| Lưu slot tiếp tục | không có gì để lưu | không lưu (`CanSaveContinue = false`) | **không lưu**; điểm an toàn bị bỏ qua; thoát có chủ đích **thoát ngay, không lưu** (Rule 15a) | lưu | chờ T9 rồi lưu | **không lưu**; thoát có chủ đích **thoát ngay, không lưu** (cờ bỏ, Rule 15a) | chờ T9 rồi lưu |
| Về menu chính | no-op | hủy nạp → Uninitialized | T11 ngay, **không lưu** (Rule 15a) | T11 | chờ T9 rồi T11 | T11 ngay, **không lưu** (xóa cờ) | chờ T9 rồi T11 |

Thoát có chủ đích ở Restoring **không** lưu slot tiếp tục: trạng thái trên đĩa vẫn là save đang được nạp, nên không mất gì.

### Interactions with Other Systems

*calls* = hệ kia gọi vào API của Time; *observes* = hệ kia nghe sự kiện; *registers* = hệ kia đăng ký phase/provider. Time không tham chiếu hệ nào ở trên nó. Mọi hệ dưới đây chưa có GDD — hợp đồng ở đây là **tạm thời**, phải được GDD tương ứng xác nhận.

| Hệ | Vào Time | Ra từ Time | Kiểu | Chủ giao diện |
|---|---|---|---|---|
| Game Data & Tuning Config | các hằng lịch (Tuning Knobs), có version | — | Time đọc | Game Data (registry); Time sở hữu schema lịch |
| Save/Load | lời gọi restore (nhận restore handle) | `game_minute` (DTO có version); phase cuối của pipeline; loại chuyển ngày cho slot; tín hiệu `CanSaveContinue` (không có cờ chờ, không DayTransition, không token `RegionLoad` / `Restore` — bảng trạng thái × lệnh) | registers (saveable + stage 7) + calls | Save/Load sở hữu hợp đồng saveable, slot tiếp tục và điểm an toàn; Time sở hữu stage 7 tồn tại |
| Flags, Conditions & World State | — | predicate ngày/mùa/khoảng giờ (vd. `day ≥ N`, `season == Spring`, `IsNight`) | Time registers condition provider | Flags |
| Input | — | — | không trực tiếp | — |
| Menu & Navigation Framework | token | — | calls | Time |
| Dialogue & Discovery Hints | token | truy vấn "lần kế tiếp" cho tin đồn/hẹn | calls | Time |
| Region Loading | token khi chuyển vùng; giữ restore handle khi nạp save / game mới | `game_minute` cho catch-up | calls | Time |
| Defeat & Retry | token; `AdvanceBy(defeat_time_cost_minutes)` (≤ 30, Rule 2) | `next_dawn` (F7) | calls | Time; túi đồ rơi (D-2) thuộc Defeat & Retry |
| Interaction & Context Prompts | lệnh ngủ (giường) qua API `RequestSleep` → `Accepted` / `NotSleepy` / `Busy` | `sleep_allowed` (F6), `sleep_target` — để giường hiện "Ngủ" / "Ngủ rồi thoát" hoặc "chưa buồn ngủ" | calls | Time sở hữu luật ngủ; Interaction sở hữu việc giường không tương tác được khi có hội thoại (AC-C12) |
| Boss Encounter | token arena | — | calls | Time |
| Cutscene & Scripted Sequence | token | — | calls | Time |
| Farming | phase stage 4 (làm tươi hiển thị) | `game_minute` để tính growth lười | calls + registers | Farming sở hữu công thức growth |
| Crafting & Forge | phase stage 4 | `game_minute` cho processing | calls + registers | Crafting |
| Enemy AI, Spawning & Drops | phase stage 3 | `IsNight`, sự kiện khoảng giờ; `game_minute` cho respawn | observes + registers | Enemy AI |
| NPC Affinity | phase stage 5 | ngày hiện tại | registers | Affinity |
| Quest & Hidden Quest | phase stage 6 | ngày, truy vấn "lần kế tiếp" | registers | Quest |
| NPC Schedules, Currency & Shops | phase stage 3/4 | khoảng giờ, thứ, mùa | observes + registers | hệ đó — **ràng buộc:** không hoạt động cốt lõi nào (tặng quà, quest, cửa hàng) chỉ mở trong một khoảng giờ ngắn hơn `min_core_activity_window` (ngưỡng do game-designer chốt ở GDD NPC Schedules) |
| HUD | — | giờ, ngày, khoảng giờ, mùa | observes | HUD (Rule 10: không đếm ngược) |
| Audio, Atmosphere & Lighting | — | khoảng giờ, tiến độ trong khoảng (0–1, mượt theo thời gian lẻ chỉ-đọc — Rule 3), mùa | observes | hệ đó; khi đồng hồ dừng, hệ đó tự quyết đứng yên hay tiếp tục hiệu ứng khung hình |
| Telemetry | — | `game_minute`, `DayStarted`, sự kiện ngủ/autosave | observes | Telemetry — **ghi thời gian game, không dùng giờ thực cho khoảng gameplay** |

**Ghi nhận cho GDD phụ thuộc (anti-fantasy):** nếu mọi tiến trình chỉ tính theo *ngày* (không theo giờ), người chơi sẽ ngủ sớm để tua. Farming và Crafting nên tính theo `game_minute` hoặc theo ranh giới ngày một cách có chủ đích; GDD của họ quyết.

**Rủi ro phụ thuộc → ADR:**
- **Time ↔ Save/Load:** Time đăng ký saveable vào Save/Load, Save/Load đăng ký stage 7 vào Time. Cả hai là đăng ký vào hợp đồng nên không phải vòng runtime, nhưng cần một **assembly hợp đồng trung lập** để asmdef không vòng (TD-C).
- **Time → Game Data & Tuning:** Time đọc hằng tuning, nên index phải ghi cạnh này (Time không còn "không phụ thuộc gì").

## Formulas

*Nguồn: systems-designer, 2026-10-25, dựa trên Section C và quyết định D1–D4. Mọi công thức là số nguyên trên `game_minute` trừ F1 (bộ đệm phần lẻ) và F12 (ước tính thời gian thực).*

**Quy ước chung.** `MINUTES_PER_DAY = 1440` và `DAY_START_CLOCK = 360` (06:00) là hằng (Rule 9, 22). Mọi giờ trong dữ liệu tuning được author theo **giờ đồng hồ** (vd. 18:00) và quy về **phút kể từ 06:00** bằng `offset(c)` (F4a), nên mọi so sánh trong ngày không phải xử lý wrap qua nửa đêm. Ví dụ chuẩn xuyên suốt: `game_minute = 1500`, `s = 0,75`, `D = 14`.

### F1. Minute advance per frame (kẹp + phần lẻ)

The minute_advance formula is defined as:

`real_eff = (lifecycle ≠ Active ∨ token_count > 0 ∨ pending ∨ ¬(real_delta > 0)) ? 0 : min(real_delta, max_real_delta_per_frame)`
`acc_raw = acc + real_eff / real_seconds_per_game_minute`
`minute_advance = floor(acc_raw)`
`acc_next = acc_raw − minute_advance`
`game_minute_next = min(game_minute + minute_advance, MAX_GAME_MINUTE)` (bão hòa, Rule 16; chạm trần thì ghi log); nếu `minute_advance = 1` **và** `game_minute_next mod 1440 = 0` (tức `day_index(game_minute_next) > day_index(game_minute)`) → T7 (đặt cờ chờ Rollover). Đứng yên ở mốc bình minh (game mới tại 0, khôi phục tới bội của 1440, sau T9) **không** kích hoạt T7 (AC-S19).

*Ghi chú triển khai:* điều kiện `¬(real_delta > 0)` là **đúng khi `real_delta` là NaN** (mọi so sánh với NaN đều sai), nên NaN, 0 và số âm đều cho `real_eff = 0`; `+∞ > 0` nên `+∞` bị kẹp về `max_real_delta_per_frame`. Không được viết `max(real_delta, 0)` — với NaN, `Math.Max` của .NET trả NaN, làm `acc` thành NaN vĩnh viễn và đồng hồ đứng mãi. Nếu `acc` vì bất kỳ lý do gì không hữu hạn, đặt lại 0 và ghi error.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `real_delta` | float (s) | bất kỳ (thực tế 0,004 – vài giây; NaN/âm do lỗi) | calculated (engine, unscaled) | Giây thực từ khung hình trước, **không nhân** `timeScale` (Rule 4–5) |
| `lifecycle` | enum | Uninitialized, Restoring, Active, DayTransition | calculated | Trạng thái vòng đời (States); ngoài Active đồng hồ không tiến dù n = 0 (vd. Uninitialized ở menu chính) |
| `token_count` | int | 0 – ∞ | calculated | Số token tạm dừng đang giữ (Rule 6) |
| `pending` | bool | | calculated | Cờ `pending_day_transition` (Rule 12) |
| `max_real_delta_per_frame` | float (s) | 0,05 – 0,25; mặc định **0,1** | data file | Trần giây thực tính mỗi khung hình; phần vượt bị bỏ |
| `real_seconds_per_game_minute` (`s`) | float (s) | 0,4 – 1,5 (cứng); thiết kế 0,75 – 0,9; mặc định **0,75** | data file | Giây thực cho 1 phút game |
| `acc` | float (phút) | [0, 1) | calculated (nội bộ, không lưu) | Bộ đệm phần lẻ; giữ nguyên (không reset) khi dừng; về 0 ở stage 2 và khi restore |
| `minute_advance` | int | {0, 1} | calculated | Phút nguyên cộng vào trong khung hình này |
| `game_minute` | số nguyên (kiểu do ADR chọn, OQ-1) | 0 – `MAX_GAME_MINUTE` (2.147.480.767) | calculated (lưu trong save) | Đồng hồ chuẩn (Rule 1); trần là giá trị thiết kế (Rule 16) |

**Output Range:** `minute_advance ≥ 0` luôn đúng (đơn điệu, Rule 2). Vì dải cứng của kẹp (≤ 0,25) nằm dưới dải cứng của `s` (≥ 0,4), luôn có `real_eff / s < 1` và `acc < 1`, nên `minute_advance ∈ {0, 1}` — một khung hình không bao giờ vượt quá một ranh giới. Hệ quả: tick luôn **dừng đúng** ở `next_dawn` (không thể nhảy qua), nên không cần kẹp riêng tại `next_dawn`; ngay khi chạm, cờ chờ dừng đồng hồ (T7). Vượt nhiều khoảng giờ chỉ xảy ra khi ngủ, ở stage 2 (F6, Rule 11). Ràng buộc `max_real_delta_per_frame ≤ s` vẫn được validator kiểm như hàng rào phòng thủ (nếu sau này nới dải).

| Trường hợp biên | Kết quả | Thoái hóa? |
|---|---|---|
| `s = 0,4`, kẹp 0,25 | tối đa 0,625 phút/khung hình | Không |
| `s = 1,5`, kẹp 0,05 | tối đa 0,033 phút/khung hình | Không |
| kẹp > `s` (chỉ khi dải bị nới) | `minute_advance` có thể ≥ 2, nhảy qua `next_dawn` | **Có** — validator từ chối |
| `s ≤ 0` | chia cho 0 / thời gian âm | **Có** — validator từ chối |
| `real_delta` NaN, 0 hoặc âm | `real_eff = 0` | Không — xem ghi chú triển khai |
| Máy chạy dưới 1/kẹp FPS (vd. 15 FPS, kẹp 0,05) | đồng hồ chậm hơn spec (75%) | Có lợi cho người chơi, không phạt; test dùng Δ cố định |
| Hitch 2 s, kẹp 0,1 | mất 1,9 s ≈ 2,5 phút game | Chấp nhận (Rule 4: phần vượt bị bỏ) |

Lý do 0,1 s: tương đương 10 FPS — mọi khung hình bình thường (60 FPS = 0,0167 s; 30 FPS = 0,033 s) được tính đủ; hitch hay alt-tab chỉ "rò" tối đa 0,1 s.

**Example:** `s = 0,75`, 60 FPS (`real_delta = 0,01667`), không token: mỗi khung hình +0,02222 phút vào `acc`; sau 45 khung hình (0,75 s) `acc_raw = 1,0` → `minute_advance = 1`, `acc_next = 0`. Hitch `real_delta = 1,8` → `real_eff = 0,1` → +0,1333 phút. Menu mở (`token_count = 1`) → `real_eff = 0`, `acc` giữ nguyên.

### F2. day_index và day_number

The day_index formula is defined as:

`day_index = floor(game_minute / MINUTES_PER_DAY)`
`day_number = day_index + 1`
`next_dawn = (day_index + 1) · MINUTES_PER_DAY` *(mốc 06:00 kế tiếp — dùng ở F6, F7, Rule 12)*

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `game_minute` | số nguyên | 0 – `MAX_GAME_MINUTE` | calculated (save) | Đồng hồ chuẩn |
| `MINUTES_PER_DAY` | int | 1440 | constant | Phút mỗi ngày |
| `day_index` | long | 0 – ∞ | calculated | Ngày đếm từ 0 (Ngày 1 = 0) |
| `day_number` | long | 1 – ∞ | calculated | Ngày hiển thị cho người chơi và Telemetry |

**Output Range:** `day_index` ∈ [0, 1.491.306] (`MAX_GAME_MINUTE` / 1440); tăng đúng 1 mỗi lần pipeline chạy (Rule 12). Ở `MAX_GAME_MINUTE`, `next_dawn` = 2.147.482.080 — vẫn trong int32. Trần tương đương ~26.600 năm game ở `D = 14`, không đạt được trong thực tế. Không thoái hóa.

**Example:** `game_minute = 1500` → `day_index = 1` → **Ngày 2**. `game_minute = 1439` → `day_index = 0` (05:59, đêm Ngày 1).

### F3. minute_of_day và clock_minute (giờ hiển thị)

The clock_minute formula is defined as:

`minute_of_day = game_minute mod MINUTES_PER_DAY`
`clock_minute = (minute_of_day + DAY_START_CLOCK) mod MINUTES_PER_DAY`
`clock_hour = floor(clock_minute / 60)`, `clock_min = clock_minute mod 60`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `game_minute` | long | 0 – ∞ | calculated | Đồng hồ chuẩn |
| `DAY_START_CLOCK` | int | 360 | constant | 06:00 tính bằng phút đồng hồ |
| `minute_of_day` | int | 0 – 1439 | calculated | Phút kể từ 06:00 của ngày hiện tại — dùng cho mọi so sánh trong ngày |
| `clock_minute` | int | 0 – 1439 | calculated | Phút kể từ 00:00 của **đồng hồ hiển thị** |
| `clock_hour`, `clock_min` | int | 0–23, 0–59 | calculated | HH:MM cho HUD |

**Output Range:** [0, 1439] cho cả hai (modulo). 00:00–05:59 ứng với `minute_of_day` 1080–1439 và vẫn mang `day_index` của ngày trước, vì ngày lấy từ F2 — đây chính là luật "đêm thuộc ngày N" (Rule 9). Không thoái hóa.

**Example:** `game_minute = 1500` → `minute_of_day = 60` → `clock_minute = 420` → **07:00, Ngày 2**. `game_minute = 1230` → `minute_of_day = 1230` → `clock_minute = 1590 mod 1440 = 150` → **02:30**, `day_index = 0` → vẫn là **đêm Ngày 1**.

### F4. Segment lookup (+ tiến độ trong khoảng)

The segment formula is defined as:

`offset(c) = (c − DAY_START_CLOCK + MINUTES_PER_DAY) mod MINUTES_PER_DAY`  *(F4a — quy giờ đồng hồ `c` sang offset)*
`segment = max{ i : start_i ≤ minute_of_day }`, với `start_i = offset(segment_start_clock[i])`
`segment_progress = (minute_of_day − start_i) / (start_{i+1} − start_i)`, với `start_5 = 1440`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `segment_start_clock[0..4]` | int (phút đồng hồ) | mặc định 360, 660, 840, 1080, 1260 | data file | Morning 06:00, Noon 11:00, Afternoon 14:00, Evening 18:00, Night 21:00 |
| `start_i` | int | 0 – 1439 | calculated | Offset mặc định: 0, 300, 480, 720, 900 |
| `minute_of_day` | int | 0 – 1439 | calculated (F3) | |
| `segment` | enum | {Morning, Noon, Afternoon, Evening, Night} | calculated | Khoảng giờ hiện tại |
| `segment_progress` | float | [0, 1) | calculated | Cho Atmosphere/Audio làm gradient |

**Output Range:** Luôn đúng một khoảng giờ nếu dữ liệu hợp lệ. Validator phải bắt: `start_0 = 0` (Morning bắt đầu đúng ranh giới ngày), `start_i` **tăng nghiêm ngặt**, mỗi khoảng không phải Night dài **≥ `MIN_SEGMENT_MINUTES` (120)**, và mỗi mốc nằm trong dải của nó (Tuning Knobs). Thoái hóa: hai mốc trùng nhau → khoảng dài 0 phút và mẫu số `segment_progress` = 0; mốc không tăng dần (vd. Night 19:00 trước Evening 20:00) → khoảng giờ đảo; `start_0 ≠ 0` → đầu ngày không thuộc khoảng nào. Cả ba bị từ chối. Khoảng ngắn nhất ở mặc định là Noon 180 phút = 2,25 phút thực (s = 0,75); 1,2 phút thực (s = 0,4); 4,5 phút thực (s = 1,5).

**Example:** `game_minute = 1500` → `minute_of_day = 60` → `0 ≤ 60 < 300` → **Morning**, `segment_progress = 60/300 = 0,20`. `game_minute = 1230` → `1230 ≥ 900` → **Night**, `progress = (1230 − 900)/540 = 0,611`.

### F5. IsNight

The is_night formula is defined as:

`is_night = (segment == Night)`  ⇔  `minute_of_day ≥ offset(segment_start_clock[Night])`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `segment` | enum | 5 giá trị | calculated (F4) | |
| `segment_start_clock[Night]` | int | 1200 – 1320 (20:00–22:00); mặc định 1260 | data file | Mốc bắt đầu đêm — **một nguồn duy nhất**, không có knob `night_start` riêng |
| `is_night` | bool | {true, false} | calculated | Cờ cho Enemy AI, Drops, Audio, Lighting |

**Output Range:** Boolean. Đêm kéo dài từ mốc Night tới 06:00: mặc định 540 phút game = 6,75 phút thực (s = 0,75), dải 3,6–13,5 phút thực theo `s`. Ở biên mốc: Night 20:00 → đêm 600 phút (Evening mặc định 18:00 còn đúng 120 phút); Night 22:00 → 480 phút. Validator chặn mốc Night dưới 20:00 (với Evening mặc định 18:00, Night sớm hơn làm Evening < `MIN_SEGMENT_MINUTES`). Không thoái hóa nếu F4 hợp lệ.

**Example:** `game_minute = 1500` (07:00) → `60 < 900` → `false`. `game_minute = 1230` (02:30) → `1230 ≥ 900` → `true`.

### F6. Sleep allowed + sleep target timestamp

The sleep_target formula is defined as:

`sleep_allowed = minute_of_day ≥ offset(earliest_sleep_clock)`
`sleep_target = (day_index + 1) · MINUTES_PER_DAY`
`minutes_skipped = sleep_target − game_minute`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `earliest_sleep_clock` | int (phút đồng hồ) | 720 – 1260 (12:00–21:00); mặc định **1080** (18:00) | data file | Giờ sớm nhất được ngủ (Rule 13, D3) |
| `minute_of_day`, `day_index` | int / long | xem F2–F3 | calculated | |
| `sleep_allowed` | bool | | calculated | `false` → giường nói "chưa buồn ngủ" |
| `sleep_target` | long | | calculated | Luôn là 06:00 ngày kế tiếp; trùng `next_dawn` (F2) và stage 2 của pipeline |
| `minutes_skipped` | int | 1 – 720 (mặc định) | calculated | Phút game bị nhảy khi ngủ |

**Output Range:** `minutes_skipped ∈ [1, 1440 − offset(earliest_sleep_clock)]` — mặc định [1, 720]. Không có giờ ngủ muộn nhất: 05:59 vẫn hợp lệ, nhảy 1 phút. Pipeline luôn tăng đúng một ngày vì `sleep_target` luôn là ranh giới kế tiếp, kể cả ngủ sau nửa đêm. Ở biên knob: 12:00 → nhảy tối đa 1080 phút; 21:00 → tối đa 540 phút, chỉ ngủ được trong đêm. Ngoài dải: giờ < 06:00 (vd. 03:00 → offset 1260) thu cửa sổ ngủ còn 3 giờ cuối đêm; 06:00 (offset 0) = ngủ bất cứ lúc nào (phương án D3-B đã bị bác). Validator từ chối mọi offset ngoài [360, 900].

**Example (ngủ lúc 02:30):** `game_minute = 1230` (đêm Ngày 1) → `1230 ≥ 720` → được ngủ → `sleep_target = 1440` → **06:00 Ngày 2**, `minutes_skipped = 210`. Ngủ 18:00 Ngày 2 (`game_minute = 2160`, `minute_of_day = 720`) → `sleep_target = 2880`, nhảy 720. Thử ngủ 07:00 Ngày 2 (`game_minute = 1500`) → `60 < 720` → **bị từ chối**.

### F7. Defeat time (Rule 18)

The defeat_wake formula is defined as:

`defeat_wake_minute = min(game_minute + defeat_time_cost_minutes, next_dawn − 1)`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `defeat_time_cost_minutes` | int | 0 – 30; mặc định **0** | data file | Phút game cộng khi thua (D1) |
| `next_dawn` | long | | calculated (F2) | |
| `defeat_wake_minute` | long | | calculated | Giờ người chơi tỉnh dậy ở nhà |

**Output Range:** `[game_minute, next_dawn − 1]` (luôn khác rỗng vì khi đang Active thì `game_minute ≤ next_dawn − 1`). Phép `min` giữ Rule 18 "không chuyển ngày" khi cost > 0: không kẹp, thua lúc 05:45 với cost 30 sẽ vượt 06:00 và kích hoạt rollover trong chuỗi thua (U1 ở Edge Cases chọn kẹp). Với mặc định 0, hàm là đồng nhất. Giá trị được ghi qua `AdvanceBy` (Rule 2, đường 3), không bao giờ lùi.

**Example:** cost 0, thua 07:00 Ngày 2 (`game_minute = 1500`) → tỉnh dậy ở 1500. Cost 30, thua 05:45 (`game_minute = 1425`, `next_dawn = 1440`) → `min(1455, 1439) = 1439` → 05:59.

### F8. season_index

The season_index formula is defined as:

`season_index = seasons_enabled ? (floor(day_index / days_per_season) mod SEASONS_PER_YEAR) : None`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `seasons_enabled` | bool | false (Fun-Gate, MVP-Complete), true (VS+) | data file | Rule 20 |
| `days_per_season` (`D`) | int | {7, 14, 21, 28}; mặc định **14** | data file | Ngày mỗi mùa |
| `day_index` | long | 0 – ∞ | calculated (F2) | |
| `SEASONS_PER_YEAR` | int | 4 | constant | |
| `season_index` | int? | {0..3} hoặc None | calculated | 0 Spring, 1 Summer, 2 Autumn, 3 Winter |

**Output Range:** {0, 1, 2, 3} hoặc None. `D = 0` (chia cho 0) và `D` âm bị loại sẵn bởi tập cho phép. Bật mùa giữa một save cũ → mùa tính lại từ `day_index` (Ngày 20 đột ngột là Summer) — hệ quả đã được Rule 22 chấp nhận (DTO có version).

**Example:** `game_minute = 1500` → `day_index = 1`; bật mùa, `D = 14` → `floor(1/14) mod 4 = 0` → **Spring**. Tắt mùa → **None** (HUD chỉ hiện "Ngày 2"). `day_index = 14` → Summer; `day_index = 56` → Spring năm 2.

### F9. year

The year formula is defined as:

`year = seasons_enabled ? floor(day_index / (SEASONS_PER_YEAR · days_per_season)) + 1 : None`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `day_index` | long | 0 – ∞ | calculated | |
| `days_per_season` | int | {7, 14, 21, 28} | data file | |
| `SEASONS_PER_YEAR` | int | 4 | constant | |
| `year` | long? | 1 – ∞ hoặc None | calculated | Năm hiển thị |

**Output Range:** Không chặn trên; năm dài 28 / 56 / 84 / 112 ngày theo `D`. Tắt mùa → **None** (không trả 1, để HUD không hiện "Năm 1" vô nghĩa ở Fun-Gate). Không thoái hóa trong dải.

**Example:** `day_index = 1`, `D = 14` → `floor(1/56) + 1` = **Năm 1**. `day_index = 56` → Năm 2.

### F10. day_of_season

The day_of_season formula is defined as:

`day_of_season = seasons_enabled ? (day_index mod days_per_season) + 1 : None`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `day_index` | long | 0 – ∞ | calculated | |
| `days_per_season` | int | {7, 14, 21, 28} | data file | |
| `day_of_season` | int? | 1 – D hoặc None | calculated | "Spring 2" |

**Output Range:** [1, D] hoặc None; tắt mùa thì HUD dùng `day_number` (F2).

**Example:** `day_index = 1`, `D = 14` → **2** → "Spring 2, Năm 1".

### F11. weekday

The weekday formula is defined as:

`weekday = day_index mod DAYS_PER_WEEK`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `day_index` | long | 0 – ∞ | calculated | |
| `DAYS_PER_WEEK` | int | 7 | constant | |
| `weekday_names[0..6]` | string | 7 nhãn | data file | Tên thứ (localize) |
| `weekday` | int | 0 – 6 | calculated | 0 = thứ của Ngày 1 |

**Output Range:** [0, 6], không phụ thuộc `seasons_enabled` (giờ mở cửa và hẹn NPC dùng được từ Fun-Gate). Vì `D` là bội của 7, ngày 1 mỗi mùa luôn cùng thứ — lịch in được dạng lưới tuần cố định. `D` không phải bội 7 sẽ làm thứ trôi giữa các mùa (thoái hóa mềm) — tập cho phép đã loại.

**Example:** `day_index = 1` → **weekday 1**. `day_index = 14` (Summer 1) → 0, cùng thứ với Spring 1.

### F12. Thời gian thực mỗi ngày và mỗi phiên (kiểm H1)

The real_time formula is defined as:

`real_min_full_day = MINUTES_PER_DAY · s / 60`
`real_min_awake_day = offset(sleep_clock) · s / 60`
`real_min_session_day = real_min_awake_day / (1 − suspended_share)`
`days_per_session = session_real_minutes / real_min_session_day`
`real_min_to_day_N = (N − 1) · real_min_session_day`
`real_min_outing = (offset(return_clock) − offset(depart_clock)) · s / 60` *(D-1: độ dài một chuyến)*
`u_max = 1 − (N − 1) · real_min_awake_day / 60` *(trần `suspended_share` để ngày N bắt đầu trước phút 60)*

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `s` | float | 0,4 – 1,5 (cứng); thiết kế 0,75 – 0,9; mặc định 0,75 | data file | |
| `depart_clock`, `return_clock` | int (phút đồng hồ) | điển hình đi 11:00–14:00; về 21:00 (hoàng hôn hết), 23:00, hoặc 05:59 (qua đêm) | calculated (hành vi người chơi — giả định playtest) | Lúc rời nhà và lúc về/ngủ của chuyến trong ngày |
| `sleep_clock` | int (phút đồng hồ) | 18:00 – 05:59; điển hình 22:00–23:00 | calculated (hành vi người chơi — giả định playtest) | Giờ người chơi thường đi ngủ |
| `suspended_share` (`u`) | float | 0 – 0,4; giả định **0,15** | calculated (ước tính playtest — **không phải knob**) | Tỷ lệ thời gian thực đồng hồ đứng (menu, hội thoại, nạp vùng) |
| `session_real_minutes` | float | 30 – 120 | constant (concept › Session-Level) | Độ dài phiên |
| `N` | int | ≥ 1 | constant (H1: N = 4) | Ngày mục tiêu |

**Output Range:** Mọi đầu ra thời gian thực dương, tuyến tính theo `s`. `u → 1` làm mẫu số → 0, không xảy ra trong thực tế; giả định chặn ở ≤ 0,4. **`u_max` có thể ≤ 0:** với ngủ 23:00 và N = 4, `u_max ≤ 0` khi `s ≳ 1,18` (vd. s = 1,5 → −0,275) — nghĩa là **H1 không đạt được ở bất kỳ u nào**, không phải một trần âm; nhất quán với trần H1 `s ≤ 1,0` (ở s = 1,0, `u_max` = 0,15).

| `s` | Cả ngày 24 h | Ngày thức tới 23:00 | Có u = 0,15 | Ngày 4 bắt đầu (u = 0 / 0,15) | Ngày/phiên 30–120 phút (u = 0,15) |
|---|---|---|---|---|---|
| 0,4 | 9,6 | 6,8 | 8,0 | 20,4 / 24,0 | 3,8 – 15,0 |
| **0,75** | **18,0** | **12,75** | **15,0** | **38,3 / 45,0** | **2,0 – 8,0** |
| 0,9 | 21,6 | 15,3 | 18,0 | 45,9 / 54,0 | 1,7 – 6,7 |
| 1,0 | 24,0 | 17,0 | 20,0 | 51,0 / 60,0 | 1,5 – 6,0 |
| 1,1 | 26,4 | 18,7 | 22,0 | 56,1 / 66,0 | 1,4 – 5,5 |
| 1,5 | 36,0 | 25,5 | 30,0 | 76,5 / 90,0 | 1,0 – 4,0 |

(phút thực; ngủ lúc 22:00 với s = 0,75 → 12,0 phút/ngày → ngày 4 ở phút 36,0 / 42,4.)

**Độ dài một chuyến (D-1)** — rời nhà 14:00:

| `s` | Về khi hết hoàng hôn (21:00) | Về 23:00 | Qua đêm (05:59) | Concept › Short-Term 5–15 phút |
|---|---|---|---|---|
| **0,75** | **5,25** | **6,75** | **12,0** | khớp cả ba |
| 0,9 | 6,3 | 8,1 | 14,4 | khớp cả ba |
| 1,0 | 7,0 | 9,0 | 16,0 | qua đêm vượt 15 |

Rời nhà 11:00 cộng thêm 2,25 phút (s = 0,75). Phần sáng trước chuyến (06:00–14:00) là 6,0 phút ở s = 0,75.

Kết luận:
- "Ngày 4 ≈ phút 36–40" (cổng H1) đúng **khi chỉ tính thời gian đồng hồ chạy**; tính cả menu và hội thoại là ≈ 42–45 phút — vẫn trong một phiên 60 phút.
- **Trần an toàn cho H1: `s ≤ 1,0`.** Ở `s = 1,0` và `u = 0,15`, ngày 4 bắt đầu đúng phút 60,0 — không còn biên; ở `s = 1,1` là phút 66 (đã trượt). Mọi `s > 1,0` sinh cảnh báo validator.
- **Dải thiết kế `s` 0,75 – 0,9:** là dải duy nhất mà cả ba kiểu chuyến đều nằm trong 5–15 phút của concept và ngày 4 vẫn trước phút 60 với `u = 0,15`.
- **Trần `suspended_share`:** ở `s = 0,75`, ngủ 23:00, `u_max = 1 − 3 · 12,75 / 60 = 0,3625` → nếu playtest đo `u > 0,36` thì F12 không còn dự đoán đúng H1 (AC-PT3).
- **Ngày có chuyến qua đêm dài hơn:** một ngày thức trọn (ngủ 05:59) là 18,0 phút ở s = 0,75 (21,2 với u = 0,15). Người chơi ở lại qua đêm nhiều sẽ tới ngày 4 muộn hơn dự đoán — đó là hành vi fantasy muốn, không phải lỗi; `/playtest-report` ghi giờ ngủ trung vị cạnh ngày 4 (AC-PT2).
- Với `s = 0,4`, một phiên thành 4–15 ngày — quá nhanh cho hình dạng của ngày (Noon còn 1,2 phút thực).

| `D` | Một mùa (s = 0,75, 15 phút/ngày) | Một năm |
|---|---|---|
| 7 | 1,75 giờ | 7 giờ |
| **14** | **3,5 giờ** | **14 giờ** |
| 21 | 5,25 giờ | 21 giờ |
| 28 | 7 giờ | 28 giờ |

Với `D = 28`, một lượt chơi ~20 giờ chưa tới mùa đông — không vi phạm Rule 21 (nội dung vẫn lặp), nhưng nội dung mùa cuối gần như không được thấy; đây cũng là rủi ro "năm quá dài" của Player Fantasy. `D = 14` cho một năm ≈ 14 giờ chơi, tức "năm sau" đến trong lượt chơi 1.0 (~20–30 giờ).

**Example:** `s = 0,75`, ngủ 23:00, `u = 0,15`: `real_min_awake_day = 1020 · 0,75 / 60 = 12,75`; `real_min_session_day = 12,75 / 0,85 = 15,0`; phiên 60 phút → `days_per_session = 4,0`; `real_min_to_day_4 = 3 · 15,0 = 45,0` (38,25 nếu u = 0).

### F13. Truy vấn "lần xuất hiện kế tiếp" (Rule 21)

**F13a — thứ kế tiếp.** The next_weekday formula is defined as:

`k = (target_weekday − (day_index mod 7) + 7) mod 7`; nếu `k = 0` và không `include_today` thì `k = 7`
`next_weekday_day_index = day_index + k`

**F13b — ngày N của mùa S kế tiếp.** The next_season_day formula is defined as:

`year_len = SEASONS_PER_YEAR · days_per_season`
`target_in_year = S · days_per_season + (N − 1)`
`k = (target_in_year − (day_index mod year_len) + year_len) mod year_len`; nếu `k = 0` và không `include_today` thì `k = year_len`
`next_season_day_index = day_index + k`

**F13c — giờ đồng hồ c kế tiếp** (hẹn kiểu "ngày mai 10:00"). The next_clock_time formula is defined as:

`cand = day_index · 1440 + offset(c)`
`next_clock_game_minute = (cand > game_minute) ? cand : cand + 1440`

Mốc `game_minute` của một ngày tìm được là `target_day_index · 1440` (06:00 ngày đó).

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `target_weekday` | int | 0 – 6 | calculated (tham số truy vấn) | |
| `S` | int | 0 – 3 | calculated (tham số) | Mùa đích |
| `N` | int | 1 – D | calculated (tham số) | Ngày trong mùa đích |
| `c` | int (phút đồng hồ) | 0 – 1439 | calculated (tham số) | Giờ đích |
| `include_today` | bool | | calculated (tham số) | Hôm nay khớp thì trả hôm nay (`k = 0`) hay lần sau |
| `day_index`, `game_minute` | long | | calculated | |
| `days_per_season` | int | {7, 14, 21, 28} | data file | |
| `k` | int | F13a: 0–7; F13b: 0–`year_len` | calculated | Số ngày tới lần kế tiếp |

**Output Range:** F13a: `k ≤ 7`. F13b: `k ≤ year_len` (≤ 112). F13c: trong vòng 1440 phút. Mọi truy vấn hợp lệ có lời giải trong ≤ 1 năm — **bảo đảm toán học của Rule 21** "không có gì bị lỡ vĩnh viễn". Thoái hóa: F13b khi tắt mùa → trả **Unsupported** (hệ gọi fallback sang ngày tuyệt đối hoặc thứ); `N > D` hoặc `S ∉ 0..3` → lỗi tham số (dữ liệu author theo `D` cũ sẽ vỡ khi đổi `D` — Rule 22); F13c với `c` trong 00:00–05:59 → kết quả thuộc **đêm của ngày hiện tại**, không phải "ngày mai" theo lịch — hệ hẹn phải biết điều này.

**Example:** Tại `game_minute = 1500` (Ngày 2, `day_index = 1`, Spring 2, weekday 1, `D = 14`, `year_len = 56`):
- F13a, `target_weekday = 0`: `k = (0 − 1 + 7) mod 7 = 6` → `day_index` 7 = **Ngày 8** (`game_minute` 10080).
- F13b, Summer 1 (S = 1, N = 1): `target_in_year = 14`, `k = (14 − 1 + 56) mod 56 = 13` → **Ngày 15**.
- F13b, Spring 1, không `include_today`: `k = 55` → `day_index` 56 = **Năm 2, Spring 1**.
- F13c, `c = 600` (10:00): `offset = 240`, `cand = 1680 > 1500` → **10:00 hôm nay**. `c = 360` (06:00): `cand = 1440 ≤ 1500` → 2880 = 06:00 Ngày 3.

### Ràng buộc validator (dữ liệu tuning)

`s ∈ [0,4; 1,5]` · `max_real_delta_per_frame ∈ [0,05; 0,25]` và `≤ s` · `days_per_season ∈ {7, 14, 21, 28}` · offset các mốc khoảng giờ bắt đầu từ 0, tăng nghiêm ngặt, mỗi khoảng không phải Night ≥ `MIN_SEGMENT_MINUTES` (120, hằng), mốc Night ∈ [20:00, 22:00] · offset của `earliest_sleep_clock` ∈ [360, 900] · `defeat_time_cost_minutes ∈ [0, 30]` · `save_retry_count ∈ [0, 3]` · `token_watchdog_seconds ∈ [120, 1800]` · `phase_timeout_seconds ∈ [2, 30]`. Mọi test của F1 dùng `real_delta` cố định (determinism, coding standards).

**Dải cứng là nguồn sự thật.** Cột "Dải an toàn" ở Tuning Knobs là **dải cứng của validator** (chặn build); test và fixture không được dùng giá trị ngoài dải này. Hai mức mềm là **cảnh báo**, không chặn: `s` ngoài dải thiết kế 0,75–0,9, và `s > 1,0` (trần H1, kèm tên trần trong thông báo).

## Edge Cases

*Nguồn: systems-designer, 2026-10-25. Quyết định của người dùng U1–U4 cùng ngày; U2 được thay bằng luật `save_sequence` và thêm D-2, D-3 (review 2026-10-26). Mục đánh dấu *[async]* chỉ xảy ra khi pipeline có phase bất đồng bộ — không áp dụng ở Fun-Gate (Rule 12).*

### Clock advance & tokens

- **If một khung hình có `real_delta` vượt `max_real_delta_per_frame`** (giật, breakpoint, máy thức dậy từ sleep): chỉ tính `max_real_delta_per_frame` (mặc định 0,1 s); phần vượt bị bỏ vĩnh viễn (F1, Rule 4). Một lần giật 2 s ở s = 0,75 làm mất ≈ 2,5 phút game — nghiêng về phía người chơi.
- **If `real_delta` âm hoặc NaN** (lỗi đồng hồ engine): `real_eff = 0` cho khung hình đó, ghi log. Đồng hồ không lùi (Rule 2), `acc` không bao giờ nhận NaN (F1, ghi chú triển khai). `real_delta = +∞` → kẹp như một hitch.
- **If máy chạy dưới 10 FPS liên tục**: đồng hồ chậm hơn spec theo tỷ lệ `kẹp / Δ`, không bù lại. Máy yếu có thêm thời gian trong ngày, không bị phạt.
- **If một token được nhả hai lần, hoặc bởi chủ không giữ nó**: no-op, `token_count` không bao giờ âm (Rule 6); cảnh báo trong log ở bản dev.
- **If chủ sở hữu token bị hủy** (unload scene, object destroyed): với chủ là `UnityEngine.Object`, token được nhả **ở bước quét liveness đầu tick kế tiếp của Time**, trước khi tính F1 (Rule 6); nếu là token cuối → T6 sang Running ngay trong tick đó. Với chủ là đối tượng C# thuần, Time không phát hiện được — token đó là token rò rỉ (mục dưới) trừ khi được giữ trong scope. **Exception không hủy chủ**: token giữ trong scope `using`/`finally` được nhả khi scope thoát; token giữ ngoài scope mà exception làm quên nhả là token rò rỉ (mục dưới).
- **If một token bị giữ quá `token_watchdog_seconds` giây thực** (mặc định 600 s): bản dev ghi cảnh báo kèm chủ và lý do; bản phát hành chỉ ghi log cục bộ (Telemetry). **Không bao giờ tự cưỡng chế nhả** — để game ở menu pause hàng giờ là hợp lệ, và cưỡng chế nhả sẽ cho đồng hồ chạy sau lưng một menu đang mở.
- **If token rò rỉ thật** (chủ còn sống nhưng quên nhả): đồng hồ đứng. Nếu giờ đã ở trong cửa sổ ngủ (≥ 18:00), người chơi **vẫn ngủ được** (T8 chấp nhận khi n ≥ 1) — ngày vẫn sang. **Lối thoát này chỉ dùng được một lần:** ngủ không xóa token (Time không biết token nào là "cũ"), nên sau T9 đồng hồ lại đứng ở 06:00 của ngày mới, dưới `earliest_sleep_clock`. Nếu rò rỉ **trước 18:00** (hoặc sau lần ngủ đó), ngủ bị từ chối; lối thoát là **nạp save hoặc về menu chính** (T10, T11), cả hai xóa sạch token (epoch mới, Rule 6). Watchdog vẫn không cưỡng chế nhả — log của nó là cách phát hiện lỗi này trong playtest. *Đã cân nhắc và bác:* xóa token "cũ" khi lên giường — sẽ cho đồng hồ chạy sau lưng một menu hay hội thoại còn mở.
- **If ứng dụng mất focus** (alt-tab, OS pause): Time tự giữ token focus (Rule 8), lấy lại focus thì nhả; khung hình đầu sau resume bị kẹp như mọi khung hình. Trong Editor, toggle dev có thể tắt token này (Rule 8); bản phát hành luôn bật.
- **If người chơi mở menu hay hội thoại liên tục để "đóng băng" đêm** (chiến lược thoái hóa): chấp nhận. Token dừng đồng hồ nhưng cũng chặn mọi gameplay (Rule 7), nên không cho lợi thế nào ngoài thời gian suy nghĩ — đúng tinh thần *NOT a time-pressure game*.
- **If người chơi chuyển vùng qua lại để kéo dài thời gian dừng của Region Loading**: chấp nhận; chỉ dừng trong lúc nạp, không có lợi thế.

### Sleep & day transition

- **If người chơi xác nhận ngủ đúng 18:00** (`minute_of_day = 720`): được ngủ (ranh giới bao gồm, F6), nhảy 720 phút. Lúc 17:59 bị từ chối — giường nói "chưa buồn ngủ", không phải lỗi.
- **If người chơi ngủ lúc 05:59**: được ngủ, nhảy 1 phút, pipeline chạy đầy đủ, save vào slot ngủ (Rule 13) — gần như rollover khi thức, chỉ khác slot.
- **If người chơi săn trọn đêm rồi ngủ lúc 05:59**: chấp nhận — **đây là ý định thiết kế**. Đêm là cơ hội (Rule 10); cái giá là quái mạnh hơn, không phải lịch.
- **If ngủ khi bộ đệm `acc` khác 0**: stage 2 đặt `game_minute = sleep_target` và `acc = 0`; phần lẻ (< 1 phút) bị bỏ.
- **If menu xác nhận ngủ đang mở đúng lúc sắp chạm 06:00**: menu giữ token nên đồng hồ đứng (vd. ở 05:59), không thể chạm 06:00. Xác nhận → `sleep_target` = 06:00 sắp tới (nhảy 1 phút); hủy → đồng hồ chạy tiếp và rollover tại chỗ một phút sau.
- **If lệnh ngủ đến khi đang giữ token hội thoại/menu khác** (không phải menu xác nhận ngủ): Time chấp nhận (T8 từ Suspended(n)); pipeline chạy, token cũ vẫn đếm, T9 về Suspended(n). Chặn ngủ giữa hội thoại là việc của Interaction (giường không tương tác được khi có hội thoại), không phải của Time — để giữ nguyên lối thoát cho token rò rỉ.
- **If lệnh ngủ đến khi đã có cờ chờ hoặc đang ở DayTransition**: trả `Busy` (cấm tái nhập, Rule 12), không xếp hàng.
- **If lệnh ngủ đến khi đang nạp vùng / catch-up** (token Region Loading hoặc restore handle đang giữ): trả `Busy` — pipeline không được chạy trên trạng thái catch-up dở (T8).
- **If tick chạm 06:00 và lệnh ngủ đến trong cùng khung hình**: cờ chờ đầu tiên thắng (Rollover nếu tick chạy trước); lệnh ngủ nhận `Busy`; chỉ một pipeline chạy. Hai đường cho cùng lợi ích lịch (Rule 14), chỉ khác slot.
- **If có hệ xin token trong lúc DayTransition** *[async]*: token được đếm ngay; đồng hồ vốn đã đứng nên không đổi gì; T9 về Suspended(n) nếu n ≥ 1. Phase **không được chờ** token đó nhả (deadlock).
- **If một handler của `DayStarted` / `SeasonChanged` / `YearChanged` thay đổi trạng thái được lưu**: vi phạm Rule 12 (sự kiện chỉ để thông báo) — thay đổi đó sẽ không có trong save của ngày này. Bản dev ghi error khi một saveable đổi "dirty" trong stage 8.
- **If một phase throw exception**: ghi log (stage, khóa, lỗi), bỏ qua phase, chạy tiếp, vẫn thử save (Rule 12). Ngày vẫn sang; trạng thái của phase đó tự sửa ở lần đọc sau vì tính lười từ timestamp (Rule 17).
- **If một phase không bao giờ hoàn thành** *[async]* (await treo): sau `phase_timeout_seconds` giây thực (mặc định 10 s), phase bị coi là lỗi và xử lý như trên. Không có timeout thì một phase treo khóa người chơi vĩnh viễn trong DayTransition. Ở Fun-Gate pipeline đồng bộ nên không có trường hợp này.
- **If một phase đăng ký khi pipeline đang chạy**: hiệu lực từ lần chuyển ngày sau (Rule 12).
- **If một phase đăng ký trùng khóa thứ tự trong cùng stage**: bị từ chối, trả lỗi, ghi log; phase cũ giữ nguyên.
- **If chủ của một phase bị hủy trong lúc pipeline chạy, trước lượt phase đó**: phase bị bỏ qua (log mức info), pipeline chạy tiếp.
- **If đồng hồ chạm 06:00 khi người chơi đang giao chiến ngoài arena**: rollover tại chỗ (Rule 14); token chuyển ngày chỉ dừng đồng hồ, không dừng combat (Rule 7). Ở Fun-Gate pipeline đồng bộ chạy trong một khung hình, nên giữa trận người chơi thấy **một khung hình dài** (có thể giật) — ngân sách của khung hình này, gồm I/O của stage 7, chốt ở OQ-5 (AC-PF4). Autosave vẫn chạy, nhưng **khi nạp, autosave không bao giờ đặt người chơi vào trận đang diễn ra** (Restore/save › Continue). *Cho GDD khác:* Save/Load quyết autosave chứa gì (vị trí an toàn, HP); phase stage 3–6 không đụng thực thể của vùng active khi rollover (Rule 12) — làm tươi vùng này hoãn tới lần nạp kế tiếp, nên không có quái reset hay respawn quanh trận đang đánh.
- **If người chơi bị đánh gục trong lúc rollover tại chỗ** *[async]*: token Defeat & Retry được đếm, chuỗi thua chạy song song; F7 dùng `next_dawn` của ngày *mới* nên không xung đột; pipeline không bị hủy. Ở Fun-Gate (đồng bộ) chuỗi thua chỉ có thể bắt đầu trước khi đặt cờ hoặc sau T9.
- **If người chơi bị đánh gục ở khung hình có cờ chờ** (trước khi pipeline bắt đầu): chuỗi thua giữ token; pipeline vẫn chạy ở bước cập nhật kế tiếp (token không chặn pipeline đã được chờ). `AdvanceBy` gọi khi cờ còn đặt trả `Busy` (Rule 2) — Defeat & Retry gọi nó lúc người chơi tỉnh dậy, tức sau T9, nên dùng `next_dawn` của ngày mới.
- **If người chơi không bao giờ ngủ** (chỉ autosave 06:00 và lưu ở điểm an toàn): hợp lệ, không phạt (Rule 10, 14). "Continue" nạp slot ghi sau cùng theo số thứ tự lưu (Restore/save › Continue).

### Restore/save

- **If save thất bại ở stage 7**: thử lại tại chỗ `save_retry_count` lần (mặc định 1). Vẫn thất bại → ngày trong bộ nhớ **vẫn đã sang**, thông báo không chặn, pipeline chạy tiếp stage 8, save thử lại ở cơ hội kế tiếp (Rule 15). Menu thoát game cảnh báo nếu lần lưu gần nhất thất bại.
- **If mất điện hoặc crash** (kể cả giữa lúc ghi save): hợp đồng nguyên tử của Save/Load giữ nguyên bản lưu trước; người chơi tiếp tục từ lần lưu gần nhất — slot tiếp tục ở điểm an toàn gần nhất, hoặc save của lần chuyển ngày gần nhất. **Mất tối đa một chuyến đi** (Rule 15a, D-3). Đây là mất dữ liệu do crash, không phải hình phạt của lịch.
- **If crash giữa DayTransition** (sau stage 2, trước khi stage 7 ghi xong): khi nạp lại, người chơi ở trạng thái trước lần chuyển ngày đó (đêm ngày N, ở điểm an toàn gần nhất hoặc nơi ngủ). Pipeline sẽ chạy lại khi họ ngủ hoặc tới 06:00 — không có phase nào chạy hai lần cho cùng một ngày vì ngày N+1 chưa từng được lưu.
- **If lệnh thoát có chủ đích đến khi có cờ chờ hoặc đang DayTransition**: thoát chờ T9 rồi mới lưu slot tiếp tục (Rule 15a) — không bao giờ lưu trạng thái giữa pipeline.
- **If ứng dụng mất focus giữa pipeline** *[async]*: token focus được đếm, pipeline **chạy tiếp** (không phụ thuộc đồng hồ tiến); T9 về Suspended (focus) cho tới khi lấy lại focus.
- **If nạp save thất bại giữa chừng** (file hỏng, DTO không đọc được, `game_minute` âm hoặc > `MAX_GAME_MINUTE`): T3b — Time về Uninitialized, tập token rỗng, người chơi về menu chính với thông báo lỗi — không giữ trạng thái nửa vời, không kẹp giá trị. Nếu thất bại trước khi Restoring bắt đầu ghi (file không mở được), phiên hiện tại giữ nguyên.
- **If nạp save từ menu trong lúc rollover tại chỗ** *[async]*: pipeline bị hủy (không chạy tiếp, không save); T10 sang Restoring xóa mọi token; trạng thái nạp là nguồn sự thật mới.
- **If khôi phục làm mất bộ đệm `acc`** (không lưu, Rule 4): `acc = 0`; mất tối đa < 1 phút game — chấp nhận.
- **If "Continue" phải chọn giữa slot ngủ, slot autosave và slot tiếp tục**: **nạp slot được ghi sau cùng**, xác định bằng **số thứ tự lưu đơn điệu** (`save_sequence`, Save/Load tăng 1 mỗi lần ghi thành công vào bất kỳ slot nào) — không dùng `game_minute` (hai slot có thể cùng giờ, và lưu ở điểm an toàn sau lúc ngủ có thể mang `game_minute` lớn hơn hay bằng) và không dùng giờ hệ điều hành (có thể lùi, Rule 4). *(Thay U2 "`game_minute` lớn nhất", review 2026-10-26.)* **Nạp autosave hay slot tiếp tục không bao giờ đặt người chơi vào một trận đang diễn ra** (CD-GDD-ALIGN C3) — cơ chế (khôi phục về vị trí an toàn gần nhất hoặc cổng vùng) do GDD Save/Load quyết.
- **If người chơi đổi qua lại hai slot để "reroll" ngày** (chiến lược thoái hóa): vô hiệu nếu mọi random theo ngày được seed bằng **`hash(world_seed, day_index, system_id, entity_id)`** — nạp lại cùng ngày cho cùng kết quả, và hai hệ hay hai thực thể trong cùng ngày không dùng chung một chuỗi random. *Cho Enemy AI, Shops, Quest:* seed theo công thức này, không theo giờ thực hay thứ tự gọi.
  - **`world_seed` thuộc Flags, Conditions & World State**: đặt một lần khi tạo game mới, lưu trong save của World State, không bao giờ đổi. Time **không** lưu nó — trạng thái duy nhất của Time vẫn là `game_minute` (Rule 1); Time chỉ cung cấp `day_index`.
  - **Hợp đồng hash:** thuật toán **cố định, xác định qua mọi process và nền tảng** (vd. FNV-1a hoặc xxHash trên các số nguyên little-endian) — **không** dùng `object.GetHashCode()`, `string.GetHashCode()` hay `System.HashCode` (ngẫu nhiên theo process). `system_id` lấy từ một bảng ID cố định (Game Data). Ai cài helper dùng chung: OQ-7.

### Defeat & boss arena

- **If `defeat_time_cost_minutes` > 0 và cộng vào sẽ vượt 06:00** (vd. thua 05:45, cost 30): **kẹp ở `next_dawn − 1`** (05:59), không chuyển ngày (F7; U1). Người chơi tỉnh dậy lúc 05:59; một phút sau rollover tại chỗ chạy bình thường (Rule 14). Lý do: giữ đúng Rule 18; không chạy pipeline và autosave lồng trong chuỗi thua đang giữ token; với người chơi gần như không khác (lệch 1 phút).
- **If người chơi cố ý chết để về nhà** (cost = 0): không còn miễn phí — **túi đồ chưa cất rơi tại chỗ thua** (Rule 18, D-2), nên chết để về nhà nghĩa là phải quay lại lấy túi, hoặc bỏ nó. Với túi gần rỗng (vừa cất đồ xong), chết vẫn là một lối về nhanh — chấp nhận: không có gì để mang về thì cũng không có gì để lợi. Nếu playtest vẫn cho thấy nó thay thế việc đi về: tăng chi phí phi thời gian trước (Defeat & Retry: quái hồi, chuỗi thua dài hơn) — **thời gian là biện pháp cuối**, chỉ trong dải 0–30 phút. Sửa chi phí, không sửa lịch (CD-GDD-ALIGN C2). Điểm tỉnh dậy vẫn là **nhà** — khớp "một mái nhà để quay về". Telemetry ghi mỗi lần thua kèm khoảng cách tới nhà, số món trong túi, và túi có được nhặt lại không.
- **If túi đồ rơi nằm trong vùng chưa nạp khi ngày sang hoặc mùa đổi**: túi vẫn còn (Rule 18 — không hết hạn theo thời gian); không phase nào ở stage 3–6 được xóa túi. *Cho Defeat & Retry, World State:* túi là trạng thái được lưu, không phải quái hay đồ rơi được respawn.
- **If đồng hồ chạm 06:00 khi đang giữ token boss arena**: **không thể xảy ra** — token đóng băng đồng hồ và T7 chỉ kích hoạt từ Running. Vào arena lúc 05:59 thì ra vẫn là 05:59; rollover tại chỗ chạy một phút sau khi rời.
- **If người chơi thua boss hoặc retry nhiều lần**: không tốn phút game nào (Rule 19). **F7 không áp dụng cho đường thua boss**, kể cả khi người chơi bỏ cuộc và được đưa về nhà — bật cost ở đường này sẽ trái "thua boss không tốn phút game nào".
- **If người chơi đứng trong arena để "trú" ngoài thời gian**: chấp nhận; arena không có tài nguyên lịch.
- **If retry từ checkpoint arena**: **không đi qua Restoring**, Time không đổi trạng thái (Rule 19); token arena giữ liên tục; checkpoint không ghi đồng hồ. Bản dev kiểm `game_minute` bằng giá trị lúc vào arena, khác thì ghi error. Không sự kiện, không catch-up.
- **If người chơi thoát game giữa trận boss**: slot tiếp tục lưu `game_minute` (bằng giờ lúc vào arena) và vị trí an toàn ngoài arena (C3); khi nạp, người chơi đứng trước cổng arena.
- **If lệnh ngủ đến khi đang giữ token arena** (chỉ qua debug — arena không có giường): Time chấp nhận theo T8; Interaction và Boss Encounter bảo đảm điều này không xảy ra trong gameplay. Không thêm luật riêng ở Time.

### Calendar/seasons

- **If HUD hiển thị giờ 00:00–05:59**: ngày, thứ và mùa là của **ngày N** (ngày trước) vì `day_index` lấy từ F2 (Rule 9) — vd. `game_minute = 1230` hiện "Ngày 1, 02:30". Lý do: "hẹn ngày mai" luôn nghĩa là "sau ranh giới 06:00 kế tiếp"; lật ngày lúc nửa đêm làm hẹn mơ hồ. Cách trình bày cụ thể ("Đêm Ngày 1") thuộc GDD HUD; HUD không hiện đếm ngược tới 06:00 (Rule 10).
- **If game mới bắt đầu** (T1): không chạy pipeline, **không phát `DayStarted`**; hệ phụ thuộc khởi tạo bằng truy vấn trạng thái hiện tại. Giữ bất biến "mỗi lần pipeline chạy, `DayStarted` phát đúng 1 lần". Telemetry ghi "phiên bắt đầu" từ sự kiện của chính nó.
- **If một DayTransition cũng là ngày đầu mùa hoặc đầu năm**: stage 8 phát theo thứ tự cố định **`DayStarted` → `SeasonChanged`** (nếu `day_of_season == 1`) **→ `YearChanged`** (nếu cũng là Spring 1). Mọi giá trị dẫn xuất đã đúng trước sự kiện đầu tiên.
- **If khôi phục save rơi vào ngày đầu mùa**: không phát `SeasonChanged` (Rule 16).
- **If ngủ vượt nhiều ranh giới khoảng giờ** (vd. ngủ 19:00 vượt Night và Morning): stage 2 phát **một** sự kiện gộp `Evening → Morning` với `boundary_minute = sleep_target` (Rule 11), trước `DayStarted`; không phát `Evening → Night` — không hệ nào bật chế độ đêm trong lúc ngủ. Rollover tại chỗ chỉ vượt đúng một ranh giới (`Night → Morning`), vì các ranh giới trong ngày đã được tick phát.
- **If `AdvanceBy` vượt một ranh giới** (vd. thua 17:50 với cost 30 → 18:20): phát đúng một sự kiện `Afternoon → Evening` với `boundary_minute = 720` (Rule 11). Không bao giờ vượt quá một ranh giới (cost ≤ 30 < 120) và không bao giờ vượt bình minh (F7).
- **If cây hay nội dung theo mùa đang dở khi đổi mùa**: không bị giết hay xóa (Rule 20); hệ sở hữu chỉ quyết cái gì *mới* được trồng/xuất hiện.
- **If ngày cuối Winter sang ngày kế**: thành Spring 1, Year +1 (F8–F10); nội dung theo mùa quay lại (Rule 21).
- **If truy vấn "lần kế tiếp của mùa S ngày N" khi `seasons_enabled = false`**: trả `Unsupported` (F13b); hệ gọi fallback sang ngày tuyệt đối hoặc thứ, **không được hiện "đã lỡ"**.
- **If truy vấn có `N > days_per_season` hoặc `S ∉ 0..3`**: trả lỗi tham số, ghi log; hệ gọi coi như không có lịch hẹn. Thường do dữ liệu author theo `D` cũ (Rule 22).
- **If F13c được hỏi với giờ 00:00–05:59**: kết quả thuộc đêm của ngày hiện tại, không phải "ngày mai" theo lịch; hệ hẹn giờ phải hiển thị đúng.
- **If `seasons_enabled` lật từ false sang true trên save có sẵn** (save Fun-Gate mở bằng build VS): `game_minute` giữ nguyên; **mùa tính lại ngay từ `day_index`** (U4) — vd. Ngày 20 thành Summer 6 ở D = 14; không phát `SeasonChanged` cho các mùa đã "qua"; cây đã trồng vẫn sống. Chấp nhận vì save Fun-Gate/MVP là save test nội bộ, không mang sang VS.

### Offscreen catch-up

- **If delta catch-up âm** (timestamp của hệ phụ thuộc > `game_minute` đã khôi phục): delta = 0, ghi log kèm hệ, timestamp và đồng hồ (Rule 17). Nguyên nhân hợp lệ duy nhất là debug lùi đồng hồ; trong bản phát hành, log này là dấu hiệu lỗi thứ tự restore.
- **If một vùng không được ghé trong 100 ngày** (delta ≈ 144.000 phút): catch-up tốn đúng như delta 1 ngày (công thức đóng, Rule 17). Mọi trạng thái **bão hòa ở trần**: cây ở giai đoạn cuối (không héo), xử lý xong, respawn đầy, cửa hàng restock **một lần** — không cộng dồn 100 lần. *Cho Farming, Crafting, World State, Shops:* công thức catch-up phải có trần.
- **If catch-up chưa xong khi khung hình đầu tiên của vùng sắp hiển thị**: token Region Loading vẫn giữ, màn hình nạp kéo dài (Rule 17); không hiển thị nội dung chưa catch-up.
- **If một hệ phụ thuộc chưa từng có timestamp "cập nhật lần cuối"** (nội dung mới thêm bằng bản cập nhật): lấy `game_minute` hiện tại làm mốc (delta = 0); nội dung mới bắt đầu từ trạng thái ban đầu, không được "bù" thời gian đã qua.

### Data/tuning

- **If nạp save tạo với `days_per_season` khác hiện tại** (version lịch không khớp, Rule 22): **sau phát hành, Save/Load migration quy đổi `game_minute` để giữ nguyên (năm, mùa, ngày trong mùa, phút trong ngày)** (U3); nếu `day_of_season` cũ > `D` mới, kẹp về ngày cuối của mùa đó. **Mọi timestamp `last_updated` của hệ phụ thuộc** (Rule 17) và mọi `game_minute` khác nằm trong save (hẹn NPC, túi đồ rơi…) phải được quy đổi bằng **cùng một phép** — nếu không, delta catch-up sau migration sẽ sai (âm hoặc nhảy hàng chục ngày). Trước phát hành, chấp nhận ngày dịch và ghi log. Ghi cho GDD Save/Load (OQ-6).
- **If nạp save tạo với ranh giới khoảng giờ hoặc `earliest_sleep_clock` khác**: không cần quy đổi; `minute_of_day` không đổi, chỉ nhãn khoảng giờ và `IsNight` dịch theo dữ liệu mới; log mức info.
- **If dữ liệu lịch vi phạm ràng buộc validator** (Formulas › Ràng buộc validator): validator **chặn build** trong CI. Nếu dữ liệu sai vẫn lọt tới runtime (file cài đặt hỏng): ghi error, dùng giá trị mặc định **cho riêng trường sai** (các trường hợp lệ giữ nguyên), không crash.
- **If đổi `real_seconds_per_game_minute` lúc đang chạy** (live tuning, bản dev): hiệu lực từ khung hình kế; `acc` giữ nguyên (đơn vị là phút game); đồng hồ không nhảy.
- **If đổi `earliest_sleep_clock` lúc đang chạy**: quyền ngủ tính lại ở lần hỏi kế tiếp.
- **If debug đặt đồng hồ lùi** (chỉ bản dev, bị loại khỏi bản phát hành): đặt theo kiểu khôi phục im lặng — không sự kiện, không pipeline (Rule 2, 16). Hệ quả chấp nhận: delta âm → 0 + log; điều kiện `day ≥ N` có thể thành false; save ghi sau đó mang mốc lùi. Test không được dùng lệnh này để dựng trạng thái — dùng fixture.
- **If debug đặt đồng hồ tiến** (vd. nhảy 10 ngày): đặt im lặng, **không** chạy pipeline 10 lần; trạng thái lười tự đúng theo catch-up. Muốn kiểm pipeline thì dùng lệnh debug riêng "skip day" — chạy pipeline thật đúng một lần.
- **If `game_minute` tiến gần giới hạn kiểu số**: không đạt được trong thực tế (F2).

### Chiến lược thoái hóa (tóm tắt)

| Chiến lược | Chấp nhận? | Lý do |
|---|---|---|
| Cố ý chết để về nhà (cost 0) | Có, nhưng có giá | Túi đồ chưa cất rơi tại chỗ thua, nhặt lại được (Rule 18, D-2); theo dõi bằng Telemetry; nếu cần chỉnh: chi phí phi thời gian trước, `defeat_time_cost_minutes` (≤ 30) sau cùng |
| Đóng băng đêm bằng menu/hội thoại | Có | Token cũng chặn gameplay — không có lợi thế |
| Săn trọn đêm rồi ngủ 05:59 | Có — là ý định | Đêm là cơ hội |
| Ngủ sớm để tua ngày | Bị chặn | `earliest_sleep_clock` = 18:00 (D3) |
| Trú trong arena | Có | Không có lợi thế |
| Không bao giờ ngủ | Có | Autosave 06:00, không phạt |
| Reroll ngày bằng hai slot lưu | Vô hiệu | Random theo ngày seed bằng `hash(world_seed, day_index, system_id, entity_id)` |
| Crash / kill process để "hoàn tác" một chuyến xấu | Có, không lợi | Mất tối đa một chuyến (D-3); slot tiếp tục ở điểm an toàn gần nhất không chứa gì tốt hơn |

## Dependencies

**Upstream (Time phụ thuộc vào):**

| Hệ | Cứng/mềm | Giao diện | Trạng thái GDD |
|---|---|---|---|
| Game Data & Tuning Config | **Cứng** — không có hằng lịch thì không chạy | Time đọc các knob dưới (có version, Rule 22); Time sở hữu schema lịch, Game Data chỉ là registry | Chưa có (#4) — cần ADR ID ổn định trước (TD-B4) |

Time không phụ thuộc hệ nào khác. Save/Load và Flags là **đăng ký vào hợp đồng** (Time đăng ký saveable vào Save/Load; Time đăng ký condition provider vào Flags; Save/Load đăng ký stage 7 vào Time) — không phải phụ thuộc runtime một chiều; cần một assembly hợp đồng trung lập để asmdef không vòng (→ ADR, Interactions).

**Downstream (phụ thuộc vào Time)** — mọi hệ dưới đây chưa có GDD; mỗi GDD khi viết **phải liệt kê Game Time & Calendar trong Dependencies** của mình (luật hai chiều):

| Hệ | Cứng/mềm | Dùng gì | Tier |
|---|---|---|---|
| Save/Load | Cứng | `game_minute` saveable; stage 7 của pipeline (thử lại `save_retry_count`); slot tiếp tục khi thoát có chủ đích và ở điểm an toàn (Rule 15a, D-3); `save_sequence` cho Continue; lời gọi restore nhận restore handle; nạp không bao giờ vào giữa trận; migration quy đổi mọi timestamp | Fun-Gate |
| Flags, Conditions & World State | Mềm | predicate ngày/mùa/khoảng giờ; timestamp cho trạng thái vùng; **sở hữu và lưu `world_seed`** (Edge Cases › reroll) | Fun-Gate |
| Menu & Navigation Framework | Cứng | token | Fun-Gate |
| Interaction & Context Prompts | Cứng | giường gọi lệnh ngủ; đọc `sleep_allowed` (F6); không gửi lệnh ngủ khi có hội thoại (AC-C12) | Fun-Gate |
| Dialogue & Discovery Hints | Cứng | token; truy vấn "lần kế tiếp" | Fun-Gate |
| Region Loading | Cứng | token; restore handle khi nạp save / game mới; `game_minute` cho catch-up; báo điểm an toàn (chuyển vùng) cho Save/Load — **lần lưu ở điểm an toàn này ghi xong trước khi giữ token `RegionLoad`** (Rule 15a: thoát khi catch-up không lưu) | Fun-Gate |
| Defeat & Retry | Cứng | token; `AdvanceBy(defeat_time_cost_minutes)` (F7); **phải ship túi đồ rơi nhặt lại được ở Fun-Gate** (Rule 18, D-2) — túi không hết hạn theo thời gian; **phải thỏa N3, N4** | Fun-Gate |
| Farming | Cứng | `game_minute` cho growth lười; phase stage 4 | Fun-Gate |
| Crafting & Forge | Cứng | `game_minute` cho processing; phase stage 4 | Fun-Gate |
| Enemy AI, Spawning & Drops | Cứng | `IsNight`, sự kiện khoảng giờ (dùng payload, Rule 11), respawn; phase stage 3; seed `hash(world_seed, day_index, system_id, entity_id)`; **phải thỏa N1, N2** | Fun-Gate |
| NPC Affinity | Mềm | ngày; phase stage 5 | Fun-Gate |
| Telemetry | Cứng | `game_minute`, `DayStarted`, ngủ/autosave | Fun-Gate |
| HUD | Cứng | giờ, ngày, khoảng giờ, mùa | Fun-Gate |
| Audio | Mềm | khoảng giờ, `segment_progress` | Fun-Gate |
| Boss Encounter | Cứng | token arena | MVP-Complete |
| Quest & Hidden Quest | Mềm | ngày, truy vấn "lần kế tiếp"; phase stage 6 | MVP-Complete |
| NPC Schedules | Cứng | khoảng giờ, thứ, mùa; phase | Vertical Slice |
| Currency & Shops | Cứng | thứ, mùa, restock; phase stage 4; seed `hash(world_seed, day_index, system_id, entity_id)` | Vertical Slice |
| Cutscene & Scripted Sequence | Cứng | token | Vertical Slice |
| Atmosphere & Lighting | Mềm | khoảng giờ, `segment_progress`, mùa | Vertical Slice |
| Tutorial & Onboarding | Mềm | sự kiện ngày (gợi ý ngủ đầu tiên) | Vertical Slice |

**Ràng buộc bắt buộc cho GDD phụ thuộc** (re-review 2026-10-26, creative-director). Nội dung thuộc các GDD đó; Time ghi ràng buộc ở đây vì "đêm là lời mời" (Player Fantasy) và phép đo ở lại qua đêm của Fun-Gate (H.6) chỉ có nghĩa khi chúng đúng. Mỗi ràng buộc là **mục review bắt buộc** khi GDD tương ứng được `/design-review`.

| # | Ràng buộc | GDD chịu trách nhiệm |
|---|---|---|
| N1 | Phần thưởng đêm **khác về loại**, không chỉ về số lượng hay tỉ lệ (vd. nguyên liệu, biến thể quái chỉ có về đêm) — đêm không được là "ban ngày nhưng nhiều hơn", nếu không nó thành nghĩa vụ hiệu suất | Enemy AI, Spawning & Drops |
| N2 | **Không món nào cần cho đường chính chỉ có về đêm** — mọi thứ trên đường tiến trình bắt buộc có nguồn ban ngày | Enemy AI, Spawning & Drops (cùng Crafting, Weapon Tree) |
| N3 | **"Chưa cất" được định nghĩa** sao cho lúc hoàng hôn túi chứa giá trị thật: mọi nguyên liệu nhặt trong chuyến hiện tại; không có cách cất từ xa giữa chuyến ở Fun-Gate | Defeat & Retry |
| N4 | **Chết không bao giờ rẻ hơn đi về nhà**: lấy lại túi đòi quay lại đúng chỗ thua, không có dịch chuyển miễn phí tới túi; Telemetry đo (AC-PT4) | Defeat & Retry |

## Tuning Knobs

*Dẫn xuất từ bảng biến của Formulas (systems-designer). Tất cả nằm trong dữ liệu Game Data & Tuning, có version (Rule 22); validator chặn build khi vi phạm.*

| Knob | Mặc định | Dải an toàn | Ảnh hưởng | Quá cao | Quá thấp |
|---|---|---|---|---|---|
| `real_seconds_per_game_minute` (`s`) | **0,75** | 0,4 – 1,5 (cứng); **thiết kế 0,75 – 0,9**; **trần H1: 1,0** (cảnh báo) | Nhịp của cả game: độ dài một ngày, một chuyến, số ngày mỗi phiên (F12) | > 0,9: chuyến qua đêm vượt 15 phút của concept; > 1,0: ngày 4 rơi sau phút 60 → H1 đo sai | < 0,75: chuyến tới hoàng hôn ngắn hơn 5 phút; 0,4: Noon chỉ 1,2 phút thực, 4–15 ngày/phiên, ngày mất hình dạng |
| `max_real_delta_per_frame` | **0,1 s** | 0,05 – 0,25; **phải ≤ `s`** | Bao nhiêu thời gian một lần giật được tính | > `s`: một khung hình vượt ≥ 2 phút → phá bất biến ranh giới (validator chặn); hitch "rò" nhiều hơn | < 0,05: máy < 20 FPS có đồng hồ chậm rõ |
| `days_per_season` (`D`) | **14** | {7, 14, 21, 28} (bội của 7) | Độ dài mùa/năm; "năm sau" có đến trong lượt chơi không (F12) | 28: năm ≈ 28 giờ, lượt chơi ~20 giờ chưa tới mùa đông — rủi ro "năm quá dài" của Player Fantasy | 7: mùa 1,75 giờ — nội dung mùa lướt qua |
| `seasons_enabled` | **false** (Fun-Gate, MVP-Complete) → true (VS+) | bool | Có mùa/năm hay chỉ đếm ngày | — | — |
| `segment_start_clock[0..4]` | 06:00, 11:00, 14:00, 18:00, 21:00 | `start_0` = 06:00 cố định; tăng nghiêm ngặt; Noon 10:00–12:00, Afternoon 13:00–15:00, Evening 17:00–19:00 (và mỗi khoảng không phải Night ≥ 120 phút) | Hình dạng ngày; không khí và cơ hội của từng phần chuyến đi | Khoảng nào quá dài → các khoảng khác thành vệt mỏng | Khoảng < 120 phút game (≈ 1,5 phút thực) → validator chặn; nếu hoạt động cốt lõi chỉ mở trong khoảng đó thì thành hạn chót mềm (`min_core_activity_window`, Interactions) |
| `segment_start_clock[Night]` | **21:00** | 20:00 – 22:00 | Độ dài đêm (F5): 600 → 480 phút | 22:00: đêm ngắn, ít cơ hội đồ rơi đêm | 20:00: Evening còn đúng 120 phút (tối thiểu, với Evening 18:00), nhịp "về nhà rèn" bị nén |
| `earliest_sleep_clock` | **18:00** | 12:00 – 21:00 (offset [360, 900]) | Chặn "ngủ sớm để tua" (D3) | 21:00: người muốn nghỉ sớm sau buổi rèn phải chờ | 12:00: mở cửa tua nửa ngày — anti-fantasy "ngày bị phí" |
| `defeat_time_cost_minutes` | **0** | 0 – 30 | Trọng lượng thời gian của việc thua (không phải boss) (F7) — **biện pháp cuối**, sau túi đồ rơi (D-2) và các chi phí phi thời gian khác của Defeat & Retry | > 30: thua bắt đầu "ăn" một khoảng giờ → anti-fantasy "ngày bị phí"; validator chặn | 0: thua không tốn giờ; chi phí nằm ở túi đồ rơi (theo dõi bằng Telemetry) |
| `token_watchdog_seconds` *(Stub ở Fun-Gate)* | 600 | 120 – 1800 | Chỉ chẩn đoán: cảnh báo token giữ lâu | Token rò rỉ phát hiện chậm | Log nhiễu với người chơi để menu mở |
| `phase_timeout_seconds` *[async — Defer]* | 10 | 2 – 30 | Bao lâu trước khi một phase treo bị coi là lỗi; chỉ có hiệu lực khi phase bất đồng bộ được phép (Rule 12) | Người chơi kẹt lâu trong chuyển ngày | Phase hợp lệ nhưng chậm (máy yếu, save lớn) bị cắt oan |
| `save_retry_count` | 1 | 0 – 3 | Số lần thử lại save ngay tại stage 7 | Chuyển ngày kéo dài khi đĩa hỏng | 0: một lỗi đĩa thoáng qua là báo lỗi ngay |

**Knob tương tác nhau:**
- `s` ↔ `max_real_delta_per_frame`: ràng buộc `max_real_delta_per_frame ≤ s` luôn thỏa trong dải cứng (kẹp ≤ 0,25 < 0,4 ≤ s); validator vẫn kiểm để chặn nếu sau này một trong hai dải bị nới.
- `s` ↔ concept › Short-Term: `s` quyết một chuyến dài bao nhiêu phút thực (F12, bảng độ dài chuyến). Đổi `s` ra ngoài 0,75–0,9 là đổi lời hứa 5–15 phút của concept.
- `s` ↔ `days_per_season`: cùng quyết "năm sau" dài bao nhiêu giờ thực (F12). Tăng `s` 1,5× tương đương tăng `D` 1,5× về giờ thực mỗi năm.
- `segment_start_clock[Evening]` ↔ `earliest_sleep_clock`: mặc định trùng nhau (18:00). Đặt `earliest_sleep_clock` trước Evening làm "ngủ" lấn vào buổi chiều săn.
- `segment_start_clock[Night]` ↔ Enemy AI / Drops: độ dài đêm nhân với hệ số đồ rơi đêm (GDD Enemy AI) quyết giá trị của "đêm là lời mời".

**Hằng (không phải knob):** `MINUTES_PER_DAY = 1440`, `DAY_START_CLOCK = 360` (06:00), `SEASONS_PER_YEAR = 4`, `DAYS_PER_WEEK = 7`, `MIN_SEGMENT_MINUTES = 120` (Rule 9; trước là knob 60–180, bỏ ở re-review 2026-10-26 vì validator không đọc nó), `MAX_GAME_MINUTE = 2.147.480.767` (Rule 16). Đổi chúng là đổi luật (Rule 9, 16, 20), không phải tuning.

## Visual/Audio Requirements

*Category `Core` — mục này không bắt buộc; ghi ngắn ở mức hợp đồng. `art-director` / `audio-director` không được tham vấn — chi tiết thuộc GDD Atmosphere & Lighting và Audio.*

Time không tự vẽ hay phát âm thanh; nó **công bố** `segment`, `segment_progress` (0–1), `IsNight`, mùa và các sự kiện ranh giới. Yêu cầu cho hệ tiêu thụ:
- **Báo giờ bằng thế giới trước, con số sau** (design test phụ ở Player Fantasy): ánh sáng giờ vàng, đèn lồng bật lúc Evening, đom đóm về đêm, nhạc theo khoảng giờ. Con số trên HUD là phụ.
- **Chuyển mượt theo `segment_progress`**, không nhảy cứng ở ranh giới — trừ khi ngủ (nhảy có chủ đích, che bằng fade).
- **Khi đồng hồ dừng** (menu, hội thoại, arena), ánh sáng và nhạc theo giờ **đứng yên ở trạng thái hiện tại**; hiệu ứng khung hình (lửa, lá rung) có thể tiếp tục. Không có âm thanh hay VFX nào báo "thời gian đang trôi đi".
- **Không có tín hiệu cảnh báo hết ngày** — không tiếng chuông, đổi nhạc gấp hay rung màn hình khi gần 06:00 (Rule 10).
- **Khoảnh khắc thức dậy** sau khi ngủ là beat cảm xúc chính của Player Fantasy (nắng sáng, cây đã lớn): cần một chuyển cảnh và một sting âm thanh riêng — GDD Audio/Atmosphere thiết kế.
- **Bình minh khi thức qua đêm:** rollover tại chỗ lúc 06:00 cũng có một tín hiệu bình minh nhẹ (ánh sáng thế giới + một câu nhạc ngắn) cho người không ngủ — **không có báo trước** trước 06:00.
- **Vùng sương "kháng" mùa một phần** (Visual Identity Anchor): Atmosphere đọc mùa từ Time nhưng giảm cường độ trong vùng sương.

> 📌 **Asset Spec** — Visual/Audio requirements are defined. After the art bible is approved, run `/asset-spec system:game-time-calendar` to produce per-asset visual descriptions, dimensions, and generation prompts from this section.

## Game Feel

*Category `Core` — không bắt buộc. Cảm giác của hệ này là cảm giác **nhịp**, không phải phản hồi tay.*

- **Độ dài một ngày** là thông số feel chính: ở `s = 0,75`, một ngày thức ≈ 12–15 phút thực (F12) — đủ cho **việc nhà buổi sáng và một chuyến** (D-1) mà không thấy vội; không đủ, và không nhằm, cho mỗi khoảng giờ một việc. Chuyến tới hoàng hôn ≈ 5–7 phút thực, qua đêm ≈ 12 phút. Noon (2,25 phút thực) là lúc làng **đông nhất**, không phải lúc **duy nhất** để ghé làng — tặng quà, nhận quest, mua bán phải làm được ở nhiều khoảng giờ (CD-GDD-ALIGN C4).
- **Ngủ phải nhanh và êm**: từ lúc xác nhận tới lúc điều khiển lại không dài hơn một fade ngắn (ngân sách thời gian thực của pipeline — AC-PF4, TBD). Một chuyển ngày giật hay chờ lâu phá "dấu chấm êm" của Player Fantasy.
- **Đồng hồ không bao giờ "giật"**: không nhảy giờ khi đóng menu, khi nạp vùng xong, hay sau alt-tab (Rule 4, 6).

## UI Requirements

Time không sở hữu màn hình; nó cung cấp dữ liệu cho HUD (Fun-Gate) và lịch/nhật ký (VS).
- **HUD (Fun-Gate):** giờ (HH:MM), ngày (`day_number`), biểu tượng khoảng giờ; khi bật mùa (VS) thêm mùa và ngày trong mùa. 00:00–05:59 hiển thị **ngày N** (vd. "Đêm Ngày 1"). **Không đếm ngược, không "còn lại X giờ", không cảnh báo hết ngày** (Rule 10, AC-C14).
- **Giường:** prompt "Ngủ" và "Ngủ rồi thoát" (Rule 13) chỉ hiện khi `sleep_allowed`; trước 18:00 hiện "chưa buồn ngủ" — không báo lỗi, không đỏ. (AC-UI1)
- **Thông báo lưu lỗi:** không chặn (toast), và cảnh báo ở menu thoát game (Rule 15, AC-E22).
- **Thoát game:** "Lưu & thoát" luôn có trong menu pause; không có hộp thoại "bạn sẽ mất tiến độ" khi thoát có chủ đích (Rule 15a). (AC-UI2)
- **Túi đồ rơi (D-2):** vị trí túi hiện trên bản đồ/la bàn của vùng — không kèm đếm ngược hay hạn (Rule 10). Trình bày do GDD Defeat & Retry và HUD.
- **Từ ngữ:** tuân danh sách từ cấm ở Player Fantasy › Ngôn ngữ (AC-C15).
- **Lịch / "lần kế tiếp" (VS):** hệ nào hiện lịch hẹn dùng F13 để nói "quay lại vào …", không bao giờ "đã lỡ".

> **📌 UX Flag — Game Time & Calendar**: This system has UI requirements. In Phase 4 (Pre-Production), run `/ux-design` to create a UX spec for each screen or HUD element this system contributes to **before** writing epics. Stories that reference UI should cite `design/ux/[screen].md`, not the GDD directly.

## Cross-References

Chưa có GDD hệ thống nào khác (đây là GDD đầu tiên). Các tham chiếu hiện có:

| Tài liệu | Phần | Dùng cho |
|---|---|---|
| `design/gdd/game-concept.md` | Core Loop › Session-Level, Calendar Contract; Pillar 3; Anti-Pillars (*NOT a time-pressure game*); Flow State Design › Recovery from failure; Target Player Profile | Hợp đồng lịch, Player Fantasy, luật không phạt |
| `design/gdd/game-concept.md` | MVP Definition › Cổng vui (H1: ngày 4) | F12, AC-PT2–PT3 |
| `design/gdd/game-concept.md` | Core Loop › Short-Term (chuyến 5–15 phút), Session-Level (mỗi ngày một chuyến — sửa 2026-10-26 theo D-1) | Rule 9, F12, Tuning Knobs › `s` |
| `design/gdd/reviews/game-time-calendar-review-log.md` | Review 2026-10-26 (NEEDS REVISION), quyết định D-1 – D-4 | Bản sửa 2026-10-26 |
| `design/gdd/systems-index.md` | Dependency Map › Foundation item 1 (TD-B1), item 4 (TD-B2); Creative Director Note; Producer Note | Hợp đồng token, pipeline, timestamp; phạm vi theo tier; stub Defeat |
| `design/art/art-bible.md` | §1 Visual Identity (Lantern & Mist) | Visual/Audio Requirements |

Khi GDD của các hệ ở Dependencies › Downstream được viết, mỗi GDD thêm một dòng ở đây (hệ, mục, dùng gì) và liệt kê Game Time & Calendar trong Dependencies của mình.

## Acceptance Criteria

*Nguồn: qa-lead, 2026-10-25; sửa theo review 2026-10-26. Mỗi tiêu chí có mã `AC-…` và tag loại test + mức cổng theo Testing Standards (Logic/Integration BLOCKING; **UI/Visual BLOCKING** — bằng chứng là ảnh chụp lưu ở `production/qa/evidence/`; Playtest advisory — bằng chứng là **documented playtest** ghi bằng `/playtest-report`; `[Config/Data — smoke check]` advisory theo story, blocking ở `/smoke-check`). Kiểm fantasy AC-PT1 là **chẩn đoán**, không phải cổng (D-4, 2026-10-26). Tag *[async]* = chỉ viết test khi ADR cho phép phase bất đồng bộ (Rule 12); *[Defer: tier]* = viết khi tới tier đó.*

**Quy ước test (áp dụng cho mọi tiêu chí trừ khi ghi khác)**
- **Vị trí:** Logic → `Assets/Tests/EditMode/GameTime/`; Integration → `Assets/Tests/PlayMode/GameTime/`; Playtest → `/playtest-report`.
- **Đặt tên** (NUnit): class `[System]Tests` (`GameClockTests`, `PauseTokenTests`, `DayPipelineTests`, `CalendarTests`, `NextOccurrenceTests`, `CalendarValidatorTests`); method `Scenario_Expected` (vd. `Tick_EightFramesOfOneEighthMinute_AddsOneMinute`).
- **Fixture clock tại M:** dựng đồng hồ ở `game_minute = M` bằng đường khôi phục im lặng (Rule 16) rồi nhả restore handle; `acc = 0` trừ khi AC ghi khác — fixture có `acc ≠ 0` (AC-F01.3/.4/.5/.7, S07, E07, E26, E46) đặt `acc` qua hook H-3b; AC khẳng định `real_eff` (AC-E01, E06, F01.2) đọc qua H-3c. Tuning là object trong bộ nhớ (không đọc file): `s = 0,75`, kẹp 0,1, `D = 14`, `seasons_enabled = false`, mốc 06/11/14/18/21, `earliest_sleep_clock = 18:00`, `defeat_time_cost_minutes = 0`, `save_retry_count = 1`, `phase_timeout_seconds = 10`. Không dùng lệnh debug đặt giờ để dựng trạng thái. **Focus:** fixture tiêm `is_focused = true` qua hook H-9 trừ khi AC ghi khác — test chạy ở batch mode (CI) thường không có focus, nên nếu không tiêm, T1/T3 cho n = 2 và mọi AC kỳ vọng n = 1 sẽ sai với một cài đặt đúng.
- **Delta cố định chính xác nhị phân:** `Δ⅛ = 0,09375 s` (= s/8) → mỗi khung hình cộng đúng 1/8 phút, không sai số float. "k khung hình Δ" = gọi tick k lần với `real_delta = Δ` qua đầu vào được tiêm. **`Step()`** = một khung hình cập nhật của Time với `real_delta = 0` — chạy bước bắt đầu pipeline nếu có cờ chờ (Rule 12), không tiến đồng hồ. Khung hình chạm 06:00 chỉ đặt cờ; pipeline chạy ở `Step()` kế tiếp — AC đếm pipeline sau "8 khung hình Δ⅛ rồi 1 `Step()`", để AC không phụ thuộc thứ tự trong khung hình (OQ-3). Chỉ AC-C03 đọc delta thật của engine.
- **Phase giả** đăng ký qua API của Rule 12 và ghi lại thứ tự gọi; **saver giả** đăng ký stage 7, cấu hình thành công/thất bại N lần. "Ghi log" = bắt được qua `LogAssert` hoặc logger tiêm (hook H-6).
- **AC xuyên hệ chia hai loại** (re-review 2026-10-26):
  - **(a) THEN chỉ khẳng định đầu ra của Time** (`token_count`, `game_minute`, sự kiện, lời gọi phase/saver) — test dùng MonoBehaviour **stub** đóng vai hệ kia (Menu, Dialogue, Region Loading, Defeat, Boss, Cutscene…), xin/nhả token đúng hợp đồng Rule 8. Loại này **thuộc Definition of Done của story Time**.
  - **(b) THEN khẳng định hành vi của hệ khác** (kho đồ, HP, cooldown, túi đồ, chọn slot, catch-up có trần, frustum…) — stub không chứng minh được hệ thật. Loại này mang tag **`[Defer: GDD <hệ>]`**, **không** thuộc DoD của Time, và được chuyển vào GDD của hệ đó khi viết. Danh sách loại (b): AC-R03.1, R03.2, R05.1 (Combat); AC-R10.1, E16, R12.9 (Inventory, phase thật); AC-R13.2, R15a.1, R15a.3, E27, E28, E28b (Save/Load — phần của Time: AC-R13.3, R15a.4); AC-R14.1 phần vị trí, AC-C04, E42 (Region Loading); AC-R18.2, R18.3 (Defeat & Retry, World State); AC-E14a phần HP và trận tiếp tục, E15 (Combat); AC-E41 (Farming, Crafting, Shops); AC-C01, C02, C05, C06, C07 (Menu, Dialogue, Defeat, Boss, Cutscene — hợp đồng giữ token của hệ thật; phần của Time đã có ở AC-S03, R06.1); AC-C09 (Telemetry); AC-C10, C13 (Enemy AI, Shops); AC-C11 (Farming); AC-C12 (Interaction); AC-C16 (Quest, Dialogue); AC-C17 (Flags).
  - **Defer khác, không thuộc DoD của Time:** AC-E22, E32, C14, UI1, UI2 (ảnh chụp màn hình của HUD / giường / menu — `[Defer: GDD HUD, Interaction, Menu]`); AC-PF1, PF3, PF4 (`[Defer: OQ-5]` — chưa có ngưỡng); AC-PF2, E40, E43 (`[Defer: OQ-7]` — thuộc DoD của chủ helper `ElapsedSince`, Time chỉ cung cấp `game_minute`). Mọi AC còn lại là loại (a).
- **Giá trị trong fixture phải nằm trong dải cứng của validator** (Formulas › Ràng buộc validator). Exact-value AC (±1e-6, "bằng từng bit") giả định `acc` là float; nếu ADR OQ-1 chọn bộ đếm tick nguyên, các AC này được viết lại theo đơn vị tick trước story đầu tiên.

### H.1 Core Rules

**Rule 1 — Đồng hồ chuẩn**
- **GIVEN** game mới (T1), **WHEN** đọc trạng thái, **THEN** `game_minute == 0`, `day_number == 1`, giờ `06:00`, segment `Morning`, lifecycle Active với `token_count == 1` (restore handle). (AC-R01.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** fixture tại 1500, **WHEN** serialize DTO save của Time, **THEN** DTO có đúng hai trường `game_minute = 1500` và version dữ liệu lịch; không có ngày, giờ, segment, mùa, năm, thứ hay `acc`. (AC-R01.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** đồng hồ A khôi phục thẳng tới 20.000 và đồng hồ B tiến từ 0 tới 20.000 bằng tick Δ⅛ cộng các lần ngủ, **WHEN** đọc mọi trường F2–F11, **THEN** A và B bằng nhau ở từng trường. (AC-R01.3) `[Logic/EditMode — BLOCKING]`

**Rule 2 — Đơn điệu**
- **GIVEN** fixture tại 0 và kịch bản cố định 50.000 khung hình (xen kẽ Δ⅛, Δ = 0, Δ = 1,8, giữ/nhả token, 3 lần ngủ, 2 lần rollover tại chỗ), **WHEN** ghi `game_minute` sau mỗi khung hình, **THEN** chuỗi không giảm ở bước nào. (AC-R02.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** assembly Time biên dịch không có define dev (H-13), **WHEN** reflection liệt kê mọi member public ghi được `game_minute`, **THEN** chỉ có `Restore` và `AdvanceBy` (tick và stage 2 là nội bộ); lệnh debug đặt giờ không tồn tại. (AC-R02.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** fixture tại 1500, **WHEN** `AdvanceBy(−5)`, `AdvanceBy(0)`, `AdvanceBy(5)`, **THEN** −5 bị từ chối + 1 log và `game_minute` không đổi; 0 không đổi gì; 5 → 1505, 0 sự kiện. (AC-R02.3) `[Logic/EditMode — BLOCKING]`
- **GIVEN** fixture tại 540 (15:00), recorder sự kiện, **WHEN** `AdvanceBy(31)` rồi `AdvanceBy(200)`, **THEN** cả hai bị từ chối + 1 error mỗi lần, `game_minute == 540`, 0 sự kiện; **WHEN** `AdvanceBy(30)`, **THEN** `game_minute == 570`. (AC-R02.6) `[Logic/EditMode — BLOCKING]`
- **GIVEN** fixture tại 710 (17:50), recorder sự kiện, **WHEN** `AdvanceBy(30)`, **THEN** `game_minute == 740` và đúng 1 sự kiện với payload `(Afternoon, Evening, 720)`; **GIVEN** fixture tại 1425, **WHEN** `AdvanceBy(30)`, **THEN** `game_minute == 1439`, 0 sự kiện, cờ chờ không đặt. (AC-R02.4) `[Logic/EditMode — BLOCKING]`
- **GIVEN** cờ chờ Rollover đã đặt tại 1440, **WHEN** `AdvanceBy(5)`, **THEN** trả `Busy`, `game_minute == 1440`, 0 sự kiện. (AC-R02.5) `[Logic/EditMode — BLOCKING]`

**Rule 3 — Hai loại thời gian**
- **GIVEN** cooldown combat còn 2,0 s thời gian khung hình và một token đang giữ, **WHEN** 2,0 s khung hình trôi, **THEN** cooldown về 0 trong khi `game_minute` không đổi. (AC-R03.1) `[Integration/PlayMode — BLOCKING]` — đồng sở hữu GDD Combat.
- **GIVEN** cooldown đó, **WHEN** người chơi ngủ lúc 18:00 (nhảy 720 phút game), **THEN** cooldown chỉ giảm đúng bằng thời gian khung hình trôi trong pipeline, không theo 720 phút game. (AC-R03.2) `[Integration/PlayMode — BLOCKING]`

**Rule 4 — Cách đồng hồ tiến**
- **GIVEN** fixture tại 0, `acc = 0`, **WHEN** 1 khung hình Δ = 2,0 rồi 7 khung hình Δ = 0, **THEN** `game_minute == 0` và `acc == 0,1/0,75` (±1e-6); phần vượt 1,9 s không được cộng dồn. (AC-R04.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** wall-clock tiêm vào (H-2) nhảy −24 h rồi +30 ngày giữa các khung hình, **WHEN** 8 khung hình Δ⅛, **THEN** `game_minute` tăng đúng +1; wall-clock chỉ xuất hiện ở trường "lần chơi cuối" của header save. (AC-R04.2) `[Logic/EditMode — BLOCKING]`

**Rule 5 — Hit-stop và timeScale**
- **GIVEN** Running, `token_count == 0`, **WHEN** Combat kích hoạt hit-stop 20 lần (cơ chế do Combat chọn), **THEN** `token_count == 0` ở mọi khung hình. (AC-R05.1) `[Integration/PlayMode — BLOCKING]` — phần `timeScale` xem AC-C03.

**Rule 6 — Token tạm dừng**
- **GIVEN** Running, **WHEN** giữ A, giữ B, nhả A rồi 8 khung hình Δ⅛, **THEN** `token_count == 1`, `game_minute` và `acc` không đổi (bằng từng bit); **WHEN** nhả B rồi 8 khung hình Δ⅛, **THEN** `token_count == 0` và `game_minute` +1. (AC-R06.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** chỉ A đang giữ, **WHEN** A bị nhả hai lần và một chủ khác gọi nhả, **THEN** `token_count` đi 1 → 0 → 0 → 0, không bao giờ âm; bản dev ghi đúng 2 cảnh báo. (AC-R06.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** giữ (Menu, "Inventory") và (Dialogue, "NPC_01"), **WHEN** liệt kê token (H-4), **THEN** kết quả có đúng 2 cặp đó. (AC-R06.3) `[Logic/EditMode — BLOCKING]`
- **GIVEN** token cuối do một GameObject giữ, **WHEN** GameObject bị `Destroy` hoặc scene bị unload, **THEN** ở tick đầu tiên của Time sau khi việc hủy hoàn tất, bước quét liveness nhả token đó **trước** khi tính F1 (tick đó đã có thể tiến); trạng thái Running; 8 khung hình Δ⅛ kể từ tick đó cộng +1. (AC-R06.4) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** token giữ trong một scope `using`, **WHEN** scope thoát vì exception, **THEN** `token_count` về giá trị trước khi giữ. (AC-R06.5) `[Logic/EditMode — BLOCKING]`
- **GIVEN** token cuối do một đối tượng C# thuần (không phải `UnityEngine.Object`) giữ, **WHEN** mọi tham chiếu tới chủ bị bỏ, **THEN** token **vẫn giữ** (không có liveness), `token_count == 1`, và watchdog (H-5) ghi cảnh báo khi quá `token_watchdog_seconds`. (AC-R06.8) `[Logic/EditMode — BLOCKING]`
- **GIVEN** 3 token đang giữ, **WHEN** save rồi nạp lại, **THEN** DTO không chứa token nào; sau Restoring tập token chỉ có restore handle (T3). (AC-R06.6) `[Logic/EditMode — BLOCKING]`
- **GIVEN** handle H được cấp ở epoch e, **WHEN** T10 (epoch e+1) rồi nhả H, **THEN** no-op: `token_count` không đổi, restore handle mới vẫn giữ. (AC-R06.7) `[Logic/EditMode — BLOCKING]`

**Rule 7 — Token không dừng thế giới**
- **GIVEN** `Time.timeScale = 1`, **WHEN** giữ rồi nhả token 100 lần, **THEN** `Time.timeScale == 1` sau mỗi lần — Time không bao giờ ghi `timeScale`. (AC-R07.1) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** `Time.timeScale = 0` và không token nào, **WHEN** đồng hồ chạy bằng nguồn delta thật, **THEN** đồng hồ vẫn tiến theo AC-C03 — timeScale một mình không dừng được lịch. (AC-R07.2) `[Integration/PlayMode — BLOCKING]`

**Rule 8 — Ai giữ token** (bên ngoài Time: xem H.4)
- **GIVEN** Running, với `Application.runInBackground` lần lượt `true` và `false`, **WHEN** giả lập mất focus (H-9) rồi lấy lại, **THEN** mất focus → `token_count` +1 với (GameTime, Focus); lấy lại → −1; giống nhau ở cả hai giá trị. (AC-R08.1) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** phase giả ở stage 3–6 và saver giả, **WHEN** pipeline chạy, **THEN** trong mỗi callback `token_count ≥ 1` và tập token có (GameTime, DayTransition). (AC-R08.2) `[Logic/EditMode — BLOCKING]`

**Rule 9 — Ranh giới ngày 06:00**
- **GIVEN** `game_minute` 1079, 1080, 1439, **WHEN** đọc ngày và giờ, **THEN** cả ba `day_number == 1`, giờ lần lượt `23:59`, `00:00`, `05:59`. (AC-R09.1) `[Logic/EditMode — BLOCKING]`

**Rule 10 — Đêm là dữ liệu, không phải hạn chót**
- **GIVEN** người chơi thức ngoài nhà lúc 05:58 với các phase Fun-Gate thật đã đăng ký, snapshot kho đồ và tiền, **WHEN** rollover tại chỗ xong, **THEN** không món đồ nào giảm và tiền không giảm. (AC-R10.1) `[Integration/PlayMode — BLOCKING]` — HUD và từ ngữ: AC-C14, AC-C15.

**Rule 11 — Sự kiện ranh giới**
- **GIVEN** fixture tại 0, thức suốt, recorder sự kiện, **WHEN** Δ⅛ tới khi rollover tại chỗ xong, **THEN** đúng 5 sự kiện theo thứ tự: Morning→Noon @300, Noon→Afternoon @480, Afternoon→Evening @720, Evening→Night @900 (cả bốn phát từ tick), Night→Morning @1440 (phát từ stage 2, trước phase stage 3 đầu tiên); mỗi sự kiện mang payload `(from, to, boundary_minute)` khớp danh sách này; tick chạm 1440 phát 0 sự kiện. (AC-R11.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** fixture tại 899, `acc = 0`, **WHEN** 8 khung hình Δ⅛, **THEN** khung hình 1–7: 0 sự kiện; khung hình 8 (`game_minute` thành 900): đúng 1 sự kiện Evening→Night và `IsNight` đọc trong handler trả `true`. (AC-R11.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** một subscriber đăng ký sau 3 lần ngủ, **WHEN** nó đăng ký, **THEN** nhận 0 sự kiện cho ranh giới đã qua (không replay) và truy vấn trả `day_number == 4`. (AC-R11.3) `[Logic/EditMode — BLOCKING]`
- **GIVEN** ba handler của `DayStarted` theo thứ tự H1, H2 (ném exception), H3, và ba handler tương tự cho sự kiện ranh giới từ tick tại 899, **WHEN** ngủ lúc 1230 và (riêng) tick tại 899 → 900, **THEN** mỗi lần phát: H1 và H3 được gọi đúng 1 lần, 1 log nêu sự kiện + handler H2; sau stage 8 token `DayTransition` đã nhả, lifecycle Active, 8 khung hình Δ⅛ cộng +1 (khi không còn token khác); trong handler ranh giới, `game_minute` đọc được = 900. **GIVEN** một handler tự hủy đăng ký trong lúc phát, **THEN** lần phát đó vẫn gọi đủ danh sách đã chụp, lần sau không gọi handler đó. (AC-R11.4) `[Logic/EditMode — BLOCKING]`

**Rule 12 — Pipeline chuyển ngày**
- **GIVEN** phase giả đăng ký lộn xộn (Social, 5), (World, 20), (Economy, 10), (World, 10), (Narrative, 1) và saver giả, **WHEN** pipeline chạy, **THEN** thứ tự gọi: World 10, World 20, Economy 10, Social 5, Narrative 1, Save, `DayStarted`, rồi token chuyển ngày mới nhả (trong handler `DayStarted`, `token_count ≥ 1`). (AC-R12.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** một phase World ghi lại `game_minute`, `day_index`, segment, `IsNight`, **WHEN** người chơi ngủ lúc 1230, **THEN** phase đọc được 1440, 1, Morning, false. (AC-R12.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** fixture tại 1230 (02:30, `sleep_allowed`), **WHEN** lệnh ngủ được chấp nhận (gọi API trong một callback giả), **THEN** API trả `Accepted`; khi API trả về `game_minute == 1230`, phase giả 0 lần, cờ chờ Sleep đặt; **WHEN** 1 `Step()`, **THEN** pipeline đúng 1 lần, kết thúc `game_minute == 1440`; `Step()` thứ hai → tổng pipeline vẫn 1. (AC-R12.8a) `[Logic/EditMode — BLOCKING]`
- **GIVEN** fixture tại 1439, `acc = 0`, phase giả, **WHEN** 8 khung hình Δ⅛, **THEN** khung hình 8 đặt cờ Rollover và phase giả 0 lần trong chính khung hình đó; **WHEN** 1 `Step()`, **THEN** pipeline đúng 1 lần. (AC-R12.8b) `[Logic/EditMode — BLOCKING]`
- **GIVEN** pipeline đồng bộ (Fun-Gate) với phase Fun-Gate thật, **WHEN** ngủ, **THEN** stage 1–8 hoàn tất trong cùng một khung hình. (AC-R12.9) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** saveable giả đánh dấu "dirty" trong handler `DayStarted`, **WHEN** pipeline chạy ở bản dev, **THEN** 1 error nêu tên saveable và stage 8. (AC-R12.10) `[Logic/EditMode — BLOCKING]`
- **GIVEN** đã có (Economy, 10), **WHEN** phase khác đăng ký (Economy, 10), **THEN** API trả lỗi và ghi log; lần chuyển ngày kế tiếp gọi phase cũ 1 lần, phase mới 0 lần. (AC-R12.3) `[Logic/EditMode — BLOCKING]`
- **GIVEN** pipeline đang chạy, **WHEN** một phase World mới đăng ký giữa chừng, **THEN** nó được gọi 0 lần ở lần này và 1 lần ở lần sau. (AC-R12.4) `[Logic/EditMode — BLOCKING]`
- **GIVEN** API đăng ký phase, **WHEN** đăng ký vào Begin, Clock, End, hoặc bất kỳ cách nào khiến một phase chạy sau Save, hoặc một hệ không phải Save/Load đăng ký stage 7, **THEN** bị từ chối. (AC-R12.5) `[Logic/EditMode — BLOCKING]`
- **GIVEN** cờ chờ đã đặt hoặc DayTransition, **WHEN** có lệnh ngủ thứ hai, **THEN** trả `Busy` không xếp hàng; bộ đếm lần chạy pipeline = 1; sau T9 không pipeline nào tự chạy. (AC-R12.6) `[Logic/EditMode — BLOCKING]`
- **GIVEN** 10 lần chuyển ngày liên tiếp (5 ngủ, 5 rollover tại chỗ, xen kẽ), **WHEN** đếm, **THEN** `day_number` +10 và `DayStarted` phát 10 lần. (AC-R12.7) `[Logic/EditMode — BLOCKING]`
- **GIVEN** cờ chờ Rollover đặt tại 1440 và, trong cùng khung hình, stub Region Loading giữ một token `RegionLoad`, **WHEN** 5 bước cập nhật trôi rồi token `RegionLoad` được nhả, **THEN** trong 5 bước đó phase giả 0 lần và saver 0 lần, cờ vẫn đặt, `game_minute == 1440`; sau khi nhả, pipeline chạy đúng 1 lần ở bước kế tiếp; trong callback saver không có token loại `RegionLoad` / `Restore`. (AC-R12.11) `[Logic/EditMode — BLOCKING]`
- **GIVEN** phase giả ghi lại ngữ cảnh nhận được, **WHEN** một lần rollover tại chỗ và một lần ngủ, **THEN** ngữ cảnh mang loại chuyển ngày lần lượt `Rollover` và `Sleep` (để phase thực thi luật "không đụng vùng active", Rule 12). (AC-R12.12) `[Logic/EditMode — BLOCKING]` — tuân thủ của phase thật: mục review GDD World State, Enemy AI, Farming.

**Rule 13 — Ngủ**
- **GIVEN** fixture tại 1020 (23:00 Ngày 1) và fixture riêng tại 1260 (03:00), **WHEN** mỗi bên xác nhận ngủ, **THEN** cả hai kết thúc `game_minute == 1440` và yêu cầu save mang loại slot `Sleep` (H-7). (AC-R13.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** fixture tại 1020, **WHEN** chọn "Ngủ rồi thoát", **THEN** pipeline 1 lần, save `Sleep` ghi thành công, rồi lệnh thoát được phát sau stage 8; Continue nạp `game_minute == 1440`. (AC-R13.2) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** fixture tại 1020, saver giả, recorder lệnh thoát, **WHEN** "Ngủ rồi thoát", **THEN** thứ tự: phase giả, saver (loại `Sleep`) có kết quả, `DayStarted`, nhả token chuyển ngày, **rồi** lệnh thoát được phát đúng 1 lần; saver thất bại (`save_retry_count = 0`) → lệnh thoát vẫn phát sau stage 8 và `LastSaveFailed == true`. (AC-R13.3) `[Logic/EditMode — BLOCKING]`
- **GIVEN** cùng tập phase giả, **WHEN** một lần chạy bằng ngủ (1439) và một lần bằng rollover tại chỗ (1439), **THEN** danh sách phase được gọi và đối số của chúng giống hệt, chỉ khác loại chuyển ngày và loại slot. (AC-R14.2) `[Logic/EditMode — BLOCKING]`

**Rule 14 — Không ngủ mà chạm 06:00**
- **GIVEN** fixture tại 1439, người chơi ở vị trí P ngoài nhà, **WHEN** 8 khung hình Δ⅛, **THEN** sau khung hình 8 cờ chờ Rollover đặt và pipeline 0 lần; **WHEN** thêm 1 `Step()`, **THEN** pipeline chạy đúng 1 lần; kết thúc `game_minute == 1440` (không phải 2880), `day_number == 2`; vị trí vẫn là P; save loại `Autosave`; slot ngủ không bị ghi. (AC-R14.1) `[Integration/PlayMode — BLOCKING]`

**Rule 15 — Lưu thất bại:** xem AC-E21.

**Rule 15a — Thoát game**
- **GIVEN** fixture tại 540 (15:00) với kho đồ đã khác slot ngủ, **WHEN** "Lưu & thoát" rồi Continue, **THEN** `game_minute == 540`, kho đồ bằng lúc thoát, pipeline 0 lần, 0 sự kiện, và người chơi không ở trong trận đang diễn ra. (AC-R15a.1) `[Integration/PlayMode — BLOCKING]` — đồng sở hữu Save/Load.
- **GIVEN** cờ chờ Rollover đã đặt tại 1440, **WHEN** lệnh thoát có chủ đích đến trong cùng khung hình, **THEN** pipeline chạy xong (saver `Autosave` 1 lần) rồi mới ghi slot tiếp tục với `game_minute == 1440`; không có save nào mang trạng thái giữa pipeline. (AC-R15a.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** người chơi đi qua một điểm an toàn lúc 600 rồi tiếp tục chơi tới 900, **WHEN** process bị kill (fault injection) rồi Continue, **THEN** nạp slot tiếp tục với `game_minute == 600`, ở vị trí điểm an toàn đó; phần mất đúng bằng phần chơi sau điểm an toàn. Việc đặt điểm an toàn đủ dày để phần này không vượt một chuyến được kiểm ở GDD Save/Load / Region Loading. (AC-R15a.3) `[Integration/PlayMode — BLOCKING]` — chủ: Save/Load.
- **GIVEN** Running tại 600 và saver giả, **WHEN** lệnh lưu ở điểm an toàn rồi (riêng) lệnh thoát có chủ đích, **THEN** mỗi lệnh gọi saver đúng 1 lần với slot `Continue` và `game_minute == 600`; pipeline 0 lần, 0 sự kiện, `game_minute` không đổi. **GIVEN** token `RegionLoad` đang giữ, **WHEN** lệnh thoát có chủ đích, **THEN** saver 0 lần, lệnh thoát được phát ngay (không chờ). (AC-R15a.4) `[Logic/EditMode — BLOCKING]`

**Rule 16 — Khôi phục im lặng**
- **GIVEN** recorder sự kiện và phase giả, **WHEN** khôi phục tới 1440 và (bật mùa) tới 20.160 (đầu Summer), **THEN** 0 sự kiện khoảng giờ, 0 `DayStarted`, 0 `SeasonChanged`, phase giả 0 lần, trạng thái cuối Suspended(1) với restore handle do người gọi (Region Loading) giữ. (AC-R16.1) `[Logic/EditMode — BLOCKING]` — thứ tự restore: AC-C08.
- **GIVEN** DTO có `game_minute` ∈ {−1; `MAX_GAME_MINUTE` + 1; trường không đọc được}, **WHEN** restore, **THEN** cả ba → T3b (Uninitialized, `token_count == 0`), 1 error mỗi lần, không có giá trị nào bị kẹp; DTO với `game_minute = MAX_GAME_MINUTE` → nạp thành công. (AC-R16.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** fixture tại `MAX_GAME_MINUTE − 1`, **WHEN** 80 khung hình Δ⅛, **THEN** `game_minute == MAX_GAME_MINUTE` (không tràn, không vượt), 1 log khi chạm trần, cờ chờ không đặt. (AC-R16.3) `[Logic/EditMode — BLOCKING]`

**Rule 17 — Catch-up ngoài màn hình:** xem AC-E40 – AC-E43, AC-C04, AC-PF2.

**Rule 18 — Thua (không phải boss)**
- **GIVEN** Running tại 1500, cost 0, **WHEN** chuỗi thua chạy tới lúc điều khiển lại ở nhà, **THEN** `game_minute == 1500`, pipeline 0 lần, saver 0 lần, `token_count ≥ 1` ở mọi tick trong chuỗi thua. (AC-R18.1) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** snapshot kho đồ (đã cất, vũ khí, trang bị, nguyên liệu chưa cất), **WHEN** thua ở vị trí P, **THEN** đồ đã cất, vũ khí, trang bị không đổi; mọi nguyên liệu chưa cất nằm trong **một** túi tại P; quay lại P chạm túi → kho đồ bằng snapshot. Thua lần hai trước khi nhặt → túi cũ biến mất, túi mới tại vị trí mới. (AC-R18.2) `[Integration/PlayMode — BLOCKING]` — chủ: Defeat & Retry (loại b, Defer).
- **GIVEN** túi đồ rơi ở vùng A, **WHEN** 3 lần chuyển ngày (ngủ và rollover) và (khi bật mùa) một lần đổi mùa trong khi người chơi ở vùng khác, **THEN** túi vẫn còn đủ món. (AC-R18.3) `[Integration/PlayMode — BLOCKING]` — chủ: Defeat & Retry, World State.

**Rule 19 — Boss arena (MVP-Complete):** xem AC-E30, AC-E31.

**Rule 20 — Mùa và năm**
- **GIVEN** `seasons_enabled = false`, **WHEN** đọc `season_index`, `year`, `day_of_season` tại mọi `day_index` 0..200, **THEN** cả ba `None`; không `SeasonChanged`/`YearChanged` khi qua `day_index` 14 hay 56. (AC-R20.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** save có `game_minute = 28.800`, **WHEN** lật `seasons_enabled` false → true, **THEN** `game_minute` vẫn 28.800. (AC-R20.2) `[Logic/EditMode — BLOCKING]` — đổi mùa không giết cây: AC-C11.

**Rule 21 — Không có gì bị lỡ vĩnh viễn:** xem AC-F13.4, AC-C16.

**Rule 22 — Độ dài lịch có version**
- **GIVEN** DTO save ghi với version lịch v1, `D = 14`, trong build trước phát hành v2, `D = 28`, **WHEN** nạp, **THEN** nạp thành công, `game_minute` giữ nguyên, 1 log cảnh báo nêu cả hai version. (AC-R22.1) `[Logic/EditMode — BLOCKING]`

**Trạng thái và chuyển tiếp**
- **T1.** **GIVEN** Uninitialized, **WHEN** tạo game mới, **THEN** Active với `token_count == 1` (restore handle của người gọi), `game_minute == 0`, 0 sự kiện; nhả handle → Running. (AC-S01) `[Logic/EditMode — BLOCKING]`
- **T2/T3.** **GIVEN** Uninitialized, **WHEN** nạp save, **THEN** Restoring rồi Suspended(1) với restore handle; 0 sự kiện. (AC-S02) `[Logic/EditMode — BLOCKING]`
- **T3b.** Xem AC-R16.2 và AC-E24.
- **T11.** **GIVEN** Suspended(2), **WHEN** về menu chính, **THEN** Uninitialized, `token_count == 0`, các lệnh nhả từ chủ cũ là no-op. **GIVEN** cờ chờ đặt, **WHEN** về menu chính, **THEN** chờ T9 rồi mới sang Uninitialized. (AC-S11) `[Logic/EditMode — BLOCKING]`
- **Ngủ khi đang nạp.** **GIVEN** Suspended(1) với restore handle (catch-up chưa xong) tại 1230, **WHEN** lệnh ngủ, **THEN** `Busy`, pipeline 0 lần; nhả handle rồi ngủ → `Accepted`. (AC-S10) `[Logic/EditMode — BLOCKING]`
- **T4/T5/T6.** **GIVEN** Running, **WHEN** giữ 1, giữ 2, nhả 1, nhả 2, **THEN** Suspended(1) → Suspended(2) → Suspended(1) → Running. (AC-S03) `[Logic/EditMode — BLOCKING]`
- **T7.** **GIVEN** Running tại 1439, `acc = 0`, **WHEN** 8 khung hình Δ⅛ (khung hình 8 chạm 1440), **THEN** cờ chờ Rollover và token chuyển ngày được đặt ngay trong khung hình 8, pipeline 0 lần; **WHEN** 1 `Step()`, **THEN** DayTransition chạy đúng 1 lần; giữa khung hình 8 và `Step()` `game_minute` vẫn 1440. (AC-S04) `[Logic/EditMode — BLOCKING]`
- **T8/T9.** **GIVEN** Suspended(2) tại 1230, **WHEN** xác nhận ngủ, **THEN** DayTransition rồi về Suspended(2); cùng lệnh từ Running → về Running. (AC-S05) `[Logic/EditMode — BLOCKING]`
- **T10.** **GIVEN** Suspended(3), **WHEN** nạp save từ menu, **THEN** Restoring với `token_count == 1` (chỉ restore handle mới, cấp ở T10; ứng dụng có focus), rồi T3 vẫn đúng 1 token đó; các lệnh nhả từ chủ cũ sau đó là no-op, không âm, và không nhả được restore handle mới. (AC-S06) `[Logic/EditMode — BLOCKING]`
- **Cấm Suspended → T7.** **GIVEN** Suspended(1) tại 1439, `acc = 0,875`, **WHEN** 10.000 khung hình Δ = 0,1, **THEN** `game_minute == 1439`, không bao giờ DayTransition. (AC-S07) `[Logic/EditMode — BLOCKING]`
- **Cấm ngủ sớm.** **GIVEN** fixture tại 719 (17:59), **WHEN** lệnh ngủ, **THEN** `NotSleepy`, không log error, trạng thái và `game_minute` không đổi. (AC-S08) `[Logic/EditMode — BLOCKING]`
- **Cấm giảm đồng hồ.** **GIVEN** mọi trạng thái trừ Restoring, **WHEN** chạy kịch bản AC-R02.1, **THEN** không bước nào giảm `game_minute`. (AC-S09) `[Logic/EditMode — BLOCKING]`
- **T10 khi có cờ chờ.** **GIVEN** cờ chờ Rollover đặt tại 1440 (pipeline chưa bắt đầu), **WHEN** nạp save từ menu, **THEN** cờ bị xóa, phase giả 0 lần, saver 0 lần, `DayStarted` 0 lần; sau T3 tập token chỉ có restore handle. (AC-S12) `[Logic/EditMode — BLOCKING]`
- **Nạp khi đang nạp / game mới khi đang chơi.** **GIVEN** Restoring, **WHEN** nạp save hoặc game mới, **THEN** `Busy`, trạng thái không đổi; **GIVEN** Running tại 1500, **WHEN** game mới, **THEN** đi qua T11 rồi T1 (`game_minute == 0`, epoch mới). (AC-S13) `[Logic/EditMode — BLOCKING]`
- **Token Focus qua epoch.** **GIVEN** ứng dụng mất focus (H-9) và Suspended(1) chủ Focus, **WHEN** T10 rồi T3, **THEN** tập token có restore handle **và** một token `Focus` mới; lấy lại focus → nhả token `Focus`. (AC-S14) `[Logic/EditMode — BLOCKING]`
- **Ngủ khi giữ token RegionLoad.** **GIVEN** Running tại 1230 và một token loại `RegionLoad` đang giữ, **WHEN** lệnh ngủ, **THEN** `Busy`; cùng tình huống với token loại `Menu` → `Accepted`. (AC-S15) `[Logic/EditMode — BLOCKING]`
- **Lưu slot tiếp tục trong catch-up.** **GIVEN** restore handle đang giữ, **WHEN** đọc `CanSaveContinue` và gửi lệnh lưu ở điểm an toàn, **THEN** `false` và saver 0 lần; **WHEN** lệnh thoát có chủ đích, **THEN** saver 0 lần và lệnh thoát phát ngay (Rule 15a). (AC-S16) `[Logic/EditMode — BLOCKING]`
- **Bảng trạng thái × lệnh.** **GIVEN** từng trạng thái của bảng (7 cột rời nhau: Uninitialized, Restoring, catch-up không cờ, bình thường, có cờ chờ không catch-up, catch-up + cờ chờ, DayTransition), **WHEN** gửi từng lệnh (7 hàng), **THEN** kết quả khớp đúng ô của bảng — một test tham số hóa, 49 trường hợp, mỗi trường hợp khẳng định lifecycle, `token_count`, cờ chờ, `game_minute`, số lần saver và kết quả trả về. (AC-S17) `[Logic/EditMode — BLOCKING]`
- **T3 không tăng epoch.** **GIVEN** nạp save (T2) trả restore handle R và ứng dụng đang có focus, **WHEN** T3 rồi nhả R, **THEN** `token_count` 1 → 0, Running; **GIVEN** ứng dụng mất focus, **WHEN** T1 và (riêng) T2 → T3, **THEN** n = 2 (handle + `Focus`); **WHEN** T11, **THEN** Uninitialized với n = 0 và `is_focused` vẫn đúng trạng thái thật. (AC-S18) `[Logic/EditMode — BLOCKING]`
- **T7 không lặp ở bình minh.** **GIVEN** khôi phục tới 1440 rồi nhả restore handle (và, riêng, game mới tại 0 rồi nhả handle), `acc = 0`, **WHEN** 100 khung hình Δ⅛, **THEN** pipeline 0 lần, cờ chờ không đặt, `game_minute` = mốc + 12; **GIVEN** một rollover tại chỗ vừa xong (T9, n = 0, `game_minute == 1440`), **WHEN** 100 khung hình Δ⅛, **THEN** tổng số lần pipeline vẫn là 1. (AC-S19) `[Logic/EditMode — BLOCKING]`

### H.2 Formulas

**F1**
- **GIVEN** fixture tại 0, **WHEN** 7 khung hình Δ⅛, **THEN** `game_minute == 0`, `acc == 0,875`; **WHEN** khung hình thứ 8, **THEN** `game_minute == 1`, `acc == 0` (chính xác tuyệt đối). (AC-F01.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** `acc = 0`, **WHEN** một khung hình Δ = 1,8, **THEN** `real_eff == 0,1`, `acc == 0,13333` (±1e-5), `game_minute` không đổi. (AC-F01.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** `token_count = 1`, `acc = 0,5`, **WHEN** 100 khung hình Δ⅛, **THEN** `acc == 0,5`, `game_minute` không đổi. (AC-F01.3) `[Logic/EditMode — BLOCKING]`
- **GIVEN** bảng cố định các bộ (s, kẹp) ở góc của dải cứng (0,4; 0,25), (0,4; 0,05), (1,5; 0,25), (1,5; 0,05), (0,75; 0,1), `acc = 0,999999`, Δ ∈ {kẹp; 10; 1e6; +∞}, **WHEN** tick 1 lần, **THEN** `minute_advance ∈ {0, 1}` ở mọi dòng. (AC-F01.4) `[Logic/EditMode — BLOCKING]`
- **GIVEN** fixture tại 1439, `acc = 0,875`, **WHEN** một khung hình Δ = 0,1, **THEN** `game_minute == 1440` (không bao giờ 1441) và T7 kích hoạt. (AC-F01.5) `[Logic/EditMode — BLOCKING]`
- **GIVEN** 60 FPS, Δ = 1/60, s = 0,75, **WHEN** 45 khung hình, **THEN** `game_minute == 1` tại khung hình 45 hoặc 46 và `acc < 0,03` (dung sai có chủ ý: 1/60 không biểu diễn chính xác trong float). (AC-F01.6) `[Logic/EditMode — BLOCKING]`
- **GIVEN** fixture tại 0, `acc = 0,5`, **WHEN** 1 khung hình `real_delta = NaN` rồi 4 khung hình Δ⅛, **THEN** sau khung hình NaN `acc == 0,5` (không phải NaN); sau 4 khung hình `game_minute == 1`, `acc == 0`. (AC-F01.7) `[Logic/EditMode — BLOCKING]`

**F2** — **GIVEN** `game_minute` ∈ {0, 1439, 1440, 1500}, **WHEN** tính, **THEN** `day_index` {0, 0, 1, 1}, `day_number` {1, 1, 2, 2}; với `game_minute = MAX_GAME_MINUTE` (2.147.480.767, tức 08:07 ngày cuối) → `day_index == 1.491.306`, `next_dawn == 2.147.482.080`, không tràn int32; hàm F6 thuần `SleepTarget(day_index = 1.491.306) == 2.147.482.080` (cũng không tràn). Ở đúng `MAX_GAME_MINUTE` thì `sleep_allowed == false` — đồng hồ bão hòa nên ngày cuối không bao giờ tới 18:00 và không có save nào mang `game_minute` > `MAX_GAME_MINUTE`. (AC-F02.1) `[Logic/EditMode — BLOCKING]`

**F3** — **GIVEN** `game_minute` ∈ {0, 1079, 1080, 1230, 1439, 1500}, **WHEN** tính, **THEN** giờ {06:00, 23:59, 00:00, 02:30, 05:59, 07:00}, `minute_of_day` {0, 1079, 1080, 1230, 1439, 60}. (AC-F03.1) `[Logic/EditMode — BLOCKING]`

**F4**
- **GIVEN** mốc mặc định, **WHEN** tính `start_i`, **THEN** [0, 300, 480, 720, 900]; tra `minute_of_day` ∈ {0, 299, 300, 479, 480, 719, 720, 899, 900, 1439} → {M, M, Noon, Noon, A, A, E, E, Night, Night}. (AC-F04.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** `game_minute` 1500 và 1230, **WHEN** tính, **THEN** 1500 → Morning, progress 0,2000; 1230 → Night, 0,6111 (±1e-4); tại mọi `start_i` progress = 0; progress luôn < 1. (AC-F04.2) `[Logic/EditMode — BLOCKING]`

**F5** — **GIVEN** mốc Night mặc định, **WHEN** `minute_of_day` 899 rồi 900, **THEN** false rồi true; **GIVEN** Night = 20:00 (offset 840, Evening mặc định 18:00), **WHEN** 839 rồi 840, **THEN** false rồi true, và tổng phút `IsNight = true` trong một ngày = 600. (AC-F05.1) `[Logic/EditMode — BLOCKING]`

**F6** — **GIVEN** `game_minute` ∈ {1230, 2160, 1500, 719, 720, 1439}, **WHEN** tính, **THEN** `sleep_allowed` {true, true, false, false, true, true}, `sleep_target` {1440, 2880, —, —, 1440, 1440}, `minutes_skipped` {210, 720, —, —, 720, 1}. (AC-F06.1) `[Logic/EditMode — BLOCKING]`

**F7** — **GIVEN** (cost, `game_minute`) = (0, 1500), (30, 1425), (30, 1500), (30, 1438), (30, 1409), (30, 1410), (30, 1439), **WHEN** tính, **THEN** `defeat_wake_minute` = 1500, 1439, 1530, 1439, 1439, 1439, 1439 — (30, 1409) là biên đúng 1409 + 30 = 1439 không bị kẹp; không bộ nào kích hoạt DayTransition. (AC-F07.1) `[Logic/EditMode — BLOCKING]`

**F8–F10** (`seasons_enabled = true`)
- **GIVEN** D = 14, **WHEN** `day_index` ∈ {1, 13, 14, 55, 56}, **THEN** `season_index` {0, 0, 1, 3, 0}, `day_of_season` {2, 14, 1, 14, 1}, `year` {1, 1, 1, 1, 2}. (AC-F08.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** D = 7, **WHEN** `day_index` 27 rồi 28, **THEN** (Winter, 7, Năm 1) rồi (Spring, 1, Năm 2); **GIVEN** D = 28, **WHEN** 111 rồi 112, **THEN** Năm 1 rồi Năm 2. (AC-F08.2) `[Logic/EditMode — BLOCKING]`

**F11** — **GIVEN** `day_index` ∈ {0, 1, 14}, **WHEN** tính, **THEN** `weekday` {0, 1, 0} dù bật hay tắt mùa; **GIVEN** mọi D ∈ {7, 14, 21, 28} và S ∈ 0..3, **THEN** `weekday(S·D) == weekday(0)`. (AC-F11.1) `[Logic/EditMode — BLOCKING]`

**F12** (phần tất định; phần hành vi ở H.6) — **GIVEN** fixture tại 0 không token, **WHEN** Δ⅛ với s = 0,75, **THEN** T7 kích hoạt đúng ở khung hình 11.520 (= 1080 s = 18,0 phút thực); s = 1,5 với cùng Δ⅛ = 0,09375 → khung hình 23.040 (36,0 phút); s = 0,75 chạm 1020 (23:00) ở khung hình 8160 (12,75 phút). (AC-F12.1) `[Logic/EditMode — BLOCKING]`

**F13**
- **GIVEN** tại 1500 (`day_index = 1`, D = 14), **WHEN** chạy các truy vấn của ví dụ F13, **THEN** F13a target 0 → `day_index` 7 (10.080); F13b Summer 1 → 14; F13b Spring 1 không include_today → 56; F13c 10:00 → 1680; F13c 06:00 → 2880; F13c 03:00 → 2700 (đêm Ngày 2). (AC-F13.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** `day_index = 7`, target_weekday 0, **WHEN** F13a, **THEN** include_today=true → k = 0; false → k = 7; **GIVEN** `game_minute = 1440`, **WHEN** F13c c = 06:00, **THEN** 2880 (so sánh nghiêm ngặt). (AC-F13.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** `seasons_enabled = false`, **WHEN** F13b, **THEN** `Unsupported`; **GIVEN** N = D+1, S = 4 hoặc S = −1, **WHEN** F13b, **THEN** lỗi tham số + 1 log, không ném exception. (AC-F13.3) `[Logic/EditMode — BLOCKING]`
- **GIVEN** quét toàn bộ (không random) mọi D ∈ {7, 14, 21, 28}, mọi `day_index` ∈ [0, 2·year_len), mọi S, mọi N ∈ 1..D, mọi include_today, **WHEN** F13b, **THEN** 0 ≤ k ≤ year_len; (mùa, ngày trong mùa) của kết quả = (S, N); không k nhỏ hơn nào cũng thỏa; k = 0 chỉ khi include_today=true và hôm nay khớp. F13a quét tương tự với k ≤ 7. (AC-F13.4) `[Logic/EditMode — BLOCKING]`

**Ràng buộc validator** (hàm thuần trên DTO tuning trong bộ nhớ)
- **GIVEN** s ∈ {0; −0,75; 0,75}, **THEN** từ chối, từ chối, chấp nhận. (AC-V01) `[Logic/EditMode — BLOCKING]`
- **GIVEN** (s, kẹp) = (0,75; 0,1), (0,4; 0,25), (0,75; 0,26), (0,75; 0,04), (0,39; 0,25), (1,51; 0,1), **THEN** chấp nhận, chấp nhận, từ chối (kẹp ngoài dải), từ chối, từ chối (s ngoài dải), từ chối. Ràng buộc `kẹp ≤ s` được kiểm riêng bằng một DTO dựng tay bỏ qua kiểm dải (s = 0,2; kẹp 0,25) → từ chối với thông báo nêu `kẹp ≤ s`. (AC-V02) `[Logic/EditMode — BLOCKING]`
- **GIVEN** D ∈ {7, 14, 21, 28} → chấp nhận; D ∈ {0, 15, 35, −14} → từ chối. (AC-V03) `[Logic/EditMode — BLOCKING]`
- **GIVEN** bộ mốc: mặc định; Morning 07:00; Noon = Afternoon = 14:00; Evening 20:00 với Night 19:00, **THEN** chấp nhận, từ chối, từ chối, từ chối. (AC-V04) `[Logic/EditMode — BLOCKING]`
- **GIVEN** `earliest_sleep_clock` ∈ {12:00, 21:00, 11:59, 21:01, 03:00, 06:00}, **THEN** chấp nhận, chấp nhận, từ chối ×4. (AC-V05) `[Logic/EditMode — BLOCKING]`
- **GIVEN** cost ∈ {0, 30, −1, 31}, **THEN** chấp nhận, chấp nhận, từ chối, từ chối. (AC-V06) `[Logic/EditMode — BLOCKING]`
- **GIVEN** s ∈ {0,75; 0,9; 0,6; 1,0; 1,05}, **THEN** cả năm được chấp nhận; số cảnh báo lần lượt 0, 0, 1 (ngoài dải thiết kế), 1 (ngoài dải thiết kế), 2 (ngoài dải thiết kế + trên trần H1 1,0). (AC-V09) `[Logic/EditMode — BLOCKING]`
- **GIVEN** bộ mốc (các mốc khác mặc định): Noon 12:00 / Afternoon 14:00 (Noon dài 120) → chấp nhận; Noon 12:00 / Afternoon 13:59 → từ chối (< 120, cả hai mốc trong dải); Noon 09:59 → từ chối (ngoài dải); Night 22:01 → từ chối; Night 19:59 → từ chối (ngoài dải 20:00–22:00); Night 20:00 → chấp nhận; Evening 19:00 / Night 21:00 → chấp nhận; Evening 19:00 / Night 20:00 → từ chối (Evening < 120). (AC-V10) `[Logic/EditMode — BLOCKING]`
- **GIVEN** `save_retry_count` ∈ {0, 3, −1, 4}, `token_watchdog_seconds` ∈ {120, 1800, 119, 1801}, `phase_timeout_seconds` ∈ {2, 30, 1, 31}, **THEN** mỗi nhóm: chấp nhận, chấp nhận, từ chối, từ chối. (AC-V11) `[Logic/EditMode — BLOCKING]`
- **GIVEN** commit dữ liệu tuning có s = 0, **WHEN** CI chạy, **THEN** build fail với thông báo nêu tên trường. (AC-V07) `[Config/Data — smoke check]`
- **GIVEN** runtime nhận file cài đặt hỏng có s = 0, **WHEN** khởi tạo, **THEN** 1 error, không exception; 8 khung hình Δ⅛ cộng +1 (dùng s = 0,75 mặc định cho riêng trường đó). (AC-V08) `[Logic/EditMode — BLOCKING]`

### H.3 Edge Cases

**Tiến đồng hồ và token**
- **GIVEN** `real_delta` ∈ {−0,5; NaN}, **WHEN** tick, **THEN** `real_eff == 0`, `game_minute` và `acc` không đổi, 1 log; `real_delta = +∞` → `real_eff == kẹp`. (AC-E01) `[Logic/EditMode — BLOCKING]`
- **GIVEN** 5 FPS (Δ = 0,2) suốt 100 khung hình, **WHEN** tick, **THEN** `game_minute` +13, `acc == 0,3333` (±1e-4) — chậm lại, không bù. (AC-E02) `[Logic/EditMode — BLOCKING]`
- **GIVEN** một token giữ liên tục và bộ hẹn giờ thực tiêm vào (H-5) tới 601 s, **WHEN** watchdog chạy, **THEN** đúng 1 cảnh báo nêu chủ và lý do; `token_count` không đổi; đồng hồ vẫn đứng — watchdog không bao giờ tự nhả. (AC-E03) `[Logic/EditMode — BLOCKING]`
- **GIVEN** token rò rỉ (chủ còn sống) giữ lúc 1230 (02:30), **WHEN** xác nhận ngủ, **THEN** pipeline 1 lần, `game_minute == 1440`, Suspended(1), 8 khung hình Δ⅛ sau đó cộng +0, lệnh ngủ tiếp theo trả `NotSleepy` (lối thoát chỉ một lần); **WHEN** về menu chính hoặc nạp save, **THEN** token rò rỉ không còn (epoch mới) và sau khi restore handle được nhả, `token_count == 0`. (AC-E04) `[Logic/EditMode — BLOCKING]`
- **GIVEN** token rò rỉ giữ lúc 1500 (07:00), **WHEN** lệnh ngủ, **THEN** `NotSleepy`; nạp save / về menu chính là lối thoát duy nhất. (AC-E05) `[Logic/EditMode — BLOCKING]`
- **GIVEN** vừa lấy lại focus, **WHEN** khung hình đầu Δ = 30, **THEN** `real_eff == 0,1`. (AC-E06) `[Logic/EditMode — BLOCKING]`

**Ngủ và chuyển ngày**
- **GIVEN** fixture tại 1438, `acc = 0,875`, **WHEN** xác nhận ngủ, **THEN** sau pipeline `game_minute == 1440`, `acc == 0`. (AC-E07) `[Logic/EditMode — BLOCKING]`
- **GIVEN** fixture tại 1439, menu xác nhận ngủ đang mở, **WHEN** 100 khung hình Δ⅛, **THEN** `game_minute == 1439`; xác nhận → nhảy 1 phút, slot `Sleep`; hủy → 8 khung hình Δ⅛ rồi 1 `Step()` → rollover tại chỗ đúng 1 lần, slot `Autosave`. (AC-E08) `[Logic/EditMode — BLOCKING]`
- **GIVEN** Suspended(1) chủ Dialogue tại 1230, **WHEN** lệnh ngủ qua API, **THEN** được chấp nhận; sau T9 Suspended(1) chủ Dialogue. (AC-E09) `[Logic/EditMode — BLOCKING]`
- **GIVEN** DayTransition, **WHEN** một hệ xin token giữa stage 4, **THEN** `token_count` tăng ngay; các phase còn lại vẫn chạy không chờ; T9 về Suspended(1). (AC-E10) `[Logic/EditMode — BLOCKING; async]`
- **GIVEN** phase (Economy, 10) ném exception, **WHEN** pipeline chạy, **THEN** 1 log chứa stage, khóa, lỗi; phase sau vẫn được gọi; saver được gọi; `day_number` +1; `DayStarted` 1 lần. (AC-E11) `[Logic/EditMode — BLOCKING]`
- **GIVEN** một phase không bao giờ hoàn thành, bộ hẹn giờ tiêm vào (H-5) tới 10,0 s, **WHEN** pipeline chạy, **THEN** phase đó bị coi là lỗi như AC-E11, pipeline hoàn tất, và trước 10,0 s không phase nào sau nó được gọi. (AC-E12) `[Logic/EditMode — BLOCKING; async, Defer]`
- **GIVEN** chủ của (Social, 5) bị hủy khi stage 3 đang chạy, **WHEN** tới stage 5, **THEN** phase đó 0 lần, 1 log mức info, pipeline hoàn tất. (AC-E13) `[Integration/PlayMode — BLOCKING; async]`
- **GIVEN** đang giao chiến ngoài arena lúc 05:59, **WHEN** rollover tại chỗ, **THEN** trong pipeline input vẫn được nhận và AI quái vẫn tick (số khung hình > 0); autosave đúng 1 lần. (AC-E14) `[Integration/PlayMode — BLOCKING; async]` — ở Fun-Gate thay bằng AC-E14a.
- **GIVEN** đang giao chiến ngoài arena lúc 05:59 (pipeline đồng bộ), **WHEN** rollover tại chỗ, **THEN** autosave đúng 1 lần, trận đấu tiếp tục ở khung hình sau (HP của người chơi và quái không đổi do pipeline), và độ dài khung hình pipeline được ghi cho AC-PF4. (AC-E14a) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** người chơi bị đánh gục ở stage 4 của rollover tại chỗ, **WHEN** cả hai chuỗi xong, **THEN** pipeline hoàn tất 1 lần, `defeat_wake_minute` tính theo `next_dawn = 2880`, `day_number == 2`. (AC-E15) `[Integration/PlayMode — BLOCKING; async]`
- **GIVEN** tick chạm 1440 rồi, trong cùng khung hình sau tick đó, lệnh ngủ đến, **WHEN** khung hình kế tiếp chạy, **THEN** pipeline đúng 1 lần, lệnh ngủ trả `Busy`, slot `Autosave`. (AC-E17) `[Logic/EditMode — BLOCKING]`
- **GIVEN** người chơi không ngủ suốt 3 ngày, **WHEN** 3 lần rollover tại chỗ, **THEN** `day_number` +3, 3 save `Autosave`, slot ngủ không bị ghi, kho đồ và tiền không giảm do Time. (AC-E16) `[Integration/PlayMode — BLOCKING]`

**Restore và save**
- **GIVEN** saver giả luôn thất bại, `save_retry_count = 1`, **WHEN** pipeline chạy, **THEN** saver được gọi đúng 2 lần; stage 8 vẫn chạy; `day_number` +1; `DayStarted` 1 lần; sự kiện báo lưu lỗi phát 1 lần (H-8) không thêm token; `LastSaveFailed == true`; lần chuyển ngày kế tiếp saver được gọi lại. (AC-E21) `[Logic/EditMode — BLOCKING]`
- **GIVEN** saver giả luôn thất bại, **WHEN** pipeline chạy với `save_retry_count` = 0 rồi = 3, **THEN** saver được gọi đúng 1 rồi 4 lần; **GIVEN** saver thất bại 2 lần rồi thành công, `save_retry_count = 3`, **THEN** gọi 3 lần, `LastSaveFailed == false`, 0 sự kiện lưu lỗi. (AC-E21b) `[Logic/EditMode — BLOCKING]`
- **GIVEN** `LastSaveFailed == true`, **WHEN** mở menu thoát game, **THEN** ảnh chụp cho thấy cảnh báo lưu lỗi. (AC-E22) `[UI — screenshot evidence, BLOCKING]`
- **GIVEN** mất focus ở stage 4, **WHEN** pipeline chạy tiếp, **THEN** hoàn tất; T9 về Suspended(1) chủ (GameTime, Focus). (AC-E23) `[Integration/PlayMode — BLOCKING; async]`
- **GIVEN** DTO hỏng làm nạp thất bại trong Restoring, **WHEN** nạp, **THEN** Uninitialized, `token_count == 0`; **GIVEN** file save không mở được, **THEN** trạng thái và `game_minute` của phiên hiện tại không đổi. (AC-E24) `[Logic/EditMode — BLOCKING]`
- **GIVEN** đang rollover tại chỗ ở stage 4, **WHEN** nạp save từ menu, **THEN** các phase còn lại 0 lần, saver 0 lần, `DayStarted` 0 lần, Restoring với `token_count == 1` (chỉ restore handle mới), `game_minute` = giá trị trong save. (AC-E25) `[Logic/EditMode — BLOCKING; async]`
- **GIVEN** `acc = 0,875` trước khôi phục, **WHEN** khôi phục xong, **THEN** `acc == 0`. (AC-E26) `[Logic/EditMode — BLOCKING]`
- **GIVEN** ba slot với (`save_sequence`, `game_minute`) = ngủ (7, 4320), autosave (9, 5760), tiếp tục (8, 6000), **WHEN** Continue, **THEN** nạp autosave (sequence lớn nhất, dù `game_minute` không lớn nhất); **GIVEN** ngủ (12, 2880) và tiếp tục (11, 2880) cùng giờ, **THEN** nạp slot ngủ; **GIVEN** giờ hệ điều hành bị lùi 1 ngày giữa hai lần lưu, **THEN** kết quả không đổi. (AC-E27) `[Integration/PlayMode — BLOCKING]` — đồng sở hữu Save/Load, Menu.
- **GIVEN** ghi save bị ngắt giữa chừng (fault injection), **WHEN** nạp lại, **THEN** nạp được bản lưu trước với `game_minute` của nó. (AC-E28) `[Integration/PlayMode — BLOCKING]` — chủ: GDD Save/Load.
- **GIVEN** crash (fault injection) sau stage 2 và trước khi stage 7 ghi xong, ở đêm Ngày 3, **WHEN** Continue, **THEN** `day_number == 3`, đồng hồ ở giá trị của lần lưu trước; ngủ lại → pipeline 1 lần, `day_number == 4`; mỗi phase giả được gọi đúng 1 lần cho Ngày 4 tính trên toàn bộ các lần đã lưu. (AC-E28b) `[Integration/PlayMode — BLOCKING]` — đồng sở hữu Save/Load.

**Thua và boss arena**
- **GIVEN** cost 30, thua lúc 1425 (05:45), **WHEN** chuỗi thua chạy, **THEN** tỉnh dậy ở 1439 (05:59), pipeline 0 lần trong chuỗi thua; sau khi trả điều khiển, 8 khung hình Δ⅛ rồi 1 `Step()` → rollover tại chỗ đúng 1 lần. (AC-E29) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** vào arena lúc 1439 với cost 30, **WHEN** thua 3 lần, retry 3 lần (không đi qua Restoring), qua 10.000 khung hình Δ⅛, rồi bỏ cuộc và được đưa về nhà, **THEN** lúc rời arena `game_minute == 1439` (F7 không áp dụng cho đường thua boss); 8 khung hình Δ⅛ rồi 1 `Step()` sau khi rời → rollover tại chỗ 1 lần. (AC-E30) `[Integration/PlayMode — BLOCKING; MVP-Complete]`
- **GIVEN** retry từ checkpoint arena, **WHEN** retry chạy, **THEN** lifecycle của Time không rời Active (0 lần Restoring), token arena giữ liên tục (không có khung hình nào `token_count == 0`), `game_minute` = giá trị lúc vào arena, 0 sự kiện, 0 callback catch-up. (AC-E31) `[Integration/PlayMode — BLOCKING; Defer: MVP-Complete]`

**Lịch và mùa**
- **GIVEN** `game_minute = 1230`, **WHEN** chụp HUD, **THEN** ảnh cho thấy Ngày 1 (không phải Ngày 2) và 02:30. (AC-E32) `[UI — screenshot evidence, BLOCKING]`
- **GIVEN** game mới, **WHEN** 60 khung hình đầu trôi, **THEN** 0 `DayStarted`, 0 lần chạy pipeline. (AC-E33) `[Logic/EditMode — BLOCKING]`
- **GIVEN** bật mùa, D = 14, `day_index` 55, **WHEN** ngủ, **THEN** thứ tự `DayStarted`, `SeasonChanged(Spring)`, `YearChanged(2)`, và trong handler `DayStarted` đã đọc được (Spring, 1, Năm 2); `day_index` 13 → chỉ `DayStarted` rồi `SeasonChanged(Summer)`, 0 `YearChanged`. (AC-E34) `[Logic/EditMode — BLOCKING]`
- **GIVEN** fixture tại 780 (19:00), **WHEN** ngủ, **THEN** đúng 1 sự kiện khoảng giờ với payload `(Evening, Morning, 1440)` rồi `DayStarted`; **không** có `Evening → Night`; fixture tại 720 (18:00) → giống hệt; fixture tại 1230 (02:30) → `(Night, Morning, 1440)`. (AC-E36) `[Logic/EditMode — BLOCKING]`
- **GIVEN** save Fun-Gate ở `day_index` 19, tắt mùa, **WHEN** mở bằng build bật mùa (D = 14), **THEN** đọc được Summer 6, 0 `SeasonChanged`, `game_minute` không đổi. (AC-E39) `[Logic/EditMode — BLOCKING]`

**Catch-up ngoài màn hình**
- **GIVEN** timestamp của hệ phụ thuộc > đồng hồ vừa khôi phục, **WHEN** tính delta catch-up, **THEN** delta = 0 và 1 log chứa tên hệ, timestamp, đồng hồ. (AC-E40) `[Logic/EditMode — BLOCKING]` — cần H-12.
- **GIVEN** cửa hàng tham chiếu và một luống cây không được ghé 144.000 phút, **WHEN** vùng nạp, **THEN** cửa hàng restock đúng 1 lần, cây ở giai đoạn cuối và không héo, xử lý ở trạng thái xong. (AC-E41) `[Integration/PlayMode — BLOCKING]` — chủ: Farming, Crafting, Shops.
- **GIVEN** catch-up giả lập chậm 3 khung hình, **WHEN** vùng nạp, **THEN** token Region Loading giữ tới khi catch-up xong; chưa khung hình nào render nội dung vùng trước đó. (AC-E42) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** một hệ chưa từng có timestamp, **WHEN** vùng nạp, **THEN** mốc của hệ = `game_minute` hiện tại, delta = 0. (AC-E43) `[Logic/EditMode — BLOCKING]`

**Dữ liệu và tuning**
- **GIVEN** save sau phát hành ở (Năm 2, Summer 10, 13:00) với `D = 14`, một hệ phụ thuộc có `last_updated` cách đồng hồ 300 phút, **WHEN** migration sang `D = 21`, **THEN** đồng hồ thành (Năm 2, Summer 10, 13:00) theo `D = 21` và delta catch-up của hệ phụ thuộc vẫn là 300 phút. (AC-E44) `[Logic/EditMode — BLOCKING; Defer: sau phát hành]` — đồng sở hữu Save/Load.
- **GIVEN** save tạo với mốc khoảng giờ hoặc `earliest_sleep_clock` khác, **WHEN** nạp, **THEN** `minute_of_day` không đổi, 1 log mức info. (AC-E45) `[Logic/EditMode — BLOCKING]`
- **GIVEN** `acc = 0,5` ở s = 0,75, kẹp 0,1, **WHEN** đổi s sang 1,5 lúc đang chạy, **THEN** `acc == 0,5`, `game_minute` không đổi trong khung hình đó; sau đó 8 khung hình Δ⅛ (= 0,09375, mỗi khung hình +1/16 phút ở s = 1,5) cộng đúng +1 và `acc == 0`. (AC-E46) `[Logic/EditMode — BLOCKING]`
- **GIVEN** 17:00, **WHEN** đổi `earliest_sleep_clock` sang 16:00, **THEN** lệnh ngủ ngay sau đó được chấp nhận. (AC-E47) `[Logic/EditMode — BLOCKING]`
- **GIVEN** build dev, **WHEN** debug đặt đồng hồ tiến +14.400, **THEN** pipeline 0 lần, 0 sự kiện; **WHEN** "skip day", **THEN** pipeline đúng 1 lần; **WHEN** đặt lùi, **THEN** 0 sự kiện. (AC-E48) `[Logic/EditMode — BLOCKING]`
- **GIVEN** build release, **WHEN** kiểm symbol, **THEN** lệnh debug đặt giờ và skip-day không có mặt. (AC-E49) `[Config/Data — smoke check]`

### H.4 Cross-system

- **GIVEN** Running, **WHEN** mở rồi đóng lần lượt pause, kho, lò rèn, cây vũ khí, settings, **THEN** mỗi lần mở `token_count` +1 chủ Menu, mỗi lần đóng −1; 8 khung hình Δ⅛ khi menu mở cộng +0. (AC-C01) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** Running, **WHEN** hội thoại bắt đầu rồi kết thúc, **THEN** suốt hội thoại có token chủ Dialogue; kết thúc → `token_count` về giá trị ban đầu. (AC-C02) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** nguồn delta thật của production và `Time.timeScale` ∈ {0; 0,05; 1; 2} (mỗi giá trị ≥ 30 khung hình), hoặc một hit-stop của Combat, **WHEN** ghi `Time.unscaledDeltaTime` từng khung hình, **THEN** phần tiến của đồng hồ (`game_minute` + `acc`) = Σ min(unscaled, kẹp)/s (±1e-4) ở mọi giá trị — hit-stop không làm lịch nhanh hay chậm hơn. (AC-C03) `[Integration/PlayMode — BLOCKING]` — *API engine NOT SOURCEABLE; xác nhận tên API ở ADR time domains.*
- **GIVEN** yêu cầu chuyển vùng, **WHEN** quan sát từ yêu cầu tới lúc trả input, **THEN** token Region Loading giữ liên tục; mọi callback catch-up chạy khi `token_count ≥ 1` và `game_minute` không đổi; khung hình đầu có nội dung vùng mới đến sau callback catch-up cuối; token chỉ nhả sau khi trả input. (AC-C04) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** chuỗi thua (không phải boss), **WHEN** quan sát, **THEN** token Defeat giữ từ khung hình đầu của chuỗi tới khung hình người chơi điều khiển lại ở nhà. (AC-C05) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** vào arena boss, **WHEN** rời (thắng, thua, bỏ), **THEN** token Boss giữ từ khung hình vào tới khung hình rời. (AC-C06) `[Integration/PlayMode — BLOCKING; MVP-Complete]`
- **GIVEN** cutscene, **WHEN** chạy từ đầu tới cuối, **THEN** token Cutscene giữ suốt. (AC-C07) `[Integration/PlayMode — BLOCKING; Vertical Slice]`
- **GIVEN** Flags, Inventory, Farming đã đăng ký saveable, **WHEN** nạp save, **THEN** Time khôi phục trước tất cả; trong callback restore đầu tiên của Flags, `game_minute` đã bằng giá trị trong save. (AC-C08) `[Integration/PlayMode — BLOCKING]` — cần H-11.
- **GIVEN** telemetry sink giả (H-14), **WHEN** có ngủ, autosave, `DayStarted`, thua, **THEN** mỗi bản ghi có `game_minute`; khoảng gameplay tính theo `game_minute`, không theo giờ thực. (AC-C09) `[Integration/PlayMode — BLOCKING]`
- **GIVEN** cùng `day_number`, **WHEN** nạp lại ngày đó 3 lần từ hai slot khác nhau, **THEN** random theo ngày (spawn, drop, stock) giống nhau từng phần tử. (AC-C10) `[Integration/PlayMode — BLOCKING]` — chủ: Enemy AI, Shops.
- **GIVEN** cây đang dở, **WHEN** Winter 14 sang Spring 1, **THEN** cây vẫn ở giai đoạn cũ hoặc cao hơn. (AC-C11) `[Integration/PlayMode — BLOCKING; Vertical Slice]` — chủ: Farming.
- **GIVEN** đang có hội thoại, **WHEN** người chơi tương tác với giường, **THEN** không lệnh ngủ nào gửi tới Time. (AC-C12) `[Integration/PlayMode — BLOCKING]` — chủ: Interaction.
- **GIVEN** quái được respawn ở stage 3, **WHEN** stage 3 chạy, **THEN** 0 quái xuất hiện trong frustum camera. (AC-C13) `[Integration/PlayMode — BLOCKING]` — chủ: Enemy AI.
- **GIVEN** HUD lúc 20:59, 23:00 và 05:50, **WHEN** chụp ảnh, **THEN** chỉ có giờ, ngày, khoảng giờ (và mùa khi bật) — không đếm ngược, không số "còn lại", không cảnh báo hết ngày. (AC-C14) `[UI — screenshot evidence, BLOCKING]`
- **GIVEN** giường lúc 17:59 và lúc 18:00, **WHEN** chụp prompt, **THEN** 17:59: "chưa buồn ngủ", không màu lỗi; 18:00: có cả "Ngủ" và "Ngủ rồi thoát". (AC-UI1) `[UI — screenshot evidence, BLOCKING]`
- **GIVEN** menu pause ở 15:00 và 23:00, **WHEN** chọn "Lưu & thoát", **THEN** ảnh chụp không có hộp thoại "sẽ mất tiến độ"; game thoát sau khi ghi slot tiếp tục. (AC-UI2) `[UI — screenshot evidence, BLOCKING]`
- **GIVEN** bảng chuỗi UI, hội thoại NPC và localization, **WHEN** lint danh sách từ cấm ở Player Fantasy › Ngôn ngữ (*hết giờ, trễ, lãng phí, bỏ lỡ, deadline, tối ưu ngày, "chỉ còn X giờ"* và bản tiếng Anh do writer chốt), **THEN** 0 lần xuất hiện. (AC-C15) `[Config/Data — smoke check]`
- **GIVEN** dữ liệu nội dung gắn với ngày, mùa hay thứ, **WHEN** lint, **THEN** mọi mục có chu kỳ lặp ≤ 1 năm (không mục "chỉ một lần"). (AC-C16) `[Config/Data — smoke check]` — chủ: Quest, Dialogue, Shops.
- **GIVEN** `game_minute` ở 7 ngày và mỗi khoảng giờ, **WHEN** Flags đánh giá `day ≥ N`, `IsNight`, `season == Spring`, **THEN** khớp F2, F5, F8 ở mọi mẫu. (AC-C17) `[Logic/EditMode — BLOCKING]` — chủ: Flags.

### H.5 Performance

Nguồn có được: frame budget 16,6 ms ở 60 fps (`.claude/docs/technical-preferences.md` › Performance Budgets). Ngân sách riêng của Game Time là **TBD — performance-analyst chốt** (Open Questions); test có ngưỡng chỉ viết được khi có số.
- **GIVEN** Running với 10 subscriber trên phần cứng tham chiếu (TBD), **WHEN** đo 10.000 tick, **THEN** thời gian trung bình một tick ≤ `gt_tick_budget_ms` (TBD); 0 byte GC alloc mỗi tick nếu technical-director xác nhận là chuẩn dự án. (AC-PF1) `[Integration/PlayMode — BLOCKING sau khi có budget]`
- **GIVEN** mỗi hệ phụ thuộc có catch-up, **WHEN** catch-up với delta 1440 rồi 144.000, **THEN** số lần đánh giá hàm catch-up (bộ đếm, H-12) bằng nhau ở hai lần — không lặp theo ngày. (AC-PF2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** vùng Fun-Gate với nội dung đầy đủ, **WHEN** đo, **THEN** tổng catch-up khi nạp vùng ≤ `gt_catchup_budget_ms` (TBD). (AC-PF3) `[Integration/PlayMode — BLOCKING sau khi có budget]`
- **GIVEN** các phase Fun-Gate thật, **WHEN** đo pipeline, **THEN** tổng stage 1–6 và 8 ≤ `gt_day_pipeline_budget_ms` (TBD); I/O của stage 7 đo **riêng** ≤ `gt_save_io_budget_ms` (TBD); **khung hình chuyển ngày được loại khỏi ngưỡng 16,6 ms mỗi khung hình** (Rule 12), mọi khung hình khác trong và quanh lần chuyển ngày ≤ 16,6 ms trên phần cứng tham chiếu. (AC-PF4) `[Integration/PlayMode — BLOCKING sau khi có budget]`

### H.6 Playtest (Fun-Gate, n = 5 người test bên ngoài, ngưỡng đóng băng trước buổi đầu tiên)

*Mọi mục ở đây là **documented playtest** (`/playtest-report`), advisory. Cổng của Fun-Gate là H1/H2 trong game-concept; các mục dưới đây là chẩn đoán cho hệ Time (D-4).*

*Điều kiện hợp lệ:* đọc hành vi "ở lại qua đêm" (AC-PT1, PT4, PT5) như bằng chứng người chơi **dám** ở lại chỉ hợp lệ khi build Fun-Gate thỏa **N1–N4** (Dependencies). `/playtest-report` ghi rõ build có thỏa không; nếu không, phần đọc về rủi ro đêm được đánh dấu **không hợp lệ** — chỉ còn là quan sát.

- **GIVEN** buổi test Fun-Gate n = 5, **WHEN** sau buổi test hỏi nguyên văn **hai câu riêng** — (a) *"Có lúc nào bạn phải về nhà hoặc đi ngủ dù chưa muốn không? Vì sao?"* và (b) *"Có lúc nào bạn thấy phải chơi tiếp hoặc ở lại ngoài kia dù muốn nghỉ không? Vì sao?"* — và xếp lý do mỗi câu "có" vào một nhóm (thời gian / **nguy hiểm — quái đêm, sợ mất túi** / chán / hết máu / túi đầy / khác — "nguy hiểm" là thử thách có chủ đích, **không** phải lỗi của Time); hỏi thêm trước hai câu đó một **câu kiến thức** *"Bạn nghĩ ở nhà có chuyện gì xảy ra khi bạn ở ngoài lâu?"* và sau đó một **câu tích cực** *"Có lúc nào bạn thấy yên tâm vì biết nhà vẫn ổn không? Lúc nào?"*, **THEN** `/playtest-report` ghi số người "có, vì thời gian" **cho từng câu**, câu trả lời kiến thức và câu tích cực, kèm trích dẫn nguyên văn; mỗi câu "có, vì thời gian" là một phát hiện thiết kế phải được phân loại (lỗi của Time, của hệ khác, hay hiểu lầm UI) trước buổi test kế tiếp. **Không có ngưỡng đạt/trượt.** (AC-PT1) `[Playtest — advisory, chẩn đoán]`
- **GIVEN** cùng buổi test, telemetry ghi thời gian thực của phiên, `game_minute` và giờ ngủ, **WHEN** đo, **THEN** với mỗi người tới được Ngày 4, ghi phút thực lúc Ngày 4 bắt đầu và giờ ngủ trung vị; `/playtest-report` ghi trung vị cạnh dự đoán F12 (38,3 phút khi u = 0; 45,0 khi u = 0,15; s = 0,75; dài hơn nếu người chơi hay ở lại qua đêm — F12 › Kết luận). Kỳ vọng ≤ 60 phút; build phải có **s ≤ 1,0**. (AC-PT2) `[Playtest — advisory]` — H1 (≥ 4/5 tự chơi sang Ngày 4) do game-concept sở hữu.
- **GIVEN** cùng buổi test, **WHEN** đo `suspended_share` (tỷ lệ thời gian thực có ≥ 1 token), **THEN** trung vị **≤ 0,36** (`u_max` ở s = 0,75, F12); vượt thì F12 không còn dự đoán đúng H1 — tune lại `s` hoặc tìm token đang giữ quá lâu. (AC-PT3) `[Playtest — advisory]`
- **GIVEN** cùng buổi test, **WHEN** đo, **THEN** `/playtest-report` ghi cho từng người các **chỉ số hành vi**: (1) số lần ở ngoài sau 21:00 và thời gian ở ngoài; (2) thời gian rảnh chờ giường mở (15:00–18:00 ở nhà không hoạt động); (3) số lần thua (không phải boss) kèm khoảng cách tới nhà, số món trong túi rơi và túi có được nhặt lại không; (4) số lần quay về trước 18:00 khi túi chưa đầy; (5) độ dài mỗi chuyến (phút thực, rời nhà → về nhà/ngủ); (6) số lần giường trả `NotSleepy` và giờ lúc đó (dữ liệu cho OQ-12). Ngưỡng hành động: OQ-9 (game-designer, chốt trước buổi test đầu). (AC-PT4) `[Playtest — advisory]`
- **GIVEN** một **kịch bản ép hoàng hôn** chuẩn bị sẵn — người test được đặt sâu trong vùng lúc 18:30, túi còn thiếu đúng một nguyên liệu để rèn món họ đã chọn, quái đêm bắt đầu xuất hiện — **WHEN** họ chơi tới khi về nhà hoặc ngủ, **THEN** ghi: ở lại hay về, lúc nào, và lý do họ nói (hỏi ngay sau, không gợi ý). Fantasy được hỗ trợ khi người ở lại nói lý do là phần thưởng/tò mò và người về nói lý do không phải thời gian; mỗi lý do "sợ mất gì đó ở nhà / sợ hết ngày" là phát hiện thiết kế. Nếu người test chưa từng trải qua một đêm ở ngoài mà không mất gì trước kịch bản này, kết quả được gắn nhãn **"chưa biết luật"** và không dùng làm bằng chứng cho fantasy. (AC-PT5) `[Playtest — advisory, chẩn đoán]` — đây là khoảnh khắc neo của Player Fantasy.

### Giới hạn của bộ tiêu chí

- **Kiểm không trọn bằng test:** Rule 3 (không hệ nào dùng nhầm loại thời gian) và Rule 10 (không hệ nào dùng đêm làm hạn chót) là luật phủ định áp lên mọi hệ — test chỉ kiểm phần quan sát được (AC-R03, AC-C14, AC-C15, AC-R10.1); phần còn lại là **mục review bắt buộc cho từng GDD phụ thuộc**. Rule 12 "pipeline chỉ xử lý trạng thái luôn được nạp" là ràng buộc cho người viết phase (AC-PF4 là chỉ báo gián tiếp). Rule 17, 21, 22: phần công thức catch-up, chu kỳ nội dung và migration thuộc Farming/Crafting/Shops/World State, Quest/Dialogue và Save/Load.
- **Test hook GDD chưa bảo đảm (→ ADR):** H-1 đầu vào `real_delta` tiêm được cho PlayMode (driver tách khỏi core) · H-2 wall-clock tiêm được · H-3 test đọc được `acc` (`InternalsVisibleTo`) · H-3b test **đặt** được `acc` trong fixture · H-3c test đọc được `real_eff` của tick gần nhất · H-4 trạng thái và danh sách token đọc được trong build test · H-5 bộ hẹn giờ thực tiêm được (watchdog, timeout) · H-6 logger tiêm được hoặc `LogAssert` · H-7 yêu cầu save mang loại slot · H-8 sự kiện lưu lỗi + `LastSaveFailed` · H-9 điểm vào giả lập focus · H-10 bộ đếm và trace pipeline · H-11 quan sát thứ tự restore · H-12 helper chung `ElapsedSince(last_updated)` có kẹp delta âm (chủ sở hữu luật "delta âm → 0" cần chốt) · H-13 tên define build dev · H-14 telemetry sink giả · H-15 tuning dạng object trong bộ nhớ.

## Open Questions

*OQ-1 – OQ-4 và OQ-11 **chặn việc viết code** (story đầu tiên của Time), **không chặn việc duyệt GDD** — GDD chỉ nêu hợp đồng hành vi.*

| # | Câu hỏi | Chủ | Hạn |
|---|---|---|---|
| OQ-1 | **ADR "time domains":** nguồn tick (Update + unscaled delta), giao diện tiêm thời gian (H-1, H-2, H-5), kiểu số nguyên của `game_minute` (int32 vs int64; trần `MAX_GAME_MINUTE` đã chốt ở Rule 16, cộng có bão hòa), bộ đệm phần lẻ float hay **bộ đếm tick nguyên** (khuyến nghị review 2026-10-26: tick nguyên loại bỏ sai số float; nếu chọn, viết lại các AC giá trị chính xác theo đơn vị tick trước story đầu tiên). Xác nhận các API engine hiện là NOT SOURCEABLE (`timeScale`, `unscaledDeltaTime`, `FixedUpdate` khi timeScale = 0, `Run In Background`, `OnApplicationPause/Focus` trên desktop/Linux/Steam Deck, `Application.quitting` cho Rule 15a) bằng tài liệu Unity 6.3 hoặc test trên máy | technical-director, unity-specialist | **Trước story đầu tiên của Time** |
| OQ-2 | **ADR token:** handle `IDisposable` có epoch (Rule 6); bước quét liveness đầu tick (cách phát hiện chủ đã hủy — kiểm "fake null" của `UnityEngine.Object`); liệt kê token trong build test (H-4); toggle dev cho token focus; tham chiếu chủ có kiểu `UnityEngine.Object` (phép so sánh null của engine) và giao diện liveness tiêm được để test EditMode; **không có trạng thái static** (mọi trạng thái của Time là instance do composition root sở hữu, hoặc reset lúc khởi động) để đúng khi Enter Play Mode tắt domain reload; ghim *Run In Background* trong ProjectSettings; nguồn `is_focused` (Rule 6) | technical-director, lead-programmer | **Trước story đầu tiên của Time** |
| OQ-3 | **ADR pipeline + sự kiện:** API đăng ký phase (khóa thứ tự, cô lập lỗi); **đồng bộ ở Fun-Gate**, có cho phép phase bất đồng bộ về sau không (mở khóa các mục *[async]* và `phase_timeout_seconds`); cờ chờ và "bước cập nhật kế tiếp" cụ thể là gì; sự kiện đồng bộ hay xếp hàng; thứ tự sự kiện so với catch-up; API restore trả restore handle; **tái nhập** — lệnh nào được gọi từ trong handler sự kiện hay phase (ngủ, `AdvanceBy`, token, nạp) và kết quả của chúng; **thứ tự các bước trong một khung hình** (quét liveness, bắt đầu pipeline, tick; tick có chạy trong khung hình pipeline kết thúc không); cách pipeline chờ token `RegionLoad`/`Restore` (Rule 12); cơ chế hoãn thoát và lần ghi đồng bộ khi thoát (Rule 15a); phase không được nạp asset; không bắt đầu pipeline khi ngủ cho tới khi fade đã kín | technical-director, lead-programmer | **Trước story đầu tiên của Time** |
| OQ-4 | **Assembly hợp đồng trung lập** cho Time ↔ Save/Load ↔ Flags (đăng ký hai chiều) và asmdef theo layer (TD-C) | technical-director | **Trước story đầu tiên của Time** |
| OQ-5 | **Ngân sách hiệu năng** `gt_tick_budget_ms`, `gt_catchup_budget_ms`, `gt_day_pipeline_budget_ms`, `gt_save_io_budget_ms` (I/O stage 7, đo riêng — Rule 12) và phần cứng tham chiếu (minimum spec) — AC-PF1, PF3, PF4. Vì pipeline Fun-Gate đồng bộ, `gt_day_pipeline_budget_ms` là **một khung hình**, gồm I/O của stage 7: chốt ngân sách hitch chấp nhận được khi rollover xảy ra giữa trận (AC-E14a), hoặc tách I/O của stage 7 ra luồng nền | performance-analyst | Trước `/gate-check technical-setup` |
| OQ-6 | **Hợp đồng với Save/Load** (xác nhận khi viết GDD Save/Load): nguyên tử khi ghi; loại slot trên yêu cầu save (H-7); `LastSaveFailed` (H-8); quan sát thứ tự restore (H-11); `save_sequence` cho Continue; slot tiếp tục: thoát có chủ đích, bắt tín hiệu thoát, **danh sách điểm an toàn** đủ dày để crash mất ≤ một chuyến (D-3); migration U3 **quy đổi cả `last_updated` của hệ phụ thuộc và mọi `game_minute` khác trong save** (AC-E44); nạp autosave/slot tiếp tục không bao giờ vào giữa trận (CD C3) | systems-designer (GDD Save/Load) | GDD #3 |
| OQ-7 | **Ai sở hữu helper `ElapsedSince(last_updated)`** có kẹp delta âm → 0 + log (H-12), và **helper seed theo ngày** `DaySeed(world_seed, day_index, system_id, entity_id)` với thuật toán hash cố định (Edge Cases › reroll): Time hay một thư viện dùng chung; bảng `system_id` | lead-programmer | Cùng OQ-3 |
| OQ-8 | **Farming/Crafting tính theo phút hay theo ngày** — ảnh hưởng anti-fantasy "ngủ sớm để tua" (Player Fantasy; Interactions) | game-designer (GDD Farming, Crafting) | GDD Farming |
| OQ-9 | **Ngưỡng hành động cho các chỉ số hành vi** (AC-PT4): bao nhiêu lần thua gần nhà, bao lâu chờ giường, bao nhiêu lần về sớm khi túi chưa đầy thì coi là vấn đề; thứ tự biện pháp: chi phí phi thời gian (Defeat & Retry) trước, `defeat_time_cost_minutes` (≤ 30) sau cùng | game-designer | **Trước buổi test Fun-Gate đầu tiên** (ngưỡng đóng băng) |
| OQ-10 | **Độ dài mùa so với độ dài phiên** chốt trước Vertical Slice (mặc định D = 14 → năm ≈ 14 giờ chơi) — xem lại với dữ liệu playtest thật | game-designer, producer | Trước Vertical Slice |
| OQ-11 | **Spike phần cứng (checklist kiểm được) cho time / app-lifecycle / object-lifetime** trên Unity 6.3, chạy trên Windows **và Steam Deck**: (1) callback mất/lấy lại focus — kể cả khởi động khi không có focus, overlay nền tảng, máy ngủ/thức; (2) mọi đường thoát — menu, đóng cửa sổ, "Exit game" của Deck, tắt máy/đăng xuất, kill process — đường nào đi qua cơ chế hoãn thoát; (3) thời điểm `Destroy` / unload scene có hiệu lực và phép so sánh null với đối tượng đã hủy; (4) hành vi khi Enter Play Mode tắt domain reload; (5) `unscaledDeltaTime` khi `timeScale = 0` và sau hitch; (6) thời gian ghi save đồng bộ trên thẻ SD của Deck (đầu vào OQ-5). Kết quả ghi vào `docs/engine-reference/unity/`. Hiện mọi hành vi engine trong GDD này là NOT SOURCEABLE | unity-specialist | **Trước story đầu tiên của Time** (cùng OQ-1) |
| OQ-12 | **Tranh luận chưa đóng — giờ ngủ sớm nhất 18:00 (D3):** game-designer đề nghị bỏ hoặc dời; CD giữ D3 ("đo, đừng thiết kế lại"). Dữ liệu quyết: chỉ số "chờ giường" của AC-PT4 | creative-director, game-designer | Sau buổi test Fun-Gate đầu tiên |
