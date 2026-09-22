using AniRankApp.Helpers;
using AniRankApp.Models;
using SQLite;

namespace AniRankApp.Services;

/// <summary>
/// Async SQLite access (sqlite-net-pcl). Owns connection setup, table creation,
/// the default Admin seed, and all CRUD for users, reviews &amp; follows.
/// </summary>
public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    /// <summary>Guards <see cref="InitAsync"/> - two screens can hit the database at once.</summary>
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private const string DbFileName = "anirank.db3";

    private async Task InitAsync()
    {
        if (_db is not null) return;

        await _initLock.WaitAsync();
        try
        {
            // Re-check: another caller may have finished while we waited for the lock.
            if (_db is not null) return;

            var path = Path.Combine(FileSystem.AppDataDirectory, DbFileName);
            var db = new SQLiteAsyncConnection(
                path,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            await db.CreateTableAsync<User>();
            await db.CreateTableAsync<Review>();
            await db.CreateTableAsync<Follow>();
            await db.CreateTableAsync<ReviewLike>();
            await db.CreateTableAsync<CachedAnime>();

            await SeedAdminAsync(db);

            // Published only once the schema is ready, so no caller sees a half-built database.
            _db = db;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>Creates the default Admin account on first launch.</summary>
    private static async Task SeedAdminAsync(SQLiteAsyncConnection db)
    {
        var admin = await db.Table<User>().Where(u => u.Username == "admin").FirstOrDefaultAsync();
        if (admin is not null) return;

        await db.InsertAsync(new User
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

    /// <summary>
    /// Id -&gt; username for every account, in a single projected query. Lets screens that
    /// show many reviews resolve authors at once instead of one lookup per review.
    /// </summary>
    public async Task<Dictionary<int, string>> GetUsernamesAsync()
    {
        await InitAsync();
        var rows = await _db!.QueryAsync<User>("SELECT Id, Username FROM Users");
        return rows.ToDictionary(u => u.Id, u => u.Username);
    }

    /// <summary>Every account except the given one, banned users left out (community list).</summary>
    public async Task<List<User>> GetOtherUsersAsync(int currentUserId)
    {
        await InitAsync();
        return await _db!.Table<User>()
            .Where(u => u.Id != currentUserId && !u.IsBanned)
            .OrderBy(u => u.Username)
            .ToListAsync();
    }

    /// <summary>Deletes a user together with all of their reviews, likes and follow relations.</summary>
    public async Task<int> DeleteUserAsync(int userId)
    {
        await InitAsync();

        // Likes given by this user, plus likes left on the reviews we are about to delete.
        await _db!.ExecuteAsync(
            "DELETE FROM ReviewLikes WHERE UserId = ? OR ReviewId IN (SELECT Id FROM Reviews WHERE UserId = ?)",
            userId, userId);

        await _db.Table<Review>().DeleteAsync(r => r.UserId == userId);
        await _db.Table<Follow>().DeleteAsync(f => f.FollowerId == userId || f.FollowingId == userId);
        return await _db.DeleteAsync<User>(userId);
    }

    /// <summary>Makes the profile private (list visible to followers only) or public again.</summary>
    public async Task<int> SetUserPrivateAsync(int userId, bool isPrivate)
    {
        await InitAsync();
        var user = await _db!.FindAsync<User>(userId);
        if (user is null) return 0;

        user.IsPrivate = isPrivate;
        return await _db.UpdateAsync(user);
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

    public async Task<Review?> GetReviewByIdAsync(int reviewId)
    {
        await InitAsync();
        return await _db!.FindAsync<Review>(reviewId);
    }

    public async Task<List<Review>> GetAllReviewsAsync()
    {
        await InitAsync();
        return await _db!.Table<Review>()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Deletes a review and every "useful" mark left on it.</summary>
    public async Task<int> DeleteReviewAsync(int reviewId)
    {
        await InitAsync();
        await _db!.Table<ReviewLike>().DeleteAsync(l => l.ReviewId == reviewId);
        return await _db.DeleteAsync<Review>(reviewId);
    }

    /// <summary>
    /// Newest entries of the given users (the "friends" feed). Returns nothing when the
    /// list is empty; the IN-list is built from ints we control, never from user text.
    /// </summary>
    public async Task<List<Review>> GetFeedAsync(IReadOnlyCollection<int> userIds, int limit = 40)
    {
        if (userIds.Count == 0) return new List<Review>();

        await InitAsync();

        var ids = string.Join(",", userIds.Select(id => id.ToString()));
        return await _db!.QueryAsync<Review>(
            $"SELECT * FROM Reviews WHERE UserId IN ({ids}) " +
            "ORDER BY MAX(CreatedAt, UpdatedAt) DESC LIMIT ?", limit);
    }

    /// <summary>
    /// Top anime by the ratings given inside this app. Only rated entries count
    /// ("Plan to Watch" / "Dropped" are stored with Rating = 0).
    /// </summary>
    public async Task<List<CommunityRankItem>> GetCommunityTopAsync(int minVotes = 1, int limit = 30)
    {
        await InitAsync();

        var rows = await _db!.QueryAsync<Review>(
            "SELECT AnimeKitsuId, AnimeTitle, AnimeImageUrl, Rating FROM Reviews WHERE Rating >= 1");

        return rows
            .GroupBy(r => r.AnimeKitsuId)
            .Where(g => g.Count() >= minVotes)
            .Select(g => new CommunityRankItem(
                g.Key,
                g.First().AnimeTitle,
                g.First().AnimeImageUrl,
                Math.Round(g.Average(r => r.Rating), 1),
                g.Count()))
            .OrderByDescending(x => x.Average)
            .ThenByDescending(x => x.Votes)
            .Take(limit)
            .Select((x, i) => x with { Rank = i + 1 })
            .ToList();
    }

    /// <summary>Local community score for one anime: average of real ratings and how many there are.</summary>
    public async Task<(double average, int votes)> GetAnimeRatingStatsAsync(string animeKitsuId)
    {
        await InitAsync();

        var votes = await _db!.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Reviews WHERE AnimeKitsuId = ? AND Rating >= 1", animeKitsuId);
        if (votes == 0) return (0, 0);

        var avg = await _db.ExecuteScalarAsync<double>(
            "SELECT AVG(Rating) FROM Reviews WHERE AnimeKitsuId = ? AND Rating >= 1", animeKitsuId);

        return (Math.Round(avg, 1), votes);
    }

    /// <summary>Number of reviews per user id - one query, only the UserId column.</summary>
    public async Task<Dictionary<int, int>> GetReviewCountsByUserAsync()
    {
        await InitAsync();
        var rows = await _db!.QueryAsync<Review>("SELECT UserId FROM Reviews");
        return rows.GroupBy(r => r.UserId).ToDictionary(g => g.Key, g => g.Count());
    }

    // ==================== FOLLOWS ====================

    /// <summary>Ids of the users <paramref name="userId"/> follows.</summary>
    public async Task<List<int>> GetFollowingIdsAsync(int userId)
    {
        await InitAsync();
        var rows = await _db!.Table<Follow>().Where(f => f.FollowerId == userId).ToListAsync();
        return rows.Select(f => f.FollowingId).ToList();
    }

    /// <summary>Ids of the users that follow <paramref name="userId"/>.</summary>
    public async Task<List<int>> GetFollowerIdsAsync(int userId)
    {
        await InitAsync();
        var rows = await _db!.Table<Follow>().Where(f => f.FollowingId == userId).ToListAsync();
        return rows.Select(f => f.FollowerId).ToList();
    }

    /// <summary>Number of followers per user id - one query, only the FollowingId column.</summary>
    public async Task<Dictionary<int, int>> GetFollowerCountsAsync()
    {
        await InitAsync();
        var rows = await _db!.QueryAsync<Follow>("SELECT FollowingId FROM Follows");
        return rows.GroupBy(f => f.FollowingId).ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>How many people follow this user (COUNT in SQL, no rows materialised).</summary>
    public async Task<int> CountFollowersAsync(int userId)
    {
        await InitAsync();
        return await _db!.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Follows WHERE FollowingId = ?", userId);
    }

    /// <summary>How many people this user follows.</summary>
    public async Task<int> CountFollowingAsync(int userId)
    {
        await InitAsync();
        return await _db!.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Follows WHERE FollowerId = ?", userId);
    }

    public async Task<bool> IsFollowingAsync(int followerId, int followingId)
    {
        await InitAsync();
        var row = await _db!.Table<Follow>()
            .Where(f => f.FollowerId == followerId && f.FollowingId == followingId)
            .FirstOrDefaultAsync();
        return row is not null;
    }

    /// <summary>
    /// Adds the relation if it isn't there yet (no self-follow). The unique index on
    /// (FollowerId, FollowingId) is the real guard - a duplicate insert is ignored.
    /// </summary>
    public async Task FollowAsync(int followerId, int followingId)
    {
        if (followerId <= 0 || followingId <= 0 || followerId == followingId) return;

        await InitAsync();

        await _db!.ExecuteAsync(
            "INSERT OR IGNORE INTO Follows (FollowerId, FollowingId, CreatedAt) VALUES (?, ?, ?)",
            followerId, followingId, DateTime.UtcNow);
    }

    public async Task UnfollowAsync(int followerId, int followingId)
    {
        await InitAsync();
        await _db!.Table<Follow>()
            .DeleteAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
    }

    /// <summary>Follow relations pointing at this user that are newer than the given moment.</summary>
    public async Task<int> CountFollowersSinceAsync(int userId, DateTime since)
    {
        await InitAsync();
        return await _db!.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Follows WHERE FollowingId = ? AND CreatedAt > ?", userId, since);
    }

    // ==================== REVIEW LIKES ("korisno") ====================

    /// <summary>Like count per review id, for a whole list at once.</summary>
    public async Task<Dictionary<int, int>> GetLikeCountsAsync()
    {
        await InitAsync();
        var rows = await _db!.QueryAsync<ReviewLike>("SELECT ReviewId FROM ReviewLikes");
        return rows.GroupBy(l => l.ReviewId).ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>Ids of the reviews this user already marked as useful.</summary>
    public async Task<HashSet<int>> GetLikedReviewIdsAsync(int userId)
    {
        await InitAsync();
        var rows = await _db!.QueryAsync<ReviewLike>(
            "SELECT ReviewId FROM ReviewLikes WHERE UserId = ?", userId);
        return rows.Select(l => l.ReviewId).ToHashSet();
    }

    /// <summary>Marks a review useful. The unique index makes a repeated tap a no-op.</summary>
    public async Task LikeReviewAsync(int reviewId, int userId)
    {
        if (reviewId <= 0 || userId <= 0) return;

        await InitAsync();
        await _db!.ExecuteAsync(
            "INSERT OR IGNORE INTO ReviewLikes (ReviewId, UserId, CreatedAt) VALUES (?, ?, ?)",
            reviewId, userId, DateTime.UtcNow);
    }

    public async Task UnlikeReviewAsync(int reviewId, int userId)
    {
        await InitAsync();
        await _db!.Table<ReviewLike>()
            .DeleteAsync(l => l.ReviewId == reviewId && l.UserId == userId);
    }

    // ==================== OFFLINE CACHE (Explore) ====================

    /// <summary>Replaces the cached Explore page with the one we just loaded from the API.</summary>
    public async Task SaveAnimeCacheAsync(IReadOnlyList<Anime> animes)
    {
        if (animes.Count == 0) return;

        await InitAsync();
        await _db!.ExecuteAsync("DELETE FROM CachedAnime");
        await _db.InsertAllAsync(animes.Select((a, i) => CachedAnime.From(a, i)));
    }

    /// <summary>The last cached Explore page, in its original order.</summary>
    public async Task<List<Anime>> GetCachedAnimeAsync()
    {
        await InitAsync();
        var rows = await _db!.Table<CachedAnime>().OrderBy(c => c.SortOrder).ToListAsync();
        return rows.Select(c => c.ToAnime()).ToList();
    }

    /// <summary>When the cache was written (null when nothing is cached yet).</summary>
    public async Task<DateTime?> GetAnimeCacheTimeAsync()
    {
        await InitAsync();
        var row = await _db!.Table<CachedAnime>().FirstOrDefaultAsync();
        return row?.CachedAt;
    }
}
