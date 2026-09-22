using AniRankApp.Helpers;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class UserProfileView : ContentPage
{
    private readonly UserProfileViewModel _vm;

    public UserProfileView() : this(ServiceHelper.GetService<UserProfileViewModel>()) { }

    public UserProfileView(UserProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    private void OnStatusTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Element { BindingContext: StatusCountItem item })
            _vm.OpenStatusCommand.Execute(item);
    }
}
