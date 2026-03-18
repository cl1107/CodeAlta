using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeAlta.Agent;

/// <summary>
/// Represents normalized context-window and token-usage information for a session.
/// </summary>
/// <param name="CurrentTokens">The current number of tokens in the active context window when known.</param>
/// <param name="TokenLimit">The total context-window limit for the active model when known.</param>
/// <param name="MessageCount">The number of messages currently contributing to the active context window when known.</param>
/// <param name="UpdatedAt">The time the usage snapshot was last updated.</param>
/// <param name="Details">Optional backend-specific usage details.</param>
public sealed record AgentSessionUsage(
    long? CurrentTokens,
    long? TokenLimit,
    int? MessageCount,
    DateTimeOffset UpdatedAt,
    AgentSessionUsageDetails? Details = null)
{
    /// <summary>
    /// Gets the percentage of the context window currently in use when both values are available.
    /// </summary>
    public double? WindowUsagePercentage =>
        TokenLimit is > 0 && CurrentTokens is >= 0
            ? (CurrentTokens.Value * 100d) / TokenLimit.Value
            : null;
}

/// <summary>
/// Base type for backend-specific session usage information.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(CodexSessionUsageDetails), "codex")]
[JsonDerivedType(typeof(CopilotSessionUsageDetails), "copilot")]
public abstract record AgentSessionUsageDetails;

/// <summary>
/// Codex-specific usage details.
/// </summary>
/// <param name="LastTurnUsage">Token usage for the most recent turn when available.</param>
/// <param name="TotalUsage">Cumulative thread token usage when available.</param>
/// <param name="ModelContextWindow">The model context-window size reported by Codex when available.</param>
/// <param name="RateLimits">The latest Codex account rate-limit snapshot when available.</param>
public sealed record CodexSessionUsageDetails(
    CodexTokenUsage? LastTurnUsage = null,
    CodexTokenUsage? TotalUsage = null,
    long? ModelContextWindow = null,
    CodexRateLimitSnapshot? RateLimits = null)
    : AgentSessionUsageDetails;

/// <summary>
/// Copilot-specific usage details.
/// </summary>
/// <param name="LastAssistantUsage">Usage for the most recent Copilot model call when available.</param>
/// <param name="LastCompaction">Usage and reduction details for the most recent compaction when available.</param>
/// <param name="QuotaSnapshots">Opaque quota snapshots surfaced by the Copilot backend.</param>
public sealed record CopilotSessionUsageDetails(
    CopilotAssistantUsage? LastAssistantUsage = null,
    CopilotCompactionUsage? LastCompaction = null,
    CopilotQuotaSnapshot[]? QuotaSnapshots = null)
    : AgentSessionUsageDetails;

/// <summary>
/// Codex token-usage breakdown.
/// </summary>
/// <param name="CachedInputTokens">The number of cached input tokens reused.</param>
/// <param name="InputTokens">The number of fresh input tokens consumed.</param>
/// <param name="OutputTokens">The number of output tokens produced.</param>
/// <param name="ReasoningOutputTokens">The number of reasoning output tokens produced.</param>
/// <param name="TotalTokens">The total number of tokens represented by the breakdown.</param>
public sealed record CodexTokenUsage(
    long CachedInputTokens,
    long InputTokens,
    long OutputTokens,
    long ReasoningOutputTokens,
    long TotalTokens);

/// <summary>
/// Codex rate-limit snapshot.
/// </summary>
/// <param name="LimitId">The backend-specific limit identifier.</param>
/// <param name="LimitName">The backend-specific limit display name.</param>
/// <param name="PlanType">The active Codex plan type when known.</param>
/// <param name="Primary">The primary rate-limit window when available.</param>
/// <param name="Secondary">The secondary rate-limit window when available.</param>
public sealed record CodexRateLimitSnapshot(
    string? LimitId,
    string? LimitName,
    string? PlanType,
    CodexRateLimitWindow? Primary,
    CodexRateLimitWindow? Secondary);

