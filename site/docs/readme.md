---
title: User Guide
---

# User Guide

CodeAlta is a terminal workspace for agentic coding. It brings together local project navigation, model-provider setup, prompt attachments, durable session history, agent prompts, delegated work, MCP-connected tools, skills, and trusted plugins behind the `alta` command.

Start with the first pages, then use the workflow and extension topics as references while you work.

<figure class="my-4">
  <img class="img-fluid rounded-4 shadow" src="{{site.basepath}}/img/alta-home.png" alt="CodeAlta home workspace showing the projects sidebar, active session timeline, prompt editor, and provider footer" loading="lazy">
  <figcaption class="small text-secondary mt-2">The main <code>alta</code> workspace keeps projects, sessions, timeline entries, prompt drafting, provider state, agent profile selection, and context usage in one terminal surface.</figcaption>
</figure>

## Start here

1. [Getting Started](getting-started.md): install the tool, launch it, configure the first provider, choose an agent prompt, and send a first prompt.
2. [Model Providers](model-providers.md): understand provider configuration, credentials, models, reasoning settings, and provider testing.
3. [Agent Prompts](prompts.md): use Default and Plan modes, create global/project workflow prompts, and understand prompt override rules.
4. [Workspace and Dialogs](workspace.md): learn the main terminal screen, timeline, prompt editor, file picker, logs, settings, and management dialogs.

## Workflow topics

- [Sessions and Delegation](sessions.md): global vs project sessions, multiple-agent delegation, prompt queues, steering, compaction, notes, and reminders.
- [Advanced Agent Workflows](advanced-agent-workflows.md): how custom prompts can leverage CodeAlta live-tool capabilities for asks, notes, reminders, sessions, MCP, skills, model comparisons, and self-inspection.
- [CodeAlta Principles](principles.md): the efficient, transparent, keyboard-first, session-oriented, provider-agnostic, native .NET, error-aware, and extensible design principles.

## Extensibility and integrations

- [Plugins](plugins/readme.md): built-in plugins, trusted source plugins, plugin management, safe mode, and developer authoring guidance.
- [MCP plugin](plugins/mcp.md): configure MCP servers, manage policy, discover tools, and activate MCP tools for future agent turns.
- [Troubleshooting](troubleshooting.md): logs, broken configuration, plugin startup failures, login flows, shortcut issues, and single-instance behavior.

## What is intentionally not here

This website is end-user documentation. Internal architecture notes, implementation specs, and development plans live in the repository `doc/` folder instead of the public site.
