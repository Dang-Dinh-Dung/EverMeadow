# Flags, Conditions & World State

> **Status**: In Design
> **Author**: Đặng Đình Dũng + agents
> **Last Updated**: 2026-10-27
> **Last Verified**: 2026-10-27
> **Implements Pillar**: Pillar 3 (Two Lives, One Loop — luật đường chính, mở vùng bằng flag); Pillar 1 (khám phá là flag); Pillar 5 (nội dung đã mở không khóa lại)

## Summary

[To be designed]

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `None (providers & effect handlers register into it)`

## Overview

Flags, Conditions & World State là **bộ nhớ sự thật của thế giới Evermeadow**: nơi duy nhất ghi *điều gì đã xảy ra và điều gì đang đúng* (boss nào đã hạ, bí mật nào đã tìm, vùng nào đã mở, quest đang ở bước nào, rương nào đã mở, `world_seed` của lần chơi này), và là nơi duy nhất trả lời câu hỏi *"điều kiện X có đang thỏa không?"* cho mọi hệ khác. Hệ gồm ba phần gắn liền. **Flag và World State** là các giá trị có kiểu, đặt tên theo namespace (`boss.*`, `quest.*`, `rumor.*`…), cùng trạng thái theo vùng (rương, điểm thu hoạch, respawn) được tính lười từ timestamp của Game Time & Calendar khi vùng nạp. **Condition** là một *tập predicate đóng* — flag, affinity ≥ N, có vật phẩm, ngày/mùa/khoảng giờ, trạng thái quest — mà các hệ khác ghép lại để nói *khi nào* một thứ mở ra; bản thân giá trị affinity, vật phẩm hay thời gian không nằm ở đây mà do hệ sở hữu chúng đăng ký **provider** vào. **Effect** là một *tập lệnh ghi đóng* (SetFlag, GiveItem/TakeItem, AffinityDelta, GiveCurrency, StartQuest) để Dialogue và Quest đổi trạng thái thế giới mà không phụ thuộc ngược vào Affinity hay Inventory. Hệ này không biết các hệ kia là ai: chúng đăng ký provider và handler vào nó, còn nó không bao giờ tham chiếu lên (TD-A4, TD-B3, TD-B5); mỗi namespace flag chỉ **một** hệ được ghi, và hệ cũng là chủ của `world_seed` mà Game Time và các hệ có random theo ngày dựa vào.

Người chơi không "dùng" hệ này — họ chỉ thấy thế giới *nhớ họ*. Nó là chất liệu của hầu hết luật mở khóa trong game, được biểu diễn bằng điều kiện trên flag thay vì bằng cạnh phụ thuộc trực tiếp giữa các hệ: một loại vũ khí hay nhánh bí mật "đã tìm thấy" là một flag (Pillar 1); luật đường chính và việc mở vùng mới là điều kiện trên tiến độ boss `boss.*` (Pillar 3); trần tier chung là một điều kiện trên cùng tiến độ đó (Pillar 2); nội dung đã mở nhờ quan hệ NPC không bao giờ khóa lại khi quan hệ giảm (Pillar 5). Không có hệ này, mỗi hệ sẽ tự giữ một cờ "đã xảy ra chưa" riêng, logic mở khóa rải khắp nơi, và save/load mất một nguồn sự thật để khôi phục. Đồng thời đây là hệ dễ phình nhất của nền: nên thiết kế giữ tập predicate và tập effect **đóng** — thêm một loại là sửa GDD này — để nó không biến thành một ngôn ngữ scripting, và để flag không trở thành trạng thái toàn cục khả biến mà ai cũng ghi được. Hệ đăng ký saveable vào Save/Load và được khôi phục sau đồng hồ Game Time, trước Inventory.

## Player Fantasy

*Framing: gián tiếp — người chơi cảm nhận hệ quả, không bao giờ thao tác với hệ này. Hướng chọn 2026-10-27 (creative-director đề xuất 3 hướng; người dùng chọn A làm xương sống, phép thử của B làm luật ràng buộc, khoảnh khắc nạp save của C làm bằng chứng).*

**"Thần nhớ tên bạn."**

Thế giới Evermeadow **ghi nhận** người chơi. Mỗi việc họ làm — tìm ra một loại vũ khí, lần ra một lối bí mật, hạ một linh thú hộ vệ, mở một rương, nhận lời một NPC — trở thành một phần của thế giới, và thế giới cư xử như thể nó đã xảy ra, vì nó đã xảy ra. Đây là phần mở rộng của giao ước trong World Premise ("thần nhớ tên người thắng") từ boss sang mọi khám phá: không chỉ chiến thắng lớn mà cả những dấu chân nhỏ đều được giữ.

Cảm xúc lõi là **được công nhận**, đứng trên một nền **tin cậy**: người chơi không bao giờ phải tự hỏi *"cái đó có được tính không, có mất không?"*. Nó phục vụ **Pillar 1** (khám phá có giá trị vì nó ở lại), **Pillar 3** (đường chính không bao giờ bị khóa sau farm hay affinity) và **Pillar 5** — *"Nội dung đã mở nhờ affinity (nhánh bí mật, công thức) không bao giờ khóa lại khi affinity giảm."* Sự tin cậy đó là thứ cho phép người chơi dám mạo hiểm: thử một NPC khó tính, đi sâu vào sương, đổi sang một loại vũ khí khác.

**Khoảnh khắc neo.**
- *Trong phiên (kiểm được từ Fun-Gate):* vài ngày sau khi hạ linh thú hộ vệ, người chơi đi lại qua Mist Forest. Lối từng bị phong ấn vẫn thông, điện thờ vẫn yên, và bác thợ rèn nhắc qua về chiến thắng ấy trong một câu bâng quơ. Không có popup "đã lưu tiến độ" — thế giới chỉ đơn giản *biết*.
- *Bằng chứng khi quay lại (nạp save):* sau một tuần không chơi, người chơi nạp save. Rương đã mở vẫn mở, tin đồn đã lần theo vẫn được gạch trong nhật ký, vùng vẫn mang đúng hình dạng cũ (cùng `world_seed`). Họ tiếp tục như chưa từng rời đi.
- *Tin cậy khi quan hệ nguội:* người chơi quên món quà lễ hội và quan hệ với bác thợ rèn nguội đi. Bước vào lò rèn, họ chuẩn bị tinh thần — nhưng nhánh bí mật vẫn nằm trong cây vũ khí. Chỉ có giá đắt hơn, và tình bạn vẫn hàn gắn được.

*Design test (chính, cho cách thể hiện):* Nếu phân vân giữa để thế giới **kể** cho người chơi điều họ đã làm (danh sách, dấu tích, thông báo) và để thế giới **cho thấy** điều đó (lối mở, rương mở, lời NPC đổi), hệ này chọn **cho thấy**.

*Design test (ràng buộc, cho luật):* Nếu phân vân giữa một thứ đã mở được **kiểm lại liên tục** (có thể "tắt" khi affinity giảm) và một thứ **được ghi một lần lúc nó mở** (một chiều), hệ này chọn **ghi một lần**. Luật này được cụ thể hóa ở Detailed Design.

*Design test (phụ, cho khôi phục):* Nếu phân vân giữa **tạo lại** trạng thái khi nạp save và **khôi phục chính xác** nó, hệ này chọn **khôi phục chính xác**. Thứ được thiết kế để làm mới (điểm thu hoạch, quái respawn) làm mới **theo lịch**, không bao giờ theo việc nạp lại.

**Anti-fantasy: "thế giới hay quên".** Cách lớn nhất để hệ này phản bội fantasy không phải xóa nội dung (dễ thấy) mà là gieo một **nghi ngờ âm thầm**: *"cái đó có được tính không? có thể mất không?"* — nghi ngờ khiến người chơi tích trữ, save-scum, tránh mạo hiểm với NPC và kiểm lại những việc đã làm. Các dạng cụ thể hệ này phải chặn:
- Một thứ đã mở **biến mất** khi affinity giảm.
- Một khám phá bị **giấu lại** do thứ tự nạp hay một lần reset.
- Người chơi **không rõ** một thứ đã được mở hay chưa, vì vật một-lần và vật làm-mới trông giống nhau.
- Một NPC **không bao giờ** nhận ra rằng boss đã bị hạ.

Và một ranh giới thứ hai: thế giới nhớ **để cho**, không phải **để phạt**. Một "mối hận" không hàn gắn được phá Pillar 5 — trí nhớ của thế giới không bao giờ được thành một hình phạt vĩnh viễn.

**Ngôn ngữ.** Dùng: *nhớ, giữ, ghi dấu, đã tìm thấy, công nhận, dấu chân, mở (và mở mãi), quay lại, lời hứa*. Tránh trong GDD, UI copy và hội thoại NPC: *khóa lại, thu hồi, mất, reset, hết hạn, bỏ lỡ vĩnh viễn, "missable"*, và mọi chữ "flag" hay "điều kiện chưa thỏa" hiện ra trước người chơi.

## Detailed Design

*Nguồn: systems-designer (luật lõi, trạng thái, tương tác), gameplay-programmer (khả thi, testability, hiệu năng), engine-programmer (tích hợp engine), 2026-10-27. Quyết định của người dùng F-1 – F-4 cùng ngày (dưới). Mọi hành vi engine nhắc ở đây là **NOT SOURCEABLE** từ `docs/engine-reference/unity/` (không có module core/scripting/serialization) — GDD chỉ nêu hợp đồng hành vi; cơ chế để ADR (Open Questions). **[P]** = giả định tạm về một hệ chưa có GDD; GDD đó phải xác nhận.*

*Quyết định người dùng (2026-10-27):* **F-1** kiểu flag chỉ Bool + Int, ba loại Latch / Stage / Counter (không Enum). **F-2** hệ tiêu thụ ghi latch mở khóa đúng lúc mở (không có bảng luật mở khóa tập trung). **F-3** trạng thái vật thể vùng hỏi qua API truy vấn riêng, tập lá Condition giữ đóng. **F-4** chống phát thưởng hai lần bằng trường `once_key` trên lô effect.

*Sửa đổi 2026-10-27 (từ rà soát Edge Cases, systems-designer; người dùng chọn sửa thẳng luật thay vì để Edge Cases ghi đè):* **F-5** ngủ trong vùng → Region Loading hỏi lại tình trạng có sẵn (Rule 13). **F-6** trần cascade chỉ áp cho chuỗi có Counter đổi (Rule 9, F4). **F-7** điều kiện khai báo `usage ∈ {Permanent, Transient}` (Rule 10). Theo khuyến nghị (guided, quyết định nhỏ): chụp `renew_days` lúc tiêu thụ (F1); `once_key` vẫn đặt khi handler lỗi sau kiểm trước (Rule 12); cấm đổi `default` / loại / kiểu của khóa đã phát hành nếu không kèm migration (validator); tiêu thụ vật thể nguyên tử với lô thưởng (Rule 13); handler được ghi flag nhưng save chỉ chụp ở điểm yên tĩnh (Rule 15). Các luật đã sửa: 1, 3, 4, 5, 8, 9, 10, 11, 12, 13, 15, 16, States, F1, F4, validator.

### Core Rules

**Flag và kho trạng thái**

1. **Khóa flag.** Mỗi flag có một khóa ổn định dạng `namespace.tên`, là ID của Game Data & Tuning (TD-B4 — ID không đổi khi đổi tên hiển thị). Mỗi flag được **khai báo một lần** trong dữ liệu (catalog) với: kiểu, loại, giá trị mặc định, namespace sở hữu, và (với Counter) dải `[min, max]`. **Flag chưa khai báo không ghi được.** Bên trong, khóa được quy về ID số nguyên lúc nạp; mọi so sánh là theo ID, không theo chuỗi hay tham chiếu đối tượng. **Định dạng:** chỉ `[a-z0-9_.]`, chữ thường, có ít nhất một dấu `.`; **namespace là đoạn trước dấu `.` đầu tiên** (`weapon.ember.unlocked` thuộc `weapon`).
2. **Kiểu (tập đóng, F-1):** `Bool` và `Int` (int32). Không có chuỗi, số thực hay Enum.
3. **Loại (tập đóng, F-1):**
   - **Latch** — `Bool`, chỉ đi `false → true`. Dùng cho mọi thứ "đã xảy ra": đã tìm vũ khí, đã mở bí mật, đã nhận thưởng.
   - **Stage** — `Int`, chỉ tăng. Dùng cho tiến trình có thứ tự: tiến độ boss, bước quest.
   - **Counter** — `Int`, đổi tự do, kẹp trong `[min, max]` khai báo. Dùng cho số đếm có thể giảm.
   - **Mọi thứ mở khóa phải là Latch hoặc Stage** (luật ràng buộc của Player Fantasy). Lệnh ghi làm **giảm** một Latch hay Stage bị **từ chối**: không đổi gì, ghi error, trả `Rejected(Monotonic)` cho người gọi.
   - **Ghi không đổi giá trị** (Latch `true` → `true`, Stage đặt bằng giá trị hiện tại, `Add` 0) trả `Ok(NoChange)`: không phải lỗi, không phát `FlagChanged`, không tăng `Revision`.
   - **Stage bão hòa:** `Add` chạm `int32.Max` từ dưới → `Ok`, warn; `Add` khi đã ở `int32.Max` → `Ok(NoChange)`, warn một lần mỗi khóa mỗi phiên.
   - Mặc định khai báo: Latch luôn `false`; Stage `≥ 0`.
4. **Đọc giá trị.** Flag đã khai báo nhưng chưa từng ghi trả **giá trị mặc định** đã khai báo. Đọc một khóa chưa khai báo **qua API đọc trực tiếp** trả `false` / `0` và ghi cảnh báo (một lần mỗi khóa mỗi phiên); **qua lá `Flag` của điều kiện thì lá trả `Unknown`** (Rule 7) — nếu trả `false`, `Not(Flag(khóa_gõ_sai))` sẽ thành `true` và tự mở nội dung. Ghi một khóa chưa khai báo bị từ chối (`Rejected(Undeclared)`). Đọc khi kho không ở Ready trả giá trị mặc định và ghi error một lần mỗi lần vào trạng thái (đếm **riêng** với `Evaluate`, Rule 8) — không bao giờ lộ trạng thái khôi phục dở dang.
5. **Một hệ ghi mỗi namespace (TD-B3).** Trong cửa sổ đăng ký (States), mỗi hệ sở hữu **nhận** namespace của mình và được cấp một **writer handle**. Hệ thứ hai nhận cùng namespace bị từ chối (error). Mọi lệnh ghi phải mang handle; lệnh ghi sai handle **bị từ chối, không đổi gì, ghi error** (bản dev: assert). Ai cũng đọc được mọi namespace. Bảng khởi đầu:

   | Namespace | Hệ ghi duy nhất | Nội dung |
   |---|---|---|
   | `boss.*` | Boss Encounter | Stage tiến độ từng boss (Pillar 2 trần tier, Pillar 3 mở vùng) |
   | `quest.*` | Quest & Hidden Quest | Stage bước quest |
   | `rumor.*` | Dialogue & Discovery Hints | Latch tin đồn / hiểu biết đã có |
   | `secret.*` | Interaction & Context Prompts **[P]** | Latch bí mật vùng đã tìm (systems-index C3) |
   | `weapon.*` | Weapon Tree **[P]** | Latch loại vũ khí / nhánh đã tìm, đã mở |
   | `recipe.*` | Crafting & Forge **[P]** | Latch công thức đã mở |
   | `cutscene.*` | Cutscene & Scripted Sequence **[P]** | Latch cảnh đã xem |
   | `tutorial.*` | Tutorial & Onboarding **[P]** | Latch gợi ý đã hiện |
   | `world.*`, `region.*` | Hệ này | **Dành riêng** — v1 không có flag nào trong catalog; `world_seed` (Rule 14) và vật thể vùng (Rule 13) là trường riêng của DTO |

   Thêm namespace là sửa bảng này (sửa GDD); validator từ chối mọi khóa catalog thuộc namespace không có trong bảng. Nhận namespace trùng (sau khi hạ chữ thường) bị từ chối. Affinity, vật phẩm và thời gian **không phải flag** — chúng là giá trị do hệ sở hữu cung cấp qua provider (Rule 7).

**Condition**

6. **Ngữ pháp điều kiện — không phải ngôn ngữ scripting.** Một điều kiện là một cây gồm các nút `All[...]`, `Any[...]`, `Not(...)` và **lá**. Giới hạn cứng: độ sâu ≤ `COND_MAX_DEPTH`, số lá ≤ `COND_MAX_LEAVES` (Formulas); **không có biến, không có số học, không so hai giá trị runtime với nhau, không có tác dụng phụ** — mỗi lá so **một** giá trị với **một** hằng. `All` / `Any` rỗng và cây vượt giới hạn bị **validator lúc author** từ chối (không phải lúc chạy). Tập lá (đóng; thêm lá là sửa GDD + thêm unit test):

   | Lá | Ý nghĩa | Nguồn giá trị |
   |---|---|---|
   | `Flag(khóa, op, v)` | `op ∈ {==, ≠, ≥, ≤}`, so với hằng `v` | Kho flag của hệ này |
   | `Affinity(npc) ≥ N` | Ngưỡng quan hệ; giá trị N do GDD NPC Affinity sở hữu | Provider Affinity |
   | `HasItem(item) ≥ n` | Có ít nhất n vật phẩm | Provider Inventory |
   | `Time(...)` | `day ≥ N`, `segment ∈ {…}`, `IsNight`, `season == S`, `weekday ∈ {…}` | Provider Game Time (Interactions của GDD Game Time) |
   | `Quest(q) ==/≥ bước` | **Cú pháp gọn** trên Stage `quest.q` — không cần provider riêng **[P]** | Kho flag |

