# ADR-0002: Skill Runtime Data Contract

## Status
Proposed

## Date
2026-11-06

## Last Verified
2026-11-06

## Decision Makers
Đặng Đình Dũng (user), Claude (lead). Rà soát: unity-specialist (2 BLOCKING về Addressables và importer → đã chuyển sang ADR-0003 / bỏ Addressables), technical-director TD-ADR (CONCERNS → đã sửa theo lựa chọn của người dùng: tách hai ADR, XML thật trong git, SkillCatalog, bảng tổ hợp là graph). **Sửa 2026-11-06 (quyết định người dùng, lúc bắt đầu code):** bỏ
`SkillCatalog`/`TextAsset`; XML là file `.xml` thật trong thư mục riêng `Assets/StreamingAssets/Skills/`, game đọc bằng
`FolderSkillTextSource` (System.IO), không Addressables.

## Summary
GDD Skill & Moveset biến mọi đòn thành dữ liệu XML mà game chỉ đọc, nhưng chưa chốt hợp đồng giữa dữ liệu và game. ADR này
chốt hợp đồng **phía chạy**: định dạng XML của kỹ năng và bảng tổ hợp, lõi C# thuần `Evermeadow.Core.Skills` (parser, bộ
kiểm tra, thư viện, hàm chọn, dòng thời gian tick nguyên), cách quy thời gian về bước 1/60 s, cách nạp qua `ISkillTextSource`
(hiện thực đầu tiên `FolderSkillTextSource` đọc file `.xml` trong `Assets/StreamingAssets/Skills/`), và bộ kiểm tra cấp thư viện nhận danh mục dữ liệu ngoài hệ. ADR không phụ thuộc
công cụ soạn; Skill Tool (mở/lưu XML) nằm ở ADR-0003. **Sửa 2026-11-06 (người dùng):** id kỹ năng = tên file XML, số 6
chữ số từ 100000; trong file không có thuộc tính `id`. **Sửa 2026-11-07 (người dùng):** header `owner="player|creature"`;
node Start chụp giá trị toàn cục lúc bắt đầu (`SkillStartValues`: Owner Position/ID, Target Position/ID — Target chỉ cho
quái, là người chơi); node có cổng vào dữ liệu `origin` / `toward` / `only` ghi thành thuộc tính (ví dụ
`origin="target.position"`), bảng cổng theo loại node nằm ở `SkillValues.InputsOf` / `Accepts`; node `LookAt` (thay Face, cả người chơi lẫn quái).
**Sửa 2026-11-07 (2):** điểm rẽ tổng quát — một HoldGate **hoặc** một `Search` mỗi kỹ năng (`<node type="Search" at for gid
radius center>` với nhánh `found` / `not_found`; `SkillDef.Search`, `SplitAt`; kết quả `search.list` / `search.nearest` /
`search.nearest_position` chỉ hợp lệ trong nhánh found); kiểu dữ liệu thêm danh sách thực thể; node có thể mang
`after="end"` (mốc tính từ lúc cửa sổ của node trước đóng, đã cộng sẵn vào `at`; giữ để tool mở lại).
**Sửa 2026-11-08 (người dùng):** file kỹ năng chỉ thể hiện kỹ năng (animation, hiệu ứng, vùng chạm, chuyển động); header
chỉ còn `schema_version` + `owner`. `name_key`, `kind`, `contexts`, `weapon_families`, `tags`, `cooldown`, `land_policy`
bị từ chối và rời khỏi `SkillDef`; logic thuộc hệ thống gán id (GDD OQ-S15). Bảng tổ hợp bỏ liên kết `after="tag:…"`.
`SkillLibraryValidator` kiểm luật phụ thuộc cách dùng cho kỹ năng nằm trong bảng tổ hợp: `ArmorInAttackSkill`,
`MoveInRecovery`; bỏ `RowSpecial`, `FamilyMismatch`, `ContextMismatch`, `NoHit`, `SpecialNeedsCooldown`.
**Sửa 2026-11-08 (2):** node `CancelWindow`, `HoldGate`, `Move`, `Armor` rời schema kỹ năng (thuộc hệ combo); điểm rẽ duy
nhất là `Search` (`SkillDef.Found` / `NotFound`); `SkillEvent` bỏ `Move`/`Amount`/`Opposite`; bỏ `SkillKind`, `LandPolicy`,
`MoveKind`. Validator bỏ `ArmorInAttackSkill`, `MoveInRecovery`, dòng `dive` bắt buộc; còn `MissingSkill`, `MissingAfter`,
`RowCreatureSkill`, dòng bắt buộc theo context. Kỹ năng dự phòng chỉ còn PlayAnimation, Hit, End.
**Sửa 2026-11-09 (người dùng — làm lại theo tool tham khảo):** file kỹ năng là **đồ thị sự kiện** `schema_version="2"`:
`<node id type x y>` với `<f n v|ref>`, `<link from to when=start|end|hit|found|not_found>`, `<memo>`; bảng node / trường /
cổng ra / điều kiện nằm ở `SkillSchema` (Core); dữ liệu sửa được là `SkillGraphData`; `SkillGraphValidator` kiểm Start id 0,
id không trùng, một cha, không vòng, điều kiện hợp lệ theo loại cha, giá trị đúng kiểu, dây từ Start hoặc tổ tiên với kiểu
gán được. `SkillXmlReader.TryParseData` đọc khoan dung (tool mở file lỗi), `TryParse` = đọc + kiểm. Bỏ dòng thời gian phẳng
(`SkillEvent`, `SkillFlow`, End, Found/NotFound dạng nhánh). `Vec3` thêm vào Foundation.Common. Bộ thực thi lúc chạy chưa có
(GDD OQ-S16); `SkillLibrary.Select` / bảng tổ hợp không đổi.

