using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace nbd;

[Serializable, XmlRoot("model")]
[JsonSerializable(typeof(ModelWrapper))]
[JsonSerializable(typeof(List<ModelWrapper>))]
public class ModelWrapper
{
    [XmlElement("model")][JsonPropertyName("model")] public GrainModel Model { get; set; } = new();
    [XmlElement("simulation")][JsonPropertyName("simulation")] public Simulation Simulation { get; set; } = new();
    [XmlElement("thumbnail")][JsonPropertyName("thumbnail")] public string Thumbnail { get; set; } = "";

    public ModelWrapper() { }

    public ModelWrapper(GrainModel model, Simulation simulation, string thumbnail = "")
    {
        Model = model;
        Simulation = simulation;
        Thumbnail = thumbnail;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}
