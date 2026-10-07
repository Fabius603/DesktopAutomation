namespace TaskAutomation.Tests.Localization;

// Changing the application language affects the singleton and default thread cultures.
// These tests must not overlap with any other collection, including localization readers.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApplicationLocalizationCollection
{
    public const string Name = "Application localization";
}
