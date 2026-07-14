# Quy trình test SSFG.exe bằng mô phỏng (HardwareSimulator + Snap7 Tag Monitor)

> Dùng 2 công cụ mô phỏng song song:
> 1. **`HardwareSimulator.exe`** (Task H) — giả lập 2 camera Cognex (Metal, Scale, Telnet) + máy in
>    AnserU2 (TCP) — dùng cho barcode QR và protocol in tem.
> 2. **Snap7 SCADA — Tag Monitor & Test** (công cụ có sẵn của user, mô phỏng vùng nhớ PLC S7 tại
>    `phucthinhautomation.ddns.net:102`, Rack 0 Slot 1 — cùng host/rack/slot mà `tags.json` của
>    `SSFG.exe` trỏ tới) — dùng để giả lập sensor (S1/S2/S5/S6/S_MD_OUT), kết quả kiểm tra kim loại
>    (Metal_Result), giá trị cân (Scale_Value/Scale_Value_Stable/Scale_Stable_Trigger), và **đọc lại**
>    giá trị app ghi xuống (RJ1, Check_Weight_Result) để verify không cần debugger.
>
> Bao phủ toàn bộ luồng 2 trạm hiện tại (Metal + Scale — trạm Distribution đã bỏ theo DEC-006), kể cả
> 2 bug vừa fix (Task F: guard `_isStartCountTimerWeight`; Task I: QR2 box info).

## 0. Chuẩn bị môi trường

1. Build cả 2 project (Release|AnyCPU):
   - `WeightChecking/WeightChecking/WeightChecking.csproj` → `bin/Release/SSFG.exe`
   - `WeightChecking/HardwareSimulator/HardwareSimulator.csproj` → `bin/Release/HardwareSimulator.exe`
2. Trong DB (`tblConfig.ConfigJson`) hoặc qua `frmSettings` (PropertyGridControl), set:
   | Key | Giá trị test |
   |---|---|
   | `IsTest` | `true` |
   | `IpCognexCamMetal` | `127.0.0.2` |
   | `IpCognexCamScale` | `127.0.0.3` |
   | `IpPrinter` | `127.0.0.1` |
   | `PortPrinter` | `4001` |
   | `TimerCheckQrMetal` | để nguyên hoặc hạ xuống `2` (giây) để rút ngắn thời gian chờ timeout khi test reject |
   | `TimerCheckQrScale` | để nguyên hoặc hạ xuống `2` (giây), tương tự |

   Port scanner luôn là `23` (hardcode trong `DriverTelnet`, không cấu hình được) — 2 trạm phân biệt
   bằng IP khác nhau, đúng lý do simulator dùng `127.0.0.2`/`127.0.0.3`. Tag PLC S7 (`Host`/`Rack`/
   `Slot`) đọc từ `tags.json`, không đọc từ `ConfigJson` — không cần sửa gì thêm nếu PLC memory
   simulator đã chạy sẵn tại đúng `Host` khai báo trong `tags.json`.
3. Chạy PLC memory simulator (server phía sau công cụ "Tag Monitor & Test") **trước**, xác nhận công
   cụ Tag Monitor báo `Status: Connected`.
4. Chạy `HardwareSimulator.exe`, bấm **Listen** ở cả 3 tab (Metal Scanner `127.0.0.2:23`, Scale
   Scanner `127.0.0.3:23`, Printer `127.0.0.1:4001`).
5. Chạy `SSFG.exe`, đăng nhập, mở form Scale (`frmScaleNewUI`). Quan sát:
   - `HardwareSimulator`: phải thấy "client connected" ở cả 3 tab trong vài giây (app tự kết nối lúc
     `FrmScale_Load`).
   - Tag Monitor: cột **Updated** của các tag phải tiếp tục nhảy timestamp đều đặn (app polling 200ms
     qua `PlcSubscriptionManager`/`_plc1Client.StartWatchdog(2000)`), nghĩa là `SSFG.exe` đã đọc được
     nhóm tag.
   Nếu không thấy connect: kiểm tra lại IP config ở bước 2 (2 driver Cognex), hoặc `Host`/`Rack`/
   `Slot` trong `tags.json` (PLC S7).
