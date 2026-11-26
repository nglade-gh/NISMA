using System.Text.Json;
using System.Text.Json.Serialization;
using NISMA_Core;
using NISMA_Lang;

namespace NISMA_Ide;

/// <summary>
/// Hôte de l'IDE NISMA : sert l'interface web et expose NISMA Core derrière trois
/// routes. Aucune dépendance externe : le compilateur est appelé en processus.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        var app = builder.Build();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapGet("/api/health", () => Results.Ok(new { ok = true, name = "nisma-ide", version = 1 }));

        // Exemples pris dans NISMA/scripts : une seule source de vérité, pas de copie
        // dans wwwroot qui dériverait au premier changement de syntaxe.
        app.MapGet("/api/examples", () =>
        {
            var folder = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "scripts"));
            if (!Directory.Exists(folder)) return Results.Ok(Array.Empty<object>());
            var examples = Directory.EnumerateFiles(folder, "*.ni")
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => new
                {
                    name = Path.GetFileNameWithoutExtension(path),
                    source = File.ReadAllText(path),
                })
                .ToList();
            return Results.Ok(examples);
        });

        app.MapPost("/api/compile", (CompileRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Script))
                return Results.BadRequest(new { ok = false, error = new { line = 0, message = "script vide" } });

            try
            {
                var script = Parser.Parse(request.Script);
                var result = Compiler.Compile(script);
                var nbd2 = JsonUtils.SaveNbd(result.Model);
                var model = result.Model;
                return Results.Ok(new
                {
                    ok = true,
                    nbd2,
                    notes = result.Notes,
                    summary = new
                    {
                        grains = model.Model.Grains.Count,
                        fields = model.Model.Fields.Count,
                        behaviours = model.Model.Behaviours.Count,
                        modifiers = model.Model.Modifiers.Count,
                        cells = model.Simulation.Agents?.Count ?? 0,
                        bytes = System.Text.Encoding.UTF8.GetByteCount(nbd2),
                    },
                });
            }
            catch (ScriptException error)
            {
                return Results.Ok(new { ok = false, error = new { line = error.Line, message = error.Reason } });
            }
            catch (Exception error)
            {
                return Results.Ok(new { ok = false, error = new { line = 0, message = error.Message } });
            }
        });

        app.Run();
        return 0;
    }

    public sealed record CompileRequest(string Script);
}
