using System.Text;

namespace NISMA_Ide;

/// <summary>
/// Minimal Markdown renderer for the documentation shipped with the workshop: headings,
/// fenced code, tables, lists, quotes, rules, and the inline forms actually used by
/// GUIDE.md, LANGAGE.md and the tutorials.
///
/// Written by hand rather than pulled as a package: the documentation uses a small, stable
/// subset, and the workshop has no third-party dependency by design. Anything outside the
/// subset is rendered as a paragraph or kept verbatim rather than silently dropped.
/// </summary>
public static class Markdown
{
    public static string ToHtml(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var html = new StringBuilder();
        var index = 0;

        while (index < lines.Length)
        {
            var line = lines[index];

            if (line.TrimStart().StartsWith("```"))
            {
                index = CodeBlock(lines, index, html);
                continue;
            }

            if (IsTableStart(lines, index))
            {
                index = Table(lines, index, html);
                continue;
            }

            var heading = Heading(line);
            if (heading is not null)
            {
                var level = heading.Value.level;
                html.AppendLine($"<h{level}>{Inline(heading.Value.text)}</h{level}>");
                index++;
                continue;
            }

            if (line.Trim() is "---" or "***" or "___")
            {
                html.AppendLine("<hr>");
                index++;
                continue;
            }

            if (line.TrimStart().StartsWith(">"))
            {
                index = Quote(lines, index, html);
                continue;
            }

            if (IsBullet(line) || IsNumbered(line))
            {
                index = List(lines, index, html);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                index++;
                continue;
            }

            index = Paragraph(lines, index, html);
        }

        return html.ToString();
    }

    private static bool IsBullet(string line)
    {
        var trimmed = line.TrimStart();
        return (trimmed.StartsWith("- ") || trimmed.StartsWith("* ")) && trimmed.Length > 2;
    }

    private static bool IsNumbered(string line)
    {
        var trimmed = line.TrimStart();
        var digits = 0;
        while (digits < trimmed.Length && char.IsDigit(trimmed[digits])) digits++;
        return digits > 0 && trimmed.Length > digits + 1 && trimmed[digits] == '.' && trimmed[digits + 1] == ' ';
    }

    private static (int level, string text)? Heading(string line)
    {
        var trimmed = line.TrimStart();
        var hashes = 0;
        while (hashes < trimmed.Length && trimmed[hashes] == '#') hashes++;
        if (hashes is < 1 or > 6) return null;
        if (trimmed.Length <= hashes || trimmed[hashes] != ' ') return null;
        return (hashes, trimmed[(hashes + 1)..].Trim());
    }

    private static bool IsTableStart(string[] lines, int index)
    {
        if (index + 1 >= lines.Length) return false;
        var head = lines[index].Trim();
        var separator = lines[index + 1].Trim();
        if (!head.StartsWith("|") || !separator.StartsWith("|")) return false;
        var cells = separator.Split('|', StringSplitOptions.RemoveEmptyEntries);
        return cells.Length > 0 && cells.All(cell => cell.Trim().Trim('-', ':').Length == 0 && cell.Contains('-'));
    }

    private static int CodeBlock(string[] lines, int index, StringBuilder html)
    {
        var fence = lines[index].Trim();
        var language = fence.Length > 3 ? fence[3..].Trim() : "";
        index++;
        var code = new StringBuilder();
        while (index < lines.Length && !lines[index].TrimStart().StartsWith("```"))
        {
            code.AppendLine(lines[index]);
            index++;
        }
        if (index < lines.Length) index++;   // fermeture

        var attribute = language.Length > 0 ? $" data-lang=\"{Escape(language)}\"" : "";
        html.AppendLine($"<pre class=\"code\"{attribute}><code>{Escape(code.ToString().TrimEnd('\n'))}</code></pre>");
        return index;
    }

    private static int Table(string[] lines, int index, StringBuilder html)
    {
        var header = SplitRow(lines[index]);
        index += 2;
        html.AppendLine("<table><thead><tr>");
        foreach (var cell in header) html.AppendLine($"<th>{Inline(cell)}</th>");
        html.AppendLine("</tr></thead><tbody>");

        while (index < lines.Length && lines[index].Trim().StartsWith("|"))
        {
            var row = SplitRow(lines[index]);
            html.AppendLine("<tr>");
            foreach (var cell in row) html.AppendLine($"<td>{Inline(cell)}</td>");
            html.AppendLine("</tr>");
            index++;
        }

        html.AppendLine("</tbody></table>");
        return index;
    }

