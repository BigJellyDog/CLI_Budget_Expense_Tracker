namespace CLI_Budget_Expense_Tracker.Test;

using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public sealed class RecurringExpenseGeneratorTests
{
    private SqliteTestDatabase database = null!;
    private Guid categoryId;

    [SetUp]
    public async Task SetUp()
    {
        database = new SqliteTestDatabase();
        await database.InitializeAsync();

        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ExpenseTrackerDbContext>();

        var category = new Category("Subscriptions");
        categoryId = category.Id;

        context.Categories.Add(category);
        await context.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await database.DisposeAsync();
    }

    [Test]
    public async Task GenerateAsync_CreatesMissedPaymentsAndAdvancesSchedule()
    {
        var scheduleId = await AddScheduleAsync(new DateOnly(2026, 1, 31));

        var count = await GenerateAsync(new DateOnly(2026, 3, 31));

        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ExpenseTrackerDbContext>();

        var expenses = await context.Expenses
            .OrderBy(x => x.Date)
            .ToListAsync();

        var schedule = await context.RecurringExpenses.SingleAsync();

        Assert.That(count, Is.EqualTo(3));
        Assert.That(expenses.Select(x => x.Date), Is.EqualTo(new[]
        {
            new DateOnly(2026, 1, 31),
            new DateOnly(2026, 2, 28),
            new DateOnly(2026, 3, 31)
        }));

        Assert.That(expenses.All(x =>
            x.RecurringExpenseId == scheduleId
            && x.CategoryId == categoryId
            && x.Amount == 15.99m
            && x.Description == "Subscription"), Is.True);

        Assert.That(
            schedule.NextDueDate,
            Is.EqualTo(new DateOnly(2026, 4, 30)));
    }

    [Test]
    public async Task GenerateAsync_RepeatedCallDoesNotCreateDuplicates()
    {
        await AddScheduleAsync(new DateOnly(2026, 10, 1));

        Assert.That(await GenerateAsync(), Is.EqualTo(1));
        Assert.That(await GenerateAsync(), Is.Zero);

        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ExpenseTrackerDbContext>();

        Assert.That(await context.Expenses.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task GenerateAsync_DoesNotRecreateDeletedOccurrence()
    {
        var dueDate = new DateOnly(2026, 10, 1);
        var scheduleId = await AddScheduleAsync(dueDate);

        await using (var scope = database.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ExpenseTrackerDbContext>();

            var expense = new Expense(
                Guid.NewGuid(), 15.99m, categoryId,
                dueDate, "Subscription", scheduleId);

            expense.MarkAsDeleted();
            context.Expenses.Add(expense);
            await context.SaveChangesAsync();
        }

        Assert.That(await GenerateAsync(dueDate), Is.Zero);

        await using var readScope = database.Services.CreateAsyncScope();
        var read = readScope.ServiceProvider
            .GetRequiredService<ExpenseTrackerDbContext>();

        Assert.That(await read.Expenses.CountAsync(), Is.Zero);
        Assert.That(
            await read.Expenses.IgnoreQueryFilters().CountAsync(),
            Is.EqualTo(1));

        Assert.That(
            (await read.RecurringExpenses.SingleAsync()).NextDueDate,
            Is.EqualTo(new DateOnly(2026, 11, 1)));
    }

    [Test]
    public async Task GenerateAsync_SkipsInactiveAndFutureSchedules()
    {
        await AddScheduleAsync(new DateOnly(2026, 10, 16));
        await AddScheduleAsync(new DateOnly(2026, 10, 1), active: false);

        Assert.That(await GenerateAsync(), Is.Zero);
    }

    [Test]
    public void GenerateAsync_RejectsFutureCutoff()
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            GenerateAsync(new DateOnly(2026, 10, 16)));
    }

    [Test]
    public async Task GenerateAsync_FailedSaveRollsBackExpensesAndSchedule()
    {
        var startDate = new DateOnly(2026, 1, 31);
        await AddScheduleAsync(startDate);

        await using (var scope = database.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ExpenseTrackerDbContext>();

            // Fail the second insert to verify that earlier changes roll back.
            await context.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER fail_second_expense
                BEFORE INSERT ON Expenses
                WHEN (SELECT COUNT(*) FROM Expenses) = 1
                BEGIN
                    SELECT RAISE(ABORT, 'Forced second insert failure');
                END;
                """);
        }

        Assert.ThrowsAsync<DbUpdateException>(() =>
            GenerateAsync(new DateOnly(2026, 3, 31)));

        await using var readScope = database.Services.CreateAsyncScope();
        var read = readScope.ServiceProvider
            .GetRequiredService<ExpenseTrackerDbContext>();

        Assert.That(await read.Expenses.CountAsync(), Is.Zero);
        Assert.That(
            (await read.RecurringExpenses.SingleAsync()).NextDueDate,
            Is.EqualTo(startDate));
    }

    private async Task<Guid> AddScheduleAsync(
        DateOnly startDate,
        bool active = true)
    {
        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ExpenseTrackerDbContext>();

        var schedule = new RecurringExpense(
            Guid.NewGuid(), 15.99m, categoryId,
            RecurrenceFrequency.Monthly, startDate, "Subscription");

        if (!active)
        {
            schedule.Deactivate();
        }

        context.RecurringExpenses.Add(schedule);
        await context.SaveChangesAsync();

        return schedule.Id;
    }

    private async Task<int> GenerateAsync(DateOnly? throughDate = null)
    {
        await using var scope = database.Services.CreateAsyncScope();

        return await scope.ServiceProvider
            .GetRequiredService<IRecurringExpenseGenerator>()
            .GenerateAsync(throughDate);
    }
}