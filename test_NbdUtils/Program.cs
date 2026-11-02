using System;
using nbd;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing NbdUtils...");

        // Create a simple GrainModel
        var grains = new List<Grain> { new() { Id = 0, Name = "Test" } };
        var model = new nbd.sub_classes.GrainModel
        {
            Name = "Test",
            Grains = grains
        };

        NbdUtils.SaveNbd("output.nbd", model);
        Console.WriteLine("SUCCESS - file created");
    }
}
