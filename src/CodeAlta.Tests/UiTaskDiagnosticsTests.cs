using CodeAlta.App;
using CodeAlta.Models;
using CodeAlta.Presentation.Shell;

namespace CodeAlta.Tests;

[TestClass]
public sealed class UiTaskDiagnosticsTests
{
    [TestMethod]
    public async Task ObserveAsync_ReportsUnexpectedExceptionsToStatus()
    {
        string? message = null;
        var busy = true;
        var tone = StatusTone.Info;

        await UiTaskDiagnostics.ObserveAsync(
            Task.FromException(new InvalidOperationException("boom")),
            "submit the current prompt",
            (statusMessage, statusBusy, statusTone) =>
            {
                message = statusMessage;
                busy = statusBusy;
                tone = statusTone;
            });

        Assert.IsNotNull(message);
        StringAssert.Contains(message, "submit the current prompt");
        StringAssert.Contains(message, "boom");
        Assert.IsFalse(busy);
        Assert.AreEqual(StatusTone.Error, tone);
    }
}
