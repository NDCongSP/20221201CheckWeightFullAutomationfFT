---
description: Scan changed or specified C# files in WeightChecking for cross-thread UI violations, UI-thread blocking calls, and other RULE-RT-01..05 violations from CLAUDE.md section 7.1. Use before committing changes to frmScaleNewUI.cs or any hardware event handler, or when the app hangs/deadlocks/throws a cross-thread InvalidOperationException.
allowed-tools: Read, Grep, Glob, Bash(git diff*), Bash(git status*)
---

Run this scan on the current diff (or a file the user names) against the project's real-time reliability rules:

```
RULE-RT-01: No blocking I/O (DB, PLC, Serial) on the UI thread — must run on a background thread.
RULE-RT-02: Cross-thread UI updates must use GlobalVariables.InvokeIfRequired() — never a bare control.Invoke().
RULE-RT-03: Every long-running Task has a CancellationTokenSource, cancelled in FormClosing, disposed in finally.
RULE-RT-04: No Thread.Sleep() on the UI thread — use Task.Delay() or move the wait to a background thread.
RULE-RT-05: _scannerIsBussy[] guards are reset only by the matching hardware sensor-after event, never by a timer.
```

## Steps

1. Get the scope: `git diff --name-only` (or `git diff` for the actual hunks) if no file was specified; otherwise use the named file(s).
2. For each changed C# file that touches `frmScaleNewUI.cs`, `Class/`, `StaticClass/`, or any file with an event handler / callback (`OnBarcodeEvent`, `DataEvent_...`, `EventHandle...`, PLC/Modbus callbacks):
   - Grep for `Invoke(` / `BeginInvoke(` not wrapped in `GlobalVariables.InvokeIfRequired` → RULE-RT-02 violation.
   - Grep for `Thread.Sleep(` → check which thread the enclosing method runs on; flag if it's a UI event handler or anything called synchronously from one → RULE-RT-04.
   - Grep for new `Task.Run(`/`Task.Factory.StartNew(` → confirm a `CancellationTokenSource` is threaded through and disposed in `FrmScale_FormClosing` → RULE-RT-03.
   - Grep for `_scannerIsBussy[` assignments → confirm the reset site matches the correct sensor-after event for that station (see the `scanner-flow-debugger` agent's station→guard table) → RULE-RT-05.
   - Grep for direct property/control sets (`.Text =`, `.Value =`, `.BackColor =`, etc.) inside anything not already known to run on the UI thread.
3. For anything ambiguous (can't tell which thread a method runs on from local context), trace the call site up one level rather than guessing — state your reasoning for the conclusion.

## Output

Report as a flat list: `file:line — RULE-RT-0X — what will physically happen if unfixed — suggested fix`. If the diff is clean, say so plainly rather than padding the report. This skill only scans/report — if the user wants fixes applied, apply them with Edit only after presenting the findings, unless they've already asked for both in one step.
