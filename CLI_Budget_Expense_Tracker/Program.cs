namespace CLI_Budget_Expense_Tracker;

using Core.Interfaces;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UI;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var generateOnly = args is ["generate", ..];
        var pathArgs = generateOnly ? args[1..] : args;

        if (pathArgs is ["--help"] or ["-h"])
        {
            Console.WriteLine("""
                ExpenseTracker [--data-dir <directory>]
                ExpenseTracker generate [--data-dir <directory>]

                generate  Generate recurring expenses due through today, then exit.
                """);

            return 0;
        }

        try
        {
            var paths = AppPaths.FromArguments(pathArgs);
            Directory.CreateDirectory(paths.DataDirectory);

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = paths.DatabaseFile,
                ForeignKeys = true
            }.ToString();

            var services = new ServiceCollection();

            services.AddExpenseTrackerSqlite(connectionString);
            services.AddSingleton(paths);
            services.AddSingleton<TextReader>(Console.In);
            services.AddSingleton<TextWriter>(Console.Out);
            services.AddSingleton<ConsoleApplication>();

            await using var provider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true,
                    ValidateOnBuild = true
                });

            await using (var scope = provider.CreateAsyncScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<DatabaseInitializer>()
                    .InitializeAsync();
            }

            await using (var scope = provider.CreateAsyncScope())
            {
                var generated = await scope.ServiceProvider
                    .GetRequiredService<IRecurringExpenseGenerator>()
                    .GenerateAsync();

                if (generateOnly || generated > 0)
                {
                    Console.WriteLine(
                        $"Generated {generated} recurring expense(s).");
                }
            }

            if (generateOnly)
            {
                return 0;
            }

            Console.WriteLine($"Data directory: {paths.DataDirectory}");

            await provider
                .GetRequiredService<ConsoleApplication>()
                .RunAsync();

            return 0;
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException
                or IOException or UnauthorizedAccessException
                or SqliteException or DbUpdateException)
        {
            Console.Error.WriteLine(
                $"Application error: {exception.Message}");

            return 1;
        }
    }
}