namespace AniRankApp.Models;

public enum AnimeSort
{
    Trending,
    TopRated,
    MostPopular,
    Newest
}

/// <summary>Filter/sort options for the Explore screen -> Kitsu query parameters.</summary>
public class AnimeFilter
{
    public string? Text { get; set; }
    public AnimeSort Sort { get; set; } = AnimeSort.Trending;

    /// <summary>Kitsu subtype: TV, movie, OVA, ONA, special, music.</summary>
    public string? Subtype { get; set; }

    /// <summary>Kitsu status: current, finished, upcoming.</summary>
    public string? Status { get; set; }

    public int Limit { get; set; } = 20;

    /// <summary>Pagination offset (page[offset]).</summary>
    public int Offset { get; set; }

    /// <summary>First page, no filters, default sort -> use the dedicated trending endpoint.</summary>
    public bool IsPlainTrending =>
        Sort == AnimeSort.Trending
        && Offset == 0
        && string.IsNullOrWhiteSpace(Text)
        && string.IsNullOrWhiteSpace(Subtype)
        && string.IsNullOrWhiteSpace(Status);
}
