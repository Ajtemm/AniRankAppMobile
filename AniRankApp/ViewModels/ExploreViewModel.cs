using System.Collections.ObjectModel;
using AniRankApp.Models;
using AniRankApp.Services;
using AniRankApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

public partial class ExploreViewModel : BaseViewModel
{
    private const int PageSize = 20;

    private readonly KitsuApiService _api;
    private CancellationTokenSource? _cts;
    private bool _bulkUpdate;
    private bool _loadingMore;
    private int _offset;
    private bool _hasMore = true;

    public ExploreViewModel(KitsuApiService api)
    {
        _api = api;
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

    partial void OnSelectedSortIndexChanged(int value) => ReloadIfNotBulk();
    partial void OnSelectedTypeIndexChanged(int value) => ReloadIfNotBulk();
    partial void OnSelectedStatusIndexChanged(int value) => ReloadIfNotBulk();

    private void ReloadIfNotBulk()
    {
        if (!_bulkUpdate)
            ApplyCommand.Execute(null);
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
        var seen = Animes.Select(a => a.Id).ToHashSet();
        var added = 0;
        foreach (var a in list)
        {
            if (seen.Add(a.Id))
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
            ErrorMessage = null;
            _offset = 0;
            _hasMore = true;

            var filter = BuildFilter();
            filter.Offset = 0;
            filter.Limit = PageSize;

            var list = await _api.GetAnimeAsync(filter, ct);
            if (ct.IsCancellationRequested) return;

            Animes.Clear();
            Append(list);
            _offset = list.Count;
            // The trending endpoint returns a short fixed set; still allow paging,
            // which then continues from the regular (sorted) anime endpoint.
            _hasMore = list.Count >= PageSize || filter.IsPlainTrending;

            ResultInfo = Animes.Count == 0 ? string.Empty : $"{Animes.Count} rezultata";
            if (Animes.Count == 0)
                ErrorMessage = "Nema rezultata za zadate filtere.";
        }
        catch (OperationCanceledException)
        {
            // superseded by a newer request - ignore
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri učitavanju sa Kitsu API-ja: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }
}
