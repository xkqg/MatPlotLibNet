// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A line of the documentation that holds a number one of the owners it is read against holds too, and is kept: the number
/// is another quantity (matplotlib's own value, or the start of a setting no owner holds).</summary>
/// <param name="Path">The file, from the repository root, with forward slashes.</param>
/// <param name="Fragment">A piece of the line that finds it.</param>
/// <param name="Reason">Why the number is not the owner's.</param>
internal readonly record struct KeptProse(string Path, string Fragment, string Reason);

/// <summary>The start of a setting typed again in the XML documentation. A start has one owner: a constant, whatever its name; a
/// static value that rests at it (a rule made with a <c>Resting</c> member, by position, by name or <c>with</c> one, a record whose type
/// is a <c>Resting</c> one, a width in a unit such as <c>0.8.PointsInPixels</c>); or the literal a property starts from. A sentence that
/// says "Default 20", "20 unless said otherwise", "reads 20", "20 included" or, in a wire form, "absent for 20" spells the number a second
/// time, and goes stale the day the owner moves; it names the owner with a <c>cref</c> instead (or says what the start means in words).
/// An owner belongs to the type that declares it, whatever file that type stands in, and a <c>///</c> line is read against the owners of
/// every type the type it stands in names: itself, the types around it, and every type its code or its documentation names in any of
/// its files, so a line is judged the same however its type is split over files or its members are laid out. The notes of the wire
/// forms under <c>Serialization</c> are read too, where an absent field's number is a resting value by definition.
/// <para>Not read as a start: the type's own zero and <c>false</c>; the literal the member a line documents starts from (its own start,
/// stated where it stands); and a number that is text rather than a value: in a code span with other words (<c>size=5</c>), quoted, a
/// version, a power, a percentage, a numbered case "(2)", a compound such as "3-D", or a number of matplotlib's "default style".</para></summary>
internal sealed class RestingProse
{
    private const string Serialization = "Src/MatPlotLibNet/Serialization/";

    private static readonly CSharpParseOptions Options = new(LanguageVersion.Preview);
    private static readonly Regex Name = new(@"^(?:Resting|Default)\w*$", RegexOptions.CultureInvariant);
    private static readonly Regex Identifier = new(@"\b[A-Z]\w*\b", RegexOptions.CultureInvariant);
    private static readonly Regex Literal = new(@"^(?:-?[\d_]+(?:\.[\d_]+)?[dDfFmM]?|true|false|[A-Z]\w*\.[A-Z]\w*)$", RegexOptions.CultureInvariant);
    private static readonly Regex Alias = new(@"^(?:(?:\w+\.)*?(\w+)\.)?([A-Za-z_]\w*)$", RegexOptions.CultureInvariant);

    /// <summary>A value as the documentation types it: a <c>langword</c> flag, a <c>cref</c> to an enum member (a <c>cref</c> to a
    /// <c>Resting</c> or <c>Default</c> constant is the spelling asked for, never a value), a number or an enum member's name that is the
    /// whole of a <c>&lt;c&gt;</c>, a bare number (not in a code span with other words, part of a word, a compound such as "3-D", a
    /// version, a power, a percentage, a numbered case, a hex colour, a path, a product or a quoted text of matplotlib's) or a bare flag.</summary>
    private const string Value =
        @"(?:<see\s+langword=""(?<w>true|false)""\s*/>|<see\s+cref=""(?:[\w.]+\.)?(?<e>[A-Z]\w*\.(?!Resting|Default)[A-Z]\w*)""\s*/>|<c>(?<c>-?\d[\d,]*(?:\.\d+)?)</c>"
        + @"|<c>(?<m>[A-Z]\w*)</c>|(?<![\w.\-#/'])(?<!\bversion\s|<c>[^<]*)(?!(?<=\()-?\d[\d,]*(?:\.\d+)?\))(?<n>-?\d[\d,]*(?:\.\d+)?)"
        + @"(?![\w%^]|\s%|-\w|\.\d|,\d|\s*[x×*/+]\s*\d)|\b(?<b>true|false)\b)";

