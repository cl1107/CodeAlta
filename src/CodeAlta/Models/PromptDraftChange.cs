namespace CodeAlta.Models;

internal readonly record struct PromptDraftChange(
    bool TextChanged,
    bool EditedStateChanged);
