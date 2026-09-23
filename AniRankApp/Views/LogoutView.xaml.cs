using AniRankApp.Helpers;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class LogoutView : ContentPage
{
    private readonly LogoutViewModel _vm;

    public LogoutView() : this(ServiceHelper.GetService<LogoutViewModel>()) { }

    public LogoutView(LogoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Refresh();
    }
}
