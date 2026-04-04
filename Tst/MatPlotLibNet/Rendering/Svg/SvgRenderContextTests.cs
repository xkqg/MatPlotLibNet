// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Svg;

public class SvgRenderContextTests
{
    [Fact]
    public void DrawLine_ProducesSvgLineElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawLine(new Point(10, 20), new Point(30, 40),
            Color.Red, 2.0, LineStyle.Solid);

        string svg = ctx.GetOutput();
        Assert.Contains("<line", svg);
        Assert.Contains("x1=", svg);
        Assert.Contains("x2=", svg);
        Assert.Contains("#FF0000", svg);
    }

    [Fact]
    public void DrawRectangle_ProducesSvgRectElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawRectangle(new Rect(10, 20, 100, 50),
            fill: Color.Blue, stroke: null, strokeThickness: 0);

        string svg = ctx.GetOutput();
        Assert.Contains("<rect", svg);
        Assert.Contains("#0000FF", svg);
    }

    [Fact]
    public void DrawText_ProducesSvgTextElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText("Hello", new Point(50, 50), new Font(), TextAlignment.Center);

        string svg = ctx.GetOutput();
        Assert.Contains("<text", svg);
        Assert.Contains("Hello", svg);
    }

    [Fact]
    public void DrawCircle_ProducesSvgCircleElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawCircle(new Point(50, 50), 10, Color.Green, null, 0);

        string svg = ctx.GetOutput();
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void DrawLines_ProducesSvgPolylineElement()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawLines([new Point(0, 0), new Point(10, 10), new Point(20, 5)],
            Color.Blue, 1.5, LineStyle.Solid);

        string svg = ctx.GetOutput();
        Assert.Contains("<polyline", svg);
    }

    [Fact]
    public void DashedLine_HasStrokeDasharray()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawLine(new Point(0, 0), new Point(10, 10),
            Color.Black, 1, LineStyle.Dashed);

        string svg = ctx.GetOutput();
        Assert.Contains("stroke-dasharray", svg);
    }
}
