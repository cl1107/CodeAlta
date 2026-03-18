using CodeAlta.Agent;
using Tomlyn;

namespace CodeAlta.Catalog;

/// <summary>
/// Loads and persists CodeAlta TOML configuration files.
/// </summary>
public sealed class CodeAltaConfigStore
{
    private readonly CatalogOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeAltaConfigStore"/> class.
    /// </summary>
    /// <param name="options">Catalog layout options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <see cref="CatalogOptions.GlobalRoot"/> is empty.</exception>
    public CodeAltaConfigStore(CatalogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.GlobalRoot))
        {
            throw new ArgumentException("Global catalog root is required.", nameof(options));
        }

        _options = options;
    }

    /// <summary>
    /// Loads the global user configuration.
    /// </summary>
    /// <returns>The parsed configuration document.</returns>
    public CodeAltaConfigDocument LoadGlobal()
        => LoadDocument(_options.ConfigPath);

    /// <summary>
    /// Loads the project-local configuration when present.
    /// </summary>
    /// <param name="projectRoot">The project root directory.</param>
    /// <returns>The parsed configuration document, or an empty document when the file is missing.</returns>
    public CodeAltaConfigDocument LoadProject(string? projectRoot)
        => string.IsNullOrWhiteSpace(projectRoot)
            ? new CodeAltaConfigDocument()
            : LoadDocument(GetProjectConfigPath(projectRoot));

    /// <summary>
    /// Resolves the effective backend preference for a scope.
    /// </summary>
    /// <param name="backendId">The backend identifier.</param>
    /// <param name="projectRoot">Optional project root for project-local overrides.</param>
    /// <returns>The merged backend preference.</returns>
    public CodeAltaBackendPreference GetEffectiveBackendPreference(AgentBackendId backendId, string? projectRoot = null)
    {
        var global = LoadGlobal();
        var project = LoadProject(projectRoot);
        return ResolveBackendPreference(global, project, backendId);
    }

    /// <summary>
    /// Persists the global backend preference.
    /// </summary>
    /// <param name="backendId">The backend identifier.</param>
    /// <param name="model">The preferred model identifier.</param>
    /// <param name="reasoningEffort">The preferred reasoning effort.</param>
    public void SaveGlobalBackendPreference(
        AgentBackendId backendId,
        string? model,
        AgentReasoningEffort? reasoningEffort)
    {
        var document = LoadGlobal();
        var normalizedModel = NormalizeModel(model);
        var normalizedReasoning = FormatReasoningEffort(reasoningEffort);

        if (normalizedModel is null && normalizedReasoning is null)
        {
            document.Backends.Remove(backendId.Value);
        }
        else
        {
            document.Backends[backendId.Value] = new CodeAltaBackendSettingsDocument
            {
                Model = normalizedModel,
                ReasoningEffort = normalizedReasoning,
            };
        }

        SaveDocument(_options.ConfigPath, document);
    }

    internal static AgentReasoningEffort? ParseReasoningEffort(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "none" => AgentReasoningEffort.None,
            "minimal" => AgentReasoningEffort.Minimal,
            "low" => AgentReasoningEffort.Low,
            "medium" => AgentReasoningEffort.Medium,
            "high" => AgentReasoningEffort.High,
            "xhigh" => AgentReasoningEffort.XHigh,
            _ => null,
        };
    }

    internal static string? FormatReasoningEffort(AgentReasoningEffort? effort)
    {
        return effort switch
        {
            null => null,
            AgentReasoningEffort.None => "none",
            AgentReasoningEffort.Minimal => "minimal",
            AgentReasoningEffort.Low => "low",
            AgentReasoningEffort.Medium => "medium",
            AgentReasoningEffort.High => "high",
            AgentReasoningEffort.XHigh => "xhigh",
            _ => null,
        };
    }

    internal static CodeAltaBackendPreference ResolveBackendPreference(
        CodeAltaConfigDocument global,
        CodeAltaConfigDocument? project,
        AgentBackendId backendId)
    {
        ArgumentNullException.ThrowIfNull(global);

        return MergeBackendPreference(
            GetBackendSettings(global, backendId),
            project is null ? null : GetBackendSettings(project, backendId));
    }

    internal static CodeAltaBackendSettingsDocument? GetBackendSettings(
        CodeAltaConfigDocument document,
        AgentBackendId backendId)
    {
        return document.Backends.TryGetValue(backendId.Value, out var settings)
            ? settings
            : null;
    }

    private static CodeAltaBackendPreference MergeBackendPreference(
        CodeAltaBackendSettingsDocument? global,
        CodeAltaBackendSettingsDocument? project)
    {
        var model = NormalizeModel(project?.Model) ?? NormalizeModel(global?.Model);
        var reasoning = ParseReasoningEffort(project?.ReasoningEffort) ?? ParseReasoningEffort(global?.ReasoningEffort);
        return new CodeAltaBackendPreference(model, reasoning);
    }

    private static string? NormalizeModel(string? model)
        => string.IsNullOrWhiteSpace(model) ? null : model.Trim();

    private static string GetProjectConfigPath(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        return Path.Combine(projectRoot, ".codealta", "config.toml");
    }

    private static CodeAltaConfigDocument LoadDocument(string path)
    {
        if (!File.Exists(path))
        {
            return new CodeAltaConfigDocument();
        }

        var content = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(content))
        {
            return new CodeAltaConfigDocument();
        }

        try
        {
            var document = TomlSerializer.Deserialize(content, CodeAltaTomlSerializerContext.Default.CodeAltaConfigDocument)
                ?? new CodeAltaConfigDocument();
            NormalizeDocument(document);
            return document;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or IOException or TomlException)
        {
            throw new InvalidDataException($"Failed to parse CodeAlta config '{path}'.", ex);
        }
    }

    private static void SaveDocument(string path, CodeAltaConfigDocument document)
    {
        NormalizeDocument(document);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var content = TomlSerializer.Serialize(document, CodeAltaTomlSerializerContext.Default.CodeAltaConfigDocument);
        File.WriteAllText(path, content);
    }

    private static void NormalizeDocument(CodeAltaConfigDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Backends = document.Backends
            .Where(static entry => !string.IsNullOrWhiteSpace(entry.Key))
            .ToDictionary(
                static entry => entry.Key.Trim(),
                static entry => new CodeAltaBackendSettingsDocument
                {
                    Model = NormalizeModel(entry.Value?.Model),
                    ReasoningEffort = NormalizeReasoningEffortText(entry.Value?.ReasoningEffort),
                },
                StringComparer.OrdinalIgnoreCase);
    }

    private static string? NormalizeReasoningEffortText(string? value)
        => FormatReasoningEffort(ParseReasoningEffort(value));
}
