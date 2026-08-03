using System.Globalization;
using System.Runtime.CompilerServices;
using DesktopAutomationApp.Converters;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class FlexibleDoubleConverterTests
{
    [Theory]
    [InlineData("0,5", "de-DE", 0.5)]
    [InlineData("0.5", "de-DE", 0.5)]
    [InlineData("0,5", "en-US", 0.5)]
    [InlineData("0.5", "en-US", 0.5)]
    public void ConvertBack_AcceptsCommaAndPoint(
        string text, string cultureName, double expected)
    {
        var converter = new FlexibleDoubleConverter();

        var result = converter.ConvertBack(
            text, typeof(double), null!, CultureInfo.GetCultureInfo(cultureName));

        Assert.Equal(expected, Assert.IsType<double>(result));
    }

    [Theory]
    [InlineData("0,5", 0.5)]
    [InlineData("0.5", 0.5)]
    [InlineData("12,75", 12.75)]
    public void TryParse_AcceptsBothDecimalSeparators(string text, double expected)
    {
        Assert.True(FlexibleDoubleConverter.TryParse(
            text, CultureInfo.GetCultureInfo("de-DE"), out var result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MovementFactorFields_KeepRawTextWhileTyping()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors",
            "Interaction", "KlickOnPoint3DStepEditor.xaml"));

        Assert.Contains(
            "Text=\"{Binding KlickOnPoint3DStep_MovementFactorX, UpdateSourceTrigger=PropertyChanged}\"",
            xaml, StringComparison.Ordinal);
        Assert.Contains(
            "Text=\"{Binding KlickOnPoint3DStep_MovementFactorY, UpdateSourceTrigger=PropertyChanged}\"",
            xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Converter={StaticResource FlexibleDoubleConverter}", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateSourceTrigger=LostFocus", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void StepFieldLabels_AreAlignedAtTheTop()
    {
        var xaml = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Styles", "StepEditors.xaml"));
        var styleStart = xaml.IndexOf("x:Key=\"StepFieldLabel\"", StringComparison.Ordinal);
        var styleEnd = xaml.IndexOf("</Style>", styleStart, StringComparison.Ordinal);
        var style = xaml[styleStart..styleEnd];

        Assert.Contains("Property=\"VerticalAlignment\" Value=\"Top\"", style, StringComparison.Ordinal);
        Assert.DoesNotContain("Property=\"VerticalAlignment\" Value=\"Center\"", style, StringComparison.Ordinal);
    }

    private static string RepositoryRoot([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", "..", ".."));
}
