using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NISMA_Core;

namespace SimSimple;

/// <summary>
/// SimSimple : 4 grains, 2 comportements, 1 champ.
/// </summary>
class Program
{
    static void Main()
    {
        var model = new NbdModel
        {
            Model = new Model
            {
                Name = "Réservoir simple",
                Description = "4 agriculteurs avec des comportements de base",
                Grains =
                {
                    new Grain { Id = 0, Name = "Agriculteur A", Color = "DarkGreen", Icon = "square-full", Size = 0.0f, FieldProductions = new Dictionary<int, float> { {0, 1f} }, FieldInfluences = new Dictionary<int, float> { {0, 0.5f} } },
                    new Grain { Id = 1, Name = "Agriculteur B", Color = "Green", Icon = "square-full", Size = 0.0f, FieldProductions = new Dictionary<int, float> { {0, 0.8f} }, FieldInfluences = new Dictionary<int, float> { {0, 0.4f} } },
                    new Grain { Id = 2, Name = "Agriculteur C", Color = "YellowGreen", Icon = "square-full", Size = 0.0f, FieldProductions = new Dictionary<int, float> { {0, 0.6f} }, FieldInfluences = new Dictionary<int, float> { {0, 0.3f} } },
                    new Grain { Id = 3, Name = "Agriculteur D", Color = "Olive", Icon = "square-full", Size = 0.0f, FieldProductions = new Dictionary<int, float> { {0, 0.4f} }, FieldInfluences = new Dictionary<int, float> { {0, 0.2f} } }
                },
                Fields =
                {
                    new Field { Id = 0, Name = "Champ", Color = "SandyBrown", Speed = 0.6f, HalfLife = 10, AllowedDirection = new List<string> { "Left", "Up", "Right", "Down" }, Formula = "0.5 + 0.3*sin(step/10)" }
                },
                Behaviours =
                {
                    new Behaviour("Production de blé", 0, 1, -1)
                    {
                        Probability = 0.8f,
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 10 },
                        FieldPredicates = new List<FieldPredicate> { new FieldPredicate { First = 0, Second = new Predicate { Op = "GreaterThan", Constant = 0.00001f } } },
                        Reaction = new List<Reaction> { new Reaction(0, 1) }
                    },
                    new Behaviour("Production de maïs", 0, 2, -1)
                    {
                        Probability = 0.6f,
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 15 },
                        FieldPredicates = new List<FieldPredicate> { new FieldPredicate { First = 0, Second = new Predicate { Op = "GreaterThan", Constant = 0.00002f } } },
                        Reaction = new List<Reaction> { new Reaction(0, 2) }
                    }
                }
            },
            Simulation = new Simulation { Name = "Réservoir simple", Description = "4 agriculteurs avec des comportements de base", Settings = new SimulationSettings { Size = 100 }, Agents = CreateAgents() }
        };

        var json = JsonUtils.SaveNbd(model);
        System.IO.Directory.CreateDirectory("models");
        System.IO.File.WriteAllText("models/simulation_simple.netbiodyn", json);
        Console.WriteLine("Saved models/simulation_simple.netbiodyn");
    }

    static List<int> CreateAgents()
    {
        var agents = Enumerable.Repeat(-1, 100 * 100).ToList();
        var rnd = new Random(42);
        var centreX = 50;
        var centreY = 50;

        foreach (var grainId in new[] { 0, 1, 2, 3 })
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
}
