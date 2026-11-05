namespace ExpenseTracker.App.Services;

using CLI_Budget_Expense_Tracker.Core.Interfaces;
using CLI_Budget_Expense_Tracker.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Initializes storage once per application run and allows retry after failure.
/// </summary>
public sealed class AppInitializer(
    IServiceScopeFactory scopeFactory,
    string dataDirectory) : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    /// <summary>Creates or migrates the database once; later calls wait for the first one and return.</summary>
    public async Task InitializeAsync()
    {
        await _gate.WaitAsync();

        try
        {
            if (_initialized) return;

            Directory.CreateDirectory(dataDirectory);

            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<DatabaseInitializer>()
                    .InitializeAsync();
            }

            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<StandardCategories>()
                    .SeedAsync();
            }

            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<IRecurringExpenseGenerator>()
                    .GenerateAsync();
            }

            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Releases the gate that serializes the initialization.</summary>
    public void Dispose() => _gate.Dispose();
}