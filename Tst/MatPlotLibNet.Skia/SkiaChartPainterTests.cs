// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A native control paints through one <see cref="SkiaChartPainter"/>: an unchanged subplot is recorded once (from its
/// second paint under one key) and replayed after that, which draws exactly the pixels a direct draw draws, at any scale of the
/// canvas, hatches and text included. A replaced recording is freed, a subplot the layout moved is drawn again, another figure
/// starts over, and a disposed painter frees what it kept and paints on from scratch. The asserts never hand a recording to xUnit's
/// formatter: a freed picture read there is a fatal access violation, not a failure. The layout a paint returns is what a native
/// control aims its host with, and the pointer is converted with the range and scale it drew.</summary>
public sealed class SkiaChartPainterTests
{
    private static Figure Wall() =>
        Plt.Create().WithSize(320, 200).WithTitle("Wall")
            .AddSubPlot(1, 2, 1, ax => ax.Plot([0.0, 1, 2], [1.0, 3, 2], s => s.Label = "line").WithLegend().SetXLabel("t"))
            .AddSubPlot(1, 2, 2, ax => ax.ShowGrid(false)
                .AddPatch(new Rectangle(new(1, 1), 4, 3) { FaceColor = Colors.Orange, Hatch = HatchPattern.Horizontal, HatchColor = Colors.Black }))
            .Build();

    /// <summary>What a direct draw of <paramref name="figure"/> puts on a fresh canvas at <paramref name="scale"/>.</summary>
    private static byte[] Direct(Figure figure, float scale)
    {
        using var surface = Surface(figure, scale);
        surface.Canvas.Clear(SKColors.White);
        new MatPlotLibNet.Rendering.ChartRenderer().Render(figure, new SkiaRenderContext(surface.Canvas));
        return Pixels(surface);
    }

    private static SKSurface Surface(Figure figure, float scale)
    {
        var surface = SKSurface.Create(new SKImageInfo((int)(figure.Width * scale), (int)(figure.Height * scale)));
        surface.Canvas.Scale(scale);
        return surface;
    }

    private static byte[] Pixels(SKSurface surface)
    {
        using var image = surface.Snapshot();
        using var pixels = image.PeekPixels();
        return pixels.GetPixelSpan<byte>().ToArray();
    }

    private static MatPlotLibNet.Rendering.Rect PlotAreaOf(Figure figure, int index)
    {
        using var surface = Surface(figure, 1);
        return new MatPlotLibNet.Rendering.ChartRenderer().ComputeLayout(figure, new SkiaRenderContext(surface.Canvas)).PlotAreas[index];
    }

