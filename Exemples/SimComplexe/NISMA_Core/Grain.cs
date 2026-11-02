using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using nbd.sub_classes;

using System.Collections.Generic;
namespace nbd;

[Serializable, XmlRoot("grains")]
[JsonSerializable(typeof(Grain))]
[JsonSerializable(typeof(List<Grain>))]
public class Grain
{
    [XmlElement("id")][JsonPropertyName("id")] public int Id { get; set; } // >= 0
    [XmlElement("name")][JsonPropertyName("name")] public string Name { get; set; } = "";
    [XmlElement("color")][JsonPropertyName("color")] public string Color { get; set; } = "red";
    [XmlElement("icon")][JsonPropertyName("icon")] public string Icon { get; set; } = "square-full";
    [XmlElement("invisible")][JsonPropertyName("invisible")] public bool Invisible { get; set; }
    [XmlElement("size")][JsonPropertyName("size")] public double Size { get; set; } = 1.0;
    [XmlElement("description")][JsonPropertyName("description")] public string Description { get; set; } = "";
    [XmlElement("halfLife")][JsonPropertyName("halfLife")] public int HalfLife { get; set; }
    [XmlElement("movementProbability")][JsonPropertyName("movementProbability")]
    public double MovementProbability
    {
        get => _movementProbability;
        set => _movementProbability = Math.Clamp(value, 0.0, 1.0);
    }

    // Directions autorisées (singulier, aligné avec centyllion)
    [XmlElement("allowedDirection")][JsonPropertyName("allowedDirection")]
    public List<string> AllowedDirection { get; set; } = new() { "Left", "Up", "Right", "Down" };

    // Paramètres liés aux champs
    [XmlElement("fieldProductions")][JsonPropertyName("fieldProductions")]
    public Dictionary<int, float> FieldProductions { get; set; } = new();
    [XmlElement("fieldInfluences")][JsonPropertyName("fieldInfluences")]
    public Dictionary<int, float> FieldInfluences { get; set; } = new();
    [XmlElement("fieldPermeable")][JsonPropertyName("fieldPermeable")]
    public Dictionary<int, float> FieldPermeable { get; set; } = new();

    private double _movementProbability;

    /*********************************************CONSTRUCTOR ZONE*****************************************************/
    public Grain() { }

    public Grain(int id, string name, string color)
    {
        Id = id;
        Name = name;
        Color = color;
    }

    public Grain(int id, string name, string color, int halfLife)
    {
        Id = id;
        Name = name;
        Color = color;
        HalfLife = halfLife;
    }

    public Grain(int id, string name, string color, int halfLife, string icon)
    {
        Id = id;
        Name = name;
        Color = color;
        HalfLife = halfLife;
        Icon = icon;
    }

    public Grain(int id, string name, string color, int halfLife, string icon, double movementProbability)
    {
        Id = id;
        Name = name;
        Color = color;
        HalfLife = halfLife;
        Icon = icon;
        MovementProbability = movementProbability;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
