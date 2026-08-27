namespace CLI_Budget_Expense_Tracker;

public class DataLogic
{
    private double AccountSum = 0;
    private double CurrentMoney = 0;
    private double WholeIncome = 0;
    private double WholeExpenses = 0;


    public double AddMoneyToAccount(double amount)
    {
        AccountSum += amount;
        return AccountSum;
    }
}