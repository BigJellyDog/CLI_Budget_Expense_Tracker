namespace CLI_Budget_Expense_Tracker;


public class DataLogic(decimal accountBalance, Categories category, DateTime date)
{
    private decimal AccountBalance { get; set; } = accountBalance;
    public Categories Category { get; set; } = category;
    public DateTime Date { get; set; } = date;
    

    public decimal MoneyAdded(decimal amount)
    {
        AccountBalance += amount;
        return AccountBalance;
    }

    public decimal MoneySpend(decimal amount)
    {
        AccountBalance -= amount;
        return AccountBalance;
    }
}