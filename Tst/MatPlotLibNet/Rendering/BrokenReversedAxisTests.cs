// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A break on an axis drawn high to low (a limit set high to low, or an inverted axis) is cut out of it as it is on
/// an axis drawn low to high: both ends of the break stand at one place and the range closes up. The break map used to clip
/// each break to [first limit, second limit], which on a reversed range is empty, so every gap was 0 and the hole was drawn
/// at its full width.</summary>
public class BrokenReversedAxisTests
{
    private static readonly Rect Plot = new(80, 60, 640, 480);

    private static DataTransform Drawn(bool onX, AxisScale scale, double first, double second, bool inverted, double from, double to)
    {
        var axes = new Axes();
        var axis = onX ? axes.XAxis : axes.YAxis;
        axis.Scale = scale;
        axis.Min = first;
        axis.Max = second;
        axis.Inverted = inverted;
        var other = onX ? axes.YAxis : axes.XAxis;
        other.Min = 0;
        other.Max = 10;
        axes.Plot([first, second], [first, second]);
        if (onX) axes.AddXBreak(from, to);
        else axes.AddYBreak(from, to);

        var renderer = new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default);
        renderer.Render();
        return renderer.DrawnTransform!;
    }

    private static double Along(DataTransform t, bool onX, double value) =>
        onX ? t.DataToPixel(value, 5).X : t.DataToPixel(5, value).Y;

    [Theory]
    [InlineData(true, AxisScale.Linear, 150.0, 0.0, false, 30.0, 80.0)]
    [InlineData(false, AxisScale.Linear, 150.0, 0.0, false, 30.0, 80.0)]
    [InlineData(true, AxisScale.Linear, 0.0, 150.0, true, 30.0, 80.0)]
    [InlineData(false, AxisScale.Linear, 0.0, 150.0, true, 30.0, 80.0)]
    [InlineData(true, AxisScale.Log, 1e6, 1.0, false, 100.0, 1000.0)]
    [InlineData(false, AxisScale.Log, 1.0, 1e6, true, 100.0, 1000.0)]
    [InlineData(true, AxisScale.SymLog, 1e4, -1e4, false, 100.0, 1000.0)]
    [InlineData(false, AxisScale.SymLog, -1e4, 1e4, true, 100.0, 1000.0)]
    public void ABreakOnAReversedAxis_IsCutOut_BothItsEndsStandingAtOnePlace(
        bool onX, AxisScale scale, double first, double second, bool inverted, double from, double to)
    {
        var t = Drawn(onX, scale, first, second, inverted, from, to);

        Assert.Equal(Along(t, onX, from), Along(t, onX, to), 1e-9);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ABreakOnAReversedAxis_LeavesTheRangeOnEitherSideItsWidth(bool onX)
    {
        // 150..0 less the break (30, 80) is 100 drawn units over the plot: 0..30 takes 30 of them, 80..150 the other 70.
        var t = Drawn(onX, AxisScale.Linear, 150, 0, inverted: false, 30, 80);
        double length = onX ? Plot.Width : Plot.Height;

        double highEnd = Along(t, onX, 150), lowEnd = Along(t, onX, 0), cut = Along(t, onX, 30);
        Assert.Equal(length, Math.Abs(lowEnd - highEnd), 1e-9);
        Assert.Equal(length * 0.3, Math.Abs(lowEnd - cut), 1e-9);
        Assert.Equal(length * 0.7, Math.Abs(cut - highEnd), 1e-9);
    }

    [Fact]
    public void APixelOnAReversedAxis_PastTheBreak_ReadsBackTheValueDrawnThere()
    {
        var t = Drawn(onX: true, AxisScale.Linear, 150, 0, inverted: false, 30, 80);

        foreach (double v in new[] { 0.0, 10, 29, 81, 120, 150 })
            Assert.Equal(v, t.PixelToData(t.DataToPixel(v, 5)).X, 1e-9);
    }
}
