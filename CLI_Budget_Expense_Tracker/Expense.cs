using System.Reflection;

namespace CLI_Budget_Expense_Tracker;


public class Expense(decimal amount, Categories category, DateTime date)
{
    public decimal Amount { get; set; } = amount;
    public Categories Category { get; set; } = category;
    public DateTime Date { get; set; } = date;
}
