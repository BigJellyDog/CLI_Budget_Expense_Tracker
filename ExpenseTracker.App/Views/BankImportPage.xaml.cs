namespace ExpenseTracker.App.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using ViewModels;

public partial class BankImportPage : ContentPage
{
    private readonly BankImportViewModel _viewModel;
    private readonly IServiceProvider _services;

    private bool _pickingFile;

    public BankImportPage(BankImportViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _services = services;
        BindingContext = viewModel;
    }

    private async void OnEditRulesClicked(object? sender, EventArgs e)
    {
        if (_pickingFile || _viewModel.IsBusy) return;

        await Navigation.PushAsync(_services.GetRequiredService<RulesPage>());
    }

    protected override bool OnBackButtonPressed() =>
        _pickingFile || _viewModel.IsBusy || base.OnBackButtonPressed();

    private async void OnImportClicked(object? sender, EventArgs e) =>
        await _viewModel.ImportAsync();

    private async void OnChooseFileClicked(object? sender, EventArgs e)
    {
        if (_pickingFile || _viewModel.IsBusy) return;

        _pickingFile = true;

        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choose a Revolut CSV statement",
                FileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        [DevicePlatform.WinUI] = [".csv"]
                    })
            });

            if (file is not null)
                await _viewModel.LoadAsync(file.FileName, file.OpenReadAsync);
        }
        catch (OperationCanceledException)
        {
            // Keep the previous preview when file selection is canceled.
        }
        catch (Exception exception)
        {
            _viewModel.ReportFileError(exception);
        }
        finally
        {
            _pickingFile = false;
        }
    }
}