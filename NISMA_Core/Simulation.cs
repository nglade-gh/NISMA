using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NISMA_Core;

/// <summary>
/// Simulation settings (grid size, etc.).
/// </summary>
public class SimulationSettings
{
    /// <summary>Grid size (number of cells per side).</summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    public SimulationSettings()
    {
    }

    public SimulationSettings(int size)
    {
        Size = size;
    }
}

/// <summary>
/// 3D asset definition for the simulation environment.
/// </summary>
public class Asset3d
{
    /// <summary>Asset name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Asset URL or path.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    public Asset3d()
    {
    }

    public Asset3d(string name, string url)
    {
        Name = name;
        Url = url;
    }
}

/// <summary>
/// Top-level simulation description (environment parameters).
/// </summary>
public class Simulation
{
    /// <summary>Simulation name. Optional.</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Name { get; set; }

    /// <summary>Simulation description. Optional.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; }

    /// <summary>Simulation settings.</summary>
    [JsonPropertyName("settings")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public SimulationSettings? Settings { get; set; }

    /// <summary>Grid width. Optional.</summary>
    [JsonPropertyName("width")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Width { get; set; }

    /// <summary>Grid height. Optional.</summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Height { get; set; }

    /// <summary>Grid depth. Optional.</summary>
    [JsonPropertyName("depth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Depth { get; set; }

    /// <summary>Agent placement grid (flattened, row-major). Must be Size*Size entries.</summary>
    [JsonPropertyName("agents")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<int>? Agents { get; set; }

    /// <summary>3D assets for the environment. Optional.</summary>
    [JsonPropertyName("assets")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<Asset3d>? Assets { get; set; }

    public Simulation()
    {
    }

    public Simulation(SimulationSettings settings, List<int> agents)
    {
        Settings = settings;
        Agents = agents;
    }
}
