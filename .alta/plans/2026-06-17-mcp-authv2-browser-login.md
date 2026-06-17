# MCP Authv2 browser login support

- Status: Implemented (automated verification passed; protected-server manual verification pending)
- Plan file: `.alta/plans/2026-06-17-mcp-authv2-browser-login.md`
- Created: 2026-06-17
- Task: Evaluate and plan support for MCP Authv2/OAuth browser authorization from the MCP plugin and dialog, including endpoints such as `https://mcp.atlassian.com/v1/mcp/authv2`.
- Git: `.alta/plans/` is not ignored; commit this plan with the related implementation work.

## Objective
- Add CodeAlta-managed browser authorization for protected remote MCP HTTP/SSE servers that follow the MCP authorization flow (OAuth 2.1 with RFC 9728 protected-resource metadata, authorization-server discovery, PKCE, optional Dynamic Client Registration, and bearer-token requests).
- Make authorization discoverable and usable from the MCP Servers dialog, with a command-line/live-tool fallback for headless or agent-assisted setup.
- Preserve existing static-header and environment-variable credential support; do not add OAuth for stdio servers or implement MCP resources/prompts/elicitation as part of this work.

## Context and evidence
- Current MCP config/runtime supports stdio and HTTP/SSE only, with static HTTP headers expanded from JSON config (`src/CodeAlta.Plugin.Mcp/McpConfigModels.cs`, `McpConfigFormatAdapter.ReadServer`, `McpRuntimeService.CreateTransport`).
- Current HTTP transport creates `HttpClientTransportOptions` without OAuth and only passes `AdditionalHeaders`; HTTP 401/403 is reported as `server_authentication_failed` with “OAuth UX is not implemented” (`src/CodeAlta.Plugin.Mcp/McpRuntimeService.cs:456-482`, `:1074-1085`).
- The MCP dialog can add/edit JSON server fields, test/list tools, and toggle policy, but has no auth action or auth status (`src/CodeAlta.Plugin.Mcp/McpServersDialog.cs:422-445`, `:494-500`).
- The `alta mcp` command tree exposes config/server/tool/activate flows and explicitly states OAuth/interactive auth is not implemented (`src/CodeAlta.Plugin.Mcp/McpCommandFactory.cs:130-140`).
- The C# MCP SDK package already exposes client OAuth hooks: `HttpClientTransportOptions.OAuth` is `ModelContextProtocol.Authentication.ClientOAuthOptions`; options include `RedirectUri`, `ClientId`, `ClientSecret`, `Scopes`, `AuthorizationRedirectDelegate`, `DynamicClientRegistration`, and `TokenCache` (`ModelContextProtocol.Core 1.4.0` XML/reflection evidence).
- Provider browser login patterns already exist: browser opening via `ProcessStartInfo.UseShellExecute`, loopback `HttpListener`, UI status callbacks, cancellation, and token persistence for Codex/xAI/Copilot (`src/CodeAlta/App/ProviderFrontendCoordinator.cs:216-234`, `:392-410`, `:634-647`; `src/CodeAlta.Agent.Xai/XaiDirectLoginManager.cs:35-107`).
- MCP documentation describes the target client flow: protected server returns `401` with `WWW-Authenticate: Bearer ... resource_metadata=...`; client fetches PRM, discovers auth server metadata, registers dynamically or uses pre-registered client info, opens browser for authorization-code+PKCE, exchanges tokens, then sends `Authorization: Bearer ...` (https://modelcontextprotocol.io/docs/tutorials/security/authorization.md).
- Project guidance says MCP connection definitions belong in fixed JSON files, policy belongs in TOML, runtime diagnostics must redact secrets, and OAuth UX must not be documented as shipped until implemented/tested (`AGENTS.md`; `doc/development-guide.md:114-121`; `doc/mcp.md:141`, `:189-192`).

## Assumptions and open decisions
- Assumption: “Authv2 browser login” means the MCP standard authorization flow over HTTP/SSE, not an Atlassian-specific nonstandard protocol. Atlassian should work if its endpoint follows the MCP auth challenge/discovery flow supported by the SDK.
- Assumption: CodeAlta should default to Dynamic Client Registration and loopback redirect for servers that support it, requiring no extra config beyond the MCP server URL.
- Approved decision: for authorization servers that do not support DCR, CodeAlta should support optional per-server OAuth client metadata in MCP JSON (`client_id`, optional `client_secret`, scopes, and possibly fixed redirect URI/client metadata document URI); choose the exact JSON shape during implementation to minimize conflicts with existing MCP client formats.
- Approved decision: include manual verification guidance for Atlassian Authv2 when credentials are available, but do not block automated verification on real Atlassian credentials.

## Design notes
- Prefer using the C# MCP SDK’s `ClientOAuthOptions` instead of implementing OAuth discovery/token exchange directly.
- Add a small MCP-owned OAuth layer inside `src/CodeAlta.Plugin.Mcp/`:
  - a parsed per-server OAuth config model for HTTP servers;
  - a file-backed `ITokenCache` implementation under CodeAlta-owned user state (for example `~/.alta/auth/mcp/<safe-server-key>.json` or plugin user state), redacting and avoiding command output of token material;
  - a browser authorization delegate that binds a loopback listener, opens the browser, captures the `code`, handles cancellation, and returns the code to the SDK;
  - a non-interactive delegate that returns `null`/diagnostic when no cached token can be refreshed, so agent runs do not unexpectedly block on browser login.
- Keep interactive authorization explicit:
  - MCP dialog gets an **Authorize/Login** action for HTTP servers and **Logout/Clear token** where cached credentials exist;
  - `Test` may use cached tokens and should direct users to **Authorize/Login** when auth is required;
  - `alta mcp auth login <server>`, `status`, and `logout` provide a headless/live-tool fallback and can print the URL when browser opening fails.
- Do not write bearer tokens into `.alta/mcp.json` or TOML. JSON may contain only non-token OAuth client settings if needed; tokens belong in CodeAlta-owned state.
- Default agent-run and direct-tool paths should use cached/refreshable tokens only; they should surface a redacted diagnostic and guidance to run dialog/command login when interactive auth is needed.
- Rejected alternative: ask users to paste Authorization headers into MCP JSON. This remains supported but does not satisfy browser-login Authv2 and encourages long-lived bearer tokens in config.

## Risks and challenges
- OAuth browser login in a terminal app is cross-platform sensitive: loopback port conflicts, browser-open failures, headless terminals, cancellation, callback CORS/private-network preflight, and redirect URI allowlisting must be handled carefully.
- OAuth tokens and authorization codes are secrets; diagnostics, command JSONL output, dialogs, logs, and tests must never print raw tokens, codes, refresh tokens, or client secrets.
- Dynamic Client Registration may be unavailable or restricted by some authorization servers; optional manual client config is needed for completeness.
- The SDK may trigger authorization during ordinary MCP connection/list-tools. The implementation must avoid accidental interactive browser prompts during agent tool enumeration or background dialog prefill.
- Real Atlassian verification may require an account and browser interaction; automated tests should use local fake servers and SDK-compatible fixtures instead.
- Existing MCP config flavor preservation must not corrupt VS Code/Copilot/Claude/IntelliJ JSON shapes; unknown fields are currently preserved on save but ignored on read.

## Implementation checklist
- [x] Re-check `git status --short` and preserve unrelated user work before editing.
- [x] Inspect the SDK auth behavior in a small local spike or targeted test: confirm `ClientOAuthOptions.AuthorizationRedirectDelegate`, DCR defaults, `ITokenCache` lifetime, and non-interactive failure behavior when the delegate returns `null`.
- [x] Extend MCP config models in `src/CodeAlta.Plugin.Mcp/McpConfigModels.cs` with per-server OAuth/auth settings for HTTP servers only; keep static headers unchanged.
- [x] Extend `McpConfigFormatAdapter.ReadServer`/rendering to parse and preserve a minimal CodeAlta-owned auth object while leaving unknown fields intact for external MCP config flavors.
- [x] Add validation/redaction for OAuth auth fields in `McpRuntimeService.ValidateRuntimeDefinition`, `CreateServerCacheKey`, management snapshots, and diagnostics; reject auth settings on stdio servers.
- [x] Add a file-backed MCP OAuth token cache implementing `ModelContextProtocol.Authentication.ITokenCache`, with deterministic per-server filenames, atomic JSON writes, delete/logout support, and no static mutable state.
- [x] Add MCP OAuth interaction helpers for loopback browser login: choose a safe loopback redirect URI, bind before browser open, handle callback `code`/`state`/errors via the SDK delegate contract, support cancellation, and show a simple success/failure browser response.
- [x] Update `McpRuntimeService.CreateTransport` to set `HttpClientTransportOptions.OAuth` for HTTP servers when auth is enabled or cached credentials exist; use interactive or non-interactive delegate behavior based on request options.
- [x] Extend `McpRuntimeRequest`/call sites so dialog/explicit auth commands can allow interactive login, while `activate`, direct agent-tool enumeration, tool search/describe/call, and automatic dialog prefill are non-interactive unless explicitly requested.
- [x] Add `McpManagementService` methods for auth status, login, and logout, returning redacted status/diagnostics and refreshing cached snapshots after auth changes.
- [x] Update `McpServersDialog` header/details UI with auth status and **Authorize/Login**, **Cancel Login**, and **Logout/Clear token** actions for configured HTTP servers; surface authorization URL/status like the Model Providers dialog without exposing secrets.
- [x] Add `alta mcp auth status|login|logout <server>` in `McpCommandFactory` with JSONL records, redacted diagnostics, and a browser-open/manual-URL fallback.
- [x] Update current 401/403 diagnostics from “OAuth UX is not implemented” to actionable guidance: use **Authorize/Login** or `alta mcp auth login <server>`, or configure static headers if appropriate.
- [x] Update docs in `doc/mcp.md`, `doc/live-tool.md`, `doc/development-guide.md`, and `site/docs/plugins/mcp.md` to describe shipped MCP auth behavior, token storage, command/dialog flows, and security caveats.

## Verification checklist
- [x] Add unit tests for MCP config parsing/rendering of HTTP OAuth auth settings, including preservation of unknown fields and rejection on stdio servers.
- [x] Add unit tests for token cache read/write/delete, filename safety, expiration/status reporting, and secret redaction.
- [ ] Add runtime tests using a local protected HTTP MCP/OAuth fixture: no cached token produces an auth-required diagnostic in non-interactive mode; explicit login delegate obtains/stores tokens; subsequent list-tools/call uses cached bearer auth; logout clears access.
- [x] Add command tests for `alta mcp auth status|login|logout` records and error paths, with no raw tokens/codes in stdout/stderr.
- [ ] Add dialog/view-model or interaction tests where existing patterns allow: auth buttons are enabled only for configured HTTP rows, cancel works, and successful login updates status without blocking close/refresh.
- [x] Run targeted tests first: `dotnet test src/CodeAlta.Tests/CodeAlta.Tests.csproj -c Release --filter "FullyQualifiedName~Mcp"`.
- [x] Run broader verification from project guidance: `cd src; dotnet build -c Release; dotnet test -c Release`; run `cd site; lunet build` after docs changes if Lunet is installed.
- [ ] Perform manual verification with a local protected MCP server from the MCP authorization tutorial and, if credentials are available, with the Atlassian Authv2 endpoint.
- [x] Self-review final diff for secret handling, unwanted browser prompts during agent runs, config flavor preservation, docs consistency, and no unrelated churn.

## Execution notes
- Implemented generic SDK-backed OAuth wiring via `HttpClientTransportOptions.OAuth`, explicit interactive runtime request flags, and non-interactive default behavior for ordinary tool discovery/agent runs.
- Added CodeAlta-owned local token cache at `~/.alta/auth/mcp/` with deterministic per-server filenames, atomic writes, delete/logout, and best-effort Unix file-mode hardening.
- Added dialog and `alta mcp auth status|login|logout` flows; MCP dialog browser login now follows the model-provider pattern with a small modal progress dialog, copyable login URL, and cancel shortcuts/buttons; tokens and authorization codes are not emitted in management/command output.
- Added regression coverage for cached OAuth bearer use during non-interactive HTTP MCP discovery, loopback callback state/preflight handling, token-cache status/logout, and auth diagnostic redaction.
- Targeted MCP tests, repository build/tests, and site build pass; explicit end-to-end browser token exchange against a protected MCP server and real protected-server manual verification remain pending in this execution pass.

## Handoff notes
- Keep the implementation inside the MCP plugin/runtime boundary; do not move reusable auth orchestration into frontend-only CodeAlta views.
- Prefer SDK OAuth hooks over custom OAuth implementation; only implement CodeAlta-specific browser/callback/token-cache glue.
- Treat all OAuth artifacts as secrets. Do not log or render tokens, refresh tokens, authorization codes, PKCE verifiers, client secrets, or full callback URLs.
- Interactive auth should be opt-in from dialog or explicit command; background tool discovery and agent tool registration must not surprise-open a browser.
- If the SDK behavior makes non-interactive cached-token refresh impossible without risking browser prompts, stop and revise the design before broad integration.
