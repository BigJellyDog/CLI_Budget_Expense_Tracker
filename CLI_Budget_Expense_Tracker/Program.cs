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
        
        //Description
        Console.WriteLine("Type a description or press Enter to skip:");
        var description = Console.ReadLine() ?? "";
        
        // Category
        Console.WriteLine("Type the category you want to add: ");
        var category = Enum.Parse<Categories>(Console.ReadLine());
        var expense = new Expense(
            amount,
            category,
            description);
        
        Console.WriteLine($"Id: {expense.Id}");
        Console.WriteLine($"Amount: {expense.Amount}");
        Console.WriteLine($"Category: {expense.Category}");
        Console.WriteLine($"Date: {expense.Date}");
        Console.WriteLine($"Description: {expense.Description}");
    }
}