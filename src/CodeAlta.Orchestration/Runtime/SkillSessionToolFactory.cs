using System.Text.Json;
using CodeAlta.Agent;
using CodeAlta.Catalog.Skills;

namespace CodeAlta.Orchestration.Runtime;

internal static class SkillSessionToolFactory
{
    private static readonly JsonElement ActivationInputSchema = JsonDocument.Parse(
        """
        {
          "type": "object",
          "properties": {
            "skillName": {
              "type": "string",
              "description": "The skill name to activate."
            }
          },
          "required": ["skillName"],
          "additionalProperties": false
        }
        """).RootElement.Clone();

    public static AgentToolDefinition CreateActivateTool(
        SkillCatalog skillCatalog,
        SkillCatalogQuery query)
    {
        ArgumentNullException.ThrowIfNull(skillCatalog);
        ArgumentNullException.ThrowIfNull(query);

        return new AgentToolDefinition(
            new AgentToolSpec(
                "codealta.skills.activate",
                "Loads a discovered filesystem skill and returns the canonical activation payload.",
                ActivationInputSchema),
            async (invocation, cancellationToken) =>
            {
                var skillName = GetRequiredString(invocation.Arguments, "skillName");
                var activation = await skillCatalog.ActivateAsync(query, skillName, cancellationToken).ConfigureAwait(false);
                if (activation is null)
                {
                    var message = $"Skill '{skillName}' was not found.";
                    return new AgentToolResult(false, [new AgentToolResultItem.Text(message)], message);
                }

                return new AgentToolResult(true, [new AgentToolResultItem.Text(activation.Payload)]);
            });
    }

    public static IReadOnlyList<AgentToolDefinition> MergeWithActivationTool(
        IReadOnlyList<AgentToolDefinition>? tools,
        AgentToolDefinition activationTool)
    {
        ArgumentNullException.ThrowIfNull(activationTool);

        var merged = new List<AgentToolDefinition>();
        if (tools is not null)
        {
            merged.AddRange(tools.Where(tool =>
                !string.Equals(tool.Spec.Name, activationTool.Spec.Name, StringComparison.Ordinal)));
        }

        merged.Add(activationTool);
        return merged;
    }

    private static string GetRequiredString(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new ArgumentException($"Tool argument '{propertyName}' is required.", nameof(arguments));
        }

        return value.GetString()!.Trim();
    }
}
