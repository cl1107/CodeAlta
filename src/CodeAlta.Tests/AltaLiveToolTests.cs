using System.Text.Json;
using CodeAlta.Agent;
using CodeAlta.LiveTool;
using CodeAlta.Plugins.Abstractions;
using XenoAtom.CommandLine;
using Command = XenoAtom.CommandLine.Command;

namespace CodeAlta.Tests;

[TestClass]
public sealed class AltaLiveToolTests
{
    [TestMethod]
    public async Task Dispatcher_Version_ReturnsJsonlResultHeaderAndVersionRecord()
    {
        var result = await CreateDispatcher().InvokeAsync(["version"], caller: AltaCallerIdentity.Cli).ConfigureAwait(false);

        Assert.AreEqual(AltaExitCodes.Success, result.ExitCode);
        var lines = ReadJsonLines(result.Stdout);
        Assert.AreEqual("alta.result", lines[0].GetProperty("type").GetString());
        Assert.AreEqual(0, lines[0].GetProperty("exitCode").GetInt32());
        Assert.AreEqual(1, lines[0].GetProperty("recordCount").GetInt32());
        Assert.AreEqual("alta.version", lines[1].GetProperty("type").GetString());
    }

    [TestMethod]
    public async Task Dispatcher_UnknownCommand_ReturnsUsageJsonlDiagnostic()
    {
        var result = await CreateDispatcher().InvokeAsync(["no-such-command"], caller: AltaCallerIdentity.Cli).ConfigureAwait(false);

        Assert.AreEqual(AltaExitCodes.Usage, result.ExitCode);
        var lines = ReadJsonLines(result.Stdout);
        Assert.AreEqual("alta.result", lines[0].GetProperty("type").GetString());
        Assert.AreEqual(AltaExitCodes.Usage, lines[0].GetProperty("exitCode").GetInt32());
        Assert.AreEqual(1, lines[0].GetProperty("diagnosticCount").GetInt32());
        Assert.AreEqual("alta.error", lines[1].GetProperty("type").GetString());
        Assert.AreEqual("usage.invalid", lines[1].GetProperty("code").GetString());
        StringAssert.Contains(lines[1].GetProperty("message").GetString(), "no-such-command");
    }

    [TestMethod]
    public async Task Dispatcher_HelpInvocations_CanRunConcurrentlyWithFreshCommandTrees()
    {
        var tasks = Enumerable.Range(0, 24)
            .Select(static _ => CreateDispatcher().InvokeAsync(["session", "--help"], caller: AltaCallerIdentity.Cli).AsTask())
            .ToArray();

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        foreach (var result in results)
        {
            Assert.AreEqual(AltaExitCodes.Success, result.ExitCode);
            Assert.IsTrue(result.IsHelp);
            StringAssert.Contains(result.Stdout, "Usage: alta session");
            Assert.AreEqual(string.Empty, result.Stderr);
        }
    }

