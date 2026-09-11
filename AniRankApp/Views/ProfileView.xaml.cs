using AniRankApp.Helpers;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class ProfileView : ContentPage
{
    private readonly ProfileViewModel _vm;

    public ProfileView() : this(ServiceHelper.GetService<ProfileViewModel>()) { }

    public ProfileView(ProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
