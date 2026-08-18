using System.Text;
using System.IO;

namespace TaskAutomation.Logging;

public interface ILogFileStorageService
{
    IReadOnlyList<string> GetNewestFiles(string directory, string searchPattern, int maximum, out bool hasMore);
    IReadOnlyList<string> ReadLastLines(string filePath, int maximum);
    void ApplyRetention(string directory, string searchPattern, int maximumFiles, long maximumBytes, TimeSpan maximumAge,
        Func<string, IEnumerable<string>>? companionFiles = null);
}

public sealed class LogFileStorageService : ILogFileStorageService
{
    public IReadOnlyList<string> GetNewestFiles(string directory, string searchPattern, int maximum, out bool hasMore)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximum);
        Directory.CreateDirectory(directory);
        var files = Directory.EnumerateFiles(directory, searchPattern)
            .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Take(maximum + 1)
            .ToList();
        hasMore = files.Count > maximum;
        return files.Take(maximum).ToArray();
    }

    public IReadOnlyList<string> ReadLastLines(string filePath, int maximum)
    {
        if (maximum <= 0 || !File.Exists(filePath)) return Array.Empty<string>();
        const int blockSize = 64 * 1024;
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete, blockSize, FileOptions.RandomAccess);
        if (stream.Length == 0) return Array.Empty<string>();

        var chunks = new List<byte[]>();
        var remaining = stream.Length;
        var newlineCount = 0;
        while (remaining > 0 && newlineCount <= maximum)
        {
            var count = (int)Math.Min(blockSize, remaining);
            remaining -= count;
            stream.Position = remaining;
            var buffer = new byte[count];
            stream.ReadExactly(buffer);
            chunks.Add(buffer);
            newlineCount += buffer.Count(value => value == (byte)'\n');
        }

        var bytes = new byte[chunks.Sum(chunk => chunk.Length)];
        var offset = 0;
        for (var index = chunks.Count - 1; index >= 0; index--)
        {
            Buffer.BlockCopy(chunks[index], 0, bytes, offset, chunks[index].Length);
            offset += chunks[index].Length;
        }

        var lines = Encoding.UTF8.GetString(bytes).Split('\n');
        var end = lines.Length;
        while (end > 0 && string.IsNullOrEmpty(lines[end - 1])) end--;
        return lines[Math.Max(0, end - maximum)..end]
            .Select(line => line.TrimStart('\uFEFF').TrimEnd('\r'))
            .ToArray();
    }

    public void ApplyRetention(string directory, string searchPattern, int maximumFiles, long maximumBytes,
        TimeSpan maximumAge, Func<string, IEnumerable<string>>? companionFiles = null)
    {
        try
        {
            var cutoff = DateTime.UtcNow - maximumAge;
            var files = Directory.EnumerateFiles(directory, searchPattern)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ToList();
            var retainedBytes = 0L;
            for (var index = 0; index < files.Count; index++)
            {
                var file = files[index];
                retainedBytes += file.Exists ? file.Length : 0;
                if (file.LastWriteTimeUtc < cutoff || index >= maximumFiles || retainedBytes > maximumBytes)
                    TryDelete(file.FullName, companionFiles);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void TryDelete(string path, Func<string, IEnumerable<string>>? companionFiles)
    {
        try
        {
            File.Delete(path);
            if (companionFiles == null) return;
            foreach (var companion in companionFiles(path)) File.Delete(companion);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