6. Chuẩn bị sẵn 1 chuỗi Main QR thật lấy từ `tblScanData`/log cũ (simulator không có preset — định
   dạng QR phụ thuộc OC/product code thật trong DB). Gọi chuỗi này là `<MainQR>` trong các case bên
   dưới. `<MainQR2>` là 1 chuỗi khác (OC khác hoặc cùng OC nhưng mã hàng khác) dùng cho test "thùng
   kế tiếp không có QR2".

### 0.1 Bảng tag PLC S7 (Tag Monitor) ↔ hiệu ứng trong code

Tất cả các tag dưới đây có trong `tags.json` (`Host=phucthinhautomation.ddns.net, Rack 0, Slot 1`) và
được `frmScaleNewUI.cs` subscribe `ValueChanged` (~dòng 688-771). Ghi (Write) một tag trong Tag
Monitor tương đương PLC thật gửi tín hiệu — app phản ứng y hệt production.

| Tag | Address | Ai ghi trong test | C# field/event khi tag đổi giá trị | Ghi chú |
|---|---|---|---|---|
| `S1` | DB1.DBW0 | Tester (Write) | `SensorBeforeMetalScan` — `NewValue==1` xoá label QR Identification; `NewValue==0` (và guard `_isStartCountTimer==false`) **arm** watchdog `CheckReadQr` | Trạm Metal |
| `S2` | DB1.DBW2 *(RJ1 thật ra là DBW2 — xem hàng riêng)* | — | — | *(không phải S2, xem RJ1 bên dưới)* |
| `S2` | DB1.DBW4 | Tester (Write) | `SensorMiddleMetal` — `NewValue==1` reset guard `_scannerIsBussy[0]=false`, `_isStartCountTimer=false` | Trạm Metal — mở guard cho thùng kế tiếp |
| `Metal_Result` | DB1.DBW6 | Tester (Write, trước khi ghi `S_MD_OUT`) | `MetalCheckResult` → field `_metalCheckResult` (0=pass, 1=fail) | Đọc bởi handler `S_MD_OUT` |
| `S_MD_OUT` | DB1.DBW8 | Tester (Write) | `SensorAfterMetalScan` — `NewValue==1` chốt kết quả kiểm tra kim loại (đọc `_metalCheckResult`), ghi `tblMetalScanResult`, tăng `CountMetalScan`, tự động chạy auto-transfer kho | Trạm Metal — chạy SAU khi đã set `Metal_Result` |
| `RJ1` | DB1.DBW2 | App ghi, tester **đọc lại** để verify | App ghi qua `WriteData2PlcSeimens("RJ1", value)` (1=cần kiểm tra kim loại, 2=không cần, 3=reject) | Xem cột **Value** trong Tag Monitor đổi đúng sau mỗi case Metal |
| `S5` | DB1.DBW12 | Tester (Write) | `SensorBeforeWeightScan` — `NewValue==1 && _isStartCountTimerWeight==false` → arm watchdog `CheckReadQrWeight`, reset UI trạm cân | Trạm Scale |
| `S6` | DB1.DBW26 | Tester (Write) | `SensorAfterWeightScan` — `NewValue==1` reset `_scannerIsBussy[1]`, `_readQrStatus[1]`, `_isStartCountTimerWeight` | Trạm Scale — mở guard cho thùng kế tiếp (**điểm test chính của Task F**) |
| `Scale_Value` | DB1.DBD14 (Real) | Tester (Write, tuỳ chọn) | `ScaleValue` → `_scaleValue`, chỉ hiển thị `labScaleValue` real-time, KHÔNG dùng để tính pass/fail | Cosmetic, có thể bỏ qua khi test logic |
| `Scale_Value_Stable` | DB1.DBD20 (Real) | Tester (Write **trước** `Scale_Stable_Trigger`) | `ScaleValueStable` → `_scaleValueStable` — giá trị này được gán vào `_scanDataWeight.GrossWeight` ngay khi `_stableScale` chuyển sang 1 | Đây là số cân "chốt" dùng để so tolerance |
| `Scale_Stable_Trigger` | DB1.DBW18 | Tester (Write **sau** `Scale_Value_Stable`) | `StableScale` → `_stableScale` — `BarcodeScanner2Handle` spin-wait tại `_stableScale==0` (dòng ~1875), chỉ thoát vòng lặp khi tag này = 1 | **Phải reset về 0 sau mỗi case** (xem §4) — nếu để nguyên =1, thùng kế tiếp sẽ bỏ qua luôn bước chờ cân ổn định |
| `Check_Weight_Result` | DB1.DBW24 | App ghi, tester **đọc lại** để verify | App ghi qua `WriteData2PlcSeimens("Check_Weight_Result", value)` sau khi tính pass/fail | Xem cột Value đổi đúng sau mỗi case Scale |
| `Delay_Time_To_Print` | DB1.DBW28 | Không cần ghi khi test logic scan/reject | `GlobalVariables.D506Value` | Chỉ liên quan timing lệnh in, không ảnh hưởng pass/fail |
| `Box_On_Scale` | DB1.DBW30 | — | Không có `ValueChanged` handler nào trong `frmScaleNewUI.cs` hiện dùng tag này | Tag khai báo sẵn trong `tags.json` nhưng chưa wiring — bỏ qua |

