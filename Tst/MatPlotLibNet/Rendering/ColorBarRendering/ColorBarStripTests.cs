// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>The body of a colour bar, drawn in one place for the bar of an axes and the bar of the figure: the gradient in
/// steps, a wedge at each extended end, the line at the start of each step when edges are drawn, and the frame.</summary>
public class ColorBarStripTests
{
    private static readonly Rect Bar = new(100, 40, 300, 20);
    private static readonly Rect Tall = new(500, 40, 20, 300);
    private static readonly ColorBarScale Continuous = new(0, 1, [], [], [], 0);
    private static readonly Color Ink = Colors.Black;
    private static readonly IColorMap Ramp = new LinearColorMap("ramp", [Colors.Red, Colors.Blue]);
    private static readonly IColorMap Capped = new LinearColorMap("capped", [Colors.Red, Colors.Blue])
    {
        UnderColor = Color.FromHex("#111111"),
        OverColor = Color.FromHex("#EEEEEE"),
    };

    private static List<(Rect Rect, Color? Fill, Color? Stroke)> Rectangles(RecordingRenderContext ctx) =>
        [.. ctx.Of<DrawRectangleCall>().Select(c => (c.Rect, c.Fill, c.Stroke))];

    private static RecordingRenderContext Drawn(ColorBarStrip strip, IColorMap map)
    {
        var ctx = new RecordingRenderContext();
        strip.Draw(ctx, map, Continuous, Ink);
        return ctx;
    }

    [Fact]
    public void AHorizontalStripWithNoWedge_IsAGradientOfFiftyStepsAndAFrame()
    {
        var strip = new ColorBarStrip(Bar, Vertical: false, ColorBarExtend.Neither, DrawEdges: false, Steps: 50);
        var rects = Rectangles(Drawn(strip, Ramp));

        Assert.Equal(Bar, strip.Gradient);
        Assert.Equal(51, rects.Count);
        Assert.Equal(new Rect(100, 40, 300.0 / 50 + 1, 20), rects[0].Rect);
        Assert.Equal(Ramp.GetColor(0), rects[0].Fill);
        Assert.Equal(new Rect(100 + 300.0 * 49 / 50, 40, 300.0 / 50 + 1, 20), rects[49].Rect);
        Assert.Equal(Ramp.GetColor(49 / 50.0), rects[49].Fill);
        Assert.Equal((Bar, (Color?)null, (Color?)Ink), rects[50]);
    }

    [Fact]
    public void AVerticalStripWithNoWedge_RunsItsGradientFromTheTopDown()
    {
        var strip = new ColorBarStrip(Tall, Vertical: true, ColorBarExtend.Neither, DrawEdges: false, Steps: 50);
        var rects = Rectangles(Drawn(strip, Ramp));

        Assert.Equal(Tall, strip.Gradient);
        Assert.Equal(new Rect(500, 40, 20, 300.0 / 50 + 1), rects[0].Rect);
        Assert.Equal(Ramp.GetColor(1.0), rects[0].Fill);
        Assert.Equal(new Rect(500, 40 + 300.0 * 49 / 50, 20, 300.0 / 50 + 1), rects[49].Rect);
        Assert.Equal(Ramp.GetColor(1.0 - 49 / 50.0), rects[49].Fill);
        Assert.Equal((Tall, (Color?)null, (Color?)Ink), rects[50]);
    }

    [Fact]
    public void AHorizontalStripExtendedBothWays_DrawsTheUnderWedgeFirstAndTheOverWedgeAfterTheSteps()
    {
        var strip = new ColorBarStrip(Bar, Vertical: false, ColorBarExtend.Both, DrawEdges: false, Steps: 50);
        var rects = Rectangles(Drawn(strip, Capped));

        Assert.Equal(new Rect(130, 40, 240, 20), strip.Gradient);
        Assert.Equal(53, rects.Count);
        Assert.Equal((new Rect(100, 40, 30, 20), (Color?)Color.FromHex("#111111"), (Color?)null), rects[0]);
        Assert.Equal(new Rect(130, 40, 240.0 / 50 + 1, 20), rects[1].Rect);
        Assert.Equal((new Rect(370, 40, 30, 20), (Color?)Color.FromHex("#EEEEEE"), (Color?)null), rects[51]);
        Assert.Equal(Bar, rects[52].Rect);
    }

