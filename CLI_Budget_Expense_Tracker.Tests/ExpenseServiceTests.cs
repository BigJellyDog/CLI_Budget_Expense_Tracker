namespace CLI_Budget_Expense_Tracker.Test;
using Services;



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
            new Expense(12.50m, Categories.Food));

        expenseService.AddExpense(
            new Expense(7.25m, Categories.Transportation));

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

        var beforePeriod = new Expense(10m, Categories.Food)
        {
            Date = new DateTime(2026, 10, 1)
        };

        var insidePeriod = new Expense(20m, Categories.Food)
        {
            Date = new DateTime(2026, 10, 5)
        };

        var afterPeriod = new Expense(30m, Categories.Food)
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

        var firstDayExpense = new Expense(10m, Categories.Food)
        {
            Date = new DateTime(2026, 10, 3, 0, 0, 0)
        };

        var lastDayExpense = new Expense(20m, Categories.Food)
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
}