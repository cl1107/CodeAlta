namespace CodeAlta.Views;

internal sealed record PromptComposerViewController(
    Action<string> AcceptPrompt,
    Action SendPrompt,
    Action AbortThread,
    Action OpenHelp,
    Action OpenCommandPalette)
{
    public static PromptComposerViewController Create(
        Action<string> acceptPrompt,
        Action sendPrompt,
        Action abortThread,
        Action openHelp,
        Action openCommandPalette)
    {
        ArgumentNullException.ThrowIfNull(acceptPrompt);
        ArgumentNullException.ThrowIfNull(sendPrompt);
        ArgumentNullException.ThrowIfNull(abortThread);
        ArgumentNullException.ThrowIfNull(openHelp);
        ArgumentNullException.ThrowIfNull(openCommandPalette);
        return new PromptComposerViewController(acceptPrompt, sendPrompt, abortThread, openHelp, openCommandPalette);
    }
}
