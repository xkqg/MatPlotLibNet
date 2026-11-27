// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Tests.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The lollipop chart in one call: ONE stem series, because a stem already draws its own head, standing on the
/// centre of each category's slot with the categories named on the X axis. One drawn unit is one series, so the chart wears
/// one colour, has one legend entry, and a click on that entry hides the whole chart. The verb sets no axis limits: a limit
/// clips every series on the axes and overwrites the caller's own.</summary>
public class LollipopTests
{
    private static readonly string[] Products = ["Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta"];
    private static readonly double[] Scores = [82.5, 67.3, 91.1, 54.8, 76.4, 88.2];

    /// <summary>The whole chart is one stem series: no scatter on top for the heads, no baseline or label series.</summary>
    [Fact]
    public void TheChart_IsOneStemSeries_AndNothingElse()
    {
        var axes = Plt.Create().OneAxes(ax => ax.Lollipop(Products, Scores));

        var stem = Assert.IsType<StemSeries>(Assert.Single(axes.Series));
        Assert.Empty(axes.YAxes.Right);
        Assert.Empty(axes.XSecondarySeries);
        Assert.Equal(Scores, stem.YData);
    }

    /// <summary>Each stem stands on the centre of its category's slot, the one rule a bar and a category name read.</summary>
    [Fact]
    public void EachStem_StandsOnTheCentreOfItsCategorysSlot()
    {
        var stem = Assert.IsType<StemSeries>(Assert.Single(Plt.Create().OneAxes(ax => ax.Lollipop(Products, Scores)).Series));

        Assert.Equal(Enumerable.Range(0, Products.Length).Select(k => CategorySlot.Of(k).Centre), stem.XData);
        Assert.Equal([0.5, 1.5, 2.5, 3.5, 4.5, 5.5], stem.XData);
    }

    /// <summary>The categories are the ticks of the X axis: a built-in locator and formatter made as one pair by the
    /// category-ticks owner, so they survive JSON.</summary>
    [Fact]
    public void TheCategories_AreNamedOnTheXAxis()
    {
        var axis = Plt.Create().OneAxes(ax => ax.Lollipop(Products, Scores)).XAxis;

        Assert.IsType<FixedLocator>(axis.TickLocator);
        Assert.IsType<CategoryFormatter>(axis.TickFormatter);
        var ticks = axis.TickLocator!.Locate(0, Products.Length);
        Assert.Equal([0.5, 1.5, 2.5, 3.5, 4.5, 5.5], ticks);
        Assert.Equal(Products, ticks.Select(axis.TickFormatter!.Format));
    }

    /// <summary>A limit the caller set before the verb is theirs and stays.</summary>
    [Fact]
    public void ALimitTheCallerSetFirst_IsKept()
    {
        var axes = Plt.Create().OneAxes(ax => ax.SetXLim(-1, 9).SetYLim(0, 120).Lollipop(Products, Scores));

        Assert.Equal(-1.0, axes.XAxis.Min);
        Assert.Equal(9.0, axes.XAxis.Max);
        Assert.Equal(0.0, axes.YAxis.Min);
        Assert.Equal(120.0, axes.YAxis.Max);
    }

    /// <summary>The callback styles the one series, once.</summary>
    [Fact]
    public void TheCallback_StylesTheOneSeries_Once()
    {
        int calls = 0;
        StemSeries? seen = null;

        var axes = Plt.Create().OneAxes(ax => ax.Lollipop(Products, Scores, s =>
        {
            calls++;
            seen = s;
            s.Label = "Score";
            s.Marker = MarkerStyle.Diamond;
        }));

        Assert.Equal(1, calls);
        Assert.Same(axes.Series[0], seen);
        Assert.Equal("Score", axes.Series[0].Label);
    }

    /// <summary>Under the automatic range every head is a whole circle inside the plot area and stands level with its
    /// category's name: the end heads keep the axis margin (W9: the verb sets no limit to make room for them).</summary>
    [Fact]
    public void EveryHead_IsWhole_AndStandsOverItsCategorysName()
    {
        string svg = Plt.Create().WithSize(800, 450)
            .AddSubPlot(1, 1, 1, ax => ax.Lollipop(Products, Scores))
            .ToSvg();

        var plot = svg.PlotArea();
        var heads = StemSeriesRendererTests.Heads(svg);

        Assert.Equal(Products.Length, heads.Length);
        for (int k = 0; k < Products.Length; k++)
        {
            Assert.InRange(heads[k].Cx - heads[k].Reach, plot.X, plot.X + plot.Width);
            Assert.InRange(heads[k].Cx + heads[k].Reach, plot.X, plot.X + plot.Width);
            Assert.InRange(heads[k].Cy - heads[k].Reach, plot.Y, plot.Y + plot.Height);
            var name = Assert.Single(BarSeriesRendererTests.Texts(svg, Products[k]));
            Assert.Equal(heads[k].Cx, name.X, 6);
        }
    }

    /// <summary>With no colour named the stem and its head wear the same colour, the series' own slot: one drawn unit, one
    /// colour.</summary>
    [Fact]
    public void WithNoColourNamed_StemAndHeadWearTheOneSlotColour()
    {
        string svg = Plt.Create().WithSize(800, 450)
            .AddSubPlot(1, 1, 1, ax => ax.Lollipop(Products, Scores))
            .ToSvg();

        var headFills = StemSeriesRendererTests.Heads(svg).Select(h => h.Fill).Distinct().ToList();
        var stemStrokes = svg.Elements("line").Where(line => line["stroke-width"] == "2.08").Select(line => line["stroke"]).OfType<string>()
            .Where(stroke => stroke.StartsWith('#')).Distinct().ToList();

        Assert.Equal(["#1F77B4"], headFills);
        Assert.Contains("#1F77B4", stemStrokes);
    }

    /// <summary>A missing score (NaN) draws no stem and no head, as for every other mark; the rest is drawn and no NaN
    /// reaches the picture.</summary>
    [Fact]
    public void AMissingScore_LeavesThatStemOut()
    {
        double[] scores = [82.5, 67.3, double.NaN, 54.8, 76.4, 88.2];

        string svg = Plt.Create().WithSize(800, 450)
            .AddSubPlot(1, 1, 1, ax => ax.Lollipop(Products, scores))
            .ToSvg();

        Assert.Equal(5, StemSeriesRendererTests.Heads(svg).Length);
        svg.AssertNoNonFiniteNumber();
    }

    /// <summary>Every refusal names the parameter at fault.</summary>
    [Fact]
    public void BadArguments_AreRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).Lollipop(Products, Scores)).ParamName);
        Assert.Equal("categories", Assert.Throws<ArgumentNullException>(() => Plt.Create().OneAxes(ax => ax.Lollipop(null!, Scores))).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => Plt.Create().OneAxes(ax => ax.Lollipop(Products, null!))).ParamName);

        var misaligned = Assert.Throws<ArgumentException>(() => Plt.Create().OneAxes(ax => ax.Lollipop(Products, [1.0, 2.0])));
        Assert.Equal("values", misaligned.ParamName);
        Assert.Contains(2.RefusalAgainst(6), misaligned.Message, StringComparison.Ordinal);
    }
}
