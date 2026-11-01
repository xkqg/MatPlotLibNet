// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>What the drawn-property contract (<see cref="AxesDrawnPropertiesRoundTripTests"/>) cannot reach on its own: it
/// reflects the public setters of an axes, so it never saw what the call that adds a subplot sets (its cell of a grid, which
/// a round trip lost without a test going red), nor the items of an overlay list, which it holds only through a part written
/// by hand. Two reflected guards close that: every internally set property of <see cref="Axes"/> is set by a public call whose
/// figure comes back from JSON with it, or is named with the reason no JSON carries it; and every concrete item type of every
/// overlay list of <see cref="Axes"/> has a part that holds an item of that list, with every value of every item, the ones it
/// was made with included, coming back from JSON.
/// <para>Tagged into the <c>ChartSerializerGlobalState</c> collection: the figures read back through the process-global series
/// registry, which <see cref="SeriesRegistryTests"/> briefly rewrites.</para></summary>
[Collection("ChartSerializerGlobalState")]
public class AxesContractReachTests
{
    private static readonly ChartSerializer Serializer = new();

    private static Figure RoundTrip(Figure figure) => Serializer.FromJson(Serializer.ToJson(figure));

    // ---- the properties of an axes no public setter reaches ---------------------------------------------------------

    /// <summary>Every internally set property of an axes is either set by a public call of the routes, or named with the reason
    /// no JSON carries it, never both and never neither; a name in either list is a property the axes has.</summary>
    [Fact]
    public void EveryInternallySetPropertyOfAxes_IsCarriedOrNamed()
    {
        var reflected = DrawnAxesFixtures.InternallySet.Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        var routed = DrawnAxesFixtures.InternallySetRoutes.Select(route => route.Property).ToHashSet(StringComparer.Ordinal);
        var named = DrawnAxesFixtures.InternallySetNotCarried;

        var gaps = reflected.Where(name => !routed.Contains(name) && !named.ContainsKey(name)).Order(StringComparer.Ordinal).ToList();
        var both = routed.Where(named.ContainsKey).ToList();
        var stale = routed.Concat(named.Keys).Where(name => !reflected.Contains(name)).ToList();

        Assert.True(gaps.Count == 0, $"Set inside the library, and neither carried by a route nor named with a reason: {string.Join(", ", gaps)}.");
        Assert.True(both.Count == 0, $"Both carried by a route and named as not carried: {string.Join(", ", both)}.");
        Assert.True(stale.Count == 0, $"Named, but not an internally set property of Axes: {string.Join(", ", stale)}.");
        Assert.All(named, pair => Assert.False(string.IsNullOrWhiteSpace(pair.Value), $"{pair.Key} is named without a reason."));
    }

    /// <summary>The reflection finds what the call that adds a subplot sets: a refactor that hid them would leave the guard above
    /// with nothing to check.</summary>
    [Fact]
    public void TheGuard_FindsTheCellAndThePlaceOfASubplot()
    {
        var names = DrawnAxesFixtures.InternallySet.Select(property => property.Name).ToList();

        Assert.Contains(nameof(Axes.GridRows), names);
        Assert.Contains(nameof(Axes.GridCols), names);
        Assert.Contains(nameof(Axes.GridIndex), names);
        Assert.Contains(nameof(Axes.GridPosition), names);
        Assert.Contains(nameof(Axes.ShareXWith), names);
    }

    /// <summary>A figure built through the public call of a route has the property set on some axes, and read back from JSON
    /// every axes has the value it had.</summary>
    [Theory]
    [MemberData(nameof(DrawnAxesFixtures.InternallySetRouteRows), MemberType = typeof(DrawnAxesFixtures))]
    public void AnInternallySetPropertyOfAxes_ComesBackFromJson(string property, string call)
    {
        var figure = DrawnAxesFixtures.Route(property, call).Build();
        var written = DrawnAxesFixtures.InternallySetValues(figure, property);

        var read = DrawnAxesFixtures.InternallySetValues(RoundTrip(figure), property);

        object? atRest = DrawnAxesFixtures.InternallySetAtRest(property);
        Assert.True(written.Any(value => !Equals(value, atRest)), $"{property} through {call}: no axes of the figure has it set, so the row proves nothing.");
        Assert.True(written.SequenceEqual(read),
            $"{property} through {call}: written as [{string.Join(", ", written.Select(DrawnSeriesFixtures.Show))}], read back from JSON as [{string.Join(", ", read.Select(DrawnSeriesFixtures.Show))}].");
    }

    // ---- the overlay lists of an axes -------------------------------------------------------------------------------

