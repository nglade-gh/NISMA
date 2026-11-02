using System;
using System.Collections.Generic;

namespace autopulse.sub_classes;

public class FieldProduction
{
    public Dictionary<int, float> Productions { get; set; } = new();
}

public class FieldInfluences
{
    public Dictionary<int, float> Influences { get; set; } = new();
}

public class FieldPermeable
{
    public Dictionary<int, float> Permeable { get; set; } = new();
}
