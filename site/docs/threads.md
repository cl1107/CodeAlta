---
title: Threads and Delegation
---

# Threads and Delegation

A CodeAlta thread is a durable work session with provider/model state, prompt history, queue state, session journal, and timeline projections. Threads can be global or project-scoped.

## Global vs project threads

- **Global threads** are useful for coordination across projects: planning, triage, comparing work, or creating project-specific child sessions.
- **Project threads** are scoped to one project. They can attach project files, see same-project thread context, and use project-local configuration from `<project>/.alta/config.toml`.

The sidebar keeps running threads visible even when their tab is closed. Closing a tab does not stop active work, and unsent thread drafts are saved under `~/.alta/saved_prompts/`.

## Starting and reopening work

Open a project with `Ctrl+O`, select a provider/model/reasoning combination, and send a prompt. CodeAlta stores local raw-API session journals under `~/.alta/sessions/yyyy/mm/dd/<session-id>.jsonl`.

When reopening an existing thread, CodeAlta restores history and provider state where the provider supports it. Local raw-API threads can switch providers while idle; Codex and Copilot threads stay locked to their original provider when hidden runtime state cannot be reconstructed safely.

## Busy threads and queues

If a thread is busy, `Enter` queues your prompt instead of losing it. The waiting list appears above the status line.

Queued prompts can be:

- edited before they run;
- repeated;
- deleted;
- cleared with `F10`;
- converted to live steering when the provider supports it.

Use `Ctrl+Enter` with an empty draft to steer the first queued prompt immediately when queued work exists.

## Steering

Steering lets you adjust a running turn without waiting for completion.

```text
Focus on the parser only. Do not refactor unrelated files.
```

If no provider run is active, CodeAlta falls back to a normal send. If the provider does not support live steering, CodeAlta preserves the prompt and queues it for the next turn.

## Compaction

Press `F11` or click the compact button beside the provider/model/reasoning selectors to compact an idle started thread. Manual compaction uses the thread's current provider/model/reasoning configuration and emits visible start/completion notices in the timeline.

Local compaction targets a smaller post-compaction context by default so long sessions can continue without immediately hitting the context limit.

## The in-session `alta` live tool

CodeAlta-managed local-runtime sessions receive an in-process tool named `alta`. It lets an agent inspect projects, inspect sessions, create child sessions, send/queue/steer prompts, activate skills, and use plugin-contributed commands through bounded JSONL responses.

Agents can discover it progressively:

```text
alta --help
alta session --help
alta tool capability list
```

Non-help commands return newline-delimited JSON records headed by an `alta.result` record. The command returns a finite response; it does not keep streaming future activity.

## Delegating work to another session

Use the UI delegation shortcut (`F7`) or let an agent create a child session with the live tool:

```text
alta session create --project CodeAlta --title "Investigate parser" --same-model-as <thread-id> --reasoning low
alta session send <thread-id> --message "Summarize the latest failing test."
alta session queue <thread-id> --message "Run this after the current turn."
alta session steer <thread-id> --message "Focus on the smallest fix."
alta session abort <thread-id> --reason "Superseded by user request"
alta session compact <thread-id>
```

For parent/child delegated work, the default pattern is notification-based: the child final reply or child-run error is forwarded back to the parent thread automatically. Agents should not poll or sleep by default while waiting for a child session.

## Inspecting sessions

Useful diagnostic commands:

```text
alta project list
alta project current
alta session list --project CodeAlta --state all --limit 20
alta session show <thread-id>
alta session status <thread-id>
alta session children <thread-id> --recursive
alta session tail <thread-id> --last 10
alta session metrics <thread-id> --scope last-turn
alta session result <thread-id>
```

Use `tail`, `events`, and `status` for diagnostics or explicit observation. Use small limits and fields to keep tool results compact.

## Provider and model discovery from a session

```text
alta provider list
alta provider list --detailed
alta provider model list --provider codex
alta model list --provider anthropic --contains sonnet
alta model show --model-ref copilot:claude-sonnet-4.6@high
alta model resolve --model-ref codex:gpt-5.5@high
```

Provider/model discovery is id/ref based so copied refs are stable in prompts, session creation, and reports.

## Visibility and provenance

The live tool enforces scope and provenance:

- the global coordinator can inspect local projects and sessions;
- project-scoped sessions can inspect/control their own project and same-project descendants;
- denied cross-project reads or mutations return policy-denied JSONL errors;
- sessions created or controlled by agents/plugins carry `createdBy` and `submittedBy` metadata so sidebars and timelines can reconstruct parent/child relationships after restart.
