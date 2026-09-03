using System.Reflection;

namespace CLI_Budget_Expense_Tracker;


public class DataObject(decimal accountBalance, Categories category, DateTime date)
{
    public decimal AccountBalance { get; set; } = accountBalance;
    public Categories Category { get; set; } = category;
    public DateTime Date { get; set; } = date;

    //Test list
    // public override string ToString()
    // {
    //     return $"{AccountBalance}, {Category}, {Date}";
    // }


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