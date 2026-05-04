using System.Reflection;
using CodeAlta.Plugins.Abstractions;

namespace CodeAlta.Plugins.Tests;

[TestClass]
public sealed class PluginStartupFeedbackReporterTests
{
    [TestMethod]
    public void ReporterKeepsFastPathQuietAndWritesInteractiveProgress()
    {
        using var temp = new TestTempDirectory();
        var package = CreatePackage(temp.Path, "hello");
        var interactive = new List<string>();
        var headless = new List<string>();
        var reporter = new PluginStartupFeedbackReporter(PluginStartupFeedbackMode.Interactive, interactive.Add, headless.Add);

        reporter.ReportStaleBuilds(1);
        reporter.ReportProgress(new PluginBuildProgress { Package = package, Index = 0, Total = 1, State = PluginBuildProgressState.Running });
        reporter.ReportResult(new PluginBuildResult { Package = package, Succeeded = true, IsUpToDate = true });

        Assert.AreEqual(2, interactive.Count);
        Assert.AreEqual(0, headless.Count);
        Assert.IsFalse(interactive.Any(static message => message.Contains("up", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ReporterUsesHeadlessFallbackWithoutMarkupControlSequences()
    {
        using var temp = new TestTempDirectory();
        var package = CreatePackage(temp.Path, "hello");
        var interactive = new List<string>();
        var headless = new List<string>();
        var reporter = new PluginStartupFeedbackReporter(PluginStartupFeedbackMode.Headless, interactive.Add, headless.Add);

        reporter.ReportProgress(new PluginBuildProgress { Package = package, Index = 0, Total = 1, State = PluginBuildProgressState.Failed });
        reporter.ReportResult(new PluginBuildResult { Package = package, Succeeded = false });

        Assert.AreEqual(0, interactive.Count);
        Assert.AreEqual(2, headless.Count);
        Assert.IsTrue(headless.All(static message => !message.Contains('\u001b') && !message.Contains("[/]", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void LiveStatusAppliesQueuedProgressToBindableState()
    {
        using var temp = new TestTempDirectory();
        var firstPackage = CreatePackage(temp.Path, "hello");
        var secondPackage = CreatePackage(temp.Path, "world");
        var requests = new List<PluginBuildRequest>
        {
            new() { Package = firstPackage },
            new() { Package = secondPackage },
        };
        var status = CreateLiveStatus(requests);

        InvokeLiveStatus(status, "Report", new PluginBuildProgress { Package = firstPackage, Index = 0, Total = 2, State = PluginBuildProgressState.Running });

        Assert.IsTrue(BuildHeaderMarkup(status).Contains("0/2 complete", StringComparison.Ordinal));

        InvokeLiveStatus(status, "ApplyPendingProgress");

        Assert.IsTrue(BuildHeaderMarkup(status).Contains("0/2 complete, 1 running", StringComparison.Ordinal));
        Assert.AreEqual("◌  1. Building hello", BuildItemMarkup(status, 0));

        InvokeLiveStatus(status, "Report", new PluginBuildProgress { Package = firstPackage, Index = 0, Total = 2, State = PluginBuildProgressState.Succeeded });
        InvokeLiveStatus(status, "Report", new PluginBuildProgress { Package = secondPackage, Index = 1, Total = 2, State = PluginBuildProgressState.UpToDate });
        InvokeLiveStatus(status, "ApplyPendingProgress");
        InvokeLiveStatus(status, "MarkCompleted");

        Assert.AreEqual("✓ Plugin builds finished (2/2 complete)", BuildHeaderMarkup(status));
        Assert.AreEqual("✓ Plugin build live output paused. Press Enter to continue.", BuildFooterMarkup(status, waitForEnterAfterCompletion: true));
    }

    private static SourcePluginPackage CreatePackage(string rootPath, string id)
    {
        var directory = Path.Combine(rootPath, id);
        Directory.CreateDirectory(directory);
        var entry = Path.Combine(directory, "plugin.cs");
        File.WriteAllText(entry, "// plugin");
        return new SourcePluginPackage
        {
            PackageId = id,
            PackageDirectory = directory,
            EntryFilePath = entry,
            Root = new PluginRoot { RootPath = rootPath, Scope = PluginScope.Global },
        };
    }

    private static object CreateLiveStatus(IReadOnlyList<PluginBuildRequest> requests)
    {
        var statusType = GetLiveStatusType();
        return Activator.CreateInstance(statusType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, args: [requests], culture: null)
            ?? throw new InvalidOperationException("Could not create plugin build live status.");
    }

    private static string BuildHeaderMarkup(object status)
        => (string)InvokeLiveStatus(status, "BuildHeaderMarkup")!;

    private static string BuildItemMarkup(object status, int index)
        => (string)InvokeLiveStatus(status, "BuildItemMarkup", index)!;

    private static string BuildFooterMarkup(object status, bool waitForEnterAfterCompletion)
        => (string)InvokeLiveStatus(status, "BuildFooterMarkup", waitForEnterAfterCompletion)!;

    private static object? InvokeLiveStatus(object status, string methodName, params object[] args)
    {
        var method = status.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Could not find live status method {methodName}.");
        return method.Invoke(status, args);
    }

    private static Type GetLiveStatusType()
        => typeof(PluginStartupFeedbackReporter).GetNestedType("PluginBuildLiveStatus", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find plugin build live status type.");
}
