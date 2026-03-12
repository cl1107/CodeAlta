using CodeAlta.Agent;

namespace AgentMessageDiagnosticApp;

internal sealed record DiagnosticCliOptions(
    AgentBackendId BackendId,
    string SessionId,
    bool Indented)
{
    public static bool TryParse(
        IReadOnlyList<string> args,
        out DiagnosticCliOptions? options,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);

        options = null;
        error = null;

        string? codexSessionId = null;
        string? copilotSessionId = null;
        var indented = false;

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--codex":
                    if (!TryReadValue(args, ref index, out codexSessionId, out error))
                    {
                        return false;
                    }

                    break;

                case "--copilot":
                    if (!TryReadValue(args, ref index, out copilotSessionId, out error))
                    {
                        return false;
                    }

                    break;

                case "--indented":
                    indented = true;
                    break;

                case "--help":
                case "-h":
                    error = null;
                    return false;

                default:
                    error = $"Unknown argument '{argument}'.";
                    return false;
            }
        }

        var backendCount = (codexSessionId is null ? 0 : 1) + (copilotSessionId is null ? 0 : 1);
        if (backendCount != 1)
        {
            error = "Specify exactly one of --codex <session-id> or --copilot <session-id>.";
            return false;
        }

        options = codexSessionId is not null
            ? new DiagnosticCliOptions(AgentBackendIds.Codex, codexSessionId, indented)
            : new DiagnosticCliOptions(AgentBackendIds.Copilot, copilotSessionId!, indented);
        return true;
    }

    public static string GetUsage()
        => """
           Usage:
             AgentMessageDiagnosticApp --codex <session-id> [--indented]
             AgentMessageDiagnosticApp --copilot <session-id> [--indented]
           """;

    private static bool TryReadValue(
        IReadOnlyList<string> args,
        ref int index,
        out string? value,
        out string? error)
    {
        value = null;
        error = null;

        if (index + 1 >= args.Count)
        {
            error = $"Missing value for '{args[index]}'.";
            return false;
        }

        value = args[++index];
        if (string.IsNullOrWhiteSpace(value))
        {
            error = $"Missing value for '{args[index - 1]}'.";
            return false;
        }

        return true;
    }
}
