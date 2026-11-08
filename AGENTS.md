# AGENTS.md — AI Agent Onboarding <!-- TAUSIK:GENERATED-SHA256:2e1f30a19b9d348940c03077c5a6311d7c4d0dd5986d210266a97627e0d3c06e -->

You are an AI agent working on this project.

## Project: my-project

Stack: not detected
Framework: [TAUSIK](https://github.com/Kibertum/tausik-core) — AI agent governance implementing [SENAR v1.5](https://senar.tech)

**WHETHER THE RULES BELOW ARE ENFORCED DEPENDS ON THE HOST READING THIS FILE.** This file is host-agnostic, so it cannot answer that; `.tausik/tausik doctor` prints the coverage measured on disk per host. On a host where TAUSIK deployed no real-time mechanism, everything below is instructions, not checks: the gates still exist in the CLI (`.tausik/tausik verify`, `task done`) and you reach them by running them.

## Hard Constraints

- **No code without a task.** Run `task start <slug>` before any Write/Edit. No exceptions. (SENAR Rule 9.1)
- **QG-0 Context Gate.** `task start` requires goal + acceptance_criteria; work that changes behaviour names at least one negative scenario, work that changes only prose does not. Set both before starting.
- **QG-2 Implementation Gate (Verify-First v1.4).** Heavy gates (pytest, tsc, cargo, phpstan, …) live on a separate `verify` step. Sequence: run `tausik verify --task <slug>` once everything is in place — it caches a green; then `task done --ac-verified` looks the cache up and closes the task in milliseconds. If the cache is missing or stale → `task done` blocks with the explicit remediation command. Opt-out for CI: `.tausik/config.json` → `{ "task_done": { "auto_verify": true } }` (legacy inline behavior).
- **No commit without gates, and verification is PROPORTIONATE to the change.** Gates run automatically — fix blocking failures before committing. Run the scoped `verify` for what you touched; the full suite belongs in CI and at the release gate, not after every local step. A test asserts behaviour — never a number in a document, never that a generated file is fresh (whatever moves its source regenerates it). One test that goes red on the defect beats a suite that goes red on bookkeeping.
- **No direct DB access.** MCP tools and the CLI keep the projections and the audit trail in step; raw SQLite writes go past both.
- **Don't guess CLI arguments.** Run `.tausik/tausik <cmd> --help` or read the CLI reference.
- **MCP-first.** Prefer MCP tools (`tausik_*`) over CLI when equivalent.
- **Git: ask before commit/push.**
- **Max 500 lines per file.** Filesize gate warns. Exceptions: tests, generated code.
- **Continuous logging.** Run `task log <slug> "message"` after every meaningful step. (SENAR Rule 9.4)
- **Document dead ends.** Run `.tausik/tausik dead-end "approach" "reason"` on failed approaches. (SENAR Rule 9.4)
- **Checkpoint when the signal says so.** The count of calls since the last handoff is derived from the ledger and advised in tool responses. (SENAR Rule 9.3)
- **Context pressure is a signal, not a gate.** Session time and call capacity advise a `/checkpoint` or a handoff; they never refuse a task start (decision #376; basis: `docs/en/session-active-time.md`, SENAR 1.5 §9.4(c)).

## Workflow (minimal tier)

`/start` → `/plan` or `task start` → implement → `.tausik/tausik verify --task <slug>` →
`task done --ac-verified` → `/end`.

Full diagram: [Workflow](docs/en/workflow.md) (or `docs/ru/workflow.md`).

## Memory (minimal)

- **Project memory** — "here it is done this way": `memory add` (SQLite `.tausik/tausik.db`).
- **Shared knowledge** — "the tool is built this way", true beyond this project: `memory add --global`
  (`~/.tausik-knowledge/knowledge.db`; not redacted — never a secret or a client name).
- **Agent auto-memory** — the user's own prefs: host-specific (`~/.claude/` is Claude-only — see glossary).
- **Memory-first:** `memory_search` BEFORE asking the user for / guessing an
  established project fact (hosts, env, paths, decisions). Store env facts as `context`.
- **Routing litmus:** would another agent, in another tool, need this to work on
  THIS project? Then `memory add`; true of the tool beyond this project? `memory add --global` —
  never your host's own memory. Foreign sinks (`~/.claude/**/memory/`, `.cursor/rules/`,
  `.github/copilot-instructions.md`, `.aider*`, …) are blocked by the `memory_route` gate.

## Compaction (minimal)

Carry forward verbatim through any compaction: the active task and slug, the declared
scope and verify receipt, this session's measurements, retired rules, owner prohibitions,
open forks. Full contract: `context_tier: standard`.

## Commands (minimal)

```bash
.tausik/tausik status
.tausik/tausik verify --task <slug>
.tausik/tausik task done <slug> --ac-verified
.tausik/tausik task log <slug> "…"
```

Full CLI: [docs/en/cli.md](docs/en/cli.md).
Skills remain discoverable in [docs/en/skills.md](docs/en/skills.md); MCP tools and
their parameters remain discoverable in [docs/en/mcp.md](docs/en/mcp.md). Load the
specific skill or tool description when the task needs it.

## Answer shape

- Use user's language.
- SHAPE, omit empty: done → verified by → left → your call.
- PROSE (no ASD-STE100 claim): name actor/action; active voice if natural; one action/sentence; one term/concept; short paragraphs.
- BYTE-EXACT: code, shell commands, tool output, file paths, error messages; FULL: acceptance-criteria evidence, decisions, SPEC/ADAPT, task logs, handoffs.
- EXCEPTIONS: explanation asked; destructive action; 3 failed debug turns → assumption + question; ambiguity → one question; rule deletes answer itself.
- Steps: numbered, one action each, last ≤2 min; ≤5/group unless more needed; tangent last; estimate if useful.
- PRE-SEND: delete announcements, recaps, side branches, empty hedges; first line = next action; last = current state.

## Rule pack size

This body was generated with **`context_tier: minimal`** (`.tausik/config.json`). Switch to
`standard` or `full` and re-run TAUSIK bootstrap / refresh for long-form tool routing, full
SENAR tables, and skill/role sections.

<!-- DYNAMIC:START -->
## Current State
Session: none | Branch: master | TAUSIK: 1.11.7
Tasks: 0/0 done, 0 active, 0 blocked
<!-- DYNAMIC:END -->
