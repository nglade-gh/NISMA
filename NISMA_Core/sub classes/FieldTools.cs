using System;
using System.Collections.Generic;

namespace NISMA_Core.sub_classes;

/// <summary>
/// Field production descriptor: field id -> amount produced.
/// </summary>
public class FieldProduction
{
    /// <summary>Field id.</summary>
    public int FieldId { get; set; }

    /// <summary>Production amount.</summary>
    public float Amount { get; set; }

    public FieldProduction()
    {
    }

    public FieldProduction(int fieldId, float amount)
    {
        FieldId = fieldId;
        Amount = amount;
    }

    public KeyValuePair<int, float> ToPair() => new KeyValuePair<int, float>(FieldId, Amount);
}

/// <summary>
/// Field influence descriptor: field id -> influence coefficient.
/// </summary>
public class FieldInfluence
{
    /// <summary>Field id.</summary>
    public int FieldId { get; set; }

    /// <summary>Influence coefficient.</summary>
    public float Coefficient { get; set; }

    public FieldInfluence()
    {
    }

    public FieldInfluence(int fieldId, float coefficient)
    {
        FieldId = fieldId;
        Coefficient = coefficient;
    }

    public KeyValuePair<int, float> ToPair() => new KeyValuePair<int, float>(FieldId, Coefficient);
}

/// <summary>
/// Field permeability descriptor: field id -> permeability coefficient.
/// </summary>
public class FieldPermeable
{
    /// <summary>Field id.</summary>
    public int FieldId { get; set; }

    /// <summary>Permeability coefficient.</summary>
    public float Coefficient { get; set; }

    public FieldPermeable()
    {
    }

    public FieldPermeable(int fieldId, float coefficient)
    {
        FieldId = fieldId;
        Coefficient = coefficient;
    }

    public KeyValuePair<int, float> ToPair() => new KeyValuePair<int, float>(FieldId, Coefficient);
}
