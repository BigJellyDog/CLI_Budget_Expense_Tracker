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
}