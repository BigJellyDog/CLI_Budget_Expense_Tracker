namespace CLI_Budget_Expense_Tracker.Core.Models;

public class BudgetLimit(Categories category, decimal monthlyAmount)
{
    public Categories Category { get; } = category;

    public decimal MonthlyAmount { get; } = monthlyAmount;
}