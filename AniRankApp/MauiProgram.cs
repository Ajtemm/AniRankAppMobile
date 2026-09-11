using Microsoft.Extensions.Logging;
using AniRankApp.Services;
using AniRankApp.ViewModels;
using AniRankApp.Views;

namespace AniRankApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ---- Services (singletons: shared for the whole app lifetime) ----
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<FileExportService>();
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<KitsuApiService>();

        // ---- Shell ----
        builder.Services.AddSingleton<AppShell>();

        // ---- ViewModels ----
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<ExploreViewModel>();
        builder.Services.AddTransient<AnimeDetailViewModel>();
        builder.Services.AddTransient<MyReviewsViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<AdminViewModel>();
        builder.Services.AddTransient<AdminUserReviewsViewModel>();

        // ---- Views ----
        builder.Services.AddTransient<LoginView>();
        builder.Services.AddTransient<RegisterView>();
        builder.Services.AddTransient<ExploreView>();
        builder.Services.AddTransient<AnimeDetailView>();
        builder.Services.AddTransient<MyReviewsView>();
        builder.Services.AddTransient<ProfileView>();
        builder.Services.AddTransient<AdminView>();
        builder.Services.AddTransient<AdminUserReviewsView>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