**Sửa 2026-11-11 (người dùng — nạp lại nóng):** sửa kỹ năng trong Play Mode, Save là game dùng ngay, không cần dừng /
chạy lại. Thêm `SkillRepository` (Core, thuần C#): giữ `SkillLibrary` hiện hành, `LoadAll` nạp cả thư mục, `ReloadText(tên
file, xml)` thay **một** kỹ năng / bảng (`SkillLibrary.Replace` / `ReplaceTable`), `RemoveFile` xóa; file mới có lỗi →
`Rejected`, **game giữ bản cũ**; cùng nội dung → `Unchanged` (Save và import báo hai lần không sao); sự kiện `Changed` +
`Version`. **Luật cho bộ thực thi (OQ-S16):** tra kỹ năng theo id ở **mỗi lần ra đòn**, không giữ `SkillDef` qua các lần;
đòn đang chạy chạy nốt bản nó bắt đầu (`SkillDef` bất biến). Host trong scene: `SkillLibraryRunner`
(`Evermeadow.Core.Skills.Unity`, nạp `StreamingAssets/Skills` lúc Awake; Editor kiểm cả mã băm), `GameBootstrap` thêm khi
scene chưa có và lộ ra `Skills`. Cầu nối Editor ở ADR-0003 §1f. Bản build không nạp lại (không có nguồn đẩy).

**Sửa 2026-11-13 (người dùng — moveset từ đá khảm):** thêm hợp đồng dữ liệu thứ hai, **moveset XML**
(`StreamingAssets/Movesets/<id>.xml`: `<moveset schema_version="1" name weapons>`, `<note>`, `<row input skill context moving
dir after link_window priority cooldown/>`) trong assembly thuần C# mới `Evermeadow.Core.Moveset` (tham chiếu `Core.Skills`,
`Foundation.Time`): `MoveInput` (nút logic + cử chỉ), `MovesetData` / `MovesetDef`, `MovesetXml` (đọc khoan dung, ghi xác
định), `MovesetValidator`, `MovesetLibrary` / `MovesetRepository` (nạp lại nóng, luật như `SkillRepository`), `MovesetLoadout`
(2–4 ô, chặn sai vũ khí / trùng / khảm hai lần), `MoveSelector` (hàm thuần, không cấp phát), `MoveGestureRecognizer`. Host
`MovesetLibraryRunner` (`Evermeadow.Core.Moveset.Unity`); `GameBootstrap` thêm và lộ ra `Movesets`. Bảng tổ hợp
(`CommandTable`) bị thay về thiết kế (GDD Rule 35–44), code giữ tới khi Controller chuyển. Chưa có ADR riêng cho moveset:
nếu `modes.workflow: full` đòi, tách thành ADR-0004 khi viết GDD Loadout / Skill Stone.

**Sửa 2026-11-14 (người dùng — node End + bộ chạy tối thiểu):** thêm loại node `End` (khối trường chung, không điều kiện
ra; validator `EndHasChildren`). Lõi `SkillRun` (`Core.Skills`, thuần C#): hàng đợi sự kiện bắt đầu / kết thúc theo tick,
trigger start / end / found / not_found (hit chờ Combat), End = kết thúc kỹ năng (bỏ node chưa chạy, dừng animation và node
không hạn, node đang chạy chạy hết), `EndTick` cho cửa sổ nối, `Abort`; phần hiển thị qua `ISkillPerformer`. Assembly mới
`Evermeadow.Presentation.SkillRuntime.Unity`: `PlayerSkillPerformer` (clip theo tên trên PlayableGraph đè Animator, VFX qua
`VfxSpawner`, vẽ vùng Hit), `SkillAreaDebug`, `SkillCaster` (một kỹ năng tới End, khóa `MoveOnly`, chạy nốt nền). `GameBootstrap`
thêm `SkillCaster` (trường `vfxCatalog`, Editor tự nạp `Assets/Game/Art/VFX/VfxCatalog.asset`); Moveset Tester ra đòn qua nó.

**Sửa 2026-11-14 (2) (người dùng — animation chỉ qua skill, slot chung, quái + BT):** `AnimationSlots` (Presentation.SpriteAnimation,
thuần): danh sách slot từ file, slot tự phát vs slot skill; Animator player / creature chỉ nối slot tự phát, slot skill là state
không transition. Controller bỏ hẳn đòn đánh (`WeaponMoveset`, các state đòn, `AttackData.cs` đã xóa). Bộ chạy skill tổng quát:
`ISkillActor` (player: `PlayerSkillActor` giữ khóa MoveOnly; creature: `CreatureBody`), `SkillPerformer` (đổi tên từ
`PlayerSkillPerformer`), `SkillFxHost` (một `VfxSpawner` + `SkillAreaDebug` dùng chung). Assembly mới `Evermeadow.Core.AI`
(BT thuần: `BehaviourTree`, các node, `IBtAgent`) và `Evermeadow.Presentation.Creatures.Unity` (`CreatureBody`: tuần tra / đuổi /
tung skill; đặt ở lớp Presentation vì cần `SkillRuntime.Unity`). Skill Tool: AnimationType là ô chọn slot.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.3.27f1 (6.3 LTS) |
| **Domain** | Scripting (dữ liệu và lõi C# thuần); không có mô-đun engine-reference cho miền này |
| **Layer** | Core |
| **Knowledge Risk** | MEDIUM — phần chạy chỉ dùng BCL (`System.Xml`, `System.IO`, `System.Security.Cryptography`) và `Application.streamingAssetsPath`; không dùng API sau mốc kiến thức. Phần công cụ Editor nằm ở ADR-0003. Tài liệu engine-reference không có mục Scripting/Core: **NOT SOURCEABLE** trên tài liệu đã ghim |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `breaking-changes.md`, `deprecated-apis.md` (không có mục nào về XML, TextAsset, IL2CPP stripping) |
| **Post-Cutoff APIs Used** | Không có |
| **Verification Required** | (R1) `System.Xml` (`XmlReader`) biên dịch trong asmdef `noEngineReferences` với .NET Standard 2.1 và chạy trong player IL2CPP (cần `link.xml` hay không); (R2) 600 bước chạy kỹ năng cấp phát 0 byte; (R3) nạp bộ ~300 file ≤ 150 ms trên máy mục tiêu; (R4) file trong `StreamingAssets` vào bản build PC nguyên từng byte (không chuẩn hóa) để so mã băm, và đọc được bằng `System.IO` từ `Application.streamingAssetsPath` |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Accepted): đơn vị tick `long` 1 µs, `IFrameClock`, composition root |
| **Enables** | ADR-0003 (pipeline soạn); story lõi Skill & Moveset; Controller chạy `SkillDef`; GDD Weapon Tree, Skill Stone, Loadout, Combat (đọc sự kiện Hit) |
| **Blocks** | Story lõi Skill & Moveset (parser, bộ kiểm tra, hàm chọn, dòng thời gian) — **chờ Accept ADR này, không chờ spike** |
| **Ordering Note** | ADR-0001 có dòng Enables nhắc "ADR-0002 Character controller physics" (đã bị người dùng bỏ) — dòng đó được sửa cùng lượt ghi file này. TD-B4 "ID ổn định" chưa viết: id kỹ năng ở đây là đề xuất đầu vào |

## Context

### Problem Statement
Code hiện tại viết cứng ba ngữ cảnh đòn (`AttackTiming` trong `WeaponMoveset`). GDD Skill & Moveset đòi mọi đòn là kỹ năng
dữ liệu (ước tính 14 họ × ~16 kỹ năng + stone ≈ 300 file, 14 bảng tổ hợp) mà Controller chạy không cấp phát. Cần chốt trước
dòng code đầu tiên: định dạng, ranh giới assembly, cách quy thời gian, cách nạp và cách kiểm tra chéo giữa các file.

### Constraints
- Lõi C# thuần `noEngineReferences`, test được ngoài Unity; asmdef `Evermeadow.<Layer>.<System>[.Unity]`, chỉ tham chiếu
  cùng tầng hoặc thấp hơn; DI, không singleton tĩnh (TD-C, Enter Play Mode tắt domain reload); zero GC ở đường nóng.
- Controller (tầng **Core**) phải tham chiếu mô hình kỹ năng, nên mô hình **không thể ở tầng Feature** dù GDD ghi Layer
  Feature (Open: cập nhật systems index và GDD, xem Migration Plan).
- Combat (Core) phụ thuộc Controller để đọc sự kiện Hit; vì vậy `Core.Skills` **không bao giờ** tham chiếu Combat hay
  Weapon Tree (sẽ tạo vòng).
- Một lập trình viên.

### Requirements
- Skill & Moveset Rule 3, 5, 6, 12, 13, 21–24, 31–34; AC-D01–D22, AC-P01–P06.
- Thời gian quy về bước 1/60 s cùng một cách ở lúc build và lúc chạy.
- Mọi dữ liệu ngoài hệ (hit profile, khóa anim, cue, spawn, dải `timeline_scale`) đi vào bộ kiểm tra qua danh mục được inject.

## Decision

### 1. Định dạng XML (hợp đồng)
Tên trường theo GDD (`schema_version`, `weapon_families`, `land_policy`). Số thập phân dùng `.` (culture bất biến); UTF-8
không BOM; xuống dòng LF; node xếp theo `at` rồi thứ tự khai báo; phần tử hoặc thuộc tính lạ bị từ chối ở build (Rule 32).

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- file: Assets/StreamingAssets/Skills/100120.xml  → id 100120 (không có thuộc tính id) -->
<skill schema_version="1" kind="normal" contexts="ground"
       weapon_families="hammer" land_policy="continue" cooldown="0">
  <meta payload_hash="…"/>
  <timeline>
    <node type="PlayAnimation" at="0" anim="hammer.windup"/>
    <node type="HoldGate" at="0.30" hold_max="0.60">
      <branch name="tap">                         <!-- at = giây tính từ lúc thoát HoldGate -->
        <node type="Hit" at="0.05" duration="0.10" profile="hp.hammer.light"/>
        <node type="CancelWindow" at="0.18"/>
        <node type="End" at="0.50"/>
      </branch>
      <branch name="charged">
        <node type="Hit" at="0.05" duration="0.10" profile="hp.hammer.heavy"/>
        <node type="CancelWindow" at="0.25"/>
        <node type="End" at="0.55"/>
      </branch>
    </node>
  </timeline>
</skill>
```
- **Nhánh HoldGate:** đoạn trước HoldGate là chung; mỗi nhánh có mốc **tương đối so với lúc thoát cổng** (thời gian chờ
  `charge` không nằm trong dòng thời gian) và End riêng. `SkillDef` giữ đoạn chung + hai nhánh (`Common[]`, `Tap[]`,
  `Charged[]`), không phải một mảng phẳng.
