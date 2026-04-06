using Microsoft.Extensions.AI;

namespace CodeAlta.Agent.LocalRuntime.Tools;

/// <summary>
/// Converts local agent tool definitions into provider-agnostic chat-tool declarations.
/// </summary>
public static class LocalAgentToolBridge
{
    /// <summary>
    /// Converts tool definitions into <see cref="AITool"/> declarations.
    /// </summary>
    /// <param name="tools">Tool definitions.</param>
    /// <returns>The declared tools.</returns>
    public static IReadOnlyList<AITool> CreateDeclarations(IReadOnlyList<AgentToolDefinition>? tools)
    {
        if (tools is not { Count: > 0 })
        {
            return [];
        }

        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var declarations = new List<AITool>(tools.Count);
        foreach (var tool in tools)
        {
            ArgumentNullException.ThrowIfNull(tool);
            var registeredName = GetRegisteredToolName(tool.Spec.Name, usedNames);
            declarations.Add(AIFunctionFactory.CreateDeclaration(
                registeredName,
                tool.Spec.Description,
                tool.Spec.InputSchema));
        }

        return declarations;
    }

    /// <summary>
    /// Creates a lookup map from registered tool name to the underlying definition.
    /// </summary>
    /// <param name="tools">Tool definitions.</param>
    /// <returns>A case-sensitive map keyed by registered tool name.</returns>
    public static IReadOnlyDictionary<string, AgentToolDefinition> CreateDefinitionMap(
        IReadOnlyList<AgentToolDefinition>? tools)
    {
        if (tools is not { Count: > 0 })
        {
            return new Dictionary<string, AgentToolDefinition>(StringComparer.Ordinal);
        }

        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var map = new Dictionary<string, AgentToolDefinition>(tools.Count, StringComparer.Ordinal);
        foreach (var tool in tools)
        {
            ArgumentNullException.ThrowIfNull(tool);
            map.Add(GetRegisteredToolName(tool.Spec.Name, usedNames), tool);
        }

        return map;
    }

    internal static string GetRegisteredToolName(string toolName, ISet<string>? usedNames = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        Span<char> buffer = stackalloc char[toolName.Length];
        var length = 0;
        var lastWasSeparator = false;

        foreach (var ch in toolName)
        {
            if (char.IsLetterOrDigit(ch) || ch is '_' or '-')
            {
                buffer[length++] = ch;
                lastWasSeparator = false;
                continue;
            }

            if (!lastWasSeparator)
            {
                buffer[length++] = '_';
                lastWasSeparator = true;
            }
        }

        var candidate = length == 0
            ? "tool"
            : new string(buffer[..length]).Trim('_');
        if (candidate.Length == 0)
        {
            candidate = "tool";
        }

        const int maxToolNameLength = 64;
        if (candidate.Length > maxToolNameLength)
        {
            candidate = candidate[..maxToolNameLength];
        }

        if (usedNames is null)
        {
            return candidate;
        }

        if (usedNames.Add(candidate))
        {
            return candidate;
        }

        for (var suffix = 2; ; suffix++)
        {
            var suffixText = $"_{suffix}";
            var baseLength = Math.Min(candidate.Length, maxToolNameLength - suffixText.Length);
            var uniqueCandidate = string.Concat(candidate.AsSpan(0, baseLength), suffixText);
            if (usedNames.Add(uniqueCandidate))
            {
                return uniqueCandidate;
            }
        }
    }
}
