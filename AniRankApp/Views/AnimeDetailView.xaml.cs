using AniRankApp.Helpers;
using AniRankApp.Models;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class AnimeDetailView : ContentPage
{
    private readonly AnimeDetailViewModel _vm;

    public AnimeDetailView() : this(ServiceHelper.GetService<AnimeDetailViewModel>()) { }

    public AnimeDetailView(AnimeDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    private void OnLikeClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: Review review })
            _vm.ToggleLikeCommand.Execute(review);
    }
}
