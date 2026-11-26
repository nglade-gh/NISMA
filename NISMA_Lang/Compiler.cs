using System.Globalization;
using NISMA_Core;

namespace NISMA_Lang;

public sealed class CompilationResult
{
    public required NbdModel Model { get; init; }
    public List<string> Notes { get; } = new();
    public int BehaviourCount => Model.Model.Behaviours.Count;
    public int GrainCount => Model.Model.Grains.Count;
}

/// <summary>
/// Compile un script en modèle NetBioDyn. Trois phases : résolution des noms, expansion
/// des comportements polymorphes, émission du modèle.
/// </summary>
public sealed class Compiler
{
    private static readonly string[] Directions8 =
        { "Left", "Up", "Right", "Down", "LeftUp", "RightUp", "RightDown", "LeftDown" };

    private static readonly string[] Directions4 = { "Left", "Up", "Right", "Down" };

    /// <summary>Palette vive de Centyllion : les couleurs ternes rendaient la grille sale.</summary>
    private static readonly string[] VividColors =
    {
        "Red", "Orange", "Gold", "Chartreuse", "Lime", "SpringGreen",
        "Cyan", "DeepSkyBlue", "DodgerBlue", "RoyalBlue", "BlueViolet", "DarkViolet",
        "Magenta", "DeepPink", "Crimson", "Chocolate", "Violet",
    };

    private readonly Dictionary<string, string> _colors = new(StringComparer.Ordinal);
    private readonly HashSet<string> _usedColors = new(StringComparer.Ordinal);
    private Random _colorRandom = new(0);

    private sealed class TypeInfo
    {
        public required string Name { get; init; }
        public string? Parent { get; init; }
        public bool IsAbstract { get; init; }
        public List<string> Tags { get; } = new();
        public int Id { get; set; } = -1;
        public Dictionary<string, string> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool IsConcrete => !IsAbstract;
    }

    private readonly Script _script;
    private readonly Dictionary<string, TypeInfo> _types = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _fieldIds = new(StringComparer.Ordinal);
    private readonly List<string> _notes = new();
    private readonly HashSet<string> _tags = new(StringComparer.Ordinal);

    private int _gridWidth = 50;
    private int _gridHeight = 50;
    private int _seed;
    private bool _sequential;

    private Compiler(Script script)
    {
        _script = script;
    }

    public static CompilationResult Compile(Script script)
    {
        var compiler = new Compiler(script);
        var model = compiler.Build();
        var result = new CompilationResult { Model = model };
        result.Notes.AddRange(compiler._notes);
        return result;
    }

    // -----------------------------------------------------------------------
    // Construction
    // -----------------------------------------------------------------------

    private NbdModel Build()
    {
        var model = new NbdModel();
        var root = model.Model;

        RegisterDeclarations(model, root);
        AddProductions(root);
        AddBehaviours(root);
        AddStart(model);
        AddModifiers(root);

        root.Grains.Sort((a, b) => a.Id.CompareTo(b.Id));
        root.Fields.Sort((a, b) => a.Id.CompareTo(b.Id));

        return model;
    }

