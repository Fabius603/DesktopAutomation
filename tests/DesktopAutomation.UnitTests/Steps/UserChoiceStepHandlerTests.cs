using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class UserChoiceStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsStableIdLabelValueAndCurrentIndex()
    {
        var service = new ChoiceService("production");
        var step = CreateStep();
        step.Settings.DesktopIndex = 2;
        var context = new PipelineContextStub();

        var result = Assert.IsType<UserChoiceResult>(
            await new UserChoiceStepHandler(service).ExecuteAsync(step, context, default));

        Assert.True(result.WasExecuted);
        Assert.False(result.WasCancelled);
        Assert.Equal("production", result.SelectedOptionId);
        Assert.Equal("Produktion", result.SelectedLabel);
        Assert.Equal("prod", result.SelectedValue);
        Assert.Equal(1, result.SelectedIndex);
        Assert.Same(result, context.Results.GetRaw(step.Id));
        Assert.Equal(2, service.LastRequest?.DesktopIndex);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledDialog_ReturnsExplicitCancelledResult()
    {
        var result = Assert.IsType<UserChoiceResult>(await new UserChoiceStepHandler(
            new ChoiceService(null)).ExecuteAsync(CreateStep(), new PipelineContextStub(), default));

        Assert.True(result.WasExecuted);
        Assert.True(result.WasCancelled);
        Assert.Equal(-1, result.SelectedIndex);
        Assert.Equal(string.Empty, result.SelectedOptionId);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyReturnValue_FallsBackToVisibleAnswerText()
    {
        var step = CreateStep();
        step.Settings.Options[0].Value = "  ";

        var result = Assert.IsType<UserChoiceResult>(await new UserChoiceStepHandler(
            new ChoiceService("development")).ExecuteAsync(step, new PipelineContextStub(), default));

        Assert.Equal("Entwicklung", result.SelectedValue);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownServiceResponse_Fails()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => new UserChoiceStepHandler(
            new ChoiceService("deleted")).ExecuteAsync(CreateStep(), new PipelineContextStub(), default));
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new UserChoiceStepHandler(
            new ChoiceService("development")).ExecuteAsync(
                CreateStep(), new PipelineContextStub(), cancellation.Token));
    }

    private static UserChoiceStep CreateStep() => new()
    {
        Id = "choice",
        Settings = new()
        {
            Title = "Umgebung",
            Question = "Welche Umgebung?",
            Options =
            [
                new() { Id = "development", Label = "Entwicklung", Value = "dev" },
                new() { Id = "production", Label = "Produktion", Value = "prod" }
            ]
        }
    };

    private sealed class ChoiceService(string? selectedId) : IUserChoiceService
    {
        public UserChoiceDialogRequest? LastRequest { get; private set; }

        public Task<string?> ChooseAsync(
            UserChoiceDialogRequest request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(["development", "production"], request.Options.Select(option => option.Id));
            return Task.FromResult(selectedId);
        }
    }
}
