using AniRankApp.Helpers;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class AnimeDetailView : ContentPage
{
    public AnimeDetailView() : this(ServiceHelper.GetService<AnimeDetailViewModel>()) { }

    public AnimeDetailView(AnimeDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
