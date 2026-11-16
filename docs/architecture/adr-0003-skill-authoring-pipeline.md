# ADR-0003: Skill Authoring Tool (Skill Tool: GraphView ↔ XML)

## Status
Proposed

## Date
2026-11-06 (viết lại cùng ngày: bỏ Graph Toolkit, XML là nguồn duy nhất)

## Last Verified
2026-11-06

## Decision Makers
Đặng Đình Dũng (user), Claude (lead). Bản đầu (Graph Toolkit → XML sinh ra) được unity-specialist và technical-director
TD-ADR rà soát (xem ADR-0002). Bản này thay hướng theo quyết định người dùng 2026-11-06: **Skill Tool là công cụ đọc và
tạo file skill XML, không phải một phần dữ liệu của project; id = tên file, số 6 chữ số từ 100000; đặt trong menu
Evermeadow cạnh công cụ animation; dựng bằng GraphView, gỡ Graph Toolkit.** Chưa rà soát độc lập bản này.

## Summary
Người làm game soạn mỗi kỹ năng bằng **Skill Tool** — một cửa sổ Editor (`Evermeadow ▸ Skills ▸ Skill Tool`) vẽ kỹ năng
thành luồng node Start → … → End, mở thẳng file XML và lưu thẳng lại file XML trong `Assets/StreamingAssets/Skills/`.
**XML là nguồn duy nhất**; đồ thị chỉ là cách xem và sửa, không có file đồ thị nào trong project. Id của kỹ năng là tên
file (6 chữ số từ 100000), là thứ dev gán vào đòn đánh thường (dòng bảng tổ hợp) hoặc bùa chú (Skill Stone). Công cụ
dựng bằng GraphView có sẵn trong Editor, nằm trong một assembly chỉ-Editor, không vào bản build.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.3.27f1 (6.3 LTS) |
| **Domain** | Editor tooling (UI Toolkit, GraphView); không có mô-đun engine-reference cho miền này |
| **Layer** | Presentation (công cụ Editor) trên lõi Core (`Evermeadow.Core.Skills`, ADR-0002) |
| **Knowledge Risk** | MEDIUM — `UnityEditor.Experimental.GraphView` có từ 2018 và vẫn có trong 6.3 (`UnityEditor.GraphViewModule.dll`, không đánh dấu `Obsolete`; đã kiểm XML doc của Editor 6000.3.27f1); các field UI Toolkit (`DoubleField`, `EnumField`…) nằm ở `UnityEngine.UIElements`, toolbar ở `UnityEditor.UIElements` (đã kiểm). Tài liệu engine-reference không có mục Editor tooling: **NOT SOURCEABLE** trên tài liệu đã ghim |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`; XML doc của Editor đã cài (`UnityEditor.GraphViewModule.xml`, `UnityEngine.UIElementsModule.xml`, `UnityEditor.CoreModule.xml`); mã nguồn `com.unity.graphtoolkit@0.4.0-exp.2` (lý do bỏ, xem Alternative 1) |
| **Post-Cutoff APIs Used** | Không có |
| **Verification Required** | (T1) cửa sổ mở được, thêm/nối/xóa node; (T2) Open XML → đồ thị → Save không đổi nội dung (đã có test lõi `SkillFlowTests`); (T3) New cấp id kế tiếp, Save tạo `<id>.xml`; (T4) bảng tổ hợp mở/sửa/kéo đổi thứ tự/lưu; (T5) player build không chứa assembly của tool; (T6) build lỗi khi một XML bị sửa tay |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0002 (hợp đồng XML, `SkillXmlReader`/`SkillXmlWriter`, thư mục nạp) |
| **Enables** | Soạn nội dung kỹ năng và bảng tổ hợp; story nội dung vũ khí đầu tiên |
| **Blocks** | — |
| **Ordering Note** | Lõi runtime (ADR-0002) không phụ thuộc ADR này. Accept sau khi kiểm T1–T6 trong Editor |

## Context

### Problem Statement
GDD yêu cầu mỗi kỹ năng là một file XML, soạn bằng kéo thả node. Bản đầu của ADR này coi đồ thị Graph Toolkit là nguồn và
sinh XML từ đó. Người dùng làm rõ (2026-11-06): công cụ đồ thị chỉ để **đọc và tạo** file XML, không phải dữ liệu của
project; dev chỉ dùng id (= tên file) để gán kỹ năng. Vì vậy công cụ phải **mở được một file XML có sẵn thành đồ thị** —
điều Graph Toolkit 0.4 không làm được qua API công khai.

### Constraints
- XML là nguồn duy nhất, trong git; không có file đồ thị trong project.
- Id = tên file, 6 chữ số, 100000–999999; không có thuộc tính `id` trong file.
- Không thêm package thử nghiệm; công cụ không vào bản build.
- Đặt trong menu Evermeadow, cạnh công cụ animation (`Evermeadow ▸ Animation ▸ …`).
- Một lập trình viên; đường lui rẻ nếu GraphView bị gỡ ở bản Unity sau.

### Requirements
- Skill & Moveset Rule 2, 3, 5, 6, 11, 12, 21, 31; AC-D03, D07, D09.
- Mở/lưu không làm đổi nội dung (khứ hồi tất định).
- Không bao giờ lưu file mà game sẽ từ chối.

## Decision

### 1. XML là nguồn, Skill Tool là bộ ghi duy nhất
Cửa sổ `SkillToolWindow` (sửa 2026-11-08: chỉ còn soạn kỹ năng; tab Command Table bị bỏ — hệ thống tạo combo riêng soạn
bảng tổ hợp):
- `New` (cấp id kế tiếp còn trống = id lớn nhất + 1, tối thiểu 100000; tạo sẵn PlayAnimation → Hit → End theo nhịp F-G),
  `Open ▾` (liệt kê `<id>  (owner, search)` trong thư mục), `Save` / Ctrl+S, `Save As New Id`.
- Node: PlayAnimation, Hit, Cue, Spawn, LookAt, Search, End. Node Start chỉ có Owner. CancelWindow, HoldGate, Move, Armor
  và mọi logic combo thuộc hệ combo (GDD Rule 4).
- `Validate Folder` nạp cả thư mục như game (`SkillLoader` + `SkillLibraryValidator`, kiểm `payload_hash`).

### 1b. Owner và dây dữ liệu (2026-11-07)
Node Start có ô **Owner** (Player / Creature) và bốn cổng ra dữ liệu — Owner Position, Owner ID, Target Position,
Target ID (hai cổng Target ẩn và bị gỡ dây khi Owner = Player). Node có cổng vào theo bảng `SkillValues.InputsOf`
(Hit: Origin, Only; Spawn: Origin, Toward; Cue: Origin; Move: Toward; Look At: Target; Search: Center). Chỉ nối được cổng cùng kiểu
(vị trí: xanh, id: cam). Bảng node bên trái kéo thả vào canvas (hoặc bấm để thêm vào giữa).

### 1h. Công tắc debug — Evermeadow ▸ Project Settings (2026-11-14)
Trang `Project/Evermeadow` trong Project Settings (menu `Evermeadow ▸ Project Settings`), lưu `ProjectSettings/EvermeadowDevOptions.json`
(dùng chung qua git), đẩy vào `DevOptions` (Foundation.Common, cờ tĩnh chỉ dành cho công cụ dev; gameplay không đọc; bản build giữ mặc
định). Công tắc: Show Hit Area (vùng Hit xanh, đúng pivot / hình / thời lượng, bám nhân vật khi Steady + IsFollowPivot), Show Search
Area (vàng), Log Skill Trace, Log AI Decisions, Show Moveset Tester. Bật / tắt khi đang Play có tác dụng ngay.

### 1g. Moveset Tool (2026-11-13)
Cửa sổ thứ hai cùng menu (`Evermeadow ▸ Skills ▸ Moveset Tool`, cùng assembly `SkillTool.Editor`) soạn moveset XML: tên,
vũ khí (sửa 2026-11-13: **chọn** bằng ô tick Any + từng họ trong `StreamingAssets/Weapons/weapon_families.txt` — `WeaponFamilies.Parse`, mặc định 5 họ của art bible, nút "Sửa danh sách…" tạo / mở file; họ lạ hiện màu cam và bị chặn lưu `UnknownWeapon`; Socket test chọn họ vũ khí bằng popup), ghi chú, bảng dòng (nút, cử chỉ, nút thứ hai của tổ hợp, context, moving, dir, skill — chọn từ thư mục Skill Tool,
nút ↗ mở thẳng `SkillToolWindow.OpenSkill`, after, link, priority, cooldown; ↑ ↓ nhân đôi, xóa), kiểm lỗi trực tiếp (Save bị
chặn khi còn lỗi hoặc skill id không tồn tại), Undo / Redo bằng ảnh chụp XML, giữ bản đang sửa qua domain reload, khung
**Socket test** (họ vũ khí + số ô + chọn moveset cho từng ô → kết quả khảm, xung đột, bảng input → đòn). Save khi Play →
`MovesetHotReload.Push`; `SkillFolderPostprocessor` đẩy cả file moveset đổi trên đĩa. Tham chiếu thêm `Evermeadow.Core.Moveset`,
`Evermeadow.Core.Moveset.Unity`.

### 1f. Sửa trong Play Mode — nạp lại nóng (2026-11-11)
Skill Tool dùng được khi đang Play (đồ thị đang sửa vẫn giữ qua domain reload nhờ `_wipXml`). **Save khi đang Play** →
`SkillHotReload.Push` đọc file vừa ghi và đẩy vào mọi `SkillLibraryRunner` đang chạy (`SkillRepository.ReloadText`); thanh
trạng thái báo "Play Mode: live…" (hoặc "…has errors, the game keeps its previous version"). `SkillFolderPostprocessor`
làm việc đó cho mọi XML trong thư mục Skills được import / chuyển / xóa khi đang Play (sửa tay, git) — cần Auto Refresh
của Editor bật trong Play Mode cho đường này; đường Save của tool không phụ thuộc. **VFX:** `VfxCatalogPostprocessor` sau
khi dựng lại catalog gọi `VfxSpawner.Invalidate(key)` trên mọi `VfxRunner` đang chạy với các prefab vừa đổi: pool cũ bị bỏ,
lần spawn kế dùng prefab mới, effect đang chạy chạy nốt rồi bị hủy thay vì trả về pool (đếm thế hệ theo key). Effect
Browser đang mở cũng tự quét lại và nạp lại preview (giữ mốc thời gian). Thêm tham chiếu `SkillTool.Editor →
Evermeadow.Core.Skills.Unity`.

### 1e. Chọn hiệu ứng từ hệ VFX (2026-11-11)
Trường **Effect.PrefabName** và **Hit.HitEffectAddressable** không còn là ô gõ chữ: là nút hiện key, bấm mở **VFX Effect
Browser** (`VfxPickerWindow`, cũng mở được từ `Evermeadow ▸ VFX ▸ Effect Browser`) — lưới mọi prefab trong
`Assets/Game/Art/VFX/<Loại>/` (lọc theo loại, tìm theo chữ, thumbnail, ô "không dùng") và khung **preview** (Play / Pause /
Restart, kéo thời gian, Loop, Scale, nền; dựng trong `PreviewRenderUtility`, thời gian đặt bằng `SkillVfx.SetTime` nên khớp
lúc chạy). Node Hit mở sẵn loại Hit. Key không có prefab → chữ cam + cảnh báo trong tooltip (Save vẫn cho lưu: key là chuỗi,
validator kỹ năng không phụ thuộc thư mục art). XML vẫn chỉ lưu **key = tên file prefab**. Ranh giới assembly:
`SkillTool.Editor` thêm tham chiếu `Evermeadow.Presentation.Vfx` + `Evermeadow.Presentation.Vfx.Editor` (cùng lớp
Presentation; mục 5 cập nhật). Hệ VFX: lõi thuần C# `Evermeadow.Presentation.Vfx` (đặt tên, loại theo thư mục, toán thời
gian, chỉ mục + lọc — 40 test), `…Vfx.Unity` (`SkillVfx` gốc prefab, `VfxFlipbook`, `VfxCatalog` ScriptableObject,
`VfxSpawner` pool theo key + `Tick(dt)` để đồng hồ Game đẩy), `…Vfx.Editor` (catalog tự dựng lại khi prefab đổi, menu tạo
prefab mẫu, preview, browser). Runtime chưa gọi spawner (chờ bộ thực thi Skill Graph, GDD OQ-S16 / OQ-S17).

### 1d. Làm lại theo tool tham khảo (2026-11-09 — thay 1b, 1c)
Node dựng tự động từ `SkillSchema`: tiêu đề `id:Kind`, cổng luồng vào / ra ở thanh tiêu đề, mỗi trường một hàng (cổng dữ
liệu + tên + ô nhập; nối dây thì hiện `nodeId.Output`), cổng ra ở đáy, hình xem trước vùng cho Search / Hit. Dây luồng
(`TriggerEdge`) có nhãn điều kiện ở giữa, bấm hoặc chuột phải để đổi. Toolbar: New, Open, Save, Save As, Memo (StickyNote),
Copy, Paste (Ctrl+C / V), Undo, Redo (ảnh chụp XML, tối đa 100). Palette trái: 10 loại node. Tooltip tiếng Việt từ
`SkillToolNotes`. Mở được file có lỗi (báo ở thanh trạng thái), chỉ lưu file hợp lệ.

### 1c. Search, Look At và cổng On Start / On End (2026-11-07)
Node Search có cổng nhánh Found / Not Found và ba cổng ra dữ liệu (Result: danh sách — tím; Nearest: thực thể — cam;
Nearest Position — xanh). Look At nhận vị trí, thực thể hoặc danh sách (cổng trắng). Node có cửa sổ (Hit, Armor, Move)
có hai cổng ra On Start / On End; cổng nào được nối quyết định `after="end"`. Mỗi kỹ năng một điểm rẽ (Hold Gate hoặc Search).

### 2. Mô hình luồng thuần C# (`SkillFlow`, trong `Evermeadow.Core.Skills`)
Luồng = header (node Start) + danh sách bước, mỗi bước có **độ trễ so với node trước** (GDD Rule 5); HoldGate kết thúc luồng
chính và rẽ thành Tap / Charged (độ trễ nhánh tính từ lúc thoát cổng). `SkillFlow.FromDef` đổi mốc thời gian thành độ trễ;
`ToDraft` cộng dồn độ trễ thành mốc (làm tròn 1 µs) cho `SkillXmlWriter`. Vị trí node không thuộc dữ liệu: mở file thì đồ
thị tự xếp từ trái sang phải. Phần này test được ngoài Unity (`SkillFlowTests`: khứ hồi ba kỹ năng mẫu).

### 3. Lưu: chỉ file hợp lệ, chỉ khi byte đổi
Save đọc đồ thị từ Start theo cạnh nối → `SkillFlow` → `SkillXmlWriter.Write` → parse lại bằng `SkillXmlReader`; có lỗi thì
**không ghi** và hiện lỗi ở thanh trạng thái. Ghi UTF-8 không BOM, LF, chỉ khi byte khác bản trên đĩa, rồi
`AssetDatabase.Refresh`. Node không nối vào luồng không được lưu (có ghi chú).

### 4. Build chỉ kiểm tra
`SkillBuildCheck : IPreprocessBuildWithReport` nạp lại thư mục như game; mọi lỗi nạp, kể cả `PayloadHashMismatch` (file bị
sửa ngoài Skill Tool), → `BuildFailedException`. Không bao giờ ghi file.

### 5. Ranh giới assembly
- Công cụ trong `Evermeadow.Presentation.SkillTool.Editor` (`Assets/Game/Presentation/SkillTool.Editor/`,
  `includePlatforms: ["Editor"]`), tham chiếu `Evermeadow.Core.Skills`, `Evermeadow.Core.Skills.Unity` (mục 1f) và (từ
  2026-11-11, mục 1e) hai assembly VFX
  `Evermeadow.Presentation.Vfx`, `Evermeadow.Presentation.Vfx.Editor`.
- Assembly runtime không tham chiếu `UnityEditor` hay GraphView (AC-D03).
- `.gitattributes`: `Assets/StreamingAssets/Skills/**/*.xml text eol=lf` (mã băm phủ từng byte).

### Architecture Diagram
```
 Editor only (Evermeadow.Presentation.SkillTool.Editor)                       Game (ADR-0002)
 ┌──────────────────────────────┐   Open: TryParse → SkillFlow.FromDef   ┌────────────────────────────┐
 │ SkillToolWindow              │ ◀───────────────────────────────────── │ StreamingAssets/Skills/    │
 │  ├ SkillGraphView (GraphView)│   Save: SkillFlow.ToDraft → Writer →   │   100000.xml …  (git)      │──▶ FolderSkillTextSource
 │  └ Command Table list        │ ─────── TryParse ok → write ─────────▶ │   Tables/spear.xml         │    → SkillLoader → SkillLibrary
 └──────────────────────────────┘                                        └────────────────────────────┘
                                         SkillBuildCheck (build): load + validate, never write
