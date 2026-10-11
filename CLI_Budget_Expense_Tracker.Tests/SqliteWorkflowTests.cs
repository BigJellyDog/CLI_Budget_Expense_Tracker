namespace CLI_Budget_Expense_Tracker.Test;

using Core.Models;
using Core.Services;
using Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public sealed class SqliteWorkflowTests
{
    private SqliteTestDatabase database = null!;

    [SetUp]
    public async Task SetUp()
    {
        database = new SqliteTestDatabase();
        await database.InitializeAsync();
    }

    [TearDown]
    public async Task TearDown() => await database.DisposeAsync();

    [Test]
    public async Task SavedExpenseAndBudget_AreVisibleInANewScope()
    {
        Guid categoryId;
        Guid expenseId;
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var category = await scope.ServiceProvider.GetRequiredService<CategoryService>().AddAsync("  Food  ");
            categoryId = category.Id;
            var expense = await scope.ServiceProvider.GetRequiredService<ExpenseService>()
                .AddAsync(12.50m, categoryId, new DateOnly(2026, 10, 9), "Lunch");
            expenseId = expense.Id;
            await scope.ServiceProvider.GetRequiredService<BudgetService>()
                .SetMonthlyLimitAsync(categoryId, new DateOnly(2026, 10, 9), 10m);
        }
        await database.InitializeAsync(); // A second startup must preserve data.
        await using var readScope = database.Services.CreateAsyncScope();
        var report = await readScope.ServiceProvider.GetRequiredService<ExpenseService>().GetReportAsync();
        var status = await readScope.ServiceProvider.GetRequiredService<BudgetService>()
            .GetStatusAsync(categoryId, new DateOnly(2026, 10, 1));
        Assert.Multiple(() =>
        {
            Assert.That(report.Expenses.Single().Id, Is.EqualTo(expenseId));
            Assert.That(report.Expenses.Single().Description, Is.EqualTo("Lunch"));
            Assert.That(report.Total, Is.EqualTo(12.50m));
            Assert.That(status.Overspend, Is.EqualTo(2.50m));
        });
    }

    [Test]
    public async Task Categories_RejectDuplicatesAndRenameWithoutChangingExpenseReference()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var categories = scope.ServiceProvider.GetRequiredService<CategoryService>();
        var expenses = scope.ServiceProvider.GetRequiredService<ExpenseService>();
        var food = await categories.AddAsync("Food");
        await categories.AddAsync("Travel");
        await expenses.AddAsync(1m, food.Id, new DateOnly(2026, 10, 1));
        Assert.ThrowsAsync<InvalidOperationException>(() => categories.AddAsync("  FOOD "));
        Assert.ThrowsAsync<InvalidOperationException>(() => categories.RenameAsync(food.Id, "travel"));
        Assert.That((await categories.GetAllAsync()).Single(x => x.Id == food.Id).Name, Is.EqualTo("Food"));
        await categories.RenameAsync(food.Id, "Groceries");
        Assert.That((await expenses.GetReportAsync()).Expenses.Single().CategoryId, Is.EqualTo(food.Id));
        Assert.That((await categories.GetAllAsync()).Single(x => x.Id == food.Id).NormalizedName, Is.EqualTo("GROCERIES"));
    }

    [Test]
    public async Task DeleteExpense_ExcludesItFromReportsAndBudgetsButKeepsCategoryReference()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var categories = scope.ServiceProvider.GetRequiredService<CategoryService>();
        var expenses = scope.ServiceProvider.GetRequiredService<ExpenseService>();
        var budgets = scope.ServiceProvider.GetRequiredService<BudgetService>();
        var category = await categories.AddAsync("Food");
        var expense = await expenses.AddAsync(20m, category.Id, new DateOnly(2026, 10, 1));
        await budgets.SetMonthlyLimitAsync(category.Id, expense.Date, 10m);
        Assert.That(await expenses.DeleteAsync(expense.Id), Is.True);
        Assert.That(await expenses.DeleteAsync(expense.Id), Is.False);
        Assert.That((await expenses.GetReportAsync()).Total, Is.Zero);
        Assert.That((await budgets.GetStatusAsync(category.Id, expense.Date)).Overspend, Is.Zero);
        await budgets.DeleteMonthlyLimitAsync(category.Id, expense.Date);
        Assert.ThrowsAsync<InvalidOperationException>(() => categories.DeleteAsync(category.Id));
        var context = scope.ServiceProvider.GetRequiredService<ExpenseTrackerDbContext>();
        Assert.That((await context.Expenses.IgnoreQueryFilters().SingleAsync()).IsDeleted, Is.True);
    }

    [Test]
    public async Task CategoryUsedOnlyByBudget_CannotBeDeletedUntilLimitIsRemoved()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var categories = scope.ServiceProvider.GetRequiredService<CategoryService>();
        var budgets = scope.ServiceProvider.GetRequiredService<BudgetService>();
        var category = await categories.AddAsync("Rent");
        var month = new DateOnly(2026, 10, 1);
        await budgets.SetMonthlyLimitAsync(category.Id, month, 0);
        Assert.ThrowsAsync<InvalidOperationException>(() => categories.DeleteAsync(category.Id));
        Assert.That(await budgets.DeleteMonthlyLimitAsync(category.Id, month), Is.True);
        Assert.That(await categories.DeleteAsync(category.Id), Is.True);
        Assert.That(await categories.DeleteAsync(category.Id), Is.False);
    }

    [Test]
    public async Task Report_FiltersInclusiveDatesAndCategoryAndHandlesTiedTopCategories()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var categories = scope.ServiceProvider.GetRequiredService<CategoryService>();
        var expenses = scope.ServiceProvider.GetRequiredService<ExpenseService>();
        var food = await categories.AddAsync("Food");
        var travel = await categories.AddAsync("Travel");
        var start = new DateOnly(2026, 10, 3);
        var end = new DateOnly(2026, 10, 7);
        await expenses.AddAsync(100m, food.Id, start.AddDays(-1));
        await expenses.AddAsync(10m, food.Id, start);
        await expenses.AddAsync(20m, food.Id, end);
        await expenses.AddAsync(30m, travel.Id, end);
        await expenses.AddAsync(100m, food.Id, end.AddDays(1));
        var report = await expenses.GetReportAsync(start, end);
        Assert.That(report.Total, Is.EqualTo(60m));
        Assert.That(report.TopCategoryIds, Is.EquivalentTo(new[] { food.Id, travel.Id }));
        Assert.That((await expenses.GetReportAsync(start, end, food.Id)).Expenses, Has.Count.EqualTo(2));
        Assert.ThrowsAsync<ArgumentException>(() => expenses.GetReportAsync(end, start));
    }

    [Test]
    public async Task Budget_UsesMonthAndYearAndDistinguishesZeroFromNoLimit()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var category = await scope.ServiceProvider.GetRequiredService<CategoryService>().AddAsync("Food");
        var expenses = scope.ServiceProvider.GetRequiredService<ExpenseService>();
        var budgets = scope.ServiceProvider.GetRequiredService<BudgetService>();
        var date = new DateOnly(2026, 10, 1);
        await expenses.AddAsync(10m, category.Id, date);
        await expenses.AddAsync(100m, category.Id, new DateOnly(2025, 10, 1));
        await expenses.AddAsync(100m, category.Id, date.AddDays(-1));
        var unlimited = await budgets.GetStatusAsync(category.Id, date);
        Assert.That(unlimited.Limit, Is.Null);
        Assert.That(unlimited.Overspend, Is.Zero);
        await budgets.SetMonthlyLimitAsync(category.Id, date, 0m);
        Assert.That((await budgets.GetStatusAsync(category.Id, date)).Overspend, Is.EqualTo(10m));
        await budgets.SetMonthlyLimitAsync(category.Id, date.AddDays(14), 5m);
        await budgets.SetMonthlyLimitAsync(category.Id, date.AddMonths(1), 20m);
        Assert.ThrowsAsync<ArgumentException>(() => budgets.SetMonthlyLimitAsync(category.Id, date, 1.234m));
        Assert.That((await budgets.GetStatusAsync(category.Id, date)).Limit, Is.EqualTo(5m));
        var statuses = await budgets.GetMonthlyStatusesAsync(date);
        Assert.That(statuses, Has.Count.EqualTo(1));
        Assert.That(statuses.Single().Spent, Is.EqualTo(10m));
        Assert.That((await budgets.GetStatusAsync(category.Id, date.AddMonths(1))).Limit, Is.EqualTo(20m));
    }

    [Test]
    public async Task Expenses_RejectFutureDatesAndMissingCategoriesWithoutWriting()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var expenses = scope.ServiceProvider.GetRequiredService<ExpenseService>();
        var category = await scope.ServiceProvider.GetRequiredService<CategoryService>().AddAsync("Food");
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => expenses.AddAsync(1m, category.Id, expenses.Today.AddDays(1)));
        Assert.ThrowsAsync<InvalidOperationException>(() => expenses.AddAsync(1m, Guid.NewGuid(), expenses.Today));
        Assert.That((await expenses.GetReportAsync()).Expenses, Is.Empty);
    }

    [Test]
    public async Task Report_CanSumMoreThanInt64CentsWithoutLosingPrecision()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var category = await scope.ServiceProvider.GetRequiredService<CategoryService>().AddAsync("Large amounts");
        var expenses = scope.ServiceProvider.GetRequiredService<ExpenseService>();
        var maximum = long.MaxValue / 100m;
        await expenses.AddAsync(maximum, category.Id, expenses.Today);
        await expenses.AddAsync(maximum, category.Id, expenses.Today);
        Assert.That((await expenses.GetReportAsync()).Total, Is.EqualTo(maximum * 2m));
    }

    [Test]
    public async Task Migration_ModelMatchesSnapshotAndOnlyApplicationTablesExist()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ExpenseTrackerDbContext>();
        Assert.That(context.Database.HasPendingModelChanges(), Is.False);
        Assert.That(
        await context.Database.GetAppliedMigrationsAsync(),
    Is.EquivalentTo(context.Database.GetMigrations()));
        await using var connection = new SqliteConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE '__EF%' AND name NOT LIKE 'sqlite_%' ORDER BY name";
        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        Assert.That(tables, Is.EqualTo(new[]
        {
            "BudgetLimits",
            "Categories",
            "Expenses",
            "RecurringExpenses"
        }));
    }
}
