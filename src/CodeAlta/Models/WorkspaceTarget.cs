using CodeAlta.Orchestration;

namespace CodeAlta.Models;

internal abstract record WorkspaceTarget
{
    public sealed record Draft(string? ProjectId, bool IsGlobal) : WorkspaceTarget;

    public sealed record Thread(string ThreadId, string? ProjectId) : WorkspaceTarget;

    public sealed record Agent(AgentIdentity Identity) : WorkspaceTarget;

    public sealed record Fleet(AgentScope Scope) : WorkspaceTarget;
}
