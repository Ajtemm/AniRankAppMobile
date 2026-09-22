using System.Collections.ObjectModel;
using AniRankApp.Models;
using AniRankApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

/// <summary>
/// Admin view of a single user's reviews (userId &gt; 0) or of every review in the
/// system (userId = 0). Reached from the Admin panel, not shown all at once.
/// </summary>
public partial class AdminUserReviewsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly DatabaseService _db;

    public AdminUserReviewsViewModel(DatabaseService db)
    {
        _db = db;
        Title = "Recenzije";
    }

    public ObservableCollection<Review> Reviews { get; } = new();

    public int UserId { get; private set; }

    [ObservableProperty] private bool hasNoReviews;
    [ObservableProperty] private string headerText = string.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        UserId = query.TryGetValue("userId", out var raw) && int.TryParse(raw?.ToString(), out var id)
            ? id
            : 0;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            List<Review> list;
            if (UserId <= 0)
            {
                list = await _db.GetAllReviewsAsync();
                HeaderText = "Sve recenzije u sistemu";
                Title = "Sve recenzije";
            }
            else
            {
                list = await _db.GetUserReviewsAsync(UserId);
                var user = await _db.GetUserByIdAsync(UserId);
                HeaderText = $"Recenzije korisnika: {user?.Username ?? "?"}";
                Title = user?.Username ?? "Recenzije";
            }

            // One username lookup for the whole list instead of one query per review.
            var usernames = list.Count > 0 ? await _db.GetUsernamesAsync() : new Dictionary<int, string>();

            Reviews.Clear();
            foreach (var r in list)
            {
                if (string.IsNullOrEmpty(r.Username))
                    r.Username = usernames.GetValueOrDefault(r.UserId) ?? "Nepoznat";

                Reviews.Add(r);
            }

            HasNoReviews = Reviews.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri učitavanju: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteReviewAsync(Review? review)
    {
        if (review is null) return;

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Brisanje recenzije",
            $"Obrisati recenziju korisnika \"{review.Username}\" za \"{review.AnimeTitle}\"?",
            "Obriši", "Otkaži");
        if (!confirm) return;

        await _db.DeleteReviewAsync(review.Id);
        Reviews.Remove(review);
        HasNoReviews = Reviews.Count == 0;
    }
}
