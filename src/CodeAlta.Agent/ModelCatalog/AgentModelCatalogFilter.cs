using System.Text.RegularExpressions;

namespace CodeAlta.Agent.ModelCatalog;

internal static class AgentModelCatalogFilter
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    public static IReadOnlyList<AgentModelInfo> ApplyIncludeRegex(
        IReadOnlyList<AgentModelInfo> models,
        string? includeRegex)
    {
        ArgumentNullException.ThrowIfNull(models);

        if (models.Count == 0 || string.IsNullOrWhiteSpace(includeRegex))
        {
            return models;
        }

        var regex = CreateRegex(includeRegex);
        var filtered = models
            .Where(model => regex.IsMatch(model.Id))
            .ToArray();
        return filtered.Length == models.Count ? models : filtered;
    }

    public static void ValidateIncludeRegex(string includeRegex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(includeRegex);
        _ = CreateRegex(includeRegex);
    }

    private static Regex CreateRegex(string includeRegex)
        => new(includeRegex.Trim(), RegexOptions.CultureInvariant, MatchTimeout);
}
