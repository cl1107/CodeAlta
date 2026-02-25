using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CodeAlta.CodexSdk.Generator;

internal static partial class CodexVersionDetector
{
    private static readonly Version UnknownVersion = new(0, 0, 0);

    public static async Task<CodexVersionInfo> DetectAsync(
        string? codexPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codexPath))
        {
            return new CodexVersionInfo(
                UnknownVersion,
                "codex executable path is not resolved",
                IsDetected: false);
        }

        var processStartInfo = CodexProcessHelper.CreateCommandProcessStartInfo(codexPath, "--version");

        try
        {
            using var process = Process.Start(processStartInfo);
            if (process is null)
            {
                return new CodexVersionInfo(
                    UnknownVersion,
                    "unable to start codex --version process",
                    IsDetected: false);
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);

            var rawOutput = string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
            var normalizedOutput = NormalizeLineBreaks(rawOutput).Trim();

            if (process.ExitCode != 0)
            {
                return new CodexVersionInfo(
                    UnknownVersion,
                    string.IsNullOrWhiteSpace(normalizedOutput)
                        ? $"codex --version exited with code {process.ExitCode}"
                        : normalizedOutput,
                    IsDetected: false);
            }

            return TryParseVersion(normalizedOutput, out var version)
                ? new CodexVersionInfo(version, normalizedOutput, IsDetected: true)
                : new CodexVersionInfo(
                    UnknownVersion,
                    string.IsNullOrWhiteSpace(normalizedOutput) ? "unknown" : normalizedOutput,
                    IsDetected: false);
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException or IOException)
        {
            return new CodexVersionInfo(UnknownVersion, exception.Message, IsDetected: false);
        }
    }

    internal static bool TryParseVersion(string text, out Version version)
    {
        ArgumentNullException.ThrowIfNull(text);

        var match = VersionRegex().Match(text);
        if (!match.Success)
        {
            version = UnknownVersion;
            return false;
        }

        if (!int.TryParse(match.Groups[1].Value, out var major) ||
            !int.TryParse(match.Groups[2].Value, out var minor) ||
            !int.TryParse(match.Groups[3].Value, out var build))
        {
            version = UnknownVersion;
            return false;
        }

        if (match.Groups[4].Success && int.TryParse(match.Groups[4].Value, out var revision))
        {
            version = new Version(major, minor, build, revision);
            return true;
        }

        version = new Version(major, minor, build);
        return true;
    }

    private static string NormalizeLineBreaks(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"(?<!\d)(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?", RegexOptions.CultureInvariant)]
    private static partial Regex VersionRegex();
}

internal readonly record struct CodexVersionInfo(Version Version, string RawOutput, bool IsDetected);
