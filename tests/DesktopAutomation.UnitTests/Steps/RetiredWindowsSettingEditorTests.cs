using System.Text.Json;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.Steps;

public sealed class RetiredWindowsSettingEditorTests
{
    [Theory]
    [InlineData("audio.default_output")]
    [InlineData("audio.default_input")]
    [InlineData("personalization.theme")]
    [InlineData("notifications.focus_mode")]
    public void RemovedSelection_IsPreservedUntilAnExplicitSupportedReplacement(string id)
    {
        var selection = new StepWindowsCapabilitySelectionValue(id, new Dictionary<string, string?> { ["old_value"] = "saved" });
        var editor = new GeneratedWindowsCapabilityEditorViewModel(JsonSerializer.SerializeToNode(selection),
            StepWindowsCapabilityPickerMode.SettingChange);
        Assert.Null(editor.Picker.SelectedCapability);
        Assert.False(editor.Picker.IsValid);
        Assert.Equal(id, editor.ToValue().CapabilityId);
        Assert.Equal("saved", editor.ToValue().Parameters["old_value"]);
        editor.Picker.SelectedCapability = new WindowsCapabilityCatalog().Find("audio.master_volume");
        Assert.True(editor.Picker.IsValid);
        Assert.Equal("audio.master_volume", editor.ToValue().CapabilityId);
        Assert.DoesNotContain("old_value", editor.ToValue().Parameters.Keys);
    }
}
