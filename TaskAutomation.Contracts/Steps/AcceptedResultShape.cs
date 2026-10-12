using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed record AcceptedResultShape
{
    public AcceptedResultShape(ResultValueKind ValueKind, params ResultCardinality[] Cardinalities)
        : this(ValueKind, (IReadOnlyList<ResultCardinality>)Cardinalities) { }

    [System.Text.Json.Serialization.JsonConstructor]
    public AcceptedResultShape(ResultValueKind ValueKind, IReadOnlyList<ResultCardinality> Cardinalities)
    {
        this.ValueKind = ValueKind;
        this.Cardinalities = Array.AsReadOnly(Cardinalities.ToArray());
    }
    public ResultValueKind ValueKind { get; }
    public IReadOnlyList<ResultCardinality> Cardinalities { get; }
    public bool Accepts(ResultPropertyDescriptor property) => ValueKind == property.DataType
        && (Cardinalities.Count == 0 || Cardinalities.Contains(property.Cardinality));

    public bool Accepts(ResultValueKind valueKind, ResultCardinality cardinality) =>
        ValueKind == valueKind && (Cardinalities.Count == 0 || Cardinalities.Contains(cardinality));
}

public enum CollectionConsumptionMode { NotApplicable, FirstValue, AllValues }

