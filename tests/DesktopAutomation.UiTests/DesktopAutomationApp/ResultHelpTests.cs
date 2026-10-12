using System.Xml.Linq;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Services.Jobs;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class ResultHelpTests
{
    [Theory]
    [InlineData("Strings.resx")]
    [InlineData("Strings.en.resx")]
    public void EveryStepInput_HasAnExplanationInBothLanguages(string file)
    {
        var texts = XDocument.Load(Path.Combine(StepListRenderHost.RepositoryRoot(),
            "DesktopAutomationApp", "Resources", file)).Descendants("data")
            .ToDictionary(element => (string)element.Attribute("name")!, element => (string)element.Element("value")!);
        foreach (var definition in TaskAutomation.Steps.Definitions.BuiltInStepDefinitions.Instance.Definitions)
            foreach (var field in definition.Descriptor.Fields)
            {
                var key = field.DescriptionKey ?? field.LabelKey + ".Help";
                Assert.True(texts.TryGetValue(key, out var text) && !string.IsNullOrWhiteSpace(text),
                    $"Missing input explanation in {file}: {definition.Descriptor.TypeId}.{field.Id} ({key})");
            }
        foreach (var parameter in new TaskAutomation.WindowsIntegration.WindowsCapabilityCatalog().Capabilities
                     .SelectMany(capability => capability.Parameters ?? []))
        {
            var name = parameter.Type switch
            {
                TaskAutomation.WindowsIntegration.WindowsParameterType.ProcessName => "ProcessName",
                TaskAutomation.WindowsIntegration.WindowsParameterType.Drive => "Drive",
                _ => parameter.Name
            };
            var key = $"Windows.Parameter.{name}.Help";
            Assert.True(texts.TryGetValue(key, out var text) && !string.IsNullOrWhiteSpace(text),
                $"Missing Windows input explanation in {file}: {key}");
        }
    }

    [Theory]
    [InlineData("Strings.resx", "Aufgenommenes Bild")]
    [InlineData("Strings.en.resx", "Captured image")]
    public void CapturedImageTooltip_UsesReadableSourceAndPropertyText(string file, string imageLabel)
    {
        var texts = XDocument.Load(Path.Combine(StepListRenderHost.RepositoryRoot(),
            "DesktopAutomationApp", "Resources", file)).Descendants("data")
            .ToDictionary(element => (string)element.Attribute("name")!, element => (string)element.Element("value")!);
        var metadata = StepResultMetadata.GetResultType(nameof(DesktopDuplicationResult))!;
        var localized = new ResultTypeDescriptor(metadata.TypeName, metadata.DisplayName,
            metadata.Properties.Select(property => ResultPropertyLocalization.Localize(metadata.TypeName, property,
                key => texts.GetValueOrDefault(key))).ToArray());
        var picker = new ValueReferencePickerViewModel([new SourceStepItem("capture", "Capture desktop", localized)],
            InputContractRegistry.Find(typeof(SaveImageStep), "image")!);
        picker.UseStepResultCommand.Execute(null);
        var image = Assert.Single(Flatten(picker.SelectionTree), node => node.DisplayName == imageLabel);
        image.SelectCommand!.Execute(null);
        Assert.Equal($"{Loc.Get("Ui.ValueReference.ResultVariables")} \u2192 Capture desktop \u2192 {imageLabel}", picker.SelectedTooltipValue);
        Assert.DoesNotContain("\u00e2\u2020\u2019", picker.SelectedTooltipValue);
        Assert.Equal("image", picker.ToBinding().PropertyId);
    }

    [Theory]
    [InlineData("Strings.resx")]
    [InlineData("Strings.en.resx")]
    public void EverySelectableResult_HasLocalizedMeaningAndRetainsItsBindingIdentity(string file)
    {
        var texts = XDocument.Load(Path.Combine(StepListRenderHost.RepositoryRoot(),
            "DesktopAutomationApp", "Resources", file)).Descendants("data")
            .ToDictionary(element => (string)element.Attribute("name")!, element => (string)element.Element("value")!);
        foreach (var result in StepResultMetadata.ResultTypes)
            foreach (var property in result.Properties)
            {
                Assert.True(texts.ContainsKey($"Step.ResultProperty.{result.TypeName}.{property.Name}")
                    || texts.ContainsKey($"Step.ResultProperty.{property.Name}"),
                    $"Missing display name in {file}: {result.TypeName}.{property.Name}");
                var meaning = ResultPropertyLocalization.Description(result.TypeName, property.Name, key => texts.GetValueOrDefault(key));
                Assert.True(!string.IsNullOrWhiteSpace(meaning), $"{file}: {result.TypeName}.{property.Name}");
                var localized = ResultPropertyLocalization.Localize(result.TypeName, property, key => texts.GetValueOrDefault(key));
                Assert.Equal(property.StableId, localized.StableId);
                Assert.Equal(property.Name, localized.Name);
                Assert.Equal(property.DataType, localized.DataType);
                Assert.Equal(property.Cardinality, localized.Cardinality);
                Assert.Equal(property.IsDiagnostic, localized.IsDiagnostic);
                Assert.Equal(property.EnumValues, localized.EnumValues);
                Assert.NotEqual(property.DataType.ToString(), localized.Description);
            }
    }

    [Fact]
    public void OutputDetails_ExplainNestedGeometryAndSeparateDiagnostics()
    {
        var step = new OcrStep();
        var details = new JobStepDetailsProvider().GetDetails(step, new[] { step }).Result!;
        var diagnostics = Assert.Single(details.Properties, property => property.Name == Loc.Get("Ui.ValueReference.ResultDiagnostics"));
        Assert.Equal(2, diagnostics.Children.Count);
        var bounds = Assert.Single(details.Properties, property => property.Name == "Erkannter Bereich");
        Assert.Contains("Pixel", bounds.Description);
        Assert.Contains("Beispiel:", bounds.Description);
        var empty = Assert.Single(bounds.Children, property => property.Name == "Bereich ist leer");
        Assert.Contains("Breite", empty.Description);
        Assert.DoesNotContain("Zwischenablage", empty.Description);
        var words = Assert.Single(details.Properties, property => property.Name == "Erkannte Wörter");
        Assert.Contains("Wörter", words.Description);
        Assert.DoesNotContain(words.Children, child => child.Name.Contains("Words"));
    }

    [Fact]
    public void Picker_SearchesMeaningAndKeepsDiagnosticValuesSelectable()
    {
        var contract = StepLocalization.ResultType(StepResultMetadata.GetResultType(nameof(TemplateMatchingResult))!);
        var picker = new ValueReferencePickerViewModel([new SourceStepItem("detect", "Erkennung", contract)],
            InputContractRegistry.ForField(new("text", "", StepValueKind.Text)), selectDefault: false);
        picker.UseStepResultCommand.Execute(null);
        var source = Assert.Single(picker.SelectionTree);
        var diagnostic = Assert.Single(source.Children, node => node.DisplayName == Loc.Get("Ui.ValueReference.ResultDiagnostics"));
        Assert.False(diagnostic.IsExpanded);
        picker.SearchText = "Reihenfolgeprüfung";
        source = Assert.Single(picker.SelectionTree);
        diagnostic = Assert.Single(source.Children);
        Assert.True(diagnostic.IsExpanded);
        var timestamp = Assert.Single(diagnostic.Children);
        Assert.NotNull(timestamp.SelectCommand);
        timestamp.SelectCommand.Execute(null);
        Assert.Equal("source_frame_timestamp", picker.ToBinding().PropertyId);
        Assert.Contains("Monotoner", picker.SelectedTooltipDescription);
        picker.SearchText = "Klickziel";
        Assert.Contains(Flatten(picker.SelectionTree), node => node.Description?.Contains("Klickziel") == true);
    }

    private static IEnumerable<ConditionSelectionNode> Flatten(IEnumerable<ConditionSelectionNode> nodes) =>
        nodes.SelectMany(node => new[] { node }.Concat(Flatten(node.Children)));
}