---

## 1. Trạm Metal (Identification) — `BarcodeScanner1Handle`

Guard liên quan: `_scannerIsBussy[0]` (reset bởi tag `S2` = `SensorMiddleMetal`), `_isStartCountTimer`/
`_ckQRTask` (watchdog `CheckReadQr`, arm bởi tag `S1`, timeout `TimerCheckQrMetal`).

| # | Case | Bước thực hiện | Kỳ vọng |
|---|------|-----------------|---------|
| M1 | Happy path — pass, cần kiểm tra kim loại | Tab Metal (HardwareSimulator): Send `<MainQR>` (sản phẩm có `MetalScan=true`) | `_labResultIdentification`/UI cập nhật; `tblIncomingIDC` có dòng mới; Tag Monitor: `RJ1` đổi thành **1** (yêu cầu kiểm tra kim loại) |
| M2 | Happy path — pass, bypass kiểm tra kim loại | Send `<MainQR>` (sản phẩm `MetalScan=false`) | Tag Monitor: `RJ1` = **2** (không cần kiểm tra) |
| M3 | Reject — QR sai định dạng | Send 1 chuỗi rác không khớp pattern OC (`"xyz"`) | Tag Monitor: `RJ1` = **3** (reject); `tblScanDataReject`/`tblItemMissingInfo` ghi Reason phù hợp; UI FAILED |
| M4 | Reject — thùng đã được check trước đó | Send `<MainQR>` 2 lần liên tiếp (đợi guard reset giữa 2 lần — xem M6) | Lần 2: `RJ1 = 3`, log lý do "đã check" |
| M5 | Reject — không tìm thấy sản phẩm / thiếu info | Send QR đúng định dạng OC nhưng mã hàng không tồn tại trong `sp_vProductItemInfoGet` | `RJ1 = 3`; `tblItemMissingInfo` ghi dòng mới |
| M6 | Guard `_scannerIsBussy[0]` reset đúng lúc | Sau M1: Tag Monitor → Write `S2 = 1` | `_scannerIsBussy[0]` về `false`, `_isStartCountTimer` về `false` — trạm sẵn sàng nhận thùng kế tiếp (Send `<MainQR2>` phải chạy bình thường ngay sau đó) |
| M7 | Watchdog reject khi không đọc được QR trong timeout | KHÔNG Send gì ở tab Metal. Tag Monitor: Write `S1 = 1` rồi sau ~1s Write `S1 = 0` (arm watchdog), đợi hết `TimerCheckQrMetal` giây | `CheckReadQr()` tự ghi reject đúng 1 lần khi `_readQrStatus[0]` vẫn `false`; kiểm tra `RJ1` chuyển sang giá trị reject |
| M8 | Kiểm tra kim loại fail | Sau M1 (RJ1=1), Tag Monitor: Write `Metal_Result = 1` rồi Write `S_MD_OUT = 1` | `_metalCheckResult=1` được đọc bởi handler `S_MD_OUT`; `tblMetalScanResult` ghi nhận fail; `CountMetalScan` tăng 1; auto-transfer kho theo `AutoPostingHelper` chạy nhánh reject |
| M9 | **QR2 (box info) bị bỏ qua hoàn toàn tại trạm Metal** | Send `"BX1,DKP,987.3Gr"` (QR2 Carton) hoặc `"G250001,1320"` (QR2 Plastic) ở tab Metal | `DataEventMetal_EventHandleValueChange` return sớm tại `TryParseBoxInfoQr` (dòng ~929) — **không** set `_scannerIsBussy[0]`, không ghi `tblScanDataReject`, `RJ1` KHÔNG đổi, UI không đổi. Debug Output có dòng "Box info QR ignored at Metal station" |

