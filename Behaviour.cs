using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace nbd;

[Serializable, XmlRoot("grains")]
[JsonSerializable(typeof(Behaviour))]
public class Behaviour
{
    [XmlElement("name")][JsonPropertyName("name")] public string Name{get; set;}
    [XmlElement("mainReactiveId")][JsonPropertyName("mainReactiveId")] public uint MainReactiveId{get; set;}
    [XmlElement("mainProductId")][JsonPropertyName("mainProductId")] public uint MainProductId{get; set;}
    [XmlElement("sourceReactive")][JsonPropertyName("sourceReactive")] public uint SourceReactive{get; set;}
    
    public Behaviour() { }
    
    public Behaviour(string name, uint mainReactiveId, uint mainProductId, uint sourceReactive){
            this.Name = name;
            this.MainReactiveId = mainReactiveId;
            this.MainProductId = mainProductId;
            this.SourceReactive = sourceReactive;
    }
    
    public override string ToString()
    {
        String s = JsonSerializer.Serialize(this);
        return s;
    }

}