    /// <summary>The words of a wire form that say a field is left out of the JSON at a value.</summary>
    private const string Absent = @"(?:\b[Aa]bsent (?:for|at|reads as)|\b[Ll]eft out (?:for|at)|\bleaves? it out (?:for|at))";

    /// <summary>The words that say a value is where a setting starts, before the value ("default" that closes a phrase about the value
    /// before it, names matplotlib's style, or is a theme's name in a code span is not one).</summary>
    private const string Before =
        @"(?:(?<!<c>)\b[Dd]efaults?\b(?:,(?=\s*\d)|(?![,)]|" + Style + @")(?:\s+(?:is|to|of))?:?)|" + Absent + @"|\breads|\brests? at|\bresting at|(?<!/)\belse|\bUnset:"
        + @"|\bstarts? at|library's own themes?|\btold none:?)";

    /// <summary>matplotlib's default style, which "default" names rather than a start.</summary>
    private const string Style = @"\s+(?:style|one)\b";

    /// <summary>The words that say a value is where a setting starts, after the value ("the default" that names matplotlib's style, or
    /// goes on to the value it is, is not one).</summary>
    private const string After =
        @"(?:by default|\bunless\b|\(the default\)|,? the default\b(?!" + Style + @"|\s*(?:<see|<c>|-?\d))|\(default\)|\bincluded\b|\bamong them\b|\bat rest\b|under every other\b|under the library's own)";

    private static readonly Regex StartBefore = new(Before + @"[^.;\d]{0,24}?" + Value, RegexOptions.CultureInvariant);
    private static readonly Regex StartAfter = new(Value + @"[^.;\d]{0,16}?" + After, RegexOptions.CultureInvariant);
    private static readonly Regex WireNote = new(Absent + @"\s+(?:the default(?:\s+of)?\s+)?" + Value, RegexOptions.CultureInvariant);
    private static readonly Regex[] Rules = [StartBefore, StartAfter];
    private static readonly Regex[] WireRules = [StartBefore, StartAfter, WireNote];

    private readonly IReadOnlyList<(SourceFile File, SyntaxNode Root)> _sources;
    private readonly IReadOnlyList<KeptProse> _kept;
    private readonly Dictionary<(string Type, string Name), string> _declared = [];
    private readonly Dictionary<string, string> _outer = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _named = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, string>> _own = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<string>> _parameters = new(StringComparer.Ordinal);

    /// <summary>Reads the owners of <paramref name="sources"/>, by the type that declares each, and the types each type names; the
    /// lines of <paramref name="kept"/> are not findings.</summary>
    /// <param name="sources">The files to read.</param>
    /// <param name="kept">The lines that hold an owner's number as another quantity.</param>
    public RestingProse(IEnumerable<SourceFile> sources, IReadOnlyList<KeptProse> kept)
    {
        _sources = [.. sources.Select(file => (file, CSharpSyntaxTree.ParseText(file.Text, Options, file.Path).GetRoot()))];
        _kept = kept;
        foreach (var record in _sources.SelectMany(source => source.Root.DescendantNodes().OfType<RecordDeclarationSyntax>()).Where(r => r.ParameterList is not null))
        {
            _parameters[record.Identifier.ValueText] = [.. record.ParameterList!.Parameters.Select(p => p.Identifier.ValueText)];
        }

        foreach (var type in _sources.SelectMany(source => source.Root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()).Where(IsNamed))
        {
            string name = type.Identifier.ValueText;
            var around = type.Ancestors().OfType<BaseTypeDeclarationSyntax>().Where(IsNamed).Select(t => t.Identifier.ValueText).ToList();
            if (around.Count > 0)
            {
                _outer[name] = around[0];
            }

            var named = _named.TryGetValue(name, out var known) ? known : _named[name] = [name];
            named.UnionWith(around);
            named.UnionWith(Identifier.Matches(type.ToFullString()).Select(m => m.Value));
            foreach (var (member, start) in StartsOf(type))
            {
                _declared[(name, member)] = start;
            }
        }
    }

