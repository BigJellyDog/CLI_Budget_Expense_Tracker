namespace CLI_Budget_Expense_Tracker.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Creates the database context for EF Core migration tools.
/// </summary>
public sealed class ExpenseTrackerDbContextFactory
    : IDesignTimeDbContextFactory<ExpenseTrackerDbContext>
{
    /// <summary>
    /// Configures SQLite for schema generation.
    /// Use --connection when applying migrations to a database file.
    /// </summary>
    public ExpenseTrackerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ExpenseTrackerDbContext>()
            .UseSqlite("Data Source=:memory:;Foreign Keys=True")
            .Options;

        return new ExpenseTrackerDbContext(options);
    }
}