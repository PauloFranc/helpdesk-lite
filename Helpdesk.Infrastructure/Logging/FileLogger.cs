using Helpdesk.Application.Abstractions;

namespace Helpdesk.Infrastructure.Logging;

public sealed class FileLogger : IAppLogger, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _sync = new();

    public FileLogger(string path)
    {
        var stream = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(stream) { AutoFlush = true };
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        var line = $"{DateTimeOffset.UtcNow:o} {level} {message}";
        lock (_sync)
        {
            _writer.WriteLine(line);
        }
    }

    public void Dispose() => _writer.Dispose();
}

