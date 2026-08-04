using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Media;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Contracts.Steps;
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
    public void InitializingGeneratedEditor_DoesNotUseHandledConversionExceptions()
    {
        var exceptions = new ConcurrentQueue<Exception>();
        EventHandler<FirstChanceExceptionEventArgs> handler = (_, args) =>
        {
            if (args.Exception is JsonException or InvalidOperationException or FormatException
                && args.Exception.StackTrace?.Contains(
                    nameof(GeneratedStepEditorViewModel), StringComparison.Ordinal) == true)
                exceptions.Enqueue(args.Exception);
        };

        AppDomain.CurrentDomain.FirstChanceException += handler;
        try
        {
            _ = new GeneratedStepEditorViewModel(new ShowTextStepDefinition());
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
}
