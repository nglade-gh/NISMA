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
            Console.WriteLine("=== Simulation moyenne (héritage + comportements génériques) ===");

            var meta = new MetaModel();

            meta.AddType(new TypeDecl { Name = "lymphocyte", IsAbstract = true });
            meta.AddType(new TypeDecl { Name = "LA", Parent = "lymphocyte", IsAbstract = false });
            meta.AddType(new TypeDecl { Name = "LB", Parent = "lymphocyte", IsAbstract = false });
            meta.AddType(new TypeDecl { Name = "CelluleCible", IsAbstract = true });
            meta.AddType(new TypeDecl { Name = "A", Parent = "CelluleCible", IsAbstract = false });
            meta.AddType(new TypeDecl { Name = "B", Parent = "CelluleCible", IsAbstract = false });
            meta.AddType(new TypeDecl { Name = "C", Parent = "CelluleCible", IsAbstract = false });

            meta.AddBehaviour(new BehaviourDecl
            {
                Name = "Lymphocyte-Cible",
                Categories = new[] { "lymphocyte", "CelluleCible" },
                Reactions = new List<Reaction>
                {
                    new()
                    {
                        ReactiveId = -1,
                        ProductId = -1,
                        SourceReactive = 0,
                        AllowedDirection = new() { "Left", "Up", "Right", "Down" }
                    }
                }
            });

            var model = meta.Generate();
            Console.WriteLine("Types concrets générés :");
            foreach (var g in model.Grains)
            {
                Console.WriteLine("  Grain : " + g.Name + " (id=" + g.Id + ")");
            }

            Console.WriteLine("Comportements générés : " + model.Behaviours.Count);
            foreach (var b in model.Behaviours)
            {
                Console.WriteLine("  Comportement : " + b.Name);
            }

            // --- 3. Sauvegarde du modèle au format .netbiodyn (compatible centyllion) ---
            var simulation = new nbd.Simulation
            {
                Name = model.Name,
                Description = model.Description,
                Settings = new nbd.SimulationSettings { Size = 100, ShowGrid = true },
                Width = 100, Height = 100, Depth = 1,
                Agents = CreateAgents(model)
            };
            NbdUtils.SaveNbd("models/simulation_moyen.netbiodyn", model, simulation);
            Console.WriteLine("\nModèle sauvegardé dans models/simulation_moyen.netbiodyn");
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
                for (var i = 0; i < 25; i++)
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
}
