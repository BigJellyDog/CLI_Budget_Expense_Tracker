namespace ExpenseTracker.App.Services;

using CLI_Budget_Expense_Tracker.Infrastructure.Backup;
using CLI_Budget_Expense_Tracker.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Clears everything the person entered, then starts over with the standard categories, as on a first start.
/// A backup is made first, so it can be undone with Restore.
/// </summary>
public sealed class ClearDataService(
    IServiceScopeFactory scopeFactory,
    AppInitializer initializer,
    DatabaseBackupService backups,
    StandardCategories standardCategories,
    AppSettings settings)
{
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await initializer.InitializeAsync();

        backups.CreateBackup(DatabaseBackupService.AutomaticPrefix);

        await Task.Run(async () =>
        {
            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<DatabaseResetService>()
                    .ClearAllAsync(cancellationToken);
            }

            // The colors belonged to the categories that are gone.
            settings.ForgetCategories();

            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                await standardCategories.SeedAsync();
            }
        }, cancellationToken);
    }
}
