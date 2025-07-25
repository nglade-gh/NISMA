using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace nbd;

[Serializable, XmlRoot("grains")]
[JsonSerializable(typeof(Behaviour))]
public class Behaviour
{
    /********************************main properties*******************************************/
    
    [XmlElement("name")][JsonPropertyName("name")] public string Name{get; set;}
    [XmlElement("description")][JsonPropertyName("description")] public string Description{get; set;}
    public float _porbability; 

    [XmlElement("probability")]
    [JsonPropertyName("probability")]
    public float probability
    {
        get => _porbability;
        set => _porbability = Math.Clamp (value, 0, 1);

    }
    [XmlElement("mainReactiveId")][JsonPropertyName("mainReactiveId")] public uint MainReactiveId{get; set;}
    [XmlElement("mainProductId")][JsonPropertyName("mainProductId")] public uint MainProductId{get; set;}
    [XmlElement("sourceReactive")][JsonPropertyName("sourceReactive")] public uint SourceReactive{get; set;}

    // private float _fieldProduction;
    // [XmlElement("fieldProduction")]
    // [JsonPropertyName("fieldProduction")]
    // public float fieldProduction
    // {
    //     get => _fieldProduction; 
    //     set => _fieldProduction = Math.Clamp (value, -1, 1);
    // }
    //
//field influences here
    
    /*****************************lowkey hella optional, for real**************************/
    //age predicate is missing here
/*  private List<Directions> directionD;
  //to be added to the reaction 
    [XmlElement("allowedDirections")][JsonPropertyName("allowedDirections")]
    public List<string> directionS
    {
        get { return directionD.ConvertAll(f => Enum.GetName(f.GetType(), f)).ToList(); } 
        set{directionD = directionS.Select(element =>(Directions)Enum.Parse(typeof(Directions), element)).ToList();}
    }*/
    
    /****************************construction zone*****************************************/
    public Behaviour()
    {
        Name="Behaviour";
    }
    public Behaviour(string name, uint mainReactiveId){
        this.Name = name;
        this.MainReactiveId = mainReactiveId;
    }
    public Behaviour(string name, uint mainReactiveId, uint mainProductId){
        this.Name = name;
        this.MainReactiveId = mainReactiveId;
        this.MainProductId = mainProductId;
        
    }
    
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