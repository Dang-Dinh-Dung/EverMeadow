# Quy cách sprite sheet nhân vật — Evermeadow

> **Dành cho:** người vẽ bên ngoài giao sprite sheet nhân vật (người chơi, NPC, quái).
> **Nguồn ràng buộc:** `design/art/art-bible.md` §5 (nhân vật), §8 (chuẩn asset). Khi tài liệu này và art bible lệch nhau, art bible thắng.
> **Phiên bản:** 2.0 — 2026-11-02 (Animator của Unity; mỗi vũ khí của người chơi là một bộ sprite toàn thân riêng)

Mỗi frame là **sprite toàn thân nhân vật**: thân, quần áo, tóc và vũ khí/dụng cụ vẽ chung trong một hình, không tách lớp.
Sheet giao đến được cắt thành từng frame trong Unity (Sprite Editor) rồi đưa vào AnimationClip, Animator của Unity phát. Lưới ô đều và pivot
cố định ở mục 2 giúp cắt nhanh bằng **Grid By Cell Size** và giữ nhân vật không bị giật khi đổi frame. Tốc độ clip được chỉnh
trong Unity; clip đánh tự khớp nhịp vũ khí. Người vẽ chỉ cần ghi chú (mục 7).

---

## 1. Định dạng file

| Mục | Yêu cầu |
|---|---|
| Định dạng | **PNG-32 RGBA**, sRGB, alpha thẳng (không premultiplied) |
| Phong cách | Pixel art, **không khử răng cưa**, alpha chỉ 0 hoặc 255 |
| Kích thước sheet | ≤ **2048 × 2048** px (art bible §8.5) |
| Tên file | ASCII, chữ thường, `snake_case`, không dấu tiếng Việt (art bible §8.3–8.4) |
| Màu | Nhân vật ≤ 16 màu (không tính viền); **viền mực 1 px màu `#2A1E1A`**; cấm `#FF2BD6` (art bible §8.6) |
| Ánh sáng vẽ sẵn | Nguồn sáng trên–trái, ánh sáng trung tính (không nướng mùa hay giờ trong ngày) |

## 2. Lưới ô

Mỗi sheet là một **lưới ô kích thước cố định**. Mỗi **hàng là một clip** (đếm từ trên xuống, hàng đầu là hàng 0), các
frame xếp **từ trái sang phải**, bắt đầu từ cột đầu tiên.

| Loại | Ô (px) | Pivot = điểm giữa hai bàn chân, tính từ góc **dưới–trái** của ô |
|---|---|---|
| Người chơi | 96 × 96 | x = 48, y = 16 |
| NPC | 64 × 64 | x = 32, y = 8 |
| Quái nhỏ / vừa / lớn | 48 / 128 / 192 vuông | giữa ô theo x; y thống nhất cho từng loài |

- **Thân người chơi cao đúng 48 px** (art bible §8.1), bàn chân chạm đường y = 16 của ô. Khoảng trống quanh thân dành
  cho tư thế duỗi, squash/stretch và khăn bay.
- **Mọi frame dùng cùng một pivot ở chân**, để nhân vật đứng yên một chỗ khi đổi frame và đổi clip.
- Nhân vật nên **quay mặt sang phải**; game tự lật khi quay trái. Sheet vẽ quay sang trái vẫn dùng được: bật **Art Faces
  Left** trên component Animated Sprite của nhân vật đó (mọi frame trong một sheet phải cùng một hướng).

## 3. Các hàng (clip)

### Người chơi

