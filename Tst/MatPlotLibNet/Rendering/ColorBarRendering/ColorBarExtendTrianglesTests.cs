// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>The extend of a colour bar under the matplotlib themes, as matplotlib's. Printed with matplotlib 3.11.2 for
/// <c>fig.colorbar(im, ax=ax, extend='both')</c> on a 6 x 4 inch figure at 100 dpi, in both styles: <c>cb.extendfrac</c> is
/// <c>None</c> and <c>cb.extendrect</c> <c>False</c>, so each end is a triangle 0.05 of the interior long
/// (<c>_proportional_y()[1] = [0.05, 0.05]</c>, "extend lengths are fraction of the *inner* part of colorbar"); the interior is the
/// bar's length shrunk by <c>1 / (1 + 0.05 + 0.05)</c> (<c>_ColorbarAxesLocator</c>: the original position 0.77 of the figure high,
/// the inner 0.70, 280 px) and the triangles stand outside it (<c>_extend_patches</c>: <c>[[0, 0], [0.5, -0.05], [1, 0]]</c> and
/// <c>[[0, 1], [0.5, 1.05], [1, 1]]</c> in the inner axes' fraction), and the outline runs round the interior and both tips
/// (<c>[[0, 0], [0.5, -0.05], [1, 0], [1, 1], [0.5, 1.05], [0, 1], [0, 0]]</c>, closed where it began; the library closes the polygon
/// without repeating the first point). A horizontal bar turns them
/// (<c>extend='min'</c>: <c>[[0, 0], [-0.05, 0.5], [0, 1]]</c>). The thickness stays the length over the aspect (15.4 px for the
/// whole 308 px). The library's own themes keep a rectangle a tenth of the whole bar long, inside it.</summary>
public sealed class ColorBarExtendTrianglesTests
{
    private static readonly double[,] Matrix = { { 0, 1, 2, 3 }, { 4, 5, 6, 7 }, { 8, 9, 10, 11 } };

