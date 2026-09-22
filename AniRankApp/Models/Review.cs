using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace AniRankApp.Models;

/// <summary>
/// One user's entry for one anime (status + optional rating/comment).
/// Inherits <see cref="ObservableObject"/> so in-place UI state (likes) can change
/// without rebuilding the list; only non-<c>[Ignore]</c> properties are columns.
/// </summary>
[Table("Reviews")]
public class Review : ObservableObject
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

    /// <summary>Episodes the user has watched so far (0 = not tracked).</summary>
    public int EpisodesWatched { get; set; }

    /// <summary>When the entry was first added - never overwritten by an edit.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last edit. <see cref="DateTime.MinValue"/> on rows written before this column existed.</summary>
    public DateTime UpdatedAt { get; set; }

    // ---- Not persisted (filled in memory for display) ----

    /// <summary>Author username, resolved when showing reviews in detail / admin screens.</summary>
    [Ignore] public string Username { get; set; } = string.Empty;

    /// <summary>True when the signed-in user wrote this review (no "useful" button on your own).</summary>
    [Ignore] public bool IsMine { get; set; }

    [Ignore] public bool CanLike => !IsMine;

    /// <summary>Drives the DataTrigger colour of the score badge.</summary>
    [Ignore] public string RatingTier => Rating >= 8 ? "High" : Rating >= 5 ? "Medium" : "Low";

    /// <summary>False for "Plan to Watch" / "Dropped" entries (Rating stored as 0).</summary>
    [Ignore] public bool HasRating => Rating >= 1;

    [Ignore] public string RatingText => HasRating ? $"{Rating:0.0}/10" : "Bez ocene";

    /// <summary>Status with legacy null rows normalised to "Completed".</summary>
    [Ignore] public string StatusDisplay => WatchStatus.Normalize(Status);

    [Ignore] public string CreatedAtText => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

    /// <summary>Edit time when there was one, otherwise the creation time.</summary>
    [Ignore] public DateTime LastActivityAt => UpdatedAt > CreatedAt ? UpdatedAt : CreatedAt;

    [Ignore] public bool WasEdited => UpdatedAt > CreatedAt;

    [Ignore] public string LastActivityText => WasEdited
        ? $"Izmenjeno {UpdatedAt.ToLocalTime():dd.MM.yyyy HH:mm}"
        : CreatedAtText;

    [Ignore] public bool HasProgress => EpisodesWatched > 0;

    [Ignore] public string ProgressText => EpisodesWatched > 0 ? $"Odgledano: {EpisodesWatched} ep." : string.Empty;

    // ---- Likes ("korisno"), kept in memory and toggled in place ----

    private int likeCount;
    private bool isLikedByMe;

    [Ignore]
    public int LikeCount
    {
        get => likeCount;
        set
        {
            if (SetProperty(ref likeCount, value))
                OnPropertyChanged(nameof(LikeText));
        }
    }

    [Ignore]
    public bool IsLikedByMe
    {
        get => isLikedByMe;
        set
        {
            if (SetProperty(ref isLikedByMe, value))
                OnPropertyChanged(nameof(LikeText));
        }
    }

    /// <summary>Text of the like button: filled heart once I marked it useful.</summary>
    [Ignore] public string LikeText => IsLikedByMe ? $"♥ Korisno ({LikeCount})" : $"♡ Korisno ({LikeCount})";
}
