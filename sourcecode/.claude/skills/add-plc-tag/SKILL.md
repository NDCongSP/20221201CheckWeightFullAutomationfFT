---
description: Steps to add a new Siemens S7 PLC tag to WeightChecking's tags.json and wire it into the Snap7-based read/write code. Use when the user wants to read a new sensor or write a new pusher/output tag on the PLC (S7-1200), not for the Modbus RTU scale (that's a different driver — see StaticClass/ScaleHelper.cs).
---

WeightChecking talks to the Siemens S7-1200 PLC via Snap7 (`Snap7ClientLib`), with tag definitions declared in `WeightChecking/WeightChecking/tags.json` and read at runtime by `PlcSubscriptionManager` (~200ms poll interval, per CLAUDE.md 7.3).

## 1. Add the tag definition

Edit `tags.json`. Each PLC block has a `Name`, `Host`, `Rack`, `Slot`, and a `Tags` array. Add an entry under the correct PLC block:

```json
{
  "Name": "New_Tag_Name",
  "Address": "DB1.DBBnn",
  "DataType": "Word"
}
```

For analog/float values add the extra fields already used by `Sccale_Value` in the file:

```json
{
  "Name": "New_Analog_Tag",
  "Address": "DB1.DBBnn",
  "DataType": "Real",
  "Deadband": 0.01,
  "OffsetValue": 0,
  "GainRate": 1,
  "NumDecimal": 2
}
```

- Pick the next free `DBB` offset — check the existing addresses in the file first, don't reuse or overlap an in-use byte range (`Word` = 2 bytes, `Real` = 4 bytes typically; confirm against the PLC's actual DB1 layout with whoever owns the PLC program in `PLC/SSFG_PLC`).
- `Deadband` suppresses event noise for analog values — only needed for tags read continuously (like scale value), not for discrete sensor/pusher tags.

## 2. Wire it into code

- **Reading a new sensor**: find how an existing sensor tag (e.g. `S_1`, `S_M`) is consumed — subscribe via the PLC subscription/event mechanism, not by polling `tags.json` values directly in a tight loop. Route the new value into the matching `CustomEvents` property (add one in `Events/CustomEvents.cs` following the existing get/set-with-event pattern if none fits) so downstream handlers stay decoupled from the PLC layer.
- **Writing a new pusher/output**: use `WriteData2PlcSeimens("TagName", value)` (uses `PlcRuntime.Tags`) — do not write raw bytes to `GlobalVariables.DataWriteDb1` unless you're extending that specific byte-array path, and if so document which byte index you're using (0=Metal, 1=Scale, 2=Print are already taken).

## 3. Guard the write path

Per RULE-RT-01/RULE-RT-02: PLC writes triggered from a UI event (button click) must not block the UI thread; PLC reads that update UI controls must go through `GlobalVariables.InvokeIfRequired`.

## 4. Verify

- [ ] With `IsTest = false` and a real/simulated PLC reachable, confirm the new tag round-trips (write a value, read it back, or observe the sensor toggle in the PLC test harness under `WeightChecking/S7Client`).
- [ ] Confirm polling doesn't slow down below the 200ms target (CLAUDE.md 7.3) — adding many analog tags with tight deadbands can increase per-cycle read time.
- [ ] Run `update-session-log` skill afterward to log the tag addition in CLAUDE.md's ConfigJson/tags table if it's a durable addition other devs need to know about.
