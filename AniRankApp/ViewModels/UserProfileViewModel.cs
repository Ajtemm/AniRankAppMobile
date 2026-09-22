using System.Collections.ObjectModel;
using AniRankApp.Models;
using AniRankApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

/// <summary>
/// Public profile of another user, opened from the "Zajednica" tab: their stats,
/// their watch-status breakdown (tap a row for those reviews) and the follow button.
/// </summary>
public partial class UserProfileViewModel : BaseViewModel, IQueryAttributable
{
    private readonly DatabaseService _db;
    private readonly AuthService _auth;

    public UserProfileViewModel(DatabaseService db, AuthService auth)
    {
        _db = db;
        _auth = auth;
        Title = "Profil";
    }

    public ObservableCollection<StatusCountItem> StatusBreakdown { get; } = new();

    public int UserId { get; private set; }

    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string role = string.Empty;
    [ObservableProperty] private string joinedText = string.Empty;
    [ObservableProperty] private int reviewCount;
    [ObservableProperty] private int followerCount;
    [ObservableProperty] private int followingCount;
    [ObservableProperty] private bool isFollowing;
    [ObservableProperty] private bool followsMe;
    [ObservableProperty] private bool isSelf;
    [ObservableProperty] private bool isPrivateProfile;
    [ObservableProperty] private bool canSeeList = true;
    [ObservableProperty] private string privacyNotice = string.Empty;

    public string FollowActionText => IsFollowing ? "Otprati" : "Zaprati";

    partial void OnIsFollowingChanged(bool value) => OnPropertyChanged(nameof(FollowActionText));

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        UserId = query.TryGetValue("userId", out var raw) && int.TryParse(raw?.ToString(), out var id)
            ? id
            : 0;
        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || UserId <= 0) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var me = _auth.CurrentUserId;
            IsSelf = UserId == me;

            var user = await _db.GetUserByIdAsync(UserId);
            if (user is null)
            {
                ErrorMessage = "Korisnik više ne postoji.";
                return;
            }

            Username = user.Username;
            Role = user.Role;
            JoinedText = $"Član od {user.CreatedAtText}";
            Title = user.Username;

            FollowerCount = await _db.CountFollowersAsync(UserId);
            FollowingCount = await _db.CountFollowingAsync(UserId);
            FollowsMe = !IsSelf && await _db.IsFollowingAsync(UserId, me);
            IsFollowing = !IsSelf && await _db.IsFollowingAsync(me, UserId);

            // A private profile shows its list only to followers (the owner and admins always see it).
            IsPrivateProfile = user.IsPrivate;
            CanSeeList = !user.IsPrivate || IsSelf || IsFollowing || _auth.IsAdmin;
            PrivacyNotice = CanSeeList
                ? string.Empty
                : "Ovaj profil je privatan. Zaprati korisnika da bi video njegovu listu.";

            StatusBreakdown.Clear();

            if (!CanSeeList)
            {
                ReviewCount = 0;
                return;
            }

            var reviews = await _db.GetUserReviewsAsync(UserId);
            ReviewCount = reviews.Count;

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
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleFollowAsync()
    {
        if (IsSelf || UserId <= 0) return;

        var me = _auth.CurrentUserId;

        if (IsFollowing)
            await _db.UnfollowAsync(me, UserId);
        else
            await _db.FollowAsync(me, UserId);

        // Following a private profile unlocks its list, so reload rather than patch counters.
        await LoadAsync();
    }

    /// <summary>Opens this user's reviews filtered by the tapped status row.</summary>
    [RelayCommand]
    private Task OpenStatusAsync(StatusCountItem? item)
        => item is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync($"reviews?userId={UserId}&status={Uri.EscapeDataString(item.Status)}");

    [RelayCommand]
    private Task OpenAllReviewsAsync()
        => Shell.Current.GoToAsync($"reviews?userId={UserId}");
}