```

### Key Interfaces
```csharp
namespace Evermeadow.Core.Skills                       // pure C#, tested outside Unity
{
    public sealed class SkillFlow
    {
        public static SkillFlow CreateDefault(string id);
        public static SkillFlow FromDef(SkillDef skill);   // times → delays
        public SkillDraft ToDraft();                       // delays → times (1 µs rounding)
    }
}
namespace Evermeadow.Presentation.SkillTool.Editor
{
    public static class SkillToolFiles
    {
        public static string NextId();                                // "100000" for an empty folder
        public static SkillFlow Load(string id, List<SkillError> errors);
        public static List<SkillError> Save(SkillFlow flow);          // writes nothing when errors
        public static List<SkillError> ValidateFolder(out int skillCount);
    }
}
```

### Implementation Guidelines
1. Mọi đọc/ghi XML đi qua `SkillXmlReader` / `SkillXmlWriter` của lõi; tool không tự dựng chuỗi XML.
2. Không lưu file mà `SkillXmlReader` từ chối.
3. Không giữ file đồ thị hay asset trung gian trong project; trạng thái chưa lưu chỉ sống trong cửa sổ (giữ qua domain reload
   khi luồng dựng được).
4. Đổi id = `Save As New Id` (tạo file mới); xóa hay đổi tên file cũ là việc có chủ đích của người làm game, vì bảng tổ hợp
   và Skill Stone trỏ tới id.

## Alternatives Considered

### Alternative 1: Graph Toolkit (`com.unity.graphtoolkit` 0.4.0-exp.2) — hướng của bản đầu
- **Description**: Đồ thị là asset `.skillgraph` trong project; postprocessor xuất XML khi lưu.
- **Pros**: Khung đồ thị hiện đại, Unity định hướng thay GraphView; menu tạo asset, kiểm lỗi trên node có sẵn.
- **Cons**: Spike 2026-11-06 (đọc mã nguồn package): **không có API công khai để tạo node hay nối cạnh từ code** (chỉ có
  ở phần internal, mở cho assembly test của Unity) → không mở được XML có sẵn thành đồ thị; mỗi đồ thị bắt buộc là một file
  asset trong project; package thử nghiệm, API đổi ở 6.4+. Những điểm đã học (lưu bằng `SaveToSerializedFileAndForget`,
  không cần importer, đuôi file khai báo không dấu chấm) không còn cần.
- **Rejection Reason**: Trái yêu cầu "tool đọc và tạo XML, không phải một phần của project". Đã gỡ khỏi manifest.

### Alternative 2: Đồ thị là nguồn (bất kỳ khung nào), XML sinh ra
- **Description**: Giữ file đồ thị trong git, XML là sản phẩm xuất; build so byte giữa đồ thị và XML.
- **Pros**: Giữ được bố cục node.
- **Cons**: Hai file mỗi kỹ năng; hai nguồn có thể lệch; dev phải hiểu cả hai.
- **Rejection Reason**: Người dùng chọn XML là nguồn duy nhất.

### Alternative 3: Soạn XML thủ công theo schema + validator
- **Description**: Viết XML bằng trình soạn văn bản.
- **Pros**: Rẻ nhất.
- **Cons**: Không kéo thả node; dễ sai mốc thời gian.
- **Rejection Reason**: Trái yêu cầu dùng đồ thị. Vẫn là đường lui: lõi và bộ kiểm tra không phụ thuộc tool.

## Consequences

### Positive
- Một nguồn dữ liệu (XML); mở/lưu khứ hồi không đổi nội dung; id dễ đọc và gán.
- Không package thử nghiệm; công cụ không vào bản build.
- Phần logic của tool (`SkillFlow`) test được ngoài Unity.

### Negative
- Bố cục node không được lưu (tự xếp khi mở).
- GraphView nằm trong namespace `Experimental` và Unity định hướng thay bằng Graph Toolkit; khi GraphView bị gỡ ở bản sau,
  phải viết lại phần giao diện (lõi `SkillFlow` giữ nguyên).
- GraphView không có undo cho thao tác trên đồ thị ở bản đầu.

## Risks
- **GraphView bị gỡ/đổi ở Unity sau 6.3.** Mitigation: giao diện tách khỏi `SkillFlow`; đường lui Alternative 3; xem lại khi
  nâng lên 6.7 LTS (Graph Toolkit có thể đã có API tạo node).
- **Hết id (999999).** Mitigation: 900 000 id; `NextId` báo khi hết.
- **Sửa tay XML.** Mitigation: `payload_hash` + build lỗi; mở bằng Skill Tool và lưu lại để sửa.
- **Mất chỉnh sửa chưa lưu.** Mitigation: `hasUnsavedChanges` hỏi khi đóng; xác nhận trước khi mở file khác; giữ qua domain
  reload khi luồng dựng được.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| skill-moveset.md | Rule 2: XML là nguồn, Skill Tool mở/tạo XML, không sửa tay | Decision 1, 3 |
| skill-moveset.md | Rule 3: id = tên file 6 chữ số từ 100000; gán vào bảng tổ hợp và Skill Stone | Decision 1 (`New` cấp id), lõi ADR-0002 |
| skill-moveset.md | Rule 5, 6, 12: luồng node có độ trễ, HoldGate rẽ nhánh | Decision 2 |
| skill-moveset.md | Rule 11, 13, 15: bảng tổ hợp, thứ tự khai báo, dòng bắt buộc | Decision 1 (chế độ Command Table) |
| skill-moveset.md | Rule 21, 31; AC-D07: không lưu file lỗi; build lỗi khi sửa tay | Decision 3, 4 |
| skill-moveset.md | AC-D03: không kiểu Editor ở runtime | Decision 5 |
| skill-moveset.md | OQ-S1 (Graph Toolkit) | Đã chốt: bỏ Graph Toolkit (Alternative 1) |

## Performance Implications
- **CPU**: Chỉ ở Editor; mở/lưu một kỹ năng là parse một file nhỏ (< 10 ms).
- **Memory / Load Time / Network**: Không ảnh hưởng bản build.

## Migration Plan
1. ~~Spike Graph Toolkit~~ — xong, bỏ (Alternative 1); package đã gỡ khỏi manifest, mã Graph Toolkit đã xóa.
2. Lõi: id = tên file (`IsSkillId`, `IdFromFileName`), `SkillFlow`, `SkillXmlWriter` không ghi `id` — xong, có test.
3. Tool: `SkillToolWindow`, `SkillGraphView`, `SkillToolFiles`, `SkillBuildCheck` — xong, biên dịch được; kiểm T1–T6 trong Editor.
4. Accept khi T1–T6 đạt.

## Validation Criteria
- T1–T6 đạt trên 6000.3.27f1.
- `SkillFlowTests`, `SkillXmlWriterTests`, `SkillXmlReaderTests` đạt.
- Sửa tay một XML → build đỏ `PayloadHashMismatch`; mở bằng Skill Tool và lưu lại → build qua.

## Related
- ADR-0002: Skill Runtime Data Contract.
- `design/gdd/skill-moveset.md` (Rule 2, 3, 5, 11–15, 21, 31; OQ-S1).
- Công cụ animation cùng menu: `Evermeadow ▸ Animation ▸ Sprite Clip Editor` (`Assets/Game/Presentation/SpriteAnimation.Editor/`).
