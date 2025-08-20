namespace nbd.sub_classes;
//covers field productions, influences and permeability in grains
//potentially covers field thresholds in behaviours
public class FieldProduction
{ // dictionary(fieldId, productionValue)
    Dictionary<uint, float> productions =
        new Dictionary<uint, float>();
}
public class FieldInfluences
{ // dictionary(fieldId, influenceValue)
    Dictionary<uint, float> influences =
        new Dictionary<uint, float>();
}
public class fieldPermeable
{ // dictionary(fieldId, permeabilityValue)
    Dictionary<uint, float> fieldPermeable =
        new Dictionary<uint, float>();
}
