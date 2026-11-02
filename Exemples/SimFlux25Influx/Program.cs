using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using NISMA_Core;

namespace flux25influx;

/// <summary>
/// Reconstructs flux25_influx.netbiodyn from the reference file.
/// </summary>
public class Program
{
    private const string ReferencePath = "../../NetBioDyn_Models/flux25_influx.netbiodyn";

    public static void Main()
    {
        var model = new NbdModel
        {
            Model = new Model
            {
                Grains =
                {
                    new Grain { Name = "A_a", Color = "HotPink", MovementProbability = 1, AllowedDirection = new List<string> { "Down", "LeftDown", "RightDown" }, FieldProductions = new Dictionary<int, float> { { 0, 1 }, { 3, 1 } }, FieldInfluences = new Dictionary<int, float> { { 0, 0.6f }, { 2, 1 }, { 1, -0.6f } } },
                    new Grain { Id = 1, Name = "I_a", Color = "CornflowerBlue", MovementProbability = 1, AllowedDirection = new List<string> { "Up", "LeftUp", "RightUp" }, FieldProductions = new Dictionary<int, float> { { 1, 1 }, { 3, 1 } }, FieldInfluences = new Dictionary<int, float> { { 0, -0.6f }, { 1, 0.6f }, { 2, 1 } } },
                    new Grain { Id = 2, Name = "G", Color = "Lime", MovementProbability = 1, FieldProductions = new Dictionary<int, float> { { 2, 1 }, { 4, -1 } }, FieldInfluences = new Dictionary<int, float> { { 1, -0.5f }, { 0, -0.5f } } },
                    new Grain { Id = 3, Name = "A_i", Color = "Crimson", Icon = "square", Size = 0.3f, MovementProbability = 1, AllowedDirection = new List<string> { "Down", "RightDown", "LeftDown" }, FieldProductions = new Dictionary<int, float> { { 0, 1 } }, FieldInfluences = new Dictionary<int, float> { { 0, 0.6f }, { 1, -0.6f }, { 2, 1 } } },
                    new Grain { Id = 4, Name = "I_i", Color = "Blue", Icon = "square", Size = 0.3f, MovementProbability = 1, AllowedDirection = new List<string> { "Up", "LeftUp", "RightUp" }, FieldProductions = new Dictionary<int, float> { { 0, 0 }, { 1, 1 } }, FieldInfluences = new Dictionary<int, float> { { 0, -0.6f }, { 1, 0.6f }, { 2, 1 } } },
                    new Grain { Id = 5, Name = "A_0", Color = "LightSalmon", Size = 0.2f, MovementProbability = 1, AllowedDirection = new List<string> { "Down", "LeftDown", "RightDown" }, FieldProductions = new Dictionary<int, float> { { 0, 1 } }, FieldInfluences = new Dictionary<int, float> { { 0, 0.6f }, { 1, -0.6f }, { 2, 1 } } },
                    new Grain { Id = 6, Name = "I_0", Color = "DarkTurquoise", Size = 0.2f, MovementProbability = 1, AllowedDirection = new List<string> { "Up", "LeftUp", "RightUp" }, FieldProductions = new Dictionary<int, float> { { 1, 1 } }, FieldInfluences = new Dictionary<int, float> { { 1, 0.6f }, { 0, -0.6f }, { 2, 1 } } }
                },
                Fields =
                {
                    new Field { Name = "A_trail", Color = "Tan" },
                    new Field { Id = 1, Name = "I_trail", Color = "Lavender" },
                    new Field { Id = 2, Name = "A_I_attractant", Color = "LightGreen", Speed = 0.6f, AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } },
                    new Field { Id = 3, Name = "influx", Color = "Pink", Speed = 0.4f, AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } },
                    new Field { Id = 4, Name = "inducteur", Color = "LightSlateGray", Invisible = true, AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "RightUp", "LeftUp", "LeftDown", "RightDown" }, Formula = "1/(y+1)*sin(0.5*step)" }
                },
                Behaviours =
                {
                    new Behaviour("Aa+Ia -> Ia+Ia", 0, 1, 0) { Probability = 0.1f, FieldInfluences = new Dictionary<int, float> { { 3, 1 } }, Reaction = new List<Reaction> { new Reaction(1, 1) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } } },
                    new Behaviour("G+I0 - G+A0", 2, 2, 0) { Reaction = new List<Reaction> { new Reaction(6, 5) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } } },
                    new Behaviour("Aa -> Ai", 0, 3) { AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 3 } },
                    new Behaviour("Ai -> A0", 3, 5) { AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 20 } },
                    new Behaviour("A0 -> Aa", 5, 0) { FieldPredicates = new List<FieldPredicate> { new FieldPredicate { First = 3, Second = new Predicate { Op = "GreaterThan", Constant = 0.1f } } }, FieldInfluences = new Dictionary<int, float> { { 4, 1 } } },
                    new Behaviour("Ia -> Ii", 1, 4) { AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 3 } },
                    new Behaviour("Ii -> I0", 4, 6) { AgePredicate = new Predicate { Op = "GreaterThanOrEquals", Constant = 20 } },
                    new Behaviour("I0 -> Ia", 6, 1) { FieldPredicates = new List<FieldPredicate> { new FieldPredicate { First = 3, Second = new Predicate { Op = "GreaterThan", Constant = 0.01f } } }, FieldInfluences = new Dictionary<int, float> { { 4, -1 } } },
                    new Behaviour("Aa+Ii -> Ia+Ii", 0, 1, 0) { Probability = 0.1f, FieldInfluences = new Dictionary<int, float> { { 3, -1 } }, Reaction = new List<Reaction> { new Reaction(4, 4) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } } },
                    new Behaviour("Aa+I0 -> Ia+I0", 0, 1, 0) { Probability = 0.1f, FieldInfluences = new Dictionary<int, float> { { 4, 1 } }, Reaction = new List<Reaction> { new Reaction(6, 6) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } } },
                    new Behaviour("Ai+Ia -> Ii+Ia", 3, 4, 0) { Probability = 0.1f, FieldInfluences = new Dictionary<int, float> { { 3, -1 } }, Reaction = new List<Reaction> { new Reaction(1, 1) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } } },
                    new Behaviour("Ai+Ii -> Ii+Ii", 3, 4, 0) { Probability = 0.1f, FieldInfluences = new Dictionary<int, float> { { 3, 0 }, { 4, 1 } }, Reaction = new List<Reaction> { new Reaction(4, 4) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } } },
                    new Behaviour("Ai+I0 -> Ii+I0", 3, 4, 0) { Probability = 0.1f, FieldInfluences = new Dictionary<int, float> { { 3, -0.5f }, { 4, 0.5f } }, Reaction = new List<Reaction> { new Reaction(6, 6) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } } },
                    new Behaviour("A0+Ia -> I0+Ia", 5, 6, 0) { Probability = 0.1f, FieldInfluences = new Dictionary<int, float> { { 3, 0.5f }, { 4, 0.5f } }, Reaction = new List<Reaction> { new Reaction(1, 1) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } } },
                    new Behaviour("A0+Ii -> I0+Ii", 5, 6, 0) { Probability = 0.1f, FieldInfluences = new Dictionary<int, float> { { 3, -0.5f }, { 4, 0.5f } }, Reaction = new List<Reaction> { new Reaction(4, 4) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } } },
                    new Behaviour("A0+I0 -> I0+I0", 5, 6, 0) { Probability = 0.1f, FieldInfluences = new Dictionary<int, float> { { 4, -1 } }, Reaction = new List<Reaction> { new Reaction(6, 6) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } } },
                    new Behaviour("G+Ia - G+Aa", 2, 2, 0) { Reaction = new List<Reaction> { new Reaction(1, 0) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } } },
                    new Behaviour("G+Ii - G+Ai", 2, 2, 0) { Reaction = new List<Reaction> { new Reaction(4, 3) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } } }
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
        File.WriteAllText("models/flux25_influx.netbiodyn", json);
        Console.WriteLine("Saved models/flux25_influx.netbiodyn");
    }
}
