using CodeAlta.Agent;
using CodeAlta.Catalog;
using CodeAlta.Catalog.Roles;
using CodeAlta.Orchestration.Runtime;

namespace CodeAlta.Tests;

[TestClass]
public sealed class AgentInstructionTemplateProviderTests
{
    [TestMethod]
    public void BuildGeneralInstructions_LoadsEmbeddedDefaultSystemPrompt()
    {
        var provider = new AgentInstructionTemplateProvider();

        var instructions = provider.BuildGeneralInstructions(CreateThread(), project: null, CreateProfile());

        Assert.IsFalse(string.IsNullOrWhiteSpace(instructions.SystemMessage));
        StringAssert.Contains(instructions.SystemMessage, "You are CodeAlta");
        Assert.IsNull(instructions.DeveloperInstructions);
    }

    [TestMethod]
    public void BuildCoordinatorInstructions_LoadsEmbeddedDefaultSystemPrompt()
    {
        var provider = new AgentInstructionTemplateProvider();

        var instructions = provider.BuildCoordinatorInstructions(CreateThread(), project: null, CreateProfile());

        Assert.IsFalse(string.IsNullOrWhiteSpace(instructions.SystemMessage));
        StringAssert.Contains(instructions.SystemMessage, "software engineering agent");
        Assert.IsNull(instructions.DeveloperInstructions);
    }

    private static WorkThreadDescriptor CreateThread()
        => new()
        {
            ThreadId = "thread-1",
            Kind = WorkThreadKind.GlobalThread,
            BackendId = AgentBackendIds.OpenAIResponses.Value,
            BackendSessionId = "backend-session-1",
            WorkingDirectory = @"C:\code\CodeAlta",
            Title = "Thread",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            LastActiveAt = DateTimeOffset.UtcNow,
        };

    private static RoleProfile CreateProfile()
        => new()
        {
            RoleId = "general",
            Name = "General",
            Description = "General role",
            Instructions = "Follow the task.",
            ToolsPolicy = new RoleToolsPolicy(),
            SourcePath = "role.md",
        };
}
