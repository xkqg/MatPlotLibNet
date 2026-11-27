// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Mcp;

/// <summary>The chart types this server lists and accepts. Read off <see cref="SeriesRegistry.Discriminators"/> —
/// the one list the serializer dispatches on, the map of the Geo package included — never kept by hand, minus the registered types whose JSON reader
/// ignores the document and substitutes placeholder data: listing those would advertise a chart that renders
/// someone else's numbers with a success flag.</summary>
internal sealed class ChartTypeCatalog
{
    /// <summary>Registered types whose <c>FromSeriesDto</c> returns a literal instead of reading the document.
    /// Empty today (JSON-roundtrip plan): <c>sankey</c>, <c>sunburst</c>, <c>treemap</c>, <c>polarbar</c>,
    /// <c>polarline</c>, <c>polarscatter</c>, <c>treegrid</c> and <c>dendrogram</c> all read their documents now
    /// — this is the day this set re-opened, exactly as the round-trip theory over every listed type was built to
    /// catch. A type lands back in here the day a FUTURE round-trip gap is found for it, with the measured reason.</summary>
    private static readonly IReadOnlyDictionary<string, string> NotBuildableFromJson = new Dictionary<string, string>(StringComparer.Ordinal);

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
    /// <param name="notBuildableFromJson">The registered types the catalog leaves out, each with the reason it cannot be built
    /// from JSON.</param>
    public ChartTypeCatalog(IReadOnlyDictionary<string, string> notBuildableFromJson)
    {
        // The server carries the Geo package. Its map reader is registered when that assembly loads, and nothing has loaded it
        // yet at startup, so it is registered here, before the registry is read (registering twice is the same as once).
        GeoPolygonSeries.RegisterReader();
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
            int distance = EditDistanceTo(typed, type);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = type;
            }
        }

        return nearestDistance <= MaxSuggestionDistance ? nearest : null;
    }

    /// <summary>The Levenshtein distance between <paramref name="word"/> and <paramref name="other"/>, compared
    /// without regard to case: the number of single-character insertions, deletions and substitutions between them.</summary>
    private static int EditDistanceTo(string word, string other)
    {
        string a = word.ToLowerInvariant();
        string b = other.ToLowerInvariant();
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (int i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int substitution = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), substitution);
            }
            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
