// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A ternary series is drawn where the composition stands in the triangle: the scatter as a marker on each, the line as
/// a polyline through them in the order given. The triangle's box is the whole plot area, so a sample at a corner is at a corner
/// of the box and is drawn whole, not clipped. Hovering a sample names its composition, not the position it has in the frame.</summary>
public class TernarySeriesRendererTests
{
    private static readonly Rect Box = new(100, 50, 400, 400 * Math.Sqrt(3) / 2);

    private static Axes ScatterAxes(out TernaryScatterSeries series, double[]? top = null, double[]? left = null, double[]? right = null)
    {
        var axes = new Axes();
        series = axes.TernaryScatter(top ?? [0.2, 0.0], left ?? [0.5, 1.0], right ?? [0.3, 0.0]);
        return axes;
    }

    private static RecordingRenderContext Draw(Axes axes)
    {
        var ctx = new RecordingRenderContext();
        AxesRenderer.Create(axes, Box, ctx, Theme.Default).Render();
        return ctx;
    }

    private static Point PixelOf(double top, double left, double right)
    {
        var frame = new TernaryPoint(top, left, right).Closed().InFrame();
        return new Point(Box.X + frame.X * Box.Width, Box.Y + Box.Height - frame.Y * Box.Width);
    }

    [Fact]
    public void AScatter_DrawsAMarkerOnEachComposition_AtWhereItStands()
    {
        var axes = ScatterAxes(out var series);
        series.MarkerSize = 10;
        series.Color = Color.FromHex("#C03030");

        var circles = Draw(axes).OfKind("DrawCircle").Select(c => (dynamic)c.Args!).ToArray();

        Assert.Equal(2, circles.Length);
        var first = PixelOf(0.2, 0.5, 0.3);
        Assert.Equal(first.X, ((Point)circles[0].center).X, 6);
        Assert.Equal(first.Y, ((Point)circles[0].center).Y, 6);
        Assert.Equal(5.0, (double)circles[0].radius);
        Assert.Equal(Color.FromHex("#C03030"), (Color?)circles[0].fill);
    }

    [Fact]
    public void ASampleAtACorner_IsDrawnWholeAtTheCornerOfTheBox_NotClipped()
    {
        var axes = ScatterAxes(out _);   // the second sample is (0, 1, 0): the bottom left corner

        var ctx = Draw(axes);

        var corner = ctx.OfKind("DrawCircle").Select(c => (Point)((dynamic)c.Args!).center)
            .Single(p => Math.Abs(p.X - Box.X) < 1e-6);
        Assert.Equal(Box.Y + Box.Height, corner.Y, 6);
        Assert.Equal(0, ctx.CountOf("PushClip"));   // T6: nothing cuts the marker at the edge of the box
    }

    [Fact]
    public void ALine_IsOnePolylineThroughTheCompositions_InTheOrderGiven()
    {
        var axes = new Axes();
        var line = axes.TernaryPlot([0.2, 1.0, 0.0], [0.5, 0.0, 1.0], [0.3, 0.0, 0.0]);
        line.LineWidth = 3;

        var polyline = Assert.Single(Draw(axes).OfKind("DrawLines"));

        dynamic args = polyline.Args!;
        Point[] points = args.points;
        Assert.Equal(3, points.Length);
        Assert.Equal(PixelOf(0.2, 0.5, 0.3).X, points[0].X, 6);
        Assert.Equal(PixelOf(1, 0, 0).Y, points[1].Y, 6);   // the apex
        Assert.Equal(PixelOf(0, 1, 0).X, points[2].X, 6);   // the bottom left corner
        Assert.Equal(3.0, (double)args.thickness);
    }

    [Fact]
    public void ASampleThatCannotBePlaced_IsLeftOutOfTheDrawing()
    {
        var series = new TernaryScatterSeries([1.0, -1.0, 1.0], [1.0, 1.0, 0.0], [1.0, 1.0, 0.0]);
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Ternary };
        axes.AddSeries(series);

        Assert.Equal(2, Draw(axes).CountOf("DrawCircle"));
    }

    [Fact]
    public void ATernaryFigure_RendersToSvg_WithTheMarkers()
    {
        var figure = new Figure { Width = 500, Height = 400 };
        figure.AddSubPlot(1, 1, 1).TernaryScatter([0.2, 0.4], [0.5, 0.4], [0.3, 0.2]);

        string svg = figure.ToSvg();

        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(svg, "<circle").Count);
        Assert.DoesNotContain("clip-path", svg, StringComparison.Ordinal);
    }

    // ── what hovering a sample says ─────────────────────────────────────────────

    private static (Figure Figure, IChartLayout Layout) Laid()
    {
        var figure = new Figure { Width = 500, Height = 400, ChartId = "t" };
        var axes = figure.AddSubPlot(1, 1, 1);
        axes.YAxis.Label = "Clay";
        axes.ZAxis.Label = "Silt";
        axes.XAxis.Label = "Sand";
        axes.TernaryScatter([0.2, 0.7], [0.5, 0.2], [0.3, 0.1]);
        var layout = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext());
        return (figure, ChartLayout.Create(figure, layout));
    }

    [Fact]
    public void TheNearestSample_IsFoundByItsPlaceInTheTriangle_AndNamedByItsComposition()
    {
        var (figure, layout) = Laid();
        var plot = layout.GetPlotArea(0);
        var frame = new TernaryPoint(0.2, 0.5, 0.3).InFrame();

        // The pointer is a few pixels from the first sample, converted to the frame through the transform the axes draw with.
        var pointer = layout.PixelToData(plot.X + frame.X * plot.Width + 3, plot.Y + plot.Height - frame.Y * plot.Width, 0)!.Value;
        var hit = NearestPointFinder.Find(figure, 0, pointer.DataX, pointer.DataY, layout);

        Assert.NotNull(hit);
        Assert.Equal(0, hit!.PointIndex);
        Assert.Equal("Clay 20 %, Silt 50 %, Sand 30 %", hit.Detail);
    }

    [Fact]
    public void APointerFarFromEverySample_FindsNothing()
    {
        var (figure, layout) = Laid();
        var plot = layout.GetPlotArea(0);

        var pointer = layout.PixelToData(plot.X + plot.Width * 0.05, plot.Y + plot.Height * 0.9, 0)!.Value;

        Assert.Null(NearestPointFinder.Find(figure, 0, pointer.DataX, pointer.DataY, layout));
    }

    [Fact]
    public void WithNoNamesOnTheAxes_TheCompositionIsNamedByThePositionOfThePart()
    {
        var figure = new Figure { Width = 500, Height = 400, ChartId = "t" };
        figure.AddSubPlot(1, 1, 1).TernaryScatter([0.2], [0.5], [0.3]);
        var layout = ChartLayout.Create(figure, new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()));
        var plot = layout.GetPlotArea(0);
        var frame = new TernaryPoint(0.2, 0.5, 0.3).InFrame();
        var pointer = layout.PixelToData(plot.X + frame.X * plot.Width, plot.Y + plot.Height - frame.Y * plot.Width, 0)!.Value;

        var hit = NearestPointFinder.Find(figure, 0, pointer.DataX, pointer.DataY, layout);

        Assert.Equal("top 20 %, left 50 %, right 30 %", hit!.Detail);
    }
}
