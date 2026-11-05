namespace CLI_Budget_Expense_Tracker.Infrastructure.Repositories;

using Core.Interfaces;
using Core.Models;
using Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public sealed class SqliteCategoryRuleRepository(ExpenseTrackerDbContext context) : ICategoryRuleRepository
{
    /// <summary>All rules, ordered by pattern.</summary>
    public async Task<IReadOnlyList<CategoryRule>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.CategoryRules.AsNoTracking().OrderBy(x => x.NormalizedPattern).ToListAsync(cancellationToken);

    /// <summary>The rule with the normalized pattern, or null.</summary>
    public Task<CategoryRule?> GetByNormalizedPatternAsync(string normalizedPattern, CancellationToken cancellationToken = default) =>
        context.CategoryRules.AsNoTracking().SingleOrDefaultAsync(x => x.NormalizedPattern == normalizedPattern, cancellationToken);

    /// <summary>Saves a new rule; a duplicate pattern is reported as an error.</summary>
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

    /// <summary>Deletes a rule; false when it does not exist.</summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.CategoryRules.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
}
