// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An inset that indicates its zoom (matplotlib's <c>indicate_inset_zoom</c>) has its parent draw a rectangle over the
/// region the inset shows, in the parent's data coordinates, and two of the four lines from the rectangle's corners to the
/// inset's frame, chosen by matplotlib's rule. The rectangle is the limits the inset really draws (auto limits and margins
/// included), handed over by the render pass that drew the inset: one rule, the one that draws the inset.</summary>
public class ZoomIndicatorTests
{
    /// <summary>The parent's plot area in every geometric test: 40 pixels a unit for limits 0 to 10.</summary>
    private static readonly Rect ParentArea = new(100, 100, 400, 400);

    private const string Group = "zoom-indicator";

    /// <summary>A figure whose one subplot shows 0 to 10 on both axes (or what <paramref name="parent"/> sets) and holds one
    /// inset at <paramref name="bounds"/> that plots a line, is set up by <paramref name="inset"/> and indicates its zoom.</summary>
    private static Figure WithInset(InsetBounds bounds, Action<AxesBuilder> inset, Action<AxesBuilder>? parent = null,
        Action<ZoomIndicator>? zoom = null) =>
        Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([0.0, 10.0], [0.0, 10.0]);
            if (parent is null) ax.SetXLim(0, 10).SetYLim(0, 10);
            else parent(ax);
            ax.AddInset(bounds, i => { inset(i); i.IndicateZoom(zoom); });
        }).Build();

    /// <summary>Draws the first subplot of <paramref name="figure"/> into <see cref="ParentArea"/> through the render pass every
    /// picture takes, and records what it drew.</summary>
    private static RecordingRenderContext DrawAxes(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().RenderAxes(figure, figure.SubPlots[0], new RenderPass(ParentArea, ctx, figure.Theme));
        return ctx;
    }

    /// <summary>The calls drawn inside the zoom indicator's group, or <see langword="null"/> when there is none.</summary>
    private static List<CallRecord>? Indicator(RecordingRenderContext ctx)
    {
        int begin = ctx.Calls.FindIndex(c => c.Kind == "BeginGroup" && (string)((dynamic)c.Args!).cssClass == Group);
        if (begin < 0) return null;
        int end = ctx.Calls.FindIndex(begin, c => c.Kind == "EndGroup");
        return ctx.Calls.GetRange(begin + 1, end - begin - 1);
    }

    private static Point[] Outline(List<CallRecord> indicator) =>
        (Point[])((dynamic)indicator.Single(c => c.Kind == "DrawLines").Args!).points;

    private static (Point From, Point To)[] Connectors(List<CallRecord> indicator) =>
        [.. indicator.Where(c => c.Kind == "DrawLine").Select(c => ((Point)((dynamic)c.Args!).p1, (Point)((dynamic)c.Args!).p2))];

    /// <summary>The closed outline of the pixel rectangle from <paramref name="left"/> to <paramref name="right"/> and
    /// <paramref name="top"/> to <paramref name="bottom"/>.</summary>
    private static void AssertOutline(Point[] outline, double left, double top, double right, double bottom, double precision = 1e-9)
    {
        Assert.Equal(5, outline.Length);
        Assert.Equal(outline[0], outline[4]);
        Assert.Equal(left, outline.Min(p => p.X), precision);
        Assert.Equal(right, outline.Max(p => p.X), precision);
        Assert.Equal(top, outline.Min(p => p.Y), precision);
        Assert.Equal(bottom, outline.Max(p => p.Y), precision);
        // Each side is one horizontal or vertical stroke: the outline is the rectangle, not a quadrilateral through its corners.
        for (int i = 0; i < 4; i++)
            Assert.True(outline[i].X == outline[i + 1].X || outline[i].Y == outline[i + 1].Y, $"side {i} is not axis-aligned");
    }

    private static void AssertConnector((Point From, Point To) connector, Point onInset, Point onRegion, double precision = 1e-9)
    {
        Assert.Equal(onInset.X, connector.From.X, precision);
        Assert.Equal(onInset.Y, connector.From.Y, precision);
        Assert.Equal(onRegion.X, connector.To.X, precision);
        Assert.Equal(onRegion.Y, connector.To.Y, precision);
    }

    // ---- the rectangle is what the inset draws ------------------------------------------------------------------

    [Fact]
    public void TheRectangle_SpansTheInsetsOwnLimits_InTheParentsDataCoordinates()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(1, 3).SetYLim(1, 3));

        var indicator = Indicator(DrawAxes(figure))!;

        // x 1..3 -> 140..220, y 1..3 -> 460..380 (pixel y runs down).
        AssertOutline(Outline(indicator), left: 140, top: 380, right: 220, bottom: 460);
    }

    [Fact]
    public void TheRectangle_OfAnInsetWithAutoLimits_IncludesTheInsetsMargins()
    {
        // No limit set: the inset shows its data, 2..4 and 3..5, widened by matplotlib's 5 % margin on each side.
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([2.0, 4.0], [3.0, 5.0]));

        var indicator = Indicator(DrawAxes(figure))!;

        // x 1.9..4.1 -> 176..264, y 2.9..5.1 -> 384..296: wider than the data (2..4 would be 180..260).
        AssertOutline(Outline(indicator), left: 176, top: 296, right: 264, bottom: 384, precision: 1e-6);
    }

    [Fact]
    public void TheRectangle_OfAnInvertedInset_CoversTheSameRegion()
    {
        var upright = Indicator(DrawAxes(WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(1, 3).SetYLim(1, 3))))!;
        var inverted = Indicator(DrawAxes(WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(3, 1).SetYLim(3, 1))))!;

        AssertOutline(Outline(inverted), left: 140, top: 380, right: 220, bottom: 460);
        Assert.Equal(Connectors(upright), Connectors(inverted));
    }

    // ---- the connectors: matplotlib's rule, in pixels ----------------------------------------------------------

    /// <summary>The inset stands up and to the right of the region it zooms: matplotlib draws the upper-left and the
    /// lower-right connector, [LL, UL, LR, UR] = [F, T, T, F].</summary>
    [Fact]
    public void AnInsetUpRightOfItsRegion_DrawsTheUpperLeftAndLowerRightConnectors()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(1, 3).SetYLim(1, 3));

        var connectors = Connectors(Indicator(DrawAxes(figure))!);

        // The inset's frame: 320..480 across, 120..280 down.
        Assert.Equal(2, connectors.Length);
        AssertConnector(connectors[0], onInset: new Point(320, 120), onRegion: new Point(140, 380));
        AssertConnector(connectors[1], onInset: new Point(480, 280), onRegion: new Point(220, 460));
    }

    /// <summary>The inset stands up and to the left of its region: [LL, UL, LR, UR] = [T, F, F, T].</summary>
    [Fact]
    public void AnInsetUpLeftOfItsRegion_DrawsTheLowerLeftAndUpperRightConnectors()
    {
        var figure = WithInset(new InsetBounds(0.05, 0.05, 0.4, 0.4), i => i.Plot([7.0, 9.0], [1.0, 3.0]).SetXLim(7, 9).SetYLim(1, 3));

        var connectors = Connectors(Indicator(DrawAxes(figure))!);

        // The inset's frame: 120..280 both ways; the region 380..460 both ways.
        Assert.Equal(2, connectors.Length);
        AssertConnector(connectors[0], onInset: new Point(120, 280), onRegion: new Point(380, 460));
        AssertConnector(connectors[1], onInset: new Point(280, 120), onRegion: new Point(460, 380));
    }

    /// <summary>On a parent whose X axis runs backwards the region of x 1..3 lands on the right, so the choice is made where
    /// the two stand on the picture, after the transform: the lower-left and upper-right connectors.</summary>
    [Fact]
    public void OnAReversedParentAxis_TheConnectorsFollowWhereTheRegionIsDrawn()
    {
        var figure = WithInset(new InsetBounds(0.05, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(1, 3).SetYLim(1, 3),
            parent: ax => ax.SetXLim(10, 0).SetYLim(0, 10));

        var indicator = Indicator(DrawAxes(figure))!;
        var connectors = Connectors(indicator);

        AssertOutline(Outline(indicator), left: 380, top: 380, right: 460, bottom: 460);
        Assert.Equal(2, connectors.Length);
        AssertConnector(connectors[0], onInset: new Point(120, 280), onRegion: new Point(380, 460));
        AssertConnector(connectors[1], onInset: new Point(280, 120), onRegion: new Point(460, 380));
    }

    [Fact]
    public void OnALogParentAxis_TheRectangleStandsWhereTheLogScalePutsTheLimits()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [10.0, 100.0]).SetXLim(1, 3).SetYLim(10, 100),
            parent: ax => ax.SetXLim(0, 10).SetYScale(AxisScale.Log).SetYLim(1, 1000));

        var indicator = Indicator(DrawAxes(figure))!;

        // log10 1..1000 spans 0..3 over 400 pixels: 10 -> 500 - 400/3, 100 -> 500 - 800/3.
        AssertOutline(Outline(indicator), left: 140, top: 500 - 800 / 3.0, right: 220, bottom: 500 - 400 / 3.0, precision: 1e-6);
    }

    [Fact]
    public void AnInsetWhollyInsideItsRegion_DrawsTheRectangleAndNoConnector()
    {
        var figure = WithInset(new InsetBounds(0.4, 0.4, 0.2, 0.2), i => i.Plot([0.5, 9.5], [0.5, 9.5]).SetXLim(0.5, 9.5).SetYLim(0.5, 9.5));

        var indicator = Indicator(DrawAxes(figure))!;

        AssertOutline(Outline(indicator), left: 120, top: 120, right: 480, bottom: 480);
        Assert.Empty(Connectors(indicator));
    }

    [Fact]
    public void ARegionWhollyInsideItsInset_DrawsTheRectangleAndNoConnector()
    {
        var figure = WithInset(new InsetBounds(0.1, 0.1, 0.8, 0.8), i => i.Plot([4.0, 6.0], [4.0, 6.0]).SetXLim(4, 6).SetYLim(4, 6));

        var indicator = Indicator(DrawAxes(figure))!;

        AssertOutline(Outline(indicator), left: 260, top: 260, right: 340, bottom: 340);
        Assert.Empty(Connectors(indicator));
    }

    [Fact]
    public void WithTheConnectorsSwitchedOff_OnlyTheRectangleIsDrawn()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(1, 3).SetYLim(1, 3),
            zoom: z => z.Connectors = false);

        var indicator = Indicator(DrawAxes(figure))!;

        AssertOutline(Outline(indicator), left: 140, top: 380, right: 220, bottom: 460);
        Assert.Empty(Connectors(indicator));
    }

    // ---- nothing is drawn where the indicator has no place --------------------------------------------------

    /// <summary>A limit of the inset that the parent's axis cannot place (below zero on a log axis) leaves a corner of the
    /// rectangle without a place, and nothing is drawn rather than a broken shape.</summary>
    [Fact]
    public void ACornerTheParentCannotPlace_DrawsNoIndicator()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([-1.0, 2.0], [1.0, 3.0]).SetXLim(-1, 2).SetYLim(1, 3),
            parent: ax => ax.SetXScale(AxisScale.Log).SetXLim(1, 100).SetYLim(0, 10));

        Assert.Null(Indicator(DrawAxes(figure)));
    }

    /// <summary>A polar (or 3-D, or ternary) parent has no data rectangle to draw the region in, so its inset indicates
    /// nothing; the inset itself is still drawn.</summary>
    [Theory]
    [InlineData(CoordinateSystem.Polar)]
    [InlineData(CoordinateSystem.ThreeD)]
    [InlineData(CoordinateSystem.Ternary)]
    public void ANonCartesianParent_DrawsNoIndicator(CoordinateSystem system)
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(1, 3).SetYLim(1, 3));
        figure.SubPlots[0].CoordinateSystem = system;

        var ctx = DrawAxes(figure);

        Assert.Null(Indicator(ctx));
        Assert.Contains(ctx.OfKind("DrawRectangle"), c => (Rect)((dynamic)c.Args!).rect == new Rect(320, 120, 160, 160));
    }

    /// <summary>A polar, 3-D or ternary inset shows no data rectangle (a ternary frame's transform maps the triangle, not
    /// data), so there is no region to indicate.</summary>
    [Theory]
    [InlineData(CoordinateSystem.Polar)]
    [InlineData(CoordinateSystem.ThreeD)]
    [InlineData(CoordinateSystem.Ternary)]
    public void ANonCartesianInset_DrawsNoIndicator(CoordinateSystem system)
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]));
        figure.SubPlots[0].Insets[0].CoordinateSystem = system;

        Assert.Null(Indicator(DrawAxes(figure)));
    }

    /// <summary>A render that kept no transform (a renderer registered for the Cartesian system that sets none) has no
    /// range to indicate or to draw the region in.</summary>
    [Fact]
    public void ARenderThatKeptNoTransform_HasNoIndicatorGeometry()
    {
        var parent = new Axes();
        var inset = parent.AddInset(0.5, 0.1, 0.3, 0.3);
        var area = new Rect(0, 0, 100, 100);
        var view = new DrawnView(area, new DataTransform(0, 1, 0, 1, area));

        Assert.Null(ZoomIndicatorPath.Of(parent, new DrawnView(area, null), inset, view));
        Assert.Null(ZoomIndicatorPath.Of(parent, view, inset, new DrawnView(area, null)));
        Assert.NotNull(ZoomIndicatorPath.Of(parent, view, inset, view));
    }

    /// <summary>Each corner of the region is checked: a limit the parent's log axes cannot place, at either end of either
    /// axis (a limit set high to low puts the low one last), leaves no geometry.</summary>
    [Theory]
    [InlineData(-1.0, 2.0, 1.0, 2.0)]
    [InlineData(2.0, -1.0, 1.0, 2.0)]
    [InlineData(1.0, 2.0, -1.0, 2.0)]
    [InlineData(1.0, 2.0, 2.0, -1.0)]
    public void ALimitTheParentCannotPlace_AtAnyCorner_LeavesNoGeometry(double xMin, double xMax, double yMin, double yMax)
    {
        var parent = new Axes();
        var inset = parent.AddInset(0.5, 0.1, 0.3, 0.3);
        var area = new Rect(0, 0, 100, 100);
        var logParent = new DrawnView(area, new DataTransform(0, 2, 0, 2, area, null, null, 1, 100, 1, 100,
            AxisScale.Log, AxisScale.Log, 1, 1));
        var insetView = new DrawnView(new Rect(50, 10, 30, 30), new DataTransform(xMin, xMax, yMin, yMax, new Rect(50, 10, 30, 30)));

        Assert.Null(ZoomIndicatorPath.Of(parent, logParent, inset, insetView));
    }

    [Fact]
    public void AnInsetThatIndicatesNothing_DrawsNoIndicator()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0])
            .AddInset(0.55, 0.05, 0.4, 0.4, i => i.Plot([1.0, 3.0], [1.0, 3.0]))).Build();

        Assert.Null(Indicator(DrawAxes(figure)));
        Assert.DoesNotContain(Group, figure.ToSvg());
    }

    // ---- its look ------------------------------------------------------------------------------------------

    [Fact]
    public void ByDefault_TheIndicatorIsTheThemesNeutralGrey_AtHalfOpacity_OnePixelSolid()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(1, 3).SetYLim(1, 3));

        var indicator = Indicator(DrawAxes(figure))!;

        Assert.Equal(0.5, (double)((dynamic)indicator.First(c => c.Kind == "SetOpacity").Args!).opacity);
        Assert.Equal(1.0, (double)((dynamic)indicator.Last(c => c.Kind == "SetOpacity").Args!).opacity);
        foreach (var stroke in indicator.Where(c => c.Kind is "DrawLines" or "DrawLine").Select(c => (dynamic)c.Args!))
        {
            Assert.Equal(figure.Theme.Neutral, (Color)stroke.color);
            Assert.Equal(1.0, (double)stroke.thickness);
            Assert.Equal(LineStyle.Solid, (LineStyle)stroke.style);
        }
    }

    [Fact]
    public void ItsColourOpacityWidthAndStyle_ReachEveryLine()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(1, 3).SetYLim(1, 3),
            zoom: z => { z.Color = Colors.Red; z.Alpha = 0.8; z.LineWidth = 2; z.LineStyle = LineStyle.Dashed; });

        var indicator = Indicator(DrawAxes(figure))!;

        Assert.Equal(0.8, (double)((dynamic)indicator.First(c => c.Kind == "SetOpacity").Args!).opacity);
        var strokes = indicator.Where(c => c.Kind is "DrawLines" or "DrawLine").Select(c => (dynamic)c.Args!).ToList();
        Assert.Equal(3, strokes.Count);
        foreach (var stroke in strokes)
        {
            Assert.Equal(Colors.Red, (Color)stroke.color);
            Assert.Equal(2.0, (double)stroke.thickness);
            Assert.Equal(LineStyle.Dashed, (LineStyle)stroke.style);
        }
    }

    [Fact]
    public void AnIndicatorWithoutALine_DrawsNothing()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(1, 3).SetYLim(1, 3),
            zoom: z => z.LineStyle = LineStyle.None);

        Assert.Null(Indicator(DrawAxes(figure)));
    }

    /// <summary>The indicator stands over the parent's own marks, as matplotlib's (zorder 4.99 over the lines' 2).</summary>
    [Fact]
    public void TheIndicator_IsDrawnAfterTheParentsMarks()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(1, 3).SetYLim(1, 3));

        var ctx = DrawAxes(figure);

        int parentLine = ctx.Calls.FindIndex(c => c.Kind == "DrawLines");
        int indicator = ctx.Calls.FindIndex(c => c.Kind == "BeginGroup" && (string)((dynamic)c.Args!).cssClass == Group);
        Assert.InRange(parentLine, 0, indicator - 1);
    }

    // ---- one geometry on every path -------------------------------------------------------------------------

    /// <summary>The SVG writer lays the subplots out in parallel and <see cref="ChartRenderer.Render"/> (PNG, PDF, the native
    /// controls) serially; both reach the indicator through the one render pass, so both draw it at the same place.</summary>
    [Fact]
    public void TheSvgAndTheRendererPath_DrawTheSameIndicator()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([2.0, 4.0], [3.0, 5.0]));
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        var recorded = Indicator(ctx)!;

        var group = Regex.Match(figure.ToSvg(), $"<g class=\"{Group}\">(.*?)</g>", RegexOptions.Singleline);
        Assert.True(group.Success, "the SVG holds no zoom indicator");
        var polyline = Regex.Match(group.Value, "<polyline points=\"([^\"]+)\"");
        var svgOutline = polyline.Groups[1].Value.Split(' ').Select(pair => pair.Split(','))
            .Select(xy => new Point(double.Parse(xy[0], CultureInfo.InvariantCulture), double.Parse(xy[1], CultureInfo.InvariantCulture)))
            .ToArray();
        var svgLines = Regex.Matches(group.Value, "<line x1=\"([^\"]+)\" y1=\"([^\"]+)\" x2=\"([^\"]+)\" y2=\"([^\"]+)\"")
            .Select(m => (From: new Point(Number(m.Groups[1].Value), Number(m.Groups[2].Value)),
                          To: new Point(Number(m.Groups[3].Value), Number(m.Groups[4].Value))))
            .ToArray();

        var outline = Outline(recorded);
        Assert.Equal(outline.Length, svgOutline.Length);
        for (int i = 0; i < outline.Length; i++)
        {
            Assert.Equal(outline[i].X, svgOutline[i].X, 0.01);
            Assert.Equal(outline[i].Y, svgOutline[i].Y, 0.01);
        }

        var connectors = Connectors(recorded);
        Assert.Equal(2, connectors.Length);
        Assert.Equal(connectors.Length, svgLines.Length);
        for (int i = 0; i < connectors.Length; i++)
            AssertConnector(svgLines[i], connectors[i].From, connectors[i].To, precision: 0.01);
    }

    private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    /// <summary>Read back from its JSON, the figure draws the same picture, indicator included.</summary>
    [Fact]
    public void AFigureReadBackFromJson_DrawsTheSameIndicator()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([2.0, 4.0], [3.0, 5.0]),
            zoom: z => { z.Color = Colors.Red; z.Alpha = 0.7; z.LineWidth = 1.5; z.LineStyle = LineStyle.Dotted; z.Connectors = false; });
        var serializer = new ChartSerializer();

        var read = serializer.FromJson(serializer.ToJson(figure));

        string svg = figure.ToSvg();
        Assert.Contains(Group, svg);
        Assert.Equal(svg, read.ToSvg());
    }

    [Fact]
    public void AnInsetWithoutAnIndicator_WritesNoZoomIndicatorField()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0])
            .AddInset(0.55, 0.05, 0.4, 0.4, i => i.Plot([1.0, 3.0], [1.0, 3.0]))).Build();

        string json = new ChartSerializer().ToJson(figure);

        Assert.DoesNotContain("zoomIndicator", json);
    }

    /// <summary>An indicator at its defaults is a setting by being there, so it is written as an empty object and read back
    /// as an indicator, not left out.</summary>
    [Fact]
    public void AnIndicatorAtItsDefaults_IsWrittenAsAnEmptyObject_AndReadBack()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]));
        var serializer = new ChartSerializer();

        string json = serializer.ToJson(figure);
        var read = serializer.FromJson(json).SubPlots[0].Insets[0].ZoomIndicator;

        Assert.Matches("\"zoomIndicator\"\\s*:\\s*\\{\\s*\\}", json);
        Assert.NotNull(read);
        Assert.Null(read.Color);
        Assert.Equal(0.5, read.Alpha);
        Assert.Equal(1.0, read.LineWidth);
        Assert.Equal(LineStyle.Solid, read.LineStyle);
        Assert.True(read.Connectors);
    }

    // ---- the model and the builder ---------------------------------------------------------------------------

    [Fact]
    public void ANewIndicator_HasMatplotlibsDefaults()
    {
        var zoom = new ZoomIndicator();

        Assert.Null(zoom.Color);
        Assert.Equal(0.5, zoom.Alpha);
        Assert.Equal(1.0, zoom.LineWidth);
        Assert.Equal(LineStyle.Solid, zoom.LineStyle);
        Assert.True(zoom.Connectors);
        Assert.Null(new Axes().ZoomIndicator);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void AnOpacityOutsideZeroToOne_IsRefused(double alpha)
    {
        var zoom = new ZoomIndicator();

        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => zoom.Alpha = alpha);

        Assert.Equal("value", refusal.ParamName);
        Assert.Equal(0.5, zoom.Alpha);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ALineWidthThatIsNegativeOrNotFinite_IsRefused(double width)
    {
        var zoom = new ZoomIndicator();

        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => zoom.LineWidth = width);

        Assert.Equal("value", refusal.ParamName);
        Assert.Equal(1.0, zoom.LineWidth);
    }

    [Fact]
    public void IndicateZoom_OnTheInsetsBuilder_SetsTheInsetsIndicator_AndReturnsTheBuilder()
    {
        AxesBuilder? returned = null, builder = null;
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0])
            .AddInset(0.5, 0.1, 0.3, 0.3, i => { builder = i; returned = i.IndicateZoom(z => z.LineWidth = 3); })).Build();

        var zoom = figure.SubPlots[0].Insets[0].ZoomIndicator;
        Assert.Same(builder, returned);
        Assert.NotNull(zoom);
        Assert.Equal(3, zoom.LineWidth);
        Assert.Null(figure.SubPlots[0].ZoomIndicator);
    }

    [Fact]
    public void IndicateZoom_WithoutConfigure_SetsAnIndicatorAtItsDefaults()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0])
            .AddInset(0.5, 0.1, 0.3, 0.3, i => i.IndicateZoom())).Build();

        var zoom = figure.SubPlots[0].Insets[0].ZoomIndicator;
        Assert.NotNull(zoom);
        Assert.Equal(0.5, zoom.Alpha);
    }

    /// <summary>Only an inset has a parent to draw its zoom on; asked of a subplot the verb refuses rather than set a value
    /// that draws nothing.</summary>
    [Fact]
    public void IndicateZoom_OnASubplotThatIsNoInset_IsRefused()
    {
        var refusal = Assert.Throws<InvalidOperationException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0]).IndicateZoom()).Build());

        Assert.Contains("inset", refusal.Message, StringComparison.OrdinalIgnoreCase);
    }
}
