namespace nbd.sub_classes;using System.Collections.Generic;


/// <summary>
/// Production d'un champ sur un grain (champId -> valeur de production)
/// </summary>
public class FieldProduction
{
    public Dictionary<int, float> Productions { get; set; } = new();
}

/// <summary>
/// Influence d'un champ sur un grain (champId -> valeur d'influence)
/// </summary>
public class FieldInfluences
{
    public Dictionary<int, float> Influences { get; set; } = new();
}

/// <summary>
/// Perméabilité d'un champ sur un grain (champId -> valeur de perméabilité)
/// </summary>
public class FieldPermeable
{
    public Dictionary<int, float> Permeable { get; set; } = new();
}
