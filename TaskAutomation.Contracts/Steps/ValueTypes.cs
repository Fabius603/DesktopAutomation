namespace TaskAutomation.Jobs;

public enum ResultValueKind
{
    Boolean, Integer, Number, Text, DateTime, Image, Point, Rectangle,
    Detection, ProcessReference, ResultObject, Enum, JobReference, MacroReference,
    Color, FilePath
}

public enum ResultCardinality { Single, OptionalSingle, Collection }
public enum MissingValuePolicy { FailStep, SkipStep, UseDefault }

public static class ValueProviderIds
{
    public const string LocalValue = "local_value";
    public const string JobVariable = "job_variable";
    public const string StepResult = "step_result";
    public const string Secret = "secret";
}

