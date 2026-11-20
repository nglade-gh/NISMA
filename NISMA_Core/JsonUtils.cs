using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace NISMA_Core;

/// <summary>
/// Root model for NetBioDyn serialization/deserialization.
/// </summary>
public class NbdModel
{
    [JsonPropertyName("model")]
    public Model Model { get; set; } = new Model();

    [JsonPropertyName("simulation")]
    public Simulation Simulation { get; set; } = new Simulation();

    [JsonPropertyName("thumbnail")]
    public JsonNode? Thumbnail { get; set; }

    public NbdModel()
    {
    }
}

/// <summary>
/// Contains the model definition (grains, behaviours, fields).
/// </summary>
public class Model
{
    /// <summary>Update plan values for <see cref="UpdatePlan"/>.</summary>
    public const string ParallelUpdatePlan = "Parallel";
    public const string SequentialUpdatePlan = "Sequential";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Update plan: "Parallel" (default) or "Sequential".
    /// Serialized only when it is "Sequential".
    /// </summary>
    [JsonPropertyName("updatePlan")]
    public string? UpdatePlan { get; set; }

    /// <summary>True when the update plan is "Sequential".</summary>
    [JsonIgnore]
    public bool IsSequential => UpdatePlan == SequentialUpdatePlan;

    [JsonPropertyName("grains")]
    public List<Grain> Grains { get; set; } = new List<Grain>();

    [JsonPropertyName("behaviours")]
    public List<Behaviour> Behaviours { get; set; } = new List<Behaviour>();

    [JsonPropertyName("fields")]
    public List<Field> Fields { get; set; } = new List<Field>();

    public Model()
    {
    }
}

