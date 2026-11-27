// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The built-in sets are read once and shared (a map names its built-in set by the very list it holds), whoever asks and
/// however many ask at once: two callers that found the cache empty together each read their own list and the later one replaced the
/// earlier, so a map holding the earlier list wrote its features out in full instead of the id of the set (measured as a test that
/// failed in about one run of five of the Geo suite, which asks for the countries from many tests at once). Each of the three sets is
/// read once however the callers race.</summary>
[Collection("NaturalEarthCache")]
public class NaturalEarth110mThreadSafetyTests
{
    /// <summary>Empties the caches, so the next callers find them as the first caller of a process does.</summary>
    private static void EmptyTheCaches()
    {
        var type = typeof(NaturalEarth110m);
        var loaded = type.GetMethod("Loaded", BindingFlags.NonPublic | BindingFlags.Static);
        foreach (var (field, resource) in new[] { ("_coastlines", "coastlines.geojson"), ("_countries", "countries.geojson"), ("_boundaryLines", "boundary_lines_land.geojson") })
            type.GetField(field, BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, loaded?.Invoke(null, [resource]));
    }

    private static List<GeoFeature>[] AskAtOnce(Func<List<GeoFeature>> ask)
    {
        EmptyTheCaches();
        const int Callers = 12;
        var results = new List<GeoFeature>[Callers];
        using var barrier = new Barrier(Callers);
        var threads = Enumerable.Range(0, Callers).Select(i => new Thread(() =>
        {
            barrier.SignalAndWait();
            results[i] = ask();
        })).ToList();
        threads.ForEach(thread => thread.Start());
        threads.ForEach(thread => thread.Join());
        return results;
    }

    /// <summary>Every caller was handed <paramref name="kept"/>, the list the cache keeps (a failure names the count, not the lists).</summary>
    private static void OneList(List<GeoFeature>[] results, List<GeoFeature> kept) =>
        Assert.True(results.All(list => ReferenceEquals(list, kept)), $"{results.Count(list => !ReferenceEquals(list, kept))} of {results.Length} callers read a list of their own");

    public static TheoryData<string, Func<List<GeoFeature>>> Sets => new()
    {
        { "countries", NaturalEarth110m.Countries },
        { "coastlines", NaturalEarth110m.Coastlines },
        { "boundary lines", NaturalEarth110m.BoundaryLines },
    };

    [Theory]
    [MemberData(nameof(Sets))]
    public void TwelveCallersAskingForASetTogether_ReadOneList(string set, Func<List<GeoFeature>> ask)
    {
        OneList(AskAtOnce(ask), ask());
        _ = set;
    }

    [Fact]
    public void ASetThatWasNeverAskedFor_IsNoBuiltInSetAMapHolds()
    {
        EmptyTheCaches();

        Assert.Null(NaturalEarth110m.SourceOf([]));
        Assert.Equal("ne_110m_admin_0_countries", NaturalEarth110m.SourceOf(NaturalEarth110m.Countries()));
        Assert.Equal("ne_110m_coastline", NaturalEarth110m.SourceOf(NaturalEarth110m.Coastlines()));
        Assert.Equal("ne_110m_admin_0_boundary_lines_land", NaturalEarth110m.SourceOf(NaturalEarth110m.BoundaryLines()));
    }
}

/// <summary>The tests that empty the caches of the built-in sets run alone, so no other test holds a list the cache then forgets.</summary>
[CollectionDefinition("NaturalEarthCache", DisableParallelization = true)]
public sealed class NaturalEarthCacheCollection;
