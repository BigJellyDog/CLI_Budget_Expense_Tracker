namespace CLI_Budget_Expense_Tracker.Core.Services;

using Interfaces;
using Models;

/// <summary>Manages the rules that pick a category for imported bank records.</summary>
public sealed class CategoryRuleService(
    ICategoryRuleRepository rules,
    ICategoryRepository categories)
{
    /// <summary>All rules, ordered by pattern.</summary>
    public Task<IReadOnlyList<CategoryRule>> GetAllAsync(CancellationToken cancellationToken = default) =>
        rules.GetAllAsync(cancellationToken);

    /// <summary>Adds a rule for an existing category; the same pattern cannot be used twice.</summary>
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

    /// <summary>Deletes a rule; false when it does not exist.</summary>
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        rules.DeleteAsync(id, cancellationToken);
}
