namespace ExpenseTracker.App.Views;

using ExpenseTracker.App.Services;

using Models;
using ViewModels;

public partial class RulesPage : ContentPage
{
    private readonly RulesViewModel _viewModel;

    public RulesPage(RulesViewModel viewModel)
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

    private async void OnRefreshClicked(object? sender, EventArgs e) =>
        await _viewModel.LoadAsync();

    private async void OnAddClicked(object? sender, EventArgs e) =>
        await _viewModel.AddAsync();

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanEdit
            || sender is not Button { BindingContext: RuleListItem rule })
        {
            return;
        }

        await _viewModel.DeleteAsync(
            rule.Id,
            () => DisplayAlertAsync(
                Loc.T("Delete rule?"),
                $"\"{rule.Pattern}\" → {rule.CategoryName}",
                Loc.T("Delete"),
                Loc.T("Cancel")));
    }
}
