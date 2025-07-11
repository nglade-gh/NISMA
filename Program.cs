
using System.Text.Json;

namespace nbd;

internal class Program
{
    public static void Main(string[] args)
    {
        var field2 = new Field(0,"testField2",Colours.Azure, 5f,20);
        
        JsonUtils<Field> json = new JsonUtils<Field>();
        json.Save("field2.json", field2);
        
        
        
        
        
        
        
        /* #################### TESTS ZONE #################### */
        /*var grains = new List<Grain>()
          {
              new Grain(0,"g1",Colours.Blue),
              new Grain(1,"g2",Colours.Chartreuse),
              new Grain(2,"g3",Colours.Bisque)
          };
          JsonUtils<Grain> jsonSerializer = new JsonUtils<Grain>();
          jsonSerializer.Save("grains1.json", grains);
        var testGrain = new Grain(0, "tester", Colours.Aqua);
        JsonUtils<Grain> jsonSerializer = new JsonUtils<Grain>();
        jsonSerializer.Save("grain1.json", testGrain);
        var testBehaviour = new Behaviour("Behaviour1", 0,0,0);
        JsonUtils<Grain> jsonSerializer2 = new JsonUtils<Grain>();
        jsonSerializer2.Save("behaviour1.json", testBehaviour);
        var testField = new Field(0, "tester", Colours.Aqua);
        JsonUtils<Grain> jsonSerializer3 = new JsonUtils<Grain>();
        jsonSerializer3.Save("field1.json", testGrain);*/
        

    }
}

