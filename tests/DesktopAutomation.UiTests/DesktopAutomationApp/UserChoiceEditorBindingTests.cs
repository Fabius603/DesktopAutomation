using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Xml.Linq;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Contracts.Steps;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class UserChoiceEditorBindingTests
{
    [Fact]
    public void UserChoiceOptions_UseNormalSourceAwareTextInputs()
    {
        var document = XDocument.Load(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Controls", "Jobs", "Editors", "Generated",
            "GeneratedStepEditor.xaml"));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace generated = "clr-namespace:DesktopAutomationApp.Controls.Jobs.Editors.Generated";
        XNamespace iconPacks = "http://metro.mahapps.com/winfx/xaml/iconpacks";
        var optionsTemplate = document.Descendants()
            .Single(element => element.Attribute(x + "Key")?.Value == "UserChoiceOptionsFieldTemplate");
        var sourceInputs = optionsTemplate.Descendants(generated + "GeneratedValueSourceInput")
            .ToList();

        var labelInput = Assert.Single(sourceInputs, input =>
            input.Attribute("DataContext")?.Value == "{Binding LabelField}");
        var valueInput = Assert.Single(sourceInputs, input =>
            input.Attribute("DataContext")?.Value == "{Binding ValueField}");
        Assert.Same(labelInput.Parent, valueInput.Parent);
        Assert.True(labelInput.ElementsBeforeSelf().Count() < valueInput.ElementsBeforeSelf().Count());

        Assert.DoesNotContain(optionsTemplate.Descendants(presentation + "TextBlock"), text =>
            text.Attribute("Text")?.Value == "{Binding Number}");
        var menuButton = Assert.Single(optionsTemplate.Descendants(presentation + "Button"), button =>
            button.Descendants(iconPacks + "PackIconMaterial")
                .Any(icon => icon.Attribute("Kind")?.Value == "DotsVertical"));
        Assert.Equal("OpenButtonContextMenu_Click", menuButton.Attribute("Click")?.Value);
        Assert.Equal("1", menuButton.Attribute("Grid.Column")?.Value);
        Assert.Equal("Center", menuButton.Attribute("VerticalAlignment")?.Value);
        var menuCommands = menuButton.Descendants(presentation + "MenuItem")
            .Select(item => item.Attribute("Command")?.Value)
            .ToArray();
        Assert.Equal(["{Binding MoveUpCommand}", "{Binding MoveDownCommand}", "{Binding RemoveCommand}"], menuCommands);

        var optionViewModel = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "ViewModels", "Jobs", "UserChoiceOptionEditorViewModel.cs"));
        Assert.DoesNotContain("EditorHint: StepEditorHints.EmojiText", optionViewModel);

        var singleLineTemplate = document.Descendants()
            .Single(element => element.Attribute(x + "Key")?.Value == "SingleLineTextFieldTemplate");
        var titleTextBox = Assert.Single(singleLineTemplate.Descendants(presentation + "TextBox"));
        Assert.Equal("30", titleTextBox.Attribute("Height")?.Value);
        Assert.Equal("False", titleTextBox.Attribute("AcceptsReturn")?.Value);
    }

    [Fact]
    public void UserChoiceOptions_NormalTextValuesAreSerializedDirectly()
    {
        var editor = new GeneratedUserChoiceOptionsEditorViewModel(null);
        editor.Options[0].Label = "Yes";
        editor.Options[0].Value = "confirmed";

        var options = editor.ToNode()!.Deserialize<StepUserChoiceOptionValue[]>();

        Assert.NotNull(options);
        Assert.Equal("Yes", options[0].Label);
        Assert.Equal("confirmed", options[0].Value);
    }

    private static string RepositoryRoot([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(sourceFile)!, "..", "..", ".."));
}
