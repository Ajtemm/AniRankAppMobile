using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AniRankApp.Models;

namespace AniRankApp.Services;

/// <summary>
/// Async client for the Kitsu API (https://kitsu.io/api/edge).
/// All calls use HttpClient + async/await so the UI thread is never blocked.
/// </summary>
public class KitsuApiService
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public KitsuApiService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://kitsu.io/api/edge/");
        _http.Timeout = TimeSpan.FromSeconds(30);
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
    }

    /// <summary>GET https://kitsu.io/api/edge/trending/anime</summary>
    public async Task<List<Anime>> GetTrendingAnimeAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetFromJsonAsync<KitsuAnimeResponse>("trending/anime", JsonOptions, ct);
        return resp?.Data.Select(Anime.FromKitsu).ToList() ?? new List<Anime>();
    }

    /// <summary>GET https://kitsu.io/api/edge/anime?filter[text]={query}</summary>
    public async Task<List<Anime>> SearchAnimeAsync(string query, CancellationToken ct = default)
    {
        var url = $"anime?filter[text]={Uri.EscapeDataString(query)}&page[limit]=20";
        var resp = await _http.GetFromJsonAsync<KitsuAnimeResponse>(url, JsonOptions, ct);
        return resp?.Data.Select(Anime.FromKitsu).ToList() ?? new List<Anime>();
    }

    /// <summary>
    /// Flexible query used by the Explore screen: text search + subtype + status + sort.
    /// Falls back to the trending endpoint when no filter is active.
    /// </summary>
    public async Task<List<Anime>> GetAnimeAsync(AnimeFilter filter, CancellationToken ct = default)
    {
        if (filter.IsPlainTrending)
            return await GetTrendingAnimeAsync(ct);

        var parts = new List<string> { $"page[limit]={filter.Limit}" };

        if (filter.Offset > 0)
            parts.Add($"page[offset]={filter.Offset}");
        if (!string.IsNullOrWhiteSpace(filter.Text))
            parts.Add($"filter[text]={Uri.EscapeDataString(filter.Text!.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.Subtype))
            parts.Add($"filter[subtype]={filter.Subtype}");
        if (!string.IsNullOrWhiteSpace(filter.Status))
            parts.Add($"filter[status]={filter.Status}");

        parts.Add(filter.Sort switch
        {
            AnimeSort.TopRated => "sort=-averageRating",
            AnimeSort.MostPopular => "sort=-userCount",
            AnimeSort.Newest => "sort=-startDate",
            _ => "sort=popularityRank"
        });

        var url = "anime?" + string.Join("&", parts);
        var resp = await _http.GetFromJsonAsync<KitsuAnimeResponse>(url, JsonOptions, ct);
        return resp?.Data.Select(Anime.FromKitsu).ToList() ?? new List<Anime>();
    }

    /// <summary>GET https://kitsu.io/api/edge/anime/{id}</summary>
    public async Task<Anime?> GetAnimeDetailsAsync(string id, CancellationToken ct = default)
    {
        var resp = await _http.GetFromJsonAsync<KitsuAnimeSingleResponse>($"anime/{id}", JsonOptions, ct);
        return resp?.Data is null ? null : Anime.FromKitsu(resp.Data);
    }
}
