using System.Runtime.InteropServices.JavaScript;

namespace ExpenseTracker.App;

using CLI_Budget_Expense_Tracker.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services;
using ViewModels;
using Views;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont(
                "OpenSans-Regular.ttf", "OpenSansRegular");

            fonts.AddFont(
                "OpenSans-Semibold.ttf", "OpenSansSemibold");
        });

        var dataDirectory = FileSystem.Current.AppDataDirectory;

        var databasePath = ResolveDatabasePath(dataDirectory);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true
        }.ToString();

        builder.Services.AddSingleton(new AppSettings(
            Path.Combine(Path.GetDirectoryName(databasePath)!, "settings.json")));

        builder.Services.AddSingleton(new ExportLocation(
            Path.Combine(Path.GetDirectoryName(databasePath)!, "exports")));

        builder.Services.AddExpenseTrackerSqlite(connectionString);

        builder.Services.AddSingleton(services => new AppInitializer(
            services.GetRequiredService<IServiceScopeFactory>(),
            dataDirectory));

        builder.Services.AddTransient<ExpensesViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ExpenseEditorViewModel>();
        builder.Services.AddTransient<ExpenseEditorPage>();
        builder.Services.AddTransient<CategoriesViewModel>();
        builder.Services.AddTransient<CategoriesPage>();
        builder.Services.AddTransient<BudgetsViewModel>();
        builder.Services.AddTransient<BudgetsPage>();
        builder.Services.AddTransient<RecurringExpensesViewModel>();
        builder.Services.AddTransient<RecurringExpensesPage>();
        builder.Services.AddTransient<ReportsViewModel>();
        builder.Services.AddTransient<ReportsPage>();
        builder.Services.AddTransient<BankImportViewModel>();
        builder.Services.AddTransient<BankImportPage>();
        builder.Services.AddTransient<RulesViewModel>();
        builder.Services.AddTransient<RulesPage>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var logDirectory = Path.Combine(
            Path.GetDirectoryName(databasePath)!, "logs");

        builder.Services.AddSingleton(new LogLocation(logDirectory));
        builder.Logging.AddProvider(
            new FileLoggerProvider(logDirectory, TimeProvider.System));

        // Every SQL command would otherwise bury the real problems in the log file.
        builder.Logging.AddFilter<FileLoggerProvider>(
            "Microsoft.EntityFrameworkCore", LogLevel.Warning);

        var app = builder.Build();

        GlobalErrorHandler.Register(app.Services);

        return app;
    }

    private static string ResolveDatabasePath(string dataDirectory)
    {
        const string fileName = "expense-tracker.db";

        // Keep the database next to the solution file so code and data live in one folder.
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (dir.GetFiles("*.sln*").Length > 0)
            {
                return Path.Combine(dir.FullName, fileName);
            }
        }

        return Path.Combine(dataDirectory, fileName);
    }
}