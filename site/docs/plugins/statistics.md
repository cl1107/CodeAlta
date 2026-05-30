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

Use the live-tool command to estimate UTF-8 bytes and approximate tokens for a text value:

```sh
alta statistics estimate "Summarize this change."
```

The command returns a JSONL record with byte count, formatted byte count, estimated token count, and character count.

## Disable it

Disable statistics projections and commands with:

```toml
[plugins.statistics]
enabled = false
```
