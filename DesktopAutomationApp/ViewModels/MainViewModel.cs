using DesktopAutomationApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private object? _currentContent;
        private string _currentContentName = string.Empty;

        private readonly IThemeService _themeService;

        public RelayCommand SetDarkOrangeTheme { get; }
        public RelayCommand SetMonochromeTheme { get; }

        public object? CurrentContent
        {
            get => _currentContent;
            set
            {
                SetProperty(ref _currentContent, value);
                CurrentContentName = value?.GetType().Name ?? "—";
            }
        }

        public string CurrentContentName
        {
            get => _currentContentName;
            private set => SetProperty(ref _currentContentName, value);
        }

        public RelayCommand ShowViewA { get; }
        public RelayCommand ShowViewB { get; }

        public MainViewModel()
        {
            _themeService = new ThemeService();

            SetDarkOrangeTheme = new RelayCommand(() => _themeService.UseDarkOrange());
            SetMonochromeTheme = new RelayCommand(() => _themeService.UseMonochrome());

            // Startcontent
            CurrentContent = new ViewAViewModel();
        }
    }
}
