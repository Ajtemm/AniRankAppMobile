using AniRankApp.Helpers;
using AniRankApp.Models;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class CommunityView : ContentPage
{
    private readonly CommunityViewModel _vm;

    public CommunityView() : this(ServiceHelper.GetService<CommunityViewModel>()) { }

    public CommunityView(CommunityViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private void OnUserSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CommunityUserItem item) return;

        UserList.SelectedItem = null;
        _vm.OpenUserCommand.Execute(item);
    }

    private void OnFeedEntrySelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Review review) return;

        FeedList.SelectedItem = null;
        _vm.OpenFeedEntryCommand.Execute(review);
    }

    private void OnFollowClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: CommunityUserItem item })
            _vm.ToggleFollowCommand.Execute(item);
    }
}
