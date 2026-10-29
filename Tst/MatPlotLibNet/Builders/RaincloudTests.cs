// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>A raincloud plot in one call: a half violin, a narrow box and the raw, jittered points of each group, all
/// three sharing one colour group from the theme's cycle. Only the violin carries a label, so it alone stands for
/// the trio in the legend. The jitter is a fixed, deterministic formula (no <see cref="Random"/>), so the figure is
/// the same on every run and every platform.</summary>
public class RaincloudTests
{
    private static readonly NamedSample A = new("A", [4.1, 4.8, 5.0, 5.3, 5.5, 5.9, 6.2, 6.4, 6.8, 7.1, 7.7, 8.4]);
    private static readonly NamedSample B = new("B", [5.6, 6.1, 6.5, 6.9, 7.2, 7.4, 7.9, 8.3, 8.8, 9.1, 9.6, 10.2]);

    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    private static Axes Chart(IReadOnlyList<NamedSample> groups, Action<RaincloudParts>? configure = null) =>
        AxesOf(ax => ax.Raincloud(groups, configure));

    // ---- what the verb adds ----------------------------------------------------------------------------------------

    /// <summary>One half violin, one box and one scatter per chart — not per group: a multi-dataset
    /// <see cref="ViolinSeries"/> and <see cref="BoxSeries"/> already draw one shape per group on their own, so the
    /// verb needs no more than one of each, plus one scatter for every group's points together.</summary>
    [Fact]
    public void TheChart_IsOneHalfViolinOneBoxAndOneScatter_ForAllGroupsTogether()
    {
        var axes = Chart([A, B]);

        Assert.Equal(3, axes.Series.Count);
        var violin = Assert.IsType<ViolinSeries>(axes.Series[0]);
        var box = Assert.IsType<BoxSeries>(axes.Series[1]);
        var points = Assert.IsType<ScatterSeries>(axes.Series[2]);
        Assert.Equal(ViolinSide.High, violin.Side);
        Assert.False(box.ShowOutliers);
        Assert.Empty(axes.YAxes.Right);
    }

    /// <summary>The violin and the box hold one dataset per group, in the order given, each at its group's index — the
    /// default a multi-dataset series already stands at, so the verb sets no explicit positions.</summary>
    [Fact]
    public void TheViolinAndTheBox_HoldOneDatasetPerGroup_InOrder()
    {
        var axes = Chart([A, B]);
        var violin = (ViolinSeries)axes.Series[0];
        var box = (BoxSeries)axes.Series[1];

        Assert.Equal(2, violin.Datasets.Length);
        Assert.Equal(A.Values, violin.Datasets[0]);
        Assert.Equal(B.Values, violin.Datasets[1]);
        Assert.Equal(2, box.Datasets.Length);
        Assert.Equal(A.Values, box.Datasets[0]);
        Assert.Equal(B.Values, box.Datasets[1]);
        Assert.Null(violin.Positions);
        Assert.Null(box.Positions);
    }

    /// <summary>Every group's points are jittered the same deterministic way: a group's own index plus 0.35 (clear of
    /// the violin's own half-width and its whisker caps), then a 4-step cycle of 0.03 by the point's position in its
    /// group — no randomness, so two runs draw the same figure.</summary>
    [Fact]
    public void ThePoints_AreJitteredByAFixedDeterministicFormula()
    {
        var axes = Chart([A, B]);
        var points = (ScatterSeries)axes.Series[2];

        double[] expectedX = [.. Enumerable.Range(0, A.Values.Length).Select(i => 0.0 + 0.35 + (i % 4) * 0.03),
            .. Enumerable.Range(0, B.Values.Length).Select(i => 1.0 + 0.35 + (i % 4) * 0.03)];
        Assert.Equal(expectedX, points.XData);
        Assert.Equal([.. A.Values, .. B.Values], points.YData);
    }

    /// <summary>The groups are named along the X axis, one tick per group, at its index.</summary>
    [Fact]
    public void TheGroups_AreNamedAlongTheXAxis()
    {
        var axes = Chart([A, B]);

        Assert.NotNull(axes.XAxis.MajorTicks);
        Assert.NotNull(axes.XAxis.TickFormatter);
    }

    /// <summary>The violin, the box and the points of one chart all wear the same colour, from the theme's cycle — the
    /// first (and only, here) colour.</summary>
    [Fact]
    public void TheViolinTheBoxAndThePoints_ShareOneColourGroup()
    {
        var axes = Chart([A, B]);

        Assert.All(axes.Series, s => Assert.Equal(0, ((ChartSeries)s).ColorGroup));
    }

    /// <summary>Only the violin carries a label: one swatch names the trio as a whole rather than standing, falsely,
    /// for several differently named groups in one colour. The box and the points carry none, so the legend lists
    /// exactly one entry, and the groups' own names along the X axis are given separately.</summary>
    [Fact]
    public void OnlyTheViolin_CarriesALabel_SoTheLegendListsExactlyOneEntry()
    {
        var axes = Chart([A, B]);
        var violin = (ViolinSeries)axes.Series[0];
        var box = (BoxSeries)axes.Series[1];
        var points = (ScatterSeries)axes.Series[2];

        Assert.Equal("Samples", violin.Label);
        Assert.Null(box.Label);
        Assert.Null(points.Label);
        Assert.Single(axes.LegendEntries(directLabelsDrawn: false));
    }

    // ---- the callback -----------------------------------------------------------------------------------------------

    /// <summary>The callback runs once, after the verb has built all three parts, and hands over the very series on the
    /// axes, so what it sets wins.</summary>
    [Fact]
    public void TheCallback_RunsOnce_OnThePartsOnTheAxes_AfterTheVerbBuiltThem()
    {
        RaincloudParts? seen = null;

        var axes = Chart([A, B], parts => { seen = parts; parts.Violin.Alpha = 0.9; });

        Assert.NotNull(seen);
        Assert.Same(axes.Series[0], seen.Value.Violin);
        Assert.Same(axes.Series[1], seen.Value.Box);
        Assert.Same(axes.Series[2], seen.Value.Points);
        Assert.Equal(0.9, ((ViolinSeries)axes.Series[0]).Alpha);
    }

    // ---- the hand recipe --------------------------------------------------------------------------------------------

    /// <summary>The verb draws exactly the picture of the hand recipe made of the public building blocks, with and
    /// without a missing observation.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheVerb_DrawsWhatTheHandRecipeDraws(bool missing)
    {
        string byVerb = ComposedChartShapes.Named("raincloud").Build(missing).ToSvg();
        string byHand = ComposedChartShapes.RaincloudByHand(missing).ToSvg();

        Assert.Equal(byHand, byVerb);
    }

    /// <summary>A raincloud chart is its parts in JSON: read back it draws the same picture. It is not estimated
    /// again.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheChart_SurvivesJson(bool missing)
    {
        var figure = ComposedChartShapes.Named("raincloud").Build(missing).Build();

        string drawn = figure.ToSvg();
        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(drawn, readBack);
    }

    // ---- refusals -----------------------------------------------------------------------------------------------

    /// <summary>A missing argument is refused by name.</summary>
    [Fact]
    public void MissingArguments_AreRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).Raincloud([A])).ParamName);
        Assert.Equal("groups", Assert.Throws<ArgumentNullException>(() => Chart(null!)).ParamName);
    }

    /// <summary>A chart of no groups has nothing to draw.</summary>
    [Fact]
    public void NoGroups_AreRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => Chart([]));

        Assert.Equal("groups", error.ParamName);
    }
}
