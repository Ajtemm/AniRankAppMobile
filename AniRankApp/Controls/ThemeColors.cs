namespace AniRankApp.Controls;

/// <summary>
/// Reads palette colours from the app resources (CustomStyles.xaml) for controls that are
/// built in code, so every screen draws from the same tokens.
/// </summary>
internal static class ThemeColors
{
    public static Color Get(string key, string fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color c)
            return c;

        return Color.FromArgb(fallback);
    }

    public static Color ScoreHigh => Get("ScoreHigh", "#3FB950");
    public static Color ScoreMid => Get("ScoreMid", "#E3B341");
    public static Color ScoreLow => Get("ScoreLow", "#F85149");
    public static Color OnScore => Get("OnScore", "#0E0E12");

    /// <summary>Same thresholds as the RatingTier properties on the models (8+ / 5+ / below).</summary>
    public static Color ForRating(double value, double max = 10)
    {
        var pct = max > 0 ? value / max : 0;
        if (pct >= 0.8) return ScoreHigh;
        if (pct >= 0.5) return ScoreMid;
        return ScoreLow;
    }
}
