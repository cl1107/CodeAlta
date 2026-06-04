# Website agent workflow documentation

- Status: Done
- Plan file: `.alta/plans/2026-06-04-website-agent-workflow-docs.md`
- Created: 2026-06-04
- Task: Improve the public website documentation for CodeAlta extensibility, agent prompts/modes, and advanced `alta` live-tool workflows.
- Git: `.alta/plans/` is not ignored by `.gitignore`; commit this plan with the related documentation work. Preserve unrelated untracked `.alta/config.toml` and `.alta/mcp.json`; the new `site/img/alta-notes.png` and `site/img/alta-plan-mode.png` are user-provided assets intended for this docs work.

## Objective

- Make the website more top-down and user-oriented around recent CodeAlta capabilities: Plan mode, custom agent prompts, MCP servers, sticky notes/reminders, live-tool coordination, sessions/delegation, skills, and plugins.
- Reframe extensibility as a layered story: agent prompts define workflows; the in-session `alta` live tool lets those workflows coordinate CodeAlta; MCP/skills add external or reusable context; plugins remain the trusted-code extension layer, not the whole extensibility story.
- Add one substantial advanced page that lists the available `alta` live-tool command groups and explains how users should prompt agents to leverage them, while making clear that users cannot invoke those live-tool commands directly.
- Update internal docs only where necessary to keep command names/current behavior aligned with the website.
- Non-goals: implement new runtime features, redesign the website theme, create many small reference pages, document unimplemented MCP features, or turn the public site into internal architecture documentation.

## Context and evidence

- `site/AGENTS.md` says the site is end-user documentation, pages require YAML `title`, and navigation changes must update the relevant `menu.yml`.
- `site/readme.md` is the website front page. Its hero mentions providers/sessions/plugins/delegated agents, but it does not yet prominently list Plan mode, custom agent prompts, MCP servers, notes/reminders, or live-tool-based workflows. Its final principle card is currently focused on “Plugin support”.
- `site/docs/principles.md` currently summarizes principles as “Efficient. Transparent. Keyboard-first. Session-oriented. Provider-agnostic. Native .NET. Error-aware. Pluggable.” and ends with a “Plugin support” principle; this is the best place to broaden the principle to “Extensible workflows”.
- `site/docs/prompts.md` already contains the right base article for agent prompts: system-vs-agent prompts, built-in Default/Plan prompts, global/project prompt roots, frontmatter, selection/editing, and templates. It should be expanded/renamed rather than split into a tiny new page.
- Built-in prompt frontmatter confirms current user-facing mode descriptions: `src/CodeAlta.Orchestration/content/prompts/agents/default.prompt.md` describes Default as normal implementation/build mode and approved plan execution; `src/CodeAlta.Orchestration/content/prompts/agents/plan.prompt.md` describes Plan as read-only planning that writes `.alta/plans/` files before Default handoff.
- `site/docs/sessions.md` already explains the in-process `alta` live tool, child sessions, reminders, and notes for daily delegation, but it does not provide a complete advanced command atlas.
- `site/docs/plugins/mcp.md` documents MCP setup, prompt-based management, activation, and the MCP dialog, but because it lives under Plugins it can make MCP feel like only a plugin topic unless cross-linked from broader workflow/extensibility docs.
- `alta --help` and `alta tool capability list` show current live-tool roots/capabilities to document at a high level: `version`, `ask`, `notes`/`note`, `project`, `session`, `reminder`, `skill`/`skills`/`skills_activate`, `tool`, `provider`, `model`, `prompt`, `plugin`, plugin-contributed `mcp`, and plugin-contributed `statistics`.
- `alta mcp --help` includes `mcp activate`, `list`, `status`, `config`, `server`, and `tool`; `doc/live-tool.md` should be checked/updated because its MCP command examples currently risk omitting `activate`.
- User-provided images `site/img/alta-plan-mode.png` and `site/img/alta-notes.png` are available and should be used if they render well with descriptive alt text.

## Assumptions and open decisions

- Assumption: keep `site/docs/prompts.md` as the primary “Agent Prompts” article, updating its title/menu label, instead of creating a second small agent-prompt page.
- Assumption: add a single page `site/docs/advanced-agent-workflows.md` titled “Advanced Agent Workflows” for live-tool-driven workflows; avoid an `advanced/` folder until there are multiple substantial advanced topics.
- Assumption: the user-provided screenshots are suitable for public docs. If one is visually outdated or redundant during implementation review, omit it rather than forcing it into the page.
- Assumption: command-group coverage should be complete at the root/group level, not a full option-by-option CLI manual; detailed options remain discoverable through live-tool help and internal docs.
- No blocking open decisions. Naming such as “Advanced Agent Workflows” and “Extensible workflows” can be adjusted during review without changing the implementation shape.

