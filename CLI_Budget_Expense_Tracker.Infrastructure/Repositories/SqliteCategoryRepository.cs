namespace CLI_Budget_Expense_Tracker.Infrastructure.Repositories;

using Core.Interfaces;
using Core.Models;
using Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public sealed class SqliteCategoryRepository(ExpenseTrackerDbContext context) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Categories.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Category?> GetByNormalizedNameAsync(string name, CancellationToken cancellationToken = default) =>
        context.Categories.AsNoTracking().SingleOrDefaultAsync(x => x.NormalizedName == name, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Categories.AsNoTracking().OrderBy(x => x.NormalizedName).ToListAsync(cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        context.Categories.Add(category);
        await SaveCategoryAsync(category, cancellationToken);
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        context.Categories.Update(category);
        await SaveCategoryAsync(category, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetByIdAsync(id, cancellationToken);
        if (category is null) return false;
        // Deleted expenses still reference their category and retain their history.
        if (await context.Expenses.IgnoreQueryFilters().AnyAsync(x => x.CategoryId == id, cancellationToken)
            || await context.BudgetLimits.AnyAsync(x => x.CategoryId == id, cancellationToken))
            throw new InvalidOperationException("This category is used by expenses or budget limits and cannot be deleted.");

        context.Categories.Remove(category);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException { SqliteExtendedErrorCode: 787 })
        {
            context.Entry(category).State = EntityState.Detached;
            throw new InvalidOperationException("This category is used by another record and cannot be deleted.", exception);
        }
    }

    public async Task<CategoryUsage> GetUsageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expenses = context.Expenses.IgnoreQueryFilters().Where(x => x.CategoryId == id);

        return new CategoryUsage(
            await expenses.CountAsync(x => !x.IsDeleted, cancellationToken),
            await expenses.CountAsync(x => x.IsDeleted, cancellationToken),
            await context.RecurringExpenses.CountAsync(x => x.CategoryId == id, cancellationToken),
            await context.BudgetLimits.CountAsync(x => x.CategoryId == id, cancellationToken));
    }

    public async Task<CategoryUsage> MoveAndDeleteAsync(Guid id, Guid targetId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var usage = await GetUsageAsync(id, cancellationToken);

        // Deleted expenses are moved too, so no record keeps pointing at the old category.
        await context.Expenses.IgnoreQueryFilters()
            .Where(x => x.CategoryId == id)
            .ExecuteUpdateAsync(x => x.SetProperty(e => e.CategoryId, targetId), cancellationToken);

        await context.RecurringExpenses
            .Where(x => x.CategoryId == id)
            .ExecuteUpdateAsync(x => x.SetProperty(e => e.CategoryId, targetId), cancellationToken);

        // The target may already have a limit for the same month, so the old limits are dropped.
        await context.BudgetLimits
            .Where(x => x.CategoryId == id)
            .ExecuteDeleteAsync(cancellationToken);

        await context.Categories
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return usage;
    }

    private async Task SaveCategoryAsync(Category category, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        {
            context.Entry(category).State = EntityState.Detached;
            throw new InvalidOperationException("A category with this name already exists.", exception);
        }
    }
}
