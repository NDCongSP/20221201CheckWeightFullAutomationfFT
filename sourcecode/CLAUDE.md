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
    DONE (Task H) — Xây dựng công cụ giả lập phần cứng ("làm phần mềm giả lập scanner cognex và mays
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
    KHÔNG blocked. Task H (project HardwareSimulator giả lập Cognex scanner + máy in AnserU2) đã build
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

  last_session: "2026-07-09"

  open_questions:
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
