# Art Bible: Evermeadow

> **Status**: Draft
> **Owned By**: art-director
> **Last Updated**: 2026-10-25
> **Art Director Sign-Off (AD-ART-BIBLE)**: [Not yet reviewed]

`/art-bible` writes these nine sections in this order and replaces each
section's `[To be designed]` line as that section is approved. A section still
holding its placeholder is incomplete; a later run fills only those.

## 1. Visual Identity Statement

*Duyệt 2026-10-25. Xây trực tiếp từ Visual Identity Anchor của concept (Hybrid "Lantern & Mist" + "Relic Silhouette"), cộng hướng HD-2D kiểu Octopath Traveler do người dùng chọn.*

### Luật hình ảnh một dòng

> **"Hơi ấm là nhà; sương rút màu để người chơi mang màu trở lại, và màu ấy là lễ vật."**

Lý do chỉnh so với anchor gốc: bản gốc chỉ có "rút cạn", nên xung đột với tiền đề "sương là hơi thở thần" (World Premise, Giao Ước Thần Rừng). Vế "lễ vật" biến hành động trả màu thành nghi lễ. Phần đầu giữ nguyên văn.

### Nguyên tắc hỗ trợ

1. **Màu bão hòa = thứ quan trọng (và dùng cho ánh sáng, không chỉ sprite).** Dưới HD-2D, độ sáng, bloom và DoF cũng là kênh chỉ dẫn. Trong sương, bloom và màu đậm chỉ dành cho thứ tương tác được.
   - *Design test:* Khi một vật trang trí vừa sáng vừa đậm màu, chọn **giảm bloom/độ bão hòa của nó**.
   - *Pillar:* **1 — Every Weapon Is a Discovery.** Bí mật phải nổi lên từ nền giàu chi tiết.
2. **Silhouette trước, chi tiết sau.** Mỗi vũ khí đọc được bằng bóng dáng ở camera gameplay, trên mọi nền, kể cả khi DoF/bloom bật.
   - *Design test:* Khi chi tiết sprite xung đột với độ rõ của silhouette, chọn **silhouette** (viền tối/rim light, hình chủ đạo riêng).
   - *Pillar:* **2 — Your Build, Your Way.** Người chơi phải nhận ra build của mình và của boss ngay lập tức.
3. **Hình khối và độ sâu theo khoảng cách từ nhà.** Nhà: tròn, mềm, DoF nông và ấm. Vào sương: hữu cơ, gai góc, nhiều lớp parallax. Arena boss: hình học sắc cạnh, DoF tắt để sân đấu sạch.
   - *Design test:* Khi một asset hoặc camera setup chưa rõ thuộc vùng nào, chọn **độ tròn/sắc và độ sâu trường ảnh** theo khoảng cách từ nhà.
   - *Pillar:* **3 — Two Lives, One Loop** (hai thế giới đọc khác nhau ngay) và **4 — Fewer Bosses, Each an Event** (arena là sự kiện).

Pillar 5 (Relationships Change the World) được phục vụ gián tiếp: ánh đèn lồng và màu lễ hội của một nhà thay đổi khi affinity đổi. Chưa đặt thành nguyên tắc riêng; để Section 4 (Color System) quyết.

### Dòng hòa giải sương

> **"Sương không lấy màu đi mà giữ màu hộ thần; mỗi màu người chơi mang về là một lễ vật, và lễ hội chỉ dùng màu của đèn lồng, vàng hổ phách, đỏ son, không bao giờ dùng màu hazard của boss."**

Màu hazard dành riêng cho boss được chốt ở Section 4. Đề xuất: một màu nằm ngoài họ ấm của lễ hội và ngoài các họ nguyên tố.

### HD-2D thay đổi gì

Hướng HD-2D (sprite pixel trong thế giới có chiều sâu, DoF, ánh sáng kiểu 3D, bloom) là quyết định của người dùng ngày 2026-10-25. Hệ quả và rủi ro:

- **Renderer:** URP 2D chuyển sang URP kiểu 3D (sprite trên quad, camera phối cảnh/ortho nghiêng, DoF, bloom, đèn thật). Pixel-perfect camera như concept mô tả không còn nguyên nghĩa → cần ADR camera/renderer.
- **Độ phân giải sprite:** nhân vật 32–48 px dễ bị "mềm" dưới DoF và bloom. Nên thử 48–64 px, nhưng đó là gần nhân đôi khối lượng vẽ, đặc biệt paper-doll ~14 vũ khí và boss → chốt ở Section 8.
- **Đọc silhouette:** nền giàu chi tiết, bloom và DoF làm giảm tương phản vũ khí. Cần rim light hoặc outline và một lớp nền "lùi màu" khi chiến đấu.
- **Màu hazard boss:** bloom/đèn lễ hội dễ lấn sang màu hazard. Khóa màu này khỏi mọi đèn môi trường, và giảm bloom trong arena.
- **Rủi ro solo:** hiệu năng và thời gian tinh chỉnh ánh sáng. Đề xuất prototype một cảnh Farm và một arena trước khi cam kết toàn bộ.

## 2. Mood & Atmosphere

*Duyệt 2026-10-25.*

**Luật chung của section:** mỗi trạng thái được phân biệt đồng thời trên 3 trục — **nhiệt độ màu**, **DoF/bloom**, và **loại nguồn sáng (có hướng / khuếch tán / điểm)**. Hai trạng thái được trùng một trục, không bao giờ trùng cả ba.

| Trạng thái | Cảm xúc mục tiêu | Ánh sáng & HD-2D | Mô tả không khí | Năng lượng | Yếu tố mang mood |
|---|---|---|---|---|---|
| **Farm ban ngày** | "Đất này là của mình" — an toàn, cảm giác sở hữu nhỏ | Nắng có hướng, 5000K buổi trưa → 3200K giờ vàng; tương phản vừa; DoF nông, ấm (tilt-shift nhẹ); bloom thấp, chỉ trên cây chín | thoáng, lộng gió, nắng, mùi đất | 3/10 | Bóng mây trôi qua ruộng; cây chín là thứ bão hòa nhất trên màn hình |
| **Green Field Village** | Thuộc về, được chào đón | Sáng bầu trời khuếch tán + đèn lồng treo cả ban ngày (point light 2200K); tương phản thấp; DoF nông; nhiều nguồn ấm nhỏ | nhộn nhịp, vải đỏ son, khói bếp, ngõ hẹp | 4/10 | Dây đèn lồng của từng nhà (tín hiệu Pillar 5) |
| **Mist Forest** | Tò mò dè chừng, "đang bị nhìn" | Không key light; ambient teal khuếch tán ~8000K; tương phản thấp ở xa, cao ở gần; DoF sâu, 5–7 lớp parallax + fog card; bloom chỉ trên thứ tương tác được | im lặng, ẩm, nhiều lớp, mắt lấp lánh trong lá | 5/10 | Một tia god ray rơi đúng vào một bí mật |
| **Mist Village** | Tĩnh lặng thiêng liêng, chốn nghỉ chân giữa đường | Ambient lạnh 8000K đối 2000K đèn lồng; tương phản cục bộ cao; DoF vừa; bloom chỉ trên đèn lồng | trầm lặng, nghi lễ, cổ xưa, hương trầm | 2/10 | Đèn lồng là màu ấm duy nhất — hơi ấm mượn từ nhà |
| **Đêm** | Hưng phấn của thợ săn, cơ hội — không phải sợ hãi | Trăng có hướng 9000K, rim xanh tím; tương phản cao; bloom tăng trên đồ rơi và mắt quái; ở nhà: đom đóm, cửa sổ 2200K | sắc, lấp lánh, mời gọi, hiểm nguy có thưởng | 6/10 | Đồ hiếm có rim hổ phách sáng hơn ban ngày; quái mạnh hơn = mắt sáng hơn + thêm một lớp rim |
| **Mùa** | Nhịp của cuộc sống, không bao giờ là đếm ngược | Đổi LUT + góc nắng: Xuân đào nhạt mềm, Hạ trưa trắng gắt, Thu giờ vàng dài (đỉnh lễ hội), Đông phẳng sáng với bóng xanh | — | Xuân 3 / Hạ 4 / Thu 5 / Đông 2 | Vật theo mùa ở tiền cảnh (hoa, lá rơi, tuyết); **vùng sương chỉ nhận 25–30% LUT** |
| **Combat thường** | Tự tin, sắc bén, nhịp nhanh | Giữ ánh sáng của vùng, nhưng **nền lùi** (−20% bão hòa) và DoF siết nhẹ vào lớp chơi; bloom chỉ khi trúng đòn (flash trắng 1–2 frame) | gọn, chặt, động | 7/10 | Flash trúng đòn + khăn quàng bay theo người chơi |
| **Arena boss** | Choáng ngợp + tập trung; một nghi lễ trang nghiêm, không phải tai họa | **Tắt DoF**; key light trung tính 6000K từ trên xuống (như sân khấu nghi lễ); nền ép phẳng còn 1–2 lớp ở giá trị trung bình; nâng ngưỡng bloom; màu hazard là màu bão hòa duy nhất ngoài người chơi | trang nghiêm, hình học, hoành tráng, sạch | 10/10 | Vòng đèn lồng quanh arena, **thắp thêm một ngọn sau mỗi phase** (tiến độ diegetic, màu hổ phách, không bao giờ là màu hazard) |
| **Thắng boss** | Được công nhận, biết ơn — không giết chóc | DoF bật lại, ấm; god ray vàng; bloom mở rộng; 6000K → 3500K trong ~3s; màu tràn từ boss ra arena | trang trọng, ấm, giải tỏa | 8 → 2 | Linh thú cúi chào rồi tan vào sương (không xác, không nổ) — thần không chết |
| **Thua** | "Thử lại ngay", không mất gì | Fade ≤1,5s sang **màu giấy đèn lồng kem** (không bao giờ đen hay đỏ); DoF mở nhẹ | nhẹ, ngắn, bao dung | 1/10 | Một ngọn đèn lồng vẫn cháy ở mép khung — nhà vẫn đang chờ |
| **Tìm thấy vũ khí** | "Thần trao lại cho mình" — kinh ngạc | Làm chậm thời gian; DoF **rack focus** vào vũ khí; xung quanh mất thêm bão hòa; vũ khí có rim ngược sáng + là bloom duy nhất | thiêng liêng, lặng, rực rỡ | đỉnh 6 | Silhouette vũ khí phóng to; màu chảy từ sương vào vũ khí |
| **Lò rèn / chế tạo** | Chăm chút, làm chủ, lời hứa | Nội thất; lửa lò 1900K, chiaroscuro tương phản cao; DoF nông; bloom trên tia lửa | ấm, nặng, nhịp búa, khói | 4/10 | Vũ khí trên đe là vật sáng nhất |
| **Menu / cây vũ khí** | Lên kế hoạch thong thả, sưu tập | UI phẳng, không DoF trên UI; game phía sau mờ mạnh và tối 40%; ánh sáng "giấy dưới đèn" | gọn gàng, giấy và mực, bình thản | 1/10 | Nhánh đã tìm = đèn lồng đã thắp; chưa tìm = ô sương xám |

