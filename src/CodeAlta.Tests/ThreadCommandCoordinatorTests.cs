using CodeAlta.Agent;
using CodeAlta.App;
using CodeAlta.App.Context;
using CodeAlta.Catalog;
using CodeAlta.Catalog.Roles;
using CodeAlta.Models;
using CodeAlta.Orchestration.Runtime;
using CodeAlta.Persistence;
using CodeAlta.Threading;
using CodeAlta.ViewModels;
using CodeAlta.Views;

namespace CodeAlta.Tests;

[TestClass]
public sealed class ThreadCommandCoordinatorTests
{
    [TestMethod]
    public async Task SendSelectedThreadPromptAsync_UsesDraftPromptCapturedBeforeThreadCreationRefresh()
    {
        using var temp = TempDirectory.Create();
        var db = await CreateDbAsync(temp.Path).ConfigureAwait(false);
        var repository = new AgentRepository(db);

        var backendFactory = new AgentBackendFactory();
        var backend = new RecordingBackend();
        backendFactory.Register(backend.BackendId.Value, () => backend);

        await using var hub = new AgentHub(backendFactory, repository);
        var catalogOptions = new CatalogOptions { GlobalRoot = temp.Path };
        var runtimeService = new WorkThreadRuntimeService(
            hub,
            new ProjectCatalog(catalogOptions),
            new WorkThreadCatalog(catalogOptions),
            new RoleProfileStore(),
            new AgentInstructionTemplateProvider(),
            catalogOptions);

        var dispatcher = new InlineUiDispatcher();
        var threadState = new ShellThreadStateCoordinator(
            new ProjectCatalog(catalogOptions),
            new WorkThreadCatalog(catalogOptions),
            () => dispatcher,
            static () => null,
            static _ => true,
            static _ => { },
            static (_, _, _, _, _) => { },
            static (_, _) => Task.CompletedTask,
            static () => { },
            static () => { },
            static () => { },
            static _ => { },
            static (_, _, _) => { });
        threadState.ViewState = new WorkThreadViewState();
        threadState.GlobalScopeSelected = true;

        var threadSelection = new ThreadSelectionContext(
            threadState,
            static (_, _) => Task.CompletedTask,
            threadId => string.Equals(threadState.SelectedThreadId, threadId, StringComparison.OrdinalIgnoreCase));
        var threadInput = new ChatPromptEditor(_ => { })
        {
            Text = "Investigate the regression",
        };
        var selectorUi = new ChatSelectorUiContext(
            static () => null,
            static () => null,
            static () => null,
            () => threadInput,
            () => dispatcher,
            static () => { });
        var backendState = new ChatBackendState(backend.BackendId, "Fake Chat")
        {
            Availability = ChatBackendAvailability.Ready,
        };
        var chatBackendStates = new Dictionary<string, ChatBackendState>(StringComparer.OrdinalIgnoreCase)
        {
            [backend.BackendId.Value] = backendState,
        };
        var queueCoordinator = new ThreadPromptQueueCoordinator(
            new ThreadWorkspaceViewModel(),
            threadSelection,
            static () => { },
            action => action(),
            static () => { },
            static (_, _, _) => Task.CompletedTask,
            static (_, _, _) => Task.CompletedTask);
        var clearThreadInputCallCount = 0;
        var commandContext = new ThreadCommandContext(
            static () => false,
            async () =>
            {
                var thread = await runtimeService.CreateGlobalThreadAsync(CreateExecutionOptions(backend.BackendId, temp.Path), title: null).ConfigureAwait(false);
                await threadState.RegisterCreatedThreadAsync(thread).ConfigureAwait(false);
                threadInput.Text = string.Empty;
                return thread;
            },
            static () => Task.FromResult<WorkThreadDescriptor?>(null),
            static () => Task.CompletedTask,
            static () => true,
            static () => { },
            static () => { },
            () =>
            {
                clearThreadInputCallCount++;
                threadInput.Text = string.Empty;
            },
            static () => { },
            static () => { },
            static (_, _, _) => { },
            static (_, _, _, _) => { },
            static (_, _, _) => { });
        var coordinator = new ThreadCommandCoordinator(
            runtimeService,
            catalogOptions,
            chatBackendStates,
            threadSelection,
            selectorUi,
            new ChatPreferenceContext(
                static _ => { },
                static _ => { },
                static (_, _, _) => { },
                static (_, _, _, _, _) => { }),
            commandContext,
            queueCoordinator,
            new PromptComposerViewModel());

        await coordinator.SendSelectedThreadPromptAsync(steer: false).ConfigureAwait(false);

        Assert.AreEqual("Investigate the regression", backend.LastSentText);
        Assert.AreEqual(1, clearThreadInputCallCount);
        Assert.IsNotNull(threadSelection.GetSelectedThread());
        Assert.IsNotNull(threadSelection.GetSelectedThread()!.StartedAt);
    }

