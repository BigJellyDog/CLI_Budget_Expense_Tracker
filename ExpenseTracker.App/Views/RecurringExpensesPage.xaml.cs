namespace ExpenseTracker.App.Views;

using ViewModels;

public partial class RecurringExpensesPage : ContentPage
{
    private readonly RecurringExpensesViewModel _viewModel;

    public RecurringExpensesPage(RecurringExpensesViewModel viewModel)
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

    private async void OnGenerateClicked(object? sender, EventArgs e) =>
        await _viewModel.GenerateDueAsync();

    private async void OnCreateClicked(object? sender, EventArgs e) =>
        await _viewModel.CreateAsync();

    private void OnAddAnotherClicked(object? sender, EventArgs e) =>
        _viewModel.StartNewSchedule();
}