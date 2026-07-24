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
using Common.ApplicationData;
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
using TaskAutomation.Timing;
using DesktopAutomationApp.Logging;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using TaskAutomation.WindowsIntegration;
using DesktopAutomationApp.Behaviors;

namespace DesktopAutomationApp
{
    public partial class App : Application
    {
        private IHost _host = null!;
        private System.Windows.Forms.NotifyIcon? _trayIcon;
        private AccentIconSet? _accentIcons;
        private IThemeService? _themeService;
        private MainWindow? _mainWindow;
        private string[] _startupArguments = [];
        private Task? _backgroundInitializationTask;

        [STAThread]
        public static void Main(string[] args)
        {
            VelopackApp.Build().Run();

            var app = new App();
            app._startupArguments = args;
            app.Run();
        }

        public App()
        {
            InitializeComponent();
            GlobalScrollBehavior.Initialize();

            AppPaths.MigrateLegacyData();
            var logDirectory = AppPaths.LogsDirectory;
            var applicationLogService = new ApplicationLogService(logDirectory);

            // Logs must live outside Velopack's replaceable application directory.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.Sink(applicationLogService)
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

            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
                Log.Fatal(eventArgs.ExceptionObject as Exception, "Unbehandelte Ausnahme in der Anwendung.");
            DispatcherUnhandledException += (_, eventArgs) =>
            {
                Log.Fatal(eventArgs.Exception, "Unbehandelte Ausnahme im UI-Thread.");
            };
            TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
            {
                Log.Error(eventArgs.Exception, "Nicht beobachtete Task-Ausnahme.");
                eventArgs.SetObserved();
            };

            var options = GetOptions();

            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddJsonRepository<Job>(AppPaths.JobConfigDirectory, options, j => j.Id.ToString());
                    services.AddJsonRepository<Makro>(AppPaths.MakroConfigDirectory, options, m => m.Id.ToString());
                    services.AddJsonRepository<AutomationDefinition>(AppPaths.AutomationConfigDirectory, options, a => a.Id.ToString());

                    services.AddSingleton<IJobExecutor, JobExecutor>();
                    services.AddSingleton<IPreciseDelayService, WindowsPreciseDelayService>();
                    services.AddSingleton<IDesktopCaptureService, DesktopCaptureService>();
                    services.AddSingleton<ICameraCaptureService, CameraCaptureService>();
                    services.AddSingleton<IMakroExecutor, MakroExecutor>();
                    services.AddSingleton<IInputController, WindowsInputController>();
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
                    services.AddSingleton<IReleaseNotesService, ReleaseNotesService>();
                    services.AddSingleton<IExecutionLogService, ExecutionLogService>();
                    services.AddSingleton<IAutomationLogService, AutomationLogService>();
                    services.AddSingleton<IApplicationLogService>(applicationLogService);
                    services.AddSingleton<IAutomationEngine, AutomationEngine>();
                    services.AddSingleton<IWindowsCapabilityCatalog, WindowsCapabilityCatalog>();
                    services.AddSingleton<IWindowsStateProvider, DefaultWindowsStateProvider>();
                    services.AddSingleton<IWindowsSystemStateService, WindowsSystemStateService>();
                    services.AddSingleton<IWindowsEventSource, NativeWindowsEventSource>();
                    services.AddSingleton<IWindowsEventSource, ProcessTraceWindowsEventSource>();
                    services.AddSingleton<IWindowsEventSource, WlanWindowsEventSource>();
                    services.AddSingleton<IWindowsEventSource, Win32MessageWindowsEventSource>();
                    services.AddSingleton<IWindowsEventSource, CoreAudioWindowsEventSource>();
                    services.AddSingleton<IWindowsEventSource, PrinterWindowsEventSource>();
                    services.AddSingleton<IWindowsEventSource, EventLogWindowsEventSource>();
                    services.AddSingleton<FileSystemWindowsEventSource>();
                    services.AddSingleton<IWindowsSubscriptionEventSource>(sp => sp.GetRequiredService<FileSystemWindowsEventSource>());
                    services.AddSingleton<InputIdleWindowsEventSource>();
                    services.AddSingleton<IWindowsSubscriptionEventSource>(sp => sp.GetRequiredService<InputIdleWindowsEventSource>());
                    services.AddSingleton<IWindowsSystemEventHub, WindowsSystemEventHub>();
                    services.AddSingleton<IAutomationTriggerProvider, HotkeyAutomationTriggerProvider>();
                    services.AddSingleton<IAutomationTriggerProvider, TimeAutomationTriggerProvider>();
                    services.AddSingleton<IAutomationTriggerProvider, ProcessAutomationTriggerProvider>();
                    services.AddSingleton<IAutomationTriggerProvider, WindowAutomationTriggerProvider>();
                    services.AddSingleton<IAutomationTriggerProvider, FileSystemAutomationTriggerProvider>();
                    services.AddSingleton<IAutomationTriggerProvider, SystemAutomationTriggerProvider>();
                    services.AddSingleton<IAutomationTriggerProvider, WindowsEventAutomationTriggerProvider>();
                    services.AddSingleton<IAutomationTriggerProvider, WebhookAutomationTriggerProvider>();

                    services.AddSingleton<IJobApplicationService, JobApplicationService>();
                    services.AddSingleton<IMakroApplicationService, MakroApplicationService>();
                    services.AddSingleton<IAutomationApplicationService, AutomationApplicationService>();
                    services.AddSingleton<IDialogService, WpfDialogService>();
                    services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
                    services.AddSingleton<IWindowsStartupRegistrationService, WindowsStartupRegistrationService>();
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
                    services.AddSingleton<LogsHomeViewModel>();
                    services.AddSingleton<AutomationLogsViewModel>();
                    services.AddSingleton<ApplicationLogsViewModel>();
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
                    services.AddSingleton<LogsHomeView>();
                    services.AddSingleton<AutomationLogsView>();
                    services.AddSingleton<ApplicationLogsView>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Log.Information("Anwendung gestartet. Version: {Version}; Hintergrundstart: {StartInBackground}; Betriebssystem: {OperatingSystem}",
                typeof(App).Assembly.GetName().Version?.ToString(),
                _startupArguments.Any(argument => string.Equals(argument, "--background", StringComparison.OrdinalIgnoreCase)),
                Environment.OSVersion.VersionString);

            // ThreadPool auf genug Threads vorheizen: verhindert Thread-Injection-Verzögerung (~500 ms/Thread)
            // wenn MaxJobCount Jobs gleichzeitig starten. TP-Threads werden sofort bereitgestellt.
            var minWorkers = Math.Max(JobDispatcher.MaxJobCount + 16,
                                      Environment.ProcessorCount * 4);
            ThreadPool.SetMinThreads(minWorkers, minWorkers);

            await _host.StartAsync();

            var preferences = _host.Services.GetRequiredService<IUserPreferencesService>();
            await preferences.LoadAsync();
            _host.Services.GetRequiredService<ILocalizationService>().SetCulture(preferences.Current.Culture);
            _themeService = _host.Services.GetRequiredService<IThemeService>();
            _themeService.Apply(preferences.Current.ThemeMode, preferences.Current.Accent);
            try
            {
                _host.Services.GetRequiredService<IWindowsStartupRegistrationService>().Apply(
                    preferences.Current.StartWithWindows,
                    preferences.Current.StartInBackgroundAtWindowsStartup);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Der Windows-Autostart konnte nicht synchronisiert werden.");
            }

            _ = _host.Services.GetRequiredService<IJobDispatcher>();
            _ = _host.Services.GetRequiredService<IGlobalHotkeyService>();

            // F10 global: alle Jobs & Makros stoppen (bypass Pause-Zustand)
            var dispatcher = _host.Services.GetRequiredService<IJobDispatcher>();
            var hotkeyServiceGlobal = _host.Services.GetRequiredService<IGlobalHotkeyService>();
            hotkeyServiceGlobal.EmergencyStopPressed += () =>
            {
                Log.Warning("Notstopp über F10 ausgelöst.");
                dispatcher.ForceStopAllJobs();
                foreach (var id in dispatcher.RunningMakroIds.ToList())
                    dispatcher.CancelMakro(id);
            };

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            _mainWindow = mainWindow;
            mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
            mainWindow.SourceInitialized += (_, _) => UpdateAccentIcons();

            SetupTrayIcon(mainWindow);
            _themeService.ThemeChanged += OnThemeChanged;
            UpdateAccentIcons();

            var startInBackground = _startupArguments.Any(argument =>
                string.Equals(argument, "--background", StringComparison.OrdinalIgnoreCase));
            if (!startInBackground)
                ShowMainWindow(mainWindow);

            // Alles, was nicht für das erste sichtbare Fenster benötigt wird, startet im Hintergrund.
            // Dadurch blockieren Datei-I/O und die Registrierung der Automation-Trigger nicht mehr die UI.
            _backgroundInitializationTask = Task.Run(InitializeBackgroundServicesAsync);
        }

