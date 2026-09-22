using SQLite;

namespace AniRankApp.Models;

/// <summary>
/// One "X prati Y" relation. Directed: a row means <see cref="FollowerId"/> follows
/// <see cref="FollowingId"/>; the other direction needs its own row.
/// </summary>
[Table("Follows")]
public class Follow
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>User that clicked "Zaprati".</summary>
    [Indexed(Name = "UX_Follows_Pair", Order = 1, Unique = true)]
    public int FollowerId { get; set; }

    /// <summary>
    /// User being followed. Together with <see cref="FollowerId"/> it forms a unique
    /// index, so the same pair can never be stored twice (even on a double tap).
    /// </summary>
    [Indexed(Name = "UX_Follows_Pair", Order = 2, Unique = true)]
    [Indexed(Name = "IX_Follows_Following", Order = 1)]
    public int FollowingId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
