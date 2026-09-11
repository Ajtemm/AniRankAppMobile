using System.Collections.ObjectModel;
using AniRankApp.Models;
using AniRankApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

/// <summary>One row of the profile watch-status breakdown.</summary>
public record StatusCountItem(string Status, int Count);

public partial class ProfileViewModel : BaseViewModel
{
    private readonly AuthService _auth;
    private readonly DatabaseService _db;
    private readonly FileExportService _export;

    public ProfileViewModel(AuthService auth, DatabaseService db, FileExportService export)
    {
        _auth = auth;
        _db = db;
        _export = export;
        Title = "Profil";
    }

    public ObservableCollection<StatusCountItem> StatusBreakdown { get; } = new();

    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string role = string.Empty;
    [ObservableProperty] private string loginInfo = string.Empty;
    [ObservableProperty] private int reviewCount;
    [ObservableProperty] private string? statusMessage;

    [RelayCommand]
    public async Task LoadAsync()
    {
        Username = _auth.CurrentUsername;
        Role = _auth.CurrentUserRole;
        LoginInfo = _auth.LoginAt is { } dt
            ? $"Prijava: {dt.ToLocalTime():dd.MM.yyyy HH:mm}"
            : "Prijava: -";

        var reviews = await _db.GetUserReviewsAsync(_auth.CurrentUserId);
        ReviewCount = reviews.Count;

        StatusBreakdown.Clear();
        foreach (var status in WatchStatus.All)
            StatusBreakdown.Add(new StatusCountItem(
                status,
                reviews.Count(r => WatchStatus.Normalize(r.Status) == status)));
    }

    [RelayCommand]
    private Task ExportJsonAsync() => ExportAsync(FileExportService.ExportFormat.Json);

    [RelayCommand]
    private Task ExportTxtAsync() => ExportAsync(FileExportService.ExportFormat.Txt);

    private async Task ExportAsync(FileExportService.ExportFormat format)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = null;

            var reviews = await _db.GetUserReviewsAsync(_auth.CurrentUserId);
            if (reviews.Count == 0)
            {
                StatusMessage = "Nemate recenzija za izvoz.";
                return;
            }

            var path = await _export.ExportReviewsAsync(reviews, _auth.CurrentUsername, format);
            StatusMessage = $"Sačuvano: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Greška pri izvozu: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

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
