namespace CLI_Budget_Expense_Tracker;
using System.Text.Json;
using System.Globalization;
using Services;
using Data;
using Models;



class Program
{
    private static void Main(string[] args)
    {
        var expenseService = new ExpenseService();

        var budgetService = new BudgetService();

        var storageDirectory = Path.Combine(FindProjectDirectory(), "Storage");

        var filePath = Path.Combine(storageDirectory,"expenses.json");

        var storage = new JsonExpenseStorage(filePath);
        
        var budgetFilePath = Path.Combine(storageDirectory, "budgets.json");
        
        var budgetStorage = new JsonBudgetStorage(budgetFilePath);

        var csvExporter = new CsvExpenseExporter();
        var exportDirectory = Path.Combine(FindProjectDirectory(), "Exports");

        try
        {
            var loadedExpenses = storage.Load();

            foreach (var expense in loadedExpenses)
            {
                expenseService.AddExpense(expense);
            }

            var loadedLimits = budgetStorage.Load();

            foreach (var limit in loadedLimits)
            {
                // Valid JSON arrays can still contain invalid null entries.
                if (limit is null)
                {
                    throw new JsonException("Budget limits must not contain null entries.");
                }

                // Restore limits through the service to validate their values.

                budgetService.SetMonthlyLimit(
                    limit.Category,
                    limit.MonthlyAmount);
            }

            Console.WriteLine($"Loaded expenses: {loadedExpenses.Count}");
            Console.WriteLine($"Loaded budget limits: {loadedLimits.Count}");
        }
        catch (JsonException exception)
        {
            Console.WriteLine($"Invalid data file: {exception.Message}");
            return;
        }
        catch (IOException exception)
        {
            Console.WriteLine($"File error: {exception.Message}");
            return;
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.WriteLine($"Access denied: {exception.Message}");
            return;
        }
        catch (ArgumentOutOfRangeException exception)
        {
            Console.WriteLine($"Invalid stored budget limit: {exception.Message}");
            return;
        }


        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1. Add expense");
            Console.WriteLine("2. Show expenses");
            Console.WriteLine("3. Show total amount");
            Console.WriteLine("4. Filter by category");
            Console.WriteLine("5. Show today's expenses");
            Console.WriteLine("6. Filter by date range");
            Console.WriteLine("7. Save expenses and budget limits");
            Console.WriteLine("8. Set monthly budget limit");
            Console.WriteLine("9. Export expenses to CSV");
            Console.WriteLine("0. Exit");
            Console.WriteLine("Chose an option ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    CreateExpense(expenseService, budgetService);
                    if (!TrySaveExpenses(expenseService, storage))
                    {
                        Console.WriteLine("The new expense is only in memory. Select 7 to retry saving.");
                    }
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
                    TrySaveExpenses(expenseService, storage);
                    TrySaveBudgetLimits(budgetService, budgetStorage);
                    break;
                case "8":
                    SetBudgetLimit(budgetService);

                    if (!TrySaveBudgetLimits(budgetService, budgetStorage))
                    {
                        Console.WriteLine(
                            "The updated limit is only in memory. Select 7 to retry saving.");
                    }

                    break;
                case "9":
                    ExportExpensesByPeriod(expenseService, csvExporter, exportDirectory);
                    break;
                case "0":
                    var expensesSaved = TrySaveExpenses(expenseService, storage);
                    var limitsSaved = TrySaveBudgetLimits(budgetService, budgetStorage);

                    // Exit only after both files have been saved successfully.
                    if (expensesSaved && limitsSaved) return;

                    break;
                default:
                    Console.WriteLine("Invalid option. Try again.");
                    break;
            }
        }
    }

    public static bool TrySaveExpenses(
        ExpenseService expenseService,
        JsonExpenseStorage storage)
    {
        try
        {
            storage.Save(expenseService.GetAllExpenses());

            Console.WriteLine("Expenses saved.");
            return true;
        }
        catch (IOException exception)
        {
            Console.WriteLine($"File error: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.WriteLine($"Access denied: {exception.Message}");
        }

        return false;
    }
    
    /// <summary>
    /// Attempts to save all configured budget limits and reports file errors.
    /// </summary>
    /// <param name="budgetService">The service containing the current limits.</param>
    /// <param name="budgetStorage">The storage used to save the limits.</param>
    /// <returns>True if saving succeeds; otherwise, false.</returns>
    public static bool TrySaveBudgetLimits(
        BudgetService budgetService,
        JsonBudgetStorage budgetStorage)
    {
        try
        {
            budgetStorage.Save(budgetService.GetAllLimits());

            Console.WriteLine("Budget limits saved.");
            return true;
        }
        catch (IOException exception)
        {
            Console.WriteLine($"Budget file error: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.WriteLine($"Access denied: {exception.Message}");
        }

        // Let the caller decide whether to retry or keep the application running.
        return false;
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
    
    /// <summary>
    /// Prompts for a date range and exports matching expenses to a CSV file.
    /// </summary>
    /// <param name="expenseService">The service used to select expenses.</param>
    /// <param name="csvExporter">The exporter used to write the CSV file.</param>
    /// <param name="exportDirectory">The directory for exported reports.</param>
    public static void ExportExpensesByPeriod(
        ExpenseService expenseService,
        CsvExpenseExporter csvExporter,
        string exportDirectory)
    {
        var startDate = ReadDate("Start date");
        var endDate = ReadDate("End date");

        while (endDate < startDate)
        {
            Console.WriteLine("End date cannot be earlier than start date.");
            endDate = ReadDate("End date");
        }

        var expenses = expenseService.GetExpensesByPeriod(startDate, endDate);

        // Skip file creation when the selected period contains no expenses.
        if (expenses.Count == 0)
        {
            Console.WriteLine("No expenses found for the selected period.");
            return;
        }

        // Exporting the same period again replaces its previous report.
        var fileName = $"expenses_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.csv";
        var filePath = Path.Combine(exportDirectory, fileName);

        try
        {
            csvExporter.Export(expenses, filePath);

            Console.WriteLine($"Exported expenses: {expenses.Count}");
            Console.WriteLine($"Report saved to: {filePath}");
        }
        catch (IOException exception)
        {
            Console.WriteLine($"Export failed: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.WriteLine($"Access denied: {exception.Message}");
        }
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

    public static void SetBudgetLimit(BudgetService budgetService)
    {
        var category = ReadCategory();

        var currentLimit = budgetService.GetMonthlyLimit(category);

        if (currentLimit is not null)
        {
            Console.WriteLine($"Current monthly limit: {currentLimit.MonthlyAmount:F2}");
        }

        decimal monthlyAmount;

        while (true)
        {
            Console.WriteLine("Enter monthly limit (0 or greater): ");

            if (decimal.TryParse(Console.ReadLine(), out monthlyAmount) && monthlyAmount >= 0)
            {
                break;
            }

            Console.WriteLine("Enter a valid non-negative amount.");
        }

        budgetService.SetMonthlyLimit(category, monthlyAmount);

        Console.WriteLine($"Monthly limit for {category}: {monthlyAmount:F2}");
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
    
    public static void CreateExpense(ExpenseService expenseService, BudgetService budgetService)
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

        var overspend =
            budgetService.GetMonthlyOverspend(
                expense.Category, 
                expense.Date, 
                expenseService.GetAllExpenses());

        if (overspend > 0m)
        {
            Console.WriteLine($"Warning: monthly budget for {expense.Category} " +
                              $"exceeded by {overspend:F2}.");
        }

        Console.WriteLine("");
        Console.WriteLine($"Id: {expense.Id}");
        Console.WriteLine($"Amount: {expense.Amount}");
        Console.WriteLine($"Category: {expense.Category}");
        Console.WriteLine($"Date: {expense.Date}");
        Console.WriteLine($"Description: {expense.Description}");
    }
}