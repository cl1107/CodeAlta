namespace CodeAlta.DotNet;

/// <summary>
/// Represents a compact context snippet for agent consumption.
/// </summary>
public sealed record DotNetContextSnippet
{
    /// <summary>
    /// Gets snippet title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets snippet content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets stable source URI (`file://...#Lx`).
    /// </summary>
    public required string SourceUri { get; init; }
}
