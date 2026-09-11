using System.Collections.ObjectModel;
using AniRankApp.Models;
using AniRankApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

[QueryProperty(nameof(AnimeId), "id")]
public partial class AnimeDetailViewModel : BaseViewModel
{
    private readonly KitsuApiService _api;
    private readonly DatabaseService _db;
    private readonly AuthService _auth;

    private int? _myReviewId;

    public AnimeDetailViewModel(KitsuApiService api, DatabaseService db, AuthService auth)
    {
        _api = api;
        _db = db;
        _auth = auth;
        Title = "Detalji";
    }

    public ObservableCollection<Review> Reviews { get; } = new();

    /// <summary>Watch statuses for the picker.</summary>
    public IReadOnlyList<string> Statuses { get; } = WatchStatus.All;

    [ObservableProperty] private string animeId = string.Empty;
    [ObservableProperty] private Anime? anime;
    [ObservableProperty] private double newRating = 5;
    [ObservableProperty] private string newComment = string.Empty;
    [ObservableProperty] private string selectedStatus = WatchStatus.Completed;
    [ObservableProperty] private bool canRate = true;
    [ObservableProperty] private bool hasNoReviews;
    [ObservableProperty] private string submitButtonText = "Sačuvaj u moju listu";

    partial void OnAnimeIdChanged(string value) => _ = LoadAsync();

    partial void OnSelectedStatusChanged(string value)
        => CanRate = WatchStatus.IsRatable(value);

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(AnimeId) || IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Anime = await _api.GetAnimeDetailsAsync(AnimeId);
            if (Anime is not null)
                Title = Anime.Title;

            await LoadReviewsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri učitavanju detalja: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadReviewsAsync()
    {
        Reviews.Clear();

        var list = await _db.GetReviewsForAnimeAsync(AnimeId);
        foreach (var r in list)
        {
            var author = await _db.GetUserByIdAsync(r.UserId);
            r.Username = author?.Username ?? "Nepoznat korisnik";
            Reviews.Add(r);
        }

        HasNoReviews = Reviews.Count == 0;

        // Pre-fill the form if the current user already has an entry for this anime.
        var mine = list.FirstOrDefault(r => r.UserId == _auth.CurrentUserId);
        if (mine is not null)
        {
            _myReviewId = mine.Id;
            NewRating = mine.Rating is >= 1 and <= 10 ? mine.Rating : 5;
            NewComment = mine.Comment;
            SelectedStatus = WatchStatus.Normalize(mine.Status); // updates CanRate
            SubmitButtonText = "Ažuriraj moju listu";
        }
        else
        {
            _myReviewId = null;
            SubmitButtonText = "Sačuvaj u moju listu";
        }
    }

    [RelayCommand]
    private async Task SubmitReviewAsync()
    {
        if (Anime is null || IsBusy) return;
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(SelectedStatus))
        {
            ErrorMessage = "Izaberite status gledanja.";
            return;
        }

        var ratable = WatchStatus.IsRatable(SelectedStatus);
        if (ratable && (NewRating < 1 || NewRating > 10))
        {
            ErrorMessage = "Ocena mora biti između 1 i 10.";
            return;
        }

        // "Plan to Watch" / "Dropped" -> no rating is stored.
        var ratingToSave = ratable ? Math.Round(NewRating, 1) : 0;

        try
        {
            IsBusy = true;

            if (_myReviewId is int id)
            {
                var existing = (await _db.GetReviewsForAnimeAsync(Anime.Id))
                    .FirstOrDefault(r => r.Id == id);

                if (existing is not null)
                {
                    existing.Rating = ratingToSave;
                    existing.Comment = NewComment?.Trim() ?? string.Empty;
                    existing.Status = SelectedStatus;
                    existing.CreatedAt = DateTime.UtcNow;
                    await _db.UpdateReviewAsync(existing);
                }
            }
            else
            {
                await _db.InsertReviewAsync(new Review
                {
                    UserId = _auth.CurrentUserId,
                    AnimeKitsuId = Anime.Id,
                    AnimeTitle = Anime.Title,
                    AnimeImageUrl = Anime.PosterUrl,
                    Rating = ratingToSave,
                    Comment = NewComment?.Trim() ?? string.Empty,
                    Status = SelectedStatus,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await LoadReviewsAsync();
            await Shell.Current.DisplayAlertAsync("Sačuvano", "Vaša lista je ažurirana.", "OK");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri čuvanju: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