---

## 2. Trạm Scale (Weight Check) — `BarcodeScanner2Handle`

Guard liên quan: `_scannerIsBussy[1]` (reset bởi tag `S6`), `_isStartCountTimerWeight`/
`_ckQrWeightScanTask` (watchdog `CheckReadQrWeight`, arm bởi tag `S5`, timeout `TimerCheckQrScale` —
**fix Task F**), `_boxTypeQR`/`_boxWeightQR` (**Task I**), cân ổn định `_stableScale` (tag
`Scale_Stable_Trigger`, giá trị dùng là tag `Scale_Value_Stable`).

**Trình tự chuẩn cho 1 lần cân (dùng lại cho mọi case bên dưới trừ khi ghi chú khác):**
1. Tag Monitor: Write `S5 = 1` (arm watchdog, reset UI trạm cân).
2. HardwareSimulator tab Scale: Send `<MainQR>` — `BarcodeScanner2Handle` chạy tới đoạn spin-wait chờ
   `_stableScale==1`.
3. Tag Monitor: Write `Scale_Value_Stable = <khối lượng cân giả lập, gram>` (vd `8777`).
4. Tag Monitor: Write `Scale_Stable_Trigger = 1` — giải phóng spin-wait, `GrossWeight` được gán bằng
   giá trị bước 3.
5. Quan sát `Check_Weight_Result` trong Tag Monitor đổi theo kết quả pass/fail.
6. Tag Monitor: Write `Scale_Stable_Trigger = 0` (**bắt buộc** — xem cảnh báo ở bảng §0.1), rồi Write
   `S6 = 1` để mở guard cho thùng kế tiếp.

### 2.1 Happy path & reject cơ bản

| # | Case | Khác biệt so với trình tự chuẩn | Kỳ vọng |
|---|------|-----------------------------------|---------|
| SC1 | Happy path — trong tolerance | `Scale_Value_Stable` = giá trị nằm trong `lowerToleranceOfBox`..`upperToleranceOfBox` | UI PASSED; `Check_Weight_Result` ghi pass; `tblScanData` có dòng mới, `GrossWeight`/`Deviation` đúng công thức `StdNetWeight + PackageWeight + BoxWeight` |
| SC2 | Reject — ngoài tolerance | `Scale_Value_Stable` = giá trị lệch hẳn khỏi tolerance | UI FAILED, `tblScanDataReject` ghi dòng mới, `Check_Weight_Result` ghi reject |
| SC3 | Reject — QR sai định dạng | Ở bước 2, Send chuỗi rác thay vì `<MainQR>` | Reject tương tự M3, log Reason phù hợp — **không cần** bước 3/4 (không tới đoạn chờ cân) |
| SC4 | In tem sau khi pass | Sau SC1 | Tab Printer (HardwareSimulator) log nhận đúng frame StartPrint (byte[4]==0x46, byte[5]==2) rồi SetDynamicString (0xCA) với `grossWeight`/`createdDate`/`idLabel` khớp giá trị vừa tính; bấm "Send Print Success" (hoặc bật Auto ACK) → app cập nhật trạng thái in thành công |

### 2.2 Fix Task F — guard `_isStartCountTimerWeight` (chống spawn trùng watchdog)

