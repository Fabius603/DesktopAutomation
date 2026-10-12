using System.Text.Json.Nodes;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class ValueSourceCompatibilityTests
{
    [Fact]
    public async Task AddingAnOverlayTextRow_KeepsItsNumericDefaultsValidInTheLiveEditor()
    {
        var step = new ShowOnDesktopStep();
        using var vm = JobStepsViewModelExecutionTests.CreateRenderViewModel(new Job { Steps = [step] });
        vm.SelectedStep = step;
        var editor = vm.SelectedGeneratedEditor!;
        var overlay = editor.Fields.Single().VisualOverlayEditor!;
        overlay.AddOverlayTextCommand.Execute(null);
        Assert.True(editor.TryCreateStep(out _), editor.ValidationError);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (vm.IsMutationBusy || vm.Steps[0] is not ShowOnDesktopStep current || current.Settings.Overlay.TextResults.Count != 1)
            await Task.Delay(5, timeout.Token);
        Assert.Null(vm.InlineEditorValidationError);
    }

    [Fact]
    public void WholeStructuredVariable_IsSelectableForTextAsTheWholeValue()
    {
        var variable = new JobVariable
        {
            Name = "Payload",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.ResultObject,
            Value = JsonNode.Parse("{\"count\":7}")
        };
        var contract = InputContractRegistry.ForField(new("title", "", StepValueKind.Text));
        var picker = new ValueReferencePickerViewModel([], contract, false, [variable]);
        picker.UseJobVariableCommand.Execute(null);
        var node = Assert.Single(picker.SelectionTree, node => node.DisplayName == "Payload");
        Assert.NotNull(node.SelectCommand);
        node.SelectCommand.Execute(null);
        Assert.Equal(variable.Id.ToString("D"), picker.ToBinding().SourceId);
        Assert.Null(picker.ToBinding().ValuePath);
        Assert.True(picker.IsConfigured);
    }

    [Fact]
    public void ExistingProcessVariables_AreAvailableEvenWithoutAScalarEditor()
    {
        var contract = InputContractRegistry.Find(typeof(ActiveProcessStep), "process")!;
        var picker = new ValueReferencePickerViewModel([], contract, false,
            context: new ValueReferencePickerContext("Process", "Target", CreateJobVariable: _ => new JobVariable()));
        Assert.True(picker.CanUseJobVariables);
        Assert.False(picker.CanCreateJobVariable);
        Assert.False(picker.CreateJobVariableCommand.CanExecute(null));
    }

    [Fact]
    public void OptionalSingleIntegerVariable_IsSelectableForAnIntegerField()
    {
        var variable = new JobVariable
        {
            Name = "Coordinate",
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Integer,
            Cardinality = ResultCardinality.OptionalSingle,
            Value = JsonValue.Create(4)
        };
        var contract = InputContractRegistry.ForField(new("roi.x", "", StepValueKind.Integer));
        var picker = new ValueReferencePickerViewModel([], contract, false, [variable]);
        picker.UseJobVariableCommand.Execute(null);
        var node = Assert.Single(picker.SelectionTree, node => node.DisplayName == "Coordinate");
        Assert.NotNull(node.SelectCommand);
        node.SelectCommand.Execute(null);
        Assert.Equal(variable.Id.ToString("D"), picker.ToBinding().SourceId);
    }
}
