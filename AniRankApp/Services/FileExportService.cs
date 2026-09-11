using System.Text;
using System.Text.Json;
using AniRankApp.Models;

namespace AniRankApp.Services;

/// <summary>
/// Writes the user's reviews to a local .json or .txt file in app storage,
/// then offers the OS share sheet so the file can be saved elsewhere.
/// </summary>
public class FileExportService
{
    public enum ExportFormat { Json, Txt }

    public async Task<string> ExportReviewsAsync(IEnumerable<Review> reviews, string username, ExportFormat format)
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "exports");
        Directory.CreateDirectory(dir);

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var ext = format == ExportFormat.Json ? "json" : "txt";
        var path = Path.Combine(dir, $"reviews_{username}_{stamp}.{ext}");

        if (format == ExportFormat.Json)
        {
            var payload = reviews.Select(r => new
            {
                r.AnimeTitle,
                r.AnimeKitsuId,
                r.Rating,
                r.Comment,
                CreatedAt = r.CreatedAt.ToString("o")
            });

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Recenzije korisnika: {username}");
            sb.AppendLine($"Datum izvoza: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine(new string('=', 44));

            foreach (var r in reviews)
            {
                sb.AppendLine($"Anime   : {r.AnimeTitle}");
                sb.AppendLine($"Ocena   : {r.Rating:0.0}/10");
                sb.AppendLine($"Komentar: {r.Comment}");
                sb.AppendLine($"Datum   : {r.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}");
                sb.AppendLine(new string('-', 44));
            }

            await File.WriteAllTextAsync(path, sb.ToString());
        }

        // Let the user push the file to Drive / Files / email etc.
        try
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Izvoz recenzija",
                File = new ShareFile(path)
            });
        }
        catch
        {
            // Sharing is best-effort; the file is already written to local storage.
        }

        return path;
    }
}
