using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CodeAlta.Agent.LocalRuntime.Tools;

/// <summary>
/// Creates the default non-provider built-in tools used by local raw-API sessions.
/// </summary>
public static class LocalAgentBuiltInToolFactory
{
    private static readonly JsonElement ReadFileSchema = ParseSchema(
        """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Path to the file to read." },
            "offset": { "type": "integer", "description": "1-based line offset.", "minimum": 1 },
            "limit": { "type": "integer", "description": "Maximum number of lines to return.", "minimum": 1 }
          },
          "required": ["path"],
          "additionalProperties": false
        }
        """);

    private static readonly JsonElement ListDirSchema = ParseSchema(
        """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Directory to list. Defaults to the session working directory." }
          },
          "additionalProperties": false
        }
        """);

    private static readonly JsonElement GrepFilesSchema = ParseSchema(
        """
        {
          "type": "object",
          "properties": {
            "pattern": { "type": "string", "description": "Search pattern." },
            "path": { "type": "string", "description": "Root directory to search. Defaults to the session working directory." },
            "glob": { "type": "string", "description": "Optional file-name glob like *.cs." },
            "caseSensitive": { "type": "boolean", "description": "Whether matching is case-sensitive." },
            "maxMatches": { "type": "integer", "description": "Maximum matches to return.", "minimum": 1 }
          },
          "required": ["pattern"],
          "additionalProperties": false
        }
        """);

    private static readonly JsonElement WebGetSchema = ParseSchema(
        """
        {
          "type": "object",
          "properties": {
            "url": { "type": "string", "description": "Absolute URL to fetch." },
            "timeoutSeconds": { "type": "integer", "description": "Optional timeout override in seconds.", "minimum": 1 }
          },
          "required": ["url"],
          "additionalProperties": false
        }
        """);

    private static readonly JsonElement ViewImageSchema = ParseSchema(
        """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Path to the local image file." }
          },
          "required": ["path"],
          "additionalProperties": false
        }
        """);

    private static readonly JsonElement RequestUserInputSchema = ParseSchema(
        """
        {
          "type": "object",
          "properties": {
            "prompts": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "id": { "type": "string" },
                  "question": { "type": "string" },
                  "header": { "type": "string" },
                  "allowFreeform": { "type": "boolean" },
                  "isSecret": { "type": "boolean" },
                  "options": {
                    "type": "array",
                    "items": {
                      "type": "object",
                      "properties": {
                        "label": { "type": "string" },
                        "description": { "type": "string" }
                      },
                      "required": ["label"],
                      "additionalProperties": false
                    }
                  }
                },
                "required": ["id", "question"],
                "additionalProperties": false
              }
            }
          },
          "required": ["prompts"],
          "additionalProperties": false
        }
        """);

    /// <summary>
    /// Creates the default built-in tools.
    /// </summary>
    /// <param name="options">Tool options.</param>
    /// <returns>The built-in tools.</returns>
    public static IReadOnlyList<AgentToolDefinition> CreateDefaultTools(LocalAgentBuiltInToolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var httpClient = options.HttpClient ?? new HttpClient();
        return
        [
            new AgentToolDefinition(
                new AgentToolSpec(
                    "read_file",
                    "Read the contents of a text file or return a local image attachment.",
                    ReadFileSchema),
                (invocation, cancellationToken) => ReadFileAsync(options, invocation, cancellationToken)),
            new AgentToolDefinition(
                new AgentToolSpec(
                    "list_dir",
                    "List the direct children of a directory.",
                    ListDirSchema),
                (invocation, cancellationToken) => ListDirectoryAsync(options, invocation, cancellationToken)),
            new AgentToolDefinition(
                new AgentToolSpec(
                    "grep_files",
                    "Search text files under a directory for a pattern.",
                    GrepFilesSchema),
                (invocation, cancellationToken) => GrepFilesAsync(options, invocation, cancellationToken)),
            new AgentToolDefinition(
                new AgentToolSpec(
                    "webget",
                    "Fetch web content from a known URL with basic size and content-type safeguards.",
                    WebGetSchema),
                (invocation, cancellationToken) => WebGetAsync(options, httpClient, invocation, cancellationToken)),
            new AgentToolDefinition(
                new AgentToolSpec(
                    "view_image",
                    "Return a local image as an attachment-like result.",
                    ViewImageSchema),
                (invocation, cancellationToken) => ViewImageAsync(options, invocation, cancellationToken)),
            new AgentToolDefinition(
                new AgentToolSpec(
                    "request_user_input",
                    "Pause and request structured user input from the host.",
                    RequestUserInputSchema),
                (invocation, cancellationToken) => RequestUserInputAsync(options, invocation, cancellationToken)),
        ];
    }