7. **Provider.** Mỗi loại lá ngoài (Affinity, Inventory, Time) có **đúng một** provider, do hệ sở hữu giá trị đăng ký; đăng ký trùng bị từ chối. Provider phải **không có tác dụng phụ** (đọc thuần). **Lá không có provider, provider ném exception, hay provider nhận ID thực thể không tồn tại đều trả `Unknown`** (exception bị bắt): `Unknown` lan qua cây (`Not(Unknown) = Unknown`; `All` có một `Unknown` mà không có `false` thì là `Unknown`; `Any` tương tự với `true`), và **gốc `Unknown` được coi là `false`** ("đóng an toàn"), ghi error một lần mỗi loại lá mỗi phiên, không bao giờ ném exception. An toàn vì mọi thứ đã mở đều đã được ghi thành latch (Rule 10): thiếu provider chỉ làm **chậm** một lần mở, không bao giờ **lấy lại** thứ đã mở.
8. **Đánh giá theo yêu cầu.** Điều kiện chỉ được đánh giá khi một hệ hỏi (kéo), đồng bộ, thuần, trên trạng thái **đã commit**. Không có đăng ký theo dõi điều kiện. Chỉ đánh giá được ở trạng thái **Ready** (States); hỏi ở trạng thái khác trả **`NotReady`** (kết quả là `{True, False, NotReady}`, không phải bool trơn — để `if (!Evaluate(...))` không hiện gợi ý khi đang ở menu hay đang nạp) và ghi error một lần mỗi lần vào trạng thái (đếm riêng với đọc trực tiếp, Rule 4). Toàn bộ API **chỉ chạy trên main thread**; từ thread khác, lệnh ghi, lô và `TryConsume` trả `Rejected(WrongThread)`, còn `Evaluate` trả `NotReady` và ghi error (bản dev: assert).
   - **`Revision`** là uint64 tăng ở **mỗi** commit (kể cả từng commit trong một chuỗi cascade) và ở mỗi S2, S4, S6; **không bao giờ reset** trong vòng đời process, nên cache từ thế giới trước không bao giờ khớp nhầm.
   - Lá dựa vào provider (affinity, vật phẩm, thời gian) không được `Revision` bao. API cho biết tĩnh một điều kiện có cache theo `Revision` được không (mọi lá là `Flag` / `Quest`). **Nguồn của một lô effect đánh giá lại điều kiện của nút ngay lúc người chơi xác nhận**, trước khi chạy lô — không dùng kết quả cache lúc hiển thị.
   - **"Đã commit"**: trong handler `FlagChanged`, thấy trạng thái sau lô vừa commit, chưa có lệnh ghi đang chờ trong hàng; trong pha áp dụng của một lô (Rule 11), thấy trạng thái **trước** lô.

**Ghi một lần (Player Fantasy, F-2)**

9. **Sự kiện thay đổi.** `FlagChanged(khóa, cũ, mới, hệ ghi)` phát **sau khi cả lô commit**, theo thứ tự ghi, **chỉ khi giá trị thật sự đổi**; chỉ để thông báo. Mỗi handler chạy trong lớp bắt lỗi riêng (như Rule 11 của Game Time): handler ném exception → ghi log, các handler còn lại vẫn được gọi. **Không phát trong Restoring** và không phát khi trạng thái vùng được tính lại lúc nạp vùng. Lệnh ghi từ trong một handler **được xếp hàng FIFO** và chạy sau khi lượt phát hiện tại xong; người gọi nhận `Queued`, không phải `Ok` — lệnh bị từ chối lúc chạy chỉ được ghi error kèm handle và khóa. Kiểm trước và `once_key` của lô xếp hàng chạy **lúc lấy ra khỏi hàng**, không phải lúc xếp. Hàng luôn xả hết trước khi lệnh ghi ngoài cùng trả về.
   - **Trần cascade (F-6).** Chuỗi **có ít nhất một Counter đổi** sâu quá `EFFECT_CASCADE_MAX` thế hệ hoặc quá `EFFECT_CASCADE_TOTAL_MAX` lệnh ghi (F4) là lỗi: **chỉ các lệnh ghi Counter** còn trong hàng bị bỏ — lệnh Latch / Stage vẫn chạy — ghi error kèm chuỗi khóa. Thế hệ và tổng lệnh được đếm từ lệnh ghi ngoài cùng. Chuỗi chỉ gồm Latch / Stage tự dừng (chỉ phát khi giá trị đổi) và **không bao giờ bị cắt** — một latch mở khóa bị bỏ sẽ không bao giờ được thử lại (Rule 12). Validator đếm độ sâu chuỗi tĩnh từ dữ liệu author.
   - **Observer khác dịch vụ.** Đăng ký nghe `FlagChanged` không phải đăng ký dịch vụ (Rule 16): mở **mọi lúc** (scene nạp sau Sealed vẫn nghe được), trả một handle hủy được; **hủy có hiệu lực ngay** với các callback chưa gọi trong lượt hiện tại, đăng ký mới có hiệu lực từ lượt sau. Thứ tự gọi observer **không** phải bảo đảm, và không luật nào được dựa vào thứ tự ghi giữa hai namespace.
   - **Sự kiện vòng đời.** `WorldStateReady(revision)` phát sau S2 và S4; `WorldStateCleared` phát sau S5 và S6. Observer (nhật ký, HUD, Tutorial) xử lý chúng bằng cách **đọc lại toàn bộ** trạng thái — đây là cơ chế để "tin đồn đã lần theo vẫn được gạch trong nhật ký" sau khi nạp save.
   - **Vòng đời giữa lúc phát.** Yêu cầu S5 / S6 / nạp save đến trong lúc đang phát hay xả hàng: chờ handler hiện tại xong, **hủy cả hàng FIFO** chưa chạy (không commit, không phát), bỏ các handler còn lại của lượt, rồi chuyển trạng thái.
10. **Hệ tiêu thụ ghi latch lúc mở (F-2).** Khi một thứ lâu dài mở ra (nhánh vũ khí bí mật, công thức, vùng), **hệ sở hữu thứ đó** đánh giá điều kiện **đúng lúc mở** và **ghi latch của mình trong cùng bước** (vd. Weapon Tree ghi `weapon.<nhánh>.unlocked`). Từ đó, thứ đó được kiểm bằng latch, không bằng điều kiện gốc. **Nội dung lâu dài không bao giờ được chặn trực tiếp bằng một giá trị có thể giảm** (affinity, số vật phẩm, Counter); validator báo lỗi mọi điều kiện chặn nội dung lâu dài mà không đi qua một latch. Hệ quả: affinity giảm chỉ làm chậm hay đắt hơn những thứ *chưa* mở (Pillar 5), không bao giờ khóa lại thứ đã mở.
    - **`usage` của điều kiện (F-7).** Mỗi điều kiện khai báo `usage ∈ {Permanent, Transient}`. **Permanent** (chặn nội dung lâu dài, quyền vào vùng) chỉ cho lá **không bao giờ quay lại `false`**: `Flag` trên Latch **đúng dạng `== true`**, `≥` trên Stage, `Quest(q) ≥`, `Time(day ≥ N)`; cấm `Not`, cấm `==` / `≤` / `≠` trên Stage, cấm lá trên Counter, `Affinity`, `HasItem`, `Time(segment / IsNight / season / weekday)` — những thứ đó phải đi qua một latch do hệ tiêu thụ ghi. **Transient** (prompt, gợi ý, hội thoại thường ngày, nút đang hiển thị) cho mọi lá. Validator kiểm `usage`.

**Effect**

11. **Lô effect.** Effect (tập đóng: `SetFlag`, `GiveItem`, `TakeItem`, `AffinityDelta`, `GiveCurrency`, `StartQuest`) luôn chạy theo **lô** từ một nguồn (một nút hội thoại, một bước quest, một cảnh), mang writer handle của nguồn đó; lô dài ≤ `EFFECT_BATCH_MAX` (Formulas), danh sách phẳng, **không có effect có điều kiện** (điều kiện thuộc nút sở hữu lô). `SetFlag` trong lô chỉ ghi được namespace của nguồn; effect thuộc hệ khác do **handler** của hệ đó thực thi (`GiveItem` / `TakeItem` → Inventory, `AffinityDelta` → NPC Affinity, `GiveCurrency` → Currency & Shops, `StartQuest` → Quest).
    - **Kiểm trước, áp sau.** Mọi effect được kiểm trước (handle hợp lệ, có handler, `TakeItem` đủ hàng, `GiveItem` nhận được); **một effect không qua thì cả lô không áp dụng gì** và người gọi nhận lý do — `Rejected(NotReady | Stage8 | WrongThread | WrongHandle | NoHandler | HandlerRefused(handler, mã) | AlreadyApplied | Invalid)` — Dialogue có thể chọn câu thoại dự phòng. Lô vượt `EFFECT_BATCH_MAX` lọt vào catalog lúc chạy (dữ liệu sửa tay) được bộ nạp thay bằng lô luôn trả `Rejected(Invalid)` + error. Kiểm trước **cộng dồn** theo từng handler: mỗi handler nhận cả danh sách effect của mình trong lô (`CanApplyAll`), không kiểm từng effect riêng — hai `TakeItem` cùng vật phẩm hay nhiều `GiveItem` cùng làm tràn kho đều bị bắt. Qua hết thì áp theo thứ tự danh sách. Handler ném exception **sau** khi đã qua kiểm là bug: ghi error kèm ID lô (bản dev: assert), chạy tiếp các effect còn lại, không rollback; lô trả `Applied(WithErrors)`.
    - **`SetFlag` được dồn lại.** Các `SetFlag` của lô và `once_key` (Rule 12) được ghi vào kho **một lần**, sau effect cuối của pha áp dụng: `Revision` tăng một lần, rồi `FlagChanged` phát. Handler hỏi flag giữa pha áp dụng thấy trạng thái trước lô, nên thứ tự `SetFlag` trong danh sách không đổi kết quả. Lô không đổi gì thật (mọi `SetFlag` không đổi giá trị, không có effect không idempotent) thì không phát gì. **[P]** Inventory bảo đảm một `GiveItem` đã qua kiểm luôn thành công (xử lý tràn kho thuộc Inventory).
    - **Khi nào được chạy.** Lô và `SetFlag` bị từ chối (`Rejected(NotReady)`, không xếp hàng) ở mọi trạng thái khác Ready, và (`Rejected(Stage8)`) trong handler của sự kiện stage 8 của Game Time (`DayStarted` — chỉ để thông báo, Rule 12 của Game Time); được phép trong các phase stage 3–6 của pipeline chuyển ngày. Hệ này biết mình đang ở stage 8 qua một cờ "đang phát stage 8" mà Game Time công bố trên giao diện đồng hồ ở assembly hợp đồng — không tham chiếu Game Time **[P]** (Open Questions).
12. **Thưởng một lần (F-4).** `GiveItem`, `TakeItem`, `AffinityDelta`, `GiveCurrency` **không idempotent**; `SetFlag` thì có. Mỗi lô khai báo **`repeat ∈ {Once, Repeatable}`** (bắt buộc, không có mặc định — validator không tự đoán được "nội dung một lần"). Một lô có thể khai báo **`once_key`** — một Latch trong namespace của nguồn: lúc kiểm trước, nếu latch đã đặt thì cả lô trả `Rejected(AlreadyApplied)`; nếu áp dụng, latch được đặt **trong cùng lần commit**, **kể cả khi một handler lỗi sau kiểm trước** (Rule 11) — chấp nhận có thể mất một phần thưởng để không bao giờ phát thưởng hai lần. Validator yêu cầu `once_key` cho mọi lô `repeat = Once` chứa effect không idempotent (thưởng quest, quà khám phá); lô `Repeatable` (mua bán, quà tặng NPC lặp lại) phải khai báo rõ. **Effect không bao giờ được chạy lại khi nạp save** — trạng thái đã khôi phục là trạng thái sau effect.

**World State**

13. **Vật thể vùng.** Mỗi vật thể có trạng thái trong vùng có khóa **`object_id`** — ID ổn định, duy nhất toàn thế giới, **không bao giờ đổi sau khi phát hành** kể cả khi đổi tên hay dời sang vùng khác; `region_id` chỉ là chỉ mục trong catalog, **không** là một phần của khóa save (nếu là, dời rương sang vùng khác sẽ reset nó và phát lại thưởng). ID trùng hoặc thiếu **làm fail build** (validator); ID đã gỡ được giữ trong danh sách tombstone và không bao giờ được cấp cho vật thể khác. Hai loại (tập đóng):
    - **OneTime** (rương, bí mật, cửa phong ấn): một latch; mở rồi là mở mãi.
    - **Renewable** (điểm thu hoạch, nhóm quái): lưu `last_consumed_minute` (`game_minute` của Game Time); **có sẵn lại khi** sang đủ `renew_days` ngày lịch (F-renew, Formulas) — theo lịch, **không bao giờ theo việc nạp lại**. **[P]** `renew_days` của từng loại vật thể do GDD Farming / Enemy AI / level design chốt trong dải ở Tuning Knobs.
    - **Tính lười, không có phase.** Tình trạng có sẵn được tính **khi vùng nạp**, dưới token của Region Loading (Game Time Rule 17), bằng công thức đóng — một khoảng trống 100 ngày tốn như 1 ngày. **Region Loading chụp `now` một lần** (gọi là `region_now`) và truyền cùng giá trị vào mọi `IsAvailable` của lần tính đó (nạp vùng hoặc hỏi lại sau ngủ), rồi công bố `region_now` cho các hệ tiêu thụ của vùng **[P]**. Hệ này **không đăng ký phase** vào pipeline chuyển ngày, nên rollover tại chỗ không bao giờ đổi vật thể của vùng đang active (Game Time Rule 12). Độ trễ âm (timestamp > đồng hồ) được kẹp **lúc khôi phục** (F1).
    - **Ngủ trong vùng (F-5).** Truy vấn `IsAvailable(object_id, now)` là hàm **thuần** (F1), không có trạng thái. Sau khi người chơi ngủ sang ngày mới mà vẫn ở trong vùng, **Region Loading hỏi lại** mọi vật thể Renewable của vùng trong lúc màn hình đen — qua một phase stage 3 (World) của **Region Loading** chỉ chạy khi loại chuyển ngày là `Sleep` (Game Time Rule 12: khi ngủ các phase áp dụng bình thường, che bằng fade) — nên ruộng cạnh giường có lại vào buổi sáng. Hệ này vẫn không có phase. Rollover **không** do ngủ (thức qua 06:00) vẫn không làm hồi tại chỗ. **[P]** GDD Region Loading xác nhận.
    - **API truy vấn riêng (F-3).** Region Loading, Enemy AI và Interaction hỏi "vật thể có sẵn không?" qua `IsAvailable` và **tiêu thụ** qua một lệnh nguyên tử `TryConsume(object_id, handle, region_now) → Consumed | Rejected(AlreadyConsumed | NotAvailable | UnknownObject | WrongOwner | WrongThread)` — không tách "hỏi rồi báo đã dùng", nên hai nguồn cùng frame không tiêu thụ hai lần.
      - `TryConsume` tính tình trạng theo **`region_now`**, không đọc đồng hồ hiện tại — nên rollover tại chỗ không làm vật thể dùng lại được. Bản ghi lưu `last_consumed_minute` = **đồng hồ hiện tại** và `renew_days_at_consume`.
      - `AlreadyConsumed` = OneTime đã dùng, hoặc Renewable đã bị dùng trong cùng ngày lịch với `region_now` hay sau đó; `NotAvailable` = Renewable chưa đủ `renew_eff` (F1) tính theo `region_now`. Lần bị từ chối **không** ghi đè `last_consumed_minute`. **Thưởng chỉ trao khi `Consumed`**, và lô thưởng chạy **trong cùng call stack** ngay sau đó, nên vùng unload không thể chen vào giữa "đã đánh dấu" và "đã trao". Nhóm quái được tiêu thụ khi **hạ con cuối**, không phải khi bắt đầu giao chiến. Tất cả **không qua lá Condition** — tập lá giữ đóng. Quyền **vào vùng** (Pillar 3) vẫn là lá `Flag` trên `boss.*`. **[P]** Mỗi loại vật thể khai báo hệ được báo "đã dùng" nó (vd. rương → Interaction; nhóm quái → Enemy AI).
14. **`world_seed`.** Một số nguyên không dấu 32 bit, **đặt một lần khi tạo game mới**, không bao giờ đổi hay sinh lại khi nạp; chỉ đọc với mọi hệ khác. Dùng cho random theo ngày `hash(world_seed, day_index, system_id, entity_id)` (Game Time › Edge Cases › reroll); helper dùng chung: OQ-7 của Game Time.

**Lưu, khôi phục, khởi động**

