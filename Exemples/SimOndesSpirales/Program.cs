using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using NISMA_Core;

namespace SimOndesSpirales;

/// <summary>
/// Reconstructs ondes_spirales.netbiodyn from the reference file.
/// </summary>
public class Program
{
    private const string ReferencePath = "../../NetBioDyn_Models/ondes_spiralles.netbiodyn";

    public static void Main()
    {
        var model = new NbdModel
        {
            Model = new Model
            {
                Grains =
                {
                    new Grain { Name = "glande active insuline", Color = "Lime" },
                    new Grain { Id = 1, Name = "insuline", Color = "HotPink", MovementProbability = 1, AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "RightDown", "LeftDown", "LeftUp", "RightUp" } },
                    new Grain { Id = 2, Name = "glande inactive insuline ", Color = "Chocolate" },
                    new Grain { Id = 3, Name = "glande active glucagon", Color = "MediumTurquoise" },
                    new Grain { Id = 4, Name = "glande inactive glucagon ", Color = "DarkBlue" },
                    new Grain { Id = 5, Name = "hormone glucagon ", Color = "Bisque", MovementProbability = 1, AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftDown", "RightDown", "RightUp", "LeftUp" } }
                },
                Behaviours =
                {
                    new Behaviour("GA»GA+H insuline ", 0, 0, 0)
                    {
                        AgePredicate = new Predicate { Op = "NotEquals", Constant = 0 },
                        Reaction = new List<Reaction> { new Reaction(null, 1) }
                    },
                    new Behaviour("GA»GI insuline ", 0, 2)
                    {
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 10 }
                    },
                    new Behaviour("GI » GA insuline ", 2, 0)
                    {
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 50 }
                    },
                    new Behaviour("GA<GA+H glucagon", 3, 3, 0)
                    {
                        Reaction = new List<Reaction>
                        {
                            new Reaction(null, 5)
                            {
                                AllowedDirection = new List<string> { "Left", "Up", "Right", "LeftDown", "Down", "RightUp", "LeftUp", "RightDown" }
                            }
                        }
                    },
                    new Behaviour("GA<GI glucagon", 3, 4)
                    {
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 20 }
                    },
                    new Behaviour("GI<GA glucagon", 4, 3, 0)
                    {
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 60 }
                    },
                    new Behaviour("activation insuline ", 5, 0),
                    new Behaviour("inhibition glucagon", 1, 4, 0),
                    new Behaviour("autoactivation insuline ", 1, 0, 0)
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
        File.WriteAllText("models/ondes_spirales.netbiodyn", json);
        Console.WriteLine("Saved models/ondes_spirales.netbiodyn");
    }
}
