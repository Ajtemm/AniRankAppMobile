using AniRankApp.Helpers;
using AniRankApp.Models;
using SQLite;

namespace AniRankApp.Services;

/// <summary>
/// Async SQLite access (sqlite-net-pcl). Owns connection setup, table creation,
/// the default Admin seed, and all CRUD for users &amp; reviews.
/// </summary>
public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    private const string DbFileName = "anirank.db3";

    private async Task InitAsync()
    {
        if (_db is not null) return;

        var path = Path.Combine(FileSystem.AppDataDirectory, DbFileName);
        _db = new SQLiteAsyncConnection(
            path,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _db.CreateTableAsync<User>();
        await _db.CreateTableAsync<Review>();

        await SeedAdminAsync();
    }

    /// <summary>Creates the default Admin account on first launch.</summary>
    private async Task SeedAdminAsync()
    {
        var admin = await _db!.Table<User>().Where(u => u.Username == "admin").FirstOrDefaultAsync();
        if (admin is not null) return;

        await _db.InsertAsync(new User
        {
            Username = "admin",
            Email = "admin@anirank.local",
            PasswordHash = SecurityHelper.Hash("admin123"),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        });
    }

    // ==================== USERS ====================

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        await InitAsync();
        return await _db!.Table<User>().Where(u => u.Username == username).FirstOrDefaultAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        await InitAsync();
        return await _db!.FindAsync<User>(id);
    }

    public async Task<int> InsertUserAsync(User user)
    {
        await InitAsync();
        return await _db!.InsertAsync(user);
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        await InitAsync();
        return await _db!.Table<User>().OrderBy(u => u.Username).ToListAsync();
    }

    /// <summary>Deletes a user together with all of their reviews.</summary>
    public async Task<int> DeleteUserAsync(int userId)
    {
        await InitAsync();
        await _db!.Table<Review>().DeleteAsync(r => r.UserId == userId);
        return await _db.DeleteAsync<User>(userId);
    }

    /// <summary>Bans / unbans a user instead of deleting the account. Banned users can't log in.</summary>
    public async Task<int> SetUserBannedAsync(int userId, bool isBanned)
    {
        await InitAsync();
        var user = await _db!.FindAsync<User>(userId);
        if (user is null) return 0;

        user.IsBanned = isBanned;
        return await _db.UpdateAsync(user);
    }

    // ==================== REVIEWS ====================

    public async Task<int> InsertReviewAsync(Review review)
    {
        await InitAsync();
        return await _db!.InsertAsync(review);
    }

    public async Task<int> UpdateReviewAsync(Review review)
    {
        await InitAsync();
        return await _db!.UpdateAsync(review);
    }

    public async Task<List<Review>> GetReviewsForAnimeAsync(string animeKitsuId)
    {
        await InitAsync();
        return await _db!.Table<Review>()
            .Where(r => r.AnimeKitsuId == animeKitsuId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Review>> GetUserReviewsAsync(int userId)
    {
        await InitAsync();
        return await _db!.Table<Review>()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Review>> GetAllReviewsAsync()
    {
        await InitAsync();
        return await _db!.Table<Review>()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> DeleteReviewAsync(int reviewId)
    {
        await InitAsync();
        return await _db!.DeleteAsync<Review>(reviewId);
    }
}
