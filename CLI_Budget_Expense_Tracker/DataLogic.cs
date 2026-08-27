namespace CLI_Budget_Expense_Tracker;


public class DataLogic
{
    private decimal AccountSum = 0;
    private decimal CurrentMoney = 0;
    private decimal WholeIncome = 0;
    private decimal WholeExpenses = 0;
    private Categories TheCategory;


    public decimal AddMoneyToAccount(decimal amount)
    {
        AccountSum += amount;
        return AccountSum;
    }
}