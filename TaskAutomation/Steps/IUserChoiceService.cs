namespace TaskAutomation.Steps;

public sealed record UserChoiceDialogOption(string Id, string Label);

public sealed record UserChoiceDialogRequest(
    string Title,
    string Question,
    string Description,
    int DesktopIndex,
    IReadOnlyList<UserChoiceDialogOption> Options);

public interface IUserChoiceService
{
    Task<string?> ChooseAsync(UserChoiceDialogRequest request, CancellationToken cancellationToken);
}
