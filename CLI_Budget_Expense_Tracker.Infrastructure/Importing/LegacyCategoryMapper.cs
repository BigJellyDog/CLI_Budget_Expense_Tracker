namespace CLI_Budget_Expense_Tracker.Infrastructure.Importing;

using Core.Models;

/// <summary>
/// Maps legacy category values to explicit category names.
/// </summary>
public static class LegacyCategoryMapper
{
    public static string GetName(LegacyCategories category)
    {
        return category switch
        {
            LegacyCategories.Test => "Test",
            LegacyCategories.Housing => "Housing",
            LegacyCategories.Food => "Food",
            LegacyCategories.Transportation => "Transportation",
            LegacyCategories.Health => "Health",
            LegacyCategories.Clothing => "Clothing",
            LegacyCategories.Entertainment => "Entertainment",
            LegacyCategories.Education => "Education",
            LegacyCategories.Savings => "Savings",
            LegacyCategories.Gifts => "Gifts",
            LegacyCategories.Utilities => "Utilities",
            LegacyCategories.Pets => "Pets",
            LegacyCategories.PersonalCare => "PersonalCare",
            LegacyCategories.Travel => "Travel",
            LegacyCategories.Miscellaneous => "Miscellaneous",

            _ => throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unknown legacy category.")
        };
    }
}