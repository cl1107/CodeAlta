using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace CodeAlta.Agent.LocalRuntime;

internal static class LocalAgentInstructionComposer
{
    public static LocalAgentInstructionBundle Compose(AgentSessionCreateOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var systemMessage = Normalize(options.SystemMessage);
        var runtimeContext = BuildRuntimeContextSection(options.WorkingDirectory, options.ProjectRoots);
        var developerSections = new List<string>();
        if (!string.IsNullOrWhiteSpace(options.DeveloperInstructions))
        {
            developerSections.Add(options.DeveloperInstructions.Trim());
        }

        foreach (var path in EnumerateAgentInstructionFiles(options.WorkingDirectory, options.ProjectRoots))
        {
            var content = File.ReadAllText(path).Trim();
            if (content.Length == 0)
            {
                continue;
            }

            developerSections.Add(
                $"""
                File: {path}
                {content}
                """);
        }

        var developerInstructions = developerSections.Count == 0
            ? null
            : string.Join(Environment.NewLine + Environment.NewLine, developerSections);
        var hash = ComputeHash(systemMessage, developerInstructions, runtimeContext);
        return new LocalAgentInstructionBundle(systemMessage, developerInstructions, runtimeContext, hash);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildRuntimeContextSection(string? workingDirectory, IReadOnlyList<string> projectRoots)
    {
        var lines = new List<string>
        {
            $"Current date: {DateTimeOffset.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}",
            $"Platform: {GetPlatformLabel()}",
            $"Default shell for `shell_command`: `{GetDefaultShellLabel()}`",
        };

        var normalizedWorkingDirectory = NormalizePath(workingDirectory);
        if (normalizedWorkingDirectory is not null)
        {
            lines.Add($"Current working directory: `{normalizedWorkingDirectory}`");
        }

        var normalizedProjectRoots = projectRoots
            .Select(NormalizePath)
            .Where(static path => path is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToArray();
        if (normalizedProjectRoots.Length > 0)
        {
            lines.Add(
                normalizedProjectRoots.Length == 1
                    ? $"Project root: `{normalizedProjectRoots[0]}`"
                    : $"Project roots: {string.Join(", ", normalizedProjectRoots.Select(static root => $"`{root}`"))}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<string> EnumerateAgentInstructionFiles(string? workingDirectory, IReadOnlyList<string> projectRoots)
    {
        var files = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidateRelativePaths = new[]
        {
            "AGENTS.md",
            "CLAUDE.md",
            Path.Combine(".github", "copilot-instructions.md"),
        };

        void AddWalk(string? root)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                return;
            }

            var current = Path.GetFullPath(root);
            var stack = new Stack<string>();
            while (!string.IsNullOrWhiteSpace(current))
            {
                stack.Push(current);
                var parent = Directory.GetParent(current);
                if (parent is null)
                {
                    break;
                }

                current = parent.FullName;
            }

            while (stack.Count > 0)
            {
                var directory = stack.Pop();
                var selectedFile = candidateRelativePaths
                    .Select(relativePath => Path.Combine(directory, relativePath))
                    .Where(File.Exists)
                    .Select(path => new FileInfo(path))
                    .OrderByDescending(static file => file.Length)
                    .ThenBy(static file => file.FullName, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (selectedFile is not null && seen.Add(selectedFile.FullName))
                {
                    files.Add(selectedFile.FullName);
                }
            }
        }

        AddWalk(workingDirectory);
        foreach (var projectRoot in projectRoots)
        {
            AddWalk(projectRoot);
        }

        return files;
    }

    private static string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string GetPlatformLabel()
    {
        if (OperatingSystem.IsWindows())
        {
            return "Windows";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "macOS";
        }

        if (OperatingSystem.IsLinux())
        {
            return "Linux";
        }

        return RuntimeInformation.OSDescription.Trim();
    }

    private static string GetDefaultShellLabel()
    {
        if (OperatingSystem.IsWindows())
        {
            return "pwsh";
        }

        var shell = Environment.GetEnvironmentVariable("SHELL");
        return string.IsNullOrWhiteSpace(shell)
            ? "/bin/sh"
            : shell.Trim();
    }

    private static string ComputeHash(string? systemMessage, string? developerInstructions, string? runtimeContext)
    {
        var payload = $"{systemMessage ?? string.Empty}\n---\n{developerInstructions ?? string.Empty}\n---\n{runtimeContext ?? string.Empty}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}

internal sealed record LocalAgentInstructionBundle(
    string? SystemMessage,
    string? DeveloperInstructions,
    string RuntimeContext,
    string InstructionHash);
