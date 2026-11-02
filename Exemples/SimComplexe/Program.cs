using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NISMA_Core;

namespace SimComplexe;

/// <summary>
/// SimComplexe : exemple complexe avec plusieurs grains et behaviours.
/// </summary>
class Program
{
    static void Main()
    {
        var model = new NbdModel
        {
            Model = new Model
            {
                Name = "Simulation complexe",
                Description = "Exemple avec plusieurs grains et behaviours",
                Grains =
                {
                    new Grain { Id = 0, Name = "Herbivore", Color = "Brown", FieldProductions = new Dictionary<int, float> { {0, 0.8f} }, FieldInfluences = new Dictionary<int, float> { {0, -0.3f} } },
                    new Grain { Id = 1, Name = "Carnivore", Color = "Red", FieldProductions = new Dictionary<int, float> { {1, 1.0f} }, FieldInfluences = new Dictionary<int, float> { {1, -0.5f} } },
                    new Grain { Id = 2, Name = "Plante", Color = "Green", FieldProductions = new Dictionary<int, float> { {0, 1.0f} }, FieldInfluences = new Dictionary<int, float> { {0, 0.2f} } },
                    new Grain { Id = 3, Name = "Decomposeur", Color = "Yellow", FieldProductions = new Dictionary<int, float> { {0, -0.5f} }, FieldInfluences = new Dictionary<int, float> { {0, 0.1f} } },
                    new Grain { Id = 4, Name = "Eau", Color = "Blue", FieldProductions = new Dictionary<int, float> { {1, 0.5f} }, FieldInfluences = new Dictionary<int, float> { {1, 0.0f} } },
                    new Grain { Id = 5, Name = "Nutriment", Color = "Orange", FieldProductions = new Dictionary<int, float> { {0, 0.3f} }, FieldInfluences = new Dictionary<int, float> { {0, -0.1f} } }
                },
                Fields =
                {
                    new Field { Id = 0, Name = "Nourriture", Color = "LightGreen", Speed = 0.2f, HalfLife = 20 },
                    new Field { Id = 1, Name = "Danger", Color = "Red", Speed = 0.4f, HalfLife = 10 }
                },
                Behaviours =
                {
                    new Behaviour("Chasse", 1, 1, 0)
                    {
                        Probability = 0.8f,
                        FieldInfluences = new Dictionary<int, float> { {1, -1.0f} },
                        Reaction = new List<Reaction> { new Reaction(1, 1) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down" } } }
                    },
                    new Behaviour("Reproduction", 0, 0, 0)
                    {
                        Probability = 0.6f,
                        Reaction = new List<Reaction> { new Reaction(0, 0) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down" } } }
                    },
                    new Behaviour("Photosynthese", 2, 2, 0)
                    {
                        FieldInfluences = new Dictionary<int, float> { {0, 1.0f} },
                        Reaction = new List<Reaction> { new Reaction(2, 2) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down" } } }
                    },
                    new Behaviour("Decomposition", 3, 3, 0)
                    {
                        FieldInfluences = new Dictionary<int, float> { {0, 0.5f} },
                        Reaction = new List<Reaction> { new Reaction(3, 3) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down" } } }
                    },
                    new Behaviour("Predation", 1, 0, 0)
                    {
                        Probability = 0.5f,
                        FieldInfluences = new Dictionary<int, float> { {1, 1.0f} },
                        Reaction = new List<Reaction> { new Reaction(1, 0) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down" } } }
                    }
                }
            },
            Simulation = new Simulation { Name = "Simulation complexe", Description = "Exemple avec plusieurs grains et behaviours", Settings = new SimulationSettings { Size = 100 }, Agents = CreateAgents() }
        };

        var json = JsonUtils.SaveNbd(model);
        System.IO.Directory.CreateDirectory("models");
        System.IO.File.WriteAllText("models/simulation_complexe.netbiodyn", json);
        Console.WriteLine("Saved models/simulation_complexe.netbiodyn");
    }

    static List<int> CreateAgents()
    {
        var agents = Enumerable.Repeat(-1, 100 * 100).ToList();
        var rnd = new Random(42);
        var centreX = 50;
        var centreY = 50;

        foreach (var grainId in new[] { 0, 1, 2, 3, 4, 5 })
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
