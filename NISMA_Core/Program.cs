using System.Text.Json;
using nbd.sub_classes;

namespace nbd;

internal class Program
{
    public static void Main(string[] args)
    {
        // Test 1 : création d'un grain complet
        Console.WriteLine("=== Test 1 : Grain complet ===");
        var grain = new Grain(0, "Agent", "DarkGoldenRod", 0, "square-full", 0.0);
        Console.WriteLine("Grain créé : " + grain);
        Console.WriteLine();

        // Test 2 : création d'un comportement complet
        Console.WriteLine("=== Test 2 : Comportement complet ===");
        var behaviour = new Behaviour(
            name: "Cristallisation",
            mainReactiveId: 0,
            mainProductId: 1,
            sourceReactive: -1
        );
        behaviour.AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 50);
        behaviour.FieldPredicates.Add(new FieldPredicate(1, new Predicate(Operator.GreaterThan, 0.00001)));
        behaviour.Reaction.Add(new Reaction {
            ReactiveId = 0,
            ProductId = 1,
            SourceReactive = -1,
            AllowedDirections = new() { "Left", "Up", "Right", "Down" }
        });
        Console.WriteLine("Comportement créé : " + behaviour);
        Console.WriteLine();

        // Test 3 : création d'un champ
        Console.WriteLine("=== Test 3 : Champ ===");
        var field = new Field(0, "Champ", "OrangeRed", 0.8f, 10, new() { "Left", "Up", "Right", "Down" });
        field.Formula = "(1+sin(step/25))/2";
        Console.WriteLine("Champ créé : " + field);
        Console.WriteLine();

        // Test 4 : sérialisation/désérialisation JSON
        Console.WriteLine("=== Test 4 : Sérialisation JSON ===");
        string json = JsonSerializer.Serialize(grain, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("Grain en JSON :\n" + json);
        Console.WriteLine();

        // Test 5 : chargement d'un modèle au format NISMA
        Console.WriteLine("=== Test 5 : Chargement d'un modèle (models/nisma_format.json) ===");
        try
        {
            var jsonUtils = new JsonUtils<GrainModel>();
            var modelNisma = jsonUtils.Load("models/nisma_format.json");
            Console.WriteLine("Chargement réussi : " + modelNisma.Grains.Count + " grains, " + modelNisma.Behaviours.Count + " comportements, " + modelNisma.Fields.Count + " champs");
            foreach (var g in modelNisma.Grains)
            {
                Console.WriteLine("  Grain : " + g.Name + " (id=" + g.Id + ", color=" + g.Color + ")");
            }
            foreach (var b in modelNisma.Behaviours)
            {
                Console.WriteLine("  Comportement : " + b.Name + " (mainReactiveId=" + b.MainReactiveId + ")");
            }
            foreach (var f in modelNisma.Fields)
            {
                Console.WriteLine("  Champ : " + f.Name + " (id=" + f.Id + ", speed=" + f.Speed + ")");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erreur de chargement : " + ex.Message);
            Console.WriteLine("Création du fichier de test nécessaire.");
        }
        Console.WriteLine();

        // Test 6 : crée un modèle complet et le sauvegarde
        Console.WriteLine("=== Test 6 : Modèle complet sauvegardé ===");
        var model = CreateSampleModel();
        string modelJson = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText("models/sample_test.json", modelJson);
        Console.WriteLine("Modèle sauvegardé dans models/sample_test.json");
        Console.WriteLine();

        // Test 7 : héritage de types + comportements génériques
        Console.WriteLine("=== Test 7 : Héritage de types + comportements génériques ===");
        var meta = new MetaModel();

        // Types avec héritage
        meta.AddType(new TypeDecl { Name = "lymphocyte", IsAbstract = true });
        meta.AddType(new TypeDecl { Name = "LA", Parent = "lymphocyte", IsAbstract = false });
        meta.AddType(new TypeDecl { Name = "LB", Parent = "lymphocyte", IsAbstract = false });
        meta.AddType(new TypeDecl { Name = "CelluleCible", IsAbstract = true });
        meta.AddType(new TypeDecl { Name = "A", Parent = "CelluleCible", IsAbstract = false });
        meta.AddType(new TypeDecl { Name = "B", Parent = "CelluleCible", IsAbstract = false });
        meta.AddType(new TypeDecl { Name = "C", Parent = "CelluleCible", IsAbstract = false });

        // Comportement générique : Lymphocyte → CelluleCible
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
                    AllowedDirections = new() { "Left", "Up", "Right", "Down" }
                }
            }
        });

        // Génération du modèle concret
        var model7 = meta.Generate();
        Console.WriteLine("Types concrets générés :");
        foreach (var g in model7.Grains)
        {
            Console.WriteLine("  Grain : " + g.Name + " (id=" + g.Id + ")");
        }

        Console.WriteLine("Comportements générés : " + model7.Behaviours.Count);
        foreach (var b in model7.Behaviours)
        {
            Console.WriteLine("  Comportement : " + b.Name);
            foreach (var r in b.Reaction)
            {
                Console.WriteLine("    Réaction : reactiveId=" + r.ReactiveId + ", productId=" + r.ProductId);
            }
        }

        // Sauvegarde
        string json7 = JsonSerializer.Serialize(model7, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText("models/hierarchy_test.json", json7);
        Console.WriteLine("\nModèle sauvegardé dans models/hierarchy_test.json");
        Console.WriteLine();
    }

    private static GrainModel CreateSampleModel()
    {
        var grains = new List<Grain>
        {
            new(0, "Agent", "DarkGoldenRod", 0, "square-full", 0.0) { FieldProductions = { {0, 1f} }, FieldInfluences = { {0, 1f} } },
            new(1, "Agent 1", "Teal", 0, "square-full", 0.0) { FieldProductions = { {0, -0.25f} }, FieldInfluences = { {0, 0.55f} } }
        };

        var behaviours = new List<Behaviour>
        {
            new("Cristallisation", 0, 1, -1)
            {
                AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 50),
                FieldPredicates = { new FieldPredicate(0, new Predicate(Operator.GreaterThan, 0.00001)) },
                Reaction = { new Reaction { ReactiveId = 0, ProductId = 1, SourceReactive = -1 } }
            }
        };

        var fields = new List<Field>
        {
            new(0, "Champ", "OrangeRed", 0.8f, 10, new() { "Left", "Up", "Right", "Down" })
            {
                Formula = "(1+sin(step/25))/2"
            }
        };

        return new GrainModel
        {
            Name = "Modèle de test",
            Description = "Modèle créé pour tester la sérialisation NISMA",
            Grains = grains,
            Behaviours = behaviours,
            Fields = fields
        };
    }
}
