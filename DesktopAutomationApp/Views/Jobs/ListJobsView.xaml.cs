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
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.Views
{
    public partial class ListJobsView : UserControl
    {
        public ListJobsView() => InitializeComponent();

        private void JobGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            Dispatcher.BeginInvoke(new Action(async () =>
            {
                if (DataContext is ListJobsViewModel vm && e.Row?.Item is Job j)
                {
                    // Name eindeutig machen
                    vm.EnsureUniqueNameFor(j);

                    // Sicherstellen, dass Items den aktuellen Stand hat
                    var index = vm.Items.IndexOf(j);
                    if (index >= 0)
                        vm.Items[index] = j;

                    // Änderungen abspeichern
                    await vm.SaveAllAsync();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}