- **Bảng tổ hợp** là file riêng:
  ```xml
  <command_table schema_version="1" weapon_family="spear">
    <meta payload_hash="…"/>
    <row skill="100001" context="ground" moving="any" dir="any" priority="0"/>
    <row skill="100002" context="ground" moving="any" dir="any" after="100001,tag:light" link_window="0.30"/>
  </command_table>
  ```
- Schema đầy đủ từng trường thuộc GDD Skill & Moveset (Rule 4–6, 11); ADR chốt cấu trúc, tên và quy ước định dạng.

### 2. Lõi C# thuần `Evermeadow.Core.Skills`
`noEngineReferences`, tham chiếu **chỉ** `Foundation.Common` và `Foundation.Time`. Chứa: `SkillDef`, `CommandTable`,
`SkillLibrary`, `SkillXmlReader`, `SkillValidator`, `SkillTime`, `SkillSelector`. Phân tích bằng `System.Xml.XmlReader`
(ít cấp phát hơn `XDocument`, schema đóng nên đọc tuần tự được). Enum riêng cho `Context`, `Dir`; Controller ánh xạ từ
`InputRecord` sang `SelectionQuery`.

### 3. Thời gian: một hàm quy về bước
Lưới 1/60 s là lưới **danh nghĩa**, độc lập với bước con thay đổi (≤ 1/30 s) của Controller (Rule 30). Chỉ có **một** hàm:
```
SkillTime.StepsOf(double seconds) = ceil(seconds × 60 − 1e-6)        // số bước nguyên, ≥ 0
SkillTime.TickOfStep(int steps)   = (steps × Ticks.PerSecond + 59) / 60   // tick long, làm tròn lên
```
Bộ kiểm tra lúc build, bộ kiểm tra lúc chạy và exporter đều gọi hai hàm này. `Ticks.FromSeconds` (làm tròn gần nhất) **không**
dùng cho mốc kỹ năng. `SkillDef` giữ mốc **chưa co giãn** (giây `double`); lúc `StartSkill`, Controller nhân
`timeline_scale` của nhánh vũ khí (Rule 34), quy về bước, rồi ghi tick vào một `SkillRun` có buffer cấp phát trước
(**bộ số đã chụp** của Rule 22). Sự kiện bắn ở bước mô phỏng đầu tiên có `ScaledNow ≥` mốc đã quy.

