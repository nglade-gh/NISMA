using System.Text.Json;
using nbd;
using nbd.sub_classes;
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Simulation simple (pas d'héritage) ===");

        // --- 1. Création des grains (agents directs) ---
        var grains = new GrainModel
        {
            Name = "Réservoir simple",
            Description = "4 agriculteurs avec des comportements de base",
            Grains = new List<Grain>
            {
                new(0, "Agriculteur A", "DarkGreen", 0, "square-full", 0.0)
                { FieldProductions = { {0, 1f} }, FieldInfluences = { {0, 0.5f} } },
                new(1, "Agriculteur B", "Green", 1, "square-full", 0.0)
                { FieldProductions = { {0, 0.8f} }, FieldInfluences = { {0, 0.4f} } },
                new(2, "Agriculteur C", "YellowGreen", 2, "square-full", 0.0)
                { FieldProductions = { {0, 0.6f} }, FieldInfluences = { {0, 0.3f} } },
                new(3, "Agriculteur D", "Olive", 3, "square-full", 0.0)
                { FieldProductions = { {0, 0.4f} }, FieldInfluences = { {0, 0.2f} } }
            },
            Behaviours = new List<Behaviour>
            {
                new("Production de blé", 0, 1, -1)
                {
                    Probability = 0.8,
                    AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 10),
                    FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.GreaterThan, 0.00001)) },
                    Reaction = { new Reaction { ReactiveId = 0, ProductId = 1, SourceReactive = -1 } }
                },
                new("Production de maïs", 0, 2, -1)
                {
                    Probability = 0.6,
                    AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 15),
                    FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.GreaterThan, 0.00002)) },
                    Reaction = { new Reaction { ReactiveId = 0, ProductId = 2, SourceReactive = -1 } }
                }
            },
            Fields = new List<Field>
            {
                new(0, "Champ", "SandyBrown", 0.6f, 10, new() { "Left", "Up", "Right", "Down" })
                { Formula = "0.5 + 0.3*sin(step/10)" }
            }
        };

        // --- 3. Sauvegarde du modèle au format .netbiodyn (compatible centyllion) ---
        var simulation = new nbd.Simulation
        {
            Name = grains.Name,
            Description = grains.Description,
            Settings = new nbd.SimulationSettings { Size = 100, ShowGrid = true },
            Width = 100, Height = 100, Depth = 1,
            Agents = CreateAgents(grains)
        };
        nbd.NbdUtils.SaveNbd("models/simulation_simple.netbiodyn", grains, simulation);
        Console.WriteLine("Modèle sauvegardé dans models/simulation_simple.netbiodyn");
        Console.WriteLine($"Grains : {grains.Grains.Count}, Behaviours : {grains.Behaviours.Count}, Champs : {grains.Fields.Count}");
        Console.WriteLine($"Agents placés : {simulation.Agents.Count(a => a >= 0)}");
        Console.WriteLine();

        static List<int> CreateAgents(GrainModel model)
        {
            var agents = Enumerable.Repeat(-1, 100 * 100).ToList();
            var rnd = new Random(42);
            var centreX = 50;
            var centreY = 50;

            // Place des agents pour chaque grain qui est main reactive d'au moins un behaviour
            var mainReactiveIds = model.Behaviours
                .Where(b => b.MainReactiveId >= 0)
                .Select(b => b.MainReactiveId)
                .Distinct()
                .ToList();

            foreach (var grainId in mainReactiveIds)
            {
                for (var i = 0; i < 30; i++)
                {
                    var x = centreX + rnd.Next(-15, 16);
                    var y = centreY + rnd.Next(-15, 16);
                    x = Math.Clamp(x, 0, 99);
                    y = Math.Clamp(y, 0, 99);
                    var index = y * 100 + x;
                    agents[index] = grainId;
                }
            }

            return agents;
        }


        // --- Test ModelWrapper serialization ---
        var mw = new nbd.ModelWrapper
        {
            Name = grains.Name,
            Description = grains.Description,
            Grains = grains.Grains,
            Behaviours = grains.Behaviours,
            Fields = grains.Fields
        };
        var mwJson = System.Text.Json.JsonSerializer.Serialize(mw, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("=== MODELWRAPPER JSON ===");
        Console.WriteLine(mwJson);


        // --- Test NbdModel serialization with SaveNbd options ---
        var testModel = new nbd.ModelWrapper
        {
            Name = grains.Name,
            Description = grains.Description,
            Grains = grains.Grains,
            Behaviours = grains.Behaviours,
            Fields = grains.Fields
        };
        var testSim = new nbd.Simulation
        {
            Name = grains.Name,
            Description = grains.Description,
            Settings = new nbd.SimulationSettings { Size = 100, ShowGrid = true },
            Agents = Enumerable.Repeat(-1, 100 * 100).ToList(),
            Width = 100, Height = 100, Depth = 1
        };
        var testNbd = new nbd.NbdModel
        {
            Model = testModel,
            Simulation = testSim,
            Thumbnail = ""
        };
        var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = false };
        var nbdJson = System.Text.Json.JsonSerializer.Serialize(testNbd, options);
        Console.WriteLine("=== NBDMODEL JSON (no indent) ===");
        Console.WriteLine(nbdJson);

        // --- 4. Fin du test simple ---
        Console.WriteLine("=== Test simple terminé ===");
    }
}
