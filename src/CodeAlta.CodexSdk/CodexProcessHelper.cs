using System.Diagnostics;

namespace CodeAlta.CodexSdk;

internal static class CodexProcessHelper
{
    internal static string? ResolveCodexExecutable(bool tryFnmLookup = true)
    {
        return FindExecutable("codex")
               ?? (tryFnmLookup ? FindExecutableViaFnm("codex") : null);
    }

    internal static ProcessStartInfo CreateCommandProcessStartInfo(
        string executablePath,
        string arguments,
        bool redirectStandardInput = false,
        bool redirectStandardOutput = true,
        bool redirectStandardError = true,
        bool createNoWindow = false)
    {
        ArgumentNullException.ThrowIfNull(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);

        return new ProcessStartInfo(executablePath, arguments)
        {
            RedirectStandardInput = redirectStandardInput,
            RedirectStandardOutput = redirectStandardOutput,
            RedirectStandardError = redirectStandardError,
            UseShellExecute = false,
            CreateNoWindow = createNoWindow
        };
    }

    internal static string? FindExecutable(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return FindExecutableInDirectories(name, pathVar.Split(Path.PathSeparator));
    }

    internal static string? FindExecutableViaFnm(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var fnmPath = FindExecutable("fnm");
        if (fnmPath is null)
            return null;

        var multishellDir = GetFnmMultishellPath(fnmPath);
        if (multishellDir is null)
            return null;

        return FindExecutableInDirectories(name, [multishellDir]);
    }

    internal static string? ParseFnmMultishellPath(string fnmEnvOutput)
    {
        ArgumentNullException.ThrowIfNull(fnmEnvOutput);

        const string prefix = "SET FNM_MULTISHELL_PATH=";
        foreach (var line in fnmEnvOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return line[prefix.Length..];
        }

        return null;
    }

    private static string? GetFnmMultishellPath(string fnmPath)
    {
        var processStartInfo = CreateCommandProcessStartInfo(
            fnmPath,
            "env --shell cmd",
            redirectStandardInput: false,
            createNoWindow: OperatingSystem.IsWindows());

        Process? process;
        try
        {
            process = Process.Start(processStartInfo);
        }
        catch (Exception)
        {
            return null;
        }

        if (process is null)
            return null;

        using (process)
        {
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                return null;

            return ParseFnmMultishellPath(output);
        }
    }

    private static string? FindExecutableInDirectories(string name, ReadOnlySpan<string> directories)
    {
        var extensions = OperatingSystem.IsWindows()
            ? new[] { ".exe", ".cmd", ".bat" }
            : Array.Empty<string>();

        foreach (var dir in directories)
        {
            if (string.IsNullOrWhiteSpace(dir))
                continue;

            foreach (var ext in extensions)
            {
                var withExt = Path.Combine(dir, name + ext);
                if (File.Exists(withExt))
                    return withExt;
            }

            if (!OperatingSystem.IsWindows())
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }
}
