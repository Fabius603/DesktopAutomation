using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TaskAutomation.Automations;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;

internal static class Examples
{
    public static void Export(string output)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        JobJsonSerialization.Configure(options);
        var choose = new UserChoiceStep { Settings = new() { Title = "Arbeitsunterlagen", Question = "Was möchtest du vorbereiten?",
            Options = [new() { Id = "rechnung", Label = "Rechnungen", Value = "rechnung" }, new() { Id = "notiz", Label = "Notizen", Value = "notiz" }] } };
        var prepare = new Job { Id = Id("unterlagen"), Name = "Arbeitsunterlagen vorbereiten", Steps = [choose,
            If(choose, "selected_value", "rechnung"), Copy("Rechnungen", "Arbeitsablage\\Rechnungen"), Start("notepad"),
            new ElseStep(), Copy("Notizen", "Arbeitsablage\\Notizen"), Start("notepad"), new EndIfStep(),
            new MakroExecutionStep { IsEnabled = false, Settings = new() { MakroId = Id("makro"), MakroName = "Notiz eingeben" } },
            new ShowTextStep { Settings = new() { TextSource = ShowTextSource.TaskResult, TextResult = ResultBinding.ForStepResult(choose.Id, "selected_label"), FontColor = "#243044" } }] };
        var confirm = new UserChoiceStep { Settings = new() { Title = "Dateisicherung", Question = "Beispieldateien sichern?",
            Options = [new() { Id = "ja", Label = "Sichern", Value = "ja" }, new() { Id = "nein", Label = "Überspringen", Value = "nein" }] } };
        var backup = new Job { Id = Id("sicherung"), Name = "Eingang mit Bestätigung sichern", Steps = [confirm,
            If(confirm, "selected_value", "ja"), Copy("Rechnungen", "Sicherung\\Rechnungen"), Copy("Notizen", "Sicherung\\Notizen"),
            Text("Beide Ordner wurden kopiert."), new ElseStep(), Text("Sicherung wurde nicht angefordert."), new EndIfStep(), Text("Die Details stehen im Ausführungsverlauf.")] };
        var process = new ActiveProcessStep { Settings = new() { Target = new() { ProcessName = "notepad" } } };
        var workplace = new Job { Id = Id("arbeitsplatz"), Name = "Arbeitsplatz nach Zeitplan", Steps = [process,
            If(process, "is_running", "False"), Start("notepad"), Text("Editor wurde gestartet."), new ElseStep(), Text("Editor läuft bereits."),
            new EndIfStep(), Start("calc"), Text("Arbeitsplatz ist vorbereitet.")] };
        var macro = new Makro { Id = Id("makro"), Name = "Notiz eingeben", Befehle = [new TextInputBefehl { Text = "Notiz aus dem Beispielmakro", DurationMicroseconds = 0 }, new TimeoutBefehl { Duration = 200 }] };
        var packs = new[] { ("unterlagen", prepare), ("sicherung", backup), ("arbeitsplatz", workplace) };
        foreach (var (key, job) in packs)
        {
            for (var i = 0; i < job.Steps.Count; i++) job.Steps[i].Id = key + "-" + i;
            // Bindings must follow deterministic step IDs.
            foreach (var condition in job.Steps.OfType<IfStep>())
                foreach (var binding in condition.Settings.Conditions) binding.SourceId = ResultBinding.ForStepResult(job.Steps[0].Id, binding.PropertyId!).SourceId;
            foreach (var show in job.Steps.OfType<ShowTextStep>().Where(s => s.Settings.TextSource == ShowTextSource.TaskResult))
                show.Settings.TextResult = ResultBinding.ForStepResult(job.Steps[0].Id, "selected_label");
            JobVariableInputMigration.Migrate(job);
            // Migration creates local variable IDs: make them reproducible and preserve references.
            Stabilize(job, key);
            var json = JsonSerializer.Serialize(job, options);
            var reread = JsonSerializer.Deserialize<Job>(json, options)!;
            var result = JobValidation.ValidateJob(reread);
            if (!result.IsValid) throw new InvalidOperationException(key + ": " + JsonSerializer.Serialize(result));
            var directory = Path.Combine(output, "examples", key, "jobs");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, key + ".json"), json + "\n");
            var automation = new AutomationDefinition { Id = Id("automation-" + key), Name = job.Name, Active = false,
                CreatedAt = new DateTimeOffset(2026, 10, 8, 0, 0, 0, TimeSpan.Zero), UpdatedAt = new DateTimeOffset(2026, 10, 8, 0, 0, 0, TimeSpan.Zero),
                Trigger = new ScheduleAutomationTrigger { Days = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday], TimeOfDay = new TimeOnly(8, 30) },
                Action = new() { JobId = job.Id, Name = job.Name } };
            if (key == "unterlagen") automation.Trigger = new HotkeyAutomationTrigger { VirtualKeyCode = 0x55, Modifiers = TaskAutomation.Hotkeys.KeyModifiers.Control | TaskAutomation.Hotkeys.KeyModifiers.Alt };
            if (key == "sicherung") automation.Trigger = new FileSystemAutomationTrigger { DirectoryPath = @"C:\DesktopAutomation-Beispiele\Rechnungen", Filter = "*.txt" };
            var automationJson = JsonSerializer.Serialize(automation, options);
            var rereadAutomation = JsonSerializer.Deserialize<AutomationDefinition>(automationJson, options)!;
            // The folder must exist on the importing machine. Validate its other rules with an isolated fixture.
            var temporary = Path.Combine(Path.GetTempPath(), "DesktopAutomation-website-" + Guid.NewGuid());
            Directory.CreateDirectory(temporary);
            try
            {
                if (rereadAutomation.Trigger is FileSystemAutomationTrigger folder) folder.DirectoryPath = temporary;
                var valid = AutomationValidation.Validate(rereadAutomation);
                if (!valid.IsValid) throw new InvalidOperationException(key + " automation: " + valid.Error);
            }
            finally { Directory.Delete(temporary); }
            directory = Path.Combine(output, "examples", key, "automations");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, key + ".json"), automationJson + "\n");
        }
        for (var i = 0; i < macro.Befehle.Count; i++) macro.Befehle[i].Id = "notiz-" + i;
        var macroJson = JsonSerializer.Serialize(macro, options);
        if (!MakroValidation.Validate(JsonSerializer.Deserialize<Makro>(macroJson, options)!).IsValid) throw new InvalidOperationException("Macro invalid");
        Directory.CreateDirectory(Path.Combine(output, "examples/unterlagen/makros"));
        File.WriteAllText(Path.Combine(output, "examples/unterlagen/makros/notiz.json"), macroJson + "\n");
    }

    private static Guid Id(string value) => new(MD5.HashData(Encoding.UTF8.GetBytes("website-example-" + value)));
    private static ShowTextStep Text(string text) => new() { Settings = new() { Text = text, FontColor = "#243044" } };
    private static StartProcessStep Start(string executable) => new() { Settings = new() { ExecutablePath = @"C:\Windows\System32\" + executable + ".exe" } };
    private static FileSystemOperationStep Copy(string source, string target) => new() { Settings = new() { Operation = FileSystemOperation.Copy,
        SourcePath = @"C:\DesktopAutomation-Beispiele\" + source, TargetPath = @"C:\DesktopAutomation-Beispiele\" + target,
        RetryLockedFiles = true, RetryCount = 3, RetryDelayMs = 100 } };
    private static IfStep If(JobStep source, string property, string value) => new() { Settings = new() { Conditions = [new StepCondition
        { ProviderId = ValueProviderIds.StepResult, SourceId = ResultBinding.ForStepResult(source.Id, property).SourceId, Operator = ConditionOperator.Equals, ComparisonValue = value }] } };

    internal static void Stabilize(Job job, string prefix)
    {
        var jsonOptions = new JsonSerializerOptions();
        JobJsonSerialization.Configure(jsonOptions);
        var json = JsonSerializer.Serialize(job, jsonOptions);
        for (var i = 0; i < job.Variables.Count; i++) json = json.Replace(job.Variables[i].Id.ToString(), Id(prefix + "-value-" + i).ToString(), StringComparison.Ordinal);
        for (var i = 0; i < job.LocalValues.Count; i++) json = json.Replace(job.LocalValues[i].Id.ToString(), Id(prefix + "-local-value-" + i).ToString(), StringComparison.Ordinal);
        var stable = JsonSerializer.Deserialize<Job>(json, jsonOptions)!;
        job.LocalValues = stable.LocalValues;
        job.Variables = stable.Variables;
        job.Steps = stable.Steps;
    }
}