    private void RegisterDeclarations(NbdModel model, NISMA_Core.Model root)
    {
        string? modelName = null;
        string? modelDescription = null;

        // les tags sont collectés d'abord : l'ordre de déclaration ne doit pas obliger à
        // écrire « tag Active » avant l'agent qui le porte.
        foreach (var tag in _script.Declarations.OfType<TagDecl>()) _tags.Add(tag.Name);

        // la graine est lue avant les couleurs : celles-ci sont tirées au sort de façon
        // reproductible, comme les agents de l'état initial
        foreach (var seed in _script.Declarations.OfType<SeedDecl>()) _seed = seed.Seed;
        _colorRandom = new Random(unchecked(_seed * 7919 + 13));
        AssignColors();

        foreach (var decl in _script.Declarations)
        {
            switch (decl)
            {
                case NISMA_Lang.ModelInfo modelInfo:
                    modelName = modelInfo.Name;
                    modelDescription = modelInfo.Description;
                    break;
                case GridDecl grid:
                    _gridWidth = grid.Width;
                    _gridHeight = grid.Height;
                    break;
                case UpdateDecl update:
                    _sequential = update.Sequential;
                    break;
                case SeedDecl seed:
                    _seed = seed.Seed;
                    break;
                case TagDecl:
                    break;
                case AbstractDecl abs:
                    if (_types.ContainsKey(abs.Name))
                        throw new ScriptException(abs.Line, $"type « {abs.Name} » déclaré deux fois");
                    _types[abs.Name] = new TypeInfo { Name = abs.Name, Parent = abs.Parent, IsAbstract = true };
                    break;
                case ConcreteDecl concrete:
                    if (_types.ContainsKey(concrete.Name))
                        throw new ScriptException(concrete.Line, $"agent « {concrete.Name} » déclaré deux fois");
                    var info = new TypeInfo { Name = concrete.Name, Parent = concrete.Parent, IsAbstract = false };
                    foreach (var tag in concrete.Tags)
                    {
                        if (!_tags.Contains(tag))
                            throw new ScriptException(concrete.Line, $"tag « {tag} » utilisé mais jamais déclaré");
                        info.Tags.Add(tag);
                    }

                    _types[concrete.Name] = info;
                    break;
                case AgentDecl agent:
                    if (_types.ContainsKey(agent.Name))
                        throw new ScriptException(agent.Line, $"agent « {agent.Name} » déclaré deux fois");
                    var plain = new TypeInfo { Name = agent.Name, Parent = null, IsAbstract = false };
                    foreach (var (key, value) in agent.Attributes) plain.Attributes[key] = value;
                    _types[agent.Name] = plain;
                    break;
                case FieldDecl field:
                    if (_fieldIds.ContainsKey(field.Name))
                        throw new ScriptException(field.Line, $"champ « {field.Name} » déclaré deux fois");
                    var fieldId = _fieldIds.Count;
                    _fieldIds[field.Name] = fieldId;
                    var directions = field.Directions == 4 ? Directions4 : Directions8;
                    root.Fields.Add(Field.FromPhysical(fieldId, field.Name, _colors[field.Name],
                        (float)field.Diffusion, field.HalfLife, directions.ToList()));
                    if (field.Opacity != 100)
                        root.Fields[^1].Opacity = field.Opacity;
                    break;
            }
        }

        // hiérarchie : parents connus, pas de cycle, un seul parent
        foreach (var info in _types.Values)
        {
            if (info.Parent is null) continue;
            if (!_types.ContainsKey(info.Parent))
                throw new ScriptException(0, $"parent inconnu « {info.Parent} » pour « {info.Name} »");
        }

        // identifiants : ordre de déclaration des agents concrets
        var nextId = 0;
        foreach (var decl in _script.Declarations)
        {
            var name = decl switch
            {
                AgentDecl a => a.Name,
                ConcreteDecl c => c.Name,
                _ => null,
            };
            if (name is null) continue;
            var info = _types[name];
            info.Id = nextId++;
            var grain = new Grain(info.Id, info.Name, _colors[name]);
            ApplyAttributes(grain, info.Attributes);
            root.Grains.Add(grain);
            _grains.Add(grain);
        }

        if (root.Grains.Count == 0)
            throw new ScriptException(0, "aucun agent déclaré : une simulation doit avoir au moins un grain");

        if (modelName is not null) root.Name = modelName;
        if (modelDescription is not null) root.Description = modelDescription;
        if (_sequential) root.UpdatePlan = NISMA_Core.Model.SequentialUpdatePlan;

        model.Simulation.Settings = new SimulationSettings(_gridWidth == _gridHeight ? _gridWidth : 0);
        if (_gridWidth != _gridHeight)
        {
            model.Simulation.Settings.Size = 0;
            model.Simulation.Settings.CustomWidth = _gridWidth;
            model.Simulation.Settings.CustomHeight = _gridHeight;
        }
    }

