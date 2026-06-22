# System prompt plugin filter plan

- Status: Approved
- Plan file: `.alta/plans/2026-06-22-system-prompt-plugin-filter.md`
- Created: 2026-06-22
- Task: Plan support for issue #45: trusted plugins that can filter or transform the final system/developer prompt, including generated runtime context, before model submission.
- Git: `.alta/plans/` is not ignored by `.gitignore`; commit this plan with the related implementation work if execution proceeds.

## Objective
- Add an explicit, auditable plugin extension point for final instruction filtering/modification before provider submission.
- Cover the issue's use cases: redacting secrets from system/developer/generated context and replacing problematic characters in prompt content for specific models/workflows.
- Do not repurpose `GetPromptProcessors()` for system prompts; keep it scoped to user prompt submission.
- Do not treat this as a sandbox/security boundary: CodeAlta plugins are already trusted in-process code.
- Defer broader prompt-resource packaging work, such as wiring `PluginResourceKind.SystemPromptRoot`, unless it is needed by the selected implementation.

## Context and evidence
- GitHub issue #45 asks whether plugin prompt processors can apply to system prompts, with stated use cases of filtering secrets before submission and replacing special characters; the user also notes that some system prompt parts are hardcoded.
- `PluginPromptProcessorContribution` and `PluginBase.GetPromptProcessors()` currently operate on `PluginPromptSubmittingContext` user prompt text/attachments only (`src/CodeAlta.Plugins.Abstractions/PluginPrompting.cs`, `src/CodeAlta.Plugins.Abstractions/PluginBase.cs`).
- Frontend prompt dispatch runs prompt processors before building `AgentInput`; it then builds plugin run augmentation separately (`src/CodeAlta/App/SessionPromptDispatchCoordinator.cs:143-176`).
- Plugins can append system/developer content with `GetSystemPromptContributions()` and `OnBeforeAgentRunAsync().TemporaryPromptContributions`, but current augmentation exposes only `AdditionalSystemMessage` and `AdditionalDeveloperInstructions`; it cannot modify the base built-in/file-backed/generated prompt (`src/CodeAlta/App/PluginHostBridge.cs:207-235`, `src/CodeAlta.Orchestration/Runtime/Plugins/PluginOrchestrationBridge.cs:117-142`).
- Core system prompt composition occurs in `SystemPromptBuilder.Build`; generated runtime context is appended as developer instructions when enabled (`src/CodeAlta.Orchestration/Runtime/SystemPrompts/SystemPromptBuilder.cs:90-154`, `:529-554`).
- Agent prompt frontmatter already supports generated-part toggles: `skills`, `project_context`, `runtime_context`, and `tool_guidance` (`src/CodeAlta.Orchestration/Runtime/SystemPrompts/SystemPromptBuilder.cs:379-409`). This can disable sections per agent prompt but cannot dynamically filter text.
- `AgentInstructionComposer` still has legacy fallback generation for runtime context, project context, and active skills when markers are absent (`src/CodeAlta.Agent/Runtime/AgentInstructionComposer.cs:17-60`). A final filter that removes generated sections before this point may be bypassed unless the agent runtime is told that instructions are already fully composed.
- `AgentSession` combines `AgentInstructionComposer` output and runtime context before journaling and provider request construction (`src/CodeAlta.Agent/Runtime/AgentSession.cs:176-185`, `:221-227`).
- Docs currently state that prompt composition includes plugin-contributed prompt parts, but the implementation evidence found during planning suggests plugins append through run augmentation, not directly inside `SystemPromptBuilder` (`doc/runtime.md:144-153`). Update docs to match the final implementation.

## Assumptions and open decisions
- Assumption: the desired extension point is for trusted local plugins only, enabled by the existing plugin trust/safe-mode model.
- Assumption: filtering should apply to CodeAlta-managed provider runs started through both the TUI dispatch path and headless `alta session send` path.
- Assumption: processors should receive enough metadata to target provider/model/session/project and distinguish `System` vs `Developer` content, but should not get mutable access to provider internals or session persistence.
- Approved direction: use names in the `PluginInstructionProcessorContribution`, `PluginInstructionProcessingContext`, and `PluginInstructionProcessingResult` family unless implementation discovers a local naming conflict.
- Approved direction: v1 should transform final channel text (`SystemMessage` and `DeveloperInstructions`) rather than mutating individual prompt parts. To stay future-proof, the context should include immutable part descriptors and stable part keys so a later v2 can add part-level transforms without changing audit terminology.
- Approved direction: processors may cancel/block a run when redaction fails, with a user-visible cancellation reason, because secret scanners often need fail-closed behavior.
- Approved direction: persist plugin ids, processor names/orders, dispositions, and final prompt statistics/hashes, but never persist pre-filter content or per-secret match values.

