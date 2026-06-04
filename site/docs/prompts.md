---
title: Agent Prompts
---

# Agent Prompts

Agent prompts are selectable workflow profiles for CodeAlta sessions. They tell the agent how to work: implement directly, plan first, review only, coordinate child sessions, keep visible notes, or follow a project-specific release workflow.

CodeAlta separates prompt-related instructions into a few layers:

{.table}
| Layer | Files | Use it for |
|---|---|---|
| **Agent prompt** | `prompts/agents/<id>.prompt.md` | Session workflow, mode behavior, delegation style, review rules, and when to ask the user. |
| **System prompt** | `prompts/system/<id>.system-prompt.md` | Stable host/agent invariants shared by one or more agent prompts. |
| **Template** | `prompts/template.yml` | Advanced defaults for which generated context sections are included. |

> [!TIP]
> Most customization belongs in an agent prompt. Change a system prompt only when you really need to change stable host-level instructions across workflows.

The active agent prompt is included when a session starts or resumes. Its optional `system` property chooses which system prompt id to use; when omitted, CodeAlta uses `default`.

## Built-in modes

CodeAlta ships two built-in agent prompts:

{.table}
| Mode | Prompt id | Best for | What it does |
|---|---|---|---|
| **Default** | `default` | Normal implementation/build work | Inspects relevant files, edits when implementation is feasible, verifies, self-reviews diffs, reports concrete outcomes, and executes approved plan files. |
| **Plan** | `plan` | Larger or riskier tasks where you want a plan before edits | Researches read-only, writes an implementation-ready Markdown plan under `.alta/plans/`, asks for review, and can hand off to Default when approved. |

<figure class="my-4">
  <img class="img-fluid rounded-4 shadow" src="{{site.basepath}}/img/alta-plan-mode.png" alt="CodeAlta Plan mode session showing a saved plan and review workflow" loading="lazy">
  <figcaption class="small text-secondary mt-2">Plan mode is an agent prompt profile: it researches, writes a plan file, keeps the plan reviewable, and hands off to Default only after approval.</figcaption>
</figure>

> [!IMPORTANT]
> Plan mode is prompt-enforced behavior, not a hard filesystem sandbox. The built-in Plan prompt tells the agent not to mutate project files except for the plan under `.alta/plans/`, but normal host permissions still exist. Use normal review practices for sensitive work.

Typical prompts:

```text
Use Plan mode for this task. Inspect the relevant docs and code, then write an implementation-ready plan before any source edits.
```

```text
Execute the approved plan at .alta/plans/2026-06-04-website-agent-workflow-docs.md.
```

If git is active and `.alta/plans/` is not ignored, the built-in prompts treat plan files as repository artifacts: Plan keeps the saved plan current, and Default keeps it synchronized with implementation progress.

## Source locations and precedence

Prompt resources are layered in this order:

1. Built-in resources shipped with CodeAlta.
2. User-global resources under `~/.alta/prompts/`.
3. Project-local resources under `<project>/.alta/prompts/`.

Each root has the same layout:

```text
prompts/
  system/
    default.system-prompt.md
    my-custom-system.system-prompt.md
  agents/
    default.prompt.md
    plan.prompt.md
    reviewer.prompt.md
  template.yml        # optional advanced defaults
```

If multiple roots contain the same prompt or system id, the later source overrides the earlier one: project overrides global, and global overrides built-in. This lets you keep a global `reviewer` prompt while giving one repository a stricter project-local `reviewer.prompt.md`.

> [!NOTE]
> Prompt ids come from file names. For example, `reviewer.prompt.md` creates or overrides the agent prompt id `reviewer`.

## Create a custom agent prompt

Use a custom prompt when you repeat the same workflow often enough that it deserves a named mode.