        private async Task InitializeBackgroundServicesAsync()
        {
            try
            {
                await _host.Services.GetRequiredService<IJobApplicationService>().ReloadAsync().ConfigureAwait(false);
                await _host.Services.GetRequiredService<IMakroApplicationService>().ReloadAsync().ConfigureAwait(false);
                await _host.Services.GetRequiredService<IAutomationEngine>().StartAsync().ConfigureAwait(false);

                await Dispatcher.InvokeAsync(
                        () => _host.Services.GetRequiredService<StartViewModel>().RefreshAsync(),
                        DispatcherPriority.Background)
                    .Task.Unwrap();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Die Hintergrundinitialisierung konnte nicht abgeschlossen werden.");
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            Log.Information("Anwendung wird beendet.");
            if (_themeService != null)
                _themeService.ThemeChanged -= OnThemeChanged;
            _trayIcon?.Dispose();
            _trayIcon = null;
            _accentIcons?.Dispose();
            _accentIcons = null;

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
                Log.Information("Alle Jobs wurden über das Tray-Menü gestoppt.");
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

            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += (_, _) => ShowMainWindow(mainWindow);
        }

        private void OnThemeChanged(object? sender, EventArgs e) => UpdateAccentIcons();

        private void UpdateAccentIcons()
        {
            if (_mainWindow == null || _trayIcon == null)
                return;

            var accent = TryFindResource("App.Color.Accent") switch
            {
                System.Windows.Media.Color color => color,
                System.Windows.Media.SolidColorBrush brush => brush.Color,
                _ => System.Windows.Media.Color.FromRgb(0x21, 0x96, 0xF3)
            };

            try
            {
                var newIcons = AccentIconFactory.Create(accent);
                _mainWindow.Icon = newIcons.WindowIcon;
                _trayIcon.Icon = newIcons.TrayIcon;

                var windowHandle = new WindowInteropHelper(_mainWindow).Handle;
                if (windowHandle != IntPtr.Zero)
                {
                    SendMessage(windowHandle, WmSetIcon, IconBig, newIcons.TrayIcon.Handle);
                    SendMessage(windowHandle, WmSetIcon, IconSmall, newIcons.TrayIcon.Handle);
                }

                var oldIcons = _accentIcons;
                _accentIcons = newIcons;
                oldIcons?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Das App-Icon konnte nicht an die Akzentfarbe angepasst werden.");
            }
        }

        private const int WmSetIcon = 0x0080;
        private static readonly IntPtr IconSmall = IntPtr.Zero;
        private static readonly IntPtr IconBig = new(1);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr parameter, IntPtr value);

        private void ShowMainWindow(MainWindow mainWindow)
        {
            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.WindowState = WindowState.Normal;

            mainWindow.Show();
            mainWindow.ShowActivated = true;
            mainWindow.Activate();
            _ = _host.Services.GetRequiredService<IReleaseNotesService>().ShowIfNewAsync();

            // Ein kurzes Topmost-Umschalten stellt sicher, dass Windows das frisch gestartete
            // Fenster auch vor bereits geöffneten Fenstern platziert.
            mainWindow.Topmost = true;
            mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                mainWindow.Topmost = false;
                mainWindow.Activate();
            });
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
