namespace ExpenseTracker.App.Views;

using ExpenseTracker.App.Controls;
using ExpenseTracker.App.Services;

using Models;
using ViewModels;

public partial class BudgetsPage : ContentPage
{
    private readonly BudgetsViewModel _viewModel;

    public BudgetsPage(BudgetsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    protected override bool OnBackButtonPressed() =>
        _viewModel.IsBusy || base.OnBackButtonPressed();

    // As many cards in a row as fit, each at least 340 wide.
    private void OnCardsSizeChanged(object? sender, EventArgs e)
    {
        if (Cards.ItemsLayout is GridItemsLayout layout && Cards.Width > 0)
            layout.Span = Math.Max(1, (int)((Cards.Width + 16) / (340 + 16)));
    }

    private async void OnPreviousClicked(object? sender, EventArgs e) =>
        await _viewModel.MoveMonthAsync(-1);

    private async void OnNextClicked(object? sender, EventArgs e) =>
        await _viewModel.MoveMonthAsync(1);

    private async void OnMonthPicked(object? sender, DateTime month) =>
        await _viewModel.SetMonthAsync(month);

    private async void OnRefreshClicked(object? sender, EventArgs e) =>
        await _viewModel.LoadAsync();

    private async void OnSaveClicked(object? sender, EventArgs e) =>
        await _viewModel.SaveAsync();

    // The pencil and "Set limit" both put the category into the form.
    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanEdit
            || sender is not BindableObject { BindingContext: BudgetListItem item })
        {
            return;
        }

        _viewModel.SelectBudget(item);
        LimitEntry.Focus();
    }

    private void OnMoreClicked(object? sender, EventArgs e)
    {
        if (sender is not VisualElement { BindingContext: BudgetListItem item } anchor)
            return;

        MenuPopup.Show(anchor, [Loc.T("Remove limit")], async _ => await RemoveLimitAsync(item));
    }

    private async Task RemoveLimitAsync(BudgetListItem item)
    {
        if (!_viewModel.CanEdit)
            return;

        await _viewModel.RemoveLimitAsync(
            item.CategoryId,
            () => DisplayAlertAsync(
                Loc.T("Remove budget limit?"),
                $"{item.CategoryName}\n{_viewModel.MonthTitle}\n\n"
                + Loc.T("Remove the limit for this month? Your expenses will be kept."),
                Loc.T("Remove"),
                Loc.T("Cancel")));
    }
    
    
}