*Ghi chú:* cảm xúc của "Tìm thấy vũ khí" được viết theo Core Fantasy đã chỉnh trong `/design-review` 2026-10-25 (người được thần chọn trao lại vũ khí), không theo câu cũ "người duy nhất tìm ra" ở game-concept.md L38.

## 3. Shape Language

*Duyệt 2026-10-25.*

### Silhouette nhân vật

Silhouette được kiểm ở khoảng cách camera gameplay **với DoF và bloom bật**, tô đặc màu đen trên 3 nền (farm, sương, arena). Mỗi archetype giữ **một nét nhận diện riêng**:

- **Người chơi:** thân tròn (thuộc về nhà). Nét riêng là **khăn quàng màu ấm (hổ phách) bay theo**, đốm ấm, bão hòa, chuyển động duy nhất giữa sương. Khăn cho biết hướng và tốc độ, và là "nhà đi vào sương". Vũ khí phóng đại 1,3–1,5× để build được đọc trước cả khuôn mặt. *(Pillar 2)*
  - Đã cân nhắc khăn đỏ son (gắn người chơi với lễ vật mạnh hơn) và bỏ, vì cặp đỏ son ↔ xanh lá nền hỏng với protan/deutan.
- **Dân làng (NPC):** dáng oval, đầu to thân nhỏ. Nét riêng là **một vật nghi lễ của dòng họ** (mũ, đèn, dụng cụ) — mỗi nhà giữ mảnh giao ước của mình. *(Pillar 5)*
- **Quái:** hữu cơ, bất đối xứng, gai hướng ra ngoài. Nét riêng là **mắt lấp lánh màu nguyên tố của nó**. Mắt là thứ tương tác được nên được phép bão hòa (§1 Nguyên tắc 1). Cấp kích thước đổi silhouette, không chỉ đổi tỉ lệ.
- **Boss linh thú hộ vệ:** đối xứng, hình học, trang nghiêm. Nét riêng là **mặt nạ/vầng hào quang nghi lễ cứng**, xoay hoặc đổi dạng theo từng phase. Cao ≥ 4× người chơi. Sự đối xứng nói "nghi lễ có trật tự", không phải "quái vật nổi điên". *(Pillar 4)*

### Silhouette vũ khí — 5 họ animation, 14 slot loại

> **Đây là slot hình học, không phải danh sách vũ khí.** Tên và danh sách thật chốt ở GDD Weapon Tree (Open Question của concept). Bộ này phân họ theo **hình dáng**, không theo cách đánh: ranged nằm trong Needle (cung kim), catalyst nằm trong Ring (chuông, quạt) — khác với ví dụ "light blade, heavy, polearm, ranged, catalyst" trong concept.

Hình chủ đạo chiếm ≥60% pixel của vũ khí; các loại trong cùng họ khác nhau ở tỉ lệ dài và vị trí dồn khối.

| Họ | Hình chủ đạo | Slot loại | Cảm xúc |
|---|---|---|---|
| **Crescent** | cung cong | kiếm cong, lưỡi hái, song liềm | mềm mại, thu hoạch (nông cụ thành vũ khí) |
| **Slab** | khối chữ nhật nặng | đại kiếm, búa, rìu | sức nặng, bảo vệ |
| **Needle** | đường dài mảnh | giáo, rapier, cung kim | chính xác, tầm với |
| **Ring** | vòng tròn | chakram, chuông nghi lễ, quạt | nghi lễ, nhịp điệu |
| **Chain** | đường chữ S | roi, xích liềm | khó đoán, bồn chồn |

Viền tối 1px + rim light giữ silhouette đọc được dưới bloom (§1, HD-2D).

### Hình học môi trường

Theo §1 Nguyên tắc 3, khoảng cách từ nhà:

- **Nhà:** tròn và elip, mái tranh, đường ngang chiếm ưu thế (nghỉ ngơi), 2–3 lớp parallax.
- **Mist Village:** vùng chuyển tiếp. Mái vẫn tròn, nhưng cột đèn lồng cao thẳng đứng cắt đường chân trời.
- **Mist Forest:** đường dọc và chéo, rễ và cành mép răng cưa, 5–7 lớp.
- **Arena:** vuông và tròn, platform ngang sạch, đối xứng, tắt DoF.

Người chơi cảm được **mình đang cách nhà bao xa mà không cần bản đồ**. *(Pillar 3)*

### Ngữ pháp hình khối UI — "giấy và ấn triện"

- Panel là hình chữ nhật bo góc, chất giấy, viền mực.
- Lựa chọn = **một dấu triện đỏ son**.
- Thanh máu boss là một dây bện hình học cạnh sắc — cùng ngữ pháp với arena.
- HUD combat tối giản, góc cạnh; menu ở nhà tròn hơn. UI theo cùng ngữ pháp khoảng-cách-từ-nhà như thế giới.

### Hero vs supporting shapes

Tương phản giá trị cục bộ cao nhất chỉ dành cho hero shape: silhouette vũ khí, mặt nạ boss, vật tương tác. Nền được nén vào dải giá trị 30–70%.

## 4. Color System

*Duyệt 2026-10-25.*

### Bảng màu chính (6 + 1 dành riêng)

| Màu | ~Hex | Ý nghĩa trong thế giới |
|---|---|---|
| **Hearth Amber** | #E9A23B | nhà, an toàn, phần thưởng |
| **Meadow Green** | #7FA650 | sinh trưởng, nông trại |
| **Festival Vermilion** | #D9442E | lễ vật, nghi lễ, affinity (chủ yếu không phát sáng: vải, sơn, ấn triện) |
| **Mist Teal** | #5E8C8A (giảm bão hòa) | hơi thở thần, màu đang được giữ |
| **Deep Violet** | #3E2F5B | chiều sâu, lớp xa, đêm |
| **Lantern Paper** | #F4E6C8 | ánh sáng trung tính, UI, nền chữ |
| **God Magenta (DÀNH RIÊNG)** | **#FF2BD6** | **chỉ hazard của boss** |

### Ngữ nghĩa màu

- **Nguy hiểm (quái thường):** không dành riêng hue nào. Telegraph = flash trắng + silhouette vung đòn; đạn thường có lõi tối, viền sáng (giá trị đảo, đọc được trên mọi nền).
- **Phần thưởng:** hổ phách + lấp lánh + âm thanh nhặt.
- **Tương tác được:** bão hòa + bloom + nhịp sáng chậm (~1,5s).
- **Bí mật:** "màu rò rỉ" — một mảng nhỏ nơi sương trả lại chút màu, ấm hơn xung quanh. Khám phá chính là luật một dòng hiện ra bằng hình.
- **Affinity:** xem Pillar 5 bên dưới.

### Nhiệt độ màu theo vùng

Nhà 3200–5500K; Mist Village ambient lạnh với point light 2000K; Mist Forest ≥7500K, không key light; arena trung tính 6000K.

**Luật cứng:** ánh sáng môi trường không bao giờ vượt quá hổ phách (**hue ≤45°**), kể cả lửa lò và tia lửa.

### Mùa

Thực hiện bằng LUT/Volume, không vẽ tile mới. Vùng sương chỉ nhận 25–30% mỗi lần đổi.

