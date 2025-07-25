using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using nbd.sub_classes;

namespace nbd;

[Serializable, XmlRoot("grains")]
[JsonSerializable(typeof(Grain))]
public class Grain
{
    //properties obligatoirement explicites
    [XmlElement("id")][JsonPropertyName("id")] public uint Id{get; set;} //positive int
    [XmlElement("name")][JsonPropertyName("name")] public string Name{get; set;}

    private Database.Colours colorC;

    [XmlElement("color")][JsonPropertyName("color")] public String colorS
    {
        get { return Enum.GetName(colorC.GetType(), colorC); ;}
        set { colorC = (Database.Colours)Enum.Parse(typeof(Database.Colours), value, true); }
    }
    //properties implicites
    [XmlElement("halfLife")][JsonPropertyName("halfLife")] public uint Halflife{get; set;}

    private String _icon = "square";
    
    [XmlElement("icon")]
    [JsonPropertyName("icon")]
    public string Icon
    {
        get { return _icon; }
        set
        {
           
            if (Database.icons.Contains(value))
            {
                _icon=value;
            }
            else
            {
                _icon = "square";
            }
            // appelle le checkIcon(value)
            // si ok _icon = value;
            // sinon default behaviour
        }
    }
    
    /*********************************************CONSTRUCTOR ZONE*****************************************************/
    public Grain() { }
    
    public Grain(uint id, string name, Database.Colours color){
        Id = id;
        Name = name;
        this.colorC = color;
    }
    public Grain(uint id, string name, Database.Colours color, uint halflife){
        Id = id;
        Name = name;
        this.colorC = color;
        Halflife = halflife;
    }
    public Grain(uint id, string name, Database.Colours color, uint halflife, string icon){
        Id = id;
        Name = name;
        this.colorC = color;
        Halflife = halflife;
        Icon = icon;
    }
    public override string ToString()
    {
        String s = JsonSerializer.Serialize(this);// "[{id:" + Id + ", name:" + Name + ", color:" + colorS + "}]";
        return s;
    }
    

}