using AniRankApp.Helpers;
using AniRankApp.Models;

namespace AniRankApp.Services;

/// <summary>
/// Registration, login, and session persistence in <see cref="Preferences"/>.
/// </summary>
public class AuthService
{
    private readonly DatabaseService _db;

    public AuthService(DatabaseService db) => _db = db;

    // ---- Session (read from Preferences) ----
    public bool IsLoggedIn => Preferences.Get(PrefKeys.UserId, 0) > 0;
    public int CurrentUserId => Preferences.Get(PrefKeys.UserId, 0);
    public string CurrentUsername => Preferences.Get(PrefKeys.Username, string.Empty);
    public string CurrentUserRole => Preferences.Get(PrefKeys.Role, "User");
    public bool IsAdmin => CurrentUserRole == "Admin";
    public DateTime? LoginAt =>
        DateTime.TryParse(Preferences.Get(PrefKeys.LoginAt, string.Empty), out var dt) ? dt : null;

    // ---- Registration ----
    public async Task<(bool ok, string error)> RegisterAsync(string username, string email, string password, string role = "User")
    {
        username = username?.Trim() ?? string.Empty;
        email = email?.Trim() ?? string.Empty;

        if (username.Length < 3)
            return (false, "Korisničko ime mora imati bar 3 karaktera.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || !email.Contains('.'))
            return (false, "Unesite ispravnu email adresu.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "Lozinka mora imati bar 6 karaktera.");

        var existing = await _db.GetUserByUsernameAsync(username);
        if (existing is not null)
            return (false, "Korisničko ime je već zauzeto.");

        await _db.InsertUserAsync(new User
        {
            Username = username,
            Email = email,
            PasswordHash = SecurityHelper.Hash(password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        });

        return (true, string.Empty);
    }

    // ---- Login ----
    public async Task<(bool ok, string error, User? user)> LoginAsync(string username, string password)
    {
        var user = await _db.GetUserByUsernameAsync(username?.Trim() ?? string.Empty);
        if (user is null)
            return (false, "Korisnik sa tim imenom ne postoji.", null);

        if (user.IsBanned)
            return (false, "Vaš nalog je uklonjen/banovan od strane administratora. Prijava nije moguća.", null);

        if (user.PasswordHash != SecurityHelper.Hash(password ?? string.Empty))
            return (false, "Pogrešna lozinka.", null);

        SaveSession(user);
        return (true, string.Empty, user);
    }

    // ---- Session persistence ----
    public void SaveSession(User user)
    {
        SaveIdentity(user);
        Preferences.Set(PrefKeys.LoginAt, DateTime.UtcNow.ToString("o"));
    }

    /// <summary>
    /// Re-reads the account from the database and refreshes the cached identity.
    /// <see cref="Preferences"/> are only a cache - the database decides who you are and
    /// which role you have, so a hand-edited preferences file can't grant Admin rights.
    /// Returns the account, or null when it is gone or banned.
    /// </summary>
    public async Task<User?> RefreshSessionAsync()
    {
        if (!IsLoggedIn) return null;

        var user = await _db.GetUserByIdAsync(CurrentUserId);
        if (user is null || user.IsBanned) return null;

        SaveIdentity(user);
        return user;
    }

    private static void SaveIdentity(User user)
    {
        Preferences.Set(PrefKeys.UserId, user.Id);
        Preferences.Set(PrefKeys.Username, user.Username);
        Preferences.Set(PrefKeys.Role, user.Role);
    }

    public void Logout()
    {
        Preferences.Remove(PrefKeys.UserId);
        Preferences.Remove(PrefKeys.Username);
        Preferences.Remove(PrefKeys.Role);
        Preferences.Remove(PrefKeys.LoginAt);
    }

    // ---- "New follower" badge bookkeeping ----

    /// <summary>Per account, so two users on the same device don't share the badge state.</summary>
    private string FollowersSeenKey => $"{PrefKeys.FollowersSeenAt}_{CurrentUserId}";

    /// <summary>Moment the follower list was last reviewed (epoch start when never).</summary>
    public DateTime FollowersSeenAt =>
        DateTime.TryParse(Preferences.Get(FollowersSeenKey, string.Empty), out var dt)
            ? dt.ToUniversalTime()
            : DateTime.MinValue;

    public void MarkFollowersSeen()
        => Preferences.Set(FollowersSeenKey, DateTime.UtcNow.ToString("o"));
}
