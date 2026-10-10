namespace CLI_Budget_Expense_Tracker.Test;

using Core.Models;
using System.Text.Json;

public class LegacyJsonCompatibilityTests
{
    /// <summary>
    /// Verifies that renamed models still read categories from v1 JSON.
    /// </summary>
    [Test]
    public void DeserializeV1Json_PreservesExpenseAndBudgetCategories()
    {
        const string expenseJson = """
                                   {
                                       "Amount": 12.50,
                                       "Category": 2,
                                       "Date": "2026-10-09T12:00:00",
                                       "Description": "Lunch"
                                   }
                                   """;

        const string budgetJson = """
                                  {
                                      "Category": 3,
                                      "MonthlyAmount": 100
                                  }
                                  """;

        var expense = JsonSerializer.Deserialize<LegacyExpense>(expenseJson);
        var budget = JsonSerializer.Deserialize<LegacyBudgetLimit>(budgetJson);

        Assert.Multiple(() =>
        {
            Assert.That(
                expense?.LegacyCategory,
                Is.EqualTo(LegacyCategories.Food));

            Assert.That(
                budget?.LegacyCategory,
                Is.EqualTo(LegacyCategories.Transportation));
        });
    }
}