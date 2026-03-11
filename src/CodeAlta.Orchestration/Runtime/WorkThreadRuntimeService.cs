using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using CodeAlta.Agent;
using CodeAlta.Persistence;
using CodeAlta.Catalog;
using CodeAlta.Catalog.Roles;

namespace CodeAlta.Orchestration.Runtime;

/// <summary>
/// Owns per-thread coordinator sessions and projects sanitized runtime events.
/// </summary>
public sealed class WorkThreadRuntimeService : IAsyncDisposable
{
    private static readonly Regex ScheduleBlockRegex = new(
        @"```codealta_schedule\s*\n.*?```",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);

    private readonly AgentHub _agentHub;
    private readonly WorkspaceCatalog _workspaceCatalog;
    private readonly WorkThreadCatalog _threadCatalog;
    private readonly RoleProfileStore _roleProfileStore;
    private readonly AgentInstructionTemplateProvider _instructionTemplateProvider;
    private readonly WorkspaceCatalogOptions _catalogOptions;
    private readonly Channel<WorkThreadRuntimeEvent> _events = Channel.CreateUnbounded<WorkThreadRuntimeEvent>();
    private readonly Dictionary<string, ThreadSessionEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _gate = new(initialCount: 1, maxCount: 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkThreadRuntimeService"/> class.
    /// </summary>
    public WorkThreadRuntimeService(
        AgentHub agentHub,
        WorkspaceCatalog workspaceCatalog,
        WorkThreadCatalog threadCatalog,
        RoleProfileStore roleProfileStore,
        AgentInstructionTemplateProvider instructionTemplateProvider,
        WorkspaceCatalogOptions catalogOptions)
    {
        ArgumentNullException.ThrowIfNull(agentHub);
        ArgumentNullException.ThrowIfNull(workspaceCatalog);
        ArgumentNullException.ThrowIfNull(threadCatalog);
        ArgumentNullException.ThrowIfNull(roleProfileStore);
        ArgumentNullException.ThrowIfNull(instructionTemplateProvider);
        ArgumentNullException.ThrowIfNull(catalogOptions);

        _agentHub = agentHub;
        _workspaceCatalog = workspaceCatalog;
        _threadCatalog = threadCatalog;
        _roleProfileStore = roleProfileStore;
        _instructionTemplateProvider = instructionTemplateProvider;
        _catalogOptions = catalogOptions;
    }

    /// <summary>
    /// Streams sanitized runtime events across all active threads.
    /// </summary>
    public IAsyncEnumerable<WorkThreadRuntimeEvent> StreamEventsAsync(CancellationToken cancellationToken = default)
    {
        return _events.Reader.ReadAllAsync(cancellationToken);
    }

    /// <summary>
    /// Ensures that the thread has an active coordinator session.
    /// </summary>
    /// <param name="thread">The thread descriptor.</param>
    /// <param name="options">Execution options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The coordinator agent identifier.</returns>
    public async Task<AgentId> EnsureCoordinatorSessionAsync(
        WorkThreadDescriptor thread,
        WorkThreadExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(thread);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.WorkingDirectory);

        var workspaces = await _workspaceCatalog.LoadAsync(cancellationToken).ConfigureAwait(false);
        var workspace = thread.Kind == WorkThreadKind.Global
            ? null
            : workspaces.FirstOrDefault(x => string.Equals(x.Id, thread.WorkspaceRef, StringComparison.OrdinalIgnoreCase));
        var projects = ResolveProjects(thread, workspace);
        var coordinatorProfile = await _roleProfileStore.GetByIdAsync(
                [Path.Combine(_catalogOptions.GlobalRepoRoot, "agents"), .. options.ProjectRoots.Select(static root => Path.Combine(root, ".codealta", "agents"))],
                "coordinator",
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Coordinator profile was not available.");
        var instructions = _instructionTemplateProvider.BuildCoordinatorInstructions(
            thread,
            workspace,
            projects,
            coordinatorProfile);

        ThreadSessionEntry? previousEntry = null;
        AgentId agentId;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_entries.TryGetValue(thread.ThreadId, out var existing)
                && existing.Matches(options))
            {
                return existing.AgentId;
            }

            if (_entries.TryGetValue(thread.ThreadId, out previousEntry))
            {
                _entries.Remove(thread.ThreadId);
            }
        }
        finally
        {
            _gate.Release();
        }

