namespace ExpenseTracker.App.ViewModels;

using System.Text;
using CLI_Budget_Expense_Tracker.Core.Interfaces;
using CLI_Budget_Expense_Tracker.Core.Models;
using CLI_Budget_Expense_Tracker.Core.Services;
using CLI_Budget_Expense_Tracker.Infrastructure.Importing;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using Services;

public sealed class BankImportViewModel : ObservableObject
{
    private readonly IBankStatementReader _reader;
    private readonly IBankStatementClassifier _classifier;
    private readonly GenericCsvReader _genericReader;
    private readonly GenericStatementClassifier _genericClassifier;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInitializer _initializer;
    private readonly ILogger<BankImportViewModel> _logger;

    private readonly AppSettings _settings;

    private bool _loaded;
    private bool _imported;
    private Category? _selectedCategory;
    private ImportFormatOption _selectedFormat;
    private byte[]? _customBytes;
    private string _currency;

    public BankImportViewModel(
        IBankStatementReader reader,
        IBankStatementClassifier classifier,
        GenericCsvReader genericReader,
        GenericStatementClassifier genericClassifier,
        AppSettings settings,
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        ILogger<BankImportViewModel> logger)
    {
        _settings = settings;
        _currency = settings.Currency;
        _reader = reader;
        _classifier = classifier;
        _genericReader = genericReader;
        _genericClassifier = genericClassifier;
        _selectedFormat = Formats[0];
        _scopeFactory = scopeFactory;
        _initializer = initializer;
        _logger = logger;
    }

    public IReadOnlyList<ImportFormatOption> Formats { get; } =
    [
        new(ImportFormat.Revolut, Loc.T("Revolut (German column names)")),
        new(ImportFormat.Custom, Loc.T("Other bank CSV (choose the columns)"))
    ];

    public ImportFormatOption SelectedFormat
    {
        get => _selectedFormat;
        set
        {
            if (value is null || !SetProperty(ref _selectedFormat, value)) return;

            // A preview from the other format would be misleading.
            ClearPreview();
            _customBytes = null;
            FileName = "";
            ErrorMessage = "";
            NotifyState();
        }
    }

    public bool IsCustomFormat => SelectedFormat.Format == ImportFormat.Custom;

    public CustomCsvSettings Custom { get; } = new();

    public bool ShowMapping => IsCustomFormat && _customBytes is not null && !IsBusy;

    public string FormatHint => IsCustomFormat
        ? Loc.T("Choose a CSV file, tell the app how it is written, then press Show preview.")
        : Loc.F("Revolut CSV with German column names · {0}", _settings.Currency);

    public IReadOnlyList<BankImportListItem> Items { get; private set; } = [];

    public IReadOnlyList<Category> Categories { get; private set; } = [];

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (!SetProperty(ref _selectedCategory, value)) return;

            foreach (var item in Items)
                item.UseDefaultCategory(value);

