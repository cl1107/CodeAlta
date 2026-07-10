using System.Text.Json;
using CodeAlta.Agent;

namespace CodeAlta.Agent.OpenAI.Codex;

internal readonly record struct CodexSubscriptionModelCapabilities(
    bool SupportsReasoningSummaries,
    bool SupportsVerbosity,
    bool SupportsParallelToolCalls,
    bool SupportsImageDetailOriginal,
    bool UseResponsesLite)
{
    public static CodexSubscriptionModelCapabilities FromModel(AgentModelInfo? model)
        => new(
            GetBoolean(model, "supportsReasoningSummaries", fallback: true),
            GetBoolean(model, "supportVerbosity", fallback: true),
            GetBoolean(model, "supportsParallelToolCalls", fallback: true),
            GetBoolean(model, "supportsImageDetailOriginal", fallback: true),
            GetBoolean(model, "useResponsesLite", fallback: false));

    private static bool GetBoolean(AgentModelInfo? model, string key, bool fallback)
    {
        if (model?.Capabilities is null || !model.Capabilities.TryGetValue(key, out var value))
        {
            return fallback;
        }

        return value switch
        {
            bool boolean => boolean,
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            _ => fallback,
        };
    }
}
