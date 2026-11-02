using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using NISMA_Core;

namespace SimEco;

/// <summary>
/// Reconstructs eco.netbiodyn from the reference file.
/// </summary>
public class Program
{
    private const string ReferencePath = "../../NetBioDyn_Models/eco.netbiodyn";

    public static void Main()
    {
        var model = new NbdModel
        {
            Model = new Model
            {
                Grains =
                {
                    new Grain { Name = "Agt-1", Color = "Blue", MovementProbability = 1, FieldProductions = new Dictionary<int, float> { { 0, 0 } } },
                    new Grain { Id = 1, Name = "Agt-2", Color = "Brown", MovementProbability = 1 },
                    new Grain { Id = 2, Name = "Agt-3", Color = "SandyBrown", MovementProbability = 1 },
                    new Grain { Id = 3, Name = "Agt-4", Color = "LawnGreen", MovementProbability = 1, FieldInfluences = new Dictionary<int, float> { { 0, 1 } } },
                    new Grain { Id = 4, Name = "Ctr", Color = "Tan", FieldProductions = new Dictionary<int, float> { { 0, 1 } } }
                },
                Fields =
                {
                    new Field { Name = "Champ", Color = "PeachPuff" }
                },
                Behaviours =
                {
                    new Behaviour("12=>11", 0, 0, 0)
                    {
                        Reaction = new List<Reaction> { new Reaction(1, 0) }
                    },
                    new Behaviour("23=>22", 1, 1, 0)
                    {
                        Reaction = new List<Reaction> { new Reaction(2, 1) }
                    },
                    new Behaviour("31=>33", 2, 2, 0)
                    {
                        Reaction = new List<Reaction> { new Reaction(0, 2) }
                    },
                    new Behaviour("1=>11", 0, 0, 0)
                    {
                        Reaction = new List<Reaction> { new Reaction(null, 0) }
                    },
                    new Behaviour("2=>22", 1, 1, 0)
                    {
                        Reaction = new List<Reaction> { new Reaction(null, 1) }
                    },
                    new Behaviour("3=>33", 2, 2, 0)
                    {
                        Reaction = new List<Reaction> { new Reaction(null, 2) }
                    },
                    new Behaviour("Comportement", 3, null)
                    {
                        SourceReactive = -1,
                        Reaction = new List<Reaction> { new Reaction(0, 3) }
                    },
                    new Behaviour("Comportement 1", 3, null)
                    {
                        SourceReactive = -1,
                        Reaction = new List<Reaction> { new Reaction(1, 3) }
                    },
                    new Behaviour("Comportement 2", 3, null)
                    {
                        SourceReactive = -1,
                        Reaction = new List<Reaction> { new Reaction(2, 3) }
                    }
                }
            },
            Simulation = new Simulation { Name = "", Description = "" }
        };

        // Load agents from reference
        var refText = File.ReadAllText(ReferencePath);
        var refJson = JsonNode.Parse(refText);
        var refAgents = refJson?["simulation"]?["agents"]?.AsArray();
        if (refAgents is not null)
        {
            model.Simulation.Agents = new List<int>();
            foreach (var a in refAgents)
            {
                if (a is JsonValue v && v.TryGetValue<int>(out var val))
                    model.Simulation.Agents.Add(val);
            }
        }

        var json = JsonUtils.SaveNbd(model, ReferencePath);

        Directory.CreateDirectory("models");
        File.WriteAllText("models/eco.netbiodyn", json);
        Console.WriteLine("Saved models/eco.netbiodyn");
    }
}
