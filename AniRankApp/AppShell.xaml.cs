using AniRankApp.Helpers;
using AniRankApp.Services;
using AniRankApp.Views;

namespace AniRankApp;

public partial class AppShell : Shell
{
    private const string CommunityTitle = "👥  Zajednica";

    private readonly AuthService _auth;
    private readonly DatabaseService _db;

    public AppShell() : this(ServiceHelper.GetService<AuthService>(), ServiceHelper.GetService<DatabaseService>()) { }

    public AppShell(AuthService auth, DatabaseService db)
    {
        InitializeComponent();
        _auth = auth;
        _db = db;
        BindingContext = this;

        // Routes pushed on top of a tab (detail navigation).
        Routing.RegisterRoute(nameof(AnimeDetailView), typeof(AnimeDetailView));
        Routing.RegisterRoute("adminuserreviews", typeof(AdminUserReviewsView));
        Routing.RegisterRoute("reviews", typeof(MyReviewsView));
        Routing.RegisterRoute("userprofile", typeof(UserProfileView));
        Routing.RegisterRoute("toplist", typeof(TopListView));

        // Session restore: if Preferences already hold a session -> go straight in.
        CurrentItem = _auth.IsLoggedIn ? ExploreItem : LoginItem;

        RefreshTabs();

        // In case the saved session belongs to a user an Admin has since banned/removed,
        // or whose role changed while the app was closed.
        if (_auth.IsLoggedIn)
            _ = ValidateSessionAsync();
    }

    /// <summary>Bound by the Admin item's IsVisible.</summary>
    public bool IsAdmin => _auth.IsAdmin;

    /// <summary>Bound by the sidebar header/footer.</summary>
    public bool IsLoggedIn => _auth.IsLoggedIn;
    public string CurrentUsername => _auth.CurrentUsername;
    public string CurrentRoleLabel => $"Uloga: {_auth.CurrentUserRole}";

    /// <summary>Call after login/logout so the sidebar reflects the current session.</summary>
    public void RefreshTabs()
    {
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(CurrentUsername));
        OnPropertyChanged(nameof(CurrentRoleLabel));

        if (AdminTab is not null)
            AdminTab.IsVisible = _auth.IsAdmin;

        _ = RefreshCommunityBadgeAsync();
    }

    private void OnTopListClicked(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        _ = GoToAsync("toplist");
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;

        var confirm = await DisplayAlertAsync("Odjava", "Odjaviti se sa naloga?", "Odjava", "Otkaži");
        if (!confirm) return;

        _auth.Logout();
        RefreshTabs();
        await GoToAsync("//login");
    }

    /// <summary>Marks the Zajednica tab when somebody new started following the user.</summary>
    public async Task RefreshCommunityBadgeAsync()
    {
        try
        {
            if (!_auth.IsLoggedIn)
            {
                SetCommunityBadge(false);
                return;
            }

            var fresh = await _db.CountFollowersSinceAsync(_auth.CurrentUserId, _auth.FollowersSeenAt);
            SetCommunityBadge(fresh > 0);
        }
        catch
        {
            // A badge is never worth crashing the shell over.
            SetCommunityBadge(false);
        }
    }

    /// <summary>Shell tabs have no badge API - a marker in the title does the job.</summary>
    public void SetCommunityBadge(bool hasNew)
    {
        if (CommunityTab is not null)
            CommunityTab.Title = hasNew ? $"{CommunityTitle} ●" : CommunityTitle;
    }

    private async Task ValidateSessionAsync()
    {
        // Re-reads the account and refreshes the cached role/username from the database.
        var user = await _auth.RefreshSessionAsync();
        if (user is not null)
        {
            RefreshTabs();
            return;
        }

        var stored = await _db.GetUserByIdAsync(_auth.CurrentUserId);

        _auth.Logout();
        RefreshTabs();
        await GoToAsync("//login");

        if (stored is { IsBanned: true })
            await DisplayAlertAsync("Nalog banovan", "Vaš nalog je banovan od strane administratora.", "OK");
    }
}
