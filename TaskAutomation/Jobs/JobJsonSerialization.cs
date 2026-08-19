using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace TaskAutomation.Jobs;

/// <summary>Writes reference-backed steps without duplicating literal settings into job files.</summary>
public static class JobJsonSerialization
{
    public static void Configure(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            if (!typeof(JobStep).IsAssignableFrom(typeInfo.Type)) return;
            foreach (var property in typeInfo.Properties.Where(property =>
                         string.Equals(property.Name, "settings", StringComparison.OrdinalIgnoreCase)))
                property.ShouldSerialize = (owner, _) => owner is not JobStep step || step.Inputs.Count == 0;
        });
        options.TypeInfoResolver = resolver;
    }
}
