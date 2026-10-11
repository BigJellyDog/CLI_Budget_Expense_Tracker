namespace CLI_Budget_Expense_Tracker.Infrastructure;

using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers SQLite persistence services.
/// </summary>
public static class SqliteServiceCollectionExtensions
{
    public static IServiceCollection AddExpenseTrackerSqlite(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<ExpenseTrackerDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}