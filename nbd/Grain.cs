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
    //properties are now in order of appearance in the case they are all explicit, which is
    //id, name, color, icon, invisible, description, size, halflife, movementProbability
    //allowedDirection, fieldProductions, fieldInfluences and fieldPermeable
    [XmlElement("id")][JsonPropertyName("id")] public uint Id{get; set;} //positive int
    [XmlElement("name")][JsonPropertyName("name")] public string Name{get; set;}
    private Database.Colours colorC;

    [XmlElement("color")][JsonPropertyName("color")] public String colorS
    {
        get { return Enum.GetName(colorC.GetType(), colorC); ;}
        set { colorC = (Database.Colours)Enum.Parse(typeof(Database.Colours), value, true); }
    }
    private String _icon = "square";
    [XmlElement("icon")]
    [JsonPropertyName("icon")]
    public string Icon
    {
        get { return _icon; }
        set{
            if (Database.icons.Contains(value))// appelle le checkIcon(value)
            {
                _icon=value;// si ok _icon = value;
            }
            else// sinon default behaviour
            {
                _icon = "square";
            }
        }
    }
    [XmlElement"invisible"][JsonPropertyName("invisible")] public bool invisible{get; set;}
    [XmlElement"description"][JsonPropertyName("description")] public string Description{get; set;}
    [XmlElement("size")]  [JsonPropertyName("size")] 
    public float size
    {
        get => _size;
        set => _size = Math.Clamp (value, 1, 5);

    }
    [XmlElement("halfLife")][JsonPropertyName("halfLife")] public uint Halflife{get; set;}
    [XmlElement("movementProbability")]  [JsonPropertyName("movementProbability")] 
    public float movementProbability
    {
        get => _movementProbability;
        set => _movementProbability = Math.Clamp (value, 0, 1);

    }
    private List<Directions> directionD;
    [XmlElement("allowedDirections")][JsonPropertyName("allowedDirections")]
    public List<string> directionS
    {
        get { return directionD.ConvertAll(f => Enum.GetName(f.GetType(), f)).ToList(); } 
        set{directionD = directionS.Select(element =>(Directions)Enum.Parse(typeof(Directions), element)).ToList();}
    }

//field productions
//field influences
//fieldPermeable
    
    

    
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