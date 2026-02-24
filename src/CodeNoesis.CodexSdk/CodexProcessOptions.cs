namespace CodeNoesis.CodexSdk;

/// <summary>
/// Options that control how <see cref="CodexProcess"/> locates and starts the
/// codex app-server executable.
/// </summary>
public sealed class CodexProcessOptions
{
    /// <summary>
    /// Gets or sets an explicit path to the <c>codex</c> executable.
    /// When <see langword="null"/>, the executable is resolved from <c>PATH</c>
    /// (and optionally via <c>fnm</c> — see <see cref="TryFnmLookup"/>).
    /// </summary>
    public string? CodexPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the process should attempt to
    /// locate codex through <c>fnm</c> (Fast Node Manager) when it is not
    /// found on <c>PATH</c>.  Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// When enabled, <see cref="CodexProcess"/> runs <c>fnm env --shell cmd</c>,
    /// extracts the <c>FNM_MULTISHELL_PATH</c> directory, and probes it for the
    /// codex executable.  <c>PATH</c> is never modified; only the resolved
    /// executable path is used.  If <c>fnm</c> is not installed this is a no-op.
    /// </remarks>
    public bool TryFnmLookup { get; set; } = true;
}
