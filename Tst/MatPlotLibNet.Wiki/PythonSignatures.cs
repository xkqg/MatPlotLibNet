// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// The calls, attributes and settings of matplotlib 3.11.2 and of the Python packages the documents compare with, as
/// <c>tools/mpl_reference/generate_signatures.py</c> writes them from <c>inspect.signature</c>: for each owner (a class a document calls
/// methods on, a module) the name of each call and its parameters, each attribute, each class with the classes it derives from; every
/// setting (<c>rcParams</c>) with its default under the default and the classic style. The suite reads the table and never runs Python.
/// A document's Python call (<see cref="PythonCall"/>) is held to it.
/// </summary>
internal sealed class PythonSignatures
{
    /// <summary>The table checked in beside this suite.</summary>
    public static PythonSignatures Matplotlib { get; } = Read(RepositoryTree.FileAt("Tst", "MatPlotLibNet.Wiki", "Python", "signatures.txt").Text);

    private const string KeywordsOwner = "keywords";

    private const string Settings = "rcParams";

    private const string ClassicSettings = "rcParams.classic";

    private readonly Dictionary<string, Dictionary<string, PythonMember>> _owners;

    private readonly HashSet<string> _keywords;

    private readonly ILookup<string, (string Owner, string Key)> _byName;

    private PythonSignatures(Dictionary<string, Dictionary<string, PythonMember>> owners, HashSet<string> keywords)
    {
        _owners = owners;
        _keywords = keywords;
        _byName = owners.SelectMany(owner => owner.Value.Keys.Select(key => (Name: key[(key.LastIndexOf('.') + 1)..], Owner: owner.Key, Key: key)))
            .ToLookup(each => each.Name, each => (each.Owner, each.Key), StringComparer.Ordinal);
    }

    /// <summary>The owners the table holds, by the name the generator gives them.</summary>
    public IReadOnlyCollection<string> Owners => _owners.Keys;

    /// <summary>Whether a keyword is one matplotlib takes somewhere: a parameter of a call of the table, a keyword the documentation of a
    /// call lists, or a property an artist takes as a keyword (a call that passes its <c>**kwargs</c> on to an artist takes it).</summary>
    /// <param name="keyword">The keyword.</param>
    /// <returns>True when matplotlib takes it.</returns>
    public bool IsKeyword(string keyword) => _keywords.Contains(keyword);

    /// <summary>A member of an owner by its key (<c>hist</c>, <c>MaxNLocator.tick_values</c>), a method a class inherits read through the
    /// classes it derives from.</summary>
    /// <param name="owner">The owner, as the table names it.</param>
    /// <param name="key">The member's key.</param>
    /// <returns>The member, or null when the owner has none of that key.</returns>
    public PythonMember? Of(string owner, string key)
    {
        if (_owners.TryGetValue(owner, out var members) && members.TryGetValue(key, out var found))
        {
            return found;
        }

        // A polar or 3-D axes is written with what it declares or changes; what it inherits is the Axes'.
        if (owner is "PolarAxes" or "Axes3D" or "XAxis" or "YAxis")
        {
            return Of(owner.EndsWith("Axis", StringComparison.Ordinal) ? "Axis" : "Axes", key);
        }

        // A method of a class a module declares: read on the classes it derives from (Rectangle.set_xy is Patch's).
        var dot = key.IndexOf('.', StringComparison.Ordinal);

        if (dot < 0 || members is null || !members.TryGetValue(key[..dot], out var type))
        {
            return null;
        }

        return type.Bases.Select(qualified => (Module: qualified[..qualified.LastIndexOf('.')], Class: qualified[(qualified.LastIndexOf('.') + 1)..]))
            .Select(each => _owners.TryGetValue(each.Module, out var held) && held.TryGetValue(each.Class + key[dot..], out var inherited) ? inherited : null)
            .FirstOrDefault(each => each is not null);
    }

    /// <summary>Where a name stands in the table, written without its owner (<c>Rectangle</c>, <c>tick_left</c>, <c>_nonsingular</c>):
    /// every owner and key whose last name it is.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The places, in the order the table lists them.</returns>
    public IReadOnlyList<(string Owner, string Key)> Named(string name) => [.. _byName[name]];

    /// <summary>Whether a dotted name is a setting of matplotlib (<c>axes.labelpad</c>).</summary>
    /// <param name="key">The setting.</param>
    /// <returns>True for a setting.</returns>
    public bool IsSetting(string key) => Of(Settings, key) is not null;

