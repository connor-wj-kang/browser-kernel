// #region
//
// using System.Collections.Concurrent;
// using System.Diagnostics;
//
// #endregion
//
// namespace Browser;
//
// public class TaskRunner {
//     private readonly object _lock = new();
//     public readonly Thread MainThread;
//     public readonly ConcurrentQueue<Task> Tasks = new();
//     public bool NeedsQuit;
//     public Tab Tab;
//
//     public TaskRunner(Tab tab) {
//         Tab = tab;
//         MainThread = new Thread(Run) {
//             Name = "Main Thread",
//             IsBackground = true
//         };
//     }
//
//     public void ScheduleTask(Task task) {
//         Tasks.Enqueue(task);
//         lock (_lock) {
//             Monitor.PulseAll(_lock);
//         }
//     }
//
//     public void SetNeedsQuit() {
//         lock (_lock) {
//             NeedsQuit = true;
//             Monitor.PulseAll(_lock);
//         }
//     }
//
//     public void ClearPendingTasks() {
//         Tasks.Clear();
//     }
//
//     public void StartThread() {
//         MainThread.Start();
//     }
//
//     public void HandleQuit() {
//     }
//
//     public void Run() {
//         while (true) {
//             Debug.WriteLine("Main thread running");
//             bool needsQuit;
//             lock (_lock) {
//                 needsQuit = NeedsQuit;
//             }
//             if (needsQuit) {
//                 HandleQuit();
//                 return;
//             }
//             if (Tasks.TryDequeue(out var task)) task.Start();
//             lock (_lock) {
//                 if (Tasks.IsEmpty && !NeedsQuit) Monitor.Wait(_lock);
//             }
//         }
//     }
// }