    /// <summary>The value the owner <paramref name="name"/> of <paramref name="type"/> holds, written as the documentation is read (a
    /// number in its shortest form, a flag, an enum member), following an owner that reads another; null for none or no literal.</summary>
    /// <param name="type">The type that declares the owner, by its name.</param>
    /// <param name="name">The owner: a member, or a member and the part of a resting record it holds (<c>Rests.Alpha</c>).</param>
    /// <returns>The value, or null.</returns>
    public string? ValueOf(string type, string name) => ValueOf(type, name, 0);

    /// <summary>Every <c>///</c> line that types a start an owner holds, and every kept line that no longer holds one.</summary>
    /// <returns>One sentence per finding, naming the file, the line and the owner.</returns>
    public IReadOnlyList<string> Violations()
    {
        var findings = new List<string>();
        var used = new HashSet<KeptProse>();
        foreach (var (file, root) in _sources)
        {
            var lines = file.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (!file.IsDocumentation(i))
                {
                    continue;
                }

                int start = file.StartOf(i);

                // A sentence that runs on is read with the start of the next line: "Default" at the end of this one finds its value there.
                string text = i + 1 < lines.Count && file.IsDocumentation(i + 1) ? line + " " + lines[i + 1].TrimStart()[3..].TrimStart() : line;
                var own = OwnOf(root.FindToken(start + line.Length - line.TrimStart().Length).Parent ?? root);
                var rules = file.Path.StartsWith(Serialization, StringComparison.Ordinal) ? WireRules : Rules;
                string? owner = OwnerTyped(text, line.Length, own, rules, LiteralStartOf(root, start + line.TrimEnd().Length - 1));
                if (owner is null)
                {
                    continue;
                }

                var keep = _kept.FirstOrDefault(k => k.Path == file.Path && line.Contains(k.Fragment, StringComparison.Ordinal));
                if (keep.Path is not null)
                {
                    used.Add(keep);
                    continue;
                }

                findings.Add($"{file.Path}:{i + 1}: types the start {owner} owns again (name it with a cref): {line.Trim()}");
            }
        }

