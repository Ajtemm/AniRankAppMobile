using AniRankApp.Helpers;
using AniRankApp.Services;
using AniRankApp.Views;

namespace AniRankApp;

public partial class AppShell : Shell
{
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

        // Session restore: if Preferences already hold a session -> go straight in.
        CurrentItem = _auth.IsLoggedIn ? MainTab : LoginItem;

        RefreshTabs();

        // In case the saved session belongs to a user an Admin has since banned/removed.
        if (_auth.IsLoggedIn)
            _ = ValidateSessionAsync();
    }

    /// <summary>Bound by the Admin tab's IsVisible.</summary>
    public bool IsAdmin => _auth.IsAdmin;

    /// <summary>Call after login/logout so the Admin tab shows/hides correctly.</summary>
    public void RefreshTabs()
    {
        OnPropertyChanged(nameof(IsAdmin));
        if (AdminTab is not null)
            AdminTab.IsVisible = _auth.IsAdmin;
    }

    private async Task ValidateSessionAsync()
    {
        var user = await _db.GetUserByIdAsync(_auth.CurrentUserId);
        if (user is not null && !user.IsBanned)
            return;

        _auth.Logout();
        RefreshTabs();
        await GoToAsync("//login");

        if (user is { IsBanned: true })
            await DisplayAlertAsync("Nalog banovan", "Vaš nalog je banovan od strane administratora.", "OK");
    }
}