| # | Case | Bước thực hiện | Kỳ vọng |
|---|------|-----------------|---------|
| SC5 | Không đọc được QR — reject đúng 1 lần | KHÔNG Send gì ở tab Scale. Tag Monitor: Write `S5 = 1`, đợi ~1s, Write `S5 = 0` rồi `S5 = 1` lại 2-3 lần liên tiếp trong vòng `TimerCheckQrScale` giây (mô phỏng sensor rung/nhiễu) | `EventHandleSensorBeforeWeightScan` chỉ spawn **1** `_ckQrWeightScanTask` (nhờ guard `_isStartCountTimerWeight`); `Check_Weight_Result`/`WeightPusher` reject chỉ bị ghi **đúng 1 lần** — đây chính là bug đã fix ở Task F, verify bằng cách đếm số dòng `tblScanDataReject` mới VÀ số lần Debug Output hit `WeightPusher = 2` |
| SC6 | Guard mở lại đúng lúc cho thùng kế tiếp | Sau SC5 (hoặc SC1): Tag Monitor Write `S6 = 1` | `_isStartCountTimerWeight` về `false`; lặp lại trình tự chuẩn cho `<MainQR2>` ngay sau đó phải chạy watchdog mới bình thường (không bị đứng vì guard cũ chưa reset) |
| SC7 | Sensor after không bắn → trạm "đứng hình" (trade-off có chủ đích, không phải bug) | Lặp lại SC5, nhưng **không** Write `S6 = 1` sau đó | `_isStartCountTimerWeight` giữ `true` mãi; Write `S5=1` cho thùng kế tiếp sẽ KHÔNG spawn watchdog mới (giống hệt cơ chế `_isStartCountTimer` ở trạm Metal) — xác nhận đây là hành vi đã biết, để chẩn đoán đúng nếu trạm Scale "đứng hình" thật ngoài sản xuất |

### 2.3 Task I — QR2 (box info) tại trạm Scale

| # | Case | Bước thực hiện | Kỳ vọng |
|---|------|-----------------|---------|
| SC8 | QR2 Carton override đúng box weight/type | Trình tự chuẩn, nhưng ở bước 2: Send `"BX1,DKP,987.3Gr"` **trước**, rồi mới Send `<MainQR>` (sản phẩm mà master-data tính ra BX khác BX1, ví dụ BX2) | Sau QR2: `_boxTypeQR=BX1`, `_boxWeightQR=987.3`, không exception, không đổi `_scanDataWeight`/UI khác `labQrScale.Text`. Sau Main QR: `_scanDataWeight.BoxWeight == 987.3` (không phải giá trị suy từ master data), `_boxType == BX1`, UI label box type = "BX1". Debug Output có dòng cảnh báo mismatch (BX1 từ QR2 vs BX2 từ master-data) nhưng **QR2 vẫn thắng** |
| SC9 | QR2 Plastic override | Send `"G250001,1320"` rồi `<MainQR>` (sản phẩm Plastic) | `_boxTypeQR=Plastic`, `_boxWeightQR=1320`; sau Main QR: `BoxWeight==1320`, không cảnh báo mismatch nếu master-data cũng tính ra Plastic |
| SC10 | QR2 khớp master-data — không cảnh báo | Send QR2 với box type/weight ĐÚNG khớp giá trị master-data sẽ tính ra | Không có dòng Debug.WriteLine cảnh báo mismatch; `BoxWeight` vẫn lấy từ QR2 |
| SC11 | **Guard-bypass fix (HIGH) — QR2 đến nhưng Main QR không bao giờ đến** | Tag Monitor: Write `S5 = 1`. Tab Scale: Send QR2 (`"BX1,DKP,987.3Gr"`), sau đó **KHÔNG** Send Main QR, đợi hết `TimerCheckQrScale` giây | `CheckReadQrWeight()` watchdog **vẫn phải bắn** reject đúng như bình thường (`Check_Weight_Result` đổi sang giá trị reject) — vì `DataEvent_EventHandleValueChange` đã early-return cho QR2 TRƯỚC khi set `_readQrStatus[1]=true`/`_scannerIsBussy[1]=true`. **Đây là bug quan trọng nhất vừa fix — nếu thùng này lọt qua mà không bị reject, fix chưa đúng, dừng lại kiểm tra ngay** |
| SC12 | **Không rò rỉ state QR2 sang thùng kế tiếp** | Sau SC8 (thùng A có QR2, pass, đã Write `S6=1`), lặp trình tự chuẩn cho `<MainQR2>` (thùng B, **không** kèm QR2 riêng) | Thùng B phải dùng box weight/type từ **master data** (không dính giá trị `987.3`/`BX1` của thùng A) — verify `_boxTypeQR`/`_boxWeightQR` đã bị reset về `null`/`0` trong `finally` của `BarcodeScanner2Handle` |
| SC13 | Không rò rỉ state khi thùng có QR2 bị reject giữa chừng | Send QR2 cho thùng A, sau đó Send Main QR nhưng là 1 chuỗi khiến `BarcodeScanner2Handle` reject sớm (vd. QR sai định dạng OC) TRƯỚC khi chạm khối override — rồi lặp trình tự chuẩn cho `<MainQR2>` (thùng B) | Thùng B vẫn dùng giá trị master-data, không dính data cũ của A (đường đi khác SC12 — reject sớm thay vì pass) |
| SC14 | QR2 gửi lặp lại nhiều lần trước Main QR | Send QR2 2-3 lần liên tiếp (không xen Main QR) | Mỗi lần chỉ update `_boxTypeQR`/`_boxWeightQR` (giá trị lần cuối thắng), không set `_scannerIsBussy[1]`/`_readQrStatus[1]`, không tăng số lần watchdog spawn |
| SC15 | *(Open question — xem CLAUDE.md open_questions)* Tolerance category không tự sync theo QR2 khi đổi hẳn nhóm Carton↔Plastic | Send QR2 chỉ ra nhóm khác hẳn so với master-data (vd. master-data tính Carton nhưng QR2 là Plastic) | **Biết trước là chưa xử lý**: `lowerToleranceOfBox`/`upperToleranceOfBox` vẫn theo nhánh master-data đã chọn trước override, không re-tính theo nhóm QR2 mới. Ghi nhận lại kết quả thực tế quan sát được (không phải fix, chỉ để xác nhận có xảy ra thật trong test hay không) |