            OnPropertyChanged(nameof(CanImport));
        }
    }

    public string FileName { get; private set; } = "";

    public string Summary { get; private set; } = "";

    public decimal CandidateTotal { get; private set; }

    public string TotalText =>
        Loc.F("Expense candidates: {0:N2} {1}", CandidateTotal, _currency) + (IsCustomFormat ? "" : Loc.T(" (fees excluded)"));

    public string SelectionText
    {
        get
        {
            var selected = Items.Where(x => x.IsSelected).ToArray();
            var total = selected.Sum(x => -x.Entry.Source.Amount);

            return Loc.F("Selected: {0} · {1:N2} {2}", selected.Length, total, _currency);
        }
    }

    public bool NoPreview => !ShowPreview;

    public bool HasFile => FileName.Length > 0;

    public bool NoFile => !HasFile;

    public string FileInfoText => Items.Count > 0 ? Loc.F("{0} records", Items.Count) : "";

    private int MatchedCount => Items.Count(x => x.HasRuleNote);

    private int NeedCategoryCount => Items.Count(x => x.NeedsCategoryMark);

    private int SkippedCount => Items.Count(x => !x.CanSelect);

    public string MatchedText => Loc.F("{0} matched by rules", MatchedCount);

    public bool HasMatched => MatchedCount > 0;

    public string NeedsCategoryText => Loc.F("{0} use the default category", NeedCategoryCount);

    public bool HasNeedsCategory => NeedCategoryCount > 0;

    public string SkippedText => Loc.F("{0} skipped", SkippedCount);

    public bool HasSkipped => SkippedCount > 0;

    public string SelectedCountText => Loc.F("{0} of {1} selected", Items.Count(x => x.IsSelected), Items.Count);

    public string SelectedTotalText => $"· {Items.Where(x => x.IsSelected).Sum(x => -x.Entry.Source.Amount):N2} {_currency}";

    public string ImportButtonText => Loc.F("Import {0} records", Items.Count(x => x.IsSelected));

    /// <summary>The header box of the table: true when every record that can be imported is ticked.</summary>
    public bool AllSelected
    {
        get
        {
            var selectable = Items.Where(x => x.CanSelect).ToArray();
            return selectable.Length > 0 && selectable.All(x => x.IsSelected);
        }
        set
        {
            foreach (var item in Items.Where(x => x.CanSelect))
                item.IsSelected = value;

            NotifySelection();
        }
    }

    public IReadOnlyList<ImportStep> Steps
    {
        get
        {
            var step = _imported ? 4 : ShowPreview ? 2 : 1;

            string State(int number) => number < step ? "Done" : number == step ? "Current" : "Todo";

            return
            [
                new("1", Loc.T("Choose format and file"), Loc.T("Pick your bank's format, then select the CSV export."), State(1)),
                new("2", Loc.T("Check the records"), Loc.T("Untick what you don't need. Rules fill in categories; the rest use the default until you change them."), State(2)),
                new("3", Loc.T("Import selected"), Loc.T("Duplicates and incoming payments are skipped automatically."), State(3))
            ];
        }
    }

    /// <summary>Throws the preview away and goes back to choosing a file.</summary>
    public void Cancel()
    {
        if (IsBusy) return;

        ClearPreview();
        _customBytes = null;
        FileName = "";
        ErrorMessage = "";
        NotifyState();
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
        && Items.Any(x => x.IsSelected)
        && Items.Where(x => x.IsSelected).All(x => x.Category is not null);

    public Task LoadAsync(
        string fileName,
        Func<Task<Stream>> openReadAsync) =>
        LoadCoreAsync(
            fileName,
            async () =>
            {
                await using var stream = await openReadAsync();

                return await Task.Run(() => _reader.Read(stream));
            },
            _classifier,
            _settings.Currency);

    /// <summary>Opens a CSV of another bank and offers its columns for mapping.</summary>
    public async Task LoadCustomFileAsync(
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
            byte[] bytes;

            await using (var stream = await openReadAsync())
            await using (var memory = new MemoryStream())
            {
                await stream.CopyToAsync(memory);
                bytes = memory.ToArray();
            }

            var found = await Task.Run(() =>
            {
                var delimiter = GenericCsvReader.DetectDelimiter(bytes);

                return (Delimiter: delimiter, Headers: _genericReader.ReadHeaders(bytes, delimiter));
            });

            _customBytes = bytes;

            Custom.UseDelimiter(found.Delimiter);
            Custom.SetHeaders(found.Headers);

            if (_settings.LastCsvProfile is { } saved)
                Custom.ApplySaved(saved);

            // Amounts are labeled with the app currency unless the file has its own column.
            Custom.DefaultCurrency = _settings.Currency;
        }
        catch (Exception exception)
        {
            _customBytes = null;

            _logger.LogError(exception, "Failed to open a CSV file.");

            ErrorMessage = exception is FormatException
                ? exception.Message
                : Loc.T("Could not read the file. Check it and try again.");
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    /// <summary>Reads the column names again after the delimiter was changed.</summary>
    public void RefreshCustomHeaders()
    {
        if (_customBytes is not { } bytes || IsBusy) return;

        try
        {
            Custom.SetHeaders(_genericReader.ReadHeaders(bytes, Custom.SelectedDelimiter.Value));
            ErrorMessage = "";
        }
        catch (FormatException exception)
        {
            ErrorMessage = exception.Message;
        }

        NotifyState();
    }

    public async Task PreviewCustomAsync()
    {
        if (IsBusy || _customBytes is not { } bytes) return;

        if (!Custom.TryBuildProfile(out var profile, out var error))
        {
            ErrorMessage = error;
            NotifyState();
            return;
        }

        try
        {
            _settings.SetLastCsvProfile(profile);
        }
        catch (Exception exception)
        {
            // Remembering the last choices is a convenience only.
            _logger.LogWarning(exception, "Could not remember the CSV column choices.");
        }

        await LoadCoreAsync(
            FileName,
            () => Task.Run(() => _genericReader.Read(bytes, profile)),
            _genericClassifier,
            profile.DefaultCurrency);
    }

    private async Task LoadCoreAsync(
        string fileName,
        Func<Task<IReadOnlyList<BankStatementEntry>>> readAsync,
        IBankStatementClassifier classifier,
        string currency)
    {
        if (IsBusy) return;

        ClearPreview();
        FileName = fileName;
        ErrorMessage = "";
        _currency = currency;
        IsBusy = true;
        NotifyState();

        try
        {
            var source = await readAsync();

            var result = await Task.Run(async () =>
            {
                var entries = classifier.Classify(source, currency);
                var hashes = BankImportHasher.ComputeAll(source);

                var categories = await LoadCategoriesAsync();
                var rules = await LoadRulesAsync();

                return (Entries: entries, Hashes: hashes, Categories: categories, Rules: rules);
            });

            var entries = result.Entries;

            Items = entries
                .Select(x => new BankImportListItem(
                    x,
                    result.Hashes[x.Source.RowNumber],
                    result.Categories,
                    NotifySelection))
                .ToArray();

            var categoriesById = result.Categories.ToDictionary(x => x.Id);
            var matched = 0;

            foreach (var item in Items.Where(x => x.CanSelect))
            {
                var rule = CategoryRuleMatcher.FindMatch(result.Rules, item.Description);

                if (rule is not null
                    && categoriesById.TryGetValue(rule.CategoryId, out var ruleCategory))
                {
                    item.ApplyRule(ruleCategory, rule.Pattern);
                    matched++;
                }
            }

            CandidateTotal = entries
                .Where(x => x.Kind == BankEntryKind.ExpenseCandidate)
                .Sum(x => -x.Source.Amount);

            int Count(BankEntryKind kind) =>
                entries.Count(x => x.Kind == kind);

            Summary =
                Loc.F("Records: {0} · Expense candidates: {1}", entries.Count, Count(BankEntryKind.ExpenseCandidate)) + "\n" +
                Loc.F("Incoming: {0} · Internal transfers: {1}", Count(BankEntryKind.Incoming), Count(BankEntryKind.InternalTransfer)) + "\n" +
                Loc.F("Needs review: {0} · Ignored: {1}", Count(BankEntryKind.NeedsReview), Count(BankEntryKind.Ignored)) + "\n" +
                Loc.F("Category set by rules: {0}", matched);

            SetCategories(result.Categories);
            _loaded = true;
        }
        catch (Exception exception)
        {
            ClearPreview();

            _logger.LogError(
                exception,
                "Failed to preview a bank statement.");

            ErrorMessage = exception is FormatException
                ? exception.Message
                : Loc.T("Could not read the statement. Check the file and try again.");
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    public async Task ImportAsync()
    {
        if (!CanImport) return;

        var candidates = Items
            .Where(x => x.IsSelected)
            .Select(x => new BankImportCandidate(x.Entry, x.Hash, x.Category!.Id))
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
                Loc.T("Could not save the imported expenses. Nothing was added; try again.");
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

        ErrorMessage = Loc.T("Could not open the file picker. Try again.");
        NotifyState();
    }

    private static string Describe(BankImportResult result)
    {
        var text = new StringBuilder(
            Loc.F("Added: {0} · Skipped as duplicates: {1} · Not imported: {2}", result.Added, result.Duplicates, result.Failures.Count));

        foreach (var failure in result.Failures)
            text.Append("\n" + Loc.F("Row {0}: {1}", failure.RowNumber, failure.Reason));

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

    private async Task<IReadOnlyList<CategoryRule>> LoadRulesAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        return await scope.ServiceProvider
            .GetRequiredService<CategoryRuleService>()
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
        OnPropertyChanged(nameof(MatchedText));
        OnPropertyChanged(nameof(HasMatched));
        OnPropertyChanged(nameof(NeedsCategoryText));
        OnPropertyChanged(nameof(HasNeedsCategory));
        OnPropertyChanged(nameof(SelectedCountText));
        OnPropertyChanged(nameof(SelectedTotalText));
        OnPropertyChanged(nameof(ImportButtonText));
        OnPropertyChanged(nameof(AllSelected));
    }

    private void NotifyState()
    {
        NotifySelection();

        OnPropertyChanged(nameof(HasFile));
        OnPropertyChanged(nameof(NoPreview));
        OnPropertyChanged(nameof(NoFile));
        OnPropertyChanged(nameof(FileInfoText));
        OnPropertyChanged(nameof(SkippedText));
        OnPropertyChanged(nameof(HasSkipped));
        OnPropertyChanged(nameof(Steps));
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
        OnPropertyChanged(nameof(IsCustomFormat));
        OnPropertyChanged(nameof(ShowMapping));
        OnPropertyChanged(nameof(FormatHint));
    }
}
