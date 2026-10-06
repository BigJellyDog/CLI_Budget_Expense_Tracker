using System.Net.WebSockets;
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
        decimal amount;
        
        while (true)
        {
            Console.WriteLine("Type the amount you want to add:");

            // if ReadLine == true to decimal AND amount > 0 then it gives out amount back
            // else the loop goes again
            if (decimal.TryParse(Console.ReadLine(), out amount) && amount > 0)
            {
                break;
            }

            Console.WriteLine("Invalid amount. Enter a number greater than 0.");
        }
        
        
        //Description
        Console.WriteLine("Type a description or press Enter to skip:");
        var description = Console.ReadLine() ?? "";
        
        // Category
        Console.WriteLine("Available categories:");

        // Enum.GetValues<Name og the Enum Class> to get all values from the Enum class
        foreach (var availableCategory in Enum.GetValues<Categories>())
        {
            Console.WriteLine(availableCategory);
        }

        // Make an Enum object
        Categories category;

        while (true)
        {
            Console.WriteLine("Type the category you want to add:");

            if (Enum.TryParse(Console.ReadLine(), true, out category)
                && Enum.IsDefined(category))
            {
                break;
            }

            Console.WriteLine("Invalid category. Choose a category from the list.");
            
        }
        
        
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