### 4. Nạp: `ISkillTextSource`
```csharp
public interface ISkillTextSource
{
    /// Enumerates (assetName, xmlText) for every skill and command table, once, at boot.
    void ReadAll(List<SkillSourceText> into);
}
```
Hiện thực đầu tiên `FolderSkillTextSource` (trong `Evermeadow.Core.Skills`, chỉ dùng `System.IO`, không engine) đọc **mọi
file `*.xml`** dưới một thư mục, kể cả thư mục con, theo thứ tự ordinal của đường dẫn tương đối (tất định trên mọi máy).
Thư mục là **`Assets/StreamingAssets/Skills/`** (quyết định người dùng 2026-11-06: file `.xml` thật trong thư mục riêng,
không Addressables, không `TextAsset`): Unity chép nguyên thư mục này vào bản build, nên XML trong git chính là XML game đọc.
Composition root truyền `Path.Combine(Application.streamingAssetsPath, "Skills")`, gọi `SkillLoader.Load` **đồng bộ** một
lần, rồi bỏ danh sách văn bản. Gốc của mỗi file (`<skill>` hay `<command_table>`) quyết định cách đọc; id của một kỹ năng là tên file
(`IdFromFileName`, ví dụ `spear/100123.xml` → `100123`), tên file bảng tổ hợp tùy ý (Skill Tool ghi `Tables/<họ>.xml`).
Thư mục thiếu thì đọc như rỗng và game chạy bằng kỹ năng dự phòng (Rule 22). Có thể thêm hiện thực khác sau mà không sửa lõi.
Không có Addressables ở ADR này (chưa cài, chưa xác minh phiên bản cho 6.3, giá trị thấp cho ~300 file nạp một lần).

