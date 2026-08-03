using System;
using System.ComponentModel;
using System.IO;

namespace TaskAutomation.Jobs;

public enum StepErrorKind
{
    Unexpected,
    FileNotFound,
    DirectoryNotFound,
    AccessDenied,
    TimedOut,
    InvalidConfiguration,
    InputOutput,
    ExternalProgram
}

public static class StepErrorClassification
{
    public static StepErrorKind Classify(Exception exception)
    {
        var current = Unwrap(exception);
        return current switch
        {
            FileNotFoundException => StepErrorKind.FileNotFound,
            DirectoryNotFoundException => StepErrorKind.DirectoryNotFound,
            UnauthorizedAccessException => StepErrorKind.AccessDenied,
            TimeoutException => StepErrorKind.TimedOut,
            ArgumentException or FormatException => StepErrorKind.InvalidConfiguration,
            Win32Exception => StepErrorKind.ExternalProgram,
            IOException => StepErrorKind.InputOutput,
            _ => StepErrorKind.Unexpected
        };
    }

    public static string GetErrorCode(StepErrorKind kind) => kind switch
    {
        StepErrorKind.FileNotFound => "STEP_FILE_NOT_FOUND",
        StepErrorKind.DirectoryNotFound => "STEP_DIRECTORY_NOT_FOUND",
        StepErrorKind.AccessDenied => "STEP_ACCESS_DENIED",
        StepErrorKind.TimedOut => "STEP_TIMED_OUT",
        StepErrorKind.InvalidConfiguration => "STEP_INVALID_CONFIGURATION",
        StepErrorKind.InputOutput => "STEP_IO_ERROR",
        StepErrorKind.ExternalProgram => "STEP_EXTERNAL_PROGRAM_ERROR",
        _ => "STEP_UNEXPECTED_ERROR"
    };

    private static Exception Unwrap(Exception exception)
    {
        while (exception is AggregateException { InnerExceptions.Count: 1 } aggregate)
            exception = aggregate.InnerExceptions[0];
        return exception;
    }
}
