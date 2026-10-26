namespace CLI_Budget_Expense_Tracker.Core.Services;

using Models;

/// <summary>Finds the rule that fits a bank record description.</summary>
public static class CategoryRuleMatcher
{
    /// <summary>
    /// Returns the matching rule, ignoring case. When several rules match, the most
    /// specific one wins: the longest pattern, then alphabetical order.
    /// </summary>
    public static CategoryRule? FindMatch(IEnumerable<CategoryRule> rules, string description)
    {
        ArgumentNullException.ThrowIfNull(rules);

        if (string.IsNullOrWhiteSpace(description)) return null;

        return rules
            .Where(x => description.Contains(x.Pattern, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Pattern.Length)
            .ThenBy(x => x.NormalizedPattern, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
