using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed class UserChoiceStepHandler(IUserChoiceService choiceService)
    : JobStepHandler<UserChoiceStep, UserChoiceResult>
{
    protected override async Task<UserChoiceResult> ExecuteCoreAsync(
        UserChoiceStep step,
        IStepPipelineContext context,
        CancellationToken cancellationToken)
    {
        var options = step.Settings.Options
            .Select(option => new UserChoiceDialogOption(option.Id, option.Label))
            .ToArray();
        var selectedId = await choiceService.ChooseAsync(
            new UserChoiceDialogRequest(
                step.Settings.Title,
                step.Settings.Question,
                step.Settings.Description,
                step.Settings.DesktopIndex,
                options),
            cancellationToken).ConfigureAwait(false);

        if (selectedId is null)
            return new UserChoiceResult { WasExecuted = true, WasCancelled = true, SelectedIndex = -1 };

        var selectedIndex = step.Settings.Options.FindIndex(option =>
            string.Equals(option.Id, selectedId, StringComparison.Ordinal));
        if (selectedIndex < 0)
            throw new InvalidOperationException("The selected answer no longer exists in the step configuration.");

        var selected = step.Settings.Options[selectedIndex];
        return new UserChoiceResult
        {
            WasExecuted = true,
            SelectedOptionId = selected.Id,
            SelectedLabel = selected.Label,
            SelectedValue = string.IsNullOrWhiteSpace(selected.Value)
                ? selected.Label
                : selected.Value,
            SelectedIndex = selectedIndex
        };
    }

    protected override UserChoiceResult CreateDefault() => UserChoiceResult.Default;
}