    /// <summary>The values a setting rests at: under the default style, and under the classic style when that one differs.</summary>
    /// <param name="key">The setting.</param>
    /// <returns>The values as Python writes them, the default first.</returns>
    public IReadOnlyList<string> SettingValues(string key) =>
        [.. new[] { Of(Settings, key), Of(ClassicSettings, key) }.OfType<PythonMember>().Select(member => member.Value!)];

    /// <summary>Reads a table in the generator's format.</summary>
    /// <param name="text">The table.</param>
    /// <returns>The signatures.</returns>
    public static PythonSignatures Read(string text)
    {
        var owners = new Dictionary<string, Dictionary<string, PythonMember>>(StringComparer.Ordinal);
        var keywords = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, PythonMember>? members = null;
        var owner = string.Empty;

        foreach (var line in SourceFile.LinesOf(text).Where(line => line.Length > 0 && !line.StartsWith('#')))
        {
            if (line.StartsWith("@ ", StringComparison.Ordinal))
            {
                owner = line.Split(' ')[1];
                members = owner == KeywordsOwner ? null : owners[owner] = new(StringComparer.Ordinal);
                continue;
            }

            if (members is null)
            {
                keywords.UnionWith(line.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                continue;
            }

            var member = owner is Settings or ClassicSettings ? PythonMember.OfSetting(line) : PythonMember.Parse(line);
            members[member.Key] = member;
            keywords.UnionWith(member.Signature?.Parameters.Where(parameter => parameter.Kind is PythonParameterKind.Named or PythonParameterKind.KeywordOnly).Select(parameter => parameter.Name) ?? []);
            keywords.UnionWith(member.Documented);
        }

        return new PythonSignatures(owners, keywords);
    }
}

/// <summary>One name of an owner of the table: a call with its signature, an attribute, or a setting with its value.</summary>
/// <param name="Key">The name, after the class it belongs to when it is a method (<c>MaxNLocator.tick_values</c>).</param>
/// <param name="Signature">The parameters of a call; null for an attribute or a setting.</param>
/// <param name="Documented">The keywords the documentation of a call that takes <c>**kwargs</c> lists.</param>
/// <param name="Bases">The classes a class derives from, each with its module (<c>matplotlib.patches.Patch</c>).</param>
/// <param name="Value">A setting's value as Python writes it; null for anything else.</param>
internal sealed record PythonMember(string Key, PythonSignature? Signature, IReadOnlyList<string> Documented, IReadOnlyList<string> Bases, string? Value)
{
    /// <summary>Reads one line of an owner: <c>name(params) [documented] : bases</c>, or a name alone.</summary>
    /// <param name="line">The line.</param>
    /// <returns>The member.</returns>
    public static PythonMember Parse(string line)
    {
        var open = line.IndexOf('(', StringComparison.Ordinal);

        if (open < 0)
        {
            return new PythonMember(line, null, [], [], null);
        }

        var close = PythonText.Close(line, open);
        var rest = line[(close + 1)..];
        var documented = rest.IndexOf('[', StringComparison.Ordinal) is var bracket and >= 0 ? rest[(bracket + 1)..rest.IndexOf(']', bracket)].Split(' ', StringSplitOptions.RemoveEmptyEntries) : [];
        var bases = rest.IndexOf(" : ", StringComparison.Ordinal) is var colon and >= 0 ? rest[(colon + 3)..].Split(' ', StringSplitOptions.RemoveEmptyEntries) : [];

        return new PythonMember(line[..open], PythonSignature.Parse(line[(open + 1)..close]), documented, bases, null);
    }

    /// <summary>Reads one line of a section of settings: <c>key=value</c>.</summary>
    /// <param name="line">The line.</param>
    /// <returns>The setting.</returns>
    public static PythonMember OfSetting(string line)
    {
        var equals = line.IndexOf('=', StringComparison.Ordinal);

        return new PythonMember(line[..equals], null, [], [], line[(equals + 1)..]);
    }

    /// <summary>Whether the member is a call a document may write with arguments.</summary>
    public bool IsCall => Signature is not null;

