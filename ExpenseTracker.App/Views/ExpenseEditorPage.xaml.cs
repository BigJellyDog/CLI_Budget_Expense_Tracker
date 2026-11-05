namespace ExpenseTracker.App.Views;

using ViewModels;

public partial class ExpenseEditorPage : ContentPage
{
    private readonly ExpenseEditorViewModel _viewModel;

    /// <summary>Creates the editor for a new or an existing expense.</summary>
    public ExpenseEditorPage(ExpenseEditorViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>Switches the editor to change the given expense instead of adding one.</summary>
    public void ConfigureForEdit(Guid expenseId) =>
        _viewModel.ConfigureForEdit(expenseId);

    /// <summary>Loads the data each time the page is shown, so it is current after returning from another page.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    /// <summary>Ignores the back button while something is being saved or loaded.</summary>
    protected override bool OnBackButtonPressed() =>
        _viewModel.IsBusy || base.OnBackButtonPressed();

    /// <summary>Reloads the categories.</summary>
    private async void OnReloadClicked(object? sender, EventArgs e) =>
        await _viewModel.LoadAsync();

    /// <summary>Adds the category typed in the form.</summary>
    private async void OnAddCategoryClicked(object? sender, EventArgs e) =>
        await _viewModel.AddCategoryAsync();

    /// <summary>Saves the form.</summary>
    private async void OnSaveClicked(object? sender, EventArgs e) =>
        await _viewModel.SaveAsync();

    /// <summary>Clears the form for the next expense.</summary>
    private void OnAddAnotherClicked(object? sender, EventArgs e) =>
        _viewModel.StartNewExpense();
}