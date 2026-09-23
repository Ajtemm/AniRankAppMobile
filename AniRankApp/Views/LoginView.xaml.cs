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

#if DEBUG
        // The seeded admin account is a development convenience - never advertise it in Release.
        DemoHint.IsVisible = true;
#endif
    }

    private void OnUsernameCompleted(object? sender, EventArgs e) => PasswordEntry.Focus();

    private void OnTogglePasswordClicked(object? sender, EventArgs e)
        => PasswordVisibility.Toggle(PasswordEntry, PasswordToggle);
}
