using AniRankApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

/// <summary>
/// "Odjava" tab: the page itself is the confirmation - log out, or go back to the tab
/// the user came from.
/// </summary>
public partial class LogoutViewModel : BaseViewModel
{
    private readonly AuthService _auth;

    public LogoutViewModel(AuthService auth)
    {
        _auth = auth;
        Title = "Odjava";
    }

    [ObservableProperty] private string username = string.Empty;

    /// <summary>Called from OnAppearing - the session can change while the tab stays alive.</summary>
    public void Refresh() => Username = _auth.CurrentUsername;

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _auth.Logout();

        if (Shell.Current is AppShell shell)
            shell.RefreshTabs();

        await Shell.Current.GoToAsync("//login");
    }

    [RelayCommand]
    private Task CancelAsync()
    {
        var route = (Shell.Current as AppShell)?.LastTabRoute ?? "explore";
        return Shell.Current.GoToAsync($"//{route}");
    }
}
