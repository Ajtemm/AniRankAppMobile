using System.Collections.ObjectModel;
using AniRankApp.Models;
using AniRankApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

public partial class MyReviewsViewModel : BaseViewModel
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

    [ObservableProperty] private bool hasNoReviews;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string selectedStatusFilter = WatchStatus.FilterAll;
    [ObservableProperty] private string summary = string.Empty;

    partial void OnSelectedStatusFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (_loading) return;
        _loading = true;

        try
        {
            IsRefreshing = true;
            ErrorMessage = null;

            var list = await _db.GetUserReviewsAsync(_auth.CurrentUserId);
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

        Reviews.Clear();
        foreach (var r in query)
            Reviews.Add(r);

        HasNoReviews = Reviews.Count == 0;
    }

    [RelayCommand]
    private async Task DeleteReviewAsync(Review? review)
    {
        if (review is null) return;

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Brisanje", $"Obrisati recenziju za \"{review.AnimeTitle}\"?", "Obriši", "Otkaži");
        if (!confirm) return;

        await _db.DeleteReviewAsync(review.Id);
        _all.RemoveAll(r => r.Id == review.Id);
        BuildSummary();
        ApplyFilter();
    }
}