## Design notes

- Information architecture:
  - Keep the start path simple: Getting Started → Model Providers → Agent Prompts → Workspace → Sessions → Advanced Workflows.
  - Add the advanced page as one menu entry in `site/docs/menu.yml` near Sessions; rename the existing prompts menu label from “Prompts and Instructions” to “Agent Prompts”.
  - Keep Plugin pages under `site/docs/plugins/`, but cross-link them from the broader extensibility narrative.
- Front page:
  - Add a compact “latest capabilities” or “what you can build with CodeAlta” section/cards linking to Plan mode/custom prompts, MCP, advanced workflows/live tools, sessions/delegation, and plugins/skills.
  - Update principle copy from plugin-centered wording to extensibility-centered wording while still linking to plugin docs.
- Principles page:
  - Change the summary word “Pluggable” to “Extensible” or “Extensible workflows”.
  - Replace the final “Plugin support” section with a layered extensibility principle that includes agent prompts, the `alta` live tool, MCP servers, skills, and trusted plugins.
- Agent prompts page:
  - Start with the user concept: an agent prompt is a selectable workflow profile for a session.
  - Explain Default vs Plan mode in a concise table, including Plan’s `.alta/plans/` output and review/handoff workflow.
  - Use `site/img/alta-plan-mode.png` near the Plan mode explanation if suitable.
  - Add a practical “create your own prompt” flow for global and project scopes, covering file paths, frontmatter, body guidance, descriptions in model context, selecting prompts, and project overrides.
  - Keep system prompts/templates as advanced subsections, with a caution that most workflow customization belongs in agent prompts.
- Advanced workflows page:
  - Explain that the `alta` live tool is for CodeAlta-managed agents and trusted plugins; users should ask for outcomes in prompts, not paste low-level tool commands into a shell.
  - Include a command-group table with: group/aliases, what the agent can do, example user prompt, and safety notes. Cover all roots from `alta --help` / `alta tool capability list`, including plugin-contributed `mcp` and `statistics`.
  - Include recipes for plan review with `ask`, visible progress with `notes`, delayed checks with `reminder`, child sessions with `session`, prompt/mode switching with `prompt` and `session set_agent`, MCP activation/discovery, skills activation, provider/model comparison, and project/session self-inspection.
  - Use `site/img/alta-notes.png` in the notes/progress recipe if suitable.
  - Use GitHub enhanced blockquotes (`[!TIP]`, `[!NOTE]`, `[!WARNING]`, `[!IMPORTANT]`) to distinguish practical tips, two-turn MCP activation, live-tool mutability, and Plan mode’s prompt-enforced nature.
- Existing topic pages:
  - Keep `site/docs/sessions.md` focused on daily session/delegation workflows and link to the advanced page for the full command atlas.
  - Add a short extensibility note to `site/docs/plugins/readme.md`: prefer agent prompts/live tools/MCP/skills for workflow/context extensibility; use plugins when trusted .NET code must extend the host.
  - Add cross-links from `site/docs/plugins/mcp.md`, `site/docs/getting-started.md`, and `site/docs/workspace.md` only where they improve navigation; avoid repeating the full advanced content.
- Internal docs:
  - Update `doc/live-tool.md` only if implementation confirms drift from current live-tool help, especially `mcp activate` and plugin command roots such as `statistics estimate`.
  - Avoid broad internal-doc churn unless public docs reveal a mismatched implementation contract.

## Risks and challenges

- A full live-tool command atlas can become too low-level for end users. Mitigate by organizing it around “what to ask the agent for” and keeping command names as supporting details.
- Live-tool command availability can vary with plugins and host/runtime context. Mitigate by documenting core vs plugin-contributed roots and recommending `alta tool capability list` / `alta --help` as live evidence for agents.
- Some command groups mutate state (`ask`, `notes`, `reminder`, `session send`, `prompt create/edit`, `mcp server add/remove/enable/disable`, etc.). The advanced page needs clear warnings about scope, credentials, user/project files, and prompt-driven confirmation.
- Plan mode is prompt-enforced, not a host filesystem sandbox. Document this honestly so users do not over-trust it for isolation.
- New screenshots may be large, stale, or visually inconsistent. Use them with useful alt text if they fit; otherwise leave them unused and mention why in the implementation report.
- Homepage changes can easily become marketing-heavy. Keep feature copy concrete and link to task-oriented docs.
- Project rules ask for website build and full build/tests before submitting; docs-only changes should still run the site build and record any broader verification that cannot run.

