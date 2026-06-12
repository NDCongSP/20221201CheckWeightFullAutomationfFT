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
  current_task: "Thay thế Serial Port (COM) bằng TCP cho máy in Anser SmartU2"

  related_files:
    - "WeightChecking/WeightChecking/frmScaleNewUI.cs"              # Form chính — đã cập nhật PrinterOpen/Close
    - "WeightChecking/WeightChecking/Class/AnserU2Print.cs"         # TCP driver mới (AnserU2TcpDriver)
    - "WeightChecking/WeightChecking/Models/Entities/IDCScanSystem/tblConfig.cs"  # Thêm IpPrinter, PortPrinter

  blocked_by: ""

  next_step: >
    - Cần cập nhật dữ liệu JSON trong tblConfig DB: thêm "IpPrinter":"192.168.4.70" và "PortPrinter":4001
    - Test với IsTest=false, máy in kết nối TCP 192.168.4.70:4001

  last_session: "2026-06-12"

  open_questions:
    - "Port TCP của máy in là 4001 — xác nhận lại với Hercules screenshot?"
    - "Có cần cấu hình IP tĩnh trên máy in không, hay đã có sẵn?"
```

### 4.1b Bản đồ luồng Scanner (quan trọng — đọc kỹ trước khi sửa)

```
Trạm 1 — Identification (Metal Scanner):
  Hardware: Zebra CoreScanner USB (CCoreScanner)
  Event: OnBarcodeEvent → BarcodeScanner1Handle(1, barcode)
  Guard: _scannerIsBussy[0] — reset bởi EventHandleSensorMiddleMetal

Trạm 2 — Scale (Weight Check):
  Hardware: Cognex DM290-X (Telnet, _driverTelnet)
  Event: DataEvent_EventHandleValueChange → BarcodeScanner2Handle(2, barcode)
  Guard: _scannerIsBussy[1] — reset bởi EventHandleSensorAfterWeightScan
  Scale: Modbus RTU, đợi _stableScale==1 (spin-wait với Thread.Yield)

Trạm 3 — Distribution (Printing Scanner):
  Hardware: Zebra CoreScanner USB (CCoreScanner, scanner ID khác trạm 1)
  Event: OnBarcodeEvent → BarcodeScanner3Handle(3, barcode)
  Guard: _scannerIsBussy[2] — reset bởi EventHandlerSensorAfterPrintScanner

PLC Write (Siemens S7):
  WriteData2PlcSeimens("P1"|"P2"|"P3"|"P4", value) — dùng PlcRuntime.Tags
  P1 = MetalPusher, P2 = WeightPusher, P3 = PrintPusher, P4 = MetalRejectPusher
```

### 4.2 Quyết định đã chốt (Decision Log)

| ID | Ngày | Quyết định | Ai quyết | File liên quan |
|----|------|-----------|----------|---------------|
| DEC-001 | 2025-05-10 | Dùng Cognex DM290-X Telnet thay Zebra USB ở trạm Scale | Dev | `frmScaleNewUI.cs` → `_driverTelnet` |
| DEC-002 | 2025-XX-XX | Snap7ClientLib thay Modbus TCP cho PLC Siemens | Dev | `frmScaleNewUI.cs` → `_plcRuntime` |
| DEC-003 | 2024-08-19 | AutoPostingHelper.CheckIn() trước rồi mới AutoTransfer() | Dev | `StaticClass/AutoPostingHelper.cs` |
| DEC-004 | 2025-XX-XX | Custom titlebar (FormBorderStyle.None + Panel) | Dev | `frmScaleNewUI.cs` constructor |
| DEC-005 | 2026-06-12 | CLAUDE.md cập nhật từ template TypeScript → thực tế C# WinForms | NDCongSP | `CLAUDE.md` |
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

<!-- Thêm session mới lên ĐẦU, trên dòng này -->

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