    [TestMethod]
    public async Task SessionTool_Help_ReturnsPlainHelpText()
    {
        using var arguments = JsonDocument.Parse("""{"args":["--help"]}""");
        var tool = AltaSessionToolFactory.Create(CreateDispatcher(), new AltaSessionToolOptions());

        var result = await tool.Handler(CreateInvocation(arguments.RootElement), CancellationToken.None).ConfigureAwait(false);

        Assert.IsTrue(result.Success);
        Assert.IsNull(result.Error);
        var text = AssertTextItem(result);
        StringAssert.StartsWith(text, "Usage: alta");
        Assert.IsFalse(text.Contains("\"type\":\"alta.result\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task SessionTool_CommandFailure_ReturnsFailedJsonlTranscriptAndShortError()
    {
        using var arguments = JsonDocument.Parse("""{"args":["no-such-command"]}""");
        var tool = AltaSessionToolFactory.Create(CreateDispatcher(), new AltaSessionToolOptions());

        var result = await tool.Handler(CreateInvocation(arguments.RootElement), CancellationToken.None).ConfigureAwait(false);

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error, "no-such-command");
        var text = AssertTextItem(result);
        StringAssert.Contains(text, "\"type\":\"alta.result\"");
        StringAssert.Contains(text, "\"type\":\"alta.error\"");
        StringAssert.Contains(text, "\"exitCode\":2");
    }

    [TestMethod]
    public async Task SessionTool_OutputRecordLimit_TruncatesTranscriptAndUpdatesHeaderCounts()
    {
        using var arguments = JsonDocument.Parse("""{"args":["version"],"maxOutputRecords":0}""");
        var tool = AltaSessionToolFactory.Create(CreateDispatcher(), new AltaSessionToolOptions());

        var invalidResult = await tool.Handler(CreateInvocation(arguments.RootElement), CancellationToken.None).ConfigureAwait(false);

        Assert.IsFalse(invalidResult.Success);
        StringAssert.Contains(invalidResult.Error, "maxOutputRecords");

        using var cappedArguments = JsonDocument.Parse("""{"args":["tool","capability","list"],"maxOutputRecords":1}""");
        var cappedResult = await tool.Handler(CreateInvocation(cappedArguments.RootElement), CancellationToken.None).ConfigureAwait(false);

        Assert.IsTrue(cappedResult.Success);
        var lines = ReadJsonLines(AssertTextItem(cappedResult));
        Assert.AreEqual(2, lines.Count);
        Assert.IsTrue(lines[0].GetProperty("truncated").GetBoolean());
        Assert.AreEqual(1, lines[0].GetProperty("recordCount").GetInt32());
        Assert.AreEqual("alta.tool.capability", lines[1].GetProperty("type").GetString());
    }

    [TestMethod]
    public async Task SessionTool_InvalidTimeout_ReturnsToolArgumentError()
    {
        using var arguments = JsonDocument.Parse("""{"args":["version"],"timeoutSeconds":0}""");
        var tool = AltaSessionToolFactory.Create(CreateDispatcher(), new AltaSessionToolOptions());

        var result = await tool.Handler(CreateInvocation(arguments.RootElement), CancellationToken.None).ConfigureAwait(false);

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error, "timeoutSeconds");
        StringAssert.Contains(AssertTextItem(result), "usage.invalidToolArguments");
    }

    [TestMethod]
    public async Task Dispatcher_MissingRuntimeService_ReturnsServiceUnavailableDiagnostic()
    {
        var result = await CreateDispatcher().InvokeAsync(["session", "list"], caller: AltaCallerIdentity.Cli).ConfigureAwait(false);

        Assert.AreEqual(AltaExitCodes.ServiceUnavailable, result.ExitCode);
        var lines = ReadJsonLines(result.Stdout);
        Assert.AreEqual("alta.result", lines[0].GetProperty("type").GetString());
        Assert.AreEqual(AltaExitCodes.ServiceUnavailable, lines[0].GetProperty("exitCode").GetInt32());
        Assert.AreEqual("service.unavailable", lines[1].GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task Dispatcher_CancelledCommand_ReturnsCancellationDiagnostic()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);
        var result = await CreateDispatcher(new CancellingContributor())
            .InvokeAsync(["wait"], caller: AltaCallerIdentity.Cli, cancellationToken: cts.Token)
            .ConfigureAwait(false);

        Assert.AreEqual(AltaExitCodes.TimeoutOrCancellation, result.ExitCode);
        var lines = ReadJsonLines(result.Stdout);
        Assert.AreEqual(AltaExitCodes.TimeoutOrCancellation, lines[0].GetProperty("exitCode").GetInt32());
        Assert.AreEqual("runtime.cancelled", lines[1].GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task PluginAltaCommandContribution_OptionValidationFailureReturnsUsageDiagnostic()
    {
        var catalog = new FakeAltaPluginCatalog(
            new AltaPluginCommandContribution
            {
                Plugin = CreatePluginDescriptor("validator-plugin"),
                Services = NoopPluginServices.Create(),
                Scope = PluginScope.Global,
                Command = new PluginAltaCommandContribution
                {
                    Path = "validator",
                    Policy = new PluginAltaCommandPolicy { RequiresInProcessRuntime = true },
                    CreateCommandNode = _ =>
                    {
                        var command = new Command("validator", "Validate custom plugin options.")
                        {
                            new CommandUsage(),
                            new HelpOption(),
                        };
                        command.Add("mode=", "Validation mode.", _ => throw new CommandOptionException("Invalid custom mode.", "mode"));
                        command.Add((_, _) => new ValueTask<int>(AltaExitCodes.Success));
                        return command;
                    },
                },
            });
        var dispatcher = CreateDispatcher(catalog);

        var result = await dispatcher.InvokeAsync(["validator", "--mode", "bad"], caller: AltaCallerIdentity.Cli).ConfigureAwait(false);

        Assert.AreEqual(AltaExitCodes.Usage, result.ExitCode);
        var lines = ReadJsonLines(result.Stdout);
        Assert.AreEqual(AltaExitCodes.Usage, lines[0].GetProperty("exitCode").GetInt32());
        Assert.AreEqual("alta.error", lines[1].GetProperty("type").GetString());
        StringAssert.Contains(lines[1].GetProperty("message").GetString(), "Invalid custom mode");
    }

    [TestMethod]
    public async Task PluginAltaCommandContribution_AppearsInHelpAndRunsWithPluginContext()
    {
        var plugin = CreatePluginDescriptor("sample-plugin");
        var catalog = new FakeAltaPluginCatalog(
            new AltaPluginCommandContribution
            {
                Plugin = plugin,
                Services = NoopPluginServices.Create(),
                Scope = PluginScope.Global,
                Command = new PluginAltaCommandContribution
                {
                    Path = "statistics",
                    Description = "Sample statistics command.",
                    Policy = new PluginAltaCommandPolicy { RequiresInProcessRuntime = true },
                    CreateCommandNode = pluginContext =>
                    {
                        var command = new Command("statistics", "Plugin statistics.")
                        {
                            new CommandUsage(),
                            new HelpOption(),
                        };
                        command.Add((_, _) =>
                        {
                            AltaJsonlWriter.WriteRecord(pluginContext.Stdout, new
                            {
                                type = "alta.plugin.sample",
                                version = 1,
                                correlationId = pluginContext.CorrelationId,
                                pluginRuntimeKey = pluginContext.Plugin.RuntimeKey,
                            });
                            return new ValueTask<int>(AltaExitCodes.Success);
                        });
                        return command;
                    },
                },
            });
        var dispatcher = CreateDispatcher(catalog);

        var help = await dispatcher.InvokeAsync(["statistics", "--help"], caller: AltaCallerIdentity.Cli).ConfigureAwait(false);
        var result = await dispatcher.InvokeAsync(["statistics"], caller: AltaCallerIdentity.Cli).ConfigureAwait(false);

        Assert.AreEqual(AltaExitCodes.Success, help.ExitCode);
        Assert.IsTrue(help.IsHelp);
        StringAssert.Contains(help.Stdout, "Usage: alta statistics");
        Assert.AreEqual(AltaExitCodes.Success, result.ExitCode);
        var lines = ReadJsonLines(result.Stdout);
        Assert.AreEqual("alta.result", lines[0].GetProperty("type").GetString());
        Assert.AreEqual("alta.plugin.sample", lines[1].GetProperty("type").GetString());
        Assert.AreEqual("sample-plugin", lines[1].GetProperty("pluginRuntimeKey").GetString());

        var capabilities = await dispatcher.InvokeAsync(["tool", "capability", "list"], caller: AltaCallerIdentity.Cli).ConfigureAwait(false);
        Assert.AreEqual(AltaExitCodes.Success, capabilities.ExitCode);
        Assert.IsTrue(ReadJsonLines(capabilities.Stdout).Any(line =>
            line.GetProperty("type").GetString() == "alta.tool.capability" &&
            line.GetProperty("path").GetString() == "statistics"));
    }

    [TestMethod]
    public async Task PluginAltaCommandContribution_CoreRootCollisionIsSkipped()
    {
        var factoryCalled = false;
        var catalog = new FakeAltaPluginCatalog(
            new AltaPluginCommandContribution
            {
                Plugin = CreatePluginDescriptor("collision-plugin"),
                Services = NoopPluginServices.Create(),
                Scope = PluginScope.Global,
                Command = new PluginAltaCommandContribution
                {
                    Path = "session",
                    CreateCommandNode = _ =>
                    {
                        factoryCalled = true;
                        return new Command("session", "Collision");
                    },
                },
            });
        var dispatcher = CreateDispatcher(catalog);

        var result = await dispatcher.InvokeAsync(["session", "--help"], caller: AltaCallerIdentity.Cli).ConfigureAwait(false);

        Assert.AreEqual(AltaExitCodes.Success, result.ExitCode);
        StringAssert.Contains(result.Stdout, "list                       List recoverable/live sessions as JSONL.");
        Assert.IsFalse(factoryCalled);
    }

    private static AltaCommandDispatcher CreateDispatcher(params IAltaCommandContributor[] contributors)
    {
        var registry = contributors.Length == 0 ? new AltaCommandRegistry() : new AltaCommandRegistry(contributors);
        return new AltaCommandDispatcher(registry, new AltaServiceCollection());
    }

    private static AltaCommandDispatcher CreateDispatcher(IAltaPluginCatalog pluginCatalog)
    {
        var registry = new AltaCommandRegistry();
        var services = new AltaServiceCollection()
            .Add(pluginCatalog)
            .Add(registry);
        return new AltaCommandDispatcher(registry, services);
    }

    private static PluginDescriptor CreatePluginDescriptor(string runtimeKey)
        => new()
        {
            RuntimeKey = runtimeKey,
            TypeName = "Sample.Plugin",
            AssemblyName = "Sample.Plugin",
            DisplayName = "Sample Plugin",
            Version = "1.0.0",
        };

    private static AgentToolInvocation CreateInvocation(JsonElement arguments)
        => new(
            new AgentBackendId("openai-responses"),
            "session-1",
            "call-1",
            "alta",
            arguments.Clone());

    private static string AssertTextItem(AgentToolResult result)
    {
        Assert.AreEqual(1, result.Items.Count);
        Assert.IsInstanceOfType(result.Items[0], typeof(AgentToolResultItem.Text));
        return ((AgentToolResultItem.Text)result.Items[0]).Value;
    }

    private static List<JsonElement> ReadJsonLines(string text)
    {
        var values = new List<JsonElement>();
        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            using var document = JsonDocument.Parse(line);
            values.Add(document.RootElement.Clone());
        }

        return values;
    }

    private sealed class FakeAltaPluginCatalog(params AltaPluginCommandContribution[] contributions) : IAltaPluginCatalog
    {
        public IReadOnlyList<AltaPluginSummary> ListPlugins()
            => contributions
                .Select(static contribution => new AltaPluginSummary
                {
                    RuntimeKey = contribution.Plugin.RuntimeKey,
                    DisplayName = contribution.Plugin.DisplayName,
                    Version = contribution.Plugin.Version,
                    Scope = contribution.Scope.ToString().ToLowerInvariant(),
                    State = "active",
                })
                .ToArray();

        public AltaPluginSummary? GetPlugin(string runtimeKey)
            => ListPlugins().FirstOrDefault(plugin => string.Equals(plugin.RuntimeKey, runtimeKey, StringComparison.OrdinalIgnoreCase));

        public IReadOnlyList<AltaCommandPolicy> ListCommandPolicies()
            => contributions
                .Select(static contribution => new AltaCommandPolicy
                {
                    Path = contribution.Command.Path,
                    RequiresInProcessRuntime = contribution.Command.Policy.RequiresInProcessRuntime,
                    IsMutating = contribution.Command.Policy.IsMutating,
                    IsDisruptive = contribution.Command.Policy.IsDisruptive,
                    SupportsCatalogOnlyContext = contribution.Command.Policy.SupportsCatalogOnlyContext,
                })
                .ToArray();

        public IReadOnlyList<AltaPluginCommandContribution> ListCommandContributions()
            => contributions;
    }

    private sealed class CancellingContributor : IAltaCommandContributor
    {
        public IEnumerable<CommandNode> CreateCommandLineNodes(AltaCommandContributionContext context)
        {
            var command = new Command("wait", "Wait until cancelled.");
            command.Add((_, _) =>
            {
                context.Invocation.CancellationToken.ThrowIfCancellationRequested();
                return new ValueTask<int>(AltaExitCodes.Success);
            });
            yield return command;
        }

        public IEnumerable<AltaCommandPolicy> GetCommandPolicies(AltaCommandContributionContext context)
        {
            yield return new AltaCommandPolicy { Path = "wait", RequiresInProcessRuntime = true };
        }
    }
}
