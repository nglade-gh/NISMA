namespace NISMA_Lang;

/// <summary>Erreur de compilation, toujours rattachée à une ligne du script.</summary>
public sealed class ScriptException : Exception
{
    public int Line { get; }

    /// <summary>Message seul, sans le préfixe de ligne : c'est ce qu'affiche un IDE.</summary>
    public string Reason { get; }

    public ScriptException(int line, string message) : base($"ligne {line}: {message}")
    {
        Line = line;
        Reason = message;
    }
}

// ---------------------------------------------------------------------------
// Déclarations
// ---------------------------------------------------------------------------

public abstract record Declaration(int Line);

public sealed record ModelInfo(int Line, string Name, string? Description) : Declaration(Line);

public sealed record GridDecl(int Line, int Width, int Height) : Declaration(Line);

public sealed record UpdateDecl(int Line, bool Sequential) : Declaration(Line);

public sealed record SeedDecl(int Line, int Seed) : Declaration(Line);

/// <summary>Grain concret déclaré sans parent.</summary>
public sealed record AgentDecl(int Line, string Name, string? Color, IReadOnlyDictionary<string, string> Attributes)
    : Declaration(Line);

/// <summary>Type abstrait : jamais écrit dans le fichier produit.</summary>
public sealed record AbstractDecl(int Line, string Name, string? Parent) : Declaration(Line);

/// <summary>Grain concret sous-type d'un type abstrait, éventuellement porteur de tags.</summary>
public sealed record ConcreteDecl(int Line, string Name, string Parent, IReadOnlyList<string> Tags, string? Color)
    : Declaration(Line);

public sealed record TagDecl(int Line, string Name) : Declaration(Line);

public sealed record FieldDecl(int Line, string Name, string? Color, double Diffusion, int HalfLife, int Directions, int Opacity)
    : Declaration(Line);

/// <summary>Production, influence ou perméabilité d'un champ par un grain.</summary>
public sealed record ProductionDecl(int Line, string Agent, string Verb, string Field, double Value)
    : Declaration(Line);

// ---------------------------------------------------------------------------
// Conditions
// ---------------------------------------------------------------------------

public abstract record Cond;

public sealed record AndCond(IReadOnlyList<Cond> Items) : Cond;

public sealed record OrCond(IReadOnlyList<Cond> Items) : Cond;

public sealed record NotCond(Cond Inner) : Cond;

/// <summary>Comptage de voisins : <c>finds 3 Cell</c>, <c>finds Cell &lt; 2</c>, <c>finds any</c>.</summary>
public sealed record CountCond(bool Any, string? Type, string Op, int Value) : Cond;

/// <summary><c>finds nothing</c> : aucun voisin occupé.</summary>
public sealed record NothingCond : Cond;

/// <summary><c>finds only Cell, Virus</c> : aucun type de voisin hors de la liste.</summary>
public sealed record OnlyCond(IReadOnlyList<string> Types) : Cond;

/// <summary>Rythme régulier : <c>every 3 steps</c> → <c>step % 3 == 0</c>.</summary>
public sealed record PeriodCond(int Period) : Cond;

/// <summary>Grandeur comparée : champ, âge ou pas. <paramref name="Near"/> = chez un voisin.</summary>
public sealed record MeasureCond(string Name, string Op, double Value, bool Near) : Cond;

// ---------------------------------------------------------------------------
// Règles et modificateurs
// ---------------------------------------------------------------------------

public enum RuleKind
{
    Spawn,
    Die,
    Transform,
}

public sealed record Rule(
    int Line,
    string? Label,
    string? Group,
    string? Subject,
    bool Neighbour,
    string? Product,
    RuleKind Kind,
    Cond? Condition,
    double Probability,
    IReadOnlyList<string> Actions);

public sealed record ModifierDecl(int Line, string Trigger, int Period, Cond? Condition, string Action);

public abstract record StartItem(int Line);

public sealed record StartRandom(int Line, int Count, string Agent) : StartItem(Line);

public sealed record StartAt(int Line, string Agent, int X, int Y) : StartItem(Line);

/// <summary>Un script analysé, prêt à être compilé.</summary>
public sealed class Script
{
    public List<Declaration> Declarations { get; } = new();
    public List<Rule> Rules { get; } = new();
    public List<ModifierDecl> Modifiers { get; } = new();
    public List<StartItem> Start { get; } = new();
}
