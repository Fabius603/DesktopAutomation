using System.Runtime.CompilerServices;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class UserChoiceCancellationTests
{
    [Fact]
    public void CancellationIsPropagatedAfterLeavingTheDispatcherCallback()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Services", "WpfUserChoiceService.cs"));

        var dispatcherCall = source.IndexOf(
            "var selectedOptionId = await dispatcher.InvokeAsync",
            StringComparison.Ordinal);
        var cooperativeCheck = source.IndexOf(
            "if (cancellationToken.IsCancellationRequested)",
            StringComparison.Ordinal);
        var callbackResult = source.IndexOf(
            "return accepted ? dialog.SelectedOptionId : null;",
            StringComparison.Ordinal);
        var cancellationPropagation = source.IndexOf(
            "cancellationToken.ThrowIfCancellationRequested();",
            StringComparison.Ordinal);

        Assert.True(dispatcherCall >= 0);
        Assert.True(cooperativeCheck > dispatcherCall);
        Assert.True(callbackResult > cooperativeCheck);
        Assert.True(cancellationPropagation > callbackResult);
        Assert.Equal(
            1,
            CountOccurrences(source, "cancellationToken.ThrowIfCancellationRequested();"));
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        for (var index = 0; (index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0;
             index += search.Length)
            count++;
        return count;
    }

    private static string RepositoryRoot([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(sourceFile)!, "..", "..", ".."));
}
