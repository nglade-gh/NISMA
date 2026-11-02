using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using nbd.sub_classes;

using System.Collections.Generic;
namespace nbd;

[Serializable, XmlRoot("fields")]
[JsonSerializable(typeof(Field))]
[JsonSerializable(typeof(List<Field>))]
public class Field
{
    // Paramètres principaux
    [XmlElement("id")][JsonPropertyName("id")] public int Id { get; set; } = 0;
    [XmlElement("name")][JsonPropertyName("name")] public string Name { get; set; } = "";
    [XmlElement("color")][JsonPropertyName("color")] public string Color { get; set; } = "SkyBlue";
    [XmlElement("invisible")][JsonPropertyName("invisible")] public bool Invisible { get; set; }

    // Directions autorisées (singulier, aligné avec centyllion)
    [XmlElement("allowedDirection")][JsonPropertyName("allowedDirection")]
    public List<string> AllowedDirection { get; set; } = new() { "Left", "Up", "Right", "Down" };

    // Paramètres physiques
    [XmlElement("speed")][JsonPropertyName("speed")]
    public float Speed { get; set; } = 0.8f;
    [XmlElement("halfLife")][JsonPropertyName("halfLife")] public int HalfLife { get; set; } = 10;

    // Formule (évaluée dynamiquement pendant la simulation)
    [XmlElement("formula")][JsonPropertyName("formula")] public string Formula { get; set; } = "";

    /****************************construction zone*****************************************/
    public Field() { }

    public Field(int id, string name, string color)
    {
        Id = id;
        Name = name;
        Color = color;
        AllowedDirection = new() { "Left", "Up", "Right", "Down" };
        }


    public Field(int id, string name, string color, List<string> allowedDirections)
    {
        Id = id;
        Name = name;
        Color = color;
        AllowedDirection = allowedDirections ?? new() { "Left", "Up", "Right", "Down" };

    }

    public Field(int id, string name, string color, float speed, int halfLife, List<string> allowedDirections)
    {
        Id = id;
        Name = name;
        Color = color;
        Speed = speed;
        HalfLife = halfLife;
        AllowedDirection = allowedDirections ?? new() { "Left", "Up", "Right", "Down" };

    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}