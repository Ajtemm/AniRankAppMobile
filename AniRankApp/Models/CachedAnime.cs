using SQLite;

namespace AniRankApp.Models;

/// <summary>
/// Last successfully loaded Explore page, stored locally so the list still shows
/// something when the device is offline. Mirrors <see cref="Anime"/> plus ordering.
/// </summary>
[Table("CachedAnime")]
public class CachedAnime
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    /// <summary>Position in the cached page, so the order survives a round trip.</summary>
    public int SortOrder { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Synopsis { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int EpisodeCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int PopularityRank { get; set; }
    public int RatingRank { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    public static CachedAnime From(Anime a, int sortOrder) => new()
    {
        Id = a.Id,
        SortOrder = sortOrder,
        Title = a.Title,
        Synopsis = a.Synopsis,
        PosterUrl = a.PosterUrl,
        CoverUrl = a.CoverUrl,
        AverageRating = a.AverageRating,
        EpisodeCount = a.EpisodeCount ?? 0,
        Status = a.Status,
        PopularityRank = a.PopularityRank ?? 0,
        RatingRank = a.RatingRank ?? 0,
        StartDate = a.StartDate,
        CachedAt = DateTime.UtcNow
    };

    public Anime ToAnime() => new()
    {
        Id = Id,
        Title = Title,
        Synopsis = Synopsis,
        PosterUrl = PosterUrl,
        CoverUrl = CoverUrl,
        AverageRating = AverageRating,
        EpisodeCount = EpisodeCount > 0 ? EpisodeCount : null,
        Status = Status,
        PopularityRank = PopularityRank > 0 ? PopularityRank : null,
        RatingRank = RatingRank > 0 ? RatingRank : null,
        StartDate = StartDate
    };
}
