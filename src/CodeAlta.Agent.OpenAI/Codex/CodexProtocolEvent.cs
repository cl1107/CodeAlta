#pragma warning disable OPENAI001

using OpenAI.Responses;

namespace CodeAlta.Agent.OpenAI.Codex;

internal enum CodexProtocolTransport
{
    Http,
    WebSocket,
}

internal sealed record CodexProtocolEvent(
    CodexProtocolTransport Transport,
    string? Type,
    StreamingResponseUpdate? Update,
    CodexResponseMetadata Metadata,
    CodexTerminalMetadata? Terminal = null);

internal sealed record CodexTerminalMetadata(bool? EndTurn);

internal sealed record CodexResponseMetadata(
    string? RequestId = null,
    string? EffectiveModel = null,
    string? ModelsETag = null,
    bool? ReasoningIncluded = null,
    CodexSafetyBuffering? SafetyBuffering = null,
    IReadOnlyList<CodexNamedRateLimitSnapshot>? RateLimits = null,
    string? VerificationRecommendation = null,
    string? TurnModeration = null);

internal sealed record CodexSafetyBuffering(
    bool RetryModelPresent,
    string? RetryModel,
    string? Treatment);

internal sealed record CodexNamedRateLimitSnapshot(
    string Name,
    double? UsedPercent,
    long? Limit,
    long? Remaining,
    DateTimeOffset? ResetAt);
