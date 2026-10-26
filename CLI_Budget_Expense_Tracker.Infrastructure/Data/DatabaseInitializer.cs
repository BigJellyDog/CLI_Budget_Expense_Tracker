namespace CLI_Budget_Expense_Tracker.Infrastructure.Data;

using Backup;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Initializes the database by applying pending migrations.
/// An existing database is backed up first, so a failed migration cannot lose data.
/// </summary>
public sealed class DatabaseInitializer(
    ExpenseTrackerDbContext context,
    DatabaseBackupService backups)
{
    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        var pending = await context.Database
            .GetPendingMigrationsAsync(cancellationToken);

        if (pending.Any()
            && (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).Any())
        {
            backups.CreateBackup(DatabaseBackupService.AutomaticPrefix);
        }

        await context.Database.MigrateAsync(cancellationToken);
    }
}