    private static List<CallRecord> BarCalls(Theme theme, ColorBarExtend extend, ColorBarOrientation orientation = ColorBarOrientation.Vertical)
    {
        var figure = Plt.Create().WithTheme(theme).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(Matrix).WithColorBar(cb => cb with { Extend = extend, Orientation = orientation })).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        int start = ctx.Calls.FindIndex(c => c is BeginGroupCall { CssClass: "colorbar" });
        int end = ctx.Calls.FindIndex(start, c => c is EndGroupCall);
        return ctx.Calls.GetRange(start + 1, end - start - 1);
    }

    private static List<Point[]> FilledTriangles(List<CallRecord> bar) =>
        [.. bar.OfType<DrawPolygonCall>().Where(k => k.Fill is not null && k.Points.Length == 3)
            .Select(c => c.Points)];

    /// <summary>The span of the gradient's steps: the interior of the bar.</summary>
    private static Rect Interior(List<CallRecord> bar)
    {
        var steps = bar.OfType<DrawRectangleCall>().Where(k => k.Fill is not null)
            .Select(c => c.Rect).ToList();
        double top = steps.Min(r => r.Y), left = steps.Min(r => r.X);
        double bottom = steps.Max(r => r.Y + r.Height), right = steps.Max(r => r.X + r.Width);
        return new Rect(left, top, right - left, bottom - top);
    }

    private static Point[] Outline(List<CallRecord> bar) =>
        bar.OfType<DrawPolygonCall>().Where(k => k.Fill is null).Select(k => k.Points).Single();

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void UnderTheMatplotlibThemes_ABarExtendedBothWays_HasATriangleOutsideEachEndOfItsInterior(string style)
    {
        var theme = style.AsTheme();
        var bar = BarCalls(theme, ColorBarExtend.Both);

        var triangles = FilledTriangles(bar);
        var outline = Outline(bar);
        Assert.Equal(2, triangles.Count);
        // The outline runs round the interior and both tips: its box is the whole bar.
        double barTop = outline.Min(p => p.Y), barBottom = outline.Max(p => p.Y), barLeft = outline.Min(p => p.X), barRight = outline.Max(p => p.X);
        double whole = barBottom - barTop, inner = whole / 1.1, tip = 0.05 * inner, mid = (barLeft + barRight) / 2;

        // The over triangle on top: its base on the top of the interior, its tip 0.05 of the interior above it.
        var over = triangles.OrderBy(t => t.Min(p => p.Y)).First();
        var under = triangles.OrderBy(t => t.Min(p => p.Y)).Last();
        Assert.Equal(barTop, over.Min(p => p.Y), 6);
        Assert.Equal(barTop + tip, over.Max(p => p.Y), 6);
        Assert.Equal(mid, over.Single(p => Math.Abs(p.Y - barTop) < 1e-6).X, 6);
        Assert.Equal(barBottom, under.Max(p => p.Y), 6);
        Assert.Equal(barBottom - tip, under.Min(p => p.Y), 6);
        Assert.Equal(mid, under.Single(p => Math.Abs(p.Y - barBottom) < 1e-6).X, 6);

        // The gradient fills the interior, the bar less its two triangles.
        var interior = Interior(bar);
        Assert.Equal(barTop + tip, interior.Y, 6);
        Assert.Equal(inner, interior.Height - 1, 6);   // a step is drawn a pixel longer than its share, so no seam shows
        Assert.Equal(6, outline.Length);               // round the interior and both tips
    }

    [Fact]
    public void UnderClassic_AHorizontalBarExtendedAtItsLowEnd_HasOneTriangleToTheLeftOfItsInterior()
    {
        var bar = BarCalls(Theme.MatplotlibClassic, ColorBarExtend.Min, ColorBarOrientation.Horizontal);

        var triangle = Assert.Single(FilledTriangles(bar));
        var outline = Outline(bar);
        double left = outline.Min(p => p.X), right = outline.Max(p => p.X), top = outline.Min(p => p.Y), bottom = outline.Max(p => p.Y);
        double inner = (right - left) / 1.05;
        Assert.Equal(left, triangle.Min(p => p.X), 6);
        Assert.Equal(left + 0.05 * inner, triangle.Max(p => p.X), 6);
        Assert.Equal((top + bottom) / 2, triangle.Single(p => Math.Abs(p.X - left) < 1e-6).Y, 6);
        Assert.Equal(left + 0.05 * inner, Interior(bar).X, 6);
        Assert.Equal(5, outline.Length);
    }

    /// <summary>The library's own themes keep their rectangle, a tenth of the whole bar, inside it, and the frame a rectangle.</summary>
    [Fact]
    public void UnderTheLibrarysOwnTheme_TheExtendStaysARectangleInsideTheBar()
    {
        var bar = BarCalls(Theme.Default, ColorBarExtend.Both);

        Assert.DoesNotContain(bar, c => c is DrawPolygonCall);
    }

    /// <summary>A bar that extends neither way keeps its rectangle frame under every theme.</summary>
    [Fact]
    public void ABarThatExtendsNeitherWay_KeepsItsRectangleFrame()
    {
        var bar = BarCalls(Theme.MatplotlibV2, ColorBarExtend.Neither);

        Assert.DoesNotContain(bar, c => c is DrawPolygonCall);
    }

    /// <summary>The strip alone, at the ends the bar tests above do not reach: a horizontal bar's high end ends in a triangle to the
    /// right of its interior, a vertical bar's low end in one below it, each 0.05 of the interior, the bar's 315 over 1.05.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ATriangleStripExtendedAtOneEnd_DrawsItsTriangleBeyondThatEnd(bool vertical)
    {
        var bar = vertical ? new Rect(500, 40, 20, 315) : new Rect(100, 40, 315, 20);
        var extend = vertical ? ColorBarExtend.Min : ColorBarExtend.Max;
        var strip = new ColorBarStrip(bar, vertical, extend, DrawEdges: false, Steps: 50) { Triangles = true };
        var ctx = new RecordingRenderContext();
        strip.Draw(ctx, ColorMaps.Viridis, new ColorBarScale(0, 1, [], [], [], 0), Colors.Black);

        var polygons = ctx.Polygons().ToList();
        Point[] expected = vertical
            ? [new(500, 340), new(510, 355), new(520, 340)]
            : [new(400, 40), new(415, 50), new(400, 60)];
        Assert.Equal(2, polygons.Count);   // the triangle, then the outline
        Assert.Equal(expected.Length, polygons[0].Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].X, polygons[0][i].X, 9);
            Assert.Equal(expected[i].Y, polygons[0][i].Y, 9);
        }
        Assert.Equal(5, polygons[1].Length);
    }

    /// <summary>The strip alone: a triangle strip extended at its high end only shrinks its interior by 1 / 1.05 and keeps its low
    /// end at the bar's.</summary>
    [Fact]
    public void ATriangleStripExtendedAtItsHighEnd_ShrinksItsInteriorByOneOverOnePointZeroFive()
    {
        var strip = new ColorBarStrip(new Rect(500, 40, 20, 315), Vertical: true, ColorBarExtend.Max, DrawEdges: false, Steps: 50) { Triangles = true };

        Assert.Equal(new Rect(500, 55, 20, 300), strip.Gradient);
    }
}