| Hàng | Clip | Lặp | Gợi ý số frame | Ghi chú |
|---|---|---|---|---|
| 0 | `idle` | có | 4 | thở nhẹ, khăn lay |
| 1 | `walk` | có | 6–8 | đi bộ, dáng mặc định khi di chuyển (C-13) |
| 2 | `run` | có | 6 | chạy khi giữ nút Run; dáng phải đọc được là chạy (≈ 4 lần chiều cao thân mỗi giây) |
| 3 | `rise` | có | 2 | đang bay lên |
| 4 | `apex` | có | 2 | quanh đỉnh cú nhảy |
| 5 | `fall` | có | 2 | đang rơi |
| 6 | `land` | không | 2 | squash khi đáp |
| 7 | `dodge` | không | 3 | thân kéo dài ngang, khăn duỗi thẳng ra sau (art bible §5) |
| 8 | `attack_ground` | không | 5 | chuỗi trên đất: anticipation → contact → recovery |
| 9 | `attack_air` | không | 4 | thân cuộn tròn, đòn xoay |
| 10 | `attack_dash` | không | 4 | |
| 11 | `plunge` | có | 2 | trục dọc, vũ khí chĩa xuống |
| 12 | `plunge_land` | không | 2 | squash mạnh khi chạm đất |
| 13 | `hitstun` | không | 2 | |
| 14 | `defeated` | không | 1 | |
| 15 | `bonk` | không | 1 | đầu khựng khi cụng trần |

Frame đánh chia ba nhịp theo art bible §5: anticipation 2–3 frame (squash 10–15%, ≥ 2 px), contact 1 frame
(stretch theo hướng đòn), recovery 2–4 frame (khăn rơi trễ 1–2 frame). Ghi lại frame nào thuộc nhịp nào trong ghi chú
giao hàng (mục 7); Unity dùng thông tin đó để khớp frame với thời gian của vũ khí.

### NPC

`idle`, `walk`, `talk`, `work`, `sit`, `emote` (hàng 0–5). Dân làng idle 2–4 frame (art bible §5).

### Quái

`idle`, `move`, `attack`, `hurt`, `die` (hàng 0–4). Từng loài có thể thêm hàng; thống nhất danh sách với dev trước khi vẽ.

## 4. Look: vũ khí của người chơi, quần áo NPC theo mùa

Một **look** là **một bộ sprite toàn thân khác** của cùng nhân vật, phủ **mọi clip**:

- **Vũ khí của người chơi:** đổi vũ khí là đổi look. Mỗi vũ khí (họ vũ khí) là một bộ sheet riêng, vẽ nhân vật cầm vũ khí đó ở
  mọi clip (idle, walk, run, nhảy, né, đánh…). Bộ không vũ khí là bộ gốc.
- **NPC theo mùa:** mỗi mùa có đồ riêng là một look.

Mỗi look **cùng lưới và cùng pivot** với bộ gốc. Số frame mỗi clip **được phép khác** bộ gốc (ví dụ thương có đòn đánh 6 frame,
kiếm 5 frame); clip đánh luôn khớp thời gian đòn của vũ khí. Clip nào look chưa vẽ thì nhân vật giữ sprite cuối của clip trước,
nên hãy giao đủ clip trước khi đưa vào game.

## 5. Đặt tên (art bible §8.4)

```
<loại>_<vùng>_<chủ thể>.png              sheet chính
<loại>_<vùng>_<chủ thể>_<look>.png       look: vũ khí hoặc mùa
```

Ví dụ: `chr_player.png` (không vũ khí), `chr_player_spear.png`, `npc_vil_elder.png`, `npc_vil_elder_winter.png`,
`mon_mfr_slime.png`. Một look có nhiều clip thì có thể tách nhiều file: `chr_player_spear_idle.png`, `chr_player_spear_walk.png`.
Loại: `chr` người chơi, `npc`, `mon` quái, `boss`; vùng: `frm vil mvl mfr arn cmn`.

## 6. Ghép vào Unity *(dành cho người dựng trong project)*

Thư mục theo loại, mỗi nhân vật (hoặc mỗi kiểu thân NPC / loài quái) một thư mục:

```
Assets/Game/Rigs/Player/          player.controller, Base/ (clip gốc), spear/ (look thương), chr_player*.png
Assets/Game/Rigs/NPC/Elder/       npc.controller, Base/, winter/, npc_vil_elder*.png
Assets/Game/Rigs/Creature/Slime/  creature.controller, Base/, mon_mfr_slime.png
```

