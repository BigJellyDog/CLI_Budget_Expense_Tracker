namespace CLI_Budget_Expense_Tracker.Core.Models;

/// <summary>
/// Defines a recurring payment and its next scheduled date.
/// </summary>
public sealed class RecurringExpense
{
    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Description { get; private set; }

    public RecurrenceFrequency Frequency { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly NextDueDate { get; private set; }

    /// <summary>Last day an occurrence may fall on. Null means the schedule never ends.</summary>
    public DateOnly? EndDate { get; private set; }

    public bool IsActive { get; private set; }

    public RecurringExpense(
        Guid id,
        decimal amount,
        Guid categoryId,
        RecurrenceFrequency frequency,
        DateOnly startDate,
        string description = "",
        DateOnly? endDate = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Recurring expense ID must not be empty.",
                nameof(id));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Category ID must not be empty.",
                nameof(categoryId));
        }

        if (!Enum.IsDefined(frequency))
        {
            throw new ArgumentOutOfRangeException(
                nameof(frequency),
                frequency,
                "Unsupported recurrence frequency.");
        }

        MoneyRules.Validate(amount, nameof(amount), allowZero: false);
        ArgumentNullException.ThrowIfNull(description);

        if (endDate is { } end && end < startDate)
        {
            throw new ArgumentException(
                "End date cannot be before the start date.",
                nameof(endDate));
        }

        Id = id;
        Amount = amount;
        CategoryId = categoryId;
        Frequency = frequency;
        StartDate = startDate;
        NextDueDate = startDate;
        Description = description;
        EndDate = endDate;
        IsActive = true;
    }

    /// <summary>
    /// Changes the schedule. Expenses created earlier stay as they are.
    /// A new frequency counts from the next due date, so the cycle stays consistent.
    /// An end date before the next due date finishes the schedule.
    /// </summary>
    public void Update(
        decimal amount,
        Guid categoryId,
        RecurrenceFrequency frequency,
        string description,
        DateOnly? endDate)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Category ID must not be empty.",
                nameof(categoryId));
        }

        if (!Enum.IsDefined(frequency))
        {
            throw new ArgumentOutOfRangeException(
                nameof(frequency),
                frequency,
                "Unsupported recurrence frequency.");
        }

        MoneyRules.Validate(amount, nameof(amount), allowZero: false);
        ArgumentNullException.ThrowIfNull(description);

        if (endDate is { } end && end < StartDate)
        {
            throw new ArgumentException(
                "End date cannot be before the start date.",
                nameof(endDate));
        }

        if (frequency != Frequency)
        {
            StartDate = NextDueDate;
        }

        Amount = amount;
        CategoryId = categoryId;
        Frequency = frequency;
        Description = description;
        EndDate = endDate;

        if (EndDate is { } last && NextDueDate > last)
        {
            Deactivate();
        }
    }

    /// <summary>
    /// Stops generating expenses for this schedule.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }
    
    /// <summary>
    /// Resumes the schedule. Occurrences missed while it was inactive are skipped,
    /// so the next due date is the first one on or after <paramref name="today"/>.
    /// </summary>
    public void Reactivate(DateOnly today)
    {
        IsActive = true;

        while (IsActive && NextDueDate < today)
        {
            AdvanceNextDueDate();
        }
    }

    /// <summary>
    /// Advances the schedule after the current occurrence is generated.
    /// Deactivates the schedule if the next date exceeds DateOnly's range.
    /// </summary>
    public void AdvanceNextDueDate()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException(
                "Cannot advance an inactive recurring expense.");
        }

        try
        {
            NextDueDate = Frequency switch
            {
                RecurrenceFrequency.Daily =>
                    NextDueDate.AddDays(1),

                RecurrenceFrequency.Weekly =>
                    NextDueDate.AddDays(7),

                RecurrenceFrequency.Monthly =>
                    StartDate.AddMonths(
                        (NextDueDate.Year - StartDate.Year) * 12
                        + NextDueDate.Month - StartDate.Month
                        + 1),

                RecurrenceFrequency.Yearly =>
                    StartDate.AddYears(
                        NextDueDate.Year - StartDate.Year + 1),

                _ => throw new InvalidOperationException(
                    "Unsupported recurrence frequency.")
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            // No further occurrence can fit within DateOnly's supported range.
            Deactivate();
            return;
        }

        if (EndDate is { } last && NextDueDate > last)
        {
            Deactivate();
        }
    }
}