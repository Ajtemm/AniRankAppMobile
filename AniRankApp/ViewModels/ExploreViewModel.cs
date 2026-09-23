using System.Collections.ObjectModel;
using AniRankApp.Models;
using AniRankApp.Services;
using AniRankApp.Views;
using Microsoft.Maui.Networking;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

public partial class ExploreViewModel : BaseViewModel
{
    private const int PageSize = 20;

    /// <summary>How long we wait after the last filter change before hitting the API.</summary>
    private const int FilterDebounceMs = 350;

    private readonly KitsuApiService _api;
    private readonly DatabaseService _db;

    /// <summary>Ids already in <see cref="Animes"/> - kept so paging dedup is O(1) per item.</summary>
    private readonly HashSet<string> _loadedIds = new();

    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _debounceCts;
    private bool _bulkUpdate;
    private bool _loadingMore;
    private int _offset;
    private bool _hasMore = true;

    public ExploreViewModel(KitsuApiService api, DatabaseService db)
    {
        _api = api;
        _db = db;
        Title = "Istraži";
    }

    public ObservableCollection<Anime> Animes { get; } = new();

    public IReadOnlyList<string> SortOptions { get; } =
        new[] { "Popularno (trending)", "Najbolje ocenjeni", "Najpopularniji", "Najnoviji" };

    public IReadOnlyList<string> TypeOptions { get; } =
        new[] { "Svi tipovi", "TV serije", "Filmovi", "OVA", "ONA", "Specijali" };

    public IReadOnlyList<string> StatusOptions { get; } =
        new[] { "Svi statusi", "U toku", "Završeno", "Najavljeno" };

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private int selectedSortIndex;
    [ObservableProperty] private int selectedTypeIndex;
    [ObservableProperty] private int selectedStatusIndex;
    [ObservableProperty] private string resultInfo = string.Empty;
    [ObservableProperty] private bool isOffline;
    [ObservableProperty] private string offlineNotice = string.Empty;

    /// <summary>True only while the very first page loads - the list shows skeleton cards.</summary>
    [ObservableProperty] private bool showSkeleton;

    // ---- Filter pills: current choice as text, and whether it differs from the default ----
    public string SortLabel => SortOptions[SelectedSortIndex];
    public string TypeLabel => TypeOptions[SelectedTypeIndex];
    public string StatusLabel => StatusOptions[SelectedStatusIndex];
    public bool IsTypeFiltered => SelectedTypeIndex != 0;
    public bool IsStatusFiltered => SelectedStatusIndex != 0;
    public bool HasActiveFilters =>
        SelectedSortIndex != 0 || IsTypeFiltered || IsStatusFiltered || !string.IsNullOrWhiteSpace(SearchText);

