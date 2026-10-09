using System.Text.Json.Nodes;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class StepAuthoringRegressionTests
{
    [Fact]
    public void DynamicRegionPadding_StartsWithIntegerZero_AndRetainsLegacyLiteral()
    {
        var dialog = new AddJobStepDialogViewModel(new ControllableJobExecutor([]), [], cameraCaptureService: new NoOpCameraCaptureService());
        dialog.SelectedType = "DynamicRoi";
        var padding = dialog.GeneratedEditor!.Fields.Single(field => field.Descriptor.Id == DynamicRoiStepDefinition.PaddingSourceFieldId);
        Assert.Equal("0", padding.InputText);
        Assert.Equal(0, padding.InputReferenceEditor!.Picker.SelectedJobVariable!.Value!.GetValue<int>());
        Assert.True(dialog.TryLoadGeneratedStep(new DynamicRoiStep { Settings = new DynamicRoiSettings { Padding = 12 } }));
        padding = dialog.GeneratedEditor!.Fields.Single(field => field.Descriptor.Id == DynamicRoiStepDefinition.PaddingSourceFieldId);
        Assert.Equal("12", padding.InputText);
        Assert.Equal(12, padding.InputReferenceEditor!.Picker.SelectedJobVariable!.Value!.GetValue<int>());
    }

    [Theory]
    [InlineData("KlickOnPoint")]
    [InlineData("KlickOnPoint3D")]
    public void ClickPointSources_OfferReferences_AndKeepPersistedLocalValuesReadable(string stepType)
    {
        var dialog = new AddJobStepDialogViewModel(new ControllableJobExecutor([]), [], cameraCaptureService: new NoOpCameraCaptureService());
        dialog.SelectedType = stepType;
        var source = dialog.GeneratedEditor!.Fields.Single(field => field.Descriptor.Id == "points_source");
        Assert.False(source.SupportsDirectValue);
        Assert.False(source.UseDirectValueCommand.CanExecute(null));
        Assert.True(source.InputReferenceEditor!.Picker.CanUseJobVariables);
        Assert.True(source.InputReferenceEditor.Picker.CanUseStepResults);
        Assert.True(BuiltInStepDefinitions.Instance.TryGetByName(stepType, out var definition));
        var contract = StepInputContractRegistry.Get(definition.StepType, "points")!;
        Assert.False(contract.AllowsProvider(ValueProviderIds.LocalValue));
        Assert.Contains(ValueProviderIds.LocalValue, contract.LegacyAllowedProviderIds);
        var step = definition.CreateDefault();
        var local = new LocalValue { OwnerStepId = step.Id, InputPath = "points_source", ValueKind = ResultValueKind.Point, Value = new JsonObject { ["x"] = 3, ["y"] = 7 } };
        step.Inputs["points_source"] = new ResultBinding { ProviderId = ValueProviderIds.LocalValue, SourceId = local.Id.ToString("D") };
        var validation = JobValidation.ValidateCandidate([], step, [step], [local]);
        Assert.True(validation.IsValid, validation.Error);
    }

    [Fact]
    public void WindowsSettingDefaults_AreAppliedWithoutReplacingExplicitlyEmptyRequiredValues()
    {
        var definition = new WindowsSettingChangeStepDefinition();
        var draft = definition.CreateDraft();
        Assert.Empty(definition.ValidateDraft(draft));
        var step = Assert.IsType<WindowsSettingChangeStep>(definition.ApplyDraft(draft));
        Assert.Equal("50", step.Settings.Parameters["value"]);
        step.Settings.Parameters["value"] = "";
        Assert.Contains(definition.ValidateDraft(definition.CreateDraft(step)), issue => issue.Severity == StepValidationSeverity.Error);
        step.Settings.Parameters["value"] = "101";
        Assert.Contains(definition.ValidateDraft(definition.CreateDraft(step)), issue => issue.Severity == StepValidationSeverity.Error);
        step.Settings.Parameters["value"] = "25";
        Assert.Empty(definition.ValidateDraft(definition.CreateDraft(step)));
    }
}
