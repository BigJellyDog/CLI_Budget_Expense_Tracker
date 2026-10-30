namespace ExpenseTracker.App.Services;

using CLI_Budget_Expense_Tracker.Core.Interfaces;
using CLI_Budget_Expense_Tracker.Core.Models;
using CLI_Budget_Expense_Tracker.Core.Services;
using CLI_Budget_Expense_Tracker.Infrastructure.Backup;
using Microsoft.Extensions.DependencyInjection;

public sealed record DemoDataResult(int Categories, int Expenses, int Schedules);

/// <summary>
/// Fills the app with a year of sample data, counted back from today, so every screen has something
/// to show: categories, expenses, monthly budgets, recurring schedules and import rules.
/// The same seed gives the same data each time. A backup is made first.
/// </summary>
public sealed class DemoDataService(
    IServiceScopeFactory scopeFactory,
    AppInitializer initializer,
    DatabaseBackupService backups,
    TimeProvider timeProvider)
{
    private sealed record Spending(
        string Category,
        double ChancePerDay,
        int MinCents,
        int MaxCents,
        string[] Descriptions,
        double[] MonthFactor);

    private static readonly double[] Even = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1];

    // More shopping and fun around the holidays, more going out in summer.
    private static readonly double[] Holidays = [0.9, 0.8, 0.9, 0.9, 1, 1, 1.1, 1.2, 1, 1, 1.4, 1.8];
    private static readonly double[] Summer = [0.7, 0.7, 0.9, 1, 1.2, 1.4, 1.5, 1.4, 1.1, 0.9, 0.8, 1.2];

    private static readonly Spending[] Habits =
    [
        new("Food", 0.42, 800, 5500, ["LIDL groceries", "REWE Markt", "Bakery", "Farmers market"], Even),
        new("Food", 0.12, 1200, 3800, ["Takeaway", "Restaurant", "Lunch"], Summer),
        new("Transport", 0.10, 500, 4500, ["Taxi", "Fuel", "Train ticket"], Even),
        new("Entertainment", 0.09, 1000, 9500, ["Cinema", "Theatre", "Concert", "Book", "Game"], Summer),
        new("Health", 0.035, 1500, 12000, ["Pharmacy", "Checkup", "Dentist"], Even),
        new("Pets", 0.07, 800, 9000, ["Fressnapf", "Vet", "Toys"], Even),
        new("Shopping", 0.08, 1500, 14000, ["Amazon EU", "Clothes", "Electronics", "Shoes"], Holidays),
        new("Personal care", 0.05, 800, 5500, ["Hairdresser", "dm-drogerie markt", "Cosmetics"], Even),
        new("Education", 0.025, 1500, 8000, ["Online course", "Textbook", "Workshop", "Language class"], Even),
        new("Gifts", 0.035, 1500, 9000, ["Birthday gift", "Flowers", "Wedding present"], Holidays),
        new("Travel", 0.02, 3000, 28000, ["Hotel", "Train tickets", "Flight", "Hostel", "Souvenirs"], Summer)
    ];

    private static readonly string[] CategoryNames =
    [
        "Food", "Transport", "Entertainment", "Health", "Pets", "Shopping", "Personal care", "Education",
        "Gifts", "Travel", "Housing", "Utilities", "Insurance", "Sports"
    ];

    private static readonly (string Category, decimal Limit)[] Budgets =
    [
        ("Food", 500m), ("Entertainment", 190m), ("Health", 130m), ("Pets", 120m), ("Transport", 160m),
        ("Shopping", 220m), ("Personal care", 60m), ("Education", 70m), ("Gifts", 80m), ("Travel", 260m),
        ("Housing", 700m), ("Utilities", 120m), ("Insurance", 45m), ("Sports", 40m)
    ];

    private static readonly (string Pattern, string Category)[] Rules =
    [
        ("LIDL", "Food"), ("REWE", "Food"), ("BVG", "Transport"), ("Amazon", "Shopping"), ("Fressnapf", "Pets"),
        ("Deutsche Bahn", "Travel"), ("dm-drogerie", "Personal care")
    ];

    public async Task<DemoDataResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        await initializer.InitializeAsync();

        // The sample data is added on top of what is there; this is the way back.
        backups.CreateBackup(DatabaseBackupService.AutomaticPrefix);

        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        return await Task.Run(() => AddAsync(today, cancellationToken), cancellationToken);
    }

    private async Task<DemoDataResult> AddAsync(DateOnly today, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var categoryService = services.GetRequiredService<CategoryService>();
        var expenseService = services.GetRequiredService<ExpenseService>();
        var budgetService = services.GetRequiredService<BudgetService>();
        var scheduleService = services.GetRequiredService<RecurringExpenseService>();
        var ruleService = services.GetRequiredService<CategoryRuleService>();

        // Categories that already exist are reused, so loading twice does not make duplicates.
        var existing = (await categoryService.GetAllAsync(cancellationToken))
            .ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase);

        var created = 0;

        // The standard categories carry their names in the language of the screens, so the demo uses those.
        async Task<Guid> CategoryAsync(string englishName)
        {
            var name = Loc.T(englishName);

            if (existing.TryGetValue(name, out var id)) return id;

            var category = await categoryService.AddAsync(name, cancellationToken);
            existing[name] = category.Id;
            created++;
            return category.Id;
        }

        foreach (var name in CategoryNames)
            await CategoryAsync(name);

        // A year back from today, a little different each day but the same on every run.
        var random = new Random(2026);
        var expenses = 0;

        for (var date = today.AddYears(-1).AddDays(1); date <= today; date = date.AddDays(1))
        {
            foreach (var habit in Habits)
            {
                var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 1.3 : 1.0;

                if (random.NextDouble() >= habit.ChancePerDay * habit.MonthFactor[date.Month - 1] * weekend)
                    continue;

                var amount = random.Next(habit.MinCents, habit.MaxCents + 1) / 100m;
                var description = habit.Descriptions[random.Next(habit.Descriptions.Length)];

                await expenseService.AddAsync(amount, await CategoryAsync(habit.Category), date, description, cancellationToken);
                expenses++;
            }
        }

        // Limits for each of the twelve months, so the budget screens have history.
        for (var back = 0; back < 12; back++)
        {
            var month = new DateOnly(today.Year, today.Month, 1).AddMonths(-back);

            foreach (var (category, limit) in Budgets)
                await budgetService.SetMonthlyLimitAsync(await CategoryAsync(category), month, limit, cancellationToken);
        }

        // Monthly payments: the generator below creates their expenses for the whole year.
        var firstDue = new DateOnly(today.Year, today.Month, Math.Min(today.Day, 28)).AddMonths(-11);
        var schedules = 0;

        foreach (var (description, category, amount, offset) in new (string, string, decimal, int)[]
                 {
                     ("Bus pass", "Transport", 50m, 0),
                     ("Streaming subscription", "Entertainment", 15.99m, 3),
                     ("Gym", "Sports", 29.90m, 6),
                     ("Rent", "Housing", 650m, 1),
                     ("Electricity", "Utilities", 48m, 4),
                     ("Internet and phone", "Utilities", 39.99m, 5),
                     ("Liability insurance", "Insurance", 36.50m, 2)
                 })
        {
            await scheduleService.AddAsync(
                amount,
                await CategoryAsync(category),
                RecurrenceFrequency.Monthly,
                firstDue.AddDays(offset) > today ? firstDue : firstDue.AddDays(offset),
                description,
                null,
                cancellationToken);

            schedules++;
        }

        expenses += await services.GetRequiredService<IRecurringExpenseGenerator>()
            .GenerateAsync(today, cancellationToken);

        foreach (var (pattern, category) in Rules)
        {
            try
            {
                await ruleService.AddAsync(pattern, await CategoryAsync(category), cancellationToken);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // The rule is already there.
            }
        }

        return new DemoDataResult(created, expenses, schedules);
    }
}