    private static byte[] Painted(SkiaChartPainter painter, Figure figure, float scale)
    {
        using var surface = Surface(figure, scale);
        surface.Canvas.Clear(SKColors.White);
        _ = painter.Paint(surface.Canvas, figure, band: null);
        return Pixels(surface);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    public void EveryPaint_DrawsThePixelsADirectDrawDraws(float scale)
    {
        var figure = Wall();
        byte[] expected = Direct(figure, scale);
        using var painter = new SkiaChartPainter();

        for (int paint = 0; paint < 4; paint++)
            Assert.True(expected.AsSpan().SequenceEqual(Painted(painter, figure, scale)), $"paint {paint} at scale {scale}");
    }

    [Fact]
    public void AnUnchangedFigure_IsRecordedOnItsSecondPaint_AndReplayedAfter()
    {
        var figure = Wall();
        using var painter = new SkiaChartPainter();

        _ = Painted(painter, figure, 1);
        Assert.All(painter.Recordings!, Assert.Null);
        _ = Painted(painter, figure, 1);
        var recorded = painter.Recordings!;
        _ = Painted(painter, figure, 1);

        Assert.All(recorded, r => Assert.True(r is not null, "every subplot is recorded on the second paint"));
        Assert.True(recorded.SequenceEqual(painter.Recordings!, ReferenceEqualityComparer.Instance),
            "the third paint replays the recordings of the second");
    }

    [Fact]
    public void ARecordingAChangeReplaces_IsFreed_AndTheOtherIsKept()
    {
        var figure = Wall();
        using var painter = new SkiaChartPainter();
        _ = Painted(painter, figure, 1);
        _ = Painted(painter, figure, 1);
        var line = painter.Recordings![0]!;
        var patch = painter.Recordings![1]!;

        // A value inside the line's range (1 to 3): its ticks, and so the layout of both subplots, stay as they were.
        ((LineSeries)figure.SubPlots[0].Series[0]).YData[2] = 2.5;
        byte[] painted = Painted(painter, figure, 1);

        Assert.Equal(IntPtr.Zero, line.Handle);
        Assert.True(ReferenceEquals(patch, painter.Recordings![1]), "the unchanged subplot's recording is replayed, not recorded again");
        Assert.True(Direct(figure, 1).AsSpan().SequenceEqual(painted));
    }

    [Fact]
    public void AChangeThatMovesTheOtherSubplot_DrawsThatOneAgainToo()
    {
        var figure = Wall();
        using var painter = new SkiaChartPainter();
        _ = Painted(painter, figure, 1);
        _ = Painted(painter, figure, 1);
        var patch = painter.Recordings![1]!;
        var before = PlotAreaOf(figure, 1);

        // The line's top falls from 3 to 2.5: its ticks read 1.00, 1.25 ... (wider labels), so the layout moves both subplots.
        ((LineSeries)figure.SubPlots[0].Series[0]).YData[1] = 2.5;
        byte[] painted = Painted(painter, figure, 1);

        Assert.NotEqual(before, PlotAreaOf(figure, 1));
        Assert.Equal(IntPtr.Zero, patch.Handle);
        Assert.True(Direct(figure, 1).AsSpan().SequenceEqual(painted));
    }

    [Fact]
    public void ACanvasAtAnotherScale_IsDrawnAgain_NeverReplayedFromTheOther()
    {
        var figure = Wall();
        using var painter = new SkiaChartPainter();
        _ = Painted(painter, figure, 1);
        _ = Painted(painter, figure, 1);
        var atOne = painter.Recordings![0]!;

        byte[] atTwo = Painted(painter, figure, 2);

        Assert.True(Direct(figure, 2).AsSpan().SequenceEqual(atTwo));
        Assert.Equal(IntPtr.Zero, atOne.Handle);
    }

    [Fact]
    public void AnotherFigure_StartsOver_AndFreesWhatTheFirstKept()
    {
        var first = Wall();
        using var painter = new SkiaChartPainter();
        _ = Painted(painter, first, 1);
        _ = Painted(painter, first, 1);
        var kept = painter.Recordings![0]!;

        var second = Wall();
        _ = Painted(painter, second, 1);

        Assert.Equal(IntPtr.Zero, kept.Handle);
        Assert.All(painter.Recordings!, Assert.Null);
    }

    [Fact]
    public void ADisposedPainter_FreesWhatItKept_AndPaintsOnFromScratch()
    {
        var figure = Wall();
        var painter = new SkiaChartPainter();
        _ = Painted(painter, figure, 1);
        _ = Painted(painter, figure, 1);
        var kept = painter.Recordings![1]!;

        painter.Dispose();
        painter.Dispose();

        Assert.Equal(IntPtr.Zero, kept.Handle);
        Assert.Null(painter.Recordings);
        Assert.True(Direct(figure, 1).AsSpan().SequenceEqual(Painted(painter, figure, 1)));
        painter.Dispose();
    }

    [Fact]
    public void ThePaintsLayout_IsTheLayoutComputeLayoutGives_AndTheBandIsDrawnOverIt()
    {
        var figure = Wall();
        using var painter = new SkiaChartPainter();
        using var surface = Surface(figure, 1);
        surface.Canvas.Clear(SKColors.White);

        var layout = painter.Paint(surface.Canvas, figure, new BrushSelectState(10, 10, 60, 50, 0));

        var computed = new MatPlotLibNet.Rendering.ChartRenderer().ComputeLayout(figure, new SkiaRenderContext(surface.Canvas));
        Assert.Equal(computed.PlotAreas, layout.PlotAreas);
        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);
        var inside = bitmap.GetPixel(30, 30);
        Assert.True(inside.Blue > inside.Red, $"the band tints what it covers blue, found {inside}");
    }