    private static WorkThreadExecutionOptions CreateExecutionOptions(AgentBackendId backendId, string workingDirectory)
    {
        return new WorkThreadExecutionOptions
        {
            BackendId = backendId,
            WorkingDirectory = workingDirectory,
            OnPermissionRequest = static (_, _) => Task.FromResult(new AgentPermissionDecision(AgentPermissionDecisionKind.AllowOnce)),
            OnUserInputRequest = static (_, _) => Task.FromResult(new AgentUserInputResponse(new Dictionary<string, string>())),
        };
    }

    private static async Task<CodeAltaDb> CreateDbAsync(string rootPath)
    {
        var dbPath = Path.Combine(rootPath, "state", "db", "codealta.db");
        var db = new CodeAltaDb(new CodeAltaDbOptions { DatabasePath = dbPath });
        await db.InitializeAsync().ConfigureAwait(false);
        return db;
    }

    private sealed class InlineUiDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;

        public void Post(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            action();
        }

        public Task InvokeAsync(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            action();
            return Task.CompletedTask;
        }

        public Task<T> InvokeAsync<T>(Func<T> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            return Task.FromResult(action());
        }
    }

    private sealed class RecordingBackend : IAgentBackend
    {
        public AgentBackendId BackendId => new("fakechat");

        public string DisplayName => "Fake Chat";

        public string? LastSentText { get; private set; }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<AgentModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AgentModelInfo>>([]);

        public Task<IReadOnlyList<AgentSessionMetadata>> ListSessionsAsync(
            AgentSessionListFilter? filter = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AgentSessionMetadata>>([]);

        public Task<IAgentSession> CreateSessionAsync(
            AgentSessionCreateOptions options,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IAgentSession>(new RecordingSession(this));

        public Task<IAgentSession> ResumeSessionAsync(
            string sessionId,
            AgentSessionResumeOptions options,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IAgentSession>(new RecordingSession(this, sessionId));

        private sealed class RecordingSession : IAgentSession
        {
            private readonly RecordingBackend _backend;

            public RecordingSession(RecordingBackend backend, string? sessionId = null)
            {
                _backend = backend;
                SessionId = sessionId ?? "fakechat-session";
            }

            public AgentBackendId BackendId => _backend.BackendId;

            public string SessionId { get; }

            public string? WorkspacePath => null;

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public async IAsyncEnumerable<AgentEvent> StreamEventsAsync(
                [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.CompletedTask.ConfigureAwait(false);
                yield break;
            }

            public IDisposable Subscribe(Action<AgentEvent> handler)
                => DisposableAction.Create(static () => { });

            public Task<AgentRunId> SendAsync(AgentSendOptions options, CancellationToken cancellationToken = default)
            {
                _backend.LastSentText = (options.Input.Items.Single() as AgentInputItem.Text)?.Value;
                return Task.FromResult(new AgentRunId("fake-run-1"));
            }

            public Task<AgentRunId> SteerAsync(AgentSteerOptions options, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task AbortAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task CompactAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task<IReadOnlyList<AgentEvent>> GetHistoryAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<AgentEvent>>([]);
        }
    }

    private sealed class DisposableAction : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        private DisposableAction(Action dispose)
        {
            _dispose = dispose;
        }

        public static IDisposable Create(Action dispose)
        {
            ArgumentNullException.ThrowIfNull(dispose);
            return new DisposableAction(dispose);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _dispose();
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        private TempDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempDirectory Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"CodeAlta.Tests.{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return new TempDirectory(path);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
