namespace ExpenseTracker.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Services;

public sealed class SettingsViewModel(
    AppSettings settings,
    ILogger<SettingsViewModel> logger) : ObservableObject
{
    public string CurrencyText { get; set; } = settings.Currency;

    public string DataFolder =>
        Path.GetDirectoryName(Path.GetFullPath(settings.FilePath)) ?? "";

    public string ErrorMessage { get; private set; } = "";

    public string SuccessMessage { get; private set; } = "";

    public bool HasError => ErrorMessage.Length > 0;

    public bool HasSuccess => SuccessMessage.Length > 0;

    public void Reload()
    {
        CurrencyText = settings.Currency;
        ErrorMessage = "";
        SuccessMessage = "";

        NotifyState();
    }

    public void Save()
    {
        var code = (CurrencyText ?? "").Trim().ToUpperInvariant();

        if (!AppSettings.IsValidCurrency(code))
        {
            ErrorMessage = "Enter the currency as three letters, for example EUR.";
            SuccessMessage = "";
            NotifyState();
            return;
        }

        try
        {
            settings.SetCurrency(code);

            CurrencyText = code;
            ErrorMessage = "";
            SuccessMessage =
                $"Saved. Amounts are shown in {code}. Amounts already saved are not converted.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to save the settings.");

            ErrorMessage = "Could not save the settings. Try again.";
            SuccessMessage = "";
        }

        NotifyState();
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(CurrencyText));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(SuccessMessage));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasSuccess));
    }
}
