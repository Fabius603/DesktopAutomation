using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Views
{
    public partial class MakroStepsView : UserControl
    {
        private MakroStepsViewModel? _vm;
        private bool _syncingSelection;

        public MakroStepsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null) _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm = e.NewValue as MakroStepsViewModel;
            if (_vm != null) _vm.PropertyChanged += OnVmPropertyChanged;
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MakroStepsViewModel.SelectedStep)) return;

            if (_vm!.SelectedStep != null && StepsList.SelectedItems.Contains(_vm.SelectedStep))
                return;

            _syncingSelection = true;
            try
            {
                if (_vm.SelectedStep is null)
                    StepsList.SelectedItems.Clear();
                else
                    StepsList.SelectedItem = _vm.SelectedStep;
            }
            finally { _syncingSelection = false; }
        }

        private void StepsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection || sender is not ListBox lb) return;

            if (lb.SelectedItem != null)
                lb.Dispatcher.BeginInvoke(() => lb.ScrollIntoView(lb.SelectedItem));

            _vm?.SetSelectedSteps(lb.SelectedItems.Cast<object>());
        }
    }
}