## Implementation checklist

- [x] Refresh `git status --short` and preserve unrelated user-owned files, especially untracked `.alta/config.toml` and `.alta/mcp.json`; treat the user-provided screenshots as docs assets.
- [x] Inspect `site/img/alta-plan-mode.png` and `site/img/alta-notes.png` enough to choose accurate alt text and decide where each image belongs.
- [x] Update `site/readme.md` so the front page lists recent capabilities (Plan mode, custom agent prompts, MCP servers, advanced/live-tool workflows, notes/reminders, sessions/delegation, skills/plugins) and reframes the last principle from plugin-only to extensibility.
- [x] Update `site/docs/menu.yml` and `site/docs/readme.md` to rename the prompt topic to “Agent Prompts”, add the advanced workflow page, and reflect the revised reading path/latest capabilities.
- [x] Rework `site/docs/principles.md` from “Pluggable / Plugin support” to an “Extensible workflows” principle covering agent prompts, live-tool coordination, MCP, skills, and plugins.
- [x] Expand `site/docs/prompts.md` into the main “Agent Prompts” article: concept, Default vs Plan mode table, Plan screenshot if suitable, global/project prompt authoring, frontmatter, selection/override rules, best practices, and system prompt/template cautions.
- [x] Add `site/docs/advanced-agent-workflows.md` as one substantial advanced page explaining the `alta` live tool, complete command-group coverage, prompt-oriented recipes, safety notes, and notes/progress screenshot if suitable.
- [x] Update `site/docs/sessions.md` to keep daily delegation guidance but link readers to the advanced workflow page for the full live-tool command atlas and deeper recipes.
- [x] Update `site/docs/plugins/readme.md` to position plugins as one trusted-code extension layer among prompts, live tools, MCP, and skills; preserve the existing trust/preview warnings.
- [x] Update `site/docs/plugins/mcp.md` with targeted cross-links to the new advanced workflow page and agent-prompt docs where they clarify prompt-based MCP management/activation.
- [x] Update `site/docs/getting-started.md` and `site/docs/workspace.md` only as needed for navigation and brief Plan/custom prompt discoverability; avoid duplicating the expanded prompt/advanced pages.
- [x] Update `doc/live-tool.md` and, if needed, `doc/readme.md`/`doc/runtime.md` for any command-name or behavior drift discovered while writing the public docs.
- [x] Self-review the documentation organization for top-down flow, page size balance, user-oriented examples, cross-links, enhanced blockquotes, image alt text, and absence of undocumented future behavior.

## Verification checklist

- [x] Re-run live-tool discovery used by the docs: `alta --help`, `alta tool capability list`, `alta session --help`, `alta prompt --help`, `alta reminder --help`, `alta mcp --help`, and `alta statistics --help`; adjust command-group wording if output differs.
- [x] Run `git diff --check` from the project root.
- [x] Run `lunet build` from `site` and fix any Markdown/frontmatter/link/image generation problems.
- [x] Run `dotnet build -c Release` from `src` per project pre-submit guidance, or record why it could not be run for docs-only work.
- [x] Run `dotnet test -c Release` from `src` per project pre-submit guidance, or record why it could not be run for docs-only work.
- [x] Manually inspect generated/previewed pages for broken local links, oversized pages, duplicated content, screenshot fit, and readability of GitHub enhanced blockquotes.
- [x] Final `git status --short` review to ensure only intended docs, images, and the plan file changed; unrelated `.alta/config.toml`/`.alta/mcp.json` remain untouched.

## Handoff notes

- Execute this plan in Default/build mode before editing website or internal docs.
- Keep the documentation user-facing: explain outcomes and prompt examples first, raw live-tool command syntax second.
- Do not document the `alta` live tool as a normal terminal command surface for users; state that it is model/plugin-invoked, while users prompt for the desired operation.
- Preserve existing plugin/MCP detail where correct, but add cross-links so readers understand MCP and plugins as part of a broader extensibility system.
- Because `.alta/plans/` is versioned in this checkout, include this plan file with the related docs commit if a commit is made.
- Verification note: the first full `dotnet test -c Release` run hit a transient Windows file-lock cleanup failure in `AltaLiveToolTests.ReminderCreate_DefaultsToCallerSessionAndDispatchesContentLater`; the targeted retry and a full `dotnet test -c Release --no-restore` rerun passed.
