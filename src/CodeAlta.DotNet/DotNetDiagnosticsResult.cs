using CodeAlta.Persistence;

namespace CodeAlta.DotNet;

/// <summary>
/// Represents the result of running .NET diagnostics/build checks.
/// </summary>
public sealed record DotNetDiagnosticsResult
{
    /// <summary>
    /// Gets build process exit code.
    /// </summary>
    public required int ExitCode { get; init; }

    /// <summary>
    /// Gets whether the build succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets output artifact id.
    /// </summary>
    public required ArtifactId ArtifactId { get; init; }

    /// <summary>
    /// Gets output artifact path.
    /// </summary>
    public required string ArtifactPath { get; init; }
}
