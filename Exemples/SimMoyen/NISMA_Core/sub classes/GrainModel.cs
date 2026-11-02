using System.Collections.Generic;using System.Linq;

using System.Text.Json.Serialization;

namespace nbd.sub_classes;

/// <summary>
/// Modèle de simulation complet (grains, comportements, champs)
/// </summary>
public class GrainModel
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("grains")] public List<Grain> Grains { get; set; } = new();
    [JsonPropertyName("behaviours")] public List<Behaviour> Behaviours { get; set; } = new();
    [JsonPropertyName("fields")] public List<Field> Fields { get; set; } = new();

    public GrainModel() { }

    public GrainModel(string name, string description, List<Grain> grains, List<Behaviour> behaviours, List<Field> fields)
    {
        Name = name;
        Description = description;
        Grains = grains;
        Behaviours = behaviours;
        Fields = fields;
    }

    public Grain? GrainForId(int id)
    {
        return Grains.Find(g => g.Id == id);
    }

    public Field? FieldForId(int id)
    {
        return Fields.Find(f => f.Id == id);
    }

    public bool HasField(int id)
    {
        return Fields.Exists(f => f.Id == id);
    }
}

// ─── Nouveau : déclarations de types et comportements génériques ─────────────────

/// <summary>
/// Déclaration d'un type (agent) avec héritage (abstract + enfants concrets)
/// </summary>
public class TypeDecl
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("parent")] public string? Parent { get; set; }
    [JsonPropertyName("isAbstract")] public bool IsAbstract { get; set; }
    [JsonPropertyName("children")] public List<string> ChildNames { get; set; } = new();
}

/// <summary>
/// Déclaration d'un comportement générique (s'applique à une catégorie de types)
/// </summary>
public class BehaviourDecl
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("categories")] public string[] Categories { get; set; } = [];
    [JsonPropertyName("reactions")] public List<Reaction> Reactions { get; set; } = new();
}

/// <summary>
/// Modèle métier : types avec héritage + comportements génériques
/// </summary>
public class MetaModel
{
    [JsonPropertyName("types")] public List<TypeDecl> Types { get; set; } = new();
    [JsonPropertyName("behaviours")] public List<BehaviourDecl> Behaviours { get; set; } = new();

    public MetaModel() { }

    public MetaModel(List<TypeDecl> types, List<BehaviourDecl> behaviours)
    {
        Types = types;
        Behaviours = behaviours;
    }

    public void AddType(TypeDecl type)
    {
        Types.Add(type);
        // Ajouter ce type à la liste des enfants de son parent
        if (type.Parent != null)
        {
            var parent = Types.Find(t => t.Name == type.Parent);
            if (parent != null)
            {
                parent.ChildNames.Add(type.Name);
            }
        }
    }

    public void AddBehaviour(BehaviourDecl behaviour)
    {
        Behaviours.Add(behaviour);
    }

