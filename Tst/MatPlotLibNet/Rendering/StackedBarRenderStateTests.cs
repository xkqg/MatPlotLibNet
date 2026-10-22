// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A stacked render puts each bar on the bars before it, and side-by-side bars step aside for each other.
/// Both are the RENDER's arithmetic: they are worked out afresh on every render and never written into what the user
/// set. A render that wrote them into the figure would leave a trace that the next render, and the JSON, would read
/// as the user's own choice.</summary>
public class StackedBarRenderStateTests
{
    private static Figure Bars(BarMode mode) => Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax
        .Bar(["A", "B"], [1.0, 2.0])
        .Bar(["A", "B"], [3.0, 4.0])
        .SetBarMode(mode)).Build();

    [Fact]
    public void AStackedRender_LeavesTheStackBaselineOfEveryBarAsTheUserSetIt()
    {
        var figure = Bars(BarMode.Stacked);

        figure.ToSvg();

        Assert.All(figure.SubPlots[0].Series.OfType<BarSeries>(), bar => Assert.Null(bar.StackBaseline));
    }

    [Fact]
    public void AStackedRender_ChangesNothingTheJsonCarries()
    {
        var figure = Bars(BarMode.Stacked);
        string before = figure.ToJson();

        figure.ToSvg();

        Assert.Equal(before, figure.ToJson());
    }

    [Fact]
    public void AfterAStackedRender_AGroupedRender_DrawsWhatAFreshGroupedFigureDraws()
    {
        var figure = Bars(BarMode.Stacked);
        figure.ToSvg();

        figure.SubPlots[0].BarMode = BarMode.Grouped;

        Assert.Equal(Bars(BarMode.Grouped).ToSvg(), figure.ToSvg());
    }

    [Fact]
    public void AfterAGroupedRender_AStackedRender_DrawsWhatAFreshStackedFigureDraws()
    {
        var figure = Bars(BarMode.Grouped);
        figure.ToSvg();

        figure.SubPlots[0].BarMode = BarMode.Stacked;

        Assert.Equal(Bars(BarMode.Stacked).ToSvg(), figure.ToSvg());
    }

    /// <summary>A stacked bar still stands on the one before it: the second series' bars start where the first
    /// series' bars end, exactly as a bar the user lifts onto those heights by hand.</summary>
    [Fact]
    public void AStackedBar_StandsOnTheBarsBeforeIt()
    {
        var stacked = Bars(BarMode.Stacked);
        var byHand = Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax
            .Bar(["A", "B"], [1.0, 2.0], s => s.StackBaseline = [0, 0])
            .Bar(["A", "B"], [3.0, 4.0], s => s.StackBaseline = [1, 2])
            .SetYLim(0, 6.3)).Build();
        stacked.SubPlots[0].YAxis.Min = 0;
        stacked.SubPlots[0].YAxis.Max = 6.3;

        Assert.Equal(Rects(byHand.ToSvg()), Rects(stacked.ToSvg()));
    }

    /// <summary>The stack sums every stackable series, in the order they were added, the same set the data range sums;
    /// a stackable that is not a bar lifts the bars after it too.</summary>
    [Fact]
    public void AStackableThatIsNotABar_LiftsTheBarsAfterIt()
    {
        var figure = Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax.SetBarMode(BarMode.Stacked)).Build();
        var axes = figure.SubPlots[0];
        axes.AddSeries(new ForeignStackable([5.0, 7.0]));
        var bar = axes.Bar(["A", "B"], [1.0, 2.0]);

        figure.ToSvg();

        Assert.NotNull(bar.StackedBottoms);
        Assert.Equal([5.0, 7.0], bar.StackedBottoms);
        Assert.Null(bar.StackBaseline);
    }

    private static string[] Rects(string svg) =>
        [.. System.Text.RegularExpressions.Regex.Matches(svg, "<rect[^>]*>").Select(m => m.Value).Where(r => !r.Contains("width=\"400\"", StringComparison.Ordinal))];

    /// <summary>A stackable series that draws nothing and is not a bar.</summary>
    private sealed class ForeignStackable(double[] values) : ChartSeries, IStackable
    {
        public double[] Values { get; } = values;

        public double[]? StackBaseline { get; set; }

        public override void Accept(MatPlotLibNet.Rendering.ISeriesVisitor visitor, MatPlotLibNet.Rendering.RenderArea area) { }

        public override DataRangeContribution ComputeDataRange(IAxesContext context) => new(0, 2, 0, 12);

        public override MatPlotLibNet.Serialization.SeriesDto ToSeriesDto() => new() { Type = "foreign" };
    }
}
