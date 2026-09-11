using AniRankApp.Helpers;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class LoginView : ContentPage
{
    public LoginView() : this(ServiceHelper.GetService<LoginViewModel>()) { }

    public LoginView(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
