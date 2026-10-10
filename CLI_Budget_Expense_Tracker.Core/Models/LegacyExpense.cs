namespace CLI_Budget_Expense_Tracker.Core.Models;
using System.Text.Json.Serialization;


public class LegacyExpense(
    decimal amount, 
    LegacyCategories legacyCategory,
    string description = "")
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal Amount { get; set; } = amount;
    // Preserve the property name used by v1 JSON files.
    [JsonPropertyName("Category")]
    public LegacyCategories LegacyCategory { get; set; } = legacyCategory;
    public DateTime Date { get; set; } = DateTime.Now;
    public string Description { get; set; } = description;
    
}
