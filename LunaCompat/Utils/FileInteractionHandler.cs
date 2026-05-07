using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using LunaCompatCommon.Utils;

namespace LunaCompat.Utils;

internal class FileInteractionHandler
{
    #region Fields

    private readonly ILogger _logger;
    private readonly ConcurrentQueue<ActionEntry> _queue;
    private static FileInteractionHandler instance;

    #endregion

    #region Constructors

    public FileInteractionHandler(ILogger logger)
    {
        instance = this;
        _logger = logger;
        _queue = new ConcurrentQueue<ActionEntry>();
    }

    #endregion

    #region Public Methods

    public static void ExecuteTask<T>(Func<T> asyncAction, Action<T> callback = null)
    {
        instance.ExecuteTaskInternal(asyncAction, callback);
    }

    public static void ExecuteTask(Action asyncAction, Action callback = null)
    {
        instance.ExecuteTaskInternal(asyncAction, callback);
    }

    public void Update()
    {
        try
        {
            if (!_queue.TryDequeue(out var action))
                return;

            action.Invoke();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to enqueue task: {ex}");
        }
    }

    #endregion

    #region Non-Public Methods

    private void ExecuteTaskInternal<T>(Func<Task<T>> asyncAction, Action<T> callback = null)
    {
        try
        {
            Task.Run(async () =>
            {
                var obj = await asyncAction();
                if (callback != null)
                    _queue.Enqueue(new ActionEntry<T>(callback, obj));
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to enqueue task: {ex}");
        }
    }

    private void ExecuteTaskInternal<T>(Func<T> asyncAction, Action<T> callback = null)
    {
        try
        {
            Task.Run(() =>
            {
                var obj = asyncAction();
                if (callback != null)
                    _queue.Enqueue(new ActionEntry<T>(callback, obj));
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to enqueue task: {ex}");
        }
    }

    private void ExecuteTaskInternal(Action asyncAction, Action callback)
    {
        try
        {
            Task.Run(() =>
            {
                asyncAction();
                if (callback != null)
                    _queue.Enqueue(new ActionEntry(callback));
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to enqueue task: {ex}");
        }
    }

    #endregion

    #region Nested Types

    private class ActionEntry
    {
        #region Fields

        private readonly Action _action;

        #endregion

        #region Constructors

        public ActionEntry(Action action)
        {
            _action = action;
        }

        protected ActionEntry()
        {
        }

        #endregion

        #region Public Methods

        public virtual void Invoke()
        {
            _action();
        }

        #endregion
    }

    private class ActionEntry<T> : ActionEntry
    {
        #region Fields

        private readonly Action<T> _action;
        private readonly T _parameter;

        #endregion

        #region Constructors

        public ActionEntry(Action<T> action, T parameter)
        {
            _action = action;
            _parameter = parameter;
        }

        #endregion

        #region Public Methods

        public override void Invoke()
        {
            _action(_parameter);
        }

        #endregion
    }

    #endregion
}
