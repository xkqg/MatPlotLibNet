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

    public static TheoryData<IEnumerable<string>, string[], string[]> WhatTheGuardFinds => new()
    {
        { DrawnAxesFixtures.InternallySet.Select(property => property.Name),
            [nameof(Axes.GridRows), nameof(Axes.GridCols), nameof(Axes.GridIndex), nameof(Axes.GridPosition), nameof(Axes.ShareXWith)], [] },
        { DrawnAxesFixtures.AxesLists.Select(list => list.Name),
            [nameof(Axes.Spans), nameof(Axes.Signals), nameof(Axes.XBreaks)], [nameof(Axes.Series), nameof(Axes.Insets)] },
    };

    [Theory]
    [MemberData(nameof(WhatTheGuardFinds))]
    public void TheGuard_FindsTheCellAndThePlaceOfASubplot_AndTheOverlayListsOfAnAxes(IEnumerable<string> names, string[] found, string[] notFound)
    {
        var all = names.ToList();
        Assert.All(found, name => Assert.Contains(name, all));
        Assert.All(notFound, name => Assert.DoesNotContain(name, all));
    }

    /// <summary>A figure built through the public call of a route has the property set on some axes, and read back from JSON
    /// every axes has the value it had.</summary>
    [Theory]
    [MemberData(nameof(DrawnAxesFixtures.InternallySetRouteRows), MemberType = typeof(DrawnAxesFixtures))]
    public void AnInternallySetPropertyOfAxes_ComesBackFromJson(string property, string call)
    {
        var figure = DrawnAxesFixtures.Route(property, call).Build();
        var written = DrawnAxesFixtures.InternallySetValues(figure, property);

        var read = DrawnAxesFixtures.InternallySetValues(figure.RoundTripped(), property);

        object? atRest = DrawnAxesFixtures.InternallySetAtRest(property);
        Assert.True(written.Any(value => !Equals(value, atRest)), $"{property} through {call}: no axes of the figure has it set, so the row proves nothing.");
        Assert.True(written.SequenceEqual(read),
            $"{property} through {call}: written as [{string.Join(", ", written.Select(DrawnSeriesFixtures.Show))}], read back from JSON as [{string.Join(", ", read.Select(DrawnSeriesFixtures.Show))}].");
    }

    // ---- the overlay lists of an axes -------------------------------------------------------------------------------

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
            var itemType = list.PropertyType.GetGenericArguments()[0];
            foreach (var type in itemType.Assembly.ConcretePublicSubtypesOf(itemType))
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

    /// <summary>A figure whose subplots share their scales draws the same picture after a round trip, whether the shares were asked
    /// by key through the builder (<c>ShareX(key)</c>) or made as matplotlib's <c>add_subplot(sharex=ax)</c> makes them, with no key:
    /// each shared subplot spans its partner's data too, so a share lost on the way moves its ticks.</summary>
    [Fact]
    public void AFigureWithSharedScales_DrawsTheSameAfterARoundTrip()
    {
        var keyed = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0, 2, 3], [2.0, 4, 3]), key: "left")
            .AddSubPlot(1, 2, 2, ax => ax.Plot([10.0, 20, 30], [5.0, 1, 8]).ShareX("left").ShareY("left"))
            .Build();
        var unkeyed = DrawnAxesFixtures.SharedScalesWithNoKey();

        foreach (var figure in new[] { keyed, unkeyed })
        {
            var read = figure.RoundTripped();
            Assert.Same(read.SubPlots[0], read.SubPlots[1].ShareXWith);
            Assert.Equal(figure.ToSvg(), read.ToSvg());
        }
        var readUnkeyed = unkeyed.RoundTripped();
        Assert.Same(readUnkeyed.SubPlots[1], readUnkeyed.SubPlots[2].ShareYWith);
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

        string readBack = figure.RoundTripped().ToSvg();

        Assert.Equal(2, figure.SubPlots[0].Signals.Count);
        Assert.Equal(drawn, readBack);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  An axes without a signal writes no signal list, so every figure without one writes the bytes it wrote before the signals reached
        // the JSON.
        new DrawnToken("AnAxesWithoutASignal_WritesNoSignalsField", () => Serializer.ToJson(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [1.0, 2])).Build())) { Absent = ["\"signals\""] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>The lists an axes and a figure carry, each read value by value.</summary>
    public static TheoryData<string, Func<Figure, IReadOnlyDictionary<string, object?>>> Lists => new()
    {
        { "Axes", DrawnAxesFixtures.AxesListValues },
        { "Figure", DrawnAxesFixtures.FigureListValues },
    };

    /// <summary>The length of each overlay list and every value of every item of it, the ones it was made with included (a
    /// span's edges, a signal's place and direction, a break's ends), and of each list of the figure, the ones given when it is
    /// made (a text's words and place, an image's pixels and offsets) included, comes back from JSON as it was. One case, every
    /// value the DTO drops named: <c>Spans[0].Max</c>, <c>Signals[0].Direction</c>, <c>Texts[0].Y</c>, <c>Images[0].XOffset</c>.</summary>
    [Theory]
    [MemberData(nameof(Lists))]
    public void EveryListValue_ComesBackFromJson_AsItWasWritten(string subject, Func<Figure, IReadOnlyDictionary<string, object?>> valuesOf)
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: true);

        DrawnSeriesFixtures.AssertKept(subject, valuesOf(figure), valuesOf(figure.RoundTripped()), "from JSON");
    }
}
