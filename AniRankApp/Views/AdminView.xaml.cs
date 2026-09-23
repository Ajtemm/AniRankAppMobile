using AniRankApp.Helpers;
using AniRankApp.Models;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class AdminView : ContentPage
{
    private readonly AdminViewModel _vm;

    public AdminView() : this(ServiceHelper.GetService<AdminViewModel>()) { }

    public AdminView(AdminViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    /// <summary>Expands / collapses the "add user" form; the arrow flips with it.</summary>
    private void OnAddUserHeaderTapped(object? sender, TappedEventArgs e)
    {
        AddUserForm.IsVisible = !AddUserForm.IsVisible;
        AddUserChevron.Rotation = AddUserForm.IsVisible ? 180 : 0;
    }

    private void OnOpenUserReviewsClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: User user })
            _vm.OpenUserReviewsCommand.Execute(user);
    }

    private void OnToggleBanClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: User user })
            _vm.ToggleBanCommand.Execute(user);
    }

    private void OnDeleteUserClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: User user })
            _vm.DeleteUserCommand.Execute(user);
    }
}
