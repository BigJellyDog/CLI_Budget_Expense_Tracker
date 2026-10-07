using CLI_Budget_Expense_Tracker.Services;
namespace CLI_Budget_Expense_Tracker;

class Program
{
    private static void Main(string[] args)
    {
        var expenseService = new ExpenseService();

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1. Add expense");
            Console.WriteLine("2. Show expenses");
            Console.WriteLine("3. Show total amount");
            Console.WriteLine("0. Exit");
            Console.WriteLine("Chose an option ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    CreateExpense(expenseService);
                    break;
                case "2":
                    ShowExpenses(expenseService);
                    break;
                case "3":
                    var total = expenseService.GetTotalAmount();
                    Console.WriteLine($"Total expenses: {total:F2}");
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Try again.");
                    break;
            }
        }
    }

    public static void ShowExpenses(ExpenseService expenseService)
    {
        var expenses = expenseService.GetAllExpenses();

        if (expenses.Count == 0)
        {
            Console.WriteLine("No expenses yet.");
            return;
        }
        
        Console.WriteLine($"Added expenses: {expenses.Count}");

        foreach (var expense in expenses)
        {
            Console.WriteLine(
                $"{expense.Date:dd.MM.yyyy HH:mm} | " +
                $"{expense.Category} | " +
                $"{expense.Amount} | " +
                $"{expense.Description}");
        }
    }
    
    public static void CreateExpense(ExpenseService expenseService)
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
        
        expenseService.AddExpense(expense);
        
        Console.WriteLine($"Id: {expense.Id}");
        Console.WriteLine($"Amount: {expense.Amount}");
        Console.WriteLine($"Category: {expense.Category}");
        Console.WriteLine($"Date: {expense.Date}");
        Console.WriteLine($"Description: {expense.Description}");
    }
}