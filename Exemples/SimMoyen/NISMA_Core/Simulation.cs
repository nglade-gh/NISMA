using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace nbd;

[Serializable, XmlRoot("simulation")]
[JsonSerializable(typeof(Simulation))]
[JsonSerializable(typeof(List<Simulation>))]
public class Simulation
{
    [XmlElement("name")][JsonPropertyName("name")] public string Name { get; set; } = "";
    [XmlElement("description")][JsonPropertyName("description")] public string Description { get; set; } = "";
    [XmlElement("settings")][JsonPropertyName("settings")] public SimulationSettings Settings { get; set; } = new();
    [XmlElement("agents")][JsonPropertyName("agents")] public List<int> Agents { get; set; } = new();
    [XmlElement("assets")][JsonPropertyName("assets")] public List<Asset3d> Assets { get; set; } = new();
    [XmlElement("width")][JsonPropertyName("width")] public int Width { get; set; } = 100;
    [XmlElement("height")][JsonPropertyName("height")] public int Height { get; set; } = 100;
    [XmlElement("depth")][JsonPropertyName("depth")] public int Depth { get; set; } = 1;

    public Simulation() { }

    public Simulation(string name, string description, SimulationSettings settings, List<int> agents, List<Asset3d> assets, int width, int height, int depth)
    {
        Name = name;
        Description = description;
        Settings = settings;
        Agents = agents;
        Assets = assets;
        Width = width;
        Height = height;
        Depth = depth;
    }

    public override string ToString() => JsonSerializer.Serialize(this);
}

[Serializable, XmlRoot("settings")]
[JsonSerializable(typeof(SimulationSettings))]
[JsonSerializable(typeof(List<SimulationSettings>))]
public class SimulationSettings
{
    [XmlElement("size")][JsonPropertyName("size")] public int Size { get; set; } = 100;
    [XmlElement("showGrid")][JsonPropertyName("showGrid")] public bool ShowGrid { get; set; } = true;
    [XmlElement("gridTextureUrl")][JsonPropertyName("gridTextureUrl")] public string? GridTextureUrl { get; set; }
    [XmlElement("backgroundColor")][JsonPropertyName("backgroundColor")] public string? BackgroundColor { get; set; }
    public SimulationSettings() { }
    public SimulationSettings(int size, bool showGrid, string? gridTextureUrl, string? backgroundColor)
    {
        Size = size;
        ShowGrid = showGrid;
        GridTextureUrl = gridTextureUrl;
        BackgroundColor = backgroundColor;
    }
    public override string ToString() => JsonSerializer.Serialize(this);
}

[Serializable, XmlRoot("assets")]
[JsonSerializable(typeof(Asset3d))]
[JsonSerializable(typeof(List<Asset3d>))]
public class Asset3d
{
    [XmlElement("url")][JsonPropertyName("url")] public string Url { get; set; } = "";
    [XmlElement("opacity")][JsonPropertyName("opacity")] public double Opacity { get; set; } = 1.0;
    [XmlElement("x")][JsonPropertyName("x")] public double X { get; set; } = 0.0;
    [XmlElement("y")][JsonPropertyName("y")] public double Y { get; set; } = 0.0;
    [XmlElement("z")][JsonPropertyName("z")] public double Z { get; set; } = 0.0;
    [XmlElement("xScale")][JsonPropertyName("xScale")] public double XScale { get; set; } = 1.0;
    [XmlElement("yScale")][JsonPropertyName("yScale")] public double YScale { get; set; } = 1.0;
    [XmlElement("zScale")][JsonPropertyName("zScale")] public double ZScale { get; set; } = 1.0;
    [XmlElement("xRotation")][JsonPropertyName("xRotation")] public double XRotation { get; set; } = 0.0;
    [XmlElement("yRotation")][JsonPropertyName("yRotation")] public double YRotation { get; set; } = 0.0;
    [XmlElement("zRotation")][JsonPropertyName("zRotation")] public double ZRotation { get; set; } = 0.0;
    public Asset3d() { }
    public Asset3d(string url, double opacity, double x, double y, double z, double xScale, double yScale, double zScale, double xRotation, double yRotation, double zRotation)
    {
        Url = url;
        Opacity = opacity;
        X = x;
        Y = y;
        Z = z;
        XScale = xScale;
        YScale = yScale;
        ZScale = zScale;
        XRotation = xRotation;
        YRotation = yRotation;
        ZRotation = zRotation;
    }
    public override string ToString() => JsonSerializer.Serialize(this);
}
