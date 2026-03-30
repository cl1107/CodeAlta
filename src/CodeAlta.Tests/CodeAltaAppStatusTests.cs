using CodeAlta.Agent;
using CodeAlta.Models;
using CodeAlta.Presentation.Shell;

namespace CodeAlta.Tests;

[TestClass]
public sealed class CodeAltaAppStatusTests
{
    [TestMethod]
    public void ResolveSelectionStatus_PrefersThreadSpecificStatus()
    {
        var snapshot = SelectionStatusResolver.Resolve(
            readyMessage: "Prompt ready",
            hasThreadStatus: true,
            threadStatusMessage: "Thinking...",
            threadStatusBusy: true,
            threadStatusTone: StatusTone.Info,
            promptEdited: false,
            promptUnavailable: true,
            promptUnavailableMessage: "Codex is unavailable.",
            promptUnavailableTone: StatusTone.Warning);

        Assert.AreEqual("Thinking...", snapshot.Message);
        Assert.IsTrue(snapshot.Busy);
        Assert.AreEqual(StatusTone.Info, snapshot.Tone);
    }

    [TestMethod]
    public void ResolveSelectionStatus_FallsBackToPromptUnavailableWhenThreadHasNoCustomStatus()
    {
        var snapshot = SelectionStatusResolver.Resolve(
            readyMessage: "Prompt ready",
            hasThreadStatus: false,
            threadStatusMessage: "Stopped",
            threadStatusBusy: false,
            threadStatusTone: StatusTone.Warning,
            promptEdited: false,
            promptUnavailable: true,
            promptUnavailableMessage: "Codex is unavailable.",
            promptUnavailableTone: StatusTone.Warning);

        Assert.AreEqual("Codex is unavailable.", snapshot.Message);
        Assert.IsFalse(snapshot.Busy);
        Assert.AreEqual(StatusTone.Warning, snapshot.Tone);
    }

    [TestMethod]
    public void ResolveSelectionStatus_UsesReadyMessageWhenNothingOverridesIt()
    {
        var snapshot = SelectionStatusResolver.Resolve(
            readyMessage: "Prompt ready",
            hasThreadStatus: false,
            threadStatusMessage: null,
            threadStatusBusy: false,
            threadStatusTone: StatusTone.Info,
            promptEdited: false,
            promptUnavailable: false,
            promptUnavailableMessage: null,
            promptUnavailableTone: StatusTone.Warning);

        Assert.AreEqual("Prompt ready", snapshot.Message);
        Assert.IsFalse(snapshot.Busy);
        Assert.AreEqual(StatusTone.Ready, snapshot.Tone);
    }

    [TestMethod]
    public void ResolveSelectionStatus_UsesEditedPromptStateWhenReady()
    {
        var snapshot = SelectionStatusResolver.Resolve(
            readyMessage: "Prompt ready",
            hasThreadStatus: false,
            threadStatusMessage: null,
            threadStatusBusy: false,
            threadStatusTone: StatusTone.Info,
            promptEdited: true,
            promptUnavailable: false,
            promptUnavailableMessage: null,
            promptUnavailableTone: StatusTone.Warning);

        Assert.AreEqual(StatusVisualFormatter.BuildPromptEditedStatusText(), snapshot.Message);
        Assert.IsFalse(snapshot.Busy);
        Assert.AreEqual(StatusTone.Info, snapshot.Tone);
        Assert.AreEqual(StatusVisualFormatter.BuildPromptEditedIconMarkup(), snapshot.IconMarkup);
    }

    [TestMethod]
    public void BuildStatusTextStyle_UsesGradientBrushForThinking()
    {
        var style = StatusVisualFormatter.BuildStatusTextStyle(
            StatusVisualFormatter.BuildThinkingStatusText(),
            busy: true,
            StatusTone.Info,
            thinkingAnimationPhase01: 0.25f);

        Assert.IsNotNull(style.ForegroundBrush);
        Assert.IsNull(style.Foreground);
    }

    [TestMethod]
    public void BuildStatusTextStyle_UsesSolidToneColorWhenIdle()
    {
        var style = StatusVisualFormatter.BuildStatusTextStyle(
            "Prompt ready",
            busy: false,
            StatusTone.Ready,
            thinkingAnimationPhase01: 0f);

        Assert.IsNull(style.ForegroundBrush);
        Assert.IsNotNull(style.Foreground);
    }
}
