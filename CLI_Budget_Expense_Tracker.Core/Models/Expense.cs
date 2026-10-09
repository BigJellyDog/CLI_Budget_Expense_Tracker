namespace CLI_Budget_Expense_Tracker.Core.Models;


public class Expense(
    decimal amount, 
    Categories category,
    string description = "")
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal Amount { get; set; } = amount;
    public Categories Category { get; set; } = category;
    public DateTime Date { get; set; } = DateTime.Now;
    public string Description { get; set; } = description;
}
