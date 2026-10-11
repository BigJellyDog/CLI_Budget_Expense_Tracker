namespace CLI_Budget_Expense_Tracker.Infrastructure.Repositories;

using Core.Interfaces;
using Core.Models;
using Data;
using Microsoft.EntityFrameworkCore;

public sealed class SqliteBudgetRepository(ExpenseTrackerDbContext context) : IBudgetRepository
{
    public Task<BudgetLimit?> GetAsync(Guid categoryId, DateOnly month, CancellationToken cancellationToken = default) =>
        context.BudgetLimits.SingleOrDefaultAsync(x => x.CategoryId == categoryId && x.Month == month, cancellationToken);

    public async Task<IReadOnlyList<BudgetLimit>> GetForMonthAsync(DateOnly month, CancellationToken cancellationToken = default) =>
        await context.BudgetLimits.AsNoTracking().Where(x => x.Month == month).ToListAsync(cancellationToken);

    public async Task AddAsync(BudgetLimit limit, CancellationToken cancellationToken = default)
    {
        context.BudgetLimits.Add(limit);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BudgetLimit limit, CancellationToken cancellationToken = default)
    {
        context.BudgetLimits.Update(limit);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid categoryId, DateOnly month, CancellationToken cancellationToken = default)
    {
        var limit = await GetAsync(categoryId, month, cancellationToken);
        if (limit is null) return false;
        context.BudgetLimits.Remove(limit);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
