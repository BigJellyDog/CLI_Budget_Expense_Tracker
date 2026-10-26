namespace ExpenseTracker.App.Models;

using CLI_Budget_Expense_Tracker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

public sealed class BankImportListItem : ObservableObject
{
    private readonly Action _selectionChanged;
    private bool _isSelected;
    private Category? _category;
    private bool _categoryChosen;

    public BankImportListItem(
        ClassifiedBankEntry entry,
        string hash,
        IReadOnlyList<Category> categories,
        Action selectionChanged)
    {
        Entry = entry;
        Hash = hash;
        Categories = categories;
        _selectionChanged = selectionChanged;

        // Only outgoing payments can become expenses.
        CanSelect = entry.Source.Amount < 0
            && entry.Kind is BankEntryKind.ExpenseCandidate
                or BankEntryKind.NeedsReview;

        _isSelected = entry.Kind == BankEntryKind.ExpenseCandidate
            && CanSelect;
    }

    public ClassifiedBankEntry Entry { get; }

    public string Hash { get; }

    public bool CanSelect { get; }

    public IReadOnlyList<Category> Categories { get; }

    /// <summary>The category this record is imported into.</summary>
    public Category? Category
    {
        get => _category;
        set
        {
            // A picker can briefly write null while its list is being filled.
            if (value is null || !SetProperty(ref _category, value)) return;

            _categoryChosen = true;
            ClearRuleNote();
            _selectionChanged();
        }
    }

    /// <summary>Says which rule picked the category, until the user picks another one.</summary>
    public string RuleNote { get; private set; } = "";

    public bool HasRuleNote => RuleNote.Length > 0;

    /// <summary>Uses the category of a matching auto-categorization rule.</summary>
    public void ApplyRule(Category category, string pattern)
    {
        SetProperty(ref _category, category, nameof(Category));
        _categoryChosen = true;

        RuleNote = $"Category set by rule: contains \"{pattern}\"";
        OnPropertyChanged(nameof(RuleNote));
        OnPropertyChanged(nameof(HasRuleNote));
    }

    private void ClearRuleNote()
    {
        if (RuleNote.Length == 0) return;

        RuleNote = "";
        OnPropertyChanged(nameof(RuleNote));
        OnPropertyChanged(nameof(HasRuleNote));
    }

    /// <summary>Follows the default category unless this record already has its own.</summary>
    public void UseDefaultCategory(Category? category)
    {
        if (_categoryChosen) return;

        SetProperty(ref _category, category, nameof(Category));
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!CanSelect || _isSelected == value) return;

            _isSelected = value;
            OnPropertyChanged();
            _selectionChanged();
        }
    }

    public string Kind => Entry.Kind switch
    {
        BankEntryKind.ExpenseCandidate => "Expense candidate",
        BankEntryKind.Incoming => "Incoming",
        BankEntryKind.InternalTransfer => "Internal transfer",
        BankEntryKind.Ignored => "Ignored",
        _ => "Needs review"
    };

    public string Description => Entry.Source.Description;

    public string Amount =>
        $"{Entry.Source.Amount:N2} {Entry.Source.Currency}";

    public string Date => Entry.Source.CompletedAt is { } completedAt
        ? $"Completed: {completedAt:dd.MM.yyyy HH:mm}"
        : "Not completed";

    public string Details =>
        $"Row {Entry.Source.RowNumber} · {Entry.Source.Type} · " +
        $"{Entry.Source.Product} · {Entry.Source.Status} · " +
        $"Fee: {Entry.Source.Fee:N2} {Entry.Source.Currency}";

    public string Reason => Entry.Reason;
}
