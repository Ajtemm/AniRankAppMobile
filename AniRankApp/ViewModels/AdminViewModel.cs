using System.Collections.ObjectModel;
using AniRankApp.Models;
using AniRankApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AniRankApp.ViewModels;

public partial class AdminViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly AuthService _auth;
    private readonly List<User> _allUsers = new();

    public AdminViewModel(DatabaseService db, AuthService auth)
    {
        _db = db;
        _auth = auth;
        Title = "Admin Panel";
    }

    /// <summary>Filtered users view bound to the CollectionView.</summary>
    public ObservableCollection<User> Users { get; } = new();

    [ObservableProperty] private string userSearch = string.Empty;

    partial void OnUserSearchChanged(string value) => ApplyUserFilter();

    // ---- Manual "add user" form ----
    [ObservableProperty] private string newUsername = string.Empty;
    [ObservableProperty] private string newEmail = string.Empty;
    [ObservableProperty] private string newPassword = string.Empty;
    [ObservableProperty] private bool newUserIsAdmin;
    [ObservableProperty] private string? addUserMessage;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var users = await _db.GetAllUsersAsync();
            _allUsers.Clear();
            _allUsers.AddRange(users);
            ApplyUserFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri učitavanju: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyUserFilter()
    {
        IEnumerable<User> query = _allUsers;

        var term = UserSearch?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(u =>
                u.Username.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        Users.Clear();
        foreach (var u in query)
            Users.Add(u);
    }

    /// <summary>Lets an admin create a user account by hand, optionally as another Admin.</summary>
    [RelayCommand]
    private async Task AddUserAsync()
    {
        if (IsBusy) return;

        ErrorMessage = null;
        AddUserMessage = null;

        try
        {
            IsBusy = true;
            var role = NewUserIsAdmin ? "Admin" : "User";
            var (ok, error) = await _auth.RegisterAsync(NewUsername, NewEmail, NewPassword, role);
            if (!ok)
            {
                ErrorMessage = error;
                return;
            }

            AddUserMessage = $"Korisnik \"{NewUsername.Trim()}\" je uspešno dodat.";
            NewUsername = NewEmail = NewPassword = string.Empty;
            NewUserIsAdmin = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Greška pri dodavanju korisnika: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Open one user's reviews on a dedicated page.</summary>
    [RelayCommand]
    private async Task OpenUserReviewsAsync(User? user)
    {
        if (user is null) return;
        await Shell.Current.GoToAsync($"adminuserreviews?userId={user.Id}");
    }

    /// <summary>Open every review in the system (userId = 0).</summary>
    [RelayCommand]
    private Task OpenAllReviewsAsync()
        => Shell.Current.GoToAsync("adminuserreviews?userId=0");

    /// <summary>Bans/unbans a user. Preferred over deleting - the account and its
    /// reviews stay, but a banned user can no longer log in.</summary>
    [RelayCommand]
    private async Task ToggleBanAsync(User? user)
    {
        if (user is null) return;

        if (user.Id == _auth.CurrentUserId)
        {
            await Shell.Current.DisplayAlertAsync("Nije dozvoljeno", "Ne možete banovati sopstveni nalog.", "OK");
            return;
        }

        var banning = !user.IsBanned;
        var confirm = await Shell.Current.DisplayAlertAsync(
            banning ? "Banovanje korisnika" : "Ukidanje bana",
            banning
                ? $"Banovati korisnika \"{user.Username}\"? Neće moći da se prijavi dok mu se ban ne ukine."
                : $"Ukinuti ban korisniku \"{user.Username}\"?",
            banning ? "Banuj" : "Odbanuj",
            "Otkaži");
        if (!confirm) return;

        await _db.SetUserBannedAsync(user.Id, banning);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteUserAsync(User? user)
    {
        if (user is null) return;

        if (user.Id == _auth.CurrentUserId)
        {
            await Shell.Current.DisplayAlertAsync("Nije dozvoljeno", "Ne možete obrisati sopstveni nalog.", "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Trajno brisanje korisnika",
            $"Obrisati korisnika \"{user.Username}\" i sve njegove recenzije? Ova akcija je nepovratna - razmotrite banovanje umesto brisanja.",
            "Obriši", "Otkaži");
        if (!confirm) return;

        await _db.DeleteUserAsync(user.Id);
        _allUsers.RemoveAll(u => u.Id == user.Id);
        ApplyUserFilter();
    }
}
