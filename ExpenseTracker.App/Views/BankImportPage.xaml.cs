namespace ExpenseTracker.App.Views;

using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using ViewModels;

public partial class BankImportPage : ContentPage
{
    private readonly BankImportViewModel _viewModel;

    private bool _pickingFile;

    public BankImportPage(BankImportViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override bool OnBackButtonPressed() =>
        _pickingFile || _viewModel.IsBusy || base.OnBackButtonPressed();

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