    /// <summary>Whether the call takes a keyword: a parameter of that name, a keyword its documentation lists, or, when it takes
    /// <c>**kwargs</c>, any keyword an artist takes.</summary>
    /// <param name="keyword">The keyword.</param>
    /// <param name="signatures">The table, for the keywords an artist takes.</param>
    /// <returns>True when the call takes it.</returns>
    public bool Takes(string keyword, PythonSignatures signatures) =>
        Signature is { } signature && (signature.Parameters.Any(parameter => parameter.Name == keyword && parameter.Kind is PythonParameterKind.Named or PythonParameterKind.KeywordOnly)
                                       || (signature.Parameters.Any(parameter => parameter.Kind == PythonParameterKind.Keywords) && (Documented.Contains(keyword) || signatures.IsKeyword(keyword))));
}

/// <summary>How a parameter of a Python call is given.</summary>
internal enum PythonParameterKind
{
    /// <summary>By position only (before a <c>/</c>).</summary>
    PositionalOnly,

    /// <summary>By position or by name.</summary>
    Named,

    /// <summary>By name only (after a <c>*</c>).</summary>
    KeywordOnly,

    /// <summary><c>*args</c>: any further positional arguments.</summary>
    Rest,

    /// <summary><c>**kwargs</c>: any further keywords.</summary>
    Keywords,
}

/// <summary>A parameter of a Python call: its name, how it is given, whether it has a default.</summary>
/// <param name="Name">The name.</param>
/// <param name="Kind">How it is given.</param>
/// <param name="HasDefault">Whether a call may leave it out.</param>
/// <param name="Default">The default as the table writes it; null for a parameter without one.</param>
internal readonly record struct PythonParameter(string Name, PythonParameterKind Kind, bool HasDefault, string? Default = null);

/// <summary>The parameters of one Python call.</summary>
/// <param name="Parameters">The parameters in order.</param>
internal readonly record struct PythonSignature(IReadOnlyList<PythonParameter> Parameters)
{
    /// <summary>How many arguments the call takes by position at most; null when it takes any number (<c>*args</c>).</summary>
    public int? MostPositional =>
        Parameters.Any(parameter => parameter.Kind == PythonParameterKind.Rest) ? null : Parameters.Count(parameter => parameter.Kind is PythonParameterKind.PositionalOnly or PythonParameterKind.Named);

    /// <summary>The first parameter a call must give and does not: given by neither a position nor a keyword, with no default.</summary>
    /// <param name="positional">How many arguments the call gives by position.</param>
    /// <param name="keywords">The keywords it gives.</param>
    /// <returns>The parameter's name, or null when the call gives every one it must.</returns>
    public string? MissingFrom(int positional, IReadOnlyCollection<string> keywords) =>
        Parameters.Where((parameter, index) => !parameter.HasDefault && parameter.Kind is not (PythonParameterKind.Rest or PythonParameterKind.Keywords)
                                                && !(index < positional && parameter.Kind != PythonParameterKind.KeywordOnly) && !keywords.Contains(parameter.Name))
            .Select(parameter => parameter.Name).FirstOrDefault();

    /// <summary>Reads the parameters between the brackets of a signature: <c>x, bins=None, *, range=None, **kwargs</c>.</summary>
    /// <param name="text">The text between the brackets.</param>
    /// <returns>The signature.</returns>
    public static PythonSignature Parse(string text)
    {
        var parameters = new List<PythonParameter>();
        var keywordOnly = false;

        foreach (var part in PythonText.TopLevelParts(text))
        {
            if (part == "/")
            {
                for (var at = 0; at < parameters.Count; at++)
                {
                    parameters[at] = parameters[at] with { Kind = PythonParameterKind.PositionalOnly };
                }

                continue;
            }

            if (part == "*")
            {
                keywordOnly = true;
                continue;
            }

            if (part.StartsWith("**", StringComparison.Ordinal))
            {
                parameters.Add(new PythonParameter(part[2..], PythonParameterKind.Keywords, true));
                continue;
            }

            if (part.StartsWith('*'))
            {
                parameters.Add(new PythonParameter(part[1..], PythonParameterKind.Rest, true));
                keywordOnly = true;
                continue;
            }

            var equals = part.IndexOf('=', StringComparison.Ordinal);
            parameters.Add(new PythonParameter(equals < 0 ? part : part[..equals], keywordOnly ? PythonParameterKind.KeywordOnly : PythonParameterKind.Named, equals >= 0, equals < 0 ? null : part[(equals + 1)..]));
        }

        return new PythonSignature(parameters);
    }
}
