using Microsoft.Maui.Controls.Shapes;

namespace AniRankApp.Helpers;

/// <summary>
/// Short confirmation message that doesn't interrupt the user (no "OK" to tap).
/// Used instead of <c>DisplayAlert</c> for successful actions.
/// On Android it is the native toast; elsewhere a snackbar-style pill fades in over the
/// bottom of the current page (every page's root is a Grid), with an alert only as a last resort.
/// </summary>
public static class ToastHelper
{
    private const int VisibleMs = 2200;

    public static Task ShowAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return Task.CompletedTask;

#if ANDROID
        // Toasts must be raised on the UI thread.
        MainThread.BeginInvokeOnMainThread(() =>
            Android.Widget.Toast
                .MakeText(Android.App.Application.Context, message, Android.Widget.ToastLength.Short)
                ?.Show());

        return Task.CompletedTask;
#else
        return MainThread.InvokeOnMainThreadAsync(() => ShowInPageAsync(message));
#endif
    }

    private static async Task ShowInPageAsync(string message)
    {
        var page = Shell.Current?.CurrentPage;
        if (page is not ContentPage { Content: Grid root })
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlertAsync("Gotovo", message, "OK");
            return;
        }

        var label = new Label
        {
            Text = message,
            TextColor = Colors.White,
            FontSize = 14,
            VerticalOptions = LayoutOptions.Center
        };

        var toast = new Border
        {
            Padding = new Thickness(18, 12),
            Margin = new Thickness(16, 0, 16, 28),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.End,
            BackgroundColor = Color.FromArgb("#34344A"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.5f, Radius = 14, Offset = new Point(0, 4) },
            InputTransparent = true,
            ZIndex = 100,
            Opacity = 0,
            Content = label
        };

        Grid.SetRowSpan(toast, Math.Max(1, root.RowDefinitions.Count));
        Grid.SetColumnSpan(toast, Math.Max(1, root.ColumnDefinitions.Count));
        root.Children.Add(toast);
        SemanticScreenReader.Announce(message);

        try
        {
            await toast.FadeToAsync(1, 160);
            await Task.Delay(VisibleMs);
            await toast.FadeToAsync(0, 220);
        }
        finally
        {
            root.Children.Remove(toast);
        }
    }
}
