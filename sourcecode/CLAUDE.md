# CLAUDE.md — Project Intelligence File
> Đọc file này **trước tiên** mỗi khi bắt đầu làm việc với project.  
> Dành cho: Claude Code · Claude Cowork · Cursor · Copilot  
> Cập nhật lần cuối: xem `## CHANGELOG`

---

## 📌 MỤC LỤC

1. [Project Overview](#1-project-overview)
2. [Architecture Manual](#2-architecture-manual)
3. [Coding Standards & Comment Rules](#3-coding-standards--comment-rules)
4. [Session Memory & Context Linking](#4-session-memory--context-linking)
5. [Changelog — Edit Log](#5-changelog--edit-log)
6. [Unit Test Guidelines](#6-unit-test-guidelines)
7. [Performance Optimization Rules](#7-performance-optimization-rules)
8. [How to Use This File](#8-how-to-use-this-file)

---

## 1. PROJECT OVERVIEW

```yaml
project_name:     "WeightChecking (SSFG Full Automation)"
version:          "xem Application.ProductVersion tại runtime"
language:         "C# / .NET Framework 4.8"
framework:        "WinForms + DevExpress"
target:           "x86 / Windows 10+"
primary_author:   "NDCongSP"
branch_dev:       "fIN_Main_dev"
env:              "production (factory floor)"
```

### Mục tiêu
Hệ thống kiểm tra trọng lượng tự động hoàn toàn cho trạm SSFG (Shoe-Sole Finishing & Grading) tại nhiều nhà máy Framas (fVN, fFT, fKV, fIN, fGE). Phần mềm điều phối băng tải, đọc barcode, kiểm tra kim loại, kiểm tra trọng lượng và in tem tự động — không cần thao tác tay trong luồng chính.

### Công nghệ / Thư viện chính

| Thư viện | Mục đích |
|----------|---------|
| **DevExpress WinForms** | UI (XtraForm, XtraMessageBox, SplashScreen) |
| **Entity Framework 6** | ORM → SQL Server (`IDCScanSystem` DB) |
| **Dapper** | Raw SQL query nhanh (cùng song song EF) |
| **Snap7ClientLib** | Kết nối PLC Siemens S7-1200 (TCP/IP) |
| **PLCPiProject** | Modbus RTU master (cân, PLC Delta) |
| **CognexLibrary_NETFramework** | Telnet driver cho camera Cognex DM290-X |
| **CoreScanner (Zebra)** | SDK scanner USB tại trạm kim loại & in |
| **Serilog** | Logging có cấu trúc |
| **AutoUpdaterDotNET** | Auto-update phần mềm |
| **Newtonsoft.Json** | Serialize config / RememberInfo |

### Ràng buộc quan trọng
- Connection string SQL Server được mã hóa MD5 lưu trong `userSettings` — **không commit plain-text**
- SSL validation bị bypass cho API nội bộ (self-signed cert) — chỉ dùng trong mạng nội bộ nhà máy
- Tất cả thao tác UI cross-thread phải qua `GlobalVariables.InvokeIfRequired()`
- Không dùng `async/await` cho handler barcode scanner (chạy trên background thread, gọi `Invoke` thủ công)
- `_scannerIsBussy[]` là guard chống double-scan cho mỗi trạm — không xoá trừ khi sensor out báo hiệu

---

## 2. ARCHITECTURE MANUAL

### 2.1 Sơ đồ thư mục

```
sourcecode/
├── WeightChecking/
│   └── WeightChecking/                  ← Project chính
│       ├── frmScaleNewUI.cs             ★ Form chính — toàn bộ luồng kiểm tra
│       ├── frmScaleNewUI.Designer.cs
│       ├── Login.cs / Login.Designer.cs # Đăng nhập
│       ├── Program.cs                   # Entry point
│       ├── AsyncAutoResetEvent.cs       # Primitive đồng bộ async trigger
│       ├── App.config                   # Connection string (encrypted), EF config
│       ├── tags.json                    # Cấu hình tag PLC S7 (tên, địa chỉ, deadband)
│       ├── RememberInfo.json            # State đếm thùng (persist giữa các session)
│       ├── Class/
│       │   ├── AnserU2Print.cs          # Driver máy in AnserU2 Smart One
│       │   └── CommonDefs.cs            # Enums Zebra CoreScanner SDK
│       ├── Enums/
│       │   ├── EnumBoxType.cs           # BX1~BX4 (kiểu thùng carton)
│       │   ├── EnumFactory.cs           # fVN, fFT, fKV, fIN, fGE
│       │   ├── EnumStation.cs           # Identification, Scale, Distribution
│       │   ├── EnumLocation.cs
│       │   └── RolesEnum.cs
│       ├── Events/
│       │   └── CustomEvents.cs          # Event hub trung tâm (sensor, scale, pusher...)
│       ├── Models/
│       │   ├── Entities/IDCScanSystem/  # EF entity classes (tblScanData, tblLog, ...)
│       │   ├── ProductInfoModel.cs      # DTO từ sp_vProductItemInfoGet
│       │   ├── MesoInfoModel.cs         # Thông tin nhà máy từ sp_GetMesoInfo
│       │   ├── RememberInfo.cs          # State đếm tồn (persist JSON)
│       │   ├── FT050Model.cs
│       │   └── ...
│       ├── StaticClass/
│       │   ├── GlobalVariables.cs       # Singleton state + InvokeIfRequired helper
│       │   ├── ScaleHelper.cs           # Đọc cân Modbus RTU
│       │   └── AutoPostingHelper.cs     # Logic auto transfer kho (Winline WMS)
│       ├── Views/
│       │   ├── frmMain.cs               # Shell điều hướng (nếu có)
│       │   ├── frmMasterData.cs         # Quản lý master data
│       │   ├── frmReports.cs            # Báo cáo
│       │   ├── frmSettings.cs           # Cài đặt máy
│       │   ├── frmUpdateTolerance.cs    # Chỉnh tolerance
│       │   ├── frmShowDetail.cs         # Xem chi tiết thùng
│       │   ├── frmDeleteBox.cs
│       │   └── frmConfirmPrint.cs
│       └── rptLabel.cs / rptLabelFail.cs  # DevExpress report templates
├── AnserU2_DK/                          # Test app cho máy in AnserU2
├── DLL modbus/                          # Test app Modbus RTU / TCP
├── WeightChecking/S7Client/             # Test app Snap7 standalone
└── CLAUDE.md                            # ← File này
```

### 2.2 Luồng vật lý & dữ liệu (Physical & Data Flow)

```
[Thùng hàng trên băng tải]
        │
        ▼ Sensor S_1 (before metal scan) → bắt đầu timer CheckReadQr
[Trạm 1: Metal Scanner (Zebra CoreScanner USB)]
  BarcodeScanner1Handle() → validate OC → ghi tblIncomingIDC
  → sp_vProductItemInfoGet → kiểm tra MetalScan flag
  → _metalScannerStatus (0=pass | 1=reject | 2=bypass)
        │
        ▼ Sensor S_M (middle metal) → ghi P1 (MetalPusher) xuống PLC S7
        │ Sensor S_2 (after metal scan) → ghi tblMetalScanResult, auto-transfer kho
        │
        ▼ Sensor (before weight scan) → reset UI, bắt đầu timer CheckReadQrWeight
[Trạm 2: Weight Check (Cognex DM290-X qua Telnet)]
  DataEvent_EventHandleValueChange() → BarcodeScanner2Handle()
  → validate OC → đợi _stableScale==1 (Modbus RTU)
  → sp_vProductItemInfoGet → tính StdGrossWeight + tolerance
  → so sánh → Pass/Fail → ghi tblScanData
  → auto-transfer kho → SendDynamicString() → in tem AnserU2
  → GlobalVariables.MyEvent.WeightPusher (0=ok | 1=reject)
        │
        ▼ Sensor (after weight scan) → reset _scannerIsBussy[1]
        │
        ▼ [Trạm 3: Distribution Scanner (Zebra CoreScanner USB)]
  BarcodeScanner3Handle() → kiểm tra đã pass chưa → điều khiển P3 (PrintPusher)
  → Sensor S_Sorting_FG / S_Sorting_Print → reset PrintPusher về 0

[PLC Siemens S7-1200] ←── PlcSubscriptionManager (polling 200ms)
  Tags: S_1, S_2, S_M, Metal_Result, S_Sorting_FG, S_Sorting_Print
        P1(MetalPusher), P2(WeightPusher), P3(PrintPusher), P4(MetalPusher1)

[PLC Delta DPV14SS2] ←── Modbus RTU (ComPort, 9600)
  D500~D512: delay timers, scale value, stable flag, sensor states
```

### 2.3 Cấu hình hệ thống (ConfigJson)

Config được load từ file JSON (path trong App.config / DB). Các key quan trọng:

| Key | Mục đích |
|-----|---------|
| `Station` | Mã trạm (SSFG01, SSFG02, ...) |
| `IsScale` | Bật/tắt kết nối cân Modbus RTU |
| `IsTest` | Chế độ test (bỏ qua kết nối hardware) |
| `ComPortScale` | COM port của cân |
| `IpCognexCamScale` | IP camera Cognex trạm cân |
| `TimerCheckQrMetal` | Timeout (ms) chờ scan QR trước metal |
| `AfterPrinting` | 0=trước sơn / 1=sau sơn (ảnh hưởng tolerance) |
| `FlagAutoPost` | Bật/tắt auto transfer kho WMS |
| `UpdatePath` | URL XML cho AutoUpdater |
| `DelayTimer1~5` | Delay (ms) ghi xuống PLC D508~D512 |

### 2.4 Quyết định kiến trúc (ADR)

| ID | Ngày | Quyết định | Lý do | Trạng thái |
|----|------|-----------|-------|-----------|
| ADR-1 | 2025-05-10 | Chuyển scanner trạm cân từ Zebra → Cognex DM290-X (Telnet) | Đọc QR tốt hơn trên dây chuyền tốc độ cao | Accepted |
| ADR-2 | 2025-XX-XX | Chuyển PLC protocol từ Modbus TCP → Snap7 (native S7) | Snap7 ổn định hơn, hỗ trợ tag name thay địa chỉ số | Accepted |
| ADR-3 | 2025-XX-XX | Custom title bar (FormBorderStyle.None) | UI fullscreen 1920×1080 cho màn hình công xưởng | Accepted |
| ADR-4 | 2024-08-19 | Auto posting check cả 2 chiều (check-in trước rồi transfer) | Tránh duplicate post khi thùng chạy qua nhiều trạm | Accepted |
<!-- Thêm ADR mới vào đây -->

---

## 3. CODING STANDARDS & COMMENT RULES

### 3.1 Cấu trúc comment bắt buộc

#### File header (mọi file source)
```typescript
/**
 * @file        src/services/userService.ts
 * @description Xử lý nghiệp vụ liên quan đến User: CRUD, auth, profile.
 * @author      <Tên> <email>
 * @created     YYYY-MM-DD
 * @modified    YYYY-MM-DD — <mô tả thay đổi ngắn>
 * @see         docs/user-flow.md
 */
```

#### Function / Method
```typescript
/**
 * Lấy thông tin user theo ID từ database.
 *
 * @param   {string}          userId  - UUID của user cần tìm
 * @param   {FetchOptions}    opts    - Tuỳ chọn cache / force-refresh
 * @returns {Promise<User>}           - Object User hoặc throw nếu không tìm thấy
 * @throws  {NotFoundError}           - Khi userId không tồn tại
 *
 * @example
 *   const user = await getUser("abc-123");
 *   console.log(user.name);
 */
async function getUser(userId: string, opts?: FetchOptions): Promise<User> { ... }
```

#### Inline comment — chỉ dùng khi logic KHÔNG tự giải thích được
```typescript
// ✅ Đúng: giải thích tại sao, không phải cái gì
const delay = 350; // Debounce 350ms — dưới ngưỡng này user chưa dừng gõ

// ❌ Sai: lặp lại code
const delay = 350; // gán delay bằng 350
```

#### TODO / FIXME / HACK
```typescript
// TODO(username, YYYY-MM-DD): Migrate sang API v2 sau khi backend deploy
// FIXME(username, YYYY-MM-DD): Race condition khi 2 tab cùng gọi refresh
// HACK(username, YYYY-MM-DD): Workaround bug iOS Safari — xoá khi upgrade lib
// PERF(username, YYYY-MM-DD): Bottleneck ở đây — xem CHANGELOG#PERF-001
```

### 3.2 Naming conventions

| Loại            | Convention         | Ví dụ                        |
|-----------------|--------------------|------------------------------|
| Variable/Param  | camelCase          | `userId`, `fetchOptions`     |
| Function        | camelCase verb     | `getUser`, `handleSubmit`    |
| Class/Interface | PascalCase         | `UserService`, `FetchOptions`|
| Constant        | UPPER_SNAKE_CASE   | `MAX_RETRY`, `API_BASE_URL`  |
| File (TS/JS)    | kebab-case         | `user-service.ts`            |
| CSS class       | kebab-case / BEM   | `btn--primary`               |
| Test file       | `*.test.ts`        | `user-service.test.ts`       |

### 3.3 Code style tóm tắt

```typescript
// Max line length: 100 chars
// Indent: 2 spaces (không dùng tab)
// Semicolons: bắt buộc (TypeScript)
// Single quote cho strings
// Trailing comma trong multi-line objects/arrays
// Arrow function cho callbacks
// async/await — không dùng .then().catch() thuần (trừ khi chain phức tạp)
```

---

## 4. SESSION MEMORY & CONTEXT LINKING

> **Mục đích:** Giúp Claude duy trì ngữ cảnh giữa các phiên làm việc mà không cần đọc lại toàn bộ code.

### 4.1 Active Context — Việc đang làm

```yaml
# Cập nhật phần này MỖI KHI kết thúc session làm việc
active_context:
  current_task: >
    DONE (Task K) — Fix 2 regression phát sinh từ Task J, cả 2 do user báo qua debugger/test thật trong
    cùng session: (1) `NullReferenceException` tại `frmScaleNewUI.cs:888` (`GlobalVariables.
    InvokeIfRequired(Owner, ...)` — `Owner` null vì form không show kèm owner; sửa `Owner`→`this`, khớp
    36/37 call site còn lại trong file); (2) `DataEvent_EventHandleValueChange` "chỉ nhảy vào 1 lần rồi
    im lặng dù simulator vẫn gửi liên tục" — chẩn đoán: dispatch `_dataEvent.QRCodeValue = ...` (chạy
    đồng bộ toàn bộ handler chain app-level) trước đây nằm lồng trong khối `try` lỗi mạng/IO của
    `DriverTelnet.ReadData()`, nên exception app-level (QR test không khớp DB, v.v.) bị hiểu nhầm thành
    lỗi Telnet và kích hoạt `Reconnect()` phá 1 kết nối đang khoẻ. Đã tách dispatch + vòng lặp
    opportunistic drain thêm dòng vào 2 khối try/catch riêng, chỉ log `Debug.WriteLine`, không escalate
    `Reconnect()` nữa. **Lưu ý trung thực:** fix #2 là chẩn đoán tốt nhất dựa trên đọc code kỹ (đã loại
    trừ `DataEvent.QRCodeValue` value-equality guard và `_scannerIsBussy[1]`), KHÔNG có log debug thật
    của đúng lần lỗi để xác nhận 100% — xem CHANGELOG entry mới nhất + `next_step` để biết cách verify.
    Build verify: `MSBuild WeightChecking.sln` → 0 lỗi cho `WeightChecking`(`SSFG.exe`)/
    `CognexLibrary_NETFramework`/`AnserU2_cSharp`/`WindowsFormsApp1`/`HardwareSimulator`/
    `CognexScannerTester`; chỉ còn CS0579 pre-existing của `CognexLibrary.csproj`.

    **CẬP NHẬT — root cause THẬT đã xác nhận (không còn là suy đoán):** user tự verify bằng
    `CognexScannerTester` (kết nối trực tiếp `DriverTelnet` production, bỏ qua toàn bộ `frmScaleNewUI.cs`)
    — gửi lặp lại nhiều telegram từ `HardwareSimulator`, log RX hiện ĐÚNG mỗi lần gửi, tách đúng 2 dòng
    mỗi telegram → chứng minh `DriverTelnet.cs`/thư viện Cognex hoàn toàn không phải nguyên nhân (fix ở
    trên vẫn giữ vì đúng kiến trúc, nhưng KHÔNG phải root cause của triệu chứng "chỉ nhảy 1 lần"). Đọc
    lại `frmScaleNewUI.cs` xác nhận nguyên nhân thật: `DataEvent_EventHandleValueChange` (dòng ~871) VẪN
    được gọi mỗi lần (dòng `Debug.WriteLine` đầu hàm luôn chạy), nhưng toàn bộ xử lý thật nằm trong `if
    (!_scannerIsBussy[1])` (dòng ~905) — cờ này CHỈ được reset `false` trong `S6_ValueChanged` (dòng
    ~634-649), tức tín hiệu tag PLC "sensor sau trạm cân" (S6). Bộ test của user (`HardwareSimulator` +
    `CognexScannerTester`) chỉ giả lập Cognex scanner, KHÔNG có PLC/sensor simulator, nên S6 không bao
    giờ bắn → sau lần scan đầu, `_scannerIsBussy[1]` đứng yên `true` mãi → mọi lần sau bị chặn ngay ở
    guard, giống hệt "event không nhảy vào nữa" dù thực ra event vẫn fire bình thường.

    **Đây LÀ hành vi có chủ đích, KHÔNG PHẢI bug** — đúng RULE-RT-05 đã ghi ở mục 1 (`_scannerIsBussy[]`
    không xoá trừ khi sensor out báo hiệu) và trade-off đã ghi nhận từ Task F ("nếu tag này không bắn...
    guard sẽ đứng yên true mãi... đây là trade-off có chủ đích"). Đã hỏi user qua AskUserQuestion 3
    hướng xử lý (thêm PLC/sensor simulator / thêm nút reset guard trong IsTest mode / không sửa gì) —
    **user chọn "không cần sửa gì — chỉ cần xác nhận nguyên nhân".** Không có thay đổi code nào cho phần
    này. Nếu cần test lại `_scannerIsBussy[1]`/`DataEvent_EventHandleValueChange` trong tương lai mà
    không có PLC thật, cần 1 cách giả lập tín hiệu S6 (PLC/sensor simulator hoặc nút reset debug-only) —
    xem lại 2 phương án đã đề xuất nếu user đổi ý sau này.

    ---
    (Task J — DONE) — Fix `DataEvent_EventHandleValueChange` (trạm Scale) để hoạt động đúng khi Cognex
    trả về 2 QR (Main QR + box-info QR2) gộp trong CÙNG 1 lần trigger vật lý (`\r\n` giữa 2 mã, xác
    nhận qua Cognex DataMan Result History và PuTTY capture thật của user). Yêu cầu gốc: "chỉnh lại
    code cho sự kiện scanner cognex2 để nó hoạt động đúng với trường hợp cognex trả về 2 QR code".

    **Root cause xác nhận được (không phải suy đoán — đọc lại toàn bộ `DriverTelnet.ReadData()` và
    `TryParseBoxInfoQr()`):** `DriverTelnet.ReadData()` chỉ làm ĐÚNG 1 lần `await
    reader.ReadLineAsync()` mỗi tick timer 100ms, và biến `isReading` giữ `true` suốt toàn bộ thời
    gian xử lý ĐỒNG BỘ phía sau (`_dataEvent.QRCodeValue = response` gọi thẳng
    `EventHandleValueChange`, không qua queue). Vì `DataEvent_EventHandleValueChange` gọi
    `BarcodeScanner2Handle` đồng bộ (không async — đúng theo RULE-RT của project, không được sửa),
    và `BarcodeScanner2Handle` làm việc đồng bộ nặng (query DB, đợi cân ổn định, in tem), dòng QR2 thứ
    2 trong cùng telegram gộp KHÔNG THỂ được đọc khỏi socket cho tới khi TOÀN BỘ pipeline xử lý Main
    QR (dòng đầu, theo đúng thứ tự thật trên hardware — xác nhận qua PuTTY capture: Main QR luôn đến
    TRƯỚC QR2) đã chạy xong — nghĩa là khối override box-weight trong `BarcodeScanner2Handle` (đọc
    `_boxTypeQR`/`_boxWeightQR`) KHÔNG BAO GIỜ có cơ hội áp dụng đúng thùng: field được set quá trễ
    (sau khi đã cân/in xong) rồi bị reset về null/0 ở khối `finally` trước khi thùng kế tiếp tới.

    **Fix 2 lớp (không dùng `Thread.Sleep`/delay cố định ở app layer — vi phạm RULE-RT-04 và không
    deterministic; toàn bộ độ phức tạp async nằm trong `DriverTelnet.cs`, nơi ĐÃ dùng async/await sẵn
    theo đúng ràng buộc "không async/await cho handler scanner" của project):**
    1. **`CognexLibrary_NETFramework/DriverTelnet.cs` (`ReadData()`)** — sau khi đọc dòng đầu tiên
       (blocking, hành vi cũ không đổi), thử đọc thêm tối đa 4 dòng nữa (`MaxLinesPerTelegram=5`),
       mỗi dòng chờ tối đa `MultiCodeGraceMs=50ms` bằng `Task.WhenAny(reader.ReadLineAsync(),
       Task.Delay(50))`. Nếu hết grace window mà dòng tiếp theo chưa về, Task đọc dở đó được giữ lại
       trong field mới `_pendingLine` (KHÔNG bỏ — `StreamReader` không an toàn khi đọc chồng lấn) để
       tick `ReadData` KẾ TIẾP tái sử dụng làm dòng đầu tiên, thay vì gọi `ReadLineAsync()` chồng lên
       reader đang có 1 read dở dang. Tất cả dòng gom được nối bằng `"\r\n"`, set 1 LẦN DUY NHẤT vào
       `_dataEvent.QRCodeValue` — gộp cả telegram thành 1 event thay vì rời rạc theo từng tick.
       `_pendingLine` được clear trong `DisconnectDevices()` để tránh tái sử dụng read cũ sau khi
       reconnect (reader mới, stale task sẽ không hợp lệ).
    2. **`WeightChecking/frmScaleNewUI.cs`** — thêm helper `SplitTelegramLines()` (split theo
       `"\r\n"`/`"\r"`/`"\n"`). Viết lại `DataEvent_EventHandleValueChange` (Scale): duyệt TẤT CẢ dòng
       trong telegram gộp, bắt hết các dòng box-info QR2 vào `_boxTypeQR`/`_boxWeightQR` TRƯỚC, chỉ
       dispatch dòng Main QR (dòng không khớp `TryParseBoxInfoQr`) tới `BarcodeScanner2Handle` SAU
       CÙNG khi vòng lặp kết thúc — đảm bảo box-info luôn sẵn sàng trước khi pipeline Main QR chạy,
       BẤT KỂ camera gửi QR nào trước (không phụ thuộc thứ tự vật lý, khác với trước đây chỉ xử lý
       được đúng nếu QR2 đến trước Main QR — mà thực tế hardware lại làm ngược lại). Áp dụng cùng
       pattern cho `DataEventMetal_EventHandleValueChange` (Metal) — trạm Metal vẫn bỏ qua hoàn toàn
       mọi dòng box-info QR2 (giữ đúng hành vi cũ), chỉ khác là giờ tách đúng theo dòng thay vì so
       khớp `TryParseBoxInfoQr` trên cả chuỗi gộp (trước đây nếu Metal cũng nhận telegram gộp nhiều
       dòng, cách kiểm tra cũ trên toàn chuỗi sẽ luôn fail và dòng Main QR thật cũng bị nuốt theo).

    Nếu > `MaxLinesPerTelegram` dòng lọt vào 1 telegram, hoặc có > 1 dòng không khớp box-info sau khi
    đã bắt được 1 dòng Main QR, các dòng "Main QR" thừa bị bỏ qua có log cảnh báo (Debug.WriteLine) —
    không dispatch nhiều lần `BarcodeScanner2Handle`/`BarcodeScanner1Handle` cho cùng 1 sự kiện.

    Không đụng: `TryParseBoxInfoQr()` (giữ nguyên, chỉ nhận 1 dòng — đúng contract cũ), khối early-
    return QR2 trong `BarcodeScanner2Handle` (giữ nguyên — defense-in-depth + cần cho test harness gọi
    trực tiếp theo CLAUDE.md §6.2), `CognexScannerTester`/`HardwareSimulator` (đã handle đúng multi-
    line raw value từ trước, không cần sửa gì thêm cho fix lần này).

    Build verify: `MSBuild WeightChecking.sln /p:Configuration=Release /p:Platform="Any CPU"` → 0 lỗi
    cho `WeightChecking`(`SSFG.exe`)/`AnserU2_cSharp`/`WindowsFormsApp1`/`HardwareSimulator`/
    `CognexScannerTester` (dự án cuối cùng phụ thuộc trực tiếp `CognexLibrary_NETFramework` nên xác
    nhận gián tiếp `DriverTelnet.cs` build sạch); chỉ còn lỗi CS0579 pre-existing của
    `CognexLibrary.csproj` (không liên quan, đã ghi nhận từ Task H). Chưa test được với hardware thật
    (không có mạng tới camera Cognex trong sandbox) — xem `next_step`.

    ---
    (Task I — DONE) — Thêm QR thứ 2 "box info" trên mỗi thùng (Carton: "BoxType,Supplier,BoxWeight" vd
    "BX1,DKP,987.3Gr"; Plastic: "Name,BoxWeight" vd "G250001,1320") để dùng boxWeight/boxType đọc trực
    tiếp từ tem vật lý thay cho giá trị suy ra từ master data (`sp_vProductItemInfoGet`). Chỉ áp dụng ở
    trạm Scale — trạm Metal phải bỏ qua QR này hoàn toàn. Quyết định chốt qua AskUserQuestion (session
    trước): detect QR2 bằng 2 ký tự đầu KHÔNG khớp `GlobalVariables.OcUsingList` (Main QR luôn khớp) +
    2-3 field phân cách dấu phẩy, không có `|`; weight luôn là gram, bỏ hậu tố chữ; khi boxType lệch
    giữa QR2 và master-data thì chỉ log cảnh báo, QR2 luôn thắng vô điều kiện.

    Implementation (`frmScaleNewUI.cs`): helper `TryParseBoxInfoQr()` (parse + detect, dùng
    `Enum.TryParse<EnumBoxType>` cho Carton, `Regex` lấy phần số đầu chuỗi cho weight); field mới
    `_boxTypeQR` (nullable `EnumBoxType?`) cạnh field có sẵn nhưng trước đây chết `_boxWeightQR`;
    `DataEventMetal_EventHandleValueChange` bỏ qua QR2 trước khi chạm bất kỳ guard nào (giống pattern
    cũ); `BarcodeScanner2Handle` bắt QR2 làm việc đầu tiên, lưu vào field rồi return sớm; sau khối xác
    định boxWeight/boxType từ master data (cả nhánh Carton lẫn Plastic), có 1 khối override: nếu có
    `_boxTypeQR` thì ghi đè `_boxType`/`_scanDataWeight.BoxWeight`, log cảnh báo nếu lệch với giá trị
    master-data.

    Review bằng `hardware-io-reviewer` + `weight-logic-auditor` (chạy song song sau khi build pass lần
    đầu) phát hiện **2 bug thật cần fix ngay** (không phải nitpick) trước khi coi task xong:
    1. [weight-logic-auditor, HIGH] `_boxTypeQR`/`_boxWeightQR` chỉ được reset ở nhánh thành công —
       nếu thùng A có QR2 nhưng xử lý Main QR của A ném exception (SP không tìm thấy sản phẩm, vượt
       quá số lượng BX1, v.v.) trước khi tới được khối override, field vẫn giữ data của thùng A và sẽ
       ÂM THẦM lây sang thùng B kế tiếp (không có QR2 riêng) — thùng B bị tính sai NetWeight/Deviation/
       pass-fail bằng data của thùng A hoàn toàn không liên quan. **Fix:** reset 2 field này vô điều
       kiện trong khối `finally` của `BarcodeScanner2Handle` (không chỉ ở nhánh thành công) — đã sửa,
       đồng thời bỏ đoạn reset trùng lặp cũ trong khối override vì `finally` đã bao phủ mọi trường hợp.
    2. [hardware-io-reviewer, HIGH] `DataEvent_EventHandleValueChange` (Scale sensor handler) set
       `_readQrStatus[1] = true` VÔ ĐIỀU KIỆN trước khi gọi `BarcodeScanner2Handle` — nếu QR2 (box
       info) được quét xong nhưng Main QR thật của thùng đó KHÔNG BAO GIỜ scan được (tem hỏng/lệch),
       watchdog `CheckReadQrWeight()` vẫn thấy `_readQrStatus[1]==true` (do QR2 đã set) nên KHÔNG BAO
       GIỜ ghi reject (`WeightPusher = 2`) — thùng lọt qua trạm Scale mà không hề được cân/kiểm tra,
       không bị loại. **Fix:** thêm check `TryParseBoxInfoQr` ngay đầu `DataEvent_EventHandleValueChange`
       (trước khi set `_scannerIsBussy[1]`/`_readQrStatus[1]`), return sớm nếu là QR2 — đúng pattern đã
       dùng ở trạm Metal.

    Fix phụ (LOW, tiện thể sửa cùng lúc, không mở rộng phạm vi): nhánh Plastic của khối xác định
    box-weight/box-type từ master data trước đây KHÔNG BAO GIỜ set `_boxType` (chỉ set label UI text
    "Plastic") — field persistent này giữ giá trị rác từ thùng trước, gây log cảnh báo lệch giả (false
    positive) và UI hiển thị sai box type cho thùng Plastic không có QR2 riêng. Đã thêm
    `_boxType = EnumBoxType.Plastic;` vào đúng nhánh đó. Bọc `try/catch` quanh `InvokeIfRequired` trong
    khối bắt QR2 sớm ở `BarcodeScanner2Handle` (nằm ngoài `try/catch` chính của hàm) để tránh exception
    lúc form đang đóng làm Cognex driver reconnect giả.

    **Chưa xử lý (cần xác nhận nghiệp vụ, xem open_questions):** nhánh tolerance (`lowerToleranceOfBox`/
    `upperToleranceOfBox`) được chọn theo Carton-hay-Plastic TỪ TRƯỚC khối override, không re-sync theo
    `_boxTypeQR` — nếu QR2 chỉ ra category khác hẳn (Carton↔Plastic, không chỉ khác BX1-4 trong cùng
    Carton) so với nhánh master-data đã chọn, dải tolerance áp dụng sẽ không khớp category thật đang
    dùng. Chưa rõ tình huống này có xảy ra thật trong sản xuất không (một OC/product có thể đổi giữa
    2 category qua QR2 không, hay chỉ đổi trong nội bộ BX1-4).

    Build verify: `MSBuild WeightChecking.csproj /p:Configuration=Release /p:Platform=AnyCPU` → 0 lỗi,
    4 lần build liên tiếp trong lúc sửa (implement ban đầu → sau khi bỏ đoạn `else` mồ côi không liên
    quan chặn build → sau khi fix 2 bug HIGH → sau khi bọc try/catch) đều pass, sinh `bin\Release\
    SSFG.exe` thành công. Chưa test chạy thật với hardware/GUI (không có trong sandbox) — xem
    `next_step`.

    ---
    (Task H — DONE) — Xây dựng công cụ giả lập phần cứng ("làm phần mềm giả lập scanner cognex và mays
    in để test debug"): project WinForms mới `WeightChecking/HardwareSimulator/` đóng vai "phía
    server" cho 2 camera Cognex (Metal + Scale, Telnet/TCP) và máy in AnserU2 (TCP), để dev/tester
    chạy được app chính (`SSFG.exe`) mà không cần hardware thật.

    Kiến trúc đã chốt qua 3 câu hỏi AskUserQuestion (đều chọn phương án Recommended): (1) gộp chung 1
    app 3 tab (Metal Scanner / Scale Scanner / Printer) thay vì 3 exe riêng; (2) 2 trạm scanner dùng
    chung cổng 23 mặc định (không cấu hình được, hardcode trong `DriverTelnet`) → phân biệt bằng
    loopback IP khác nhau (`127.0.0.2` cho Metal, `127.0.0.3` cho Scale) thay vì sửa code production
    để port configurable; (3) printer simulator có cả nút phản hồi thủ công (ACK/Print Success/Fail)
    lẫn tuỳ chọn "Auto ACK" tự động phản hồi sau N ms để test lặp nhanh.

    Ràng buộc cứng đã tuân thủ tuyệt đối: KHÔNG sửa bất kỳ file production nào (`frmScaleNewUI.cs`,
    `CognexLibrary_NETFramework/DriverTelnet.cs`, `WeightChecking/Class/AnserU2TcpDriver.cs`,
    `ConfigJsonModel`/`tblConfig.cs`) — toàn bộ tính năng nằm hoàn toàn trong project mới, thuần
    additive.

    Implementation: `ScannerSimulatorControl` (dùng chung cho 2 tab Metal/Scale, tham số hoá qua
    constructor `(title, defaultIp, defaultPort)`) — `TcpListener` gửi dòng ASCII kết thúc `\r\n`
    khớp `StreamReader.ReadLineAsync()` phía `DriverTelnet` thật, có ô nhập barcode tự do (không
    hardcode preset vì định dạng QR phụ thuộc DB, xem `BarcodeScanner1Handle`'s `Split('|')`/
    `Split(',')` — không giả lập chính xác được). `PrinterSimulatorControl` — `TcpListener` nhị phân,
    parser buffer STX(`0x02`)...ETX(`0x03`) (copy logic từ `AnserU2TcpDriver.ReceiveLoopAsync`), decode
    lệnh `0x46` (Start template #2 / Stop, phân biệt bằng payload byte `[5]==2` hay `[9]==0x4C`) và
    `0xCA` (SetDynamicString — length-prefix tại `[7]/[8]/[9]`, payload ASCII từ offset 12: grossWeight/
    createdDate/idLabel, đúng layout xác nhận trong `frmScaleNewUI.cs SendDynamicString()`), có 3 nút
    phản hồi thủ công (ACK `0x4F`, Print Success `0x30`, Fail `0x31`+mã lỗi nhập tay) + checkbox
    "Auto ACK" tự gửi ACK ngay rồi Print Success sau N ms (`NumericUpDown`, default 800ms).
    `MainForm` — `TabControl` 3 tab, mỗi tab host 1 instance UserControl (`Dock=Fill`).

    Verify: build riêng `HardwareSimulator.csproj` (Release|AnyCPU) → 0 lỗi, sinh
    `HardwareSimulator.exe`. Build lại toàn bộ `WeightChecking.sln` sau khi thêm project entry → tất cả
    project build được (`WeightChecking`→`SSFG.exe`, `AnserU2_cSharp`, `WindowsFormsApp1`,
    `HardwareSimulator` đều pass); duy nhất `CognexLibrary.csproj` lỗi CS0579 (duplicate
    AssemblyAttribute) — xác nhận đây là lỗi CÓ SẴN, KHÔNG liên quan tới thay đổi lần này: project này
    là SDK-style (`<Project Sdk="Microsoft.NET.Sdk">`, tự động `GenerateAssemblyInfo`) nhưng lại có
    thêm 1 file `Properties/AssemblyInfo.cs` viết tay → xung đột attribute, độc lập hoàn toàn với việc
    thêm `HardwareSimulator` vào `.sln`. Chưa test chạy thật end-to-end với `SSFG.exe` (không có màn
    hình/GUI interactive trong sandbox để trỏ `frmSettings` sang IP simulator và quan sát kết nối) —
    cần verify thủ công theo đúng bước "Verification" trong plan đã duyệt (xem `next_step`).

    ---
    (Task G — DONE — Fix crash khi chạy trên máy PRD: dialog lỗi "Could not load file or assembly
    'Sharp7, Version=1.1.84.0, Culture=neutral, PublicKeyToken=null' or one of its dependencies. The
    system cannot find the file specified." (user báo kèm screenshot custom title bar error dialog,
    chụp lúc app đang chạy UI trạm Scale bình thường — Gross Weight/Std Weight/Deviation đã hiển thị
    dữ liệu, tức app đã load qua khỏi Login/FrmScale_Load, lỗi xảy ra muộn hơn, khả năng cao là lúc
    đụng tới code đường Snap7/PLC S7 lần đầu).

    Root cause #1 (chính, đúng như tên assembly trong thông báo lỗi): `WeightChecking.csproj` reference
    `Snap7Scada.Lib` (wrapper PLC S7, HintPath trỏ ra ngoài repo tới máy dev:
    `..\..\..\..\..\..\..\..\3.Others\0.Code\202600120_Snap7\Snap7ScadaSolution\Release_Lib\
    Snap7Scada.Lib.dll`) — thư viện này phụ thuộc `Sharp7.dll` (thư viện Snap7 thuần C#, không phải
    native Snap7ClientLib COM). `packages.config` CÓ khai báo `Sharp7 version 1.1.84` (file
    `packages\Sharp7.1.1.84\lib\net40\Sharp7.dll` tồn tại sẵn trên đĩa, nuget restore đã tải về), NHƯNG
    `WeightChecking.csproj` KHÔNG có `<Reference Include="Sharp7"...>` — chỉ có Reference cho
    `Snap7Scada.Lib`, không có cho dependency `Sharp7` của nó. MSBuild chỉ copy các assembly có
    `<Reference>` trực tiếp trong .csproj vào output; vì Sharp7 không được reference trực tiếp
    (chỉ là transitive dependency của 1 DLL tham chiếu qua HintPath thô, không phải NuGet
    PackageReference nên MSBuild không tự resolve transitive deps), `bin\Release\` chỉ có
    `Snap7Scada.Lib.dll` mà KHÔNG có `Sharp7.dll` — xác nhận bằng cách `find` trực tiếp trong
    `bin\Release` trước khi sửa. Trên máy dev có thể chạy được (Sharp7.dll tình cờ có sẵn ở đâu đó —
    có thể do lần build S7Client test app khác trước đó để lại 1 bản trong cùng thư mục, hoặc do
    load từ probing path khác), nhưng máy PRD (deploy sạch chỉ copy đúng `bin\Release`) thiếu hẳn
    file này → CLR không tìm thấy assembly khi code chạm tới bất kỳ type nào từ `Snap7Scada.Lib`.

    Fix #1: Thêm `<Reference Include="Sharp7, Version=1.1.84.0, Culture=neutral,
    processorArchitecture=MSIL"><HintPath>..\packages\Sharp7.1.1.84\lib\net40\Sharp7.dll</HintPath>
    </Reference>` vào `WeightChecking.csproj`, ngay trước Reference `Snap7Scada.Lib` (đặt cạnh nhau
    vì có quan hệ phụ thuộc) — để MSBuild copy `Sharp7.dll` vào `bin\Release\` khi build. Xác nhận sau
    khi build lại: `Sharp7.dll`/`Sharp7.pdb` đã xuất hiện trong `bin\Release\`.

    Root cause #2 (phát hiện phụ khi build lại để verify fix #1, chặn đứng build hoàn toàn, không
    liên quan gì tới Sharp7/Snap7): `Properties/Settings.Designer.cs` bị THIẾU gần như toàn bộ
    property — chỉ còn đúng 1 property `conString` — trong khi `Properties/Settings.settings` (đang
    modified/uncommitted trong working tree, không phải do session này) khai báo đầy đủ 17 setting
    (`ipConveyor`, `conStringWL`, `UnitScale`, `ComPortScale`, `UpdatePath`, `IsCounter`, `Station`,
    `AfterPrinting`, `PrintComPort`, `ScannerIdMetal`, `ScannerIdWeight`, `ScannerIdPrint`,
    `TimeCheckQrMetal`, `TimeCheckQrScale`, `IsTest`, `conStringTest`, `IpCognexCam_2`, `IsScale`,
    `conString`) — tức file `.Designer.cs` (code-gen tự động từ `.settings` bởi Visual Studio
    SettingsSingleFileGenerator) đã KHÔNG được regenerate đúng sau khi `.settings` bị sửa (rất có thể
    do sửa `.settings` bằng tay/qua tool khác thay vì qua Settings Designer UI trong Visual Studio,
    nên custom tool không tự chạy lại). Gây lỗi biên dịch `CS1061: 'Settings' does not contain a
    definition for 'IpCognexCam_2'` tại `Program.cs:61`
    (`GlobalVariables.CognexCam_2Status = Properties.Settings.Default.IpCognexCam_2;`) — đây là
    property `Properties.Settings` DUY NHẤT (ngoài `conString`) còn thực sự được code đọc tới (grep
    toàn solution xác nhận 16 setting còn lại trong `.settings` không có chỗ nào gọi qua
    `Properties.Settings.Default.X` — phần lớn config thực tế giờ đọc qua `GlobalVariables.ConfigJson`
    (DB-backed `tblConfig`), không qua `Properties.Settings` nữa).

    Fix #2 (tối thiểu, đúng bám sát pattern code-gen sẵn có của `conString`, KHÔNG regenerate toàn bộ
    17 property vì 16 property kia không được code dùng tới — thêm thừa sẽ chỉ là nhiễu, ngoài phạm vi
    fix bug lần này): thêm lại đúng 1 property `IpCognexCam_2` (string, User-scoped,
    `DefaultSettingValueAttribute("192.168.80.4")` khớp giá trị default trong `.settings`) vào
    `Settings.Designer.cs`, theo đúng khuôn `[UserScopedSettingAttribute()]
    [DebuggerNonUserCodeAttribute()] [DefaultSettingValueAttribute(...)]` mà `conString` đang dùng.

    Build verify: `MSBuild WeightChecking.csproj /p:Configuration=Release /p:Platform=AnyCPU` → exit
    code 0, 0 lỗi CS/MSB, `bin\Release\Sharp7.dll` xuất hiện. Chưa test chạy thật trên máy PRD (không
    có máy PRD/PLC thật trong sandbox) — cần verify thủ công: copy `bin\Release\` mới (có kèm
    `Sharp7.dll`) lên máy PRD, chạy lại, xác nhận dialog lỗi "Could not load file or assembly Sharp7"
    không còn xuất hiện và các chức năng liên quan Snap7 (đọc/ghi tag S7, cân, sensor, pusher) hoạt
    động bình thường.)

    ---
    (Task F — DONE — Fix bug CheckReadQrWeight() bị "nhảy vào sự kiện reject liên tục" tại trạm Scale, theo yêu
    cầu user (kèm screenshot debugger tại `frmScaleNewUI.cs` dòng `GlobalVariables.MyEvent.WeightPusher
    = 2;`, breakpoint bị hit lặp lại nhiều lần trong vài giây): "review lại đoạn code trên và tìm hiểu
    tại sao nó lại nhảy vào sự kiện này liên tục, tìm nguyên nhân và sửa nó."

    Root cause: `EventHandleSensorBeforeWeightScan` (trạm Scale) start một `_ckQrWeightScanTask` MỚI
    mỗi khi sensor "before weight scan" chuyển sang giá trị 1 — KHÔNG có guard chống chạy chồng lấn,
    khác với trạm Metal (`EventHandleSensorBeforeMetalScan`) vốn có sẵn guard `_isStartCountTimer` để
    chỉ cho phép 1 `_ckQRTask` chạy tại 1 thời điểm cho 1 thùng. Nếu sensor "before weight scan" bật/tắt
    nhiều lần liên tiếp cho cùng 1 thùng (rung/nhiễu tín hiệu vật lý, hoặc 2 thùng đi sát nhau), nhiều
    task `CheckReadQrWeight()` chạy song song, mỗi task độc lập đếm ngược `TimerCheckQrScale` giây rồi
    độc lập kiểm tra `_readQrStatus[1]` — nếu vẫn `false` thì MỖI task tự ghi `WeightPusher = 2` + tự
    insert 1 dòng `tblScanDataReject` + tự update UI, khiến dòng `WeightPusher = 2` bị hit lặp lại
    nhiều lần trong vài giây cho cùng 1 sự kiện thực tế. Bằng chứng phụ: có sẵn 1 block `else` bị
    comment-out (`_ckQrWeightScanTask.Wait()/.Dispose()`) — dấu vết một nỗ lực xử lý overlap trước đó
    bị bỏ dở, và dòng tự reset `_readQrStatus[1] = false` cuối `CheckReadQrWeight()` cũng bị comment
    (khác với `CheckReadQr()` của trạm Metal luôn tự reset `_readQrStatus[0]` không điều kiện).

    Fix (bám sát pattern có sẵn của trạm Metal, không đụng bất kỳ tag literal PLC nào):
    1. Thêm field `_isStartCountTimerWeight` (mirror `_isStartCountTimer`).
    2. `EventHandleSensorBeforeWeightScan`: chỉ start `_ckQrWeightScanTask` mới khi
       `_isStartCountTimerWeight == false`; set `true` ngay khi start (chặn spawn chồng lấn).
    3. `EventHandleSensorAfterWeightScan`: reset `_isStartCountTimerWeight = false` khi thùng qua cảm
       biến sau cân (`o.NewValue == 1`) — mirror việc `_isStartCountTimer` được reset trong
       `EventHandleSensorMiddleMetal` của trạm Metal.
    4. Khôi phục (bỏ comment) dòng tự reset `_readQrStatus[1] = false;` ở cuối `CheckReadQrWeight()` —
       đồng bộ với `CheckReadQr()`, phòng vệ thêm 1 lớp (defense-in-depth) trong trường hợp cảm biến
       "after weight scan" bị delay hoặc không bắn kịp.
    Không đụng `_scannerIsBussy[1]` (giữ nguyên theo RULE-RT-05: guard này chỉ được reset bởi hardware
    signal) và không đụng bất kỳ string literal tag nào ("RJ1", "Check_Weight_Result", ...).

    Build verify: `MSYS_NO_PATHCONV=1 MSBuild WeightChecking/WeightChecking/WeightChecking.csproj
    /p:Configuration=Release /p:Platform=AnyCPU /t:Build /nologo /v:minimal` → build thành công, tạo
    ra `bin\Release\SSFG.exe`, 0 lỗi CS (chỉ còn warning nullable/unused-field có sẵn từ trước, không
    liên quan). Lỗi MSB3030 (`tags1.json` not found) của session trước KHÔNG còn xuất hiện trong lần
    build này (có thể user đã tự xử lý ngoài phiên làm việc — xem `open_questions`, chưa xác nhận).
    Chưa test chạy thật với PLC/sensor (không có kết nối hardware trong sandbox) — cần verify thủ công
    trên máy có kết nối PLC thật: cho thùng đi qua trạm cân, xác nhận `WeightPusher` chỉ ghi giá trị
    reject (2) ĐÚNG 1 LẦN mỗi thùng khi thật sự không đọc được QR trong thời gian `TimerCheckQrScale`
    (không còn bị ghi lặp lại nhiều lần liên tiếp), và các thùng scan thành công vẫn hoạt động bình
    thường (không bị guard mới chặn nhầm).)

    ---
    (Task E — Dịch toàn bộ message runtime tiếng Việt sang tiếng Anh trên TOÀN BỘ solution (14
    project), theo yêu cầu user: "Dùng chính xác các tag trong file tags.json, tuyệt đối không
    được thay đổi. Nhiệm vụ tiếp theo là chuyển tất cả các đoạn message trong project thành tiếng
    Anh". Scope đã chốt qua AskUserQuestion trước đó: (a) file scope = toàn bộ solution; (b) content
    type = CHỈ message runtime (Debug.WriteLine, XtraMessageBox/MessageBox.Show, exception message,
    Serilog Log.Error/Information/Warning, Console.WriteLine, UI .Text/.Title gán trong code-behind,
    field Reason/Note/Message ghi DB) — KHÔNG đụng `*.Designer.cs`; (c) comment = GIỮ NGUYÊN tiếng
    Việt, chỉ dịch message/log, không dịch `//`, `/* */`, `///`. Ràng buộc tối quan trọng: mọi string
    literal là tên tag PLC khớp với tags.json (`"RJ1"`, `"S1"`, `"S2"`, `"Check_Weight_Result"`,
    `"Metal_Result"`, `"Scale_Value"`, `"Scale_Value_Stable"`, `"Scale_Stable_Trigger"`, `"S5"`,
    `"S6"`, `"Delay_Time_To_Print"`, `"S_MD_OUT"`, ...) TUYỆT ĐỐI KHÔNG bị sửa — chỉ dịch text người
    đọc xung quanh, không đụng chính tên tag. Không có tag literal nào bị sửa trong toàn bộ quá trình.

    File đã dịch (session này + session liền trước, gộp thành 1 lần hoàn thành):
    `frmScaleNewUI.cs`, `StaticClass/GlobalVariables.cs` (AppStatus), `Views/frmMasterData.cs`,
    `Views/frmMain.cs`, `Views/frmConfirmPrint.cs`, `Program.cs`, `Login.cs`, `Views/frmReports.cs`,
    `StaticClass/AutoPostingHelper.cs`, `AnserU2_DK/AnserU2_cSharp/frmTcpTest.cs`,
    `AnserU2_DK/AnserU2_cSharp/Form1.cs`, `DLL modbus/ModbusRTUMaster/ModbusRTUMaster/Form1.cs`.

    Phương pháp verify: sau mỗi file, chạy Grep kết hợp prefix pattern message
    (`MessageBox.Show|XtraMessageBox.Show|Debug.WriteLine|Console.WriteLine|Log.Error|
    Log.Information|Log.Warning|.Text =|.Title =|Reason =|Note =|Message =|throw new Exception`)
    VỚI class ký tự dấu tiếng Việt để bắt hết chỗ còn sót. Sau khi xong hết các file đã biết, chạy
    thêm 2 lượt sweep toàn solution: (1) lượt dấu tiếng Việt như trên; (2) lượt KHÔNG dấu (một số
    string trong code cũ gõ tiếng Việt không dấu — vd "thanh cong", "that bai", "khong the", "vui
    long", "canh bao", "mat ket", "gui lenh", "sai dinh dang", "qua thoi gian" — không lọt qua được
    lượt (1) vì không có ký tự dấu). Lượt sweep này phát hiện thêm 2 file trước đó chưa nằm trong
    catalog ban đầu: `AnserU2_DK/AnserU2_cSharp/Form1.cs` (bản test app WinForms cũ hơn
    frmTcpTest.cs — "in thanh cong" → "Print successful", "Gui lenh xuong may in thanh cong" →
    "Command sent to printer successfully", "Loi. Error Code" → "Error. Error Code") và
    `DLL modbus/ModbusRTUMaster/ModbusRTUMaster/Form1.cs` ("doc nhiet do thanh cong" → "read
    temperature successfully").

    Xác nhận ĐÚNG ra ngoài phạm vi (không sửa): mọi `*.Designer.cs`/`.Designer.vb`; comment `//`
    (kể cả không dấu); `Views/frmScale.cs` — file legacy/dead code, gần như toàn bộ nội dung đã bị
    comment-out (bản cũ của form scanner, tiền thân của frmScaleNewUI.cs), mọi tiếng Việt tìm thấy
    ở đây đều nằm trong comment nên không đụng; `S7Client/Form1.cs` dòng 159 — false positive do
    biến tên `DemLoi` (bộ đếm) chứa chuỗi con "loi", không phải message.

    Build verify: `MSYS_NO_PATHCONV=1 MSBuild WeightChecking/WeightChecking/WeightChecking.csproj
    /p:Configuration=Release /p:Platform=AnyCPU /t:Build /nologo /v:minimal` → toàn bộ code C#
    compile sạch (0 lỗi CS, chỉ còn warning CS8618/CS8625 có sẵn từ trước không liên quan). Có
    đúng 1 lỗi MSB3030 ("Could not copy tags1.json — not found") — KHÔNG liên quan đến việc dịch:
    `tags1.json` bị XOÁ khỏi disk ngoài phiên làm việc của Claude (không nằm trong bất kỳ edit nào
    của session này/trước, xác nhận qua `git status` → xuất hiện dạng `D` deleted, không phải file
    Claude từng đọc/sửa), trong khi `.csproj` (đã commit từ trước, không phải diff của session này)
    vẫn còn `<None Include="tags1.json">`. Đây là vấn đề tồn tại sẵn trong working tree/config
    project, không thuộc phạm vi "dịch message" — không tự sửa (không biết ý định thật của user với
    file này, có thể là file cấu hình cũ user đang dọn dẹp; ngoài ra còn 1 file mới
    `tagsPRD.json` xuất hiện untracked, có thể liên quan). Cần hỏi user.

    (Các task trước đó — tối ưu Get Data WL bằng SqlBulkCopy, fix NRE frmMasterData, chuyển RJ1
    sang ghi qua event MetalPusher — đã DONE ở các session trước 2026-07-07, xem CHANGELOG bên dưới
    để biết chi tiết, không lặp lại ở đây.)

  related_files:
    - "WeightChecking/WeightChecking/frmScaleNewUI.cs"             # Task K: DataEvent_EventHandleValueChange (nhánh box-info QR, ~dòng 886) — InvokeIfRequired(Owner,...) → InvokeIfRequired(this,...), fix NullReferenceException
    - "WeightChecking/CognexLibrary_NETFramework/DriverTelnet.cs"   # Task K: ReadData() — dispatch _dataEvent.QRCodeValue và vòng lặp opportunistic drain thêm dòng mỗi cái có try/catch riêng, không escalate Reconnect() khi lỗi là app-level (fix "event chỉ nhảy vào 1 lần rồi im lặng")
    - "WeightChecking/CognexLibrary_NETFramework/DriverTelnet.cs"   # Task J: ReadData() gộp nhiều dòng CRLF trong 1 telegram thành 1 QRCodeValue event (grace window 50ms/dòng, tối đa 5 dòng), field _pendingLine giữ read dở dang giữa các tick, clear trong DisconnectDevices()
    - "WeightChecking/WeightChecking/frmScaleNewUI.cs"             # Task J: helper SplitTelegramLines() mới; DataEvent_EventHandleValueChange (Scale) + DataEventMetal_EventHandleValueChange (Metal) viết lại để duyệt hết các dòng trong telegram gộp, bắt QR2 box-info trước khi dispatch Main QR — không phụ thuộc thứ tự dòng camera gửi
    - "WeightChecking/WeightChecking/frmScaleNewUI.cs"             # Task I: TryParseBoxInfoQr() helper (~dòng 1016), field _boxTypeQR (~141), BarcodeScanner2Handle bắt QR2 + khối override (~1685, ~2031-2049), reset _boxTypeQR/_boxWeightQR trong finally (~2497)
    - "WeightChecking/HardwareSimulator/HardwareSimulator.csproj"  # Task H: project mới, .NET Framework 4.7.2, plain WinForms (không DevExpress)
    - "WeightChecking/HardwareSimulator/ScannerSimulatorControl.cs" # Task H: TCP line server giả lập Cognex Telnet (dùng chung cho tab Metal + Scale)
    - "WeightChecking/HardwareSimulator/PrinterSimulatorControl.cs" # Task H: TCP STX/ETX frame server giả lập máy in AnserU2, có Auto ACK
    - "WeightChecking/HardwareSimulator/MainForm.cs"                # Task H: TabControl 3 tab host 2x ScannerSimulatorControl + 1x PrinterSimulatorControl
    - "WeightChecking/WeightChecking.sln"                           # Task H: thêm entry project HardwareSimulator + ProjectConfigurationPlatforms
    - "WeightChecking/WeightChecking/WeightChecking.csproj"        # Fix chính Task G: thêm <Reference Include="Sharp7"> trỏ tới packages\Sharp7.1.1.84\lib\net40\Sharp7.dll (ngay trước Reference Snap7Scada.Lib)
    - "WeightChecking/WeightChecking/Properties/Settings.Designer.cs" # (Task G, phụ) khôi phục property IpCognexCam_2 (thiếu so với Settings.settings, chặn build) — 18 setting khác vẫn để thiếu vì không có chỗ nào code dùng
    - "WeightChecking/WeightChecking/frmScaleNewUI.cs"             # Fix chính Task F: thêm guard _isStartCountTimerWeight (dòng ~49, ~423-446, ~458), khôi phục self-reset _readQrStatus[1] cuối CheckReadQrWeight() (dòng ~2982)
    - "AnserU2_DK/AnserU2_cSharp/Form1.cs"                         # (Task E) Console.WriteLine tiếng Việt không dấu → tiếng Anh (Print successful / Command sent to printer successfully / Error. Error Code)
    - "DLL modbus/ModbusRTUMaster/ModbusRTUMaster/Form1.cs"        # (Task E) Console.WriteLine "doc nhiet do thanh cong" → "read temperature successfully"
    - "WeightChecking/WeightChecking/Views/frmConfirmPrint.cs"     # (Task E) 5 MessageBox.Show dịch sang tiếng Anh
    - "WeightChecking/WeightChecking/Program.cs"                   # (Task E) MessageBox update version + SplashScreenManager caption dịch sang tiếng Anh
    - "WeightChecking/WeightChecking/Login.cs"                     # (Task E) XtraMessageBox "Missing information..." dịch sang tiếng Anh
    - "WeightChecking/WeightChecking/Views/frmReports.cs"          # (Task E) XtraMessageBox + Log.Error + SplashScreenManager caption dịch sang tiếng Anh
    - "WeightChecking/WeightChecking/StaticClass/AutoPostingHelper.cs" # (Task E) Debug.WriteLine + return string trong AutoTransfer/AutoStockIn/AutoStockOut dịch sang tiếng Anh

  blocked_by: >
    KHÔNG blocked. Task J (fix DataEvent_EventHandleValueChange cho trường hợp Cognex trả 2 QR gộp
    trong 1 lần trigger — batching ở DriverTelnet + reorder ở frmScaleNewUI.cs) đã build verify thành
    công qua full-solution build (0 lỗi cho WeightChecking/AnserU2_cSharp/WindowsFormsApp1/
    HardwareSimulator/CognexScannerTester; chỉ còn lỗi CS0579 pre-existing của CognexLibrary.csproj,
    không liên quan). Chưa test được với hardware Cognex thật (sandbox không có mạng tới camera) — xem
    next_step, dùng CognexScannerTester + HardwareSimulator (2 tool đã build sẵn từ Task H) để verify.
    Task I (2 QR/thùng — QR2 box info tại trạm Scale) đã build verify thành công qua 3
    lần rebuild liên tiếp (0 lỗi CS mỗi lần) sau khi áp dụng cả implementation gốc lẫn 4 fix từ 2
    subagent review (`hardware-io-reviewer` + `weight-logic-auditor`, chạy song song theo đúng yêu cầu
    Verification bước 2 của plan). Việc còn lại duy nhất là test thật với `IsTest=true` + fake data
    (không có GUI tương tác trong sandbox) — xem next_step. Còn 1 open question (MEDIUM, tolerance
    category không tự sync theo QR2) cần user xác nhận trước khi quyết định có sửa thêm không.
    Task H (project HardwareSimulator giả lập Cognex scanner + máy in AnserU2) đã build
    verify thành công: build riêng `HardwareSimulator.csproj` → 0 lỗi; build full solution → thành
    công cho WeightChecking/AnserU2_cSharp/WindowsFormsApp1/HardwareSimulator (chỉ `CognexLibrary.csproj`
    lỗi CS0579, xác nhận là vấn đề pre-existing/không liên quan — xem CHANGELOG). Việc còn lại duy nhất
    là chạy thử thật (không có GUI/hardware tương tác trong sandbox này) — xem next_step.
    Task G (fix crash "Could not load file or assembly 'Sharp7...'" trên máy PRD) đã
    build verify thành công: MSBuild WeightChecking.csproj → exit 0, 0 lỗi, `bin\Release\Sharp7.dll`
    + `Sharp7.pdb` đã xuất hiện (trước fix thì không có). Việc còn lại duy nhất là XÁC NHẬN TRÊN MÁY
    PRD THẬT (không thể test trong sandbox này vì không có máy PRD/PLC thật) — user cần deploy lại
    toàn bộ `bin\Release\` (không chỉ SSFG.exe — phải kèm Sharp7.dll mới) rồi chạy thử — xem next_step.
    Task F (fix `_isStartCountTimerWeight`) vẫn ở trạng thái tương tự — build verify xong, chờ xác
    nhận hardware thật, chưa có phản hồi từ user.

  next_step: >
    - [Task K] "Event chỉ nhảy 1 lần" ĐÃ xác nhận root cause thật (guard `_scannerIsBussy[1]` chỉ reset
    qua tag PLC S6, bộ test hiện tại không có PLC simulator) và user xác nhận KHÔNG cần sửa gì — xem
    CHANGELOG + phần "CẬP NHẬT — root cause THẬT" trong `current_task` ở trên. Nếu sau này cần test lại
    mà không có PLC thật, cân nhắc lại 2 phương án đã đề xuất (PLC/sensor simulator, hoặc nút reset guard
    debug-only trong IsTest mode) — user đã từ chối cả 2 lần hỏi này, chỉ làm nếu user chủ động yêu cầu
    lại.
    - [Task K] Xác nhận không còn văng NullReferenceException tại nhánh box-info QR của
    `DataEvent_EventHandleValueChange` khi debug qua Visual Studio với breakpoint (fix đã build verify,
    chỉ cần chạy lại kịch bản debug cũ của user — telegram gộp 2 QR qua `127.0.0.3:23`).
    - [Task J] Dùng `HardwareSimulator.exe` (tab Scale) + `CognexScannerTester.exe` (tab Scale) — 2
    tool đã build sẵn từ Task H: gửi 1 telegram gộp thật (Main QR rồi `<0x0D><0x0A>` rồi box-info QR2,
    đúng thứ tự thật đã xác nhận qua PuTTY capture của user) từ Simulator sang Tester → xác nhận log
    Tester chỉ hiện ĐÚNG 1 dòng `RX:` chứa cả 2 mã (chứng minh Layer 1 — batching trong
    `DriverTelnet.ReadData()` — hoạt động đúng, không bị tách thành 2 event rời theo tick 100ms).
    - [Task J] Test riêng với app `SSFG.exe` thật (`IsTest=true`, theo CLAUDE.md §6.2) hoặc trên
    hardware thật tại trạm Scale: xác nhận box-weight override (`_boxTypeQR`/`_boxWeightQR` từ QR2) áp
    dụng ĐÚNG cho thùng đã sinh ra QR2 đó — thử cả 2 thứ tự dòng trong telegram gộp (Main QR trước QR2,
    và ngược lại) để xác nhận kết quả giống nhau bất kể thứ tự (đây là mục tiêu chính của Layer 2).
    - [Task J] Verify không có regression so với Task I: lặp lại các test case đã liệt kê ở mục [Task
    I] bên dưới (mismatch, watchdog reject khi thiếu Main QR, state không rò rỉ giữa các thùng) — vì
    `BarcodeScanner2Handle`/`TryParseBoxInfoQr`/guard `_scannerIsBussy` không đổi, các test này dự kiến
    vẫn pass y hệt, nhưng cần xác nhận lại vì đường dẫn dữ liệu vào (`DataEvent_EventHandleValueChange`)
    đã đổi.
    - [Task J] Verify trạm Metal không bị ảnh hưởng: gửi 1 telegram gộp giả (Main QR + 1 dòng box-info
    QR2, dù Metal không dùng QR2) vào `DataEventMetal_EventHandleValueChange` → xác nhận chỉ dòng Main
    QR được dispatch tới `BarcodeScanner1Handle`, dòng QR2 bị bỏ qua có log, không ảnh hưởng luồng Metal
    hiện tại.
    - [Task I] Test bằng fake data (`IsTest=true`, theo CLAUDE.md §6.2) trong `FrmScale_Load`: gọi
    `BarcodeScanner2Handle(2, "BX1,DKP,987.3Gr")` một mình → xác nhận không exception, `_boxTypeQR`/
    `_boxWeightQR` được set, UI không đổi gì khác ngoài `labQrScale.Text`; sau đó gọi
    `BarcodeScanner2Handle(2, "<Main QR thật>")` → xác nhận `_scanDataWeight.BoxWeight == 987.3` (không
    phải giá trị từ master data) và `_labBoxType.Text == "BX1"`.
    - [Task I] Test case mismatch: QR2 nói BX1 nhưng quantity/master-data sẽ tính ra BX2 → xác nhận có
    dòng Debug.WriteLine cảnh báo mismatch, và BX1/987.3 (từ QR2) vẫn thắng.
    - [Task I] Test tại trạm Metal: gọi `BarcodeScanner1Handle(1, "BX1,DKP,987.3Gr")` → xác nhận early
    return, không có dòng `tblScanDataReject`, không ghi `MetalPusher`, không đổi UI.
    - [Task I] **Quan trọng — verify riêng fix HIGH vừa thêm cho guard bypass:** giả lập trường hợp QR2
    đến trước rồi Main QR KHÔNG BAO GIỜ đến (thùng dán tem QR2 nhưng tem Main QR bị mờ/hỏng) → xác nhận
    `CheckReadQrWeight()` watchdog vẫn bắn `WeightPusher = 2` (reject) đúng sau `TimerCheckQrScale`
    giây, KHÔNG bị QR2 làm im lặng. Đây là bug HIGH vừa được `hardware-io-reviewer` phát hiện và sửa
    trong session này (xem CHANGELOG) — cần verify kỹ trên hardware thật vì sandbox không test được.
    - [Task I] Verify state không bị rò rỉ giữa các thùng: cho 1 thùng có QR2 đi qua thành công, rồi
    ngay sau đó cho 1 thùng KHÔNG có QR2 đi qua → xác nhận thùng thứ 2 dùng đúng box weight/type từ
    master data (không bị dính giá trị QR2 của thùng trước, nhờ fix reset trong `finally`).
    - [Task H] Chạy `HardwareSimulator.exe`, bấm Listen ở cả 3 tab (Metal `127.0.0.2:23`, Scale
    `127.0.0.3:23`, Printer `127.0.0.1:4001` — hoặc IP/port khác tuỳ chọn trên UI).
    - [Task H] Trỏ `IpCognexCamMetal`/`IpCognexCamScale`/`IpPrinter`/`PortPrinter` trong `frmSettings`
    của app WeightChecking thật (chạy với `IsTest=true`) vào các IP/port simulator đang listen, khởi
    động lại app, xác nhận app tự kết nối vào cả 3 listener (log "client connected" xuất hiện).
    - [Task H] Dán 1 chuỗi QR thật (lấy từ DB/log — simulator không có preset barcode sẵn) vào tab
    Metal → Send → xác nhận app nhận và chạy qua `BarcodeScanner1Handle`; tương tự tab Scale → Send →
    `BarcodeScanner2Handle`.
    - [Task H] Khi app gọi `StartPrint()`/`SendDynamicString()`, xác nhận tab Printer log đúng frame
    đã parse (StartPrint/StopPrint/SetDynamicString với grossWeight/createdDate/idLabel đọc đúng); bấm
    "Send Print Success" (hoặc bật Auto ACK) → xác nhận app cập nhật UI/DB pass giống in thật; bấm
    "Send Fail" → xác nhận app xử lý đúng nhánh lỗi in.
    - [Task H] Cân nhắc fix riêng lỗi CS0579 pre-existing ở `CognexLibrary.csproj` (SDK-style project
    tự sinh AssemblyInfo nhưng vẫn có `Properties/AssemblyInfo.cs` viết tay — trùng attribute) nếu cần
    build full solution sạch hoàn toàn — hiện không chặn Task H vì `HardwareSimulator` không phụ thuộc
    `CognexLibrary`.
    - [Task G] Deploy lại `bin\Release\` (đã có `Sharp7.dll`) lên máy PRD, chạy lại app, xác nhận
    dialog lỗi "Could not load file or assembly 'Sharp7...'" không còn xuất hiện nữa.
    - [Task G] Sau khi hết lỗi load assembly, xác nhận các chức năng đi qua Snap7Scada.Lib (đọc/ghi
    tag S7: Sccale_Value, Sccale_Value_Stable, Scale_Stable_Trigger, S_IN, S_OUT, Check_Weight_Result,
    RJ1, Delay_Time_To_Print...) hoạt động đúng — bản thân Sharp7 chỉ là dependency runtime bị thiếu,
    không phải logic PLC bị đổi, nhưng cần xác nhận vì đây là lần đầu Sharp7.dll thực sự được nạp.
    - [Task G] Cân nhắc kiểm tra các project khác trong solution có dùng Snap7Scada.Lib (hoặc thư viện
    NuGet khác) mà cũng thiếu Reference tương tự — bug pattern này (package có trong packages.config/
    đã restore nhưng thiếu <Reference> trong .csproj nên không được copy ra output) có thể lặp lại ở
    project khác chưa kiểm tra.
    - [Task F] Verify trên máy có PLC/sensor thật: quét 1 thùng qua trạm Scale bình thường (đọc QR
    thành công trong thời gian timeout) → xác nhận `CheckReadQrWeight()` KHÔNG bị ảnh hưởng bởi guard
    mới (`_isStartCountTimerWeight`), luồng pass/fail vẫn hoạt động như cũ.
    - [Task F] Verify tình huống lỗi: để 1 thùng KHÔNG đọc được QR (che tem hoặc rút cáp Cognex tạm
    thời) tại trạm Scale, xác nhận `GlobalVariables.MyEvent.WeightPusher = 2;` (ghi PLC reject) chỉ
    được ghi ĐÚNG 1 LẦN cho thùng đó (dùng Debug.WriteLine/tblLog để đếm số lần bắn), thay vì lặp lại
    liên tục như bug cũ. Sau đó xác nhận sensor tag `SensorAfterWeightScan` bắn lại đúng lúc thùng đi
    qua để reset guard cho thùng kế tiếp — nếu tag này không bắn (kẹt băng tải, sensor lỗi) thì guard
    sẽ đứng yên `true` mãi và trạm Scale sẽ ngừng nhận thùng mới cho tới khi guard được reset — đây là
    trade-off có chủ đích (giống hệt cơ chế `_isStartCountTimer` đã dùng ổn định ở trạm Metal), không
    phải bug, nhưng cần biết để chẩn đoán nếu trạm Scale "đứng hình" sau khi fix.
    - [Task E, còn treo] Hỏi user về `tags1.json` bị xoá + `tagsPRD.json` mới xuất hiện (untracked) —
    có phải user đang đổi tên/dọn dẹp file cấu hình tag, và `.csproj` (`<None Include="tags1.json">`)
    có cần cập nhật theo không.
    - [Task E, còn treo] Xác nhận với hardware địa chỉ DB1 thật cho tag "P4" trên PLC Siemens.

  last_session: "2026-07-16"

  open_questions:
    - "[Task I, MEDIUM — từ weight-logic-auditor] `lowerToleranceOfBox`/`upperToleranceOfBox` được
      chọn dựa trên nhánh Carton-vs-Plastic suy ra từ master data, TRƯỚC khi khối override QR2 chạy —
      nếu QR2 chỉ ra loại bao bì KHÁC HẲN nhóm (vd. master data tính ra Carton nhưng QR2 lại là
      Plastic, không chỉ khác BX1↔BX4 trong cùng nhóm Carton), tolerance đang dùng sẽ không được tính
      lại theo nhóm mới từ QR2. Thùng có thể VẬT LÝ đổi hẳn từ đóng thùng Carton sang túi Plastic (hay
      ngược lại) không, hay chỉ đổi cỡ thùng trong cùng 1 nhóm (BX1-4)? Nếu có thể đổi nhóm, cần sửa để
      tolerance selection cũng dựa theo `_boxTypeQR` khi có, thay vì giữ nguyên nhánh master-data đã
      chọn trước đó."
    - "[Task H] Simulator không hardcode preset barcode giả (định dạng QR phụ thuộc OC/product code
      thật lấy từ DB, giả không đúng định dạng sẽ chỉ test được path lỗi) — tester cần tự dán chuỗi QR
      thật lấy từ DB/log cũ vào textbox Send. Có cần bổ sung 1 vài preset mẫu (ví dụ từ tblScanData cũ)
      để tiện test nhanh không?"
    - "[Task H] CognexLibrary.csproj (SDK-style, netstandard2.1) lỗi CS0579 khi build full solution
      (GenerateAssemblyInfo mặc định true + có sẵn Properties/AssemblyInfo.cs viết tay) — pre-existing,
      không liên quan HardwareSimulator. Có cần sửa (thêm <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
      hoặc xoá AssemblyInfo.cs viết tay) để build full solution sạch hoàn toàn không?"
    - "[Task G] Settings.Designer.cs bị thiếu 18/19 property so với Settings.settings (chỉ còn conString) — có phải do lỗi công cụ, hay do ai đó sửa .settings mà không qua VS Designer UI (khiến codegen không chạy lại)? Hiện chỉ khôi phục đúng 1 property (IpCognexCam_2) đang thực sự được code dùng — có cần audit/khôi phục nốt 17 property còn lại (dù hiện không nơi nào tham chiếu) để tránh vỡ tiếp nếu sau này có code mới dùng tới?"
    - "[Task G] Snap7Scada.Lib.dll được reference qua HintPath tuyệt đối trỏ ra ngoài repo (`..\..\..\..\..\..\..\..\3.Others\0.Code\202600120_Snap7\Snap7ScadaSolution\Release_Lib\Snap7Scada.Lib.dll`) — file này có version-control ở đâu đó không, hay chỉ tồn tại cục bộ trên máy dev hiện tại? Nếu build trên máy khác (CI, máy dev khác) không có đúng đường dẫn này, build sẽ lỗi ngay — có nên đưa DLL này vào trong repo (vd. thư mục `libs/`) để build portable hơn, hay giữ nguyên vì đây là cách làm hiện có của project?"
    - "[Task F] MSB3030 (tags1.json not found) không tái hiện ở build lần này — user đã tự xử lý ngoài phiên làm việc, hay môi trường build khác lần trước?"
    - "[Task F] `_ckQrWeightScanTask` (cũng như `_ckQRTask` của trạm metal) chưa được dispose/cancel trong FrmScale_FormClosing — gap có sẵn từ trước, không thuộc phạm vi fix lần này, có cần xử lý không?"
    - "[Task F] `GlobalVariables.MyEvent.WeightPusher` setter không có edge-detection guard (luôn bắn kể cả set trùng giá trị liên tiếp), khác với `SensorBeforeWeightScan`/`PrintPusher` — có chủ đích hay nên thêm guard cho nhất quán?"
    - "tags1.json bị xoá khỏi disk (ngoài phiên Claude) nhưng .csproj vẫn include — xoá reference trong .csproj hay khôi phục file?"
    - "tagsPRD.json (mới, untracked) là file gì — có nên track vào git / dùng thay tags1.json?"
    - "Port TCP của máy in là 4001 — đã dùng từ Hercules screenshot, cần xác nhận với hardware"
    - "Có cần cấu hình IP tĩnh trên máy in không, hay đã có sẵn?"
    - "IP/port thật tại từng nhà máy (fVN/fFT/fKV/fIN/fGE) có khác 192.168.4.70:4001 mặc định không?"
    - "IP thật của camera Cognex trạm Metal (IpCognexCamMetal) là gì?"
    - "Địa chỉ DB1 thật cho tag P4 (metal reject pusher) trên PLC Siemens là gì?"
    - "Ladder PLC S7 mới có cần DelayTimer1-5 (trước ghi qua Modbus PLC Delta) không, hay đã hardcode?"
```

### 4.1b Bản đồ luồng Scanner (quan trọng — đọc kỹ trước khi sửa)

```
CẬP NHẬT 2026-07-07: chỉ còn 2 trạm scan (đã bỏ trạm 3 Distribution — không còn phân loại
FG/Sơn bằng phần mềm). Cả 2 trạm còn lại đều dùng Cognex Telnet (không còn Zebra CoreScanner SDK).

Trạm 1 — Identification (Metal Scanner):
  Hardware: Cognex Telnet (DriverTelnet instance _driverTelnetMetal, IP = ConfigJson.IpCognexCamMetal)
  Event: DataEventMetal_EventHandleValueChange → BarcodeScanner1Handle(1, barcode)
  Guard: _scannerIsBussy[0] — reset bởi EventHandleSensorMiddleMetal

Trạm 2 — Scale (Weight Check):
  Hardware: Cognex DM290-X (Telnet, _driverTelnet, IP = ConfigJson.IpCognexCamScale)
  Event: DataEvent_EventHandleValueChange → BarcodeScanner2Handle(2, barcode)
  Guard: _scannerIsBussy[1] — reset bởi EventHandleSensorAfterWeightScan
  Scale: tag S7 (Sccale_Value/Sccale_Value_Stable/Scale_Stable_Trigger qua _plc1Client/tags.json),
  đợi _stableScale==1 (spin-wait với Thread.Yield). KHÔNG còn Modbus RTU/PLC Delta.

(Trạm 3 — Distribution/Printing Scanner: ĐÃ BỎ. BarcodeScanner3Handle, S_Sorting_FG/
S_Sorting_Print, PrintPusher/tag P3 không còn được phần mềm dùng nữa.)

PLC Write (Siemens S7):
  WriteData2PlcSeimens("RJ1"|"Check_Weight_Result", value) — dùng PlcRuntime.Tags
  Check_Weight_Result = đèn tháp pass/fail
  (P3 không còn ghi từ phần mềm. CẢNH BÁO: tag "P4" hiện KHÔNG có trong tags.json — xem blocked_by ở 4.1)
```

### 4.2 Quyết định đã chốt (Decision Log)

| ID | Ngày | Quyết định | Ai quyết | File liên quan |
|----|------|-----------|----------|---------------|
| DEC-001 | 2025-05-10 | Dùng Cognex DM290-X Telnet thay Zebra USB ở trạm Scale | Dev | `frmScaleNewUI.cs` → `_driverTelnet` |
| DEC-002 | 2025-XX-XX | Snap7ClientLib thay Modbus TCP cho PLC Siemens | Dev | `frmScaleNewUI.cs` → `_plcRuntime` |
| DEC-003 | 2024-08-19 | AutoPostingHelper.CheckIn() trước rồi mới AutoTransfer() | Dev | `StaticClass/AutoPostingHelper.cs` |
| DEC-004 | 2025-XX-XX | Custom titlebar (FormBorderStyle.None + Panel) | Dev | `frmScaleNewUI.cs` constructor |
| DEC-005 | 2026-06-12 | CLAUDE.md cập nhật từ template TypeScript → thực tế C# WinForms | NDCongSP | `CLAUDE.md` |
| DEC-006 | 2026-07-07 | Bỏ trạm scan Distribution (station 3), không phân loại FG/Sơn bằng phần mềm nữa | User | `frmScaleNewUI.cs` |
| DEC-007 | 2026-07-07 | Trạm Metal chuyển từ Zebra CoreScanner SDK sang Cognex Telnet (cùng driver với trạm Scale) | User | `frmScaleNewUI.cs` → `_driverTelnetMetal` |
| DEC-008 | 2026-07-07 | Scale I/O (giá trị cân, stable trigger, sensor, đèn tháp) chuyển từ Modbus RTU (PLC Delta) sang tag S7 qua `_plc1Client`/`tags.json` | User | `frmScaleNewUI.cs` |
<!-- Thêm quyết định mới vào đây -->

### 4.3 Hướng dẫn Claude đọc context

Khi bắt đầu session mới, Claude PHẢI:
1. Đọc `active_context` → biết đang làm gì
2. Đọc `CHANGELOG` gần nhất → biết đã thay đổi gì
3. Đọc `Decision Log` → tránh đề xuất lại phương án đã bác bỏ
4. **Không** hỏi lại những gì đã ghi trong file này

**Prompt mẫu để bắt đầu session:**
```
Đọc CLAUDE.md và tiếp tục từ active_context. 
Task hiện tại: [mô tả]. File cần làm việc: [list file].
```

---

## 5. CHANGELOG — EDIT LOG

> Ghi lại **mọi thay đổi đáng kể** theo thứ tự ngược (mới nhất lên đầu).  
> Format: `[YYYY-MM-DD] [TYPE] [File/Module] — Mô tả`  
> Types: `FEAT` · `FIX` · `REFACTOR` · `PERF` · `TEST` · `DOCS` · `CHORE` · `BREAK`

---

### [YYYY-MM-DD] — Session N

```
[FEAT]     src/services/userService.ts     — Thêm hàm getUser() với cache
[FIX]      src/hooks/useAuth.ts            — Sửa race condition khi logout
[TEST]     tests/unit/userService.test.ts  — Thêm 8 test case cho getUser()
[DOCS]     CLAUDE.md                       — Cập nhật active_context
```

**Chi tiết nếu cần:**
- `getUser()`: Thêm `staleTime: 5 phút`, fallback sang localStorage khi offline
- `useAuth`: Lock bằng `ref` flag, tránh double-call `/refresh`

---

### [YYYY-MM-DD] — Session N-1

```
[CHORE]    package.json    — Upgrade Zod từ 3.21 → 3.23
[REFACTOR] lib/api.ts      — Tách error handler thành hàm riêng handleApiError()
```

### [2026-06-08] — Session: Refactor tập trung config reads

```
[REFACTOR]   Form1.cs   — Chuyển StationName từ local var → field _stationName, khởi tạo 1 lần trong Form1_Load
[REFACTOR]   Form1.cs   — Chuyển AldilaCuttingApi_Url → field _apiUrl (string), khởi tạo 1 lần trong Form1_Load
[REFACTOR]   Form1.cs   — Chuyển AldilaCuttingApi_Enabled → field _apiEnabled (bool), khởi tạo 1 lần trong Form1_Load
```

**Chi tiết:**
- `PostCuttingValidatorAsync` và `RetryQueueAsync` không còn đọc config mỗi lần gọi
- `_apiEnabled` lưu dạng `bool` (parse 1 lần) thay vì so sánh chuỗi mỗi lần
- `LoadQrPatterns()` vẫn đọc lại từ config mỗi khi FormConfig đóng — intentional (QR patterns có thể đổi runtime)

---

### [2026-05-27] — Session: Fix FormConfig Save + Password

```
[FIX]   FormConfig.cs   — _btnSave_Click: thêm lưu tài khoản (username + password nếu được nhập)
[FIX]   FormConfig.cs   — _btnSave_Click: validate password match TRƯỚC khi lưu bất cứ thứ gì
[FIX]   FormConfig.cs   — _btnChangePassword_Click: thêm try-catch, báo lỗi nếu ghi file thất bại
```

**Root cause:**
- `_btnSave_Click` chỉ gọi `SaveQrPatterns()` + `SaveErpSettings()` — không lưu tab Tài khoản
- User nhập password mới → click "Lưu" → báo thành công nhưng password KHÔNG thay đổi
- `_btnChangePassword_Click` thiếu try-catch: nếu ghi file lỗi (quyền, lock...), WinForms nuốt exception, user không biết

**Behavior sau fix:**
- "Lưu" lưu TẤT CẢ: QR patterns + ERP + username + password (nếu được nhập)
- "Cập nhật tài khoản" vẫn hoạt động như cũ (lưu tức thì, không cần nhấn Lưu thêm)
- Nếu password mới không khớp xác nhận → báo lỗi, không đóng form, không lưu gì cả

---

### [2026-05-27] — Session: API Badge UI + Retry Queue

```
[FEAT]   Form1.cs   — Thêm badge Label (_labApiStatus) hiển thị trạng thái API (xanh/đỏ)
[FEAT]   Form1.cs   — Tách TryPostAsync() riêng: trả về bool (true=2xx, false=lỗi)
[FEAT]   Form1.cs   — PostCuttingValidatorAsync: khi thất bại → EnqueueRecord() + badge đỏ
[FEAT]   Form1.cs   — EnqueueRecord(): ghi bản ghi thất bại vào api_retry_queue.txt (tab-delimited)
[FEAT]   Form1.cs   — StartRetryLoopAsync() + RetryQueueAsync(): 30s/lần đọc file, thử lại, xoá khi thành công
[REFACTOR] Form1.cs — PostCuttingValidatorAsync không còn static (cần truy cập _labApiStatus)
```

**Chi tiết:**
- Badge tại X=500, Y=594, W=960, H=100 — nằm bên phải nút Bắt Đầu / Cấu hình
- Màu xanh lá (`#00A000`) = gửi OK; màu đỏ cam (`OrangeRed`) = lỗi + đang chờ gửi lại
- File hàng đợi: `<AppDir>/api_retry_queue.txt`, tab-delimited, 7 cột, mỗi dòng 1 bản ghi
- Retry loop: mỗi 30 giây, không block UI, dừng khi form bị dispose
- Bản ghi được xoá khỏi file ngay khi API trả về 2xx; file bị xoá khi hàng đợi rỗng

---

### [2026-05-27] — Session: Aldila Cutting Validator API

```
[FEAT]   App.config   — Thêm StationName, AldilaCuttingApi_Url, AldilaCuttingApi_Enabled
[FEAT]   Form1.cs     — Thêm PostCuttingValidatorAsync() + EscapeJson() + static HttpClient (SSL bypass)
[FEAT]   Form1.cs     — _btnStartStop_Click: gọi API fire-and-forget sau khi xác định PASSED/FAILED
[DOCS]   CLAUDE.md    — Cập nhật active_context + CHANGELOG
```

**Chi tiết:**
- API endpoint: `https://192.168.96.10/aldila-portlet/service/savePrepregCuttingValidator` (POST JSON)
- Mapping: QR2 (cuộn Prepreg thực tế) → `prepregItemId/Name`; QR1 (phiếu cắt order) → `prepregOrderItemId/Name`
- SSL validation bị bypass vì server dùng self-signed cert — cần xoá khi cert hợp lệ
- API lỗi không làm crash app (try-catch, log ra `Debug.WriteLine`)
- Có thể tắt bằng `AldilaCuttingApi_Enabled = false` trong App.config

### [2026-05-25] — Session: ERP Config UI

```
[FEAT]   App.config            — Thêm 6 key ERP: TenantId, ClientId, ClientSecret, BaseUrl, Endpoint, Enabled
[FEAT]   FormConfig.cs/.Designer.cs — Thêm Tab "ERP Kết nối" với form OAuth2 (ClientSecret ẩn, checkbox show/hide)
[REFACTOR] FormConfig          — Chuyển sang TabControl (Tab QR Patterns + Tab ERP Kết nối)
```

**Chi tiết:**
- ERP dùng OAuth2 Client Credentials flow (Azure AD) — cần TenantId, ClientId, ClientSecret, BaseUrl
- ClientSecret lưu plain text trong App.config — chấp nhận được cho môi trường factory floor
- Nút Save lưu cả 2 tab cùng lúc

### [2026-05-25] — Session: Fix Reconnect + Config QR

```
[FIX]    PLCModbusManager.cs   — Thêm RECONNECT_COOLDOWN_MS=3000ms để tránh block vòng lặp
[FIX]    PLCModbusManager.cs   — Sửa EndConnect sau timeout (không gọi nữa, chỉ Close socket)
[FIX]    PLCModbusManager.cs   — Giảm Retries từ 3→1 và thêm WriteTimeout=1000ms
[FEAT]   App.config            — Thêm CutSheetQR_EndsWith và CutSheetQR_Contains (pipe-separated)
[FEAT]   FormConfig.cs/.Designer.cs — Form quản lý QR validation patterns (thêm/xoá/lưu)
[REFACTOR] Form1.cs            — Thay hardcode pattern bằng IsCutSheetQr() + LoadQrPatterns()
[FEAT]   Form1.cs              — Thêm nút "Config" để mở FormConfig tại runtime
```

**Chi tiết:**
- Reconnect: trước đây mỗi lần disconnect, vòng lặp 200ms gọi 2x EnsureConnection → block 6s. Nay có cooldown 3s nên block tối đa 2s một lần thử.
- QR patterns: `--` (EndsWith) và `"-` (Contains) được lưu trong App.config, user có thể chỉnh qua nút Config mà không cần sửa code.

### [2026-06-12] — Session: Cập nhật CLAUDE.md cho WeightChecking SSFG

```
[DOCS]   CLAUDE.md   — Viết lại toàn bộ Section 1 (Project Overview) với thông tin thực tế
[DOCS]   CLAUDE.md   — Viết lại Section 2 (Architecture Manual): thư mục thực, luồng vật lý, ConfigJson keys, ADR
[DOCS]   CLAUDE.md   — Cập nhật active_context + Decision Log
[DOCS]   CLAUDE.md   — Thêm bản đồ luồng Scanner 3 trạm (section 4.1b)
```

**Chi tiết:**
- Project thực tế là WeightChecking SSFG (không phải Cut_Sheet template cũ)
- Form chính: `frmScaleNewUI.cs` (3852 dòng), kế thừa `DevExpress.XtraEditors.XtraForm`
- Nhận dạng nhà máy tự động từ DB: `sp_GetMesoInfo` → MESOCOMP → fVN/fFT/fKV/fIN/fGE
- 3 trạm scanner với guards `_scannerIsBussy[]` riêng biệt
- Background tasks dùng `CancellationTokenSource` pattern (dispose trong `FrmScale_FormClosing`)
- Auto-posting logic: CheckIn → AutoTransfer (kho 1185/1223/964/965/2/10/4)

### [2026-06-12] — Session: TCP driver cho máy in Anser SmartU2

```
[FEAT]     Class/AnserU2Print.cs            — Viết lại thành AnserU2TcpDriver: TcpClient, auto-reconnect, STX/ETX framing, SemaphoreSlim write-lock
[FEAT]     tblConfig.cs / ConfigJsonModel   — Thêm IpPrinter (default "192.168.4.70") và PortPrinter (default 4001)
[REFACTOR] frmScaleNewUI.cs                 — Thay _serialPort (SerialPort) → _printerDriver (AnserU2TcpDriver)
[REFACTOR] frmScaleNewUI.cs                 — SerialPortOpen/Close → PrinterOpen/Close
[REFACTOR] frmScaleNewUI.cs                 — SerialPort_DataReceived → PrinterDataReceived(byte[] rcvArr)
[FIX]      frmScaleNewUI.cs StartPrint()    — Xoá `goto loop1` anti-pattern; TCP driver tự reconnect
[FIX]      frmScaleNewUI.cs                 — Tất cả _serialPort.Write(arr,0,len) → _printerDriver?.Write(arr)
[CHORE]    frmScaleNewUI.cs                 — using System.IO.Ports → using System.Net.Sockets
```

**Chi tiết:**
- Protocol binary (STX=0x02 … ETX=0x03, checksum) **không thay đổi** — chỉ đổi transport layer
- TCP driver tự parse frame (STX…ETX) → tránh partial-read vốn có của Serial + Thread.Sleep(100)
- Auto-reconnect với delay 5s — thay thế vòng `goto loop1` cũ trong `StartPrint()`
- `_printerDriver?.Write(bytes)` dùng null-conditional → không crash nếu chưa kết nối
- `DataReceived` event truyền `byte[]` thô → loại bỏ round-trip ASCII encode/decode cũ
- **Cần update tblConfig DB**: thêm `"IpPrinter":"192.168.4.70","PortPrinter":4001` vào JSON config

### [2026-06-12] — Session: Form test TCP cho AnserU2_cSharp

```
[FEAT]   AnserU2_DK/AnserU2_cSharp/AnserU2TcpDriver.cs   — Copy driver TCP (namespace AnserU2_cSharp): TcpClient, auto-reconnect, STX/ETX parser, SemaphoreSlim write-lock
[FEAT]   AnserU2_DK/AnserU2_cSharp/frmTcpTest.cs         — Form test TCP mới: Connect/Disconnect, Start/Stop Print, Send 4 strings, Get/Set Speed, Get/Set Delay, log panel
[FEAT]   AnserU2_DK/AnserU2_cSharp/frmTcpTest.Designer.cs — Designer cho frmTcpTest (830×530, ConsoleGreen log area)
[CHORE]  AnserU2_DK/AnserU2_cSharp/AnserU2_cSharp.csproj  — Thêm Compile entries cho AnserU2TcpDriver.cs, frmTcpTest.cs, frmTcpTest.Designer.cs
[DOCS]   CLAUDE.md                                        — Cập nhật active_context + CHANGELOG
```

**Chi tiết:**
- `frmTcpTest` là standalone test form: IP mặc định `192.168.4.70`, Port `4001`
- Connect → tạo `AnserU2TcpDriver`, event `DataReceived` + `ConnectionStatusChanged`
- Log panel (RichTextBox đen/xanh lá, Consolas 8pt) hiển thị mỗi frame nhận dạng hex: `RX [N bytes]: XX XX ...`
- Nút Disconnect: dispose driver, UI reset về disabled state
- Tất cả lệnh protocol giữ nguyên so với Form1.cs gốc (COM), chỉ thay `_serialPort.Write` → `_driver?.Write`
- Buttons Start/Stop Print, Send String, Speed, Delay disabled cho đến khi nhấn Connect

<!-- Thêm session mới lên ĐẦU, trên dòng này -->

---

### [2026-07-07] — Session: Verify migration máy in Anser COM→TCP (không đổi code)

```
[DOCS]   CLAUDE.md   — Đối chiếu next_step cũ với code thực tế: migration COM→TCP đã hoàn thành từ trước (session 2026-06-12), không phải việc đang chờ làm
[DOCS]   CLAUDE.md   — Cập nhật active_context: current_task, related_files, next_step, open_questions
```

**Kết quả kiểm tra:**
- `frmScaleNewUI.cs`: `PrinterOpen()`/`PrinterClose()`/`StartPrint()`/`StopPrint()`/`PrinterDataReceived()` đều dùng `_printerDriver` (kiểu `AnserU2TcpDriver`), không còn `SerialPort` cho máy in. `System.IO.Ports` còn lại trong file chỉ phục vụ Modbus RTU của cân (`ModbusRTUMaster.KetNoi`), không liên quan máy in.
- `Class/AnserU2TcpDriver.cs` tồn tại và đúng nội dung (file `AnserU2Print.cs` cũ đã được đổi tên/thay thế, không còn tồn tại — không phải thiếu file).
- `tblConfig.cs` → `ConfigJsonModel` đã có `IpPrinter` (default `"192.168.4.70"`) và `PortPrinter` (default `4001`) với property initializer, nên **không bắt buộc** phải sửa tay JSON trong DB — bản ghi cũ thiếu 2 key này khi deserialize qua Newtonsoft.Json vẫn nhận giá trị default.
- `frmSettings.cs` dùng `PropertyGridControl` bind thẳng vào `ConfigJsonModel` nên `IpPrinter`/`PortPrinter` tự động xuất hiện trong UI cấu hình, không cần thêm code riêng.
- Thử build full solution (`MSBuild WeightChecking.sln /p:Configuration=Release /p:Platform="Any CPU"`) thất bại với lỗi **MSB3103** trên `frmMain.resx` và `frmSettings.resx` (thiếu `DevExpress.Utils.Svg.SvgImage, DevExpress.Data.v24.2` design-time assembly) — đây là lỗi môi trường build (thiếu cài đặt DevExpress đầy đủ trên máy chạy sandbox này), không liên quan đến thay đổi máy in, không sửa trong session này.

**Việc còn lại (không phải code):** xác nhận IP/port thật của máy in tại từng trạm sản xuất, rồi set qua UI `frmSettings` hoặc DB — xem `open_questions`.

---

### [2026-07-07] — Session: Fix crash EF6 "model backing the context has changed"

```
[FIX]   Models/Entities/IDCScanSystem/ApplicationDbContextSSFG.cs — Thêm static constructor gọi Database.SetInitializer<ApplicationDbContextSSFG>(null)
```

**Root cause:** Chạy debug thật (Visual Studio, có đủ DevExpress) lần đầu trong phiên này thì crash
ngay ở `Program.cs:35` (`dbContext.TblConfigs.FirstOrDefault()`) với
`InvalidOperationException: The model backing the 'ApplicationDbContextSSFG' context has changed
since the database was created`. Không có `Database.SetInitializer` nào được cấu hình cho context
này (kiểm tra cả `Program.cs`, `ApplicationDbContextSSFG.cs`, `App.config` — không có), nên EF6
dùng initializer mặc định (`CreateDatabaseIfNotExists`), initializer này so hash của model hiện tại
với `__MigrationHistory`/`EdmMetadata` trong DB — DB này là DB có sẵn, quản lý ngoài EF (nhiều
stored procedure, không dùng Code First Migrations), nên kiểm tra hash này sai ngữ cảnh và sẽ vỡ
bất cứ khi nào có entity class nào đổi (không nhất thiết do session này) mà chưa từng chạy migration.
**Không liên quan tới các thay đổi trạm Metal/Distribution/Modbus ở trên** — session này không đụng
tới class entity `tblConfig` (chỉ đụng `ConfigJsonModel`, một POCO thường không được EF map).
**Fix:** tắt hẳn initializer cho context này (đúng cho app dùng DB-first).

---

### [2026-07-07] — Session: Bỏ trạm scan Distribution + Metal sang Cognex Telnet + Scale I/O sang S7 tags

```
[BREAK]    frmScaleNewUI.cs                          — Xoá BarcodeScanner3Handle, region "Read scanner using SDK" (InitializeScaner/OnBarcodeEvent/AsciiToString/_cCoreScannerClass) — hết dùng Zebra CoreScanner SDK
[FEAT]     frmScaleNewUI.cs                          — Thêm _driverTelnetMetal (Cognex Telnet) cho trạm Metal, handler DataEventMetal_EventHandleValueChange gọi BarcodeScanner1Handle
[BREAK]    frmScaleNewUI.cs                          — Xoá EventHandlerSensorAfterPrintScanner, S_Sorting_FG/S_Sorting_Print tag wiring, EventHandlerPrintPusher, tag P3 wiring — bỏ hẳn phân loại FG/Sơn bằng phần mềm (quyết định của user, không relocate sang trạm 2)
[BREAK]    frmScaleNewUI.cs                          — Xoá toàn bộ Modbus RTU (region "Ket noi modbus RTU PLC", TaskReadModbusAsync, MyEvent_EventHandleStatusLightPLC, _readHoldingRegisterArr/_writeHoldingRegisterArr)
[FEAT]     frmScaleNewUI.cs                          — Thêm tag ValueChanged cho Sccale_Value/Sccale_Value_Stable/Scale_Stable_Trigger/S_IN/S_OUT/Delay_Time_To_Print qua _plcRuntime.Tags (Snap7); đèn tháp pass/fail ghi qua tag "Check_Weight_Result" (WriteData2PlcSeimens) thay cho Modbus register 4602
[FIX]      CognexLibrary_NETFramework/DriverTelnet.cs — Đổi toàn bộ field từ static sang instance — bug ẩn (mọi instance DriverTelnet share chung 1 TCP connection/host), chỉ lộ ra khi thêm instance thứ 2 cho trạm Metal
[CHORE]    Models/Entities/IDCScanSystem/tblConfig.cs — Thêm IpCognexCamMetal (placeholder default); xoá ScannerIdMetal/ScannerIdPrint/ComPortScale (hết dùng); giữ IsScale (vẫn gate spin-wait chờ cân ổn định)
[DOCS]     CLAUDE.md                                 — Cập nhật active_context, bản đồ luồng scanner (4.1b), Decision Log (DEC-006..008)
```

**Bối cảnh:** `next_step` phiên trước (đã bị sửa trực tiếp trong CLAUDE.md, ngoài phiên làm việc của Claude) yêu cầu 2 việc: (1) bỏ Zebra CoreScanner SDK, chuyển trạm Metal sang Cognex Telnet như trạm Scale, bỏ hẳn trạm scan số 3; (2) thay toàn bộ kết nối Modbus RTU bằng `_plc1Client`/`tags.json`. Đã hỏi user cách xử lý logic phân loại FG/Sơn (trước đây chỉ nằm trong `BarcodeScanner3Handle`) — user chọn **bỏ hẳn**, không relocate sang trạm khác.

**Phát hiện quan trọng khi làm:**
- `CognexLibrary_NETFramework/DriverTelnet.cs` có TOÀN BỘ field là `static` (kể cả `_dataEvent`, `client`, `stream`...). Nếu tạo thêm 1 instance `DriverTelnet` cho trạm Metal mà không sửa, 2 trạm sẽ tranh nhau 1 kết nối TCP/host — đã sửa hết `static` → instance field trước khi wiring trạm Metal.
- `tags.json` (đang uncommitted, không phải do Claude sửa) đã có sẵn đúng các tag cần cho việc migrate scale I/O (`Sccale_Value`, `Sccale_Value_Stable`, `Scale_Stable_Trigger`, `S_IN`, `S_OUT`, `Check_Weight_Result`, `Delay_Time_To_Print`) — chỉ cần wiring vào code, không cần sửa tags.json.
- **BUG CÓ SẴN phát hiện được (không do session này gây ra):** code dùng tag `"P4"` (`_plcRuntime.Tags.FirstOrDefault(t => t.Name == "P4")`) nhưng tag này KHÔNG có trong tags.json (cả bản cũ đã commit lẫn bản mới) → `FirstOrDefault` trả `null` → `.ValueChanged +=` ném `NullReferenceException` ngay khi `FrmScale_Load` chạy. Đây là bug tồn tại từ trước, không phải do thay đổi lần này, nhưng sẽ chặn đứng toàn bộ app (kể cả các thay đổi trong session này) nếu không thêm tag "P4" vào tags.json trước khi chạy thử. Xem `blocked_by`.
- `DelayTimer1-5` (trước ghi xuống PLC Delta qua Modbus holding register 4604-4608) không có tag S7 tương ứng trong `tags.json` hiện tại → đã bỏ ghi các giá trị này thay vì đoán địa chỉ DB1 — cần xác nhận với người phụ trách ladder PLC xem có cần thêm tag hay ladder đã tự xử lý delay.
- Không build được trong sandbox này (thiếu DevExpress design-time assemblies, lỗi MSB3103 — môi trường, đã ghi nhận từ session trước) nên chưa compile-verify; đã tự grep toàn bộ symbol bị xoá (`_cCoreScannerClass`, `ModbusRTUMaster`, `_scanDataPrint`, `PrintPusher`, `TaskReadModbusAsync`, `ScannerIdMetal`, `ScannerIdPrint`, `ComPortScale`, `_printPusher`, `_readHoldingRegisterArr`...) để đảm bảo không còn tham chiếu treo trong `frmScaleNewUI.cs`.
- Không sửa `GlobalVariables.cs` (`MyDriver`/`ModbusStatus` giờ không còn được ghi từ `frmScaleNewUI.cs` nhưng để nguyên vì có thể còn dùng ở form khác) và không sửa `CustomEvents.cs` — kiểm tra thấy `frmMain.cs` (form khác, đang active) vẫn đọc `SensorAfterPrintScannerFG`/`SensorAfterPrintScannerPrinting` nên KHÔNG xoá các property này khỏi `CustomEvents.cs`.

---

### [2026-07-07] — Session: Tối ưu tốc độ nút "Get Data WL" (frmMain.cs)

```
[PERF]   Views/frmMain.cs   — barButtonItemGetDataWL_ItemClick: thay Dapper con.Execute(insertSql, res) (thực thi 1 round-trip riêng cho MỖI dòng trong res) bằng SqlBulkCopy (1 round-trip theo batch 5000 dòng)
[FIX]    Views/frmMain.cs   — Bọc truncate + bulk insert vào tblWinlineProductsInfo trong 1 SqlTransaction (trước đây không transactional — insert lỗi giữa chừng sẽ để bảng trống vĩnh viễn)
[PERF]   Views/frmMain.cs   — Query sp_IdcScanScaleGetCoreData: thêm commandTimeout=120 (tránh timeout 30s mặc định nếu SP phía Winline trả nhiều dòng)
[CHORE]  Views/frmMain.cs   — Thêm using System.Data.SqlClient (cho SqlBulkCopy, SqlConnection)
```

**Root cause:** `next_step` phiên trước ghi "mỗi lần đọc store để lấy data xong rồi update vào bảng tblWinlineProductsInfo chạy rất là lâu". Đọc code thấy `con.Execute($"Insert into tblWinlineProductsInfo (...) values (...)", res)` với `res` là `List<WinlineDataModel>` — hành vi chuẩn của Dapper khi truyền `IEnumerable` làm tham số cho `Execute` là chạy câu lệnh **một lần cho mỗi phần tử** (N round-trip riêng biệt tới SQL Server), không phải 1 lệnh gộp. `tblWinlineProductsInfo` là bảng master data sản phẩm (có thể vài nghìn dòng mỗi lần đồng bộ từ Winline) nên N round-trip tuần tự chính là nguyên nhân chạy chậm.

**Fix:** Tách logic insert ra hàm riêng `BulkInsertWinlineProductsInfo()`, dùng `SqlBulkCopy` (gửi dữ liệu theo batch, 1 lần round-trip cho toàn bộ dữ liệu thay vì N lần) và bọc chung với `truncate table` trong `SqlTransaction` để đảm bảo atomic — nếu bulk insert lỗi giữa chừng, transaction rollback, bảng KHÔNG bị để ở trạng thái trống (bug tiềm ẩn có sẵn trong code cũ, tiện thể sửa luôn vì cùng chỗ).

**Verify:** Build thành công bằng MSBuild VS2022 Professional tìm thấy trong sandbox (`WeightChecking -> ...\bin\Release\SSFG.exe`), không có lỗi compile, chỉ còn warning có sẵn từ trước (không liên quan thay đổi này). Chưa test chạy thật với DB Winline (không có kết nối DB trong sandbox) — cần verify thủ công trên máy có kết nối DB thật: bấm nút "Get Data WL", kiểm tra thời gian chạy nhanh hơn rõ rệt và số dòng insert khớp với số dòng SP trả về.

---

### [2026-07-07] — Session: Fix NullReferenceException khi refresh frmMasterData

```
[FIX]    Views/frmMasterData.cs   — Grv_SelectionChanged: null-check GetRowCellValue(gv.FocusedRowHandle, "ProductNumber") trước khi gọi .ToString() (trước đây gọi thẳng, ném NRE khi grid rỗng)
[FIX]    Views/frmMasterData.cs   — grv_RowClick: null-check cả "ProductNumber" và "CodeItemSize" trước khi gọi .ToString() (cùng pattern lỗi, cùng file)
```

**Root cause:** User báo lỗi kèm screenshot debugger break tại `Grv_SelectionChanged` dòng 37
(`NullReferenceException: DevExpress.XtraGrid.Views.Base.ColumnView.GetRowCellValue(...) returned
null`), xảy ra khi bấm nút refresh (nút "Get Data WL" ở frmMain, hoặc tự động khi mở frmMasterData —
cả 2 đều set `GlobalVariables.MyEvent.RefreshStatus = true`, kích hoạt `MyEvent_RefreshActionevent`
trong `frmMasterData.cs`). Hàm này set `grc.DataSource = null` TRƯỚC khi gán data mới (dòng
150/170) — trong khoảng đó GridView bắn `SelectionChanged` với `FocusedRowHandle` không hợp lệ (grid
rỗng), `GetRowCellValue(...)` trả về `null`, và code gọi thẳng `.ToString()` trên giá trị đó → NRE.
Exception này thực ra đã bị nuốt bởi `catch (Exception ex) { }` rỗng có sẵn nên KHÔNG làm crash app
lúc chạy thật — debugger chỉ break vì cờ "Break when this exception type is thrown" được bật trong
Visual Studio. Đây là bug tồn tại từ trước (pre-existing), không liên quan đến thay đổi SqlBulkCopy
ở phần "Get Data WL" phía trên — chỉ trùng thời điểm vì cùng nằm trên đường refresh.

**Fix:** Gán kết quả `GetRowCellValue(...)` vào biến tạm, kiểm tra `!= null` trước khi gọi
`.ToString()`, dùng `string.Empty` nếu null — áp dụng cho cả `Grv_SelectionChanged` (field
`ProductNumber`, field `CodeItemSize` đã có sẵn null-check từ trước) và `grv_RowClick` (cả 2 field
đều thiếu null-check).

**Verify:** Build lại thành công (cùng lệnh MSBuild ở trên), không có lỗi compile. Chưa test chạy
thật UI (không có màn hình/DB trong sandbox) — cần verify thủ công: mở frmMasterData, bấm refresh
nhiều lần liên tục trong lúc grid đang load lại, xác nhận không còn NRE (dù có debugger attach hay
không) và `_productNumber`/`_codeItemZise` fallback về rỗng thay vì giữ giá trị cũ khi grid trống.

---

### [2026-07-07] — Session: BarcodeScanner1Handle ghi PLC trực tiếp qua RJ1 + dịch message sang English

```
[REFACTOR] frmScaleNewUI.cs   — BarcodeScanner1Handle: thay 7 chỗ gán `_metalScannerStatus = X` bằng gọi thẳng `WriteData2PlcSeimens("RJ1", value)` — ghi PLC Siemens ngay lập tức thay vì set field cục bộ
[REFACTOR] frmScaleNewUI.cs   — Mapping giá trị mới cho tag "RJ1" (do user chỉ định): 1 = hàng yêu cầu kiểm tra kim loại, 2 = hàng không yêu cầu kiểm tra kim loại, 3 = reject
[CHORE]    frmScaleNewUI.cs   — BarcodeScanner1Handle: dịch toàn bộ thông báo tiếng Việt (Debug.WriteLine, label UI _labResultIdentification, field Reason/Note ghi vào tblScanDataReject/tblItemMissingInfo, message ghi tblLog) sang tiếng Anh
```

**Yêu cầu gốc:** User yêu cầu thay đoạn `_metalScannerStatus = 1;` (comment "ghi lệnh reject do ko
quet đc tem") bằng hàm ghi PLC `WriteData2PlcSeimens("RJ1", value)` với mapping 1=yêu cầu kiểm tra
kim loại, 2=không yêu cầu kiểm tra kim loại, 3=reject; đồng thời dịch toàn bộ thông báo tiếng Việt
sang tiếng Anh. Khi hỏi làm rõ phạm vi (chỉ đoạn snippet paste, hay toàn bộ method), **user chọn
"Toàn bộ hàm"** — áp dụng cho mọi chỗ gán `_metalScannerStatus` bên trong `BarcodeScanner1Handle`.

**Thực hiện:** Thay 7 chỗ gán còn sống (dòng ~1030, 1129, 1360, 1467, 1480, 1493, 1557 trước khi
sửa) theo mapping: `_metalScannerStatus = 0` (cần kiểm tra kim loại) → `WriteData2PlcSeimens("RJ1",
1)`; `_metalScannerStatus = 2` (không cần kiểm tra) → giữ nguyên số, đổi thành
`WriteData2PlcSeimens("RJ1", 2)`; `_metalScannerStatus = 1` (mọi trường hợp reject: QR sai định
dạng, thùng đã check OK, thiếu khối lượng, exception ở catch block) → `WriteData2PlcSeimens("RJ1",
3)`. Không đụng: field decl `_metalScannerStatus` (dòng 49) và
`GlobalVariables.MyEvent.MetalPusher = _metalScannerStatus` (dòng 475, trong
`EventHandleSensorMiddleMetal`) vì cả hai vẫn được `CheckReadQr` dùng — method đó ngoài phạm vi yêu
cầu. Cũng không đụng 3 chỗ gán đã bị comment-out sẵn (dòng ~1220, 1243, 1410) và chỗ gán trong
`CheckReadQr` (dòng ~2881).

**Phát hiện phụ (ngoài phạm vi, không sửa):** Grep toàn solution xác nhận
`GlobalVariables.MyEvent.MetalPusher` (cơ chế ghi PLC gián tiếp qua event `EventHandlerMetalPusher`
trong `CustomEvents.cs`, dùng ở dòng 475) hiện KHÔNG có subscriber nào đang hoạt động — dead code,
set property này không có tác dụng gì tới PLC thật. Đây là vấn đề tồn tại từ trước, không phải do
thay đổi trong session này (session này chỉ đổi các chỗ trong `BarcodeScanner1Handle`, không đụng
dòng 475). Ghi nhận lại trong `active_context.blocked_by` để xử lý sau nếu user yêu cầu.

> **⚠️ ĐÍNH CHÍNH (session sau, cùng ngày 2026-07-07, xem entry "BarcodeScanner1Handle chuyển RJ1
> sang ghi qua event MetalPusher" bên dưới):** Nhận định "dead code, không có subscriber" ở trên
> KHÔNG CÒN ĐÚNG — user đã tự thêm 1 subscriber thật cho `EventHandlerMetalPusher` ngay sau đó, và
> toàn bộ 7 chỗ `WriteData2PlcSeimens("RJ1", value)` vừa thêm ở session này đã bị thay lại thành
> `GlobalVariables.MyEvent.MetalPusher = value` trong session kế tiếp.

**Verify:** Build thành công bằng MSBuild VS2022 Professional (`WeightChecking -> ...\bin\Release\
SSFG.exe`), không có lỗi compile, chỉ còn warning có sẵn từ trước không liên quan thay đổi này. Chưa
test chạy thật với PLC/hardware (không có kết nối trong sandbox) — cần verify thủ công trên máy có
kết nối PLC thật: quét QR ở trạm Identification, xác nhận tag "RJ1" (DB1.DBB14) trên PLC nhận đúng
giá trị 1/2/3 theo từng tình huống (cần kiểm tra kim loại / không cần / reject).

---

### [2026-07-07] — Session: BarcodeScanner1Handle chuyển RJ1 sang ghi qua event MetalPusher

```
[REFACTOR] frmScaleNewUI.cs               — BarcodeScanner1Handle: thay 7 chỗ gọi thẳng `WriteData2PlcSeimens("RJ1", value)` (thêm ở session trước) bằng `GlobalVariables.MyEvent.MetalPusher = value;` — dùng lại cơ chế ghi PLC gián tiếp qua event, mapping 1/2/3 giữ nguyên
[CHORE]    frmScaleNewUI.cs               — Xoá 2 dòng comment thừa `//GlobalVariables.MyEvent.MetalPusher = 0;`/`= 2;` nằm cạnh 2 trong 7 chỗ trên (nay trùng với code thật, không còn cần thiết)
[FIX]      frmScaleNewUI.cs               — Dòng ~2999: sửa `$"...{}"` (string interpolation rỗng, code dở dang có sẵn trong working tree, không liên quan RJ1) → xoá đoạn `; Check_Weight_Result: {}` chưa hoàn thiện để build qua được
[FIX]      StaticClass/GlobalVariables.cs — Thêm property `CognexCam_1Status` (thiếu, gây lỗi compile CS0117 ở dòng 917/2996 — cần cho driver Cognex Telnet thứ 2 ở trạm Metal theo DEC-007, có sẵn trong working tree nhưng chưa khai báo property)
```

**Bối cảnh:** User tự thêm (ngoài phiên làm việc của Claude, thấy trong uncommitted diff khi đọc lại
file) một subscriber thật cho `GlobalVariables.MyEvent.EventHandlerMetalPusher` tại
`frmScaleNewUI.cs:649`:
```csharp
GlobalVariables.MyEvent.EventHandlerMetalPusher += (s, o) =>
{
    if (o.NewValue != 0)
    {
        Debug.WriteLine($"Event ghi DB identity {o.NewValue}. status {_plcConnectionState}");
        WriteData2PlcSeimens("RJ1", o.NewValue);
    }
    else
    {
        Debug.WriteLine($"Event ghi DB weight pusher {o.NewValue}. status {_plcConnectionState}");
    }
};
```
Subscriber này khi `MetalPusher` được set khác 0 sẽ tự gọi `WriteData2PlcSeimens("RJ1", o.NewValue)`
— tức cơ chế event giờ ĐÃ HOẠT ĐỘNG (khác với nhận định "dead code" ghi trong CHANGELOG entry ngay
phía trên, entry đó đã được đính chính). User yêu cầu: đổi các chỗ ghi RJ1 trực tiếp trong
`BarcodeScanner1Handle` (vừa thêm ở session trước) sang dùng lại cơ chế event này, tức
`GlobalVariables.MyEvent.MetalPusher = value;` thay vì gọi thẳng `WriteData2PlcSeimens`.

**Thực hiện:** Thay cả 7 chỗ (dòng 1054, 1153, 1384, 1491, 1503, 1515, 1579) từ
`WriteData2PlcSeimens("RJ1", value)` → `GlobalVariables.MyEvent.MetalPusher = value;`, giữ nguyên
mapping 1/2/3 đã chốt ở session trước. Không đụng: dòng 476 (`EventHandleSensorMiddleMetal`, vẫn
gán `GlobalVariables.MyEvent.MetalPusher = _metalScannerStatus` — nằm ngoài `BarcodeScanner1Handle`,
phục vụ `CheckReadQr`), dòng 656 (chính subscriber user vừa thêm — không phải chỗ cần sửa), dòng 764
(tag S7 "RJ1".ValueChanged → set MetalPusher, một cơ chế đọc khác, không nằm trong
`BarcodeScanner1Handle`). Đã kiểm tra: setter `MetalPusher` trong `CustomEvents.cs` có guard
`if (value != _metalPusher)` bị comment-out từ trước → event LUÔN bắn mỗi lần set kể cả set trùng
giá trị liên tiếp, nên không có rủi ro mất 1 lần ghi PLC do bị coi là "không đổi".

**Lỗi biên dịch không liên quan, gặp khi build lại (đã vá tối thiểu để verify được thay đổi chính):**
1. Dòng ~2999 (`TaskTimerAsync`, label trạng thái cuối màn hình): `$"; RJ1:{_rj1}; Check_Weight_Result: {}"`
   — `{}` là interpolation rỗng, lỗi cú pháp CS1733, rõ ràng là đoạn code dở dang có sẵn trong working
   tree (không phải do session này). Đã xoá đoạn `; Check_Weight_Result: {}` chưa hoàn thiện để build
   qua — **cần user xác nhận lại** ý định hiển thị gì ở đây (`_rj1` cũng là field chưa từng được gán
   giá trị ở đâu trong code, chỉ khai báo dòng 131).
2. `GlobalVariables.CognexCam_1Status` được dùng (gán ở dòng 917, đọc ở dòng 2996) nhưng chưa từng
   khai báo property trong `GlobalVariables.cs` — lỗi CS0117. Đã thêm
   `public static string CognexCam_1Status { get; set; }` cạnh `CognexCam_2Status` sẵn có (cùng
   pattern), phục vụ driver Cognex Telnet thứ 2 ở trạm Metal (DEC-007).
Cả 2 lỗi này không liên quan gì đến thay đổi RJ1/MetalPusher của session này — chỉ là code dở dang
có sẵn trong working tree khi Claude đọc lại file, chặn build nên đã vá tối thiểu.

**Verify:** Build thành công bằng MSBuild VS2022 Professional (exit code 0, 0 error, chỉ còn warning
CS8618/CS0414 có sẵn từ trước không liên quan). Chưa test chạy thật với PLC/hardware (không có kết
nối trong sandbox) — cần verify thủ công trên máy có kết nối PLC thật: quét QR ở trạm Identification,
xác nhận tag "RJ1" (DB1.DBB14) trên PLC vẫn nhận đúng giá trị 1/2/3 (giờ đi qua đường event thay vì
ghi thẳng — nên hành vi cuối cùng tới PLC không đổi, nhưng cần xác nhận lại vì có thêm 1 lớp gián
tiếp). Cũng cần xác nhận với user 2 fix tạm ở mục "Lỗi biên dịch không liên quan" phía trên có đúng ý
định hay cần sửa lại khác.

---

### [2026-07-08] — Session: Fix crash "Could not load file or assembly 'Sharp7...'" trên máy PRD

```
[FIX]      WeightChecking.csproj                        — Thêm <Reference Include="Sharp7"> trỏ tới packages\Sharp7.1.1.84\lib\net40\Sharp7.dll, đặt ngay trước Reference Snap7Scada.Lib
[FIX]      Properties/Settings.Designer.cs               — (phụ, không liên quan Sharp7) khôi phục property `IpCognexCam_2` bị thiếu so với Settings.settings — chặn build nên phải vá để verify được fix chính
```

**Root cause:** User chạy app trên máy PRD, gặp dialog unhandled exception ngay sau khi UI trạm Scale
đã load xong data (Gross Weight, Std Weight, Deviation hiển thị bình thường): `Could not load file or
assembly 'Sharp7, Version=1.1.84.0, Culture=neutral, PublicKeyToken=null' or one of its dependencies.
The system cannot find the file specified.` — nghĩa là lỗi xảy ra khi code chạm vào đường PLC S7
(`Snap7Scada.Lib`) lần đầu, không phải lúc load form.

`Snap7Scada.Lib.dll` (thư viện wrapper PLC S7, được reference qua `HintPath` tuyệt đối trỏ RA NGOÀI
repo: `..\..\..\..\..\..\..\..\3.Others\0.Code\202600120_Snap7\Snap7ScadaSolution\Release_Lib\
Snap7Scada.Lib.dll`) phụ thuộc runtime vào `Sharp7.dll` (gói NuGet C# thuần, v1.1.84). Grep toàn
`.csproj` xác nhận: KHÔNG có bất kỳ `<Reference Include="Sharp7"...>` nào, dù `packages.config` đã có
sẵn `<package id="Sharp7" version="1.1.84" targetFramework="net48" />` (tức `nuget restore` đã tải
`packages\Sharp7.1.1.84\lib\net40\Sharp7.dll` xuống đĩa từ trước). Với legacy `.csproj` (không phải
SDK-style), MSBuild CHỈ copy ra `bin\Release` những assembly có `<Reference>` khai báo tường minh —
không tự động resolve dependency bắc cầu của 1 reference trỏ qua `HintPath` thô (khác với
`PackageReference` của SDK-style project, vốn mang theo metadata phụ thuộc để tự resolve). Vì vậy
`Sharp7.dll` tuy có sẵn trên đĩa (đã restore) nhưng chưa từng được copy vào `bin\Release`, nên máy PRD
(chỉ nhận folder `bin\Release`, không có toàn bộ `packages\`) thiếu hẳn file này.

**Fix:** Thêm 1 `<Reference Include="Sharp7"...>` trỏ `HintPath` tới
`..\packages\Sharp7.1.1.84\lib\net40\Sharp7.dll` (chọn biến thể `net40`, khớp target .NET Framework
của project, cùng kiểu HintPath tương đối như các reference NuGet khác trong file — có biến thể
`netstandard2.0` cũng tồn tại nhưng không dùng), đặt ngay trước reference `Snap7Scada.Lib` để dễ đọc
theo cặp phụ thuộc. Không đụng bất kỳ tag literal PLC nào.

**Lỗi phụ gặp khi build lại để verify (không liên quan Sharp7):** `Program.cs(61,77): error CS1061:
'Settings' does not contain a definition for 'IpCognexCam_2'`. Đọc `Settings.Designer.cs` phát hiện
file chỉ còn ĐÚNG 1 property (`conString`), trong khi `Settings.settings` khai báo tới 19 setting —
desync nghiêm trọng có sẵn từ trước (không do session này gây ra; có thể `.settings` từng bị sửa tay/
bằng công cụ ngoài mà không qua VS Designer UI nên codegen không chạy lại). Grep toàn bộ log build
xác nhận `IpCognexCam_2` là setting DUY NHẤT trong 18 setting còn thiếu thực sự được code tham chiếu
(`Program.cs:61` → `GlobalVariables.CognexCam_2Status = Properties.Settings.Default.IpCognexCam_2;`)
— 17 setting còn lại không có chỗ nào dùng (config đã chuyển sang đọc qua `GlobalVariables.ConfigJson`/
DB `tblConfig` theo kiến trúc hiện tại). Đã khôi phục tối giản đúng 1 property `IpCognexCam_2`, theo
đúng pattern codegen của `conString` sẵn có, để không mở rộng phạm vi ngoài việc verify fix Sharp7.

**Verify:** `MSYS_NO_PATHCONV=1 MSBuild WeightChecking/WeightChecking/WeightChecking.csproj
/p:Configuration=Release /p:Platform=AnyCPU /t:Build /nologo /v:minimal` → exit 0, 0 lỗi. Kiểm tra lại
`bin\Release\`: trước fix chỉ có `Snap7Scada.Lib.dll`/`.pdb`, sau fix có thêm `Sharp7.dll`/`Sharp7.pdb`
— xác nhận assembly bị thiếu nay đã được copy vào output. Chưa test được trên máy PRD thật (không có
máy PRD/PLC thật trong sandbox) — cần user deploy lại TOÀN BỘ `bin\Release\` (không chỉ `SSFG.exe`)
lên máy PRD và xác nhận dialog lỗi không còn xuất hiện, các thao tác đọc/ghi tag PLC S7 (cân, sensor,
pusher, đèn tháp) vẫn hoạt động bình thường.

---

### [2026-07-08] — Session: Fix WeightPusher reject bắn liên tục ở trạm Scale (thiếu guard chống spawn trùng CheckReadQrWeight)

```
[FIX]      frmScaleNewUI.cs   — Thêm field guard `_isStartCountTimerWeight` (dòng ~49), cùng pattern với `_isStartCountTimer` đã dùng ổn định ở trạm Metal
[FIX]      frmScaleNewUI.cs   — EventHandleSensorBeforeWeightScan (dòng ~423-446): gate việc spawn `_ckQrWeightScanTask` (chạy CheckReadQrWeight) bằng `if (o.NewValue == 1 && _isStartCountTimerWeight == false)`, set `_isStartCountTimerWeight = true` ngay khi spawn; xoá đoạn code cleanup đã bị comment-out sẵn không còn cần thiết
[FIX]      frmScaleNewUI.cs   — EventHandleSensorAfterWeightScan (dòng ~449-462): thêm `_isStartCountTimerWeight = false;` — mở lại guard khi sensor báo hiệu thùng đã đi qua trạm Scale, cho phép CheckReadQrWeight chạy cho thùng kế tiếp (reset bằng hardware signal, đúng RULE-RT-05, không dùng timer)
[FIX]      frmScaleNewUI.cs   — CheckReadQrWeight() (cuối method): khôi phục self-reset `_readQrStatus[1] = false;` (trước đây bị comment-out), đối xứng với CheckReadQr() của trạm Metal — defense-in-depth, không phải fix chính
```

**Root cause:** User debug thấy `GlobalVariables.MyEvent.WeightPusher = 2;` (nhánh reject của
`CheckReadQrWeight()` — ghi PLC báo "không đọc được QR trong thời gian timeout") bị hit liên tục
nhiều lần cho cùng 1 thùng, thay vì chỉ đúng 1 lần. So sánh với trạm Metal (hoạt động đúng): trạm
Metal có field `_isStartCountTimer` để đảm bảo chỉ 1 `Task` watchdog `CheckReadQr` được spawn tại 1
thời điểm cho 1 thùng — `EventHandleSensorBeforeMetalScan` chỉ start task mới khi
`_isStartCountTimer == false`, và guard này chỉ được mở lại bởi sensor hạ nguồn
(`EventHandleSensorMiddleMetal`). Trạm Scale (`EventHandleSensorBeforeWeightScan`) KHÔNG có guard
tương đương — mỗi lần tag sensor `SensorBeforeWeightScan` bắn giá trị 1 (có thể bắn lại nhiều lần
cho cùng 1 thùng tuỳ hành vi PLC/sensor vật lý, ví dụ rung tín hiệu hoặc polling cycle bắt lại cạnh
lên), code spawn thêm 1 `Task CheckReadQrWeight()` mới chồng lên task cũ đang chạy dở — nhiều task
chạy song song, mỗi task hết timeout độc lập đều ghi `WeightPusher = 2`, gây hiệu ứng "bắn liên tục".
Đã loại trừ giả thuyết "event bắn lại dù giá trị không đổi" — kiểm tra `CustomEvents.cs` xác nhận
setter `SensorBeforeWeightScan` có edge-detection (`if (value != _sensorX)`), tức chỉ bắn khi giá
trị thực sự đổi; vấn đề nằm hoàn toàn ở việc thiếu guard chống spawn-trùng task, không phải ở tần
suất bắn event.

**Fix:** Áp dụng lại đúng pattern `_isStartCountTimer` đã chứng minh hoạt động ổn định ở trạm Metal
sang trạm Scale, đặt tên `_isStartCountTimerWeight` để tách biệt 2 trạm (không tái sử dụng chung 1
field, tránh side-effect chéo giữa Metal và Scale). Guard được set `true` ngay khi spawn task, và chỉ
được set lại `false` bởi tín hiệu sensor hạ nguồn thật (`EventHandleSensorAfterWeightScan`, tag
sensor sau trạm Scale) — không dùng timer để reset, giữ đúng RULE-RT-05. Không đụng tới
`_scannerIsBussy[1]` (guard riêng, khác mục đích — chống double-scan barcode, không phải chống
double-spawn watchdog task) và không đụng logic reset của nó.

**Không thuộc phạm vi fix này (ghi nhận, không sửa):** `_ckQrWeightScanTask` (cũng như
`_ckQRTask` của trạm Metal) chưa được dispose/cancel trong `FrmScale_FormClosing` — gap có sẵn từ
trước ở cả 2 trạm, không phải do fix này gây ra hay làm nặng thêm; `GlobalVariables.MyEvent.
WeightPusher` setter không có edge-detection guard (luôn bắn kể cả set trùng giá trị liên tiếp) —
không liên quan tới root cause đã xác định (bug nằm ở việc spawn nhiều task, không phải property có
bắn lại hay không), không đụng.

**Verify:** Build bằng MSBuild VS2022 Professional
(`/c/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/MSBuild.exe`)
→ 0 lỗi CS, sinh ra `bin\Release\SSFG.exe` thành công, chỉ còn warning có sẵn từ trước không liên
quan. Chưa test được trên PLC/sensor thật (không có kết nối hardware trong sandbox) — cần verify thủ
công: xác nhận thùng đọc QR thành công vẫn pass bình thường (không bị ảnh hưởng bởi guard mới), và
thùng không đọc được QR chỉ ghi `WeightPusher = 2` đúng 1 lần thay vì lặp lại liên tục. Xem
`active_context.next_step` để biết chi tiết bước verify trên hardware.

---

### [2026-07-09] — Session: Thêm QR2 (box info) tại trạm Scale — đọc box weight/type từ label thay vì master data

```
[FEAT]     WeightChecking/WeightChecking/frmScaleNewUI.cs   — Thêm helper TryParseBoxInfoQr() (~dòng 1016): phân biệt QR2 "box info" (Carton "BoxType,Supplier,BoxWeight" hoặc Plastic "Name,BoxWeight") với Main QR bằng prefix 2 ký tự đầu KHÔNG khớp GlobalVariables.OcUsingList + không chứa "|" + 2-3 field comma-split
[FEAT]     WeightChecking/WeightChecking/frmScaleNewUI.cs   — Thêm field `_boxTypeQR` (EnumBoxType?), dùng chung với field có sẵn `_boxWeightQR`
[FEAT]     WeightChecking/WeightChecking/frmScaleNewUI.cs   — DataEventMetal_EventHandleValueChange: bỏ qua hoàn toàn QR2 (early return trước khi chạm guard `_scannerIsBussy[0]`) — trạm Metal không phản ứng gì với QR2
[FEAT]     WeightChecking/WeightChecking/frmScaleNewUI.cs   — BarcodeScanner2Handle: bắt QR2 vào `_boxTypeQR`/`_boxWeightQR` (early return, không đụng `_scanDataWeight`/`_approvePrint`); khối override sau khi xác định box weight/type từ master data — ghi đè bằng giá trị QR2 khi có, log warning nếu mismatch, QR2 luôn thắng
[FIX]      WeightChecking/WeightChecking/frmScaleNewUI.cs   — (HIGH, từ hardware-io-reviewer) DataEvent_EventHandleValueChange: thêm early-return cho QR2 TRƯỚC khi set `_scannerIsBussy[1]`/`_readQrStatus[1]` — trước đây 1 QR2 scan (không kèm Main QR đọc được) tự động thoả mãn watchdog `CheckReadQrWeight()`, khiến thùng không đọc được Main QR thật bị lọt qua reject/timeout an toàn
[FIX]      WeightChecking/WeightChecking/frmScaleNewUI.cs   — (HIGH, từ weight-logic-auditor) `finally` block của BarcodeScanner2Handle: thêm reset vô điều kiện `_boxTypeQR = null; _boxWeightQR = 0;` — trước đây chỉ reset ở nhánh thành công, nên nếu thùng có QR2 bị reject/exception TRƯỚC khi tới khối override, dữ liệu QR2 sẽ rò rỉ sang thùng kế tiếp không có QR2 riêng
[FIX]      WeightChecking/WeightChecking/frmScaleNewUI.cs   — (LOW, từ weight-logic-auditor) Nhánh Plastic của khối xác định box weight: thêm `_boxType = EnumBoxType.Plastic;` (trước đây chỉ set UI label "Plastic", field `_boxType` giữ giá trị cũ từ thùng trước — gây cảnh báo mismatch giả và label cũ hiển thị sai)
[FIX]      WeightChecking/WeightChecking/frmScaleNewUI.cs   — (LOW, từ hardware-io-reviewer) Bọc try/catch quanh `InvokeIfRequired` trong nhánh early-return QR2 của BarcodeScanner2Handle — block này nằm ngoài try/catch chính của method, exception ở đây (vd. lúc đóng form) sẽ leak lên `DriverTelnet.ReadData` và ép Telnet reconnect không cần thiết
[DOCS]     CLAUDE.md                                        — Cập nhật active_context (Task I), thêm entry CHANGELOG này
```

**Bối cảnh:** Tiếp tục plan đã duyệt từ session trước (`concurrent-stirring-fiddle.md`, không sửa gì
so với bản duyệt) — mỗi thùng giờ mang 2 tem QR vật lý: Main QR (như cũ, drive lookup master data) +
QR2 "box info" ghi loại/khối lượng bao bì THẬT đang dùng. Yêu cầu: khối lượng bao bì đọc từ QR2 phải
ghi đè giá trị suy ra từ master data khi có, chỉ tại trạm Scale — trạm Metal bỏ qua hoàn toàn QR2.

**Quy trình thực hiện:** Code chính (helper + field + 2 điểm tích hợp Metal/Scale) đã viết ở session
trước. Session này: (1) xác nhận build thành công; (2) chạy song song 2 subagent theo đúng yêu cầu
Verification bước 2 của plan — `hardware-io-reviewer` (điểm chạm PLC/scanner event handler) và
`weight-logic-auditor` (BoxWeight ảnh hưởng trực tiếp pass/fail); (3) sửa tất cả finding HIGH/LOW có
hướng sửa rõ ràng (4 fix liệt kê trên); (4) build lại 3 lần liên tiếp xác nhận không lỗi và các fix
không xung đột nhau.

**Không tự sửa (để lại open_questions, cần user xác nhận):** finding MEDIUM của weight-logic-auditor
— `lowerToleranceOfBox`/`upperToleranceOfBox` được chọn theo nhánh Carton/Plastic suy ra từ master
data TRƯỚC khi override QR2 chạy, không tự đồng bộ lại nếu QR2 chỉ ra nhóm bao bì khác hẳn (không chỉ
khác cỡ BX1-4 mà đổi hẳn Carton↔Plastic) — cần xác nhận tình huống này có khả thi vật lý không trước
khi quyết định sửa. Cũng không sửa finding cosmetic của hardware-io-reviewer (dòng `tblLog` "Scanner
trigger" vẫn ghi kể cả khi barcode là QR2 bị bỏ qua) — chính reviewer xác nhận đây chỉ là vấn đề thẩm
mỹ, không bắt buộc sửa.

**Verify:** Build bằng MSBuild VS2022 Professional (Release|AnyCPU) → 3 lần rebuild liên tiếp sau mỗi
đợt fix đều 0 lỗi CS, sinh `bin\Release\SSFG.exe` thành công. Chưa test thật với PLC/scanner hardware
hay qua fake-data harness `IsTest=true` (không có GUI tương tác trong sandbox) — xem
`active_context.next_step` để biết các bước verify thủ công cần làm, đặc biệt bước verify riêng cho
fix HIGH của `DataEvent_EventHandleValueChange` (guard bypass) vì đây là bug an toàn quan trọng nhất
tìm được trong session này.

---

### [2026-07-09] — Session: Thêm project HardwareSimulator (giả lập Cognex scanner + máy in AnserU2)

```
[FEAT]     WeightChecking/HardwareSimulator/HardwareSimulator.csproj       — Project WinForms mới, .NET Framework 4.7.2, KHÔNG dùng DevExpress, chỉ System.Windows.Forms/System.Net.Sockets
[FEAT]     WeightChecking/HardwareSimulator/Program.cs                     — Entry point → Application.Run(new MainForm())
[FEAT]     WeightChecking/HardwareSimulator/ScannerSimulatorControl.cs     — UserControl dùng chung cho tab Metal + Scale: TCP line server giả lập phía camera Cognex (khớp DriverTelnet.cs)
[FEAT]     WeightChecking/HardwareSimulator/PrinterSimulatorControl.cs     — UserControl tab Printer: TCP STX/ETX frame server giả lập máy in AnserU2 (khớp AnserU2TcpDriver.cs)
[FEAT]     WeightChecking/HardwareSimulator/MainForm.cs                    — TabControl 3 tab host 2x ScannerSimulatorControl + 1x PrinterSimulatorControl
[CHORE]    WeightChecking/WeightChecking.sln                               — Thêm Project entry HardwareSimulator + ProjectConfigurationPlatforms (Debug/Release x Any CPU)
```

**Bối cảnh:** User yêu cầu xây công cụ giả lập 2 camera Cognex (trạm Metal + Scale, TCP client cố định
port 23) và máy in AnserU2 (TCP client, `IpPrinter`/`PortPrinter`, mặc định `192.168.4.70:4001`) để có
thể test/debug luồng scan/reject/print mà không cần hardware thật. Đã lập kế hoạch đầy đủ qua
`EnterPlanMode` + 3 lượt `AskUserQuestion` (chọn phương án Recommended cho cả 3), plan được user duyệt
không sửa gì (lưu tại `concurrent-stirring-fiddle.md`). Ràng buộc cứng đã tuân thủ tuyệt đối: **không
sửa bất kỳ file production nào** (`frmScaleNewUI.cs`, `DriverTelnet.cs`, `AnserU2TcpDriver.cs`,
`ConfigJsonModel`) — toàn bộ là project mới, hoàn toàn additive.

**Thực hiện:**
- `ScannerSimulatorControl`: `TcpListener` chấp nhận 1 client, gửi dòng ASCII kết thúc `\r\n` (khớp
  `DriverTelnet` phía client dùng `StreamReader.ReadLineAsync()`); tự quay lại `AcceptTcpClientAsync()`
  chờ kết nối kế tiếp khi client ngắt (khớp hành vi auto-reconnect thật). Dùng 2 instance với IP mặc
  định khác nhau (`127.0.0.2:23` cho Metal, `127.0.0.3:23` cho Scale, dải loopback `127.0.0.0/8`) vì cả
  2 trạm production đều hardcode port 23 — không thể chạy 2 listener cùng lúc trên cùng 1 IP:port, nên
  tách theo IP thay vì port để không cần sửa code production. Không hardcode preset barcode (định dạng
  QR phụ thuộc OC/product code thật từ DB) — để tester tự dán chuỗi thật vào textbox Send.
- `PrinterSimulatorControl`: parser buffer byte giữa STX(`0x02`)/ETX(`0x03`) (copy logic từ
  `AnserU2TcpDriver.ReceiveLoopAsync`), decode theo đúng layout xác nhận trong `frmScaleNewUI.cs`:
  byte[4]==`0x46`+byte[5]==2 → StartPrint; byte[4]==`0x46`+byte[9]==`0x4C` → StopPrint; byte[4]==`0xCA`
  → đọc length byte[7]/[8]/[9] rồi cắt payload ASCII (grossWeight/createdDate/idLabel) từ offset 12.
  3 nút phản hồi thủ công (ACK `0x4F`, Print Success `0x30`, Fail `0x31`+errCode) — không cần checksum
  thật vì `PrinterDataReceived` phía app chỉ đọc `rcvArr[4]`/`rcvArr[5]`. Checkbox "Auto ACK": tự gửi
  ACK ngay khi nhận đủ 1 frame, rồi sau N ms (mặc định 800ms, chỉnh được) tự gửi tiếp Print Success —
  mô phỏng độ trễ in vật lý để test lặp lại nhanh không cần bấm tay.
- `MainForm`: `TabControl` 3 tab (Metal Scanner / Scale Scanner / Printer), mỗi tab host 1 instance
  UserControl tương ứng, `Dock=Fill`.
- `.sln`: thêm project theo đúng path tương đối `HardwareSimulator\HardwareSimulator.csproj` (không có
  `..\` — vì `HardwareSimulator\` là sibling của `WeightChecking\WeightChecking\` cùng cấp với
  `WeightChecking.sln`, khác với `AnserU2_cSharp` vốn cần `..\` vì nằm ngoài thư mục `WeightChecking\`).

**Phát hiện phụ (ngoài phạm vi, không sửa):** Build full solution phát hiện
`WeightChecking/CognexLibrary/CognexLibrary.csproj` (SDK-style, `TargetFramework netstandard2.1`) lỗi
`CS0579: Duplicate 'System.Reflection.Assembly*Attribute' attribute` (6 chỗ: Company, Configuration,
FileVersion, Product, Title, Version) — do project này vừa để MSBuild SDK-style tự sinh AssemblyInfo
(`GenerateAssemblyInfo` mặc định `true`, không có override) vừa có sẵn
`Properties/AssemblyInfo.cs` viết tay, gây trùng attribute. Xác nhận qua `git status --porcelain --
WeightChecking/CognexLibrary/` (rỗng — không có thay đổi uncommitted) rằng đây là vấn đề **tồn tại sẵn
từ trước, hoàn toàn không liên quan tới việc thêm `HardwareSimulator`** — không sửa (ngoài phạm vi
task, `HardwareSimulator` không phụ thuộc `CognexLibrary`). Ghi nhận vào `open_questions`.

**Verify:** Build riêng `HardwareSimulator.csproj` bằng MSBuild VS2022 Professional → 0 lỗi. Build full
solution (`WeightChecking.sln`) → thành công cho `WeightChecking`/`AnserU2_cSharp`/`WindowsFormsApp1`/
`HardwareSimulator`; chỉ `CognexLibrary.csproj` lỗi (pre-existing, xem trên). Chưa test chạy thật
end-to-end (không có GUI/hardware tương tác trong sandbox này — không thể bấm Listen/Send hay quan sát
app WeightChecking thật kết nối vào) — xem `active_context.next_step` để biết các bước verify thủ công
cần làm trên máy có màn hình/hardware thật.

---

### [2026-07-16] — Session: Fix NRE (Owner→this) + fix scanner event "chỉ nhảy vào 1 lần rồi im lặng"

```
[FIX]      WeightChecking/WeightChecking/frmScaleNewUI.cs                 — DataEvent_EventHandleValueChange (nhánh box-info QR): GlobalVariables.InvokeIfRequired(Owner, ...) → InvokeIfRequired(this, ...) — Owner null (form không được show với owner) gây NullReferenceException "control was null" tại GlobalVariables.cs:14, đúng dòng user báo qua debugger (frmScaleNewUI.cs:888). Đây là call site DUY NHẤT trong file dùng Owner thay vì this (36 call site còn lại đều dùng this, xác nhận qua Grep) — lỗi phát sinh từ chính implementation Task J phía dưới.
[FIX]      WeightChecking/CognexLibrary_NETFramework/DriverTelnet.cs      — ReadData(): bọc try/catch RIÊNG quanh việc set _dataEvent.QRCodeValue (dispatch chạy đồng bộ toàn bộ chuỗi handler app-level: DataEvent_EventHandleValueChange → BarcodeScanner2Handle → DB/cân/in) — exception app-level (QR test không khớp master data, v.v.) trước đây lọt vào catch ngoài cùng của ReadData (dành cho lỗi mạng/IO), khiến ReadData hiểu nhầm thành lỗi đọc Telnet và gọi Reconnect() — phá 1 kết nối TCP đang khoẻ mạnh (mất mọi byte OS đã buffer sẵn) chỉ vì 1 lần scan xử lý lỗi. Đây chính là nguyên nhân "event chỉ nhảy vào 1 lần rồi im lặng dù simulator vẫn gửi liên tục". Giờ chỉ log qua Debug.WriteLine, KHÔNG gọi Reconnect(), kết nối được giữ nguyên.
[FIX]      WeightChecking/CognexLibrary_NETFramework/DriverTelnet.cs      — ReadData(): bọc try/catch RIÊNG quanh vòng lặp opportunistic đọc thêm dòng (grace window) — lỗi ở đây trước đây cũng bị catch ngoài cùng nuốt, có nguy cơ làm mất luôn dòng `response` ĐÃ đọc thành công + gọi Reconnect() không cần thiết. Giờ nếu drain lỗi, vẫn dispatch telegram với số dòng đã gom được, chỉ log cảnh báo.
[DOCS]     CLAUDE.md                                                     — Cập nhật active_context + related_files, thêm entry CHANGELOG này
```

**Bối cảnh:** 2 report riêng của user trong cùng session, cả 2 đều là regression từ chính implementation
Task J (multi-QR batching) ở entry ngay phía dưới:
1. Kèm 3 screenshot debugger: app kết nối `127.0.0.3:23` (Scale, qua `HardwareSimulator`), nhận đúng 1
   telegram gộp 2 QR thật, chạy tới `frmScaleNewUI.cs:888` thì văng `NullReferenceException — control was
   null` bên trong `GlobalVariables.InvokeIfRequired`. User: "chạy tới line 888 thì nó văng lỗi".
2. "fix lỗi sự kiện read qr code DataEvent_EventHandleValueChange chỉ nhảy vào 1 lần, sau đó ko nhảy
   vào nữa, mặc dù mô phỏng send data lên liên tực."

**Fix #1 (NRE):** Root cause xác nhận trực tiếp — `Owner` là property `Form`, null khi form không được
show kèm owner (không phải qua `ShowDialog(owner)`). Grep toàn bộ 37 call site `InvokeIfRequired` trong
`frmScaleNewUI.cs` xác nhận 36/37 dùng `this`, chỉ đúng 1 chỗ (nhánh box-info QR, mới thêm ở Task J) dùng
nhầm `Owner`. Sửa `Owner` → `this`.

**Fix #2 (event im lặng sau 1 lần) — chẩn đoán tốt nhất có thể, CHƯA có log debug thật của lần lỗi cụ
thể để xác nhận 100% (đã loại trừ: `DataEvent.QRCodeValue` setter KHÔNG có guard so sánh giá trị cũ/mới —
đọc `DataEvent.cs` xác nhận dòng check bị comment sẵn `//if (_qrCodeValue != value)` nên telegram trùng
lặp vẫn fire event bình thường; `_scannerIsBussy[1]` không giải thích được vì Debug.WriteLine đầu hàm
vẫn phải chạy bất kể guard). Kiến trúc trước fix: `ReadData()` có 1 khối `try` NGOÀI CÙNG dành cho lỗi
mạng/IO (catch → `Reconnect()`), nhưng dispatch `_dataEvent.QRCodeValue = ...` (chạy đồng bộ TOÀN BỘ
`DataEvent_EventHandleValueChange` → `BarcodeScanner2Handle` → DB/cân/in) lại nằm LỒNG bên trong cùng
khối `try` đó — nên bất kỳ exception app-level nào (QR test/giả không khớp OC thật trong DB, v.v.) đều
bị hiểu nhầm là lỗi Telnet, kích hoạt `Reconnect()` phá kết nối đang khoẻ. Nếu simulator tiếp tục gửi
data lên connection cũ đã bị đóng, event sẽ không còn fire nữa — đúng triệu chứng user báo. Vòng lặp
opportunistic đọc thêm dòng (grace window) cũng có cùng vấn đề, thêm rủi ro mất cả dòng đã đọc thành
công. Đã tách 2 khối này ra try/catch RIÊNG (không escalate `Reconnect()`, chỉ `Debug.WriteLine`), vẫn
giữ nguyên bên trong vùng được bảo vệ bởi `isReading` (bắt buộc để giữ đúng guarantee thứ tự batching
của Task J).

**Không đụng:** khối `catch` ngoài cùng của `ReadData()` (vẫn là handler lỗi mạng/IO thật, chỉ còn scope
lại đúng cho `firstLineTask` + bất kỳ exception nào KHÔNG bị 2 catch mới nuốt), `finally { isReading =
false; }`, toàn bộ cơ chế `_pendingLine`/batching của Task J.

**Verify:** `MSBuild WeightChecking.sln /p:Configuration=Release /p:Platform="Any CPU"` → 0 lỗi cho
`WeightChecking`(`SSFG.exe`)/`CognexLibrary_NETFramework`/`AnserU2_cSharp`/`WindowsFormsApp1`/
`HardwareSimulator`/`CognexScannerTester`; chỉ còn lỗi CS0579 pre-existing của `CognexLibrary.csproj`
(không liên quan). Fix #1 verify chắc chắn (root cause rõ ràng, đúng pattern 36 call site còn lại). Fix
#2 CẦN user re-test với simulator gửi liên tục để xác nhận triệt để — nếu vẫn còn im lặng, Debug Output
giờ sẽ hiện trực tiếp dòng `[DriverTelnet] QRCodeValue event handler threw...` hoặc `[DriverTelnet]
Extra-line drain failed...` thay vì bị nuốt thành 1 lần `Reconnect()` không rõ nguyên nhân — sẽ lộ ngay
root cause thật nếu chẩn đoán trên chưa đầy đủ.

---

### [2026-07-16] — Session: Fix DataEvent_EventHandleValueChange cho trường hợp Cognex trả 2 QR gộp trong 1 lần trigger

```
[FIX]      WeightChecking/CognexLibrary_NETFramework/DriverTelnet.cs      — ReadData(): sau dòng đầu tiên, đọc thêm tối đa 4 dòng nữa (MaxLinesPerTelegram=5), mỗi dòng chờ grace window 50ms (Task.WhenAny với Task.Delay) rồi gộp bằng "\r\n" thành 1 QRCodeValue duy nhất — trước đây mỗi tick timer 100ms chỉ đọc đúng 1 dòng, khiến Main QR và QR2 (box info) của cùng 1 lần trigger bị tách thành 2 event rời rạc theo 2 tick khác nhau
[FIX]      WeightChecking/CognexLibrary_NETFramework/DriverTelnet.cs      — Thêm field _pendingLine: 1 read còn dở dang khi hết grace window được giữ lại (không bỏ) để tick ReadData kế tiếp tái sử dụng làm dòng đầu tiên, vì StreamReader không an toàn khi gọi ReadLineAsync() chồng lấn
[FIX]      WeightChecking/CognexLibrary_NETFramework/DriverTelnet.cs      — DisconnectDevices(): clear _pendingLine để tránh tái sử dụng read task cũ (gắn với reader/stream đã đóng) sau khi reconnect
[FEAT]     WeightChecking/WeightChecking/frmScaleNewUI.cs                 — Thêm helper SplitTelegramLines() (split theo "\r\n"/"\r"/"\n") để tách 1 QRCodeValue đã gộp nhiều dòng trở lại thành từng dòng riêng
[FIX]      WeightChecking/WeightChecking/frmScaleNewUI.cs                 — DataEvent_EventHandleValueChange (Scale): viết lại để duyệt HẾT các dòng trong telegram gộp, bắt tất cả dòng box-info QR2 vào _boxTypeQR/_boxWeightQR TRƯỚC, chỉ dispatch dòng Main QR (dòng còn lại không khớp TryParseBoxInfoQr) SAU CÙNG khi vòng lặp kết thúc — đúng bất kể thứ tự camera gửi 2 QR, khác với trước đây (dù đã tách đúng theo dòng, xử lý theo thứ tự tuần tự nên chỉ đúng nếu QR2 đến trước Main QR)
[FIX]      WeightChecking/WeightChecking/frmScaleNewUI.cs                 — DataEventMetal_EventHandleValueChange (Metal): áp dụng cùng pattern SplitTelegramLines — vẫn bỏ qua hoàn toàn mọi dòng box-info QR2 (giữ đúng hành vi cũ), nhưng giờ tách đúng theo từng dòng thay vì so khớp TryParseBoxInfoQr trên cả chuỗi gộp (trước đây nếu Metal cũng nhận phải telegram gộp, cách so khớp cũ trên toàn chuỗi sẽ luôn fail và dòng Main QR thật bị nuốt theo QR2)
[DOCS]     CLAUDE.md                                                     — Cập nhật active_context (Task J), thêm entry CHANGELOG này
```

**Bối cảnh:** Tiếp nối trực tiếp từ Task I (thêm QR2 box-info) và công cụ chẩn đoán Task H
(`HardwareSimulator`/`CognexScannerTester`). User xác nhận qua Cognex DataMan Result History và PuTTY
capture thật: camera thật trả về Main QR rồi `<0x0D><0x0A>` rồi QR2 box-info, GỘP trong CÙNG 1 lần
trigger vật lý. Yêu cầu: "chỉnh lại code cho sự kiện scanner cognex2 để nó hoạt động đúng với trường
hợp cognex trả về 2 QR code."

**Root cause xác nhận được (đọc lại toàn bộ `DriverTelnet.ReadData()`, không suy đoán):**
`ReadData()` chỉ làm đúng 1 lần `await reader.ReadLineAsync()` mỗi tick 100ms, và `isReading` giữ
`true` suốt thời gian xử lý ĐỒNG BỘ phía sau — vì `DataEvent_EventHandleValueChange` gọi
`BarcodeScanner2Handle` đồng bộ (bắt buộc theo RULE-RT của project, không dùng async/await cho handler
scanner) và method đó làm việc nặng đồng bộ (query DB, đợi cân ổn định, in tem), dòng QR2 thứ 2 trong
cùng telegram không thể được đọc khỏi socket cho tới khi TOÀN BỘ pipeline Main QR (kể cả khối override
box-weight và `finally` reset `_boxTypeQR`/`_boxWeightQR`) đã chạy xong ở tick trước — nghĩa là override
gần như không bao giờ khớp đúng thùng.

**Fix 2 lớp** (toàn bộ độ phức tạp async nằm trong `DriverTelnet.cs` — nơi đã dùng async/await sẵn —
không đụng tới ràng buộc "không async/await cho handler scanner" của 2 method trong `frmScaleNewUI.cs`,
vẫn giữ nguyên `void` đồng bộ):
1. **Layer 1 (driver-level batching)** — `DriverTelnet.ReadData()` gom nhiều dòng CRLF đến trong cùng
   1 lần `Write()` vật lý của camera thành 1 event `QRCodeValue` duy nhất, dùng grace window 50ms/dòng
   thay vì delay cố định — không chặn luồng đọc bình thường khi chỉ có 1 QR (grace window chỉ kích hoạt
   SAU khi đã có ít nhất 1 dòng).
2. **Layer 2 (app-level reordering)** — cả 2 handler trong `frmScaleNewUI.cs` duyệt hết các dòng trong
   telegram gộp trước, bắt QR2 trước, dispatch Main QR sau — làm cho tính đúng đắn không phụ thuộc thứ
   tự dòng trong telegram (trước đây, ngay cả khi đã tách đúng theo dòng, code vẫn giả định QR2 tới
   trước Main QR — sai với thứ tự thật trên hardware).

**Không đụng:** `TryParseBoxInfoQr()` (contract cũ giữ nguyên, chỉ nhận 1 dòng), khối early-return QR2
trong `BarcodeScanner2Handle` (giữ nguyên — defense-in-depth + vẫn cần cho test harness `IsTest=true`
gọi trực tiếp theo §6.2), `_scannerIsBussy[]`/`_readQrStatus[]` guard (không đổi vị trí kiểm tra so với
Task I — vẫn đặt SAU khi đã xác định xong dòng Main QR), `CognexScannerTester`/`HardwareSimulator` (đã
xử lý đúng multi-line raw value từ Task H, không cần sửa thêm).

**Verify:** `MSBuild WeightChecking.sln /p:Configuration=Release /p:Platform="Any CPU"` → 0 lỗi cho
`WeightChecking`(`SSFG.exe`)/`AnserU2_cSharp`/`WindowsFormsApp1`/`HardwareSimulator`/
`CognexScannerTester` (project cuối cùng phụ thuộc trực tiếp `CognexLibrary_NETFramework` nên xác nhận
gián tiếp `DriverTelnet.cs` build sạch); chỉ còn lỗi CS0579 pre-existing của `CognexLibrary.csproj`
(không liên quan, đã ghi nhận từ Task H). Chưa test được với hardware Cognex thật (sandbox không có
mạng tới camera) — xem `active_context.next_step` để biết cách dùng `CognexScannerTester` +
`HardwareSimulator` (2 tool đã có từ Task H) để verify Layer 1, và cách verify Layer 2 qua app thật
hoặc fake-data harness.

---

### [2026-07-08] — Session: Dịch toàn bộ message runtime sang tiếng Anh (whole solution)

```
[CHORE]    Views/frmConfirmPrint.cs                          — 5 MessageBox.Show (QR sai định dạng, 2 dialog xác nhận thùng/deviation, thiếu quyền, không tìm thấy) dịch sang tiếng Anh
[CHORE]    Program.cs                                        — MessageBox thông báo có bản cập nhật mới + 2 chỗ SplashScreenManager.SetWaitFormCaption("Vui lòng chờ...") dịch sang tiếng Anh
[CHORE]    Login.cs                                          — XtraMessageBox "Nhập thiếu thông tin..." dịch sang tiếng Anh
[CHORE]    Views/frmReports.cs                                — XtraMessageBox (Get Data Error, Report Error) + Log.Error + SplashScreenManager caption dịch sang tiếng Anh
[CHORE]    StaticClass/AutoPostingHelper.cs                   — Debug.WriteLine + return string trong AutoTransfer/AutoStockIn/AutoStockOut ("đã cập nhật kho" / "cập nhật kho thất bại" / "Thông tin không hợp lệ") dịch sang tiếng Anh
[CHORE]    AnserU2_DK/AnserU2_cSharp/Form1.cs                 — 3 Console.WriteLine tiếng Việt không dấu (in thanh cong / Gui lenh xuong may in thanh cong / Loi. Error Code) dịch sang tiếng Anh
[CHORE]    DLL modbus/ModbusRTUMaster/ModbusRTUMaster/Form1.cs — Console.WriteLine "doc nhiet do thanh cong" dịch sang tiếng Anh
[DOCS]     CLAUDE.md                                         — Cập nhật active_context (đóng task dịch message), thêm entry CHANGELOG này
```

**Bối cảnh:** User yêu cầu (sau khi tự sửa lại tags.json cho đúng): "Dùng chính xác các tag trong
file tags.json, tuyệt đối không được thay đổi. Nhiệm vụ tiếp theo là chuyển tất cả các đoạn message
trong project thành tiếng Anh." Scope chốt qua AskUserQuestion: toàn bộ solution (14 project), chỉ
dịch message runtime (Debug.WriteLine/MessageBox/XtraMessageBox/Log.*/Console.WriteLine/`.Text`,
`.Title` gán trong code-behind/field Reason-Note-Message ghi DB), loại trừ hoàn toàn `*.Designer.cs`,
giữ nguyên mọi comment tiếng Việt.

**Ràng buộc tối quan trọng:** không string literal nào là tên tag PLC (khớp tags.json — `"RJ1"`,
`"S1"`, `"S2"`, `"Check_Weight_Result"`, `"Metal_Result"`, `"Scale_Value"`, `"Scale_Value_Stable"`,
`"Scale_Stable_Trigger"`, `"S5"`, `"S6"`, `"Delay_Time_To_Print"`, `"S_MD_OUT"`, ...) bị sửa trong
toàn bộ quá trình — chỉ dịch text người đọc xung quanh.

**Phương pháp:** Dịch từng file đã catalog từ trước (frmScaleNewUI.cs, GlobalVariables.cs,
frmMasterData.cs, frmMain.cs — hoàn thành ở session trước — rồi tiếp tục frmConfirmPrint.cs,
Program.cs, Login.cs, frmReports.cs, AutoPostingHelper.cs ở session này), verify từng file bằng Grep
kết hợp prefix pattern message với class ký tự dấu tiếng Việt. Sau khi hết catalog, chạy 2 lượt sweep
toàn solution: (1) lượt dấu tiếng Việt, (2) lượt từ khoá không dấu (một số string cũ gõ tiếng Việt
không bỏ dấu — "thanh cong", "khong the", "vui long", "mat ket", "gui lenh", "sai dinh dang"...) vì
không lọt qua được lượt (1). Lượt sweep phát hiện thêm 2 file ngoài catalog ban đầu:
`AnserU2_DK/AnserU2_cSharp/Form1.cs` (bản test app cũ hơn frmTcpTest.cs) và
`DLL modbus/ModbusRTUMaster/ModbusRTUMaster/Form1.cs`.

**Phát hiện phụ trong AutoPostingHelper.cs:** `replace_all` đầu tiên (scope theo pattern
`Debug.WriteLine(...)`) chỉ khớp 2/3 chỗ — method `AutoStockOut` có cùng câu tiếng Việt xuất hiện
CẢ trong `Debug.WriteLine(...)` LẪN trong `return $"...";` ngay phía dưới, nên bị bỏ sót ở lượt đầu.
Grep verify lại phát hiện, sửa nốt bằng 1 Edit riêng cho cả 6 dòng còn lại (3 Debug.WriteLine + 3
return).

**Xác nhận đúng ngoài phạm vi (không sửa):** mọi `*.Designer.cs`/`.Designer.vb`; mọi comment `//`
(kể cả không dấu); `Views/frmScale.cs` — toàn bộ nội dung gần như đã bị comment-out (bản legacy/dead
code, tiền thân của frmScaleNewUI.cs), tiếng Việt còn lại chỉ nằm trong comment; `S7Client/Form1.cs`
dòng 159 — false positive do biến `DemLoi` (bộ đếm) chứa chuỗi con "loi".

**Verify:** `MSYS_NO_PATHCONV=1 MSBuild WeightChecking/WeightChecking/WeightChecking.csproj
/p:Configuration=Release /p:Platform=AnyCPU /t:Build /nologo /v:minimal` → toàn bộ code C# compile
sạch (0 lỗi CS, chỉ còn warning CS8618/CS8625 có sẵn từ trước, không liên quan). Có đúng 1 lỗi
MSB3030 "Could not copy tags1.json — not found", nhưng KHÔNG liên quan tới việc dịch: `tags1.json`
bị xoá khỏi disk ngoài phiên làm việc này (`git status` báo `D`, không phải file Claude từng đọc/sửa
trong bất kỳ session dịch nào), trong khi `.csproj` (đã commit từ trước) vẫn còn
`<None Include="tags1.json">`. Không tự sửa `.csproj` hay khôi phục file — cần hỏi user ý định
(có `tagsPRD.json` mới, untracked, xuất hiện cùng lúc, có thể là file thay thế). Chưa test chạy thật
UI/hardware (không có kết nối trong sandbox) — cần verify thủ công: chạy qua các luồng scan/reject/
report/login/print và xác nhận UI hiển thị tiếng Anh đúng, không ảnh hưởng hành vi nghiệp vụ.

---

## 6. TESTING GUIDELINES (C# WinForms)

### 6.1 Không có automated test suite hiện tại

Project này là factory-floor WinForms app, không có unit test framework được cài đặt. Testing chủ yếu là:
- **Manual test** với `IsTest = true` trong ConfigJson (bỏ qua kết nối hardware)
- **Debug fake data** — các block `#region Fake data to debug` trong `FrmScale_Load` (comment/uncomment)
- **Log kiểm tra** — `tblLog` trong DB ghi lại mọi sự kiện barcode và auto-posting

### 6.2 Cách test thủ công khi thêm tính năng mới

```csharp
// 1. Bật IsTest mode trong config JSON
// 2. Uncomment fake data trong FrmScale_Load:
//    BarcodeScanner1Handle(1, "A129059,...");  // giả lập scan trạm 1
//    BarcodeScanner2Handle(2, "A129059,...");  // giả lập scan trạm 2
//    GlobalVariables.MyEvent.SensorBeforeWeightScan = 1;

// 3. Kiểm tra:
//    - UI hiển thị PASSED/FAILED đúng không?
//    - tblScanData / tblScanDataReject có record đúng không?
//    - tblLog có ghi lại đúng bước không?
```

### 6.3 Business logic cần kiểm tra kỹ khi thay đổi

| Logic | Vị trí | Điều cần kiểm tra |
|-------|--------|------------------|
| Tính `StdGrossWeight` | `BarcodeScanner2Handle` ~line 2062 | `StdNetWeight + PackageWeight + BoxWeight` |
| Tính `DeviationPairs` | ~line 2124 | So sánh netWeight với tolerance range |
| Chọn boxWeight theo qty | ~line 1955 | BX1 > BX2 > BX3 > BX4 logic |
| Special case (hàng PU) | `specialCase` flag | Printing=0 vs Printing=1 |
| Xác định kho đích AutoPost | `AutoPostingHelper` | 1185/1223 → 2 (normal) hay → 10 (PR order) |

---

## 7. PERFORMANCE & RELIABILITY RULES (Factory Automation)

### 7.1 Nguyên tắc đặc thù cho real-time WinForms

```
RULE-RT-01: Không block UI thread — mọi I/O (DB, PLC, Serial) phải chạy trên background thread
RULE-RT-02: Cross-thread UI update PHẢI dùng GlobalVariables.InvokeIfRequired() — không dùng Invoke() trực tiếp trên control (có thể deadlock khi form đóng)
RULE-RT-03: CancellationTokenSource cho mọi long-running Task — cancel trong FormClosing, dispose trong finally
RULE-RT-04: Không dùng Thread.Sleep() trên UI thread — dùng Task.Delay() hoặc chuyển sang background thread
RULE-RT-05: _scannerIsBussy[] guard PHẢI được reset bởi hardware signal (sensor), không reset bằng timer
```

### 7.2 Checklist khi thêm kết nối hardware mới

```markdown
- [ ] Đăng ký event handler trong FrmScale_Load
- [ ] Hủy đăng ký event handler trong FrmScale_FormClosing
- [ ] Thêm CancellationTokenSource nếu có polling loop
- [ ] Cancel + Wait(1000) + Dispose trong FormClosing
- [ ] Thêm try-catch bao quanh toàn bộ handler (tránh crash app)
- [ ] Log lỗi vào tblLog với đủ context (station, barcode, timestamp)
```

### 7.3 Thời gian phản hồi mục tiêu

| Hành động | Target | Ghi chú |
|-----------|--------|---------|
| Đọc DB sau scan | < 200ms | sp_vProductItemInfoGet |
| Ghi PLC S7 (pusher) | < 50ms | WriteData2PlcSeimens |
| Ghi Modbus RTU | < 100ms | WriteHoldingRegisters |
| In tem AnserU2 | < 500ms | SendDynamicString |
| Auto-transfer kho | < 2s | AutoPostingHelper.AutoTransfer |
| Polling PLC Snap7 | 200ms interval | PlcSubscriptionManager |

---

## 8. HOW TO USE THIS FILE

### 8.1 Dành cho Claude (AI Assistant)

```
Khi đọc file này, Claude phải:

1. LUÔN đọc toàn bộ file trước khi viết bất kỳ dòng code nào
2. TUÂN THỦ naming convention, comment format đã định nghĩa
3. CẬP NHẬT active_context sau mỗi session
4. THÊM entry vào CHANGELOG mỗi khi sửa/thêm/xoá code đáng kể
5. THAM CHIẾU Decision Log trước khi đề xuất kiến trúc/công nghệ
6. VIẾT test cho mọi function mới theo Section 6
7. KIỂM TRA Performance Checklist khi code liên quan đến render/query
8. KHÔNG lặp lại câu hỏi đã có câu trả lời trong file này
```

### 8.2 Dành cho Developer

```bash
# Mỗi khi bắt đầu ngày làm việc
# 1. Pull code mới nhất
git pull origin fIN_Main_dev

# 2. Cập nhật active_context trong CLAUDE.md nếu task thay đổi
# 3. Bật IsTest=true trong ConfigJson để chạy không cần hardware

# Build & Deploy
# Solution: WeightChecking/WeightChecking.sln
# Target: x86, .NET Framework 4.8
# Output: bin/Release/ → copy toàn bộ folder lên máy trạm

# Khi tạo PR
# 1. Đảm bảo CLAUDE.md được cập nhật (active_context + CHANGELOG)
# 2. Test manual với IsTest=true + fake data
# 3. Merge vào fIN_Main_dev → sau đó main
```

### 8.2b Database & Stored Procedures quan trọng

| SP / Query | Mục đích |
|-----------|---------|
| `sp_GetMesoInfo` | Lấy thông tin nhà máy (MESOCOMP) khi load form |
| `sp_vProductItemInfoGet` | Lấy thông tin sản phẩm: weight, tolerance, box config |
| `sp_GetLotOfBrooksHC` | LotNo cho hàng HeelCounter Brooks |
| `sp_lmpScannerClient_ScanningLabel_CheckIn` | Check item đang ở kho nào (WMS) |
| `tblScanData` | Log kết quả kiểm tra (Pass/Fail, weight, deviation) |
| `tblScanDataReject` | Log thùng bị reject (lý do, trạm, thông tin thùng) |
| `tblMetalScanResult` | Log kết quả kiểm tra kim loại |
| `tblLog` | Audit log tổng quát (barcode events, auto-posting) |
| `tblIncomingIDC` | Ghi nhận thùng vào trạm (Identification) |
| `tblItemMissingInfo` | Item chưa có data trong hệ thống |

### 8.3 Template prompt để dùng với Claude Code / Cowork

```
# Bắt đầu task mới:
"Đọc CLAUDE.md. Task: [mô tả task]. 
Các file liên quan: [list files].
Sau khi xong, cập nhật active_context và CHANGELOG."

# Debug / fix:
"Đọc CLAUDE.md section 4 (context) và file [X].
Bug: [mô tả]. Expected: [hành vi đúng].
Ghi FIX vào CHANGELOG sau khi sửa xong."

# Code review:
"Đọc CLAUDE.md coding standards.
Review file [X] theo đúng conventions đã định nghĩa.
Liệt kê vi phạm theo format: [Line] [Rule] [Gợi ý sửa]."

# Viết test:
"Đọc CLAUDE.md section 6. Viết unit test cho [function/file].
Đảm bảo cover: happy path, error cases, edge cases."
```

### 8.4 Maintenance

| Việc cần làm                        | Tần suất      | Người chịu trách nhiệm |
|-------------------------------------|---------------|------------------------|
| Cập nhật `active_context`           | Mỗi session   | Dev đang làm việc      |
| Thêm entry `CHANGELOG`              | Mỗi commit    | Dev đang làm việc      |
| Review và dọn CHANGELOG cũ         | Mỗi sprint    | Tech Lead              |
| Cập nhật ADR khi có quyết định mới  | Khi phát sinh | Người quyết định       |
| Review Performance Budget           | Mỗi release   | Tech Lead              |
| Audit test coverage                 | Mỗi sprint    | QA / Dev               |

---

> **Lưu ý:** File này là nguồn sự thật duy nhất (*single source of truth*) cho AI assistant làm việc với project.  
> Khi có mâu thuẫn giữa code và CLAUDE.md → **ưu tiên CLAUDE.md**, sau đó sửa code cho nhất quán.

---
*CLAUDE.md · WeightChecking SSFG · Cập nhật: 2026-06-12 · Claude Sonnet 4.6*