15. **DTO save.** Gồm `schema_version`, `world_seed`, các flag **khác giá trị mặc định** (khóa, kiểu, giá trị), và các vật thể vùng có trạng thái (loại, trạng thái, `last_consumed_minute`, `renew_days_at_consume`). Thứ tự phần tử ổn định để save diff được.
    - **Chụp ở điểm yên tĩnh.** Snapshot chỉ được lấy khi lệnh ghi ngoài cùng đã trả về và hàng FIFO rỗng (autosave lúc boss ngã chờ cascade xong); `Snapshot()` gọi khi hàng không rỗng hay đang phát trả `Rejected(NotQuiescent)` và Save/Load thử lại sau khi lệnh ngoài cùng trả về; gọi khi không ở Ready trả `Rejected(NotReady)`. **[P]** Save/Load chụp mọi hệ ở cùng điểm đó — nếu không, `once_key` đã đặt mà Inventory chụp trước `GiveItem` sẽ mất thưởng.
    - **Khôi phục im lặng:** ghi giá trị trực tiếp — không phát `FlagChanged`, không chạy effect, không kiểm đơn điệu (khôi phục là nguồn sự thật).
    - **Khóa lạ** (flag hay vật thể bị gỡ trong một bản cập nhật) được giữ trong một **vùng mồ côi** và ghi ra lại nguyên vẹn — không bao giờ bị xóa âm thầm.
    - Lệch kiểu đi qua migration có version (Game Time Rule 22); migration thất bại → nạp thất bại (States S5). Khi chưa có migration (v1): Latch ↔ Stage đổi không mất thông tin (`true` ↔ `≥ 1`); lệch kiểu khác dùng mặc định và giữ giá trị gốc trong vùng mồ côi (không S5). Counter ngoài `[min, max]` mới được kẹp lúc khôi phục (warn). Stage khôi phục **đúng giá trị DTO**, kể cả khi nhỏ hơn `default` mới hay lớn hơn bước cao nhất mà dữ liệu biết; chỉ Stage **âm** (DTO hỏng) được sửa về `default` + warn theo mục, không S5. `last_consumed_minute` lớn hơn đồng hồ đã khôi phục được kẹp về đồng hồ (F1), log một lần mỗi mục; âm thì sửa về 0.
    - **Nạp thất bại (S5)** khi: DTO không đọc được; `schema_version` mới hơn bản đang chạy; thiếu hay hỏng `world_seed` (sinh lại sẽ đổi hình dạng vùng). Khóa trùng trong DTO không làm thất bại: Latch lấy OR, Stage lấy max, Counter lấy mục cuối, ghi log — thiên về giữ lại.
    - **Thứ tự khôi phục:** Game Time → **hệ này** → Inventory → … (Game Time Rule 16).
16. **Khởi động.** Hệ được tạo bởi composition root, **không phải static singleton** (TD-C). Mọi **dịch vụ** — provider, handler và namespace — được đăng ký trong cửa sổ **Registering**; sau khi niêm phong (Sealed), đăng ký dịch vụ mới bị từ chối và lá của provider đó là `Unknown` suốt phiên. Observer `FlagChanged` không phải dịch vụ (Rule 9). S1 chỉ xảy ra khi catalog đã nạp đủ và qua kiểm tra lúc chạy; nếu không, `Seal()` trả lỗi cho composition root, kho **ở lại Registering** (ghi error), và game không vào menu chính.

**Phạm vi xây dựng ở Fun-Gate**

| Nhóm | Nội dung |
|---|---|
| **Build now** | Bool + Int; Latch, Stage, Counter; namespace + writer handle; cây điều kiện với lá `Flag`, `Time` (day, segment, IsNight), `HasItem`, `Affinity`; effect `SetFlag`, `GiveItem` / `TakeItem`, `AffinityDelta` với kiểm trước + `once_key`; vật thể OneTime + Renewable; `world_seed`; DTO v1; vòng đời; validator |
| **Stub** | lá `Quest`, effect `StartQuest`, `GiveCurrency` (thiếu handler → kiểm trước thất bại; validator cấm dùng trong dữ liệu Fun-Gate) |
| **Defer** | lá `Time` dạng `season` / `weekday` (mùa tắt tới Vertical Slice, Game Time Rule 20); migration sau v1; hệ tiêu thụ Tutorial, Cutscene |

### States and Transitions

Vòng đời của kho (độc lập với Game Time, nhưng khôi phục sau đồng hồ — Rule 15):

| Trạng thái | Đánh giá điều kiện | Ghi / effect | Đăng ký |
|---|---|---|---|
| **Registering** | Không | Không | Có |
| **Sealed** (không có dữ liệu) | Không (`NotReady` + error) | Không | Không |
| **Restoring** | Không | Chỉ khôi phục | Không |
| **Ready** | Có | Có | Không |

| # | Từ | Sang | Kích hoạt |
|---|---|---|---|
| S1 | Registering | Sealed | Boot xong (mọi hệ đã đăng ký, catalog nạp đủ và hợp lệ) |
| S2 | Sealed | Ready | Game mới: sinh `world_seed` **mới**, mọi flag ở mặc định, không có vật thể vùng nào được ghi |
| S3 | Sealed | Restoring | Nạp save (catalog thiếu hay lỗi → S5 ngay, không khôi phục) |
| S4 | Restoring | Ready | Save/Load báo **mọi** saveable đã khôi phục xong (không phải khi riêng phần của hệ này xong); `WorldStateReady` phát sau đó, nên observer dựng lại nhật ký đọc được cả `HasItem` |
| S5 | Restoring | Sealed | Nạp thất bại (Rule 15): xóa dữ liệu, không giữ trạng thái nửa vời; **báo lên Save/Load** để mọi hệ đã khôi phục trước (Game Time) cũng về trạng thái trống; không ghi đè file save hỏng |
| S6 | Ready | Sealed | Về menu chính, hoặc nạp save từ menu (rồi S3): xóa dữ liệu; **giữ** đăng ký (provider, handler, namespace là dịch vụ, không phải dữ liệu) |

**"Xóa dữ liệu"** (S2 trước khi bắt đầu, S5, S6) nghĩa là xóa **toàn bộ**: flag, vật thể vùng, `world_seed`, vùng mồ côi, hàng FIFO và các tập "cảnh báo một lần mỗi phiên" — để mồ côi của save trước không rò vào game mới. `Revision` thì không reset (Rule 8).

**Chuyển tiếp bị cấm:** Sealed → Registering (không mở lại cửa sổ đăng ký); bất kỳ → Ready mà không qua S2 hay S4; giảm một Latch hay Stage ngoài Restoring.

### Interactions with Other Systems

*calls* = hệ kia gọi API của hệ này; *registers* = hệ kia đăng ký provider / handler / namespace; *observes* = hệ kia nghe `FlagChanged`. Hệ này không tham chiếu hệ nào ở trên nó. Mọi hệ dưới đây trừ Game Time chưa có GDD — hợp đồng là **tạm thời**.

| Hệ | Vào hệ này | Ra từ hệ này | Kiểu | Chủ giao diện |
|---|---|---|---|---|
| Game Time & Calendar | provider lá `Time`; `game_minute` / `day_index` (qua giao diện đồng hồ ở assembly hợp đồng) | `world_seed` | registers | Hệ này (lá); Game Time (đồng hồ) |
| Save/Load | lời gọi khôi phục | DTO có version | registers (saveable) | Save/Load |
| Game Data & Tuning Config | khai báo flag, ID ổn định, dữ liệu điều kiện / lô effect, bảng ID vật thể | — | hệ này đọc | Game Data (ID, TD-B4); hệ này (schema flag) |
| Region Loading | truy vấn vật thể khi nạp vùng | tình trạng có sẵn; lá `Flag` trên `boss.*` cho quyền vào vùng | calls | Hệ này |
| Interaction & Context Prompts | ghi `secret.*` **[P]**; báo "đã dùng" rương / điểm thu hoạch **[P]** | điều kiện cho prompt | calls + registers (namespace) | Hệ này |
| Weapon Tree | ghi `weapon.*` **[P]** | điều kiện trần tier, nhánh bí mật | calls + registers | Hệ này |
| Crafting & Forge | ghi `recipe.*` **[P]** | điều kiện mở công thức | calls + registers | Hệ này |
| Enemy AI, Spawning & Drops | báo "đã dùng" nhóm quái **[P]** | tình trạng có sẵn; `world_seed` | calls | Hệ này |
| Boss Encounter | ghi `boss.*` (Stage) | — | calls + registers | Hệ này |
| Defeat & Retry | — (túi đồ rơi thuộc Defeat & Retry **[P]**) | — | — | Defeat & Retry |
| NPC Affinity | provider lá `Affinity`; handler `AffinityDelta` | — | registers | Hệ này (hợp đồng); Affinity (giá trị, ngưỡng N) |
| Item & Inventory | provider lá `HasItem`; handler `GiveItem` / `TakeItem` | — | registers | Hệ này (hợp đồng); Inventory (vật phẩm, tràn kho) |
| Dialogue & Discovery Hints | ghi `rumor.*`; lô effect | điều kiện cho nút hội thoại, gợi ý | calls + registers | Hệ này |
| Quest & Hidden Quest | ghi `quest.*` (Stage); handler `StartQuest` | điều kiện bước quest | calls + registers | Hệ này |
| Cutscene & Scripted Sequence | ghi `cutscene.*` **[P]**; lô effect | điều kiện kích hoạt | calls + registers | Hệ này |
| Tutorial & Onboarding | ghi `tutorial.*` **[P]** | `FlagChanged` | observes + calls | Hệ này |
| Currency & Shops | handler `GiveCurrency` | điều kiện mở hàng | registers | Hệ này |
| HUD / nhật ký | — | `FlagChanged`; đọc flag | observes | Hệ này |

**Rủi ro phụ thuộc → ADR:**
- **Hệ này ↔ Save/Load ↔ Game Time:** đăng ký hai chiều; dùng **assembly hợp đồng trung lập** (giao diện provider / handler, ID, DTO) để asmdef không vòng (TD-C, OQ-4 của Game Time).
- **Định dạng author điều kiện và lô effect** (asset trong editor hay file dữ liệu), chính sách đổi tên kiểu: ADR; GDD chỉ yêu cầu dữ liệu **kiểm được không cần mở editor** (validator chạy trong CI).

## Formulas

*Nguồn: systems-designer, 2026-10-27, dựa trên Section C. Quyết định của người dùng cùng ngày: hồi theo **ngày lịch** (F1), nhóm quái mặc định **3 ngày**, trần cascade **8 thế hệ + tổng 64 lệnh ghi** (F4). Mọi công thức là số nguyên. `day_index` là F2 của Game Time & Calendar (`floor(game_minute / 1440)`, ranh giới ngày 06:00).*

### F1. Renewable availability (Rule 13)

The renewable_available formula is defined as:

`last′ = min(last_consumed_minute, now)` *(độ trễ âm coi là 0. `IsAvailable` chỉ tính `min()` — không ghi, không log. Việc **lưu** `last′` và log diễn ra một lần **lúc khôi phục**, khi đồng hồ đã được khôi phục trước: `last_consumed_minute > game_minute` → lưu `last′`, log một lần mỗi mục (Rule 15))*
`renew_eff = min(renew_days_at_consume, renew_days)`
`available = (rec = ∅) ∨ (day_index(now) − day_index(last′) ≥ renew_eff)`

**Variables:**
| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `now` | int | 0 – `MAX_GAME_MINUTE` (2.147.480.767) | calculated (Game Time) | `game_minute` lúc vùng nạp |
| `last_consumed_minute` | int | 0 – `MAX_GAME_MINUTE` | calculated (save) | Thời điểm vật thể bị dùng |
| `rec` | bản ghi | ∅ / có | calculated | ∅ = chưa từng dùng (không có trong DTO). **Không dùng 0 hay −1 làm giá trị canh** — phút 0 là thời điểm hợp lệ |
| `renew_days` | int (ngày) | 1 – 28; mặc định điểm thu hoạch **1**, nhóm quái **3** [P] | data file (theo loại vật thể) | Số ranh giới 06:00 phải đi qua |
| `renew_days_at_consume` | int (ngày) | 1 – 28 | calculated (save) | `renew_days` của loại vật thể lúc bị tiêu thụ; patch **rút ngắn** có hiệu lực ngay với save cũ, patch **kéo dài** không phạt timer đang chạy |
| `renew_eff` | int (ngày) | 1 – 28 | calculated | Ngưỡng thực dùng |
| `available` | bool | | calculated | Vật thể có sẵn không |

**Hồi theo lịch, không theo phút đã trôi.** Vật thể hồi đúng ở ranh giới 06:00, nên người chơi đoán trước được ("sáng mai sẽ có lại"). Tính theo phút đã trôi (`now − last ≥ renew_days · 1440`) sẽ cho hồi vào giờ lẻ (vd. 20:37) và **phạt việc ngủ**: dùng lúc 20:00, ngủ dậy vẫn chưa có. Ngủ cũng sang ngày (F6 của Game Time), nhưng mỗi lần chỉ qua **đúng một** ranh giới và người chơi bỏ lại phần còn lại của buổi tối — tốc độ hồi tối đa vẫn là một lần mỗi `renew_days` ngày dù chơi thế nào, nên ngủ ở đây là vòng lặp ngày mong muốn, không phải mẹo. Nạp lại save không đổi `day_index`, nên **không bao giờ** làm vật thể hồi (Player Fantasy › design test phụ).

**Output Range:** bool. Phép so sánh trên số ngày (hiệu ∈ [0; 1.491.306]), không cộng phút, nên không tràn int32. `renew_days = 0` nghĩa là hồi mỗi lần nạp vùng — trái Player Fantasy, validator từ chối. `renew_days > 28` (dài hơn mùa dài nhất) nên là OneTime, validator từ chối. Biên: dùng lúc 05:59 với `renew_days = 1` thì 1 phút game sau đã có lại — chấp nhận, vì đêm thuộc ngày N (Game Time Rule 9). Đồng hồ bão hòa ở `MAX_GAME_MINUTE` thì ngày đứng yên, vật thể không hồi nữa (~26.600 năm game, không đạt được). Rollover khi đang đứng trong vùng **không** làm vật thể hồi — tính lại chỉ ở lần nạp vùng kế tiếp (Rule 13).

**Example:** Thu hoạch lúc 02:30 đêm Ngày 1 (`last = 1230`, ngày 0), `renew_days = 1`; nạp vùng lúc 07:00 Ngày 2 (`now = 1500`, ngày 1): `1 − 0 = 1 ≥ 1` → **có**. Hạ nhóm quái tại 1500 (ngày 1), `renew_days = 3`: tại `now = 5000` (ngày 3) → `2 < 3` → **không**; tại 5760 (ngày 4) → **có**. Timestamp hỏng `last = 9000` khi `now = 5000`: kẹp `last′ = 5000` + log → nhóm quái có lại từ ngày 6.

### F2. Condition resolution — ba giá trị (Rule 7)

The condition_resolution formula is defined as (logic Kleene mạnh):

`All = F nếu có con F; ngược lại U nếu có con U; ngược lại T`
`Any = T nếu có con T; ngược lại U nếu có con U; ngược lại F`
`Not(T) = F, Not(F) = T, Not(U) = U`
`result = (root == T)`

**Variables:**
| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| giá trị lá | enum | {T, F, U} | calculated (kho flag / provider) | U = lá không có provider (Rule 7) |
| `root` | enum | {T, F, U} | calculated | Giá trị của nút gốc |
| `result` | bool | | calculated | Kết quả trả cho người hỏi; U → `false` (đóng an toàn) |

| A | B | All | Any | Not(A) |
|---|---|---|---|---|
| T | T | T | T | F |
| T | F | F | T | F |
| T | U | U | T | F |
| F | F | F | F | T |
| F | U | F | U | T |
| U | U | U | U | U |

(Đối xứng theo A, B.)

**Output Range:** bool. Được phép đánh giá ngắt sớm từ trái sang phải (F hút mọi giá trị trong `All`, T hút mọi giá trị trong `Any`), kết quả trùng với đánh giá đầy đủ. Thoái hóa: `Not(lá thiếu provider)` cho U rồi `false` — đóng an toàn xảy ra **ở gốc**, nên `Not` không bao giờ lật một U thành `true`.

**Example:** `All[Flag(boss.forest ≥ 2) = T, Affinity(mira) ≥ 40 = U]` → U → **false**. `Any[T, U]` → **true**.

### F3. Counter write (Rule 3)

The counter_write formula is defined as:

`raw = (op = Add) ? (int64) old + delta : v`
`new = clamp(raw, min, max)`

`FlagChanged` chỉ phát khi `new ≠ old`. Với **Stage**: `Add` có `delta < 0` hoặc `Set` có `v < old` → `Rejected(Monotonic)`; `Add` bão hòa ở `int32.Max` theo Rule 3 (chạm từ dưới → `Ok` + warn; đã ở trần → `Ok(NoChange)`, warn một lần mỗi khóa mỗi phiên).

**Variables:**
| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `old`, `v`, `delta` | int32 | ±2.147.483.647 | calculated | Giá trị cũ, giá trị đặt, lượng cộng |
| `min`, `max` | int32 | `min ≤ default ≤ max` | data file (catalog) | Dải của Counter |
| `raw` | int64 | [−2³², 2³² − 2] | calculated | Trung gian, không thể tràn |
| `new` | int32 | [min, max] | calculated | Giá trị ghi |

**Output Range:** luôn trong [min, max]. Bị kẹp là hành vi bình thường, không phải lỗi (chỉ log mức debug). Thoái hóa: `min = max` biến flag thành hằng — validator cảnh báo. `AffinityDelta` **không** phải Counter: NPC Affinity sở hữu dải và cách kẹp của mình.

**Example:** dải [0, 10]: `old = 8`, Add +5 → 13 → **10**; `old = 3`, Add −20 → −17 → **0**; `old = 2.147.483.000`, Add 1.000 → `raw` 2.147.484.000 (int64) → kẹp về `max`.

### F4. Giới hạn và chi phí đánh giá

| Hằng | Giá trị | Lý do |
|---|---|---|
| `COND_MAX_DEPTH` | **4** (gốc = 1, tính cả lá) | Đủ cho dạng chuẩn tuyển có phủ định `Any[All[Not(lá)]]` — mọi biểu thức logic đều viết được; cần sâu hơn thì tách thành latch (Rule 10) |
| `COND_MAX_LEAVES` | **16** | Điển hình 1–4 lá |
| `EFFECT_BATCH_MAX` | **16** | Thưởng quest điển hình 4–8 effect |
| `EFFECT_CASCADE_MAX` | **8** thế hệ FIFO | Chỉ áp cho chuỗi có Counter đổi (F-6). Latch và Stage tự dừng (chỉ phát khi giá trị đổi); giới hạn này bắt vòng Counter ↔ Counter |
| `EFFECT_CASCADE_TOTAL_MAX` | **64** lệnh ghi mỗi chuỗi | Chỉ áp cho chuỗi có Counter đổi (F-6). Chặn trường hợp một thế hệ sinh quá nhiều lệnh ghi |

