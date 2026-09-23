using System.Collections.ObjectModel;
using AniRankApp.Models;
using AniRankApp.Services;
using AniRankApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

/// <summary>One user row in the community list (follow state changes in place).</summary>
public partial class CommunityUserItem : ObservableObject
{
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string JoinedText { get; init; } = string.Empty;
    public int ReviewCount { get; init; }

    /// <summary>True when this user already follows me (shows the "Prati te" badge).</summary>
    public bool FollowsMe { get; init; }

    /// <summary>Private profile - their list is only visible to followers.</summary>
    public bool IsPrivate { get; init; }

    [ObservableProperty] private bool isFollowing;
    [ObservableProperty] private int followerCount;

    public string FollowActionText => IsFollowing ? "Otprati" : "Zaprati";
    public string StatsText => $"Recenzija: {ReviewCount}   ·   Pratilaca: {FollowerCount}";

    partial void OnIsFollowingChanged(bool value) => OnPropertyChanged(nameof(FollowActionText));
    partial void OnFollowerCountChanged(int value) => OnPropertyChanged(nameof(StatsText));
}

/// <summary>
/// "Zajednica" tab: a feed of what the people you follow rated, plus search over every
/// other account with follow / unfollow. Following is one-directional - they can follow back.
/// </summary>
public partial class CommunityViewModel : BaseViewModel
{
    public const string SegmentFeed = "Feed";
    public const string SegmentAll = "Svi";
    public const string SegmentFollowing = "Pratim";
    public const string SegmentFollowers = "Prate me";

    private readonly DatabaseService _db;
    private readonly AuthService _auth;

    private readonly List<CommunityUserItem> _all = new();
    private bool _loading;

    public CommunityViewModel(DatabaseService db, AuthService auth)
    {
        _db = db;
        _auth = auth;
        Title = "Zajednica";
    }

    /// <summary>Filtered user view bound to the user CollectionView.</summary>
    public ObservableCollection<CommunityUserItem> Users { get; } = new();

    /// <summary>Latest entries of the people I follow.</summary>
    public ObservableCollection<Review> Feed { get; } = new();

    public IReadOnlyList<string> Segments { get; } =
        new[] { SegmentFeed, SegmentAll, SegmentFollowing, SegmentFollowers };

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedSegment = SegmentFeed;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string emptyText = "Nema korisnika za prikaz.";
    [ObservableProperty] private bool showFeed = true;
    [ObservableProperty] private bool hasNewFollowers;
    [ObservableProperty] private string newFollowerText = string.Empty;
    [ObservableProperty] private string feedEmptyText =
        "Zaprati nekoga da bi ovde video šta gleda i kako ocenjuje.";

