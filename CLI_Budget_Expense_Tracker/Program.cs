using System.Reflection;

namespace CLI_Budget_Expense_Tracker;

class Program
{
    static void Main(string[] args)
    {
        var program = new Program();
        program.CreateExpense();
    }




    public void CreateExpense()
    {
        Console.WriteLine("Type the amount you want to add.");
        var amount = Convert.ToDecimal(Console.ReadLine());
        var userobject = new DataObject(amount, Categories.Test, DateTime.Now);
    }
}