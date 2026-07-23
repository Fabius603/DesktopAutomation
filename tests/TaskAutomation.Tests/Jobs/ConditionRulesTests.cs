using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.Jobs;

public sealed class ConditionRulesTests
{
    [Theory]
    [InlineData(ResultValueKind.Boolean, ConditionOperator.IsTrue, true)]
    [InlineData(ResultValueKind.Boolean, ConditionOperator.Contains, false)]
    [InlineData(ResultValueKind.Text, ConditionOperator.Contains, true)]
    [InlineData(ResultValueKind.Text, ConditionOperator.GreaterThan, false)]
    [InlineData(ResultValueKind.Integer, ConditionOperator.GreaterThan, true)]
    [InlineData(ResultValueKind.Enum, ConditionOperator.Equals, true)]
    [InlineData(ResultValueKind.Enum, ConditionOperator.LessThan, false)]
    public void IsOperatorAllowed_ReturnsExpected(ResultValueKind type, ConditionOperator op, bool expected) =>
        Assert.Equal(expected, ConditionRules.IsOperatorAllowed(type, op));

    [Theory]
    [InlineData(ConditionOperator.IsTrue, false)]
    [InlineData(ConditionOperator.IsFalse, false)]
    [InlineData(ConditionOperator.IsEmpty, false)]
    [InlineData(ConditionOperator.Equals, true)]
    [InlineData(ConditionOperator.GreaterThan, true)]
    public void RequiresComparisonValue_ReturnsExpected(ConditionOperator op, bool expected) =>
        Assert.Equal(expected, ConditionRules.RequiresComparisonValue(op));

    [Theory]
    [InlineData(ResultValueKind.Number, "1.25", true)]
    [InlineData(ResultValueKind.Number, "1,25", false)]
    [InlineData(ResultValueKind.Integer, "42", true)]
    [InlineData(ResultValueKind.Integer, "42.5", false)]
    [InlineData(ResultValueKind.Boolean, "true", true)]
    [InlineData(ResultValueKind.Boolean, "yes", false)]
    public void TryParseComparison_UsesStableInvariantFormats(ResultValueKind type, string text, bool expected)
    {
        var property = new ResultPropertyDescriptor("Value", "Value", type);
        Assert.Equal(expected, StepResultMetadata.TryParseComparison(property, text, out _));
    }

    [Fact]
    public void TryParseComparison_Enum_IsCaseInsensitiveButRejectsUnknown()
    {
        var property = new ResultPropertyDescriptor("State", "State", ResultValueKind.Enum,
            EnumTypeName: typeof(WindowsOnOffState).FullName, EnumValues: Enum.GetNames<WindowsOnOffState>());
        Assert.True(StepResultMetadata.TryParseComparison(property, "on", out var value));
        Assert.Equal("On", value);
        Assert.False(StepResultMetadata.TryParseComparison(property, "invalid", out _));
    }

    [Fact]
    public void FormatComparisonValue_DateTime_NormalizesToUtcRoundTripFormat()
    {
        var property = new ResultPropertyDescriptor("At", "At", ResultValueKind.DateTime);
        var value = new DateTime(2026, 4, 5, 10, 30, 0, DateTimeKind.Local);
        var formatted = ConditionRules.FormatComparisonValue(property, value);
        Assert.EndsWith("Z", formatted);
        Assert.True(StepResultMetadata.TryParseComparison(property, formatted, out _));
    }
}
