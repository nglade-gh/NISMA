using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using nbd.sub_classes;

namespace nbd;

[Serializable, XmlRoot("fields")]
[JsonSerializable(typeof(Field))]
public class Field
{
    /******MANDATORY PARAMETERS *******/
    [XmlElement("id")][JsonPropertyName("id")] public uint Id{get; set;}
    [XmlElement("name")][JsonPropertyName("name")] public string Name{get; set;}

    private Database.Colours colorC;

    [XmlElement("color")][JsonPropertyName("color")] public String colorS {
        get => Enum.GetName(colorC.GetType(), colorC);
        set => colorC = (Database.Colours)Enum.Parse(typeof(Database.Colours), value, true);
    }
    /*******************PARAMETERS THAT ONLY APPEAR IF SPECIFIED EXPLICITLY ***************/
    
    private List<Database.Directions> directionD;

    [XmlElement("allowedDirections")]
    [JsonPropertyName("allowedDirections")]
    public List<string> directionS
    { //took 2 hours to fix, do not touch
        get { return directionD.ConvertAll(f => Enum.GetName(f.GetType(), f)).ToList(); } 
        set{directionD = directionS.Select(element =>(Database.Directions)Enum.Parse(typeof(Database.Directions), element)).ToList();}
    }

    public float _speed; 

    [XmlElement("speed")]
    [JsonPropertyName("speed")]
    public float speed
    {
        get => _speed;
        set => _speed = Math.Clamp (value, 0, 1);
    }
    
    [XmlElement("halfLife")][JsonPropertyName("halfLife")] public uint halfLife{get; set;}
    //insert formula thing here
    
    /* ########################### CONSTRUCTEURS ##################################*/
    public Field()
    {
        directionD = [];
    }
    public Field(uint id, string name, Database.Colours color){
        Id = id;
        Name = name;
        this.colorC = color;
        directionD = [];
        
    }
    
    //unnecessary and will be changed once i figure out how this overloading thing works
    //I just need it rn to make these are truly optional
    public Field(uint id, string name, Database.Colours color, List<Database.Directions> dir_list = null ){
        Id = id;
        Name = name;
        this.colorC = color;
        //default value when user doesn't edit the directions list
        if(dir_list == null)
        {
            dir_list = [Database.Directions.Left, Database.Directions.Up, Database.Directions.Right, Database.Directions.Down];
        }
        this.directionD = dir_list;
        
    }
    public Field(uint id, string name, Database.Colours color,float speed, uint halfLife , List<Database.Directions> dir_list = null ){
        Id = id;
        Name = name;
        this.colorC = color;
        this.speed = speed;
        this.halfLife = halfLife;
        //default value when user doesn't edit the directions list
        if(dir_list == null)
        {
            dir_list = [Database.Directions.Left, Database.Directions.Up, Database.Directions.Right, Database.Directions.Down];
        }
        this.directionD = dir_list;
        
        
    }
    
    public override string ToString() {
        String s = JsonSerializer.Serialize(this);
        return s;
    }

}