### 5. Kiểm tra: ba tầng
- **Tầng tệp (trong `SkillXmlReader`):** cấu trúc, id, schema, node hợp lệ; trả `SkillError`, **không ném**.
- **Tầng thư viện (`SkillLibraryValidator.Validate(library, tables, ExternalCatalogs)`):** tham chiếu chéo (id tồn tại,
  đúng context và họ, đủ dòng bắt buộc, vòng nối đòn ≥ `min_loop_commit`, chuỗi đổi tên) và biên số (F1, F3, F5, F6, F9 ở
  cả hai đầu dải `timeline_scale`). Dữ liệu ngoài hệ vào qua `ExternalCatalogs` (tập chuỗi và struct thuần: `HitProfiles`,
  `AnimKeys`, `CueIds`, `SpawnIds`, `ScaleBands`). Chạy ở build và CI; ném lỗi chặn build.
- **Tầng chạy (Rule 22):** kẹp lỗi số có biên, dùng kỹ năng dự phòng khi lỗi cấu trúc, log một lần mỗi (id, lý do).

### 6. Mã băm
`payload_hash` = SHA-256 (hex thường, 64 ký tự) của byte UTF-8 của tài liệu **sau khi đặt `payload_hash=""`**, giữ nguyên
mọi byte khác; phát hiện thân XML bị sửa ngoài Skill Tool (dev build, mod). `graph_hash` cũ (bản đầu ADR-0003) không còn được ghi; bộ đọc
vẫn chấp nhận nó trong `<meta>` để không từ chối file cũ.
Lõi chỉ đọc và đối chiếu `payload_hash`; không bao giờ dùng `GetHashCode`.

