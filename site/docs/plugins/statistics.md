---
title: Statistics plugin
---

# Statistics plugin

The built-in statistics plugin projects transient per-turn and session statistics from normalized agent events. It helps you understand timing, tool activity, and usage without writing extra plugin messages into canonical conversation history.

<figure class="my-4">
  <img class="img-fluid rounded-4 shadow" src="{{site.basepath}}/img/alta-plugin-statistics.png" alt="CodeAlta timeline statistics card contributed by a plugin" loading="lazy">
  <figcaption class="small text-secondary mt-2">Statistics cards are plugin-owned timeline projections replayed from session events.</figcaption>
</figure>

## What it contributes

- Timeline statistics cards for turns and sessions.
- Transient projections that can be replayed from normalized event history.
- A `statistics estimate` command root in the in-session `alta` live tool.

Projection output is not stored as assistant/user conversation content. The session keeps canonical agent events, and the plugin derives the display card from those events.

## Estimate text size

The statistics plugin contributes a `statistics estimate` live-tool command that agents can use to estimate UTF-8 bytes and approximate tokens for a text value. Ask for the outcome in a prompt:

```text
Estimate the size of this proposed release checklist before I attach it to the next prompt.
```

The command returns a JSONL record to the agent with byte count, formatted byte count, estimated token count, and character count. Users do not invoke the live-tool command directly; for the broader model, see [Advanced Agent Workflows](../advanced-agent-workflows.md).

## Disable it

Disable statistics projections and commands with:

```toml
[plugins.statistics]
enabled = false
```
