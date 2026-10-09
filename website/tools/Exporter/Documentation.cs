using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TaskAutomation.Automations;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Makros;
using TaskAutomation.Steps.Definitions;

internal static class Documentation
{
    public static void Export(string output)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        JobJsonSerialization.Configure(options);
        JsonNode Template(object value, Type type, string id)
        {
            var node = JsonSerializer.SerializeToNode(value, type, options)!;
            Normalize(node, id);
            return node;
        }
        var steps = BuiltInStepDefinitions.Instance.Definitions.OrderBy(d => d.Descriptor.TypeId)
            .ToDictionary(d => d.Descriptor.TypeId, d => Template(d.CreateDefault(), typeof(JobStep), d.Descriptor.TypeId));
        Dictionary<string, JsonNode> Derived(Type type) => type.GetCustomAttributes<JsonDerivedTypeAttribute>()
            .OrderBy(a => a.TypeDiscriminator!.ToString()).ToDictionary(a => a.TypeDiscriminator!.ToString()!,
                a => Template(Activator.CreateInstance(a.DerivedType)!, type, a.TypeDiscriminator!.ToString()!));
        var contractOptions = new JsonSerializerOptions { WriteIndented = true };
        contractOptions.Converters.Add(new JsonStringEnumConverter());
        var bindingSchemas = typeof(ValueBindingSchemaRegistry).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue()!)
            .Order().Select(id => ValueBindingSchemaRegistry.TryGet(id, out var schema) ? schema : null).ToArray();
        var bindingExample = new Job { Id = Guid.Parse("11111111-1111-4111-8111-111111111111"), Name = "Werteverbindung: 250 ms warten",
            Steps = [new TimeoutStep { Id = "warten", Settings = new() { DelayMs = 250 } }] };
        JobVariableInputMigration.Migrate(bindingExample);
        Examples.Stabilize(bindingExample, "documentation-binding");
        var bindingJson = JsonSerializer.SerializeToNode(bindingExample, options)!;
        if (!JobValidation.ValidateJob(bindingJson.Deserialize<Job>(options)!).IsValid)
            throw new InvalidOperationException("Documentation value binding example is invalid.");
        var logExample = new LogEvent { Id = Guid.Parse("22222222-2222-4222-8222-222222222222"), Sequence = 2,
            Timestamp = new DateTimeOffset(2030, 1, 1, 8, 0, 1, TimeSpan.Zero), Source = LogSource.Job,
            SourceId = bindingExample.Id, SourceName = bindingExample.Name, Area = "Execution", Category = LogArea.Execution,
            Code = LogCodes.StepCompleted, Message = "Beispiel: Warte-Step beendet.", Phase = "Main", Iteration = 1, DurationMs = 250,
            Context = new(RunId: Guid.Parse("33333333-3333-4333-8333-333333333333"), StepId: "warten",
                StepExecutionId: Guid.Parse("44444444-4444-4444-8444-444444444444")) };
        var document = new
        {
            formatVersions = new { job = Job.CurrentFormatVersion, macro = Makro.CurrentFormatVersion, automation = AutomationDefinition.CurrentFormatVersion },
            examples = new { valueBindingJob = bindingJson, logEvent = JsonSerializer.SerializeToNode(logExample, LogRepository.JsonOptions) },
            bindingSchemas = JsonSerializer.SerializeToNode(bindingSchemas, contractOptions),
            fieldSchemas = BuiltInStepDefinitions.Instance.Definitions.ToDictionary(d => d.Descriptor.TypeId,
                d => d.Descriptor.Fields.Where(f => ValueBindingSchemaRegistry.ForField(f) is not null)
                    .ToDictionary(f => f.Id, f => ValueBindingSchemaRegistry.ForField(f))),
            templates = new { steps, automations = Derived(typeof(AutomationTrigger)), macros = Derived(typeof(MakroBefehl)),
                job = Template(new Job { Name = "Mein Job" }, typeof(Job), "job"),
                macro = Template(new Makro { Name = "Mein Makro" }, typeof(Makro), "macro"),
                automation = Template(new AutomationDefinition { Name = "Meine Automation", Active = false }, typeof(AutomationDefinition), "automation") }
        };
        File.WriteAllText(Path.Combine(output, "authoring.json"), JsonSerializer.Serialize(document, options) + "\n");
    }

    private static void Normalize(JsonNode node, string path)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(pair => pair.Key).ToArray())
            {
                if (obj[key] is JsonValue value && value.TryGetValue<string>(out var text))
                {
                    if (key == "secret") obj[key] = new string('D', 48);
                    else if (key is "created_at" or "updated_at" or "run_at" or "recordedAtUtc") obj[key] = "2030-01-01T08:00:00+00:00";
                    else if (key is "id" or "hook_id") obj[key] = Guid.TryParse(text, out _)
                        ? new Guid(MD5.HashData(Encoding.UTF8.GetBytes(path + "/" + key))).ToString()
                        : "example-" + path.Replace('/', '-');
                }
                if (obj[key] is JsonObject or JsonArray) Normalize(obj[key]!, path + "/" + key);
            }
        }
        else if (node is JsonArray array)
            for (var i = 0; i < array.Count; i++) if (array[i] is { } child) Normalize(child, path + "/" + i);
    }
}
