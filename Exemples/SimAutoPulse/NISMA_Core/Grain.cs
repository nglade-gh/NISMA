using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using autopulse.sub_classes;

namespace autopulse;

[Serializable, XmlRoot("grains")]
[JsonSerializable(typeof(Grain))]
[JsonSerializable(typeof(List<Grain>))]
public class Grain
{
    [XmlElement("id")][JsonPropertyName("id")] public int Id { get; set; }
    [XmlElement("name")][JsonPropertyName("name")] public string Name { get; set; } = "";
    [XmlElement("color")][JsonPropertyName("color")] public string Color { get; set; } = "red";
    [XmlElement("icon")][JsonPropertyName("icon")] public string? Icon { get; set; }
    [XmlElement("size")][JsonPropertyName("size")] public double Size { get; set; } = 1.0;
    [XmlElement("description")][JsonPropertyName("description")] public string Description { get; set; } = "";
    [XmlElement("halfLife")][JsonPropertyName("halfLife")] public int HalfLife { get; set; }
    [XmlElement("movementProbability")][JsonPropertyName("movementProbability")]
    public double MovementProbability { get; set; } = 0.0;

    [XmlElement("allowedDirection")][JsonPropertyName("allowedDirection")]
    public List<string> AllowedDirection { get; set; } = new() { "Left", "Up", "Right", "Down" };

    [XmlElement("fieldProductions")][JsonPropertyName("fieldProductions")]
    public Dictionary<int, float> FieldProductions { get; set; } = new();

    [XmlElement("fieldInfluences")][JsonPropertyName("fieldInfluences")]
    public Dictionary<int, float> FieldInfluences { get; set; } = new();

    [XmlElement("fieldPermeable")][JsonPropertyName("fieldPermeable")]
    public Dictionary<int, float> FieldPermeable { get; set; } = new();

    public Grain() { }

    public Grain(int id, string name, string color, double size = 1.0)
    {
        Id = id;
        Name = name;
        Color = color;
        Size = size;
    }
}
