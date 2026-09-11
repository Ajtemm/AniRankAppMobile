using SQLite;

namespace AniRankApp.Models;

[Table("Reviews")]
public class Review
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int UserId { get; set; }

    /// <summary>Kitsu API anime id this review belongs to.</summary>
    [Indexed]
    public string AnimeKitsuId { get; set; } = string.Empty;

    public string AnimeTitle { get; set; } = string.Empty;

    public string AnimeImageUrl { get; set; } = string.Empty;

    /// <summary>1.0 - 10.0</summary>
    public double Rating { get; set; }

    public string Comment { get; set; } = string.Empty;

    /// <summary>Watch status: Plan to Watch / Watching / Completed / On Hold / Dropped.</summary>
    public string Status { get; set; } = WatchStatus.Completed;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- Not persisted (filled in memory for display) ----

    /// <summary>Author username, resolved when showing reviews in detail / admin screens.</summary>
    [Ignore] public string Username { get; set; } = string.Empty;

    /// <summary>Drives the DataTrigger colour of the score badge.</summary>
    [Ignore] public string RatingTier => Rating >= 8 ? "High" : Rating >= 5 ? "Medium" : "Low";

    /// <summary>False for "Plan to Watch" / "Dropped" entries (Rating stored as 0).</summary>
    [Ignore] public bool HasRating => Rating >= 1;

    [Ignore] public string RatingText => HasRating ? $"{Rating:0.0}/10" : "Bez ocene";

    /// <summary>Status with legacy null rows normalised to "Completed".</summary>
    [Ignore] public string StatusDisplay => WatchStatus.Normalize(Status);

    [Ignore] public string CreatedAtText => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}
