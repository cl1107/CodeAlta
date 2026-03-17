using CodeAlta.Agent;

namespace CodeAlta.Tests;

[TestClass]
public sealed class CodeAltaTerminalUiStatusTests
{
    [TestMethod]
    public void ResolveSelectionStatus_PrefersThreadSpecificStatus()
    {
        var snapshot = CodeAltaTerminalUi.ResolveSelectionStatus(
            readyMessage: "Prompt ready",
            hasThreadStatus: true,
            threadStatusMessage: "Thinking...",
            threadStatusBusy: true,
            threadStatusTone: CodeAltaTerminalUi.StatusTone.Info,
            promptUnavailable: true,
            promptUnavailableMessage: "Codex is unavailable.",
            promptUnavailableTone: CodeAltaTerminalUi.StatusTone.Warning);

        Assert.AreEqual("Thinking...", snapshot.Message);
        Assert.IsTrue(snapshot.Busy);
        Assert.AreEqual(CodeAltaTerminalUi.StatusTone.Info, snapshot.Tone);
    }

    [TestMethod]
    public void ResolveSelectionStatus_FallsBackToPromptUnavailableWhenThreadHasNoCustomStatus()
    {
        var snapshot = CodeAltaTerminalUi.ResolveSelectionStatus(
            readyMessage: "Prompt ready",
            hasThreadStatus: false,
            threadStatusMessage: "Stopped",
            threadStatusBusy: false,
            threadStatusTone: CodeAltaTerminalUi.StatusTone.Warning,
            promptUnavailable: true,
            promptUnavailableMessage: "Codex is unavailable.",
            promptUnavailableTone: CodeAltaTerminalUi.StatusTone.Warning);

        Assert.AreEqual("Codex is unavailable.", snapshot.Message);
        Assert.IsFalse(snapshot.Busy);
        Assert.AreEqual(CodeAltaTerminalUi.StatusTone.Warning, snapshot.Tone);
    }

    [TestMethod]
    public void ResolveSelectionStatus_UsesReadyMessageWhenNothingOverridesIt()
    {
        var snapshot = CodeAltaTerminalUi.ResolveSelectionStatus(
            readyMessage: "Prompt ready",
            hasThreadStatus: false,
            threadStatusMessage: null,
            threadStatusBusy: false,
            threadStatusTone: CodeAltaTerminalUi.StatusTone.Info,
            promptUnavailable: false,
            promptUnavailableMessage: null,
            promptUnavailableTone: CodeAltaTerminalUi.StatusTone.Warning);

        Assert.AreEqual("Prompt ready", snapshot.Message);
        Assert.IsFalse(snapshot.Busy);
        Assert.AreEqual(CodeAltaTerminalUi.StatusTone.Ready, snapshot.Tone);
    }

    [TestMethod]
    public void BuildStatusTextStyle_UsesGradientBrushForThinking()
    {
        var style = CodeAltaTerminalUi.BuildStatusTextStyle(
            CodeAltaTerminalUi.BuildThinkingStatusText(),
            busy: true,
            CodeAltaTerminalUi.StatusTone.Info);

        Assert.IsNotNull(style.ForegroundBrush);
        Assert.IsNull(style.Foreground);
    }

    [TestMethod]
    public void BuildStatusTextStyle_UsesSolidToneColorWhenIdle()
    {
        var style = CodeAltaTerminalUi.BuildStatusTextStyle(
            "Prompt ready",
            busy: false,
            CodeAltaTerminalUi.StatusTone.Ready);

        Assert.IsNull(style.ForegroundBrush);
        Assert.IsNotNull(style.Foreground);
    }
}
