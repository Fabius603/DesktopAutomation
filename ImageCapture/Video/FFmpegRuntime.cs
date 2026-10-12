using Common.ApplicationData;
using Xabe.FFmpeg.Downloader;

namespace ImageCapture.Video;

/// <summary>One writable, absolute runtime location shared by every distribution channel.</summary>
public static class FFmpegRuntime
{
    public static string DirectoryPath => AppPaths.GetLocalPath("Tools", "FFmpeg");
    public static string ExecutablePath => Path.Combine(DirectoryPath, "ffmpeg.exe");
    private static readonly Lazy<Task> Initialization = new(InitializeAsync);

    public static Task EnsureAvailableAsync() => Initialization.Value;

    private static async Task InitializeAsync()
    {
        Directory.CreateDirectory(DirectoryPath);
        if (!File.Exists(ExecutablePath))
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, DirectoryPath).ConfigureAwait(false);
        if (!File.Exists(ExecutablePath)) throw new FileNotFoundException("FFmpeg runtime unavailable.", ExecutablePath);
    }
}
