// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>How often one draw computes the data range of an axes. The range is the sum of every series' contribution, so each
/// time the axes computes it a series is asked once more; <see cref="CountingRangeSeries"/> counts those questions. These tests
/// hold the count of the shapes a live wall draws, so a change that computes the range fewer times has to say so here.</summary>
public class RangeComputationCountTests
{
    private static readonly double[] X = [0, 1, 2, 3];
    private static readonly double[] Y = [1, 4, 9, 16];

    private static (Figure Figure, CountingRangeSeries Counter) FigureWith(Action<AxesBuilder> configure)
    {
        var counter = new CountingRangeSeries(X, Y);
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => { ax.Plot(X, Y, s => s.Label = "line"); configure(ax); }).Build();
        figure.SubPlots[0].AddSeries(counter);
        return (figure, counter);
    }

    [Fact]
    public void ACountingSeries_IsAskedForItsRangeWhenTheFigureIsDrawn()
    {
        var (figure, counter) = FigureWith(_ => { });

        Assert.Equal(0, counter.RangeCalls);
        figure.ToSvg();

        Assert.True(counter.RangeCalls >= 1);
    }

    [Fact]
    public void ACountingSeries_ReportsTheRangeOfItsPoints()
    {
        var counter = new CountingRangeSeries(X, Y);

        var range = counter.ComputeDataRange(new AxesContextAdapter(new Axes()));

        Assert.Equal(new DataRangeContribution(0, 3, 1, 16), range);
        Assert.Equal(1, counter.RangeCalls);
    }

    [Fact]
    public void ACountingSeries_HasNoTabularFormAndDrawsNothing()
    {
        var counter = new CountingRangeSeries(X, Y);

        Assert.Null(counter.ToDataTable());
        Assert.Equal("counting", counter.ToSeriesDto().Type);
        counter.Accept(null!, null!);
    }

    /// <summary>One panel with a date axis, a grid and a top-centre legend, the shape the panels of a live wall have, computes
    /// its range once in a draw: the layout measures the panel three times and the drawing draws it, and all four read the range
    /// the draw worked out first. A legend that looks for the emptiest corner reads the same range for the marks it steers around.</summary>
    [Theory]
    [InlineData(LegendPosition.UpperCenter)]
    [InlineData(LegendPosition.Best)]
    public void APanelWithALegend_ComputesItsRangeOnceInADraw(LegendPosition position)
    {
        var (figure, counter) = FigureWith(ax => ax.SetXDateAxis().WithGrid(g => g with { Visible = true })
            .WithLegend(l => l with { Position = position }));

        figure.ToSvg();

        Assert.Equal(1, counter.RangeCalls);
    }

    /// <summary>A log or symlog axis (whose legend, set by nothing, looks for the emptiest corner) computes its range once in the
    /// first draw and once in the next: a draw shares the range among its own renderers, never with the draw after it.</summary>
    [Theory]
    [InlineData(AxisScale.Log)]
    [InlineData(AxisScale.SymLog)]
    public void AScaledPanel_ComputesItsRangeOnceInEveryDraw(AxisScale scale)
    {
        var (figure, counter) = FigureWith(ax => ax.SetYScale(scale));

        figure.ToSvg();
        var first = counter.RangeCalls;
        figure.ToSvg();

        Assert.Equal(1, first);
        Assert.Equal(1, counter.RangeCalls - first);
    }

    /// <summary>The seven stacked panels of a live wall compute their range once each in one draw.</summary>
    [Fact]
    public void AStackOfSevenPanels_ComputesTheRangeOfEveryPanelOnceInADraw()
    {
        var counters = new List<CountingRangeSeries>();
        var builder = Plt.Create().WithSize(1000, 1330).WithGridSpec(7, 1);
        for (var p = 0; p < 7; p++)
        {
            builder.AddSubPlot(GridPosition.Single(p, 0), ax => ax.Plot(X, Y, s => s.Label = "line").SetXDateAxis()
                .WithGrid(g => g with { Visible = true }).WithLegend(l => l with { Position = LegendPosition.UpperCenter }));
        }
        var figure = builder.Build();
        foreach (var axes in figure.SubPlots)
        {
            var counter = new CountingRangeSeries(X, Y);
            counters.Add(counter);
            axes.AddSeries(counter);
        }

        figure.ToSvg();

        Assert.All(counters, c => Assert.Equal(1, c.RangeCalls));
    }

    /// <summary>An axes with every reader of the range: a Y slot linked to the primary, a Y slot with a series of its own, a top axis
    /// with a series, a legend at <see cref="LegendPosition.Best"/> and a text that moves out of the way.</summary>
    private static CountingRangeSeries WithEveryReader(Axes axes)
    {
        axes.Plot(X, Y).Label = "line";
        var counter = axes.AddSeries(new CountingRangeSeries(X, Y));
        axes.Legend = axes.Legend with { Position = LegendPosition.Best };
        axes.Annotate("Mover", 1, 4).AvoidOverlap = true;
        axes.YAxes.StackOn(AxisSide.Right).Add().Axis.Link = AxisTransform.Linear(1000);
        axes.YAxes.StackOn(AxisSide.Right).Add().AddSeries(new LineSeries(X, [10.0, 40, 20, 30]));
        axes.PlotXSecondary([0.0, 10, 20], [1.0, 3, 2]);
        return counter;
    }

    /// <summary>A draw of such an axes, laid out and drawn, computes its range once.</summary>
    [Fact]
    public void ADrawOfAnAxesWithEveryReader_ComputesItsRangeOnce()
    {
        var figure = new Figure();
        var counter = WithEveryReader(figure.AddSubPlot());

        figure.ToSvg();

        Assert.Equal(1, counter.RangeCalls);
    }

    /// <summary>A renderer made outside a draw works the range out itself, once for its whole render: the linked slot, the legend's
    /// marks and the moving text's marks are placed over the range the render took first.</summary>
    [Fact]
    public void ARenderOfAnAxesWithEveryReader_ComputesItsRangeOnce()
    {
        var axes = new Axes();
        var counter = WithEveryReader(axes);

        new CartesianAxesRenderer(axes, new Rect(80, 40, 560, 320), new RecordingRenderContext(), Theme.Default).Render();

        Assert.Equal(1, counter.RangeCalls);
    }

    /// <summary>The patches of an axes are clipped where they can reach past the plot (<c>AxesRange.PatchesMayReachPastPlot</c>),
    /// which depends on what asks for the range; that answer is the one the range of the draw gave, so a draw that has patches asks
    /// every series for its range once, as a draw without them does.</summary>
    [Fact]
    public void ADrawOfAnAxesWithPatches_ComputesItsRangeOnce()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var counter = WithEveryReader(axes);
        axes.AddPatch(new Rectangle(new(1, 1), 2, 1));
        axes.AddPatch(new Polygon([new(0, 0), new(2, 0), new(1, 2)]));

        figure.ToSvg();

        Assert.Equal(1, counter.RangeCalls);
    }

    /// <summary>A renderer made outside a draw, with patches, works the range out once for its render and the clip of the patches
    /// reads the answer that range gave.</summary>
    [Fact]
    public void ARenderOfAnAxesWithPatches_ComputesItsRangeOnce()
    {
        var axes = new Axes();
        var counter = WithEveryReader(axes);
        axes.AddPatch(new Rectangle(new(1, 1), 2, 1));

        new CartesianAxesRenderer(axes, new Rect(80, 40, 560, 320), new RecordingRenderContext(), Theme.MatplotlibV2).Render();

        Assert.Equal(1, counter.RangeCalls);
    }

    /// <summary>An inset that indicates its zoom is ranged once in a draw: its parent draws the indicator under it from the limits
    /// the inset will draw, before the inset is drawn, and the inset's own render (its ticks, its Best legend's marks and the view
    /// a server reads) reads the same range, which the draw keeps for the inset as it keeps it for a subplot.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnInsetThatIndicatesItsZoom_ComputesItsRangeOnceInADraw(bool serverView)
    {
        var counter = new CountingRangeSeries(X, Y);
        var builder = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 20.0])
            .AddInset(0.55, 0.05, 0.4, 0.4, i => i.Plot(X, Y, s => s.Label = "inset").WithLegend(l => l with { Position = LegendPosition.Best })
                .IndicateZoom()));
        if (serverView) builder.WithServerInteraction("chart-1", i => i.EnableZoom());
        var figure = builder.Build();
        figure.SubPlots[0].Insets[0].AddSeries(counter);

        string svg = figure.ToSvg();

        Assert.Contains("zoom-indicator", svg);
        Assert.Equal(1, counter.RangeCalls);
    }

    /// <summary>Measuring what an axes with a top axis draws above and below its plot computes the range once.</summary>
    [Fact]
    public void MeasuringTheEndsOfAnAxesWithATopAxis_ComputesItsRangeOnce()
    {
        var axes = new Axes();
        var counter = WithEveryReader(axes);

        new CartesianAxesRenderer(axes, new Rect(80, 40, 560, 320), new RecordingRenderContext(), Theme.Default).MeasureEndBands(default);

        Assert.Equal(1, counter.RangeCalls);
    }

    /// <summary>The layout a native control hit-tests with (its plot areas and the legend rows of a legend at
    /// <see cref="LegendPosition.Best"/>) computes the range once.</summary>
    [Fact]
    public void TheLayoutOfAFigure_ComputesItsRangeOnce()
    {
        var (figure, counter) = FigureWith(ax => ax.WithLegend(l => l with { Position = LegendPosition.Best }));

        new ChartRenderer().ComputeLayout(figure, new RecordingRenderContext());

        Assert.Equal(1, counter.RangeCalls);
    }
}
