namespace CLI_Budget_Expense_Tracker.Test;

using Infrastructure;
using Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "expense-tests-" + Guid.NewGuid().ToString("N"));
    public ServiceProvider Services { get; }
    public string ConnectionString { get; }

    public SqliteTestDatabase()
    {
        Directory.CreateDirectory(DirectoryPath);
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(DirectoryPath, "test.db"), ForeignKeys = true, Pooling = false
        }.ToString();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider());
        services.AddExpenseTrackerSqlite(ConnectionString);
        Services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        Directory.Delete(DirectoryPath, recursive: true);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
