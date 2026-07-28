using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls;
using TaskAutomation.Steps;
using EmojiTextBlock = Emoji.Wpf.TextBlock;

namespace DesktopAutomationApp.Views;

public partial class UserChoiceDialog : MetroWindow
{
    private bool _allowClose;
    public string? SelectedOptionId { get; private set; }

    public UserChoiceDialog(UserChoiceDialogRequest request)
    {
        InitializeComponent();
        QuestionText.Text = request.Question;
        QuestionText.Visibility = string.IsNullOrWhiteSpace(request.Question)
            ? Visibility.Collapsed
            : Visibility.Visible;
        DescriptionText.Text = request.Description;
        DescriptionText.Visibility = string.IsNullOrWhiteSpace(request.Description)
            ? Visibility.Collapsed
            : Visibility.Visible;
        var hasTitle = !string.IsNullOrWhiteSpace(request.Question);
        var hasDescription = !string.IsNullOrWhiteSpace(request.Description);
        HeaderPanel.Visibility = hasTitle || hasDescription
            ? Visibility.Visible
            : Visibility.Collapsed;
        DescriptionText.Margin = hasTitle
            ? new Thickness(0, 8, 0, 0)
            : new Thickness(0);
        OptionsList.Margin = hasTitle || hasDescription
            ? new Thickness(0, 15, 0, 0)
            : new Thickness(0);
        OptionsList.Tag = request.Options.Count <= 6
            ? 1
            : request.Options.Count <= 12
                ? 2
                : 3;
        OptionsList.ItemsSource = request.Options;
        Loaded += (_, _) =>
        {
            CenterOnDesktop(request.DesktopIndex);
            Activate();
            OptionsList.Focus();
        };
    }

    private void Option_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: UserChoiceDialogOption option }) return;
        SelectedOptionId = option.Id;
        _allowClose = true;
        DialogResult = true;
    }

    private void OptionText_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is EmojiTextBlock textBlock)
            UpdateOptionText(textBlock);
    }

    private void OptionText_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is EmojiTextBlock textBlock && e.WidthChanged)
            UpdateOptionText(textBlock);
    }

    private static void UpdateOptionText(EmojiTextBlock textBlock)
    {
        if (textBlock.DataContext is not UserChoiceDialogOption option
            || textBlock.ActualWidth <= 0)
            return;

        const double maximumTextHeight = 48;
        if (MeasureTextHeight(textBlock, option.Label) <= maximumTextHeight)
        {
            textBlock.Text = option.Label;
            return;
        }

        var textElements = StringInfo.ParseCombiningCharacters(option.Label);
        var low = 0;
        var high = textElements.Length;
        while (low < high)
        {
            var candidateLength = (low + high + 1) / 2;
            var end = candidateLength < textElements.Length
                ? textElements[candidateLength]
                : option.Label.Length;
            var candidate = option.Label[..end].TrimEnd() + "…";
            if (MeasureTextHeight(textBlock, candidate) <= maximumTextHeight)
                low = candidateLength;
            else
                high = candidateLength - 1;
        }

        var clippedEnd = low < textElements.Length
            ? textElements[low]
            : option.Label.Length;
        textBlock.Text = option.Label[..clippedEnd].TrimEnd() + "…";
    }

    private static double MeasureTextHeight(EmojiTextBlock textBlock, string text)
    {
        var dpi = VisualTreeHelper.GetDpi(textBlock);
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            textBlock.FlowDirection,
            new Typeface(
                textBlock.FontFamily,
                textBlock.FontStyle,
                textBlock.FontWeight,
                textBlock.FontStretch),
            textBlock.FontSize,
            Brushes.Black,
            dpi.PixelsPerDip)
        {
            MaxTextWidth = Math.Max(1, textBlock.ActualWidth),
            LineHeight = textBlock.LineHeight
        };
        return formattedText.Height;
    }

    public void CancelFromJob()
    {
        _allowClose = true;
        Close();
    }

    private void CenterOnDesktop(int desktopIndex)
    {
        // Keep the index contract identical to the monitor picker, which orders
        // desktops from left to right and then from top to bottom.
        var screen = ImageHelperMethods.ScreenHelper.GetScreenByDesktopIndex(desktopIndex);
        if (screen is null)
            return;

        var source = PresentationSource.FromVisual(this);
        var fromDevice = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var workAreaTopLeft = fromDevice.Transform(new Point(screen.WorkingArea.Left, screen.WorkingArea.Top));
        var workAreaBottomRight = fromDevice.Transform(new Point(screen.WorkingArea.Right, screen.WorkingArea.Bottom));
        var workWidth = workAreaBottomRight.X - workAreaTopLeft.X;
        var workHeight = workAreaBottomRight.Y - workAreaTopLeft.Y;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = workAreaTopLeft.X + (workWidth - ActualWidth) / 2;
        Top = workAreaTopLeft.Y + (workHeight - ActualHeight) / 2;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose && Application.Current?.Dispatcher.HasShutdownStarted != true)
            e.Cancel = true;
        base.OnClosing(e);
    }
}
