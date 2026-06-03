using CodeAlta.Models;
using CodeAlta.Orchestration.Runtime.SystemPrompts;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Controls;

namespace CodeAlta.Presentation.Chat;

internal static class AgentPromptPresentation
{
    public static IReadOnlyList<AgentPromptOption> BuildPromptOptions(IReadOnlyList<AgentPromptDescriptor> prompts)
    {
        ArgumentNullException.ThrowIfNull(prompts);
        return prompts
            .OrderBy(static prompt => prompt.Precedence)
            .ThenBy(static prompt => prompt.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static prompt => prompt.PromptName, StringComparer.OrdinalIgnoreCase)
            .Select(static prompt => new AgentPromptOption(
                prompt.PromptName,
                BuildPromptLabel(prompt),
                ToSourceLabel(prompt.SourceKind),
                prompt.SystemPromptName,
                prompt.Description,
                prompt.IsBuiltIn))
            .ToArray();
    }

    public static string BuildPromptLabel(AgentPromptDescriptor prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        return string.Equals(prompt.DisplayName, prompt.PromptName, StringComparison.OrdinalIgnoreCase)
            ? prompt.DisplayName
            : $"{prompt.DisplayName} ({prompt.PromptName})";
    }

    public static string BuildPromptOptionMarkup(AgentPromptOption? option)
    {
        if (option is null)
        {
            return "[gray]No prompts[/]";
        }

        var color = option.IsBuiltIn ? "gray" : option.SourceLabel == "project" ? "lime" : "cyan";
        return $"{AnsiMarkup.Escape(option.Label)} [dim][{color}]{AnsiMarkup.Escape(option.SourceLabel)}[/][/]";
    }

    public static void ReplaceSelectItems<T>(Select<T> select, IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(select);
        ArgumentNullException.ThrowIfNull(items);
        select.Items.Clear();
        foreach (var item in items)
        {
            select.Items.Add(item);
        }
    }

    public static string ToSourceLabel(AgentPromptSourceKind sourceKind)
        => sourceKind switch
        {
            AgentPromptSourceKind.BuiltIn => "built-in",
            AgentPromptSourceKind.UserGlobal => "global",
            AgentPromptSourceKind.Project => "project",
            _ => "unknown",
        };
}