        if (previousEntry is not null)
        {
            await previousEntry.DisposeAsync(_agentHub).ConfigureAwait(false);
        }

        var identity = await _agentHub.RegisterAgentAsync(
                "coordinator",
                ToAgentScope(thread),
                options.BackendId,
                cancellationToken)
            .ConfigureAwait(false);
        agentId = identity.AgentId;

        await _agentHub.StartSessionAsync(
                agentId,
                new AgentSessionCreateOptions
                {
                    Model = options.Model ?? coordinatorProfile.DefaultModel,
                    ReasoningEffort = options.ReasoningEffort ?? ParseReasoningEffort(coordinatorProfile.DefaultReasoningEffort),
                    Streaming = true,
                    WorkingDirectory = options.WorkingDirectory,
                    SystemMessage = instructions.SystemMessage,
                    DeveloperInstructions = instructions.DeveloperInstructions,
                    Tools = options.Tools,
                    OnPermissionRequest = options.OnPermissionRequest,
                    OnUserInputRequest = options.OnUserInputRequest,
                },
                cancellationToken)
            .ConfigureAwait(false);

        var projector = new EventProjector(thread.ThreadId, _events.Writer);
        var subscription = await _agentHub.SubscribeSessionEventsAsync(
                agentId,
                projector.Project,
                cancellationToken)
            .ConfigureAwait(false);

