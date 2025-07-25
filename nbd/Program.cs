
using System.Text.Json;
using nbd.sub_classes;

namespace nbd;

internal class Program
{
    public static void Main(string[] args)
    {
        var testGrain = new Grain(0, "tester", Database.Colours.Aqua,100, icon: "poop");
        JsonUtils<Grain> jsonSerializer = new JsonUtils<Grain>();
        jsonSerializer.Save("grain1.json", testGrain);
        
        
        /* #################### TESTS ZONE #################### */
        /*//test grain list
        var grains = new List<Grain>()
          {
              new Grain(0,"g1",Colours.Blue),
              new Grain(1,"g2",Colours.Chartreuse),
              new Grain(2,"g3",Colours.Bisque)
          };
          JsonUtils<Grain> jsonSerializer = new JsonUtils<Grain>();
          jsonSerializer.Save("grains1.json", grains);
          
        //test one (1) solo, singular uno grain
        var testGrain = new Grain(0, "tester", Colours.Aqua);
        JsonUtils<Grain> jsonSerializer = new JsonUtils<Grain>();
        jsonSerializer.Save("grain1.json", testGrain);
        
        //test behaviour
        var testBehaviour = new Behaviour("Behaviour1", 0,0,0);
        JsonUtils<Grain> jsonSerializer2 = new JsonUtils<Grain>();
        jsonSerializer2.Save("behaviour1.json", testBehaviour);
        
        var testBehaviour = new Behaviour();
        JsonUtils<Grain> jsonSerializer2 = new JsonUtils<Grain>();
        jsonSerializer2.Save("behaviour1.json", testBehaviour);
        
        //field tests
        var testField = new Field(0, "tester", Colours.Aqua);
        JsonUtils<Field> jsonSerializer3 = new JsonUtils<Field>();
        jsonSerializer3.Save("field1.json", testField);
        
        var field2 = new Field(0,"testField2",Colours.Azure, 0.68545f,20);
        JsonUtils<Field> json = new JsonUtils<Field>();
        json.Save("field2.json", field2);
        
                
        var testField = new Field();
        JsonUtils<Field> jsonSerializer3 = new JsonUtils<Field>();
        jsonSerializer3.Save("field1.json", testField);
;*/
        

    }
}

