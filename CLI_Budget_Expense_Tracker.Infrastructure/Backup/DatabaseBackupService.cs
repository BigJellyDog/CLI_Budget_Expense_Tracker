namespace CLI_Budget_Expense_Tracker.Infrastructure.Backup;

using Microsoft.Data.Sqlite;

public sealed record BackupInfo(string FileName, DateTime CreatedAt, long SizeBytes);

/// <summary>
/// Creates, lists and restores copies of the SQLite database file.
/// Backups are consistent snapshots made with SQLite's online backup API.
/// </summary>
public sealed class DatabaseBackupService(
    string databasePath,
    string backupDirectory,
    TimeProvider timeProvider)
{
    public const string ManualPrefix = "backup";
    public const string AutomaticPrefix = "auto";

    private const string BeforeRestorePrefix = "before-restore";
    private const int MaxAutomaticBackups = 10;

    /// <summary>Creates a backup of the current database and returns its path.</summary>
    public string CreateBackup(string prefix = ManualPrefix)
    {
        if (!File.Exists(databasePath))
        {
            throw new InvalidOperationException("There is no database to back up yet.");
        }

        Directory.CreateDirectory(backupDirectory);

        var stamp = timeProvider.GetLocalNow().ToString("yyyyMMdd-HHmmss");
        var path = Path.Combine(backupDirectory, $"{prefix}-{stamp}.db");

        for (var i = 2; File.Exists(path); i++)
        {
            path = Path.Combine(backupDirectory, $"{prefix}-{stamp}-{i}.db");
        }

        var temporaryPath = path + ".tmp";
        try
        {
            using (var source = new SqliteConnection(ConnectionString(databasePath, SqliteOpenMode.ReadWrite)))
            using (var target = new SqliteConnection(ConnectionString(temporaryPath, SqliteOpenMode.ReadWriteCreate)))
            {
                source.Open();
                target.Open();
                source.BackupDatabase(target);
            }

            File.Move(temporaryPath, path);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }

        if (prefix == AutomaticPrefix) PruneAutomaticBackups();

        return path;
    }

    /// <summary>Lists existing backups, newest first.</summary>
    public IReadOnlyList<BackupInfo> List()
    {
        if (!Directory.Exists(backupDirectory)) return [];

        return new DirectoryInfo(backupDirectory)
            .EnumerateFiles("*.db")
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .ThenByDescending(x => x.Name)
            .Select(x => new BackupInfo(x.Name, x.LastWriteTime, x.Length))
            .ToArray();
    }

    /// <summary>
    /// Replaces the database with the named backup. The current database is
    /// saved first, so a restore can itself be undone.
    /// </summary>
    public void Restore(string fileName)
    {
        var source = ResolveBackup(fileName);
        ValidateBackup(source);

        if (File.Exists(databasePath)) CreateBackup(BeforeRestorePrefix);

        // Release pooled connections so the database file can be replaced.
        SqliteConnection.ClearAllPools();

        var temporaryPath = databasePath + ".restore.tmp";
        try
        {
            File.Copy(source, temporaryPath, overwrite: true);

            File.Delete(databasePath + "-wal");
            File.Delete(databasePath + "-shm");

            File.Move(temporaryPath, databasePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private string ResolveBackup(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || Path.GetFileName(fileName) != fileName
            || !fileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid backup name.", nameof(fileName));
        }

        var path = Path.Combine(backupDirectory, fileName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Backup not found.", path);
        }

        return path;
    }

    private static void ValidateBackup(string path)
    {
        using var connection = new SqliteConnection(ConnectionString(path, SqliteOpenMode.ReadOnly));
        connection.Open();

        using var integrity = connection.CreateCommand();
        integrity.CommandText = "PRAGMA integrity_check;";

        using var tables = connection.CreateCommand();
        tables.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Expenses';";

        if (!string.Equals(integrity.ExecuteScalar() as string, "ok", StringComparison.OrdinalIgnoreCase)
            || Convert.ToInt32(tables.ExecuteScalar()) == 0)
        {
            throw new InvalidOperationException("The file is not a valid expense tracker backup.");
        }
    }

    private void PruneAutomaticBackups()
    {
        foreach (var old in List()
                     .Where(x => x.FileName.StartsWith(AutomaticPrefix + "-", StringComparison.Ordinal))
                     .Skip(MaxAutomaticBackups))
        {
            File.Delete(Path.Combine(backupDirectory, old.FileName));
        }
    }

    private static string ConnectionString(string path, SqliteOpenMode mode) =>
        new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = mode,
            Pooling = false
        }.ToString();
}
