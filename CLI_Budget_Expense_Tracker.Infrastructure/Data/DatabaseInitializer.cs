namespace CLI_Budget_Expense_Tracker.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Initializes the database by applying pending migrations.
/// </summary>
public sealed class DatabaseInitializer(
    ExpenseTrackerDbContext context)
{
    public Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        return context.Database.MigrateAsync(cancellationToken);
    }
}