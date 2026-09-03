using System.Reflection;

namespace CLI_Budget_Expense_Tracker;

class Program
{
    static void Main(string[] args)
    {
        CreateExpense();
    }

    
    public static void CreateExpense()
    {
        // Amount
        Console.WriteLine("Type the amount you want to add: ");
        var amount = Convert.ToDecimal(Console.ReadLine());
        
        // Category
        Console.WriteLine("Type the category you want to add: ");
        var category = Enum.Parse<Categories>(Console.ReadLine());
        var userObject = new DataObject(amount, category, DateTime.Now);
        Console.WriteLine(userObject.AccountBalance);
        Console.WriteLine(userObject.Category);
        Console.WriteLine(userObject.Date);
        
    }
}