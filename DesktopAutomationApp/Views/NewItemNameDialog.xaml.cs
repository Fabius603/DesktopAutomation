using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Input;

namespace DesktopAutomationApp.Views
{
    public partial class NewItemNameDialog : MetroWindow
    {
        public string ResultName { get; private set; } = string.Empty;

        public NewItemNameDialog(string title, string prompt)
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            Loaded += (_, _) => { NameBox.Focus(); NameBox.SelectAll(); };
        }

        private void Ok_Click(object sender, RoutedEventArgs e) => Confirm();

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Confirm();
        }

        private void Confirm()
        {
            var name = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            ResultName = name;
            DialogResult = true;
            Close();
        }
    }
}
