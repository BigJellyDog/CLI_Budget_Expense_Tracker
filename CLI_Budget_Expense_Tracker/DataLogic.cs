namespace CLI_Budget_Expense_Tracker;


public class DataLogic
{
    private decimal _accountSum = 0;
    public Categories Category;
    public DateTime Date;


    public decimal MoneyAdded(decimal amount)
    {
        _accountSum += amount;
        return _accountSum;
    }

    public decimal MoneySpend(decimal amount)
    {
        _accountSum -= amount;
        return _accountSum;
    }
}