    /// <summary>
    /// Une couleur explicite est retenue ; une couleur absente est tirée dans la palette
    /// vive, en évitant celles déjà prises — la règle de Centyllion pour un agent neuf.
    /// </summary>
    private void AssignColors()
    {
        foreach (var declaration in _script.Declarations)
        {
            var (name, explicitColor) = declaration switch
            {
                AgentDecl agent => (agent.Name, agent.Color),
                ConcreteDecl concrete => (concrete.Name, concrete.Color),
                FieldDecl field => (field.Name, field.Color),
                _ => (null, null),
            };
            if (name is null) continue;

            if (explicitColor is not null)
            {
                var color = CanonicalColor(explicitColor);
                _colors[name] = color;
                _usedColors.Add(color);
            }
        }

        foreach (var declaration in _script.Declarations)
        {
            var name = declaration switch
            {
                AgentDecl agent => agent.Name,
                ConcreteDecl concrete => concrete.Name,
                FieldDecl field => field.Name,
                _ => null,
            };
            if (name is null || _colors.ContainsKey(name)) continue;
            var color = AvailableColor();
            _colors[name] = color;
            _usedColors.Add(color);
        }
    }

    private string AvailableColor()
    {
        var free = VividColors.Where(c => !_usedColors.Contains(c)).ToList();
        if (free.Count > 0) return free[_colorRandom.Next(free.Count)];
        return VividColors[_colorRandom.Next(VividColors.Length)];
    }

    private void ApplyAttributes(Grain grain, Dictionary<string, string> attributes)
    {
        foreach (var (key, value) in attributes)
        {
            switch (key)
            {
                case "mob":
                    grain.MovementProbability = (float)Parse(value);
                    break;
                case "life":
                    grain.HalfLife = (int)Parse(value);
                    break;
                case "icon":
                    grain.Icon = value;
                    break;
                case "size":
                    grain.Size = (float)Parse(value);
                    break;
                case "desc":
                    grain.Description = value;
                    break;
                case "walls":
                    grain.BoundaryConditions = new BoundaryConditions(false, false, false, false);
                    break;
                default:
                    throw new ScriptException(0, $"attribut d'agent inconnu « {key} »");
            }
        }
    }

