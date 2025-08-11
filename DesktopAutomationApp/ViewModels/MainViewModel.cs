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
        private string _currentContentName = "—";

        private readonly StartViewModel _start = new();
        private readonly ListMakrosViewModel _listMakros = new();
        private readonly ListJobsViewModel _listJobs = new();
        private readonly ListHotkeysViewModel _listHotkeys = new();

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

        public RelayCommand ShowStart => new(() => CurrentContent = _start);
        public RelayCommand ShowListMakros => new(() => CurrentContent = _listMakros);
        public RelayCommand ShowListJobs => new(() => CurrentContent = _listJobs);
        public RelayCommand ShowListHotkeys => new(() => CurrentContent = _listHotkeys);


        public MainViewModel()
        {
            // Startcontent
            CurrentContent = _start;
        }
    }
}
