using AniRankApp.Helpers;
using AniRankApp.Models;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class ExploreView : ContentPage
{
    /// <summary>Minimum width of one result card before another grid column is added.</summary>
    private const double MinCardWidth = 380;

    private readonly ExploreViewModel _vm;

    public ExploreView() : this(ServiceHelper.GetService<ExploreViewModel>()) { }

    public ExploreView(ExploreViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }

    /// <summary>Phone: one column. Tablet / desktop window: as many columns as fit.</summary>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0) return;

        var span = Math.Clamp((int)(width / MinCardWidth), 1, 4);
        if (AnimeGrid.Span != span)
            AnimeGrid.Span = span;
    }

    private void OnAnimeSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Anime anime)
            _vm.GoToDetailCommand.Execute(anime);

        if (sender is CollectionView cv)
            cv.SelectedItem = null;
    }
}
