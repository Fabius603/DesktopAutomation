using DesktopAutomationApp.Services.Preview;
using DesktopOverlay;
using Microsoft.Extensions.Logging;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using ImageHelperMethods;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListMakrosViewModel : ViewModelBase
    {
        private readonly ILogger<ListMakrosViewModel> _log;
        private readonly IJobExecutor _executor;
        private readonly IMacroPreviewService _preview;
        private Overlay _overlay; // Lebenszyklus gehört hier der VM
        private MacroPreviewService.PreviewResult _lastPreview;

        public string Title => "Makros";
        public string Description => "Verfügbare Makros";

        public ObservableCollection<Makro> Items { get; } = new();
        private Makro? _selected;
        public Makro? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand CopyNameCommand { get; }
        public ICommand PreviewOverviewCommand { get; }
        public ICommand PreviewPlaybackCommand { get; }
        public ICommand PreviewStopCommand { get; }


        public ListMakrosViewModel(IJobExecutor executor, ILogger<ListMakrosViewModel> log, IMacroPreviewService preview)
        {
            _executor = executor;
            _log = log;
            _preview = preview;

            RefreshCommand = new RelayCommand(LoadMakros);
            CopyNameCommand = new RelayCommand<Makro?>(m =>
            {
                if (m?.Name is { Length: > 0 })
                    System.Windows.Clipboard.SetText(m.Name);
            }, m => m != null);
            PreviewOverviewCommand = new RelayCommand(ShowOverview, CanPreview);
            PreviewPlaybackCommand = new RelayCommand(() => ShowPlayback(), CanPreview);
            PreviewStopCommand = new RelayCommand(StopPreview, () => _overlay != null);

            LoadMakros();
        }

        private bool CanPreview() => Selected != null && (Selected.Befehle?.Count > 0);

        private void EnsureOverlay()
        {
            if (_overlay != null) return;

            var v = ScreenHelper.GetVirtualDesktopBounds();
            _overlay = new Overlay(v.Left, v.Top, v.Width, v.Height, desktopId: 0);
            _overlay.RunInNewThread(); // eigenes STA-Threading wie von dir vorgesehen
        }

        private Makro ToDomain(Makro m) => m;

        private void BuildPreview()
        {
            var v = ScreenHelper.GetVirtualDesktopBounds();
            _lastPreview = _preview.Build(ToDomain(Selected), v, v);
        }

        private void ShowOverview()
        {
            EnsureOverlay();
            BuildPreview();

            _overlay.StopPlayback();
            _overlay.ClearItems();
            _overlay.AddItems(_lastPreview.StaticItems);
                                                         
        }

        private void ShowPlayback(double speed = 1.0)
        {
            EnsureOverlay();
            BuildPreview();

            _overlay.ClearItems();
            _overlay.AddItems(_lastPreview.StaticItems);
            _overlay.AddItems(_lastPreview.TimedItems);

            _overlay.PlaybackSpeed = speed;
            _overlay.StartPlayback(0.0);
        }

        private void StopPreview()
        {
            if (_overlay == null) return;
            _overlay.StopPlayback();
            _overlay.ClearItems();
        }

        private async void LoadMakros()
        {
            await _executor.ReloadMakrosAsync();

            Items.Clear();
            foreach (var m in _executor.AllMakros.Values.OrderBy(m => m.Name))
                Items.Add(m);

            Selected = Items.FirstOrDefault();
            _log.LogInformation("Makros geladen: {Count}", Items.Count);
        }

        public void Dispose()
        {
            StopPreview();
            _overlay?.Dispose();
            _overlay = null;
        }
    }
}
