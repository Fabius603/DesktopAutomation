using System.Reflection;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using DesktopAutomationApp.ViewModels;
using DesktopAutomationApp.Views;
using DesktopAutomationApp.Models;
using DesktopAutomation.Application.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Automations;
using TaskAutomation.Hotkeys;
using TaskAutomation.WindowsIntegration;

internal static class ReferenceViews
{
    public static void Render(Assembly tests, string root, string output, HashSet<string> selected,
        IReadOnlyDictionary<string, ScreenshotSpec> specs, JsonSerializerOptions options)
    {
        var scriptPath = Path.Combine(output, "Beispiel.ps1");
        File.WriteAllText(scriptPath, "Write-Output 'Beispiel'\n"); // Fixture only; never executed.
        var templatePath = Path.Combine(output, "Beispielvorlage.png");
        var visual = new System.Windows.Media.DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new System.Windows.Rect(0,0,120,80));
            dc.DrawRectangle(System.Windows.Media.Brushes.SteelBlue, null, new System.Windows.Rect(20,20,80,40));
        }
        var fixture = new System.Windows.Media.Imaging.RenderTargetBitmap(120,80,96,96,System.Windows.Media.PixelFormats.Pbgra32);
        fixture.Render(visual);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(fixture));
        using (var stream = File.Create(templatePath)) encoder.Save(stream);
        var templates = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "website/docs/authoring.json")))!["templates"]!;
        foreach (var id in selected.Where(id => id.StartsWith("ref-step-")))
        {
            var key = id[9..].Replace('-', '_');
            var variant = key.StartsWith("file_system_operation_") ? key[22..] : "";
            if (variant != "") key = "file_system_operation";
            var node = templates["steps"]![key]!.DeepClone(); Sample(node);
            if (variant != "") node["settings"]!["operation"] = variant == "rename" ? "Rename" : "Delete";
            if (node["settings"]?["script_path"] is not null) node["settings"]!["script_path"] = Path.GetRelativePath(root, scriptPath);
            if (node["settings"]?["overlay"] is JsonObject overlay)
                overlay["text_results"] = JsonSerializer.SerializeToNode(new[] { new TextResultOverlaySettings {
                    Result = ResultBinding.ForStepResult("example-choice", "selected_value"), OffsetX=100, OffsetY=80, FontColor="#2563EB" } }, options);
            if (node["settings"]?["template_path"] is not null) node["settings"]!["template_path"] = Path.GetRelativePath(root, templatePath);
            if (node["settings"]?["image_source"] is not null)
                node["settings"]!["image_source"] = JsonSerializer.SerializeToNode(ResultBinding.ForStepResult("example-image", "image"), options);
            if (node["settings"]?["roi"] is JsonObject roi)
            {
                roi["x"]=100; roi["y"]=80; roi["width"]=320; roi["height"]=180;
                if (node["settings"]?["enable_roi"] is not null) node["settings"]!["enable_roi"]=true;
            }
            BindExamples(node, options);
            var step = node.Deserialize<JobStep>(options)!;
            step.Id = "example-step";
            var job = new Job { Name = "Beispiel: " + key, Steps = [new DesktopDuplicationStep { Id = "example-image" },
                new ColorDetectionStep { Id = "example-color", Settings = new() { ImageSource = ResultBinding.ForStepResult("example-image", "image") } },
                new StartProcessStep { Id = "example-process", Settings = new() { ExecutablePath = @"C:\Windows\System32\notepad.exe" } },
                new UserChoiceStep { Id = "example-choice", Settings = new() { Question="Welchen Status anzeigen?",
                    Options=[new() { Label="Abgeschlossen", Value="Bearbeitung abgeschlossen" }, new() { Label="Offen", Value="In Bearbeitung" }] } }, step] };
            // These examples are editor states only; block markers still need a coherent block.
            if (step is IfStep or ElseIfStep or ElseStep or EndIfStep)
            {
                var choice=new UserChoiceStep {Id="example-choice"};
                choice.Settings.Question = "Zweig ausführen?";
                choice.Settings.Options = [new() { Label="Ja", Value="yes" }, new() { Label="Nein", Value="no" }];
                var conditional=new IfStep {Id="example-if"};
                conditional.Settings.Conditions=[new StepCondition {ProviderId=ValueProviderIds.StepResult,
                    SourceId=ResultBinding.ForStepResult(choice.Id,"selected_value").SourceId,
                    Operator=ConditionOperator.Equals,ComparisonValue="yes"}];
                job.Steps=step is IfStep ? new List<JobStep> {choice,step} : new List<JobStep> {choice,conditional,step};
                if (step is IfStep first) first.Settings.Conditions=conditional.Settings.Conditions;
                if (step is not EndIfStep) job.Steps.Add(new EndIfStep());
                if (step is ElseIfStep next) next.Settings.Conditions=conditional.Settings.Conditions;
            }
            using var vm = (JobStepsViewModel)tests.GetType("TaskAutomation.Tests.DesktopAutomationApp.JobStepsViewModelExecutionTests")!
                .GetMethod("CreateRenderViewModel", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [job])!;
            var executor = (IJobExecutor)typeof(JobStepsViewModel)
                .GetField("_jobExecutionContext", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
            var sampleTarget = new Job { Name = "Unterlagen vorbereiten", Id = Guid.Parse("22222222-2222-4222-8222-222222222222"), Steps = [new TimeoutStep()] };
            ((IDictionary<string, Job>)executor.AllJobs)[sampleTarget.Id.ToString()] = sampleTarget;
            var sampleMacro = new Makro { Name = "Notiz eintragen", Id = Guid.Parse("33333333-3333-4333-8333-333333333333"), Befehle = [] };
            ((IDictionary<string, Makro>)executor.AllMakros)[sampleMacro.Id.ToString()] = sampleMacro;
            if (step is JobExecutionStep jobStep) { jobStep.Settings.JobId=sampleTarget.Id; jobStep.Settings.JobName=sampleTarget.Name; }
            if (step is MakroExecutionStep macroStep) { macroStep.Settings.MakroId=sampleMacro.Id; macroStep.Settings.MakroName=sampleMacro.Name; }
            vm.SelectedStep = vm.Steps.First(s => s.Id == step.Id);
            if (vm.SelectedGeneratedEditor is { } editor)
                foreach (var overlayEditor in editor.Fields.Select(f => f.VisualOverlayEditor).Where(o => o is not null))
                    foreach (var row in overlayEditor!.OverlayTextRows)
                    {
                        var text = row.TextSourceField!;
                        if (!text.UseDirectValueCommand.CanExecute(null)) throw new InvalidOperationException("Text fixture cannot select a direct value.");
                        text.UseDirectValueCommand.Execute(null);
                        text.InputText = "Bearbeitung abgeschlossen";
                    }
            ScreenshotCapture.Render(new JobStepsView { DataContext = vm }, id, specs, output);
        }
        foreach (var id in selected.Where(id => id.StartsWith("ref-macro-")))
        {
            var key = id[10..].Replace('-', '_');
            var node = templates["macros"]![key]!.DeepClone(); Sample(node);
            if (key == "key_combination") node["keys"] = new JsonArray("Ctrl", "S");
            var command = node.Deserialize<MakroBefehl>(options)!;
            command.DelayBeforeMicroseconds = 200_000;
            var macro = new Makro { Name = "Befehl bearbeiten", Befehle = [command] };
            using var vm = (MakroStepsViewModel)tests.GetType("TaskAutomation.Tests.DesktopAutomationApp.MakroStepsViewModelGroupingTests")!
                .GetMethod("CreateRenderViewModel", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [macro])!;
            vm.SelectedStep = vm.Steps[0];
            ScreenshotCapture.Render(new MakroStepsView { DataContext = vm }, id, specs, output);
        }
        var target = new Job { Name = "Beispieljob", Id = Guid.Parse("11111111-1111-4111-8111-111111111111"), Steps = [new TimeoutStep()] };
        foreach (var id in selected.Where(id => id.StartsWith("ref-auto-")))
        {
            var key = id[9..].Replace('-', '_');
            var node = templates["automations"]![key]!.DeepClone(); Sample(node);
            var trigger = node.Deserialize<AutomationTrigger>(options)!;
            var definition = new AutomationDefinition { Name = "Auslöser konfigurieren", Active = false, Trigger = trigger,
                Action = new() { JobId = target.Id, Name = target.Name } };
            definition.RunPolicy.EnabledFrom = new TimeOnly(8,0);
            definition.RunPolicy.EnabledUntil = new TimeOnly(18,0);
            var vm = new AutomationDetailViewModel(EditableAutomation.FromDomain(definition),
                new ExtraViews.Automations([definition]), DispatchProxy.Create<IDialogService, UnusedService>(),
                new ExtraViews.JobCatalog([target]), new ExtraViews.Macros(), DispatchProxy.Create<IGlobalHotkeyService, UnusedService>(),
                new WindowsCapabilityCatalog(), NullLogger<AutomationDetailViewModel>.Instance);
            ScreenshotCapture.Render(new AutomationDetailView { DataContext = vm }, id, specs, output);
        }
    }

    private static void BindExamples(JsonNode node, JsonSerializerOptions options)
    {
        if (node is JsonObject obj)
            foreach (var (key, value) in obj.ToArray())
            {
                if (key == "dynamic_roi_source") continue;
                if (value is JsonObject binding && binding.ContainsKey("provider_id"))
                {
                    if (!string.IsNullOrWhiteSpace(binding["provider_id"]?.GetValue<string>())) continue;
                    var result = key switch {
                        "image_source" => ("example-image", "image"),
                        "process_source" => ("example-process", "process"),
                        "bounds_source" or "dynamic_roi_source" => ("example-color", "bounding_box"),
                        "detections_source" => ("example-color", "all_detections"),
                        "padding_source" => ("example-color", "bounding_box.width"),
                        "text_result" => ("example-color", "found"),
                        _ => ("example-color", "point") };
                    obj[key] = JsonSerializer.SerializeToNode(ResultBinding.ForStepResult(result.Item1, result.Item2), options);
                }
                else if (value is JsonObject or JsonArray) BindExamples(value, options);
            }
        else if (node is JsonArray array)
            foreach (var child in array.Where(v => v is not null)) BindExamples(child!, options);
    }

    private static void Sample(JsonNode node)
    {
        if (node is JsonObject obj)
            foreach (var (key, value) in obj.ToArray())
            {
                if (value is JsonObject or JsonArray) Sample(value);
                else if (value is JsonValue scalar && scalar.TryGetValue<string>(out var str) && str == "")
                {
                    var replacement = key switch {
                        "process_name" => "notepad", "window_title_contains" => "Editor", "key" => "Tab",
                        "arguments" => @"C:\Beispiele\Notiz.txt", "working_directory" => @"C:\Beispiele",
                        "script_path" => @"C:\Beispiele\Berechnung.csx", "model" => "Beispielmodell.onnx", "class_name" => "person",
                        "camera_id" => "documentation-camera", "camera_name" => "Beispielkamera", "pixel_format" => "MJPG",
                        "new_name" => "Archiv.txt", "button" => "Left", "title" => "Hinweis", "description" => "Wähle den nächsten Arbeitsschritt.",
                        "text" => "Beispieltext", "script" => "return 42;", "filter" => "*.txt", "template_path" => @"C:\Beispiele\Vorlage.png",
                        "save_path" => @"C:\Beispiele\Ausgabe",
                        "directory_path" or "source_path" => @"C:\Beispiele\Eingang",
                        "target_path" => @"C:\Beispiele\Sicherung", "file_path" or "output_path" => @"C:\Beispiele\Ausgabe.png",
                        "executable_path" => @"C:\Windows\System32\notepad.exe", _ => "" };
                    if (replacement != "") obj[key] = replacement;
                }
            }
        else if (node is JsonArray array)
            foreach (var child in array.Where(v => v is not null)) Sample(child!);
    }
}
