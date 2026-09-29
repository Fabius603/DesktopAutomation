using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Media;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class GeneratedStepEditorConversionTests
{
    [Theory]
    [InlineData("#123", 0x11, 0x22, 0x33, 0xFF)]
    [InlineData("#80112233", 0x11, 0x22, 0x33, 0x80)]
    [InlineData("Red", 0xFF, 0x00, 0x00, 0xFF)]
    public void ColorParser_ParsesSupportedValuesWithoutUsingWpfConversion(
        string value, byte red, byte green, byte blue, byte alpha)
    {
        Assert.True(WpfColorParser.TryParse(value, out var color));
        Assert.Equal(Color.FromArgb(alpha, red, green, blue), color);
    }

    [Fact]
    public void ColorParser_ReturnsFalseForInvalidValues()
    {
        Assert.False(WpfColorParser.TryParse("not-a-color", out var color));
        Assert.Equal(Colors.White, color);
    }

    [Fact]
    public void NonChoiceField_AcceptsPrimitiveJsonWithoutTryingToDeserializeAChoice()
    {
        var descriptor = new StepFieldDescriptor(
            "value", "Ui.Common.Value", StepValueKind.Text, Required: false, Order: 0);

        var field = new GeneratedStepFieldViewModel(descriptor, JsonValue.Create(42));

        Assert.Equal("42", field.InputText);
        Assert.False(field.UsesChoicePicker);
    }

    [Fact]
    public void RequiredEnumField_WithEmptyValueDoesNotInventASelection()
    {
        var descriptor = new StepFieldDescriptor(
            "mode", "Ui.Common.Value", StepValueKind.Enum, Required: true, Order: 0,
            DefaultValue: JsonValue.Create("second"),
            Options:
            [
                new StepFieldOptionDescriptor("first", "Ui.Common.Value", "First option"),
                new StepFieldOptionDescriptor("second", "Ui.Common.Value", "Second option")
            ]);
        var field = new GeneratedStepFieldViewModel(descriptor, value: null);

        Assert.Null(field.SelectedEnumValue);
        Assert.Null(field.SelectedEnumOption);
        Assert.Equal(string.Empty, field.InputText);
        Assert.False(field.TryWriteValue(new StepDraft("test"), out var error));
        Assert.NotNull(error);

        field.SelectedEnumValue = "second";

        Assert.Equal("second", field.SelectedEnumValue);
        Assert.Equal("second", field.InputText);
    }

    [Fact]
    public void GeneratedEnumField_TreatsTokensAsCaseSensitive()
    {
        var descriptor = new StepFieldDescriptor(
            "mode", "Ui.Common.Value", StepValueKind.Enum, Required: true,
            DefaultValue: JsonValue.Create("known"),
            Options: [new StepFieldOptionDescriptor("known", "Ui.Common.Value", "Known")]);

        var field = new GeneratedStepFieldViewModel(descriptor, JsonValue.Create("KNOWN"));

        Assert.Null(field.SelectedEnumValue);
        Assert.True(field.HasInvalidEnumValue);
        Assert.Equal("KNOWN", field.InputText);
    }

    [Fact]
    public void InitializingEveryBuiltInGeneratedEditor_DoesNotUseHandledConversionExceptions()
    {
        var exceptions = new ConcurrentQueue<string>();
        string? currentStep = null;
        EventHandler<FirstChanceExceptionEventArgs> handler = (_, args) =>
        {
            if (args.Exception is JsonException or InvalidOperationException or FormatException
                && args.Exception.StackTrace?.Contains(
                    nameof(GeneratedStepEditorViewModel), StringComparison.Ordinal) == true)
                exceptions.Enqueue($"{currentStep}: {args.Exception.GetType().Name}: {args.Exception.Message}");
        };

        AppDomain.CurrentDomain.FirstChanceException += handler;
        try
        {
            foreach (var definition in BuiltInStepDefinitions.Instance.Definitions)
            {
                currentStep = definition.StepType.Name;
                _ = new GeneratedStepEditorViewModel(definition);
            }
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= handler;
        }

        Assert.Empty(exceptions);
    }

    [Fact]
    public void ImagePreview_InaccessiblePathReturnsNoPreviewWithoutAccessException()
    {
        var descriptor = new TemplateMatchingStepDefinition().Descriptor.Fields
            .Single(field => field.Id == TemplateMatchingStepDefinition.TemplatePathFieldId);
        var field = new GeneratedStepFieldViewModel(
            descriptor,
            JsonValue.Create(@"C:\System Volume Information\desktopautomation-preview.png"));
        var exceptions = new ConcurrentQueue<Exception>();
        EventHandler<FirstChanceExceptionEventArgs> handler = (_, args) =>
        {
            if (args.Exception is UnauthorizedAccessException)
                exceptions.Enqueue(args.Exception);
        };

        AppDomain.CurrentDomain.FirstChanceException += handler;
        try
        {
            Assert.Null(field.FilePreview);
            Assert.False(field.HasFilePreview);
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= handler;
        }

        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task SupersededEditorComparisons_DoNotThrowCancellationExceptions()
    {
        var exceptions = new ConcurrentQueue<Exception>();
        EventHandler<FirstChanceExceptionEventArgs> handler = (_, args) =>
        {
            if (args.Exception is TaskCanceledException
                && args.Exception.StackTrace?.Contains(nameof(EditorChangeTracker<int>), StringComparison.Ordinal) == true)
                exceptions.Enqueue(args.Exception);
        };
        using var tracker = new EditorChangeTracker<int>(
            0, static (left, right, _) => Task.FromResult(left == right), _ => { },
            TimeSpan.FromMilliseconds(20));

        AppDomain.CurrentDomain.FirstChanceException += handler;
        try
        {
            tracker.Evaluate(1);
            tracker.Evaluate(2);
            await tracker.WhenIdleAsync();
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= handler;
        }

        Assert.Empty(exceptions);
    }

    [Fact]
    public void InvalidPersistedVariableValue_IsResetWithoutJsonTypeException()
    {
        var exceptions = new ConcurrentQueue<Exception>();
        EventHandler<FirstChanceExceptionEventArgs> handler = (_, args) =>
        {
            if (args.Exception is InvalidOperationException
                && args.Exception.StackTrace?.Contains(nameof(JobVariableEditorViewModel), StringComparison.Ordinal) == true)
                exceptions.Enqueue(args.Exception);
        };
        var variable = new JobVariable
        {
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create("not-an-integer")
        };

        AppDomain.CurrentDomain.FirstChanceException += handler;
        try
        {
            var editor = new JobVariableEditorViewModel(variable, _ => { });
            Assert.Equal(0, editor.IntegerValue);
            Assert.Equal(0, variable.Value!.GetValue<int>());
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= handler;
        }

        Assert.Empty(exceptions);
    }
}
