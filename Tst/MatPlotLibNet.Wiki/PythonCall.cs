// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// A Python call a document writes beside the MatPlotLibNet call it compares with, or names as the rule a member follows:
/// <c>ax.hist(data, bins=20)</c>, <c>plt.style.use('ggplot')</c>, <c>scipy.stats.probplot(x, plot=ax)</c>, a class
/// <c>Rectangle((x, y), w, h, angle=a)</c>, a name without a call (<c>plt.gca</c>), a setting (<c>axes.labelpad</c>), a keyword alone
/// (<c>loc='best'</c>). It is read against what matplotlib 3.11.2 has (<see cref="PythonSignatures"/>): the owner has the name, the
/// call takes as many arguments by position, takes each keyword and is given each parameter it must be, a setting exists and rests at
/// the value the document writes. A call or a keyword is read wherever a document writes it: in a code span, one that wraps a line
/// included, in the words outside a span, in the lines of a tree drawn in a fence without a language, in a fence of Python, and in the
/// words of an XML comment as in its <c>&lt;c&gt;</c>.
/// </summary>
/// <param name="Document">The document's name, for the message.</param>
/// <param name="Line">The line the call stands on, from one.</param>
/// <param name="Code">The call as the document writes it.</param>
internal readonly partial record struct PythonCall(string Document, int Line, string Code)
{
    private static string[] AxesOwners => ["Axes", "PolarAxes", "Axes3D"];

    // The names a document gives the objects and modules of matplotlib, and the owners of the table each one is.
    private static readonly Dictionary<string, string[]> Receivers = new(StringComparer.Ordinal)
    {
        ["plt"] = ["matplotlib.pyplot"], ["pyplot"] = ["matplotlib.pyplot"],
        ["ax"] = AxesOwners, ["axs"] = AxesOwners, ["axes"] = AxesOwners, ["twin"] = AxesOwners, ["inset"] = AxesOwners,
        ["fig"] = ["Figure"], ["cbar"] = ["Colorbar"], ["line"] = ["Line2D"], ["spines"] = ["Spine"], ["cs"] = ["ContourSet"], ["axis"] = ["XAxis", "YAxis"],
        ["mpl"] = ["matplotlib"], ["np"] = ["numpy"], ["sns"] = ["seaborn"], ["pd"] = ["pandas"],
        ["mdates"] = ["matplotlib.dates"], ["gridspec"] = ["matplotlib.gridspec"], ["ticker"] = ["matplotlib.ticker"], ["mticker"] = ["matplotlib.ticker"],
        ["mcolors"] = ["matplotlib.colors"], ["mpatches"] = ["matplotlib.patches"],
    };

    // What an attribute of an owner is when it is no call and no module: an axis of an axes, the spines of an axes, the canvas of a figure.
    private static readonly Dictionary<string, string[]> Attributes = new(StringComparer.Ordinal)
    {
        ["Axes.xaxis"] = ["XAxis"], ["Axes.yaxis"] = ["YAxis"], ["Axes3D.zaxis"] = ["Axis"], ["Axes.spines"] = [Spines], ["Figure.canvas"] = ["FigureCanvasBase"],
        ["matplotlib.pyplot.style"] = ["matplotlib.style"], ["matplotlib.pyplot.cm"] = ["matplotlib.cm"],
    };

    // What a call gives that a document goes on calling on: the axes and the figure pyplot and a figure hand out.
    private static readonly Dictionary<string, string[]> Gives = new(StringComparer.Ordinal)
    {
        ["matplotlib.pyplot.gca"] = AxesOwners, ["matplotlib.pyplot.subplot"] = AxesOwners, ["matplotlib.pyplot.axes"] = AxesOwners,
        ["matplotlib.pyplot.gcf"] = ["Figure"], ["matplotlib.pyplot.figure"] = ["Figure"], ["Figure.add_subplot"] = AxesOwners,
        ["Figure.add_axes"] = AxesOwners, ["Axes.twinx"] = AxesOwners, ["Axes.twiny"] = AxesOwners, ["Axes.inset_axes"] = AxesOwners,
        ["Axes.colorbar"] = ["Colorbar"], ["Figure.colorbar"] = ["Colorbar"],
    };

    // The root packages of the table: a chain that starts with one names a module of it.
    private static readonly string[] Packages = ["matplotlib", "mpl_toolkits", "numpy", "seaborn", "scipy", "pandas", "squarify", "plotly", "mplfinance", "pywaffle", "joypy", "lifelines"];

    private const string Spines = "spines";

    // The words that put a span in matplotlib's hands when its shape alone does not: a keyword alone, a call written without its owner.
    [GeneratedRegex(@"\b(?:matplotlib|pyplot|seaborn|numpy|scipy|pandas|mpl_toolkits|mplot3d|rcParams)\b", RegexOptions.IgnoreCase)]
    private static partial Regex MatplotlibWords();

    // One keyword of a span that is keywords and nothing else: `loc='best'`, `nbins = 'auto'`, each of `loc='center left', bbox_to_anchor=(1, 0.5)`.
    [GeneratedRegex(@"^(?<name>[a-z_]\w*)(?<before>\s*)=(?![=>])(?<after>\s*)(?<value>\S.*)$", RegexOptions.Singleline)]
    private static partial Regex KeywordPart();

    // Where a call starts in words written outside a span: a name or a chain of names right before a bracket.
    [GeneratedRegex(@"(?<![\w.'""`\\])[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*(?=\()")]
    private static partial Regex CallStart();

    // A keyword or a setting written outside a span, told from the words around it by a value only Python writes.
    [GeneratedRegex(@"(?<![\w.'""])[a-z_]\w*(?:\.[a-z0-9_]+)*\s*=\s*(?:'[^'\n]*'|(?:True|False|None)\b)")]
    private static partial Regex WrittenKeyword();

    // The fences whose lines are read as words (a tree, a listing without a language) and the ones whose lines are Python.
    private static readonly string[] WordFences = [string.Empty, "text"];

    private static readonly string[] PythonFences = ["python", "py"];

    // The words right before a call that say it states defaults: `the default, `, `defaults to `, `default `.
    [GeneratedRegex(@"\bdefault(?:s to|s are| is|,)?\s*(?:matplotlib's\s*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex DefaultBefore();

    /// <summary>The words of the line or comment right before the call.</summary>
    public string Before { get; init; } = string.Empty;

    // The words right before a span that hand it to matplotlib: `matplotlib's `, `pyplot's `.
    [GeneratedRegex(@"\b(?:matplotlib|pyplot|seaborn|numpy|scipy|mplot3d|cartopy)(?:'s)?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex MatplotlibBefore();

    /// <summary>The line or cell the call stands in, for the words around it: a value written where the words say default is held to
    /// matplotlib's default.</summary>
    public string Context { get; init; } = string.Empty;

    // A file name: `_base.py`, `ProgressBar.json`, `wave_steps.svg`, with what may follow it after a colon.
    [GeneratedRegex(@"^[\w./-]+\.(?:py|pyi|svg|json|png|md|cs|txt|html|csv|gif|pdf|yml|yaml|xml|js|ts|ipynb|h|c|cpp|tfm|afm|ttf|otf)(?::\S*)?$")]
    private static partial Regex FileName();

    // A setting written with its value: `image.aspect: equal`, `axes.titley=None`.
    [GeneratedRegex(@"^(?<key>[a-z_][a-z0-9_]*(?:\.[a-z0-9_]+)+)\s*(?:[:=]\s*(?<value>[^,;]+?))?\s*$")]
    private static partial Regex Setting();

    /// <summary>The Python calls a markdown document writes in its prose and its tables.</summary>
    /// <param name="document">The document's name.</param>
    /// <param name="text">The document's text.</param>
    /// <returns>The calls, in the order they stand.</returns>
    public static IReadOnlyList<PythonCall> InlineOf(string document, string text)
    {
        var lines = SourceFile.MarkdownLinesOf(text);
        var fences = SourceFile.FenceBlocksOf(text);
        var texts = MemberClaim.TextsOf(lines).ToList();

        var spans = texts
            .SelectMany(each => SourceFile.CodeSpansOf(each.Text).Select(span => (each.Number, span.Code, Context: each.Text + " " + each.Column, Before: each.Text[..span.Index])))
            .Where(each => IsPython(each.Code, each.Context, each.Before))
            .Select(each => new PythonCall(document, each.Number, each.Code) { Context = each.Context, Before = each.Before });
        var words = texts.SelectMany(each => WrittenIn(document, each.Number, SourceFile.WithoutCodeSpans(each.Text), string.Empty));
        var fenced = lines.Where(line => line.InFence).SelectMany(line => fences[line.Block].Open.Info.ToLowerInvariant() switch
        {
            var info when WordFences.Contains(info) => WrittenIn(document, line.Number, line.Text, string.Empty),
            var info when PythonFences.Contains(info) => WrittenIn(document, line.Number, line.Text, "matplotlib"),
            _ => [],
        });

        return [.. spans.Concat(words).Concat(fenced).OrderBy(call => call.Line)];
    }

    /// <summary>The Python calls the XML comments of a source file write, in <c>&lt;c&gt;</c> and in the words around it.</summary>
    /// <param name="file">The source file.</param>
    /// <returns>The calls, in the order they stand.</returns>
    public static IReadOnlyList<PythonCall> InlineOf(SourceFile file)
    {
        var codes = XmlDocumentationCode.Of(file).Where(code => !code.IsBlock).ToList();

        // The words around a span of a comment are the comment with each of its spans written as a document writes one, so a class the
        // comment names in a span of its own is read as a line of a document names it.
        var contexts = codes.GroupBy(code => code.Context).ToDictionary(comment => comment.Key, comment => comment.Key + string.Concat(comment.Select(code => $" `{code.Code}`")));

        return
        [
            .. codes.Where(code => IsPython(code.Code, contexts[code.Context], code.Before))
                .Select(code => new PythonCall(file.Path, code.Line, code.Code) { Context = contexts[code.Context], Before = code.Before })
                .Concat(XmlDocumentationCode.WordsOf(file).SelectMany(each => WrittenIn(file.Path, each.Number, each.Text, string.Empty)))
                .OrderBy(call => call.Line),
        ];
    }

    // The Python a text writes outside a code span: each call that is Python by its shape or by the words before it (in a fence of Python,
    // every call), and each keyword left outside those calls whose value only Python writes.
    private static IEnumerable<PythonCall> WrittenIn(string document, int line, string text, string context)
    {
        var rest = new System.Text.StringBuilder(text);

        for (var start = CallStart().Match(text); start.Success;)
        {
            var end = EndOfChain(text, start.Index + start.Length);
            var code = end > 0 ? text[start.Index..end] : null;

            if (code is null || !IsPython(code, context, text[..start.Index]))
            {
                start = start.NextMatch();
                continue;
            }

            yield return new PythonCall(document, line, code) { Context = context, Before = text[..start.Index] };

            rest.Remove(start.Index, code.Length).Insert(start.Index, " ", code.Length);
            start = CallStart().Match(text, end);
        }

        foreach (Match keyword in WrittenKeyword().Matches(rest.ToString()))
        {
            yield return new PythonCall(document, line, keyword.Value) { Context = context, Before = text[..keyword.Index] };
        }
    }

    // Where the chain that calls at the index ends: past each call, index and name that follows; -1 when a bracket never closes.
    private static int EndOfChain(string text, int at)
    {
        while (at < text.Length)
        {
            if (text[at] is '(' or '[')
            {
                var close = PythonText.Close(text, at);

                if (close < 0)
                {
                    return -1;
                }

                at = close + 1;
            }
            else if (text[at] == '.' && PythonText.NameAt(text, at + 1) > 0)
            {
                at += 1 + PythonText.NameAt(text, at + 1);
            }
            else
            {
                break;
            }
        }

        return at;
    }

    // The keywords of a span that is keywords and nothing else, or null for any other span: an attribute of markup (`role="img"`), a
    // statement of matplotlib's source or a formula (`pad = self._pad + 7`: written with blanks, only a literal or a list makes a
    // keyword), a property of C# (`Visible = false`, `Kind = None`: a keyword starts in lower case, `cellLoc='right'`; the few of
    // matplotlib that start with a capital, `NFFT`, are held where a call writes them), a value that is neither a name, a literal, a call
    // nor a tuple.
    private static IReadOnlyList<(string Name, string Value, bool Blanks)>? KeywordsOf(string code)
    {
        var found = new List<(string Name, string Value, bool Blanks)>();

        foreach (var part in PythonText.TopLevelParts(code))
        {
            if (KeywordPart().Match(part) is not { Success: true } match)
            {
                return null;
            }

            var value = match.Groups["value"].Value.Trim();
            var blanks = match.Groups["before"].Length + match.Groups["after"].Length > 0;

            if (value.StartsWith('"') || (blanks && !PythonText.IsPythonOnly(value) && !PythonText.IsLiteral(value) && !PythonText.IsBracketed(value))
                || (value.Contains('(', StringComparison.Ordinal) && PythonText.Chain(value) is null && !PythonText.IsBracketed(value)))
            {
                return null;
            }

            found.Add((match.Groups["name"].Value, value, blanks));
        }

        return found.Count > 0 ? found : null;
    }

    // Whether the words around a span put it in matplotlib's hands: they name matplotlib or a package beside it, or a span of them is the
    // name of a class matplotlib has and the packages do not (`FancyBboxPatch`'s `rounding_size=0.2`).
    private static bool InMatplotlib(string context) =>
        MatplotlibWords().IsMatch(context)
        || SourceFile.CodeSpansOf(context).Select(span => span.Code)
            .Any(name => name.Length > 1 && char.IsAsciiLetterUpper(name[0]) && name.All(letter => char.IsAsciiLetterOrDigit(letter) || letter == '_')
                         && PythonSignatures.Matplotlib.Named(name).Count > 0 && !PackageSurface.Loaded.IsType(name));

    /// <summary>Whether a span is Python by its shape alone: a keyword given with <c>=</c>, a string in single quotes, <c>True</c>,
    /// <c>False</c> or <c>None</c>, or a name in lower case read on a name a document gives matplotlib (<c>ax.plot</c>).</summary>
    /// <param name="code">The span.</param>
    /// <returns>True for Python.</returns>
    public static bool IsPython(string code) => IsPython(code, string.Empty);

    /// <summary>Whether a span is Python: by its shape on a name a document gives matplotlib or one of the packages beside it, and for
    /// what has no such name (a keyword alone, a setting, a call written without its owner, a call on a name the document gives an object)
    /// by the words around it, the line or the column header naming matplotlib.</summary>
    /// <param name="code">The span.</param>
    /// <param name="context">The line or cell it stands in, and the header of its column.</param>
    /// <returns>True for Python.</returns>
    public static bool IsPython(string code, string context) => IsPython(code, context, string.Empty);

    /// <summary>Whether a span is Python, as <see cref="IsPython(string, string)"/> says, or because the words right before it hand it to
    /// matplotlib (<c>matplotlib's `FixedLocator(boundaries, nbins)`</c>, a class the packages have one of the same name of).</summary>
    /// <param name="code">The span.</param>
    /// <param name="context">The line or cell it stands in, and the header of its column.</param>
    /// <param name="before">The text of the line before the span.</param>
    /// <returns>True for Python.</returns>
    public static bool IsPython(string code, string context, string before)
    {
        var inMatplotlib = InMatplotlib(context);

        if (MatplotlibBefore().IsMatch(before) && PythonText.Chain(code) is { } named && named[0].Name is { } owner
            && (Receivers.ContainsKey(owner) || Packages.Contains(owner) || PythonSignatures.Matplotlib.Named(owner).Count > 0 || PythonSignatures.Matplotlib.IsSetting(code.Trim())))
        {
            return true;
        }

        if (FileName().IsMatch(code))
        {
            return false;
        }

        if (KeywordsOf(code) is { } keywords)
        {
            // A keyword whose value only Python writes (`loc='best'`, `clip_on=False`, `subs=(1.0,)`) or is a call of matplotlib
            // (`norm=LogNorm()`) is Python wherever it stands.
            return inMatplotlib || keywords.Any(keyword => PythonText.IsPythonOnly(keyword.Value) || (!keyword.Blanks && PythonText.IsBracketed(keyword.Value))
                                                           || (keyword.Value.Contains('(', StringComparison.Ordinal) && PythonText.Chain(keyword.Value) is [{ Name: { } valueCall }, ..]
                                                               && PythonSignatures.Matplotlib.Named(valueCall).Count > 0));
        }

        if (Setting().IsMatch(code))
        {
            // A setting matplotlib has (`axes.labelpad`), or a dotted name given a value Python alone writes, is Python wherever it stands.
            return inMatplotlib || Receivers.ContainsKey(code.Split('.')[0]) || Packages.Contains(code.Split('.')[0])
                   || Setting().Match(code) is var setting && (PythonSignatures.Matplotlib.IsSetting(setting.Groups["key"].Value) || PythonText.IsPythonOnly(setting.Groups["value"].Value));
        }

        if (PythonText.Chain(code) is not { } chain || chain[0].Name is not { } first)
        {
            return false;
        }

        var lowerMember = chain.Skip(1).Any(part => part.Name is { } name && char.IsAsciiLetterLower(name.TrimStart('_')[0]));
        var pythonArguments = chain.Any(part => part.Arguments is { } arguments && (arguments.Keywords.Count > 0 || arguments.PythonLiterals));
        var known = Receivers.ContainsKey(first) || Packages.Contains(first);
        var called = chain.Count > 1 && chain[1].Arguments is not null;

        // A formula of one-letter functions, S(m) or w(k), is no call of anything.
        if (first.Length == 1 && chain.Count <= 2)
        {
            return false;
        }

        // A JavaScript object written as an argument (`{ chartId, axesIndex }`) or an arrow function is no Python.
        if (chain.Any(part => part.Arguments is { Script: true }))
        {
            return false;
        }

        // A class or function of matplotlib written bare beside matplotlib's name (`AutoMinorLocator(n)`), which the packages do not have.
        var matplotlibName = called && inMatplotlib && !PackageSurface.Loaded.IsType(first) && PythonSignatures.Matplotlib.Named(first).Count > 0;

        return known ? lowerMember || pythonArguments : pythonArguments || matplotlibName || (inMatplotlib && (lowerMember || (called && char.IsAsciiLetterLower(first.TrimStart('_')[0]))));
    }

    /// <summary>What is wrong with the call against the table, or null when matplotlib has it as the document writes it.</summary>
    /// <param name="signatures">The table.</param>
    /// <returns>A line saying where the document writes a call matplotlib does not have, or null.</returns>
    public string? Missing(PythonSignatures signatures)
    {
        if (KeywordsOf(Code) is { } keywords)
        {
            var whole = this;

            // A value that is a call (`norm=LogNorm()`) is a call of matplotlib too.
            return keywords.Select(keyword => !signatures.IsKeyword(keyword.Name) ? whole.Said($"no call of matplotlib takes the keyword {keyword.Name}")
                : PythonText.Chain(keyword.Value) is { Count: > 1 } && keyword.Value.Contains('(', StringComparison.Ordinal) ? (whole with { Code = keyword.Value }).Missing(signatures) : null)
                .FirstOrDefault(said => said is not null);
        }

        var setting = Setting().Match(Code);

        if (setting.Success && signatures.IsSetting(setting.Groups["key"].Value))
        {
            return SettingMissing(signatures, setting.Groups["key"].Value, setting.Groups["value"]);
        }

        if (PythonText.Chain(Code) is not { } chain)
        {
            return Said($"matplotlib has no setting {setting.Groups["key"].Value}");
        }

        var first = chain[0].Name!;
        int at;
        IReadOnlyList<Place> places;

        if (Receivers.TryGetValue(first, out var known))
        {
            places = [.. known.Select(owner => new Place(owner, null))];
            at = 1;
        }
        else if (ModuleOf(chain, signatures) is { } module)
        {
            places = [new Place(module.Owner, null)];
            at = module.Parts;
        }
        else if (Packages.Contains(first))
        {
            return Said($"{first} has no {chain.ElementAtOrDefault(1).Name ?? "such call"}");
        }
        else
        {
            // A call written bare, or on a name the document gives an object of its own (`twin.yaxis.tick_left()`): the first name the
            // table holds, from the start, wherever it stands in the table.
            if (chain.Count > 1 && chain[1] is { Name: null, Arguments: null } && signatures.Named(first).Count == 0)
            {
                return null;
            }

            var start = chain.Count > 1 && chain[1].Name is not null && signatures.Named(first).Count == 0 ? 1 : 0;
            var name = chain[start].Name!;
            var named = signatures.Named(name);

            if (named.Count == 0)
            {
                return Said($"matplotlib has no {name}");
            }

            places = [.. named.Select(each => new Place(each.Owner, each.Key.Contains('.', StringComparison.Ordinal) ? each.Key[..each.Key.LastIndexOf('.')] : null))];
            at = start;
        }

        for (; at < chain.Count; at++)
        {
            var part = chain[at];

            if (part.Name is null)
            {
                // An index of the spines is one spine; any other index or call of a value leaves what follows unread.
                if (places.Any(place => place.Owner == Spines))
                {
                    places = [new Place("Spine", null)];
                    continue;
                }

                return null;
            }

            var arguments = chain.ElementAtOrDefault(at + 1).Arguments;
            var module = places.Select(place => place.Class is null ? place.Owner + "." + part.Name : null).OfType<string>().FirstOrDefault(signatures.Owners.Contains);
            var attribute = places.SelectMany(place => Attributes.TryGetValue((place.Class ?? place.Owner) + "." + part.Name, out var owners) ? owners : []).ToList();

            // A module of a package (`mpl.colors`) or an attribute that is an object of the table (`ax.xaxis`).
            if (arguments is null && (module is not null || attribute.Count > 0))
            {
                places = module is not null ? [new Place(module, null)] : [.. attribute.Select(owner => new Place(owner, null))];
                continue;
            }

            var found = places.Select(place => (Place: place, Member: signatures.Of(place.Owner, place.Class is null ? part.Name : place.Class + "." + part.Name)))
                .Where(each => each.Member is not null).Select(each => (each.Place, Member: each.Member!)).ToList();

            // A class of another module of the same name (`Axis.draw_grid` is the 3-D axis'): any class of the name that has the member.
            if (found.Count == 0 && at > 0 && chain[at - 1].Name is { } holder)
            {
                found = [.. signatures.Named(part.Name).Where(each => each.Key == holder + "." + part.Name)
                    .Select(each => (Place: new Place(each.Owner, holder), Member: signatures.Of(each.Owner, each.Key)!))];
            }

            if (found.Count == 0)
            {
                return Said($"{places[0].Class ?? places[0].Owner} has no {part.Name}");
            }

            if (arguments is not { } written)
            {
                return null;
            }

            var calls = found.Where(each => each.Member.IsCall).ToList();

            var stated = DefaultBefore().IsMatch(Before);

            if (calls.Count > 0 && !calls.Any(each => Unfit(string.Empty, each.Member, written, signatures, stated) is null))
            {
                return Said(Unfit(part.Name, calls[0].Member, written, signatures, stated)!);
            }

            // What the call gives: an instance of a class it constructs, the axes or the figure pyplot hands out; anything else leaves what
            // follows unread.
            var classes = found.Where(each => each.Place.Class is null && each.Member.Bases.Count > 0).Select(each => new Place(each.Place.Owner, part.Name)).ToList();
            places = classes.Count > 0
                ? classes
                : [.. found.SelectMany(each => Gives.TryGetValue((each.Place.Class ?? each.Place.Owner) + "." + part.Name, out var owners) ? owners : []).Distinct().Select(owner => new Place(owner, null))];
            at++;

            if (places.Count == 0)
            {
                return null;
            }
        }

        return null;
    }

    // A setting the document writes with a value: the value is the default under one of the styles, a string compared without its quotes.
    private string? SettingMissing(PythonSignatures signatures, string key, Group value)
    {
        if (!value.Success)
        {
            return null;
        }

        var written = value.Value.Trim().Trim('\'', '"');
        var held = signatures.SettingValues(key);
        var fits = held.Any(each => each.Trim('\'', '"') == written
                                    || (double.TryParse(each, System.Globalization.CultureInfo.InvariantCulture, out var number)
                                        && double.TryParse(written, System.Globalization.CultureInfo.InvariantCulture, out var given) && number == given));

        return fits ? null : Said($"matplotlib's {key} rests at {string.Join(" and under classic at ", held)}, the document writes {written}");
    }

    // The longest run of names at the start of the chain that the table holds as a module: scipy.stats, matplotlib.ticker.
    private static (string Owner, int Parts)? ModuleOf(IReadOnlyList<PythonPart> chain, PythonSignatures signatures)
    {
        for (var parts = chain.TakeWhile(part => part.Name is not null).Count(); parts > 0; parts--)
        {
            var name = string.Join('.', chain.Take(parts).Select(part => part.Name));

            foreach (var module in new[] { name, "matplotlib." + name, "mpl_toolkits.mplot3d." + name })
            {
                if (signatures.Owners.Contains(module) && !Receivers.ContainsKey(name))
                {
                    return (module, parts);
                }
            }
        }

        return null;
    }

    // What does not fit between a call and a member's signature, in words; null when it fits.
    private static string? Unfit(string name, PythonMember member, PythonArguments arguments, PythonSignatures signatures, bool stated)
    {
        var signature = member.Signature!.Value;

        if (arguments.Keywords.FirstOrDefault(keyword => !member.Takes(keyword, signatures)) is { } unknown)
        {
            return $"{name} takes no keyword {unknown}";
        }

        if (!arguments.Open && signature.MostPositional is { } most && arguments.Positional > most)
        {
            return $"{name} takes at most {most} arguments, the document gives {arguments.Positional}";
        }

        // A call that writes no positions names the call by the keywords it shows (`imshow(aspect='auto')`, `bar3d()`): what it must be
        // given is read only of a call that writes its positions out.
        if (!arguments.Open && arguments.Positional > 0 && signature.MissingFrom(arguments.Positional, arguments.Keywords) is { } missing)
        {
            return $"{name} must be given {missing}";
        }

        // Where the words say default, a keyword's value is the default matplotlib has (`the default, matplotlib's Quiver(color='k')`).
        if (stated)
        {
            return arguments.Keywords.Zip(arguments.Values)
                .Select(each => (Keyword: each.First, Written: each.Second, Held: signature.Parameters.FirstOrDefault(parameter => parameter.Name == each.First).Default))
                .Where(each => each.Held is not null && PythonText.IsLiteral(each.Written) && !PythonText.SameValue(each.Held, each.Written))
                .Select(each => $"the default of {name}'s {each.Keyword} is {each.Held}, the document writes {each.Written}")
                .FirstOrDefault();
        }

        return null;
    }

    private string Said(string what) => $"{Document}, line {Line}: `{Code}`: {what}";

    /// <summary>Where a name of a chain is read: an owner of the table, and the class of it an instance of which the chain holds.</summary>
    private readonly record struct Place(string Owner, string? Class);
}

/// <summary>One part of a Python chain: a name, the arguments of a call, or an index (neither).</summary>
/// <param name="Name">The name, or null for a call or an index.</param>
/// <param name="Arguments">The arguments of a call, or null.</param>
internal readonly record struct PythonPart(string? Name, PythonArguments? Arguments);

/// <summary>The arguments of a Python call as a document writes them.</summary>
/// <param name="Positional">How many it gives by position.</param>
/// <param name="Keywords">The keywords it gives.</param>
/// <param name="Open">Whether it writes a placeholder for further arguments (<c>…</c>), or unpacks a list (<c>*args</c>).</param>
/// <param name="PythonLiterals">Whether an argument is Python and no C#: a string in single quotes, True, False or None.</param>
/// <param name="Values">The value written for each keyword, in the order of <paramref name="Keywords"/>.</param>
/// <param name="Script">Whether an argument is JavaScript and no Python: an object of names alone (<c>{ chartId, axesIndex }</c>) or an arrow function.</param>
internal readonly record struct PythonArguments(int Positional, IReadOnlyList<string> Keywords, bool Open, bool PythonLiterals, IReadOnlyList<string> Values, bool Script);

/// <summary>Python as a document writes it, read into the parts a call check needs.</summary>
internal static partial class PythonText
{
    [GeneratedRegex(@"^[A-Za-z_]\w*")]
    private static partial Regex Name();

    // An object of JavaScript written with its names alone, `{ chartId, axesIndex }`, at any depth.
    [GeneratedRegex(@"\{\s*[A-Za-z_]\w*(?:\s*,\s*[A-Za-z_]\w*)+\s*\}")]
    private static partial Regex ScriptObject();

    // A Python statement that gives the call's value a name, `ticks = FixedLocator(...)` (a keyword, `levels=N`, is written without
    // the blanks): the call is what is read.
    [GeneratedRegex(@"^[a-z_]\w*\s+=(?!=)\s+")]
    private static partial Regex Named();

    [GeneratedRegex(@"^([A-Za-z_]\w*)=(?![=>])")]
    private static partial Regex Keyword();

    [GeneratedRegex(@"'[^']{2,}'|(?<![.\w])(?:True|False|None)\b")]
    private static partial Regex PythonLiteral();

    [GeneratedRegex(@"^(?:'[^']*'|True|False|None)$")]
    private static partial Regex PythonOnly();

    /// <summary>How long the name that starts at the index is; 0 when none starts there.</summary>
    /// <param name="text">The text.</param>
    /// <param name="at">The index.</param>
    /// <returns>The length of the name.</returns>
    public static int NameAt(string text, int at) => at < text.Length ? Name().Match(text[at..]).Length : 0;

    /// <summary>Whether a value is one only Python writes: a string in single quotes, True, False or None, and nothing after it.</summary>
    /// <param name="written">The value.</param>
    /// <returns>True for a value of Python.</returns>
    public static bool IsPythonOnly(string written) => PythonOnly().IsMatch(written.Trim());

    /// <summary>Whether a value is a tuple or a list written whole: <c>(1.0,)</c>, <c>[1, 2, 5, 10]</c>.</summary>
    /// <param name="written">The value.</param>
    /// <returns>True when the bracket it opens with closes at its end.</returns>
    public static bool IsBracketed(string written) => written.Trim() is { Length: > 1 } text && text[0] is '(' or '[' && Close(text, 0) == text.Length - 1;

    /// <summary>The parts of a chain of names, calls and indexes, <c>ax.spines['top'].set_visible(False)</c>; null for text that is no such
    /// chain (a formula, prose, a chain that does not start with a name).</summary>
    /// <param name="code">The span.</param>
    /// <returns>The parts, or null.</returns>
    public static IReadOnlyList<PythonPart>? Chain(string code)
    {
        var text = Named().Replace(code.Trim(), string.Empty, 1);
        var parts = new List<PythonPart>();
        var at = 0;

        if (Name().Match(text) is not { Success: true } first)
        {
            return null;
        }

        parts.Add(new PythonPart(first.Value, null));
        at = first.Length;

        while (at < text.Length)
        {
            if (text[at] == '.' && Name().Match(text[(at + 1)..]) is { Success: true } next)
            {
                parts.Add(new PythonPart(next.Value, null));
                at += 1 + next.Length;
            }
            else if (text[at] is '(' or '[' && Close(text, at) is var end and > 0)
            {
                parts.Add(new PythonPart(null, text[at] == '(' ? Arguments(text[(at + 1)..end]) : null));
                at = end + 1;
            }
            else
            {
                return null;
            }
        }

        return parts;
    }

    // The arguments between the brackets of a call.
    private static PythonArguments Arguments(string text)
    {
        var parts = TopLevelParts(text);
        var keywords = parts.Select(part => Keyword().Match(part)).Where(match => match.Success).Select(match => match.Groups[1].Value).ToList();
        var values = parts.Where(part => Keyword().IsMatch(part)).Select(part => part[(part.IndexOf('=', StringComparison.Ordinal) + 1)..].Trim()).ToList();
        var open = parts.Any(part => part is "…" or "..." || part.StartsWith('*'));
        var positional = parts.Count(part => part is not ("…" or "...") && !part.StartsWith('*') && !Keyword().IsMatch(part));

        var script = parts.Any(part => part.Contains("=>", StringComparison.Ordinal)) || ScriptObject().IsMatch(text);

        return new PythonArguments(positional, keywords, open, PythonLiteral().IsMatch(text), values, script);
    }

    /// <summary>Whether a value a document writes is a literal: a number, a string in quotes, True, False or None (a name is a value of the
    /// document's own, no default).</summary>
    /// <param name="written">The value.</param>
    /// <returns>True for a literal.</returns>
    public static bool IsLiteral(string written) =>
        written.Trim() is var text && (text.StartsWith('\'') || text.StartsWith('"') || text is "True" or "False" or "None"
                                       || double.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out _));

    /// <summary>Whether a value a document writes is the default a signature holds: the same text, quotes of either kind alike, or the
    /// same number.</summary>
    /// <param name="held">The default, as the table writes it; null for a parameter without one.</param>
    /// <param name="written">The value the document writes.</param>
    /// <returns>True when they are one value.</returns>
    public static bool SameValue(string? held, string written)
    {
        if (held is null)
        {
            return false;
        }

        var a = held.Trim().Trim('\'', '"');
        var b = written.Trim().Trim('\'', '"');

        return a == b || (double.TryParse(a, System.Globalization.CultureInfo.InvariantCulture, out var x) && double.TryParse(b, System.Globalization.CultureInfo.InvariantCulture, out var y) && x == y);
    }

    /// <summary>The parts of a list written between commas, a comma inside brackets or a string no cut, each trimmed; none for an empty list.</summary>
    /// <param name="text">The text between the brackets.</param>
    /// <returns>The parts.</returns>
    public static IReadOnlyList<string> TopLevelParts(string text)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        char? quote = null;

        for (var at = 0; at < text.Length; at++)
        {
            var c = text[at];

            if (quote is { } open)
            {
                if (c == '\\')
                {
                    at++;
                }
                else if (c == open)
                {
                    quote = null;
                }

                continue;
            }

            if (c is '\'' or '"')
            {
                quote = c;
            }
            else if (c is '(' or '[' or '{')
            {
                depth++;
            }
            else if (c is ')' or ']' or '}')
            {
                depth--;
            }
            else if (c == ',' && depth == 0)
            {
                parts.Add(text[start..at].Trim());
                start = at + 1;
            }
        }

        parts.Add(text[start..].Trim());

        return [.. parts.Where(part => part.Length > 0)];
    }

    /// <summary>The index of the bracket that closes the one at the index, strings skipped; -1 when it never closes.</summary>
    /// <param name="text">The text.</param>
    /// <param name="open">The index of the opening bracket.</param>
    /// <returns>The index of the closing bracket.</returns>
    public static int Close(string text, int open)
    {
        var depth = 0;
        char? quote = null;

        for (var at = open; at < text.Length; at++)
        {
            var c = text[at];

            if (quote is { } inside)
            {
                quote = c == inside ? null : inside;
                continue;
            }

            if (c is '\'' or '"')
            {
                quote = c;
            }
            else if (c is '(' or '[' or '{')
            {
                depth++;
            }
            else if (c is ')' or ']' or '}' && --depth == 0)
            {
                return at;
            }
        }

        return -1;
    }
}
