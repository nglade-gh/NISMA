using System.Text;

namespace NISMA_Lang;

public enum Section
{
    None = 0,
    Declare = 1,
    Modifiers = 2,
    Model = 3,
    Start = 4,
}

/// <summary>Un objet logique : une puce, ses continuations recollées, et son contexte.</summary>
public sealed record LogicalItem(int Line, string Text, Section Section, string? Group);

/// <summary>
/// Découpe le texte en objets logiques. L'indentation n'a aucune valeur : une puce se
/// poursuit uniquement si la ligne suivante commence par un opérateur de liaison, si une
/// parenthèse reste ouverte, ou si la ligne courante se termine par un antislash.
/// </summary>
public static class Lexer
{
    private static readonly string[] SectionNames = { "declare", "modifiers", "model", "start" };

    public static List<LogicalItem> Read(string text)
    {
        var lines = SignificantLines(text);

        var items = new List<LogicalItem>();
        var section = Section.None;
        var order = 0;
        string? group = null;

        for (var i = 0; i < lines.Count; i++)
        {
            var (lineNo, line) = lines[i];

            if (line.StartsWith("##", StringComparison.Ordinal))
            {
                var name = line[2..].Trim();
                if (name.Length == 0) throw new ScriptException(lineNo, "bloc sans nom après ##");
                if (section != Section.Model)
                    throw new ScriptException(lineNo, "un bloc ## n'a de sens que dans # model");
                group = name;
                continue;
            }

            if (line.StartsWith('#'))
            {
                var name = line[1..].Trim().ToLowerInvariant();
                var index = Array.IndexOf(SectionNames, name);
                if (index < 0)
                    throw new ScriptException(lineNo,
                        $"section inconnue « {name} » (attendu : declare, modifiers, model, start)");
                if (index < order)
                    throw new ScriptException(lineNo, $"la section « {name} » est en désordre");
                order = index;
                section = (Section)(index + 1);
                group = null;
                continue;
            }

            if (line[0] is not ('-' or '*'))
                throw new ScriptException(lineNo, "ligne inattendue : un objet commence par - ou *");
            if (section == Section.None)
                throw new ScriptException(lineNo, "objet hors de toute section");

            var body = line[1..].TrimStart();
            if (body.Length == 0) throw new ScriptException(lineNo, "objet vide");

            var sb = new StringBuilder(body);
            while (true)
            {
                var current = sb.ToString();
                if (current.EndsWith('\\'))
                {
                    sb.Length -= 1;
                    if (i + 1 >= lines.Count)
                        throw new ScriptException(lineNo, "antislash de continuation en fin de fichier");
                    sb.Append(' ').Append(lines[++i].Text);
                    continue;
                }

                if (UnclosedParentheses(current) > 0)
                {
                    if (i + 1 >= lines.Count)
                        throw new ScriptException(lineNo, "parenthèse non fermée en fin d'objet");
                    sb.Append(' ').Append(lines[++i].Text);
                    continue;
                }

                if (EndsWithLink(current) && i + 1 < lines.Count)
                {
                    sb.Append(' ').Append(lines[++i].Text);
                    continue;
                }

                if (i + 1 < lines.Count && StartsWithLink(lines[i + 1].Text))
                {
                    sb.Append(' ').Append(lines[++i].Text);
                    continue;
                }

                break;
            }

            items.Add(new LogicalItem(lineNo, sb.ToString().Trim(), section, group));
        }

        return items;
    }

    /// <summary>Lignes significatives : commentaires retirés, lignes vides écartées.</summary>
    private static List<(int No, string Text)> SignificantLines(string text)
    {
        var result = new List<(int, string)>();
        var raw = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (var i = 0; i < raw.Length; i++)
        {
            var line = StripComment(raw[i]).Trim();
            if (line.Length > 0) result.Add((i + 1, line));
        }

        return result;
    }

    private static string StripComment(string line)
    {
        var inString = false;
        for (var i = 0; i < line.Length - 1; i++)
        {
            if (line[i] == '"' && (i == 0 || line[i - 1] != '\\')) inString = !inString;
            if (!inString && line[i] == '/' && line[i + 1] == '/') return line[..i];
        }

        return line;
    }

