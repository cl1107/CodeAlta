namespace TinyApp;

/// <summary>
/// Provides simple math helpers for fixture-based symbol tests.
/// </summary>
public sealed class Calculator
{
    /// <summary>
    /// Adds two integers.
    /// </summary>
    public int Add(int left, int right)
    {
        return left + right;
    }

    /// <summary>
    /// Gets calculator name.
    /// </summary>
    public string Name => "TinyCalculator";
}