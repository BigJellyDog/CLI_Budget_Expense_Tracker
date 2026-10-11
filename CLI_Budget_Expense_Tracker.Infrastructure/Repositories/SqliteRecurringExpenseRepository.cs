namespace CLI_Budget_Expense_Tracker.Infrastructure.Repositories;

using Core.Interfaces;
using Core.Models;
using Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Stores and retrieves recurring expense schedules using SQLite.
/// </summary>
public sealed class SqliteRecurringExpenseRepository(
    ExpenseTrackerDbContext context)
    : IRecurringExpenseRepository
{
    public Task<RecurringExpense?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return context.RecurringExpenses.SingleOrDefaultAsync(
            x => x.Id == id,
            cancellationToken);
    }

    public async Task<IReadOnlyList<RecurringExpense>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.RecurringExpenses
            .AsNoTracking()
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.NextDueDate)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecurringExpense>> GetDueAsync(
        DateOnly throughDate,
        CancellationToken cancellationToken = default)
    {
        return await context.RecurringExpenses
            .Where(x => x.IsActive && x.NextDueDate <= throughDate)
            .OrderBy(x => x.NextDueDate)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        RecurringExpense recurringExpense,
        CancellationToken cancellationToken = default)
    {
        context.RecurringExpenses.Add(recurringExpense);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        RecurringExpense recurringExpense,
        CancellationToken cancellationToken = default)
    {
        context.RecurringExpenses.Update(recurringExpense);
        await context.SaveChangesAsync(cancellationToken);
    }
}