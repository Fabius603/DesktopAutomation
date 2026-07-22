using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.Jobs;

public sealed class ConditionRulesTests
{
    [Theory]
    [InlineData(ResultPropertyType.Bool, ConditionOperator.IsTrue, true)]
    [InlineData(ResultPropertyType.Bool, ConditionOperator.Contains, false)]
    [InlineData(ResultPropertyType.String, ConditionOperator.Contains, true)]
    [InlineData(ResultPropertyType.String, ConditionOperator.GreaterThan, false)]
    [InlineData(ResultPropertyType.Integer, ConditionOperator.GreaterThan, true)]
    [InlineData(ResultPropertyType.Enum, ConditionOperator.Equals, true)]
    [InlineData(ResultPropertyType.Enum, ConditionOperator.LessThan, false)]
    public void IsOperatorAllowed_ReturnsExpected(ResultPropertyType type, ConditionOperator op, bool expected) =>
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
    [InlineData(ResultPropertyType.Double, "1.25", true)]
    [InlineData(ResultPropertyType.Double, "1,25", false)]
    [InlineData(ResultPropertyType.Integer, "42", true)]
    [InlineData(ResultPropertyType.Integer, "42.5", false)]
    [InlineData(ResultPropertyType.Bool, "true", true)]
    [InlineData(ResultPropertyType.Bool, "yes", false)]
    public void TryParseComparison_UsesStableInvariantFormats(ResultPropertyType type, string text, bool expected)
    {
        var property = new ResultPropertyDescriptor("Value", "Value", type);
        Assert.Equal(expected, StepResultMetadata.TryParseComparison(property, text, out _));
    }

    [Fact]
    public void TryParseComparison_Enum_IsCaseInsensitiveButRejectsUnknown()
    {
        var property = new ResultPropertyDescriptor("State", "State", ResultPropertyType.Enum,
            EnumTypeName: typeof(WindowsOnOffState).FullName, EnumValues: Enum.GetNames<WindowsOnOffState>());
        Assert.True(StepResultMetadata.TryParseComparison(property, "on", out var value));
        Assert.Equal("On", value);
        Assert.False(StepResultMetadata.TryParseComparison(property, "invalid", out _));
    }

    [Fact]
    public void FormatComparisonValue_DateTime_NormalizesToUtcRoundTripFormat()
    {
        var property = new ResultPropertyDescriptor("At", "At", ResultPropertyType.DateTime);
        var value = new DateTime(2026, 4, 5, 10, 30, 0, DateTimeKind.Local);
        var formatted = ConditionRules.FormatComparisonValue(property, value);
        Assert.EndsWith("Z", formatted);
        Assert.True(StepResultMetadata.TryParseComparison(property, formatted, out _));
    }
}
