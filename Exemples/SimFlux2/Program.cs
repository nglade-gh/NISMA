using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using NISMA_Core;

namespace flux2;

/// <summary>
/// Reconstructs flux2.netbiodyn from the reference file.
/// </summary>
public class Program
{
    private const string ReferencePath = "../../NetBioDyn_Models/flux2.netbiodyn";

    public static void Main()
    {
        var model = new NbdModel
        {
            Model = new Model
            {
                Grains =
                {
                    new Grain { Name = "A", Color = "Red", MovementProbability = 1, AllowedDirection = new List<string> { "Down", "LeftDown", "RightDown" }, FieldProductions = new Dictionary<int, float> { { 0, 1 } }, FieldInfluences = new Dictionary<int, float> { { 0, 0.6f }, { 2, 1 }, { 1, -0.6f } } },
                    new Grain { Id = 1, Name = "I", Color = "DarkOliveGreen", MovementProbability = 1, AllowedDirection = new List<string> { "Up", "LeftUp", "RightUp" }, FieldProductions = new Dictionary<int, float> { { 1, 1 } }, FieldInfluences = new Dictionary<int, float> { { 0, -0.6f }, { 1, 0.6f }, { 2, 1 } } },
                    new Grain { Id = 2, Name = "G", Color = "Lime", MovementProbability = 1, FieldProductions = new Dictionary<int, float> { { 2, 1 } }, FieldInfluences = new Dictionary<int, float> { { 1, -0.5f }, { 0, -0.5f } } }
                },
                Fields =
                {
                    new Field { Name = "A_trail", Color = "Tan", AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "RightUp", "LeftUp" } },
                    new Field { Id = 1, Name = "I_trail", Color = "Lavender" },
                    new Field { Id = 2, Name = "A_I_attractant", Color = "LightGreen" }
                },
                Behaviours =
                {
                    new Behaviour("A+I -> I+I", 0, 1, 0)
                    {
                        Probability = 0.1f,
                        Reaction = new List<Reaction> { new Reaction(1, 1) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } }
                    },
                    new Behaviour("G+I - G+A", 2, 2, 0)
                    {
                        Reaction = new List<Reaction> { new Reaction(1, 0) { AllowedDirection = new List<string> { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" } } }
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
        File.WriteAllText("models/flux2.netbiodyn", json);
        Console.WriteLine("Saved models/flux2.netbiodyn");
    }
}