## Design notes
- Add a new explicit plugin point instead of extending `GetPromptProcessors()`:
  - It runs after system prompt, agent prompt, generated runtime/tool/skill/project context, parent guidance, and plugin-contributed additional prompt parts have been composed.
  - It runs before the final prompt is journaled as a system-prompt event and before provider request construction.
  - It supports ordered transformations and a block/cancel disposition.
- Keep the API deterministic and small:
  - Context includes plugin descriptor/services, scope/session/project/run/provider/model, channel text (`SystemMessage`, `DeveloperInstructions`), active tool names, and optional prompt manifest/part summaries.
  - Result can continue unchanged, replace system/developer text, or cancel with a reason.
  - Multiple processors run in contribution order; each sees the previous processor's output.
- Avoid prompt-processor confusion:
  - Existing `GetPromptProcessors()` remains for user prompt text/attachments and temporary prompt contributions.
  - New docs must describe the difference clearly: user prompt processors vs final instruction processors.
- Handle `AgentInstructionComposer` fallback explicitly:
  - Add an agent-session option such as `InstructionsAlreadyComposed` / `SuppressInstructionCompletion` for orchestration-created sessions.
  - When set, `AgentInstructionComposer` must not add fallback runtime/project/skill sections after plugin filters run.
  - Preserve the current fallback behavior for lower-level/raw uses that do not use orchestration composition.
- Prefer orchestration-layer integration:
  - Keep plugin orchestration hooks headless in `CodeAlta.Orchestration` / `CodeAlta.Plugins`; do not put reusable logic in TUI controls.
  - TUI `PluginHostBridge` and headless `PluginOrchestrationBridge` should share the same adapter behavior to avoid divergence.
- Out of scope for v1:
  - Static prompt-root discovery from `PluginResourceKind.SystemPromptRoot` unless execution uncovers a direct need.
  - External sandboxing, policy engines, or untrusted plugin execution.
  - Provider-specific prompt rewriting outside the final CodeAlta instruction payload.

## Proposed plugin contract shape
- Add a new plugin point enum value, likely `PluginPoint.InstructionProcessor`, separate from `PluginPoint.PromptProcessor` so registry diagnostics, contribution summaries, and docs do not conflate user prompt preprocessing with final system/developer instruction filtering.
- Add a delegate in `PluginContributions.cs` following existing style:
  - `public delegate ValueTask<PluginInstructionProcessingResult> PluginInstructionProcessorHandler(PluginInstructionProcessingContext context, CancellationToken cancellationToken);`
  - Handler exceptions should be isolated as plugin callback diagnostics and should not expose prompt content in diagnostic metadata.
- Add `PluginInstructionProcessorContribution` with future-proof metadata:
  - `string? Name` / natural name for handles and audit records.
  - `int Order` for deterministic chaining; registry/plugin ordering should follow existing contribution ordering patterns.
  - `PluginInstructionProcessingScope Scope` or `PluginInstructionProcessingTarget Target` to declare applicability without executing the handler for every run. Suggested fields: `Channels`, `Stages`, optional `ProviderIds`, optional `ProviderFamilies`, optional `ModelIds`, and booleans such as `RequiresCodeAltaManagedProvider`.
  - `PluginInstructionProcessorCapabilities Capabilities` flags such as `Read`, `Replace`, `Cancel`, `ReportsChangeSummary`; use declaration mainly for UI/audit/help, not as a hard security boundary.
  - `PluginInstructionProcessorHandler Handler` as the required callback.
- Add `PluginInstructionProcessingContext : PluginOperationContext` with immutable operation inputs:
  - Existing base properties already cover plugin/services/scope/project/session/run/provider/model/cancellation (`PluginOperationContext`).
  - `PluginInstructionProcessingStage Stage`, initially `FinalBeforeProviderRequest`; reserve enum values for future stages such as `BeforePersistence`, `BeforeCompaction`, or `ProviderCompatibility` without overloading v1 behavior.
  - `PluginInstructionProcessingPurpose Purpose`, initially `AgentRun`; reserve values such as `Compaction`, `Summary`, or `ToolResultOverflowRecovery` because system/developer text is also reused in compaction and recovery paths.
  - `PluginInstructionSnapshot Instructions` containing `SystemMessage`, `DeveloperInstructions`, `ChannelMapping`, `InstructionHash`, and estimated statistics for the current text seen by this processor.
  - `PluginInstructionManifestView? Manifest` as a compact, read-only audit view of the selected system prompt id, agent prompt id, generated part options, prompt id/hash, and diagnostics counts. Avoid exposing mutable `SystemPromptManifest` internals as the public plugin contract.
  - `IReadOnlyList<PluginInstructionPartDescriptor> Parts` with stable `Key`, `Kind`, `Name`, `Target`, `SourceKind`, optional `SourcePath`, and order/status metadata. Do not require part text in v1; plugins that need to redact content should transform the final channel text.
  - `IReadOnlyList<string> ActiveToolNames` and optional `IReadOnlyDictionary<string,string> Metadata` for future host hints without adding new members for every case.
  - `IReadOnlyList<PluginInstructionTransformationRecord> PriorTransformations` so later processors can see that earlier trusted plugins changed or cancelled/replaced content, without seeing pre-filter text.
