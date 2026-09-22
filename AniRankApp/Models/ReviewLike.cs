using SQLite;

namespace AniRankApp.Models;

/// <summary>
/// "This review was useful to me" mark. One row per (review, user) pair - the unique
/// index makes a double tap impossible to store twice.
/// </summary>
[Table("ReviewLikes")]
public class ReviewLike
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Name = "UX_ReviewLikes_Pair", Order = 1, Unique = true)]
    public int ReviewId { get; set; }

    [Indexed(Name = "UX_ReviewLikes_Pair", Order = 2, Unique = true)]
    [Indexed(Name = "IX_ReviewLikes_User", Order = 1)]
    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
