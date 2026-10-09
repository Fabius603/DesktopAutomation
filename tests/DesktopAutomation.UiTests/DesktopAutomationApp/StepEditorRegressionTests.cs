using DesktopAutomationApp.ViewModels;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class StepEditorRegressionTests
{
    [Fact]
    public void ProcessQuery_MarksBothAlternatives_WhenNeitherIsProvided()
    {
        var editor = new GeneratedStepEditorViewModel(new GetProcessStepDefinition());
        Assert.False(editor.TryCreateStep(out _));
        Assert.True(editor.Fields.Single(field => field.Descriptor.Id == GetProcessStepDefinition.ProcessNameFieldId).Validation.HasError);
        Assert.True(editor.Fields.Single(field => field.Descriptor.Id == GetProcessStepDefinition.ExecutablePathFieldId).Validation.HasError);
        Assert.False(editor.Fields.Single(field => field.Descriptor.Id == GetProcessStepDefinition.WindowTitleFieldId).Validation.HasError);
        editor.Fields.Single(field => field.Descriptor.Id == GetProcessStepDefinition.ProcessNameFieldId).InputText = "notepad";
        Assert.True(editor.TryCreateStep(out _), editor.ValidationError);
        Assert.All(editor.Fields, field => Assert.False(field.Validation.HasError));
    }

    [Fact]
    public void WindowsSetting_UsesVisibleParameterDefaults_InJobValidation()
    {
        var step = new WindowsSettingChangeStep();
        using var vm = JobStepsViewModelExecutionTests.CreateRenderViewModel(new Job { Steps = [step] });
        vm.SelectedStep = vm.Steps[0];
        var editor = vm.SelectedGeneratedEditor!;
        var field = Assert.Single(editor.Fields);
        Assert.True(field.WindowsCapabilityEditor!.Picker.IsValid);
        Assert.True(editor.TryCreateStep(out var created), editor.ValidationError);
        var updated = Assert.IsType<WindowsSettingChangeStep>(created);
        Assert.Equal("50", updated.Settings.Parameters["value"]);
        var result = JobValidation.ValidateCandidate([], updated, [updated], vm.Job.LocalValues.Cast<JobVariable>().Concat(vm.Job.Variables).ToArray());
        Assert.True(result.IsValid, result.Error);
    }
}
