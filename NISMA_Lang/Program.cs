using NISMA_Core;

namespace NISMA_Lang;

/// <summary>
/// nisma — compile un script NISMA en fichier .nbd2.
/// Usage : nisma &lt;script.ni&gt; [-o sortie.nbd2] [--dump]
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("usage : nisma <script.ni> [-o sortie.nbd2] [--dump]");
            return args.Length == 0 ? 2 : 0;
        }

        var input = args[0];
        string? output = null;
        var dump = false;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o":
                case "--output":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("nisma : -o attend un chemin");
                        return 2;
                    }

                    output = args[++i];
                    break;
                case "--dump":
                    dump = true;
                    break;
                default:
                    Console.Error.WriteLine($"nisma : option inconnue « {args[i]} »");
                    return 2;
            }
        }

        if (!File.Exists(input))
        {
            Console.Error.WriteLine($"nisma : fichier introuvable « {input} »");
            return 2;
        }

        try
        {
            var script = Parser.Parse(File.ReadAllText(input));
            var result = Compiler.Compile(script);
            var json = JsonUtils.SaveNbd(result.Model);

            foreach (var note in result.Notes) Console.WriteLine("note : " + note);

            if (output is not null)
            {
                File.WriteAllText(output, json);
                Console.WriteLine($"écrit {output}");
            }

            if (dump || output is null) Console.WriteLine(json);

            Console.WriteLine(
                $"{result.GrainCount} grain(s), {result.Model.Model.Fields.Count} champ(s), " +
                $"{result.BehaviourCount} comportement(s), {result.Model.Model.Modifiers.Count} modificateur(s), " +
                $"{result.Model.Simulation.Agents?.Count ?? 0} cellule(s)");

            return 0;
        }
        catch (ScriptException error)
        {
            Console.Error.WriteLine("erreur : " + error.Message);
            return 2;
        }
    }
}
