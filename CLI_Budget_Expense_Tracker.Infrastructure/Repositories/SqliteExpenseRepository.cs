namespace CLI_Budget_Expense_Tracker.Infrastructure.Repositories;

using Core.Interfaces;
using Core.Models;
using Data;
using Microsoft.EntityFrameworkCore;

public sealed class SqliteExpenseRepository(ExpenseTrackerDbContext context) : IExpenseRepository
{
    
    public Task<Expense?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        context.Expenses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

    public async Task<bool> UpdateAsync(
        Expense original,
        Expense updated,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(updated);

        if (original.Id != updated.Id
            || original.RecurringExpenseId != updated.RecurringExpenseId)
        {
            throw new ArgumentException(
                "An update must preserve the expense identity.",
                nameof(updated));
        }

        // Compare the original values and update the expense atomically.
        var affected = await context.Expenses
            .Where(x =>
                x.Id == original.Id
                && !x.IsDeleted
                && x.Amount == original.Amount
                && x.CategoryId == original.CategoryId
                && x.Date == original.Date
                && x.Description == original.Description
                && x.RecurringExpenseId == original.RecurringExpenseId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Amount, updated.Amount)
                    .SetProperty(x => x.CategoryId, updated.CategoryId)
                    .SetProperty(x => x.Date, updated.Date)
                    .SetProperty(x => x.Description, updated.Description),
                cancellationToken);

        return affected == 1;
    }
    
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

    public async Task AddRangeAsync(
        IReadOnlyCollection<Expense> expenses,
        CancellationToken cancellationToken = default)
    {
        context.Expenses.AddRange(expenses);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<string>> GetExistingImportHashesAsync(
        IReadOnlyCollection<string> hashes,
        CancellationToken cancellationToken = default)
    {
        var found = new HashSet<string>();

        // Chunked to stay below SQLite's parameter limit.
        foreach (var chunk in hashes.Distinct().Chunk(500))
        {
            found.UnionWith(await context.Expenses
                .IgnoreQueryFilters()
                .Where(x => x.ImportHash != null && chunk.Contains(x.ImportHash))
                .Select(x => x.ImportHash!)
                .ToListAsync(cancellationToken));

            // Permanently deleted expenses leave their hash behind.
            found.UnionWith(await context.RemovedImportHashes
                .Where(x => chunk.Contains(x.Hash))
                .Select(x => x.Hash)
                .ToListAsync(cancellationToken));
        }

        return found;
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
