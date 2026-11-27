// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>The patches of a Cartesian axes (<see cref="Axes.Patches"/>) are drawn by one part, in the layer of the spans: after
/// them and before the reference lines, so above the grid and below every series, as matplotlib draws a patch (zorder 1) below a
/// line (zorder 2). Each patch is one path, in the order the patches were added, its outline mapped through the transform the
/// series are drawn with (<see cref="PathSegmentExtensions.ToPixels"/>), in the colours its own style gives it
/// (<see cref="Patch.DrawnStyleUnder(Theme.Default)"/>). The automatic range takes every patch in under the library's own themes, so the plot area is
/// clipped there only when a limit was set; under the matplotlib themes it is clipped too where the patches take no part in the range. A polar, ternary or 3-D axes draws no patch.</summary>
public class CartesianPatchesPartTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);

    private static readonly Color Marked = Color.FromHex("#123456");

    private static RecordingRenderContext Render(Axes axes, DataTransform? transform = null)
    {
        var ctx = CartesianParts.PartFactory.Drawn(c => CartesianParts.PartFactory.Patches(axes, Plot, c, DefaultTheme, transform ?? Plot.Linear(), axes.RangeOf(DefaultTheme).PatchesMayReachPastPlot));
        return ctx;
    }

    private static RecordingRenderContext RenderUnder(Axes axes, Theme theme)
    {
        var ctx = CartesianParts.PartFactory.Drawn(c => CartesianParts.PartFactory.Patches(axes, Plot, c, theme, Plot.Linear(), axes.RangeOf(theme).PatchesMayReachPastPlot));
        return ctx;
    }

    private static MoveToSegment M(double x, double y) => new(new Point(x, y));

    private static LineToSegment L(double x, double y) => new(new Point(x, y));

    [Fact]
    public void EachPatch_IsOnePath_InTheOrderItWasAdded()
    {
        var axes = new Axes();
        axes.AddPatch(new Rectangle(new(1, 1), 2, 1) { FaceColor = Colors.Red });
        axes.AddPatch(new Polygon([new(4, 0), new(6, 0), new(5, 2)]) { FaceColor = Colors.Green });
        axes.AddPatch(new Arrow(new(0, 5), 4, 0) { Width = 0.2, FaceColor = Colors.Blue });

        var paths = Render(axes).Of<DrawPathCall>();

        Assert.Equal([Colors.Red, Colors.Green, Colors.Blue], paths.Select(p => p.Shape.Fill!.Value));
    }

    [Fact]
    public void ARectangle_IsDrawnThroughTheTransform()
    {
        var axes = new Axes();
        axes.AddPatch(new Rectangle(new(1, 1), 2, 1));

        var path = Assert.Single(Render(axes).Of<DrawPathCall>());

        Assert.Equal([M(40, 270), L(120, 270), L(120, 240), L(40, 240), new CloseSegment()], path.Segments);
    }

    /// <summary>On an axis that runs high to low the patch stands where its data is, mirrored with the axis.</summary>
    [Fact]
    public void OnAReversedAxis_ThePatchIsMirroredWithIt()
    {
        var axes = new Axes();
        axes.AddPatch(new Rectangle(new(1, 1), 2, 1));

        var path = Assert.Single(Render(axes, new DataTransform(0, 10, 10, 0, Plot)).Of<DrawPathCall>());

        Assert.Equal([M(40, 30), L(120, 30), L(120, 60), L(40, 60), new CloseSegment()], path.Segments);
    }

    /// <summary>A vertex at 0 on a log axis stands a thousand decades below it (matplotlib's <c>nonpositive='clip'</c>; its
    /// <c>ax.fill([1, 2, 3], [1, 0, 10])</c> is the polygon <c>[[96.14, 58.0], [307.5, -279942.0], [518.86, 338.0]]</c>,
    /// printed), so the two edges to it dive almost straight to the plot's bottom edge, where the shape is cut and runs along it.
    /// The library pinned the vertex to the edge and drew a V.</summary>
    [Fact]
    public void OnALogAxis_AVertexItCannotPlace_IsCutWhereTheShapeLeavesThePlot()
    {
        var logY = Plot.LogY();
        var axes = new Axes();
        axes.AddPatch(new Polygon([new(1, 10), new(5, 0), new(9, 10)]));
        const double beyond = 300 + 1000 * 150.0;

        var path = Assert.Single(Render(axes, logY).Of<DrawPathCall>());

        Assert.Equal([M(40, 150), L(40 + (300 - 150.0) / (beyond - 150) * (200 - 40), 300),
            L(200 + (300 - beyond) / (150 - beyond) * (360 - 200), 300), L(360, 150), new CloseSegment()], path.Segments);
    }

    /// <summary>A vertex inside an axis break has no place: the outline is left open there and resumes with a move.</summary>
    [Fact]
    public void AVertexInsideABreak_StartsANewSubpath()
    {
        var brokenY = new DataTransform(0, 10, 0, 80, Plot, null, [new AxisBreak(40, 60)], 0, 10, 0, 100);
        var axes = new Axes();
        axes.AddPatch(new Polygon([new(1, 10), new(5, 50), new(9, 10), new(9, 30)]));

        var path = Assert.Single(Render(axes, brokenY).Of<DrawPathCall>());

        Assert.Equal([M(40, 262.5), M(360, 262.5), L(360, 187.5)], path.Segments);
    }

    /// <summary>A patch with a coordinate that is not a number has no outline, and a polygon of two vertices has none either:
    /// neither is drawn, and the others are drawn as without them.</summary>
    [Fact]
    public void APatchThatCannotBeDrawn_IsSkipped_TheOthersAreDrawn()
    {
        var axes = new Axes();
        axes.AddPatch(new Rectangle(new(double.NaN, 1), 2, 1));
        axes.AddPatch(new Polygon([new(0, 0), new(1, 1)]));
        axes.AddPatch(new Arrow(new(0, 0), double.PositiveInfinity, 1));
        axes.AddPatch(new Rectangle(new(1, 1), 2, 1) { FaceColor = Marked });

        var call = Assert.IsType<DrawPathCall>(Assert.Single(Render(axes).Calls));

        Assert.Equal(Marked, call.Shape.Fill);
    }

    [Fact]
    public void TheStyleOfThePatch_ReachesTheContext()
    {
        var axes = new Axes();
        var patch = axes.AddPatch(new Rectangle(new(1, 1), 2, 1)
        {
            FaceColor = Colors.Red, EdgeColor = Colors.Green, EdgeWidth = 2.5, EdgeLineStyle = LineStyle.Dashed,
            Hatch = HatchPattern.Cross, HatchColor = Colors.Blue, Alpha = 0.5,
        });

        Assert.Equal(patch.DrawnStyleUnder(Theme.Default), Assert.Single(Render(axes).Of<DrawPathCall>()).Shape);
    }

    // ---- curved patches: cubic curves whose control points go through the transform ----------------------------------------

    /// <summary>Every point of a path in order: a move's or a line's point, a curve's two control points and its end.</summary>
    private static Point[] PointsOf(IEnumerable<PathSegment> path) =>
        [.. path.SelectMany(segment => segment switch
        {
            MoveToSegment move => new[] { move.Point },
            LineToSegment line => [line.Point],
            BezierSegment curve => [curve.Control1, curve.Control2, curve.End],
            _ => [],
        })];

    /// <summary>A circle is drawn as the cubic curves of its outline, each point (control points included) mapped through the transform.</summary>
    [Fact]
    public void ACircle_IsDrawnAsCubicCurves_EveryPointThroughTheTransform()
    {
        var circle = new Circle(new(5, 5), 1);
        var axes = new Axes();
        axes.AddPatch(circle);

        var path = Assert.Single(Render(axes).Of<DrawPathCall>()).Segments;

        Assert.Equal(1, path.Count(segment => segment is MoveToSegment));
        Assert.Equal(8, path.Count(segment => segment is BezierSegment));
        Assert.IsType<CloseSegment>(path[^1]);
        Assert.Equal(PointsOf(circle.Outline()).Select(p => Plot.Linear().DataToPixel(p.X, p.Y)), PointsOf(path));
    }

    /// <summary>On a log axis matplotlib maps a circle's control points through the scale and keeps the curves: its transformed path is
    /// log10 of the data-space control points (measured on matplotlib 3.11.2, <c>Circle((10, 10), 5)</c> on log axes from 1 to 100,
    /// <c>(patch transform + transScale + transLimits)</c> of the path's vertices, in fractions of the axes). The drawn path, read as
    /// fractions of the plot, is the same.</summary>
    [Fact]
    public void ACircleOnALogAxis_MapsItsControlPoints()
    {
        var axes = new Axes();
        axes.AddPatch(new Circle(new(10, 10), 5));
        double[][] matplotlib =
        [
            [0.5, 0.34948500216800943],
            [0.5270385758181257, 0.34948500216800943], [0.550149067199421, 0.37123811373929966], [0.5657376955437431, 0.4052663309674135],
            [0.5802818085162447, 0.43467749294224256], [0.5880456295278407, 0.4691093208748703], [0.5880456295278407, 0.5],
            [0.5880456295278407, 0.5270385758181257], [0.5802818085162447, 0.550149067199421], [0.5657376955437431, 0.5657376955437431],
            [0.550149067199421, 0.5802818085162447], [0.5270385758181257, 0.5880456295278407], [0.5, 0.5880456295278407],
            [0.4691093208748703, 0.5880456295278407], [0.43467749294224256, 0.5802818085162447], [0.4052663309674135, 0.5657376955437431],
            [0.37123811373929966, 0.550149067199421], [0.34948500216800943, 0.5270385758181257], [0.34948500216800943, 0.5],
            [0.34948500216800943, 0.4691093208748703], [0.37123811373929966, 0.43467749294224256], [0.4052663309674135, 0.4052663309674135],
            [0.43467749294224256, 0.37123811373929966], [0.4691093208748703, 0.34948500216800943], [0.5, 0.34948500216800943],
        ];

        var drawn = PointsOf((Assert.Single(Render(axes, Plot.LogLog()).Of<DrawPathCall>())).Segments);

        Assert.Equal(matplotlib.Length, drawn.Length);
        for (int i = 0; i < drawn.Length; i++)
        {
            Assert.Equal(matplotlib[i][0], drawn[i].X / Plot.Width, 1e-9);
            Assert.Equal(matplotlib[i][1], (Plot.Height - drawn[i].Y) / Plot.Height, 1e-9);
        }
    }

    /// <summary>Where a drawn circle's on-curve point at 315 degrees lies against the axis-aligned ellipse through its four on-curve
    /// points at 0, 90, 180 and 270 degrees: 1 on that ellipse, less inside it.</summary>
    private static double OnTheEllipseOfItsVertices(PathSegment[] path)
    {
        var p = PointsOf(path);
        // matplotlib's unit circle starts at 270 degrees; every third point after it ends a curve, 45 degrees further on.
        Point bottom = p[0], at315 = p[3], right = p[6], top = p[12], left = p[18];
        double cx = (left.X + right.X) / 2, cy = (top.Y + bottom.Y) / 2;
        double a = (right.X - left.X) / 2, b = (bottom.Y - top.Y) / 2;
        return Math.Pow((at315.X - cx) / a, 2) + Math.Pow((at315.Y - cy) / b, 2);
    }

    /// <summary>A circle on a log axis is not an ellipse of the same vertices, as it would be if it were placed by its centre and
    /// radius alone: every point moves through the scale on its own, so the side towards the smaller values stretches (a decade
    /// takes more room there) and the shape is an egg. From matplotlib's own transformed points (above) the on-curve point at 315
    /// degrees stands at 0.944 of the way out to that ellipse. On linear axes the same test finds the ellipse.</summary>
    [Fact]
    public void ACircleOnALogAxis_IsNotAnEllipseOfTheSameVertices()
    {
        var axes = new Axes();
        axes.AddPatch(new Circle(new(10, 10), 5));
        var onLinear = new Axes();
        onLinear.AddPatch(new Circle(new(5, 5), 2));

        var log = (Assert.Single(Render(axes, Plot.LogLog()).Of<DrawPathCall>())).Segments;
        var linear = Assert.Single(Render(onLinear).Of<DrawPathCall>()).Segments;

        Assert.Equal(1, OnTheEllipseOfItsVertices(linear), 1e-9);
        Assert.Equal(0.9443322155, OnTheEllipseOfItsVertices(log), 1e-9);
        var p = PointsOf(log);
        Assert.True(p[6].X - p[0].X < p[0].X - p[18].X, "the half towards the larger values is not the narrower one");
    }

    /// <summary>On an axis that runs high to low a curved patch is mirrored with it, every control point included.</summary>
    [Fact]
    public void OnAReversedAxis_ACircleIsMirroredWithIt()
    {
        var axes = new Axes();
        axes.AddPatch(new Wedge(new(3, 5), 2, 30, 250) { Width = 1 });

        var normal = PointsOf(Assert.Single(Render(axes).Of<DrawPathCall>()).Segments);
        var reversed = PointsOf((Assert.Single(Render(axes, new DataTransform(10, 0, 0, 10, Plot)).Of<DrawPathCall>())).Segments);

        Assert.Equal(normal.Length, reversed.Length);
        for (int i = 0; i < normal.Length; i++)
        {
            Assert.Equal(Plot.Width - normal[i].X, reversed[i].X, 1e-9);
            Assert.Equal(normal[i].Y, reversed[i].Y, 1e-9);
        }
    }

    /// <summary>An arc has no face: it is drawn as its open curve in its edge colour, black by default, and fills nothing.</summary>
    [Fact]
    public void AnArc_IsDrawnOpen_InItsEdgeColour_WithNoFill()
    {
        var axes = new Axes();
        axes.AddPatch(new Arc(new(5, 5), 4, 2) { Theta1 = 0, Theta2 = 180 });

        var call = Assert.Single(Render(axes).Of<DrawPathCall>());

        Assert.DoesNotContain(call.Segments, segment => segment is CloseSegment);
        Assert.Null(call.Shape.Fill);
        Assert.Equal(Colors.Black, call.Shape.Stroke);
    }

    // ---- the clip ----------------------------------------------------------------------------------------------------------

    /// <summary>Under a limit the caller set, a patch can reach past the plot: the patches are clipped to the plot area, by the
    /// rule a series is (<c>ClippedForSeries</c>), one clip around all of them, opened and closed once.</summary>
    [Fact]
    public void UnderASetLimit_ThePatchesAreClippedToThePlot_OnceAndBalanced()
    {
        var axes = new Axes();
        axes.XAxis.Min = 0;
        axes.AddPatch(new Rectangle(new(1, 1), 2, 1));
        axes.AddPatch(new Rectangle(new(8, 8), 5, 5));

        var calls = Render(axes).Calls;

        Assert.Equal(["PushClip", "DrawPath", "DrawPath", "PopClip"], calls.Select(c => c.Kind));
        Assert.Equal(Plot, ((PushClipCall)calls[0]).ClipRect);
    }

    /// <summary>An automatic range takes every patch in (<c>PatchContributions</c>), so nothing reaches past the plot: no clip.</summary>
    [Fact]
    public void UnderAnAutomaticRange_NoClipIsPushed()
    {
        var axes = new Axes();
        axes.AddPatch(new Rectangle(new(1, 1), 2, 1));

        Assert.Equal(["DrawPath"], Render(axes).Calls.Select(c => c.Kind));
    }

    /// <summary>Under the matplotlib themes <c>add_patch</c> asks for no range, so a patch of an axes that nothing else ranges can
    /// reach past the plot (matplotlib: xlim and ylim stay 0..1 for <c>add_patch(Rectangle((2, 2), 3, 3))</c>), and matplotlib
    /// clips every patch to the axes (<c>clip_on</c>): the patches are clipped without a set limit.</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_AnAxesOfPatchesAlone_ClipsThePatchesToThePlot()
    {
        var axes = new Axes();
        axes.AddPatch(new Rectangle(new(2, 2), 3, 3));

        var calls = RenderUnder(axes, Theme.MatplotlibV2).Calls;

        Assert.Equal(["PushClip", "DrawPath", "PopClip"], calls.Select(c => c.Kind));
    }

    /// <summary>The clip of the patches asks the same question their range does: under a theme whose frame does not clip every artist
    /// but whose patches range only an axis something asks for, a patch of an axes nothing else ranges can still reach past the plot,
    /// so it is clipped.</summary>
    [Fact]
    public void APatchTheRangeLeavesOut_IsClipped_EvenWhereTheFrameClipsNoArtist()
    {
        var axes = new Axes();
        axes.AddPatch(new Rectangle(new(2, 2), 3, 3));
        var theme = new Theme("frame-of-the-library", Theme.MatplotlibV2) { Looks = Theme.MatplotlibV2.Looks with { Frame = Theme.Default.Looks.Frame } };

        Assert.Equal(["PushClip", "DrawPath", "PopClip"], RenderUnder(axes, theme).Calls.Select(c => c.Kind));
    }

    /// <summary>A reference line asks for the range of Y alone, so the patch can still reach past the plot along X.</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_ALineThatRangesYAlone_StillClipsThePatches()
    {
        var axes = new Axes();
        axes.AxHLine(8);
        axes.AddPatch(new Rectangle(new(2, 2), 3, 3));

        Assert.Equal(["PushClip", "DrawPath", "PopClip"], RenderUnder(axes, Theme.MatplotlibV2).Calls.Select(c => c.Kind));
    }

    /// <summary>A series asks for the range of both axes, so the patches are inside it; matplotlib still clips every patch to the axes
    /// (printed with 3.11.2: <c>clip_on True</c>, the clip box the axes' bbox, for every artist under both styles), as it clips a series, so
    /// the edge of a patch at the end of the range stops at the spine: one clip, by the rule a series is clipped by.</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_ASeriesThatRangesBothAxes_StillClipsThePatches_AsEveryArtistIs()
    {
        var axes = new Axes();
        axes.AddSeries(new LineSeries([0.0, 5.0], [0.0, 5.0]));
        axes.AddPatch(new Rectangle(new(2, 2), 3, 3));

        Assert.Equal(["PushClip", "DrawPath", "PopClip"], RenderUnder(axes, Theme.MatplotlibV2).Calls.Select(c => c.Kind));
    }

    [Fact]
    public void UnderASetLimit_NothingToDraw_PushesNoClip()
    {
        var axes = new Axes();
        axes.YAxis.Max = 5;
        axes.AddPatch(new Polygon([new(0, 0), new(1, 1)]));

        Assert.Empty(Render(axes).Calls);
    }

    // ---- in the axes' drawing ---------------------------------------------------------------------------------------------

    private static int IndexOf<TCall>(RecordingRenderContext ctx, Func<TCall, bool> match) where TCall : CallRecord =>
        ctx.Calls.FindIndex(c => c is TCall call && match(call));

    /// <summary>A patch is drawn in the spans' layer, as matplotlib's patches and <c>axhspan</c> share zorder 1: after the spans
    /// and before the reference lines, so the series are drawn over it.</summary>
    [Fact]
    public void ThePatches_AreDrawnAfterTheSpans_AndBeforeTheReferenceLines()
    {
        var axes = new Axes();
        axes.Plot([0.0, 10.0], [0.0, 10.0]);
        axes.AxHSpan(2, 3).Color = Colors.Red;
        axes.AddPatch(new Rectangle(new(4, 4), 2, 2) { FaceColor = Marked });
        axes.AxHLine(5).Color = Color.FromHex("#0000FE");
        var ctx = axes.Drawn(Plot, DefaultTheme);

        int span = IndexOf<DrawRectangleCall>(ctx, a => a.Fill is Color { R: 255, G: 0, B: 0 });
        int patch = IndexOf<DrawPathCall>(ctx, a => Equals(a.Fill, Marked));
        int line = IndexOf<DrawLineCall>(ctx, a => Equals(a.Color, Color.FromHex("#0000FE")));
        int series = IndexOf<DrawLinesCall>(ctx, _ => true);
        Assert.True(span >= 0 && patch > span && line > patch && series > line, $"span {span}, patch {patch}, line {line}, series {series}");
    }

    /// <summary>An inset draws like any Cartesian axes (<c>CartesianInsetAxesRenderer</c>), its patches included.</summary>
    [Fact]
    public void AnInset_DrawsItsPatches()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0])).Build();
        figure.SubPlots[0].AddInset(0.5, 0.1, 0.4, 0.4).AddPatch(new Rectangle(new(1, 1), 2, 1) { FaceColor = Marked });
        var ctx = new RecordingRenderContext();

        new ChartRenderer().Render(figure, ctx);

        Assert.Single(ctx.Of<DrawPathCall>(), call => Equals(call.Shape.Fill, Marked));
    }

    /// <summary>An axes that holds patches and nothing else is drawn as any axes is: its frame, its ticks and labels, and its
    /// patches, in a range that frames them.</summary>
    [Fact]
    public void AnAxesOfPatchesOnly_DrawsItsFrameItsTicksAndItsPatches()
    {
        string svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddPatch(new Rectangle(new(2, 2), 1, 1) { FaceColor = Marked })).ToSvg();

        Assert.Equal(4, svg.Elements("g").Count(group => group["class"] == "spine"));
        Assert.True(svg.Elements("text").Count >= 4, "the axes drew fewer than four tick labels");
        Assert.Single(svg.Elements("path"), path => path["d"] is { } d && d.StartsWith("M ", StringComparison.Ordinal) && path["fill"] == "#123456");
    }

    /// <summary>The SVG lays the subplots out in its own loop (<c>SvgTransform</c>) and <see cref="ChartRenderer.Render"/>, the PNG's
    /// path, in turn; both draw an axes through <c>ChartRenderer.RenderAxes</c>, so on the same surface they draw the same patches.</summary>
    [Fact]
    public void TheSvgAndTheRendererPath_DrawTheSamePatches()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax
                .Plot([0.0, 10.0], [1.0, 100.0])
                .SetYScale(AxisScale.Log)
                .AddPatch(new Polygon([new(1, 10), new(5, 0), new(9, 50)]) { FaceColor = Marked })
                .AddPatch(new Arrow(new(2, 20), 6, 0) { Width = 0.5, FaceColor = Colors.Red })
                .AddPatch(new Circle(new(5, 30), 2) { FaceColor = Colors.Blue }))
            .AddSubPlot(1, 2, 2, ax => ax
                .AddPatch(new Rectangle(new(0, 0), 3, 2) { Angle = 30, EdgeColor = Colors.Black, FaceColor = Colors.Green })
                .AddPatch(new Wedge(new(1, 1), 1, 20, 300) { Width = 0.4, FaceColor = Colors.Blue }))
            .Build();
        var direct = new SvgRenderContext();
        new ChartRenderer().Render(figure, direct);

        var viaSvg = PatchPaths(figure.ToSvg());
        var viaRenderer = PatchPaths(direct.GetOutput());

        Assert.Equal(5, viaSvg.Count);
        Assert.Equal(viaSvg, viaRenderer);
        Assert.Equal(2, viaSvg.Count(path => path.Contains(" C ", StringComparison.Ordinal)));
    }

    private static List<string> PatchPaths(string svg) =>
        [.. svg.Elements("path")
            .Where(path => path["d"] is { } d && d.StartsWith("M ", StringComparison.Ordinal) && path["fill"] is "#123456" or "#FF0000" or "#008000" or "#0000FF")
            .Select(path => string.Join(' ', path.Attributes.Select(attribute => $"{attribute.Key}={attribute.Value}")))];
}
