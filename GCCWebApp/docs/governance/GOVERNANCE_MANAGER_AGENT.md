# Governance Manager Agent — Technical Documentation

**Audience:** Dev / tech ops working with the GCC Database Solution.
**Owner:** Anuki Consulting.
**Last updated:** 2026-06-01.

## TL;DR

When you have **two or more work streams running in parallel** (multiple Claude Code terminals, multiple agents, or human + AI concurrently editing related code), invoke the **Governance Manager** agent to:
1. Inventory what each stream produced.
2. Detect overlaps, conflicts, and interface drift.
3. Verify each stream against the documented plan and project conventions.
4. Audit tracking-doc adherence (DEF register, observations log, shipped log).
5. Produce a single reconciled status report.

The agent **does not write product code**. It owns *coordination, verification, and tracking* — not feature delivery.

## Why this agent exists

This project routinely has parallel work in flight:
- **Sub-agents** (`frontend-specialist`, `security-specialist`, `ux-design-compliance`, etc.) operating in parallel on the same change.
- **Multiple terminals** with the user running one Claude Code session while another agent works on related code.
- **Asynchronous human edits** — the user (Shawn) sometimes edits files directly while an agent has work in flight (the smoke template `requestedIsAuthPage` hardening on 2026-06-01 is a good example: Shawn improved the template while Day 2 view smokes were being built around it).

Without coordination, parallel work produces three failure modes:
1. **Duplication.** Two streams build the same artifact, slightly differently. Neither is wrong; both took effort. Net waste.
2. **Silent conflict.** Two streams modify the same file. The later edit wins, the earlier work disappears.
3. **Interface drift.** Stream A changes a function signature; stream B kept calling the old signature. Build green, runtime broken.

The Governance Manager is a **read-and-reconcile** agent designed specifically for these failure modes.

## When to invoke

Invoke proactively in any of these situations:

| Situation | Why |
|---|---|
| User mentions "another terminal" / "parallel work" / "concurrent" | Active parallel risk. Run early, before drift compounds. |
| Pre-deploy bundling ("bundle everything for the next Plesk push") | Highest-cost moment to discover a tracking gap. Run as a deploy gate. |
| End of a multi-agent session | Cross-stream interface check before declaring done. |
| "What's the state of X?" across recent sessions | Inventory + reconcile is exactly the question. |
| Stale tracking-doc suspicions ("did we ever log this?") | Tracking-doc audit is in scope. |

Do **not** invoke for:
- Single-stream work (one agent, one terminal — no coordination needed).
- Code feature work (the Governance Manager refuses; dispatch a feature agent instead).
- Real-time bug investigations (too narrow; just debug directly).

## What it produces