    [Fact]
    public void AVerticalStripExtendedBothWays_DrawsTheOverWedgeOnTopFirst()
    {
        var strip = new ColorBarStrip(Tall, Vertical: true, ColorBarExtend.Both, DrawEdges: false, Steps: 50);
        var rects = Rectangles(Drawn(strip, Capped));

        Assert.Equal(new Rect(500, 70, 20, 240), strip.Gradient);
        Assert.Equal((new Rect(500, 40, 20, 30), (Color?)Color.FromHex("#EEEEEE"), (Color?)null), rects[0]);
        Assert.Equal((new Rect(500, 310, 20, 30), (Color?)Color.FromHex("#111111"), (Color?)null), rects[51]);
        Assert.Equal(Tall, rects[52].Rect);
    }

    /// <summary>A map with no colour of its own past its ends paints a wedge in the colour of the end it extends.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AWedgeOfAMapWithNoColourPastItsEnds_TakesTheColourOfThatEnd(bool vertical)
    {
        var strip = new ColorBarStrip(vertical ? Tall : Bar, vertical, ColorBarExtend.Both, DrawEdges: false, Steps: 50);
        var rects = Rectangles(Drawn(strip, Ramp));

        var under = vertical ? rects[51] : rects[0];
        var over = vertical ? rects[0] : rects[51];
        Assert.Equal(Ramp.GetColor(0.0), under.Fill);
        Assert.Equal(Ramp.GetColor(1.0), over.Fill);
    }

    [Theory]
    [InlineData(ColorBarExtend.Min, false, 130, 270)]
    [InlineData(ColorBarExtend.Max, false, 100, 270)]
    [InlineData(ColorBarExtend.Min, true, 40, 270)]
    [InlineData(ColorBarExtend.Max, true, 70, 270)]
    public void AStripExtendedOneWay_KeepsOneWedge(ColorBarExtend extend, bool vertical, double start, double length)
    {
        var strip = new ColorBarStrip(vertical ? Tall : Bar, vertical, extend, DrawEdges: false, Steps: 50);
        var gradient = strip.Gradient;

        Assert.Equal(start, vertical ? gradient.Y : gradient.X, 9);
        Assert.Equal(length, vertical ? gradient.Height : gradient.Width, 9);
        Assert.Equal(52, Drawn(strip, Ramp).Count<DrawRectangleCall>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AStripWithEdges_DrawsALineAcrossTheStartOfEveryStep(bool vertical)
    {
        var strip = new ColorBarStrip(vertical ? Tall : Bar, vertical, ColorBarExtend.Neither, DrawEdges: true, Steps: 50);
        var lines = Drawn(strip, Ramp).Of<DrawLineCall>();

        Assert.Equal(50, lines.Count);
        var second = lines[1];
        var (from, to) = (second.P1, second.P2);
        if (vertical)
            Assert.Equal((new Point(500, 46), new Point(520, 46)), (from, to));
        else
            Assert.Equal((new Point(106, 40), new Point(106, 60)), (from, to));
        Assert.Equal(Ink, second.Color);
        Assert.Equal(0.3, second.Thickness);
    }

    [Fact]
    public void AStripOfBands_DrawsEachStepInTheColourOfTheBandItsMiddleFallsIn()
    {
        var bands = new ColorBarScale(0, 1, [], [], [], 2);
        var ctx = new RecordingRenderContext();
        new ColorBarStrip(Bar, Vertical: false, ColorBarExtend.Neither, DrawEdges: false, Steps: 50).Draw(ctx, Ramp, bands, Ink);

        var fills = Rectangles(ctx).Take(50).Select(r => r.Fill).Distinct().ToArray();
        Assert.Equal([Ramp.GetColor(0.0), Ramp.GetColor(0.5)], fills);
    }

    /// <summary>The bar of an axes and the bar of the figure draw one body: the same rectangle gives the same strip, step for
    /// step.</summary>
    [Fact]
    public void TheAxesBarAndTheFigureBar_DrawTheSameStripForTheSameRectangle()
    {
        var plot = new Rect(80, 40, 600, 400);
        var cb = new ColorBar { Visible = true, Orientation = ColorBarOrientation.Horizontal };
        var axesCtx = new RecordingRenderContext();
        ColorBarRendererFactory.Create(cb, ColorMaps.Viridis, 0, 1, plot, axesCtx, Theme.Default).Render();
        var figureCtx = new RecordingRenderContext();
        var figure = new Figure { Width = 800, Height = 600, Theme = Theme.Default };
        new ChartRenderer().RenderFigureColorBar(figure, [plot], cb with { Min = 0, Max = 1 }, figureCtx);

        Assert.Equal(Rectangles(axesCtx), Rectangles(figureCtx));
    }
}
