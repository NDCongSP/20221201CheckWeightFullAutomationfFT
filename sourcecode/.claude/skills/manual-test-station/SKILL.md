---
description: Guide to manually test the WeightChecking scanner stations without real hardware, using IsTest mode and the fake-data blocks in FrmScale_Load. Use when the user wants to verify a code change end-to-end (barcode → pass/fail → DB/PLC write) but doesn't have factory hardware attached, or asks "how do I test this without the PLC/scanner".
---

WeightChecking has no automated test suite (factory-floor WinForms app) — verification is manual, via `IsTest` mode and fake-data injection. Follow this procedure.

## 1. Enable test mode

Set `IsTest = true` in the JSON config (`ConfigJson`, bound to `tblConfig` in the DB, editable via `frmSettings.cs` PropertyGridControl, or directly in the `ConfigJson` column). This bypasses real hardware connections (PLC, scanner SDKs, Modbus) so the flow can run on a dev machine.

## 2. Locate and enable the fake-data block

In `WeightChecking/WeightChecking/frmScaleNewUI.cs`, find `FrmScale_Load` and the region `#region Fake data to debug` (~line 918). Uncomment the relevant simulated calls for the station under test, e.g.:

```csharp
BarcodeScanner1Handle(1, "A129059,...");  // simulate scan at station 1 (Identification)
BarcodeScanner2Handle(2, "A129059,...");  // simulate scan at station 2 (Scale)
GlobalVariables.MyEvent.SensorBeforeWeightScan = 1;  // simulate sensor trigger
```

Use a real barcode format matching what `sp_vProductItemInfoGet` expects — check an existing `tblScanData` row or an existing fake-data literal already in that region for the correct field layout (OC number, box info, etc.) rather than inventing one.

## 3. Drive the scenario

To test a full station pass, simulate in hardware order: "before" sensor → scanner handle call → "middle"/"after" sensor (to release the busy-guard) → PLC pusher write. Check `_scannerIsBussy[n]` manually if the guard doesn't reset — that means the after-sensor simulation is missing or the handler threw before reaching the reset path.

## 4. Verify results

- [ ] UI shows PASSED/FAILED as expected (check the relevant label/panel in `frmScaleNewUI.cs`).
- [ ] `tblScanData` (pass) or `tblScanDataReject` (reject) has the new row with correct weight/deviation values.
- [ ] `tblLog` has entries for each step of the flow (barcode receipt, DB lookup, auto-posting before/after, PLC write attempt).
- [ ] If testing the weight station, cross-check the printed/queued content against `StdGrossWeight` math — use the `weight-logic-auditor` agent if the numbers look off.
- [ ] If auto-posting is involved, confirm `AutoPostingHelper.AutoTransfer` logged both the "Before" and "After" `sp_lmpScannerClient_ScanningLabel_CheckLabel` log entries per DEC-003 (check-in before transfer).

## 5. Reset before committing

Comment the fake-data calls back out before committing — they should not run by default when `IsTest` is off, but leaving them uncommented is a landmine for the next person who forgets to check.
