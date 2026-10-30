namespace CLI_Budget_Expense_Tracker.Infrastructure.Services;

using Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Removes everything the person entered: expenses (also the soft-deleted ones), budgets, schedules,
/// rules and categories. The schema stays. Everything happens in one transaction.
/// </summary>
public sealed class DatabaseResetService(ExpenseTrackerDbContext context)
{
    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Children first: the foreign keys restrict deleting a category or a schedule that is in use.
        await context.Expenses.IgnoreQueryFilters().ExecuteDeleteAsync(cancellationToken);
        await context.BudgetLimits.ExecuteDeleteAsync(cancellationToken);
        await context.CategoryRules.ExecuteDeleteAsync(cancellationToken);
        await context.RecurringExpenses.ExecuteDeleteAsync(cancellationToken);
        await context.Categories.ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
