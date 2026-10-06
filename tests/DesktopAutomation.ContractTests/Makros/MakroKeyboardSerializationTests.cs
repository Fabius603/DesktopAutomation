using System.Collections.ObjectModel;
using System.Text.Json;
using TaskAutomation.Makros;

namespace TaskAutomation.Tests.Makros;

public sealed class MakroKeyboardSerializationTests
{
    [Fact]
    public void NewCommands_RoundTripPreservesUnicodeKeysIdentityGroupsAndTiming()
    {
        var macro = new Makro
        {
            Name = "unicode",
            RecordingSettings = new() { CombineKeyboardInputs = true },
            Befehle = new ObservableCollection<MakroBefehl>([
                new TextInputBefehl { Id = "text", GroupId = "group", Text = "Grüße 漢字 😀", DelayBeforeMicroseconds = 875, DurationMicroseconds = 125 },
                new KeyCombinationBefehl { Id = "keys", Keys = ["Ctrl", "S"], DurationMicroseconds = 1000 }])
        };
        var json = MakroSnapshotService.Serialize(macro);
        Assert.Contains("text_input", json);
        Assert.Contains("key_combination", json);
        var restored = JsonSerializer.Deserialize<Makro>(json, new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } })!;
        Assert.True(restored.RecordingSettings.CombineKeyboardInputs);
        var text = Assert.IsType<TextInputBefehl>(restored.Befehle[0]);
        Assert.Equal(("text", "group", "Grüße 漢字 😀", 875L, 125L), (text.Id, text.GroupId, text.Text, text.DelayBeforeMicroseconds!.Value, text.DurationMicroseconds));
        var keys = Assert.IsType<KeyCombinationBefehl>(restored.Befehle[1]);
        Assert.Equal(["Ctrl", "S"], keys.Keys);
        Assert.Equal(1000, keys.DurationMicroseconds);
    }

    [Fact]
    public void NewCommands_CloneDoesNotShareMutableKeys()
    {
        var original = new KeyCombinationBefehl { Keys = ["Ctrl", "S"] };
        var clone = Assert.IsType<KeyCombinationBefehl>(MakroSnapshotService.CloneCommand(original));
        clone.Keys[1] = "A";
        Assert.Equal("S", original.Keys[1]);
    }

    [Fact]
    public void LegacySettings_DoNotEnableRecordingConversion()
    {
        var macro = JsonSerializer.Deserialize<Makro>("""{"name":"old","commands":[{"type":"key_down","key":"A"}]}""")!;
        Assert.False(macro.RecordingSettings.CombineKeyboardInputs);
        Assert.IsType<KeyDownBefehl>(Assert.Single(macro.Befehle));
    }

    [Theory]
    [InlineData("text_input", "\"text\":\"hello\"", typeof(TextInputBefehl))]
    [InlineData("key_combination", "\"keys\":[\"Ctrl\",\"S\"]", typeof(KeyCombinationBefehl))]
    public void PolymorphicCommand_ReadsNewDiscriminators(string type, string fields, Type expected)
    {
        var options = new JsonSerializerOptions();
        var command = JsonSerializer.Deserialize<MakroBefehl>($"{{\"type\":\"{type}\",{fields}}}", options);
        Assert.IsType(expected, command);
    }
}
