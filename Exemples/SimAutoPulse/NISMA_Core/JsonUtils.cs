using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using autopulse.sub_classes;

namespace autopulse;

/// <summary>
/// Format .netbiodyn compatible avec centyllion
/// Structure exacte attendue par centyllion.model.ModelAndSimulation
/// </summary>
[JsonSerializable(typeof(NbdModel))]
[JsonSerializable(typeof(List<Grain>))]
[JsonSerializable(typeof(List<Behaviour>))]
[JsonSerializable(typeof(List<Field>))]
[JsonSerializable(typeof(GrainModel))]
[JsonSerializable(typeof(Simulation))]
[JsonSerializable(typeof(List<Simulation>))]
public class NbdModel
{
    [JsonPropertyName("model")] public GrainModel Model { get; set; } = new();
    [JsonPropertyName("simulation")] public Simulation Simulation { get; set; } = new();
    [JsonPropertyName("thumbnail")] public string Thumbnail { get; set; } = "";
}

public class GrainModel
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("grains")] public List<Grain> Grains { get; set; } = new();
    [JsonPropertyName("behaviours")] public List<Behaviour> Behaviours { get; set; } = new();
    [JsonPropertyName("fields")] public List<Field> Fields { get; set; } = new();

    public GrainModel() { }
}

public static class NbdUtils
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false
    };

    public static void SaveNbd(string path, GrainModel model, Simulation? simulation = null)
    {
        var nbdModel = new NbdModel
        {
            Model = model,
            Simulation = simulation ?? new Simulation(),
            Thumbnail = ""
        };

        string jsonString = JsonSerializer.Serialize(nbdModel, _options);
        File.WriteAllText(path, jsonString);
    }

    public static NbdModel LoadNbd(string path)
    {
        string jsonString = File.ReadAllText(path);
        return JsonSerializer.Deserialize<NbdModel>(jsonString)!;
    }
}
