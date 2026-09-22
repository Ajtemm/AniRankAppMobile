using AniRankApp.Helpers;
using AniRankApp.Models;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class TopListView : ContentPage
{
    private readonly TopListViewModel _vm;

    public TopListView() : this(ServiceHelper.GetService<TopListViewModel>()) { }

    public TopListView(TopListViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private void OnItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CommunityRankItem item) return;

        TopList.SelectedItem = null;
        _vm.OpenAnimeCommand.Execute(item);
    }
}
