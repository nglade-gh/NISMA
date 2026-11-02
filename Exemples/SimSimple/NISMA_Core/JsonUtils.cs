using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using nbd.sub_classes;

namespace nbd;

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
[JsonSerializable(typeof(ModelWrapper))]
[JsonSerializable(typeof(List<ModelWrapper>))]
public class NbdModel
{
    [JsonPropertyName("model")]
    public ModelWrapper Model { get; set; } = new();

    [JsonPropertyName("simulation")]
    public Simulation Simulation { get; set; } = new();

    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; } = "";
}

public class ModelWrapper
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("grains")]
    public List<Grain> Grains { get; set; } = new();

    [JsonPropertyName("behaviours")]
    public List<Behaviour> Behaviours { get; set; } = new();

    [JsonPropertyName("fields")]
    public List<Field> Fields { get; set; } = new();
}

public static class NbdUtils
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false
    };

    public static void SaveNbd(string path, GrainModel model, Simulation simulation = null)
    {
        ModelWrapper modelWrapper = new ModelWrapper
        {
            Name = model.Name,
            Description = model.Description,
            Grains = model.Grains,
            Behaviours = model.Behaviours,
            Fields = model.Fields
        };

        Simulation sim = simulation ?? new Simulation
        {
            Name = model.Name,
            Description = model.Description,
            Settings = new SimulationSettings { Size = 100, ShowGrid = true },
            Agents = Enumerable.Repeat(-1, 100 * 100).ToList()
        };

        NbdModel nbdModel = new NbdModel
        {
            Model = modelWrapper,
            Simulation = sim,
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

public class JsonUtils<T>
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = false };
    public T Load(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path))!;
    public void Save(string path, object obj) => File.WriteAllText(path, JsonSerializer.Serialize(obj, _options));
}
