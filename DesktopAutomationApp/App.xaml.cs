using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using DesktopAutomationApp.ViewModels;
using DesktopAutomationApp.Views;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;
using TaskAutomation.Hotkeys;
using TaskAutomation.Orchestration;
using TaskAutomation.Makros;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.JsonRepository;
using System.IO;
using Serilog;
using Serilog.Events;
using DesktopAutomationApp.Services.Preview;
using ImageCapture.DesktopDuplication.RecordingIndicator;
using TaskAutomation.Scripts;
using DesktopAutomationApp.Services;
using ImageDetection.YOLO;
using ImageDetection.Model;
using TaskAutomation.Events;

namespace DesktopAutomationApp
{
    public partial class App : Application
    {
        private IHost _host = null!;
        private System.Windows.Forms.NotifyIcon? _trayIcon;

        public App()
        {
            InitializeComponent();

            // Serilog global konfigurieren (Rolling Files im ./Logs)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.File(
                    path: Path.Combine(AppContext.BaseDirectory, "Logs", "desktop-automation-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 10_000_000,
                    shared: true,
                    outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            var options = GetOptions();

            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddJsonRepository<Job>("Configs/Job", options, j => j.Id.ToString());
                    services.AddJsonRepository<Makro>("Configs/Makro", options, m => m.Id.ToString());
                    services.AddJsonRepository<HotkeyDefinition>("Configs/Hotkey", options, hk => hk.Id.ToString());

                    // Repository-Service registrieren
                    services.AddSingleton<IRepositoryService, RepositoryService>();

                    services.AddSingleton<IJobExecutor, JobExecutor>();
                    services.AddSingleton<IMakroExecutor, MakroExecutor>();
                    services.AddSingleton<IScriptExecutor, ScriptExecutor>();
                    services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
                    services.AddSingleton<IJobDispatcher, JobDispatcher>();
                    services.AddSingleton<IMacroPreviewService, MacroPreviewService>();
                    services.AddSingleton<IRecordingIndicatorOverlay, RecordingIndicatorOverlay>();
                    services.AddSingleton<IYOLOModelDownloader, YOLOModelDownloader>();
                    services.AddSingleton<ILabelProvider, LabelProvider>();
                    services.AddSingleton(new YoloManagerOptions());
                    services.AddSingleton<IYoloManager, YoloManager>();
                    services.AddSingleton<IImageDisplayService, ImageDisplayService>();

                    // ---- ViewModels / Views ----
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<StartViewModel>();
                    services.AddSingleton<ListHotkeysViewModel>();
                    services.AddSingleton<ListJobsViewModel>();
                    services.AddSingleton<ListMakrosViewModel>();
                    services.AddSingleton<YoloDownloadsViewModel>();
                    services.AddTransient<JobStepsViewModel>();
                    services.AddTransient<HotkeyDetailViewModel>();

                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<StartView>();
                    services.AddSingleton<ListHotkeysView>();
                    services.AddSingleton<ListJobsView>();
                    services.AddSingleton<ListMakrosView>();
                    services.AddSingleton<YoloDownloadsView>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            await _host.StartAsync();

            _ = _host.Services.GetRequiredService<IJobDispatcher>();
            _ = _host.Services.GetRequiredService<IGlobalHotkeyService>();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();

            SetupTrayIcon(mainWindow);

            mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _trayIcon = null;

            Log.CloseAndFlush();

            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }

        private void SetupTrayIcon(MainWindow mainWindow)
        {
            var dispatcher = _host.Services.GetRequiredService<IJobDispatcher>();
            var hotkeyService = _host.Services.GetRequiredService<IGlobalHotkeyService>();

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var openItem = contextMenu.Items.Add("Öffnen");
            openItem.Click += (_, _) => ShowMainWindow(mainWindow);

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var stopJobsItem = new System.Windows.Forms.ToolStripMenuItem("Alle Jobs stoppen");
            stopJobsItem.Click += (_, _) =>
            {
                foreach (var id in dispatcher.RunningJobIds.ToArray())
                    dispatcher.CancelJob(id);
            };
            contextMenu.Items.Add(stopJobsItem);

            var toggleHotkeysItem = new System.Windows.Forms.ToolStripMenuItem();
            toggleHotkeysItem.Click += (_, _) =>
                hotkeyService.SetPaused(!hotkeyService.IsPaused);
            contextMenu.Items.Add(toggleHotkeysItem);

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var exitItem = contextMenu.Items.Add("Beenden");
            exitItem.Click += (_, _) => Shutdown();

            contextMenu.Opening += (_, _) =>
            {
                var hasRunningJobs = dispatcher.RunningJobIds.Count > 0;
                stopJobsItem.Visible = hasRunningJobs;
                toggleHotkeysItem.Text = hotkeyService.IsPaused ? "Hotkeys fortsetzen" : "Hotkeys pausieren";
            };

            hotkeyService.PausedChanged += () =>
                toggleHotkeysItem.Text = hotkeyService.IsPaused ? "Hotkeys fortsetzen" : "Hotkeys pausieren";

            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Text = "DesktopAutomation",
                ContextMenuStrip = contextMenu,
            };

            var iconStream = GetResourceStream(new Uri("pack://application:,,,/Assets/App.ico"));
            if (iconStream != null)
                _trayIcon.Icon = new System.Drawing.Icon(iconStream.Stream);

            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += (_, _) => ShowMainWindow(mainWindow);
        }

        private void ShowMainWindow(MainWindow mainWindow)
        {
            mainWindow.Show();
            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        }

        private JsonSerializerOptions GetOptions()
        {
            var jsonOpts = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            jsonOpts.Converters.Add(new JsonStringEnumConverter());
            jsonOpts.Converters.Add(new OpenCvRectJsonConverter());
            return jsonOpts;
        }
    }
}
