using CodeAlta.App.Context;
using CodeAlta.App.State;
using CodeAlta.Catalog;
using CodeAlta.Models;
using CodeAlta.Threading;

namespace CodeAlta.Tests;

[TestClass]
public sealed class ThreadCommandContextTests
{
    [TestMethod]
    public void ClearDraftInput_InvokesCallbackOnUiDispatcher()
    {
        var dispatcher = new RecordingUiDispatcher();
        var calledOnUiThread = false;
        var context = CreateContext(
            dispatcher,
            clearDraftInput: () => calledOnUiThread = dispatcher.CheckAccess());

        context.ClearDraftInput();

        Assert.IsTrue(calledOnUiThread);
        Assert.AreEqual(1, dispatcher.InvokeActionCount);
    }

    [TestMethod]
    public void IsThreadInputEmpty_InvokesCallbackOnUiDispatcher()
    {
        var dispatcher = new RecordingUiDispatcher();
        var callbackRanOnUi = false;
        var context = CreateContext(
            dispatcher,
            isThreadInputEmpty: () =>
            {
                callbackRanOnUi = dispatcher.CheckAccess();
                return true;
            });

        var isEmpty = context.IsThreadInputEmpty();

        Assert.IsTrue(isEmpty);
        Assert.IsTrue(callbackRanOnUi);
        Assert.AreEqual(1, dispatcher.InvokeFuncCount);
    }

    private static ThreadCommandContext CreateContext(
        IUiDispatcher dispatcher,
        Action? clearDraftInput = null,
        Func<bool>? isThreadInputEmpty = null)
    {
        clearDraftInput ??= static () => { };
        isThreadInputEmpty ??= static () => true;

        return new ThreadCommandContext(
            dispatcher,
            static () => false,
            static _ => Task.FromResult<WorkThreadDescriptor?>(null),
            static _ => Task.FromResult<WorkThreadDescriptor?>(null),
            static () => Task.CompletedTask,
            static () => true,
            clearDraftInput,
            static () => { },
            static () => { },
            isThreadInputEmpty,
            static _ => { },
            static () => { },
            static () => { },
            static (_, _, _) => { },
            static (_, _, _, _) => { },
            static (_, _, _) => { });
    }

    private sealed class RecordingUiDispatcher : IUiDispatcher
    {
        private int _depth;

        public int InvokeActionCount { get; private set; }

        public int InvokeFuncCount { get; private set; }

        public bool CheckAccess() => _depth > 0;

        public void Post(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            _depth++;
            try
            {
                action();
            }
            finally
            {
                _depth--;
            }
        }

        public Task InvokeAsync(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            InvokeActionCount++;
            _depth++;
            try
            {
                action();
                return Task.CompletedTask;
            }
            finally
            {
                _depth--;
            }
        }

        public Task<T> InvokeAsync<T>(Func<T> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            InvokeFuncCount++;
            _depth++;
            try
            {
                return Task.FromResult(action());
            }
            finally
            {
                _depth--;
            }
        }
    }
}
