namespace ExpenseTracker.App.ViewModels;

using System.Text;
using CLI_Budget_Expense_Tracker.Core.Interfaces;
using CLI_Budget_Expense_Tracker.Core.Models;
using CLI_Budget_Expense_Tracker.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using Services;

public sealed class BankImportViewModel : ObservableObject
{
    private readonly IBankStatementReader _reader;
    private readonly IBankStatementClassifier _classifier;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInitializer _initializer;
    private readonly ILogger<BankImportViewModel> _logger;

    private bool _loaded;
    private bool _imported;
    private Category? _selectedCategory;

    public BankImportViewModel(
        IBankStatementReader reader,
        IBankStatementClassifier classifier,
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        ILogger<BankImportViewModel> logger)
    {
        _reader = reader;
        _classifier = classifier;
        _scopeFactory = scopeFactory;
        _initializer = initializer;
        _logger = logger;
    }

    public IReadOnlyList<BankImportListItem> Items { get; private set; } = [];

    public IReadOnlyList<Category> Categories { get; private set; } = [];

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
                OnPropertyChanged(nameof(CanImport));
        }
    }

    public string FileName { get; private set; } = "";

    public string Summary { get; private set; } = "";

    public decimal CandidateTotal { get; private set; }

    public string TotalText =>
        $"Expense candidates: {CandidateTotal:N2} EUR (fees excluded)";

    public string SelectionText
    {
        get
        {
            var selected = Items.Where(x => x.IsSelected).ToArray();
            var total = selected.Sum(x => -x.Entry.Source.Amount);

            return $"Selected: {selected.Length} · {total:N2} EUR";
        }
    }

    public string ResultMessage { get; private set; } = "";

    public string ErrorMessage { get; private set; } = "";

    public bool IsBusy { get; private set; }

    public bool IsIdle => !IsBusy;

    public bool HasError => ErrorMessage.Length > 0;

    public bool HasResult => ResultMessage.Length > 0;

    public bool ShowPreview => _loaded && !IsBusy && !HasError;

    public bool NeedsCategory => ShowPreview && Categories.Count == 0;

    public bool CanImport =>
        ShowPreview
        && !_imported
        && SelectedCategory is not null
        && Items.Any(x => x.IsSelected);

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

            var result = await Task.Run(async () =>
            {
                var source = _reader.Read(stream);
                var entries = _classifier.Classify(source, "EUR");
                var hashes = BankImportHasher.ComputeAll(source);

                var categories = await LoadCategoriesAsync();

                return (Entries: entries, Hashes: hashes, Categories: categories);
            });

            var entries = result.Entries;

            Items = entries
                .Select(x => new BankImportListItem(
                    x,
                    result.Hashes[x.Source.RowNumber],
                    NotifySelection))
                .ToArray();

            CandidateTotal = entries
                .Where(x => x.Kind == BankEntryKind.ExpenseCandidate)
                .Sum(x => -x.Source.Amount);

            int Count(BankEntryKind kind) =>
                entries.Count(x => x.Kind == kind);

            Summary =
                $"Records: {entries.Count} · Expense candidates: {Count(BankEntryKind.ExpenseCandidate)}\n" +
                $"Incoming: {Count(BankEntryKind.Incoming)} · Internal transfers: {Count(BankEntryKind.InternalTransfer)}\n" +
                $"Needs review: {Count(BankEntryKind.NeedsReview)} · Ignored: {Count(BankEntryKind.Ignored)}";

            SetCategories(result.Categories);
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

    public async Task ImportAsync()
    {
        if (!CanImport || SelectedCategory is not { } category) return;

        var candidates = Items
            .Where(x => x.IsSelected)
            .Select(x => new BankImportCandidate(x.Entry, x.Hash, category.Id))
            .ToArray();

        IsBusy = true;
        ErrorMessage = "";
        ResultMessage = "";
        NotifyState();

        try
        {
            var result = await Task.Run(async () =>
            {
                await _initializer.InitializeAsync();

                await using var scope = _scopeFactory.CreateAsyncScope();

                return await scope.ServiceProvider
                    .GetRequiredService<BankImportService>()
                    .ImportAsync(candidates);
            });

            ResultMessage = Describe(result);

            // Importing again is harmless (duplicates are skipped) but pointless
            // until another file is loaded.
            _imported = true;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to import a Revolut statement.");

            ErrorMessage =
                "Could not save the imported expenses. Nothing was added; try again.";
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

    private static string Describe(BankImportResult result)
    {
        var text = new StringBuilder(
            $"Added: {result.Added} · Skipped as duplicates: {result.Duplicates} · Not imported: {result.Failures.Count}");

        foreach (var failure in result.Failures)
            text.Append($"\nRow {failure.RowNumber}: {failure.Reason}");

        return text.ToString();
    }

    private async Task<IReadOnlyList<Category>> LoadCategoriesAsync()
    {
        await _initializer.InitializeAsync();

        await using var scope = _scopeFactory.CreateAsyncScope();

        return await scope.ServiceProvider
            .GetRequiredService<CategoryService>()
            .GetAllAsync();
    }

    private void SetCategories(IReadOnlyList<Category> categories)
    {
        Categories = categories;
        SelectedCategory = categories.Count > 0 ? categories[0] : null;
        OnPropertyChanged(nameof(Categories));
    }

    private void ClearPreview()
    {
        Items = [];
        Categories = [];
        SelectedCategory = null;
        CandidateTotal = 0;
        Summary = "";
        ResultMessage = "";
        _loaded = false;
        _imported = false;
    }

    private void NotifySelection()
    {
        OnPropertyChanged(nameof(SelectionText));
        OnPropertyChanged(nameof(CanImport));
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(CandidateTotal));
        OnPropertyChanged(nameof(SelectionText));
        OnPropertyChanged(nameof(ResultMessage));
        OnPropertyChanged(nameof(HasResult));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ShowPreview));
        OnPropertyChanged(nameof(NeedsCategory));
        OnPropertyChanged(nameof(CanImport));
    }
}
