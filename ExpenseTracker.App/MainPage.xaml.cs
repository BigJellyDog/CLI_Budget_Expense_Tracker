namespace ExpenseTracker.App;

using ViewModels;

public partial class MainPage : ContentPage
{
    private readonly ExpensesViewModel _viewModel;

    public MainPage(ExpensesViewModel viewModel)
    {
        InitializeComponent();

        this._viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }
}