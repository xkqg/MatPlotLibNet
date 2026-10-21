// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>The native controls (Avalonia, WPF, Uno) build a <see cref="ChartLayout"/> after every paint and convert the
/// pointer with it: hover, the data cursor, brush and span select, the rectangle zoom, the wheel, pan and Home. The
/// layout converts with the range and the scale the renderer drew (the plot area, the range after autoscale, margins and
/// limits, and the log or symlog scale), so a pointer on a drawn point reads that point's value. The oracle is the
/// transform the axes renderer itself sets when it draws (<see cref="AxesRenderer.DrawnTransform"/>). Round trips apply
/// what the controller emits to the figure and lay it out again, the way a control repaints.</summary>
public class ChartLayoutDrawnViewTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [10.0, 40.0, 20.0];

    private static Figure Single(double[] x, double[] y, Func<AxesBuilder, AxesBuilder>? configure = null) =>
        Plt.Create().WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => (configure ?? (a => a))(ax.Plot(x, y, s => s.Label = "data")))
            .Build();

    /// <summary>The layout a native control builds after it paints: the renderer's layout pass, then
    /// <see cref="ChartLayout.Create(Figure, LayoutResult)"/>.</summary>
    private static ChartLayout LayoutOf(Figure figure) =>
        ChartLayout.Create(figure, ChartServices.Renderer.ComputeLayout(figure, new SvgRenderContext()));

    /// <summary>The transform the renderer draws axes <paramref name="index"/> with, in the plot area the layout holds.</summary>
    private static DataTransform Drawn(Figure figure, int index)
    {
        var area = LayoutOf(figure).GetPlotArea(index);
        var renderer = AxesRenderer.Create(figure.SubPlots[index], area, new SvgRenderContext(), figure.Theme);
        renderer.Render();
        return renderer.DrawnTransform!;
    }

    private static void AssertClose(double expected, double actual, string what, double relative = 1e-9)
    {
        double tolerance = Math.Max(Math.Abs(expected), 1e-6) * relative;
        Assert.True(Math.Abs(actual - expected) <= tolerance,
            $"{what}: expected {expected.ToString("R", CultureInfo.InvariantCulture)}, got {actual.ToString("R", CultureInfo.InvariantCulture)}");
    }

    private static void AssertReads(ChartLayout layout, int axes, Point pixel, double x, double y)
    {
        var read = layout.PixelToData(pixel.X, pixel.Y, axes);
        Assert.NotNull(read);
        AssertClose(x, read!.Value.DataX, "x");
        AssertClose(y, read.Value.DataY, "y");
    }

    // ── pixel -> data ────────────────────────────────────────────────────────────

    [Fact]
    public void AutoscaledAxes_ThePixelOfADrawnPoint_ReadsThatPoint()
    {
        var figure = Single(X, Y);
        var pixel = Drawn(figure, 0).DataToPixel(2, 40);

        AssertReads(LayoutOf(figure), 0, pixel, 2, 40);
    }

    [Fact]
    public void AutoscaledAxes_TheDataRange_IsTheRangeDrawn()
    {
        var figure = Single(X, Y);
        var drawn = Drawn(figure, 0);
        var area = drawn.PlotBounds;
        var lowLeft = drawn.PixelToData(new Point(area.X, area.Y + area.Height));
        var highRight = drawn.PixelToData(new Point(area.X + area.Width, area.Y));

        var range = LayoutOf(figure).GetDataRange(0);

        AssertClose(lowLeft.X, range.XMin, "xMin");
        AssertClose(highRight.X, range.XMax, "xMax");
        AssertClose(lowLeft.Y, range.YMin, "yMin");
        AssertClose(highRight.Y, range.YMax, "yMax");
    }

    [Fact]
    public void ExplicitLimits_ThePixelOfAPoint_ReadsThatPoint()
    {
        var figure = Single(X, Y, ax => ax.SetXLim(1.5, 2.5).SetYLim(15, 35));
        var pixel = Drawn(figure, 0).DataToPixel(2, 30);

        AssertReads(LayoutOf(figure), 0, pixel, 2, 30);
    }

    [Fact]
    public void LogAxis_ThePixelOfADrawnPoint_ReadsThatPoint_NotItsLogarithm()
    {
        var figure = Single([1.0, 10, 100, 1000], [1.0, 2, 3, 4], ax => ax.SetXScale(AxisScale.Log));
        var pixel = Drawn(figure, 0).DataToPixel(100, 3);

        AssertReads(LayoutOf(figure), 0, pixel, 100, 3);
    }

    [Fact]
    public void SymlogAxis_ThePixelOfADrawnPoint_ReadsThatPoint()
    {
        var figure = Single([1.0, 2, 3, 4], [-400.0, -1, 1, 300], ax => ax.WithSymlogYScale(2));
        var drawn = Drawn(figure, 0);

        AssertReads(LayoutOf(figure), 0, drawn.DataToPixel(1, -400), 1, -400);
        AssertReads(LayoutOf(figure), 0, drawn.DataToPixel(4, 300), 4, 300);
    }

    [Fact]
    public void TwoAxes_EachPixelReadsTheAxesItIsOver()
    {
        var figure = Plt.Create().WithSize(800, 400)
            .AddSubPlot(1, 2, 1, ax => ax.Plot(X, Y))
            .AddSubPlot(1, 2, 2, ax => ax.Plot([100.0, 200, 300], [5.0, 7, 6]).SetYScale(AxisScale.Log))
            .Build();
        var layout = LayoutOf(figure);
        var right = Drawn(figure, 1).DataToPixel(200, 7);
        var left = Drawn(figure, 0).DataToPixel(3, 20);

        Assert.Equal(1, layout.HitTestAxes(right.X, right.Y));
        AssertReads(layout, 1, right, 200, 7);
        Assert.Equal(0, layout.HitTestAxes(left.X, left.Y));
        AssertReads(layout, 0, left, 3, 20);
    }

    [Theory]
    [InlineData(false, 0.0, 1.0, 0.0, 1.0)]
    [InlineData(true, -4.0, 6.0, 10.0, 30.0)]
    public void PolarAxes_DrawNoCartesianRange_SoTheLayoutMapsTheirLimitsOrZeroToOne(
        bool withLimits, double xMin, double xMax, double yMin, double yMax)
    {
        var figure = Plt.Create().PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0]).Build();
        var axes = figure.SubPlots[0];
        Assert.Equal(CoordinateSystem.Polar, axes.CoordinateSystem);
        if (withLimits)
        {
            axes.XAxis.Min = xMin; axes.XAxis.Max = xMax;
            axes.YAxis.Min = yMin; axes.YAxis.Max = yMax;
        }
        var layout = ChartLayout.Create(figure, [new Rect(100, 50, 400, 300)]);

        var range = layout.GetDataRange(0);
        AssertClose(xMin, range.XMin, "xMin");
        AssertClose(xMax, range.XMax, "xMax");
        AssertClose(yMin, range.YMin, "yMin");
        AssertClose(yMax, range.YMax, "yMax");
        AssertReads(layout, 0, new Point(300, 200), (xMin + xMax) / 2, (yMin + yMax) / 2);
    }

    // ── what the modifiers do with it ────────────────────────────────────────────

    [Fact]
    public void LogAxis_HoverOnADrawnPoint_FindsThatPoint()
    {
        var figure = Single([1.0, 10, 100, 1000], [1.0, 2, 3, 4], ax => ax.SetXScale(AxisScale.Log));
        var pixel = Drawn(figure, 0).DataToPixel(100, 3);
        var controller = InteractionController.CreateLocal(figure, LayoutOf(figure));

        controller.HandlePointerMoved(new PointerInputArgs(pixel.X, pixel.Y, PointerButton.None, ModifierKeys.None));

        Assert.NotNull(controller.ActiveTooltip);
        Assert.Equal(2, controller.ActiveTooltip!.PointIndex);
    }

    [Fact]
    public void LogAxis_NearestPoint_MeasuresTheDistanceOnScreen()
    {
        var figure = Single([1.0, 10, 100, 1000], [1.0, 2, 3, 4], ax => ax.SetXScale(AxisScale.Log));
        var drawn = Drawn(figure, 0);
        var at = drawn.DataToPixel(100, 3);
        var nearby = drawn.PixelToData(new Point(at.X + 6, at.Y + 8));

        var hit = NearestPointFinder.Find(figure, 0, nearby.X, nearby.Y, LayoutOf(figure));

        Assert.NotNull(hit);
        Assert.Equal(2, hit!.PointIndex);
        AssertClose(10, hit.PixelDistance, "pixel distance", 1e-6);
    }

    [Fact]
    public void AutoscaledAxes_TheCrosshair_ReadsTheDrawnPoint()
    {
        var figure = Single(X, Y);
        var pixel = Drawn(figure, 0).DataToPixel(2, 40);
        var crosshair = new CrosshairModifier(LayoutOf(figure));

        crosshair.UpdatePosition(pixel.X, pixel.Y);

        var state = crosshair.ActiveCrosshair!.Value;
        AssertClose(2, state.DataX, "x");
        AssertClose(40, state.DataY, "y");
    }

    [Fact]
    public void AutoscaledAxes_HomeAfterAZoom_ReturnsToTheViewTheLayoutWasMadeFor()
    {
        var figure = Single(X, Y);
        var first = LayoutOf(figure).GetDataRange(0);
        var controller = InteractionController.CreateLocal(figure, LayoutOf(figure));
        var pixel = Drawn(figure, 0).DataToPixel(2, 40);

        controller.HandleScroll(new ScrollInputArgs(pixel.X, pixel.Y, 0, -1, ModifierKeys.None));
        controller.HandleKeyDown(new KeyInputArgs("Home", ModifierKeys.None));

        var back = LayoutOf(figure).GetDataRange(0);
        AssertClose(first.XMin, back.XMin, "xMin");
        AssertClose(first.XMax, back.XMax, "xMax");
        AssertClose(first.YMin, back.YMin, "yMin");
        AssertClose(first.YMax, back.YMax, "yMax");
    }

    // ── round trips: apply what the controller emits, lay out again ─────────────

    [Fact]
    public void AutoscaledAxes_AWheelZoom_KeepsThePointUnderThePointer_AndNarrowsTheView()
    {
        var figure = Single(X, Y);
        var before = LayoutOf(figure).GetDataRange(0);
        var pixel = Drawn(figure, 0).DataToPixel(2, 40);
        var controller = InteractionController.CreateLocal(figure, LayoutOf(figure));

        controller.HandleScroll(new ScrollInputArgs(pixel.X, pixel.Y, 0, -1, ModifierKeys.None));

        var after = Drawn(figure, 0).DataToPixel(2, 40);
        AssertClose(pixel.X, after.X, "pixel x", 1e-6);
        AssertClose(pixel.Y, after.Y, "pixel y", 1e-6);
        var range = LayoutOf(figure).GetDataRange(0);
        AssertClose(0.85 * (before.XMax - before.XMin), range.XMax - range.XMin, "x span");
        AssertClose(0.85 * (before.YMax - before.YMin), range.YMax - range.YMin, "y span");
    }

    [Fact]
    public void TwoWheelNotchesBeforeTheRepaint_ZoomTwice()
    {
        // Wheel notches can arrive faster than the control repaints; the second one zooms on from the first.
        var figure = Single(X, Y);
        var before = LayoutOf(figure).GetDataRange(0);
        var pixel = Drawn(figure, 0).DataToPixel(2, 40);
        var controller = InteractionController.CreateLocal(figure, LayoutOf(figure));

        controller.HandleScroll(new ScrollInputArgs(pixel.X, pixel.Y, 0, -1, ModifierKeys.None));
        controller.HandleScroll(new ScrollInputArgs(pixel.X, pixel.Y, 0, -1, ModifierKeys.None));

        var range = LayoutOf(figure).GetDataRange(0);
        AssertClose(0.85 * 0.85 * (before.XMax - before.XMin), range.XMax - range.XMin, "x span");
        AssertClose(pixel.X, Drawn(figure, 0).DataToPixel(2, 40).X, "pixel x", 1e-6);
    }

    [Fact]
    public void LogAxis_AWheelZoom_NarrowsEveryDecadeAlike()
    {
        var figure = Single([1.0, 10, 100, 1000], [1.0, 2, 3, 4], ax => ax.SetXScale(AxisScale.Log));
        var before = LayoutOf(figure).GetDataRange(0);
        var pixel = Drawn(figure, 0).DataToPixel(100, 3);
        var controller = InteractionController.CreateLocal(figure, LayoutOf(figure));

        controller.HandleScroll(new ScrollInputArgs(pixel.X, pixel.Y, 0, -1, ModifierKeys.None));

        var after = Drawn(figure, 0).DataToPixel(100, 3);
        AssertClose(pixel.X, after.X, "pixel x", 1e-6);
        var range = LayoutOf(figure).GetDataRange(0);
        AssertClose(0.85 * Math.Log10(before.XMax / before.XMin), Math.Log10(range.XMax / range.XMin), "decades");
    }

    [Fact]
    public void AutoscaledAxes_ADrag_MovesThePointWithThePointer()
    {
        var figure = Single(X, Y);
        var from = Drawn(figure, 0).DataToPixel(2, 40);
        var to = new Point(from.X + 30, from.Y + 20);
        var controller = InteractionController.CreateLocal(figure, LayoutOf(figure));
        // Grabbed 25 px below the point (the highest one, near the top edge), on empty plot: not the data cursor's
        // point, not the legend.
        var grab = new Point(from.X, from.Y + 25);
        Assert.Null(LayoutOf(figure).HitTestLegendItem(grab.X, grab.Y, 0));

        controller.HandlePointerPressed(new PointerInputArgs(grab.X, grab.Y, PointerButton.Left, ModifierKeys.None));
        controller.HandlePointerMoved(new PointerInputArgs(to.X, to.Y + 25, PointerButton.Left, ModifierKeys.None));
        controller.HandlePointerReleased(new PointerInputArgs(to.X, to.Y + 25, PointerButton.Left, ModifierKeys.None));

        var moved = Drawn(figure, 0).DataToPixel(2, 40);
        AssertClose(to.X, moved.X, "pixel x", 1e-6);
        AssertClose(to.Y, moved.Y, "pixel y", 1e-6);
    }

    [Fact]
    public void LogAxis_ADrag_MovesEveryPointWithThePointer()
    {
        var figure = Single([1.0, 10, 100, 1000], [1.0, 2, 3, 4], ax => ax.SetXScale(AxisScale.Log));
        var drawn = Drawn(figure, 0);
        var a = drawn.DataToPixel(10, 2);
        var b = drawn.DataToPixel(1000, 4);
        var controller = InteractionController.CreateLocal(figure, LayoutOf(figure));
        var grab = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2 + 40);

        controller.HandlePointerPressed(new PointerInputArgs(grab.X, grab.Y, PointerButton.Left, ModifierKeys.None));
        controller.HandlePointerMoved(new PointerInputArgs(grab.X - 50, grab.Y, PointerButton.Left, ModifierKeys.None));
        controller.HandlePointerReleased(new PointerInputArgs(grab.X - 50, grab.Y, PointerButton.Left, ModifierKeys.None));

        var redrawn = Drawn(figure, 0);
        AssertClose(a.X - 50, redrawn.DataToPixel(10, 2).X, "point at 10", 1e-6);
        AssertClose(b.X - 50, redrawn.DataToPixel(1000, 4).X, "point at 1000", 1e-6);
    }

    [Fact]
    public void ADragOfTwoMoves_BeforeTheControlRepaints_FollowsThePointer()
    {
        // A control repaints when it gets round to it; the second move can arrive while the layout of the first paint
        // is still in hand. The point grabbed must still end under the pointer, not twice as far.
        var figure = Single(X, Y, ax => ax.SetXLim(0, 4).SetYLim(0, 50));
        var from = Drawn(figure, 0).DataToPixel(2, 40);
        var controller = InteractionController.CreateLocal(figure, LayoutOf(figure));

        controller.HandlePointerPressed(new PointerInputArgs(from.X, from.Y - 25, PointerButton.Left, ModifierKeys.None));
        controller.HandlePointerMoved(new PointerInputArgs(from.X + 20, from.Y - 25, PointerButton.Left, ModifierKeys.None));
        controller.HandlePointerMoved(new PointerInputArgs(from.X + 40, from.Y - 25, PointerButton.Left, ModifierKeys.None));
        controller.HandlePointerReleased(new PointerInputArgs(from.X + 40, from.Y - 25, PointerButton.Left, ModifierKeys.None));

        AssertClose(from.X + 40, Drawn(figure, 0).DataToPixel(2, 40).X, "pixel x", 1e-6);
    }
}
