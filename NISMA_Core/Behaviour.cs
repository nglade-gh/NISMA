using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NISMA_Core;

/// <summary>
/// Represents a predicate on a field value used in behaviour conditions.
/// </summary>
public class FieldPredicate
{
    /// <summary>Field id this predicate applies to.</summary>
    [JsonPropertyName("first")]
    public int First { get; set; }

    /// <summary>Comparison predicate: {"op": "...", "constant": ...}.</summary>
    [JsonPropertyName("second")]
    public Predicate Second { get; set; } = new Predicate();
}

/// <summary>
/// Comparison predicate with an operator and a constant value.
/// </summary>
public class Predicate
{
    /// <summary>Comparison operator (e.g., "GreaterThan", "Equals").</summary>
    [JsonPropertyName("op")]
    public string Op { get; set; } = string.Empty;

    /// <summary>Constant value to compare against.</summary>
    [JsonPropertyName("constant")]
    public float Constant { get; set; }
}

/// <summary>
/// Predicate on the number of neighbour agents.
/// When <see cref="GrainId"/> is negative, the predicate counts any
/// neighbouring agent (occupied neighbour cells, 8 directions) instead of a
/// specific grain. All fields are written explicitly when the predicate is
/// present in the JSON.
/// </summary>
public class AgentCountPredicate
{
    /// <summary>Grain id to count. Negative = any neighbouring agent.</summary>
    [JsonPropertyName("grainId")]
    public int GrainId { get; set; } = -1;

    /// <summary>Comparison operator (e.g. "GreaterThan", "GreaterThanOrEquals").</summary>
    [JsonPropertyName("op")]
    public string Op { get; set; } = "GreaterThanOrEquals";

    /// <summary>Number of neighbour agents to compare against.</summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    public AgentCountPredicate()
    {
    }

    public AgentCountPredicate(int grainId, string op, int count)
    {
        GrainId = grainId;
        Op = op;
        Count = count;
    }
}

/// <summary>
/// Represents a single reaction rule within a behaviour.
/// </summary>
public class Reaction
{
    /// <summary>Reactive grain id consumed by this reaction. Nullable to allow serialization of 0.</summary>
    [JsonPropertyName("reactiveId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? ReactiveId { get; set; }

    /// <summary>Product grain id produced by this reaction. Nullable to allow serialization of 0.</summary>
    [JsonPropertyName("productId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? ProductId { get; set; }

    /// <summary>Allowed directions for this reaction. Optional.</summary>
    [JsonPropertyName("allowedDirection")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string>? AllowedDirection { get; set; }

    /// <summary>
    /// Age inheritance for this reaction: the product takes its age from that
    /// reactive (0 = main reactive, k = k-th reaction, negative = none).
    /// Optional — written only when not negative, so 0 is preserved.
    /// </summary>
    [JsonPropertyName("sourceReactive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? SourceReactive { get; set; }

    public Reaction()
    {
    }

    public Reaction(int? reactiveId, int? productId)
    {
        ReactiveId = reactiveId;
        ProductId = productId;
    }
}

/// <summary>
/// Represents a behaviour (set of reactions and conditions) in the model.
/// </summary>
public class Behaviour
{
    /// <summary>Display name of the behaviour.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Probability of this behaviour triggering (0-1). Optional.</summary>
    [JsonPropertyName("probability")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float? Probability { get; set; }

    /// <summary>Main reactive grain id for this behaviour. Nullable to allow serialization of 0.</summary>
    [JsonPropertyName("mainReactiveId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? MainReactiveId { get; set; }

    /// <summary>Main product grain id for this behaviour. Nullable to allow serialization of 0.</summary>
    [JsonPropertyName("mainProductId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? MainProductId { get; set; }

    /// <summary>Source reactive grain id (0 = environment, -1 = none). Nullable to allow serialization of 0.</summary>
    [JsonPropertyName("sourceReactive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? SourceReactive { get; set; }

    /// <summary>Age predicate condition for this behaviour. Optional.</summary>
    [JsonPropertyName("agePredicate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Predicate? AgePredicate { get; set; }

    /// <summary>
    /// Optional condition formula (e.g. "count(0)&gt;=3 &amp;&amp; count(0)&lt;5").
    /// Serialized only when not empty.
    /// </summary>
    [JsonPropertyName("formula")]
    public string? Formula { get; set; }

    /// <summary>
    /// Optional imperative actions executed when the behaviour is applied
    /// (e.g. "Add(0, 1, 0), Remove(1, 0, 0), Move(0, 0, 0, 1, 0)").
    /// Serialized only when not empty.
    /// </summary>
    [JsonPropertyName("actions")]
    public string? Actions { get; set; }

    /// <summary>Field predicate conditions for this behaviour. Optional.</summary>
    [JsonPropertyName("fieldPredicates")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<FieldPredicate>? FieldPredicates { get; set; }

    /// <summary>
    /// "Number of neighbours" predicate conditions. Optional — written only
    /// when non-empty. Each predicate writes grainId, op and count.
    /// </summary>
    [JsonPropertyName("agentCountPredicates")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<AgentCountPredicate>? AgentCountPredicates { get; set; }

    /// <summary>Reaction rules for this behaviour.</summary>
    [JsonPropertyName("reaction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<Reaction>? Reaction { get; set; }

    /// <summary>Field influence coefficients: fieldId -> coefficient. Optional.</summary>
    [JsonPropertyName("fieldInfluences")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<int, float>? FieldInfluences { get; set; }

    /// <summary>Description of the behaviour. Optional.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; }

    public Behaviour()
    {
    }

    public Behaviour(string name, int? mainReactiveId, int? mainProductId, int? sourceReactive = null)
    {
        Name = name;
        MainReactiveId = mainReactiveId;
        MainProductId = mainProductId;
        SourceReactive = sourceReactive;
    }
}
