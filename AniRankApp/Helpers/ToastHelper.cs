namespace AniRankApp.Helpers;

/// <summary>
/// Short confirmation message that doesn't interrupt the user (no "OK" to tap).
/// Used instead of <c>DisplayAlert</c> for successful actions.
/// On Android it is the native toast; other platforms fall back to an alert so the
/// message is never silently lost.
/// </summary>
public static class ToastHelper
{
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
        return Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.DisplayAlertAsync("Gotovo", message, "OK");
#endif
    }
}
