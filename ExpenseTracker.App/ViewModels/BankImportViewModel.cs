namespace ExpenseTracker.App.ViewModels;

using CLI_Budget_Expense_Tracker.Core.Interfaces;
using CLI_Budget_Expense_Tracker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Models;

public sealed class BankImportViewModel : ObservableObject
{
    private readonly IBankStatementReader _reader;
    private readonly IBankStatementClassifier _classifier;
    private readonly ILogger<BankImportViewModel> _logger;

    private bool _loaded;

    public BankImportViewModel(
        IBankStatementReader reader,
        IBankStatementClassifier classifier,
        ILogger<BankImportViewModel> logger)
    {
        _reader = reader;
        _classifier = classifier;
        _logger = logger;
    }

    public IReadOnlyList<BankImportListItem> Items { get; private set; } = [];

    public string FileName { get; private set; } = "";

    public string Summary { get; private set; } = "";

    public decimal CandidateTotal { get; private set; }

    public string TotalText =>
        $"Expense candidates: {CandidateTotal:N2} EUR (fees excluded)";

    public string ErrorMessage { get; private set; } = "";

    public bool IsBusy { get; private set; }

    public bool IsIdle => !IsBusy;

    public bool HasError => ErrorMessage.Length > 0;

    public bool ShowPreview => _loaded && !IsBusy && !HasError;

    public async Task LoadAsync(
        string fileName,
        Func<Task<Stream>> openReadAsync)
    {
        if (IsBusy) return;

        ClearPreview();
        FileName = fileName;
        ErrorMessage = "";
        IsBusy = true;
        NotifyState();

        try
        {
            await using var stream = await openReadAsync();

            var result = await Task.Run(() =>
            {
                var entries = _classifier.Classify(
                    _reader.Read(stream), "EUR");

                var rows = entries
                    .Select(x => new BankImportListItem(x))
                    .ToArray();

                var total = entries
                    .Where(x => x.Kind == BankEntryKind.ExpenseCandidate)
                    .Sum(x => -x.Source.Amount);

                int Count(BankEntryKind kind) =>
                    entries.Count(x => x.Kind == kind);

                var summary =
                    $"Records: {entries.Count} · Expense candidates: {Count(BankEntryKind.ExpenseCandidate)}\n" +
                    $"Incoming: {Count(BankEntryKind.Incoming)} · Internal transfers: {Count(BankEntryKind.InternalTransfer)}\n" +
                    $"Needs review: {Count(BankEntryKind.NeedsReview)} · Ignored: {Count(BankEntryKind.Ignored)}";

                return (Rows: rows, Total: total, Summary: summary);
            });

            Items = result.Rows;
            CandidateTotal = result.Total;
            Summary = result.Summary;
            _loaded = true;
        }
        catch (Exception exception)
        {
            ClearPreview();

            _logger.LogError(
                exception,
                "Failed to preview a Revolut statement.");

            ErrorMessage = exception is FormatException
                ? exception.Message
                : "Could not read the statement. Check the file and try again.";
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    public void ReportFileError(Exception exception)
    {
        ClearPreview();
        FileName = "";

        _logger.LogError(
            exception,
            "Failed to choose a statement file.");

        ErrorMessage = "Could not open the file picker. Try again.";
        NotifyState();
    }

    private void ClearPreview()
    {
        Items = [];
        CandidateTotal = 0;
        Summary = "";
        _loaded = false;
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(CandidateTotal));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ShowPreview));
    }
}