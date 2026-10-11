using CLI_Budget_Expense_Tracker.Core.Models;
using NUnit.Framework;

namespace CLI_Budget_Expense_Tracker.Test;

[TestFixture]
public sealed class RecurringExpenseTests
{
    [TestCase(2026, 31, 28)]
    [TestCase(2026, 30, 28)]
    [TestCase(2024, 31, 29)]
    public void Monthly_RestoresOriginalDayAfterFebruary(
        int year,
        int originalDay,
        int februaryDay)
    {
        var recurring = Create(
            RecurrenceFrequency.Monthly,
            new DateOnly(year, 1, originalDay));

        recurring.AdvanceNextDueDate();

        Assert.That(
            recurring.NextDueDate,
            Is.EqualTo(new DateOnly(year, 2, februaryDay)));

        recurring.AdvanceNextDueDate();

        Assert.That(
            recurring.NextDueDate,
            Is.EqualTo(new DateOnly(year, 3, originalDay)));
    }

    [Test]
    public void Yearly_RestoresLeapDayInNextLeapYear()
    {
        var recurring = Create(
            RecurrenceFrequency.Yearly,
            new DateOnly(2024, 2, 29));

        for (var year = 2025; year <= 2027; year++)
        {
            recurring.AdvanceNextDueDate();

            Assert.That(
                recurring.NextDueDate,
                Is.EqualTo(new DateOnly(year, 2, 28)));
        }

        recurring.AdvanceNextDueDate();

        Assert.That(
            recurring.NextDueDate,
            Is.EqualTo(new DateOnly(2028, 2, 29)));
    }

    [TestCase(RecurrenceFrequency.Daily, 1)]
    [TestCase(RecurrenceFrequency.Weekly, 7)]
    public void DailyAndWeekly_CrossYearBoundary(
        RecurrenceFrequency frequency,
        int januaryDay)
    {
        var recurring = Create(
            frequency,
            new DateOnly(2026, 12, 31));

        recurring.AdvanceNextDueDate();

        Assert.That(
            recurring.NextDueDate,
            Is.EqualTo(new DateOnly(2027, 1, januaryDay)));
    }

    [Test]
    public void Advance_WhenInactive_ThrowsAndPreservesDate()
    {
        var startDate = new DateOnly(2026, 10, 11);
        var recurring = Create(
            RecurrenceFrequency.Monthly,
            startDate);

        recurring.Deactivate();

        Assert.Throws<InvalidOperationException>(
            () => recurring.AdvanceNextDueDate());

        Assert.That(recurring.IsActive, Is.False);
        Assert.That(recurring.NextDueDate, Is.EqualTo(startDate));
    }

    [TestCase(RecurrenceFrequency.Daily)]
    [TestCase(RecurrenceFrequency.Weekly)]
    [TestCase(RecurrenceFrequency.Monthly)]
    [TestCase(RecurrenceFrequency.Yearly)]
    public void Advance_BeyondSupportedDateRange_Deactivates(
        RecurrenceFrequency frequency)
    {
        var recurring = Create(frequency, DateOnly.MaxValue);

        Assert.DoesNotThrow(() => recurring.AdvanceNextDueDate());

        Assert.That(recurring.IsActive, Is.False);
        Assert.That(
            recurring.NextDueDate,
            Is.EqualTo(DateOnly.MaxValue));
    }

    private static RecurringExpense Create(
        RecurrenceFrequency frequency,
        DateOnly startDate)
    {
        return new RecurringExpense(
            Guid.NewGuid(),
            15m,
            Guid.NewGuid(),
            frequency,
            startDate,
            "Subscription");
    }
}