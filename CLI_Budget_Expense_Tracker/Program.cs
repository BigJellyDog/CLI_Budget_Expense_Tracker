using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Globalization;
using CLI_Budget_Expense_Tracker.Services;
namespace CLI_Budget_Expense_Tracker;
using CLI_Budget_Expense_Tracker.Data;

class Program
{
    private static void Main(string[] args)
    {
        var expenseService = new ExpenseService();

        var storageDirectory = Path.Combine(FindProjectDirectory(), "Storage");

        var filePath = Path.Combine(storageDirectory,"expenses.json");

        var storage = new JsonExpenseStorage(filePath);

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1. Add expense");
            Console.WriteLine("2. Show expenses");
            Console.WriteLine("3. Show total amount");
            Console.WriteLine("4. Filter by category");
            Console.WriteLine("5. Show today's expenses");
            Console.WriteLine("6. Filter by date range");
            Console.WriteLine("7. Save expenses");
            Console.WriteLine("0. Exit");
            Console.WriteLine("Chose an option ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    CreateExpense(expenseService);
                    break;
                case "2":
                    ShowExpenses(expenseService.GetAllExpenses());
                    break;
                case "3":
                    var total = expenseService.GetTotalAmount();
                    Console.WriteLine($"Total expenses: {total:F2}");
                    break;
                case "4":
                    var category = ReadCategory();

                    var filteredExpenses = expenseService.GetExpensesByCategory(category);
                    ShowExpenses(filteredExpenses);
                    break;
                case "5":
                    var today = DateTime.Today;

                    var todaysExpenses = expenseService.GetExpensesByPeriod(today, today);
                    
                    ShowExpenses(todaysExpenses);
                    break;
                case "6":
                    ShowExpensesByPeriod(expenseService);
                    break;
                case "7":
                    try
                    {
                        storage.Save(expenseService.GetAllExpenses());
                        Console.WriteLine($"Expenses saved to: {filePath}");
                    }
                    catch (IOException exception)
                    {
                        Console.WriteLine($"File error: {exception.Message}");
                    }
                    catch (UnauthorizedAccessException exception)
                    {
                        Console.WriteLine($"Access denied {exception.Message}");
                    }
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Try again.");
                    break;
            }
        }
    }
    
    public static string FindProjectDirectory()
    {
        DirectoryInfo? directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (directory.EnumerateFiles("*.csproj").Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find a project directory containing a .csproj file.");
    }

    public static void ShowExpensesByPeriod(ExpenseService expenseService)
    {
        var startDate = ReadDate("Start date");
        var endDate = ReadDate("End date");

        while (endDate < startDate)
        {
            Console.WriteLine(
                "End date cannot be earlier than start date.");

            endDate = ReadDate("End date");
        }

        var expenses = expenseService.GetExpensesByPeriod(startDate, endDate);
        
        ShowExpenses(expenses);

        var total = expenseService.GetTotalAmount(startDate, endDate);

        Console.WriteLine($"Total for period: {total:F2}");

        var totalsByCategory = expenseService.GetTotalsByCategory(startDate, endDate);

        if (totalsByCategory.Count <= 0) return;
        Console.WriteLine("Totals by category:");

        foreach (var categoryTotal in totalsByCategory)
        {
            Console.WriteLine($"{categoryTotal.Key}: {categoryTotal.Value:F2}");
        }

        var topCategories = expenseService.GetTopCategories(startDate, endDate);

        Console.WriteLine($"Top spending categories: {string.Join(", ", topCategories)}");
    }
    
    public static DateTime ReadDate(string prompt)
    {
        while (true)
        {
            Console.WriteLine($"{prompt} (dd.MM.yyyy): ");

            if (DateTime.TryParseExact(
                    Console.ReadLine(),
                    "dd.MM.yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                return date;
            }

            Console.WriteLine("Invalid date. Use dd.MM.yyyy, for example 07.10.2026.");
        }
    }

    public static Categories ReadCategory()
    {
        Console.WriteLine("Available categories:");

        foreach (var availableCategory in Enum.GetValues<Categories>())
        {
            Console.WriteLine(availableCategory);
        }

        Categories category;

        while (true)
        {
            Console.WriteLine("Choose a category: ");

            if (Enum.TryParse(Console.ReadLine(), true, out category) && Enum.IsDefined(category))
            {
                return category;
            }

            Console.WriteLine("Invalid category. Choose a category from the list.");
        }
    }
    
    
    public static void ShowExpenses(IReadOnlyList<Expense> expenses)
    {
        if (expenses.Count == 0)
        {
            Console.WriteLine("No expenses yet.");
            return;
        }
        
        Console.WriteLine($"Expenses found: {expenses.Count}");

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
        
        // Make an Enum object
        var category = ReadCategory();
        
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
        
        var expense = new Expense(
            amount,
            category,
            description);
        
        expenseService.AddExpense(expense);

        Console.WriteLine("");
        Console.WriteLine($"Id: {expense.Id}");
        Console.WriteLine($"Amount: {expense.Amount}");
        Console.WriteLine($"Category: {expense.Category}");
        Console.WriteLine($"Date: {expense.Date}");
        Console.WriteLine($"Description: {expense.Description}");
    }
}