/// <summary>
/// Codex rate-limit window details.
/// </summary>
/// <param name="UsedPercent">The percentage of the window currently consumed.</param>
/// <param name="ResetsAt">The time the window resets when known.</param>
/// <param name="WindowDurationMinutes">The rate-limit window duration in minutes when known.</param>
public sealed record CodexRateLimitWindow(
    int UsedPercent,
    DateTimeOffset? ResetsAt,
    long? WindowDurationMinutes);

/// <summary>
/// Copilot assistant-call token and billing usage.
/// </summary>
/// <param name="Model">The model that serviced the call.</param>
/// <param name="InputTokens">The number of input tokens consumed.</param>
/// <param name="OutputTokens">The number of output tokens produced.</param>
/// <param name="CacheReadTokens">The number of tokens read from prompt cache.</param>
/// <param name="CacheWriteTokens">The number of tokens written to prompt cache.</param>
/// <param name="Cost">The backend-reported request cost when available.</param>
/// <param name="DurationMs">The backend-reported request duration in milliseconds when available.</param>
/// <param name="Initiator">The initiator for the call when reported.</param>
/// <param name="ParentToolCallId">The parent tool call identifier when this usage belongs to a sub-agent/tool request.</param>
/// <param name="ReasoningEffort">The reasoning-effort setting used for the request when reported.</param>
/// <param name="TotalNanoAiu">The Copilot AIU cost reported by the backend when available.</param>
/// <param name="TokenDetails">Additional backend token details when available.</param>
public sealed record CopilotAssistantUsage(
    string Model,
    long? InputTokens = null,
    long? OutputTokens = null,
    long? CacheReadTokens = null,
    long? CacheWriteTokens = null,
    double? Cost = null,
    double? DurationMs = null,
    string? Initiator = null,
    string? ParentToolCallId = null,
    string? ReasoningEffort = null,
    double? TotalNanoAiu = null,
    CopilotTokenDetail[]? TokenDetails = null);

/// <summary>
/// Copilot token-detail entry.
/// </summary>
/// <param name="TokenType">The token category label.</param>
/// <param name="TokenCount">The number of tokens in that category.</param>
public sealed record CopilotTokenDetail(
    string TokenType,
    long TokenCount);

/// <summary>
/// Copilot compaction usage and reduction details.
/// </summary>
/// <param name="Success">Whether the compaction completed successfully.</param>
/// <param name="PreCompactionTokens">The token count before compaction when reported.</param>
/// <param name="PostCompactionTokens">The token count after compaction when reported.</param>
/// <param name="PreCompactionMessages">The message count before compaction when reported.</param>
/// <param name="MessagesRemoved">The number of messages removed by compaction when reported.</param>
/// <param name="TokensRemoved">The number of tokens removed by compaction when reported.</param>
/// <param name="TokensUsed">The token usage for the compaction model call when reported.</param>
/// <param name="SummaryContent">The backend-provided compaction summary when reported.</param>
public sealed record CopilotCompactionUsage(
    bool Success,
    long? PreCompactionTokens = null,
    long? PostCompactionTokens = null,
    int? PreCompactionMessages = null,
    int? MessagesRemoved = null,
    long? TokensRemoved = null,
    CopilotCompactionTokenUsage? TokensUsed = null,
    string? SummaryContent = null);

/// <summary>
/// Copilot token usage for the compaction LLM call.
/// </summary>
/// <param name="InputTokens">The number of input tokens consumed.</param>
/// <param name="OutputTokens">The number of output tokens produced.</param>
/// <param name="CachedInputTokens">The number of cached input tokens reused.</param>
public sealed record CopilotCompactionTokenUsage(
    long InputTokens,
    long OutputTokens,
    long CachedInputTokens);

/// <summary>
/// Named Copilot quota snapshot payload.
/// </summary>
/// <param name="Name">The quota identifier.</param>
/// <param name="Payload">The opaque payload emitted by the Copilot backend.</param>
public sealed record CopilotQuotaSnapshot(
    string Name,
    JsonElement Payload);
