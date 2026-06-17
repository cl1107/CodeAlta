# MCP OpenAI strict schema rejection

- Status: Implemented (automated verification passed; live Atlassian manual check not run)
- Plan file: `.alta/plans/2026-06-17-mcp-openai-strict-schema.md`
- Created: 2026-06-17
- Task: Investigate and fix MCP direct tools whose JSON schemas are rejected by ChatGPT/Codex/OpenAI strict tool validation.
- Git: `.alta/plans/` is not ignored; commit this plan with the related implementation work per the current user request.

## Objective
- Prevent activated MCP tools such as Atlassian `editJiraIssue` from causing provider request rejection before the model turn starts.
- Preserve the ability to call MCP tools whose schemas use JSON Schema features that OpenAI strict function tools cannot faithfully represent, especially dynamic object maps.
- Non-goal: change MCP server configuration, OAuth/authentication behavior, or the MCP SDK package unless later evidence shows the SDK mutates schemas incorrectly.

## Context and evidence
- `src/CodeAlta.Plugin.Mcp/McpPlugin.cs:396-401` creates direct agent tools by passing `tool.InputSchema.Clone()` directly to `AgentToolSpec`; the handler then forwards model arguments to `McpRuntimeService.CallToolAsync`.
- `src/CodeAlta.Plugin.Mcp/McpRuntimeService.cs:970-994` obtains tool schemas from `McpClientTool.JsonSchema` and clones them into `McpRuntimeTool.InputSchema`; this indicates the MCP SDK is mainly surfacing the server-provided schema.
- `src/CodeAlta.Agent.OpenAI/OpenAIChatTurnExecutor.cs:257-268` and `src/CodeAlta.Agent.OpenAI/OpenAIResponsesTurnExecutor.cs:1410-1415` send tools to OpenAI/Codex with strict schema normalization via `AgentToolBridge.CreateOpenAIStrictInputSchema`.
- `src/CodeAlta.Agent/Runtime/Tools/AgentToolBridge.cs:192-299` rewrites `required` only when a schema has a local `properties` object. For an object schema that has `required: ["fields"]` but no sibling `properties.fields` (valid JSON Schema when `additionalProperties`/composition describes the field), CodeAlta can emit a strict schema with an extra required key, matching the reported provider error.
- `src/CodeAlta.Tests/AgentToolsTests.cs:873-953` covers strict normalization for built-in tools but not MCP-style dynamic schemas or `required` entries without sibling `properties`.
- The reported error is from OpenAI/Codex validating the request shape, but the actionable bug is on the CodeAlta adaptation path: CodeAlta should not send an MCP/JSON Schema shape that violates OpenAI strict function schema requirements.

## Assumptions and open decisions
- Assumption: Atlassian's `editJiraIssue` schema is legal or at least tolerated in MCP/JSON Schema, but not directly compatible with OpenAI strict tool schemas.
- Assumption: A small compatibility wrapper for only incompatible MCP schemas is acceptable if it avoids request rejection and preserves arbitrary JSON arguments.
- Approved decision: Use the `arguments_json` compatibility wrapper for MCP schemas that OpenAI strict mode cannot faithfully represent.

## Design notes
- Recommended approach: keep normal MCP tools unchanged when their input schema can be represented by CodeAlta's strict normalizer, but wrap incompatible MCP schemas in a strict-safe single-string argument schema.
- Wrapper schema shape: expose one required string property such as `arguments_json`, with a description containing the MCP tool name/server and a compact/redacted copy or summary of the original input schema. The model supplies a JSON object string; CodeAlta parses it and forwards the resulting dictionary to the MCP SDK.
- Incompatibility detector should be conservative and recursive. Trigger wrapping for object schemas with `required` names missing from sibling `properties`, open/dynamic maps (`additionalProperties` object or `true`), `patternProperties`, and similar constructs that OpenAI strict tools cannot preserve while requiring `additionalProperties: false`.
- Also harden `AgentToolBridge.CreateOpenAIStrictInputSchema` so it never emits a strict object schema whose `required` array contains names absent from sibling `properties`, even for non-MCP plugin tools. This is a defensive backstop; MCP wrapper preserves functionality for dynamic arguments.
- Rejected alternative: blame or patch the MCP SDK. Local evidence shows CodeAlta clones `McpClientTool.JsonSchema`; the SDK is not responsible for OpenAI/Codex's stricter schema subset.
- Rejected alternative: globally disable strict tools for Codex/OpenAI. This would affect built-in tools and structured tool reliability across the app.

