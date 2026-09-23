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

    // "Next" on the keyboard walks through the form.
    private void OnUsernameCompleted(object? sender, EventArgs e) => EmailEntry.Focus();
    private void OnEmailCompleted(object? sender, EventArgs e) => PasswordEntry.Focus();
    private void OnPasswordCompleted(object? sender, EventArgs e) => ConfirmEntry.Focus();

    private void OnTogglePasswordClicked(object? sender, EventArgs e)
        => PasswordVisibility.Toggle(PasswordEntry, PasswordToggle);
}
