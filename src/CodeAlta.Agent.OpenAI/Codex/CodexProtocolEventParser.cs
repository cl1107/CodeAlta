#pragma warning disable OPENAI001

using System.ClientModel.Primitives;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using OpenAI;
using OpenAI.Responses;

namespace CodeAlta.Agent.OpenAI.Codex;

internal static class CodexProtocolEventParser
{
    public static CodexProtocolEvent CreateInitialHttpEvent(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new CodexProtocolEvent(
            CodexProtocolTransport.Http,
            Type: null,
            Update: null,
            ParseHeaders(response.Headers, response.Content.Headers));
    }

    public static CodexProtocolEvent Parse(CodexProtocolTransport transport, BinaryData payload, string? sseEventType = null)
    {
        ArgumentNullException.ThrowIfNull(payload);
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Empty(transport, sseEventType);
            }

            var root = document.RootElement;
            var type = !string.IsNullOrWhiteSpace(sseEventType) &&
                       !string.Equals(sseEventType, "message", StringComparison.Ordinal)
                ? sseEventType
                : GetString(root, "type");
            var normalizedPayload = string.Equals(type, "response.done", StringComparison.Ordinal)
                ? NormalizeLegacyDone(root)
                : payload;
            var metadata = ParseMetadata(root);
            var terminal = ParseTerminal(root, type);
            StreamingResponseUpdate? update = null;

            if (!IsMetadataOnly(type) &&
                (string.Equals(type, "error", StringComparison.Ordinal) ||
                 type?.StartsWith("response.", StringComparison.Ordinal) == true))
            {
                try
                {
                    update = ModelReaderWriter.Read<StreamingResponseUpdate>(
                        normalizedPayload,
                        new ModelReaderWriterOptions("J"),
                        OpenAIContext.Default);
                }
                catch (JsonException ex)
                {
                    // Codex intentionally tolerates malformed and newly introduced non-terminal events.
                    if (IsTerminal(type))
                    {
                        throw new OpenAIResponsesProtocolException(
                            OpenAIResponsesProtocolErrorCode.UnsupportedTerminalResponseUpdate,
                            "Codex returned a malformed terminal response update.",
                            ex);
                    }
                }
            }