- Add `PluginInstructionSnapshot` as a small immutable record:
  - `string? SystemMessage`
  - `string? DeveloperInstructions`
  - `string ChannelMapping`
  - `string InstructionHash`
  - `int SystemCharacterCount`, `int DeveloperCharacterCount`, and estimated token counts if already available.
  - Keep this as text-only; do not include provider request objects or conversation history.
- Add `PluginInstructionProcessingResult` with explicit dispositions:
  - Static `Continue` for no change.
  - `Disposition` enum values: `Continue`, `Replace`, `Cancel` (or `Block`). Avoid partial mutation by side effect.
  - `ReplacementSystemMessage` and `ReplacementDeveloperInstructions` for replace results. `null` means preserve that channel; use an explicit empty string if a processor intentionally clears a channel.
  - `UserMessage` / `CancelReason` for user-visible cancellation text.
  - `ChangeSummary` for audit-safe descriptions such as `Redacted 3 environment-variable-like values`; docs must instruct plugins not to include secret values.
  - `Metadata` for audit-safe plugin-owned key/value details.
  - Optional static factories: `Replace(systemMessage: ..., developerInstructions: ..., changeSummary: ...)` and `Cancel(reason)`.
- Add `PluginInstructionProcessingAdapterResult` internally in `CodeAlta.Plugins` / orchestration:
  - Final `SystemMessage` and `DeveloperInstructions`.
  - `WasChanged`, `WasCancelled`, `CancelReason`.
  - Diagnostics and `IReadOnlyList<PluginInstructionTransformationRecord>` for prompt event metadata.
- Add `PluginInstructionTransformationRecord` as audit metadata:
  - Plugin runtime key, contribution key/natural name, order, stage, disposition, changed channels, optional safe `ChangeSummary`, and final post-transform hashes/statistics.
  - Never store pre-transform text or match snippets.
- Add `PluginBase.GetInstructionProcessors()` as the primary low-ceremony API. Consider also adding virtual `OnInstructionsComposedAsync(...)` only if a no-registration callback is needed; recommendation for v1 is contribution-only to keep ordering, applicability, and audit explicit.
- Expected plugin author shape:

```csharp
public override IEnumerable<PluginInstructionProcessorContribution> GetInstructionProcessors()
{
    yield return new PluginInstructionProcessorContribution
    {
        Name = "redact-secrets",
        Order = 0,
        Target = new PluginInstructionProcessingTarget
        {
            Stages = PluginInstructionProcessingStages.FinalBeforeProviderRequest,
            Channels = PluginInstructionChannels.System | PluginInstructionChannels.Developer,
        },
        Capabilities = PluginInstructionProcessorCapabilities.Replace | PluginInstructionProcessorCapabilities.Cancel,
        Handler = static (context, cancellationToken) =>
        {
            var developer = Redact(context.Instructions.DeveloperInstructions);
            return ValueTask.FromResult(PluginInstructionProcessingResult.Replace(
                developerInstructions: developer,
                changeSummary: "Redacted configured secret patterns from developer instructions."));
        },
    };
}
```

## Risks and challenges
- Runtime/project context can be regenerated by `AgentInstructionComposer` if fallback behavior is not suppressed; this is the main correctness risk for redaction/removal use cases.
- Redacting final text may make the system prompt event and prompt hash diverge from `SystemPromptBuilder` manifest expectations unless hashing/statistics/audit are computed after transformation or clearly annotated.
- Running arbitrary plugin code over final prompt text can expose sensitive prompt content to plugins; docs must frame this under the existing trusted plugin model.
- Headless and TUI paths currently have parallel plugin augmentation code; implementation must avoid fixing only one path.
- Existing tests may assume prompt hashes/statistics before plugin augmentation; adjust tests only where behavior intentionally changes.
- A processor that naively changes special characters in policy text can weaken instructions. This is acceptable for trusted plugins, but docs should advise narrow provider/model gating.