Cả năm là **hằng của validator / runtime**, không phải knob trong data file: đổi chúng là đổi hợp đồng hiệu năng và test (sửa GDD). Vượt `EFFECT_CASCADE_MAX` hoặc `EFFECT_CASCADE_TOTAL_MAX` lúc chạy: phần còn lại của chuỗi bị bỏ, ghi error (Rule 9).

The poll_cost formula is defined as:

`C_poll = Σᵢ Lᵢ · c_leaf ≤ N_cond · COND_MAX_LEAVES · c_max`

**Variables:**
| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `N_cond` | int | 5 – 20 | calculated (hành vi người chơi, ước tính) | Số điều kiện được hỏi trong một lượt (vd. các prompt tương tác quanh người chơi) |
| `Lᵢ` | int | 1 – `COND_MAX_LEAVES` | calculated | Số lá của điều kiện i |
| `c_leaf`, `c_max` | thời gian | `c_max ≤ 0,5 µs` **[P]** | constant (ngân sách) | Chi phí một lá; lá `Flag` là một lần đọc mảng theo ID, provider O(1), **0 byte GC** mỗi lần đánh giá **trên đường bình thường** (kể cả `NotReady`, `Unknown` do thiếu provider); đường lỗi provider ném exception được miễn |
| `C_poll` | thời gian | | calculated | Chi phí một lượt hỏi |

**Output Range:** xấu nhất 20 × 16 = 320 lá/lượt; ở 10 Hz = 3.200 lá/s → **≤ 0,16 ms mỗi lượt**; điển hình 20 × 3 lá ≈ 0,03 ms. Ngân sách `c_max` là đề xuất, chờ technical-director xác nhận (Open Questions).

**Example:** 12 prompt, mỗi cái 3 lá, `c_leaf = 0,3 µs` → `C_poll = 36 · 0,3 = 10,8 µs`.

### F5. `world_seed` (Rule 14)

`world_seed` là uint32, dải [0; 2³² − 1], phân bố đều, lấy từ entropy của hệ điều hành ở S2 (game mới); 0 là hợp lệ; test truyền seed cố định. Đầu vào của hash theo ngày: `day_index` [0; 1.491.306], `system_id` uint16 (bảng ID của Game Data), `entity_id` int32 (ID ổn định); đầu ra uint32. Thuật toán hash và helper `DaySeed` thuộc **OQ-7 của Game Time**; ở đây chỉ chốt dải.

### Ràng buộc validator (dữ liệu author)

`renew_days ∈ [1, 28]` · Counter: `min ≤ default ≤ max`, đều int32 · lá `Flag` trên Bool chỉ `==` / `≠` với `true` / `false`; hằng `v` của lá trên Counter nằm trong [min, max] (ngoài dải thì kết quả luôn cố định) · cây điều kiện không có `All` / `Any` rỗng, độ sâu ≤ 4, số lá ≤ 16 · lô effect ≤ 16 effect, `once_key` bắt buộc theo Rule 12 · mọi `object_id` duy nhất toàn thế giới (không theo vùng) và không trùng ID tombstone · mọi lô khai báo `repeat` · mọi khóa flag trong điều kiện / lô đã khai báo, đúng kiểu, và `SetFlag` chỉ ghi namespace của nguồn · nội dung lâu dài không bị chặn trực tiếp bằng giá trị có thể giảm (Rule 10) · mỗi điều kiện có `usage`, và điều kiện Permanent chỉ dùng lá đơn điệu (Rule 10) · khóa đúng định dạng (Rule 1); Latch mặc định `false`, Stage mặc định `≥ 0` · `HasItem ≥ n` với `n ≥ 1`; ngưỡng `Affinity` trong dải của NPC Affinity · lá có loại mà build profile không có provider bị từ chối (vd. `Affinity` khi build chưa có NPC Affinity) · một lô không có hai `SetFlag` cùng khóa, không vừa `GiveItem` vừa `TakeItem` cùng vật phẩm · độ sâu chuỗi cascade tĩnh có Counter ≤ 8 (Rule 9) · **so với baseline đã phát hành:** không đổi `default`, loại, kiểu của flag, không đổi OneTime ↔ Renewable của vật thể, không tái dùng ID tombstone — trừ khi có migration đi kèm. Validator chạy **không cần mở editor** và **chặn build** trong CI. Bộ nạp catalog lúc chạy kiểm lại rẻ các giới hạn cây; cây vi phạm được thay bằng hằng `Unknown` (→ `false`) và ghi error.

## Edge Cases

*Nguồn: systems-designer, 2026-10-27 (44 trường hợp, 12 chỗ mâu thuẫn trong Section C/D). Người dùng chọn **sửa thẳng luật** (xem ghi chú sửa đổi đầu Detailed Design), nên mỗi mục dưới đây nêu kết quả và trỏ về luật quy định nó — luật là nguồn sự thật, mục này là danh sách kiểm. Thứ tự trong từng nhóm: nguy cơ bug thật / phá "Thần nhớ tên bạn" trước.*

### Kho flag và lệnh ghi

- **Nếu lá `Flag` trỏ tới khóa chưa khai báo lúc chạy** (flag bị gỡ trong patch, gõ sai lọt qua): lá trả `Unknown`, gốc ra `false`, ghi error một lần mỗi khóa mỗi phiên. `Not(Flag(khóa_sai))` **không** mở nội dung. (Rule 4, F2)
- **Nếu một patch đổi `default`, loại hay kiểu của một flag đã phát hành**: validator so với baseline và chặn build trừ khi có migration đi kèm. Lý do: DTO chỉ lưu giá trị khác mặc định, nên đổi mặc định sẽ âm thầm đổi giá trị của mọi save cũ. (Rule 15, validator)
- **Nếu ghi một giá trị bằng giá trị hiện tại** (Latch `true` → `true`, `Add` 0, Stage đặt lại bằng chính nó, Stage đã ở `int32.Max`): `Ok(NoChange)` — không lỗi, không `FlagChanged`, không tăng `Revision`. (Rule 3)
- **Nếu code ghi cùng một khóa hai lần trong một lô** (dữ liệu author bị validator chặn): kho thấy một lần ghi với giá trị cuối; `FlagChanged` phát một lần với `cũ` = trước lô, `mới` = cuối lô, và không phát nếu hai giá trị bằng nhau (0 → 5 → 0). (Rule 11)
- **Nếu lệnh ghi có handle hợp lệ nhưng khóa thuộc namespace khác** (`weapon.x` ghi bằng handle của `quest`): `Rejected(WrongHandle)`, không đổi gì, error, bản dev assert. Namespace là đoạn trước dấu `.` đầu tiên. (Rule 1, Rule 5)
- **Nếu đọc flag khi kho không ở Ready** (Inventory khôi phục sau hệ này vô tình đọc trong lúc Restoring): trả giá trị mặc định, error một lần mỗi lần vào trạng thái; không bao giờ trả giá trị khôi phục dở dang. (Rule 4)
- **Nếu hai hệ ghi hai namespace khác nhau trong cùng frame**: không có xung đột (mỗi namespace một hệ ghi). Thứ tự giữa chúng không được là một phần của luật nào. (Rule 5, Rule 9)

### Condition và provider

- **Nếu điều kiện chặn nội dung lâu dài dùng lá có thể quay lại `false`** (`Flag(boss.forest == 2)` khi boss lên stage 3, `Not(Latch)`, `Time(IsNight)`): điều kiện đó phải khai báo `usage = Permanent` và validator chặn build. Chỉ điều kiện `Transient` (prompt, gợi ý, hội thoại thường ngày) được dùng các lá đó. (Rule 10, F-7)
- **Nếu một hệ hỏi điều kiện khi kho không ở Ready** (HUD hỏi 10 Hz trong lúc đang ở menu): trả `NotReady`, không phải `false`, nên logic "hiện gợi ý nếu chưa mở" không bắn nhầm; error một lần mỗi lần vào trạng thái, không phải mỗi lần hỏi. (Rule 8)
- **Nếu hệ tiêu thụ cache kết quả theo `Revision` qua một lần game mới hay nạp save**: cache không bao giờ khớp nhầm, vì `Revision` không reset và tăng ở mỗi S2 / S4 / S6. (Rule 8)
- **Nếu affinity hay số vật phẩm đổi giữa lúc nút hội thoại được hiển thị và lúc người chơi bấm**: nguồn đánh giá lại điều kiện của nút ngay lúc xác nhận; nếu đã sai thì không chạy lô, và nút đóng ở lần hỏi kế. Điều kiện có lá provider không cache được theo `Revision`. (Rule 8)
- **Nếu provider ném exception, hay nhận ID NPC / vật phẩm không tồn tại**: lá `Unknown` (không phải 0), exception bị bắt, error một lần mỗi loại lá mỗi phiên. (Rule 7)
- **Nếu một hệ cố đăng ký provider sau Sealed**: bị từ chối, lá đó `Unknown` suốt phiên. **Nếu dữ liệu dùng lá mà build profile không có provider** (lá `Affinity` trong một build chưa có NPC Affinity): validator chặn build — không để `Unknown` âm thầm đóng nội dung vĩnh viễn. (Rule 16, validator)
- **Nếu handler hay provider hỏi điều kiện giữa pha áp dụng của một lô**: thấy trạng thái trước lô. **Nếu hỏi trong handler `FlagChanged`**: thấy trạng thái sau lô vừa commit, chưa có lệnh ghi đang chờ trong hàng. (Rule 8)
- **Nếu cây điều kiện trong catalog lúc chạy vượt giới hạn** (dữ liệu sửa tay, bỏ qua CI): bộ nạp thay cả cây bằng hằng `Unknown` (→ `false`), ghi error; không đệ quy vô hạn, không crash. (validator)
- **Nếu lá `Time` được hỏi giữa các stage 3–6 của pipeline chuyển ngày**: thấy **ngày mới** — `day_index` mới, khoảng giờ Morning, `IsNight = false` — vì Game Time đặt đồng hồ về mốc 06:00 ở stage 2, trước mọi phase (Game Time Rule 12).

### Effect và `once_key`

- **Nếu cùng một lô được xếp hàng hai lần** (hai handler cùng ghi flag kích hoạt nó): kiểm trước và `once_key` chạy lúc lấy ra khỏi hàng; lần thứ hai trả `Rejected(AlreadyApplied)`. Thưởng không bao giờ bị nhân đôi. (Rule 9, Rule 12)
- **Nếu một lô có hai `TakeItem` cùng vật phẩm, hay nhiều `GiveItem` mà cộng lại làm tràn kho**: kiểm trước cộng dồn (`CanApplyAll`) bắt được, cả lô không áp dụng gì. Lô vừa cho vừa lấy cùng vật phẩm bị validator chặn. (Rule 11)
- **Nếu handler ném exception sau khi lô đã qua kiểm trước**: các effect còn lại vẫn chạy, `SetFlag` và `once_key` vẫn commit, lô trả `Applied(WithErrors)`, error kèm ID lô, bản dev assert. Người chơi có thể mất một phần thưởng nhưng không bao giờ nhận hai lần. (Rule 11, Rule 12)
- **Nếu lô ghi chính flag mà điều kiện của nút sở hữu nó đang đọc** (nút "nhận thưởng" có `Quest(x) == 1` ghi `quest.x → 2`): hợp lệ. Điều kiện được đánh giá trước lô; nút tự đóng ở lần hỏi kế. Nguồn không dựng lại danh sách nút đang duyệt cho tới khi hết phát sự kiện. (Rule 8, Rule 11)
- **Nếu chuỗi cascade chỉ gồm Latch / Stage dài hơn 8 thế hệ**: không bị cắt — mỗi khóa chỉ đổi được một số lần hữu hạn nên chuỗi tự dừng. **Nếu chuỗi có Counter đổi vượt 8 thế hệ hay 64 lệnh ghi**: phần còn lại bị bỏ, error kèm chuỗi khóa. (Rule 9, F4, F-6)
- **Nếu một lệnh ghi được xếp hàng rồi bị từ chối lúc chạy** (đơn điệu, sai handle): người gọi đã nhận `Queued` nên không biết; chỉ có error kèm handle và khóa. Hệ không được coi `Queued` là "đã ghi". (Rule 9)
- **Nếu lô hay `SetFlag` đến khi không ở Ready, hay trong handler `DayStarted` (stage 8)**: `Rejected(NotReady)` / `Rejected(Stage8)`, không xếp hàng. Hàng luôn xả hết trước khi lệnh ngoài cùng trả về, nên không cascade nào "sống sót" sang stage 8. (Rule 9, Rule 11)
- **Nếu một observer `FlagChanged` bị hủy (đóng scene) giữa lúc đang phát**: hủy có hiệu lực ngay, callback chưa gọi của nó không được gọi; không gọi vào đối tượng đã hủy. (Rule 9)

### Vật thể vùng

- **Nếu hai nguồn cùng tiêu thụ một vật thể trong cùng frame**: `TryConsume` nguyên tử — lần đầu `Consumed`, lần sau `Rejected(AlreadyConsumed)`, `last_consumed_minute` không bị ghi đè (nếu ghi đè sẽ kéo dài thời gian hồi). (Rule 13)
- **Nếu vùng unload (dịch chuyển nhanh, thua, thoát) giữa lúc đang mở rương hay thu hoạch**: chưa `Consumed` thì vật thể còn nguyên; đã `Consumed` thì thưởng đã vào túi trong cùng call stack. Không có trạng thái "đã đánh dấu mà chưa trao". Hoạt ảnh có thể bị bỏ qua. (Rule 13)
- **Nếu người chơi rời vùng giữa trận với một nhóm quái**: nhóm chưa bị tiêu thụ (chỉ tiêu thụ khi hạ con cuối) và có nguyên vẹn ở lần nạp vùng kế. (Rule 13)
- **Nếu người chơi ngủ sang ngày mới mà vẫn trong vùng**: Region Loading hỏi lại `IsAvailable` trong lúc màn hình đen; ruộng cạnh giường có lại vào buổi sáng. **Nếu thức qua 06:00 mà không ngủ**: vật thể vẫn chưa hồi cho tới lần nạp vùng kế. (Rule 13, F-5)
- **Nếu một vật thể bị dời sang vùng khác sau khi phát hành**: trạng thái giữ nguyên, vì khóa save là `object_id`, không gồm `region_id`. Rương đã mở vẫn mở. (Rule 13)
- **Nếu `renew_days` của một loại vật thể đổi trong patch sau khi đã có save**: dùng `min(renew_days_at_consume, renew_days)` — rút ngắn áp dụng ngay, kéo dài không phạt timer đang chạy. (F1)
- **Nếu `last_consumed_minute` lớn hơn `now`** (save hỏng, hoặc đồng hồ từng bị lùi): kẹp theo F1 và ghi log; thời gian khóa kéo dài đúng phần đồng hồ đã lùi. Giá trị âm trong DTO sửa về 0 ở mức từng mục, không làm hỏng cả lần nạp. **[P]** Defeat & Retry / Game Time xác nhận đồng hồ không bao giờ lùi trong game. (F1)
- **Nếu một ID vật thể bị gỡ trong patch**: bản ghi vào vùng mồ côi và được ghi ra lại; ID vào tombstone, không cấp lại cho vật thể khác. Thêm lại chính ID đó thì bản ghi mồ côi được nhận lại. `TryConsume` / `IsAvailable` với ID lạ: `Rejected(UnknownObject)` / không có sẵn, error, không tạo bản ghi mới. (Rule 13, Rule 15)
- **Nếu một ID đã phát hành đổi OneTime ↔ Renewable**: validator chặn trừ khi có migration. Migration đề xuất: OneTime → Renewable thì coi là "đã dùng lúc nạp" (không phát lại thưởng ngay); Renewable → OneTime thì giữ "đã dùng" vĩnh viễn. (validator)
- **Nếu đồng hồ đổi trong lúc một vùng đang nạp**: `now` được chụp một lần cho mọi vật thể của lần nạp. **[P]** Game Time xác nhận token Region Loading chặn rollover trong lúc nạp. (Rule 13)

### Lưu, khôi phục, vòng đời

