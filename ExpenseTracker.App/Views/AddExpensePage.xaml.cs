namespace ExpenseTracker.App.Views;

using ViewModels;

public partial class AddExpensePage : ContentPage
{
    private readonly AddExpenseViewModel _viewModel;

    public AddExpensePage(AddExpenseViewModel viewModel)
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

    private async void OnReloadClicked(object? sender, EventArgs e) =>
        await _viewModel.LoadAsync();

    private async void OnAddCategoryClicked(object? sender, EventArgs e) =>
        await _viewModel.AddCategoryAsync();

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (await _viewModel.SaveAsync())
            await Navigation.PopAsync();
    }
}