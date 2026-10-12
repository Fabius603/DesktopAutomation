using System.Text.Json.Nodes;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Jobs;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class TextComparisonSourceTests
{
    [Fact]
    public void TextComparison_OffersAndReopensANumericCollectionSource()
    {
        var text = new JobVariable { Name = "Text", ValueKind = ResultValueKind.Text, Value = JsonValue.Create("1\r\n2") };
        var numbers = new JobVariable
        {
            Name = "Numbers",
            ValueKind = ResultValueKind.Integer,
            Cardinality = ResultCardinality.Collection,
            Value = JsonNode.Parse("[1,2]")
        };
        var row = new ConditionRowViewModel([], [], variables: [text, numbers]);
        row.LoadFrom(new StepCondition
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = text.Id.ToString("D"),
            Operator = ConditionOperator.Equals,
            Comparison = new()
            {
                Kind = ComparisonOperandKind.JobResult,
                ProviderId = ValueProviderIds.JobVariable,
                SourceId = numbers.Id.ToString("D")
            }
        });
        Assert.True(row.IsValid);
        Assert.Contains(row.ComparisonSelectionTree.SelectMany(node => node.Children), node => node.DisplayName == "Numbers");
        Assert.Equal(numbers.Id.ToString("D"), row.ToCondition().Comparison!.SourceId);
    }
}
