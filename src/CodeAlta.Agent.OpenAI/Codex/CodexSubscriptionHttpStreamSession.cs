#pragma warning disable OPENAI001

using System.ClientModel.Primitives;
using System.Net.Http.Headers;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using OpenAI;
using OpenAI.Responses;

namespace CodeAlta.Agent.OpenAI.Codex;

internal sealed class CodexSubscriptionHttpStreamSession
{
    private readonly OpenAIProviderOptions _provider;
    private readonly OpenAICodexSubscriptionOptions _options;
    private readonly OpenAICodexSubscriptionAuthManager _authManager;
    private readonly CodexSubscriptionRequestContext _requestContext;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly OpenAIProtocolTraceLogger? _trace;

    public CodexSubscriptionHttpStreamSession(
        OpenAIProviderOptions provider,
        OpenAICodexSubscriptionAuthManager authManager,
        CodexSubscriptionRequestContext requestContext,
        OpenAIProtocolTraceLogger? trace = null)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(authManager);
        ArgumentNullException.ThrowIfNull(requestContext);

        _provider = provider;
        _options = provider.CodexSubscription ?? throw new ArgumentException("Codex subscription options are required.", nameof(provider));
        _authManager = authManager;
        _requestContext = requestContext;
        _httpClient = provider.HttpClient ?? provider.CodexSubscriptionHttpClient ?? new HttpClient();
        _ownsHttpClient = provider.HttpClient is null && provider.CodexSubscriptionHttpClient is null;
        _trace = trace;
    }

    public async IAsyncEnumerable<CodexProtocolEvent> CreateResponseStreamingAsync(
        CreateResponseOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        using var timeoutSource = CreateTimeoutSource(cancellationToken);
        var effectiveCancellation = timeoutSource?.Token ?? cancellationToken;
        using var request = await CreateRequestAsync(options, effectiveCancellation).ConfigureAwait(false);
        TraceRequest(request);

        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, effectiveCancellation).ConfigureAwait(false);
            TraceResponse(response);
            CaptureTurnState(response);
            if (!response.IsSuccessStatusCode)
            {
                throw await CreateHttpExceptionAsync(response, effectiveCancellation).ConfigureAwait(false);
            }

            yield return CodexProtocolEventParser.CreateInitialHttpEvent(response);
            await using var stream = await response.Content.ReadAsStreamAsync(effectiveCancellation).ConfigureAwait(false);
            await foreach (var sseEvent in SseParser.Create(stream).EnumerateAsync(effectiveCancellation).ConfigureAwait(false))
            {
                if (string.Equals(sseEvent.Data, "[DONE]", StringComparison.Ordinal))
                {
                    yield break;
                }

                _trace?.WriteLine($"<<< SSE event={sseEvent.EventType ?? "message"} bytes={System.Text.Encoding.UTF8.GetByteCount(sseEvent.Data)}");
                yield return CodexProtocolEventParser.Parse(
                    CodexProtocolTransport.Http,
                    BinaryData.FromString(sseEvent.Data),
                    sseEvent.EventType);
            }
        }
        finally
        {
            response?.Dispose();
            if (_ownsHttpClient)
            {
                _httpClient.Dispose();
            }
        }
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        CreateResponseOptions options,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, ResolveResponsesUri(_provider.BaseUri));
        var payload = ModelReaderWriter.Write(options, new ModelReaderWriterOptions("J"), OpenAIContext.Default);
        request.Content = new ByteArrayContent(payload.ToMemory().ToArray());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        _trace?.WriteLine($">>> body: {payload}");

        var credential = await _authManager.GetCredentialAsync(cancellationToken).ConfigureAwait(false);
        var account = await _authManager.GetAccountContextAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential.AccessToken);
        SetHeader(request, "originator", "codealta");
        SetHeader(request, "User-Agent", OpenAIProviderSdkFactory.CreateCodeAltaUserAgentApplicationId());
        SetHeader(request, "session_id", _requestContext.SessionId);
        if (_options.SendResponsesBetaHeader)
        {
            SetHeader(request, "OpenAI-Beta", "responses=experimental");
        }

        var accountId = !string.IsNullOrWhiteSpace(_options.AccountId) ? _options.AccountId : account.AccountId;
        if (!string.IsNullOrWhiteSpace(accountId))
        {
            SetHeader(request, "ChatGPT-Account-Id", accountId);
        }

        if (account.IsFedRamp)
        {
            SetHeader(request, "X-OpenAI-Fedramp", "true");
        }

        var extraHeaders = OpenAIModelRequestOverrides.MergeHeaders(
            _provider.ExtraHeaders,
            OpenAIModelRequestOverrides.Find(_provider.ModelRequestOverrides, options.Model));
        if (extraHeaders is not null)
        {
            foreach (var header in extraHeaders)
            {
                SetHeader(request, header.Key, header.Value);
            }
        }

        foreach (var header in _requestContext.CompatibilityHeaders)
        {
            SetHeader(request, header.Key, header.Value);
        }

        if (_requestContext.TurnState.TryGetCapturedState(out var turnState))
        {
            SetHeader(request, "x-codex-turn-state", turnState);
        }

        return request;
    }

    private CancellationTokenSource? CreateTimeoutSource(CancellationToken cancellationToken)
    {
        if (_provider.NetworkTimeout is not { } timeout)
        {
            return null;
        }

        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(timeout);
        return source;
    }

    private void CaptureTurnState(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("x-codex-turn-state", out var values) &&
            values.FirstOrDefault() is { } state &&
            !string.IsNullOrWhiteSpace(state))
        {
            _requestContext.TurnState.Capture(state);
        }
    }

    private static async Task<HttpRequestException> CreateHttpExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var detail = string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body;
        var exception = new HttpRequestException(
            $"ChatGPT/Codex HTTP request failed with status {(int)response.StatusCode} ({response.StatusCode}): {detail}",
            inner: null,
            response.StatusCode);
        if (response.Headers.RetryAfter is { } retryAfter)
        {
            exception.Data["Retry-After"] = retryAfter.ToString();
        }

        return exception;
    }

    private void TraceRequest(HttpRequestMessage request)
    {
        if (_trace is null)
        {
            return;
        }

        _trace.WriteLine($">>> {request.Method} {request.RequestUri}");
        foreach (var header in request.Headers)
        {
            var value = IsSensitiveHeader(header.Key) ? "<redacted>" : string.Join(",", header.Value);
            _trace.WriteLine($">>> {header.Key}: {value}");
        }
    }

    private void TraceResponse(HttpResponseMessage response)
    {
        if (_trace is null)
        {
            return;
        }

        _trace.WriteLine($"<<< HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
        foreach (var header in response.Headers)
        {
            var value = IsSensitiveHeader(header.Key) ? "<redacted>" : string.Join(",", header.Value);
            _trace.WriteLine($"<<< {header.Key}: {value}");
        }
    }

    private static bool IsSensitiveHeader(string name)
        => name.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
           name.Equals("Cookie", StringComparison.OrdinalIgnoreCase) ||
           name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) ||
           name.Equals("api-key", StringComparison.OrdinalIgnoreCase) ||
           name.Equals("x-api-key", StringComparison.OrdinalIgnoreCase);

    private static void SetHeader(HttpRequestMessage request, string name, string value)
    {
        request.Headers.Remove(name);
        request.Headers.TryAddWithoutValidation(name, value);
    }

    private static Uri ResolveResponsesUri(Uri? baseUri)
    {
        var builder = new UriBuilder(baseUri ?? new Uri("https://chatgpt.com/backend-api/codex"));
        var path = builder.Path.TrimEnd('/');
        builder.Path = path.EndsWith("/responses", StringComparison.OrdinalIgnoreCase) ? path : path + "/responses";
        return builder.Uri;
    }
}
