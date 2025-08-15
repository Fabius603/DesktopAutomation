using DesktopAutomationApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TaskAutomation.Makros;

namespace DesktopAutomationApp.Views
{
    /// <summary>
    /// Interaktionslogik für ListMakroView.xaml
    /// </summary>
    public partial class ListMakrosView : UserControl
    {
        private bool _renamingInProgress;

        public ListMakrosView() => InitializeComponent();

        private void StepsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedItem != null)
                lb.Dispatcher.BeginInvoke(() => lb.ScrollIntoView(lb.SelectedItem));
        }

        private void MakroGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            Dispatcher.BeginInvoke(new Action(async () =>
            {
                if (DataContext is ListMakrosViewModel vm && e.Row?.Item is Makro m)
                {
                    // Name eindeutig machen
                    vm.EnsureUniqueNameFor(m);

                    // Sicherstellen, dass Items den aktuellen Stand hat
                    var index = vm.Items.IndexOf(m);
                    if (index >= 0)
                        vm.Items[index] = m;

                    // Änderungen abspeichern
                    await vm.SaveAllAsync();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}
