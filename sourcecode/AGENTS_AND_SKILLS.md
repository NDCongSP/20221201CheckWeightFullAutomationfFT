# Agents & Skills — Hướng dẫn sử dụng

> Bổ sung cho `CLAUDE.md`. File này mô tả các Custom Subagents (`.claude/agents/`) và Custom Skills
> (`.claude/skills/`) được tạo riêng cho project WeightChecking SSFG, để Claude Code tự áp dụng
> đúng ngữ cảnh của solution (WinForms + DevExpress + EF6/Dapper + Snap7 PLC + Modbus RTU +
> Zebra/Cognex scanner + AnserU2 TCP printer).

---

## 1. Tổng quan cơ chế

- **Agents** (`.claude/agents/*.md`) là các subagent chuyên biệt, được gọi qua tool `Agent` (hoặc
  tự động khi Claude thấy phù hợp). Mỗi agent có ngữ cảnh sạch (không nhớ hội thoại chính), chỉ
  biết những gì viết trong file `.md` của nó cộng với prompt được truyền vào lúc gọi.
- **Skills** (`.claude/skills/<tên>/SKILL.md`) là các quy trình/checklist được nạp vào **chính
  cuộc hội thoại hiện tại** (không tách agent riêng), gọi qua `/tên-skill` hoặc tự động khi Claude
  nhận diện tình huống khớp với phần `description`.
- Cả hai đều **đọc trước `CLAUDE.md`** làm nguồn sự thật — nếu code thực tế khác với mô tả trong
  CLAUDE.md, agents/skills được yêu cầu tin vào code hiện tại và báo lệch, không tự ý sửa
  CLAUDE.md.

---

## 2. Danh sách Agents

| Agent | Dùng khi nào | Loại | Ghi chú |
|---|---|---|---|
| [`hardware-io-reviewer`](.claude/agents/hardware-io-reviewer.md) | Review code vừa sửa liên quan PLC (Snap7), Modbus RTU, scanner Zebra/Cognex, driver máy in AnserU2 TCP | Chỉ đọc (Read/Grep/Glob/Bash) | Kiểm tra RULE-RT-01..05: cross-thread, block UI thread, dispose/cancellation, guard `_scannerIsBussy[]` |
| [`scanner-flow-debugger`](.claude/agents/scanner-flow-debugger.md) | Trace luồng barcode 3 trạm (Metal → Scale → Distribution), debug "vì sao không đọc được QR", "vì sao pusher sai" | Chỉ đọc | Trả lời bằng trace file:line cụ thể, không đoán từ mô tả kiến trúc |
| [`weight-logic-auditor`](.claude/agents/weight-logic-auditor.md) | Sửa/review logic tính `StdGrossWeight`, tolerance, `DeviationPairs`, chọn `BoxWeight`, case `AfterPrinting` | Chỉ đọc | Logic này quyết định pass/fail hàng thật — audit kỹ hơn review thường |
| [`db-sp-explorer`](.claude/agents/db-sp-explorer.md) | Tìm/hiểu stored procedure, EF entity, Dapper query trước khi viết query mới | Chỉ đọc | Không đoán tên cột/tham số — luôn tra code thật |

### Cách gọi

- Để Claude tự chọn: chỉ cần mô tả việc cần làm ("review lại đoạn code vừa sửa ở
  `frmScaleNewUI.cs` liên quan PLC"), Claude sẽ tự đề xuất agent phù hợp.
- Gọi trực tiếp: yêu cầu rõ "dùng agent `hardware-io-reviewer` để review..." hoặc "spawn
  `scanner-flow-debugger` để tìm hiểu vì sao trạm 2 không nhận QR".

---

## 3. Danh sách Skills

| Skill | Dùng khi nào |
|---|---|
| [`add-hardware-connection`](.claude/skills/add-hardware-connection/SKILL.md) | Thêm kết nối phần cứng mới (sensor, PLC event, driver serial/TCP mới) — checklist đầy đủ theo section 7.2 CLAUDE.md |
| [`manual-test-station`](.claude/skills/manual-test-station/SKILL.md) | Test thủ công một trạm scanner bằng `IsTest=true` + fake data trong `FrmScale_Load`, không cần phần cứng thật |
| [`add-plc-tag`](.claude/skills/add-plc-tag/SKILL.md) | Thêm tag PLC Siemens mới vào `tags.json` và nối vào code đọc/ghi Snap7 |
| [`update-session-log`](.claude/skills/update-session-log/SKILL.md) | Cập nhật `active_context` + `CHANGELOG` trong `CLAUDE.md` sau mỗi session — bắt buộc theo section 8.4 |
| [`review-crossthread-safety`](.claude/skills/review-crossthread-safety/SKILL.md) | Quét diff hiện tại tìm vi phạm RULE-RT-01..05 trước khi commit |

### Cách gọi

- Gõ trực tiếp: `/add-hardware-connection`, `/manual-test-station`, `/add-plc-tag`,
  `/update-session-log`, `/review-crossthread-safety`.
- Hoặc mô tả tự nhiên ("giờ cập nhật CLAUDE.md giúp mình", "test thử trạm cân không cần máy cân
  thật") — Claude sẽ tự nhận diện và chạy skill tương ứng nhờ phần `description` trong frontmatter.

---

## 4. Quy trình khuyến nghị cho một task điển hình

```
1. Nhận task → đọc CLAUDE.md (active_context, Decision Log) như thường lệ.
2. Nếu task thêm phần cứng mới        → /add-hardware-connection (checklist trước khi code)
3. Nếu task thêm tag PLC              → /add-plc-tag
4. Code xong                          → gọi agent hardware-io-reviewer để review cross-thread/guard
5. Nếu đụng vào tính toán khối lượng  → gọi thêm weight-logic-auditor
6. Trước khi commit                   → /review-crossthread-safety để quét lại toàn diff
7. Test thủ công                      → /manual-test-station
8. Kết thúc session                   → /update-session-log để ghi active_context + CHANGELOG
```

## 5. Bảo trì

- Khi thêm hardware/protocol mới hẳn (ví dụ đổi PLC brand, thêm trạm thứ 4), cân nhắc thêm agent
  hoặc skill mới tương ứng thay vì nhồi vào agent hiện có — giữ mỗi agent một phạm vi trách nhiệm rõ.
- Nếu một skill/agent trích dẫn số dòng code hoặc tên hàm cụ thể và sau này code đổi chỗ, cập nhật
  lại tham chiếu đó — các file này được viết để tự grep tìm vị trí thật thay vì tin cứng vào số dòng,
  nhưng phần mô tả ngữ cảnh (bảng ADR, luồng vật lý) vẫn nên khớp với `CLAUDE.md`.
- Đây không phải là thứ thay thế `CLAUDE.md` — chúng là các "chuyên gia" và "quy trình" áp dụng
  luật đã định nghĩa trong `CLAUDE.md`, còn `CLAUDE.md` vẫn là nguồn sự thật duy nhất.
