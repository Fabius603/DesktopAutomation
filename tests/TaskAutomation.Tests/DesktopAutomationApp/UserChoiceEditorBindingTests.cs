using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class UserChoiceEditorBindingTests
{
    [Fact]
    public void TextInputs_WriteChangesBackImmediately()
    {
        var document = XDocument.Load(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));
        XNamespace emoji = "clr-namespace:Emoji.Wpf;assembly=Emoji.Wpf";

        XNamespace generated = "clr-namespace:DesktopAutomationApp.Controls.Jobs.Editors.Generated";
        var textInputs = document.Descendants(emoji + "RichTextBox").ToList();
        var sourceInputs = document.Descendants(generated + "GeneratedValueSourceInput")
            .Select(input => input.Attribute("DataContext")?.Value)
            .ToList();

        Assert.Single(textInputs);
        Assert.All(textInputs, input =>
        {
            var binding = input.Attribute("Text")?.Value;
            Assert.Contains("Mode=TwoWay", binding, StringComparison.Ordinal);
            Assert.Contains("UpdateSourceTrigger=PropertyChanged", binding, StringComparison.Ordinal);
        });
        Assert.Contains("{Binding LabelField}", sourceInputs);
        Assert.Contains("{Binding ValueField}", sourceInputs);
    }

    private static string RepositoryRoot([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(sourceFile)!, "..", "..", ".."));
}