            return new CodexProtocolEvent(transport, type, update, metadata, terminal);
        }
        catch (JsonException)
        {
            return Empty(transport, sseEventType);
        }
    }

    private static CodexProtocolEvent Empty(CodexProtocolTransport transport, string? type)
        => new(transport, type, null, new CodexResponseMetadata());

    private static CodexResponseMetadata ParseHeaders(
        HttpResponseHeaders headers,
        HttpContentHeaders contentHeaders)
    {
        string? Header(string name)
            => TryGetHeader(headers, name) ?? TryGetHeader(contentHeaders, name);

        return new CodexResponseMetadata(
            RequestId: Header("x-request-id"),
            EffectiveModel: Header("OpenAI-Model"),
            ModelsETag: Header("x-models-etag"),
            ReasoningIncluded: ParseBoolean(Header("x-reasoning-included")),
            SafetyBuffering: CreateHeaderSafetyBuffering(Header("x-codex-safety-buffering"), Header("x-codex-retry-model")),
            RateLimits: ParseRateLimitHeaders(headers, contentHeaders));
    }

    private static CodexResponseMetadata ParseMetadata(JsonElement root)
    {
        var headers = GetObject(root, "headers") ?? GetNestedObject(root, "response", "headers");
        var metadata = GetObject(root, "metadata") ?? GetNestedObject(root, "response", "metadata");
        var safety = GetObject(root, "safety_buffering") ??
                     GetNestedObject(root, "response", "safety_buffering") ??
                     (metadata is { } metadataObject ? GetObject(metadataObject, "safety_buffering") : null);

        var retryModelElement = default(JsonElement);
        var retryModelPresent = safety is { } safetyObject && safetyObject.TryGetProperty("retry_model"u8, out retryModelElement);
        var retryModel = retryModelPresent && retryModelElement.ValueKind == JsonValueKind.String
            ? retryModelElement.GetString()
            : null;
        var parsedSafety = safety is null
            ? null
            : new CodexSafetyBuffering(
                retryModelPresent,
                retryModel,
                GetString(safety.Value, "treatment"));

        return new CodexResponseMetadata(
            RequestId: GetString(headers, "x-request-id"),
            EffectiveModel: GetString(headers, "OpenAI-Model") ?? GetString(root, "model"),
            ModelsETag: GetString(headers, "x-models-etag"),
            ReasoningIncluded: GetBoolean(headers, "x-reasoning-included"),
            SafetyBuffering: parsedSafety,
            RateLimits: ParseRateLimits(root, headers),
            VerificationRecommendation: GetBoundedJson(metadata, "openai_verification_recommendation"),
            TurnModeration: GetBoundedJson(metadata, "turn_moderation"));
    }

    private static CodexTerminalMetadata? ParseTerminal(JsonElement root, string? type)
    {
        if (!IsTerminal(type))
        {
            return null;
        }

        var response = GetObject(root, "response");
        return new CodexTerminalMetadata(GetBoolean(response, "end_turn"));
    }

    private static IReadOnlyList<CodexNamedRateLimitSnapshot>? ParseRateLimits(JsonElement root, JsonElement? headers)
    {
        var limits = new List<CodexNamedRateLimitSnapshot>();
        var rateLimitObject = GetObject(root, "rate_limits") ??
                              (string.Equals(GetString(root, "type"), "codex.rate_limits", StringComparison.Ordinal) ? root : null);
        if (rateLimitObject is { } rateLimits)
        {
            foreach (var property in rateLimits.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                limits.Add(new CodexNamedRateLimitSnapshot(
                    property.Name,
                    GetDouble(property.Value, "used_percent"),
                    GetInt64(property.Value, "limit"),
                    GetInt64(property.Value, "remaining"),
                    GetUnixTime(property.Value, "reset_at")));
            }
        }

        if (headers is { } headerObject)
        {
            AddHeaderRateLimits(headerObject, limits);
        }

        return limits.Count == 0 ? null : limits.AsReadOnly();
    }

    private static IReadOnlyList<CodexNamedRateLimitSnapshot>? ParseRateLimitHeaders(
        HttpResponseHeaders headers,
        HttpContentHeaders contentHeaders)
    {
        var values = headers.Concat(contentHeaders)
            .Where(static header => header.Key.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(static header => header.Key, static header => header.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);
        var limits = new List<CodexNamedRateLimitSnapshot>();
        foreach (var pair in values.Where(static pair => pair.Key.EndsWith("-used-percent", StringComparison.OrdinalIgnoreCase)))
        {
            var name = pair.Key[2..^"-used-percent".Length];
            limits.Add(new CodexNamedRateLimitSnapshot(
                name,
                double.TryParse(pair.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var used) ? used : null,
                TryGetLong(values, $"x-{name}-limit"),
                TryGetLong(values, $"x-{name}-remaining"),
                TryGetUnixTime(values, $"x-{name}-reset-at")));
        }

        return limits.Count == 0 ? null : limits.AsReadOnly();
    }

    private static void AddHeaderRateLimits(JsonElement headers, List<CodexNamedRateLimitSnapshot> limits)
    {
        foreach (var property in headers.EnumerateObject())
        {
            if (!property.Name.StartsWith("x-", StringComparison.OrdinalIgnoreCase) ||
                !property.Name.EndsWith("-used-percent", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = property.Name[2..^"-used-percent".Length];
            limits.Add(new CodexNamedRateLimitSnapshot(name, GetDouble(property.Value), null, null, null));
        }
    }

    private static CodexSafetyBuffering? CreateHeaderSafetyBuffering(string? treatment, string? retryModel)
        => treatment is null && retryModel is null
            ? null
            : new CodexSafetyBuffering(retryModel is not null, retryModel, treatment);

    private static BinaryData NormalizeLegacyDone(JsonElement root)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals("type"u8))
                {
                    writer.WriteString("type"u8, "response.completed");
                }
                else
                {
                    property.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }

        return BinaryData.FromBytes(stream.ToArray());
    }

    private static bool IsMetadataOnly(string? type)
        => string.IsNullOrWhiteSpace(type) ||
           type is "response.metadata" or "response.server_model" or "response.models_etag" or "safety_buffering" ||
           type.StartsWith("response.rate_limits", StringComparison.Ordinal) ||
           type.StartsWith("response.model_verification", StringComparison.Ordinal) ||
           type.StartsWith("codex.", StringComparison.Ordinal);

    internal static bool IsTerminal(string? type)
        => type is "response.completed" or "response.incomplete" or "response.failed" or "response.done";

    private static JsonElement? GetObject(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object &&
           element.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.Object
            ? value
            : null;

    private static JsonElement? GetNestedObject(JsonElement element, string objectName, string name)
        => GetObject(element, objectName) is { } nested ? GetObject(nested, name) : null;

    private static string? GetString(JsonElement? element, string name)
        => element is { ValueKind: JsonValueKind.Object } value &&
           value.TryGetProperty(name, out var property) &&
           property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool? GetBoolean(JsonElement? element, string name)
    {
        if (element is not { ValueKind: JsonValueKind.Object } value || !value.TryGetProperty(name, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => ParseBoolean(property.GetString()),
            _ => null,
        };
    }

    private static bool? ParseBoolean(string? value)
        => bool.TryParse(value, out var parsed) ? parsed : null;

    private static double? GetDouble(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) ? GetDouble(value) : null;

    private static double? GetDouble(JsonElement value)
        => value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var parsed)
            ? parsed
            : value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : null;

    private static long? GetInt64(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.TryGetInt64(out var parsed) ? parsed : null;

    private static DateTimeOffset? GetUnixTime(JsonElement element, string name)
        => GetInt64(element, name) is { } seconds ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;

    private static string? GetBoundedJson(JsonElement? element, string name)
    {
        if (element is not { ValueKind: JsonValueKind.Object } value || !value.TryGetProperty(name, out var property))
        {
            return null;
        }

        var json = property.GetRawText();
        return json.Length <= 4096 ? json : json[..4096];
    }

    private static string? TryGetHeader(HttpHeaders headers, string name)
        => headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    private static long? TryGetLong(IReadOnlyDictionary<string, string?> values, string name)
        => values.TryGetValue(name, out var value) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static DateTimeOffset? TryGetUnixTime(IReadOnlyDictionary<string, string?> values, string name)
        => TryGetLong(values, name) is { } seconds ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;
}
