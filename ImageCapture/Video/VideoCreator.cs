using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace ImageCapture.Video
{
    public class VideoCreator
    {
        private class TimedFrame
        {
            public DateTime Timestamp { get; set; }
            public string FilePath { get; set; }
        }
        private readonly Task ffmpegInitTask;
        private readonly List<TimedFrame> frames = new();
        private readonly object lockObj = new();
        private readonly string frameFolder;

        public VideoCreator()
        {
            frameFolder = Path.Combine(Path.GetTempPath(), "ffmpeg_frames");
            Directory.CreateDirectory(frameFolder);
            ffmpegInitTask = FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
        }

        public void AddFrame(Bitmap image)
        {
            string filename;
            lock (lockObj)
            {
                filename = Path.Combine(frameFolder, $"frame_{frames.Count:D5}.png");
                frames.Add(new TimedFrame
                {
                    Timestamp = DateTime.UtcNow,
                    FilePath = filename
                });
            }

            try
            {
                image.Save(filename, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Speichern von Frame: {ex.Message}");
            }
            finally
            {
                image.Dispose();
            }
        }

        private string GetUniqueFilePath(string basePath)
        {
            string filePath = basePath;
            int counter = 1;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(Path.GetDirectoryName(basePath) ?? string.Empty,
                    $"{Path.GetFileNameWithoutExtension(basePath)}_{counter}{Path.GetExtension(basePath)}");
                counter++;
            }
            return filePath;
        }

        public async Task SaveAsMp4Async(string outputPath, int fps)
        {
            await ffmpegInitTask;

            List<TimedFrame> copiedFrames;
            lock (lockObj)
            {
                copiedFrames = new(frames);
            }

            if (copiedFrames.Count == 0)
                throw new InvalidOperationException("Keine Bilder vorhanden.");

            copiedFrames.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            // Zeitdifferenz und Zielanzahl an Frames
            TimeSpan duration = copiedFrames[^1].Timestamp - copiedFrames[0].Timestamp;
            int frameCount = Math.Max(1, (int)(duration.TotalSeconds * fps));

            // Zielzeitpunkte
            DateTime start = copiedFrames[0].Timestamp;
            TimeSpan step = TimeSpan.FromSeconds(1.0 / fps);

            // Auswahl: Interpolation per Wiederholung des nächsten früheren Bildes
            var selectedFiles = new List<string>();
            int currentIndex = 0;
            for (int i = 0; i < frameCount; i++)
            {
                DateTime targetTime = start + TimeSpan.FromSeconds(i / (double)fps);
                while (currentIndex + 1 < copiedFrames.Count && copiedFrames[currentIndex + 1].Timestamp <= targetTime)
                    currentIndex++;
                selectedFiles.Add(copiedFrames[currentIndex].FilePath);
            }

            FFmpeg.SetExecutablesPath(Path.Combine(AppContext.BaseDirectory));

            // Umkopieren mit sequentieller Benennung für FFmpeg
            for (int i = 0; i < selectedFiles.Count; i++)
            {
                File.Copy(selectedFiles[i], Path.Combine(frameFolder, $"ffmpeg_{i:D5}.png"), overwrite: true);
            }

            string uniquePath = GetUniqueFilePath(outputPath);
            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-framerate {fps} -i \"{frameFolder}/ffmpeg_%05d.png\" -c:v libx264 -pix_fmt yuv420p \"{uniquePath}\"", ParameterPosition.PreInput);

            await conversion.Start();
        }

        public void CleanUp()
        {
            try
            {
                if (Directory.Exists(frameFolder))
                {
                    foreach (var file in Directory.GetFiles(frameFolder))
                    {
                        File.Delete(file);
                    }

                    foreach (var dir in Directory.GetDirectories(frameFolder))
                    {
                        Directory.Delete(dir, recursive: true);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Bereinigen des Frame-Ordners: {ex.Message}");
            }
        }
    }
}
