namespace CLI_Budget_Expense_Tracker.Test;
using Services;




/// <summary>
/// Verifies monthly budget overspend calculations.
/// </summary>
[TestFixture]
public class BudgetServiceTests
{
    /// <summary>
    /// Verifies overspend below, at, and above the monthly limit.
    /// </summary>
    /// <param name="spentAmount">The amount spent during the month.</param>
    /// <param name="expectedOverspend">The expected amount above the limit.</param>
    [TestCase(60, 0)]
    [TestCase(100, 0)]
    [TestCase(125, 25)]
    public void GetMonthlyOverspend_WithConfiguredLimit_ReturnsExpectedAmount(
        decimal spentAmount,
        decimal expectedOverspend)
    {
        // Arrange: configure a limit and create an expense in the selected month.
        var budgetService = new BudgetService();
        var date = new DateTime(2026, 10, 10);

        budgetService.SetMonthlyLimit(Categories.Food, 100m);

        IReadOnlyList<Expense> expenses =
        [
            new Expense(spentAmount, Categories.Food) { Date = date }
        ];

        // Act: calculate how much spending exceeds the limit.
        var result = budgetService.GetMonthlyOverspend(
            Categories.Food,
            date,
            expenses);

        // Assert: spending below or at the limit should produce no overspend.
        Assert.That(result, Is.EqualTo(expectedOverspend));
    }

    /// <summary>
    /// Verifies that a category without a configured limit has no overspend.
    /// </summary>
    [Test]
    public void GetMonthlyOverspend_WithoutConfiguredLimit_ReturnsZero()
    {
        // Arrange: create an expense without configuring a budget limit.
        var budgetService = new BudgetService();
        var date = new DateTime(2026, 10, 10);

        IReadOnlyList<Expense> expenses =
        [
            new Expense(500m, Categories.Food) { Date = date }
        ];

        // Act: check spending for the category without a limit.
        var result = budgetService.GetMonthlyOverspend(
            Categories.Food,
            date,
            expenses);

        // Assert: an absent limit means spending is unrestricted.
        Assert.That(result, Is.EqualTo(0m));
    }
}