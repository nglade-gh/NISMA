using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using nbd.sub_classes;

namespace nbd;

/// <summary>
/// Format .netbiodyn compatible with centyllion
/// </summary>
[JsonSerializable(typeof(NbdModel))]
[JsonSerializable(typeof(List<Grain>))]
[JsonSerializable(typeof(List<Behaviour>))]
[JsonSerializable(typeof(List<Field>))]
public class NbdModel
{
    [JsonPropertyName("model")]
    public ModelSection Model { get; set; } = new();

    [JsonPropertyName("simulation")]
    public SimulationSection Simulation { get; set; } = new();
}

[JsonSerializable(typeof(List<Grain>))]
public class ModelSection
{
    [JsonPropertyName("grains")]
    public List<Grain> Grains { get; set; } = new();

    [JsonPropertyName("behaviours")]
    public List<Behaviour> Behaviours { get; set; } = new();

    [JsonPropertyName("fields")]
    public List<Field> Fields { get; set; } = new();
}

[JsonSerializable(typeof(List<int>))]
public class SimulationSection
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("agents")]
    public List<int> Agents { get; set; } = new();
}

public static class NbdUtils
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false
    };

    public static void SaveNbd(string path, GrainModel model)
    {
        NbdModel nbdModel = new NbdModel
        {
            Model = new ModelSection
            {
                Grains = model.Grains,
                Behaviours = model.Behaviours,
                Fields = model.Fields
            },
            Simulation = new SimulationSection
            {
                Name = model.Name,
                Description = model.Description,
                Agents = new List<int>()
            }
        };

        string jsonString = JsonSerializer.Serialize(nbdModel, _options);
        File.WriteAllText(path, jsonString);
    }
}

public class JsonUtils<T>
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false
    };

    public T Load(string path)
    {
        string jsonString = File.ReadAllText(path);
        T _instance = JsonSerializer.Deserialize<T>(jsonString)!;
        return _instance;
    }

    public void Save(string path, object obj)
    {
        string jsonString = JsonSerializer.Serialize(obj, _options);
        File.WriteAllText(path, jsonString);
    }
}
