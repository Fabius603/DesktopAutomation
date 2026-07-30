using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class UserChoiceEditorBindingTests
{
    [Fact]
    public void TextInputs_WriteChangesBackImmediately()
    {
        var document = XDocument.Load(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Flow",
            "UserChoiceStepEditor.xaml"));
        XNamespace emoji = "clr-namespace:Emoji.Wpf;assembly=Emoji.Wpf";

        var textInputs = document.Descendants(emoji + "RichTextBox").ToList();

        Assert.Equal(4, textInputs.Count);
        Assert.All(textInputs, input =>
        {
            var binding = input.Attribute("Text")?.Value;
            Assert.Contains("Mode=TwoWay", binding, StringComparison.Ordinal);
            Assert.Contains("UpdateSourceTrigger=PropertyChanged", binding, StringComparison.Ordinal);
        });
    }

    private static string RepositoryRoot([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(sourceFile)!, "..", "..", ".."));
}
