// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Mcp;

/// <summary>The chart types this server lists and accepts. Read off <see cref="SeriesRegistry.Discriminators"/> —
/// the one list the serializer dispatches on — never kept by hand, minus the registered types whose JSON reader
/// ignores the document and substitutes placeholder data: listing those would advertise a chart that renders
/// someone else's numbers with a success flag.</summary>
internal sealed class ChartTypeCatalog
{
    /// <summary>Registered types whose <c>FromSeriesDto</c> returns a literal instead of reading the document
    /// (measured: <c>axes.Sankey([new SankeyNode("A")], [])</c> and its six siblings). The round-trip theory over
    /// every listed type is the alarm that re-opens this set the day Core fixes one of them.</summary>
    private static readonly IReadOnlyDictionary<string, string> NotBuildableFromJson = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["sankey"] = "its JSON reader builds a fixed placeholder diagram instead of the nodes and links in the document",
        ["sunburst"] = "its JSON reader builds a fixed placeholder tree instead of the hierarchy in the document",
        ["treemap"] = "its JSON reader builds a fixed placeholder tree instead of the hierarchy in the document",
        ["polarbar"] = "its JSON reader builds a fixed placeholder bar instead of the data in the document",
        ["polarline"] = "its JSON reader builds a fixed placeholder line instead of the data in the document",
        ["polarscatter"] = "its JSON reader builds a fixed placeholder point instead of the data in the document",
        ["treegrid"] = "its JSON reader builds an empty grid instead of the rows in the document",
    };

    /// <summary>A misspelling this many edits away from a listed type is offered as the type that was meant.</summary>
    private const int MaxSuggestionDistance = 2;

    private readonly HashSet<string> _listed;

    /// <summary>Snapshots the registry: the server never registers a series, so the list is fixed at startup.</summary>
    public ChartTypeCatalog() : this(NotBuildableFromJson)
    {
    }

    /// <summary>Snapshots the registry, excluding the types <paramref name="notBuildableFromJson"/> names. The set
    /// is an input rather than a constant so the day a reader learns to read its own document, the catalog is told
    /// instead of edited — and so an empty set is a state the tests can reach.</summary>
    public ChartTypeCatalog(IReadOnlyDictionary<string, string> notBuildableFromJson)
    {
        var registered = SeriesRegistry.Discriminators;
        Listed = registered.Where(type => !notBuildableFromJson.ContainsKey(type)).ToArray();
        Excluded = registered
            .Where(notBuildableFromJson.ContainsKey)
            .ToDictionary(type => type, type => notBuildableFromJson[type], StringComparer.Ordinal);
        _listed = new HashSet<string>(Listed, StringComparer.Ordinal);
    }

    /// <summary>The types a spec may name, sorted ordinally.</summary>
    public IReadOnlyList<string> Listed { get; }

    /// <summary>The registered types a spec may NOT name, each with the reason the refusal quotes.</summary>
    public IReadOnlyDictionary<string, string> Excluded { get; }

    /// <summary>Whether <paramref name="type"/> is a listed type — an exact, case-sensitive match, because the
    /// serializer's own lookup is exact and <c>"Line"</c> would otherwise render an empty chart.</summary>
    public bool IsListed(string type) => _listed.Contains(type);

    /// <summary>The listed type a misspelling most likely meant: the same letters in another case first, then the
    /// nearest by edit distance while that distance is small; <see langword="null"/> when nothing is close.</summary>
    public string? Suggest(string typed)
    {
        foreach (var type in Listed)
        {
            if (string.Equals(type, typed, StringComparison.OrdinalIgnoreCase))
            {
                return type;
            }
        }

        string? nearest = null;
        int nearestDistance = int.MaxValue;
        foreach (var type in Listed)
        {
            int distance = typed.EditDistanceTo(type);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = type;
            }
        }

        return nearestDistance <= MaxSuggestionDistance ? nearest : null;
    }
}
