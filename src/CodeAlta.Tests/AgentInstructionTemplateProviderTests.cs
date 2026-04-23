using CodeAlta.Agent;
using CodeAlta.Catalog;
using CodeAlta.Catalog.Roles;
using CodeAlta.Catalog.Skills;
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

    [TestMethod]
    public async Task BuildCoordinatorInstructions_AdvertisesAvailableSkills()
    {
        using var temp = TestTempDirectory.Create();
        var projectRoot = Path.Combine(temp.Path, "repo");
        Directory.CreateDirectory(projectRoot);
        var skillRoot = Path.Combine(projectRoot, ".alta", "skills", "code-review");
        Directory.CreateDirectory(skillRoot);
        await File.WriteAllTextAsync(
            Path.Combine(skillRoot, "SKILL.md"),
            """
            ---
            name: code-review
            description: Review code for correctness and regressions.
            ---
            # Code Review

            Focus on correctness and regressions.
            """).ConfigureAwait(false);

        var provider = new AgentInstructionTemplateProvider(
            new SkillCatalog(),
            new CatalogOptions { GlobalRoot = temp.Path });
        var project = CreateProject(projectRoot);

        var instructions = provider.BuildCoordinatorInstructions(CreateThread(projectRoot, project.Id), project, CreateProfile());

        Assert.IsNotNull(instructions.DeveloperInstructions);
        StringAssert.Contains(instructions.DeveloperInstructions, "<available_skills>");
        StringAssert.Contains(instructions.DeveloperInstructions, "code-review");
        StringAssert.Contains(instructions.DeveloperInstructions, "project .alta/skills");
    }

    private static WorkThreadDescriptor CreateThread(string workingDirectory = @"C:\code\CodeAlta", string? projectId = null)
        => new()
        {
            ThreadId = "thread-1",
            Kind = string.IsNullOrWhiteSpace(projectId) ? WorkThreadKind.GlobalThread : WorkThreadKind.ProjectThread,
            BackendId = AgentBackendIds.OpenAIResponses.Value,
            BackendSessionId = "backend-session-1",
            ProjectRef = projectId,
            WorkingDirectory = workingDirectory,
            Title = "Thread",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            LastActiveAt = DateTimeOffset.UtcNow,
        };

    private static ProjectDescriptor CreateProject(string projectPath)
        => new()
        {
            Id = ProjectId.NewVersion7().ToString(),
            Slug = "repo",
            DisplayName = "Repo",
            ProjectPath = projectPath,
            DefaultBranch = "main",
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
