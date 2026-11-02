using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace autopulse;

[Serializable, XmlRoot("behaviours")]
[JsonSerializable(typeof(Behaviour))]
[JsonSerializable(typeof(List<Behaviour>))]
public class Behaviour
{
    [XmlElement("name")][JsonPropertyName("name")] public string Name { get; set; } = "";
    [XmlElement("description")][JsonPropertyName("description")] public string Description { get; set; } = "";
    [XmlElement("probability")][JsonPropertyName("probability")]
    public double Probability { get; set; } = 1.0;

    [XmlElement("agePredicate")][JsonPropertyName("agePredicate")]
    public Predicate? AgePredicate { get; set; }

    [XmlElement("fieldPredicates")][JsonPropertyName("fieldPredicates")]
    public List<FieldPredicate> FieldPredicates { get; set; } = new();

    [XmlElement("mainReactiveId")][JsonPropertyName("mainReactiveId")] public int MainReactiveId { get; set; } = -1;
    [XmlElement("mainProductId")][JsonPropertyName("mainProductId")] public int MainProductId { get; set; } = -1;
    [XmlElement("sourceReactive")][JsonPropertyName("sourceReactive")] public int SourceReactive { get; set; } = -1;

    [XmlElement("fieldInfluences")][JsonPropertyName("fieldInfluences")]
    public Dictionary<int, float> FieldInfluences { get; set; } = new();

    [XmlElement("reaction")][JsonPropertyName("reaction")]
    public List<Reaction> Reaction { get; set; } = new();

    public Behaviour() { }

    public Behaviour(string name, int mainReactiveId, int mainProductId, int sourceReactive = -1)
    {
        Name = name;
        MainReactiveId = mainReactiveId;
        MainProductId = mainProductId;
        SourceReactive = sourceReactive;
    }
}

// Prédicat générique (opérateur + constante)
[JsonSerializable(typeof(Predicate))]
public class Predicate
{
    [XmlElement("op")][JsonPropertyName("op")] public Operator Op { get; set; }
    [XmlElement("constant")][JsonPropertyName("constant")] public double Constant { get; set; }

    public Predicate() { }

    public Predicate(Operator op, double constant)
    {
        Op = op;
        Constant = constant;
    }
}

// Prédicat de champ (champId -> prédicat de valeur flottante)
[JsonConverter(typeof(FieldPredicateConverter))]
[JsonSerializable(typeof(FieldPredicate))]
public class FieldPredicate
{
    public int FieldId { get; set; }
    public Predicate Predicate { get; set; } = new();

    public FieldPredicate() { }

    public FieldPredicate(int fieldId, Predicate predicate)
    {
        FieldId = fieldId;
        Predicate = predicate;
    }
}

public class FieldPredicateConverter : JsonConverter<FieldPredicate>
{
    public override FieldPredicate? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Read();
            var fieldId = reader.GetInt32();
            reader.Read();
            var predicate = JsonSerializer.Deserialize<Predicate>(ref reader, options);
            reader.Read();
            return new FieldPredicate(fieldId, predicate!);
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            var fieldId = 0;
            Predicate? predicate = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                var prop = reader.GetString() ?? "";
                reader.Read();
                if (prop == "first") fieldId = reader.GetInt32();
                else if (prop == "second") predicate = JsonSerializer.Deserialize<Predicate>(ref reader, options);
            }
            return predicate == null ? null : new FieldPredicate(fieldId, predicate);
        }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, FieldPredicate value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("first", value.FieldId);
        writer.WritePropertyName("second");
        JsonSerializer.Serialize(writer, value.Predicate, options);
        writer.WriteEndObject();
    }
}

// Opérateurs
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Operator
{
    Equals, NotEquals, LessThan, LessThanOrEquals, GreaterThan, GreaterThanOrEquals
}

// Réaction d'un comportement
[JsonSerializable(typeof(Reaction))]
public class Reaction
{
    [XmlElement("reactiveId")][JsonPropertyName("reactiveId")] public int ReactiveId { get; set; } = -1;
    [XmlElement("productId")][JsonPropertyName("productId")] public int ProductId { get; set; } = -1;
    [XmlElement("sourceReactive")][JsonPropertyName("sourceReactive")] public int SourceReactive { get; set; } = -1;
    [XmlElement("allowedDirection")][JsonPropertyName("allowedDirection")]
    public List<string> AllowedDirection { get; set; } = new() { "Left", "Up", "Right", "Down" };
}