    /// <summary>Typing filters the catalogue too - debounced like the pickers.</summary>
    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasActiveFilters));
        ReloadIfNotBulk();
    }

    partial void OnSelectedSortIndexChanged(int value)
    {
        OnPropertyChanged(nameof(SortLabel));
        OnPropertyChanged(nameof(HasActiveFilters));
        ReloadIfNotBulk();
    }

    partial void OnSelectedTypeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(TypeLabel));
        OnPropertyChanged(nameof(IsTypeFiltered));
        OnPropertyChanged(nameof(HasActiveFilters));
        ReloadIfNotBulk();
    }

    partial void OnSelectedStatusIndexChanged(int value)
    {
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(IsStatusFiltered));
        OnPropertyChanged(nameof(HasActiveFilters));
        ReloadIfNotBulk();
    }

    [RelayCommand]
    private async Task PickSortAsync()
        => SelectedSortIndex = await PickAsync("Sortiranje", SortOptions, SelectedSortIndex);

    [RelayCommand]
    private async Task PickTypeAsync()
        => SelectedTypeIndex = await PickAsync("Tip", TypeOptions, SelectedTypeIndex);

    [RelayCommand]
    private async Task PickStatusAsync()
        => SelectedStatusIndex = await PickAsync("Status", StatusOptions, SelectedStatusIndex);

    /// <summary>Action sheet with the options; cancelling keeps the current choice.</summary>
    private static async Task<int> PickAsync(string title, IReadOnlyList<string> options, int current)
    {
        var choice = await Shell.Current.DisplayActionSheetAsync(title, "Otkaži", null, options.ToArray());
        var index = choice is null ? -1 : options.ToList().IndexOf(choice);
        return index >= 0 ? index : current;
    }

    /// <summary>
    /// Picker changes come in bursts (sort + tip + status). Instead of firing one HTTP
    /// request per change, wait out a short pause and load only the final combination.
    /// </summary>
    private void ReloadIfNotBulk()
    {
        if (_bulkUpdate) return;

        _debounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _debounceCts = cts;

        _ = DebouncedReloadAsync(cts.Token);
    }

    private async Task DebouncedReloadAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(FilterDebounceMs, ct);
        }
        catch (OperationCanceledException)
        {
            return; // a newer filter change took over
        }

        await LoadFirstPageAsync();
    }

    /// <summary>Called from the view's OnAppearing - loads the first page once.</summary>
    public async Task InitializeAsync()
    {
        if (Animes.Count == 0)
            await LoadFirstPageAsync();
    }

    [RelayCommand]
    private Task ApplyAsync() => LoadFirstPageAsync();

    [RelayCommand]
    private Task ResetFiltersAsync()
    {
        _bulkUpdate = true;
        SearchText = string.Empty;
        SelectedSortIndex = 0;
        SelectedTypeIndex = 0;
        SelectedStatusIndex = 0;
        _bulkUpdate = false;
        return LoadFirstPageAsync();
    }

    [RelayCommand]
    private async Task GoToDetailAsync(Anime? anime)
    {
        if (anime is null) return;
        await Shell.Current.GoToAsync($"{nameof(AnimeDetailView)}?id={anime.Id}");
    }

    /// <summary>Infinite scroll: bound to CollectionView.RemainingItemsThresholdReachedCommand.</summary>
    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (_loadingMore || IsBusy || !_hasMore || Animes.Count == 0)
            return;

        _loadingMore = true;
        var ct = _cts?.Token ?? CancellationToken.None;

        try
        {
            IsLoadingMore = true;

            var filter = BuildFilter();
            filter.Offset = _offset;
            filter.Limit = PageSize;

            var list = await _api.GetAnimeAsync(filter, ct);
            if (ct.IsCancellationRequested) return;

            var added = Append(list);
            _offset += list.Count;
            _hasMore = list.Count >= PageSize && added > 0;
            ResultInfo = $"{Animes.Count} rezultata";
        }
        catch (OperationCanceledException)
        {
            // filters changed mid-scroll - the reset load takes over
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri učitavanju sledeće strane: {ex.Message}";
            _hasMore = false;
        }
        finally
        {
            IsLoadingMore = false;
            _loadingMore = false;
        }
    }

    private AnimeFilter BuildFilter() => new()
    {
        Text = SearchText,
        Sort = SelectedSortIndex switch
        {
            1 => AnimeSort.TopRated,
            2 => AnimeSort.MostPopular,
            3 => AnimeSort.Newest,
            _ => AnimeSort.Trending
        },
        Subtype = SelectedTypeIndex switch
        {
            1 => "TV",
            2 => "movie",
            3 => "OVA",
            4 => "ONA",
            5 => "special",
            _ => null
        },
        Status = SelectedStatusIndex switch
        {
            1 => "current",
            2 => "finished",
            3 => "upcoming",
            _ => null
        }
    };

    /// <summary>Appends items whose id isn't already loaded; returns how many were added.</summary>
    private int Append(IEnumerable<Anime> list)
    {
        var added = 0;
        foreach (var a in list)
        {
            if (_loadedIds.Add(a.Id))
            {
                Animes.Add(a);
                added++;
            }
        }
        return added;
    }

    private async Task LoadFirstPageAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            IsBusy = true;
            ShowSkeleton = Animes.Count == 0;
            ErrorMessage = null;
            IsOffline = false;
            OfflineNotice = string.Empty;
            _offset = 0;
            _hasMore = true;

            // No point calling the API without a connection - show the last cached page.
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ShowCachedAsync("Nema internet konekcije.");
                return;
            }

            var filter = BuildFilter();
            filter.Offset = 0;
            filter.Limit = PageSize;

            var list = await _api.GetAnimeAsync(filter, ct);
            if (ct.IsCancellationRequested) return;

            Animes.Clear();
            _loadedIds.Clear();
            Append(list);
            _offset = list.Count;
            // The trending endpoint returns a short fixed set; still allow paging,
            // which then continues from the regular (sorted) anime endpoint.
            _hasMore = list.Count >= PageSize || filter.IsPlainTrending;

            // An empty result is shown by the list's EmptyView (with a reset button), not as an error.
            ResultInfo = Animes.Count == 0 ? string.Empty : $"{Animes.Count} rezultata";
            if (Animes.Count > 0)
                await _db.SaveAnimeCacheAsync(Animes.ToList());
        }
        catch (OperationCanceledException)
        {
            // superseded by a newer request - ignore
        }
        catch (Exception ex)
        {
            // The API is unreachable (flaky mobile data, DNS, timeout) - fall back to the cache.
            if (!await ShowCachedAsync("Kitsu API trenutno nije dostupan."))
                ErrorMessage = $"Greška pri učitavanju sa Kitsu API-ja: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
            ShowSkeleton = false;
        }
    }

    /// <summary>
    /// Fills the list from the locally cached page. Returns false when nothing is cached
    /// yet, so the caller can show a plain error instead.
    /// </summary>
    private async Task<bool> ShowCachedAsync(string reason)
    {
        var cached = await _db.GetCachedAnimeAsync();
        if (cached.Count == 0)
        {
            IsOffline = true;
            OfflineNotice = $"{reason} Nema sačuvanih podataka za offline prikaz.";
            return false;
        }

        Animes.Clear();
        _loadedIds.Clear();
        Append(cached);

        _hasMore = false; // paging needs the API
        _offset = cached.Count;

        var savedAt = await _db.GetAnimeCacheTimeAsync();
        IsOffline = true;
        OfflineNotice = savedAt is { } dt
            ? $"{reason} Prikazan je spisak sačuvan {dt.ToLocalTime():dd.MM.yyyy HH:mm}."
            : $"{reason} Prikazan je poslednji sačuvan spisak.";

        ResultInfo = $"{Animes.Count} sačuvanih";
        return true;
    }
}
