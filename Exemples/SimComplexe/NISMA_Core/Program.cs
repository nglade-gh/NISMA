using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using nbd;
using nbd.sub_classes;

namespace nbd
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Simulation complexe (champs, nombreux agents et comportements) ===");

            var grains = new GrainModel
            {
                Name = "Réseau cellulaire complexe",
                Description = "12 agents avec des comportements de signalisation et de division",
                Grains = new List<Grain>
                {
                    new(0, "Neurone A1", "Blue", 0, "square-full", 0.0)
                    { FieldProductions = { {0, 1f} }, FieldInfluences = { {0, 0.5f} } },
                    new(1, "Neurone A2", "LightBlue", 1, "square-full", 0.0)
                    { FieldProductions = { {0, 0.8f} }, FieldInfluences = { {0, 0.4f} } },
                    new(2, "Neurone B1", "Navy", 2, "square-full", 0.0)
                    { FieldProductions = { {0, 0.6f} }, FieldInfluences = { {0, 0.3f} } },
                    new(3, "Neurone B2", "MediumBlue", 3, "square-full", 0.0)
                    { FieldProductions = { {0, 0.4f} }, FieldInfluences = { {0, 0.2f} } },
                    new(4, "Glia A", "Green", 4, "square-full", 0.0)
                    { FieldProductions = { {0, 0.7f} }, FieldInfluences = { {0, 0.3f} } },
                    new(5, "Glia B", "DarkGreen", 5, "square-full", 0.0)
                    { FieldProductions = { {0, 0.5f} }, FieldInfluences = { {0, 0.2f} } },
                    new(6, "Cellule T", "Red", 6, "square-full", 0.0)
                    { FieldProductions = { {0, 0.9f} }, FieldInfluences = { {0, 0.6f} } },
                    new(7, "Cellule B", "Crimson", 7, "square-full", 0.0)
                    { FieldProductions = { {0, 0.7f} }, FieldInfluences = { {0, 0.5f} } },
                    new(8, "Macrophage", "DarkRed", 8, "square-full", 0.0)
                    { FieldProductions = { {0, 0.8f} }, FieldInfluences = { {0, 0.55f} } },
                    new(9, "Lymphocyte", "OrangeRed", 9, "square-full", 0.0)
                    { FieldProductions = { {0, 0.6f} }, FieldInfluences = { {0, 0.4f} } },
                    new(10, "Adhésif", "SaddleBrown", 10, "square-full", 0.0)
                    { FieldProductions = { {0, 0.3f} }, FieldInfluences = { {0, 0.1f} } },
                    new(11, "Base", "DimGray", 11, "square-full", 0.0)
                    { FieldProductions = { {0, 0.1f} }, FieldInfluences = { {0, 0.05f} } }
                },

                Behaviours = new List<Behaviour>
                {
                    new("Signalisation Axo-Somatique", 0, 1, -1)
                    {
                        Probability = 0.9,
                        AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 0),
                        FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.GreaterThan, 0.00001)) },
                        Reaction = { new Reaction { ReactiveId = 0, ProductId = 1, SourceReactive = -1 } }
                    },
                    new("Signalisation Somatique-Axo", 0, 2, -1)
                    {
                        Probability = 0.7,
                        AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 5),
                        FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.GreaterThan, 0.00001)) },
                        Reaction = { new Reaction { ReactiveId = 0, ProductId = 2, SourceReactive = -1 } }
                    },
                    new("Astrocytaire - Neurone", 4, 0, -1)
                    {
                        Probability = 0.8,
                        AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 0),
                        FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.GreaterThan, 0.00001)) },
                        Reaction = { new Reaction { ReactiveId = 0, ProductId = 0, SourceReactive = -1 } }
                    },
                    new("Astrocytaire - Glie", 4, 5, -1)
                    {
                        Probability = 0.6,
                        AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 5),
                        FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.GreaterThan, 0.00001)) },
                        Reaction = { new Reaction { ReactiveId = 0, ProductId = 5, SourceReactive = -1 } }
                    },
                    new("Cytotoxicité", 6, 8, -1)
                    {
                        Probability = 0.9,
                        AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 0),
                        FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.GreaterThan, 0.00001)) },
                        Reaction = { new Reaction { ReactiveId = 0, ProductId = 8, SourceReactive = -1 } }
                    },
                    new("Anticorps", 7, 9, -1)
                    {
                        Probability = 0.8,
                        AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 10),
                        FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.GreaterThan, 0.00001)) },
                        Reaction = { new Reaction { ReactiveId = 0, ProductId = 9, SourceReactive = -1 } }
                    },
                    new("Phagocytose", 8, 9, -1)
                    {
                        Probability = 0.7,
                        AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 15),
                        FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.GreaterThan, 0.00001)) },
                        Reaction = { new Reaction { ReactiveId = 0, ProductId = 9, SourceReactive = -1 } }
                    },
                    new("Adhésion Cellulaire", 10, 11, -1)
                    {
                        Probability = 0.6,
                        AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 0),
                        FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.GreaterThan, 0.00001)) },
                        Reaction = { new Reaction { ReactiveId = 0, ProductId = 11, SourceReactive = -1 } }
                    }
                },

                Fields = new List<Field>
                {
                    new(0, "Champ Nerf", "SkyBlue", 0.7f, 10, new() { "Left", "Up", "Right", "Down" })
                    { Formula = "(0.5+0.5*sin(step/10))" },
                    new(1, "Champ Glia", "LimeGreen", 0.6f, 10, new() { "Left", "Up", "Right", "Down" })
                    { Formula = "(0.6+0.4*cos(step/15))" },
                    new(2, "Champ Immune", "Tomato", 0.8f, 10, new() { "Left", "Up", "Right", "Down" })
                    { Formula = "(0.8-0.3*exp(-step/20))" }
                }
            };

            // --- 3. Sauvegarde du modèle au format .netbiodyn (compatible centyllion) ---
            var simulation = new nbd.Simulation
            {
                Name = grains.Name,
                Description = grains.Description,
                Settings = new nbd.SimulationSettings { Size = 100, ShowGrid = true },
                Width = 100, Height = 100, Depth = 1,
                Agents = CreateAgents(grains)
            };
            NbdUtils.SaveNbd("models/simulation_complexe.netbiodyn", grains, simulation);
            Console.WriteLine("Modèle complexe sauvegardé dans models/simulation_complexe.netbiodyn");
            Console.WriteLine("  Grains : " + grains.Grains.Count);
            Console.WriteLine("  Comportements : " + grains.Behaviours.Count);
            Console.WriteLine("  Champs : " + grains.Fields.Count);
            Console.WriteLine($"Agents placés : {simulation.Agents.Count(a => a >= 0)}");
        }

        static List<int> CreateAgents(GrainModel model)
        {
            var agents = Enumerable.Repeat(-1, 100 * 100).ToList();
            var rnd = new Random(42);
            var centreX = 50;
            var centreY = 50;

            var mainReactiveIds = model.Behaviours
                .Where(b => b.MainReactiveId >= 0)
                .Select(b => b.MainReactiveId)
                .Distinct()
                .ToList();

            foreach (var grainId in mainReactiveIds)
            {
                for (var i = 0; i < 15; i++)
                {
                    var x = centreX + rnd.Next(-20, 21);
                    var y = centreY + rnd.Next(-20, 21);
                    x = Math.Clamp(x, 0, 99);
                    y = Math.Clamp(y, 0, 99);
                    var index = y * 100 + x;
                    agents[index] = grainId;
                }
            }

            return agents;
        }
    }
}
