using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NISMA_Core;

/// <summary>
/// Represents a field (diffusion/decay layer) in the simulation environment.
/// </summary>
public class Field
{
    /// <summary>Unique identifier of the field. Optional (default: -1).</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; } = -1;

    /// <summary>Display name of the field.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Color name used in Centyllion.</summary>
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    /// <summary>Whether the field is invisible in the UI. Optional.</summary>
    [JsonPropertyName("invisible")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? Invisible { get; set; }

    /// <summary>Allowed diffusion directions for this field. Optional.</summary>
    [JsonPropertyName("allowedDirection")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string>? AllowedDirection { get; set; }

    /// <summary>Diffusion speed of the field. Optional.</summary>
    [JsonPropertyName("speed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float? Speed { get; set; }

    /// <summary>Half-life of the field (in simulation steps). Optional.</summary>
    [JsonPropertyName("halfLife")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? HalfLife { get; set; }

    /// <summary>
    /// Toric boundary (wrap-around). Default is true (toric). When false the
    /// field has an impermeable boundary: it does not diffuse across the grid
    /// edges (best effort). Optional — written only when false.
    /// </summary>
    [JsonPropertyName("toric")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? Toric { get; set; }

    /// <summary>True when the field boundary is toric (default).</summary>
    [JsonIgnore]
    public bool IsToric => Toric ?? true;

    /// <summary>Formula defining the field value. Optional.</summary>
    [JsonPropertyName("formula")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Formula { get; set; }

    public Field()
    {
    }

    public Field(string name, string color)
    {
        Name = name;
        Color = color;
    }

    public Field(int id, string name, string color) : this(name, color)
    {
        Id = id;
    }

    public static Field FromPhysical(int id, string name, string color, float? speed = null, int? halfLife = null, List<string>? allowedDirection = null)
    {
        return new Field(id, name, color)
        {
            Speed = speed,
            HalfLife = halfLife,
            AllowedDirection = allowedDirection
        };
    }

    public static Field FromFormula(int id, string name, string color, string formula, bool invisible = false)
    {
        return new Field(id, name, color)
        {
            Formula = formula,
            Invisible = invisible
        };
    }
}
