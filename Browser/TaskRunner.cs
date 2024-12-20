#region

#region

using System.Collections.Concurrent;

#endregion

namespace Browser;

#endregion

public sealed class TaskUnit(Delegate code, params object[] parameters) {
    public object? Run() {
        return code.DynamicInvoke(parameters);
    }
}

public sealed class TaskRunner : IDisposable {
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly object _lock = new();
    private readonly Thread _mainThread;
    private readonly Tab _tab;
    private readonly ManualResetEventSlim _taskAvailable = new(false);
    private readonly ConcurrentQueue<TaskUnit> _tasks = new();

    public TaskRunner(Tab tab) {
        _tab = tab;
        _mainThread = new Thread(Run) {
            Name = "Main thread",
            IsBackground = true
        };
    }

    public void Dispose() {
        SetNeedsQuit();
        _mainThread.Join();
        _taskAvailable.Dispose();
        _cancellationTokenSource.Dispose();
    }

    public void ScheduleTask(TaskUnit taskUnit) {
        _tasks.Enqueue(taskUnit);
        _taskAvailable.Set();
    }

    public void SetNeedsQuit() {
        _cancellationTokenSource.Cancel();
        _taskAvailable.Set();
    }

    public void ClearPendingTasks() {
        while (_tasks.TryDequeue(out _)) {
        }
    }

    public void StartThread() {
        _mainThread.Start();
    }

    public void Run() {
        try {
            while (!_cancellationTokenSource.Token.IsCancellationRequested) {
                _taskAvailable.Wait(_cancellationTokenSource.Token);
                while (_tasks.TryDequeue(out var task)) task.Run();
                _taskAvailable.Reset();
            }
            HandleQuit();
        }
        catch (OperationCanceledException) {
            // Expected when cancellation is requested
        }
    }

    private void HandleQuit() {
        // Optional override for cleanup
    }
}