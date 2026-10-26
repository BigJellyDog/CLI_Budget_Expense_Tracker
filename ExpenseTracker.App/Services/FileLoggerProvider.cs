namespace ExpenseTracker.App.Services;

using Microsoft.Extensions.Logging;

/// <summary>Folder where daily log files are written, next to the database.</summary>
public sealed record LogLocation(string Directory);

/// <summary>Writes log entries to one text file per day and removes old files.</summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private const int KeepDays = 14;

    private readonly string _directory;
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();

    public FileLoggerProvider(string directory, TimeProvider timeProvider)
    {
        _directory = directory;
        _timeProvider = timeProvider;

        try
        {
            Directory.CreateDirectory(directory);
            DeleteOldLogs();
        }
        catch
        {
            // Logging must never stop the app from starting.
        }
    }

    public ILogger CreateLogger(string categoryName) =>
        new FileLogger(this, categoryName);

    public void Dispose()
    {
    }

    private void Write(
        LogLevel level,
        string category,
        string message,
        Exception? exception)
    {
        try
        {
            var now = _timeProvider.GetLocalNow();
            var path = Path.Combine(_directory, $"app-{now:yyyyMMdd}.log");

            var entry =
                $"{now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {category}: {message}{Environment.NewLine}"
                + (exception is null ? "" : exception + Environment.NewLine);

            lock (_gate)
            {
                File.AppendAllText(path, entry);
            }
        }
        catch
        {
            // A failing log write must not crash the app.
        }
    }

    private void DeleteOldLogs()
    {
        var limit = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-KeepDays);

        foreach (var file in new DirectoryInfo(_directory).EnumerateFiles("app-*.log"))
        {
            if (file.LastWriteTimeUtc < limit) file.Delete();
        }
    }

    private sealed class FileLogger(FileLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            provider.Write(logLevel, category, formatter(state, exception), exception);
        }
    }
}
