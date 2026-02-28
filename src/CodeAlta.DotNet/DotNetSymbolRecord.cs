namespace CodeAlta.DotNet;

/// <summary>
/// Represents an indexed .NET symbol location.
/// </summary>
public sealed record DotNetSymbolRecord
{
    /// <summary>
    /// Gets symbol kind (namespace/type/member).
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>
    /// Gets short symbol name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets fully-qualified symbol name.
    /// </summary>
    public required string FullyQualifiedName { get; init; }

    /// <summary>
    /// Gets source file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets 1-based start line.
    /// </summary>
    public required int StartLine { get; init; }

    /// <summary>
    /// Gets 1-based end line.
    /// </summary>
    public required int EndLine { get; init; }

    /// <summary>
    /// Gets optional documentation summary text.
    /// </summary>
    public string? Summary { get; init; }
}
