using AniRankApp.Helpers;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class RegisterView : ContentPage
{
    public RegisterView() : this(ServiceHelper.GetService<RegisterViewModel>()) { }

    public RegisterView(RegisterViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
