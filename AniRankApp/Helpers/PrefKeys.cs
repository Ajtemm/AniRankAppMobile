namespace AniRankApp.Helpers;

/// <summary>Keys used to persist the active session in <see cref="Microsoft.Maui.Storage.Preferences"/>.</summary>
public static class PrefKeys
{
    public const string UserId = "current_user_id";
    public const string Username = "current_username";
    public const string Role = "current_user_role";
    public const string LoginAt = "login_at";

    /// <summary>When the user last opened the follower list - drives the "new follower" badge.</summary>
    public const string FollowersSeenAt = "followers_seen_at";
}