    public bool ShowUsers => !ShowFeed;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedSegmentChanged(string value)
    {
        ShowFeed = value == SegmentFeed;
        OnPropertyChanged(nameof(ShowUsers));
        ApplyFilter();
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

            var users = await _db.GetOtherUsersAsync(me);
            var followingIds = (await _db.GetFollowingIdsAsync(me)).ToHashSet();
            var followerIds = (await _db.GetFollowerIdsAsync(me)).ToHashSet();
            var reviewCounts = await _db.GetReviewCountsByUserAsync();
            var followerCounts = await _db.GetFollowerCountsAsync();

            _all.Clear();
            foreach (var u in users)
            {
                _all.Add(new CommunityUserItem
                {
                    UserId = u.Id,
                    Username = u.Username,
                    Role = u.Role,
                    JoinedText = $"Član od {u.CreatedAtText}",
                    ReviewCount = reviewCounts.GetValueOrDefault(u.Id),
                    FollowsMe = followerIds.Contains(u.Id),
                    IsPrivate = u.IsPrivate,
                    IsFollowing = followingIds.Contains(u.Id),
                    FollowerCount = followerCounts.GetValueOrDefault(u.Id)
                });
            }

            Summary = $"Pratim: {followingIds.Count}   ·   Prate me: {followerIds.Count}";

            await LoadFeedAsync(followingIds);
            await RefreshNewFollowersAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri učitavanju: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
            _loading = false;
        }
    }

    /// <summary>Newest entries of everybody I follow, newest first.</summary>
    private async Task LoadFeedAsync(HashSet<int> followingIds)
    {
        Feed.Clear();

        if (followingIds.Count == 0)
        {
            FeedEmptyText = "Zaprati nekoga da bi ovde video šta gleda i kako ocenjuje.";
            return;
        }

        var entries = await _db.GetFeedAsync(followingIds);
        var usernames = entries.Count > 0 ? await _db.GetUsernamesAsync() : new Dictionary<int, string>();

        foreach (var r in entries)
        {
            r.Username = usernames.GetValueOrDefault(r.UserId) ?? "Nepoznat korisnik";
            Feed.Add(r);
        }

        FeedEmptyText = "Korisnici koje pratiš još nisu ništa dodali u svoju listu.";
    }

    /// <summary>Counts follows that arrived since the list was last reviewed.</summary>
    private async Task RefreshNewFollowersAsync()
    {
        var fresh = await _db.CountFollowersSinceAsync(_auth.CurrentUserId, _auth.FollowersSeenAt);

        HasNewFollowers = fresh > 0;
        NewFollowerText = fresh switch
        {
            0 => string.Empty,
            1 => "Imaš 1 novog pratioca!",
            >= 2 and <= 4 => $"Imaš {fresh} nova pratioca!",
            _ => $"Imaš {fresh} novih pratilaca!"
        };

        if (Shell.Current is AppShell shell)
            shell.SetCommunityBadge(HasNewFollowers);
    }

    /// <summary>Opens the follower list and clears the badge.</summary>
    [RelayCommand]
    private void ShowNewFollowers()
    {
        _auth.MarkFollowersSeen();
        HasNewFollowers = false;
        NewFollowerText = string.Empty;
        SelectedSegment = SegmentFollowers;

        if (Shell.Current is AppShell shell)
            shell.SetCommunityBadge(false);
    }

    private void ApplyFilter()
    {
        if (ShowFeed)
        {
            Users.Clear();
            return;
        }

        IEnumerable<CommunityUserItem> query = SelectedSegment switch
        {
            SegmentFollowing => _all.Where(u => u.IsFollowing),
            SegmentFollowers => _all.Where(u => u.FollowsMe),
            _ => _all
        };

        var term = SearchText?.Trim();
        if (!string.IsNullOrEmpty(term))
            query = query.Where(u => u.Username.Contains(term, StringComparison.OrdinalIgnoreCase));

        Users.Clear();
        foreach (var u in query)
            Users.Add(u);

        EmptyText = SelectedSegment switch
        {
            SegmentFollowing => "Još uvek ne pratiš nijednog korisnika.",
            SegmentFollowers => "Još uvek te niko ne prati.",
            _ => "Nema korisnika za prikaz."
        };
    }

    /// <summary>Follow / unfollow, toggled straight from the list row.</summary>
    [RelayCommand]
    private async Task ToggleFollowAsync(CommunityUserItem? item)
    {
        if (item is null) return;

        var me = _auth.CurrentUserId;

        try
        {
            if (item.IsFollowing)
            {
                await _db.UnfollowAsync(me, item.UserId);
                item.IsFollowing = false;
                item.FollowerCount = Math.Max(0, item.FollowerCount - 1);
            }
            else
            {
                await _db.FollowAsync(me, item.UserId);
                item.IsFollowing = true;
                item.FollowerCount++;
            }

            Summary = $"Pratim: {_all.Count(u => u.IsFollowing)}   ·   Prate me: {_all.Count(u => u.FollowsMe)}";

            // The feed follows whoever I follow, so it has to be rebuilt.
            await LoadFeedAsync(_all.Where(u => u.IsFollowing).Select(u => u.UserId).ToHashSet());
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška: {ex.Message}";
        }
    }

    /// <summary>Opens the public profile of the tapped user.</summary>
    [RelayCommand]
    private Task OpenUserAsync(CommunityUserItem? item)
        => item is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync($"userprofile?userId={item.UserId}");

    /// <summary>Feed card tap -> the anime it is about.</summary>
    [RelayCommand]
    private Task OpenFeedEntryAsync(Review? review)
        => review is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync($"{nameof(AnimeDetailView)}?id={review.AnimeKitsuId}");

    /// <summary>Empty feed call-to-action: jump to the full user list to find people to follow.</summary>
    [RelayCommand]
    private void BrowseUsers() => SelectedSegment = SegmentAll;
}
