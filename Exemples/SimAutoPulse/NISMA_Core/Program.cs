using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using autopulse;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== AutoPulse Gradient (reimplementation de autopulse_gradient.netbiodyn) ===");

        var model = new GrainModel
        {
            Name = "AutoPulse Gradient",
            Description = "Modèle neuronal avec gradient et auto-impulsion",
            Grains = new List<Grain>
            {
                new(0, "Axone_A", "SpringGreen", 0.2)
                {
                    Icon = "square",
                    FieldProductions = { {0, 1f} },
                    FieldInfluences = { {1, 0f} }
                },
                new(1, "Axone_I", "OrangeRed", 0.2)
                {
                    FieldProductions = { {0, -1f} },
                    FieldInfluences = { {1, 0f} }
                },
                new(2, "Axone_0", "DarkTurquoise", 0.2),
                new(3, "pulse_A", "Green")
                {
                    FieldProductions = { {0, 1f} },
                    FieldInfluences = { {1, 0.45f} }
                },
                new(4, "Pulse_I", "MediumOrchid")
                {
                    FieldProductions = { {0, -1f} }
                },
                new(5, "Cell", "DarkGrey", 0.2)
                {
                    FieldProductions = { {0, 0f} },
                    FieldInfluences = { {0, -0.1f}, {1, 0.5f} },
                    FieldPermeable = { {0, 1f} }
                }
            },
            Behaviours = new List<Behaviour>
            {
                new("Activation_axone", 2, 0, -1)
                {
                    FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.GreaterThan, 0.01)) },
                    FieldInfluences = { {1, 0.2f} }
                },
                new("Inhibition_Latérale", 0, 1, -1)
                {
                    AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 3),
                    FieldInfluences = { {1, 0.4f} }
                },
                new("Regeneration_EtatNeutre", 1, 2, -1)
                {
                    AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 20),
                    FieldInfluences = { {1, -0.25f} }
                },
                new("Inactive_Pulse", 3, 4, 0)
                {
                    Probability = 0.9,
                    FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.GreaterThan, 0.2)) }
                },
                new("reactive_pulse", 4, 3, 0)
                {
                    Probability = 0.9,
                    FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.LessThan, 0.01)) }
                },
                new("Agregation_PA", 3, 3, 0)
                {
                    Probability = 0.5,
                    Reaction = { new Reaction { ReactiveId = 5, ProductId = 2, SourceReactive = 0 } }
                },
                new("Agregation_AA", 0, 0, 0)
                {
                    Reaction = { new Reaction { ReactiveId = 5, ProductId = 2, SourceReactive = 0 } }
                },
                new("endPulse", 3, 0, -1)
                {
                    AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 50)
                }
            },
            Fields = new List<Field>
            {
                new Field
                {
                    Id = 0, Name = "influx", Color = "Coral", Speed = 0.5f, HalfLife = 5,
                    AllowedDirection = new() { "Left", "Up", "Right", "Down" }
                },
                new Field
                {
                    Id = 1, Name = "gradient", Color = "DodgerBlue", Formula = "1/(x+1)",
                    AllowedDirection = new() { "Left", "Up", "Right", "Down" }
                }
            }
        };

        // --- Sauvegarde ---
        var simulation = new Simulation
        {
            Name = model.Name,
            Description = model.Description,
            Settings = new SimulationSettings { Size = 100, ShowGrid = true },
            Width = 100, Height = 100, Depth = 1,
            Agents = CreateAgents(model)
        };

        NbdUtils.SaveNbd("models/autopulse_gradient.netbiodyn", model, simulation);
        Console.WriteLine("Modèle sauvegardé dans models/autopulse_gradient.netbiodyn");
        Console.WriteLine($"  Grains : {model.Grains.Count}");
        Console.WriteLine($"  Behaviours : {model.Behaviours.Count}");
        Console.WriteLine($"  Fields : {model.Fields.Count}");
        Console.WriteLine($"  Agents placés : {simulation.Agents.Count(a => a >= 0)}");
    }

    static List<int> CreateAgents(GrainModel model)
    {
        var agents = Enumerable.Repeat(-1, 100 * 100).ToList();
        var rnd = new Random(42);

        // pulse_A (id=3) : 1 agent au centre
        agents[50 * 100 + 50] = 3;

        // Pulse_I (id=4) : 2 agents proche du centre
        agents[50 * 100 + 51] = 4;
        agents[49 * 100 + 50] = 4;

        // Cell (id=5) : population majoritaire dispersée
        for (var i = 0; i < 7137; i++)
        {
            var x = rnd.Next(0, 100);
            var y = rnd.Next(0, 100);
            var index = y * 100 + x;
            if (agents[index] < 0)
            {
                agents[index] = 5;
            }
            else
            {
                // Chercher une case libre voisine
                var placed = false;
                for (var dx = -1; dx <= 1 && !placed; dx++)
                {
                    for (var dy = -1; dy <= 1 && !placed; dy++)
                    {
                        var nx = x + dx;
                        var ny = y + dy;
                        if (nx >= 0 && nx < 100 && ny >= 0 && ny < 100)
                        {
                            var nidx = ny * 100 + nx;
                            if (agents[nidx] < 0)
                            {
                                agents[nidx] = 5;
                                placed = true;
                            }
                        }
                    }
                }
            }
        }

        return agents;
    }
}
