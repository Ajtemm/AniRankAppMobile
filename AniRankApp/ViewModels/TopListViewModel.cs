using System.Collections.ObjectModel;
using AniRankApp.Models;
using AniRankApp.Services;
using AniRankApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

/// <summary>
/// "Top lista zajednice": anime ranked by the ratings given inside this app, not by
/// the Kitsu score. Built from the local Reviews table.
/// </summary>
public partial class TopListViewModel : BaseViewModel
{
    private readonly DatabaseService _db;

    public TopListViewModel(DatabaseService db)
    {
        _db = db;
        Title = "Top lista zajednice";
    }

    public ObservableCollection<CommunityRankItem> Items { get; } = new();

    /// <summary>How the list is ranked: by average rating or by number of ratings.</summary>
    public IReadOnlyList<string> SortOptions { get; } = new[] { "Najbolje ocenjeni", "Najpopularniji" };

    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private int selectedSortIndex;

    /// <summary>Chip-friendly view of <see cref="SelectedSortIndex"/>.</summary>
    public string SelectedSortOption
    {
        get => SortOptions[SelectedSortIndex];
        set
        {
            var index = SortOptions.ToList().IndexOf(value);
            if (index >= 0) SelectedSortIndex = index;
        }
    }

    partial void OnSelectedSortIndexChanged(int value)
    {
        OnPropertyChanged(nameof(SelectedSortOption));
        _ = LoadAsync();
    }

    [RelayCommand]
    private Task GoToExploreAsync() => Shell.Current.GoToAsync("//explore");

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;
            ErrorMessage = null;

            var top = await _db.GetCommunityTopAsync(byPopularity: SelectedSortIndex == 1);

            Items.Clear();
            foreach (var item in top)
                Items.Add(item);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri učitavanju top liste: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenAnimeAsync(CommunityRankItem? item)
        => item is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync($"{nameof(AnimeDetailView)}?id={item.AnimeKitsuId}");
}