    /// <summary>
    /// Génère unGrainModel concret à partir du meta-modèle.
    /// 1. Résolution de la hiérarchie des types
    /// 2. Énumération des types concrets (non-abstract)
    /// 3. Génération des comportements concrets par combinaison des catégories
    /// </summary>
    public GrainModel Generate()
    {
        // 1. Construire un dictionnaire name → TypeDecl
        var typeMap = Types.ToDictionary(t => t.Name, t => t);

        // 2. Résolution récursive pour trouver tous les types concrets
        var concreteTypes = new List<string>();
        foreach (var t in Types)
        {
            if (!t.IsAbstract && !concreteTypes.Contains(t.Name))
            {
                EnumerateConcrete(t.Name, typeMap, concreteTypes);
            }
        }

        // 3. Générer les comportements concrets à partir des génériques
        var concreteBehaviours = new List<Behaviour>();
        foreach (var bd in Behaviours)
        {
            // Pour chaque paire de catégories, générer les combinaisons
            for (int i = 0; i < bd.Categories.Length; i++)
            {
                for (int j = i + 1; j < bd.Categories.Length; j++)
                {
                    var cat1 = bd.Categories[i];
                    var cat2 = bd.Categories[j];

                    // Trouver tous les types concrets pour chaque catégorie
                    var concrete1 = GetConcreteTypes(cat1, typeMap);
                    var concrete2 = GetConcreteTypes(cat2, typeMap);

                    // Générer toutes les combinaisons
                    foreach (var c1 in concrete1)
                    {
                        foreach (var c2 in concrete2)
                        {
                            concreteBehaviours.Add(CreateConcreteBehaviour(
                                bd, c1, c2));
                        }
                    }
                }
            }
        }

        // 4. Construire le GrainModel
        var grains = concreteTypes.Select((name, index) =>
        {
            var type = typeMap[name];
            return new Grain
            {
                Id = index,
                Name = type.Name,
                Color = "red",
                Icon = "square-full",
                Size = 1.0,
                HalfLife = 0,
                MovementProbability = 0.0,
                AllowedDirection = new() { "Left", "Up", "Right", "Down" }
            };
        }).ToList();

        // 5. Assigner des IDs uniques aux comportements (non stocké dans le modèle)
        for (int i = 0; i < concreteBehaviours.Count; i++)
        {
            // IDs conservés en mémoire pour la méta-modélisation uniquement
        }

        return new GrainModel
        {
            Name = "Modèle généré",
            Description = "Modèle généré à partir du meta-modèle NISMA",
            Grains = grains,
            Behaviours = concreteBehaviours,
            Fields = new List<Field>()
        };
    }

    /// <summary>
    /// Trouve tous les types concrets (non-abstract) descendants d'un type.
    /// </summary>
    private List<string> GetConcreteTypes(string typeName, Dictionary<string, TypeDecl> typeMap)
    {
        var result = new List<string>();
        var type = typeMap.TryGetValue(typeName, out var t) ? t : null;

        if (type == null) return result;

        // Si le type est concret, l'ajouter
        if (!type.IsAbstract && !result.Contains(type.Name))
        {
            result.Add(type.Name);
        }

        // Récurse sur les enfants
        foreach (var childName in type.ChildNames)
        {
            var child = typeMap.TryGetValue(childName, out var c) ? c : null;
            if (child != null)
            {
                result.AddRange(GetConcreteTypes(childName, typeMap));
            }
        }

        return result;
    }

    /// <summary>
    /// Remplit récursivement la liste des types concrets.
    /// </summary>
    private void EnumerateConcrete(string typeName, Dictionary<string, TypeDecl> typeMap, List<string> concreteList)
    {
        var type = typeMap.TryGetValue(typeName, out var t) ? t : null;
        if (type == null || concreteList.Contains(typeName)) return;

        if (!type.IsAbstract)
        {
            concreteList.Add(typeName);
        }

        foreach (var childName in type.ChildNames)
        {
            EnumerateConcrete(childName, typeMap, concreteList);
        }
    }

    /// <summary>
    /// Crée un comportement concret à partir d'un générique et de deux types concrets.
    /// </summary>
    private Behaviour CreateConcreteBehaviour(BehaviourDecl bd, string c1, string c2)
    {
        var behaviour = new Behaviour
        {
            Name = $"{c1}-{c2}",
            Probability = 1.0,
            AgePredicate = new Predicate(Operator.GreaterThanOrEquals, 0),
            Reaction = new List<Reaction>(),
            MainReactiveId = c1 switch { "LA" => 0, "LB" => 1, "A" => 2, "B" => 3, "C" => 4, _ => 0 },
            MainProductId = c2 switch { "A" => 2, "B" => 3, "C" => 4, _ => 1 }
        };

        // Réaction par défaut (générique)
        if (bd.Reactions.Count > 0)
        {
            var firstReaction = bd.Reactions[0];
            var reaction = new Reaction
            {
                ReactiveId = behaviour.MainReactiveId,
                ProductId = behaviour.MainProductId,
                SourceReactive = -1,
                AllowedDirection = new() { "Left", "Up", "Right", "Down" }
            };
            behaviour.Reaction.Add(reaction);
        }

        return behaviour;
    }
}
