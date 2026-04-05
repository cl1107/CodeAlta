using CodeAlta.Agent;
using CodeAlta.Search;

namespace CodeAlta.Presentation.Prompting;

internal sealed record ProjectFilePromptInputResult(
    string NormalizedPromptText,
    AgentInput Input,
    IReadOnlyList<ProjectFileResolution> ResolvedReferences);