1. Thả PNG vào thư mục. Lần import đầu, Unity tự đặt thông số pixel art (Sprite Mode Multiple, PPU 16, Point, không nén).
2. Mở **Sprite Editor** ▸ Slice ▸ **Grid By Cell Size** (96×96 người chơi…), Pivot **Custom** đúng điểm chân ở mục 2 ▸ Apply.
3. Chọn thư mục rồi chạy **Evermeadow ▸ Animation ▸ Create Player / NPC / Creature Animator**: tạo Animator Controller có
   sẵn mọi state, tham số và transition, kèm thư mục `Base/` chứa một clip rỗng cho mỗi state (tốc độ frame theo art bible §8.7).
4. Đổ sprite vào clip: mở **Evermeadow ▸ Animation ▸ Sprite Clip Editor**, chọn **Nhân vật** (ví dụ `Player/player`), **Look** (Gốc hoặc một
   vũ khí / mùa) rồi **Clip** (ví dụ `idle`), kéo
   các frame đã cắt hoặc cả file PNG vào vùng thả (từ thư mục nào cũng được). Frame xếp theo tên (`idle_0, idle_1, … idle_10`),
   chỉnh thứ tự nếu cần, đặt **Frames / second**, bấm **Apply**. Mở lại một clip sẽ thấy sprite nó đang có.
   Cách của Unity (cửa sổ Animation) cần chọn một GameObject trong scene có Animator dùng controller này.
5. Lên nhân vật: thêm component **Animated Sprite** (tự thêm Animator và SpriteRenderer), kéo controller vào ô **Controller**
   của Animator. Người chơi cần thêm **Player Animation Driver** ở object Player (hoặc dựng scene thử bằng
   **Evermeadow ▸ Dev ▸ Build Controller Testbed**).
6. Look (vũ khí, mùa): chọn controller, chạy **Evermeadow ▸ Animation ▸ Create Look (Weapon or Season)**. Tool tạo thư mục mới
   (đổi tên thành tên vũ khí, ví dụ `spear`) chứa một clip rỗng cho mỗi clip gốc và một Animator Override Controller. Đổ sprite
   của look vào các clip đó (bước 4), rồi ở **Looks** của Animated Sprite thêm phần tử: **Key** = id vũ khí hoặc mùa, **Controller**
   = override controller vừa tạo. Game gọi `SetWeapon("spear")` (người chơi) hoặc `SetLook("winter")` (NPC).
7. Chỉnh logic chuyển clip (nếu cần) trong cửa sổ **Animator**. Tham số người chơi: `Speed`, `Running`, `Grounded`,
   `VelocityY`, `Action` (0 di chuyển, 1 né, 2–4 đánh đất/không/lướt, 5 cắm xuống, 6 hồi sau cắm, 7 choáng, 8 gục),
   `AttackProgress`, trigger `Land`, `Bonk`. NPC: `Speed`, `Action` (1 talk, 2 work, 3 sit, 4 emote). Quái: `Speed`,
   `Action` (1 attack, 2 hurt, 3 die).

## 7. Checklist giao hàng

- [ ] PNG-32, không khử răng cưa, alpha 0/255, ≤ 2048².
- [ ] Lưới ô đều đúng kích thước của loại nhân vật; bàn chân chạm đường pivot ở mọi frame đứng trên đất.
- [ ] Hàng đúng thứ tự mục 3; frame bắt đầu từ cột đầu, liền nhau.
- [ ] Mỗi frame là hình toàn thân (thân, quần áo, vũ khí vẽ chung).
- [ ] Mỗi look (vũ khí, mùa) đủ mọi clip, cùng lưới và cùng pivot với bộ gốc.
- [ ] Viền mực 1 px `#2A1E1A`; ≤ 16 màu; không `#FF2BD6`.
- [ ] Tên file đúng mục 6.
- [ ] Ghi chú giao hàng: với mỗi clip đánh, frame nào là anticipation / contact / recovery; thời lượng đề xuất (nếu có).
