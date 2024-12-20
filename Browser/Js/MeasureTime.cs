#region

using System.Diagnostics;
using System.IO;

#endregion

namespace Browser.Js;

public sealed class MeasureTime : IDisposable {
    private readonly StreamWriter _file = new("browser.trace");
    private readonly Lock _lock = new();

    public MeasureTime() {
        _file.Write("{\"traceEvents\": [");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        _file.Write(
            "{ \"name\": \"process_name\"," +
            "\"ph\": \"M\"," +
            $"\"ts\": {ts}," +
            "\"pid\": 1, \"cat\": \"__metadata\"," +
            "\"args\": {\"name\": \"Browser\"}}");
        _file.Flush();
    }

    public void Dispose() {
        Finish();
        _file.Dispose();
    }

    public void Time(string name) {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        var tid = Environment.CurrentManagedThreadId;
        lock (_lock) {
            _file.Write(
                $", {{ \"ph\": \"B\", \"cat\": \"_\"," +
                $"\"name\": \"{name}\"," +
                $"\"ts\": {ts}," +
                $"\"pid\": 1, \"tid\": {tid}}}");
            _file.Flush();
        }
    }

    public void Stop(string name) {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        var tid = Environment.CurrentManagedThreadId;
        lock (_lock) {
            _file.Write(
                $", {{ \"ph\": \"E\", \"cat\": \"_\"," +
                $"\"name\": \"{name}\"," +
                $"\"ts\": {ts}," +
                $"\"pid\": 1, \"tid\": {tid}}}");
            _file.Flush();
        }
    }

    public void Finish() {
        lock (_lock) {
            foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
                _file.Write(
                    $", {{ \"ph\": \"M\", \"name\": \"thread_name\"," +
                    $"\"pid\": 1, \"tid\": {thread.Id}," +
                    $"\"args\": {{ \"name\": \"Thread-{thread.Id}\"}}}}"
                );
            _file.Write("]}");
            _file.Close();
        }
    }
}