using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using TaskAutomation.Automations;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Makros;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;
using TaskAutomation.WindowsIntegration;

var root = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
var options = new JsonSerializerOptions { WriteIndented = true };
options.Converters.Add(new JsonStringEnumConverter());
var strings = XDocument.Load(Path.Combine(root, "DesktopAutomationApp/Resources/Strings.resx"))
    .Descendants("data").ToDictionary(x => (string)x.Attribute("name")!, x => (string?)x.Element("value") ?? "");
string Label(string key) => strings.GetValueOrDefault(key, key);
object Field(PropertyInfo p, object? instance, string path) => new
{
    id = path + (p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name),
    name = p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name,
    type = (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType).Name,
    options = (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType) is { IsEnum: true } e ? Enum.GetNames(e) : [],
    defaultValue = instance is null ? null : SafeValue(p, instance)
};
object? SafeValue(PropertyInfo p, object instance)
{
    try
    {
        var value = p.GetValue(instance);
        if (p.Name is "Id" or "Secret" || value is Guid or DateTimeOffset or DateTime) return null;
        return value is null || value is string || value.GetType().IsValueType ? value : null;
    }
    catch (TargetInvocationException) { return null; }
}
List<object> Schema(Type type, object? instance = null, string path = "", HashSet<Type>? parents = null)
{
    parents = parents is null ? [] : new(parents);
    if (!parents.Add(type)) return [];
    var fields = new List<object>();
    foreach (var p in type.GetProperties().Where(p => p.GetIndexParameters().Length == 0 && p.GetMethod is not null
        && p.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition != JsonIgnoreCondition.Always).OrderBy(p => p.Name))
    {
        fields.Add(Field(p, instance, path));
        var child = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
        if (child != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(child))
            child = child.IsArray ? child.GetElementType()! : child.GetGenericArguments().LastOrDefault() ?? child;
        if (child.Namespace?.StartsWith("TaskAutomation", StringComparison.Ordinal) == true && !child.IsEnum)
        {
            var name = p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name;
            object? value = null;
            try { value = instance is null ? null : p.GetValue(instance); } catch (TargetInvocationException) { }
            fields.AddRange(Schema(child, value?.GetType() == child ? value : null, path + name + ".", parents));
        }
    }
    return fields;
}
ResultTypeDescriptor? ResultFor(IStepDefinition d)
{
    var step = d.CreateDefault();
    step.Id = "documentation-" + d.Descriptor.TypeId;
    if (step is UserChoiceStep choice)
        for (var i = 0; i < choice.Settings.Options.Count; i++) choice.Settings.Options[i].Id = "example-option-" + i;
    return StepResultContractRegistry.Resolve(step);
}
var steps = BuiltInStepDefinitions.Instance.Definitions.OrderBy(d => d.Descriptor.TypeId).Select(d => new
{
    id = d.Descriptor.TypeId,
    name = Label(d.Descriptor.DisplayNameKey),
    description = Label(d.Descriptor.DescriptionKey),
    category = d.Descriptor.CategoryId,
    uiFieldIds = d.Descriptor.Presentation.EditorSections.SelectMany(section => section.FieldIds).Distinct().ToArray(),
    fields = d.Descriptor.Fields.Select(f => new { id = f.Id, name = Label(f.LabelKey), descriptor = f }),
    schema = Schema(d.StepType, d.CreateDefault()),
    inputs = StepInputContractRegistry.Get(d.StepType),
    result = ResultFor(d)
}).ToArray();
string DerivedName(Type type)
{
    var instance = Activator.CreateInstance(type);
    var key = instance is AutomationTrigger trigger ? "Enum.AutomationTriggerKind." + trigger.Kind
        : instance is MakroBefehl command ? "Macro.Step.Type." + MakroCommandRules.TypeId(command) : type.Name;
    return strings.GetValueOrDefault(key, type.Name);
}
object[] Derived(Type parent) => parent.GetCustomAttributes<JsonDerivedTypeAttribute>().OrderBy(a => a.TypeDiscriminator?.ToString())
    .Select(a => (object)new { id = a.TypeDiscriminator?.ToString() ?? a.DerivedType.Name, name = DerivedName(a.DerivedType),
        fields = Schema(a.DerivedType, Activator.CreateInstance(a.DerivedType)) }).ToArray();
var automationParts = new[] { typeof(AutomationAction), typeof(AutomationRunPolicy) }.Select(t => new
    { id = t.Name, name = t.Name, fields = Schema(t, Activator.CreateInstance(t)) });
var logTypes = new[] { typeof(LogEvent), typeof(LogContext), typeof(LogRun), typeof(LogStepSnapshot), typeof(LogTriggerSnapshot),
    typeof(LogOutcome), typeof(StepLogOutcome), typeof(ExecutionLogLevel), typeof(LogReadState), typeof(LogSource), typeof(LogArea),
    typeof(LogQuery), typeof(LogPage), typeof(LogStepExecution), typeof(LogAction), typeof(LogDiagnostic),
    typeof(AutomationTriggerContext), typeof(ExecutionLogKind) };
var metadata = new
{
    formatVersion = 1,
    appVersion = XDocument.Load(Path.Combine(root, "DesktopAutomationApp/DesktopAutomationApp.csproj")).Descendants("Version").First().Value,
    releaseChannel = "working-tree",
    steps,
    automations = Derived(typeof(AutomationTrigger)).Concat(automationParts.Cast<object>()).ToArray(),
    macros = Derived(typeof(MakroBefehl)).Concat(new object[] { new { id = "recording", name = "Aufnahme und Gruppen", fields = Schema(typeof(Makro), new Makro()) } }),
    logs = logTypes.Select(t => new { id = t.Name, name = t.Name,
        options = t.IsEnum ? Enum.GetNames(t) : [], fields = t.IsEnum ? [] : Schema(t) }),
    logCodes = typeof(LogCodes).GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy(f => f.Name)
        .Select(f => new { id = f.GetRawConstantValue()?.ToString(), name = f.Name }),
    resultTypes = StepResultMetadata.ResultTypes,
    windowsCapabilities = new WindowsCapabilityCatalog().Capabilities.OrderBy(c => c.Id).Select(c => new { descriptor = c, result = WindowsQueryResultRegistry.GetContract(c.Id) }),
    sources = SourceFiles(root).ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'),
        p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))).ToLowerInvariant())
};
File.WriteAllText(Path.Combine(output, "metadata.json"), JsonSerializer.Serialize(metadata, options) + "\n");
Examples.Export(output);
Documentation.Export(output);
Console.WriteLine($"Exported {steps.Length} steps and canonical automation, macro and log contracts.");

static IEnumerable<string> SourceFiles(string root)
{
    foreach (var directory in new[] { "TaskAutomation/Steps", "TaskAutomation/Jobs", "TaskAutomation/Automations", "TaskAutomation/Makros", "TaskAutomation/Logging", "DesktopAutomation.Application/Logging", "TaskAutomation/WindowsIntegration", "TaskAutomation/Orchestration", "TaskAutomation/Scripts", "TaskAutomation.Contracts/Steps", "TaskAutomation.Contracts/Logging", "DesktopAutomationApp/ViewModels" })
        foreach (var path in Directory.GetFiles(Path.Combine(root, directory), "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal)) yield return path;
    foreach (var path in new[] { "DesktopAutomationApp/Resources/Strings.resx", "DesktopAutomationApp/Resources/Strings.en.resx", "DesktopAutomationApp/DesktopAutomationApp.csproj" }) yield return Path.Combine(root, path);
}
