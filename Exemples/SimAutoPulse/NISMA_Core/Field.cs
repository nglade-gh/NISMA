using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace autopulse;

[Serializable, XmlRoot("fields")]
[JsonSerializable(typeof(Field))]
[JsonSerializable(typeof(List<Field>))]
public class Field
{
    [XmlElement("id")][JsonPropertyName("id")] public int Id { get; set; }
    [XmlElement("name")][JsonPropertyName("name")] public string Name { get; set; } = "";
    [XmlElement("color")][JsonPropertyName("color")] public string Color { get; set; } = "SkyBlue";
    [XmlElement("invisible")][JsonPropertyName("invisible")] public bool Invisible { get; set; }

    [XmlElement("allowedDirection")][JsonPropertyName("allowedDirection")]
    public List<string> AllowedDirection { get; set; } = new() { "Left", "Up", "Right", "Down" };

    [XmlElement("speed")][JsonPropertyName("speed")] public float Speed { get; set; }
    [XmlElement("halfLife")][JsonPropertyName("halfLife")] public int HalfLife { get; set; }

    [XmlElement("formula")][JsonPropertyName("formula")] public string Formula { get; set; } = "";

    public Field() { }

    public static Field FromPhysical(int id, string name, string color, float speed, int halfLife) => new()
    {
        Id = id, Name = name, Color = color, Speed = speed, HalfLife = halfLife
    };

    public static Field FromFormula(int id, string name, string color, string formula) => new()
    {
        Id = id, Name = name, Color = color, Formula = formula
    };
}
