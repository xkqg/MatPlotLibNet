// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The dumbbell chart in one call: two dots for each category (where it was, where it is) joined by a connector.
/// The dots are two scatter series wearing the first two colour slots and labelled with their groups; the connector is an
/// error bar of no error that spans the lower dot to the higher one WHICHEVER is higher, in a neutral colour, drawn behind the
/// dots and added last so that it takes no slot. The verb sets no axis limits.</summary>
public class DumbbellTests
{
    private static readonly string[] Products = ["Product A", "Product B", "Product C", "Product D", "Product E", "Product F"];

    // The fourth product goes DOWN (83.0 to 79.5): the connector of a recipe that always ran upward from "before" ended at
    // 86.5 instead of 79.5.
    private static readonly BarGroup Before = new("Before", [62.0, 78.5, 55.0, 83.0, 70.0, 48.0]);
    private static readonly BarGroup After = new("After", [74.0, 82.0, 68.0, 79.5, 88.5, 61.0]);

    private static Axes Dumbbell(BarGroup from, BarGroup to, Action<DumbbellParts>? configure = null) =>
        Plt.Create().OneAxes(ax => ax.Dumbbell(Products, from, to, configure));

    /// <summary>The picture with the legend toggle on: that is what gives every series group its <c>data-series-index</c>,
    /// which these tests read the parts by.</summary>
    private static string Svg(Action<AxesBuilder> configure) => Svg(Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, configure).Build());

    private static string Svg(Figure figure)
    {
        figure.EnableLegendToggle = true;
        return figure.ToSvg();
    }

    // ---- what the verb adds ---------------------------------------------------------------------------------------

    /// <summary>Two scatter series, the first group then the second, and the connector LAST: the order is the colour slots.</summary>
    [Fact]
    public void TheChart_IsTwoDotSeriesAndTheConnectorLast()
    {
        var axes = Dumbbell(Before, After);

        Assert.Equal(3, axes.Series.Count);
        var from = Assert.IsType<ScatterSeries>(axes.Series[0]);
        var to = Assert.IsType<ScatterSeries>(axes.Series[1]);
        Assert.IsType<ErrorBarSeries>(axes.Series[2]);
        Assert.Equal("Before", from.Label);
        Assert.Equal("After", to.Label);
        Assert.Empty(axes.YAxes.Right);
    }

    /// <summary>Each dot stands on the centre of its category's slot at its group's value.</summary>
    [Fact]
    public void TheDots_StandOnTheSlotCentres_AtTheirGroupsValues()
    {
        var axes = Dumbbell(Before, After);
        var from = (ScatterSeries)axes.Series[0];
        var to = (ScatterSeries)axes.Series[1];

        double[] centres = [.. Enumerable.Range(0, Products.Length).Select(k => CategorySlot.Of(k).Centre)];
        Assert.Equal(centres, from.XData);
        Assert.Equal(centres, to.XData);
        Assert.Equal(Before.Values, from.YData);
        Assert.Equal(After.Values, to.YData);
        Assert.All(new[] { from, to }, dot =>
        {
            Assert.Equal(MarkerStyle.Circle, dot.Marker);
            Assert.Equal(14, dot.MarkerSize);
        });
    }

    /// <summary>The categories are the ticks of the X axis: a built-in locator and formatter made as one pair, so they survive JSON.</summary>
    [Fact]
    public void TheCategories_AreNamedOnTheXAxis()
    {
        var axis = Dumbbell(Before, After).XAxis;

        Assert.IsType<FixedLocator>(axis.TickLocator);
        Assert.IsType<CategoryFormatter>(axis.TickFormatter);
        var ticks = axis.TickLocator!.Locate(0, Products.Length);
        Assert.Equal([0.5, 1.5, 2.5, 3.5, 4.5, 5.5], ticks);
        Assert.Equal(Products, ticks.Select(axis.TickFormatter!.Format));
    }

    // ---- the connector --------------------------------------------------------------------------------------------

    /// <summary>The connector is an error bar from the LOWER of the two values upward by their distance, whichever group is
    /// higher: it spans [min, max], not [from, from + |to - from|]. The fourth product (83.0 to 79.5) used to end at 86.5.</summary>
    [Theory]
    [InlineData(10.0, 20.0, 10.0, 10.0)]
    [InlineData(20.0, 10.0, 10.0, 10.0)]
    [InlineData(83.0, 79.5, 79.5, 3.5)]
    [InlineData(5.0, 5.0, 5.0, 0.0)]
    [InlineData(-4.0, 6.0, -4.0, 10.0)]
    public void TheConnector_SpansTheLowerValueToTheHigherOne_InBothDirections(double from, double to, double low, double span)
    {
        var axes = Plt.Create().OneAxes(ax => ax.Dumbbell(["x"], new BarGroup("a", [from]), new BarGroup("b", [to])));

        var connector = (ErrorBarSeries)axes.Series[2];

        Assert.Equal([low], connector.YData);
        Assert.Equal([0.0], connector.YErrorLow);
        Assert.Equal([span], connector.YErrorHigh);
        Assert.Equal(low + span, Math.Max(from, to), 12);
    }

    /// <summary>Drawn: the connector's bar runs between the centres of the two dots of its category, going up (first product,
    /// 62 to 74) and going down (fourth product, 83.0 to 79.5), and the ranges of the other categories agree too.</summary>
    [Fact]
    public void TheConnectorBar_RunsBetweenTheTwoDotsOfItsCategory_InBothDirections()
    {
        string svg = Svg(ax => ax.Dumbbell(Products, Before, After));

        var fromDots = Dots(svg, 0);
        var toDots = Dots(svg, 1);
        var bars = ConnectorBars(svg);

        Assert.Equal(6, bars.Length);
        for (int k = 0; k < Products.Length; k++)
        {
            double top = Math.Min(fromDots[k], toDots[k]);       // pixel y grows downward: the higher dot has the smaller y
            double bottom = Math.Max(fromDots[k], toDots[k]);
            Assert.Equal(top, bars[k].Top, 6);
            Assert.Equal(bottom, bars[k].Bottom, 6);
        }

        // The two directions the data holds: going up (62 -> 74) and going down (83.0 -> 79.5).
        Assert.True(Before.Values[0] < After.Values[0]);
        Assert.True(Before.Values[3] > After.Values[3]);
    }

    /// <summary>The connector wears the theme's neutral grey (the colour of every reference line, level and trendline) without
    /// naming it, drawn behind the dots, 3 wide, without caps, with no label of its own, and it takes no colour slot: the dots
    /// wear the theme's first two colours and the connector wears neither.</summary>
    [Fact]
    public void TheConnector_IsNeutral_BehindTheDots_AndTakesNoSlot()
    {
        var figure = Plt.Create().WithSize(800, 450)
            .AddSubPlot(1, 1, 1, ax => ax.Dumbbell(Products, Before, After)).Build();
        var axes = figure.SubPlots[0];

        var connector = (ErrorBarSeries)axes.Series[2];

        Assert.Null(connector.Color);
        Assert.True(connector.WearsNeutral);
        Assert.Equal(3, connector.LineWidth);
        Assert.Equal(0, connector.CapSize);
        Assert.Equal(-1, connector.ZOrder);
        Assert.Null(connector.Label);

        string svg = Svg(figure);
        var colours = SeriesColours(svg);
        Assert.Equal(figure.Theme.CycleColors[0].ToHex(), colours[0]);
        Assert.Equal(figure.Theme.CycleColors[1].ToHex(), colours[1]);
        Assert.Equal(figure.Theme.Neutral.ToHex(), colours[2]);
        Assert.Equal(3, colours.Distinct().Count());
    }

    /// <summary>H1, measured: the connector is visible on every one of the 30 shipped themes, set AFTER the verb, because the
    /// connector names no colour and wears the theme's neutral grey, which stands at a contrast of 3 : 1 or more against the
    /// theme's plot (WCAG asks 3 : 1 of a graphic). A grey fixed when the verb runs, #808080, reached it on 25 of the 30 and
    /// stood at 2.32 : 1 on Dracula; the lighter #AAAAAA it wore before that reached it on 12 only.</summary>
    [Theory]
    [MemberData(nameof(ThemeCases.BuiltIn), MemberType = typeof(ThemeCases))]
    public void TheConnector_IsVisibleOnTheTheme_WhateverIsSetAfterTheVerb(string themeName)
    {
        var theme = themeName.AsTheme();

        var figure = Plt.Create().WithSize(800, 450)
            .AddSubPlot(1, 1, 1, ax => ax.Dumbbell(Products, Before, After)).WithTheme(theme).Build();
        var drawn = Color.FromHex(SeriesColours(Svg(figure))[2]);

        Assert.True(drawn.ContrastRatioWith(theme.AxesBackground) >= 3.0,
            $"{themeName}: the connector {drawn.ToHex()} stands at {drawn.ContrastRatioWith(theme.AxesBackground):F2} : 1 against {theme.AxesBackground.ToHex()}");
    }

    /// <summary>Added LAST with a z-order below the dots, the connector is the first thing drawn and the dots lie on top.</summary>
    [Fact]
    public void TheConnector_IsDrawnBeforeTheDots()
    {
        string svg = Svg(ax => ax.Dumbbell(Products, Before, After));

        int connector = svg.IndexOf(svg.SeriesGroup(2), StringComparison.Ordinal);
        int fromDots = svg.IndexOf(svg.SeriesGroup(0), StringComparison.Ordinal);
        int toDots = svg.IndexOf(svg.SeriesGroup(1), StringComparison.Ordinal);

        Assert.True(connector >= 0 && connector < fromDots && fromDots < toDots);
    }

    // ---- the legend (W1) ------------------------------------------------------------------------------------------

    /// <summary>A legend entry hides ONE part: the entries of the two dots carry the indices of the dots' series groups, and
    /// the connector's group (index 2) is toggled by no entry, so it stays visible when a dot series is hidden. The XML doc of
    /// <c>Dumbbell</c> says so.</summary>
    [Fact]
    public void ALegendClick_HidesOneDotSeriesOnly_TheConnectorHasNoEntry()
    {
        string svg = Svg(ax => ax.Dumbbell(Products, Before, After).WithLegend());

        var legend = svg.LegendEntries().Select(entry => entry.Index).ToList();
        var groups = svg.Elements("g").Where(group => group["class"] == "series").Select(group => (int)group.Number("data-series-index")).Order().ToList();

        Assert.Equal([0, 1], legend);
        Assert.Equal([0, 1, 2], groups);
        Assert.DoesNotContain(2, legend);
    }

    // ---- the callback ---------------------------------------------------------------------------------------------

    /// <summary>The callback runs once, after the verb has styled all three series, so what the caller sets wins; it hands over
    /// the very series on the axes.</summary>
    [Fact]
    public void TheCallback_ReceivesThePartsOnce_AfterTheVerbHasStyledThem()
    {
        int calls = 0;
        DumbbellParts seen = default;
        string? fromLabelWhenCalled = null, toLabelWhenCalled = null;
        int connectorZOrderWhenCalled = 0;

        var axes = Dumbbell(Before, After, parts =>
        {
            calls++;
            seen = parts;
            fromLabelWhenCalled = parts.From.Label;
            toLabelWhenCalled = parts.To.Label;
            connectorZOrderWhenCalled = parts.Connector.ZOrder;
            parts.From.MarkerSize = 30;
            parts.To.Marker = MarkerStyle.Diamond;
            parts.Connector.Color = Color.FromHex("#112233");
            parts.To.Label = "Later";
        });

        Assert.Equal(1, calls);
        Assert.Equal("Before", fromLabelWhenCalled);
        Assert.Equal("After", toLabelWhenCalled);
        Assert.Equal(-1, connectorZOrderWhenCalled);
        Assert.Same(axes.Series[0], seen.From);
        Assert.Same(axes.Series[1], seen.To);
        Assert.Same(axes.Series[2], seen.Connector);
        Assert.Equal(30, ((ScatterSeries)axes.Series[0]).MarkerSize);
        Assert.Equal(MarkerStyle.Diamond, ((ScatterSeries)axes.Series[1]).Marker);
        Assert.Equal(Color.FromHex("#112233"), ((ErrorBarSeries)axes.Series[2]).Color);
        Assert.Equal("Later", axes.Series[1].Label);
    }

    // ---- against the hand recipe, JSON, missing values ----------------------------------------------------------------

    /// <summary>A missing value in one group (NaN) leaves that category's dot and its connector out and nothing else.</summary>
    [Fact]
    public void AMissingValue_LeavesThatCategorysConnectorAndDotOut()
    {
        var after = new BarGroup("After", [74.0, double.NaN, 68.0, 79.5, 88.5, 61.0]);

        var figure = Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax.Dumbbell(Products, Before, after)).Build();

        figure.ToSvg().AssertNoNonFiniteNumber();                // the picture itself, without the toggle script
        string svg = Svg(figure);
        Assert.Equal(6, Dots(svg, 0).Length);
        Assert.Equal(5, Dots(svg, 1).Length);
        Assert.Equal(5, ConnectorBars(svg).Length);
    }

    // ---- refusals -------------------------------------------------------------------------------------------------

    /// <summary>Every refusal names the parameter at fault, and a group with no values at all is a refusal, not a null reference.</summary>
    [Fact]
    public void BadArguments_AreRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).Dumbbell(Products, Before, After)).ParamName);
        Assert.Equal("categories", Assert.Throws<ArgumentNullException>(() => Plt.Create().OneAxes(ax => ax.Dumbbell(null!, Before, After))).ParamName);


        LengthRefusals.AssertAGroupThatDoesNotFitIsRefusedByName((from, to) => Dumbbell(from, to), Before, After, 6);
    }

    // ---- reading the picture ----------------------------------------------------------------------------------------
    /// <summary>The vertical pixel of each dot of a dot series, in the order drawn.</summary>
    private static double[] Dots(string svg, int seriesIndex) =>
        [.. svg.SeriesGroup(seriesIndex).Elements("circle").Select(circle => circle.Number("cy"))];

    /// <summary>Where one connector bar runs, in pixels: its upper end (the smaller y) and its lower end.</summary>
    /// <param name="Top">The bar's upper end.</param>
    /// <param name="Bottom">The bar's lower end.</param>
    private readonly record struct Bar(double Top, double Bottom);

    /// <summary>The connector's bars: its lines that have a length (the zero-length cap lines are left out).</summary>
    private static Bar[] ConnectorBars(string svg) =>
        [.. svg.SeriesGroup(2).Elements("line").Where(line => line["stroke"] == "#808080" && line["stroke-width"] == "3")
            .Select(line => new Bar(Math.Min(line.Number("y1"), line.Number("y2")), Math.Max(line.Number("y1"), line.Number("y2"))))
            .Where(b => b.Bottom - b.Top > 1e-9)];

    /// <summary>The colour of each series group: the first fill or stroke inside it, upper case.</summary>
    private static List<string> SeriesColours(string svg)
    {
        var colours = new List<string>();
        foreach (int index in Enumerable.Range(0, 3))
        {
            colours.Add(svg.SeriesGroup(index).FirstColour().ToUpperInvariant());
        }

        return colours;
    }
}
