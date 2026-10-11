namespace CLI_Budget_Expense_Tracker.Core.Models;

/// <summary>Rules shared by amounts stored as integer cents.</summary>
internal static class MoneyRules
{
    public static void Validate(decimal amount, string parameterName, bool allowZero)
    {
        if (amount < 0 || (!allowZero && amount == 0) || amount > long.MaxValue / 100m)
        {
            throw new ArgumentOutOfRangeException(parameterName, amount,
                allowZero
                    ? "Amount must be non-negative and fit into a 64-bit integer in cents."
                    : "Amount must be positive and fit into a 64-bit integer in cents.");
        }
        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException("Amount must represent a whole number of cents.", parameterName);
    }
}
