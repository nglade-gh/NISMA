namespace NISMA_Ide;

/// <summary>
/// Documentation served to the workshop: the two reference texts of the language (guide and
/// grammar) and the tutorials. Files are read from the repository — there is no copy inside
/// wwwroot, so the documentation shown in the interface is exactly the one in the tree.
///
/// Rendered HTML is cached per file, keyed by its modification time: editing a text is enough
/// to see it change after a reload.
/// </summary>
public static class Documentation
{
    private static readonly Dictionary<string, (long stamp, string html)> cache = new();

    public sealed record Topic(string Id, string Title, string Html);

    public sealed record TutorialSummary(string Id, string Title, string Summary, int Steps);

    private static string Root(string contentRoot) => Path.GetFullPath(Path.Combine(contentRoot, ".."));

    /// <summary>The two reference texts, in reading order.</summary>
    public static IReadOnlyList<Topic> Help(string contentRoot)
    {
        var root = Root(contentRoot);
        return new[]
        {
            Read(root, "GUIDE.md", "guide", "Guide du modélisateur"),
            Read(root, "LANGAGE.md", "langage", "Référence du langage"),
        }.Where(topic => topic is not null).Select(topic => topic!).ToList();
    }

    public static IReadOnlyList<TutorialSummary> Tutorials(string contentRoot)
    {
        var folder = Path.Combine(Root(contentRoot), "tutorials");
        if (!Directory.Exists(folder)) return Array.Empty<TutorialSummary>();

        return Directory.EnumerateFiles(folder, "*.md")
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .Select(path =>
            {
                var text = File.ReadAllText(path);
                var id = Path.GetFileNameWithoutExtension(path);
                var title = text.Split('\n')
                    .Select(line => line.Trim())
                    .FirstOrDefault(line => line.StartsWith("# "))?[2..].Trim() ?? id;
                var summary = Summary(text);
                var steps = CountBlocks(text, "ni");
                return new TutorialSummary(id, title, summary, steps);
            })
            .ToList();
    }

    public static string? Tutorial(string contentRoot, string id)
    {
        // le nom vient de l'URL : on n'accepte qu'un identifiant simple, jamais un chemin
        if (id.Any(c => !(char.IsLetterOrDigit(c) || c is '-' or '_'))) return null;

        var path = Path.Combine(Root(contentRoot), "tutorials", id + ".md");
        if (!File.Exists(path)) return null;

        return Render(path);
    }

    private static Topic? Read(string root, string file, string id, string title)
    {
        var path = Path.Combine(root, file);
        return File.Exists(path) ? new Topic(id, title, Render(path)) : null;
    }

    private static string Render(string path)
    {
        var stamp = File.GetLastWriteTimeUtc(path).Ticks;
        if (cache.TryGetValue(path, out var entry) && entry.stamp == stamp) return entry.html;

        var html = Markdown.ToHtml(File.ReadAllText(path));
        cache[path] = (stamp, html);
        return html;
    }

    /// <summary>First paragraph after the title, for the tutorial list.</summary>
    private static string Summary(string text)
    {
        var lines = text.Split('\n');
        var start = Array.FindIndex(lines, line => line.StartsWith("# "));
        if (start < 0) return "";

        var paragraph = new List<string>();
        for (var i = start + 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                if (paragraph.Count > 0) break;
                continue;
            }
            if (line.StartsWith("#") || line.StartsWith("```") || line.StartsWith("-")) continue;
            paragraph.Add(line);
        }

        return string.Join(" ", paragraph);
    }

    private static int CountBlocks(string text, string language)
    {
        var count = 0;
        foreach (var line in text.Split('\n'))
            if (line.Trim() == "```" + language) count++;
        return count;
    }
}
