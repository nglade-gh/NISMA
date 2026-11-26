using System.Text;
using static NISMA_Lang.Lexer;

namespace NISMA_Lang;

/// <summary>Analyse syntaxique : objets logiques → <see cref="Script"/>.</summary>
public sealed class Parser
{
    private readonly List<LogicalItem> _items;
    private int _pos;

    private Parser(List<LogicalItem> items)
    {
        _items = items;
    }

    public static Script Parse(string text) => new Parser(Lexer.Read(text)).ParseAll();

    // -----------------------------------------------------------------------

    private LogicalItem Current => _items[_pos];

    private bool AtEnd => _pos >= _items.Count;

    private Script ParseAll()
    {
        var script = new Script();
        while (!AtEnd)
        {
            var item = Current;
            var tokens = Tokenize(item.Line, item.Text);
            switch (item.Section)
            {
                case Section.Declare:
                    script.Declarations.Add(ParseDeclaration(item, tokens));
                    break;
                case Section.Modifiers:
                    script.Modifiers.Add(ParseModifier(item, tokens));
                    break;
                case Section.Model:
                    ParseModelItem(item, tokens, script);
                    break;
                case Section.Process:
                    ParseProcessItem(item, script);
                    break;
                case Section.Start:
                    script.Start.Add(ParseStart(item, tokens));
                    break;
                default:
                    throw new ScriptException(item.Line, "objet hors de toute section");
            }

            _pos++;
        }

        return script;
    }

    // -----------------------------------------------------------------------
    // declare
    // -----------------------------------------------------------------------

    private static Declaration ParseDeclaration(LogicalItem item, List<Tok> tokens)
    {
        var line = item.Line;
        var head = tokens[0];

        if (head.Is("model"))
        {
            var name = ExpectString(tokens, 1, line);
            string? description = null;
            for (var i = 2; i < tokens.Count - 1; i++)
                if (tokens[i].Is("description"))
                    description = ExpectString(tokens, i + 1, line);
            return new ModelInfo(line, name, description);
        }

        if (head.Is("grid"))
        {
            var w = ExpectInt(tokens, 1, line);
            var h = tokens.Count > 2 ? ExpectInt(tokens, 2, line) : w;
            if (w <= 0 || h <= 0) throw new ScriptException(line, "taille de grille invalide");
            EnsureEnd(tokens, tokens.Count > 2 ? 3 : 2, line, "grille");
            return new GridDecl(line, w, h);
        }

        if (head.Is("update"))
        {
            var mode = ExpectWord(tokens, 1, line).ToLowerInvariant();
            return mode switch
            {
                "parallel" => new UpdateDecl(line, false),
                "sequential" => new UpdateDecl(line, true),
                _ => throw new ScriptException(line, $"plan de mise à jour inconnu « {mode} »"),
            };
        }

        if (head.Is("seed"))
        {
            var seed = ExpectInt(tokens, 1, line);
            EnsureEnd(tokens, 2, line, "graine");
            return new SeedDecl(line, seed);
        }

        if (head.Is("tag")) return new TagDecl(line, ExpectWord(tokens, 1, line));

        if (head.Is("abstract"))
        {
            var name = ExpectWord(tokens, 1, line);
            if (tokens.Count == 2) return new AbstractDecl(line, name, null);
            if (!tokens[2].Is("<")) throw new ScriptException(line, "attendu < PARENT après le nom du type abstrait");
            return new AbstractDecl(line, name, ExpectWord(tokens, 3, line));
        }

        if (head.Is("field"))
        {
            var name = ExpectWord(tokens, 1, line);
            var start = 2;
            var fieldColor = start < tokens.Count && tokens[start].Is("[")
                ? ReadColor(tokens, ref start, line)
                : null;
            var diffusion = 1.0;
            var halfLife = 0;
            var directions = 8;
            var opacity = 100;
            for (var i = start; i < tokens.Count; i++)
            {
                switch (tokens[i].Text)
                {
                    case "diff":
                        diffusion = ExpectNumber(tokens, ++i, line);
                        break;
                    case "life":
                        halfLife = ExpectInt(tokens, ++i, line);
                        break;
                    case "dirs":
                        directions = ExpectInt(tokens, ++i, line);
                        break;
                    case "opacity":
                        opacity = ExpectInt(tokens, ++i, line);
                        break;
                    default:
                        throw new ScriptException(line, $"attribut de champ inconnu « {tokens[i].Text} »");
                }
            }

            return new FieldDecl(line, name, fieldColor, diffusion, halfLife, directions, opacity);
        }

        if (head.Is("agent"))
        {
            var name = ExpectWord(tokens, 1, line);

            if (tokens.Count > 2 && tokens[2].Is("<"))
            {
                var parent = ExpectWord(tokens, 3, line);
                var tags = new List<string>();
                var cursor = 4;
                if (cursor < tokens.Count && tokens[cursor].Is("with"))
                {
                    cursor++;
                    while (cursor < tokens.Count && !tokens[cursor].Is("["))
                    {
                        if (tokens[cursor].Kind == TokKind.Word) tags.Add(tokens[cursor].Text);
                        else if (!tokens[cursor].Is(","))
                            throw new ScriptException(line, $"tag inattendu « {tokens[cursor].Text} »");
                        cursor++;
                    }
                }

                var subtypeColor = cursor < tokens.Count && tokens[cursor].Is("[")
                    ? ReadColor(tokens, ref cursor, line)
                    : null;
                if (cursor < tokens.Count)
                    throw new ScriptException(line, $"texte inattendu après la déclaration : « {tokens[cursor].Text} »");
                return new ConcreteDecl(line, name, parent, tags, subtypeColor);
            }

            var index = 2;
            string? color = null;
            if (index < tokens.Count && tokens[index].Is("["))
                color = ReadColor(tokens, ref index, line);
            else if (index < tokens.Count && tokens[index].Kind == TokKind.Word)
                throw new ScriptException(line,
                    $"la couleur s'écrit entre crochets : agent {name} [{tokens[index].Text}]");

            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = index; i < tokens.Count; i++)
            {
                var key = tokens[i].Text.ToLowerInvariant();
                if (key == "walls")
                {
                    attributes["walls"] = "yes";
                    continue;
                }

                if (i + 1 >= tokens.Count)
                    throw new ScriptException(line, $"attribut « {key} » sans valeur");
                attributes[key] = tokens[++i].Text;
            }

            return new AgentDecl(line, name, color, attributes);
        }

