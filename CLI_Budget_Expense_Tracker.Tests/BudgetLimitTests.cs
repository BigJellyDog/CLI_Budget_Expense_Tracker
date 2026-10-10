namespace CLI_Budget_Expense_Tracker.Test;

using Core.Models;

public class BudgetLimitTests
{
    private static readonly decimal[] ValidAmounts =
    [
        0m,
        0.01m,
        12.500m,
        92_233_720_368_547_758.07m
    ];

    private static readonly decimal[] InvalidAmounts =
    [
        -0.01m,
        12.345m,
        92_233_720_368_547_758.08m,
        decimal.MaxValue
    ];

    [TestCase(2026, 10, 10)]
    [TestCase(2024, 2, 29)]
    [TestCase(2026, 12, 31)]
    public void Constructor_NormalizesMonthToFirstDay(
        int year, int month, int day)
    {
        var limit = new BudgetLimit(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(year, month, day),
            100m);

        Assert.That(limit.Month, Is.EqualTo(new DateOnly(year, month, 1)));
    }

    [TestCaseSource(nameof(ValidAmounts))]
    public void Constructor_WithValidAmount_PreservesAmount(decimal amount)
    {
        var limit = new BudgetLimit(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 10, 1),
            amount);

        Assert.That(limit.MonthlyAmount, Is.EqualTo(amount));
    }

    [TestCaseSource(nameof(InvalidAmounts))]
    public void Constructor_WithInvalidAmount_Throws(decimal amount)
    {
        Assert.That(
            () => new BudgetLimit(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(2026, 10, 1),
                amount),
            Throws.InstanceOf<ArgumentException>());
    }

    [TestCaseSource(nameof(InvalidAmounts))]
    public void SetMonthlyAmount_WithInvalidAmount_ThrowsAndPreservesLimit(
        decimal amount)
    {
        var limit = new BudgetLimit(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 10, 1),
            100m);

        Assert.That(
            () => limit.SetMonthlyAmount(amount),
            Throws.InstanceOf<ArgumentException>());

        Assert.That(limit.MonthlyAmount, Is.EqualTo(100m));
    }

    [Test]
    public void SetMonthlyAmount_WithZero_ReplacesExistingAmount()
    {
        var limit = new BudgetLimit(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 10, 1),
            100m);

        limit.SetMonthlyAmount(0m);

        Assert.That(limit.MonthlyAmount, Is.Zero);
    }
}