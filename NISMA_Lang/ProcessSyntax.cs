namespace NISMA_Lang;

// ---------------------------------------------------------------------------
// Processus : un agent décrit comme une suite d'états avec durée et effets.
// C'est la couche « procès temporo-spatial » du langage ; elle se compile en
// grains (un par état) et en comportements gardés par l'âge.
// ---------------------------------------------------------------------------

/// <summary>Ouverture d'un processus sur un agent : <c># process Cell</c>.</summary>
public sealed record ProcessDecl(int Line, string Agent) : Declaration(Line);

/// <summary>Un état du processus : <c>## Infected [Crimson]</c>.</summary>
public sealed record StateDecl(int Line, string Process, string Name, string? Color) : Declaration(Line);

/// <summary>Durée de séjour déclarée dans un état : <c>stays 4 steps</c>.</summary>
public sealed record StayDecl(int Line, string Process, string State, int Steps) : Declaration(Line);

public enum TransitionKind
{
    Becomes,
    Dies,
}

/// <summary>Sortie d'un état : <c>becomes Sick</c>, <c>dies after 6 steps</c>.</summary>
public sealed record TransitionDecl(
    int Line,
    string Process,
    string From,
    string? To,
    Cond? Condition,
    int? AfterSteps,
    TransitionKind Kind,
    int? Period = null) : Declaration(Line);

/// <summary>Effet permanent d'un état : production, influence, perméabilité, mobilité.</summary>
public sealed record StateEffectDecl(int Line, string Process, string State, string Kind, string Target, double Value)
    : Declaration(Line);

/// <summary>Action sur un voisin depuis un état : <c>a neighbour Cell becomes Infected</c>.</summary>
public sealed record StateReactionDecl(
    int Line,
    string Process,
    string State,
    string Neighbour,
    string To,
    Cond? Condition,
    int? Period = null) : Declaration(Line);

/// <summary>Une règle écrite dans un état, pour l'affichage des erreurs.</summary>
public static class ProcessNames
{
    public static string Qualified(string process, string state) => process + "." + state;
}
