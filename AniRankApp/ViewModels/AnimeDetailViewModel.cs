using System.Collections.ObjectModel;
using AniRankApp.Helpers;
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

    /// <summary>Loaded reviews in database order; <see cref="Reviews"/> is the sorted view.</summary>
    private readonly List<Review> _allReviews = new();

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

    /// <summary>How the community reviews below the form are ordered.</summary>
    public IReadOnlyList<string> ReviewSortOptions { get; } =
        new[] { "Najkorisnije", "Najnovije", "Najbolje ocenjene" };

    [ObservableProperty] private string animeId = string.Empty;
    [ObservableProperty] private Anime? anime;
    [ObservableProperty] private double newRating = 5;
    [ObservableProperty] private string newComment = string.Empty;
    [ObservableProperty] private string newEpisodesWatched = string.Empty;
    [ObservableProperty] private string selectedStatus = WatchStatus.Completed;
    [ObservableProperty] private bool canRate = true;
    [ObservableProperty] private bool canTrackProgress = true;
    [ObservableProperty] private bool hasNoReviews;
    [ObservableProperty] private string submitButtonText = "Sačuvaj u moju listu";
    [ObservableProperty] private string communityRatingText = "Zajednica: još nema ocena";
    [ObservableProperty] private int selectedReviewSortIndex;

    partial void OnAnimeIdChanged(string value) => _ = LoadAsync();

    partial void OnSelectedStatusChanged(string value)
    {
        CanRate = WatchStatus.IsRatable(value);
        // Tracking episodes makes no sense before you start watching.
        CanTrackProgress = WatchStatus.Normalize(value) != WatchStatus.PlanToWatch;
    }

    partial void OnSelectedReviewSortIndexChanged(int value) => ApplyReviewSort();

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
            ErrorMessage = Connectivity.NetworkAccess != NetworkAccess.Internet
                ? "Nema internet konekcije - detalji animea se ne mogu učitati."
                : $"Greška pri učitavanju detalja: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadReviewsAsync()
    {
        var me = _auth.CurrentUserId;
        var list = await _db.GetReviewsForAnimeAsync(AnimeId);

        // One lookup each for authors and likes, instead of one query per review.
        var usernames = list.Count > 0 ? await _db.GetUsernamesAsync() : new Dictionary<int, string>();
        var likeCounts = list.Count > 0 ? await _db.GetLikeCountsAsync() : new Dictionary<int, int>();
        var myLikes = list.Count > 0 ? await _db.GetLikedReviewIdsAsync(me) : new HashSet<int>();

        _allReviews.Clear();
        foreach (var r in list)
        {
            r.Username = usernames.GetValueOrDefault(r.UserId) ?? "Nepoznat korisnik";
            r.IsMine = r.UserId == me;
            r.LikeCount = likeCounts.GetValueOrDefault(r.Id);
            r.IsLikedByMe = myLikes.Contains(r.Id);
            _allReviews.Add(r);
        }

        HasNoReviews = _allReviews.Count == 0;
        ApplyReviewSort();

        var (average, votes) = await _db.GetAnimeRatingStatsAsync(AnimeId);
        CommunityRatingText = votes == 0
            ? "Zajednica: još nema ocena"
            : $"Zajednica: {average:0.0}/10 ({FormatVotes(votes)})";

        // Pre-fill the form if the current user already has an entry for this anime.
        var mine = _allReviews.FirstOrDefault(r => r.IsMine);
        if (mine is not null)
        {
            _myReviewId = mine.Id;
            NewRating = mine.Rating is >= 1 and <= 10 ? mine.Rating : 5;
            NewComment = mine.Comment;
            NewEpisodesWatched = mine.EpisodesWatched > 0 ? mine.EpisodesWatched.ToString() : string.Empty;
            SelectedStatus = WatchStatus.Normalize(mine.Status); // updates CanRate / CanTrackProgress
            SubmitButtonText = "Ažuriraj moju listu";
        }
        else
        {
            _myReviewId = null;
            SubmitButtonText = "Sačuvaj u moju listu";
        }
    }

    private static string FormatVotes(int votes) => votes switch
    {
        1 => "1 ocena",
        >= 2 and <= 4 => $"{votes} ocene",
        _ => $"{votes} ocena"
    };

    /// <summary>Re-orders the already loaded reviews in memory - no extra database work.</summary>
    private void ApplyReviewSort()
    {
        IEnumerable<Review> sorted = SelectedReviewSortIndex switch
        {
            1 => _allReviews.OrderByDescending(r => r.LastActivityAt),
            2 => _allReviews.OrderByDescending(r => r.Rating).ThenByDescending(r => r.LikeCount),
            _ => _allReviews.OrderByDescending(r => r.LikeCount).ThenByDescending(r => r.LastActivityAt)
        };

        Reviews.Clear();
        foreach (var r in sorted)
            Reviews.Add(r);
    }

    /// <summary>"Korisno" toggle on someone else's review.</summary>
    [RelayCommand]
    private async Task ToggleLikeAsync(Review? review)
    {
        if (review is null || review.IsMine) return;

        try
        {
            if (review.IsLikedByMe)
            {
                await _db.UnlikeReviewAsync(review.Id, _auth.CurrentUserId);
                review.IsLikedByMe = false;
                review.LikeCount = Math.Max(0, review.LikeCount - 1);
            }
            else
            {
                await _db.LikeReviewAsync(review.Id, _auth.CurrentUserId);
                review.IsLikedByMe = true;
                review.LikeCount++;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška: {ex.Message}";
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

        if (!TryParseEpisodes(out var episodes))
        {
            ErrorMessage = "Broj odgledanih epizoda mora biti ceo broj (0 ili više).";
            return;
        }

        // "Plan to Watch" / "Dropped" -> no rating is stored.
        var ratingToSave = ratable ? Math.Round(NewRating, 1) : 0;
        var now = DateTime.UtcNow;

        try
        {
            IsBusy = true;

            if (_myReviewId is int id)
            {
                var existing = await _db.GetReviewByIdAsync(id);

                if (existing is not null)
                {
                    existing.Rating = ratingToSave;
                    existing.Comment = NewComment?.Trim() ?? string.Empty;
                    existing.Status = SelectedStatus;
                    existing.EpisodesWatched = episodes;
                    existing.UpdatedAt = now; // CreatedAt stays the original entry date
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
                    EpisodesWatched = episodes,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await LoadReviewsAsync();
            await ToastHelper.ShowAsync("Vaša lista je ažurirana.");
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

    /// <summary>Empty field means "not tracked" (0); anything non-numeric is rejected.</summary>
    private bool TryParseEpisodes(out int episodes)
    {
        episodes = 0;

        if (!CanTrackProgress || string.IsNullOrWhiteSpace(NewEpisodesWatched))
            return true;

        if (!int.TryParse(NewEpisodesWatched.Trim(), out var parsed) || parsed < 0)
            return false;

        episodes = parsed;
        return true;
    }
}
