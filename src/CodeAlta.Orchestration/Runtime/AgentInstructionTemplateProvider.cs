using CodeAlta.Catalog;
using CodeAlta.Catalog.Roles;

namespace CodeAlta.Orchestration.Runtime;

/// <summary>
/// Produces optional orchestration instruction overrides for agent sessions.
/// </summary>
public sealed class AgentInstructionTemplateProvider
{
    private const string SystemPromptResourceName = "CodeAlta.Orchestration.Runtime.Prompts.system_prompt.md";
    private static readonly Lazy<string> DefaultSystemPrompt = new(LoadDefaultSystemPrompt);

    /// <summary>
    /// Builds the instruction bundle for a coordinator session.
    /// </summary>
    /// <param name="thread">The active work thread.</param>
    /// <param name="project">The owning project, if any.</param>
    /// <param name="profile">The profile providing backend and role-specific defaults.</param>
    /// <returns>
    /// An instruction bundle containing no overrides so backend defaults remain active
    /// while orchestration-specific prompting is disabled.
    /// </returns>
    public AgentInstructionBundle BuildCoordinatorInstructions(
        WorkThreadDescriptor thread,
        ProjectDescriptor? project,
        RoleProfile profile)
    {
        ArgumentNullException.ThrowIfNull(thread);
        ArgumentNullException.ThrowIfNull(profile);

        return new AgentInstructionBundle
        {
            SystemMessage = DefaultSystemPrompt.Value
        };
    }

    /// <summary>
    /// Builds the instruction bundle for a general scoped agent session.
    /// </summary>
    /// <param name="thread">The active work thread.</param>
    /// <param name="project">The owning project, if any.</param>
    /// <param name="profile">The profile providing backend and role-specific defaults.</param>
    /// <returns>
    /// An instruction bundle containing no overrides so backend defaults remain active
    /// while orchestration-specific prompting is disabled.
    /// </returns>
    public AgentInstructionBundle BuildGeneralInstructions(
        WorkThreadDescriptor thread,
        ProjectDescriptor? project,
        RoleProfile profile)
    {
        ArgumentNullException.ThrowIfNull(thread);
        ArgumentNullException.ThrowIfNull(profile);

        return new AgentInstructionBundle
        {
            SystemMessage = DefaultSystemPrompt.Value
        };
    }

    private static string LoadDefaultSystemPrompt()
    {
        var assembly = typeof(AgentInstructionTemplateProvider).Assembly;
        using var stream = assembly.GetManifestResourceStream(SystemPromptResourceName)
            ?? throw new InvalidOperationException($"Embedded system prompt '{SystemPromptResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        var prompt = reader.ReadToEnd().Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new InvalidOperationException($"Embedded system prompt '{SystemPromptResourceName}' is empty.");
        }

        return prompt;
    }
}
