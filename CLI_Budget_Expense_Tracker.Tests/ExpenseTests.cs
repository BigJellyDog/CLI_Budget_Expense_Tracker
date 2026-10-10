namespace CLI_Budget_Expense_Tracker.Test;

using Core.Models;

public class ExpenseTests
{
    private static readonly decimal[] ValidAmounts =
    [
        0.01m,
        12.50m,
        12.500m,
        92_233_720_368_547_758.07m
    ];

    private static readonly decimal[] InvalidAmounts =
    [
        0m,
        -0.01m,
        12.345m,
        92_233_720_368_547_758.08m,
        decimal.MaxValue
    ];

    [TestCaseSource(nameof(ValidAmounts))]
    public void Constructor_WithValidAmount_PreservesProvidedData(decimal amount)
    {
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 31);

        var expense = new Expense(
            id, amount, categoryId, date, "Historical expense");

        Assert.Multiple(() =>
        {
            Assert.That(expense.Id, Is.EqualTo(id));
            Assert.That(expense.Amount, Is.EqualTo(amount));
            Assert.That(expense.CategoryId, Is.EqualTo(categoryId));
            Assert.That(expense.Date, Is.EqualTo(date));
            Assert.That(expense.Description, Is.EqualTo("Historical expense"));
            Assert.That(expense.Source, Is.EqualTo(ExpenseSource.Manual));
            Assert.That(expense.IsDeleted, Is.False);
        });
    }

    [TestCaseSource(nameof(InvalidAmounts))]
    public void Constructor_WithInvalidAmount_Throws(decimal amount)
    {
        Assert.That(
            () => new Expense(
                Guid.NewGuid(),
                amount,
                Guid.NewGuid(),
                new DateOnly(2026, 1, 31)),
            Throws.InstanceOf<ArgumentException>());
    }
}