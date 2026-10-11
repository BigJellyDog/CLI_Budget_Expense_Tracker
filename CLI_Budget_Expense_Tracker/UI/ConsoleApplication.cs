namespace CLI_Budget_Expense_Tracker.UI;

using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.Exporting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Owns the menu and creates a fresh service scope for each operation.</summary>
public sealed class ConsoleApplication(
    IServiceScopeFactory scopeFactory, AppPaths paths, TextReader reader, TextWriter writer)
{
    private readonly ConsoleInput input = new(reader, writer);

    public async Task RunAsync()
    {
        writer.WriteLine("Expense Tracker — changes are saved automatically.");
        while (true)
        {
            writer.WriteLine("""

                1. Add expense                 2. Show expenses
                3. Show total amount           4. Filter by category
                5. Show today's expenses       6. Report for a date range
                7. Delete expense              8. Set monthly budget limit
                9. Export expenses to CSV     10. Add category
                11. Show categories           12. Rename category
                13. Delete unused category    14. Show monthly budgets
                15. Remove monthly limit      16. Add recurring expense
                17. Show recurring expenses   18. Deactivate recurring expense
                19. Generate due expenses      0. Exit
                """);
            try
            {
                var choice = input.Read("Choose an option: ").Trim();
                if (choice == "0") return;
                await using var scope = scopeFactory.CreateAsyncScope();
                await ExecuteAsync(choice, scope.ServiceProvider);
            }
            catch (EndOfStreamException) { return; }
            catch (DbUpdateException)
            {
                writer.WriteLine("The change could not be saved. It may conflict with another record.");
            }
            catch (SqliteException exception)
            {
                writer.WriteLine($"Cannot access the database: {exception.Message}");
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
                or IOException or UnauthorizedAccessException)
            {
                writer.WriteLine($"Error: {exception.Message}");
            }
        }
    }

    private async Task ExecuteAsync(string choice, IServiceProvider services)
    {
        var categories = services.GetRequiredService<CategoryService>();
        var expenses = services.GetRequiredService<ExpenseService>();
        var budgets = services.GetRequiredService<BudgetService>();
        var recurring = services.GetRequiredService<RecurringExpenseService>();
        switch (choice)
        {
            case "1":
                await AddExpenseAsync(categories, expenses, budgets);
                break;
            case "2":
                await ShowReportAsync(await expenses.GetReportAsync(), categories);
                break;
            case "3":
                writer.WriteLine($"Total expenses: {(await expenses.GetReportAsync()).Total:F2}");
                break;
            case "4":
                var filter = await SelectCategoryAsync(categories);
                if (filter is not null)
                    await ShowReportAsync(await expenses.GetReportAsync(categoryId: filter.Id), categories);
                break;
            case "5":
                var today = expenses.Today;
                await ShowReportAsync(await expenses.GetReportAsync(today, today), categories);
                break;
            case "6":
                var period = input.ReadPeriod();
                await ShowReportAsync(await expenses.GetReportAsync(period.Start, period.End), categories, true);
                break;
            case "7":
                await DeleteExpenseAsync(expenses, categories);
                break;
            case "8":
                var budgetCategory = await SelectCategoryAsync(categories);
                if (budgetCategory is null) break;
                var month = input.ReadMonth();
                await budgets.SetMonthlyLimitAsync(budgetCategory.Id, month, input.ReadAmount(true));
                writer.WriteLine("Monthly limit saved.");
                WriteBudget(await budgets.GetStatusAsync(budgetCategory.Id, month), budgetCategory.Name);
                break;
            case "9":
                await ExportAsync(expenses, categories, services.GetRequiredService<CsvExpenseExporter>());
                break;
            case "10":
                var added = await categories.AddAsync(input.ReadName("Category name: "));
                writer.WriteLine($"Category created: {added.Name}");
                break;
            case "11":
                WriteCategories(await categories.GetAllAsync());
                break;
            case "12":
                var renamed = await SelectCategoryAsync(categories);
                if (renamed is null) break;
                await categories.RenameAsync(renamed.Id, input.ReadName("New name: "));
                writer.WriteLine("Category renamed.");
                break;
            case "13":
                var deleted = await SelectCategoryAsync(categories);
                if (deleted is null) break;
                writer.WriteLine(await categories.DeleteAsync(deleted.Id) ? "Category deleted." : "Category not found.");
                break;
            case "14":
                var statuses = await budgets.GetMonthlyStatusesAsync(input.ReadMonth());
                var names = (await categories.GetAllAsync()).ToDictionary(x => x.Id, x => x.Name);
                if (statuses.Count == 0) writer.WriteLine("No budget limits for this month.");
                foreach (var status in statuses.OrderBy(x => names[x.CategoryId]))
                    WriteBudget(status, names[status.CategoryId]);
                break;
            case "15":
                var removed = await SelectCategoryAsync(categories);
                if (removed is null) break;
                writer.WriteLine(await budgets.DeleteMonthlyLimitAsync(removed.Id, input.ReadMonth())
                    ? "Monthly limit removed." : "No limit for this category and month.");
                break;
            case "16":
                await AddRecurringExpenseAsync(categories, recurring, expenses.Today);
                break;

            case "17":
                await ShowRecurringExpensesAsync(categories, recurring);
                break;

            case "18":
                await DeactivateRecurringExpenseAsync(categories, recurring);
                break;

            case "19":
            case "generate":
                var generated = await services
                    .GetRequiredService<IRecurringExpenseGenerator>()
                    .GenerateAsync();

                writer.WriteLine($"Generated {generated} recurring expense(s).");
                break;
            default:
                writer.WriteLine("Invalid option. Try again.");
                break;
        }
    }
        
    private async Task AddRecurringExpenseAsync(
    CategoryService categories,
    RecurringExpenseService recurring,
    DateOnly today)
    {
        var category = await SelectCategoryAsync(categories);
        if (category is null) return;

        var frequencies = Enum.GetValues<RecurrenceFrequency>();

        writer.WriteLine("Frequency:");
        for (var i = 0; i < frequencies.Length; i++)
            writer.WriteLine($"{i + 1}. {frequencies[i]}");

        var number = input.ReadSelection(frequencies.Length);
        if (number == 0) return;

        var amount = input.ReadAmount(false);
        var startDate = input.ReadDate("First payment date", today);
        var description = input.Read("Description (optional): ");

        await recurring.AddAsync(
            amount,
            category.Id,
            frequencies[number - 1],
            startDate,
            description);

        writer.WriteLine(
            "Recurring expense saved. Choose 19 to generate due expenses now.");
    }

    private async Task<IReadOnlyList<RecurringExpense>> ShowRecurringExpensesAsync(
        CategoryService categories,
        RecurringExpenseService recurring,
        bool activeOnly = false)
    {
        var items = (await recurring.GetAllAsync())
            .Where(x => !activeOnly || x.IsActive)
            .ToArray();

        if (items.Length == 0)
        {
            writer.WriteLine(activeOnly
                ? "No active recurring expenses."
                : "No recurring expenses.");

            return items;
        }

        var names = (await categories.GetAllAsync())
            .ToDictionary(x => x.Id, x => x.Name);

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var status = item.IsActive ? "Active" : "Inactive";
            var nextDate = item.IsActive
                ? item.NextDueDate.ToString("dd.MM.yyyy")
                : "-";

            writer.WriteLine(
                $"{i + 1}. {names[item.CategoryId]} | {item.Amount:F2} | " +
                $"{item.Frequency} | Next: {nextDate} | {status} | {item.Description}");
        }

        return items;
    }

    private async Task DeactivateRecurringExpenseAsync(
        CategoryService categories,
        RecurringExpenseService recurring)
    {
        var items = await ShowRecurringExpensesAsync(
            categories,
            recurring,
            activeOnly: true);

        if (items.Count == 0) return;

        var number = input.ReadSelection(items.Count);
        if (number == 0) return;

        await recurring.DeactivateAsync(items[number - 1].Id);

        writer.WriteLine("Recurring expense deactivated.");
    }

    private async Task AddExpenseAsync(CategoryService categories, ExpenseService expenses, BudgetService budgets)
    {
        var category = await SelectCategoryAsync(categories);
        if (category is null) return;
        var amount = input.ReadAmount(false);
        var date = input.ReadDate("Expense date", expenses.Today);
        var expense = await expenses.AddAsync(amount, category.Id, date, input.Read("Description (optional): "));
        writer.WriteLine($"Expense saved: {expense.Id}");
        var status = await budgets.GetStatusAsync(category.Id, date);
        if (status.Overspend > 0)
            writer.WriteLine($"Warning: monthly budget for {category.Name} exceeded by {status.Overspend:F2}.");
    }

    private async Task DeleteExpenseAsync(ExpenseService expenses, CategoryService categories)
    {
        var report = await expenses.GetReportAsync();
        await ShowReportAsync(report, categories);
        if (report.Expenses.Count == 0) return;
        var number = input.ReadSelection(report.Expenses.Count);
        if (number == 0) return;
        writer.WriteLine(await expenses.DeleteAsync(report.Expenses[number - 1].Id)
            ? "Expense deleted." : "Expense not found.");
    }

    private async Task ExportAsync(ExpenseService expenses, CategoryService categories, CsvExpenseExporter exporter)
    {
        var period = input.ReadPeriod();
        var report = await expenses.GetReportAsync(period.Start, period.End);
        if (report.Expenses.Count == 0)
        {
            writer.WriteLine("No expenses for this period.");
            return;
        }
        var names = (await categories.GetAllAsync()).ToDictionary(x => x.Id, x => x.Name);
        var path = Path.Combine(paths.ExportDirectory, $"expenses_{period.Start:yyyy-MM-dd}_{period.End:yyyy-MM-dd}.csv");
        exporter.Export(report.Expenses, names, path);
        writer.WriteLine($"Exported {report.Expenses.Count} expenses to: {path}");
    }

    private async Task<Category?> SelectCategoryAsync(CategoryService categories)
    {
        var items = await categories.GetAllAsync();
        WriteCategories(items);
        if (items.Count == 0) return null;
        var number = input.ReadSelection(items.Count);
        return number == 0 ? null : items[number - 1];
    }

    private void WriteCategories(IReadOnlyList<Category> categories)
    {
        if (categories.Count == 0) writer.WriteLine("No categories yet. Choose 10 to add one.");
        for (var i = 0; i < categories.Count; i++)
            writer.WriteLine($"{i + 1}. {categories[i].Name}");
    }

    private async Task ShowReportAsync(ExpenseReport report, CategoryService categories, bool showBreakdown = false)
    {
        if (report.Expenses.Count == 0)
        {
            writer.WriteLine("No expenses found.");
            return;
        }
        var names = (await categories.GetAllAsync()).ToDictionary(x => x.Id, x => x.Name);
        for (var i = 0; i < report.Expenses.Count; i++)
        {
            var expense = report.Expenses[i];
            writer.WriteLine($"{i + 1}. {expense.Date:dd.MM.yyyy} | {names[expense.CategoryId]} | {expense.Amount:F2} | {expense.Description}");
        }
        writer.WriteLine($"Total expenses: {report.Total:F2}");
        if (!showBreakdown) return;
        foreach (var total in report.TotalsByCategory.OrderByDescending(x => x.Value))
            writer.WriteLine($"{names[total.Key]}: {total.Value:F2}");
        writer.WriteLine($"Top spending categories: {string.Join(", ", report.TopCategoryIds.Select(id => names[id]))}");
    }

    private void WriteBudget(BudgetStatus status, string categoryName) =>
        writer.WriteLine($"{categoryName} | {status.Month:MM.yyyy} | Limit: {status.Limit:F2} | Spent: {status.Spent:F2} | Overspend: {status.Overspend:F2}");
}
