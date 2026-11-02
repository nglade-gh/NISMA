using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using NISMA_Core;

namespace SimCeur;

/// <summary>
/// Reconstructs coeur.netbiodyn from the reference file.
/// </summary>
public class Program
{
    private const string ReferencePath = "../../NetBioDyn_Models/coeur.netbiodyn";

    public static void Main()
    {
        var model = new NbdModel
        {
            Model = new Model
            {
                Grains =
                {
                    new Grain { Name = "C_a", Color = "Lime", FieldProductions = new Dictionary<int, float> { { 0, 1 } } },
                    new Grain { Id = 1, Name = "C_i", Color = "DodgerBlue" },
                    new Grain { Id = 2, Name = "C_q", Color = "Crimson" },
                    new Grain { Id = 3, Name = "hiss_i", Color = "Plum" },
                    new Grain { Id = 4, Name = "hiss_a", Color = "Cyan", FieldProductions = new Dictionary<int, float> { { 0, 1 } } },
                    new Grain { Id = 5, Name = "hiss_q", Color = "DimGrey" },
                    new Grain { Id = 6, Name = "ns_i", Color = "LimeGreen", FieldProductions = new Dictionary<int, float> { { 0, 0 } } },
                    new Grain { Id = 7, Name = "ns_a", Color = "Magenta", FieldProductions = new Dictionary<int, float> { { 0, 1 } } }
                },
                Fields =
                {
                    new Field { Name = "pa", Color = "Orange", Speed = 0.3f, HalfLife = 4, AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "RightUp", "LeftUp", "LeftDown", "RightDown" } }
                },
                Behaviours =
                {
                    new Behaviour("Behaviour 1", 1, 0, 0)
                    {
                        FieldPredicates = new List<FieldPredicate> { new FieldPredicate { First = 0, Second = new Predicate { Op = "GreaterThan", Constant = 0.05f } } }
                    },
                    new Behaviour("Behaviour", 0, 2)
                    {
                        SourceReactive = -1,
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 10 }
                    },
                    new Behaviour("Behaviour 2", 2, 1)
                    {
                        SourceReactive = -1,
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 40 }
                    },
                    new Behaviour("Behaviour 3", 4, 4)
                    {
                        SourceReactive = -1,
                        Reaction = new List<Reaction> { new Reaction(3, 4) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "RightUp", "LeftUp", "LeftDown", "RightDown" } } }
                    },
                    new Behaviour("Behaviour 4", 5, 3)
                    {
                        SourceReactive = -1,
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 40 }
                    },
                    new Behaviour("Behaviour 6", 6, 7)
                    {
                        SourceReactive = -1,
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 20 }
                    },
                    new Behaviour("Behaviour 7", 7, 6)
                    {
                        SourceReactive = -1,
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 70 }
                    },
                    new Behaviour("Behaviour 5", 3, 4, 0)
                    {
                        FieldPredicates = new List<FieldPredicate> { new FieldPredicate { First = 0, Second = new Predicate { Op = "GreaterThan", Constant = 0.01f } } }
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
        File.WriteAllText("models/coeur.netbiodyn", json);
        Console.WriteLine("Saved models/coeur.netbiodyn");
    }
}
