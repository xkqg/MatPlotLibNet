// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.SeriesRenderers;
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

    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    /// <summary>The whole chart is one stem series: no scatter on top for the heads, no baseline or label series.</summary>
    [Fact]
    public void TheChart_IsOneStemSeries_AndNothingElse()
    {
        var axes = AxesOf(ax => ax.Lollipop(Products, Scores));

        var stem = Assert.IsType<StemSeries>(Assert.Single(axes.Series));
        Assert.Empty(axes.SecondarySeries);
        Assert.Empty(axes.XSecondarySeries);
        Assert.Equal(Scores, stem.YData);
    }

    /// <summary>Each stem stands on the centre of its category's slot, the one rule a bar and a category name read.</summary>
    [Fact]
    public void EachStem_StandsOnTheCentreOfItsCategorysSlot()
    {
        var stem = Assert.IsType<StemSeries>(Assert.Single(AxesOf(ax => ax.Lollipop(Products, Scores)).Series));

        Assert.Equal(Enumerable.Range(0, Products.Length).Select(k => CategorySlot.Of(k).Centre), stem.XData);
        Assert.Equal([0.5, 1.5, 2.5, 3.5, 4.5, 5.5], stem.XData);
    }

    /// <summary>The categories are the ticks of the X axis: a built-in locator and formatter made as one pair by the
    /// category-ticks owner, so they survive JSON.</summary>
    [Fact]
    public void TheCategories_AreNamedOnTheXAxis()
    {
        var axis = AxesOf(ax => ax.Lollipop(Products, Scores)).XAxis;

        Assert.IsType<FixedLocator>(axis.TickLocator);
        Assert.IsType<CategoryFormatter>(axis.TickFormatter);
        var ticks = axis.TickLocator!.Locate(0, Products.Length);
        Assert.Equal([0.5, 1.5, 2.5, 3.5, 4.5, 5.5], ticks);
        Assert.Equal(Products, ticks.Select(axis.TickFormatter!.Format));
    }

    /// <summary>The verb leaves the limits to the automatic range: a limit would clip every other series on the axes (W9).</summary>
    [Fact]
    public void TheVerb_SetsNoAxisLimits()
    {
        var axes = AxesOf(ax => ax.Lollipop(Products, Scores));

        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
        Assert.False(axes.HasExplicitLimit);
    }

    /// <summary>A limit the caller set before the verb is theirs and stays.</summary>
    [Fact]
    public void ALimitTheCallerSetFirst_IsKept()
    {
        var axes = AxesOf(ax => ax.SetXLim(-1, 9).SetYLim(0, 120).Lollipop(Products, Scores));

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

        var axes = AxesOf(ax => ax.Lollipop(Products, Scores, s =>
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

    /// <summary>The verb adds nothing to the picture that the hand-written chain did not draw: same SVG, byte for byte,
    /// with and without a missing value.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDrew(bool missing)
    {
        string byVerb = ComposedChartShapes.Named("lollipop").Build(missing).ToSvg();
        string byHand = ComposedChartShapes.LollipopByHand(missing).ToSvg();

        Assert.Equal(byHand, byVerb);
    }

    /// <summary>Under the automatic range every head is a whole circle inside the plot area and stands level with its
    /// category's name: the end heads keep the axis margin (W9: the verb sets no limit to make room for them).</summary>
    [Fact]
    public void EveryHead_IsWhole_AndStandsOverItsCategorysName()
    {
        string svg = Plt.Create().WithSize(800, 450)
            .AddSubPlot(1, 1, 1, ax => ax.Lollipop(Products, Scores))
            .ToSvg();

        var plot = BarSeriesRendererTests.PlotArea(svg);
        var heads = StemSeriesRendererTests.Heads(svg);

        Assert.Equal(Products.Length, heads.Length);
        for (int k = 0; k < Products.Length; k++)
        {
            Assert.InRange(heads[k].Cx - 4, plot.X, plot.X + plot.W);
            Assert.InRange(heads[k].Cx + 4, plot.X, plot.X + plot.W);
            Assert.InRange(heads[k].Cy - 4, plot.Y, plot.Y + plot.H);
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

        var headFills = System.Text.RegularExpressions.Regex.Matches(svg, """<circle [^>]*r="4" fill="(#[0-9A-Fa-f]{6})" />""")
            .Select(m => m.Groups[1].Value).Distinct().ToList();
        var stemStrokes = System.Text.RegularExpressions.Regex.Matches(svg, """<line x1="[-0-9.]+" y1="[-0-9.]+" x2="[-0-9.]+" y2="[-0-9.]+" stroke="(#[0-9A-Fa-f]{6})" stroke-width="1" />""")
            .Select(m => m.Groups[1].Value).Distinct().ToList();

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
        Assert.DoesNotContain("NaN", svg);
    }

    /// <summary>A figure with a lollipop chart survives JSON and draws the same picture; the one stem series keeps its
    /// styling and the categories keep their names.</summary>
    [Fact]
    public void TheChart_SurvivesJson()
    {
        var figure = Plt.Create().WithSize(800, 450)
            .AddSubPlot(1, 1, 1, ax => ax.Lollipop(Products, Scores, s => s.StemColor = Color.FromHex("#5B9BD5")))
            .Build();

        string drawn = figure.ToSvg();
        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(drawn, readBack);
    }

    /// <summary>Every refusal names the parameter at fault.</summary>
    [Fact]
    public void BadArguments_AreRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).Lollipop(Products, Scores)).ParamName);
        Assert.Equal("categories", Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.Lollipop(null!, Scores))).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.Lollipop(Products, null!))).ParamName);

        var misaligned = Assert.Throws<ArgumentException>(() => AxesOf(ax => ax.Lollipop(Products, [1.0, 2.0])));
        Assert.Equal("values", misaligned.ParamName);
        Assert.Contains("2 values were given for 6 categories", misaligned.Message, StringComparison.Ordinal);
    }

    /// <summary>The refusal leaves the axes untouched: a verb that throws has added no series and set no ticks.</summary>
    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var builder = new AxesBuilder();

        Assert.Throws<ArgumentException>(() => builder.Lollipop(Products, [1.0, 2.0]));
        var axes = builder.Build(1, 1, 1);

        Assert.Empty(axes.Series);
        Assert.Null(axes.XAxis.TickLocator);
        Assert.Null(axes.XAxis.TickFormatter);
    }
}
