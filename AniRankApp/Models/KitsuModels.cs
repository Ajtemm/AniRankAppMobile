using System.Text.Json.Serialization;

namespace AniRankApp.Models;

// ============================================================================
//  Kitsu API (JSON:API) response models  -  https://kitsu.io/api/edge
// ============================================================================

/// <summary>Response for list endpoints (trending/anime, anime?filter[text]=...).</summary>
public class KitsuAnimeResponse
{
    [JsonPropertyName("data")]
    public List<KitsuAnimeData> Data { get; set; } = new();
}

/// <summary>Response for the single-resource endpoint (anime/{id}).</summary>
public class KitsuAnimeSingleResponse
{
    [JsonPropertyName("data")]
    public KitsuAnimeData? Data { get; set; }
}

public class KitsuAnimeData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "anime";

    [JsonPropertyName("attributes")]
    public KitsuAttributes Attributes { get; set; } = new();
}

public class KitsuAttributes
{
    [JsonPropertyName("canonicalTitle")]
    public string? CanonicalTitle { get; set; }

    [JsonPropertyName("titles")]
    public KitsuTitles? Titles { get; set; }

    [JsonPropertyName("synopsis")]
    public string? Synopsis { get; set; }

    /// <summary>Kitsu returns this as a string on a 0-100 scale, e.g. "82.14".</summary>
    [JsonPropertyName("averageRating")]
    public string? AverageRating { get; set; }

    [JsonPropertyName("episodeCount")]
    public int? EpisodeCount { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>TV, movie, OVA, ONA, special, music.</summary>
    [JsonPropertyName("subtype")]
    public string? Subtype { get; set; }

    [JsonPropertyName("popularityRank")]
    public int? PopularityRank { get; set; }

    [JsonPropertyName("ratingRank")]
    public int? RatingRank { get; set; }

    [JsonPropertyName("startDate")]
    public string? StartDate { get; set; }

    [JsonPropertyName("posterImage")]
    public KitsuImage? PosterImage { get; set; }

    [JsonPropertyName("coverImage")]
    public KitsuImage? CoverImage { get; set; }
}

public class KitsuTitles
{
    [JsonPropertyName("en")] public string? En { get; set; }
    [JsonPropertyName("en_jp")] public string? EnJp { get; set; }
    [JsonPropertyName("ja_jp")] public string? JaJp { get; set; }
}

public class KitsuImage
{
    [JsonPropertyName("tiny")] public string? Tiny { get; set; }
    [JsonPropertyName("small")] public string? Small { get; set; }
    [JsonPropertyName("medium")] public string? Medium { get; set; }
    [JsonPropertyName("large")] public string? Large { get; set; }
    [JsonPropertyName("original")] public string? Original { get; set; }
}
