namespace CLI_Budget_Expense_Tracker.Core.Models;
using System.Text.Json.Serialization;


public class LegacyBudgetLimit(LegacyCategories legacyCategory, decimal monthlyAmount)
{
    // Preserve the property name used by v1 JSON files.
    [JsonPropertyName("Category")]
    public LegacyCategories LegacyCategory { get; } = legacyCategory;

    public decimal MonthlyAmount { get; } = monthlyAmount;
}