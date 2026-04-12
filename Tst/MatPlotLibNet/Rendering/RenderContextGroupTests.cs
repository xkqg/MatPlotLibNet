// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that BeginGroup/EndGroup are available on IRenderContext
/// and that type casts to SvgRenderContext are no longer needed.</summary>
public class RenderContextGroupTests
{
    /// <summary>Verifies that IRenderContext has BeginGroup with default no-op.</summary>
    [Fact]
    public void IRenderContext_BeginGroup_DefaultNoOp()
    {
        IRenderContext ctx = new NullRenderContext();
        ctx.BeginGroup("test"); // should not throw
        ctx.EndGroup();
    }

    /// <summary>Verifies that SvgRenderContext.BeginGroup emits a g element.</summary>
    [Fact]
    public void SvgRenderContext_BeginGroup_EmitsGElement()
    {
        var ctx = new SvgRenderContext();
        ctx.BeginGroup("legend");
        ctx.EndGroup();
        string output = ctx.GetOutput();
        Assert.Contains("<g class=\"legend\">", output);
        Assert.Contains("</g>", output);
    }

    /// <summary>Verifies that spine rendering uses BeginGroup without type casts.</summary>
    [Fact]
    public void DrawSpineLine_NoTypeCast_StillEmitsGroup()
    {
        var fig = new Figure();
        var ax = fig.AddSubPlot();
        ax.Plot([1.0, 2.0], [3.0, 4.0]);

        string svg = fig.ToSvg();
        Assert.Contains("class=\"spine\"", svg);
    }

    /// <summary>Verifies that legend rendering emits a group element.</summary>
    [Fact]
    public void RenderLegend_StillEmitsGroup()
    {
        var fig = new Figure();
        var ax = fig.AddSubPlot();
        ax.Plot([1.0, 2.0], [3.0, 4.0]).Label = "Series 1";

        string svg = fig.ToSvg();
        Assert.Contains("class=\"legend\"", svg);
    }

    /// <summary>A minimal IRenderContext that does nothing (verifies default interface methods work).</summary>
    private sealed class NullRenderContext : IRenderContext
    {
        public void DrawLine(Point p1, Point p2, Color color, double thickness, LineStyle style) { }
        public void DrawLines(IReadOnlyList<Point> points, Color color, double thickness, LineStyle style) { }
        public void DrawPolygon(IReadOnlyList<Point> points, Color? fill, Color? stroke, double strokeThickness) { }
        public void DrawCircle(Point center, double radius, Color? fill, Color? stroke, double strokeThickness) { }
        public void DrawRectangle(Rect rect, Color? fill, Color? stroke, double strokeThickness) { }
        public void DrawEllipse(Rect bounds, Color? fill, Color? stroke, double strokeThickness) { }
        public void DrawText(string text, Point position, Font font, TextAlignment alignment) { }
        public void DrawPath(IReadOnlyList<PathSegment> segments, Color? fill, Color? stroke, double strokeThickness) { }
        public void PushClip(Rect clipRect) { }
        public void PopClip() { }
        public Size MeasureText(string text, Font font) => new(50, 12);
        public void SetOpacity(double opacity) { }
    }
}
