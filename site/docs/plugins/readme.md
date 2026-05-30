---
title: Plugins
---

# Plugins

CodeAlta plugins are trusted source packages and built-in extensions that can extend the shell, prompt flow, agent runtime, timeline projections, and the in-session `alta` live tool.

Plugins are for local automation you choose to run. Building a source plugin can execute SDK/NuGet/MSBuild logic, and loading it executes .NET code inside the CodeAlta process.

> [!WARNING]
> Install and enable only plugins you trust. Source plugins are built on your machine and loaded into the CodeAlta process, so a plugin has the same practical risk profile as running local code.

> [!IMPORTANT]
> Plugin APIs are preview surface area before CodeAlta `1.0`. Interfaces, contribution points, service exposure, and behavior can change between `0.x` releases; some CodeAlta capabilities may not be exposed yet, and some exposed capabilities may still be incomplete or incorrectly shaped.

## Built-in plugins

CodeAlta ships trusted built-in plugins through the same plugin runtime used for source plugins:

{.table}
| Plugin | What it adds |
|---|---|
| [GitHub](github.md) | `#` issue lookup in GitHub repositories and an optional `gh` agent tool when the GitHub CLI is installed. |
| [MCP](mcp.md) | Model Context Protocol server configuration, `alta mcp` commands, session-activated MCP agent tools, and the MCP Servers dialog. |
| [Statistics](statistics.md) | Transient per-turn/session statistics timeline cards and a `statistics estimate` live-tool command. |

## Plugin locations

CodeAlta discovers dynamic source plugins from:

- `~/.alta/plugins/<package-id>/plugin.cs` for global plugins;
- `<project>/.alta/plugins/<package-id>/plugin.cs` for project-scoped plugins.

Project-scoped plugins apply only to the matching project. Global plugins apply across workspaces.

## Manage plugins

Open plugin management with `Ctrl+G Ctrl+N`, `/plugins`, or `/plugin`.

<figure class="my-4">
  <img class="img-fluid rounded-4 shadow" src="{{site.basepath}}/img/alta-plugins.png" alt="CodeAlta plugin management dialog with plugin list, diagnostics, and selected plugin contributions" loading="lazy">
  <figcaption class="small text-secondary mt-2">Plugin management keeps discovered plugins, diagnostics, source actions, and contribution summaries visible in one dialog.</figcaption>
</figure>

The dialog shows:

- plugin scope and state;
- diagnostics from discovery, config, build, load, activation, contributions, callbacks, source changes, and unload;
- contribution summaries;
- unknown config entries;
- source and README open actions when available.

You can also use a headless status summary:

```sh
alta --plugins-status
```

## Disable or bypass plugins

Source plugins and built-in plugins are enabled by default when discovered. Disable a plugin in TOML when you do not want it built or loaded:

```toml
[plugins.HelloWorld]
enabled = false
```

Built-in plugin IDs are lowercase, for example:

```toml
[plugins.github]
enabled = false

[plugins.mcp]
enabled = false

[plugins.statistics]
enabled = false
```

When a plugin is broken, start CodeAlta with a bypass:

```sh
alta --no-plugins
alta --plugin-safe-mode
```

Or set:

```sh
CODEALTA_DISABLE_PLUGINS=1
```

These bypasses are recognized before plugin-contributed command-line options.

## Minimal source plugin

Create `~/.alta/plugins/HelloWorld/plugin.cs`:

> [!TIP]
> Keep early plugins small and focused. The preview API is easiest to track when each plugin owns one narrow workflow and avoids depending on undocumented host internals.