    private static bool StartsWithLink(string text)
    {
        if (text.StartsWith(';') || text.StartsWith(',') || text.StartsWith(')')) return true;
        if (text.StartsWith("&&") || text.StartsWith("||")) return true;
        return StartsWithWord(text, "and") || StartsWithWord(text, "or");
    }

    /// <summary>Une ligne qui se termine par un connecteur appelle une suite.</summary>
    private static bool EndsWithLink(string text)
    {
        var trimmed = text.TrimEnd();
        return trimmed.EndsWith("&&") || trimmed.EndsWith("||") ||
               trimmed.EndsWith(',') || trimmed.EndsWith(';') ||
               EndsWithWord(trimmed, "and") || EndsWithWord(trimmed, "or");
    }

    private static bool EndsWithWord(string text, string word)
    {
        if (!text.EndsWith(word, StringComparison.Ordinal)) return false;
        var before = text.Length - word.Length - 1;
        return before < 0 || !char.IsLetterOrDigit(text[before]);
    }

    private static bool StartsWithWord(string text, string word)
    {
        if (!text.StartsWith(word, StringComparison.Ordinal)) return false;
        return text.Length == word.Length || !char.IsLetterOrDigit(text[word.Length]);
    }

    private static int UnclosedParentheses(string text)
    {
        var depth = 0;
        var inString = false;
        foreach (var c in text)
        {
            if (c == '"') inString = !inString;
            if (inString) continue;
            if (c == '(') depth++;
            else if (c == ')') depth--;
        }

        return depth;
    }

    // -----------------------------------------------------------------------
    // Tokens
    // -----------------------------------------------------------------------

    public enum TokKind
    {
        Word,
        Number,
        String,
        Op,
        Punct,
    }

    public readonly record struct Tok(TokKind Kind, string Text, double Number = 0)
    {
        public bool Is(string text) => Text == text;

        public override string ToString() => Text;
    }

    private static readonly string[] TwoCharOps = { "->", ">=", "<=", "==", "!=", "&&", "||" };
    private static readonly char[] OneCharOps = { '>', '<', '=', '∈', '!' };

    public static List<Tok> Tokenize(int line, string text)
    {
        var tokens = new List<Tok>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '"')
            {
                var sb = new StringBuilder();
                i++;
                while (i < text.Length && text[i] != '"')
                {
                    if (text[i] == '\\' && i + 1 < text.Length)
                    {
                        i++;
                        sb.Append(text[i]);
                    }
                    else
                    {
                        sb.Append(text[i]);
                    }

                    i++;
                }

                if (i >= text.Length) throw new ScriptException(line, "chaîne non fermée");
                i++;
                tokens.Add(new Tok(TokKind.String, sb.ToString()));
                continue;
            }

            if (char.IsDigit(c) || (c == '-' && i + 1 < text.Length && char.IsDigit(text[i + 1])))
            {
                var start = i;
                if (c == '-') i++;
                while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.')) i++;
                var raw = text[start..i];
                tokens.Add(new Tok(TokKind.Number, raw, double.Parse(raw, System.Globalization.CultureInfo.InvariantCulture)));
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                var start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_')) i++;
                tokens.Add(new Tok(TokKind.Word, text[start..i]));
                continue;
            }

            var two = i + 1 < text.Length ? text.Substring(i, 2) : string.Empty;
            if (TwoCharOps.Contains(two))
            {
                tokens.Add(new Tok(TokKind.Op, two));
                i += 2;
                continue;
            }

            if (OneCharOps.Contains(c))
            {
                tokens.Add(new Tok(TokKind.Op, c.ToString()));
                i++;
                continue;
            }

            if (c is '(' or ')' or ',' or ';' or ':' or '.' or '-' or '[' or ']' or '{' or '}')
            {
                tokens.Add(new Tok(TokKind.Punct, c.ToString()));
                i++;
                continue;
            }

            throw new ScriptException(line, $"caractère inattendu « {c} »");
        }

        return tokens;
    }
}
