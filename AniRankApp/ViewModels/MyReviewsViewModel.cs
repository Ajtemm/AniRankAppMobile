using System.Collections.ObjectModel;
using AniRankApp.Helpers;
using AniRankApp.Models;
using AniRankApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

/// <summary>
/// Review list of one profile. Reached from a profile (mine or someone else's) with
/// "userId" and an optional "status" preselecting the filter chip. Deleting is only
/// offered on my own list.
/// </summary>
public partial class MyReviewsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly DatabaseService _db;
    private readonly AuthService _auth;

    private readonly List<Review> _all = new();
    private bool _loading;

    public MyReviewsViewModel(DatabaseService db, AuthService auth)
    {
        _db = db;
        _auth = auth;
        Title = "Moje Recenzije";
    }

    /// <summary>Filtered view bound to the CollectionView.</summary>
    public ObservableCollection<Review> Reviews { get; } = new();

    /// <summary>"Sve" + every watch status - bound to the filter chip bar.</summary>
    public IReadOnlyList<string> StatusFilters { get; } = WatchStatus.Filters;

    /// <summary>Order of the list below the chips.</summary>
    public IReadOnlyList<string> SortOptions { get; } =
        new[] { "Najnovije", "Najstarije", "Najveća ocena", "Najmanja ocena", "Naziv (A-Š)" };

    /// <summary>Whose list is shown. 0 until a query sets it -> falls back to me.</summary>
    public int UserId { get; private set; }

    [ObservableProperty] private bool hasNoReviews;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string selectedStatusFilter = WatchStatus.FilterAll;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private string headerText = string.Empty;
    [ObservableProperty] private bool canDelete = true;
    [ObservableProperty] private int selectedSortIndex;
    [ObservableProperty] private string privacyNotice = string.Empty;

    partial void OnSelectedStatusFilterChanged(string value) => ApplyFilter();

    partial void OnSelectedSortIndexChanged(int value)
    {
        OnPropertyChanged(nameof(SortLabel));
        ApplyFilter();
    }

    public string SortLabel => SortOptions[SelectedSortIndex];

    /// <summary>Sort pill: action sheet with the options; cancelling keeps the current order.</summary>
    [RelayCommand]
    private async Task PickSortAsync()
    {
        var choice = await Shell.Current.DisplayActionSheetAsync("Sortiranje", "Otkaži", null, SortOptions.ToArray());
        var index = choice is null ? -1 : SortOptions.ToList().IndexOf(choice);
        if (index >= 0)
            SelectedSortIndex = index;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        UserId = query.TryGetValue("userId", out var rawId) && int.TryParse(rawId?.ToString(), out var id)
            ? id
            : _auth.CurrentUserId;

        if (query.TryGetValue("status", out var rawStatus))
        {
            var status = Uri.UnescapeDataString(rawStatus?.ToString() ?? string.Empty);
            SelectedStatusFilter = WatchStatus.Filters.Contains(status) ? status : WatchStatus.FilterAll;
        }
        else
        {
            SelectedStatusFilter = WatchStatus.FilterAll;
        }

        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (_loading) return;
        _loading = true;

        try
        {
            IsRefreshing = true;
            ErrorMessage = null;

            var me = _auth.CurrentUserId;
            var targetId = UserId > 0 ? UserId : me;
            CanDelete = targetId == me;
            PrivacyNotice = string.Empty;

            if (CanDelete)
            {
                Title = "Moje Recenzije";
                HeaderText = "Moje recenzije";
            }
            else
            {
                var owner = await _db.GetUserByIdAsync(targetId);
                Title = owner?.Username ?? "Recenzije";
                HeaderText = $"Recenzije korisnika: {owner?.Username ?? "?"}";

                // A private profile is readable only by its followers (and admins).
                if (owner is { IsPrivate: true } && !_auth.IsAdmin && !await _db.IsFollowingAsync(me, targetId))
                {
                    PrivacyNotice = $"Profil korisnika \"{owner.Username}\" je privatan. " +
                                    "Zaprati ga da bi video njegovu listu.";
                    _all.Clear();
                    Summary = string.Empty;
                    ApplyFilter();
                    return;
                }
            }

            var list = await _db.GetUserReviewsAsync(targetId);
            _all.Clear();
            _all.AddRange(list);

            BuildSummary();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri učitavanju recenzija: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
            _loading = false;
        }
    }

    private void BuildSummary()
    {
        if (_all.Count == 0)
        {
            Summary = string.Empty;
            return;
        }

        var parts = WatchStatus.All
            .Select(s => $"{s}: {_all.Count(r => WatchStatus.Normalize(r.Status) == s)}");
        Summary = string.Join("   ·   ", parts);
    }

    private void ApplyFilter()
    {
        IEnumerable<Review> query = _all;

        if (SelectedStatusFilter != WatchStatus.FilterAll)
            query = query.Where(r => WatchStatus.Normalize(r.Status) == SelectedStatusFilter);

        // Sorting runs over the already loaded list - no extra database work.
        query = SelectedSortIndex switch
        {
            1 => query.OrderBy(r => r.LastActivityAt),
            2 => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.LastActivityAt),
            3 => query.OrderBy(r => r.Rating).ThenByDescending(r => r.LastActivityAt),
            4 => query.OrderBy(r => r.AnimeTitle, StringComparer.CurrentCultureIgnoreCase),
            _ => query.OrderByDescending(r => r.LastActivityAt)
        };

        Reviews.Clear();
        foreach (var r in query)
            Reviews.Add(r);

        HasNoReviews = Reviews.Count == 0;
    }

    [RelayCommand]
    private async Task DeleteReviewAsync(Review? review)
    {
        if (review is null || !CanDelete) return;

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Brisanje", $"Obrisati recenziju za \"{review.AnimeTitle}\"?", "Obriši", "Otkaži");
        if (!confirm) return;

        await _db.DeleteReviewAsync(review.Id);
        _all.RemoveAll(r => r.Id == review.Id);
        BuildSummary();
        ApplyFilter();

        await ToastHelper.ShowAsync("Recenzija je obrisana.");
    }
}
