using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
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
using TaskAutomation.Steps;
using TaskAutomation.Automations;
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
using DesktopAutomation.Application.Interfaces;
using DesktopAutomation.Application.Services;
using DesktopAutomationApp.Infrastructure;
using TaskAutomation.Logging;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Settings;
using DesktopAutomationApp.Theming;
using Velopack;

namespace DesktopAutomationApp
{
    public partial class App : Application
    {
        private IHost _host = null!;
        private System.Windows.Forms.NotifyIcon? _trayIcon;

        [STAThread]
        public static void Main(string[] args)
        {
            VelopackApp.Build().Run();

            var app = new App();
            app.Run();
        }

        public App()
        {
            InitializeComponent();

            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DesktopAutomation",
                "Logs");

            // Logs must live outside Velopack's replaceable application directory.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "desktop-automation-.log"),
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
                    services.AddJsonRepository<AutomationDefinition>("Configs/Automation", options, a => a.Id.ToString());

                    services.AddSingleton<IJobExecutor, JobExecutor>();
                    services.AddSingleton<IDesktopCaptureService, DesktopCaptureService>();
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
                    services.AddSingleton<IImageDisplayService, WpfImageDisplayService>();
                    services.AddSingleton<IDesktopResultOverlay, WpfDesktopResultOverlay>();
                    services.AddSingleton<IUpdateService, UpdateService>();
                    services.AddSingleton<IExecutionLogService, ExecutionLogService>();
                    services.AddSingleton<IAutomationEngine, AutomationEngine>();
                    services.AddSingleton<IAutomationTriggerProvider, HotkeyAutomationTriggerProvider>();
                    services.AddSingleton<IAutomationTriggerProvider, ScheduleAutomationTriggerProvider>();
                    services.AddSingleton<IAutomationTriggerProvider, IntervalAutomationTriggerProvider>();
                    services.AddSingleton<IAutomationTriggerProvider, ProcessAutomationTriggerProvider>();

                    services.AddSingleton<IJobApplicationService, JobApplicationService>();
                    services.AddSingleton<IMakroApplicationService, MakroApplicationService>();
                    services.AddSingleton<IAutomationApplicationService, AutomationApplicationService>();
                    services.AddSingleton<IDialogService, WpfDialogService>();
                    services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
                    services.AddSingleton<ILocalizationService>(LocalizationService.Instance);
                    services.AddSingleton<IThemeService, ThemeService>();
                    services.AddSingleton<IViewModelFactory, ViewModelFactory>();
                    services.AddSingleton<IJobLauncher>(sp => (IJobLauncher)sp.GetRequiredService<IJobDispatcher>());
                    services.AddSingleton(sp => new Lazy<IJobLauncher>(() => sp.GetRequiredService<IJobLauncher>()));

                    // ---- ViewModels / Views ----
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<StartViewModel>();
                    services.AddSingleton<ListAutomationsViewModel>();
                    services.AddSingleton<ListJobsViewModel>();
                    services.AddSingleton<ListMakrosViewModel>();
                    services.AddSingleton<YoloDownloadsViewModel>();
                    services.AddSingleton<ExecutionLogsViewModel>();
                    services.AddSingleton<SettingsViewModel>();
                    services.AddTransient<JobStepsViewModel>();
                    services.AddTransient<AutomationDetailViewModel>();

                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<StartView>();
                    services.AddSingleton<ListAutomationsView>();
                    services.AddSingleton<ListJobsView>();
                    services.AddSingleton<ListMakrosView>();
                    services.AddSingleton<YoloDownloadsView>();
                    services.AddSingleton<ExecutionLogsView>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ThreadPool auf genug Threads vorheizen: verhindert Thread-Injection-Verzögerung (~500 ms/Thread)
            // wenn MaxJobCount Jobs gleichzeitig starten. TP-Threads werden sofort bereitgestellt.
            var minWorkers = Math.Max(JobDispatcher.MaxJobCount + 16,
                                      Environment.ProcessorCount * 4);
            ThreadPool.SetMinThreads(minWorkers, minWorkers);

            await _host.StartAsync();

            var preferences = _host.Services.GetRequiredService<IUserPreferencesService>();
            await preferences.LoadAsync();
            _host.Services.GetRequiredService<ILocalizationService>().SetCulture(preferences.Current.Culture);
            _host.Services.GetRequiredService<IThemeService>().Apply(preferences.Current.ThemeMode, preferences.Current.Accent);

            await _host.Services.GetRequiredService<IJobApplicationService>().ReloadAsync();
            await _host.Services.GetRequiredService<IMakroApplicationService>().ReloadAsync();
            _ = _host.Services.GetRequiredService<IJobDispatcher>();
            _ = _host.Services.GetRequiredService<IGlobalHotkeyService>();
            await _host.Services.GetRequiredService<IAutomationEngine>().StartAsync();

            // F10 global: alle Jobs & Makros stoppen (bypass Pause-Zustand)
            var dispatcher = _host.Services.GetRequiredService<IJobDispatcher>();
            var hotkeyServiceGlobal = _host.Services.GetRequiredService<IGlobalHotkeyService>();
            hotkeyServiceGlobal.EmergencyStopPressed += () =>
            {
                dispatcher.CancelAllJobs();
                foreach (var id in dispatcher.RunningMakroIds.ToList())
                    dispatcher.CancelMakro(id);
            };

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();

            SetupTrayIcon(mainWindow);

            mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _trayIcon = null;

            await _host.Services.GetRequiredService<IAutomationEngine>().StopAsync();
            Log.CloseAndFlush();

            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }

        private void SetupTrayIcon(MainWindow mainWindow)
        {
            var dispatcher = _host.Services.GetRequiredService<IJobDispatcher>();
            var automationEngine = _host.Services.GetRequiredService<IAutomationEngine>();

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var openItem = contextMenu.Items.Add(Loc.Get("Tray.Open"));
            openItem.Click += (_, _) => ShowMainWindow(mainWindow);

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var stopJobsItem = new System.Windows.Forms.ToolStripMenuItem(Loc.Get("Tray.StopAllJobs"));
            stopJobsItem.Click += (_, _) =>
            {
                dispatcher.CancelAllJobs();
            };
            contextMenu.Items.Add(stopJobsItem);

            var toggleAutomationsItem = new System.Windows.Forms.ToolStripMenuItem();
            toggleAutomationsItem.Click += async (_, _) =>
            {
                try
                {
                    await automationEngine.SetPausedAsync(!automationEngine.IsPaused);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Automationen konnten nicht pausiert oder fortgesetzt werden.");
                }
            };
            contextMenu.Items.Add(toggleAutomationsItem);

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var exitItem = contextMenu.Items.Add(Loc.Get("Tray.Exit"));
            exitItem.Click += (_, _) => Shutdown();

            contextMenu.Opening += (_, _) =>
            {
                var hasRunningJobs = dispatcher.RunningJobIds.Count > 0;
                stopJobsItem.Visible = hasRunningJobs;
                toggleAutomationsItem.Text = automationEngine.IsPaused ? Loc.Get("Tray.ResumeAutomations") : Loc.Get("Tray.PauseAutomations");
            };
            LocalizationService.Instance.CultureChanged += (_, _) =>
            {
                openItem.Text = Loc.Get("Tray.Open");
                stopJobsItem.Text = Loc.Get("Tray.StopAllJobs");
                exitItem.Text = Loc.Get("Tray.Exit");
                toggleAutomationsItem.Text = automationEngine.IsPaused ? Loc.Get("Tray.ResumeAutomations") : Loc.Get("Tray.PauseAutomations");
            };

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
