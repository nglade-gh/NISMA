using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace nbd;

[Serializable, XmlRoot("fields")]
[JsonSerializable(typeof(Field))]
public class Field
{
    /******MANDATORY PARAMETERS *******/
    [XmlElement("id")][JsonPropertyName("id")] public uint Id{get; set;}
    [XmlElement("name")][JsonPropertyName("name")] public string Name{get; set;}

    private Colours colorC;

    [XmlElement("color")][JsonPropertyName("color")] public String colorS {
        get { return Enum.GetName(colorC.GetType(), colorC); ;}
        set { colorC = (Colours)Enum.Parse(typeof(Colours), value, true); }
    }
    /*******************PARAMETERS THAT ONLY APPEAR IF SPECIFIED EXPLICITLY ***************/
    
    private List<Directions> directionD;

    [XmlElement("allowedDirections")]
    [JsonPropertyName("allowedDirections")]
    public List<string> directionS
    { //took 2 hours to fix, do not touch
        get { return directionD.ConvertAll(f => Enum.GetName(f.GetType(), f)).ToList(); } 
        set{directionD = directionS.Select(element =>(Directions)Enum.Parse(typeof(Directions), element)).ToList();}
    }

    public float _speed;

    [XmlElement("speed")]
    [JsonPropertyName("speed")]
    public float speed
    {
        get => _speed;
        //set{ Math.Clamp (speed, 0, 1);}
        set
        {
            if (value < 0 || value > 1)
            {
                _speed = 1;
            }
            else
            {
                _speed = value;
            }
        }
    }
    
    [XmlElement("halfLife")][JsonPropertyName("halfLife")] public uint halfLife{get; set;}
    //insert formula thing here
    
    /* ########################### CONSTRUCTEURS ##################################*/
    public Field()
    {
        
    }
    public Field(uint id, string name, Colours color){
        Id = id;
        Name = name;
        this.colorC = color;
        
    }
    
    //unnecessary and will be changed once i figure out how this overloading thing works
    //I just need it rn to make these are truly optional
    public Field(uint id, string name, Colours color, List<Directions> dir_list = null ){
        Id = id;
        Name = name;
        this.colorC = color;
        //default value when user doesn't edit the directions list
        if(dir_list == null)
        {
            dir_list = [Directions.Left, Directions.Up, Directions.Right, Directions.Down];
        }
        this.directionD = dir_list;
        
    }
    public Field(uint id, string name, Colours color,float speed, uint halfLife , List<Directions> dir_list = null ){
        Id = id;
        Name = name;
        this.colorC = color;
        this.halfLife = halfLife;
        //default value when user doesn't edit the directions list
        if(dir_list == null)
        {
            dir_list = [Directions.Left, Directions.Up, Directions.Right, Directions.Down];
        }
        this.directionD = dir_list;
        
        
    }
    
    public override string ToString() {
        String s = JsonSerializer.Serialize(this);
        return s;
    }

}