### Architecture Diagram
```
 ISkillTextSource ──(assetName, xml)──▶ SkillXmlReader ──▶ SkillDef / CommandTable
 (FolderSkillTextSource:                       │ errors            │
  StreamingAssets/Skills/**/*.xml)             │                   │
                                               ▼                   ▼
                              SkillLibraryValidator ◀── ExternalCatalogs (injected sets)
                                               │ ok                │
                                               ▼                   ▼
                                  SkillLibrary + CommandTable ──▶ SkillSelector.Select(query)
                                                                      │ SkillDef
                                                                      ▼
                                            Controller: StartSkill → SkillRun (ticks, prealloc)
 Assemblies:  Core.Skills (noEngine; Foundation.Common, Foundation.Time)
              (FolderSkillTextSource ở chính Core.Skills; không cần Core.Skills.Unity)   ← Core.CharacterController → Core.Skills
```

### Key Interfaces
```csharp
namespace Evermeadow.Core.Skills
{
    public sealed class SkillDef { /* Id, Kind, Contexts, WeaponFamilies, LandPolicy, Cooldown,
                                      Common[], Tap[], Charged[]  (events in seconds, unscaled) */ }
    public readonly struct SkillError { /* SkillId, Code, Message */ }

    public static class SkillXmlReader
    {
        public static bool TryParse(string xml, out SkillDef skill, List<SkillError> errors);
        public static bool TryParseTable(string xml, out CommandTable table, List<SkillError> errors);
    }
    public static class SkillTime { public static int StepsOf(double seconds); public static long TickOfStep(int steps); }

    public readonly struct SelectionQuery { /* Context, Moving, Dir, ChainRef, NowTicks, Facing */ }
    public sealed class SkillLibrary
    {
        public bool TryGet(string id, out SkillDef skill);          // O(1), no allocation
        public SkillDef Fallback(SkillContext context);             // built-in, never null
        public SkillDef Select(string family, in SelectionQuery q); // pure, allocation-free, never null
    }
    public interface ISkillTextSource { void ReadAll(List<SkillSourceText> into); }
}
```

### Implementation Guidelines
1. `Core.Skills` **must** là `noEngineReferences` và **must never** tham chiếu `UnityEngine`, `UnityEditor`, GraphView,
   Input System, Combat hay Weapon Tree.
2. Mốc thời gian **must** là số nguyên (bước hoặc tick `long`) trong đường chạy; **must never** so sánh `float` giây.
   Mọi quy đổi đi qua `SkillTime`; **must never** gọi `Ticks.FromSeconds` cho mốc kỹ năng.
3. `StartSkill`, chạy dòng thời gian và `Select` **must** không cấp phát; **must never** đọc file hay parse XML sau khi
   nạp. Con trỏ chỉ số tăng dần, không LINQ, không closure, không chuỗi trong đường nóng.
4. Parser **must** trả lỗi, **must never** ném với dữ liệu xấu; một lỗi cho mỗi (id, lý do) trong phiên.
5. Dữ liệu ngoài hệ **must** vào bộ kiểm tra qua `ExternalCatalogs` được inject; **must never** để lõi đọc Combat hay Weapon
   Tree.
6. `SkillLibrary`, `CommandTable`, `ISkillTextSource` **must** được tạo ở composition root, truyền bằng DI; **must never**
   có singleton hoặc cache tĩnh thay đổi được.
7. Id kỹ năng = **tên file**: 6 chữ số, 100000–999999 (`SkillXmlReader.IsSkillId`); không có thuộc tính `id` trong file
   (thuộc tính lạ bị từ chối). Họ vũ khí và tag vẫn là `[a-z0-9._]`. Dòng bảng tổ hợp trỏ tới id số; `after` nhận id số hoặc
   `tag:<tên>`.

## Alternatives Considered

