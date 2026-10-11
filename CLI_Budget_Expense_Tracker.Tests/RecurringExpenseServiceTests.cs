namespace CLI_Budget_Expense_Tracker.Test;

using Core.Interfaces;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public sealed class RecurringExpenseServiceTests
{
    private SqliteTestDatabase _database = null!;
    private Guid _categoryId;

    [SetUp]
    public async Task SetUp()
    {
        _database = new SqliteTestDatabase();
        await _database.InitializeAsync();

        await using var scope = _database.Services.CreateAsyncScope();

        var category = await scope.ServiceProvider
            .GetRequiredService<CategoryService>()
            .AddAsync("Subscriptions");

        _categoryId = category.Id;
    }

    [TearDown]
    public async Task TearDown()
    {
        await _database.DisposeAsync();
    }

    [Test]
    public async Task AddAsync_PersistsScheduleAcrossScopes()
    {
        var startDate = new DateOnly(2026, 10, 1);
        Guid scheduleId;

        await using (var scope = _database.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider
                .GetRequiredService<RecurringExpenseService>();

            var schedule = await AddScheduleAsync(service, startDate);
            scheduleId = schedule.Id;
        }

        await using var readScope = _database.Services.CreateAsyncScope();

        var saved = (await readScope.ServiceProvider
            .GetRequiredService<RecurringExpenseService>()
            .GetAllAsync()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(saved.Id, Is.EqualTo(scheduleId));
            Assert.That(saved.CategoryId, Is.EqualTo(_categoryId));
            Assert.That(saved.Amount, Is.EqualTo(15.99m));
            Assert.That(saved.Description, Is.EqualTo("Subscription"));
            Assert.That(
                saved.Frequency,
                Is.EqualTo(RecurrenceFrequency.Monthly));
            Assert.That(saved.StartDate, Is.EqualTo(startDate));
            Assert.That(saved.NextDueDate, Is.EqualTo(startDate));
            Assert.That(saved.IsActive, Is.True);
        });

        var report = await readScope.ServiceProvider
            .GetRequiredService<ExpenseService>()
            .GetReportAsync();

        Assert.That(report.Expenses, Is.Empty);
    }

    [Test]
    public async Task DeactivateAsync_PersistsAndCanBeRepeated()
    {
        Guid scheduleId;

        await using (var scope = _database.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider
                .GetRequiredService<RecurringExpenseService>();

            var schedule = await AddScheduleAsync(
                service,
                new DateOnly(2026, 10, 1));

            scheduleId = schedule.Id;
            await service.DeactivateAsync(scheduleId);
        }

        await using var readScope = _database.Services.CreateAsyncScope();

        var savedService = readScope.ServiceProvider
            .GetRequiredService<RecurringExpenseService>();

        var saved = (await savedService.GetAllAsync()).Single();

        Assert.That(saved.Id, Is.EqualTo(scheduleId));
        Assert.That(saved.IsActive, Is.False);

        await savedService.DeactivateAsync(scheduleId);

        var schedules = await savedService.GetAllAsync();

        Assert.That(schedules, Has.Count.EqualTo(1));
        Assert.That(schedules.Single().IsActive, Is.False);
    }

    [Test]
    public async Task GetDueAsync_ReturnsOnlyActiveSchedulesThroughDate()
    {
        var throughDate = new DateOnly(2026, 10, 15);
        Guid overdueId;
        Guid dueTodayId;

        await using (var scope = _database.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider
                .GetRequiredService<RecurringExpenseService>();

            var overdue = await AddScheduleAsync(
                service,
                throughDate.AddDays(-1));

            var dueToday = await AddScheduleAsync(
                service,
                throughDate);

            await AddScheduleAsync(
                service,
                throughDate.AddDays(1));

            var inactive = await AddScheduleAsync(
                service,
                throughDate.AddDays(-2));

            await service.DeactivateAsync(inactive.Id);

            overdueId = overdue.Id;
            dueTodayId = dueToday.Id;
        }

        await using var readScope = _database.Services.CreateAsyncScope();

        var repository = readScope.ServiceProvider
            .GetRequiredService<IRecurringExpenseRepository>();

        var dueSchedules = await repository.GetDueAsync(throughDate);

        Assert.That(
            dueSchedules.Select(x => x.Id).ToArray(),
            Is.EqualTo(new[] { overdueId, dueTodayId }));
    }

    [Test]
    public async Task AddAsync_WithMissingCategory_DoesNotSaveSchedule()
    {
        await using var scope = _database.Services.CreateAsyncScope();

        var service = scope.ServiceProvider
            .GetRequiredService<RecurringExpenseService>();

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAsync(
                15.99m,
                Guid.NewGuid(),
                RecurrenceFrequency.Monthly,
                new DateOnly(2026, 10, 1),
                "Subscription"));

        Assert.That(await service.GetAllAsync(), Is.Empty);
    }

    private Task<RecurringExpense> AddScheduleAsync(
        RecurringExpenseService service,
        DateOnly startDate)
    {
        return service.AddAsync(
            15.99m,
            _categoryId,
            RecurrenceFrequency.Monthly,
            startDate,
            "Subscription");
    }
}