    private static List<string> SplitRow(string line)
    {
        var trimmed = line.Trim().Trim('|');
        return trimmed.Split('|').Select(cell => cell.Trim()).ToList();
    }

    private static int Quote(string[] lines, int index, StringBuilder html)
    {
        var text = new StringBuilder();
        while (index < lines.Length && lines[index].TrimStart().StartsWith(">"))
        {
            text.Append(lines[index].TrimStart()[1..].Trim()).Append(' ');
            index++;
        }
        html.AppendLine($"<blockquote><p>{Inline(text.ToString().Trim())}</p></blockquote>");
        return index;
    }

    /// <summary>
    /// Lists: a continuation line — indented, and not a new bullet — belongs to the item
    /// above it, as in Markdown. Two-space indentation opens a nested level.
    /// </summary>
    private static int List(string[] lines, int index, StringBuilder html)
    {
        var ordered = IsNumbered(lines[index]);
        var tag = ordered ? "ol" : "ul";
        html.AppendLine($"<{tag}>");

        while (index < lines.Length)
        {
            var line = lines[index];

            if (IsBullet(line) && line.StartsWith("  "))
            {
                html.AppendLine("<li class=\"nested\"><ul>");
                while (index < lines.Length && lines[index].StartsWith("  ") && IsBullet(lines[index]))
                {
                    html.AppendLine($"<li>{Inline(lines[index].Trim()[2..].Trim())}</li>");
                    index++;
                }
                html.AppendLine("</ul></li>");
                continue;
            }

            if (IsBullet(line) || IsNumbered(line))
            {
                var text = ItemText(line, ordered);
                index++;
                while (index < lines.Length && IsContinuation(lines[index]))
                {
                    text += " " + lines[index].Trim();
                    index++;
                }
                html.AppendLine($"<li>{Inline(text)}</li>");
                continue;
            }

            break;
        }

        html.AppendLine($"</{tag}>");
        return index;
    }

    private static string ItemText(string line, bool ordered)
    {
        var text = line.Trim();
        if (ordered)
        {
            var dot = text.IndexOf('.');
            return dot >= 0 ? text[(dot + 1)..].Trim() : text;
        }
        return text[2..].Trim();
    }

    /// <summary>An indented line that continues the item above instead of starting a block.</summary>
    private static bool IsContinuation(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("  ")) return false;
        var trimmed = line.TrimStart();
        return !(trimmed.StartsWith("#") || trimmed.StartsWith("```") ||
                 trimmed.StartsWith("|") || trimmed.StartsWith(">") ||
                 IsBullet(line) || IsNumbered(line));
    }

    private static int Paragraph(string[] lines, int index, StringBuilder html)
    {
        var text = new StringBuilder();
        while (index < lines.Length)
        {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#") ||
                line.TrimStart().StartsWith("```") || line.TrimStart().StartsWith("|") ||
                line.TrimStart().StartsWith(">") || IsBullet(line) || IsNumbered(line))
                break;
            text.Append(line.Trim()).Append(' ');
            index++;
        }
        html.AppendLine($"<p>{Inline(text.ToString().Trim())}</p>");
        return index;
    }

    /// <summary>
    /// Inline forms: code spans, bold, italic, links.
    ///
    /// Code spans are set aside first and restored at the end: otherwise a bold phrase that
    /// contains a code span is cut in two by the split on backticks, and the asterisks stay
    /// visible in the output.
    /// </summary>
    private static string Inline(string text)
    {
        var parts = text.Split('`');
        var codes = new List<string>();
        var rest = new StringBuilder();

        for (var i = 0; i < parts.Length; i++)
        {
            if (i % 2 == 1)
            {
                rest.Append('\u0001').Append(codes.Count).Append('\u0002');
                codes.Add(Escape(parts[i]));
            }
            else rest.Append(parts[i]);
        }

        var html = Emphasis(rest.ToString());
        for (var i = 0; i < codes.Count; i++)
            html = html.Replace($"\u0001{i}\u0002", $"<code>{codes[i]}</code>");

        return html;
    }

    private static string Emphasis(string text)
    {
        var escaped = Escape(text);

        escaped = System.Text.RegularExpressions.Regex.Replace(escaped,
            @"\[([^\]]+)\]\(([^)\s]+)\)", "<a href=\"$2\" target=\"_blank\" rel=\"noopener\">$1</a>");
        escaped = System.Text.RegularExpressions.Regex.Replace(escaped,
            @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        escaped = System.Text.RegularExpressions.Regex.Replace(escaped,
            @"(?<![*\w])\*([^*\n]+)\*(?![*\w])", "<em>$1</em>");

        return escaped;
    }

    private static string Escape(string text) => text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");
}
