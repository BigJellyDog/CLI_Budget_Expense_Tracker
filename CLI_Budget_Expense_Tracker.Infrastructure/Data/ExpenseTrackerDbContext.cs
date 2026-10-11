namespace CLI_Budget_Expense_Tracker.Infrastructure.Data;

using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Maps expense tracker entities to the SQLite database.
/// </summary>
public sealed class ExpenseTrackerDbContext(
    DbContextOptions<ExpenseTrackerDbContext> options)
    : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<BudgetLimit> BudgetLimits => Set<BudgetLimit>();
    public DbSet<RecurringExpense> RecurringExpenses =>
        Set<RecurringExpense>();

    /// <summary>
    /// Configures tables, relationships, constraints, and money conversion.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var moneyConverter = new ValueConverter<decimal, long>(
            amount => ToCents(amount),
            cents => cents / 100m);

        // Configure categories and prevent duplicate normalized names.
        var category = modelBuilder.Entity<Category>();

        category.ToTable("Categories");
        category.HasKey(x => x.Id);
        category.Property(x => x.Id).ValueGeneratedNever();

        category.Property(x => x.Name).IsRequired();
        category.Property(x => x.NormalizedName).IsRequired();

        category.HasIndex(x => x.NormalizedName).IsUnique();

        // Configure expenses and store monetary values as integer cents.
        var expense = modelBuilder.Entity<Expense>();

        expense.ToTable("Expenses", table =>
        {
            table.HasCheckConstraint(
                "CK_Expenses_AmountInCents",
                """typeof("AmountInCents") = 'integer' AND "AmountInCents" > 0""");
        });

        expense.HasKey(x => x.Id);
        expense.Property(x => x.Id).ValueGeneratedNever();

        expense.Property(x => x.Amount)
            .HasConversion(moneyConverter)
            .HasColumnName("AmountInCents")
            .HasColumnType("INTEGER");

        expense.Property(x => x.Description).IsRequired();

        expense.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        expense.HasIndex(x => x.Date);
        expense.HasIndex(x => new { x.CategoryId, x.Date });

        expense.HasQueryFilter(x => !x.IsDeleted);
        
        expense.HasOne<RecurringExpense>()
            .WithMany()
            .HasForeignKey(x => x.RecurringExpenseId)
            .OnDelete(DeleteBehavior.Restrict);

        expense.HasIndex(x => new
            {
                x.RecurringExpenseId,
                x.Date
            })
            .IsUnique();

        // Allow only one budget per category and calendar month.
        var budget = modelBuilder.Entity<BudgetLimit>();
        
        // Configure recurring payments and their schedules.
        var recurring = modelBuilder.Entity<RecurringExpense>();

        recurring.ToTable("RecurringExpenses", table =>
        {
            table.HasCheckConstraint(
                "CK_RecurringExpenses_AmountInCents",
                """
                typeof("AmountInCents") = 'integer'
                AND "AmountInCents" > 0
                """);

            table.HasCheckConstraint(
                "CK_RecurringExpenses_Frequency",
                """
                "Frequency" IN (1, 2, 3, 4)
                """);

            table.HasCheckConstraint(
                "CK_RecurringExpenses_NextDueDate",
                """
                "NextDueDate" >= "StartDate"
                """);
        });

        recurring.HasKey(x => x.Id);
        recurring.Property(x => x.Id).ValueGeneratedNever();

        recurring.Property(x => x.Amount)
            .HasConversion(moneyConverter)
            .HasColumnName("AmountInCents")
            .HasColumnType("INTEGER");

        recurring.Property(x => x.Frequency)
            .HasConversion<int>();

        recurring.Property(x => x.Description)
            .IsRequired();

        recurring.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        recurring.HasIndex(x => new { x.IsActive, x.NextDueDate });

        budget.ToTable("BudgetLimits", table =>
        {
            table.HasCheckConstraint(
                "CK_BudgetLimits_MonthlyAmountInCents",
                """
                typeof("MonthlyAmountInCents") = 'integer'
                AND "MonthlyAmountInCents" >= 0
                """);

            table.HasCheckConstraint(
                "CK_BudgetLimits_Month",
                """substr("Month", 9, 2) = '01'""");
        });

        budget.HasKey(x => x.Id);
        budget.Property(x => x.Id).ValueGeneratedNever();

        budget.Property(x => x.MonthlyAmount)
            .HasConversion(moneyConverter)
            .HasColumnName("MonthlyAmountInCents")
            .HasColumnType("INTEGER");

        budget.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        budget.HasIndex(x => new { x.CategoryId, x.Month })
            .IsUnique();
    }
    

    /// <summary>
    /// Converts an exact monetary amount to cents and rejects overflow.
    /// </summary>
    private static long ToCents(decimal amount)
    {
        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException(
                "Amount must represent a whole number of cents.",
                nameof(amount));
        }

        return checked((long)(amount * 100m));
    }
}
