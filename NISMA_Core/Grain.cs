using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NISMA_Core;

/// <summary>
/// Represents a grain type in the NetBioDyn model.
/// </summary>
public class Grain
{
    /// <summary>Unique identifier of the grain type.</summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }

    /// <summary>Display name of the grain.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Color name used in Centyllion (e.g., "Red", "Blue").</summary>
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    /// <summary>Icon shape for the grain (e.g., "square", "circle"). Optional.</summary>
    [JsonPropertyName("icon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Icon { get; set; }

    /// <summary>Relative size of the grain icon. Optional.</summary>
    [JsonPropertyName("size")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float? Size { get; set; }

    /// <summary>Half-life for the grain (used in field calculations). Optional.</summary>
    [JsonPropertyName("halfLife")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? HalfLife { get; set; }

    /// <summary>Probability of movement for this grain (0-1). Optional.</summary>
    [JsonPropertyName("movementProbability")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float? MovementProbability { get; set; }

    /// <summary>Allowed movement directions for this grain. Optional.</summary>
    [JsonPropertyName("allowedDirection")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string>? AllowedDirection { get; set; }

    /// <summary>Field production rates: fieldId -> amount. Optional.</summary>
    [JsonPropertyName("fieldProductions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<int, float> FieldProductions { get; set; } = new Dictionary<int, float>();

    /// <summary>Field influence coefficients: fieldId -> coefficient. Optional.</summary>
    [JsonPropertyName("fieldInfluences")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<int, float> FieldInfluences { get; set; } = new Dictionary<int, float>();

    /// <summary>Field permeability coefficients: fieldId -> coefficient. Optional.</summary>
    [JsonPropertyName("fieldPermeable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<int, float>? FieldPermeable { get; set; }

    /// <summary>Description of the grain. Optional.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; }

    public Grain()
    {
    }

    public Grain(int id, string name, string color)
    {
        Id = id;
        Name = name;
        Color = color;
    }
}
