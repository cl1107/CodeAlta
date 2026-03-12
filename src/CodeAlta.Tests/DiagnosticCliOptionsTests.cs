using AgentMessageDiagnosticApp;
using CodeAlta.Agent;

namespace CodeAlta.Tests;

[TestClass]
public sealed class DiagnosticCliOptionsTests
{
    [TestMethod]
    public void TryParse_ParsesCodexSessionAndIndentedFlag()
    {
        var result = DiagnosticCliOptions.TryParse(
            ["--codex", "thread-1", "--indented"],
            out var options,
            out var error);

        Assert.IsTrue(result);
        Assert.IsNull(error);
        Assert.IsNotNull(options);
        Assert.AreEqual(AgentBackendIds.Codex, options.BackendId);
        Assert.AreEqual("thread-1", options.SessionId);
        Assert.IsTrue(options.Indented);
    }

    [TestMethod]
    public void TryParse_RejectsMissingBackendSelection()
    {
        var result = DiagnosticCliOptions.TryParse(
            ["--indented"],
            out var options,
            out var error);

        Assert.IsFalse(result);
        Assert.IsNull(options);
        Assert.AreEqual("Specify exactly one of --codex <session-id> or --copilot <session-id>.", error);
    }
}
