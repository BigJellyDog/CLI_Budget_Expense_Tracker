namespace CLI_Budget_Expense_Tracker.Infrastructure;

using Backup;
using Importing;
using Microsoft.Data.Sqlite;
using Services;
using Data;
using Core.Interfaces;
using Core.Services;
using Exporting;
using Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddScoped<
            IRecurringExpenseRepository,
            SqliteRecurringExpenseRepository>();
        services.AddScoped<
            IRecurringExpenseGenerator,
            SqliteRecurringExpenseGenerator>();
        
        services.AddSingleton<IBankStatementClassifier, RevolutStatementClassifier>();
        services.AddSingleton<IBankStatementReader, RevolutCsvReader>();
        services.AddSingleton<GenericCsvReader>();
        services.AddSingleton<GenericStatementClassifier>();
        services.AddScoped<RecurringExpenseService>();
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<DatabaseResetService>();
        services.AddSingleton(provider =>
        {
            var databasePath = Path.GetFullPath(
                new SqliteConnectionStringBuilder(connectionString).DataSource);

            return new DatabaseBackupService(
                databasePath,
                Path.Combine(Path.GetDirectoryName(databasePath)!, "backups"),
                provider.GetRequiredService<TimeProvider>());
        });
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICategoryRepository, SqliteCategoryRepository>();
        services.AddScoped<IExpenseRepository, SqliteExpenseRepository>();
        services.AddScoped<IBudgetRepository, SqliteBudgetRepository>();
        services.AddScoped<ICategoryRuleRepository, SqliteCategoryRuleRepository>();
        services.AddScoped<CategoryRuleService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<BudgetService>();
        services.AddScoped<BankImportService>();
        services.AddScoped<ReportService>();
        services.AddSingleton<CsvExpenseExporter>();
        

        return services;
    }
}
