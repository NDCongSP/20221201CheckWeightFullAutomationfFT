---
description: Update CLAUDE.md's active_context and CHANGELOG sections at the end of a work session, per the project's own maintenance rules (section 4 and 8.4). Use whenever the user says a task is done, asks to "wrap up", "cập nhật CLAUDE.md", or after any non-trivial code change in this repo — CLAUDE.md is the project's single source of truth for cross-session memory and must stay current.
---

CLAUDE.md at the repo root is this project's persistent memory across Claude Code sessions (see its own section 8.4 maintenance table: `active_context` updated every session, `CHANGELOG` entry every commit-worthy change). Do this before ending a session that touched code.

## 1. Update `## 4.1 Active Context`

Rewrite the YAML block in section 4.1 with:
- `current_task`: one or two lines — what was actually accomplished this session (not what was planned; if partially done, say so and why).
- `related_files`: the actual files touched or centrally relevant, each with a short inline comment on its role (follow the existing style — see current entries for the printer TCP migration).
- `blocked_by`: empty string if nothing blocking, otherwise the concrete blocker (e.g. a missing DevExpress design-time assembly, a hardware IP not yet confirmed).
- `next_step`: the next concrete action for whoever picks this up — not a vague "continue work", an actual next step.
- `last_session`: today's date in `YYYY-MM-DD`.
- `open_questions`: only genuinely open questions — remove ones that got answered this session.

## 2. Append a `## 5. CHANGELOG` entry

Add a new dated section **above** the most recent one (reverse chronological), following the existing format:

```
### [YYYY-MM-DD] — Session: <short title>

\`\`\`
[FEAT|FIX|REFACTOR|PERF|TEST|DOCS|CHORE|BREAK]   <File/Module>   — <short description>
\`\`\`

**Chi tiết:**
- <bullet detail on non-obvious root cause, behavior change, or gotcha for the next reader>
```

- Use the correct type tag: `FEAT` = new capability, `FIX` = bug fix, `REFACTOR` = restructuring without behavior change, `PERF` = performance, `DOCS` = documentation-only (like this update itself), `CHORE` = deps/tooling, `BREAK` = breaking change.
- Keep "Chi tiết" focused on *why*, not a restatement of the diff — the diff is in git history, this file is for context git history doesn't carry (root cause, rejected alternatives, gotchas).
- If the session only did research/investigation with no code change, still log it as `[DOCS]` if it corrected a wrong assumption in CLAUDE.md (see the 2026-07-07 printer-migration-verification entry as a precedent) — that prevents the next session from re-investigating the same settled question.

## 3. Cross-check the Decision Log (section 4.2) and ADR table (section 2.4)

If this session made an architectural choice (library swap, protocol change, new pattern adopted project-wide), add a row there too, not just the changelog — the changelog is per-session noise, the Decision Log is the durable "don't propose this again" record.

## 4. Do not

- Do not remove old CHANGELOG entries — they're an append-only log, not a summary to keep short. `active_context` is the only section that gets overwritten each session.
- Do not write vague entries like "fixed bugs" or "updated code" — every entry must let a future reader (or future Claude session) understand the change without re-reading the diff.
