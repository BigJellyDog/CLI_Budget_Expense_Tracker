namespace CLI_Budget_Expense_Tracker.Core.Services;

using Interfaces;
using Models;

/// <summary>Manages the rules that pick a category for imported bank records.</summary>
public sealed class CategoryRuleService(
    ICategoryRuleRepository rules,
    ICategoryRepository categories)
{
    public Task<IReadOnlyList<CategoryRule>> GetAllAsync(CancellationToken cancellationToken = default) =>
        rules.GetAllAsync(cancellationToken);

    public async Task<CategoryRule> AddAsync(string pattern, Guid categoryId, CancellationToken cancellationToken = default)
    {
        var rule = new CategoryRule(Guid.NewGuid(), pattern, categoryId);

        if (await categories.GetByIdAsync(categoryId, cancellationToken) is null)
            throw new InvalidOperationException("Category not found.");

        var existing = await rules.GetByNormalizedPatternAsync(rule.NormalizedPattern, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException("A rule with this text already exists.");

        await rules.AddAsync(rule, cancellationToken);
        return rule;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        rules.DeleteAsync(id, cancellationToken);
}