```csharp
using CodeAlta.Plugins.Abstractions;
using XenoAtom.Terminal.UI.Controls;
using CliCommand = XenoAtom.CommandLine.Command;

[Plugin(DisplayName = "Hello Plugin", Description = "Adds a command, prompt note, and status row.")]
public sealed class HelloPlugin : PluginBase
{
    public override IEnumerable<PluginCommandContribution> GetCommands()
    {
        yield return Command.Prompt(
            "hello",
            "Show a hello notification.",
            static async (context, cancellationToken) =>
            {
                await context.Ui.NotifyAsync("Hello from a plugin.", cancellationToken);
                return PluginCommandResult.Handled;
            });
    }

    public override IEnumerable<PluginSystemPromptContribution> GetSystemPromptContributions()
    {
        yield return Prompt.Developer("When the user asks about the hello plugin, explain that it is installed.");
    }

    public override IEnumerable<XenoAtom.CommandLine.CommandNode> GetCommandLineContributions()
    {
        yield return new CliCommand("hello", "Run hello plugin command-line actions.");
    }

    public override IEnumerable<PluginUiContribution> GetUiContributions()
    {
        yield return PluginUi.Status("Hello", static _ => "hello plugin active");
        yield return PluginUi.Visual(PluginUiRegion.SessionFooter, static _ => new Markup("[dim]Hello plugin[/]"));
    }
}
```

Restart CodeAlta or refresh plugin state from the management UI. CodeAlta generates the plugin-root build files it owns and invokes `dotnet build plugin.cs` from the plugin package directory.

## What plugins can contribute

Plugins can contribute:

- startup hooks and command-line commands;
- shell, prompt, and session commands;
- UI visuals, status rows, dialogs, and renderers;
- prompt processors, prompt-editor attachments, and system/developer prompt parts;
- before-agent-run hooks;
- LLM-callable agent tools and tool-call/result interception;
- provider factories;
- plugin-lifetime background tasks;
- resource roots such as skills, prompts, templates, themes, MCP manifests, and agent definitions;
- compaction hooks;
- normalized agent event observers;
- transient session event projections for plugin-owned timeline cards (current APIs still use some legacy `Session` names).

Plugin shell commands are no-argument frontend activations. A `PluginCommandContribution` declares placement, command-palette/search metadata, visibility flags, optional shortcut, and availability; CodeAlta adapts active contributions into the same shell command registry as built-ins. Plugin commands therefore appear in help, the command palette, command bars, and shortcuts without frontend-specific code.

`PluginCommandContext` exposes public services such as `Ui`, `Sessions`, `Prompts`, and `Workspace`, but not raw slash text or argument tokens. Commands that need input should use plugin UI services, prompt/session services, or a plugin-owned dialog/workflow contribution. Plugins do not receive internal frontend command types, XenoAtom `Visual` targets, or frontend view models.

Prompt-editor attachments can attach plugin-owned behavior to prompt editors. CodeAlta provides only a small editor host, including the prompt project path; each plugin owns its trigger detection and visual presentation. Attachments can also set `PluginPromptEditorContribution.PlaceholderText` with a short placeholder segment such as `[#] to reference a GitHub issue`, which appears in the ready prompt placeholder while that contribution applies.

## Extend the `alta` live tool

Plugins can add in-session live-tool command roots by returning `PluginAltaCommandContribution` records from `PluginBase.GetAltaCommands()`.

Those commands appear in:

```text
alta --help
alta tool capability list
```

Trusted plugins can also invoke built-in `alta` commands through `Services.Alta.InvokeAsync(...)`. Mutating plugin-originated commands include the plugin key in provenance so later sidebars, timelines, and reports can explain who created or submitted work.

## Safe authoring notes

- Use `Services.Tasks.Run(...)` or the `PluginBase.Tasks` shortcut for background work so CodeAlta can cancel and track plugin tasks during unload.
- Keep plugin package roots simple. For v1 source-folder plugins, do not add your own root `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, or `global.json`.
- Plugin-owned dependencies should be copied by the SDK (`EnableDynamicLoading=true`) so the plugin load context can resolve them.
- Avoid untracked background work, static references to host objects, pinned native resources, or host-static delegates that can prevent unload.

> [!CAUTION]
> A plugin that starts unmanaged background work, captures host-static state, or pins native resources can keep running after you expect it to unload. Prefer CodeAlta-managed task services and cancellable operations.
