namespace CodeAlta.Models;

internal readonly record struct ThreadVisualState(
    bool IsRunning,
    bool HasPromptDraft);