    // ── A paint's layout aims a native control's host ─────────────────────────────────────────────
    //
    // Every native control (WPF, Avalonia, Uno) paints through the painter and hands its layout to its InteractionHost, which
    // converts the pointer with the range and the scale the renderer drew: a wheel on an autoscaled chart zooms into the data (it
    // used to zoom into 0..1), hover on a log axis names the point under the pointer, and a drag on a log axis moves it by decades.

    /// <summary>A host aimed by one paint of <paramref name="figure"/>, and the plot area that paint drew.</summary>
    private static InteractionHost AimedByAPaint(Figure figure, out MatPlotLibNet.Rendering.Rect area)
    {
        using var surface = Surface(figure, 1);
        using var painter = new SkiaChartPainter();
        var layout = ChartLayout.Create(figure, painter.Paint(surface.Canvas, figure, band: null));
        area = layout.GetPlotArea(0);
        var host = new InteractionHost();
        host.Update(figure, layout);
        return host;
    }

    /// <summary>Log X from 1 to 1000 (three decades), Y from 0 to 5; the points sit at 1, 10, 100, 1000.</summary>
    private static Figure LogFigure() => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 10, 100, 1000], [1.0, 2, 3, 4])
            .SetXScale(AxisScale.Log).SetXLim(1, 1000).SetYLim(0, 5))
        .Build();

    [Fact]
    public void AWheelOnAnAutoscaledChart_ZoomsIntoTheData()
    {
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]).Build();
        var host = AimedByAPaint(figure, out var area);

        host.HandleScroll(new ScrollInputArgs(area.X + area.Width / 2, area.Y + area.Height / 2, 0, -1, ModifierKeys.None));

        var x = figure.SubPlots[0].XAxis;
        var y = figure.SubPlots[0].YAxis;
        Assert.InRange(x.Min!.Value, 1.0, 2.0);
        Assert.InRange(x.Max!.Value, 2.0, 3.0);
        Assert.InRange(y.Min!.Value, 8.0, 25.0);
        Assert.InRange(y.Max!.Value, 25.0, 42.0);
    }

    [Fact]
    public void HoverOnALogAxis_NamesThePointUnderThePointer()
    {
        var host = AimedByAPaint(LogFigure(), out var area);

        // x = 10 is one decade of three from the left edge; y = 2 is two fifths up.
        host.HandlePointerMoved(new PointerInputArgs(area.X + area.Width / 3, area.Y + area.Height * 3 / 5,
            PointerButton.None, ModifierKeys.None));

        Assert.NotNull(host.ActiveTooltip);
        Assert.Equal(1, host.ActiveTooltip!.PointIndex);
    }

    [Fact]
    public void ADragOnALogAxis_MovesTheRangeByDecades()
    {
        var figure = LogFigure();
        var host = AimedByAPaint(figure, out var area);
        double y = area.Y + area.Height * 0.9;

        host.HandlePointerPressed(new PointerInputArgs(area.X + area.Width * 2 / 3, y, PointerButton.Left, ModifierKeys.None));
        host.HandlePointerMoved(new PointerInputArgs(area.X + area.Width / 3, y, PointerButton.Left, ModifierKeys.None));
        host.HandlePointerReleased(new PointerInputArgs(area.X + area.Width / 3, y, PointerButton.Left, ModifierKeys.None));

        // A third of the width is one decade: 1..1000 becomes 10..10000.
        Assert.Equal(10.0, figure.SubPlots[0].XAxis.Min!.Value, 9);
        Assert.Equal(10000.0, figure.SubPlots[0].XAxis.Max!.Value, 6);
    }
}