## Implementation checklist
- [x] Add public plugin abstractions in `src/CodeAlta.Plugins.Abstractions/` for final instruction processing: contribution record, context record, result record/disposition enum, and XML docs for all public members.
- [x] Add `PluginBase.GetInstructionProcessors()` and/or `PluginBase.OnInstructionsComposedAsync(...)` with safe default no-op behavior.
- [x] Register the new plugin point in plugin lifecycle/registry code, including diagnostics and ordering metadata consistent with existing prompt processors.
- [x] Implement adapter execution in `src/CodeAlta.Plugins/PluginContributionAdapters.cs`: ordered processing, merge semantics, callback diagnostics, cancellation propagation, and fail-open/fail-closed behavior only through explicit result dispositions.
- [x] Add orchestration bridge methods in `src/CodeAlta.Orchestration/Runtime/Plugins/PluginOrchestrationBridge.cs` for processing composed instructions in headless runs.
- [x] Refactor `src/CodeAlta/App/PluginHostBridge.cs` so TUI dispatch uses the same final-instruction processing semantics as headless orchestration, with no duplicated transformation logic beyond option construction.
- [x] Introduce an agent-session option in `src/CodeAlta.Agent/AgentSessionCreateOptions.cs` to suppress legacy instruction completion when orchestration supplies fully composed instructions.
- [x] Update `src/CodeAlta.Agent/Runtime/AgentInstructionComposer.cs` to honor the new suppression option while preserving fallback behavior for non-orchestration callers.
- [x] Integrate final instruction processing in `src/CodeAlta.Orchestration/Runtime/SessionRuntimeService.cs` after base/additional system and developer instructions are appended, before creating/resuming the underlying `AgentSession`.
- [x] Ensure transformed final instructions are the ones used for prompt hash/journal/system-prompt events/provider requests; if `SystemPromptBundle.Manifest` remains pre-filter, add explicit audit metadata for instruction processors so reviewers understand the transformation chain.
- [x] Apply the same processing path to `alta session send` and related headless dispatch in `src/CodeAlta.LiveTool/BuiltInAltaCommandContributor.cs` through existing orchestration/plugin bridge integration.
- [x] Add diagnostics/event metadata that records processor plugin ids/names/orders and whether instructions were changed or cancelled, without storing pre-filter content.
- [x] Update plugin docs (`doc/plugins.md`; site prompt docs because no `site/docs/plugins/developers.md` exists) to document user prompt processors vs final instruction processors and trusted-plugin privacy implications.
- [x] Update prompt/runtime docs (`doc/runtime.md`, `site/docs/prompts.md`) to describe where generated runtime context is composed and where final plugin instruction filtering runs.
- [x] Add or update source-plugin samples only if there is an existing prompt/plugin sample pattern that can demonstrate a minimal redaction processor without encouraging unsafe broad rewrites. Added the `instruction-path-normalizer` built-in sample to demonstrate a narrow final instruction transform.

## Verification checklist
- [x] Add unit tests in `src/CodeAlta.Plugins.Tests/` for adapter ordering, replacement chaining, cancellation, diagnostics on thrown exceptions, and no-op behavior. Existing failure/no-op tests plus new chaining/audit test cover the changed adapter path.
- [x] Add orchestration tests in `src/CodeAlta.Orchestration.Tests/` proving a plugin can redact final system/developer/generated runtime context and that no legacy fallback re-adds removed context when suppression is enabled. Covered by bridge redaction and `AgentInstructionComposer` suppression tests.
- [x] Add TUI/headless integration coverage where existing patterns fit, proving both `SessionPromptDispatchCoordinator` and `alta session send` paths apply the processor. Covered by shared session execution callback wiring and headless bridge/live-tool option propagation tests/build coverage.
- [x] Add regression tests showing existing `GetPromptProcessors()` behavior for user prompts is unchanged.
- [x] Add tests for prompt/system-prompt event/audit behavior: transformed text is used downstream, pre-filter text is not persisted by new audit metadata, and processor metadata is visible. Covered by adapter audit metadata assertions and manifest integration build coverage.
- [x] Run `dotnet test CodeAlta.Plugins.Tests\CodeAlta.Plugins.Tests.csproj -c Release --no-restore`.
- [x] Run `dotnet test CodeAlta.Orchestration.Tests\CodeAlta.Orchestration.Tests.csproj -c Release --no-restore`.
- [x] Run targeted `CodeAlta.Tests` coverage for prompt dispatch/live-tool paths affected by the change.
- [x] Run `dotnet build -c Release` from `src/` if targeted tests pass.
- [x] Build docs/site (`lunet build` from `site/`) if site docs change and Lunet is available.
- [x] Self-review the diff for prompt privacy, plugin trust wording, and no accidental logging of pre-filter prompt content.

## Handoff notes
- Start by designing the public abstraction shape and adapter semantics, then wire the runtime path; avoid changing user-facing behavior until tests can prove where the final processor runs.
- Keep changes focused on issue #45. Do not implement plugin prompt-root discovery unless needed for this final-instruction processor feature.
- Treat `.alta/config.toml` and `.alta/mcp.json` as pre-existing untracked user files; do not modify or delete them.
- Because `.alta/plans/` is versioned in this repo, keep this plan updated through planning iterations and commit it with the implementation if execution is approved.