### Alternative 1: Addressables (nhãn `skills`) làm nguồn nạp
- **Description**: TextAsset gom nhãn Addressables, nạp bất đồng bộ lúc khởi động.
- **Pros**: Đồng bộ pipeline nội dung dự kiến; sẵn đường cho mod hay nội dung tải về.
- **Cons**: Package chưa cài, phiên bản chưa xác minh cho 6.3; nạp bất đồng bộ đổi thứ tự khởi động của `GameBootstrap`;
  cấu hình group, nhãn và bước build nội dung; rủi ro sub-asset (S7).
- **Rejection Reason**: Giá trị thực tế thấp (~300 file nhỏ, một lần nạp, không DLC hay mod). `ISkillTextSource` giữ đường mở:
  thêm hiện thực Addressables khi có nhu cầu thật mà không sửa lõi.

### Alternative 2: ScriptableObject làm dữ liệu (không XML)
- **Description**: Kỹ năng là ScriptableObject soạn trong Inspector.
- **Pros**: Tích hợp sẵn; tham chiếu asset trực tiếp.
- **Cons**: Không phải XML (trái yêu cầu GDD); gắn lõi vào engine, không test được ngoài Unity; khó diff và mod.
- **Rejection Reason**: Trái yêu cầu "mỗi kỹ năng là một file XML" và quy ước lõi thuần.

### Alternative 3: Controller định nghĩa hợp đồng chạy kỹ năng, `Feature.Skills` hiện thực (đảo phụ thuộc)
- **Description**: Giữ Skill & Moveset ở tầng Feature; Controller chỉ biết một interface `ISkillRunner`.
- **Pros**: Giữ nhãn Feature của GDD và systems index.
- **Cons**: Thêm một lớp trừu tượng cho dữ liệu thuần; mô hình dòng thời gian vẫn phải được Controller hiểu, nên interface
  gần như trùng với mô hình.
- **Rejection Reason**: Mô hình kỹ năng là dữ liệu thuần mà Controller tiêu thụ trực tiếp; đặt ở Core đơn giản hơn và cập
  nhật tên tầng trong tài liệu rẻ hơn một lớp trừu tượng.

### Alternative 4: `XDocument` (System.Xml.Linq) làm parser
- **Description**: Đọc cả tài liệu bằng LINQ to XML.
- **Pros**: Mã ngắn.
- **Cons**: Cấp phát nhiều lúc nạp; kéo thêm assembly vào build.
- **Rejection Reason**: `XmlReader` đủ cho schema đóng và nhẹ hơn; chỉ chạy lúc nạp nên không bắt buộc, chọn nhẹ hơn làm mặc định.

### Alternative 5: `SkillCatalog : ScriptableObject` giữ `TextAsset[]` (bản trước của ADR này)
- **Description**: XML nằm trong `Assets/Game/Skills/Generated/`, vào build vì được một catalog do Editor tự điền tham chiếu.
- **Pros**: Không phụ thuộc đường dẫn file lúc chạy; chạy trên mọi nền tảng.
- **Cons**: Thêm assembly `Core.Skills.Unity`, một asset catalog phải giữ đồng bộ, và bước kiểm "catalog đủ" ở build.
- **Rejection Reason**: Người dùng chọn file `.xml` thật trong thư mục riêng (2026-11-06). Đích là PC nên `StreamingAssets`
  đọc được bằng `System.IO`; nếu thêm nền tảng khác, hiện thực này có thể quay lại sau `ISkillTextSource` mà không sửa lõi.

## Consequences

### Positive
- Story lõi (parser, bộ kiểm tra, hàm chọn, dòng thời gian) bắt đầu được ngay, không chờ công cụ soạn.
- Lõi test được ngoài Unity với fixture XML; đổi công cụ soạn hay nơi nạp không đụng lõi.
- Một hàm quy bước duy nhất giữ biên kiểm lúc build khớp biên lúc chạy; `timeline_scale` không làm đổi hợp đồng.
- Không thêm package.

### Negative
- Phải cập nhật tên tầng của Skill & Moveset từ Feature sang Core trong GDD và systems index.
- Mọi dữ liệu ngoài hệ phải được đưa vào bộ kiểm tra dưới dạng danh mục (thêm một bước dựng danh mục ở Editor/CI).
- `StreamingAssets` chỉ đọc thẳng bằng `System.IO` được trên desktop; nếu sau này có nền tảng Android/Web, phải thêm một
  `ISkillTextSource` khác (đích hiện tại: PC — Steam/Epic).
- Mọi file `.xml` trong `StreamingAssets/Skills/` đều được nạp: file rác đặt nhầm vào đó sinh lỗi nạp (báo, không chặn game).

