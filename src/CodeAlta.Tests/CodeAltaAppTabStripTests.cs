using System.Reflection;
using System.IO;
using CodeAlta.Agent;
using CodeAlta.Catalog;
using CodeAlta.Catalog.Roles;
using CodeAlta.Orchestration.Runtime;
using CodeAlta.Persistence;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace CodeAlta.Tests;

[TestClass]
public sealed class CodeAltaAppTabStripTests
{
    [TestMethod]
    public void ResolveOpenTabIndicatorKind_PrefersRunningAndMapsTone()
    {
        Assert.AreEqual(
            CodeAltaApp.OpenTabIndicatorKind.Running,
            CodeAltaApp.ResolveOpenTabIndicatorKind(isBusy: true, CodeAltaApp.StatusTone.Ready));
        Assert.AreEqual(
            CodeAltaApp.OpenTabIndicatorKind.Ready,
            CodeAltaApp.ResolveOpenTabIndicatorKind(isBusy: false, CodeAltaApp.StatusTone.Ready));
        Assert.AreEqual(
            CodeAltaApp.OpenTabIndicatorKind.Warning,
            CodeAltaApp.ResolveOpenTabIndicatorKind(isBusy: false, CodeAltaApp.StatusTone.Warning));
        Assert.AreEqual(
            CodeAltaApp.OpenTabIndicatorKind.Error,
            CodeAltaApp.ResolveOpenTabIndicatorKind(isBusy: false, CodeAltaApp.StatusTone.Error));
    }

    [TestMethod]
    public void CompactTabTitle_DoesNotChangeForSelectionState()
    {
        var method = typeof(CodeAltaApp).GetMethod("CompactTabTitle", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        var title = (string?)method.Invoke(null, ["Review startup"]);

        Assert.AreEqual("Review startup", title);
    }

    [TestMethod]
    public void CreateThreadTabPageContentPlaceholder_ReturnsHiddenDetachedVisual()
    {
        var method = typeof(CodeAltaApp).GetMethod("CreateThreadTabPageContentPlaceholder", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        var first = (Visual?)method.Invoke(null, null);
        var second = (Visual?)method.Invoke(null, null);

        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        Assert.IsFalse(first.IsVisible);
        Assert.IsFalse(second.IsVisible);
        Assert.IsNull(first.Parent);
        Assert.IsNull(second.Parent);
        Assert.AreNotSame(first, second);
    }

    [TestMethod]
    public void BuildDraftTabTitle_ReflectsScope()
    {
        Assert.AreEqual("Global draft", CodeAltaApp.BuildDraftTabTitle(selectedProject: null, globalScopeSelected: true));
        Assert.AreEqual(
            "CodeAlta draft",
            CodeAltaApp.BuildDraftTabTitle(
                new ProjectDescriptor { DisplayName = "CodeAlta" },
                globalScopeSelected: false));
    }

    [TestMethod]
    public async Task SyncThreadTabControl_ProjectScopeAddsSelectedDraftTabAlongsideOpenThread()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"codealta-ui-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);

        try
        {
            var catalogOptions = new CatalogOptions { GlobalRoot = rootPath };
            var projectCatalog = new ProjectCatalog(catalogOptions);
            var threadCatalog = new WorkThreadCatalog(catalogOptions);
            var db = await CreateTestDbAsync(rootPath);
            await using var hub = new AgentHub(new AgentBackendFactory(), new AgentRepository(db));
            await using var runtimeService = new WorkThreadRuntimeService(
                hub,
                projectCatalog,
                threadCatalog,
                new RoleProfileStore(),
                new AgentInstructionTemplateProvider(),
                catalogOptions);

            var project = new ProjectDescriptor
            {
                Id = ProjectId.NewVersion7().ToString(),
                DisplayName = "CodeAlta",
                Slug = "codealta",
                ProjectPath = Path.Combine(rootPath, "repo"),
            };
            Directory.CreateDirectory(project.ProjectPath);

            var thread = new WorkThreadDescriptor
            {
                ThreadId = "thread-1",
                Kind = WorkThreadKind.ProjectThread,
                BackendId = AgentBackendIds.Codex.Value,
                BackendSessionId = "session-1",
                ProjectRef = project.Id,
                WorkingDirectory = project.ProjectPath,
                Title = "Investigate draft tabs",
                Status = WorkThreadStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                LastActiveAt = DateTimeOffset.UtcNow,
            };

            var ui = new CodeAltaApp(projectCatalog, threadCatalog, runtimeService, catalogOptions, hub);
            SetPrivateField(ui, "_projects", (IReadOnlyList<ProjectDescriptor>)[project]);
            SetPrivateField(ui, "_threads", (IReadOnlyList<WorkThreadDescriptor>)[thread]);
            SetPrivateField(ui, "_selectedProjectId", project.Id);
            SetPrivateField(ui, "_selectedThreadId", thread.ThreadId);
            SetPrivateField(
                ui,
                "_viewState",
                new WorkThreadViewState
                {
                    OpenThreadIds = [thread.ThreadId],
                    SelectedThreadId = thread.ThreadId,
                });

            var createTabControlMethod = typeof(CodeAltaApp).GetMethod("CreateThreadTabControl", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(createTabControlMethod);
            var tabControl = (TabControl?)createTabControlMethod.Invoke(ui, null);
            Assert.IsNotNull(tabControl);
            SetPrivateField(ui, "_threadTabControl", tabControl);

            InvokePrivate(ui, "SelectProjectScope", project.Id);
            InvokePrivate(ui, "SyncThreadTabControl");

            Assert.AreEqual(2, tabControl.Tabs.Count);
            Assert.AreEqual(1, tabControl.SelectedIndex);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    private static async Task<CodeAltaDb> CreateTestDbAsync(string rootPath)
    {
        var dbPath = Path.Combine(rootPath, "state", "db", "codealta.db");
        var db = new CodeAltaDb(new CodeAltaDbOptions { DatabasePath = dbPath });
        await db.InitializeAsync();
        return db;
    }

    private static object? InvokePrivate(object instance, string methodName, params object[]? args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return method.Invoke(instance, args);
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(instance, value);
    }
}
