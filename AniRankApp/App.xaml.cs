namespace AniRankApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // The whole app uses a dark palette.
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // AppShell decides in its constructor whether to show the login screen
        // or the main tabs (based on a saved session in Preferences).
        return new Window(new AppShell());
    }
}
