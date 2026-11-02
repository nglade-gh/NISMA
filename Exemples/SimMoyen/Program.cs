using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NISMA_Core;

namespace SimMoyen;

/// <summary>
/// SimMoyen : exemple avec héritage de grains.
/// </summary>
class Program
{
    static void Main()
    {
        var model = new NbdModel
        {
            Model = new Model
            {
                Name = "Simulation moyenne",
                Description = "Exemple avec héritage de grains et comportements",
                Grains =
                {
                    new Grain { Id = 0, Name = "Base", Color = "Gray", FieldProductions = new Dictionary<int, float> { {0, 0.5f} }, FieldInfluences = new Dictionary<int, float> { {0, -0.2f} } },
                    new Grain { Id = 1, Name = "Producteur", Color = "Green", FieldProductions = new Dictionary<int, float> { {0, 1.0f} }, FieldInfluences = new Dictionary<int, float> { {0, 0.3f} } },
                    new Grain { Id = 2, Name = "Consommateur", Color = "Red", FieldProductions = new Dictionary<int, float> { {0, -0.5f} }, FieldInfluences = new Dictionary<int, float> { {0, -0.1f} } }
                },
                Fields =
                {
                    new Field { Id = 0, Name = "Ressource", Color = "Blue", Speed = 0.3f, HalfLife = 15 }
                },
                Behaviours =
                {
                    new Behaviour("Production", 1, 1, 0)
                    {
                        FieldInfluences = new Dictionary<int, float> { {0, 1.0f} },
                        Reaction = new List<Reaction> { new Reaction(1, 1) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down" } } }
                    },
                    new Behaviour("Consommation", 2, 2, 0)
                    {
                        FieldInfluences = new Dictionary<int, float> { {0, -0.8f} },
                        Reaction = new List<Reaction> { new Reaction(2, 2) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down" } } }
                    },
                    new Behaviour("Transformation", 0, 1, 0)
                    {
                        Probability = 0.7f,
                        FieldInfluences = new Dictionary<int, float> { {0, 0.5f} },
                        Reaction = new List<Reaction> { new Reaction(0, 1) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down" } } }
                    }
                }
            },
            Simulation = new Simulation { Name = "Simulation moyenne", Description = "Exemple avec héritage de grains et comportements", Settings = new SimulationSettings { Size = 100 }, Agents = CreateAgents() }
        };

        var json = JsonUtils.SaveNbd(model);
        System.IO.Directory.CreateDirectory("models");
        System.IO.File.WriteAllText("models/simulation_moyen.netbiodyn", json);
        Console.WriteLine("Saved models/simulation_moyen.netbiodyn");
    }

    static List<int> CreateAgents()
    {
        var agents = Enumerable.Repeat(-1, 100 * 100).ToList();
        var rnd = new Random(42);
        var centreX = 50;
        var centreY = 50;

        foreach (var grainId in new[] { 0, 1, 2 })
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