        var entry = new ThreadSessionEntry(
            agentId,
            options.BackendId,
            options.WorkingDirectory,
            options.Model ?? coordinatorProfile.DefaultModel,
            options.ReasoningEffort ?? ParseReasoningEffort(coordinatorProfile.DefaultReasoningEffort),
            projector,
            subscription);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _entries[thread.ThreadId] = entry;
        }
        finally
        {
            _gate.Release();
        }

        return agentId;
    }

    /// <summary>
    /// Sends input to the coordinator session for a thread.
    /// </summary>
    public async Task<AgentRunId> SendAsync(
        WorkThreadDescriptor thread,
        WorkThreadExecutionOptions options,
        AgentSendOptions sendOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(thread);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sendOptions);

        var agentId = await EnsureCoordinatorSessionAsync(thread, options, cancellationToken).ConfigureAwait(false);
        if (!thread.IsWorkspaceLocked && thread.Kind == WorkThreadKind.WorkspaceThread)
        {
            thread.MarkStarted(DateTimeOffset.UtcNow);
            await _threadCatalog.SaveAsync(thread, cancellationToken).ConfigureAwait(false);
        }

        return await _agentHub.RunAsync(agentId, sendOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Steers the current coordinator run for a thread.
    /// </summary>
    public async Task<AgentRunId> SteerAsync(
        WorkThreadDescriptor thread,
        WorkThreadExecutionOptions options,
        AgentSteerOptions steerOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(thread);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(steerOptions);

        var agentId = await EnsureCoordinatorSessionAsync(thread, options, cancellationToken).ConfigureAwait(false);
        return await _agentHub.SteerAsync(agentId, steerOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an explicit host handoff from one thread into another.
    /// </summary>
    public async Task<AgentRunId> HandoffAsync(
        WorkThreadDescriptor sourceThread,
        WorkThreadDescriptor targetThread,
        WorkThreadExecutionOptions targetOptions,
        string instruction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceThread);
        ArgumentNullException.ThrowIfNull(targetThread);
        ArgumentNullException.ThrowIfNull(targetOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(instruction);

        var timestamp = DateTimeOffset.UtcNow;
        _events.Writer.TryWrite(new WorkThreadHostEvent(
            sourceThread.ThreadId,
            timestamp,
            AgentSessionUpdateKind.Handoff,
            $"Handed off work to thread '{targetThread.Title}'."));

        _events.Writer.TryWrite(new WorkThreadHostEvent(
            targetThread.ThreadId,
            timestamp,
            AgentSessionUpdateKind.Handoff,
            $"Received handoff from thread '{sourceThread.Title}'."));

        var handoffInput = AgentInput.Text(
            $"Handoff from thread '{sourceThread.Title}' ({sourceThread.ThreadId}): {instruction}");
        return await SendAsync(
                targetThread,
                targetOptions,
                new AgentSendOptions { Input = handoffInput },
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Aborts active work in the thread coordinator session.
    /// </summary>
    public async Task AbortAsync(string threadId, CancellationToken cancellationToken = default)
    {
        var entry = await GetEntryAsync(threadId, cancellationToken).ConfigureAwait(false);
        await _agentHub.AbortAsync(entry.AgentId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets sanitized history for an active thread session.
    /// </summary>
    public async Task<IReadOnlyList<AgentEvent>> GetHistoryAsync(string threadId, CancellationToken cancellationToken = default)
    {
        var entry = await GetEntryAsync(threadId, cancellationToken).ConfigureAwait(false);
        var history = await _agentHub.GetSessionHistoryAsync(entry.AgentId, cancellationToken).ConfigureAwait(false);
        return entry.Projector.ProjectHistory(history);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _events.Writer.TryComplete();

        foreach (var entry in _entries.Values)
        {
            await entry.DisposeAsync(_agentHub).ConfigureAwait(false);
        }

        _entries.Clear();
        _gate.Dispose();
    }

    private async Task<ThreadSessionEntry> GetEntryAsync(string threadId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            throw new ArgumentException("Thread id is required.", nameof(threadId));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_entries.TryGetValue(threadId, out var entry))
            {
                throw new InvalidOperationException($"Thread '{threadId}' does not have an active coordinator session.");
            }

            return entry;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static AgentScope ToAgentScope(WorkThreadDescriptor thread)
    {
        return thread.Kind switch
        {
            WorkThreadKind.Global => new AgentScope { Kind = AgentScopeKind.Global },
            WorkThreadKind.WorkspaceThread => new AgentScope { Kind = AgentScopeKind.Workspace, Id = thread.WorkspaceRef },
            _ => throw new InvalidOperationException($"Unsupported thread kind '{thread.Kind}'."),
        };
    }

    private static IReadOnlyList<ProjectDescriptor> ResolveProjects(
        WorkThreadDescriptor thread,
        WorkspaceDescriptor? workspace)
    {
        if (workspace is null)
        {
            return [];
        }

        return thread.ScopeMode switch
        {
            WorkThreadScopeMode.AllProjects => workspace.Projects.ToArray(),
            _ => workspace.Projects
                .Where(project => thread.ProjectRefs.Contains(project.Id, StringComparer.OrdinalIgnoreCase))
                .ToArray(),
        };
    }

    private static AgentReasoningEffort? ParseReasoningEffort(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "minimal" => AgentReasoningEffort.Minimal,
            "low" => AgentReasoningEffort.Low,
            "medium" => AgentReasoningEffort.Medium,
            "high" => AgentReasoningEffort.High,
            "xhigh" => AgentReasoningEffort.XHigh,
            _ => null,
        };
    }

    private sealed class ThreadSessionEntry
    {
        public ThreadSessionEntry(
            AgentId agentId,
            AgentBackendId backendId,
            string workingDirectory,
            string? model,
            AgentReasoningEffort? reasoningEffort,
            EventProjector projector,
            IDisposable subscription)
        {
            AgentId = agentId;
            BackendId = backendId;
            WorkingDirectory = workingDirectory;
            Model = model;
            ReasoningEffort = reasoningEffort;
            Projector = projector;
            Subscription = subscription;
        }

        public AgentId AgentId { get; }

        public AgentBackendId BackendId { get; }

        public string WorkingDirectory { get; }

        public string? Model { get; }

        public AgentReasoningEffort? ReasoningEffort { get; }

        public IDisposable Subscription { get; }

        public EventProjector Projector { get; }

        public bool Matches(WorkThreadExecutionOptions options)
        {
            return string.Equals(BackendId.Value, options.BackendId.Value, StringComparison.OrdinalIgnoreCase)
                && string.Equals(WorkingDirectory, options.WorkingDirectory, StringComparison.Ordinal)
                && string.Equals(Model, options.Model, StringComparison.Ordinal)
                && ReasoningEffort == options.ReasoningEffort;
        }

        public async Task DisposeAsync(AgentHub hub)
        {
            Subscription.Dispose();
            await hub.StopSessionAsync(AgentId).ConfigureAwait(false);
        }

    }

    private sealed class EventProjector
    {
        private readonly string _threadId;
        private readonly ChannelWriter<WorkThreadRuntimeEvent> _writer;
        private readonly Dictionary<string, ContentState> _content = new(StringComparer.Ordinal);

        public EventProjector(string threadId, ChannelWriter<WorkThreadRuntimeEvent> writer)
        {
            _threadId = threadId;
            _writer = writer;
        }

        public void Project(AgentEvent @event)
        {
            if (TrySanitize(@event, out var sanitized) && sanitized is not null)
            {
                _writer.TryWrite(new WorkThreadAgentEvent(_threadId, sanitized));
            }
        }

        public IReadOnlyList<AgentEvent> ProjectHistory(IReadOnlyList<AgentEvent> history)
        {
            var results = new List<AgentEvent>(history.Count);
            foreach (var @event in history)
            {
                if (TrySanitize(@event, out var sanitized) && sanitized is not null)
                {
                    results.Add(sanitized);
                }
            }

            return results;
        }

        private bool TrySanitize(AgentEvent @event, out AgentEvent? sanitized)
        {
            switch (@event)
            {
                case AgentContentDeltaEvent delta when delta.Kind == AgentContentKind.Assistant:
                    sanitized = SanitizeDelta(delta);
                    return sanitized is not null;
                case AgentContentCompletedEvent completed when completed.Kind == AgentContentKind.Assistant:
                    sanitized = SanitizeCompleted(completed);
                    return sanitized is not null;
                default:
                    sanitized = @event;
                    return true;
            }
        }

        private AgentEvent? SanitizeDelta(AgentContentDeltaEvent delta)
        {
            if (!_content.TryGetValue(delta.ContentId, out var state))
            {
                state = new ContentState();
                _content[delta.ContentId] = state;
            }

            state.Raw.Append(delta.Delta);
            var stripped = StripScheduleBlocks(state.Raw.ToString());
            if (stripped.Length == 0)
            {
                return null;
            }

            string deltaText;
            if (stripped.StartsWith(state.PreviousSanitized, StringComparison.Ordinal))
            {
                deltaText = stripped[state.PreviousSanitized.Length..];
            }
            else
            {
                deltaText = stripped;
            }

            state.PreviousSanitized = stripped;
            if (string.IsNullOrEmpty(deltaText))
            {
                return null;
            }

            return delta with { Delta = deltaText };
        }

        private AgentEvent? SanitizeCompleted(AgentContentCompletedEvent completed)
        {
            var stripped = StripScheduleBlocks(completed.Content);
            _content[completed.ContentId] = new ContentState
            {
                PreviousSanitized = stripped,
            };

            return string.IsNullOrWhiteSpace(stripped)
                ? null
                : completed with { Content = stripped };
        }

        private static string StripScheduleBlocks(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }

            var stripped = ScheduleBlockRegex.Replace(content, string.Empty);
            return stripped.Trim();
        }

        private sealed class ContentState
        {
            public StringBuilder Raw { get; } = new();

            public string PreviousSanitized { get; set; } = string.Empty;
        }
    }
}