## Risks and challenges
- Wrapping changes the model-facing UX for affected MCP tools: the model must fill `arguments_json` rather than named fields. The description must be clear enough for reliable calls.
- If the original schema is large, embedding it in the wrapper description can bloat tool declarations; cap/truncate the schema summary and mention that the JSON object must match the MCP server schema.
- Generic strict-schema hardening may make some malformed third-party schemas request-valid but less semantically constrained. Tests should assert the exact sanitized shape.
- Manual verification with Atlassian may require valid OAuth/account access; automated tests should use local fake MCP server fixtures.

## Implementation checklist
- [x] In `McpPlugin.cs`, add an internal helper to classify MCP input schemas as strict-compatible vs wrapper-required. Keep it deterministic, recursive, and independent of network/provider state.
- [x] In `McpPlugin.CreateAgentTool`, choose either the original input schema or a strict-safe wrapper schema for affected tools, and pass a wrapper flag/schema mode into the handler closure.
- [x] Extend `TryReadArguments`/`InvokeDirectToolAsync` in `McpPlugin.cs` to parse `arguments_json` for wrapped tools, require it to be a JSON object, preserve the current object-to-dictionary behavior for unwrapped tools, and return user-facing tool errors for invalid JSON/string/non-object payloads.
- [x] Add a compact original-schema summary to the wrapper property description; truncate long schemas and avoid including secrets or runtime values.
- [x] In `AgentToolBridge.CreateOpenAIStrictInputSchema`, defensively prevent extra `required` names when an object schema lacks matching local `properties`; ensure object schemas emitted for strict mode include a valid `properties`/`required` relationship.
- [x] Add regression tests in `src/CodeAlta.Tests/AgentToolsTests.cs` for strict normalization of an object schema with `required` entries but no matching `properties`.
- [x] Add MCP plugin/runtime tests in `src/CodeAlta.Tests/McpRuntimeServiceTests.cs` using the local fake MCP server to expose an Atlassian-like schema, assert the direct tool is wrapped, assert `arguments_json` is parsed into the MCP call arguments, and assert invalid wrapper payloads return tool errors instead of throwing.
- [x] Update docs only if the visible MCP tool-call contract changes for users; likely `doc/mcp.md` and `site/docs/plugins/mcp.md` should mention that some complex MCP schemas may be exposed through an `arguments_json` compatibility wrapper.

## Verification checklist
- [x] Run targeted tests from `src`: `dotnet test -c Release --filter "AgentToolsTests|McpRuntimeServiceTests"`.
- [x] If targeted tests pass and time allows, run from `src`: `dotnet test -c Release`.
- [x] If docs are changed, run from `site`: `lunet build`.
- [ ] Manual check with an Atlassian-like MCP schema: activate the server, start a Codex/OpenAI turn, and verify the request is accepted instead of failing with `invalid_function_parameters`.
- [ ] Manual check with real Atlassian MCP only when credentials are available; verify `editJiraIssue` can pass a JSON object through `arguments_json`.

## Handoff notes
- Worktree is already dirty with MCP/Auth-related edits; preserve all existing user changes and keep this fix focused.
- Do not activate the plugin skill; the user reported it blocked CodeAlta in this workspace.
- Commit this plan file with the related implementation work under the current user request.
- The likely root cause is CodeAlta's adaptation from MCP JSON Schema to OpenAI strict function schema, not Atlassian auth and not the MCP SDK transport.
- Prefer local fake-server tests over live Atlassian tests for automated coverage.