        findings.AddRange(_kept.Where(k => !used.Contains(k))
            .Select(k => $"{k.Path}: the kept line '{k.Fragment}' no longer types an owner's number (or is gone): remove the entry"));
        return findings;
    }

    /// <summary>The starts <paramref name="type"/> owns, by the member that holds each: every constant, every static value that rests at
    /// one (<see cref="StartsMadeBy"/>), and what every property of an instance starts from.</summary>
    private IEnumerable<(string Member, string Start)> StartsOf(BaseTypeDeclarationSyntax type) =>
        type.ChildNodes().SelectMany(node => node switch
        {
            FieldDeclarationSyntax field when field.Modifiers.Any(SyntaxKind.ConstKeyword) => field.Declaration.Variables
                .Where(v => v.Initializer is not null).Select(v => (v.Identifier.ValueText, v.Initializer!.Value.ToString())),
            FieldDeclarationSyntax field when field.Modifiers.Any(SyntaxKind.StaticKeyword) => field.Declaration.Variables
                .Where(v => v.Initializer is not null).SelectMany(v => StartsMadeBy(v.Initializer!.Value, field.Declaration.Type, v.Identifier.ValueText)),
            PropertyDeclarationSyntax { Initializer: { } made } property => StartsMadeBy(made.Value, property.Type, property.Identifier.ValueText),
            _ => [],
        });

    /// <summary>The starts the value <paramref name="member"/> is made by holds, each by the member (and the part of it) that holds it: a
    /// literal, an owner it reads, a literal in a unit (<c>0.8.PointsInPixels</c>), the argument of a record's <c>Resting</c> or
    /// <c>Default</c> member (by position or by name, or laid in its initializer), every argument of a record whose type or member is a
    /// <c>Resting</c> or <c>Default</c> one (as <c>Rests.Diameter</c>), and for a value made <c>with</c> another, the member it lays, else
    /// the start of the other.</summary>
    private IEnumerable<(string Member, string Start)> StartsMadeBy(ExpressionSyntax value, TypeSyntax declared, string member)
    {
        switch (value)
        {
            case WithExpressionSyntax with:
                yield return (member, StartLaidIn(with.Initializer) ?? with.Expression.ToString());
                break;
            case BaseObjectCreationExpressionSyntax made:
                string record = ((made as ObjectCreationExpressionSyntax)?.Type ?? declared).ToString().Split('.')[^1].TrimEnd('?');
                var parameters = _parameters.GetValueOrDefault(record) ?? [];
                bool resting = Name.IsMatch(record) || Name.IsMatch(member);
                var arguments = made.ArgumentList?.Arguments ?? [];
                for (int i = 0; i < arguments.Count; i++)
                {
                    string? parameter = arguments[i].NameColon?.Name.Identifier.ValueText ?? (i < parameters.Count ? parameters[i] : null);
                    if (parameter is not null && (resting || Name.IsMatch(parameter)))
                    {
                        yield return (resting ? member + "." + parameter : member, arguments[i].Expression.ToString());
                    }
                }

                if (StartLaidIn(made.Initializer) is { } laid)
                {
                    yield return (member, laid);
                }

                break;
            case MemberAccessExpressionSyntax { Expression: LiteralExpressionSyntax unit }:
                yield return (member, unit.ToString());
                break;
            case LiteralExpressionSyntax or PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax } or IdentifierNameSyntax or MemberAccessExpressionSyntax:
                yield return (member, value.ToString());
                break;
        }
    }

    private static string? StartLaidIn(InitializerExpressionSyntax? initializer) =>
        initializer?.Expressions.OfType<AssignmentExpressionSyntax>()
            .FirstOrDefault(a => a.Left is IdentifierNameSyntax left && Name.IsMatch(left.Identifier.ValueText))?.Right.ToString();

    /// <summary>The owner whose value <paramref name="text"/> types as a start in a phrase that begins before <paramref name="limit"/>
    /// (the rest is the next line, read on its own), or null. A value that is <paramref name="literal"/>, the member's own start, is not
    /// an owner's. A wire note's value that is not the type's zero is a resting value whatever owner holds it.</summary>
    private static string? OwnerTyped(string text, int limit, Dictionary<string, string> own, Regex[] rules, string? literal)
    {
        foreach (var rule in rules)
        {
            foreach (Match m in rule.Matches(text))
            {
                string value = Normalized(m);
                if (m.Index >= limit || (literal is not null && SpellingsOf(literal).Contains(value)))
                {
                    continue;
                }

                if (own.TryGetValue(value, out string? name))
                {
                    return name;
                }

                if (rule == WireNote && !IsZero(value))
                {
                    return "the resting value of the field";
                }
            }
        }

        return null;
    }

    /// <summary>The literal the member a line at <paramref name="position"/> documents starts from (a property's initializer, or the
    /// default of the parameter whose <c>&lt;param&gt;</c> the line stands in), as the documentation is read; null when it starts from
    /// an owner or from nothing written.</summary>
    private string? LiteralStartOf(SyntaxNode root, int position)
    {
        string? parameter = root.FindToken(position, findInsideTrivia: true).Parent?.AncestorsAndSelf().OfType<XmlElementSyntax>()
            .FirstOrDefault(e => e.StartTag.Name.LocalName.ValueText == "param")?.StartTag.Attributes.OfType<XmlNameAttributeSyntax>()
            .FirstOrDefault()?.Identifier.Identifier.ValueText;
        var member = root.FindToken(position).Parent?.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
        string? start = (parameter is not null
            ? member?.DescendantNodes().OfType<ParameterSyntax>().FirstOrDefault(p => p.Identifier.ValueText == parameter)?.Default?.Value
            : (member as PropertyDeclarationSyntax)?.Initializer?.Value)?.ToString();
        var alias = Alias.Match(start ?? string.Empty);
        bool readsAnOwner = alias.Groups[1].Success && DeclaringTypeOf(alias.Groups[1].Value, alias.Groups[2].Value) is not null;
        return start is not null && !readsAnOwner && Literal.IsMatch(start) ? Normalized(start) : null;
    }

    /// <summary>The values a line standing at <paramref name="node"/> is read against, each spelling to the owners that hold it: the
    /// owners of every type the nearest named type around the line names (for the documentation of a type, that type itself).</summary>
    private Dictionary<string, string> OwnOf(SyntaxNode node)
    {
        string type = node.AncestorsAndSelf().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault(IsNamed)?.Identifier.ValueText ?? string.Empty;
        if (_own.TryGetValue(type, out var cached))
        {
            return cached;
        }

        var own = _own[type] = new Dictionary<string, string>(StringComparer.Ordinal);
        var named = _named.GetValueOrDefault(type) ?? [];
        foreach (var (owner, constant) in _declared.Keys.Where(key => named.Contains(key.Type)))
        {
            if (ValueOf(owner, constant) is { } value && !IsZero(value))
            {
                string label = owner == type ? constant : owner + "." + constant;
                foreach (string spelling in SpellingsOf(value))
                {
                    own[spelling] = own.TryGetValue(spelling, out string? labels) ? labels + ", " + label : label;
                }
            }
        }

        return own;
    }

    private string? ValueOf(string type, string name, int depth)
    {
        if (depth > 4 || DeclaringTypeOf(type, name) is not { } owner)
        {
            return null;
        }

        string expression = _declared[(owner, name)];
        var alias = Alias.Match(expression);
        string reads = alias.Groups[1].Success ? alias.Groups[1].Value : owner;
        if (alias.Success && DeclaringTypeOf(reads, alias.Groups[2].Value) is not null)
        {
            return ValueOf(reads, alias.Groups[2].Value, depth + 1);
        }

        return Literal.IsMatch(expression) ? Normalized(expression) : null;
    }

    /// <summary>The type that declares <paramref name="name"/> as <paramref name="type"/> reads it unqualified: the type itself, else the
    /// nearest type around it that declares it; null for none.</summary>
    private string? DeclaringTypeOf(string type, string name)
    {
        for (string? candidate = type; candidate is not null; candidate = _outer.GetValueOrDefault(candidate))
        {
            if (_declared.ContainsKey((candidate, name)))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>A type with a name of its own: an extension block (C# 14) is read as the class it stands in.</summary>
    private static bool IsNamed(BaseTypeDeclarationSyntax type) => type.Identifier.ValueText.Length > 0;

    private static string Normalized(Match m)
    {
        if (m.Groups["m"].Success)
        {
            return "." + m.Groups["m"].Value;
        }

        foreach (string group in new[] { "w", "e", "c", "n", "b" })
        {
            if (m.Groups[group].Success)
            {
                return Normalized(m.Groups[group].Value);
            }
        }

        return string.Empty;
    }

    /// <summary>The ways the documentation may type <paramref name="value"/>: itself, and an enum member also by its name alone (as
    /// <c>&lt;c&gt;Cartesian&lt;/c&gt;</c> types <c>CoordinateSystem.Cartesian</c>).</summary>
    private static IEnumerable<string> SpellingsOf(string value)
    {
        yield return value;
        int dot = value.LastIndexOf('.');
        if (dot > 0 && char.IsUpper(value[0]))
        {
            yield return value[dot..];
        }
    }

    private static string Normalized(string value)
    {
        if (value.Length == 0 || !(char.IsDigit(value[0]) || value[0] == '-'))
        {
            return value;
        }

        string digits = value.Replace("_", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal).TrimEnd('d', 'D', 'f', 'F', 'm', 'M');
        return double.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
            ? number.ToString("R", CultureInfo.InvariantCulture)
            : value;
    }

    private static bool IsZero(string value) => value is "0" or "false";
}
