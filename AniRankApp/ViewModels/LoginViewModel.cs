using AniRankApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly AuthService _auth;

    public LoginViewModel(AuthService auth)
    {
        _auth = auth;
        Title = "Prijava";
    }

    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Unesite korisničko ime i lozinku.";
            return;
        }

        try
        {
            IsBusy = true;
            var (ok, error, _) = await _auth.LoginAsync(Username, Password);
            if (!ok)
            {
                ErrorMessage = error;
                return;
            }

            Password = string.Empty;

            if (Shell.Current is AppShell shell)
                shell.RefreshTabs();

            await Shell.Current.GoToAsync("//explore");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri prijavi: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task GoToRegisterAsync() => Shell.Current.GoToAsync("//register");
}
