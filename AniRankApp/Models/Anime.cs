using System.Globalization;

namespace AniRankApp.Models;

/// <summary>
/// UI-facing anime model. Mapped from <see cref="KitsuAnimeData"/> so the views never
/// touch the raw JSON:API shape.
/// </summary>
public class Anime
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Synopsis { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;

    /// <summary>Normalised to a 0-10 scale.</summary>
    public double AverageRating { get; set; }

    public int? EpisodeCount { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>Kitsu subtype: TV, movie, OVA, ONA, special, music.</summary>
    public string Subtype { get; set; } = string.Empty;

    public int? PopularityRank { get; set; }
    public int? RatingRank { get; set; }
    public string StartDate { get; set; } = string.Empty;

    // ---- Display helpers ----
    public string AverageRatingText => AverageRating > 0 ? $"{AverageRating:0.0}" : "N/A";
    public string EpisodeText => EpisodeCount.HasValue ? $"{EpisodeCount} epizoda" : "Broj epizoda: nepoznato";
    public string PopularityText => PopularityRank.HasValue ? $"Popularnost #{PopularityRank}" : "Popularnost: -";
    public string RatingRankText => RatingRank.HasValue ? $"Rang ocene #{RatingRank}" : "Rang ocene: -";
    public string StatusText => string.IsNullOrWhiteSpace(Status) ? "Status: nepoznato" : $"Status: {StatusDisplay}";

    /// <summary>Kitsu status in Serbian ("finished" -> "Završeno").</summary>
    public string StatusDisplay => Status switch
    {
        "current" => "U toku",
        "finished" => "Završeno",
        "upcoming" => "Najavljeno",
        "unreleased" => "Neobjavljeno",
        "tba" => "Uskoro",
        _ => Status
    };

    public string SubtypeDisplay => Subtype switch
    {
        "TV" => "TV",
        "movie" => "Film",
        "OVA" => "OVA",
        "ONA" => "ONA",
        "special" => "Specijal",
        "music" => "Muzički",
        _ => Subtype
    };

    public string YearText => StartDate.Length >= 4 ? StartDate[..4] : string.Empty;

    /// <summary>One compact line for cards: "TV · 2023 · 24 ep · Završeno".</summary>
    public string MetaText => string.Join("  ·  ", new[]
    {
        SubtypeDisplay,
        YearText,
        EpisodeCount is > 0 ? $"{EpisodeCount} ep" : string.Empty,
        StatusDisplay
    }.Where(p => !string.IsNullOrWhiteSpace(p)));

    public bool HasPopularity => PopularityRank.HasValue;

    /// <summary>Drives the DataTrigger colour of the score badge.</summary>
    public string RatingTier => AverageRating >= 8 ? "High" : AverageRating >= 5 ? "Medium" : "Low";

    public static Anime FromKitsu(KitsuAnimeData d)
    {
        var a = d.Attributes;

        double avg = 0;
        if (double.TryParse(a.AverageRating, NumberStyles.Any, CultureInfo.InvariantCulture, out var raw))
            avg = Math.Round(raw / 10.0, 2); // Kitsu averageRating is 0-100

        return new Anime
        {
            Id = d.Id,
            Title = a.CanonicalTitle
                    ?? a.Titles?.En
                    ?? a.Titles?.EnJp
                    ?? a.Titles?.JaJp
                    ?? "Bez naslova",
            Synopsis = string.IsNullOrWhiteSpace(a.Synopsis) ? "Nema dostupnog opisa." : a.Synopsis!,
            PosterUrl = a.PosterImage?.Medium ?? a.PosterImage?.Small ?? a.PosterImage?.Original ?? string.Empty,
            CoverUrl = a.CoverImage?.Large ?? a.CoverImage?.Original ?? a.CoverImage?.Small ?? string.Empty,
            AverageRating = avg,
            EpisodeCount = a.EpisodeCount,
            Status = a.Status ?? string.Empty,
            Subtype = a.Subtype ?? string.Empty,
            PopularityRank = a.PopularityRank,
            RatingRank = a.RatingRank,
            StartDate = a.StartDate ?? string.Empty
        };
    }
}
