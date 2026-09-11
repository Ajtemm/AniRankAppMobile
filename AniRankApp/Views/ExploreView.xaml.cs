using AniRankApp.Helpers;
using AniRankApp.Models;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class ExploreView : ContentPage
{
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

    private void OnAnimeSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Anime anime)
            _vm.GoToDetailCommand.Execute(anime);

        if (sender is CollectionView cv)
            cv.SelectedItem = null;
    }
}
