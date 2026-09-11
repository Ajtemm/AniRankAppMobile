namespace AniRankApp.Models;

/// <summary>
/// Watch-status values stored on a <see cref="Review"/> ("my entry" for an anime).
/// Stored as plain strings in SQLite so old rows migrate cleanly.
/// </summary>
public static class WatchStatus
{
    public const string PlanToWatch = "Plan to Watch";
    public const string Watching = "Watching";
    public const string Completed = "Completed";
    public const string OnHold = "On Hold";
    public const string Dropped = "Dropped";

    public const string FilterAll = "Sve";

    /// <summary>All real statuses, in display order (for the detail-page picker).</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        PlanToWatch, Watching, Completed, OnHold, Dropped
    };

    /// <summary>"Sve" + all statuses (for filter chip bars).</summary>
    public static readonly IReadOnlyList<string> Filters = new[]
    {
        FilterAll, PlanToWatch, Watching, Completed, OnHold, Dropped
    };

    /// <summary>Null / empty (legacy rows) map to "Completed".</summary>
    public static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? Completed : value!;

    /// <summary>"Plan to Watch" and "Dropped" cannot carry a rating.</summary>
    public static bool IsRatable(string? status)
    {
        var s = Normalize(status);
        return s != PlanToWatch && s != Dropped;
    }
}
