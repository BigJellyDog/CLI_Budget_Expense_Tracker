namespace CLI_Budget_Expense_Tracker.Infrastructure.Repositories;

using Core.Interfaces;
using Core.Models;
using Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public sealed class SqliteCategoryRuleRepository(ExpenseTrackerDbContext context) : ICategoryRuleRepository
{
    public async Task<IReadOnlyList<CategoryRule>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.CategoryRules.AsNoTracking().OrderBy(x => x.NormalizedPattern).ToListAsync(cancellationToken);

    public Task<CategoryRule?> GetByNormalizedPatternAsync(string normalizedPattern, CancellationToken cancellationToken = default) =>
        context.CategoryRules.AsNoTracking().SingleOrDefaultAsync(x => x.NormalizedPattern == normalizedPattern, cancellationToken);

    public async Task AddAsync(CategoryRule rule, CancellationToken cancellationToken = default)
    {
        context.CategoryRules.Add(rule);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        {
            context.Entry(rule).State = EntityState.Detached;
            throw new InvalidOperationException("A rule with this text already exists.", exception);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.CategoryRules.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
}
