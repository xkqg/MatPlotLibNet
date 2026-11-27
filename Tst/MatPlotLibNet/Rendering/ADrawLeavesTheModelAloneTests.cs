// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A draw reads the figure and writes nothing into it. The ticks a scale brings (the decades of a log axis, the
/// symmetric-log locator, the dates of a date X axis) are read where they are needed, never installed on the axis, so a figure
/// drawn once is the figure it was before: the next draw, its JSON and a later change of scale all see the model the caller
/// built. The range follows the same reading: an axis whose scale brings a locator is not widened to linear nice bounds, on the
/// first draw as on every later one.</summary>
public class ADrawLeavesTheModelAloneTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];

    private static Figure LogY(AxisScale scale = AxisScale.Log) =>
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [2.0, 20.0, 250.0]).SetYScale(scale)).Build();

    private static Figure BareSymlogY() =>
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [-37.0, 5.5, 88.0]).SetYScale(AxisScale.SymLog)).Build();

    private static Figure BareDateX() =>
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([19000.2, 19002.9, 19003.4], [1.0, 3.0, 2.0]).SetXScale(AxisScale.Date)).Build();

    /// <summary>Draws the figure again for real: a change of title and back makes the subplot draw again rather than hand back
    /// the drawing it kept.</summary>
    private static string DrawAgain(Figure figure)
    {
        var title = figure.SubPlots[0].Title;
        figure.SubPlots[0].Title = "again";
        figure.SubPlots[0].Title = title;
        return figure.ToSvg();
    }

    [Fact]
    public void ALogAxisDrawnAndMadeLinear_HasNoLocator_AndDrawsAsANeverDrawnLinearTwin()
    {
        var figure = LogY();
        _ = figure.ToSvg();

        figure.SubPlots[0].YAxis.Scale = AxisScale.Linear;

        Assert.Null(figure.SubPlots[0].YAxis.TickLocator);
        Assert.Null(figure.SubPlots[0].YAxis.TickFormatter);
        Assert.Equal(LogY(AxisScale.Linear).ToSvg(), figure.ToSvg());
    }

    [Fact]
    public void ABareSymlogFigure_DrawsTheSameTheFirstTimeAsTheNext()
    {
        var figure = BareSymlogY();

        var first = figure.ToSvg();

        Assert.Equal(first, DrawAgain(figure));
        Assert.Null(figure.SubPlots[0].YAxis.TickLocator);
    }

    [Fact]
    public void ABareDateXFigure_DrawsTheSameTheFirstTimeAsTheNext()
    {
        var figure = BareDateX();

        var first = figure.ToSvg();

        Assert.Equal(first, DrawAgain(figure));
        Assert.Null(figure.SubPlots[0].XAxis.TickLocator);
        Assert.Null(figure.SubPlots[0].XAxis.TickFormatter);
    }

    [Fact]
    public void TheJson_IsTheSameBeforeAndAfterADraw()
    {
        var figure = LogY();
        var before = figure.ToJson();

        _ = figure.ToSvg();

        Assert.Equal(before, figure.ToJson());
    }

    /// <summary>A Y axis slot linked to a bare symmetric-log primary is placed on the range the primary was drawn with, on the
    /// first draw too: the primary's range is never worked out once before and once after a write into the model.</summary>
    [Fact]
    public void ALinkedSlot_IsPlacedOnTheRangeThePrimaryWasDrawnWith_OnTheFirstDraw()
    {
        var axes = new Axes();
        axes.Plot(X, [-37.0, 5.5, 88.0]);
        axes.YAxis.Scale = AxisScale.SymLog;
        var slot = axes.YAxes.StackOn(AxisSide.Right).Add();
        slot.Axis.Link = AxisTransform.Linear(2, 0);
        var plot = new Rect(80, 40, 400, 300);
        var renderer = new CartesianAxesRenderer(axes, plot, new RecordingRenderContext(), Theme.Default);

        renderer.Render();

        var placement = new CartesianAxisPlacement(axes, plot, new RecordingRenderContext(), Theme.Default);
        Assert.Equal(renderer.DrawnTransform!.DrawnRange, placement.PlaceSlot(slot, placement.ComputeDataRanges()).Transform.DrawnRange);
    }
}
