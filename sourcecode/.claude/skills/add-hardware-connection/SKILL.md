---
description: Checklist workflow for wiring up a new hardware connection (PLC tag, scanner, sensor, serial/TCP device) in WeightChecking so it follows the project's real-time reliability rules (RULE-RT-01..05 in CLAUDE.md). Use when adding a new sensor, a new PLC event subscription, or a new serial/TCP driver to frmScaleNewUI.cs.
---

Follow this checklist end-to-end when adding a new hardware connection to WeightChecking (`frmScaleNewUI.cs` or a new driver class). Do not skip steps — each one maps to an incident class this codebase has already hit once (see CLAUDE.md CHANGELOG).

## 1. Register lifecycle correctly

- [ ] Subscribe to the hardware event in `FrmScale_Load` (or the relevant init method), not scattered across other handlers.
- [ ] Unsubscribe the same event in `FrmScale_FormClosing` — check `-=` matches the `+=` signature exactly.
- [ ] If the connection involves a polling loop, create a `CancellationTokenSource`, store it as a form field, and in `FrmScale_FormClosing`: `Cancel()` → `Task.Wait(1000)` (bounded, never unbounded) → `Dispose()` in a `finally`.

## 2. Never touch the UI thread directly from the hardware callback

- [ ] Every line in the callback that reads/writes a WinForms control goes through `GlobalVariables.InvokeIfRequired(control, () => { ... })` — never a bare `control.Invoke(...)`.
- [ ] Any `Thread.Sleep`, synchronous DB call, or blocking socket read in the callback runs on a background thread/Task, never inline on a UI-thread-owned callback.

## 3. Wrap the whole handler in try/catch

- [ ] The entire callback body is inside a try/catch so a single malformed frame/read can't crash the app.
- [ ] The catch block writes a `tblLog` entry with station name, barcode (if applicable), and timestamp — not just `Debug.WriteLine`.

## 4. Busy-guard discipline (only if this connection gates a scanner station)

- [ ] If this hardware event is part of the 3-station scanner flow, its guard flag (`_scannerIsBussy[n]`) is set at the start of processing and reset ONLY by the corresponding "sensor after" event — never by a timer or unconditionally in `finally`. See the `scanner-flow-debugger` agent for the exact station→guard mapping.

## 5. Resource cleanup

- [ ] `TcpClient` / `SerialPort` / Snap7 client / Modbus master instances are closed and disposed both on reconnect (before creating a new one) and on form close.
- [ ] Reconnect logic has a cooldown (see the Modbus RTU `RECONNECT_COOLDOWN_MS` fix in CLAUDE.md CHANGELOG 2026-05-25) — don't let a tight reconnect loop block the polling thread.

## 6. Config-driven, not hardcoded

- [ ] IP/port/COM-port/timeout values are read from `GlobalVariables.ConfigJson` (bound to `tblConfig` via `PropertyGridControl` in `frmSettings.cs`), with a sane default via property initializer — not hardcoded in the handler.

## 7. Verify

- [ ] Set `IsTest = true` in config and confirm existing flows still work with the new connection wired in but hardware absent (should degrade gracefully, not throw on startup).
- [ ] Run through the `manual-test-station` skill to simulate the affected station end-to-end.
- [ ] After finishing, run the `update-session-log` skill to record the change in CLAUDE.md.
