using System.Xml.Linq;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class ThemeResourceTests
{
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Theory]
    [InlineData("Dark.xaml")]
    [InlineData("Black.xaml")]
    [InlineData("Light.xaml")]
    public void ThemePalette_DefinesNeutralModernSemanticRoles(string fileName)
    {
        var document = XDocument.Load(Path.Combine(RepositoryRoot(), "DesktopAutomationApp", "Styles", "Themes", fileName));
        var keys = document.Root!
            .Elements()
            .Select(element => (string?)element.Attribute(Xaml + "Key"))
            .Where(key => key is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("App.Color.SurfaceRaised", keys);
        Assert.Contains("App.Color.SurfacePressed", keys);
        Assert.Contains("App.Color.BorderSubtle", keys);
        Assert.Contains("App.Color.BorderStrong", keys);
        Assert.Contains("App.Color.TextDisabled", keys);
        Assert.Contains("App.Brush.SurfaceRaised", keys);
        Assert.Contains("App.Brush.SurfacePressed", keys);
        Assert.Contains("App.Brush.BorderSubtle", keys);
        Assert.Contains("App.Brush.BorderStrong", keys);
        Assert.Contains("App.Brush.TextDisabled", keys);
    }

    [Fact]
    public void GlobalStyles_ReserveAccentForPrimaryAndSelectedStates()
    {
        var stylesDirectory = Path.Combine(RepositoryRoot(), "DesktopAutomationApp", "Styles");
        var buttons = File.ReadAllText(Path.Combine(stylesDirectory, "Buttons.xaml"));
        var cards = File.ReadAllText(Path.Combine(stylesDirectory, "Cards.xaml"));

        Assert.Contains("x:Key=\"ButtonPrimary\"", buttons);
        Assert.Contains("Background\" Value=\"{DynamicResource App.Brush.Accent}\"", buttons);
        Assert.Contains("x:Key=\"ButtonSecondary\"", buttons);
        Assert.Contains("Background\" Value=\"{DynamicResource App.Brush.SurfaceRaised}\"", buttons);
        Assert.Contains("BorderBrush\" Value=\"{DynamicResource App.Brush.BorderSubtle}\"", buttons);
        Assert.DoesNotContain("DropShadowEffect", cards);
        Assert.Contains("BorderBrush\" Value=\"{DynamicResource App.Brush.BorderSubtle}\"", cards);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DesktopAutomation.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
