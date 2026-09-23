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
                fonts.AddFont("Poppins-SemiBold.ttf", "PoppinsSemibold");
                fonts.AddFont("Poppins-Bold.ttf", "PoppinsBold");
                // Material Icons (Round) - tab bar and inline icons use it through the "Icons" alias.
                fonts.AddFont("MaterialIconsRound-Regular.otf", "Icons");
            });

        // ---- Services (singletons: shared for the whole app lifetime) ----
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<AuthService>();
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
        builder.Services.AddTransient<CommunityViewModel>();
        builder.Services.AddTransient<UserProfileViewModel>();
        builder.Services.AddTransient<TopListViewModel>();
        builder.Services.AddTransient<AdminViewModel>();
        builder.Services.AddTransient<AdminUserReviewsViewModel>();
        builder.Services.AddTransient<LogoutViewModel>();

        // ---- Views ----
        builder.Services.AddTransient<LoginView>();
        builder.Services.AddTransient<RegisterView>();
        builder.Services.AddTransient<ExploreView>();
        builder.Services.AddTransient<AnimeDetailView>();
        builder.Services.AddTransient<MyReviewsView>();
        builder.Services.AddTransient<ProfileView>();
        builder.Services.AddTransient<CommunityView>();
        builder.Services.AddTransient<UserProfileView>();
        builder.Services.AddTransient<TopListView>();
        builder.Services.AddTransient<AdminView>();
        builder.Services.AddTransient<AdminUserReviewsView>();
        builder.Services.AddTransient<LogoutView>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
