// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>
/// A bar's value label stands beyond the bar's end, in the direction the bar points in the data: above the end of a bar that
/// rises on an axis that runs up, below it when the axis runs down, and the other way round for a bar that falls; to the right
/// or the left of a horizontal bar's end likewise. matplotlib's <c>bar_label</c> places a label at the bar's edge this way: a
/// value of zero or more counts as rising, and padding is added away from the foot of the bar.
/// </summary>
public class BarLabelDirectionTests
{
    private static readonly Rect Plot = new(50, 40, 400, 300);
    private const double Pad = 4;

    private static readonly double[] Values = [3.0, -2.0, 0.0];

    private static DataTransform Axis(bool valueAxisRunsBackwards, bool horizontal) => horizontal
        ? new DataTransform(valueAxisRunsBackwards ? 8 : -4, valueAxisRunsBackwards ? -4 : 8, 0, 10, Plot)
        : new DataTransform(0, 10, valueAxisRunsBackwards ? 8 : -4, valueAxisRunsBackwards ? -4 : 8, Plot);

    /// <summary>The label of each bar: where its text stands and how it is aligned.</summary>
    private sealed record Drawn(Point Position, TextAlignment Alignment);

    private static List<Drawn> LabelsOf(BarSeries series, DataTransform transform)
    {
        var ctx = new RecordingRenderContext();
        series.Accept(new SvgSeriesRenderer(transform, ctx, Colors.Blue, Theme.Default, plotArea: Plot), new RenderArea(Plot, ctx));
        return [.. ctx.Of<DrawTextCall>().Select(c =>
        {
            return new Drawn(c.Position, c.Alignment);
        })];
    }

    private static double FontSize => Theme.Default.DataTextFont().Size;

    private static List<string> TextsOf(BarSeries series, DataTransform transform)
    {
        var ctx = new RecordingRenderContext();
        series.Accept(new SvgSeriesRenderer(transform, ctx, Colors.Blue, Theme.Default, plotArea: Plot), new RenderArea(Plot, ctx));
        return [.. ctx.Of<DrawTextCall>().Select(c => c.Text)];
    }

    /// <summary>matplotlib's <c>bar_label</c> annotates the end of each bar (<c>xy</c> in data coordinates), and an annotation whose point
    /// the axes do not show is not drawn (<c>Annotation._check_xy</c>: <c>Axes.contains_point</c>, the axes patch with a radius of 1.0,
    /// half a pixel past each edge). Bars of 1 and 3 on a value axis that shows 0 to 2: only the first is labelled, upright or lying
    /// down.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ALabelWhoseBarEndsPastTheAxes_IsNotDrawn(bool horizontal)
    {
        var transform = horizontal ? new DataTransform(0, 2, 0, 10, Plot) : new DataTransform(0, 10, 0, 2, Plot);
        var bars = new BarSeries(["a", "b"], [1.0, 3.0]) { ShowLabels = true, Orientation = horizontal ? BarOrientation.Horizontal : BarOrientation.Vertical };

        Assert.Equal(["1"], TextsOf(bars, transform));
    }

    /// <summary>The edge of the axes holds half a pixel either side, as matplotlib's does (a bar of 2.002 on 0 to 2 over 462 px, 0.46 px
    /// past, is labelled; one of 2.0025, 0.58 px past, is not). Here a pixel is 1/150 of a unit: 0.4 px past is shown, 0.6 px is not.</summary>
    [Theory]
    [InlineData(2.0, true)]
    [InlineData(2.0 + 0.4 / 150, true)]
    [InlineData(2.0 + 0.6 / 150, false)]
    public void TheEdgeOfTheAxes_ShowsAPointWithinHalfAPixel(double value, bool labelled)
    {
        var bars = new BarSeries(["a"], [value]) { ShowLabels = true };

        Assert.Equal(labelled ? 1 : 0, TextsOf(bars, new DataTransform(0, 10, 0, 2, Plot)).Count);
    }

    private static BarSeries Bars(BarOrientation orientation) =>
        new(["a", "b", "c"], Values) { ShowLabels = true, Orientation = orientation };

    // ---- the axis the value runs along --------------------------------------------------------------------------

    [Theory]
    [InlineData(false, false, false)]   // a vertical bar's value axis runs up: not backwards
    [InlineData(false, true, true)]     // limits set high to low: backwards
    [InlineData(true, false, false)]    // a horizontal bar's value axis runs right
    [InlineData(true, true, true)]
    public void TheValueAxis_RunsBackwards_WhenItsLimitsAreSetHighToLow(bool horizontal, bool backwards, bool expected)
    {
        var orientation = horizontal ? BarOrientation.Horizontal : BarOrientation.Vertical;

        Assert.Equal(expected, BarSeriesRenderer.ValueRunsBackwards(Axis(backwards, horizontal), orientation));
    }

