namespace CodeAlta.Presentation.Prompting;

internal sealed record ProjectFilePromptActiveReference(
    int StartIndex,
    int Length,
    string QueryText);
