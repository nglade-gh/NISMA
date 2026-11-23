using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NISMA_Core;

/// <summary>
/// Simulation modifier: a condition evaluated once per step, after the
/// behaviours and outside any cell, plus meta-actions executed when the
/// condition holds (zones, pause, activation of behaviours and modifiers).
/// </summary>
public class SimulationModifier
{
    /// <summary>Display name of the modifier.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of the modifier. Optional.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; }

    /// <summary>
    /// Condition formula, evaluated once per step without any current cell:
    /// step, agent(x, y), age(x, y), count(id, x, y) and field(id, x, y) are
    /// available. Optional — the key is removed when empty.
    /// </summary>
    [JsonPropertyName("formula")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Formula { get; set; }

    /// <summary>
    /// Meta-actions executed once per step when the condition holds, e.g.
    /// "AddRect(1, 50, 10, 10, 20, 20), Unuse(0)". Optional — removed when empty.
    /// </summary>
    [JsonPropertyName("actions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Actions { get; set; }

    /// <summary>Probability that the modifier runs, drawn once per step. Optional.</summary>
    [JsonPropertyName("probability")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? Probability { get; set; }

    /// <summary>
    /// Activation state of the modifier. Optional — written only when false.
    /// A deactivated modifier never runs, its radio is off.
    /// </summary>
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? Enabled { get; set; }

    /// <summary>True when the modifier is active (default).</summary>
    [JsonIgnore]
    public bool IsEnabled => Enabled ?? true;

    /// <summary>
    /// Identifier of the modifier, target of Use(id) / Unuse(id).
    /// Optional — omitted when 0.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Id { get; set; }

    public SimulationModifier()
    {
    }

    public SimulationModifier(string name, string? formula = null, string? actions = null)
    {
        Name = name;
        Formula = formula;
        Actions = actions;
    }
}
