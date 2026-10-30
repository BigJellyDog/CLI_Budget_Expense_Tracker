namespace ExpenseTracker.App.Services;

/// <summary>
/// A request from another screen to open Expenses on one category and month (a click on a
/// category in Reports). The Expenses screen takes it when it appears.
/// </summary>
public sealed class ExpenseFilterRequest
{
    public Guid? CategoryId { get; private set; }

    public DateTime? Month { get; private set; }

    public bool IsPending { get; private set; }

    public void Set(Guid categoryId, DateTime month)
    {
        CategoryId = categoryId;
        Month = month;
        IsPending = true;
    }

    /// <summary>Returns the request once; afterwards there is none.</summary>
    public (Guid CategoryId, DateTime Month)? Take()
    {
        if (!IsPending || CategoryId is not { } id || Month is not { } month) return null;

        IsPending = false;
        return (id, month);
    }
}
