using DesktopAutomationApp.Localization;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class DebugValueTypeLocalizationTests
{
    [Theory]
    [InlineData("Int32", "Integer")]
    [InlineData("Double", "Number")]
    [InlineData("Point", "Point")]
    [InlineData("Rectangle?", "Rectangle")]
    [InlineData("DetectionItem", "Detection")]
    [InlineData("RuntimeProcessReference", "Process reference")]
    [InlineData("TimeSpan", "Duration")]
    [InlineData("UnknownResult", "Result object")]
    public void Localize_MapsClrTypesToUserFacingResultTypes(
        string typeName,
        string expected)
    {
        var actual = DebugValueTypeLocalization.Localize(
            typeName,
            (_, fallback) => fallback);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("DetectionItem[]", "List of Detection")]
    [InlineData("IReadOnlyList<RuntimeProcessReference>", "List of Process reference")]
    public void Localize_MapsCollectionsAndTheirElementType(
        string typeName,
        string expected)
    {
        var actual = DebugValueTypeLocalization.Localize(
            typeName,
            (_, fallback) => fallback);

        Assert.Equal(expected, actual);
    }
}
