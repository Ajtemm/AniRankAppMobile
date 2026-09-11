using SQLite;

namespace AniRankApp.Models;

[Table("Users")]
public class User
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>"Admin" or "User".</summary>
    public string Role { get; set; } = "User";

    /// <summary>Set by an Admin instead of deleting the account. Blocks login.</summary>
    public bool IsBanned { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- Not persisted (display helpers) ----
    [Ignore] public bool IsAdmin => Role == "Admin";
    [Ignore] public string CreatedAtText => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy");
    [Ignore] public string BanActionText => IsBanned ? "Odbanuj" : "Banuj";
}