- **Xuân:** đào nhạt mềm. Hoa phải là **đào giảm bão hòa (#F2C4B8), không bao giờ hồng-magenta**.
- **Hạ:** xanh bão hòa.
- **Thu:** vàng đất – hổ phách; tán lá được phép đỏ son (mùa gặt = lễ hội).
- **Đông:** trắng giấy với bóng xám xanh (không bao giờ azure).

### Họ màu nguyên tố (placeholder)

> Danh sách nguyên tố chốt ở GDD Combat. **Luật giữ nguyên dù danh sách đổi:** mỗi nguyên tố sở hữu một dải hue mà môi trường không dùng, cộng một chữ ký chuyển động của particle.

| Nguyên tố | Dải hue | Chữ ký chuyển động |
|---|---|---|
| Lửa | vàng trắng, 50–60° (trên trần hổ phách ≤45°) | bay lên |
| Băng | azure, 200–215° (teal môi trường phải ≤195° và bão hòa <40%) | rơi thành mảnh góc cạnh |
| Mộc/Độc | lime, 75–90° | nhỏ giọt, bào tử |
| Sét | ultramarine, 235–250° | zigzag |

### Màu hazard boss — God Magenta #FF2BD6

- Nằm ngoài họ ấm (đỏ son cách ~56° trên vòng màu) và ngoài cả 4 dải nguyên tố.
- Là màu bù của xanh lá tán cây.
- Giữ sắc dưới bloom: G≈0, sáng bằng hai kênh, nên không bị trắng hóa như vàng hay cyan.
- Lore: "màu thần giữ", không lễ hội phàm nào dùng.
- Mỗi hazard có lõi trắng và vòng viền tối.
- Đã cân nhắc cyan-trắng điện và bỏ: dễ trắng hóa dưới bloom, sát dải băng azure.

### Bảng màu UI

Giấy #F4E6C8, mực #2A1E1A, đỏ son = chọn/xác nhận, hổ phách = tiền/phần thưởng.

Chỗ khác với thế giới: màu nguyên tố chỉ xuất hiện trên icon cây vũ khí. **UI không bao giờ dùng magenta**; thanh máu boss màu đỏ son – vàng, đóng khung trận đấu như một nghi lễ.

### Pillar 5 — đèn lồng là affinity

Mọi nhà dùng chung họ màu lễ hội; phân biệt nhau bằng **hoa văn/motif**, không bằng hue. Affinity hiện qua trạng thái đèn:

- **Thấp:** than hồng mờ — không bao giờ tắt hẳn, nghĩa là "vẫn hàn gắn được".
- **Trung bình:** hổ phách đều.
- **Cao:** đỏ son, có tua, có bloom.

Số tua/đèn là cue dự phòng không dựa vào màu.

### An toàn cho người mù màu

| Cặp | Hỏng với | Cue dự phòng |
|---|---|---|
| Đỏ son ↔ xanh lá (đèn lồng / tán lá) | protan, deutan | bloom + chuyển động lắc + số tua |
| Vàng lửa ↔ lime | protan, deutan | icon nguyên tố + chữ ký chuyển động |
| Magenta ↔ tím / ultramarine | deutan | arena không dùng tím; lõi trắng + viền; hình đạn riêng |
| Magenta ↔ đỏ son | **tritan** | đỏ son trong arena chỉ là trang trí ở giá trị nền; âm thanh xuất hiện riêng cho từng hazard |
| Băng azure ↔ teal / xanh | tritan | mảnh băng góc cạnh khác sương mềm |
| Hổ phách phần thưởng ↔ xanh lá | protan (nhẹ) | hình lấp lánh + âm nhặt |

**Quyết định 2026-10-25:** không có tùy chọn preset màu hazard. Cặp magenta ↔ đỏ son với tritan chỉ dựa vào cue dự phòng (lõi trắng, viền tối, âm thanh, đỏ son ở giá trị nền). Nếu playtest cho thấy người tritan vẫn lẫn, xem lại quyết định này.

### Chi phí cho solo dev (chuyển sang technical-artist ở §8)

1. Rim light trên sprite nhân vật làm bằng **cạnh alpha theo hướng đèn trong screen-space, không dùng normal map** (sửa theo §8, 2026-10-25: normal tự sinh cho ~130 frame sẽ nhấp nháy giữa các frame và bị pillow shading ở 48 px).
2. 5–7 lớp sương: tái dùng fog card.
3. Mùa và nguyên tố bằng LUT/palette swap, không vẽ lại.
4. 13 trạng thái ánh sáng = 13 Volume profile → prototype Farm + arena trước (như §1).

## 5. Character Design Direction

*Duyệt 2026-10-25.*

### Người chơi: archetype và tùy biến

Người chơi là **"người mới đến cày lại đất bỏ hoang"**: thân tròn, nông phục giản dị ở giá trị trung bình, và **khăn hổ phách là mảng bão hòa duy nhất trên cơ thể**. Trang phục cố ý nhạt để vũ khí (1,3–1,5×) và khăn chiếm thị giác. Bản sắc người chơi là build chứ không phải khuôn mặt (Pillar 2).

**Tùy biến (quyết định 2026-10-25): không có character creator.** Chỉ palette swap tóc, da và áo bằng shader, không vẽ thêm frame nào. Khăn luôn màu hổ phách vì nó là cue định vị. Lý do: mỗi bộ thân thêm vào nhân toàn bộ animation paper-doll — với solo dev, chi phí tăng tuyến tính và không có trần. Đã cân nhắc thêm 2 bộ thân và bỏ.

### Phân biệt theo loại: 4 kênh

| Loại | Hình | Giá trị | Chuyển động | Màu |
|---|---|---|---|---|
| **Người chơi** | tròn | tương phản cục bộ cao nhất (viền tối + highlight >80%) | nhanh, phản hồi ngay; khăn là secondary motion liên tục | khăn hổ phách + vũ khí |
| **Dân làng** | oval, đầu to | trung bình, thấp hơn người chơi | chậm, idle 2–4 frame, không bao giờ lao về phía người chơi | họ màu lễ hội; vật nghi lễ là điểm nhấn |
| **Quái** | hữu cơ, bất đối xứng | thân tối (15–35%), mắt sáng | giật, không đều, anticipation rõ | chỉ mắt mang màu nguyên tố |
| **Boss** | đối xứng, hình học, ≥4× | khối phẳng trung bình; mặt nạ có tương phản cao nhất | chậm, như nghi thức, telegraph dài | God Magenta chỉ ở hazard, không bao giờ ở thân |

Concept không có đồng minh chiến đấu (single-player, không companion), nên không có hàng cho đồng minh. Nếu sau này thêm companion, nó dùng ngữ pháp NPC + rim hổ phách và không bao giờ có mắt nguyên tố.

**Test:** bỏ một kênh bất kỳ (ví dụ grayscale làm mất màu) thì 3 kênh còn lại vẫn phải đủ để phân biệt. Đây cũng là dự phòng cho người mù màu, đồng bộ với §4.

**Luật viền:** mọi nhân vật trên lớp chơi có viền tối 1px; prop môi trường **không có viền**. Đây là cách tách mạnh nhất khỏi texture nền (xem §6).

### Biểu cảm và tư thế

Mục tiêu: **"cường điệu ở key pose, tiết kiệm ở giữa."** Đòn combat chia 3 nhịp:

- **Anticipation**, 2–3 frame: squash 10–15%, thân co về phía ngược với hướng đòn.
- **Contact**: 1 frame giữ + hit-stop, stretch theo hướng đòn, smear frame cho vũ khí.
- **Recovery**, 2–4 frame: khăn rơi trễ 1–2 frame.

Ở cỡ 48 px, squash phải lệch ít nhất 2 px mới thấy. Mắt chỉ 2 px nên khuôn mặt không mang thông tin gameplay; cảm xúc được nói bằng tư thế toàn thân và portrait. NPC nhún mềm theo nhịp lễ hội. Boss **không squash mềm**: nó đổi dạng bằng cách xoay hoặc tách mảnh hình học, và anticipation dài hơn quái để giữ chất "nghi lễ trang nghiêm" (§2).

### Chiều cao mục tiêu (chốt ở §8, 2026-10-25)

**Người chơi đúng 48 px** (không tính vũ khí, = 3 tile 16 px) ở độ phân giải tham chiếu **640×360**, khoảng 13% chiều cao màn hình. Một con số cố định thay cho dải 44–48 vì paper-doll cần một khung cố định (§8.1).

- Concept ghi 32–48 px. §1 cảnh báo 32 px bị mềm dưới DoF và bloom; 64 px gần như nhân đôi khối lượng vẽ.
- 48 px giữ được mắt 2 px, khăn rộng 3–4 px, và vũ khí ở 1,4× dài ~60–67 px, vẫn đọc được silhouette.

| Loại | Chiều cao |
|---|---|
| Dân làng | 40–52 px |
| Quái nhỏ / vừa / lớn | 24–36 / 48–80 / ≥96 px |
| **Boss** | **≥192 px** (~53% màn hình ở 640×360) |

Boss ≥192 px nghĩa là arena cần camera lùi ra hoặc boss tràn khung có chủ đích. Ở 480×270 boss chiếm ~71% màn hình — một lý do không chọn 480×270.

**Rủi ro solo:** vẽ tay từng frame cho boss ≥192 px là chi phí lớn nhất của dự án. Đề xuất dựng boss từ mảnh (cutout hoặc 2D skeletal), vì hình học đối xứng hợp với animation theo mảnh → technical-artist xác nhận ở §8.

### LOD: thứ gì còn đọc được dưới DoF/bloom

Lớp chơi luôn nằm trong mặt phẳng nét; DoF không bao giờ làm mờ nhân vật đang tham gia trận.

- **Còn đọc được:** silhouette, khăn, vũ khí, mắt quái, vật nghi lễ của NPC, mặt nạ boss.
- **Mất đi:** nếp vải, hoa văn nhỏ hơn 2 px, chi tiết khuôn mặt.
- **Luật:** cụm pixel mang thông tin phải ≥2×2 px; chi tiết nhỏ hơn chỉ được coi là texture.
- **Bloom:** nhân vật không phát sáng, trừ mắt quái (ngưỡng cao) và vũ khí trong khoảnh khắc được trao. Khăn bão hòa nhưng **không emissive**, để không bị nhầm với vật tương tác.

### Paper-doll và 4 ngữ cảnh animation

Theo concept: mỗi họ vũ khí có chuỗi đòn trên đất riêng (5 họ); đòn trên không, lướt và cắm xuống dùng chung, chỉ đổi VFX theo họ; thêm một đòn ngữ cảnh đặc trưng cho mỗi họ. **Mỗi ngữ cảnh có silhouette thân riêng:**

- **Ground:** cung đòn ngang, chân trụ vững. Frame contact duỗi hết hình chủ đạo (≥60%) của họ vũ khí.
- **Air:** thân cuộn tròn, đòn xoay, khăn bay vòng.
- **Dash:** thân kéo dài ngang, khăn duỗi thẳng ra sau — đường ngang duy nhất trên người chơi.
- **Plunge:** trục dọc, vũ khí chĩa xuống, squash mạnh khi chạm đất kèm bụi. Đọc như một cú "đóng ấn triện".

Vũ khí là **sprite cứng gắn socket, xoay theo từng frame**. Chỉ vẽ riêng smear frame và các pose đặc trưng. Ở frame idle, vũ khí không được che mặt hay khăn.

**Quyết định 2026-11-01 (người dùng, thay quyết định cùng ngày về lớp sheet và socket) — sprite toàn thân:** mỗi frame của
người chơi, NPC, quái là **một sprite toàn thân** (thân, quần áo, vũ khí vẽ chung), không tách lớp, không vật gắn socket.
**Trang bị không đổi hình nhân vật.** Biến thể duy nhất là bộ sprite toàn thân khác cùng lưới và cùng frame (quần áo NPC theo
mùa). Đoạn "vũ khí là sprite cứng gắn socket" ở trên không còn áp dụng. Quy cách giao file:
`design/art/specs/sprite-sheet-spec.md` v1.2.

**Quyết định 2026-11-02 (người dùng): đổi vũ khí là đổi sprite nhân vật.** Mỗi họ vũ khí là một bộ sprite toàn thân riêng
phủ mọi clip (Animator Override Controller trong Unity); bộ không vũ khí là bộ gốc. Quần áo NPC theo mùa dùng cùng cơ chế.

Ước lượng (tạm): ~110–130 frame một bộ. **Rủi ro đã chấp nhận:** 5 họ vũ khí + bộ không vũ khí ≈ 6 bộ ≈ 660–780 frame người
chơi, rủi ro art lớn thứ hai sau boss. Cách giảm: các clip không thấy rõ vũ khí (hitstun, defeated, bonk) dùng chung giữa các
look bằng cách để override controller trỏ về clip gốc.

### Portrait và hội thoại

**Quyết định 2026-10-25:** **bust portrait pixel (~96–128 px) chỉ cho trưởng các dòng họ giao ước và NPC chủ chốt**, mỗi người 3 biểu cảm (bình thường, vui, lo). Dân thường dùng sprite in-world kèm khung giấy. Mỗi portrait có vật nghi lễ của nhà và một đèn lồng affinity ở góc khung, theo trạng thái ở §4. **Boss không có portrait**: thần nói qua mặt nạ ngay trong thế giới.

Đã cân nhắc portrait cho mọi NPC (~30–36 bức ở 1.0) và không có portrait; chọn mức giữa.

## 6. Environment Design Language

*Duyệt 2026-10-25.*

### Kiến trúc: giao ước được xây thành nhà

Một ngữ pháp chung: **người xây tròn, thần xây hình học**, và Mist Village là nơi hai ngữ pháp gặp nhau.

- **Farm / Green Field Village:** gỗ, mái tranh tròn, nền đá thấp, vải đỏ son. Dây đèn lồng nối các nhà như một lễ hội không bao giờ tháo. Mỗi nhà có hốc thờ ở cửa mang **motif dòng họ**, trùng với vật nghi lễ NPC của nhà đó cầm. Đường ngang chiếm ưu thế, cửa thấp, mọi thứ trong tầm với.
- **Mist Village:** dạng nguyên bản của giao ước. Cũ hơn, phong hóa hơn, nhiều đá, ít vải; có miếu dâng lễ nhỏ và cột đèn lồng cao thẳng đứng. Gỗ đã ngả xám teal, và đèn là màu ấm duy nhất.
- **Mist Forest:** hầu như không có kiến trúc, chỉ còn tàn tích: cột mốc giao ước bị rễ nuốt, cổng gỗ đổ, dây lễ mục quấn quanh thân cây. Thiên nhiên đã thắng, con người chỉ để lại dấu vết.
- **Arena:** kiến trúc của thần. Vòng đá hoặc gỗ đối xứng, bậc đồng tâm, vòng đèn lồng (§2). Đọc như sân khấu lễ hội được thần dựng lại bằng hình học thuần.

### Texture: pixel vẽ tay trên mặt phẳng

Mọi bề mặt là pixel art point-filtered trên quad hoặc khối đơn giản. Chiều sâu đến từ ánh sáng, DoF và parallax, không đến từ material.

**Không dùng PBR**, vì hai lẽ: roughness và specular phản ứng ở mức dưới một pixel nên phá vỡ cụm pixel, và mỗi asset sẽ cần thêm 2–3 map.

Mật độ texel: 1 texel = 1 pixel tham chiếu ở lớp chơi. Lớp xa được phép vẽ thô hơn vì DoF đằng nào cũng làm mờ.

**Normal map theo tầng:**

| Tầng | Normal map |
|---|---|
| Bề mặt lớn ở lớp chơi gần nguồn sáng (tường, nền cạnh đèn, lò rèn) | có — tự sinh, chỉnh tay khi cần |
| Prop nhỏ và lớp giữa | không — bóng vẽ sẵn vào sprite |
| Lớp xa và fog card | không, và không nhận point light |
| Nhân vật | **không** — outline vẽ sẵn + rim theo cạnh alpha (§4, §8) |

Đánh đổi: vẽ tay normal cho mọi tile tốn thêm ~1,5–2× thời gian. Giữ normal map ở 10–15% diện tích màn hình là đủ để cảnh trông "3D".

### Mật độ prop

Ba thứ quyết định mật độ: combat phải đọc được, nền bị nén vào dải giá trị 30–70% (§3), và vật tương tác được bão hòa (§1).

**Play lane** là dải ±1 chiều cao nhân vật quanh nền đi được:

- Prop trang trí chiếm tối đa 15–20% diện tích.
- Không đặt prop trang trí cạnh vật tương tác cùng hue.

**Luật legibility:** texture môi trường ở lớp chơi không có cụm nào lớn hơn 4×4 px mà giá trị dưới 20% hoặc trên 80%. Hai đầu cực của dải giá trị thuộc về nhân vật (viền tối, highlight — §5).

- Tiền cảnh che khuất (occluder) phải tối, giảm bão hòa, chỉ nằm ở mép khung, và mờ alpha khi che người chơi.
- Mỗi màn hình có tối đa 3 vật tương tác đang sáng nhịp.

| Vùng | Nền | Play lane | Parallax (tính cả tiền cảnh) |
|---|---|---|---|
| Farm | vừa | thưa (ruộng là sân chơi) | 2–3 + 0–1 |
| Green Field Village | cao (vải, đèn) | vừa | 2–3 + 1 |
| Mist Village | vừa | thưa | 3–4 + 1 |
| Mist Forest | cao | thưa nhất | 5–7 (tái dùng fog card) |
| Arena | tối thiểu | trống | 1–2, ép phẳng |

### Kể chuyện bằng môi trường (không dùng chữ)

- **Giao ước:** motif dòng họ lặp lại ở cửa nhà, ở vật nghi lễ, ở miếu trong sương và trên mặt đá arena. Người chơi tự nối chuỗi "nhà này → miếu này → thần này".
- **Vũ khí bị quên:** trong làng, chúng hiện ra **bằng sự vắng mặt** — giá treo trống, vệt hình vũ khí trên tường quét vôi, móc trống trong lò rèn. Trong sương, vũ khí cắm trong rễ hoặc đá, buộc dây lễ, chỉ hiện silhouette tối và chưa có màu cho đến khi được trao.
- **Báo trước boss:** mặt nạ lễ hội trong làng là bản mềm hóa của mặt nạ linh thú.
- **Affinity:** ngoài đèn ở §4, nhà có affinity tăng thì có thêm lễ vật trước cửa, vải mới, hốc thờ sáng hơn. Đọc được bằng số lượng, không cần hue.
- **Bí mật = màu rò rỉ:** một mảng ấm nhỏ, hoa còn giữ màu, một tia god ray. Không bao giờ dùng chữ, không bao giờ dùng magenta.

### Tileset theo vùng

Mỗi vùng có **một tileset gốc mang dải màu riêng** — Farm/Village, Mist Village, Mist Forest — cộng **một kit arena modular** dùng chung cho mọi boss; mỗi boss chỉ đổi motif và palette. Tổng cộng 4 bộ.

- Mùa và thời gian trong ngày: đổi bằng LUT (§4).
- Biến thể trong một vùng: palette swap + bộ prop khác, không làm tileset mới.
- Mist Village tái dùng mảnh của Village, phong hóa và đẩy palette sang teal.

### Rủi ro chi phí solo

1. Mist Forest 5–7 lớp parallax: tái dùng fog card, vẽ lớp xa thô.
2. Normal map: chỉ làm cho bề mặt lớn ở lớp chơi.
3. Boss ≥192 px và vũ khí paper-doll (§5): cần prototype trước khi sản xuất.

## 7. UI/HUD Visual Direction

*Duyệt 2026-10-25. Soạn bởi art-director, kiểm tra UX bởi ux-designer; bốn chỗ bất đồng do người dùng quyết (xem "Xung đột art ↔ readability" cuối section).*

**Luật chung:** UI theo cùng ngữ pháp khoảng-cách-từ-nhà với thế giới (§3).

- **Ở nhà và trong menu, UI là giấy:** panel #F4E6C8 bo tròn, đặt trên game đã được làm mờ và tối 40% (§2).
- **Trong combat, UI là mực:** nền #2A1E1A bán trong suốt, nét giấy mảnh, góc cạnh. Lý do: hazard boss có **lõi trắng** (§4), nên một mảng giấy sáng ở mép màn hình sẽ tranh giá trị với hazard.
- **UI render sau post-processing:** không DoF, không bloom, không LUT mùa/seasonal grading (nếu không, mùa Đông và Thu sẽ đổi luôn màu giấy). UI không bao giờ dùng magenta.
- UI chạy theo **unscaled time**, để hit-stop và slow-motion không làm UI đứng.

### Diegetic vs screen-space

Cách chọn: thông tin phải đọc được **trong một nhịp né** thì đặt screen-space, ở vị trí cố định. Thông tin về nhịp sống và quan hệ thì đặt diegetic, vì nó không khẩn cấp và tự kể được chuyện.

| Yếu tố | Vị trí | Lý do |
|---|---|---|
| Máu người chơi | Screen-space, trên-trái, thanh mực góc cạnh, trên plate | Combat nhanh hơn HK nên mắt không được phải đi tìm. Không gắn vào khăn: khăn là cue định vị (§3). |
| Né/dash (nếu GDD Combat có charge) | Pip nhỏ ngay dưới thanh máu | Cùng cụm, đọc trong một lần liếc. Không có stamina thì không có thanh stamina. |
| Vũ khí + skill stone | Dưới-trái: icon silhouette họ vũ khí + 2–3 stone | Pillar 2: build luôn phải nhìn thấy được. |
| Đồng hồ ngày/mùa | Trên-phải: mặt số mặt trời/trăng trong vành motif mùa | Lịch chạy liên tục, nhưng kênh chính vẫn là ánh sáng (§2). Không đỏ, không nháy, không đếm ngược. |
| Tiền | Chỉ hiện khi thay đổi (3s); luôn hiện trong menu | Không tranh chỗ trong combat. |
| Máu boss | Đáy giữa: dây bện, **ruột mực/hổ phách, viền đỏ son**; nút thắt + **phase pips** tại mỗi ngưỡng phase | Phase pips là nguồn thông tin chính; vòng đèn lồng arena (§2) là phần thưởng cảm xúc, báo lặp lại. |
| Prompt tương tác | Neo theo vật, vẽ ở lớp UI: thẻ giấy + glyph phím/nút theo thiết bị đang dùng | Mỗi lúc chỉ một prompt (vật gần nhất). |
| Affinity | Diegetic (đèn, lễ vật ở cửa — §4, §6) + đèn ở góc portrait (§5) | Pillar 5 được đọc qua thế giới. Cue chính trên portrait là **số tua** (≥2×2 px), không phải hue. Tên người nói luôn hiển thị. |
| Số damage | **Mặc định tắt**; tùy chọn Off / Crit / All | Pattern dày kiểu Calamity biến số bay thành nhiễu. Người theorycraft (Pillar 2) có tùy chọn bật và **cột thử đòn ở lò rèn** để so build. |

Khi bật số damage: numeral pixel màu mực, viền giấy, sống 400 ms, trôi lên, gộp theo mục tiêu, tối đa ~6 số cùng lúc. Crit chỉ to hơn, không đổi hue. **Số damage không bao giờ màu trắng** (lõi hazard boss là trắng).

### Typography

**Tính cách:** "nét chữ người chép sổ lễ" — ấm, humanist, có chân nhẹ, không theo kiểu fantasy trang trí. Font display có nét bút lông chỉ dùng cho tiêu đề cỡ lớn.

**Hướng lai (quyết định 2026-10-25):**

- **Body, hội thoại, mô tả vũ khí: font smooth**, render ở độ phân giải gốc của màn hình, không qua render target 640×360. Lý do: tiếng Việt có dấu chồng hai tầng (ế, ặ, ữ, Ỗ); pixel font cỡ nhỏ không có chỗ cho dấu trên chữ hoa.
  - Ứng viên để thử: Be Vietnam Pro, Noto Serif/Sans. **Chưa kiểm** độ phủ dải Latin Extended Additional (U+1EA0–1EF9) và giấy phép OFL — kiểm trước khi chốt.
- **Numeral HUD, đồng hồ, số damage, nhãn ngắn không dấu: pixel font**, scale nguyên 3×/4× như sprite.
- Đã cân nhắc toàn pixel font với ~134 glyph dấu tự vẽ (đề xuất của ux-designer) và bỏ, vì chi phí vẽ và kiểm cho solo dev.

**Cỡ chữ** (px tham chiếu ở 640×360; ×3 = 1080p, ×4 = 1440p):

| Cấp | Ref px | 1080p | 1440p | Weight |
|---|---|---|---|---|
| Tiêu đề menu | 16 | 48 | 64 | Semibold |
| Hội thoại / body | 11 | 33 | 44 | Regular–Medium |
| Nhãn phụ | 9 | 27 | 36 | Medium |
| **Tối thiểu tuyệt đối** | **9** | **27** | **36** | Medium |

- Cap height của body ≥8 ref px (24 px ở 1080p) — chơi gamepad trên TV/Steam Deck nghĩa là ngồi xa màn hình.
- Line-height ≥1,45× cho body tiếng Việt, để dấu tầng trên không chạm descender của dòng trên.
- Không dùng weight Light/Thin: nét mảnh biến mất trên vân giấy.
- Tùy chọn cỡ chữ 100/125/150%.
- **Chữ smooth tính cỡ theo chiều cao màn hình thật**, không theo hệ số scale của world (sửa theo §8, 2026-10-25). Lý do: ở 1366×768 world scale là 2×, nên body 11 ref px chỉ còn 22 px, dưới mức tối thiểu 27 px ở trên. Mức tối thiểu 27 px (body) áp dụng ở mọi độ phân giải ≥1080p; dưới 1080p giữ ≥24 px.
- **Text expansion:** để dư ≥35% chiều ngang giữa tiếng Việt và tiếng Anh; không dùng label width cố định; tooltip wrap và cuộn được.

### Contrast

Tỉ lệ tính từ hex §4 theo WCAG relative luminance (chưa đo trong game):

| Cặp | Tỉ lệ | Dùng cho |
|---|---|---|
| Mực #2A1E1A trên giấy | 13,1:1 | mọi chữ trên giấy |
| Đỏ son #D9442E trên giấy | 3,5:1 | đồ họa, icon, chữ rất lớn — **không dùng cho body text** |
| Đỏ son sẫm ~#A8301F trên giấy | 5,5:1 | khi bắt buộc chữ màu đỏ son |
| Hổ phách #E9A23B trên giấy | 1,75:1 | **chỉ icon** (vd. đồng xu); số tiền trên giấy viết bằng mực |
| Hổ phách trên nền mực | ~7,5:1 | được dùng cho chữ trong HUD combat |

- **Luật:** chữ trên giấy luôn màu mực; màu chỉ dành cho icon.
- HUD có chữ đặt trên **plate**: chữ ≥4,5:1 so với plate; thanh/icon ≥3:1; plate có viền tối 1 px.
- **Vân giấy:** vùng có chữ là nền giấy phẳng; texture chỉ ở viền và lề, dao động giá trị ≤5%.

### Iconography

- Pixel art, point-filter, scale nguyên. HUD dùng 16×16 ref px (48 px ở 1080p); menu và cây vũ khí dùng 24×24.
- Viền mực 1 px như nhân vật (§5), vì icon thuộc lớp tiền cảnh.
- **Silhouette trước:** icon vũ khí là hình chủ đạo của 5 họ (§3), đọc được cả khi chuyển grayscale.
- 2–3 tone mực/giấy + **một** màu nhấn. Màu nguyên tố chỉ xuất hiện trên icon cây vũ khí và luôn đi kèm glyph nguyên tố (dự phòng mù màu, §4).
- Skill stone: đá tròn khắc glyph, **thân mực, glyph sáng**. Không bao giờ thiết kế dạng "tâm trắng + vòng tối" — đó là chữ ký hazard.
- Glyph input prompt đơn sắc màu mực (không dùng ký hiệu màu của PS trên nền giấy); đổi ngay theo thiết bị dùng gần nhất (keyboard, Xbox, PS, Steam Deck) và hiện đúng phím sau khi rebind.

### Cây vũ khí: ngữ pháp riêng, 5 trạng thái (quyết định 2026-10-25)

Đèn lồng đã gánh affinity (§4) và phase boss (§2), nên cây vũ khí **không dùng đèn làm trạng thái** — đèn chỉ là lớp trang trí ở đó. Trạng thái được đọc bằng **hình node + badge**:

| Trạng thái | Cue |
|---|---|
| Chưa biết / có tin đồn | ô sương xám; tin đồn = viền nét đứt |
| Đã tìm | node có silhouette họ vũ khí, giá trị trung bình |
| Đủ nguyên liệu | node sáng + viền mực dày |
| **Thiếu đúng 1 nguyên liệu** | badge "−1" (hook "thêm một chuyến") |
| Đã rèn / đang trang bị | dấu triện (đang trang bị) |

Mọi trạng thái phải qua grayscale test, như luật §5. Câu "nhánh đã tìm = đèn đã thắp" ở §2 (Menu / cây vũ khí) chỉ còn là trang trí; §7 là nguồn cho trạng thái.

### Focus, chọn và xác nhận (quyết định 2026-10-25)

Ba trạng thái phải khác nhau:

- **Focus** (con trỏ đang ở đó): khung mực dày 2 px + offset/scale nhẹ + con trỏ bút lông; chênh ≥3:1 so với ô không focus. Chuột hover thì focus luôn.
- **Đã chọn / đang trang bị:** dấu triện đỏ son.
- **Xác nhận:** animation "đóng triện" (bên dưới). Hành động phá hủy (bán, salvage) phải **giữ nút để xác nhận**.

Đã cân nhắc dùng chung dấu triện cho focus và đã chọn (bản nháp art-director) và bỏ: người chơi không biết con trỏ đang ở đâu trên lưới đã có món trang bị, và đỏ son ↔ hổ phách chỉ chênh ~2:1 về độ sáng với protan/deutan.

### Điều hướng (gamepad, keyboard, chuột)

- **Cây vũ khí không theo lưới:** mỗi node có navigation graph khai báo tay cho cả 4 hướng; Unity tự suy theo vị trí không đủ. LB/RB đổi tab theo 5 họ vũ khí; LT/RT nhảy giữa các nhánh; right stick pan/zoom. Camera luôn theo focus; focus được nhớ riêng cho từng màn hình.
- **Kho đồ và lò rèn:** quy tắc wrap rõ ràng, LT/RT lật trang, một nút sort/filter, giữ nút để so sánh với món đang trang bị.
- **Remap** đầy đủ cho keyboard và gamepad; điều hướng menu tách khỏi điều khiển gameplay.

### Chuyển động UI

| Ngữ cảnh | Thời lượng | Easing |
|---|---|---|
| Menu nhà mở/đóng (giấy mở ra) | 200–250 ms | ease-out cubic, overshoot ≤4% |
| Di chuyển focus | 80–120 ms | ease-out |
| **Đóng triện xác nhận** | 120 ms ép + 60 ms nảy | ease-in, squash 1 frame; cùng nhịp "đóng ấn" với đòn plunge (§5) |
| HUD combat | ≤100 ms | ease-out, **không bounce** |
| Mất máu | phần mất trừ ngay; vệt mực giữ 300 ms rồi rút trong 400 ms | linear |
| Prompt hiện/ẩn | 120 ms | fade ease-out |

Tùy chọn **Reduce Motion:** bỏ overshoot và squash, giữ fade.

### UI trong arena boss

1. Đồng hồ, tiền, toast và quest ẩn đi; thông báo xếp hàng, hiện sau trận. Chỉ còn máu + pip né (trên-trái), vũ khí (dưới-trái), dây máu boss (đáy giữa).
2. Mọi HUD nằm trong safe frame ở mép màn hình, tổng diện tích ≤8%.
3. **Vùng cấm:** không spawn hazard trong ô HUD; arena thiết kế để dải HUD nằm ngoài vùng đi lại được. Dự phòng: khi hazard hoặc người chơi chồng lên một ô HUD, ô đó mờ xuống 35% trong 100 ms.
4. Dây máu boss có nền mực đặc, nằm ngoài sàn chơi và không phát sáng.
5. Không có chữ nào giữa màn hình trong lúc đánh. Tên boss chỉ hiện một lần lúc intro, trên dây bện. Thoại boss ("thần nói qua mặt nạ", §5) chỉ ở lúc chuyển phase (pause hoặc không có hazard), subtitle trong dải dưới.
6. HUD không bao giờ rung theo screen shake.

### Chớp sáng — luật an toàn (quyết định 2026-10-25)

Nhịp ~0,20 s/đòn (~5 flash/s) và boss ≥192 px (~53% chiều cao màn hình) nghĩa là flash trắng cả thân boss sẽ vượt ngưỡng ~25% diện tích và tần suất 3 flash/s (Harding/ITU).

- **Hit flash trên boss chỉ ở mặt nạ hoặc viền**, hoặc giới hạn ≤3 lần/giây trên diện tích lớn. Luật này **áp lên hit flash ở §2 (Combat thường) và §5 (Contact frame)**.
- **Tùy chọn Reduce Flashing:** thay flash bằng tint viền.
- Có trần: bloom khi thắp đèn lồng arena (chỉ thắp lúc chuyển phase, khi không có hazard), bloom khi thắng boss, và độ chênh sáng của cú fade sang màu kem khi thua (lặp lại mỗi lần retry).

### Tùy chọn hiển thị và tiếp cận

HUD scale 1× / 1,5× / 2× — hệ số thực = **round(world scale × tùy chọn)**, luôn là số nguyên (sửa theo §8: 1,5 × 3 = 4,5 không phải số nguyên); HUD opacity 25–100%; cỡ chữ 100/125/150%; Reduce Motion; Reduce Flashing; screen shake 0–100%; số damage Off / Crit / All; remap đầy đủ. Không có preset màu hazard (quyết định §4); toàn bộ UI phải qua grayscale test.

### Xung đột art ↔ readability và cách đã quyết

| # | Xung đột | Quyết định |
|---|---|---|
| 1 | Dấu triện vừa là focus vừa là đã chọn | Tách: focus = khung mực + con trỏ; triện = đã trang bị/xác nhận |
| 2 | Dây máu boss đỏ son–vàng bão hòa ở foreground, mâu thuẫn luật tritan §4 | Ruột mực/hổ phách, chỉ viền đỏ son |
| 3 | Pixel font không đủ chỗ cho dấu tiếng Việt | Lai: smooth cho body, pixel cho số và nhãn không dấu |
| 4 | Đèn lồng gánh 3 nghĩa; cây vũ khí cần 5 trạng thái | Cây vũ khí dùng hình node + badge; đèn chỉ trang trí |
| 5 | Flash trắng thân boss vượt ngưỡng chớp sáng | Flash ở mặt nạ/viền, ≤3/s; tùy chọn Reduce Flashing |

### Chuyển sang §8 / ui-programmer

- **Độ phân giải không chia hết** (1366×768, 1600×900, 16:10, Steam Deck, ultrawide): đã quyết ở §8.2. HUD neo vào **khung viewport của world**, không neo vào cạnh màn hình.
- Tritan: dây máu boss (viền đỏ son) và hazard magenta vẫn là cặp cần playtest.

## 8. Asset Standards

*Duyệt 2026-10-25. Phần art do art-director soạn, phần giới hạn engine do technical-artist soạn; ba chỗ bất đồng do người dùng quyết (bảng cuối section).*

> **[NS] = NOT SOURCEABLE.** `docs/engine-reference/unity/` không có tài liệu về 2D Renderer, Pixel Perfect, Sprite Atlas, sprite normal, DoF/bloom trên sprite. Các mục đánh dấu [NS] lấy từ trí nhớ, **chưa kiểm** trên Unity 6.3 — prototype phải xác nhận trước khi sản xuất.

### 8.1 Độ phân giải tham chiếu và chiều cao (chốt)

- **Tham chiếu 640×360**, scale nguyên 2× / 3× / 4× / 6× → 720p / 1080p / 1440p / 4K.
- **Người chơi đúng 48 px** (thân, không tính vũ khí) = 3 tile. **Lưới tile 16 px. PPU 16 cho mọi asset** (1 unit = 1 tile).
- Một con số cố định vì paper-doll cần khung cố định: socket, swap mask và mọi frame đều neo vào đó; đổi giữa chừng là vẽ lại tất cả. 64 px tăng diện tích vẽ ~1,8× trên ~130 frame thân; 32 px mềm dưới DoF (§1).
- NPC, quái, boss giữ như §5.

### 8.2 Renderer và pipeline hiển thị

**Hiện trạng repo:** project đang chạy **URP 2D Renderer** (`Assets/Settings/Renderer2D.asset`, gói `com.unity.feature.2d`). Hướng dưới đây **thay thế** nó → cần **ADR-1** trước khi làm.

- **Universal (3D) Renderer + Forward+.** Bỏ 2D Renderer [NS]: 2D Light là light texture screen-space, không có falloff theo Z, không perspective; DoF cần depth texture. HD-2D trên 2D Renderer sẽ thành giả lập.
- **Camera perspective FOV thấp** (~20–30°, đặt xa). Parallax và DoF đến từ Z thật, nên 5–7 lớp không cần script parallax. Khoảng cách camera đặt sao cho ở mặt phẳng play lane, 1 texel = 1 px tham chiếu.
- **World (gồm post: DoF, bloom, LUT) render vào RT 640×360, rồi upscale point lên native.** UI là Overlay canvas native, nằm ngoài RT.
  - Mọi thứ — kể cả vũ khí và mảnh boss xoay, ánh sáng point light — bị lượng tử về cùng lưới pixel, nên không có mixel. (Giải quyết xung đột camera phối cảnh ↔ scale nguyên, và xoay sprite pixel ↔ răng cưa.)
  - DoF và bloom chạy trên ~230k px: rẻ. Overdraw của fog card gần như miễn phí.
  - Đánh đổi: bloom ở độ phân giải thấp nhấp nháy khi sprite di chuyển → threshold cao. DoF ở 360p trông thô; chấp nhận như thẩm mỹ.
  - Cách làm: prototype trước bằng URP `Render Scale` + `Upscaling Filter = Point` [NS: kiểm post chạy ở render scale và Overlay UI ở native]. Dự phòng: RT tường minh 642×362 kèm offset sub-pixel để camera cuộn mượt.
- **Luật bắt buộc:** sprite trên lớp chơi là **alpha-clip, opaque queue, ghi depth** — sprite transparent không ghi depth, DoF sẽ làm mờ chúng theo nền phía sau. Pixel art có alpha 1-bit nên luật này không tốn gì (thêm early-z, tương thích SRP Batcher). Chỉ fog card và VFX là transparent.

**Màn hình không chia hết (quyết định 2026-10-25: mở rộng tầm nhìn khi khám phá).** Integer scale = floor(min(W/640, H/360)).

| Màn hình | Scale | Khám phá | Arena |
|---|---|---|---|
| Steam Deck 1280×800 | 2× | mở rộng **640×400** | khóa 640×360, letterbox 40 px trên/dưới |
| Ultrawide 2560×1080 | 3× | mở rộng **853×360** | khóa 640×360, pillarbox |
| 1366×768 | 2× | 640×360, viền 43/24 px; tùy chọn "Sharp fill" (point lên ×3 rồi bilinear xuống) | như khám phá |

- Arena luôn khóa 640×360 để pattern boss công bằng trên mọi màn hình.
- Viền màn hình dùng texture giấy. HUD neo vào **khung viewport của world**.
- Đã cân nhắc luôn khóa 640×360 + letterbox (đề xuất của technical-artist) và bỏ. Hệ quả: camera bounds theo vùng và chuyển cảnh khi vào arena → **cần ADR camera bounds**.

### 8.3 Định dạng file

- **Source:** `.aseprite` cho mọi asset pixel, kể cả portrait và parallax xa. Tag cho animation, slice cho pivot và socket, normal map và swap mask ở layer riêng. Không dùng PSD — giữ một công cụ là tiết kiệm lớn nhất cho solo dev.
- **Export:** PNG-32 RGBA, sRGB, straight alpha. Normal map để linear (tắt sRGB). Nếu dùng Unity 2D Aseprite Importer (`com.unity.2d.aseprite`, [NS] cần xác nhận cho 6.3) thì bỏ bước export, miễn tag và slice được giữ.
- Tên file chỉ dùng ASCII, không dấu tiếng Việt.

### 8.4 Quy ước đặt tên

`[cat]_[region]_[subject]_[state]_[variant]_[nn]`, viết thường, snake_case. Không có trường size (kích thước do tầng quyết định), trừ icon.

- **Category:** `chr npc mon boss wpn tile prp bg fog vfx ui ico por lut`
- **Region:** `frm vil mvl mfr arn cmn`
- **Hậu tố map:** `_n` (normal), `_k` (swap mask), `_e` (emissive)

Ví dụ: `chr_player_atk_slab_01`, `wpn_slab_greatsword_contact`, `vfx_cmn_smear_crescent_02`, `tile_mfr_ground_a_n`, `bg_mfr_far_03`, `ico_wpn_needle_16`, `por_vil_clanhead_<ten>_happy`, `boss_arn_<ten>_mask_p2`.

### 8.5 Tầng kích thước và atlas

Atlas tổ chức **theo nhân vật cho thứ chuyển động, theo vùng cho môi trường**, để mỗi vùng chỉ nạp bộ của nó (Addressables).

| Loại | Canvas / kích thước | Atlas |
|---|---|---|
| Người chơi (thân) | khung 96×96 (đủ chỗ vũ khí 1,4× và smear) | `chr_player`, ≤2048² |
| Vũ khí (14 slot × 4–6) | ≤128×128 mỗi sprite | một atlas `wpn`, ~1024² |
| NPC | 64×64 | theo vùng |
| Quái nhỏ / vừa / lớn | 48 / 128 / 192 | theo vùng |
| Boss (từng mảnh) | mỗi mảnh ≤256; tổng ≥192 px cao; ≤30 mảnh | ≤2 atlas 2048² mỗi boss |
| Tile | 16×16, sheet 512² (+`_n` cùng cỡ cho tầng có normal) | theo vùng |
| Prop | kích thước gốc | theo vùng, ≤2048² |
| Parallax xa | mật độ 0,5× (vẽ ở PPU 8) | theo vùng |
| Fog card | 128–256, grayscale, tile được | `cmn` |
| VFX | flipbook 32–128 px, grayscale + gradient map | `cmn` |
| UI / icon | panel 9-slice ở ref px; icon 16 / 24 | `ui` |
| Portrait | canvas 128×128, 3 biểu cảm | theo dòng họ |

- **Atlas tối đa 2048²; cấm 4096.** Mỗi vùng 1 tileset atlas + 1 prop atlas.
- Padding và extrude ≥2 px (outline cần 1 px trong suốt; tránh rò màu ở vị trí lẻ).
- Mesh type: Tight cho sprite >128 px và fog card (giảm overdraw); Full Rect cho tile và 9-slice.

### 8.6 Palette

- **Một master palette toàn cục ~48–64 màu**, dựng từ 6 màu chính ở §4. Mỗi vùng có sub-palette 24–32 màu lấy từ master.
- Trần số màu mỗi sprite, không tính viền: nhân vật ≤16, quái ≤12 (+ màu mắt nguyên tố), prop ≤12.
- **God Magenta #FF2BD6 không có trong palette của asset nào** ngoài VFX hazard của boss → một script lint để bắt lỗi.
- Vẽ dưới ánh sáng trung tính; không nướng mùa hay giờ vào sprite (LUT lo việc đó). Bóng vẽ sẵn (prop không normal) luôn lấy nguồn sáng trên-trái.
- **Viền nhân vật vẽ sẵn** bằng màu mực #2A1E1A dùng làm **màu khóa**; shader chỉ đổi màu ở màu khóa đó → làm được "hit flash chỉ ở viền" (§7) và Reduce Flashing.
- **Palette swap tóc/da/áo (§5):** dải màu khóa riêng hoặc texture mask `_k` — **phải chốt trước frame production đầu tiên** (thêm sau = vẽ lại toàn bộ frame) → thuộc ADR-2.

### 8.7 Animation

- Game chạy 60 Hz; thời lượng frame tính bằng **tick** (16,7 ms): idle/đi bộ 6 tick (~10 fps), chạy 5 tick (12 fps). Đòn đánh dùng thời lượng riêng từng frame: anticipation 4–6 tick, contact 2–3 tick + hit-stop, recovery 5 tick.
- Vị trí di chuyển mượt 60 fps, frame sprite nhảy bậc — đó là chất HD-2D.
- **Smear theo họ vũ khí, dạng VFX** (`vfx_cmn_smear_[họ]`): 5 bộ thay vì 14 (quyết định 2026-10-25). Các slot trong cùng họ dùng chung smear.
- **Vũ khí socket (xác nhận):** ScriptableObject frame data, mỗi frame thân gồm socket (px nguyên), góc, sorting front/back, override smear → **ADR-4**. Xoay trong RT 640×360 tự lượng tử. Dự phòng cho họ mảnh (Needle, Chain) nếu vẫn răng cưa: 8 góc vẽ sẵn bằng RotSprite (14 × 8 sprite).
- **Boss cutout (xác nhận):** Transform hierarchy, animate qua Animator. **Bác skinning của 2D Animation** (mesh deform resample texel; SpriteSkin với material lit của Universal Renderer chưa rõ [NS]). Chỉ dùng skinning cho đuôi/vải nếu prototype đạt. Mảnh boss chủ yếu tịnh tiến.

### 8.8 LOD và import

Không có LOD mesh. LOD là **mật độ texel theo dải độ sâu**:

| Dải | Mật độ |
|---|---|
| Lớp chơi | 1:1 |
| Lớp giữa | 1:1, bỏ cụm <2 px |
| Lớp xa | 0,5× |
| Bầu trời, fog | gradient hoặc 0,25× |

Vật có animation ở lớp xa dùng ≤4 frame.

**Import:**

| Loại | Filter | Mipmap | Nén |
|---|---|---|---|
| Lớp chơi (nhân vật, tile, prop), UI, icon | Point, Clamp | tắt | **RGBA32, không nén** |
| Lớp xa, fog card | Point | **bật** (chống shimmer khi thu nhỏ trong camera perspective) | BC7 |
| Normal map (env) | Point | theo lớp | BC5 |

- **Cấm Crunch.**
- **Lớp chơi không bao giờ nén** (quyết định 2026-10-25). Đã cân nhắc cho tile môi trường lớp chơi dùng BC7 nếu qua A/B (đề xuất của technical-artist) và bỏ.

### 8.9 Normal map và shader

- **Normal tự sinh (Laigter, SpriteIlluminator): chỉ cho bề mặt env lớn** ở lớp chơi gần nguồn sáng (§6). ≤12 renderer có normal trên màn hình, ≤15% diện tích.
- **Nhân vật không dùng normal map** (quyết định 2026-10-25; sửa §4 và §6). Rim là alpha-edge theo hướng đèn trong screen-space.
- Shader Graph target "Sprite Lit" chỉ dành cho 2D Renderer [NS] → dùng Lit/Unlit + Alpha Clip + Custom Function. Duyệt light Forward+ trong Custom Function là **rủi ro chưa kiểm** → **ADR-2** (shader sprite: lit, rim, recolor outline, palette swap).

### 8.10 Giới hạn cứng

| Hạng mục | Giới hạn |
|---|---|
| Light | Forward+. ≤16 point light thật đang thấy, ≤6 chạm play lane, không shadow cho additional light [NS: trần light Forward+]. **Đèn lồng trang trí là sprite emissive + bloom, không phải light thật**; chỉ đèn chủ chốt là light thật |
| Volume | 1 global + tối đa 2 volume blend cùng lúc; đổi profile bằng blend weight |
| LUT | **Chỉ dành cho mùa** (vùng sương contribution 0,25–0,3). 13 trạng thái ánh sáng (§2) dùng White Balance, Color Adjustments, Shadows/Midtones/Highlights, Bloom, DoF — không dùng LUT. (Color Lookup chỉ có một slot; param texture trong volume không nội suy khi blend [NS].) |
| Parallax / fog | ≤4 lớp transparent phủ toàn màn hình trong RT 640×360 |
| Draw call (≤150) | Env 60 / nhân vật + boss 25 / đạn + VFX 15 / post 15 / UI 20 / dự phòng 15 |
| Đạn boss | ≤1500 đạn, ≤2 draw call (instanced, 1 atlas). Logic và va chạm ở CPU, **không** dùng particle collision |
| Particle VFX | ≤2000 hạt, **chỉ trang trí**; VFX Graph không đọc ngược về gameplay |
| Boss | ≤30 mảnh |

### 8.11 Bộ nhớ (ĐỀ XUẤT — technical-director chốt)

Memory ceiling chưa được cấu hình trong `technical-preferences.md`. Đề xuất của technical-artist:

- **Trần:** RAM ≤3 GB, VRAM ≤1,5 GB (Steam Deck dùng bộ nhớ chung, VRAM mặc định ~1 GB [NS]).
- **Texture mỗi cảnh vùng ≤256 MB:** env 128 MB (6 atlas RGBA32 2048² + 2 normal), boss 32 MB, người chơi + 14 vũ khí 32 MB, UI + font atlas tiếng Việt native 32 MB, LUT + RT 32 MB.
- **Đỉnh khi chuyển vùng ≤384 MB**, qua Addressables → **ADR-5**.

### 8.12 Asset pack

**Asset pack chỉ dùng cho tile, prop, parallax xa và nền VFX** (quyết định 2026-10-25). Không dùng cho người chơi, vũ khí, boss hay portrait — các asset mang bản sắc của Pillar 1, 2 và 4.

Quy trình, theo thứ tự:

1. **Lọc:** chỉ nhận pack lưới 16 px, cùng phối cảnh side-view. Kiểm giấy phép: cho phép thương mại và sửa.
2. **Khớp tỉ lệ:** chỉ scale nguyên. Pack 32 px cho nhân vật thì loại, không phóng 1,5×.
3. **Remap palette:** chuyển Indexed theo sub-palette của vùng, sửa tay chỗ bị banding.
4. **Viền:** xóa viền khỏi prop; thêm viền mực 1 px cho quái lấy từ pack.
5. **Giá trị:** ép về dải 30–70%; không cụm nào lớn hơn 4×4 px nằm ngoài 20–80% (§6).
6. Vẽ lại highlight theo nguồn sáng trên-trái, rồi đổi tên theo §8.4.

Ngân sách: nếu chuẩn hóa một tile mất hơn ~50% thời gian vẽ mới, thì vẽ mới.

### 8.13 Xung đột art ↔ kỹ thuật và cách đã quyết

| # | Art muốn | Kỹ thuật giới hạn | Quyết định |
|---|---|---|---|
| 1 | Camera phối cảnh + lớp chơi scale nguyên | Perspective cho tỉ lệ lẻ ở độ sâu khác | Render world vào RT 640×360, upscale point (§8.2) |
| 2 | Mở rộng tầm nhìn trên Deck/ultrawide | Khóa 640×360 + letterbox đơn giản hơn | **Mở rộng khi khám phá, khóa trong arena**; cần ADR camera bounds |
| 3 | Rim light trên nhân vật bằng normal tự sinh (§4, §6 cũ) | Normal sinh độc lập theo frame nhấp nháy, pillow shading ở 48 px | **Không normal cho nhân vật**; rim theo alpha-edge |
| 4 | Lớp chơi không nén | BC7 cho tile env nếu qua A/B | **Không nén lớp chơi** |
| 5 | Xoay vũ khí/mảnh boss tự do | Mixel, răng cưa ở góc lẻ | RT lượng tử; dự phòng 8 góc vẽ sẵn |
| 6 | Nhiều đèn lồng là nguồn sáng thật (§2) | Trần ≤16 light thật | Đèn trang trí là emissive + bloom |
| 7 | LUT cho cả mùa lẫn trạng thái ánh sáng | Một slot LUT | LUT chỉ cho mùa |

### 8.14 ADR cần có và prototype

| ADR | Nội dung |
|---|---|
| ADR-1 | Renderer: Universal + Forward+, RT 640×360, alpha-clip/DoF — thay thế Renderer2D hiện tại |
| ADR-2 | Shader sprite: lit, rim, recolor outline, palette swap (swap mask chốt trước frame đầu tiên) |
| ADR-3 | Đạn kiểu Calamity: instanced, logic Jobs/Burst, 60 fps trên Deck (cùng gameplay-programmer) |
| ADR-4 | Frame data paper-doll và chất lượng xoay |
| ADR-5 | Ngân sách bộ nhớ, Steam Deck |
| (nhỏ) | Phân lớp LUT/Volume; camera bounds khi mở rộng tầm nhìn |

**Rủi ro khác (gameplay-programmer quyết):** hit-stop bằng `timeScale` toàn cục ở nhịp ~5 đòn/s sẽ đóng băng toàn bộ đạn và volume blend liên tục → đề xuất hit-stop cục bộ theo entity. Camera shake và Cinemachine chạy unscaled.

**Prototype trước khi sản xuất** (mở rộng phạm vi Farm + một arena của §1), để quyết ADR-1:

- Một player kèm một họ vũ khí có socket.
- Một boss cutout 10 mảnh.
- 1000 đạn.
- **Một lát Mist Forest 7 lớp** — trường hợp overdraw xấu nhất.
- Đo GPU ms, draw call và memory trên máy dev, và trên Steam Deck nếu có.

**Phải chốt trước frame production đầu tiên:** 48 px, PPU 16, swap mask, master palette. Đổi bất kỳ thứ nào sau đó là vẽ lại tất cả.

## 9. Reference Direction

*Duyệt 2026-10-25.*

**Luật chung:** mỗi nguồn chỉ được lấy đúng phần ghi ở mục "Lấy". Cần thứ khác từ nguồn đó thì sửa section này trước — không lấy ngầm. Golden hour và rừng sương (nguồn phi-game trong concept) không có mục riêng: §2 đã viết chúng thành nhiệt độ màu và DoF.

### 1. Octopath Traveler (HD-2D) — ánh sáng và chiều sâu

- **Lấy:**
  - Sprite pixel được chiếu sáng trong không gian có Z thật.
  - Parallax và DoF tilt-shift sinh từ khoảng cách thật, không dùng script (§8.2).
  - Point light ấm trên nền lạnh (§2 Mist Village: 8000K đối 2000K).
  - Bloom chỉ trên nguồn sáng.
  - Sprite nhảy frame theo bậc trong khi vị trí di chuyển mượt (§8.7).
- **Không giữ được qua pipeline của ta:**
  - Bokeh mịn và bloom độ phân giải cao — DoF/bloom của ta chạy ở 360p, ngưỡng bloom cao; thô là chủ ý.
  - Texture môi trường mật độ cao trộn với sprite thấp (mixel) — luật của ta là 1 texel = 1 px (§6).
  - Sprite trong suốt bị DoF làm mờ — lớp chơi của ta là alpha-clip (§8.2).
  - Color grading riêng từng cảnh — LUT chỉ dành cho mùa (§8.10).
  - Camera 3/4 nhìn từ trên — ta dùng side-view, chiều sâu chạy vào trong màn hình.
- **Tránh:**
  - Grading nâu-sepia và vignette nặng — phá §1 ("hơi ấm là nhà") và bảng lễ hội §4.
  - Làm mờ nhân vật đang chiến đấu — §5 cấm DoF trên lớp chơi; arena tắt DoF.

### 2. Stardew Valley — nhà và làng biết thay đổi

- **Lấy:**
  - Quan hệ đọc được bằng số lượng vật trong thế giới: lễ vật, vải mới, đèn ở cửa (§6, §4 Pillar 5).
  - Cây chín là thứ bão hòa nhất trên ruộng (§2 Farm).
  - Lễ hội biến đổi cả quảng trường bằng prop.
- **Tránh:**
  - Tileset vẽ lại theo mùa — mùa của ta làm bằng LUT (§4, §8.10).
  - Bão hòa đều khắp màn hình — phá §1 Nguyên tắc 1 và dải giá trị 30–70% (§6).
  - Ánh sáng phẳng không có nguồn sáng.
  - UI dạng trái tim — affinity của ta là đèn lồng + số tua (§4).

### 3. Terraria (gộp Calamity mod) — vũ khí và đạn

*Gộp vì Calamity là mod của Terraria; để riêng thì hai mục cùng một hướng (quyết định 2026-10-25).*

- **Lấy:**
  - Hàng chục vũ khí phân biệt được chỉ bằng silhouette ở cỡ icon (§3 hình chủ đạo ≥60%, §7 icon 16/24).
  - Boss nhiều phase, mỗi phase leo thang độ dày pattern (Calamity), trong trần ≤1500 đạn (§8.10).
  - Đạn đọc bằng giá trị đảo: lõi tối viền sáng cho đạn thường, lõi trắng viền tối cho hazard boss (§4).
- **Tránh:**
  - Đạn cầu vồng nhiều hue và glow phủ màn hình — phá luật God Magenta độc quyền (§4) và luật chớp sáng (§7).
  - Sprite trộn mật độ pixel giữa các họa sĩ — phá luật scale nguyên (§8.2, §8.12).
  - Lighting kiểu lưới theo tile.
  - Số damage bật mặc định — §7 để mặc định tắt.

### 4. Hollow Knight — đọc quái và cảm giác đòn

- **Lấy:**
  - Thân tối với mắt sáng là thông tin chính (§5 quái: thân 15–35%, mắt nguyên tố).
  - Occluder tiền cảnh tối, giảm bão hòa, chỉ ở mép khung (§6).
  - Contact frame + hit-stop + anticipation rõ (§5 ba nhịp đòn).
- **Tránh:**
  - Nét vẽ tay độ phân giải cao, gradient mịn — phá point-filter và luật cụm ≥2×2 px (§5, §8).
  - Bảng màu xanh lạnh gần đơn sắc và giọng u buồn — phá §2 (đêm là "hưng phấn thợ săn, không phải sợ").
  - Flash trắng cả thân — §7 chỉ cho flash ở mặt nạ hoặc viền.
  - Xác và cái chết của boss — §2: thần cúi chào rồi tan vào sương.

### 5. Tranh dân gian Đông Hồ — vũ khí như cổ vật, lễ hội như bảng màu

*Chọn Đông Hồ thay vì ukiyo-e (Hiroshige) để không kéo cả bộ sang chất Nhật Bản (quyết định 2026-10-25).*

- **Lấy:**
  - Bảng màu khoáng phẳng, ít tông, có đỏ son — gần với bảng lễ hội ở §4.
  - Ánh nền giấy điệp (lấp lánh nhẹ) — **chỉ cho UI giấy** (§7), không cho sprite thế giới.
  - Mảng màu phẳng + nét mực + dấu triện cho ngữ pháp UI "giấy và ấn triện" (§3, §7).
  - Bố cục "cổ vật": vật ở giữa khung, đối xứng, cô lập trên nền rút màu — dùng cho khoảnh khắc trao vũ khí (§2).
- **Kỹ thuật tự phát triển (không lấy từ Đông Hồ):** dải sương ngang tách các lớp xa trong Mist Forest (§6) và bầu trời chuyển màu cho lớp gradient (§8.8) là cách xếp fog card riêng của Evermeadow. Đông Hồ không có truyền thống vẽ dải sương; không mượn kasumi/bokashi của ukiyo-e.
- **Tránh:**
  - Vân giấy và vân gỗ trên sprite thế giới — phá luật legibility (§6). Vân giấy chỉ dành cho UI, dao động ≤5% (§7).
  - Viền đậm trên prop — §5 quy định prop không có viền.
  - Sao chép nguyên tranh, đề tài hay motif tôn giáo/kiến trúc cụ thể — motif dòng họ giao ước phải là của riêng Evermeadow (§6).
