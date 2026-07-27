using System.Windows;
using System.Windows.Controls;

namespace DesktopAutomationApp.Controls.Jobs;

    public partial class ResultBindingPicker : UserControl
    {
        public static readonly DependencyProperty ShowClearButtonProperty = DependencyProperty.Register(
            nameof(ShowClearButton), typeof(bool), typeof(ResultBindingPicker),
            new PropertyMetadata(true));

        public ResultBindingPicker() => InitializeComponent();

        public bool ShowClearButton
        {
            get => (bool)GetValue(ShowClearButtonProperty);
            set => SetValue(ShowClearButtonProperty, value);
        }
    }
