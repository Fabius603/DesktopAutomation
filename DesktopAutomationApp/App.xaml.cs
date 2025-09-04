using System.Configuration;
using System.Data;
using System.Windows;
using ControlzEx.Theming;
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

namespace DesktopAutomationApp
{
    public partial class App : Application
    {
        private IHost _host = null!;

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
                    services.AddJsonRepository<Job>("Configs/Job", "jobs.json", options, j => j.Name);
                    services.AddJsonRepository<Makro>("Configs/Makro", "makros.json", options, m => m.Name);
                    services.AddJsonRepository<HotkeyDefinition>("Configs/Hotkey", "hotkeys.json", options, hk => hk.Name);

                    // Repository-Service registrieren
                    services.AddSingleton<IRepositoryService, RepositoryService>();

                    services.AddSingleton<JobExecutor>();

                    services.AddSingleton<IJobExecutor>(sp => sp.GetRequiredService<JobExecutor>());
                    services.AddSingleton<IJobExecutor>(sp => sp.GetRequiredService<JobExecutor>());
                    services.AddSingleton<IMakroExecutor, MakroExecutor>();
                    services.AddSingleton<IScriptExecutor, ScriptExecutor>();
                    services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
                    services.AddSingleton<IJobDispatcher, JobDispatcher>();
                    services.AddSingleton<IMacroPreviewService, MacroPreviewService>();
                    services.AddSingleton<IRecordingIndicatorOverlay, RecordingIndicatorOverlay>();
                    services.AddSingleton<IYOLOModelDownloader, YOLOModelDownloader>();
                    services.AddSingleton<IYoloManager, YoloManager>();

                    // ---- ViewModels / Views ----
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<StartViewModel>();
                    services.AddSingleton<ListHotkeysViewModel>();
                    services.AddSingleton<ListJobsViewModel>();
                    services.AddSingleton<ListMakrosViewModel>();
                    services.AddTransient<JobStepsViewModel>();

                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<StartView>();
                    services.AddSingleton<ListHotkeysView>();
                    services.AddSingleton<ListJobsView>();
                    services.AddSingleton<ListMakrosView>();
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
            mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // Serilog ordentlich schließen
            Log.CloseAndFlush();

            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
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
