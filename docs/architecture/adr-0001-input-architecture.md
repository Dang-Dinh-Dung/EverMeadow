# ADR-0001: Input Architecture and Frame Clock

## Status
Accepted (2026-10-31, by user)

## Date
2026-10-31

## Last Verified
2026-10-31

## Decision Makers
Đặng Đình Dũng (user), Claude (lead); review: unity-specialist (2 BLOCKING → đã sửa), technical-director TD-ADR (CONCERNS → đã sửa theo lựa chọn của người dùng)

## Summary
GDD Input yêu cầu bộ đệm phím có mốc thời gian, stack chế độ có rào chống rò phím, nhận biết thiết bị và đổi phím,
chạy đúng khi hit-stop làm chậm thời gian; Combat và Character Controller cần chung một nguồn thời gian có scale.
ADR này chốt: một `FrameClock` (tick `long` 1 µs, nguồn thật duy nhất của dự án, scale qua handle có hạn dùng), lõi
Input C# thuần không tham chiếu engine, và một adapter Unity mỏng xử lý Input System **thủ công** trong một
`FrameDriver` theo thứ tự clock → input → gameplay, mọi action map luôn bật, chế độ lọc trong bộ đệm.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.3.27f1 (6.3 LTS), Input System package 1.20.1 |
| **Domain** | Input (+ Core: thời gian khung hình) |
| **Layer** | Foundation |
| **Knowledge Risk** | MEDIUM — Input System 1.20.1 mới hơn tài liệu engine (xác minh 1.11) và mới hơn mốc kiến thức LLM |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/input.md`, `breaking-changes.md`, `deprecated-apis.md` |
| **Post-Cutoff APIs Used** | Có nguồn (ref 1.11): `InputActionAsset`, action map, `InputAction.performed/canceled`, `CallbackContext.control.device`, `ReadValue<T>`, `PerformInteractiveRebinding`, `SaveBindingOverridesAsJson`/`LoadBindingOverridesFromJson`, control scheme. Giả định (chưa có nguồn): `InputSettings.updateMode = ProcessEventsManually` + `InputSystem.Update()`, `InputSystem.onDeviceChange`, `Application.focusChanged`, `Time.unscaledTimeAsDouble`, assembly `Unity.InputSystem.TestFramework` |
| **Verification Required** | (V1) callback bắn riêng cho từng sự kiện nhấn–nhả–nhấn trong một lần `InputSystem.Update()`; (V2) với `ProcessEventsManually`, callback chỉ bắn trong `InputSystem.Update()` do `FrameDriver` gọi, kể cả khi `timeScale` = 0 hoặc 0,05; UI (`InputSystemUIInputModule`) và đổi phím vẫn hoạt động ở manual mode; (V3) rút thiết bị khi đang giữ nút có bắn `canceled` không; (V4) mất focus (Editor và build) có reset thiết bị / bắn `canceled` không; (V5) trạng thái enabled và binding override của asset clone có sót lại giữa các lần Play khi tắt domain reload không; (V6) `InputTestFixture` dùng được qua tham chiếu `Unity.InputSystem.TestFramework` từ asmdef test; (V7) Button action với interaction mặc định: `started` và `performed` bắn cùng lúc; (V8) composite 2DVector mode `DigitalNormalized` cho đường chéo độ dài 1; (V9) `PerformInteractiveRebinding().Start()` ném lỗi khi action đang bật; (V10) `Object.Instantiate(InputActionAsset)` giữ GUID của action và binding, và khóa trong JSON của `SaveBindingOverridesAsJson` trên bản clone khớp asset gốc. V1–V2, V5–V10 có EditMode/PlayMode test trước khi đóng đợt 2 (AC-R10, AC-R19, AC-R32, AC-R41 của GDD Input) |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | TD-B4 "ID ổn định" — **phụ thuộc mềm** (chưa viết): chỉ chặn story lưu/nạp cấu hình đổi phím. Mọi phần khác của ADR không phụ thuộc |
| **Enables** | ADR "Character controller physics" (đã bỏ qua, người dùng 2026-11-01; số 0002 nay thuộc ADR-0002 Skill runtime data contract; dùng `IFrameClock`, `IInputQueries`); ADR/GDD Combat (hit-stop qua `ITimeScaleControl`); ADR "time domains" của Game Time (đọc đơn vị và nguồn thời gian chốt ở đây) |
| **Blocks** | Epic Input (đợt 1–2); epic Character Controller; story hit-stop của Combat |
| **Ordering Note** | Story lưu/nạp đổi phím chờ TD-B4. Khi viết ADR "time domains", tham chiếu ADR-0001 cho đơn vị tick và nguồn thời gian thật, không định nghĩa lại |

## Context

### Problem Statement
Prototype đọc `UnityEngine.Input` cũ trực tiếp trong `Update` của nhân vật. GDD Input (18 luật) và GDD Character
Controller (30 luật) cần: lần bấm không bao giờ mất trong hit-stop; rào chuyển chế độ để lần bấm của menu không
lọt xuống gameplay; mọi thiết bị đọc song song; đổi phím mà Settings đẩy vào; lõi unit test được không cần thiết bị;
và một nguồn thời gian có scale duy nhất để Input, Combat, Controller không lệch nhau một frame khi hit-stop kết thúc
(CC OQ-C3). Phải chốt trước dòng code production đầu tiên.

### Constraints
- Unity 6.3, Input System 1.20.1 đã cài; *Active Input Handling* = *Both* vì hai prototype còn dùng API cũ.
- Coding standards: DI, không singleton tĩnh (TD-C — Enter Play Mode tắt domain reload), doc comment cho public API,
  logic có unit test trước. Systems index đề xuất asmdef theo layer để cạnh phụ thuộc ngược thành lỗi biên dịch.
- Tài liệu engine không có nguồn cho update mode, thứ tự PlayerLoop, hành vi khi mất focus.
- Một lập trình viên; giải pháp phải ít mảnh.

### Requirements
- GDD Input Rule 1–18, F1–F5; 0 B GC mỗi frame ổn định (AC-P01); `Input.Step` p95 ≤ 0,1 ms (ADVISORY).
- GDD Character Controller Rule 28 (cửa sổ theo đồng hồ có scale), Rule 30 (tick, bước con), OQ-C3.

## Decision

**1. Phân lớp và quy ước asmdef.**
- Quy ước: một asmdef cho mỗi hệ, tên `Evermeadow.<Layer>.<System>[.Unity]`. Layer trong tên phản ánh layer của systems
  index (Foundation / Core / Feature / Presentation) để cạnh ngược hiện ra ngay ở tên; asmdef chỉ được tham chiếu asmdef
  cùng layer hoặc layer thấp hơn. Phần `.Unity` là adapter.
- `Evermeadow.Foundation.Time` — `FrameClock`, `noEngineReferences: true`.
- `Evermeadow.Foundation.Input` — lõi Input (bộ đệm, F1, stack chế độ, rào, chốt nút, theo dõi thiết bị F4, lặp
  Navigate F5, điều kiện Xuống F3, cấu hình + validator, xử lý xung đột đổi phím), `noEngineReferences: true`.
- `Evermeadow.Foundation.Input.Unity` — adapter: dịch sự kiện Input System thành sự kiện trừu tượng, cung cấp nguồn
  thời gian thật, bọc `InputActionAsset` cho đổi phím và biểu tượng phím, chứa `FrameDriver`. Không có luật gameplay.
- `Evermeadow.Bootstrap` — composition root (tham chiếu mọi asmdef, không asmdef nào tham chiếu nó).

**2. `FrameClock`: đơn vị và nguồn thời gian của toàn dự án.**
- **Đơn vị:** tick `long`, 1 tick = 1 µs. Mọi thời gian gameplay (Input, Controller, Combat) dùng đơn vị này.
- **Nguồn thật duy nhất:** `Time.unscaledTimeAsDouble` (cố định trong một frame) quy ra tick, kẹp đơn điệu
  (`RealNow = max(RealNow, mới)`).
- **Đọc** qua `IFrameClock`: `RealNow`, `ScaledNow`, `RealDelta`, `ScaledDelta`, `Scale`.
- **Ghi scale** qua `ITimeScaleControl` (giao diện riêng; composition root chỉ inject cho Combat và hệ Pause):
  - `PushScale(scale, owner, isAlive, durationRealTicks?)` → handle mang số thế hệ. Handle có `durationRealTicks` (hit-stop)
    **tự hết hạn bên trong `Advance`**: `Advance` chia khoảng thời gian thật của frame tại điểm hết hạn, tích phân phần
    trước với scale cũ và phần sau với scale mới — nên frame đầu tiên sau hit-stop đã chạy đúng tốc độ (CC OQ-C3).
  - Handle không thời hạn (pause) phải `PopScale`; chủ chết (`isAlive` = false) → tự gỡ ở `Advance` kế tiếp, log lỗi.
    Handle không hợp lệ / đã gỡ / của instance khác → `PopScale` trả `false`, log cảnh báo.
  - **Gộp:** scale hiệu dụng = **min** các scale đang hoạt động (hai hit-stop chồng nhau không nhân thành 0,0025).
  - Chủ pause về scale 0 **phải** push `Menu` hoặc `Locked` của Input cùng lúc (GDD Input Rule 10).
- **Làm tròn:** `ScaledDelta = floor((RealDelta × Scale) + remainder)`, phần dư (`double`, < 1 tick) giữ sang frame sau;
  tổng `ScaledNow` không trôi theo thời gian.
- **Đồng bộ engine:** `FrameDriver` gán `Time.timeScale = Scale` sau `Advance`. Hệ quả: Animator, particle và vật lý
  engine nhận scale mới trễ một frame (Consequences). Không hệ nào khác được ghi `Time.timeScale`.
- **Ranh giới với Game Time "time domains" (Game Time OQ-1):** ADR-0001 chốt **đơn vị tick và nguồn thời gian thật**
  cho toàn dự án. Miền lịch (kiểu `game_minute`, bộ đệm phần lẻ của lịch, token tạm dừng lịch, pipeline chuyển ngày)
  vẫn thuộc ADR "time domains". Game Time chỉ đọc `RealDelta` và tự kẹp theo luật của nó (Game Time Rule 4, 5, 7);
  `FrameClock` không biết lịch. Tên `FrameClock` / `ScaledNow` (không phải "Game clock") để không lẫn với đồng hồ lịch.

**3. Input System xử lý thủ công trong `FrameDriver`; mọi action map luôn bật; chế độ lọc trong bộ đệm.**
- `InputSettings.updateMode = ProcessEventsManually`, đặt trong **asset Input Settings của dự án** (không gán lúc runtime).
  Lý do (unity-specialist, TD): ở Dynamic Update, Input System xử lý sự kiện ở PlayerLoop `PreUpdate`, *trước* mọi
  `MonoBehaviour.Update` bất kể execution order — callback sẽ đóng dấu bằng đồng hồ của frame trước.
- Thứ tự mỗi frame, trong `Update` của một `FrameDriver` duy nhất có `[DefaultExecutionOrder(-1000)]`:
  1. `FrameClock.Advance(realΔ)`; gán `Time.timeScale`.
  2. `InputSystem.Update()` — callback nút bắn ở đây, đóng dấu bằng đồng hồ vừa tiến.
  3. Adapter đọc `Move`/`Navigate` bằng `ReadValue<Vector2>()`, đẩy một mẫu trục vào lõi.
  4. `InputService.EndStep()` — áp push/pop đã hoãn, tự gỡ chủ chết, phát `DeviceChanged` tối đa một lần.
  5. Các hệ gameplay `Update` (order mặc định). Không hệ gameplay nào đọc input trong `FixedUpdate`.
- Mọi action map được `Enable()` một lần lúc khởi tạo, không bật/tắt theo chế độ. `InputModeStack` (lõi) quyết định
  hành động nào được ghi; rào (epoch++, chốt nút đang giữ) hiện thực trong lõi. **Ngoại lệ duy nhất: đổi phím** (Decision 6).
- **Pha của nút:** nhấn = `performed`, nhả = `canceled`; `started` của action Button bị bỏ qua (V7). Press point mặc
  định cho bàn phím/nút; cò tay cầm dùng `trigger_threshold` của GDD Input làm press point trên binding.
- **Trục:** không dựa vào callback; `ReadValue<Vector2>()` mỗi frame (bước 3). Composite WASD mode `DigitalNormalized`
  (V8). Stick không đặt processor dead zone (hoặc tối thiểu) vì dead zone tròn F4 do lõi áp, không chuẩn hóa hai lần.
  Value action nhiều binding chỉ báo control có độ lệch lớn nhất — chấp nhận cho trục.
- Đóng dấu bằng `FrameClock` **lúc callback**; không dùng `CallbackContext.time` ở MVP (OQ-I4; nếu dùng sau, thời gian
  sự kiện theo realtime-since-startup, cần offset).

**4. Asset hành động riêng.** `Assets/Game/Input/EvermeadowInput.inputactions` theo bảng Rule 2 GDD Input; map
`Gameplay`, `Menu` (dùng chung cho Dialogue), `Fishing`, `Map`, `Debug` (chỉ dev build). Không dùng
`InputSystem_Actions.inputactions` của template, không dùng project-wide actions (`InputSystem.actions` — gỡ khỏi Input
Settings vì asset đó tự enable), không dùng `PlayerInput`. Adapter **clone** asset lúc khởi tạo (`Object.Instantiate`)
để override không ghi vào asset gốc; `Object.Destroy` clone khi Dispose. UI dùng `InputSystemUIInputModule` trỏ vào map
`Menu` khi có EventSystem.

**5. Composition root và vòng đời.** `GameBootstrap` (MonoBehaviour) tạo `FrameClock`, `InputService`, adapter, `FrameDriver`
và inject. Không static mutable. Event tĩnh (`InputSystem.onDeviceChange`, `Application.focusChanged`) đăng ký trong
`OnEnable`, gỡ trong `OnDisable`/`Dispose()`; `Dispose()` cũng tắt action và hủy asset clone. Instance mới đặt lại
`seq`/`epoch`. Không chèn `PlayerLoopSystem` (trừ phương án dự phòng ở Risks).

**6. Đổi phím (ngoại lệ của "map luôn bật") và ID.**
- Hàm đổi phím tương tác cần action bị tắt (V9). Luồng: màn hình đổi phím (chế độ `Menu` đã ở đỉnh) → adapter
  `Disable()` action đang đổi → `PerformInteractiveRebinding` → hoàn tất/hủy → `Enable()` lại → lõi dựng rào
  (GDD Input Rule 17 "đổi phím tạo rào"). `ApplyBindingOverrides` lúc khởi động và `ResetBindings` cũng đi qua rào.
- **ID:** `InputActionId` là enum trong lõi, chỉ dùng trong bộ nhớ, **không bao giờ được lưu xuống đĩa**. Ánh xạ enum ↔
  `InputAction` qua GUID của action (validator lúc khởi tạo bắt sai lệch). Dữ liệu đổi phím do Input System sinh (khóa
  theo GUID của binding) được Input trả cho Settings dưới dạng **chuỗi mờ (opaque string)**; Settings lưu nguyên văn,
  không phân tích. **Không được xóa rồi tạo lại binding trong asset.** Story lưu/nạp chờ TD-B4 xác nhận cách khóa này.

### Architecture Diagram

```
                 Unity frame (Update phase)
  ┌───────────────────────────────────────────────────────────────┐
  │ FrameDriver [order -1000]                                      │
  │   1. FrameClock.Advance(unscaledΔ→ticks); Time.timeScale=Scale │
  │   2. InputSystem.Update()  ─► adapter callbacks ─► IInputEventSink
  │   3. ReadValue<Vector2>(Move/Navigate) ─► IInputEventSink      │
  │   4. InputService.EndStep()                                    │
  ├───────────────────────────────────────────────────────────────┤
  │ gameplay Update: Controller, Combat, UI…                       │
  │   read IInputQueries / IFrameClock;  Combat → ITimeScaleControl│
  └───────────────────────────────────────────────────────────────┘

 Evermeadow.Foundation.Time   Evermeadow.Foundation.Input   Evermeadow.Foundation.Input.Unity
 (noEngineReferences)         (noEngineReferences)          (Unity + Input System)
 ┌───────────────────┐        ┌───────────────────────┐     ┌──────────────────────────┐
 │ FrameClock        │◄───────│ InputService          │◄────│ UnityInputAdapter        │
 │  IFrameClock      │        │  InputBuffer (ring 32)│     │  InputActionAsset (clone)│
 │  ITimeScaleControl│        │  InputModeStack       │     │  UnityRealTimeSource     │
 │  ScaleHandle      │        │  ActiveDeviceTracker  │     │  GlyphProvider           │
 └───────────────────┘        │  NavRepeater, DownQual│     │  RebindController        │
                              │  InputConfig+Validator│     │  FrameDriver             │
                              └───────────────────────┘     └──────────────────────────┘
                     Evermeadow.Bootstrap (composition root) references all