- **Nếu autosave được yêu cầu giữa một cascade** (boss ngã → `boss.*` đã commit, `rumor.*` còn trong hàng): snapshot chờ tới điểm yên tĩnh. Chụp giữa chừng sẽ mất vĩnh viễn latch còn trong hàng, vì khi nạp không có effect hay sự kiện nào chạy lại. (Rule 15)
- **Nếu handler yêu cầu nạp save, về menu hay thoát giữa lúc đang phát**: chờ handler hiện tại xong, hủy cả hàng FIFO và các handler còn lại, rồi chuyển trạng thái — các lệnh ghi bị hủy thuộc thế giới sắp bị xóa. (Rule 9)
- **Nếu bắt đầu game mới sau khi đã nạp một save** (S6 → S2): toàn bộ dữ liệu, kể cả vùng mồ côi và `world_seed`, bị xóa; `world_seed` mới được sinh. Mồ côi của save cũ không rò vào game mới. (States)
- **Nếu sau khi nạp, nhật ký / HUD không biết trạng thái đã đổi** (Restoring không phát `FlagChanged`): `WorldStateReady` phát sau S4 và observer đọc lại toàn bộ — tin đồn đã lần theo vẫn được gạch. (Rule 9)
- **Nếu catalog chưa nạp xong hay nạp lỗi một phần lúc boot hoặc lúc nạp save**: không vào Sealed; nạp save đi thẳng S5. Không khôi phục vào một catalog thiếu, vì mọi khóa sẽ thành "lạ" và người chơi thấy thế giới trống. (States, Rule 16)
- **Nếu nạp thất bại sau khi Game Time đã khôi phục đồng hồ**: S5 báo lên Save/Load, mọi hệ về trạng thái trống; file save hỏng không bị ghi đè. (States)
- **Nếu DTO thiếu hay hỏng `world_seed`, hoặc `schema_version` mới hơn bản đang chạy**: S5. Không sinh lại seed âm thầm — đổi seed là đổi hình dạng vùng. (Rule 15)
- **Nếu DTO có khóa lệch kiểu so với catalog và chưa có migration**: Latch ↔ Stage đổi không mất thông tin; lệch khác dùng mặc định và giữ giá trị gốc trong vùng mồ côi. **Nếu Counter được lưu ngoài dải mới**: kẹp, warn. **Stage không bao giờ bị kẹp.** (Rule 15)
- **Nếu DTO có khóa trùng**: Latch lấy OR, Stage lấy max, Counter lấy mục cuối, ghi log — thiên về giữ lại, không bao giờ mất. (Rule 15)
- **Nếu API được gọi từ thread khác main thread**: `Rejected(WrongThread)`, bản dev assert. (Rule 8)

## Dependencies

*Hệ này nằm ở Foundation và không tham chiếu lên hệ nào ở trên nó (systems-index › hướng phụ thuộc; TD-A4, TD-C). Phần lớn cạnh dưới đây là **đảo phụ thuộc**: hệ khác đăng ký provider / handler / namespace / observer vào hợp đồng của hệ này, nên hệ đó phụ thuộc vào hợp đồng, còn hệ này chỉ biết giao diện trong **assembly hợp đồng trung lập** (OQ-4 của Game Time). **Cứng** = hệ kia không chạy đúng nếu thiếu cạnh này; **mềm** = thiếu thì chỉ mất một tính năng, hệ vẫn chạy (lá tương ứng trả `Unknown`, Rule 7).*

### Hệ này phụ thuộc vào (upstream)

| Hệ | Cứng / mềm | Dùng gì | Trạng thái GDD |
|---|---|---|---|
| **Game Data & Tuning Config** | **Cứng** | Catalog khai báo flag, ID ổn định (TD-B4), dữ liệu điều kiện / lô effect, bảng `object_id` và `renew_days` theo loại vật thể. Thiếu catalog thì không vào được Sealed (States S1) | Chưa có; **ADR ID ổn định (TD-B4) phải có trước** |
| **Game Time & Calendar** | **Cứng** cho World State, **mềm** cho flag / điều kiện | Giao diện đồng hồ (`game_minute`, `day_index`) cho F1 và `last_consumed_minute`; provider lá `Time`; loại chuyển ngày (`Sleep` / `Rollover`); thứ tự khôi phục (Time trước) | **Approved** 2026-10-27 |
| **Save/Load** | **Mềm** (game vẫn chạy, chỉ không lưu) | Hệ này đăng ký saveable; Save/Load gọi snapshot ở điểm yên tĩnh và gọi khôi phục theo thứ tự Time → hệ này → Inventory (Rule 15) | Chưa có |

### Hệ phụ thuộc vào hệ này (downstream)

| Hệ | Cứng / mềm | Cạnh | Ghi chú |
|---|---|---|---|
| Region Loading | Cứng | `IsAvailable` khi nạp vùng; lá `Flag` trên `boss.*` cho quyền vào vùng; phase stage 3 khi `Sleep` (Rule 13, F-5) | **[P]** |
| Interaction & Context Prompts | Cứng | Ghi `secret.*`; `TryConsume` rương / điểm thu hoạch; điều kiện Transient cho prompt | **[P]** |
| Weapon Tree | Cứng | Ghi `weapon.*` (latch mở khóa, F-2); điều kiện trần tier (Pillar 2) | **[P]** |
| Crafting & Forge | Cứng | Ghi `recipe.*` | **[P]** |
| Enemy AI, Spawning & Drops | Cứng | `IsAvailable` / `TryConsume` nhóm quái (khi hạ con cuối); `world_seed` | **[P]** |
| Boss Encounter | Cứng | Ghi `boss.*` (Stage) | |
| Defeat & Retry | Mềm | Đọc flag; túi đồ rơi **không** thuộc hệ này | **[P]** xác nhận đồng hồ không bao giờ lùi (Edge Cases) |
| NPC Affinity | Cứng | Đăng ký provider lá `Affinity` và handler `AffinityDelta` | |
| Item & Inventory | Cứng | Đăng ký provider lá `HasItem` và handler `GiveItem` / `TakeItem` với `CanApplyAll` (Rule 11); bảo đảm `GiveItem` đã qua kiểm luôn thành công | **[P]** |
| Dialogue & Discovery Hints | Cứng | Ghi `rumor.*`; nguồn lô effect; điều kiện cho nút và gợi ý; nghe `WorldStateReady` để dựng lại nhật ký | |
| Quest & Hidden Quest | Cứng | Ghi `quest.*` (Stage); handler `StartQuest`; lô thưởng với `once_key` | Stub ở Fun-Gate |
| Cutscene & Scripted Sequence | Cứng | Ghi `cutscene.*`; nguồn lô effect | Vertical Slice |
| Tutorial & Onboarding | Mềm | Ghi `tutorial.*`; nghe `FlagChanged` | Vertical Slice |
| Currency & Shops | Mềm | Handler `GiveCurrency`; điều kiện mở hàng | Stub ở Fun-Gate |
| HUD / nhật ký | Mềm | Nghe `FlagChanged`, `WorldStateReady` / `WorldStateCleared`; đọc flag | |

Mọi hệ downstream trừ Game Time chưa có GDD: các hợp đồng đánh **[P]** ở Detailed Design phải được GDD đó xác nhận, và GDD đó phải liệt kê hệ này trong Dependencies của mình.

### Kiểm tra hai chiều (2026-10-28)