A structured status report (always in this exact shape so it's scannable):

```markdown
## Governance status — YYYY-MM-DD

### What's in flight
| Stream | Artifact | Status | Aligned |
|---|---|---|---|
| ... | ... | shipped / pending / blocked | yes / no — reason |

### Conflicts / overlap
- (none) — or — file path: A wins because X; file path: needs merge — Y

### Plan vs reality gaps
- Plan says X is done; actual state is Y
- Working-tree change Z has no DEF row — recommend DEF-NNN

### Recommended next steps (ordered)
1. ...

### Tracking doc updates I made this session
- defect-register.md: added DEF-NNN
- shipped-2026-MM.md: appended entry for X
- observations_log.md: appended bullet for Y
```

## Inputs the agent expects

- A description of which streams are running (e.g., "Claude in terminal A is building the smoke library; I'm in terminal B writing the view smokes").
- Optional: a specific question to answer ("are there conflicts?" / "is everything tracked?" / "are we ready to deploy?").

If you don't provide stream context, the agent will infer from `git status`, working-tree changes, and recent edits — but it'll work harder and the report will be less precise. **Tell it what's in flight.**

## Outputs / side-effects

The agent **may** edit these files (always tracking, never product code):
- `docs/tracking/defect-register.md` — add a DEF row for a finding (next unused DEF-NNN).
- `docs/tracking/shipped-2026-MM.md` (current month) — append a client-readable shipped entry.
- `observations_log.md` — append a dated bullet entry for the audit findings.
- `docs/tracking/e2e-smoke-plan.md` and similar plan docs — append progress-log entries.

The agent **will not** edit (without explicit user permission):
- `Controllers/`, `Services/`, `Models/`, `Views/`, `wwwroot/` — product code.
- `Program.cs`, `appsettings*.json`, `*.csproj` — config / infra code.
- Run `git commit`, `git push`, or any destructive git operation.

If the agent discovers a code-level defect during audit, it logs a DEF row with status `Identified during governance audit — pending owner` and tells you. **You decide whether to dispatch a feature agent** (or do it yourself) to fix it.

## How it interacts with project memory

The agent reads the auto-memory at `C:\Users\swest\.claude\projects\<projkey>\memory\MEMORY.md` to ground every audit in established preferences. Specifically it leans on these `feedback_*` memories:

| Memory | How the agent uses it |
|---|---|
| `feedback_qc_review_no_changes` | Confirms audit ≠ code edits. |
| `feedback_track_every_fix_in_defect_register` | Every working-tree change must have a DEF row. |
| `feedback_def_entries_note_qc_id` | DEF rows must cite originating QC #. Flags rows that don't. |
| `feedback_data_issue_vs_defect` | Filters out data-only "issues" that shouldn't get DEF rows. |
| `feedback_scoping_check_adjacent_endpoints` | When auditing a fix, looks for missed siblings (the DEF-233 surface in Day 1 was exactly this). |
| `feedback_no_commits` | Refuses to commit even if asked. |
| `feedback_continue_means_execute` | When you say "go," it picks the highest-value next action — no menu. |
| `feedback_no_chaos_rabbit_holes` | Caps diagnosis depth; if a finding gets deep, it's logged as a DEF and the audit continues. |

If a memory rule is missing and the agent encounters a recurring pattern, it will recommend adding the rule rather than just acting on it once.

## Worked example — Day 1 of the E2E Smoke Plan (2026-06-01)

This is the kind of run the agent was designed for.

**Setup.** Shawn was running one terminal building `_lib/smoke-template.js` + `_views/home.js` (Day 1 reference impl per `docs/tracking/e2e-smoke-plan.md`). Another terminal was building the remaining 9 Tier 1 view smokes plus a `run-all.js` orchestrator. Both terminals were editing files in `Scripts/smoke-prod/`. Without coordination this is the classic silent-conflict surface.

**What the Governance Manager would do (if invoked at end of Day 1):**

1. **Inventory.**
   - Stream A produced: `_lib/smoke-template.js` (Day 1 reference) + `_views/home.js` + `_results/` directory.
   - Stream B produced: `_views/login.js`, `_views/cert-tile-public.js`, `_views/cert-tile-external.js`, `_views/cert-tile-internal.js`, `_views/cert-create.js`, `_views/cert-edit.js`, `_views/dashboard.js`, `_views/admin-user-mgmt.js`, `_views/admin-tags.js`, `_views/run-all.js`.
   - Both touched: `_lib/smoke-template.js` (Stream A built it; Stream B hardened it with the `requestedIsAuthPage` check needed for the login smoke).

2. **Conflicts / overlap.**
   - File `_lib/smoke-template.js` was edited by both streams. The hardening (`requestedIsAuthPage`) is layered on top of Stream A's original, no merge conflict — Stream A's base was preserved.
   - Both streams produced a `home.js`. Stream B's version refactored Stream A's into the `module.exports = { run }` + `if (require.main === module)` pattern used by `run-all.js`. Stream B's wins because it's compatible with the orchestrator; Stream A's design intent is preserved.

3. **Plan vs reality gaps.**
   - Plan said Day 2 was "not yet started." Reality: Day 2 is done. Update `docs/tracking/e2e-smoke-plan.md` progress log.
   - DEF-233 (pageUnloading gap discovered during Day 1) is in the defect register but not in the June shipped log. Recommend appending.

4. **Tracking doc updates.**
   - `defect-register.md`: DEF-233 row already present.
   - `e2e-smoke-plan.md`: Day 2 progress entry missing → append.
   - `shipped-2026-06.md`: DEF-233 not yet recorded → append.
   - `observations_log.md`: Day 2 completion bullet missing → append.

5. **Recommended next steps.**
   1. Run `node Scripts/smoke-prod/_views/run-all.js` against localhost to confirm 10/10 green.
   2. If any view FAILs, log per-view DEFs and decide which to fix before deploy.
   3. Update the plan doc with the verification result.

This is the kind of clean closure that **is the agent's deliverable**. It replaces 30-40 minutes of "what did we do" reconciliation with a 5-minute structured pass.

## Architecture decisions

These are the choices baked into the agent definition. They're explicit so future maintainers know what's load-bearing.

| Decision | Why |
|---|---|
| **Read-only on product code** | Lets two agents work in parallel safely — Governance Manager won't race the code-writing agent. The cost is one extra hop (governance flags, code-writer fixes). |
| **Tracking-doc edits are in scope** | Tracking debt accumulates fast in this project. Letting governance close tracking gaps directly is the point — otherwise it's the same diffusion problem the agent exists to solve. |
| **Sonnet model (not Opus)** | The work is structured inventory + verification, not deep reasoning. Sonnet is fast and cheap enough to run on every parallel-work session. Upgrade to Opus only if the audit finds something it can't classify. |
| **Status report has a fixed shape** | Scanability for Shawn. Free-form prose summaries get skipped. The same template every time means the user can find what they need in seconds. |
| **No git commits, ever** | Hard memory rule (`feedback_no_commits`). Shawn commits himself. |
| **Cap diagnosis at ~5 tool calls before falling back to "log as DEF, continue"** | Hard memory rule (`feedback_no_chaos_rabbit_holes`). The agent's value is breadth, not depth. If a finding needs depth, that's a separate engagement. |

## Maintenance — when to update the agent

Update `.claude/agents/governance-manager.md` when:
- A new tracking doc joins the canonical set (e.g., a new monthly shipped log). Update the "Project context" section.
- A new `feedback_*` memory rule emerges that should constrain governance behavior. Add it to the "memory rules that govern YOUR behavior" list.
- The status-report template changes shape (rare — only do this if Shawn explicitly asks).
- A new failure mode appears that the agent missed. Add it to the "Core operations" section.

Avoid:
- Adding code-writing capabilities. If you find yourself wanting to, that's a sign you need a different agent (frontend-specialist, security-specialist, etc.), not an expansion here.
- Loosening the tracking-doc audit rules. The whole point is to keep tracking honest.

## Invocation

From the main Claude Code conversation:

```
Use the governance-manager agent to <audit X, reconcile Y, etc.>
```

The agent definition includes proactive-invocation examples — Claude Code will offer to use it when parallel work is mentioned. You can also invoke it explicitly via `Task` tool in scripted contexts.

## Files

- Agent definition: `.claude/agents/governance-manager.md` (this is what Claude Code loads).
- Tech doc: `docs/governance/GOVERNANCE_MANAGER_AGENT.md` (this file — for humans).
- Project memory the agent reads: `C:\Users\swest\.claude\projects\<projkey>\memory\MEMORY.md`.
- Canonical tracking docs (what the agent audits against): `docs/tracking/defect-register.md`, `docs/tracking/shipped-2026-MM.md`, `docs/tracking/e2e-smoke-plan.md`, `observations_log.md`.

## Open questions / future work

- **Multi-project version.** This agent is GCC-specific (file paths, conventions). If we end up with a second project using the same playbook, factor the "memory rules + tracking doc layout" into a reusable spec.
- **CI integration.** Could the governance audit be run as a pre-deploy hook (`gh workflow run`)? Open question — adds complexity vs. on-demand invocation, which is currently sufficient.
- **Status-report archival.** Status reports are currently inline in chat. Worth archiving them to `docs/governance/audits/YYYY-MM-DD.md`? Probably yes once we have >5 of them, to spot trends in tracking gaps over time.
