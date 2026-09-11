namespace AniRankApp.Helpers;

/// <summary>Keys used to persist the active session in <see cref="Microsoft.Maui.Storage.Preferences"/>.</summary>
public static class PrefKeys
{
    public const string UserId = "current_user_id";
    public const string Username = "current_username";
    public const string Role = "current_user_role";
    public const string LoginAt = "login_at";
}