    private void AddProductions(NISMA_Core.Model root)
    {
        foreach (var decl in _script.Declarations.OfType<ProductionDecl>())
        {
            if (!_fieldIds.TryGetValue(decl.Field, out var fieldId))
                throw new ScriptException(decl.Line, $"champ « {decl.Field} » inconnu");
            var grain = ConcreteGrain(decl.Agent, decl.Line);
            var value = (float)decl.Value;
            switch (decl.Verb)
            {
                case "produces":
                    grain.FieldProductions[fieldId] = value;
                    break;
                case "absorbs":
                    grain.FieldInfluences[fieldId] = value;
                    break;
                case "lets":
                    grain.FieldPermeable ??= new Dictionary<int, float>();
                    grain.FieldPermeable[fieldId] = value;
                    break;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Comportements
    // -----------------------------------------------------------------------

    private void AddBehaviours(NISMA_Core.Model root)
    {
        var index = 0;
        foreach (var rule in _script.Rules)
        {
            var subjects = ConcreteSet(rule.Subject!, rule.Line);
            if (subjects.Count == 0)
                throw new ScriptException(rule.Line, $"aucun agent concret pour « {rule.Subject} »");

            int? productId = null;
            if (rule.Kind == RuleKind.Spawn)
                productId = subjects[0].Id;
            else if (rule.Product is not null)
            {
                var candidates = ConcreteSet(rule.Product, rule.Line);
                productId = CandidatesToId(rule.Product, candidates, rule.Line);
            }

            if (subjects.Count > 1 && rule.Neighbour is false && rule.Kind == RuleKind.Transform)
                _notes.Add($"ligne {rule.Line} : « {rule.Subject} » s'étend en {subjects.Count} comportements");

            foreach (var subject in subjects)
            {
                var name = rule.Label ?? (rule.Group is null ? DefaultName(rule, subject) : $"{rule.Group}/{index + 1}");
                int? mainReactive = rule.Neighbour || rule.Kind == RuleKind.Spawn ? null : subject.Id;
                int? mainProduct = rule.Neighbour || rule.Kind == RuleKind.Die ? null : productId;
                int? sourceReactive = rule.Neighbour || rule.Kind == RuleKind.Spawn ? null : 0;

                var behaviour = new Behaviour(name, mainReactive, mainProduct, sourceReactive)
                {
                    Id = index == 0 ? null : index,
                };
                if (Math.Abs(rule.Probability - 1.0) > 1e-9) behaviour.Probability = (float)rule.Probability;

                if (rule.Condition is not null) ApplyCondition(behaviour, rule.Condition, rule.Line);

                if (rule.Neighbour)
                {
                    behaviour.Reaction ??= new List<Reaction>();
                    behaviour.Reaction.Add(new Reaction(subject.Id, productId) { SourceReactive = 0 });
                }

                if (rule.Actions.Count > 0)
                    behaviour.Actions = string.Join(", ", rule.Actions.Select(a => CompileAction(a, rule.Line)));

                root.Behaviours.Add(behaviour);
                index++;
            }
        }
    }

    private static string DefaultName(Rule rule, TypeInfo subject) => rule.Kind switch
    {
        RuleKind.Spawn => $"{subject.Name} spawns",
        RuleKind.Die => $"{subject.Name} dies",
        _ => $"{subject.Name} -> {rule.Product}",
    };

    private void ApplyCondition(Behaviour behaviour, Cond condition, int line)
    {
        if (TryStructured(condition, out var counts, out var fields, out var age, out var error))
        {
            if (counts.Count > 0) behaviour.AgentCountPredicates = counts;
            if (fields.Count > 0) behaviour.FieldPredicates = fields;
            if (age is not null) behaviour.AgePredicate = age;
            return;
        }

        if (error is not null) throw new ScriptException(line, error);
        behaviour.Formula = ToFormula(condition, line);
    }

    private bool TryStructured(Cond condition, out List<AgentCountPredicate> counts,
        out List<FieldPredicate> fields, out Predicate? age, out string? error)
    {
        counts = new List<AgentCountPredicate>();
        fields = new List<FieldPredicate>();
        age = null;
        error = null;

        var flat = condition is AndCond and ? and.Items : new List<Cond> { condition };
        foreach (var item in flat)
        {
            switch (item)
            {
                case CountCond count:
                    if (!TryCountId(count, out var grainId, out _))
                    {
                        // type à plusieurs agents concrets : la somme des comptages passe
                        // par une formule, plus bas
                        return false;
                    }

                    counts.Add(new AgentCountPredicate(grainId, OperatorName(count.Op), count.Value));
                    break;
                case MeasureCond measure when _fieldIds.ContainsKey(measure.Name):
                    if (measure.Near)
                    {
                        error = "« in near » n'est pas encore implémenté : écrivez la formule au niveau mécanique";
                        return false;
                    }

                    fields.Add(new FieldPredicate
                    {
                        First = _fieldIds[measure.Name],
                        Second = new Predicate { Op = OperatorName(measure.Op), Constant = (float)measure.Value },
                    });
                    break;
                case MeasureCond { Name: "age" } measure:
                    if (age is not null)
                    {
                        error = "deux conditions d'âge dans le même comportement : non représentable, passez par une formule";
                        return false;
                    }

                    age = new Predicate { Op = OperatorName(measure.Op), Constant = (int)measure.Value };
                    break;
                default:
                    // or / not / step / nothing / only : pas de forme structurée
                    return false;
            }
        }

        return error is null;
    }

    private bool TryCountId(CountCond count, out int grainId, out string? error)
    {
        error = null;
        if (count.Any)
        {
            grainId = -1;
            return true;
        }

        var set = ConcreteSet(count.Type!, 0);
        if (set.Count == 1)
        {
            grainId = set[0].Id;
            return true;
        }

        grainId = 0;
        error = $"comptage sur « {count.Type} », qui désigne {set.Count} agents concrets : " +
                "écrivez la somme des comptages au niveau mécanique";
        return false;
    }

    private string ToFormula(Cond condition, int line) => condition switch
    {
        AndCond and => string.Join(" && ", and.Items.Select(c => "(" + ToFormula(c, line) + ")")),
        OrCond or => string.Join(" || ", or.Items.Select(c => "(" + ToFormula(c, line) + ")")),
        NotCond not => "!(" + ToFormula(not.Inner, line) + ")",
        CountCond count => $"{CountExpression(count, line)}{count.Op}{count.Value}",
        NothingCond => "count(-1)==0",
        OnlyCond only => $"hasOnly({{{string.Join(",", only.Types.Select(t => ConcreteGrain(t, line).Id))}}})",
        MeasureCond measure => MeasureFormula(measure, line),
        _ => throw new ScriptException(line, "condition non compilable"),
    };

    /// <summary>count(id) pour un agent, count(-1) pour « any », somme pour un type.</summary>
    private string CountExpression(CountCond count, int line)
    {
        if (count.Any) return "count(-1)";
        var set = ConcreteSet(count.Type!, line);
        if (set.Count == 1) return $"count({set[0].Id})";
        return "(" + string.Join("+", set.Select(t => $"count({t.Id})")) + ")";
    }

    private string MeasureFormula(MeasureCond measure, int line)
    {
        if (_fieldIds.TryGetValue(measure.Name, out var fieldId))
        {
            if (measure.Near) throw new ScriptException(line, "« in near » n'est pas encore implémenté");
            return $"field({fieldId}){measure.Op}{Number(measure.Value)}";
        }

        return measure.Name switch
        {
            "age" => $"age{measure.Op}{Number(measure.Value)}",
            "step" => $"step{measure.Op}{Number(measure.Value)}",
            _ => throw new ScriptException(line, $"« {measure.Name} » n'est ni un champ, ni age, ni step"),
        };
    }

    // -----------------------------------------------------------------------
    // État initial et modificateurs
    // -----------------------------------------------------------------------

    private void AddStart(NbdModel model)
    {
        var cells = new List<int>(_gridWidth * _gridHeight);
        for (var i = 0; i < _gridWidth * _gridHeight; i++) cells.Add(-1);

        var random = new Random(_seed);
        foreach (var item in _script.Start)
        {
            switch (item)
            {
                case StartRandom start:
                {
                    var grain = ConcreteGrain(start.Agent, start.Line);
                    var free = cells.Select((value, index) => (value, index))
                        .Where(pair => pair.value < 0).Select(pair => pair.index).ToList();
                    if (start.Count > free.Count)
                        throw new ScriptException(start.Line,
                            $"{start.Count} agents demandés dans une grille de {free.Count} cellules libres");
                    for (var i = 0; i < start.Count; i++)
                    {
                        var pick = random.Next(free.Count);
                        cells[free[pick]] = grain.Id;
                        free.RemoveAt(pick);
                    }

                    break;
                }
                case StartAt start:
                {
                    var grain = ConcreteGrain(start.Agent, start.Line);
                    if (start.X < 0 || start.Y < 0 || start.X >= _gridWidth || start.Y >= _gridHeight)
                        throw new ScriptException(start.Line, $"position ({start.X},{start.Y}) hors de la grille");
                    cells[start.Y * _gridWidth + start.X] = grain.Id;
                    break;
                }
            }
        }

        model.Simulation.Agents = cells;
    }

    private void AddModifiers(NISMA_Core.Model root)
    {
        var index = 0;
        foreach (var modifier in _script.Modifiers)
        {
            var formula = modifier.Trigger switch
            {
                "every" => $"step % {modifier.Period} == 0",
                "at" => $"step == {modifier.Period}",
                "when" => ToFormula(modifier.Condition!, modifier.Line),
                _ => throw new ScriptException(modifier.Line, "déclencheur inconnu"),
            };

            root.Modifiers.Add(new SimulationModifier($"modifier/{index + 1}", formula,
                CompileAction(modifier.Action, modifier.Line)) { Id = index });
            index++;
        }
    }

    private string CompileAction(string text, int line)
    {
        var tokens = Lexer.Tokenize(line, text);
        if (tokens.Count == 1)
        {
            return tokens[0].Text switch
            {
                "pause" => "Pause()",
                "capture" => "Capture()",
                _ => throw new ScriptException(line, $"action inconnue « {tokens[0].Text} »"),
            };
        }

        // set FIELD VALUE  (sur la cellule courante)
        if (tokens[0].Is("set") && tokens.Count == 3)
        {
            if (!_fieldIds.TryGetValue(tokens[1].Text, out var fieldId))
                throw new ScriptException(line, $"champ « {tokens[1].Text} » inconnu");
            return $"SetField({fieldId}, 0, 0, {Number(Parse(tokens[2].Text, line))})";
        }

        // add N Agent in square (x1,y1)-(x2,y2)
        if (tokens[0].Is("add") && tokens.Count >= 9)
        {
            var count = (int)Parse(tokens[1].Text, line);
            var grain = ConcreteGrain(tokens[2].Text, line);
            // le premier nombre est le compte, les quatre suivants le rectangle
            var numbers = tokens.Where(t => t.Kind == Lexer.TokKind.Number).Select(t => (int)t.Number).Skip(1).ToList();
            if (numbers.Count != 4)
                throw new ScriptException(line, "attendu un rectangle (x1,y1)-(x2,y2)");
            return $"AddRect({grain.Id}, {count}, {numbers[0]}, {numbers[1]}, {numbers[2]}, {numbers[3]})";
        }

        throw new ScriptException(line, $"action non implémentée dans le prototype : « {text} »");
    }

    // -----------------------------------------------------------------------
    // Résolution des noms
    // -----------------------------------------------------------------------

    private List<TypeInfo> ConcreteSet(string name, int line)
    {
        if (_types.TryGetValue(name, out var info))
        {
            var members = _types.Values.Where(t => t.IsConcrete && DescendsFrom(t, name)).ToList();
            if (info.IsConcrete) members.Insert(0, info);
            return members;
        }

        if (_tags.Contains(name))
            return _types.Values.Where(t => t.IsConcrete && t.Tags.Contains(name)).ToList();

        if (line == 0) return new List<TypeInfo>();
        throw new ScriptException(line, $"« {name} » n'est ni un agent, ni un type, ni un tag déclaré");
    }

    private bool DescendsFrom(TypeInfo candidate, string ancestor)
    {
        var current = candidate.Parent;
        var guard = 0;
        while (current is not null && guard++ < 32)
        {
            if (current == ancestor) return true;
            current = _types.TryGetValue(current, out var parent) ? parent.Parent : null;
        }

        return false;
    }

    private int CandidatesToId(string name, List<TypeInfo> candidates, int line)
    {
        if (candidates.Count == 0)
            throw new ScriptException(line, $"le produit « {name} » ne désigne aucun agent concret");
        if (candidates.Count > 1)
            throw new ScriptException(line,
                $"« {name} » est ambigu : {string.Join(", ", candidates.Select(c => c.Name))} " +
                "répondent tous à ce nom. Nommez l'agent voulu.");
        return candidates[0].Id;
    }

    private Grain ConcreteGrain(string name, int line)
    {
        if (_types.TryGetValue(name, out var exact) && exact.IsConcrete)
            return _grains.First(g => g.Id == exact.Id);

        var set = ConcreteSet(name, line);
        if (set.Count == 0) throw new ScriptException(line, $"« {name} » ne désigne aucun agent concret");
        if (set.Count > 1)
            throw new ScriptException(line,
                $"« {name} » désigne {set.Count} agents concrets ({string.Join(", ", set.Select(c => c.Name))}) : " +
                "précisez le nom");
        var info = set[0];
        var grain = _grains.FirstOrDefault(g => g.Id == info.Id);
        return grain ?? throw new ScriptException(line, $"grain « {name} » introuvable");
    }

    private readonly List<Grain> _grains = new();

    // -----------------------------------------------------------------------

    private static string OperatorName(string op) => op switch
    {
        "==" or "=" => "Equals",
        "!=" => "NotEquals",
        "<" => "LessThan",
        "<=" => "LessThanOrEquals",
        ">" => "GreaterThan",
        ">=" => "GreaterThanOrEquals",
        _ => throw new ScriptException(0, $"opérateur inconnu « {op} »"),
    };

    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static double Parse(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    private static double Parse(string text, int line)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new ScriptException(line, $"nombre attendu, trouvé « {text} »");
        return value;
    }

    private static string CanonicalColor(string color)
    {
        if (color.Length == 0) return "Grey";
        return char.ToUpperInvariant(color[0]) + color[1..];
    }
}
