using AniRankApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly AuthService _auth;

    public RegisterViewModel(AuthService auth)
    {
        _auth = auth;
        Title = "Registracija";
    }

    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;
    [ObservableProperty] private string? successMessage;

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy) return;
        ErrorMessage = null;
        SuccessMessage = null;

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Lozinke se ne poklapaju.";
            return;
        }

        try
        {
            IsBusy = true;
            var (ok, error) = await _auth.RegisterAsync(Username, Email, Password);
            if (!ok)
            {
                ErrorMessage = error;
                return;
            }

            SuccessMessage = "Nalog je kreiran. Sada se možete prijaviti.";
            Username = Email = Password = ConfirmPassword = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri registraciji: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task GoToLoginAsync() => Shell.Current.GoToAsync("//login");
}
