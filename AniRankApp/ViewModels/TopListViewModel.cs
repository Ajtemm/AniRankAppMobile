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

    /// <summary>Minimum number of ratings an anime needs to appear.</summary>
    public IReadOnlyList<string> MinVotesOptions { get; } = new[] { "Sve ocene", "Bar 2 ocene", "Bar 3 ocene" };

    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private int selectedMinVotesIndex;

    partial void OnSelectedMinVotesIndexChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;
            ErrorMessage = null;

            var top = await _db.GetCommunityTopAsync(SelectedMinVotesIndex + 1);

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
