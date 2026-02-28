namespace CodeAlta.DotNet;

/// <summary>
/// Represents options for .NET first-class services.
/// </summary>
public sealed class DotNetOptions
{
    /// <summary>
    /// Gets or sets the root directory used to persist .NET knowledge artifacts.
    /// </summary>
    public string ArtifactRoot { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".codealta",
        "knowledge",
        "dotnet");
}