1. Open the prompt manager with `Ctrl+G Ctrl+H` or `/prompt`.
2. Choose **Agent Prompts**.
3. Create a **Global** prompt for all projects or a **Project** prompt for the current project.
4. Pick a short lowercase id such as `reviewer`, `triage`, or `release`.
5. Fill in a concise name, description, optional system prompt id, and Markdown body.
6. Select the prompt from the footer **Agent:** selector or cycle prompts with `Ctrl+T` / `/next_prompt`.

A minimal prompt file looks like this:

```markdown
---
name: Reviewer
description: Review-only workflow that inspects diffs and reports risks before code changes.
---
You are the active CodeAlta review agent for this session.

Review the requested change without editing files. Inspect relevant code,
tests, and docs. Report findings first, ordered by severity, with file and
line references when possible. If no issues are found, say so and mention any
verification gaps.
```

`description` is optional but should be decision-useful: CodeAlta surfaces effective agent prompt descriptions in generated model context so agents can discover available workflows and switch or delegate appropriately.

> [!TIP]
> Keep descriptions short and specific, for example “Plan-first release workflow that writes a checklist and asks before publishing.” Avoid long policy text in descriptions; put detailed behavior in the body.

## What to put in a prompt body

Good agent prompt bodies are concrete enough to shape behavior but small enough to stay understandable:

- state the role and scope of the workflow;
- define when the agent should inspect, implement, verify, or stop;
- say when it should ask for structured approval;
- explain whether it may delegate to child sessions;
- describe progress-note expectations when the task is long-running;
- name the smallest meaningful verification for the workflow;
- call out non-goals and safety boundaries.

For advanced workflows, the body can mention CodeAlta live-tool capabilities the model may use, such as session delegation, sticky notes, reminders, or structured asks. Users still prompt for the outcome; the model invokes live-tool commands internally when the selected provider/session supports them. See [Advanced Agent Workflows](advanced-agent-workflows.md) for recipes and command-group coverage.

## Selecting, switching, and editing prompts

Use the **Agent:** selector below the prompt editor to choose the prompt for the current draft/session. Built-in prompts appear first, followed by global prompts and project prompts.

The prompt manager lists built-in, global, and project prompts, shows shadowed overrides, and lets you create, edit, save, or delete global/project prompt files. Built-in prompt and system prompt files are visible for inspection but read-only; create a global or project file with the same id to override one.

Agents can also use the `prompt` and `session` live-tool command groups internally when you ask for prompt automation. For example, you can ask:

```text
List the available agent prompts for this project and recommend which one fits a read-only security review.
```

```text
Create a project agent prompt named release-checklist that plans release steps, keeps notes visible, and asks before publishing.
```

```text
Switch this session to the reviewer prompt for the next turn.
```

Those are user prompts, not terminal commands. CodeAlta-managed agents translate them into the appropriate prompt/session operations when available.

## Default and project-specific overrides

A project prompt can specialize a global workflow without changing other repositories. Common examples:

- `plan.prompt.md` that adds repository-specific plan-file sections;
- `reviewer.prompt.md` that includes local test commands and review gates;
- `release.prompt.md` that knows the repository's packaging and publishing checklist;
- `triage.prompt.md` that asks child sessions to inspect logs, issues, and recent session history.

> [!WARNING]
> Project-local prompts are part of the repository workspace. Review prompt changes like code changes: a prompt can change how agents decide, ask, delegate, and mutate files.

## System prompts and templates

System prompt files carry host-level behavior and should be short, stable, and explicit. Agent prompts are better for workflow-specific session behavior.

Advanced users can add `template.yml` under `~/.alta/prompts/` or `<project>/.alta/prompts/` to choose default ids and generated context parts:

```yaml
version: 1
system: default
agent: default
skills: true
project_context: true
runtime_context: true
tool_guidance: true
```

Use templates sparingly. Disabling generated context such as tool guidance can make advanced prompt workflows less discoverable to the agent. For most day-to-day customization, create an agent prompt and select it from the footer.
