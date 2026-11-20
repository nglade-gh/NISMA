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

    /// <summary>True when the grain is not drawn in the simulation view. Optional.</summary>
    [JsonPropertyName("invisible")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? Invisible { get; set; }

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

    /// <summary>
    /// Per-wall boundary conditions for this grain's agents. Optional.
    /// Written only when the grain is not toric everywhere; when written,
    /// all four walls are explicit.
    /// </summary>
    [JsonPropertyName("boundaryConditions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public BoundaryConditions? BoundaryConditions { get; set; }

    /// <summary>Effective boundary conditions (toric everywhere when unset).</summary>
    [JsonIgnore]
    public BoundaryConditions EffectiveBoundaryConditions => BoundaryConditions ?? new BoundaryConditions();

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

/// <summary>
/// Per-wall authorisation for a grain's agents to cross the grid walls
/// (toric wrap-around). Each flag authorises crossing the corresponding
/// wall: Left / Right for the vertical walls, Top / Bottom for the
/// horizontal walls. Default (all true) is toric in all directions.
/// When the object is present in the JSON, all four walls are written
/// explicitly; when a grain is toric everywhere the parent omits the
/// object entirely.
/// </summary>
public class BoundaryConditions
{
    /// <summary>Authorises crossing the left wall (moving left at x = 0).</summary>
    [JsonPropertyName("left")]
    public bool Left { get; set; } = true;

    /// <summary>Authorises crossing the right wall (moving right at x = width-1).</summary>
    [JsonPropertyName("right")]
    public bool Right { get; set; } = true;

    /// <summary>Authorises crossing the top wall (moving up at y = 0).</summary>
    [JsonPropertyName("top")]
    public bool Top { get; set; } = true;

    /// <summary>Authorises crossing the bottom wall (moving down at y = height-1).</summary>
    [JsonPropertyName("bottom")]
    public bool Bottom { get; set; } = true;

    public BoundaryConditions()
    {
    }

    public BoundaryConditions(bool left, bool right, bool top, bool bottom)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }

    /// <summary>True when all walls are open (toric everywhere).</summary>
    [JsonIgnore]
    public bool IsToric => Left && Right && Top && Bottom;
}
