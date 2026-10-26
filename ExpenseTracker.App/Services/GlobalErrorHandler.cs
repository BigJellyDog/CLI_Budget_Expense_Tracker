namespace ExpenseTracker.App.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Logs errors nobody caught and shows a message instead of letting the app crash.
/// </summary>
public static class GlobalErrorHandler
{
    private static ILogger? _logger;
    private static string _logDirectory = "";
    private static int _alertOpen;

    public static void Register(IServiceProvider services)
    {
        _logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Unhandled");
        _logDirectory = services.GetRequiredService<LogLocation>().Directory;

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            _logger.LogCritical(
                e.ExceptionObject as Exception,
                "Unhandled exception (terminating: {IsTerminating}).",
                e.IsTerminating);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _logger.LogError(e.Exception, "Unobserved task exception.");
            e.SetObserved();
        };
    }

    /// <summary>Logs an exception from the UI thread and tells the user where the log is.</summary>
    public static void HandleUiException(Exception exception)
    {
        _logger?.LogError(exception, "Unhandled UI exception.");

        // One message at a time, so a repeating error cannot flood the screen.
        if (Interlocked.Exchange(ref _alertOpen, 1) == 1) return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var page = Application.Current?.Windows.FirstOrDefault()?.Page;

                if (page is not null)
                {
                    await page.DisplayAlertAsync(
                        "Something went wrong",
                        $"The error was written to the log:\n{_logDirectory}",
                        "OK");
                }
            }
            catch
            {
                // Showing the message is best effort.
            }
            finally
            {
                Interlocked.Exchange(ref _alertOpen, 0);
            }
        });
    }
}