- **Game Time & Calendar** — ✅ liệt kê hệ này ở Interactions (provider lá `Time`) và Dependencies (mềm; `world_seed` thuộc hệ này). ⚠️ **Lệch chữ:** bảng stage của Game Time (Rule 12, dòng stage 3) ghi "phase của World State / Enemy", trong khi hệ này **không có phase** (Rule 13) — phase stage 3 thuộc Region Loading (khi `Sleep`) và Enemy AI. Đề xuất sửa ghi chú đó thành "phase của Region Loading / Enemy" ở lần sửa kế tiếp của Game Time; không đổi hành vi. Ghi ở Open Questions.
- **systems-index** — các dòng sau chưa liệt kê hệ này dù có cạnh ở trên: **Region Loading** (#8), **Item & Inventory** (#11), **Crafting & Forge** (#22), **Currency & Shops** (#27), **HUD** (#30). Cập nhật ở Phase 5 (cần đồng ý). Dòng của hệ này (#3) ghi "—" là đúng theo đảo phụ thuộc, nhưng nên bổ sung "Game Data & Tuning Config (catalog, cứng)".
- Các hệ còn lại chưa có GDD — kiểm tra hai chiều khi từng GDD được viết.

## Tuning Knobs

*Rút từ biến của Formulas (main session, 2026-10-28). `systems-designer` không được hỏi riêng cho mục này: công thức ở Section D đơn giản và chỉ một biến là knob cân bằng thật (`renew_days`); các giới hạn khác đã được chốt là **hằng** ở F4. Hệ này gần như không có knob — đó là chủ ý: nó là kho sự thật, không phải hệ cân bằng.*

### Knob cân bằng (data file, đổi không cần sửa code)

| Knob | Mặc định | Dải an toàn | Ảnh hưởng | Quá cao | Quá thấp |
|---|---|---|---|---|---|
| `renew_days` — điểm thu hoạch hoang | **1** | 1 – 7 (validator: 1 – 28) | Nhịp "sáng mai có lại" của vòng thu thập; tài nguyên rơi vào mỗi chuyến | > 3: vùng trông trống, người chơi phải nhớ lịch từng điểm — vòng thu thập thành việc vặt | — (1 là sàn; 0 bị validator từ chối vì sẽ hồi mỗi lần nạp vùng, trái Player Fantasy) |
| `renew_days` — nhóm quái | **3** | 1 – 7 (validator: 1 – 28) | Mật độ chiến đấu khi quay lại một vùng; nguồn đồ rơi lặp lại | > 7: vùng đã dọn trông chết, mất nguồn nguyên liệu từ quái | 1: mỗi sáng vùng đầy lại — dọn vùng không còn ý nghĩa, đi lại thành mài quái |

- **Ai chốt giá trị:** hệ này sở hữu **dải** và công thức (F1); giá trị cho từng loại vật thể do **Farming / Enemy AI / level design** chốt trong dải **[P]**. Không tạo knob trùng ở các GDD đó — chúng trỏ về đây.
- **Tương tác:** đổi ra thời gian thực qua `real_seconds_per_game_minute` của **Game Time** (mặc định 0,75 s → một ngày ≈ 18 phút thực nếu thức trọn; ngủ rút ngắn). Ví dụ: `renew_days = 3` ≈ tối đa 54 phút thực, thường ít hơn vì người chơi ngủ. Đổi knob của Game Time làm dịch nhịp hồi của mọi vật thể — cân bằng hai knob cùng nhau.
- **Đổi sau khi phát hành:** an toàn với save cũ nhờ `renew_days_at_consume` (F1): rút ngắn có hiệu lực ngay, kéo dài không phạt timer đang chạy.

### Dữ liệu author (không phải knob cân bằng)

Khai báo theo từng mục trong catalog, kiểm bằng validator; đổi sau khi phát hành bị chặn nếu không có migration (validator, Edge Cases):

| Trường | Ở đâu | Ràng buộc |
|---|---|---|
| Kiểu, loại, `default` của flag | Catalog flag | Rule 2–3; Latch `false`, Stage `≥ 0` |
| `[min, max]` của Counter | Catalog flag | `min ≤ default ≤ max` (F3) |
| `usage` của điều kiện | Dữ liệu điều kiện | Permanent chỉ dùng lá đơn điệu (Rule 10) |
| `repeat` và `once_key` của lô | Dữ liệu lô effect | `repeat` bắt buộc; `once_key` bắt buộc với lô `Once` có effect không idempotent (Rule 12) |
| Loại vật thể (OneTime / Renewable), hệ được tiêu thụ | Bảng vật thể | Rule 13 |

### Hằng — cố ý **không** phải knob

`COND_MAX_DEPTH` 4 · `COND_MAX_LEAVES` 16 · `EFFECT_BATCH_MAX` 16 · `EFFECT_CASCADE_MAX` 8 · `EFFECT_CASCADE_TOTAL_MAX` 64 (F4). Đổi chúng là đổi hợp đồng hiệu năng và bộ test, nên phải sửa GDD này, không sửa data file. Ngân sách `c_max ≤ 0,5 µs` mỗi lá (F4) là **mục tiêu hiệu năng**, không phải knob — chờ technical-director xác nhận (Open Questions).

## Visual/Audio Requirements

[To be designed]

## Game Feel

[To be designed]

## UI Requirements

[To be designed]

## Cross-References

[To be designed]

## Acceptance Criteria

*Nguồn: qa-lead, 2026-10-28. Mỗi tiêu chí có mã `AC-…` và tag loại test + mức cổng theo Testing Standards (Logic/Integration BLOCKING; **UI/Visual BLOCKING** — bằng chứng là ảnh chụp lưu ở `production/qa/evidence/`, Visual kèm lead sign-off; Playtest advisory — documented playtest ghi bằng `/playtest-report`; `[Config/Data — smoke check]` advisory theo story, blocking ở `/smoke-check`). `[Logic/CI — BLOCKING]` = validator chạy ở batch mode trong CI, không mở cửa sổ editor. *[Defer: …]* = viết khi tới tier đó / khi GDD đó có. Mã AC chỉ duy nhất trong GDD này; trích chéo luôn kèm tên GDD ("Game Time AC-C08"). Mọi hành vi engine nhắc ở đây là **NOT SOURCEABLE** (`docs/engine-reference/unity/` không có module scripting / testing / profiling).*

**Quy ước test (áp dụng cho mọi tiêu chí trừ khi ghi khác)**
- **Vị trí:** Logic và validator → `Assets/Tests/EditMode/WorldState/`; Integration và Performance → `Assets/Tests/PlayMode/WorldState/`; Playtest → `/playtest-report`.
- **Đặt tên** (NUnit): class `[System]Tests` (`FlagStoreTests`, `NamespaceTests`, `ConditionEvaluatorTests`, `ProviderTests`, `EffectBatchTests`, `OnceKeyTests`, `CascadeTests`, `RegionObjectTests`, `RenewableTests`, `WorldSeedTests`, `WorldStateSaveTests`, `WorldStateLifecycleTests`, `WorldStateValidatorTests`, `WorldStatePerfTests`); method `Scenario_Expected` (vd. `Write_LatchTrueToFalse_ReturnsRejectedMonotonic`).
- **Fixture "Ready":** composition root test (H-1) dựng kho với **catalog là object trong bộ nhớ** (không đọc file), đăng ký fake, Seal (S1), rồi S2 với nguồn seed tiêm trả `0x00C0FFEE` (H-3). Catalog fixture (chỉ dùng namespace có trong bảng Rule 5): `rumor.mist`, `rumor.ash` (Latch); `boss.forest` (Stage, mặc định 0); `quest.a` (Stage, 0); `quest.b` (Stage, mặc định 2); `quest.kills` (Counter [0, 10], 0); `quest.ping`, `quest.pong` (Counter [0, 100], 0); `quest.reward_a` (Latch, dùng làm `once_key`); `weapon.ember.unlocked` (Latch). Handle: `H_boss`, `H_quest`, `H_rumor`, `H_weapon`. Vật thể: `obj.chest_01` (OneTime, hệ tiêu thụ Interaction), `obj.herb_01` (Renewable, `renew_days` 1, Interaction), `obj.pack_01` (Renewable, 3, Enemy AI).
- **Đồng hồ:** stub của giao diện đồng hồ ở assembly hợp đồng, đặt được `game_minute` (H-2); `day_index = floor(game_minute / 1440)`. Không đọc giờ thực, không dùng API thời gian của engine, không dùng lệnh debug để dựng trạng thái. Hằng F4 giữ đúng giá trị GDD.
- **Fake:** provider giả (Affinity, HasItem, Time) cấu hình trả T / F, ném exception, hay báo "ID không tồn tại"; handler giả (Inventory, Affinity, Currency, Quest) ghi lại mọi lời gọi `CanApplyAll` / apply kèm danh sách effect, cấu hình được để từ chối hay ném **sau** kiểm trước; recorder ghi thứ tự + payload của `FlagChanged`, `WorldStateReady`, `WorldStateCleared`; saveable giả khôi phục sau hệ này (vai Inventory). "Ghi log / warn / error" = bắt được qua `LogAssert` hoặc logger tiêm (H-4) **đúng mức**; "một lần mỗi phiên / mỗi lần vào trạng thái" = đếm qua logger. "Bản dev: assert" kiểm riêng với define dev bật (H-5); mặc định test chạy không có define dev và khẳng định kết quả trả về + error.
- **Quan sát:** `Revision` qua H-6; trạng thái vòng đời qua H-7; hàng FIFO và thế hệ cascade qua H-9.
- **AC xuyên hệ chia hai loại:**
  - **(a)** THEN chỉ khẳng định đầu ra của hệ này (kết quả trả về, giá trị flag, `Revision`, sự kiện, lời gọi tới provider / handler giả). Test dùng stub đóng vai hệ kia. **Game Time & Calendar** (upstream, Approved, xây trước) được dùng **bản thật**. Loại này **thuộc Definition of Done của story hệ này**.
  - **(b)** THEN khẳng định hành vi của hệ khác (kho đồ thật, nhật ký, cây vũ khí, file save…). Mang tag **`[Defer: GDD <hệ>]`**, **không** thuộc DoD của hệ này, và được chuyển vào GDD hệ đó khi viết. Danh sách: AC-C01b (Save/Load), C03, C04b (Save/Load), C05b (Region Loading), C06 (Inventory), C07b, C08 (Dialogue), C09 (Interaction, Region Loading), C10b (Weapon Tree, NPC Affinity), C11 (Enemy AI), C13 (Enemy AI / level), PT1, PT2.
- **Fun-Gate scope:** AC trên lá `Quest`, effect `StartQuest` / `GiveCurrency`, lá `Time` dạng `season` / `weekday`, và migration sau v1 mang *[Defer]* theo bảng "Phạm vi xây dựng ở Fun-Gate".
- Giá trị trong fixture nằm trong dải cứng của validator. Mọi AC validator chạy trên dữ liệu trong bộ nhớ, không mở editor.

### H.1 Core Rules

**Rule 1 — Khóa flag**
- **GIVEN** Ready, **WHEN** ghi `rumor.typo = true` bằng `H_rumor`, **THEN** trả `Rejected(Undeclared)`; kho và `Revision` không đổi; 0 `FlagChanged`. (AC-R01.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** DTO chụp khi `rumor.mist = true`, `boss.forest = 2` với catalog khai báo theo thứ tự X, **WHEN** khôi phục vào catalog cùng khóa nhưng khai báo theo thứ tự đảo (ID số nguyên nội bộ khác), **THEN** mọi giá trị đọc theo khóa bằng giá trị đã lưu; DTO chỉ chứa khóa chuỗi, không chứa ID số nguyên. (AC-R01.2) `[Logic/EditMode — BLOCKING]`

**Rule 2, 3 — Kiểu và loại**
- **GIVEN** `rumor.mist = true`, `boss.forest = 3`, recorder, **WHEN** ghi `rumor.mist = false`, `Set boss.forest 2`, `Add boss.forest −1`, **THEN** mỗi lệnh trả `Rejected(Monotonic)` + 1 error; giá trị không đổi; 0 sự kiện; `Revision` không đổi. (AC-R03.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** `rumor.mist = true`, `boss.forest = 3`, `quest.kills = 4`, **WHEN** ghi `rumor.mist = true`, `Set boss.forest 3`, `Add quest.kills 0`, **THEN** mỗi lệnh trả `Ok(NoChange)`; 0 sự kiện; `Revision` không đổi; 0 error. Stage đã bão hòa: xem AC-F3.1. (AC-R03.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** assembly của hệ này, **WHEN** reflection liệt kê (H-13), **THEN** đúng các tập đóng sau:
  - kiểu flag {Bool, Int};
  - loại {Latch, Stage, Counter};
  - nút {All, Any, Not};
  - lá {Flag, Affinity, HasItem, Time, Quest};
  - effect {SetFlag, GiveItem, TakeItem, AffinityDelta, GiveCurrency, StartQuest};
  - vật thể {OneTime, Renewable}.

  Không có lá "vật thể vùng", không có Enum / float / string. Phủ Rules 2, 3, 6, 11, 13. (AC-R03.3) `[Logic/EditMode — BLOCKING]`

**Rule 4 — Đọc**
- **GIVEN** Ready, **WHEN** đọc `quest.b`, `quest.kills`, `rumor.mist` chưa từng ghi, **THEN** được 2, 0, `false`.
  - **WHEN** đọc trực tiếp `rumor.typo` 3 lần và `quest.typo` 1 lần, **THEN** được `false` / `0` và đúng 2 warn (một warn mỗi khóa).
  - Sau S6 → S2, đọc lại `rumor.typo` → thêm đúng 1 warn.

  (AC-R04.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** Ready, **WHEN** đánh giá `Not(Flag(rumor.typo == true))` và `Any[Not(Flag(rumor.typo == true))]`, mỗi cái 50 lần, **THEN** lá và gốc là `Unknown`, kết quả `False` (không bao giờ `True`); đúng 1 error cho khóa đó trong phiên. (AC-R04.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** Restoring với DTO có `rumor.mist = true`, và restore đã dừng sau khi mục đó vào kho nội bộ (H-8), **WHEN** đọc `rumor.mist` 10 lần, **THEN** được `false` (mặc định) cả 10 lần, đúng 1 error; sau S4 đọc → `true`. (AC-R04.3) `[Logic/EditMode — BLOCKING]` — ranh giới S4: States S4.

**Rule 5 — Một hệ ghi mỗi namespace**
- **GIVEN** Registering, Quest đã nhận `quest`, **WHEN** hệ thứ hai nhận `quest` rồi `Quest`, **THEN** cả hai bị từ chối + 1 error mỗi lần; handle đầu vẫn ghi được. (AC-R05.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** Ready, **WHEN** `H_quest` ghi `weapon.ember.unlocked = true`, và một handle không do kho này cấp (handle của một instance kho khác) ghi `quest.a`, **THEN** cả hai trả `Rejected(WrongHandle)`, không đổi gì, 1 error mỗi lệnh; với define dev thì assert bắt được (H-5). `H_quest` đọc `weapon.ember.unlocked` → được. Namespace của `weapon.ember.unlocked` là `weapon`. (AC-R05.2) `[Logic/EditMode — BLOCKING]`

**Rule 6 — Ngữ pháp điều kiện**
- **GIVEN** `boss.forest = 2`, **WHEN** đánh giá `Flag(boss.forest op v)` với `op ∈ {==, ≠, ≥, ≤}`, `v ∈ {1, 2, 3}`, **THEN** 12 kết quả khớp phép so sánh số nguyên; `Flag(rumor.mist == true)` và `Flag(rumor.mist ≠ true)` khớp giá trị bool ở cả `false` và `true`. (AC-R06.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** catalog lúc chạy bị sửa tay (bỏ qua CI) có: cây sâu 5, cây 17 lá, `All[]` rỗng, chuỗi `Not` lồng 10.000 tầng, **WHEN** nạp catalog và đánh giá từng cây, **THEN** mỗi cây vi phạm được thay bằng hằng `Unknown` → `False`, 1 error mỗi cây lúc nạp, không exception, không tràn stack; các cây hợp lệ khác cho kết quả đúng. (AC-R06.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** `quest.a ∈ {0, 1, 2, 3}`, **WHEN** đánh giá `Quest(a) ≥ 2` và `Quest(a) == 2`, **THEN** kết quả bằng `Flag(quest.a ≥ 2)` / `Flag(quest.a == 2)` ở mọi giá trị. (AC-R06.3) `[Logic/EditMode — BLOCKING; Defer: lá Quest hết stub]`

**Rule 7 — Provider**
- **GIVEN** `All[Flag(boss.forest ≥ 0), Affinity(mira) ≥ 40]` và `HasItem(herb) ≥ 1`, **WHEN** lần lượt: không có provider Affinity; provider ném exception; provider báo `mira` không tồn tại; không có provider HasItem — mỗi trường hợp hỏi 100 lần, **THEN** lá là `Unknown`, kết quả `False`, không exception lọt ra người gọi; trong phiên đúng 1 error cho loại `Affinity` và 1 cho loại `HasItem`. (AC-R07.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** Registering, provider Affinity P1 đã đăng ký, **WHEN** đăng ký P2 cho cùng loại lá, **THEN** bị từ chối + error; sau S2, lá `Affinity` gọi P1, không gọi P2. (AC-R07.2) `[Logic/EditMode — BLOCKING]`

**Rule 8 — Đánh giá theo yêu cầu**
- **GIVEN** lần lượt Registering, Sealed, Restoring, **WHEN** hỏi `Evaluate(Flag(rumor.mist == false))` 100 lần ở mỗi trạng thái, **THEN** mọi lần trả `NotReady` (không phải `False`), đúng 1 error mỗi lần vào trạng thái. (AC-R08.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** Ready, **WHEN** từ một thread khác main thread (H-14) gọi ghi flag, chạy lô, `TryConsume`, `Evaluate`, **THEN** lệnh ghi / lô / `TryConsume` trả `Rejected(WrongThread)`, kho không đổi; `Evaluate` trả `NotReady` + error; với define dev thì assert. (AC-R08.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** Ready với `Revision = r0`, **WHEN** lần lượt: (1) lô 3 `SetFlag` đổi giá trị; (2) một ghi `Ok(NoChange)`; (3) cascade 3 commit; (4) S6; (5) S2; (6) S6; (7) S3; (8) S4, **THEN** `Revision` tăng lần lượt +1, +0, +3, +1, +1, +1, +0, +1; dãy tăng chặt, không bao giờ về 0 hay về giá trị cũ; một cache khóa theo `Revision` của thế giới trước không khớp ở thế giới sau. (AC-R08.3) `[Logic/EditMode — BLOCKING]`
- **GIVEN** các điều kiện chỉ có `Flag` / `Quest`, và các điều kiện có ít nhất một lá `Affinity` / `HasItem` / `Time`, **WHEN** hỏi API "cache được theo `Revision`?", **THEN** lần lượt `true` / `false`; kết quả xác định tĩnh, không gọi provider. (AC-R08.4) `[Logic/EditMode — BLOCKING]`
- **GIVEN** lô `[SetFlag rumor.mist = true, GiveItem]` và handler GiveItem giả đánh giá `Flag(rumor.mist == true)` trong lúc apply; một observer `FlagChanged` đánh giá cùng điều kiện và xếp hàng ghi `rumor.ash`, **WHEN** chạy lô, **THEN** handler thấy `False` (trạng thái trước lô); observer thấy `rumor.mist == true` và `rumor.ash == false` (lệnh đang chờ chưa hiện). (AC-R08.5) `[Logic/EditMode — BLOCKING]`

**Rule 9 — Sự kiện thay đổi**
- **GIVEN** recorder, **WHEN** một lô ghi `quest.a 0 → 1`, `rumor.mist` (đang `true`, không đổi), `quest.kills 0 → 3`, **THEN** recorder nhận đúng 2 sự kiện theo thứ tự `(quest.a, 0, 1, Quest)`, `(quest.kills, 0, 3, Quest)`, và cả hai phát **sau** lời gọi apply cuối của lô (trace H-9). (AC-R09.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** 3 observer, observer thứ 2 ném exception, **WHEN** một ghi đổi giá trị, **THEN** 1 error log; observer 1 và 3 đều được gọi; lệnh ghi trả `Ok`. (AC-R09.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** observer O1: khi `boss.forest` đổi thì ghi `rumor.ash = true`, **WHEN** Boss ghi `boss.forest = 1`, **THEN** lệnh trong O1 nhận `Queued`; trace là: commit boss → phát tới mọi observer → commit rumor → phát rumor; khi lệnh của Boss trả về thì hàng rỗng và `rumor.ash == true`. Biến thể: O1 xếp hàng `Set boss.forest 0` bằng `H_rumor` → nhận `Queued`; lúc chạy có 1 error chứa handle + khóa, không exception tới Boss. (AC-R09.3) `[Logic/EditMode — BLOCKING]`
- Trần cascade (F-6):
  - **GIVEN** vòng Counter: ping đổi → `Add quest.pong 1`, pong đổi → `Add quest.ping 1`, **WHEN** `Add quest.ping 1`, **THEN** chuỗi dừng khi vượt 8 thế hệ; 1 error chứa chuỗi khóa; hàng rỗng khi trả về.
  - **GIVEN** một observer sinh 70 lệnh Counter trong một thế hệ, **THEN** dừng ở lệnh thứ 64.
  - **GIVEN** chuỗi 20 Latch `rumor.l1 → … → rumor.l20` (mỗi observer ghi latch kế tiếp), **WHEN** ghi `rumor.l1`, **THEN** cả 20 là `true`, 0 error.
  - **GIVEN** chuỗi trộn: vòng Counter ping / pong như trên, và mỗi lần `quest.pong` đổi thì observer cũng xếp hàng ghi `rumor.l<n> = true`, **WHEN** chuỗi vượt trần (đếm từ lệnh ngoài cùng), **THEN** chỉ các lệnh Counter còn trong hàng bị bỏ; mọi lệnh Latch đã xếp hàng vẫn commit; 1 error.

  (AC-R09.4) `[Logic/EditMode — BLOCKING]`
- **GIVEN** observer A và B trên cùng sự kiện, A hủy B và đăng ký C khi được gọi, **WHEN** phát một sự kiện, **THEN** B không được gọi (hủy có hiệu lực ngay) và C không được gọi ở lượt này; lượt sau gọi A, C. Đăng ký observer sau Sealed thành công. (AC-R09.5) `[Logic/EditMode — BLOCKING]`
- **GIVEN** recorder, **WHEN** khôi phục DTO có 10 flag khác mặc định, rồi stub Region Loading gọi `IsAvailable` cho 3 vật thể, **THEN** 0 `FlagChanged` trong cả hai bước. (AC-R09.6) `[Logic/EditMode — BLOCKING]`
- **GIVEN** recorder vòng đời, **WHEN** S2, S6, S3 → S4, S3 → S5, **THEN** `WorldStateReady(revision)` phát đúng 1 lần sau mỗi S2 / S4 với `revision == Revision` lúc phát; `WorldStateCleared` phát đúng 1 lần sau mỗi S5 / S6; không lần nào khác. (AC-R09.7) `[Logic/EditMode — BLOCKING]`
- **GIVEN** đang phát `FlagChanged` với 3 observer còn chờ và 2 lệnh ghi trong hàng, observer hiện tại yêu cầu S6 (cũng chạy với "nạp save"), **WHEN** observer đó trả về, **THEN** 2 lệnh trong hàng không commit, không phát; 3 observer còn lại không được gọi; trạng thái chuyển Sealed; `WorldStateCleared` 1 lần. (AC-R09.8) `[Logic/EditMode — BLOCKING]`

**Rule 10 — Ghi latch lúc mở**
- **GIVEN** provider Affinity giả trả 50; stub Weapon Tree đánh giá `Affinity(smith) ≥ 40` (Transient) và ghi `weapon.ember.unlocked = true` trong cùng bước, **WHEN** provider đổi sang 0 và đồng hồ stub tiến 7 ngày, **THEN** `Flag(weapon.ember.unlocked == true)` (Permanent) vẫn `True` ở mọi lần hỏi; không API công khai nào của hệ này đặt latch về `false` ngoài khôi phục. (AC-R10.1) `[Logic/EditMode — BLOCKING]` — loại (a); `usage`: AC-V03; hệ thật: AC-C10b.

**Rule 11 — Lô effect**
- **GIVEN** lô `[SetFlag quest.a = 1, GiveItem(herb, 1), TakeItem(ore, 5)]`, Inventory giả từ chối TakeItem; và một lô khác chứa `GiveCurrency` mà không có handler (Fun-Gate), **WHEN** chạy mỗi lô, **THEN** trả `Rejected(<lý do>)`; 0 lời gọi apply; kho, `Revision` không đổi; 0 sự kiện. (AC-R11.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** lô `[TakeItem(ore, 1), TakeItem(ore, 1), GiveItem(herb, 1), AffinityDelta(mira, +5)]`, **WHEN** kiểm trước, **THEN** Inventory giả nhận đúng **1** lời gọi `CanApplyAll` với cả 3 effect của nó theo thứ tự danh sách; Affinity giả nhận 1 lời gọi với 1 effect; Inventory trả "không đủ" cho tổng (2 > 1) → cả lô bị từ chối. (AC-R11.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** lô `[GiveItem A, GiveItem B, SetFlag quest.a = 1]` có `once_key = quest.reward_a`, handler ném exception ở GiveItem A **sau** kiểm trước, **WHEN** chạy, **THEN** GiveItem B vẫn được apply; `quest.a == 1` và `quest.reward_a == true` commit; trả `Applied(WithErrors)`; 1 error chứa ID lô; không rollback; với define dev thì assert. (AC-R11.3) `[Logic/EditMode — BLOCKING]`
- **GIVEN** lô 3 `SetFlag` đổi giá trị + 1 `GiveItem`, **WHEN** chạy, **THEN** `Revision` +1 đúng một lần; mọi `FlagChanged` phát sau lời gọi apply cuối. Lô chỉ gồm `SetFlag` không đổi giá trị → 0 sự kiện, `Revision` +0. Đổi thứ tự `SetFlag` trong lô cho cùng trạng thái cuối. (AC-R11.4) `[Logic/EditMode — BLOCKING]`
- **GIVEN** lần lượt: Sealed; Restoring; trong handler `DayStarted` (tín hiệu stage 8, H-12); trong một phase stage 3–6 giả, **WHEN** chạy lô `[SetFlag quest.a = 1]` và ghi `SetFlag` lẻ, **THEN** lần lượt `Rejected(NotReady)`, `Rejected(NotReady)`, `Rejected(Stage8)`, `Applied`; ở ba trường hợp đầu không có gì được xếp hàng (sau khi về Ready, `quest.a` vẫn 0). (AC-R11.5) `[Logic/EditMode — BLOCKING]` — bản Game Time thật: AC-C12.

**Rule 12 — Thưởng một lần**
- **GIVEN** lô `[GiveItem(herb, 1)]` có `once_key = quest.reward_a`, **WHEN** chạy hai lần, **THEN**:
  - lần 1 trả `Applied`, `quest.reward_a == true` trong **cùng** commit (`Revision` +1 một lần, `FlagChanged` gồm `quest.reward_a`);
  - lần 2 trả `Rejected(AlreadyApplied)`, 0 lời gọi tới handler.

  (AC-R12.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** hai observer cùng xếp hàng lô thưởng trên khi `boss.forest` đổi, **WHEN** Boss ghi `boss.forest = 1`, **THEN** đúng một lần apply (`GiveItem` 1 lần); lần lấy ra thứ hai trả `Rejected(AlreadyApplied)` (kiểm lúc lấy ra khỏi hàng, không phải lúc xếp). (AC-R12.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** DTO chụp sau khi lô thưởng đã apply, **WHEN** khôi phục, **THEN** 0 lô chạy, 0 lời gọi tới handler / provider, 0 `FlagChanged`; `quest.reward_a == true`. (AC-R12.3) `[Logic/EditMode — BLOCKING]` — cũng phủ "khôi phục im lặng" (Rule 15).

**Rule 13 — Vật thể vùng**
- **GIVEN** `obj.chest_01` chưa dùng, đồng hồ 1500, **WHEN** `TryConsume` bằng handle Interaction, **THEN** trả `Consumed`. **WHEN** gọi lại, và lần nữa sau khi đồng hồ tiến 100 ngày, **THEN** cả hai trả `Rejected(AlreadyConsumed)`; `IsAvailable` luôn `false`. (AC-R13.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** `obj.herb_01` chưa dùng, đồng hồ 1500, **WHEN** `TryConsume` hai lần trong cùng frame, **THEN** lần 1 `Consumed` với `last_consumed_minute = 1500`; lần 2 `Rejected(AlreadyConsumed)`. **WHEN** đồng hồ 1600 rồi `TryConsume`, **THEN** trả `Rejected(AlreadyConsumed)` (cùng ngày lịch với lần tiêu thụ) và `last_consumed_minute` vẫn 1500. (AC-R13.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** Ready, **WHEN** `TryConsume(obj.nope_99)` và `IsAvailable(obj.nope_99, now)`; rồi `TryConsume(obj.pack_01)` bằng handle Interaction (hệ tiêu thụ khai báo là Enemy AI), **THEN** lần lượt `Rejected(UnknownObject)` / `false` + error, rồi `Rejected(WrongOwner)`; DTO chụp sau đó không có bản ghi mới nào. (AC-R13.3) `[Logic/EditMode — BLOCKING]`
- **GIVEN** recorder `FlagChanged` và observer / provider giả, **WHEN** `TryConsume` trả `Consumed`, **THEN** lời gọi hoàn tất đồng bộ, không gọi bất kỳ callback, observer hay provider nào trong lúc chạy (không có điểm tái nhập giữa "đánh dấu" và "trả về"); bản ghi đọc được ngay sau khi trả về. (AC-R13.4) `[Logic/EditMode — BLOCKING]` — trao thưởng cùng call stack: AC-C09.
- **GIVEN** save có `obj.chest_01` đã dùng khi catalog đặt nó ở `region.a`, **WHEN** catalog dời nó sang `region.b` rồi khôi phục, **THEN** `IsAvailable == false` và `TryConsume` trả `Rejected(AlreadyConsumed)`; DTO không chứa `region_id`. (AC-R13.5) `[Logic/EditMode — BLOCKING]`
- **GIVEN** Game Time thật và kho Ready, `obj.herb_01` đã dùng hôm trước, **WHEN** liệt kê phase đã đăng ký vào pipeline chuyển ngày (Game Time H-10), rồi cho rollover tại chỗ (thức qua 06:00, không ngủ), **THEN** 0 phase của hệ này; hệ này không phát gì và không ghi bản ghi vật thể nào trong rollover. (AC-R13.6) `[Integration/PlayMode — BLOCKING]` — loại (a).
- **GIVEN** `obj.herb_01` (`renew_days` 1) dùng lúc 20:00 ngày N với `region_now` chụp ở ngày N, **WHEN** rollover tại chỗ sang ngày N+1 rồi `TryConsume(herb_01, handle, region_now)` với cùng `region_now`, **THEN** trả `Rejected(AlreadyConsumed)` dù đồng hồ đã ở ngày N+1. **WHEN** Region Loading tính lại với `region_now` mới (ngày N+1), **THEN** `IsAvailable` → `true` và `TryConsume` → `Consumed`, bản ghi có `last_consumed_minute` = đồng hồ hiện tại. (AC-R13.7) `[Logic/EditMode — BLOCKING]`

**Rule 14 — `world_seed` (và F5)**
- **GIVEN** nguồn seed tiêm trả lần lượt `0`, `0xFFFFFFFF`, **WHEN** S2 → đọc seed → S6 → S2 → đọc seed, **THEN** đọc được `0` (hợp lệ) rồi `0xFFFFFFFF`; nguồn seed được gọi đúng 1 lần mỗi S2. Reflection: không có member public nào ghi được `world_seed` ngoài đường khôi phục. (AC-R14.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** game có seed `0x00C0FFEE`, **WHEN** save rồi nạp lại 3 lần (S6 → S3 → S4), **THEN** seed bằng `0x00C0FFEE` mỗi lần; nguồn seed không được gọi lần nào. (AC-R14.2) `[Logic/EditMode — BLOCKING]`

**Rule 15 — DTO save**
- **GIVEN** trạng thái: `rumor.mist = true`; `boss.forest = 2`; `quest.kills` ghi 5 rồi về 0; `obj.chest_01` đã dùng; `obj.herb_01` dùng tại 1500; `obj.pack_01` chưa dùng, **WHEN** chụp DTO hai lần liên tiếp, rồi khôi phục và chụp lần nữa, **THEN**:
  - DTO có `schema_version`, `world_seed`;
  - đúng 2 flag (không có `quest.kills`, `quest.b`);
  - đúng 2 vật thể (không có `pack_01`); `herb_01` có `last_consumed_minute = 1500`, `renew_days_at_consume = 1`;
  - ba bản serialize giống nhau từng byte (H-10).

  (AC-R15.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** observer: khi `boss.forest` đổi thì xếp hàng `rumor.ash = true`, và cũng chính observer đó yêu cầu snapshot, **WHEN** Boss ghi `boss.forest = 1`, **THEN** không snapshot nào được chụp khi hàng còn lệnh; snapshot cuối cùng chứa cả `boss.forest = 1` lẫn `rumor.ash = true`. Snapshot hỏi ngoài Ready bị từ chối. (AC-R15.2) `[Logic/EditMode — BLOCKING]` — `Snapshot()` khi chưa yên tĩnh trả `Rejected(NotQuiescent)`; Save/Load thử lại.
- **GIVEN** DTO có flag `rumor.gone = true` và vật thể `obj.gone_01`, cả hai không có trong catalog, **WHEN** khôi phục rồi chụp lại, **THEN**:
  - vào S4; cả hai mục có trong DTO mới, nguyên vẹn từng byte;
  - đọc `rumor.gone` → `false` + warn; `TryConsume(obj.gone_01)` → `Rejected(UnknownObject)`.

  **WHEN** catalog thêm lại `rumor.gone` rồi nạp lại, **THEN** đọc → `true`. (AC-R15.3) `[Logic/EditMode — BLOCKING]`
- **GIVEN** DTO có: `quest.x` Latch `true` (catalog mới: Stage); `quest.y` Stage 3 (catalog mới: Latch); `quest.z` Latch `true` (catalog mới: Counter), **WHEN** khôi phục ở v1 (không có migration), **THEN** `quest.x == 1`; `quest.y == true`; `quest.z` ở mặc định và giá trị gốc nằm ở vùng mồ côi (được ghi ra lại); vào S4, không S5; 1 log mỗi mục. (AC-R15.4) `[Logic/EditMode — BLOCKING]`
- **GIVEN** DTO có `quest.kills = 25`, rồi một DTO khác có `quest.kills = −3` (dải [0, 10]); và `boss.forest = 2.000.000.000`, **WHEN** khôi phục, **THEN** `quest.kills` được kẹp về 10 / 0 + 1 warn mỗi lần; `boss.forest` giữ nguyên, 0 warn. (AC-R15.5) `[Logic/EditMode — BLOCKING]`
- **GIVEN** từng DTO: (1) byte không đọc được; (2) `schema_version` = bản hiện tại + 1; (3) thiếu `world_seed`; (4) `world_seed` sai kiểu hoặc ngoài [0; 2³² − 1]; (5) hợp lệ ở nửa đầu và hỏng ở mục giữa, **WHEN** nạp, **THEN** mỗi trường hợp:
  - chuyển S5; mọi flag ở mặc định, 0 vật thể, vùng mồ côi rỗng (không giữ phần đã khôi phục);
  - sink "nạp thất bại" (H-11) nhận đúng 1 báo cáo; `WorldStateCleared` 1 lần;
  - không sinh seed mới; byte DTO đầu vào không bị hệ này sửa.

  **GIVEN** DTO có `last_consumed_minute = −50`, **THEN** không S5: mục đó sửa về 0 + log. (AC-R15.6) `[Logic/EditMode — BLOCKING]`
- **GIVEN** DTO có khóa trùng: `rumor.mist` `false` / `true`; `boss.forest` 3 / 1; `quest.kills` 2 / 7, **WHEN** khôi phục, **THEN** được `true`, 3, 7; 1 log mỗi khóa; không S5. (AC-R15.7) `[Logic/EditMode — BLOCKING]`

**Rule 16 — Khởi động**
- **GIVEN** hai instance kho dựng từ hai composition root trong cùng domain, **WHEN** ghi `quest.a = 1` ở instance A, **THEN** instance B vẫn đọc 0. Reflection: assembly không có trường `static` khả biến nào (đúng khi Enter Play Mode tắt domain reload). (AC-R16.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** Sealed, **WHEN** đăng ký provider `Affinity`, handler `GiveCurrency`, namespace `cutscene`, và một observer, **THEN**:
  - ba đăng ký dịch vụ bị từ chối + error; observer thành công;
  - suốt phiên (kể cả sau S2), lá `Affinity` trả `Unknown` và lô có `GiveCurrency` trả `Rejected`;
  - sau S6 → S2, các provider đã đăng ký trong cửa sổ Registering vẫn trả lời.

  (AC-R16.2) `[Logic/EditMode — BLOCKING]`

### H.2 States and Transitions

- **GIVEN** Registering, **WHEN** Seal với catalog đủ và hợp lệ, **THEN** vào Sealed và `Evaluate` trả `NotReady`. **WHEN** Seal với catalog thiếu hay lỗi một phần, **THEN** không vào Sealed, có error, và mọi ghi / đánh giá vẫn bị từ chối. (AC-S01) `[Logic/EditMode — BLOCKING]` — kho ở lại Registering, `Seal()` trả lỗi cho composition root (Rule 16).
- **GIVEN** Ready sau khi nạp một save có flag, vật thể, mục mồ côi, seed A, và các tập "cảnh báo một lần" đã có mục, **WHEN** S6 rồi S2 (nguồn seed trả B), **THEN**:
  - mọi flag ở mặc định; 0 vật thể; DTO mới không còn mục mồ côi nào; cảnh báo một-lần được phát lại; seed = B;
  - `Revision` lớn hơn mọi giá trị trước;
  - recorder: `WorldStateCleared` rồi `WorldStateReady`, mỗi cái 1 lần.

  (AC-S02) `[Logic/EditMode — BLOCKING]`
- **GIVEN** Sealed, **WHEN** nạp DTO hợp lệ, **THEN** suốt lúc khôi phục là Restoring (`Evaluate` → `NotReady`); S4 → Ready; `WorldStateReady` phát **sau** khi trạng thái đã là Ready (observer đọc được giá trị đã khôi phục ngay trong handler). **WHEN** nạp khi catalog thiếu hay lỗi, **THEN** vào S5 ngay, 0 mục được khôi phục. (AC-S03) `[Logic/EditMode — BLOCKING]`
- **GIVEN** Ready, **WHEN** S6, **THEN** vào Sealed; dữ liệu bị xóa như AC-S02; provider / handler / namespace được giữ; `Revision` +1; `WorldStateCleared` 1 lần. (AC-S04) `[Logic/EditMode — BLOCKING]` — S5: AC-R15.6.
- **GIVEN** từng trạng thái, **WHEN** thử: Sealed → Registering; `NewGame` (S2) khi đang Ready hay Restoring; báo "khôi phục xong" (S4) khi đang Sealed hay Ready; giảm Latch / Stage ngoài Restoring, **THEN** mọi lệnh bị từ chối + error, trạng thái không đổi; không có API công khai nào vào Ready ngoài S2 và S4. (AC-S05) `[Logic/EditMode — BLOCKING]`

### H.3 Formulas

**F1 — Renewable**
- **GIVEN** fixture F1, **WHEN** đánh giá `IsAvailable` theo bảng, **THEN** kết quả khớp từng dòng (AC-F1.1) `[Logic/EditMode — BLOCKING]`:

  | `rec` / `last` | `renew_days` | `now` | Kết quả |
  |---|---|---|---|
  | ∅ | 1 | 0 | `true` |
  | 1230 | 1 | 1500 | `true` |
  | 1500 | 3 | 5000, 5759 | `false` |
  | 1500 | 3 | 5760 | `true` |
  | 1439 | 1 | 1440 | `true` |
  | 0 (phút 0, không phải giá trị canh) | 1 | 1439 | `false` |
  | 0 | 28 | `MAX_GAME_MINUTE` | `true`, không exception tràn số |

  Đánh giá cùng đầu vào 3 lần cho cùng kết quả.
- **GIVEN** DTO có `obj.pack_01` với `last_consumed_minute = 9000` (`renew_days = 3`) và đồng hồ đã khôi phục ở 5000, **WHEN** khôi phục rồi đánh giá `IsAvailable` với `now` ∈ {5000, 8639, 8640}, **THEN** lúc khôi phục bản ghi được lưu thành 5000 + đúng 1 log; `IsAvailable` cho `false`, `false`, `true` (có lại từ ngày 6) và không ghi log. (AC-F1.2) `[Logic/EditMode — BLOCKING]`
- **GIVEN** `obj.pack_01` dùng tại 1500 với `renew_days = 3` (`renew_days_at_consume = 3`), **WHEN** patch catalog đổi `renew_days` thành 1, **THEN** có lại tại 2880. **WHEN** patch đổi thành 5, **THEN** có lại tại 5760 (không phải 8640). Giá trị `renew_days_at_consume` qua save / nạp vẫn là 3. (AC-F1.3) `[Logic/EditMode — BLOCKING]`

**F2 — Ba giá trị**
- **GIVEN** lá giả trả T / F / U, **WHEN** đánh giá `All[A, B]`, `Any[A, B]`, `Not(A)` với mọi (A, B) ∈ {T, F, U}², **THEN** từng ô khớp bảng F2 (đối xứng); `result = (root == T)`; ví dụ F2 (`All[T, U]` → `false`, `Any[T, U]` → `true`) đúng. (AC-F2.1) `[Logic/EditMode — BLOCKING]`
- **GIVEN** bộ liệt kê tất định mọi cây sâu ≤ 3 với ≤ 4 lá trên {T, F, U}, **WHEN** đánh giá bằng evaluator thật (có ngắt sớm) và bằng evaluator tham chiếu đầy đủ trong test, **THEN** kết quả trùng ở mọi cây. (AC-F2.2) `[Logic/EditMode — BLOCKING]`

**F3 — Ghi Counter / Stage**
- **GIVEN** `quest.kills` dải [0, 10], và một Counter test dải [0, int32.Max], **WHEN** ghi:
  - Counter: `old 8 + 5`, `old 3 − 20`, `old 2.147.483.000 + 1.000`;
  - Stage: `Add` vào Stage ở `int32.Max − 1` với `+5`, rồi `Add +1` khi Stage đã ở `int32.Max`,

  **THEN** Counter cho 10, 0, `int32.Max`: không exception, log chỉ ở mức debug, `FlagChanged` chỉ khi `new ≠ old`. Stage cho `int32.Max` + log, rồi `Ok(NoChange)`. (AC-F3.1) `[Logic/EditMode — BLOCKING]`

**F4 — Giới hạn**
- **GIVEN** validator và bộ nạp catalog lúc chạy, **WHEN** đưa vào: `Any[All[Not(lá)]]` (sâu 4) và cây sâu 5; cây 16 lá và 17 lá; lô 16 effect và 17 effect, **THEN** sâu 4, 16 lá, 16 effect được nhận; sâu 5, 17 lá, 17 effect bị từ chối. Năm hằng F4 là hằng trong code, không đọc từ data file (reflection / tìm trong schema tuning: không có trường tương ứng). (AC-F4.1) `[Logic/EditMode — BLOCKING]` — trần cascade: AC-R09.4; lô vượt lúc chạy: thay bằng lô luôn `Rejected(Invalid)` + error (Rule 11).

### H.4 Ràng buộc validator

*Mỗi AC dưới đây là test tham số: một fixture tối thiểu cho mỗi vi phạm → **đúng 1** lỗi với mã luật và vị trí; fixture hợp lệ tương ứng → 0 lỗi. Validator là thư viện C# thuần nhận dữ liệu trong bộ nhớ (H-15).*

- **Catalog:**
  - khóa sai định dạng: `Weapon.x`, `weapon`, `weapon-x.a`, `.x`;
  - kiểu `Float` / `String` / `Enum`;
  - Latch mặc định `true`; Stage mặc định −1;
  - Counter `min 5, default 3, max 10`; Counter `min = max` → **warn**, không lỗi;
  - namespace không có trong bảng Rule 5.

  (AC-V01) `[Logic/EditMode — BLOCKING]`
- **Cây điều kiện:**
  - `All[]` / `Any[]` rỗng; khóa chưa khai báo;
  - lá Bool dùng `≥`, hay so với `2`; hằng lá Counter ngoài [min, max];
  - `HasItem ≥ 0`; ngưỡng `Affinity` ngoài dải của NPC Affinity **[P]**.

  (AC-V02) `[Logic/EditMode — BLOCKING]`
- **`usage`:** điều kiện thiếu `usage` → lỗi.
  - Permanent **được** dùng: `Flag(Latch == true)`, `Flag(Stage ≥ n)`, `Quest(q) ≥ n`, `Time(day ≥ N)`.
  - Permanent **bị cấm** (mỗi cái 1 lỗi): `Not`; Stage `==` / `≤` / `≠`; mọi lá trên Counter; `Affinity`; `HasItem`; `Time` dạng `segment` / `IsNight` / `season` / `weekday`.
  - Transient chứa mọi lá trên → 0 lỗi.

  (AC-V03) `[Logic/EditMode — BLOCKING]`
- **Lô effect:**
  - lô thiếu `repeat`; lô `repeat = Once` chứa effect không idempotent mà không có `once_key`;
  - `once_key` không phải Latch, hay thuộc namespace khác nguồn;
  - hai `SetFlag` cùng khóa; `GiveItem` + `TakeItem` cùng vật phẩm; `SetFlag` ngoài namespace nguồn;
  - `StartQuest` / `GiveCurrency` trong profile Fun-Gate.

  (AC-V04) `[Logic/EditMode — BLOCKING]`
- **Vật thể:** `object_id` trùng (kể cả ở hai vùng khác nhau); vật thể thiếu ID; `renew_days` 0 và 29 bị từ chối, 1 và 28 được nhận; tái dùng ID tombstone. (AC-V05) `[Logic/EditMode — BLOCKING]`
- **Cascade tĩnh:** chuỗi author có Counter sâu 9 → lỗi; sâu 8 → 0 lỗi; chuỗi chỉ Latch sâu 20 → 0 lỗi. (AC-V06) `[Logic/EditMode — BLOCKING]`
- **So với baseline đã phát hành** (H-16): đổi `default`; đổi loại Latch → Counter; đổi kiểu; đổi OneTime ↔ Renewable; tái dùng ID tombstone → mỗi cái 1 lỗi. Cùng thay đổi kèm mục migration cho khóa đó → 0 lỗi. (AC-V07) `[Logic/EditMode — BLOCKING]`
- **Build profile:** profile không có provider `Affinity` mà dữ liệu dùng lá `Affinity` → lỗi; profile có provider → 0 lỗi. (AC-V08) `[Logic/EditMode — BLOCKING]`
- **GIVEN** repo có một vi phạm bất kỳ của AC-V01–V08, **WHEN** CI chạy validator ở batch mode, **THEN** exit code ≠ 0, build bị chặn, log nêu mã luật + file + khóa; không cần cửa sổ editor. Repo sạch → exit 0. (AC-V09) `[Logic/CI — BLOCKING]`. Cách gọi validator ở batch mode NOT SOURCEABLE: `commands.test` hiện chạy `-runTests` EditMode; entry riêng hay chạy như một EditMode test thì chốt ở ADR định dạng author.

### H.5 Edge Cases

- **GIVEN** code (không phải dữ liệu author) ghi cùng khóa hai lần trong một lô, **WHEN** `quest.kills` đi 0 → 5 → 0, rồi 0 → 5 → 7, **THEN** lần đầu 0 sự kiện; lần hai đúng 1 sự kiện `(0, 7)`. (AC-E01) `[Logic/EditMode — BLOCKING]`
- **GIVEN** nút "nhận thưởng" có điều kiện `Quest(a) == 1` và lô ghi `quest.a → 2`, **WHEN** chạy lô, **THEN** lô `Applied` và lần hỏi kế của điều kiện → `False`. (AC-E02) `[Logic/EditMode — BLOCKING; Defer: lá Quest hết stub]`

**Bản đồ phủ Edge Cases nguy cơ cao → AC:**

| Edge Case | AC |
|---|---|
| Khóa chưa khai báo qua `Not` → `Unknown` | R04.2 |
| `usage` Permanent | V03 |
| `once_key` bị xếp hàng hai lần | R12.2 |
| `CanApplyAll` cộng dồn | R11.2 |
| Handler lỗi vẫn đặt `once_key` | R11.3 |
| Trần cascade chỉ cho Counter | R09.4, V06 |
| `TryConsume` nguyên tử / thưởng cùng call stack | R13.2, R13.4, C09 |
| Khóa `object_id` khi dời vùng | R13.5 |
| `min()` của `renew_days_at_consume` | F1.3 |
| Snapshot ở điểm yên tĩnh | R15.2, C03 |
| Yêu cầu vòng đời giữa lúc phát | R09.8 |
| S6 → S2 xóa mồ côi + seed mới | S02 |
| `WorldStateReady` sau S4 | R09.7, S03, C07a |
| `Revision` không reset | R08.3 |
| `NotReady` | R08.1 |
| Khóa trùng trong DTO | R15.7 |
| Schema mới hơn / thiếu seed → S5 | R15.6 |
| Latch ↔ Stage fallback | R15.4 |
| Counter kẹp khi khôi phục / Stage không kẹp | R15.5 |
| Provider ném / ID lạ | R07.1 |
| Cây runtime vượt giới hạn | R06.2 |
| Observer hủy giữa lúc phát | R09.5 |
| Lệnh xếp hàng bị từ chối lúc chạy | R09.3 |
| Stage 8 | R11.5, C12 |
| Thread khác | R08.2 |
| Timestamp tương lai / âm | F1.2, R15.6 |
| Ngủ trong vùng | C05 |
| Lá `Time` giữa stage 3–6 | C02 |

### H.6 Cross-system

- **GIVEN** Game Time thật, orchestrator Save/Load giả gọi khôi phục theo thứ tự Time → hệ này → saveable giả (vai Inventory), **WHEN** nạp save có `game_minute = 20.000`, **THEN**:
  - trong callback restore của hệ này, đồng hồ đã bằng 20.000;
  - restore của hệ này hoàn tất trước khi callback của saveable giả bắt đầu (H-17);
  - `IsAvailable` gọi sau S4 dùng đồng hồ đã khôi phục.

  (AC-C01a) `[Integration/PlayMode — BLOCKING]` — loại (a), đối xứng Game Time AC-C08.
- **GIVEN** Save/Load, Game Time, hệ này, Inventory thật, **WHEN** nạp save, **THEN** thứ tự khôi phục là Time → Flags → Inventory. (AC-C01b) `[Integration/PlayMode — BLOCKING; Defer: GDD Save/Load]`
- **GIVEN** provider lá `Time` của Game Time thật và `game_minute` ở 7 ngày × mỗi khoảng giờ, **WHEN** hệ này đánh giá `Time(day ≥ N)`, `Time(segment ∈ …)`, `Time(IsNight)`, và cả trong một phase stage 3–6, **THEN** khớp F2 / F5 của Game Time ở mọi mẫu; trong phase stage 3–6 thì thấy ngày mới, `Morning`, `IsNight = false`. (AC-C02) `[Logic/EditMode — BLOCKING]` — chủ: Flags (đối xứng Game Time AC-C17). `season == S`: `[Defer: Vertical Slice]`.
- **GIVEN** boss ngã → `boss.*` commit, lô thưởng có `once_key` và `GiveItem` nằm trong hàng, autosave được yêu cầu, **WHEN** Save/Load chụp mọi hệ, **THEN** save chứa `once_key = true` **và** vật phẩm trong kho (cùng điểm yên tĩnh); nạp lại không mất và không nhân đôi thưởng. (AC-C03) `[Integration/PlayMode — BLOCKING; Defer: GDD Save/Load]`
- **GIVEN** Game Time đã khôi phục và DTO của hệ này hỏng, **WHEN** nạp, **THEN** (a) hệ này gọi sink thất bại đúng 1 lần, trước khi rời Restoring (AC-C04a, `[Logic/EditMode — BLOCKING]`); (b) Save/Load đưa Game Time và mọi hệ về trạng thái trống, file save hỏng không bị ghi đè (AC-C04b, `[Integration/PlayMode — BLOCKING; Defer: GDD Save/Load]`).
- **GIVEN** `obj.herb_01` dùng lúc 20:00 ngày N và người chơi ở trong vùng, **WHEN** ngủ, **THEN**:
  - (a) Game Time thật + stub Region Loading đăng ký phase stage 3 chỉ khi loại chuyển ngày là `Sleep`: stub gọi `IsAvailable(herb_01, now mới)` → `true`; với rollover không ngủ, stub không được gọi và kết quả cũ giữ nguyên (AC-C05a, `[Integration/PlayMode — BLOCKING]`);
  - (b) Region Loading thật hiện lại ruộng cạnh giường vào sáng hôm sau; thức qua 06:00 thì không hiện cho tới lần nạp vùng kế (AC-C05b, `[Integration/PlayMode — BLOCKING; Defer: GDD Region Loading]`).
- **GIVEN** Inventory thật có 1 `ore`, kho gần đầy, **WHEN** lô có 2 × `TakeItem(ore, 1)`, và lô nhiều `GiveItem` mà tổng cộng làm tràn kho, **THEN** `CanApplyAll` từ chối cả hai, kho đồ không đổi; một `GiveItem` đã qua kiểm thì luôn thành công. (AC-C06) `[Integration/PlayMode — BLOCKING; Defer: GDD Inventory]`
- **GIVEN** save có `rumor.mist = true`, **WHEN** nạp:
  - (a) observer giả nhận `WorldStateReady` đúng 1 lần và đọc được `rumor.mist == true` trong handler (AC-C07a, `[Logic/EditMode — BLOCKING]`);
  - (b) nhật ký Dialogue thật dựng lại và tin đồn hiện là đã gạch; ảnh chụp lưu ở `production/qa/evidence/` (AC-C07b, `[UI — screenshot evidence, BLOCKING; Defer: GDD Dialogue]`).
- **GIVEN** nút hội thoại có `Affinity(mira) ≥ 40` đang hiển thị, **WHEN** affinity giảm còn 30 trước khi người chơi bấm, **THEN** Dialogue đánh giá lại lúc xác nhận, không chạy lô, và nút đóng ở lần hỏi kế. (AC-C08) `[Integration/PlayMode — BLOCKING; Defer: GDD Dialogue]`
- **GIVEN** người chơi mở rương / thu hoạch, **WHEN** vùng unload ở mọi frame trong khoảng tương tác (dịch chuyển, thua, thoát), **THEN** hoặc vật thể còn nguyên và không có thưởng, hoặc `Consumed` và thưởng đã vào túi — không bao giờ "đã đánh dấu mà chưa trao". (AC-C09) `[Integration/PlayMode — BLOCKING; Defer: GDD Interaction, Region Loading]`
- **GIVEN** nhánh vũ khí bí mật đã mở nhờ affinity, **WHEN** affinity giảm dưới ngưỡng (bỏ quà lễ hội), **THEN** nhánh vẫn có trong cây vũ khí; chỉ những thứ chưa mở bị chậm hoặc đắt hơn. (AC-C10b) `[Integration/PlayMode — BLOCKING; Defer: GDD Weapon Tree, NPC Affinity]` — bản (a): AC-R10.1.
- **GIVEN** nhóm quái `obj.pack_01`, **WHEN** người chơi rời vùng giữa trận, rồi ở lần khác hạ con cuối, **THEN** lần 1 không có `TryConsume`, nhóm còn nguyên ở lần nạp kế; lần 2 có đúng 1 `TryConsume` → `Consumed`. (AC-C11) `[Integration/PlayMode — BLOCKING; Defer: GDD Enemy AI]`
- **GIVEN** Game Time thật, **WHEN** một phase stage 3 giả chạy một lô, và một handler `DayStarted` chạy một lô, **THEN** lần lượt `Applied` và `Rejected(Stage8)`; khi lệnh ngoài cùng của phase trả về, hàng đã rỗng. (AC-C12) `[Integration/PlayMode — BLOCKING]` — loại (a); cần H-12.
- **GIVEN** cùng `world_seed`, **WHEN** nạp cùng save 3 lần, **THEN** hình dạng vùng và random theo ngày (spawn, drop) giống nhau từng phần tử. (AC-C13) `[Integration/PlayMode — BLOCKING; Defer: GDD Enemy AI / level, Game Time OQ-7]`
- **GIVEN** bảng chuỗi UI, hội thoại NPC, localization, **WHEN** lint theo danh sách từ cấm ở Player Fantasy › Ngôn ngữ (*khóa lại, thu hồi, mất, reset, hết hạn, bỏ lỡ vĩnh viễn, "missable", "flag", "điều kiện chưa thỏa"* + bản tiếng Anh do writer chốt), **THEN** 0 lần xuất hiện. (AC-C14) `[Config/Data — smoke check]`

### H.7 Performance

Nguồn có được: frame budget 16,6 ms (`.claude/docs/technical-preferences.md` › Performance Budgets); F4: `c_max ≤ 0,5 µs` **[P]**, 0 byte GC mỗi lần đánh giá. **Cách đo allocation và thời gian là NOT SOURCEABLE** (engine reference không có module scripting / testing / profiling). Ứng viên chốt ở ADR: package Performance Testing của Unity, marker `GC.Alloc` trong Profiler, hoặc bộ đếm allocation theo thread của .NET — phải xác nhận có sẵn dưới scripting backend của dự án trước khi dùng (H-17).

- **GIVEN** bộ xấu nhất F4 — 20 điều kiện × 16 lá, trộn `Flag` / `Quest` với provider giả không cấp phát — evaluator đã chạy 1 lượt khởi động, **WHEN** đo allocation managed quanh 1.000 lượt hỏi, **THEN** tổng cấp phát = 0 byte (cả `NotReady` và `Unknown` do thiếu provider; đường lỗi provider ném exception được miễn, F4). (AC-PF1) `[Performance/EditMode — BLOCKING]`
- **GIVEN** cùng bộ đó trên phần cứng tham chiếu (TBD), trong player build cấu hình như bản phát hành (backend theo ADR), **WHEN** đo 1.000 lượt ở 10 Hz, **THEN** thời gian trung bình một lượt ≤ 0,16 ms (= 320 lá × 0,5 µs) và p99 được báo cáo. (AC-PF2) `[Performance/PlayMode — BLOCKING sau khi technical-director xác nhận c_max và phần cứng tham chiếu]`

### H.8 Playtest và bằng chứng fantasy

*"Thần nhớ tên bạn." Bằng chứng khách quan là ảnh chụp (Visual, BLOCKING — chỉ viết được khi các hệ hiển thị đã có). Cảm nhận của người chơi là documented playtest, advisory, chẩn đoán — không phải cổng.*

- **GIVEN** build có Interaction, Dialogue, Region Loading:
  - phiên 1: người test mở 2 rương, lần theo 1 tin đồn, đứng ở 3 điểm mốc của vùng; QA chụp rương, nhật ký, 3 điểm mốc; ghi `world_seed` vào metadata ảnh; lưu rồi thoát hẳn process;

  **WHEN** khởi động lại và nạp (thời gian thực trôi giữa hai lần không ảnh hưởng — hệ không dùng giờ thực), **THEN** ảnh cặp trước / sau cho thấy rương vẫn mở, tin đồn vẫn gạch, 3 điểm mốc cùng hình dạng, cùng seed. Bằng chứng lưu ở `production/qa/evidence/world-state-load-proof-[date]/` + lead sign-off. (AC-PT1) `[Visual — screenshot evidence + lead sign-off, BLOCKING; Defer: GDD Interaction, Dialogue, Region Loading]`
- **GIVEN** kịch bản dựng sẵn: người test đã mở nhánh bí mật nhờ affinity với thợ rèn, rồi quan hệ nguội dưới ngưỡng (bỏ quà lễ hội), **WHEN** vào lò rèn, **THEN** ảnh chụp cây vũ khí có nhánh đó (không khóa, không thông báo "mất"); lưu ở `production/qa/evidence/` + lead sign-off. (AC-PT2) `[Visual — screenshot evidence + lead sign-off, BLOCKING; Defer: GDD Weapon Tree, NPC Affinity]`
- **GIVEN** buổi test Fun-Gate (n = 5), **WHEN** sau buổi test hỏi nguyên văn (a) *"Có lúc nào bạn không chắc một việc đã làm có được tính hay còn giữ lại không? Lúc nào?"* và (b) *"Có lúc nào bạn tránh làm gì vì sợ mất thứ đã có không?"*, kèm chỉ số hành vi (số lần nạp lại cùng save để kiểm tra, số lần quay lại rương / điểm đã mở chỉ để xem), **THEN** `/playtest-report` ghi:
  - số người "có" cho từng câu, kèm trích dẫn nguyên văn;
  - mỗi "có" được phân loại: lỗi hệ này / hệ khác / vật một-lần và vật làm-mới trông giống nhau (UI).

  Không có ngưỡng đạt / trượt. (AC-PT3) `[Playtest — advisory, chẩn đoán]` — anti-fantasy "thế giới hay quên".

### Giới hạn của bộ tiêu chí

- **Kiểm không trọn bằng test của hệ này:**
  - Rule 7 "provider không có tác dụng phụ" và F-2 "hệ tiêu thụ ghi latch trong cùng bước" (Rule 10) là hợp đồng đặt lên hệ khác → **mục review bắt buộc** cho GDD NPC Affinity, Inventory, Game Time (provider) và Weapon Tree, Crafting, Region Loading (latch).
  - Rule 9 "không luật nào dựa vào thứ tự giữa hai namespace" là luật phủ định → mục review cho mọi GDD downstream.
  - F1 "dạng đóng, không lặp theo ngày" được kiểm gián tiếp qua AC-F1.1 (`MAX_GAME_MINUTE`) và review code.
- **Test hook GDD chưa bảo đảm (→ ADR):**
  - H-1 composition root test dựng kho với catalog trong bộ nhớ, không trạng thái static.
  - H-2 giao diện đồng hồ tiêm được (`game_minute`, `day_index`, loại chuyển ngày).
  - H-3 nguồn `world_seed` tiêm được ở S2.
  - H-4 logger tiêm được hoặc `LogAssert`, phân biệt mức debug / warn / error và payload (khóa, handle, ID lô, chuỗi khóa cascade).
  - H-5 tên define build dev (assert ↔ error) — dùng chung với Game Time H-13.
  - H-6 đọc được `Revision`.
  - H-7 đọc được trạng thái vòng đời.
  - H-8 dừng / quan sát giữa lúc restore (AC-R04.3).
  - H-9 trace phát sự kiện + quan sát hàng FIFO (độ dài, thế hệ, tổng lệnh của chuỗi).
  - H-10 serialize DTO tất định ra byte để so sánh; DTO dựng được trong test (hỏng, trùng khóa, schema mới hơn).
  - H-11 sink "nạp thất bại" tiêm được (báo lên Save/Load).
  - H-12 tín hiệu "đang trong stage 8 / trong phase stage 3–6" từ Game Time qua assembly hợp đồng.
  - H-13 tập đóng (loại lá, effect, vật thể, kiểu, loại) là enum đọc được bằng reflection.
  - H-14 kiểm thread tiêm được (ID main thread) để test EditMode giả lập thread khác.
  - H-15 validator là thư viện C# thuần, đầu vào trong bộ nhớ, kết quả có cấu trúc (mã luật, vị trí), cộng một entry batch mode trả exit code.
  - H-16 baseline catalog đã phát hành làm đầu vào validator, kèm định dạng mục migration.
  - H-17 phương pháp đo allocation / thời gian (AC-PF1/2) và quan sát thứ tự restore (dùng chung Game Time H-11).

## Open Questions

[To be designed]
