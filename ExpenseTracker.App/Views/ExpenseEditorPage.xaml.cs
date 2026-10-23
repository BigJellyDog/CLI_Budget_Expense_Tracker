namespace ExpenseTracker.App.Views;

using ViewModels;

public partial class ExpenseEditorPage : ContentPage
{
    private readonly ExpenseEditorViewModel _viewModel;

    public ExpenseEditorPage(ExpenseEditorViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ConfigureForEdit(Guid expenseId) =>
        _viewModel.ConfigureForEdit(expenseId);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    protected override bool OnBackButtonPressed() =>
        _viewModel.IsBusy || base.OnBackButtonPressed();

    private async void OnReloadClicked(object? sender, EventArgs e) =>
        await _viewModel.LoadAsync();

    private async void OnAddCategoryClicked(object? sender, EventArgs e) =>
        await _viewModel.AddCategoryAsync();

    private async void OnSaveClicked(object? sender, EventArgs e) =>
        await _viewModel.SaveAsync();

    private void OnAddAnotherClicked(object? sender, EventArgs e) =>
        _viewModel.StartNewExpense();
}