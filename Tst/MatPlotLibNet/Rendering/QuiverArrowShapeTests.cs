// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The arrow of an autoscaled quiver is the polygon matplotlib's quiver draws. matplotlib 3.11.2, <c>Quiver._init</c>, <c>_make_verts</c>
/// and <c>_h_arrows</c>, run on axes 400 pixels wide (<c>fig.add_axes([0, 0, 1, 1])</c>, 4 x 3 inches at 100 dpi) and printed as
/// <c>q._make_verts(q.XY, q.U, q.V, q.angles) * q._trans_scale</c> (pixels from the tail, up positive): the shaft is
/// <c>width = 0.06 * span / clip(sqrt(N), 8, 25)</c> of the axes' width across, the head <c>headwidth = 3</c> shaft widths across and
/// <c>headlength = 5</c> long with its barbs <c>headaxislength = 4.5</c> back from the tip; an arrow shorter than <c>minshaft * headlength</c> =
/// 5 widths is shrunk to its head and one shorter than <c>minlength = 1</c> width is a heptagon of that diameter; the polygon is filled with
/// <c>facecolors</c> and has <c>linewidths=(0,)</c>, no edge. The arrows are 8 vertices: the tail's two corners, the shaft's end, the
/// barbs and the tip.</summary>
public sealed class QuiverArrowShapeTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static QuiverSeries Three() => new([1.0, 2.0, 3.0], [1.0, 1.0, 1.0], [1.0, 2.0, 0.0], [0.0, 0.0, 3.0]);

    private static RecordingRenderContext Render(QuiverSeries series, Theme? theme = null, params ISeries[] before)
    {
        var ctx = new RecordingRenderContext();
        var context = new SeriesRenderContext(new DataTransform(0, 10, 0, 10, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx))
        {
            Theme = theme ?? Theme.MatplotlibV2,
            AxesSeries = [.. before, series],
        };
        new QuiverSeriesRenderer(context).Render(series);
        return ctx;
    }

    /// <summary>The polygon matplotlib prints, relative to the tail with up positive, put at <paramref name="tail"/> on the screen.</summary>
    private static Point[] At(Point tail, double[][] relative) => [.. relative.Select(p => new Point(tail.X + p[0], tail.Y - p[1]))];

    private static void AssertPolygon(Point[] expected, Point[] actual, string what)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.True(Math.Abs(expected[i].X - actual[i].X) < 2e-3 && Math.Abs(expected[i].Y - actual[i].Y) < 2e-3,
                $"{what}: vertex {i} is ({actual[i].X:F4}, {actual[i].Y:F4}), matplotlib prints ({expected[i].X:F4}, {expected[i].Y:F4})");
        }
    }

    private static Point TailOf(double dataX, double dataY) => new DataTransform(0, 10, 0, 10, Bounds).DataToPixel(dataX, dataY);

    /// <summary>N = 3 gives <c>sn = 8</c>, so a shaft is 3 pixels across on 400; the arrows of lengths 1, 2 and 3 over a mean of 2 are
    /// 11.1111, 22.2222 and 33.3333 pixels long, the first shorter than five widths (shrunk to a head), the third straight up.</summary>
    [Fact]
    public void AnArrow_IsMatplotlibsPolygon()
    {
        var polygons = Render(Three()).Polygons();

        Assert.Equal(3, polygons.Count);
        AssertPolygon(At(TailOf(1, 1), [[0, 1.1111], [1.1111, 1.1111], [0, 3.3333], [11.1111, 0], [0, -3.3333], [1.1111, -1.1111], [0, -1.1111], [0, 1.1111]]),
            polygons[0], "the short arrow");
        AssertPolygon(At(TailOf(2, 1), [[0, 1.5], [8.7222, 1.5], [7.2222, 4.5], [22.2222, 0], [7.2222, -4.5], [8.7222, -1.5], [0, -1.5], [0, 1.5]]),
            polygons[1], "the arrow of a shaft and a head");
        AssertPolygon(At(TailOf(3, 1), [[-1.5, 0], [-1.5, 19.8333], [-4.5, 18.3333], [0, 33.3333], [4.5, 18.3333], [1.5, 19.8333], [1.5, 0], [-1.5, 0]]),
            polygons[2], "the arrow that points up");
    }

    /// <summary>The rotational field of the Fidelity row (N = 100, <c>sn = 10</c>, a shaft 2.4 pixels across on 400): the arrow at the
    /// point (-2, 2) takes the vector (-2, -2) and points down and to the left, 26.2333 pixels along each axis of the screen.</summary>
    [Fact]
    public void ADiagonalArrow_IsThePolygonRotated()
    {
        double[] axis = [.. Enumerable.Range(0, 10).Select(i => -2 + 4.0 * i / 9)];
        var x = new double[100]; var y = new double[100]; var u = new double[100]; var v = new double[100];
        for (int iy = 0; iy < 10; iy++)
            for (int ix = 0; ix < 10; ix++)
            {
                int k = iy * 10 + ix;
                x[k] = axis[ix]; y[k] = axis[iy]; u[k] = -axis[iy]; v[k] = axis[ix];
            }

        var ctx = new RecordingRenderContext();
        var transform = new DataTransform(-2.2, 2.2, -2.2, 2.2, Bounds);
        var series = new QuiverSeries(x, y, u, v);
        new QuiverSeriesRenderer(new SeriesRenderContext(transform, ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)) { Theme = Theme.MatplotlibV2, AxesSeries = [series] })
            .Render(series);

        var polygons = ctx.Polygons();

        Assert.Equal(100, polygons.Count);
        AssertPolygon(At(transform.DataToPixel(-2, 2),
                [[0.8485, -0.8485], [-17.748, -19.4451], [-15.2024, -20.2936], [-26.2333, -26.2333], [-20.2936, -15.2024], [-19.4451, -17.748], [-0.8485, 0.8485], [0.8485, -0.8485]]),
            polygons[90], "the arrow at (-2, 2)");
        AssertPolygon(At(transform.DataToPixel(axis[5], axis[5]),
                [[-0.2915, -0.2915], [-0.583, 0], [-0.8744, -0.8744], [-2.9148, 2.9148], [0.8744, 0.8744], [0, 0.583], [0.2915, 0.2915], [-0.2915, -0.2915]]),
            polygons[55], "a short arrow that points up and to the left");
    }

    /// <summary>An arrow shorter than one shaft width is a heptagon one width across (<c>th = arange(0, 8) * (pi / 3)</c>, radius
    /// <c>minlength / 2</c>): printed for the arrows 1000 and 3 long, the second is 0.044 widths long.</summary>
    [Fact]
    public void AnArrowShorterThanItsWidth_IsAHeptagon()
    {
        var series = new QuiverSeries([1.0, 2.0], [1.0, 1.0], [1000.0, 3.0], [0.0, 0.0]);

        var polygons = Render(series).Polygons();

        AssertPolygon(At(TailOf(1, 1), [[0, 1.5], [30.8115, 1.5], [29.3115, 4.5], [44.3115, 0], [29.3115, -4.5], [30.8115, -1.5], [0, -1.5], [0, 1.5]]),
            polygons[0], "the long arrow");
        AssertPolygon(At(TailOf(2, 1), [[1.5, 0], [0.75, 1.299], [-0.75, 1.299], [-1.5, 0], [-0.75, -1.299], [0.75, -1.299], [1.5, 0], [0.75, 1.299]]),
            polygons[1], "the dot");
    }

    /// <summary>The shaft is <c>0.06 / clip(sqrt(N), 8, 25)</c> of the axes' width across: 3 pixels for up to 64 arrows, 2.4 for 100 and
    /// 0.96 for 625 and more.</summary>
    [Theory]
    [InlineData(3, 3.0)]
    [InlineData(100, 2.4)]
    [InlineData(1000, 0.96)]
    public void TheShaftWidth_FollowsTheNumberOfArrows(int count, double widthPixels)
    {
        var x = new double[count]; var y = new double[count];
        for (int i = 0; i < count; i++) { x[i] = i % 10; y[i] = i / 10 % 10; }
        var series = new QuiverSeries(x, y, [.. x.Select(_ => 1.0)], [.. x.Select(_ => 0.0)]);

        var polygon = Render(series).Polygons()[0];

        // the tail's two corners are the first and the last but one vertex: a half shaft either side of the tail
        Assert.Equal(widthPixels, polygon[6].Y - polygon[0].Y, 9);
    }

    /// <summary>The polygon is filled with the quiver's colour and has no edge (<c>linewidths=(0,)</c>), and no stroke is drawn for a shaft.</summary>
    [Fact]
    public void ThePolygon_IsFilledWithNoEdge_AndNothingElseIsDrawn()
    {
        var ctx = Render(Three());

        foreach (var polygon in ctx.Of<DrawPolygonCall>())
        {
            Assert.NotNull(polygon.Fill);
            Assert.Null(polygon.Stroke);
            Assert.Equal(0.0, polygon.StrokeThickness);
        }
        Assert.Equal(0, ctx.Count<DrawLineCall>());
    }

    /// <summary>Arrows in data units are strokes, as the library has always drawn them.</summary>
    [Fact]
    public void ArrowsInDataUnits_AreStillStrokes()
    {
        var series = Three();
        series.AutoScale = false;

        var ctx = Render(series);

        Assert.Equal(0, ctx.Count<DrawPolygonCall>());
        Assert.Equal(9, ctx.Count<DrawLineCall>());
    }

    /// <summary>The key is the quiver's own arrow (<c>self.Q._make_verts([[0., 0.]], u, v, 'uv')</c>, pivot <c>'tip'</c> for the label to the
    /// east): matplotlib prints, on axes 620 pixels wide for the rotational field and a key of 1, the tip at the point, the shaft 20.331
    /// pixels back and 3.72 across.</summary>
    [Fact]
    public void AKey_IsTheSamePolygon_WithItsTipOnItsPoint()
    {
        double[] axis = [.. Enumerable.Range(0, 10).Select(i => -2 + 4.0 * i / 9)];
        var x = new double[100]; var y = new double[100]; var u = new double[100]; var v = new double[100];
        for (int iy = 0; iy < 10; iy++)
            for (int ix = 0; ix < 10; ix++)
            {
                int k = iy * 10 + ix;
                x[k] = axis[ix]; y[k] = axis[iy]; u[k] = -axis[iy]; v[k] = axis[ix];
            }

        var bounds = new Rect(0, 0, 620, 480);
        var quiver = new QuiverSeries(x, y, u, v);
        var key = new QuiverKeySeries(0.9, 0.9, 1.0, "1 unit") { LabelPosition = QuiverKeyLabelPosition.Right };
        var ctx = new RecordingRenderContext();
        new QuiverKeySeriesRenderer(new SeriesRenderContext(new DataTransform(-2.2, 2.2, -2.2, 2.2, bounds), ctx, Colors.Tab10Blue, new RenderArea(bounds, ctx))
            { Theme = Theme.MatplotlibV2, AxesSeries = [quiver, key] }).Render(key);

        var polygon = Assert.Single(ctx.Polygons());

        var tip = new Point(bounds.X + 0.9 * bounds.Width, bounds.Y + (1 - 0.9) * bounds.Height);
        AssertPolygon(At(tip, [[-20.331, 1.86], [-16.74, 1.86], [-18.6, 5.58], [0, 0], [-18.6, -5.58], [-16.74, -1.86], [-20.331, -1.86], [-20.331, 1.86]]),
            polygon, "the key");
        Assert.Equal(0, ctx.Count<DrawLineCall>());
    }
    /// <summary>matplotlib draws a vector of no length among longer ones as a dot (<c>length = 0 &lt; minlength</c>); when every vector is
    /// zero long the scale is 0, the lengths are not numbers and nothing is drawn.</summary>
    [Fact]
    public void AVectorOfNoLength_IsADot_AndAFieldOfThem_DrawsNothing()
    {
        var some = new QuiverSeries([1.0, 2.0], [1.0, 1.0], [1.0, 0.0], [0.0, 0.0]);
        var none = new QuiverSeries([1.0, 2.0], [1.0, 1.0], [0.0, 0.0], [0.0, 0.0]);

        var dot = Render(some).Polygons()[1];

        Assert.Equal(TailOf(2, 1).X + 1.5, dot[0].X, 6);   // N = 2: a shaft is 3 pixels across, the dot 3 pixels across
        Assert.Equal(TailOf(2, 1).X - 1.5, dot[3].X, 6);
        Assert.Empty(Render(none).Polygons());
    }

    /// <summary>A key of a negative length points to the left, its tip still on its point under the label to the east.</summary>
    [Fact]
    public void AKeyOfANegativeLength_PointsLeft_AndAKeyOfAQuiverWithNoScale_DrawsNothing()
    {
        var quiver = Three();
        var key = new QuiverKeySeries(0.5, 0.5, -1.0, "key") { LabelPosition = QuiverKeyLabelPosition.Right };
        var empty = new QuiverSeries([1.0], [1.0], [0.0], [0.0]);

        Point[] Draw(QuiverSeries before, QuiverKeySeries k)
        {
            var ctx = new RecordingRenderContext();
            new QuiverKeySeriesRenderer(new SeriesRenderContext(new DataTransform(0, 10, 0, 10, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx))
                { Theme = Theme.MatplotlibV2, AxesSeries = [before, k] }).Render(k);
            return [.. ctx.Polygons().SelectMany(p => p)];
        }

        var polygon = Draw(quiver, key);

        Assert.Equal(200, polygon[3].X, 6);            // the tip is on the point
        Assert.True(polygon[0].X > polygon[3].X);      // and the foot is to the right of it
        Assert.Empty(Draw(empty, key));
    }
    /// <summary>An arrow whose tail cannot be placed is not drawn, and the others are.</summary>
    [Fact]
    public void AnArrowWhoseTailCannotBePlaced_IsLeftOut()
    {
        var series = new QuiverSeries([1.0, double.NaN, 3.0], [1.0, 1.0, 1.0], [1.0, 1.0, 1.0], [0.0, 0.0, 0.0]);

        Assert.Equal(2, Render(series).Polygons().Count);
    }
}