        // Agent produces|absorbs|lets FIELD VALUE
        if (tokens.Count >= 4 && (tokens[1].Is("produces") || tokens[1].Is("absorbs") || tokens[1].Is("lets")))
            return new ProductionDecl(line, tokens[0].Text, tokens[1].Text.ToLowerInvariant(),
                tokens[2].Text, ExpectNumber(tokens, 3, line));

        throw new ScriptException(line, $"déclaration inconnue « {head.Text} »");
    }

    // -----------------------------------------------------------------------
    // modifiers
    // -----------------------------------------------------------------------

    private static ModifierDecl ParseModifier(LogicalItem item, List<Tok> tokens)
    {
        var line = item.Line;
        if (tokens.Count == 0) throw new ScriptException(line, "modificateur vide");

        if (tokens[0].Is("every"))
        {
            var period = ExpectInt(tokens, 1, line);
            if (!tokens[2].Is("steps")) throw new ScriptException(line, "attendu « steps »");
            return new ModifierDecl(line, "every", period, null, ActionText(tokens, 3, line));
        }

        if (tokens[0].Is("at") && tokens.Count > 1 && tokens[1].Is("step"))
            return new ModifierDecl(line, "at", ExpectInt(tokens, 2, line), null, ActionText(tokens, 3, line));

        if (tokens[0].Is("when"))
        {
            var (cond, next) = new ConditionReader(tokens, 1, line).ReadOr();
            if (next >= tokens.Count || !tokens[next].Is("everywhere"))
                throw new ScriptException(line, "attendu « everywhere » après la condition du modificateur");
            return new ModifierDecl(line, "when", 0, cond, ActionText(tokens, next + 1, line));
        }

        throw new ScriptException(line, $"déclencheur inconnu « {tokens[0].Text} »");
    }

    /// <summary>Texte de l'action qui suit les deux-points, recollé depuis les tokens.</summary>
    private static string ActionText(List<Tok> tokens, int start, int line)
    {
        if (start >= tokens.Count || !tokens[start].Is(":"))
            throw new ScriptException(line, "attendu « : » avant l'action");
        var sb = new StringBuilder();
        for (var i = start + 1; i < tokens.Count; i++) AppendToken(sb, tokens[i]);
        var text = sb.ToString().Trim();
        if (text.Length == 0) throw new ScriptException(line, "action vide");
        return text;
    }

    private static void AppendToken(StringBuilder sb, Tok token)
    {
        var noSpaceBefore = token.Is(",") || token.Is(")") || token.Is("]");
        if (sb.Length > 0 && !noSpaceBefore) sb.Append(' ');
        sb.Append(token.Text);
    }

    // -----------------------------------------------------------------------
    // model
    // -----------------------------------------------------------------------

    private void ParseModelItem(LogicalItem item, List<Tok> tokens, Script script)
    {
        var line = item.Line;

        // p 0.2 — probabilité par défaut du bloc
        if (tokens[0].Is("p") && tokens.Count == 2)
        {
            _blockProbabilities[item.Group ?? ""] = tokens[1].Number;
            return;
        }

        // étiquette éventuelle : « Birth: »
        string? label = null;
        var start = 0;
        if (tokens.Count > 1 && tokens[0].Kind == TokKind.Word && tokens[1].Is(":"))
        {
            label = tokens[0].Text;
            start = 2;
        }

        var neighbour = false;
        if (tokens[start].Is("neighbour"))
        {
            neighbour = true;
            start++;
        }

        var subject = tokens[start].Text;
        start++;

        RuleKind kind;
        string? product = null;

        if (tokens[start].Is("->"))
        {
            kind = RuleKind.Transform;
            product = tokens[start + 1].Text;
            start += 2;
        }
        else if (tokens[start].Is("spawns"))
        {
            kind = RuleKind.Spawn;
            start++;
        }
        else if (tokens[start].Is("dies"))
        {
            kind = RuleKind.Die;
            start++;
        }
        else if (tokens[start].Is("reacts") && tokens[start + 1].Is("into"))
        {
            kind = RuleKind.Transform;
            product = tokens[start + 2].Text;
            start += 3;
        }
        else
        {
            throw new ScriptException(line, $"verbe inconnu « {tokens[start].Text} »");
        }

        Cond? condition = null;
        double probability = 1.0;
        var actions = new List<string>();

        if (start < tokens.Count && tokens[start].Is("when"))
        {
            var reader = new ConditionReader(tokens, start + 1, line);
            (condition, start) = reader.ReadOr();
        }

        if (start < tokens.Count && tokens[start].Is("p"))
        {
            probability = ExpectNumber(tokens, start + 1, line);
            start += 2;
        }
        else if (_blockProbabilities.TryGetValue(item.Group ?? "", out var blockProb))
        {
            probability = blockProb;
        }

        // actions : chaque groupe commence par « ; »
        while (start < tokens.Count)
        {
            if (!tokens[start].Is(";")) throw new ScriptException(line, $"texte inattendu « {tokens[start].Text} »");
            var sb = new StringBuilder();
            start++;
            while (start < tokens.Count && !tokens[start].Is(";")) AppendToken(sb, tokens[start++]);
            var text = sb.ToString().Trim();
            if (text.Length == 0) throw new ScriptException(line, "action vide");
            actions.Add(text);
        }

        script.Rules.Add(new Rule(line, label, item.Group, subject, neighbour, product, kind, condition,
            probability, actions));
    }

    private readonly Dictionary<string, double> _blockProbabilities = new(StringComparer.Ordinal);


    // -----------------------------------------------------------------------
    // process — états, durées, effets
    // -----------------------------------------------------------------------

    private void ParseProcessItem(LogicalItem item, Script script)
    {
        var line = item.Line;

        if (item.Text == "process")
        {
            script.Declarations.Add(new ProcessDecl(line, item.Subject!));
            return;
        }

        if (item.Text == "state")
        {
            var tokens = Tokenize(line, item.Group!);
            var name = tokens[0].Text;
            var index = 1;
            var color = index < tokens.Count && tokens[index].Is("[")
                ? ReadColor(tokens, ref index, line)
                : null;
            if (index < tokens.Count)
                throw new ScriptException(line, $"texte inattendu après l'état : « {tokens[index].Text} »");
            script.Declarations.Add(new StateDecl(line, item.Subject!, name, color));
            return;
        }

        var process = item.Subject
                      ?? throw new ScriptException(line, "règle de processus hors de tout # process");
        var state = item.Group
                    ?? throw new ScriptException(line, "une règle de processus doit être dans un ## état");

        var rule = Tokenize(line, item.Text);
        var head = rule[0];

        if (head.Is("stays"))
        {
            var steps = ExpectInt(rule, 1, line);
            if (rule.Count < 3 || !rule[2].Is("steps"))
                throw new ScriptException(line, "attendu « steps » après la durée");
            EnsureEnd(rule, 3, line, "durée de séjour");
            script.Declarations.Add(new StayDecl(line, process, state, steps));
            return;
        }

        if (head.Is("moves"))
        {
            var value = ExpectNumber(rule, 1, line);
            EnsureEnd(rule, 2, line, "mobilité");
            script.Declarations.Add(new StateEffectDecl(line, process, state, "moves", "", value));
            return;
        }

        if (head.Is("produces") || head.Is("absorbs") || head.Is("lets"))
        {
            var field = ExpectWord(rule, 1, line);
            var value = ExpectNumber(rule, 2, line);
            EnsureEnd(rule, 3, line, $"effet « {head.Text.ToLowerInvariant()} »");
            script.Declarations.Add(new StateEffectDecl(line, process, state, head.Text.ToLowerInvariant(), field, value));
            return;
        }

        if (head.Is("becomes"))
        {
            var target = ExpectWord(rule, 1, line);
            var (period, nextToken) = ReadPeriod(rule, 2, line);
            var (condition, end) = ReadTrailingCondition(rule, nextToken, line);
            EnsureEnd(rule, end, line, "sortie d'état");
            script.Declarations.Add(new TransitionDecl(line, process, state, target, condition, null,
                TransitionKind.Becomes, period));
            return;
        }

        if (head.Is("dies"))
        {
            var index = 1;
            int? after = null;
            if (index < rule.Count && rule[index].Is("after"))
            {
                after = ExpectInt(rule, index + 1, line);
                index += 2;
                if (index < rule.Count && rule[index].Is("steps")) index++;
            }

            var (condition, end) = ReadTrailingCondition(rule, index, line);
            EnsureEnd(rule, end, line, "sortie d'état");
            script.Declarations.Add(new TransitionDecl(line, process, state, null, condition, after,
                TransitionKind.Dies));
            return;
        }

        // a neighbour Cell becomes Infected ...
        var cursor = 0;
        if (head.Is("a") && rule.Count > 1 && rule[1].Is("neighbour")) cursor = 2;
        else if (head.Is("neighbour")) cursor = 1;

        if (cursor > 0)
        {
            var neighbour = ExpectWord(rule, cursor, line);
            if (rule.Count <= cursor + 1 || !rule[cursor + 1].Is("becomes"))
                throw new ScriptException(line, "attendu « becomes » après le type du voisin");
            var to = ExpectWord(rule, cursor + 2, line);
            var (period, next) = ReadPeriod(rule, cursor + 3, line);
            var (condition, end) = ReadTrailingCondition(rule, next, line);
            EnsureEnd(rule, end, line, "action de voisinage");
            script.Declarations.Add(new StateReactionDecl(line, process, state, neighbour, to, condition, period));
            return;
        }

        if (head.Is("remembers") || head.Is("remembersValue"))
            throw new ScriptException(line,
                "la mémoire par agent n'existe pas dans .nbd2 : une cellule ne connaît que son grain et son âge");
        if (head.Is("waits") || head.Is("await"))
            throw new ScriptException(line,
                "une attente d'événement n'existe pas dans .nbd2 : le moteur avance par pas, sans file d'échéances");
        if (head.Is("parallel") || head.Is("simultaneously"))
            throw new ScriptException(line,
                "deux rythmes dans un même état ne sont pas exprimables : découpez l'état, ou passez par un modificateur");

        throw new ScriptException(line, $"règle de processus inconnue « {head.Text} »");
    }

    /// <summary>Lit « every N steps », ou rien.</summary>
    private static (int? Period, int Next) ReadPeriod(List<Tok> tokens, int index, int line)
    {
        if (index >= tokens.Count || !tokens[index].Is("every")) return (null, index);
        var period = ExpectInt(tokens, index + 1, line);
        if (period <= 0) throw new ScriptException(line, "la période doit être strictement positive");
        var cursor = index + 2;
        if (cursor < tokens.Count && tokens[cursor].Is("steps")) cursor++;
        return (period, cursor);
    }

    /// <summary>Lit « when CONDITION » à partir de l'index, ou rien du tout.</summary>
    private static (Cond? Condition, int Next) ReadTrailingCondition(List<Tok> tokens, int index, int line)
    {
        if (index >= tokens.Count) return (null, index);
        if (!tokens[index].Is("when"))
            throw new ScriptException(line, $"texte inattendu « {tokens[index].Text} »");
        var (condition, next) = new ConditionReader(tokens, index + 1, line).ReadOr();
        return (condition, next);
    }

    /// <summary>Refuse tout ce qui reste : une forme acceptée doit être entièrement lue.</summary>
    private static void EnsureEnd(List<Tok> tokens, int index, int line, string context)
    {
        if (index >= tokens.Count) return;
        var rest = tokens[index].Text;
        if (rest == "every")
            throw new ScriptException(line,
                $"{context} : un effet est permanent, il ne peut pas avoir de rythme. " +
                "Utilisez un modificateur (« every N steps: ... » dans # modifiers)");
        throw new ScriptException(line, $"{context} : texte inattendu « {rest} »");
    }

    // -----------------------------------------------------------------------
    // start
    // -----------------------------------------------------------------------

    private static StartItem ParseStart(LogicalItem item, List<Tok> tokens)
    {
        var line = item.Line;
        if (tokens[0].Kind == TokKind.Number)
        {
            var count = ExpectInt(tokens, 0, line);
            var agent = ExpectWord(tokens, 1, line);
            if (tokens.Count < 3 || !tokens[2].Is("random"))
                throw new ScriptException(line, "attendu « random » après le nom de l'agent");
            EnsureEnd(tokens, 3, line, "état initial");
            return new StartRandom(line, count, agent);
        }

        var name = tokens[0].Text;
        if (tokens.Count >= 2 && tokens[1].Is("random")) return new StartRandom(line, 1, name);
        if (tokens.Count >= 6 && tokens[1].Is("in") && tokens[2].Is("("))
        {
            var x = ExpectInt(tokens, 3, line);
            var y = ExpectInt(tokens, 5, line);
            return new StartAt(line, name, x, y);
        }

        throw new ScriptException(line, "forme d'état initial inconnue (attendu : « 200 Cell random » ou « Cell in (x,y) »)");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>Lit « [Couleur] » et avance l'index au-delà du crochet fermant.</summary>
    private static string ReadColor(List<Tok> tokens, ref int index, int line)
    {
        if (!tokens[index].Is("[")) throw new ScriptException(line, "couleur attendue entre crochets");
        if (index + 1 >= tokens.Count) throw new ScriptException(line, "couleur absente entre les crochets");
        var color = tokens[index + 1];
        if (color.Kind != TokKind.Word)
            throw new ScriptException(line, $"couleur invalide « {color.Text} »");
        if (index + 2 >= tokens.Count || !tokens[index + 2].Is("]"))
            throw new ScriptException(line, "crochet fermant attendu après la couleur");
        index += 3;
        return color.Text;
    }

    private static Tok Expect(List<Tok> tokens, int index, int line, string what)
    {
        if (index >= tokens.Count) throw new ScriptException(line, $"attendu {what} en fin d'objet");
        return tokens[index];
    }

    private static int ExpectInt(List<Tok> tokens, int index, int line)
    {
        var token = Expect(tokens, index, line, "un nombre entier");
        if (token.Kind != TokKind.Number || token.Number != Math.Floor(token.Number))
            throw new ScriptException(line, $"attendu un entier, trouvé « {token.Text} »");
        return (int)token.Number;
    }

    private static double ExpectNumber(List<Tok> tokens, int index, int line)
    {
        var token = Expect(tokens, index, line, "un nombre");
        if (token.Kind != TokKind.Number) throw new ScriptException(line, $"attendu un nombre, trouvé « {token.Text} »");
        return token.Number;
    }

    private static string ExpectWord(List<Tok> tokens, int index, int line)
    {
        var token = Expect(tokens, index, line, "un nom");
        if (token.Kind != TokKind.Word) throw new ScriptException(line, $"attendu un nom, trouvé « {token.Text} »");
        return token.Text;
    }

    private static string ExpectString(List<Tok> tokens, int index, int line)
    {
        var token = Expect(tokens, index, line, "une chaîne entre guillemets");
        if (token.Kind != TokKind.String) throw new ScriptException(line, $"attendu une chaîne, trouvé « {token.Text} »");
        return token.Text;
    }

    // -----------------------------------------------------------------------
    // Conditions
    // -----------------------------------------------------------------------

    private sealed class ConditionReader
    {
        private readonly List<Tok> _tokens;
        private readonly int _line;
        private int _pos;

        public ConditionReader(List<Tok> tokens, int pos, int line)
        {
            _tokens = tokens;
            _pos = pos;
            _line = line;
        }

        private bool AtEnd => _pos >= _tokens.Count;

        private Tok Peek => _tokens[_pos];

        public (Cond Cond, int Next) ReadOr()
        {
            var items = new List<Cond> { ReadAnd() };
            while (!AtEnd && (Peek.Is("or") || Peek.Is("||")))
            {
                _pos++;
                items.Add(ReadAnd());
            }

            return (items.Count == 1 ? items[0] : new OrCond(items), _pos);
        }

        private Cond ReadAnd()
        {
            var items = new List<Cond> { ReadNot() };
            while (!AtEnd && (Peek.Is("and") || Peek.Is("&&")))
            {
                _pos++;
                items.Add(ReadNot());
            }

            return items.Count == 1 ? items[0] : new AndCond(items);
        }

        private Cond ReadNot()
        {
            if (!AtEnd && (Peek.Is("not") || Peek.Is("!")))
            {
                _pos++;
                return new NotCond(ReadNot());
            }

            return ReadPrimary();
        }

        private Cond ReadPrimary()
        {
            if (AtEnd) throw new ScriptException(_line, "condition incomplète");
            if (Peek.Is("("))
            {
                _pos++;
                var (inner, _) = ReadOr();
                if (AtEnd || !Peek.Is(")")) throw new ScriptException(_line, "parenthèse non fermée dans la condition");
                _pos++;
                return inner;
            }

            return ReadAtom();
        }

        private Cond ReadAtom()
        {
            var token = Peek;

            if (token.Is("finds")) return ReadFinds();

            if (token.Is("at") && _pos + 1 < _tokens.Count && _tokens[_pos + 1].Is("step"))
            {
                _pos += 2;
                var value = ExpectInt(_tokens, _pos, _line);
                _pos++;
                return new MeasureCond("step", "==", value, false);
            }

            if (token.Is("step"))
            {
                _pos++;
                var op = ReadOp();
                var value = ExpectInt(_tokens, _pos, _line);
                _pos++;
                return new MeasureCond("step", op, value, false);
            }

            if (token.Kind != TokKind.Word) throw new ScriptException(_line, $"condition inattendue « {token.Text} »");

            _pos++;
            var name = token.Text;
            var near = false;
            if (!AtEnd && Peek.Is("in"))
            {
                _pos++;
                if (AtEnd || !Peek.Is("near")) throw new ScriptException(_line, "attendu « near » après « in »");
                _pos++;
                near = true;
            }

            var comparison = ReadOp();
            var constant = ExpectNumber(_tokens, _pos, _line);
            _pos++;
            return new MeasureCond(name, comparison, constant, near);
        }

        private Cond ReadFinds()
        {
            _pos++; // finds
            if (AtEnd) throw new ScriptException(_line, "attendu un type après « finds »");

            if (Peek.Is("nothing"))
            {
                _pos++;
                return new NothingCond();
            }

            if (Peek.Is("only"))
            {
                _pos++;
                var types = new List<string>();
                while (!AtEnd && Peek.Kind == TokKind.Word)
                {
                    types.Add(Peek.Text);
                    _pos++;
                    if (!AtEnd && Peek.Is(",")) _pos++;
                    else break;
                }

                if (types.Count == 0) throw new ScriptException(_line, "attendu au moins un type après « finds only »");
                return new OnlyCond(types);
            }

            var count = (int?)null;
            if (Peek.Kind == TokKind.Number)
            {
                count = (int)Peek.Number;
                _pos++;
            }

            if (AtEnd || Peek.Kind != TokKind.Word) throw new ScriptException(_line, "attendu un type après « finds »");
            var type = Peek.Text;
            _pos++;
            var any = type is "any";

            if (count.HasValue) return new CountCond(any, any ? null : type, "==", count.Value);

            string op;
            if (!AtEnd && Peek.Kind == TokKind.Op)
            {
                op = ReadOp();
                var value = ExpectInt(_tokens, _pos, _line);
                _pos++;
                return new CountCond(any, any ? null : type, op, value);
            }

            if (!AtEnd && Peek.Is("is"))
            {
                _pos++;
                var value = ExpectInt(_tokens, _pos, _line);
                _pos++;
                return new CountCond(any, any ? null : type, "==", value);
            }

            return new CountCond(any, any ? null : type, ">=", 1);
        }

        private string ReadOp()
        {
            if (AtEnd) throw new ScriptException(_line, "attendu un opérateur de comparaison");
            if (Peek.Is("is"))
            {
                _pos++;
                return "==";
            }

            if (Peek.Kind != TokKind.Op)
                throw new ScriptException(_line, $"attendu un opérateur de comparaison, trouvé « {Peek.Text} »");
            var op = Peek.Text;
            _pos++;
            return op;
        }
    }
}
