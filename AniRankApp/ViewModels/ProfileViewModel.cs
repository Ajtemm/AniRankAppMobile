using System.Collections.ObjectModel;
using AniRankApp.Helpers;
using AniRankApp.Models;
using AniRankApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

/// <summary>One row of the profile watch-status breakdown. Tapping it opens that list.</summary>
public record StatusCountItem(string Status, int Count);

public partial class ProfileViewModel : BaseViewModel
{
    private readonly AuthService _auth;
    private readonly DatabaseService _db;

    public ProfileViewModel(AuthService auth, DatabaseService db)
    {
        _auth = auth;
        _db = db;
        Title = "Profil";
    }

    public ObservableCollection<StatusCountItem> StatusBreakdown { get; } = new();

    /// <summary>True while <see cref="LoadAsync"/> fills the form, so toggles don't write back.</summary>
    private bool _loading;

    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string role = string.Empty;
    [ObservableProperty] private string loginInfo = string.Empty;
    [ObservableProperty] private int reviewCount;
    [ObservableProperty] private int followerCount;
    [ObservableProperty] private int followingCount;
    [ObservableProperty] private bool isPrivate;
    [ObservableProperty] private string privacyHint = string.Empty;

    /// <summary>Flipping the switch saves immediately - there is no "Save" button on this screen.</summary>
    partial void OnIsPrivateChanged(bool value)
    {
        UpdatePrivacyHint();
        if (!_loading)
            _ = SavePrivacyAsync(value);
    }

    private void UpdatePrivacyHint()
        => PrivacyHint = IsPrivate
            ? "Tvoju listu vide samo korisnici koji te prate."
            : "Tvoju listu može da vidi svako iz Zajednice.";

    private async Task SavePrivacyAsync(bool value)
    {
        try
        {
            await _db.SetUserPrivateAsync(_auth.CurrentUserId, value);
            await ToastHelper.ShowAsync(value ? "Profil je sada privatan." : "Profil je sada javan.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri čuvanju podešavanja: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        Username = _auth.CurrentUsername;
        Role = _auth.CurrentUserRole;
        LoginInfo = _auth.LoginAt is { } dt
            ? $"Prijava: {dt.ToLocalTime():dd.MM.yyyy HH:mm}"
            : "Prijava: -";

        // Called from OnAppearing (async void) - a thrown exception here would crash the app.
        try
        {
            _loading = true;
            ErrorMessage = null;

            var me = _auth.CurrentUserId;
            var reviews = await _db.GetUserReviewsAsync(me);
            ReviewCount = reviews.Count;

            FollowerCount = await _db.CountFollowersAsync(me);
            FollowingCount = await _db.CountFollowingAsync(me);

            var user = await _db.GetUserByIdAsync(me);
            IsPrivate = user?.IsPrivate ?? false;
            UpdatePrivacyHint();

            StatusBreakdown.Clear();
            foreach (var status in WatchStatus.All)
                StatusBreakdown.Add(new StatusCountItem(
                    status,
                    reviews.Count(r => WatchStatus.Normalize(r.Status) == status)));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri učitavanju profila: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>Opens my reviews filtered by the tapped status row.</summary>
    [RelayCommand]
    private Task OpenStatusAsync(StatusCountItem? item)
        => item is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync($"reviews?userId={_auth.CurrentUserId}&status={Uri.EscapeDataString(item.Status)}");

    /// <summary>Opens my whole list, unfiltered.</summary>
    [RelayCommand]
    private Task OpenAllReviewsAsync()
        => Shell.Current.GoToAsync($"reviews?userId={_auth.CurrentUserId}");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await Shell.Current.DisplayAlertAsync("Odjava", "Odjaviti se sa naloga?", "Odjava", "Otkaži");
        if (!confirm) return;

        _auth.Logout();

        if (Shell.Current is AppShell shell)
            shell.RefreshTabs();

        await Shell.Current.GoToAsync("//login");
    }
}
