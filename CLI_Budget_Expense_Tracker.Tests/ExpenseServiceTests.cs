namespace CLI_Budget_Expense_Tracker.Test;
using Core.Services;
using Core.Models;



    /// <summary>
    /// Verifies expense total calculations.
    /// </summary>
    [TestFixture]
    public class ExpenseServiceTests
    {   
    /// <summary>
    /// Verifies that an empty expense collection has a zero total.
    /// </summary>
    [Test]
    public void GetTotalAmount_WhenNoExpenses_ReturnsZero()
    {
        // Arrange: create a service with no expenses.
        var expenseService = new ExpenseService();

        // Act: calculate the total.
        var total = expenseService.GetTotalAmount();

        // Assert: an empty collection should have a zero total.
        Assert.That(total, Is.EqualTo(0m));
    }

    /// <summary>
    /// Verifies that expenses from different categories are added together.
    /// </summary>
    [Test]
    public void GetTotalAmount_WithExpenses_ReturnsSum()
    {
        // Arrange: add expenses with known amounts.
        var expenseService = new ExpenseService();

        expenseService.AddExpense(
            new LegacyExpense(12.50m, LegacyCategories.Food));

        expenseService.AddExpense(
            new LegacyExpense(7.25m, LegacyCategories.Transportation));

        // Act: calculate the total across all categories.
        var total = expenseService.GetTotalAmount();

        // Assert: the total should match the expected sum.
        Assert.That(total, Is.EqualTo(19.75m));
    }
    /// <summary>
    /// Verifies that only expenses within the selected period are returned.
    /// </summary>
    [Test]
    public void GetExpensesByPeriod_WithExpensesOutsidePeriod_ReturnsOnlyMatchingExpenses()
    {
        // Arrange: create expenses before, inside, and after the selected period.
        var expenseService = new ExpenseService();

        var beforePeriod = new LegacyExpense(10m, LegacyCategories.Food)
        {
            Date = new DateTime(2026, 10, 1)
        };

        var insidePeriod = new LegacyExpense(20m, LegacyCategories.Food)
        {
            Date = new DateTime(2026, 10, 5)
        };

        var afterPeriod = new LegacyExpense(30m, LegacyCategories.Food)
        {
            Date = new DateTime(2026, 10, 10)
        };

        expenseService.AddExpense(beforePeriod);
        expenseService.AddExpense(insidePeriod);
        expenseService.AddExpense(afterPeriod);

        // Act: select expenses from October 3 through October 7.
        var result = expenseService.GetExpensesByPeriod(
            new DateTime(2026, 10, 3),
            new DateTime(2026, 10, 7));

        // Assert: only the expense inside the period should be returned.
        Assert.That(result, Is.EquivalentTo(new[] { insidePeriod }));
    }

    /// <summary>
    /// Verifies that both boundary dates are included regardless of expense time.
    /// </summary>
    [Test]
    public void GetExpensesByPeriod_WithExpensesOnBoundaryDates_IncludesBothExpenses()
    {
        // Arrange: add expenses at the beginning and end of the selected days.
        var expenseService = new ExpenseService();

        var firstDayExpense = new LegacyExpense(10m, LegacyCategories.Food)
        {
            Date = new DateTime(2026, 10, 3, 0, 0, 0)
        };

        var lastDayExpense = new LegacyExpense(20m, LegacyCategories.Food)
        {
            Date = new DateTime(2026, 10, 7, 23, 59, 59)
        };

        expenseService.AddExpense(firstDayExpense);
        expenseService.AddExpense(lastDayExpense);

        // Act: select the period using dates with midnight as their time.
        var result = expenseService.GetExpensesByPeriod(
            new DateTime(2026, 10, 3),
            new DateTime(2026, 10, 7));

        // Assert: both expenses should be included in the result.
        Assert.That(
            result,
            Is.EquivalentTo(new[] { firstDayExpense, lastDayExpense }));
    }
    /// <summary>
    /// Verifies that expenses are summed separately for each legacyCategory.
    /// </summary>
    [Test]
    public void GetTotalsByCategory_WithDifferentCategories_ReturnsSeparateSums()
    {
        // Arrange: add two food expenses and one transportation expense.
        var expenseService = new ExpenseService();
        var date = new DateTime(2026, 10, 10);

        expenseService.AddExpense(
            new LegacyExpense(12.50m, LegacyCategories.Food) { Date = date });

        expenseService.AddExpense(
            new LegacyExpense(7.25m, LegacyCategories.Food) { Date = date });

        expenseService.AddExpense(
            new LegacyExpense(5m, LegacyCategories.Transportation) { Date = date });

        // Act: calculate legacyCategory totals for October.
        var result = expenseService.GetTotalsByCategory(
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31));

        // Assert: each legacyCategory should contain its own total.
        var expected = new Dictionary<LegacyCategories, decimal>
        {
            [LegacyCategories.Food] = 19.75m,
            [LegacyCategories.Transportation] = 5m
        };

        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that expenses from other months or years are excluded.
    /// </summary>
    [Test]
    public void GetTotalsByCategory_WithExpensesOutsideSelectedMonth_ExcludesThem()
    {
        // Arrange: add one matching expense and expenses outside October 2026.
        var expenseService = new ExpenseService();

        expenseService.AddExpense(
            new LegacyExpense(20m, LegacyCategories.Food)
            {
                Date = new DateTime(2026, 10, 10)
            });

        expenseService.AddExpense(
            new LegacyExpense(100m, LegacyCategories.Food)
            {
                Date = new DateTime(2026, 9, 30)
            });

        expenseService.AddExpense(
            new LegacyExpense(100m, LegacyCategories.Food)
            {
                Date = new DateTime(2026, 11, 1)
            });

        expenseService.AddExpense(
            new LegacyExpense(100m, LegacyCategories.Transportation)
            {
                Date = new DateTime(2025, 10, 10)
            });

        // Act: calculate legacyCategory totals for October 2026 only.
        var result = expenseService.GetTotalsByCategory(
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31));

        // Assert: only the matching food expense should contribute.
        var expected = new Dictionary<LegacyCategories, decimal>
        {
            [LegacyCategories.Food] = 20m
        };

        Assert.That(result, Is.EqualTo(expected));
    }
    
}