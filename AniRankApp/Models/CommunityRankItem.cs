namespace AniRankApp.Models;

/// <summary>
/// One row of the local "Top lista zajednice": an anime with the average of all
/// ratings given inside this app (not the Kitsu score).
/// </summary>
public record CommunityRankItem(
    string AnimeKitsuId,
    string Title,
    string ImageUrl,
    double Average,
    int Votes)
{
    /// <summary>Position in the list, filled in when the list is built.</summary>
    public int Rank { get; init; }

    public string RankText => $"{Rank}.";

    /// <summary>Gold / silver / bronze for the podium, plain otherwise (DataTrigger key).</summary>
    public string Medal => Rank switch { 1 => "Gold", 2 => "Silver", 3 => "Bronze", _ => "None" };

    public string AverageText => $"{Average:0.0}/10";

    public string VotesText => Votes switch
    {
        1 => "1 ocena",
        >= 2 and <= 4 => $"{Votes} ocene",
        _ => $"{Votes} ocena"
    };

    /// <summary>Drives the DataTrigger colour of the score badge.</summary>
    public string RatingTier => Average >= 8 ? "High" : Average >= 5 ? "Medium" : "Low";
}