/// <summary>
/// JSON utilities for NetBioDyn serialization with post-cleanup.
/// </summary>
public static class JsonUtils
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serialize a model to a NetBioDyn-compatible JSON string, with cleanup of empty/optional fields.
    /// </summary>
    public static string SaveNbd(NbdModel model, string referencePath = "")
    {
        var json = JsonSerializer.SerializeToNode(model, SerializerOptions);
        if (json is not JsonObject root) return json!.ToJsonString();

        // Remove thumbnail if present
        root.Remove("thumbnail");

        // Remove model.name and model.description if present (even if empty)
        if (root["model"] is JsonObject modelObj)
        {
            modelObj.Remove("name");
            modelObj.Remove("description");

            // empty collections are omitted by Centyllion
            CleanEmptyArray(modelObj, "grains");
            CleanEmptyArray(modelObj, "behaviours");
            CleanEmptyArray(modelObj, "fields");

            // updatePlan is kept only when it is "Sequential" (the default is "Parallel")
            var planNode = modelObj["updatePlan"];
            var keepPlan = planNode is JsonValue pv && pv.TryGetValue<string>(out var plan) && plan == Model.SequentialUpdatePlan;
            if (!keepPlan) modelObj.Remove("updatePlan");
        }

        // Clean grains
        if (root["model"]?["grains"] is JsonArray grains)
        {
            foreach (var g in grains.OfType<JsonObject>())
            {
                CleanOptionalString(g, "description");
                CleanOptionalString(g, "color");
                CleanOptionalString(g, "name");
                CleanEmptyArray(g, "allowedDirection");
                CleanEmptyObject(g, "fieldPermeable");
                CleanEmptyObject(g, "fieldProductions");
                CleanEmptyObject(g, "fieldInfluences");
                CleanDefaultBool(g, "invisible", false);

                // boundaryConditions: keep only when the grain is not toric everywhere
                if (g["boundaryConditions"] is JsonObject bcObj)
                {
                    bool allToric = true;
                    foreach (var wall in new[] { "left", "right", "top", "bottom" })
                    {
                        if (bcObj[wall] is not JsonValue wv || !wv.TryGetValue<bool>(out var open) || !open)
                        {
                            allToric = false;
                            break;
                        }
                    }
                    if (allToric) g.Remove("boundaryConditions");
                }
            }
        }

        // Clean behaviours
        if (root["model"]?["behaviours"] is JsonArray behaviours)
        {
            foreach (var b in behaviours.OfType<JsonObject>())
            {
                CleanOptionalString(b, "description");
                CleanOptionalString(b, "name");
                CleanOptionalString(b, "formula");
                CleanOptionalString(b, "actions");
                CleanNullSafe(b, "probability");
                CleanNullSafe(b, "agePredicate");
                CleanEmptyArray(b, "fieldPredicates");
                CleanEmptyObject(b, "fieldInfluences");
                CleanEmptyArray(b, "reaction");
                CleanEmptyArray(b, "agentCountPredicates");

                // Clean reaction items
                if (b["reaction"] is JsonArray reactions)
                {
                    foreach (var r in reactions.OfType<JsonObject>())
                    {
                        CleanNullSafe(r, "reactiveId");
                        CleanNullSafe(r, "productId");
                        CleanEmptyArray(r, "allowedDirection");

                        // per-reaction age inheritance: only remove if < 0 (keep 0)
                        if (r["sourceReactive"] is JsonValue rsv && rsv.TryGetValue<int>(out var rsrcVal) && rsrcVal < 0)
                        {
                            r.Remove("sourceReactive");
                        }
                    }
                }

                // sourceReactive: only remove if < 0 (keep 0)
                if (b["sourceReactive"] is JsonValue sv && sv.TryGetValue<int>(out var srcVal) && srcVal < 0)
                {
                    b.Remove("sourceReactive");
                }

                // mainProductId: remove if null
                CleanNullSafe(b, "mainProductId");

                // mainReactiveId: remove if null (but 0 is valid)
                CleanNullSafe(b, "mainReactiveId");
            }
        }

        // Clean fields
        if (root["model"]?["fields"] is JsonArray fields)
        {
            foreach (var f in fields.OfType<JsonObject>())
            {
                CleanDefaultInt(f, "id", -1);
                CleanDefaultBool(f, "invisible", false);
                CleanDefaultBool(f, "toric", true);
                CleanEmptyArray(f, "allowedDirection");
                CleanNull(f, "speed");
                CleanNull(f, "halfLife");
                CleanOptionalString(f, "formula");
                CleanOptionalString(f, "description");
                CleanOptionalString(f, "color");
                CleanOptionalString(f, "name");
            }
        }

        // Clean simulation
        if (root["simulation"] is JsonObject sim)
        {
            // simulation.name and simulation.description are kept even if empty (reference contains "")
            CleanNull(sim, "name");
            CleanNull(sim, "description");
            CleanDefaultObject(sim, "settings");
            CleanEmptyArray(sim, "assets");

            if (sim["settings"] is JsonObject settings)
            {
                CleanDefaultBool(settings, "showGrid", true);
                CleanDefaultInt(settings, "size", 0);
                CleanOptionalString(settings, "gridTextureUrl");
                CleanOptionalString(settings, "backgroundColor");
            }

            if (sim["assets"] is JsonArray assets)
            {
                foreach (var a in assets.OfType<JsonObject>())
                {
                    // Centyllion does not write an asset name: do not add one
                    CleanOptionalString(a, "name");
                    CleanOptionalString(a, "url");
                }
            }

            if (sim["width"] is JsonValue wv && wv.TryGetValue<int>(out var wv2) && wv2 == 0) sim.Remove("width");
            if (sim["height"] is JsonValue hv && hv.TryGetValue<int>(out var hv2) && hv2 == 0) sim.Remove("height");
            if (sim["depth"] is JsonValue dv && dv.TryGetValue<int>(out var dv2) && dv2 == 0) sim.Remove("depth");
        }

        return root.ToJsonString(SerializerOptions.WriteIndented ? new JsonSerializerOptions { WriteIndented = true } : null);
    }

    private static void CleanOptionalString(JsonObject obj, string property)
    {
        // SerializeToNode stores null properties as explicit null entries, which the
        // indexer returns as C# null (not a JsonValue), so ContainsKey + null check
        if (!obj.ContainsKey(property)) return;
        var node = obj[property];
        if (node is null || (node is JsonValue v && (v.GetValueKind() == JsonValueKind.Null || v.GetValueKind() == JsonValueKind.String && v.GetValue<string>() == string.Empty)))
        {
            obj.Remove(property);
        }
    }

    private static void CleanNull(JsonObject obj, string property)
    {
        if (obj[property] is JsonValue v && v.GetValueKind() == JsonValueKind.Null)
        {
            obj.Remove(property);
        }
    }

    private static void CleanNullSafe(JsonObject obj, string property)
    {
        var node = obj[property];
        if (node is null) return;
        if (node.GetValueKind() == JsonValueKind.Null)
        {
            obj.Remove(property);
        }
    }

    private static void CleanEmptyArray(JsonObject obj, string property)
    {
        if (obj[property] is JsonArray arr && arr.Count == 0)
        {
            obj.Remove(property);
        }
    }

    private static void CleanEmptyObject(JsonObject obj, string property)
    {
        if (obj[property] is JsonObject jobj && jobj.Count == 0)
        {
            obj.Remove(property);
        }
    }

    private static void CleanDefaultObject(JsonObject obj, string property)
    {
        if (obj[property] is JsonObject jobj && jobj.Count == 0)
        {
            obj.Remove(property);
        }
    }

    private static void CleanDefaultInt(JsonObject obj, string property, int defaultValue)
    {
        if (obj[property] is JsonValue v && v.TryGetValue<int>(out var val) && val == defaultValue)
        {
            obj.Remove(property);
        }
    }

    private static void CleanDefaultBool(JsonObject obj, string property, bool defaultValue)
    {
        if (obj[property] is JsonValue v && v.TryGetValue<bool>(out var val) && val == defaultValue)
        {
            obj.Remove(property);
        }
    }
}
