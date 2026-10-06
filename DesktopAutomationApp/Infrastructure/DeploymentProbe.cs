using Common.ApplicationData;
using DesktopAutomation.Application.Deployment;
using DesktopAutomation.Application.Settings;
using DesktopAutomationApp.Services;
using ImageCapture.DesktopDuplication;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TaskAutomation.Steps;
using Tesseract;
using Windows.ApplicationModel;

namespace DesktopAutomationApp.Infrastructure;

/// <summary>Opt-in installation diagnostics; refuses the user's default data profile.</summary>
public static class DeploymentProbe
{
    public static int Run(string[] args, InstallationContext installation)
    {
        if (AppPaths.IsDefaultProfile) return 2;
        var index = Array.IndexOf(args, "--deployment-probe");
        if (index < 0 || index + 1 >= args.Length) return 2;
        var checks = new Dictionary<string, string>();
        void Check(string name, Action action)
        {
            try { action(); checks[name] = "Passed"; }
            catch (Exception exception) { checks[name] = exception.GetType().Name + ": " + exception.Message; }
        }
        Check("SharedData", () =>
        {
            Directory.CreateDirectory(AppPaths.JobConfigDirectory);
            var path = AppPaths.GetRoamingPath("deployment-marker.txt");
            if (File.Exists(path) && File.ReadAllText(path) != "shared") throw new IOException("Marker mismatch.");
            File.WriteAllText(path, "shared");
            var preferences = new UserPreferencesService();
            preferences.LoadAsync().GetAwaiter().GetResult();
            preferences.SaveAsync().GetAwaiter().GetResult();
        });
        Check("DpapiAcrossChannels", () =>
        {
            var path = AppPaths.GetLocalPath("deployment-secret.bin");
            var entropy = Encoding.UTF8.GetBytes("DesktopAutomation.SecretStore.dpapi_current_user_v1");
            if (!File.Exists(path)) File.WriteAllBytes(path, ProtectedData.Protect(Encoding.UTF8.GetBytes("probe"), entropy, DataProtectionScope.CurrentUser));
            if (Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path), entropy, DataProtectionScope.CurrentUser)) != "probe")
                throw new CryptographicException("Cross-channel decryption failed.");
        });
        Check("UpdateIsolation", () =>
        {
            if (installation.CanUpdateInApp) return; // No network access during diagnostics.
            var updates = new UpdateService(NullLogger<UpdateService>.Instance, installation);
            if (updates.CheckForUpdateAsync().GetAwaiter().GetResult().HasUpdate ||
                updates.DownloadUpdateAsync().GetAwaiter().GetResult() || updates.PrepareUpdateAndRestart())
                throw new InvalidOperationException("Unexpected in-app update.");
        });
        Check("NativeOcr", () =>
        {
            using var engine = new TesseractEngine(Path.Combine(AppContext.BaseDirectory, "tessdata"), "eng", EngineMode.LstmOnly);
            using var image = new Bitmap(200, 60);
            using (var graphics = Graphics.FromImage(image)) graphics.Clear(Color.White);
            using var memory = new MemoryStream();
            image.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            using var pix = Pix.LoadFromMemory(memory.ToArray());
            using var page = engine.Process(pix);
            _ = page.GetText();
        });
        Check("NativeImageDetection", () =>
        {
            using var matrix = new OpenCvSharp.Mat(2, 2, OpenCvSharp.MatType.CV_8UC1, OpenCvSharp.Scalar.All(0));
            if (matrix.Empty()) throw new InvalidOperationException("Native image library unavailable.");
        });
        Check("DesktopCapture", () =>
        {
            using var capture = new DesktopDuplicator(0);
            for (var attempt = 0; attempt < 5 && !capture.HasImage; attempt++) capture.UpdateFrame(200);
            if (!capture.HasImage) throw new InvalidOperationException("No desktop frame available.");
            using var frame = capture.CopyFrame(true);
        });
        Check("ProcessLaunch", () =>
        {
            using var process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/c exit 0")
            { UseShellExecute = false, CreateNoWindow = true })!;
            if (!process.WaitForExit(5000) || process.ExitCode != 0) throw new InvalidOperationException("Process launch failed.");
        });
        Check("GlobalHotkey", () =>
        {
            if (!RegisterHotKey(IntPtr.Zero, 0xDA01, 0x4003, 0x87)) throw new System.ComponentModel.Win32Exception();
            UnregisterHotKey(IntPtr.Zero, 0xDA01);
        });
        if (installation.Kind == InstallationKind.Msix)
            Check("StartupManifest", () => _ = StartupTask.GetAsync(Settings.WindowsStartupRegistrationService.StartupTaskId).AsTask().GetAwaiter().GetResult());

        File.WriteAllText(args[index + 1], JsonSerializer.Serialize(new
        {
            installation = installation.Kind.ToString(),
            installation.Version,
            installation.PackageFamilyName,
            packageVersion = installation.Kind == InstallationKind.Msix ? FormatPackageVersion(Package.Current.Id.Version) : null,
            localRoot = AppPaths.LocalRoot,
            roamingRoot = AppPaths.RoamingRoot,
            checks
        }, new JsonSerializerOptions { WriteIndented = true }));
        var holdIndex = Array.IndexOf(args, "--probe-hold");
        if (holdIndex >= 0 && holdIndex + 1 < args.Length && int.TryParse(args[holdIndex + 1], out var milliseconds))
            Thread.Sleep(Math.Clamp(milliseconds, 0, 30000));
        return checks.Values.All(value => value == "Passed") ? 0 : 1;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr window, int id);

    private static string FormatPackageVersion(PackageVersion version) => $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
}
