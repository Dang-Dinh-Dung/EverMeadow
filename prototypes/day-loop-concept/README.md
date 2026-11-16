<!-- PROTOTYPE - NOT FOR PRODUCTION -->
<!-- Question: Do the farm half and the outing half still feed each other when the world is a set of free, daily-regenerated biome maps joined by mid-map branches you must unlock? -->
<!-- Date: 2026-10-28 (round 1) · 2026-10-29 (round 2: world map) · 2026-10-30 (round 3: free maps, branches, fishing) -->

# Concept Prototype: Day Loop (round 3 — map tự do, ngã rẽ, câu cá)

**Vị trí code (ràng buộc biên dịch của Unity):** code chạy được nằm ở
**`Assets/_Prototypes/DayLoop/Scripts/`** (namespace `DayLoopProto`, để không trùng
tên lớp với CombatFeel). Thư mục này giữ tài liệu và log telemetry (`telemetry/`).

## Giả thuyết

> Map khám phá là những dải biome dài, do seed sinh ra và **đổi hình dạng mỗi sáng**.
> Lối sang map khác là **ngã rẽ giữa map**, mở bằng boss, chìa khóa, vật câu được hoặc
> phá tường. Hai đầu map bị **vách dốc hoặc nước** chặn, và nước thì câu cá được. Trong
> thế giới như vậy, nông trại và chuyến đi vẫn nuôi nhau: potion, món cay, cá nướng giúp
> hạ boss; đồ rơi và lõi boss nuôi lò rèn; nông sản và cá đổi ra vàng mua vé. Người chơi
> vẫn quay về nhà đều đặn.
> - **Đúng nếu:** từ ngày 2, phần lớn các ngày là "ngày liên kết" (≥ 1 sản phẩm nông trại
>   dùng cho chuyến đi **và** ≥ 1 đồ rơi tiêu ở nhà), và sau khi tới Làng Cát Vàng người
>   chơi về nhà ít nhất 2 ngày một lần.
> - **Bác bỏ nếu:** bỏ nông trại ≥ 2 ngày liền.

Round 1 (2026-10-28) và round 2 (2026-10-29) đo cùng câu hỏi với thế giới đơn giản hơn;
cả hai **chưa được playtest** trước khi chuyển round.

**Lưu ý phạm vi:** bản này kiểm cùng lúc sinh thế giới, câu cá và nông trại. Nếu trượt,
khó tách nguyên nhân. Câu cá là hệ mới, chưa có trong concept và systems index.

## Thế giới

```
Rừng Sương ─(tường nứt)─ Làng Đồng Xanh (nhà) ─(mở sẵn)─ Đồng Cỏ ─(boss)─ Sa Mạc Cổ ─(chìa khóa)─ Làng Cát Vàng
                                                                              └─(bùa câu được)─ Hang Động
```

**Map khám phá** (Đồng Cỏ, Rừng Sương, Sa Mạc Cổ, Hang Động):
- Seed = `worldSeed` + id + **số ngày**. Trong ngày, quay lại vẫn y nguyên; sang ngày mới thì đổi hết.
- Dài **150–300 ô**, chia thành các **khu sinh thái con** 25–50 ô:

| Map | Khu con | Quái (rơi) | Boss | Công trình làm tay |
|---|---|---|---|---|
| Đồng Cỏ | đồng bằng, đồi, rừng thưa, ao | Slime, Sói đồng | Sói Chúa — **canh ngã rẽ sang Sa Mạc** | Tàn tích trạm gác, Giếng cổ |
| Rừng Sương | rừng rậm (platform), trảng trống, lùm nấm, ao rừng | Nhện rừng, Gấu | Hươu Sương (tùy chọn) | Đền rừng, Nhà cây |
| Sa Mạc Cổ | cồn cát, đá phẳng, **ốc đảo (luôn có)** | Bọ cạp, Xác ướp | Pharaoh — **canh Kim tự tháp** | Kim tự tháp (có Chìa Khóa Cát), Trại đổ nát |
| Hang Động | đường hầm thấp, hang lớn, hồ ngầm | Slime đá, Nhện hang | Nhện Mẹ (tùy chọn) | Phòng thợ mỏ ×2 |

- **Hai đầu map:** vách dốc (cao hơn tầm nhảy) hoặc sông, hồ, biển (không lội qua được; có
  chỗ câu cá ở bờ). Hang Động: vách đá hoặc hồ ngầm. Ao giữa map thì nông, đi qua được và câu được.
- **Làng giữ cố định** theo seed thế giới. Làng Đồng Xanh có sông ở một đầu (câu ngay tại
  nhà); Làng Cát Vàng giáp biển.

**Ngã rẽ** (lối sang map khác nằm giữa map, vị trí đổi theo ngày; mở rồi thì mở vĩnh viễn hai chiều):

