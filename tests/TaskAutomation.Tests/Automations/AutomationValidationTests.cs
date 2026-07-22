using TaskAutomation.Automations;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Automations;

public sealed class AutomationValidationTests
{
    [Fact] public void Validate_ValidHotkeyAutomation_IsValid() => AssertValid(Valid());
    [Fact] public void Validate_MissingName_ReturnsNameRequired() => AssertError(Valid().WithChange(a => a.Name = " "), AutomationValidationError.NameRequired);
    [Fact] public void Validate_JobIdMissing_ReturnsActionRequired() => AssertError(Valid().WithChange(a => a.Action.JobId = null), AutomationValidationError.ActionRequired);
    [Fact] public void Validate_MakroIdMissing_ReturnsActionRequired() => AssertError(Valid().WithChange(a => { a.Action.ActionType = AutomationActionTarget.Makro; a.Action.MakroId = null; }), AutomationValidationError.ActionRequired);
    [Fact] public void Validate_HotkeyMissing_ReturnsHotkeyRequired() => AssertError(Valid().WithChange(a => a.Trigger = new HotkeyAutomationTrigger()), AutomationValidationError.HotkeyRequired);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_ProcessTriggerRequiresName(bool started)
    {
        var trigger = started ? (AutomationTrigger)new ProcessStartedAutomationTrigger() : new ProcessExitedAutomationTrigger();
        AssertError(Valid().WithChange(a => a.Trigger = trigger), AutomationValidationError.ProcessNameRequired);
    }

    [Fact] public void Validate_WindowTriggerRequiresAtLeastOneFilter() => AssertError(
        Valid().WithChange(a => a.Trigger = new WindowEventAutomationTrigger()), AutomationValidationError.WindowFilterRequired);

    [Fact]
    public void Validate_FileSystemTriggerChecksPathAndFilter()
    {
        AssertError(Valid().WithChange(a => a.Trigger = new FileSystemAutomationTrigger()), AutomationValidationError.FolderRequired);
        AssertError(Valid().WithChange(a => a.Trigger = new FileSystemAutomationTrigger { DirectoryPath = "Z:\\unlikely-missing", Filter = "*" }),
            AutomationValidationError.FolderNotFound);
        using var directory = new TemporaryDirectory();
        AssertError(Valid().WithChange(a => a.Trigger = new FileSystemAutomationTrigger { DirectoryPath = directory.Path, Filter = "" }),
            AutomationValidationError.FileFilterRequired);
        AssertValid(Valid().WithChange(a => a.Trigger = new FileSystemAutomationTrigger { DirectoryPath = directory.Path, Filter = "*.txt" }));
    }

    [Fact] public void Validate_ScheduleRequiresWeekday() => AssertError(Valid().WithChange(a =>
        a.Trigger = new ScheduleAutomationTrigger { Days = [] }), AutomationValidationError.WeekdayRequired);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_IntervalMustBePositive(int seconds) => AssertError(Valid().WithChange(a =>
        a.Trigger = new IntervalAutomationTrigger { Interval = TimeSpan.FromSeconds(seconds) }), AutomationValidationError.IntervalPositive);

    [Fact]
    public void Validate_EnabledWindowRequiresBothBounds()
    {
        AssertError(Valid().WithChange(a => a.RunPolicy.EnabledFrom = new TimeOnly(8, 0)), AutomationValidationError.ActiveWindowPair);
        AssertValid(Valid().WithChange(a => { a.RunPolicy.EnabledFrom = new TimeOnly(22, 0); a.RunPolicy.EnabledUntil = new TimeOnly(6, 0); }));
    }

    [Fact]
    public void Validate_WindowsEventChecksCapabilityAndRequiredFilters()
    {
        AssertError(Valid().WithChange(a => a.Trigger = new WindowsEventAutomationTrigger { EventType = "unknown" }), AutomationValidationError.WindowsEventRequired);
        AssertError(Valid().WithChange(a => a.Trigger = new WindowsEventAutomationTrigger { EventType = "filesystem.created" }), AutomationValidationError.WindowsEventRequired);
        AssertValid(Valid().WithChange(a => a.Trigger = new WindowsEventAutomationTrigger { EventType = "filesystem.created",
            Filters = new(StringComparer.OrdinalIgnoreCase) { ["path"] = "C:\\temp" } }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1023)]
    [InlineData(65536)]
    public void Validate_WebhookRejectsInvalidPort(int port) => AssertError(Valid().WithChange(a =>
        a.Trigger = ValidWebhook().WithChange(w => w.Port = port)), AutomationValidationError.WebhookConfigurationInvalid);

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com?a=1")]
    [InlineData("not-a-url")]
    public void Validate_OnlineWebhookRequiresCleanHttpsBaseUrl(string url) => AssertError(Valid().WithChange(a =>
        a.Trigger = ValidWebhook().WithChange(w => { w.NetworkMode = WebhookNetworkMode.Online; w.OnlineBaseUrl = url; })),
        AutomationValidationError.WebhookConfigurationInvalid);

    [Fact]
    public void Validate_ValidOnlineWebhook_IsValid() => AssertValid(Valid().WithChange(a =>
        a.Trigger = ValidWebhook().WithChange(w => { w.NetworkMode = WebhookNetworkMode.Online; w.OnlineBaseUrl = "https://example.com/hooks"; })));

    private static AutomationDefinition Valid() => new() { Name = "valid", Trigger = new HotkeyAutomationTrigger { VirtualKeyCode = 65 },
        Action = new() { ActionType = AutomationActionTarget.Job, JobId = Guid.NewGuid(), Name = "job" } };
    private static WebhookAutomationTrigger ValidWebhook() => new() { Port = 17843, Secret = new string('x', 24) };
    private static void AssertValid(AutomationDefinition definition) => Assert.Equal(AutomationValidationError.None, AutomationValidation.Validate(definition).Error);
    private static void AssertError(AutomationDefinition definition, AutomationValidationError expected) => Assert.Equal(expected, AutomationValidation.Validate(definition).Error);
}

internal static class TestObjectExtensions
{
    public static T WithChange<T>(this T value, Action<T> change) { change(value); return value; }
}