    private static async Task<AgentToolResult> ReadFileAsync(
        LocalAgentBuiltInToolOptions options,
        AgentToolInvocation invocation,
        CancellationToken cancellationToken)
    {
        var path = GetRequiredString(invocation.Arguments, "path");
        var resolvedPath = ResolvePath(options.WorkingDirectory, path);
        if (!File.Exists(resolvedPath))
        {
            return Failure($"File '{resolvedPath}' was not found.");
        }

        if (IsImagePath(resolvedPath))
        {
            return new AgentToolResult(
                true,
                [
                    new AgentToolResultItem.Text($"Image: {resolvedPath}"),
                    new AgentToolResultItem.ImageUrl(new Uri(resolvedPath).AbsoluteUri),
                ]);
        }

        var offset = Math.Max(1, GetOptionalInt(invocation.Arguments, "offset") ?? 1);
        var requestedLimit = GetOptionalInt(invocation.Arguments, "limit") ?? options.DefaultReadFileLineLimit;
        var limit = Math.Clamp(requestedLimit, 1, options.MaxReadFileLineLimit);

        var lines = new List<string>(limit);
        using var reader = new StreamReader(resolvedPath);
        string? line;
        for (var lineNumber = 1; (line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null; lineNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (lineNumber < offset)
            {
                continue;
            }

            lines.Add($"{lineNumber,5}: {line}");
            if (lines.Count >= limit)
            {
                break;
            }
        }

        return new AgentToolResult(
            true,
            [new AgentToolResultItem.Text(string.Join(Environment.NewLine, lines))]);
    }

    private static Task<AgentToolResult> ListDirectoryAsync(
        LocalAgentBuiltInToolOptions options,
        AgentToolInvocation invocation,
        CancellationToken cancellationToken)
    {
        var path = GetOptionalString(invocation.Arguments, "path");
        var resolvedPath = ResolvePath(options.WorkingDirectory, path);
        if (!Directory.Exists(resolvedPath))
        {
            return Task.FromResult(Failure($"Directory '{resolvedPath}' was not found."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var entries = Directory.EnumerateFileSystemEntries(resolvedPath)
            .Select(static entry =>
            {
                var name = Path.GetFileName(entry);
                return Directory.Exists(entry) ? $"[dir]  {name}" : $"[file] {name}";
            })
            .OrderBy(static entry => entry, StringComparer.OrdinalIgnoreCase);

        return Task.FromResult(new AgentToolResult(
            true,
            [new AgentToolResultItem.Text(string.Join(Environment.NewLine, entries))]));
    }

    private static async Task<AgentToolResult> GrepFilesAsync(
        LocalAgentBuiltInToolOptions options,
        AgentToolInvocation invocation,
        CancellationToken cancellationToken)
    {
        var pattern = GetRequiredString(invocation.Arguments, "pattern");
        var root = ResolvePath(options.WorkingDirectory, GetOptionalString(invocation.Arguments, "path"));
        if (!Directory.Exists(root))
        {
            return Failure($"Directory '{root}' was not found.");
        }

        var glob = GetOptionalString(invocation.Arguments, "glob");
        var caseSensitive = GetOptionalBool(invocation.Arguments, "caseSensitive") ?? false;
        var maxMatches = Math.Clamp(GetOptionalInt(invocation.Arguments, "maxMatches") ?? options.MaxGrepMatches, 1, options.MaxGrepMatches);
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories);
        var matches = new List<string>(maxMatches);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (glob is not null && !MatchesGlob(Path.GetFileName(file), glob))
            {
                continue;
            }

            if (IsImagePath(file))
            {
                continue;
            }

            string[] lines;
            try
            {
                lines = await File.ReadAllLinesAsync(file, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            for (var index = 0; index < lines.Length; index++)
            {
                if (lines[index].IndexOf(pattern, comparison) < 0)
                {
                    continue;
                }

                matches.Add($"{Path.GetRelativePath(root, file)}:{index + 1}: {lines[index]}");
                if (matches.Count >= maxMatches)
                {
                    goto Done;
                }
            }
        }

Done:
        return new AgentToolResult(
            true,
            [new AgentToolResultItem.Text(matches.Count == 0 ? "(no matches)" : string.Join(Environment.NewLine, matches))]);
    }

    private static async Task<AgentToolResult> WebGetAsync(
        LocalAgentBuiltInToolOptions options,
        HttpClient httpClient,
        AgentToolInvocation invocation,
        CancellationToken cancellationToken)
    {
        var url = GetRequiredString(invocation.Arguments, "url");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return Failure($"'{url}' is not a valid absolute URL.");
        }

        var timeoutOverride = GetOptionalInt(invocation.Arguments, "timeoutSeconds");
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeoutOverride is > 0 ? TimeSpan.FromSeconds(timeoutOverride.Value) : options.WebGetTimeout);

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                linkedCts.Token)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null &&
            !mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mediaType, "application/xml", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mediaType, "application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
        {
            return Failure($"Content type '{mediaType}' is not supported by webget.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(linkedCts.Token).ConfigureAwait(false);
        using var buffer = new MemoryStream(capacity: options.MaxWebGetBytes);
        var rented = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(rented, linkedCts.Token).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > options.MaxWebGetBytes)
            {
                return Failure($"Response exceeded the {options.MaxWebGetBytes} byte limit.");
            }

            buffer.Write(rented, 0, read);
        }

        var text = Encoding.UTF8.GetString(buffer.ToArray());
        if (string.Equals(mediaType, "text/html", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mediaType, "application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
        {
            text = SimplifyHtml(text);
        }

        return new AgentToolResult(
            true,
            [new AgentToolResultItem.Text(text.Trim())]);
    }

    private static Task<AgentToolResult> ViewImageAsync(
        LocalAgentBuiltInToolOptions options,
        AgentToolInvocation invocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = GetRequiredString(invocation.Arguments, "path");
        var resolvedPath = ResolvePath(options.WorkingDirectory, path);
        if (!File.Exists(resolvedPath))
        {
            return Task.FromResult(Failure($"Image '{resolvedPath}' was not found."));
        }

        if (!IsImagePath(resolvedPath))
        {
            return Task.FromResult(Failure($"'{resolvedPath}' is not a supported image path."));
        }

        return Task.FromResult(new AgentToolResult(
            true,
            [
                new AgentToolResultItem.Text($"Image: {resolvedPath}"),
                new AgentToolResultItem.ImageUrl(new Uri(resolvedPath).AbsoluteUri),
            ]));
    }

    private static async Task<AgentToolResult> RequestUserInputAsync(
        LocalAgentBuiltInToolOptions options,
        AgentToolInvocation invocation,
        CancellationToken cancellationToken)
    {
        if (options.OnUserInputRequest is null)
        {
            return Failure("No user-input handler is configured for this session.");
        }

        var promptsElement = invocation.Arguments.TryGetProperty("prompts", out var prompts)
            ? prompts
            : throw new ArgumentException("The 'prompts' field is required.", nameof(invocation));
        if (promptsElement.ValueKind != JsonValueKind.Array)
        {
            return Failure("The 'prompts' field must be an array.");
        }

        var mappedPrompts = new List<AgentUserInputPrompt>();
        foreach (var prompt in promptsElement.EnumerateArray())
        {
            var id = GetRequiredString(prompt, "id");
            var question = GetRequiredString(prompt, "question");
            var header = GetOptionalString(prompt, "header");
            var allowFreeform = GetOptionalBool(prompt, "allowFreeform") ?? true;
            var isSecret = GetOptionalBool(prompt, "isSecret") ?? false;
            var optionsList = prompt.TryGetProperty("options", out var promptOptions) && promptOptions.ValueKind == JsonValueKind.Array
                ? promptOptions.EnumerateArray()
                    .Select(static option => new AgentUserInputOption(
                        GetRequiredString(option, "label"),
                        GetOptionalString(option, "description")))
                    .ToArray()
                : null;
            mappedPrompts.Add(new AgentUserInputPrompt(id, question, header, optionsList, allowFreeform, isSecret));
        }

        var request = new AgentUserInputRequest(
            options.BackendId,
            options.SessionId,
            DateTimeOffset.UtcNow,
            null,
            $"tool-input:{Guid.NewGuid():N}",
            new AgentUserInputForm(mappedPrompts));
        var response = await options.OnUserInputRequest(request, cancellationToken).ConfigureAwait(false);

        var json = SerializeAnswers(response.Answers);
        return new AgentToolResult(true, [new AgentToolResultItem.Text(json)]);
    }

    private static JsonElement ParseSchema(string json)
        => JsonDocument.Parse(json).RootElement.Clone();

    private static AgentToolResult Failure(string message)
        => new(false, [new AgentToolResultItem.Text(message)], message);

    private static string ResolvePath(string? workingDirectory, string? path)
    {
        var candidate = string.IsNullOrWhiteSpace(path) ? workingDirectory : path;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new ArgumentException("A path or working directory is required.");
        }

        return Path.GetFullPath(Path.IsPathRooted(candidate)
            ? candidate
            : Path.Combine(workingDirectory ?? Environment.CurrentDirectory, candidate));
    }

    private static bool IsImagePath(string path)
        => Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".bmp";

    private static string SimplifyHtml(string html)
    {
        var withoutScripts = Regex.Replace(html, "<(script|style)[^>]*>.*?</\\1>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var withoutTags = Regex.Replace(withoutScripts, "<[^>]+>", " ");
        var decoded = System.Net.WebUtility.HtmlDecode(withoutTags);
        return Regex.Replace(decoded, "\\s+", " ").Trim();
    }

    private static bool MatchesGlob(string fileName, string glob)
    {
        var pattern = "^" + Regex.Escape(glob).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase);
    }

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        var value = GetOptionalString(element, propertyName);
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"The '{propertyName}' field is required.")
            : value;
    }

    private static string? GetOptionalString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static int? GetOptionalInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static bool? GetOptionalBool(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? property.GetBoolean()
            : null;
    }

    private static string SerializeAnswers(IReadOnlyDictionary<string, string> answers)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var answer in answers)
            {
                writer.WriteString(answer.Key, answer.Value);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