## Risks
- **`System.Xml` bị IL2CPP strip (R1).** Mitigation: `link.xml`; player build test (AC-D04); nếu thất bại, parser tự viết nhỏ cho schema đóng.
- **Hệ số nhân nội dung làm thời gian nạp vượt 150 ms (R3).** Mitigation: đo sớm; có thể nạp theo họ vũ khí.
- **Lệch hai biên kiểm.** Mitigation: một hàm `SkillTime` duy nhất, golden test so biên build với biên chạy ở hai đầu `timeline_scale`.
- **Vòng phụ thuộc Combat ↔ Controller ↔ Skills.** Mitigation: `ExternalCatalogs` inject; test asmdef quét tham chiếu (AC-D03).
- **Schema đổi.** Mitigation: `schema_version`, bước chuyển đổi (AC-D11), từ chối bản mới hơn.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| skill-moveset.md | Rule 3: id ổn định, `schema_version`, bảng đổi tên | Định dạng XML, `SkillXmlReader`, validator thư viện |
| skill-moveset.md | Rule 5, 12, 34: dòng thời gian Start → End, HoldGate hai nhánh, `timeline_scale` | Cấu trúc `Common/Tap/Charged`, `SkillRun` chụp bộ số sau co giãn |
| skill-moveset.md | Rule 13, 14, 28: bảng tổ hợp, hàm chọn thuần | `command_table` XML, `SkillLibrary.Select` |
| skill-moveset.md | Rule 21, 22, 24, 32: kiểm tra ba tầng, kẹp và dự phòng, từ chối dữ liệu lạ | Ba tầng kiểm tra, `ExternalCatalogs`, `SkillLibrary.Fallback` |
| skill-moveset.md | Rule 23: mọi mốc quy về bước 1/60 s | `SkillTime.StepsOf` / `TickOfStep` |
| skill-moveset.md | Rule 31: payload hash | Mục 6 |
| skill-moveset.md | AC-D01–D22, AC-P01/P02/P05 | Lõi test được ngoài Unity; Guideline 3 |
| character-controller.md | Rule 12: Controller chạy kỹ năng, không sở hữu dữ liệu | `Core.CharacterController → Core.Skills`; mô hình ở tầng Core |
| input.md | I-10 Special | ADR không đổi Input; `SelectionQuery` ánh xạ từ bản ghi lần bấm |

## Performance Implications
- **CPU**: Chọn kỹ năng và chạy dòng thời gian là so sánh số nguyên trên mảng sắp xếp; mục tiêu p99 ≤ 5 µs (ADVISORY). Parse chỉ lúc nạp.
- **Memory**: ~300 file ≈ 1,2 MB XML + mô hình bất biến (mảng struct phẳng) + một `SkillRun` có buffer cấp phát trước.
- **Load Time**: nạp nguội ≤ 150 ms cho bộ đầy đủ (ADVISORY, đề xuất).
- **Network**: Không áp dụng.

## Migration Plan
1. Tạo `Evermeadow.Core.Skills` với `SkillDef` sinh từ `AttackTiming` hiện có (adapter), giữ test Controller xanh.
2. Thêm `SkillXmlReader`, `SkillTime`, `SkillLibrary`, `Select` với fixture XML trong test.
3. Chuyển Controller từ `StartAttack(ctx, index, timing)` sang `StartSkill(def)` (Rule 12 của Controller đã sửa); bỏ adapter.
4. Cập nhật tên tầng Skill & Moveset từ Feature sang Core trong `skill-moveset.md` (Quick reference) và `systems-index.md`.
5. Thêm hành động Special vào Input theo I-10.

## Validation Criteria
- AC-D01–D22 và AC-P01/P02/P05 của Skill & Moveset đạt, phần lõi chạy ngoài Unity.
- Player build IL2CPP chạy được `XmlReader`; 0 byte cấp phát trong 600 bước chạy kỹ năng.
- Nạp bộ đầy đủ ≤ 150 ms; biên kiểm lúc build khớp biên chạy ở hai đầu dải `timeline_scale`.

## Related
- ADR-0001: Input Architecture and Frame Clock (đơn vị tick, composition root).
- ADR-0003: Skill Authoring Tool (Skill Tool: GraphView ↔ XML).
- `design/gdd/skill-moveset.md` (chính), `design/gdd/character-controller.md` (Rule 12), `design/gdd/input.md` (I-10).
- TD-B4 "ID ổn định" (chưa viết).
