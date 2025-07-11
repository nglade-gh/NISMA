using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace nbd;

[Serializable, XmlRoot("grains")]
[JsonSerializable(typeof(Grain))]
public class Grain
{
    [XmlElement("id")][JsonPropertyName("id")] public uint Id{get; set;}
    [XmlElement("name")][JsonPropertyName("name")] public string Name{get; set;}

    private Colours colorC;

    [XmlElement("color")][JsonPropertyName("color")] public String colorS
    {
        get { return Enum.GetName(colorC.GetType(), colorC); ;}
        set { colorC = (Colours)Enum.Parse(typeof(Colours), value, true); }
    }
    
    public Grain() { }
    
    public Grain(uint id, string name, Colours color){
        Id = id;
        Name = name;
        this.colorC = color;
    }
    
    public override string ToString()
    {
        String s = JsonSerializer.Serialize(this);// "[{id:" + Id + ", name:" + Name + ", color:" + colorS + "}]";
        return s;
    }

}