| Ngã rẽ | Cách mở |
|---|---|
| Làng Đồng Xanh → Đồng Cỏ | mở sẵn |
| Làng Đồng Xanh → Rừng Sương | **phá tường nứt**: đánh 3 lần, cần vũ khí tier 2 |
| Đồng Cỏ → Sa Mạc Cổ | **hạ Sói Chúa** đứng canh |
| Sa Mạc Cổ → Làng Cát Vàng | **Chìa Khóa Cát** trong rương Kim tự tháp (Pharaoh canh) |
| Sa Mạc Cổ → Hang Động | **Bùa Đá Cổ** câu được ở ao ốc đảo (30% mỗi lần câu cho tới khi có) |

## Câu cá (minigame giữ thanh)

Đứng ở chỗ câu, bấm **F** (hoặc E) để thả câu. Chờ dấu **!** rồi bấm F trong 0,8 giây. Bấm
sớm thì cá chạy. Sau đó **giữ F** (hoặc chuột trái) để nâng vùng xanh, thả ra để hạ, giữ cá
trong vùng cho tới khi đầy thanh vàng. Esc để thu cần. Cá khó thì giãy nhanh hơn.

| Nguồn nước | Cá thường | Cá to |
|---|---|---|
| Sông | cá chép, cá trê | cá lăng |
| Hồ | cá rô | cá chình |
| Biển | cá thu | cá ngừ |
| Ốc đảo | cá cát (+ Bùa Đá Cổ) | cá vàng sa mạc |
| Hồ ngầm | cá mù | cá đèn |

Nồi nấu **[3]**: 1 cá → 1 cá nướng (cá to → 2), hồi 4 HP. Phím **Q** dùng potion trước, hết
thì ăn cá nướng. Quầy chợ **[3]**: cá thường 2 vàng, cá to 6 vàng. Đồng hồ vẫn chạy khi câu.

## Giữ từ round 2

Nông trại 4 ô, nồi nấu, lò rèn (tier 2/3/4 = 3 / 6+1 lõi / 10+2 lõi), combat, Làng Cát Vàng
có quán trọ và dịch chuyển nhanh (8 vàng), bản đồ M, ngày 06:00–22:00 ≈ 8 phút. Rương được
nạp lại cùng map mỗi ngày (rủi ro: loot quá dồi dào).

## Cách chạy

1. Mở project bằng Unity 6.3 LTS (`6000.3.27f1`).
2. **File → New Scene** → Basic (2D).
3. **GameObject → Create Empty**, thêm component **`DayLoopBootstrap`**.
4. Nhấn Play.

**Phím:** A/D đi · Space nhảy · J (hoặc chuột trái) đánh · Shift né · **E** tương tác
(ngã rẽ, rương, trạm) · **F** câu cá · **Q** hồi máu · **R** món cay · **M** bản đồ ·
trong menu **1/2/3** chọn, **E/Esc** đóng · **Tab** số liệu · **F5** đổi seed (chỉ cho
người quan sát).

**Kiểm tra bộ sinh map không cần Play:**
`Unity.exe -batchmode -quit -projectPath . -executeMethod DayLoopProto.DayLoopMapSmoke.Run -logFile -`
(lần chạy 2026-10-30: 5 seed × 6 map × 4 ngày = 120 map, 0 lỗi).

## Cách test

- Người test lý tưởng là người chưa thấy game. Chỉ nói phím. Không bật Tab trước mặt họ.
- Chơi tự do ~30–40 phút (≈ 4–5 ngày).
- Quan sát: họ có tự tìm ra cách mở từng ngã rẽ không, và mất bao lâu? Có quay về rèn để phá
  tường không? Có ngồi câu ở ốc đảo để lấy bùa không? Sau khi tới Cát Vàng, họ ngủ ở đâu?
- Sau buổi chơi hỏi: "Nếu chơi tiếp một ngày nữa, bạn sẽ làm gì trước?", "Map đổi mỗi ngày
  có làm bạn thấy lạc không, hay thích?", "Câu cá có vui không, hay chỉ là việc phải làm?"

## Đọc số liệu

- **Tab:** dòng TÍN HIỆU (ngày liên kết, về nhà sau khi tới Cát Vàng, hoặc BÁC BỎ) và bảng từng ngày.
- **File** `telemetry/session-<thời gian>.txt`, CSV mỗi ngày: các map đã qua, có về nhà không,
  thời gian ở làng / ngoài hoang, potion, món cay, cá nướng, nông sản bán, phân bón, rèn,
  boss, rương, ngã rẽ đã dùng, dịch chuyển, cá câu được / sổng, thời gian câu, cá nấu / bán,
  ngã rẽ mở bằng cách nào và ngày nào.

## Trạng thái

Đang làm — round 3 dựng xong 2026-10-30 (Phase 5 — Implement), chưa playtest.

## Findings

Chưa có — điền khi prototype kết thúc (REPORT.md).
