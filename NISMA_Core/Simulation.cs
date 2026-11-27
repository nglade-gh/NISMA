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

    /// <summary>
    /// Custom width of the grid, used instead of <see cref="Size"/> when both
    /// custom values are set. Optional — omitted when 0.
    /// </summary>
    [JsonPropertyName("customWidth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? CustomWidth { get; set; }

    /// <summary>Custom height of the grid. Optional — omitted when 0.</summary>
    [JsonPropertyName("customHeight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? CustomHeight { get; set; }

    /// <summary>True when both custom values are set: the grid is not square.</summary>
    [JsonIgnore]
    public bool IsCustomSize => (CustomWidth ?? 0) > 0 && (CustomHeight ?? 0) > 0;

    /// <summary>Whether the grid is drawn. Optional (default: true).</summary>
    [JsonPropertyName("showGrid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? ShowGrid { get; set; }

    /// <summary>Texture URL used to draw the grid. Optional.</summary>
    [JsonPropertyName("gridTextureUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? GridTextureUrl { get; set; }

    /// <summary>Background colour of the simulation view. Optional.</summary>
    [JsonPropertyName("backgroundColor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? BackgroundColor { get; set; }

    public SimulationSettings()
    {
    }

    public SimulationSettings(int size)
    {
        Size = size;
    }
}

/// <summary>
/// 3D asset definition for the simulation environment: URL plus the
/// position / scale / rotation transform written by Centyllion.
/// </summary>
public class Asset3d
{
    /// <summary>Asset name. Not written by Centyllion; kept for compatibility.</summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Name { get; set; }

    /// <summary>Asset URL or path.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>Opacity of the asset (default: 1). Optional.</summary>
    [JsonPropertyName("opacity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? Opacity { get; set; }

    [JsonPropertyName("x")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? X { get; set; }

    [JsonPropertyName("y")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? Y { get; set; }

    [JsonPropertyName("z")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? Z { get; set; }

    [JsonPropertyName("xScale")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? XScale { get; set; }

    [JsonPropertyName("yScale")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? YScale { get; set; }

    [JsonPropertyName("zScale")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? ZScale { get; set; }

    [JsonPropertyName("xRotation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? XRotation { get; set; }

    [JsonPropertyName("yRotation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? YRotation { get; set; }

    [JsonPropertyName("zRotation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? ZRotation { get; set; }

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
    /// <summary>
    /// Simulation name. Always written, empty when unnamed: Centyllion declares it as a
    /// non-nullable field without default, so a file that omits it cannot be opened.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Simulation description. Always written, for the same reason as <see cref="Name"/>.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

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
