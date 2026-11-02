using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using NISMA_Core;

namespace autopulse;

/// <summary>
/// Reconstructs autopulse_gradient.netbiodyn from the reference file.
/// </summary>
public class Program
{
    private const string ReferencePath = "../../NetBioDyn_Models/autopulse_gradient.netbiodyn";

    public static void Main()
    {
        var model = new NbdModel
        {
            Model = new Model
            {
                Grains =
                {
                    new Grain { Name = "Axone_A", Color = "SpringGreen", Icon = "square", Size = 0.2f, FieldProductions = new Dictionary<int, float> { { 0, 1 } }, FieldInfluences = new Dictionary<int, float> { { 1, 0 } } },
                    new Grain { Id = 1, Name = "Axone_I", Color = "OrangeRed", Size = 0.2f, FieldProductions = new Dictionary<int, float> { { 0, -1 } }, FieldInfluences = new Dictionary<int, float> { { 1, 0 } } },
                    new Grain { Id = 2, Name = "Axone_0", Color = "DarkTurquoise", Size = 0.2f },
                    new Grain { Id = 3, Name = "pulse_A", Color = "Green", FieldProductions = new Dictionary<int, float> { { 0, 1 } }, FieldInfluences = new Dictionary<int, float> { { 1, 0.45f } } },
                    new Grain { Id = 4, Name = "Pulse_I", Color = "MediumOrchid", FieldProductions = new Dictionary<int, float> { { 0, -1 } } },
                    new Grain { Id = 5, Name = "Cell", Color = "DarkGrey", Size = 0.2f, MovementProbability = 1, FieldProductions = new Dictionary<int, float> { { 0, 0 } }, FieldInfluences = new Dictionary<int, float> { { 0, -0.1f }, { 1, 0.5f } }, FieldPermeable = new Dictionary<int, float> { { 0, 1f } } }
                },
                Fields =
                {
                    new Field { Name = "influx", Color = "Coral", Speed = 0.5f, HalfLife = 5 },
                    new Field { Id = 1, Name = "gradient", Color = "DodgerBlue", Formula = "1/(x+1)" }
                },
                Behaviours =
                {
                    new Behaviour("Activation_axone", 2, 0)
                    {
                        SourceReactive = -1,
                        FieldPredicates = new List<FieldPredicate> { new FieldPredicate { First = 0, Second = new Predicate { Op = "GreaterThan", Constant = 0.01f } } },
                        FieldInfluences = new Dictionary<int, float> { { 1, 0.2f } }
                    },
                    new Behaviour("Inhibition_Latérale", 0, 1)
                    {
                        SourceReactive = -1,
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 3 },
                        FieldInfluences = new Dictionary<int, float> { { 1, 0.4f } }
                    },
                    new Behaviour("Regeneration_EtatNeutre", 1, 2)
                    {
                        SourceReactive = -1,
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 20 },
                        FieldInfluences = new Dictionary<int, float> { { 1, -0.25f } }
                    },
                    new Behaviour("Inactive_Pulse", 3, 4)
                    {
                        SourceReactive = 0,
                        Probability = 0.9f,
                        FieldPredicates = new List<FieldPredicate> { new FieldPredicate { First = 0, Second = new Predicate { Op = "GreaterThan", Constant = 0.2f } } }
                    },
                    new Behaviour("reactive_pulse", 4, 3)
                    {
                        SourceReactive = 0,
                        Probability = 0.9f,
                        FieldPredicates = new List<FieldPredicate> { new FieldPredicate { First = 0, Second = new Predicate { Op = "LessThan", Constant = 0.01f } } }
                    },
                    new Behaviour("Agregation_PA", 3, 3)
                    {
                        SourceReactive = 0,
                        Probability = 0.5f,
                        Reaction = new List<Reaction> { new Reaction(5, 2) }
                    },
                    new Behaviour("Agregation_AA", 0, 0)
                    {
                        SourceReactive = 0,
                        Reaction = new List<Reaction> { new Reaction(5, 2) }
                    },
                    new Behaviour("endPulse", 3, 0)
                    {
                        SourceReactive = -1,
                        AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 50 }
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
        File.WriteAllText("models/autopulse_gradient.netbiodyn", json);
        Console.WriteLine("Saved models/autopulse_gradient.netbiodyn");
    }
}
