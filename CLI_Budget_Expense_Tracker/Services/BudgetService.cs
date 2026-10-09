using CLI_Budget_Expense_Tracker;
using CLI_Budget_Expense_Tracker.Models;

public class BudgetService
{
    private readonly Dictionary<Categories, BudgetLimit> _limits = [];

    public void SetMonthlyLimit(Categories category, decimal monthlyAmount)
    {
        if (!Enum.IsDefined(category))
            throw new ArgumentOutOfRangeException(nameof(monthlyAmount));

        _limits[category] = new BudgetLimit(category, monthlyAmount);
    }

    public BudgetLimit? GetMonthlyLimit(Categories category)
    {
        return _limits.GetValueOrDefault(category);
    }
}