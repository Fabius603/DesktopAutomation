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
        XNamespace controls = "clr-namespace:DesktopAutomationApp.Controls";
        XNamespace generated = "clr-namespace:DesktopAutomationApp.Controls.Jobs.Editors.Generated";
        XNamespace iconPacks = "http://metro.mahapps.com/winfx/xaml/iconpacks";
        XNamespace context = "clr-namespace:DesktopAutomationApp.Behaviors";
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
        Assert.Equal("True", menuButton.Attribute(context + "ContextActions.IsMenuButton")?.Value);
        var optionList = Assert.Single(optionsTemplate.Descendants(presentation + "ListBox"));
        Assert.Equal("Answers", optionList.Attribute(context + "ContextActions.Profile")?.Value);
        Assert.Equal("Extended", optionList.Attribute("SelectionMode")?.Value);
        Assert.Equal(controls + "ResponsiveActionPanel", menuButton.Parent?.Name);
        Assert.Equal("Right", menuButton.Attribute("HorizontalAlignment")?.Value);
        Assert.Equal("Center", menuButton.Attribute("VerticalAlignment")?.Value);
        Assert.Empty(menuButton.Descendants(presentation + "MenuItem"));

        var optionViewModel = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "ViewModels", "Jobs", "UserChoiceOptionEditorViewModel.cs"));
        Assert.DoesNotContain("EditorHint: StepEditorHints.EmojiText", optionViewModel);

        var singleLineTemplate = document.Descendants()
            .Single(element => element.Attribute(x + "Key")?.Value == "SingleLineTextFieldTemplate");
        var titleTextBox = Assert.Single(singleLineTemplate.Descendants(presentation + "TextBox"));
        Assert.Equal("34", titleTextBox.Attribute("Height")?.Value);
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
