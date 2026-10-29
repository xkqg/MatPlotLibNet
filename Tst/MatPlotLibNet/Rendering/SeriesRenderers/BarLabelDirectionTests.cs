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
        return [.. ctx.OfKind("DrawText").Select(c =>
        {
            dynamic args = c.Args!;
            return new Drawn((Point)args.position, (TextAlignment)args.alignment);
        })];
    }

    private static double FontSize => ThemedFontProvider.DataTextFont(Theme.Default).Size;

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

        Assert.Equal(expected, Axis(backwards, horizontal).ValueRunsBackwards(orientation));
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

    [Fact]
    public void AFallingBar_OnAnAxisThatRunsUp_HasItsLabelBelowItsEnd()
    {
        var transform = Axis(valueAxisRunsBackwards: false, horizontal: false);

        var label = LabelsOf(Bars(BarOrientation.Vertical), transform)[1];

        // Below the end: the pad, then the height the letters rise above their baseline.
        Assert.Equal(transform.DataToPixel(0, -2).Y + Pad + FontSize, label.Position.Y, 6);
    }

    [Fact]
    public void ARisingBar_OnAnAxisThatRunsDown_HasItsLabelBelowItsEnd()
    {
        var transform = Axis(valueAxisRunsBackwards: true, horizontal: false);

        var label = LabelsOf(Bars(BarOrientation.Vertical), transform)[0];

        Assert.Equal(transform.DataToPixel(0, 3).Y + Pad + FontSize, label.Position.Y, 6);
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

    [Fact]
    public void ARisingHorizontalBar_OnAnAxisThatRunsRight_HasItsLabelRightOfItsEnd()
    {
        var transform = Axis(valueAxisRunsBackwards: false, horizontal: true);

        var label = LabelsOf(Bars(BarOrientation.Horizontal), transform)[0];

        Assert.Equal(transform.DataToPixel(3, 0).X + Pad, label.Position.X, 6);
        Assert.Equal(TextAlignment.Left, label.Alignment);
    }

    [Fact]
    public void AFallingHorizontalBar_OnAnAxisThatRunsRight_HasItsLabelLeftOfItsEnd_AlignedToIt()
    {
        var transform = Axis(valueAxisRunsBackwards: false, horizontal: true);

        var label = LabelsOf(Bars(BarOrientation.Horizontal), transform)[1];

        Assert.Equal(transform.DataToPixel(-2, 0).X - Pad, label.Position.X, 6);
        Assert.Equal(TextAlignment.Right, label.Alignment);
    }

    [Fact]
    public void ARisingHorizontalBar_OnAnAxisThatRunsLeft_HasItsLabelLeftOfItsEnd()
    {
        var transform = Axis(valueAxisRunsBackwards: true, horizontal: true);

        var label = LabelsOf(Bars(BarOrientation.Horizontal), transform)[0];

        Assert.Equal(transform.DataToPixel(3, 0).X - Pad, label.Position.X, 6);
        Assert.Equal(TextAlignment.Right, label.Alignment);
    }

    [Fact]
    public void AFallingHorizontalBar_OnAnAxisThatRunsLeft_HasItsLabelRightOfItsEnd()
    {
        var transform = Axis(valueAxisRunsBackwards: true, horizontal: true);

        var label = LabelsOf(Bars(BarOrientation.Horizontal), transform)[1];

        Assert.Equal(transform.DataToPixel(-2, 0).X + Pad, label.Position.X, 6);
        Assert.Equal(TextAlignment.Left, label.Alignment);
    }

    // ---- what does not change --------------------------------------------------------------------------------------

    [Fact]
    public void OnALogAxis_ARisingBarsLabelStaysPadAboveItsEnd()
    {
        var transform = new DataTransform(0, 10, 0, 2, Plot, null, null, 0, 10, 1, 100, AxisScale.Linear, AxisScale.Log, 1, 1);
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