    // ---- vertical bars --------------------------------------------------------------------------------------------

    [Fact]
    public void ARisingBar_OnAnAxisThatRunsUp_HasItsLabelPadAboveItsEnd()
    {
        var transform = Axis(valueAxisRunsBackwards: false, horizontal: false);

        var label = LabelsOf(Bars(BarOrientation.Vertical), transform)[0];

        Assert.Equal(transform.DataToPixel(0, 3).Y - Pad, label.Position.Y, 6);
        Assert.Equal(TextAlignment.Center, label.Alignment);
    }

    [Theory]
    [InlineData(false, 1, -2)]
    [InlineData(true, 0, 3)]
    public void AVerticalBar_EndingTowardsTheBottom_HasItsLabelBelowItsEnd(bool valueAxisRunsBackwards, int bar, double end)
    {
        var transform = Axis(valueAxisRunsBackwards, horizontal: false);

        var label = LabelsOf(Bars(BarOrientation.Vertical), transform)[bar];

        // Below the end: the pad, then the height the letters rise above their baseline.
        Assert.Equal(transform.DataToPixel(0, end).Y + Pad + FontSize, label.Position.Y, 6);
    }

    [Fact]
    public void AFallingBar_OnAnAxisThatRunsDown_HasItsLabelAboveItsEnd()
    {
        var transform = Axis(valueAxisRunsBackwards: true, horizontal: false);

        var label = LabelsOf(Bars(BarOrientation.Vertical), transform)[1];

        Assert.Equal(transform.DataToPixel(0, -2).Y - Pad, label.Position.Y, 6);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AZeroBar_CountsAsRising(bool backwards)
    {
        var transform = Axis(backwards, horizontal: false);

        var label = LabelsOf(Bars(BarOrientation.Vertical), transform)[2];

        double end = transform.DataToPixel(0, 0).Y;
        Assert.Equal(backwards ? end + Pad + FontSize : end - Pad, label.Position.Y, 6);
    }

    // ---- horizontal bars ------------------------------------------------------------------------------------------

    /// <summary>A horizontal bar's label stands a pad past its end, on the side the bar ends towards: right of a rising bar on an axis that
    /// runs right and of a falling one on an axis that runs left, left of the other two, aligned to the end.</summary>
    [Theory]
    [InlineData(false, 0, 3, 1, TextAlignment.Left)]
    [InlineData(false, 1, -2, -1, TextAlignment.Right)]
    [InlineData(true, 0, 3, -1, TextAlignment.Right)]
    [InlineData(true, 1, -2, 1, TextAlignment.Left)]
    public void AHorizontalBar_HasItsLabelBesideItsEnd(bool valueAxisRunsBackwards, int bar, double end, int side, TextAlignment alignment)
    {
        var transform = Axis(valueAxisRunsBackwards, horizontal: true);

        var label = LabelsOf(Bars(BarOrientation.Horizontal), transform)[bar];

        Assert.Equal(transform.DataToPixel(end, 0).X + side * Pad, label.Position.X, 6);
        Assert.Equal(alignment, label.Alignment);
    }

    // ---- what does not change --------------------------------------------------------------------------------------

    [Fact]
    public void OnALogAxis_ARisingBarsLabelStaysPadAboveItsEnd()
    {
        var transform = Plot.LogY();
        var series = new BarSeries(["a", "b"], [10.0, 50.0]) { ShowLabels = true };

        var labels = LabelsOf(series, transform);

        Assert.Equal(transform.DataToPixel(0, 10).Y - Pad, labels[0].Position.Y, 6);
        Assert.Equal(transform.DataToPixel(1, 50).Y - Pad, labels[1].Position.Y, 6);
    }

    [Fact]
    public void AStackedBarsLabel_StandsPadAboveItsEnd_NotItsFoot()
    {
        var transform = Axis(valueAxisRunsBackwards: false, horizontal: false);
        var series = new BarSeries(["a", "b"], [2.0, 3.0]) { ShowLabels = true, StackedBottoms = [1.0, 4.0] };

        var labels = LabelsOf(series, transform);

        Assert.Equal(transform.DataToPixel(0, 3).Y - Pad, labels[0].Position.Y, 6);
        Assert.Equal(transform.DataToPixel(1, 7).Y - Pad, labels[1].Position.Y, 6);
    }

    [Fact]
    public void ATallBar_ThatLeavesNoRoomAboveIt_KeepsItsLabelInsideThePlot()
    {
        var transform = new DataTransform(0, 10, 0, 5, Plot);
        var series = new BarSeries(["a", "b"], [5.0, 2.0]) { ShowLabels = true };

        var tall = LabelsOf(series, transform)[0];

        Assert.True(tall.Position.Y >= Plot.Y, $"the label of the tallest bar stands at y {tall.Position.Y}, above the plot area at {Plot.Y}");
    }
}
