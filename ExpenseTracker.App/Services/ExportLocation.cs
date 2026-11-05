namespace ExpenseTracker.App.Services;

using System.Diagnostics;

/// <summary>Folder where exported files are written, next to the database.</summary>
public sealed record ExportLocation(string Directory)
{
    /// <summary>Shows the folder in the file explorer, creating it first if nothing was exported yet.</summary>
    public void Open()
    {
        System.IO.Directory.CreateDirectory(Directory);

        Process.Start(new ProcessStartInfo
        {
            FileName = Directory,
            UseShellExecute = true
        });
    }
}

public enum ExportFormat
{
    Csv,
    Markdown
}
