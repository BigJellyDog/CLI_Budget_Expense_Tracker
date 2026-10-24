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

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(
                dataDirectory, "expense-tracker.db"),
            ForeignKeys = true
        }.ToString();

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
        builder.Services.AddTransient<BankImportViewModel>();
        builder.Services.AddTransient<BankImportPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}