namespace CLI_Budget_Expense_Tracker.Infrastructure.Repositories;

using Core.Interfaces;
using Core.Models;
using Data;
using Microsoft.EntityFrameworkCore;

public sealed class SqliteExpenseRepository(ExpenseTrackerDbContext context) : IExpenseRepository
{
    public async Task<IReadOnlyList<Expense>> GetAsync(
        DateOnly? start = null, DateOnly? end = null, Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Expenses.AsNoTracking();
        if (start.HasValue) query = query.Where(x => x.Date >= start.Value);
        if (end.HasValue) query = query.Where(x => x.Date <= end.Value);
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
        return await query.OrderByDescending(x => x.Date).ThenBy(x => x.Id).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        context.Expenses.Add(expense);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await context.Expenses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (expense is null) return false;
        expense.MarkAsDeleted();
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
