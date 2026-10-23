namespace ExpenseTracker.App.Views;

using CLI_Budget_Expense_Tracker.Core.Models;
using ViewModels;

public partial class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel _viewModel;

    public CategoriesPage(CategoriesViewModel viewModel)
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

    private async void OnSaveClicked(object? sender, EventArgs e) =>
        await _viewModel.SaveAsync();

    private void OnCancelClicked(object? sender, EventArgs e) =>
        _viewModel.CancelEdit();

    private void OnRenameClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanEdit
            || sender is not Button { BindingContext: Category category })
        {
            return;
        }

        _viewModel.StartRename(category);
        CategoryNameEntry.Focus();
    }
    
    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanEdit
            || sender is not Button { BindingContext: Category category })
        {
            return;
        }

        await _viewModel.DeleteAsync(
            category.Id,
            () => DisplayAlertAsync(
                "Delete category?",
                $"Delete \"{category.Name}\"?\n\nOnly unused categories can be deleted.",
                "Delete",
                "Cancel"));
    }
}