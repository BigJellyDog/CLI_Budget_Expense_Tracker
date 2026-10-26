namespace ExpenseTracker.App.Services;

/// <summary>Folder where exported files are written, next to the database.</summary>
public sealed record ExportLocation(string Directory);

public enum ExportFormat
{
    Csv,
    Markdown
}
