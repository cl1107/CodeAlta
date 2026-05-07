# Frontend callback seam inventory

This checklist tracks the remaining callback-heavy frontend seams for the TUI frontend refactor. It is generated from the same source roots inspected by `ArchitectureGuardrailTests.ArchitectureInventory_ReportsRemainingDelegateBasedFrontendSeams` and should be updated as seams are burned down.

## Replacement targets

- [ ] `App/CodeAltaFrontendServicesAdapter.cs` / `App/ICodeAltaFrontendServices.cs` -> small named ports (`IWorkspaceSurface`, `IShellStatusService`, prompt/session and projection invalidation ports).
- [ ] `Frontend/Commands/ShellInputCoordinator.cs` -> `ShellInputRouter` + `IShellCommandDispatcher` only. Initial command dispatcher migration is in place.
- [ ] `Frontend/Commands/ShellCommandSurfaceCoordinator.cs` -> command registry/handlers grouped by domain; keep command metadata separate from execution.
- [ ] `App/ShellThreadStateCoordinator.cs` -> thread lifecycle service returning typed results and publishing frontend events.
- [ ] `App/ShellWorkspaceCoordinator.cs` / `App/ShellWorkspacePorts.cs` -> projection controllers driven by `ShellFrontendEvent` invalidation.
- [ ] `Views/ThreadWorkspaceView.cs` -> view models + command dispatcher/bindings + narrow visual services.
- [ ] `Views/ModelProvidersDialog.cs` -> `IModelProviderDialogService`.
- [ ] `Views/SidebarView.cs` and sidebar row actions -> shell commands/events.
- [ ] `Views/FileEditorWorkspaceCoordinator.cs` -> logical shell tab commands/events.
- [ ] Runtime event coordinators/history/queue/provider switch coordinators -> runtime facade/state reductions + typed frontend events.

## Current burn-down snapshot (2026-05-08)

- [ ] `Views/ThreadWorkspaceView.cs` (110 delegate seam lines) -> split prompt composer, image strip, queue strip, provider selector, status line, tab host.
- [ ] `Views/ModelProvidersDialog.cs` (33) -> model-provider dialog service.
- [ ] `Views/SidebarView.cs` (30) -> command dispatcher and projection-only row rendering.
- [ ] `App/IModelProviderPreferencePort.cs` (26) -> model-provider service/state store.
- [ ] `App/ThreadCommandPorts.cs` (25) -> command handlers + runtime facade.
- [ ] `App/ThreadTabPorts.cs` (24) -> canonical shell tab service/projection controller.
- [ ] `App/ShellWorkspacePorts.cs` (22) -> workspace projection controller and status service.
- [ ] `App/ThreadRuntimeEventCoordinator.cs` (22) -> runtime event projector publishing `ShellFrontendEvent`.
- [ ] `App/ThreadHistoryCoordinator.cs` (21) -> runtime facade/history service.
- [ ] `Views/QueuedPromptListView.cs` (21) -> queue view model + command dispatcher.
- [ ] `App/ShellThreadStateCoordinator.cs` (20) -> thread lifecycle service and typed results.
- [ ] `App/ThreadCreationCoordinator.cs` (18) -> thread lifecycle/creation service.
- [ ] `App/RawApiBackendRegistrar.cs` (15) -> model-provider adapter boundary.
- [ ] `App/IShellWorkspaceSurfacePort.cs` (14) -> `IWorkspaceSurface`.
- [ ] `App/SidebarCoordinator.cs` (13) -> sidebar projection controller.
- [ ] `Views/FileEditorWorkspaceCoordinator.cs` (13) -> file-editor tab commands/events.
- [ ] `App/IPromptSessionPort.cs` / `App/IShellSelectionPort.cs` / `App/LegacyPromptSessionPort.cs` / `App/NavigatorActionCoordinator.cs` / `App/OpenThreadStateStore.cs` (12 each) -> named prompt/session/selection/thread-session services.

Lower-count seams remain in app/view dialogs, factories, runtime reducers/renderers, and utility classes; keep the architecture inventory test as the source of truth when updating this list.