---

## 3. Máy in AnserU2 (Printer tab — HardwareSimulator)

| # | Case | Bước thực hiện | Kỳ vọng |
|---|------|-----------------|---------|
| P1 | Auto ACK — in liên tục nhanh | Bật checkbox "Auto ACK" (giữ N ms mặc định 800), chạy nhiều case SC1 liên tiếp | Mỗi lần app gọi `StartPrint`/`SendDynamicString`, simulator tự ACK rồi tự báo Print Success sau N ms — không cần bấm tay, app không bị treo chờ phản hồi in |
| P2 | Phản hồi thủ công — Fail | Tắt Auto ACK, sau khi app gửi frame in, bấm "Send Fail" kèm mã lỗi tuỳ ý | App xử lý đúng nhánh lỗi in (không crash, log lỗi, không đánh dấu pass) |
| P3 | Mất kết nối máy in giữa chừng | Trong lúc app đang chờ phản hồi in, bấm "Stop Listen" ở tab Printer rồi bấm lại "Listen" | `AnserU2TcpDriver` tự reconnect sau vài giây (cơ chế auto-reconnect có sẵn), không crash app |

---

## 4. Regression nhanh sau khi test xong

- [ ] Chạy lại đúng 1 case happy-path Metal (M1) + 1 case happy-path Scale không có QR2 (SC1) để xác
  nhận 2 luồng cơ bản không bị ảnh hưởng bởi bất kỳ thay đổi nào ở trên.
- [ ] Kiểm tra `tblLog` có đầy đủ dòng cho mỗi bước (barcode receipt, DB lookup, PLC write attempt)
  của toàn bộ case đã chạy — dùng để đối chiếu nếu có case nào kết quả không như kỳ vọng.
- [ ] **Xác nhận `Scale_Stable_Trigger` đã Write về `0` sau case cuối cùng** — nếu quên, thùng kế tiếp
  (kể cả test thủ công sau này) sẽ bỏ qua bước chờ cân ổn định vì `_stableScale` vẫn còn `1` từ trước.
- [ ] Đóng `SSFG.exe` bằng nút thoát thật (không kill process) — xác nhận `FrmScale_FormClosing` chạy
  hết, 2 `DriverTelnet` disconnect sạch (tab Metal/Scale trong HardwareSimulator log "client
  disconnected"), không có exception trong Debug Output lúc đóng.

## 5. Sau khi test xong

Trả `IsTest`/IP config (`IpCognexCamMetal`, `IpCognexCamScale`, `IpPrinter`, `PortPrinter`,
`TimerCheckQrMetal`, `TimerCheckQrScale`) về giá trị production trước khi bàn giao máy — theo đúng
CLAUDE.md §6.2 bước "Reset trước khi commit". Nếu PLC memory simulator dùng chung `Host` với PLC thật
trong `tags.json`, đảm bảo ngắt kết nối simulator trước khi máy PRD kết nối lại vào PLC thật.
