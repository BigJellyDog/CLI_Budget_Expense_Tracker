namespace ExpenseTracker.App.Views;

using ExpenseTracker.App.Services;

using Models;
using ViewModels;

public partial class RulesPage : ContentPage
{
    private readonly RulesViewModel _viewModel;

    /// <summary>Creates the page of category rules for imported entries.</summary>
    public RulesPage(RulesViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>Loads the data each time the page is shown, so it is current after returning from another page.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    /// <summary>Ignores the back button while something is being saved or loaded.</summary>
    protected override bool OnBackButtonPressed() =>
        _viewModel.IsBusy || base.OnBackButtonPressed();

    /// <summary>Reloads the data from the database.</summary>
    private async void OnRefreshClicked(object? sender, EventArgs e) =>
        await _viewModel.LoadAsync();

    /// <summary>Adds the rule typed in the form.</summary>
    private async void OnAddClicked(object? sender, EventArgs e) =>
        await _viewModel.AddAsync();

    /// <summary>Deletes the rule of the clicked row.</summary>
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
