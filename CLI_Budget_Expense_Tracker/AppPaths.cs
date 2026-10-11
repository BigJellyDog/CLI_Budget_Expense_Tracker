namespace CLI_Budget_Expense_Tracker;

/// <summary>Uses a stable user-data directory, independent of the working directory or build output.</summary>
public sealed record AppPaths(string DataDirectory)
{
    public string DatabaseFile => Path.Combine(DataDirectory, "expense-tracker.db");
    public string ExportDirectory => Path.Combine(DataDirectory, "Exports");

    public static AppPaths FromArguments(string[] args)
    {
        if (args.Length == 2 && args[0] == "--data-dir" && !string.IsNullOrWhiteSpace(args[1]))
            return new AppPaths(Path.GetFullPath(args[1]));
        if (args.Length != 0)
            throw new ArgumentException(
                "Usage: ExpenseTracker [generate] [--data-dir <directory>]");

        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localData))
            throw new InvalidOperationException("User data directory is unavailable. Specify --data-dir <directory>.");
        return new AppPaths(Path.Combine(localData, "CLI_Budget_Expense_Tracker"));
    }
}
