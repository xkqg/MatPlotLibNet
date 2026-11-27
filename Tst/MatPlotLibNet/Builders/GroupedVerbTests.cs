// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>What the verbs that draw one mark for each group of values (bump chart, dot plot, Marimekko, parallel coordinates, stream
/// graph, Kaplan-Meier, Q-Q plot, ridgeline, risk table) answer alike: an argument that is not there is refused by name, a chart of no
/// groups is refused by the parameter and in words that say what the chart draws, and the cells of a stacked area are opaque.</summary>
public class GroupedVerbTests
{
    private static readonly double[] X = [1, 2];

    private static readonly string[] Names = ["a", "b"];

    private static readonly BarGroup[] Bars = [new("g", [1, 2]), new("h", [3, 4])];

    private static readonly SurvivalGroup[] Survivors = [new("g", [1, 2], [true, true])];

    private static readonly NamedSample[] Samples = [new("g", [1, 2, 3])];

    private static Axes InOneAxes(Action<AxesBuilder> verb) => Plt.Create().OneAxes(verb);

    /// <summary>Each verb with the names of its parameters, axes first, and a call that leaves out the first or the second argument when
    /// told to.</summary>
    public static TheoryData<string[], Action<AxesBuilder, bool, bool>> Verbs => new()
    {
        { ["axes", "x", "ranks"], (axes, first, second) => axes.BumpChart(first ? X : null!, second ? Bars : null!) },
        { ["axes", "categories", "groups"], (axes, first, second) => axes.DotPlot(first ? Names : null!, second ? Bars : null!) },
        { ["axes", "columns", "segments"], (axes, first, second) => axes.Marimekko(first ? Names : null!, second ? Bars : null!) },
        { ["axes", "dimensions", "rows"], (axes, first, second) => axes.ParallelCoordinates(first ? Names : null!, second ? Bars : null!) },
        { ["axes", "x", "layers"], (axes, first, second) => axes.StreamGraph(first ? X : null!, second ? Bars : null!) },
        { ["axes", "groups"], (axes, first, _) => axes.KaplanMeier(first ? Survivors : null!) },
        { ["axes", "sample"], (axes, first, _) => axes.QQPlot(first ? X : null!) },
        { ["axes", "groups"], (axes, first, _) => axes.Ridgeline(first ? Samples : null!) },
        { ["axes", "groups"], (axes, first, _) => axes.RiskTable(first ? Survivors : null!) },
    };

    /// <summary>An axes, an array or a list of groups that is not there is refused by name, the axes first.</summary>
    [Theory]
    [MemberData(nameof(Verbs))]
    public void ANullArgument_IsRefusedByName(string[] parameters, Action<AxesBuilder, bool, bool> verb)
    {
        Assert.Equal(parameters[0], Assert.Throws<ArgumentNullException>(() => verb(null!, true, true)).ParamName);
        for (int missing = 1; missing < parameters.Length; missing++)
        {
            int at = missing;
            Assert.Equal(parameters[at], Assert.Throws<ArgumentNullException>(() => InOneAxes(ax => verb(ax, at != 1, at != 2))).ParamName);
        }
    }

    /// <summary>A chart draws one mark for each group, so no groups is refused, by the parameter, in words that say what the chart draws.</summary>
    public static TheoryData<Action, string, string> NoGroups => new()
    {
        { () => InOneAxes(ax => ax.BumpChart(X, [])), "ranks", "No groups were given" },
        { () => InOneAxes(ax => ax.DotPlot(Names, [])), "groups", "No groups were given" },
        { () => InOneAxes(ax => ax.KaplanMeier([])), "groups", "No groups were given; a Kaplan-Meier chart draws one curve per group." },
        { () => InOneAxes(ax => ax.Ridgeline([])), "groups", "No groups were given; a ridgeline draws one ridge per group." },
        { () => InOneAxes(ax => ax.RiskTable([])), "groups", "No groups were given; a risk table counts one row per group." },
    };

    [Theory]
    [MemberData(nameof(NoGroups))]
    public void NoGroups_AreRefused(Action call, string parameter, string opening)
    {
        var error = Assert.Throws<ArgumentException>(call);

        Assert.Equal(parameter, error.ParamName);
        Assert.StartsWith(opening, error.Message, StringComparison.Ordinal);
    }

    /// <summary>The cells of a Marimekko chart and the streams of a stream graph are opaque, as bars and matplotlib's stackplot layers are,
    /// so each wears exactly the colour of its legend entry (an area is faint, 0.3, by default).</summary>
    public static TheoryData<Func<Axes>> StackedAreas => new()
    {
        () => InOneAxes(ax => ax.Marimekko(Names, Bars)),
        () => InOneAxes(ax => ax.StreamGraph(X, Bars)),
    };

    [Theory]
    [MemberData(nameof(StackedAreas))]
    public void TheCellsOfAStackedArea_AreOpaque(Func<Axes> chart)
    {
        var axes = chart();

        Assert.NotEmpty(axes.Series);
        Assert.All(axes.Series, series => Assert.Equal(1.0, Assert.IsType<AreaSeries>(series).Alpha));
    }
}
