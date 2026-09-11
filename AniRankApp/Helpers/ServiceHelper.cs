namespace AniRankApp.Helpers;

/// <summary>
/// Bridge to the DI container for objects that may be created without constructor
/// injection (e.g. Shell ContentTemplate pages, AppShell inside App.CreateWindow).
/// </summary>
public static class ServiceHelper
{
    public static T GetService<T>() where T : notnull
        => Current.GetService<T>()
           ?? throw new InvalidOperationException($"Servis {typeof(T).Name} nije registrovan u DI kontejneru.");

    private static IServiceProvider Current =>
        IPlatformApplication.Current?.Services
        ?? throw new InvalidOperationException("Service provider još nije dostupan.");
}
