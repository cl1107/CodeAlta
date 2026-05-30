---
title: MCP plugin
---

# MCP plugin

The built-in MCP plugin connects CodeAlta to configured Model Context Protocol servers. It contributes the `alta mcp` live-tool command root, compact prompt guidance for active/inactive servers, session-activated MCP agent tools, and the MCP Servers dialog.

<figure class="my-4">
  <img class="img-fluid rounded-4 shadow" src="{{site.basepath}}/img/alta-plugin-mcp.png" alt="CodeAlta MCP Servers dialog showing configured MCP servers and tools" loading="lazy">
  <figcaption class="small text-secondary mt-2">The MCP Servers dialog keeps server configuration, policy, diagnostics, and discovered tools visible without blocking the workspace.</figcaption>
</figure>

## Configuration files

MCP connection definitions live in fixed JSON files:

{.table}
| Scope | File | Notes |
|---|---|---|
| Global | `~/.alta/mcp.json` | Loaded first and available across workspaces. |
| Project | `<project>/.alta/mcp.json` | Loaded only for that project and shadows global servers with the same key. |

CodeAlta does not scan editor- or provider-specific MCP paths. New files use the `mcpServers` format:

```json
{
  "mcpServers": {
    "memory": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-memory"]
    },
    "docs": {
      "url": "https://example.test/mcp",
      "headers": {
        "Authorization": "Bearer ${MCP_TOKEN}"
      }
    }
  }
}
```

Stdio servers use `command`, optional `args`, optional `cwd`, and optional `env`. HTTP/SSE servers use `url` and optional `headers`. Environment and header values can reference environment variables with `${NAME}` so secrets do not need to be stored in the JSON file.

## Policy in TOML

Connection fields stay in JSON. Enablement, tool filtering, prompt limits, timeouts, and direct-exposure policy stay in TOML under `[plugins.mcp]`:

```toml
[plugins.mcp]
enabled = true
discover_in_prompt = true
prompt_max_servers = 6
prompt_max_tools = 12

[plugins.mcp.servers.memory]
enabled = true
disabled_tools = ["delete_graph"]
```

Use server/tool policy when you want to hide a server or tool without deleting its JSON connection definition.

## Use `alta mcp`

The plugin adds MCP commands to the in-session `alta` tool:

```sh
alta mcp list
alta mcp status
alta mcp activate memory
alta mcp tool search
alta mcp tool describe --server memory --tool read_graph
alta mcp tool call --server memory --tool echo --arguments '{"text":"hi"}'
```

`list` and config/status commands inspect configuration without connecting to every server. Tool commands connect lazily, apply TOML policy filters, and report redacted diagnostics.

## Activate MCP tools for a session

MCP agent tools are progressive and session-activated. Run:

```sh
alta mcp activate <server> [<server>...]
```

Activation marks configured servers active for the current session and enumerates tools for immediate feedback. On the next agent run, CodeAlta refreshes tools from active servers and registers enabled tools as deterministic aliases such as:

```text
mcp__memory__read_graph
```

If a server fails to connect or list tools, it is reported as unavailable for that runtime request and does not contribute agent tools for that run.

## MCP Servers dialog

Open the dialog from `/mcp`, the command palette entry **MCP Servers**, the MCP status indicator, or plugin management. The dialog can:

- show global and project MCP definitions, including project definitions that shadow global ones;
- add, edit, save, and remove server JSON definitions;
- toggle server enablement through TOML policy;
- discover tools in the background when a configured server is selected;
- test a server with configured timeouts;
- toggle individual tools by writing/removing raw tool names in `disabled_tools`.

Use `alta mcp server add/remove/...` or **Open JSON Config** for advanced JSON shapes that the dialog form does not expose.

## Disable it

Disable MCP runtime exposure and MCP command paths with:

```toml
[plugins.mcp]
enabled = false
```

The MCP plugin remains a built-in trusted plugin, so the global plugin bypasses on the [plugins overview](readme.md#disable-or-bypass-plugins) are still available for startup recovery.
