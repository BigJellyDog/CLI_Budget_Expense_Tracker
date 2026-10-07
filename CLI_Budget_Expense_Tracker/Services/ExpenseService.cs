namespace CLI_Budget_Expense_Tracker.Services;

public class ExpenseService
{
    // Create a list with objects inside called with <>
    // _expenses ist the name of the list 
    private readonly List<Expense> _expenses = [];

    public void AddExpense(Expense expense)
    {
        _expenses.Add(expense);
    }

    public IReadOnlyList<Expense> GetAllExpenses()
    {
        return _expenses.AsReadOnly();
    }

    public decimal GetTotalAmount()
    {
        return _expenses.Sum(expense => expense.Amount);
    }

    public IReadOnlyList<Expense> GetExpensesByCategory(Categories category)
    {
        return [.. _expenses.Where(expense => expense.Category == category)];
    }

    public IReadOnlyList<Expense> GetExpensesByPeriod(
        DateTime startDate,
        DateTime endDate)
    {
        return
        [
            .. _expenses.Where(expense =>
                expense.Date.Date >= startDate.Date &&
                expense.Date.Date <= endDate.Date)];
    }

}