namespace TaskAutomation.Steps;

/// <summary>A valid optional source was skipped in this iteration.</summary>
internal sealed class StepInputUnavailableException(string inputId) : Exception(inputId);