```

### Key Interfaces

```csharp
// Evermeadow.Foundation.Time
public interface IFrameClock {
    long RealNow { get; }      // µs ticks, monotonic
    long ScaledNow { get; }    // µs ticks, scaled (hit-stop, pause)
    long RealDelta { get; }
    long ScaledDelta { get; }
    double Scale { get; }      // min of active scales, 1.0 when none
}
public interface ITimeScaleControl {   // injected only into Combat and Pause owners
    ScaleHandle PushScale(double scale, object owner, System.Func<bool> isAlive, long? durationRealTicks = null);
    bool PopScale(ScaleHandle handle);
}

// Evermeadow.Foundation.Input — what gameplay systems use
public interface IInputQueries {
    bool TryConsume(InputActionId action, long windowTicks, ClockKind clock, out InputRecord record);
    bool Peek(InputActionId action, long windowTicks, ClockKind clock, out InputRecord record);
    bool WasReleasedAfter(InputActionId action, long seq);
    bool IsHeld(InputActionId action);
    Vec2 ReadMove();
    Vec2 ReadNavigate();
    bool TryGetNavigateStep(out Vec2 direction);   // bước lặp Navigate của frame này (F5)
    void RegisterConsumer(object consumer, InputActionId action, long maxWindowTicks, ClockKind clock);
}
public interface IInputModes {
    ModeHandle Push(InputMode mode, object owner, System.Func<bool> isAlive);
    bool Pop(ModeHandle handle);
    bool IsTop(ModeHandle handle);
    event System.Action<ModeHandle, bool> TopChanged;
}
public interface IInputDevices {
    DeviceKind Active { get; }
    event System.Action<DeviceKind> DeviceChanged;
    event System.Action DeviceLost;
}
// Adapter → core
public interface IInputEventSink {
    void OnAction(InputActionId action, InputPhase phase, DeviceKind device, int deviceId, bool isPointer);
    void OnAxisSample(AxisId axis, Vec2 raw, DeviceKind device);
    void OnDeviceSignal(DeviceKind device, int deviceId, SignalKind kind, float magnitude);
    void OnDeviceRemoved(DeviceKind device, int deviceId);
    void OnFocusChanged(bool focused);
}
```

**Sửa đổi khi triển khai đợt 1 (2026-10-31, người dùng duyệt):** (1) `OnAction`/`OnDeviceSignal` thêm `deviceId` để rút
một tay cầm chỉ chốt nút của tay cầm đó (Rule 15) và biết tay cầm dùng gần nhất; (2) `OnAction` thêm `isPointer` để bỏ
qua click chuột ở bước lấy lại focus (Rule 15); (3) `IInputQueries` thêm `TryGetNavigateStep` (Rule 16, F5); (4) cảnh báo
"cửa sổ vượt mức đã đăng ký" (Rule 7) đếm một lần **mỗi hành động**, không mỗi cặp (bên tiêu thụ, hành động), vì
`TryConsume` không nhận bên tiêu thụ; (5) test lõi ở `Assets/Tests/EditMode/Foundation/`. `InputService` còn có
`BeginStep`/`EndStep`, `ApplyBarrier` (rào đổi phím), `SetChordMode`, `SetHoldMode`, `SetDeadZone`, `AddLatchedHold`
(nút đang giữ lúc khởi tạo).

`ClockKind` = `Real` | `Scaled` (GDD gọi là `Real` | `Game`). `InputRecord` là struct (seq, action, baseAction, phase,
tReal, tScaled, device, epoch, move, consumed). `Vec2` là struct riêng của lõi (không dùng `UnityEngine.Vector2`).

### Implementation Guidelines
- Asmdef `noEngineReferences` **must never** tham chiếu `UnityEngine` hay `UnityEngine.InputSystem` (AC-R01, AC-R40).
- Asmdef **must only** tham chiếu asmdef cùng layer hoặc layer thấp hơn; không asmdef nào tham chiếu `Evermeadow.Bootstrap`.
- Gameplay **must never** đọc `Keyboard.current`, `Gamepad.current`, `Mouse.current`, `UnityEngine.Input` hay `InputAction`
  trực tiếp; chỉ qua `IInputQueries` / `IInputModes` / `IInputDevices`.
- Không hệ nào ngoài `FrameDriver` **must** ghi `Time.timeScale`; thay đổi tốc độ **must** qua `ITimeScaleControl`.
  Chỉ Combat và hệ Pause **may** nhận `ITimeScaleControl`.
- Mọi thời gian gameplay **must** là tick `long` 1 µs; không cộng dồn `float`/`double` cho bộ đếm.
- `InputSettings.updateMode` **must** là `ProcessEventsManually` trong asset settings; chỉ `FrameDriver` gọi `InputSystem.Update()`.
- Adapter **must** ghi nút từ `performed` (nhấn) và `canceled` (nhả), **must never** ghi `started` của action Button.
- Trục **must** được đọc bằng `ReadValue<Vector2>()` mỗi frame trong `FrameDriver`, không qua callback.
- Action map **must not** được bật/tắt theo chế độ; ngoại lệ duy nhất: action đang đổi phím, luôn kết thúc bằng rào.
- `InputActionId` **must never** được lưu; dữ liệu đổi phím **must** đi dưới dạng chuỗi mờ. **Must never** xóa rồi tạo lại binding.
- Không static mutable, không singleton; composition root inject; `Dispose()` gỡ mọi đăng ký.
- Bộ đệm và tập dữ liệu runtime **must** cấp phát trước; 0 B GC mỗi frame ổn định.
- Không dùng `PlayerInput`, `InputSystem.actions`, `InputSystem_Actions.inputactions`.

## Alternatives Considered

### Alternative 1: Bật/tắt action map theo chế độ
- **Description**: mỗi chế độ ↔ một map; push/pop gọi `Enable()/Disable()`.
- **Pros**: gần cách dùng mẫu của Unity; engine tự chặn hành động ngoài chế độ.
- **Cons**: hành vi khi bật lại map lúc nút đang giữ và khi tắt map chưa có nguồn; rào và chốt nút vẫn phải tự làm; khó unit test.
- **Rejection Reason**: phụ thuộc hành vi engine chưa xác minh đúng ở binding test 1 của GDD Input.

### Alternative 2: `PlayerInput` + project-wide actions
- **Pros**: ít code; tự đổi control scheme.
- **Cons**: trạng thái global (vi phạm TD-C khi tắt domain reload); tự đổi scheme (xung đột Rule 3/13: mọi thiết bị luôn đọc); không unit test được.
- **Rejection Reason**: mâu thuẫn TD-C và GDD Input Rule 3, 13, 18.

### Alternative 3: Dynamic Update (mặc định của Input System)
- **Pros**: không phải tự gọi `InputSystem.Update()`.
- **Cons**: sự kiện xử lý ở `PreUpdate`, trước mọi `Update` — callback đóng dấu bằng đồng hồ frame trước; muốn clock chạy trước phải chèn PlayerLoop (phải reset khi tắt domain reload).
- **Rejection Reason**: thứ tự clock → input không đảm bảo; giữ làm phương án dự phòng dạng PlayerLoop (Risks).

### Alternative 4: Fixed Update cho input và gameplay
- **Cons**: ở `timeScale` 0,05 bước vật lý gần như không chạy → lần bấm dồn/trễ trong hit-stop; Fixed Timestep đang 50 Hz.
- **Rejection Reason**: vi phạm GDD Input Rule 10 và CC binding test 3.

### Alternative 5: Thời gian `double` giây; scale gộp bằng nhân
- **Cons**: so sánh biên ≤ (F1) và `floor` (F5) hỏng vì sai số thực; hai hit-stop chồng nhau nhân thành 0,0025.
- **Rejection Reason**: GDD Input Rule 5 yêu cầu tick nguyên; gộp bằng min (TD).

## Consequences

### Positive
- Hơn 120 tiêu chí nghiệm thu của hai GDD chạy được ở EditMode, không cần thiết bị.
- Một nguồn thời gian; hit-stop tự hết hạn bên trong `Advance` nên không lệch frame khi kết thúc; handle bị rò tự gỡ.
- Rào chuyển chế độ tất định, không phụ thuộc engine.
- Ranh giới rõ với Game Time "time domains": đơn vị và nguồn thật chốt một lần.

### Negative
- Tự viết nhiều hơn dùng `PlayerInput` (bộ đệm, stack, tracker, glyph).
- `FrameDriver` gọi `InputSystem.Update()` thủ công: mọi hệ dựa vào xử lý input tự động của engine (UI module) phải được kiểm ở manual mode (V2).
- `Time.timeScale` là "bóng" của `FrameClock.Scale` và được gán sau khi frame bắt đầu: Animator, particle, vật lý engine nhận scale mới trễ một frame.
- Adapter phải giữ ánh xạ enum ↔ action GUID đồng bộ với asset (validator lúc khởi tạo).

## Risks
- **V1–V10 sai giả định** → mỗi mục có test trước story tiếp theo. V2 sai → phương án dự phòng: Dynamic Update + chèn
  `PlayerLoopSystem` gọi `FrameClock.Advance` trước bước xử lý input của engine, reset trong
  `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`.
- **Project-wide actions / Input Settings của template còn tự enable asset mặc định** → bước setup gỡ ra; test tĩnh kiểm.
- **TD-B4 chọn cách khóa khác** → story lưu/nạp đổi phím đang chờ TD-B4; chưa có dữ liệu người chơi nên chi phí đổi thấp.
- **GDD Input và Character Controller chưa review độc lập** → ADR có thể phải sửa theo `/design-review`; lõi tách lớp giảm chi phí sửa.
- **Steam Input / layout tay cầm** (OQ-I8) chưa giải quyết; kiểu biểu tượng có ghi đè từ Settings.
- **Module thời gian chưa có dòng trong systems index** → thêm khi cập nhật index (đề xuất: ghi vào hàng Game Time & Calendar hoặc thêm hàng "Frame Clock" Foundation).

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| input.md | Rule 1 (chỉ hành động có tên; cưỡng chế bằng test tĩnh) | asmdef theo layer + Implementation Guidelines |
| input.md | Rule 5, F1 (bản ghi, tick nguyên, đồng hồ inject đơn điệu, đóng dấu lúc callback) | `FrameClock` tick µs đơn điệu; `IInputEventSink`; Decision 3 |
| input.md | Rule 8 (`TryConsume`, `Peek`, `WasReleasedAfter`, `IsHeld`, `ReadMove`; đồng hồ Real/Game) | `IInputQueries`, `ClockKind` |
| input.md | Rule 10 (độc lập timeScale; timeScale = 0 phải kèm push) | manual update trong `FrameDriver`; luật chủ pause push `Menu`/`Locked` |
| input.md | Rule 11–12 (stack token, rào) | `IInputModes` trong lõi; map luôn bật |
| input.md | Rule 13–15 (thiết bị, biểu tượng trước UI, rút tay cầm, focus) | `ActiveDeviceTracker` lõi + adapter `onDeviceChange`/focus; `EndStep` trước gameplay |
| input.md | Rule 17 (đổi phím, rào, ID ổn định) | Decision 6 |
| input.md | Rule 18, OQ-I1/I3/I6/I11 (một instance, Dispose, hooks) | Decision 5; interfaces lõi làm hook H-1…H-13 |
| character-controller.md | Rule 28, 30, OQ-C3 (đồng hồ có scale, tick, dịch vụ dùng chung, không trễ frame khi hit-stop hết) | `IFrameClock` dùng chung; hit-stop tự hết hạn trong `Advance` |

## Performance Implications
- **CPU**: `Advance` + một lần `InputSystem.Update()` + `EndStep` mỗi frame; mục tiêu `Input.Step` p95 ≤ 0,1 ms (ADVISORY).
- **Memory**: vòng 32 bản ghi + tập nhỏ cấp phát trước; 0 B GC mỗi frame.
- **Load Time**: clone một asset nhỏ lúc khởi động — không đáng kể.
- **Network**: không áp dụng.

## Migration Plan
Chưa có code production. Prototype trong `Assets/_Prototypes/` giữ API cũ, không được tham chiếu từ code production; khi
xóa prototype, chuyển *Active Input Handling* sang chỉ Input System (GDD Input OQ-I9). Đổi `updateMode` sang Manual sẽ
làm hai prototype (dùng API cũ) không bị ảnh hưởng, vì API cũ không đi qua Input System.

## Validation Criteria
- EditMode: AC của GDD Input H.1–H.5 và AC-P01 xanh trên lõi với đồng hồ giả và nguồn sự kiện giả; test `FrameClock`:
  hit-stop hết hạn giữa frame chia delta đúng, gộp bằng min, phần dư làm tròn không trôi, handle chủ chết tự gỡ.
- PlayMode: AC-R10, AC-R19, AC-R32, AC-R41 (V1, V2, V5) và test V6–V10 xanh trước khi đóng đợt 2.
- Test tĩnh AC-R01 và quy ước tham chiếu asmdef theo layer xanh.

## Related
- design/gdd/input.md; design/gdd/character-controller.md (Rule 28, 30, OQ-C3); design/gdd/game-time-calendar.md (OQ-1 "time domains" — ranh giới ở Decision 2)
- ADR "Character controller physics" (đã bỏ qua 2026-11-01; ADR-0002 là Skill runtime data contract, phụ thuộc ADR này)
- TD-B4 "ID ổn định" (chưa viết; phụ thuộc mềm)
