namespace CLI_Budget_Expense_Tracker.Test;
using Core.Services;
using Core.Models;



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
    /// <summary>
    /// Verifies that a negative limit is rejected without replacing the existing limit.
    /// </summary>
    /// <param name="monthlyAmount">The invalid negative limit to test.</param>
    [TestCase(-1)]
    [TestCase(-100)]
    public void SetMonthlyLimit_WithNegativeAmount_ThrowsAndPreservesExistingLimit(
        decimal monthlyAmount)
    {
        // Arrange: configure a valid limit before attempting to replace it.
        var budgetService = new BudgetService();
        budgetService.SetMonthlyLimit(Categories.Food, 100m);

        // Act and assert: the invalid replacement should throw an exception.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => budgetService.SetMonthlyLimit(Categories.Food, monthlyAmount));

        // The rejected update must not change the previously configured limit.
        var limit = budgetService.GetMonthlyLimit(Categories.Food);

        Assert.That(limit?.MonthlyAmount, Is.EqualTo(100m));
    }

    /// <summary>
    /// Verifies that zero is accepted as a valid monthly budget limit.
    /// </summary>
    [Test]
    public void SetMonthlyLimit_WithZeroAmount_StoresZeroLimit()
    {
        // Arrange: create a service without configured limits.
        var budgetService = new BudgetService();

        // Act: configure a zero limit.
        budgetService.SetMonthlyLimit(Categories.Food, 0m);

        // Assert: the limit should exist and contain zero.
        var limit = budgetService.GetMonthlyLimit(Categories.Food);

        Assert.That(limit?.MonthlyAmount, Is.EqualTo(0m));
    }
}