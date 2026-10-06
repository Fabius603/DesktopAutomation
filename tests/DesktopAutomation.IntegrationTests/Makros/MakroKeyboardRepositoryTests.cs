using System.Collections.ObjectModel;
using Common.JsonRepository;
using TaskAutomation.Makros;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Makros;

public sealed class MakroKeyboardRepositoryTests
{
    [Fact]
    public async Task SavedKeyboardCommands_ReloadWithUnicodeGroupsAndRecordingPolicy()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new JsonRepository<Makro>(new JsonRepositoryOptions { DirectoryPath = directory.Path }, macro => macro.Id.ToString());
        var macro = new Makro
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000123"),
            Name = "Keyboard",
            RecordingSettings = new() { CombineKeyboardInputs = true },
            Gruppen = new([new MakroGruppe { Id = "input", Title = "Input" }]),
            Befehle = new ObservableCollection<MakroBefehl>([
                new TextInputBefehl { Id = "text", GroupId = "input", Text = "Grüße 😀", DelayBeforeMicroseconds = 875 },
                new KeyCombinationBefehl { Id = "keys", GroupId = "input", Keys = ["Ctrl", "S"], DurationMicroseconds = 125 }])
        };
        await repository.SaveAsync(macro);
        var restored = Assert.Single(await repository.LoadAllAsync());
        Assert.Empty(repository.LoadErrors);
        Assert.True(MakroValidation.Validate(restored).IsValid);
        Assert.True(restored.RecordingSettings.CombineKeyboardInputs);
        Assert.Equal("Grüße 😀", Assert.IsType<TextInputBefehl>(restored.Befehle[0]).Text);
        Assert.Equal(["Ctrl", "S"], Assert.IsType<KeyCombinationBefehl>(restored.Befehle[1]).Keys);
        Assert.Equal(1000, MakroTimeline.GetTotalDurationMicroseconds(restored.Befehle));
    }
}