    /// <summary>The public types an item of a list of <paramref name="itemType"/> can be: the type itself when it is concrete,
    /// and every concrete type of its assembly that derives from it.</summary>
    private static IEnumerable<Type> ConcreteTypesOf(Type itemType) =>
        itemType.Assembly.GetTypes()
            .Where(type => type.IsPublic && !type.IsAbstract && !type.IsInterface && itemType.IsAssignableFrom(type))
            .OrderBy(type => type.Name, StringComparer.Ordinal);

    /// <summary>Every concrete item type of every overlay list of an axes has a part whose holder is an item of that very list in
    /// the fixture, so the part sets every settable property of it and the JSON has to carry them; or the type is named for that
    /// list with the reason no JSON carries it.</summary>
    [Fact]
    public void EveryOverlayListOfAxes_HasAPartPerConcreteItemType()
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: false);
        var axes = figure.SubPlots[0];
        var gaps = new List<string>();

        foreach (var list in DrawnAxesFixtures.AxesLists)
        {
            var items = ((IEnumerable)list.GetValue(axes)!).Cast<object>().ToList();
            foreach (var type in ConcreteTypesOf(list.PropertyType.GetGenericArguments()[0]))
            {
                string key = DrawnAxesFixtures.OverlayKey(list.Name, type);
                bool held = DrawnAxesFixtures.Parts.Any(part => part.Type == type && items.Any(item => ReferenceEquals(item, part.Holder(figure))));
                if (!held && !DrawnAxesFixtures.OverlaysNotCarried.ContainsKey(key))
                {
                    gaps.Add(key);
                }
            }
        }

        Assert.True(gaps.Count == 0, $"An overlay list item type with no part that holds an item of the list in the fixture, and no reason it is not carried: {string.Join("; ", gaps)}.");
    }

    /// <summary>The reflection finds the overlay lists, the ones a series verb never builds included: a refactor that hid them
    /// would leave the guard above with nothing to check.</summary>
    [Fact]
    public void TheGuard_FindsTheOverlayListsOfAnAxes()
    {
        var names = DrawnAxesFixtures.AxesLists.Select(list => list.Name).ToList();

        Assert.Contains(nameof(Axes.Spans), names);
        Assert.Contains(nameof(Axes.Signals), names);
        Assert.Contains(nameof(Axes.XBreaks), names);
        Assert.DoesNotContain(nameof(Axes.Series), names);
        Assert.DoesNotContain(nameof(Axes.Insets), names);
    }

    /// <summary>A buy and a sell signal, one of them coloured, sized and named, draw the same picture after a round trip: the
    /// overlay list the guard above found with no part, and no field of the JSON either.</summary>
    [Fact]
    public void AFigureWithSignals_DrawsTheSameAfterARoundTrip()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2, 3, 4], [2.0, 3, 2.5, 4])
            .BuyAt(1, 2)
            .SellAt(3, 2.5, signal => { signal.Color = Color.FromHex("#9467BD"); signal.Size = 18; signal.Label = "exit"; })).Build();
        string drawn = figure.ToSvg();

        string readBack = RoundTrip(figure).ToSvg();

        Assert.Equal(2, figure.SubPlots[0].Signals.Count);
        Assert.Equal(drawn, readBack);
    }

    /// <summary>An axes without a signal writes no signal list, so every figure without one writes the bytes it wrote before the
    /// signals reached the JSON.</summary>
    [Fact]
    public void AnAxesWithoutASignal_WritesNoSignalsField()
    {
        string json = Serializer.ToJson(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [1.0, 2])).Build());

        Assert.DoesNotContain("\"signals\"", json);
    }

    /// <summary>The length of each overlay list and every value of every item of it, the ones it was made with included (a
    /// span's edges, a signal's place and direction, a break's ends), comes back from JSON as it was. One row per value, so a
    /// field the DTO drops is named: <c>Spans[0].Max</c>, <c>Signals[0].Direction</c>.</summary>
    [Theory]
    [MemberData(nameof(DrawnAxesFixtures.AxesListValueKeys), MemberType = typeof(DrawnAxesFixtures))]
    public void AnOverlayListValue_ComesBackFromJson_AsItWasWritten(string key)
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: true);
        object? written = DrawnAxesFixtures.AxesListValues(figure)[key];

        var read = DrawnAxesFixtures.AxesListValues(RoundTrip(figure));

        Assert.True(read.ContainsKey(key), $"Axes.{key}: written as {DrawnSeriesFixtures.Show(written)}, and nothing of it came back from JSON.");
        Assert.True(DrawnSeriesFixtures.SameValue(written, read[key]),
            $"Axes.{key}: written as {DrawnSeriesFixtures.Show(written)}, read back from JSON as {DrawnSeriesFixtures.Show(read[key])}.");
    }
}
