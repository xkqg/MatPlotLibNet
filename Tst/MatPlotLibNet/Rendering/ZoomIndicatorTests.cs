// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
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

    /// <summary>The group of the zoom indicator the picture draws; the test fails when it holds none.</summary>
    private static XElement ZoomGroup(string svg)
    {
        var group = svg.Each("g").FirstOrDefault(element => (string?)element.Attribute("class") == Group);
        Assert.True(group is not null, "the SVG holds no zoom indicator");
        return group!;
    }

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
        int begin = ctx.Calls.FindIndex(c => c is BeginGroupCall k && k.CssClass == Group);
        if (begin < 0) return null;
        int end = ctx.Calls.FindIndex(begin, c => c is EndGroupCall);
        return ctx.Calls.GetRange(begin + 1, end - begin - 1);
    }

    private static Point[] Outline(List<CallRecord> indicator) =>
        indicator.OfType<DrawLinesCall>().Single().Points;

    private static (Point From, Point To)[] Connectors(List<CallRecord> indicator) =>
        [.. indicator.OfType<DrawLineCall>().Select(c => (c.P1, c.P2))];

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

    /// <summary>Two connectors, the first from <paramref name="firstOnInset"/> to <paramref name="firstOnRegion"/>, the second likewise.</summary>
    private static void AssertTwoConnectors((Point From, Point To)[] connectors, Point firstOnInset, Point firstOnRegion, Point secondOnInset, Point secondOnRegion)
    {
        Assert.Equal(2, connectors.Length);
        AssertConnector(connectors[0], onInset: firstOnInset, onRegion: firstOnRegion);
        AssertConnector(connectors[1], onInset: secondOnInset, onRegion: secondOnRegion);
    }

    /// <summary>The inset's frame stands at 120..280 both ways and the region at 380..460: the lower-left and the upper-right connector.</summary>
    private static void AssertTheLowerLeftAndUpperRightConnectors((Point From, Point To)[] connectors) =>
        AssertTwoConnectors(connectors, new Point(120, 280), new Point(380, 460), new Point(280, 120), new Point(460, 380));

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
        AssertTwoConnectors(connectors, new Point(320, 120), new Point(140, 380),
            new Point(480, 280), new Point(220, 460));
    }

    /// <summary>The inset stands up and to the left of its region: [LL, UL, LR, UR] = [T, F, F, T].</summary>
    [Fact]
    public void AnInsetUpLeftOfItsRegion_DrawsTheLowerLeftAndUpperRightConnectors()
    {
        var figure = WithInset(new InsetBounds(0.05, 0.05, 0.4, 0.4), i => i.Plot([7.0, 9.0], [1.0, 3.0]).SetXLim(7, 9).SetYLim(1, 3));

        var connectors = Connectors(Indicator(DrawAxes(figure))!);

        // The inset's frame: 120..280 both ways; the region 380..460 both ways.
        AssertTheLowerLeftAndUpperRightConnectors(connectors);
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
        AssertTheLowerLeftAndUpperRightConnectors(connectors);
    }

    /// <summary>On a parent whose Y axis runs backwards the region of y 1..3 lands near the top, and matplotlib's
    /// <c>ey = 1 - ey</c> keeps each connector on the corner where the region is drawn: the inset beside and below its region keeps
    /// the lower-left and the upper-left connector.</summary>
    [Fact]
    public void OnAParentWhoseYRunsBackwards_TheConnectorsFollowWhereTheRegionIsDrawn()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(1, 3).SetYLim(1, 3),
            parent: ax => ax.SetXLim(0, 10).SetYLim(10, 0));

        var indicator = Indicator(DrawAxes(figure))!;
        var connectors = Connectors(indicator);

        // y 1..3 -> 140..220 down the plot (0 at the top); x 1..3 -> 140..220.
        AssertOutline(Outline(indicator), left: 140, top: 140, right: 220, bottom: 220);
        AssertTwoConnectors(connectors, new Point(320, 280), new Point(140, 220),
            new Point(320, 120), new Point(140, 140));
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

    /// <summary>An inset wholly inside its region, and a region wholly inside its inset, draw the rectangle and no connector.</summary>
    [Theory]
    [InlineData(0.4, 0.2, 0.5, 9.5, 120, 480)]
    [InlineData(0.1, 0.8, 4.0, 6.0, 260, 340)]
    public void AnInsetAndItsRegion_OneWhollyInsideTheOther_DrawTheRectangleAndNoConnector(
        double inset, double insetSize, double low, double high, double near, double far)
    {
        var figure = WithInset(new InsetBounds(inset, inset, insetSize, insetSize), i => i.Plot([low, high], [low, high]).SetXLim(low, high).SetYLim(low, high));

        var indicator = Indicator(DrawAxes(figure))!;

        AssertOutline(Outline(indicator), left: near, top: near, right: far, bottom: far);
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

    // ---- a corner the parent's scale cannot place -----------------------------------------------------------

    /// <summary>The figure of the log cases: a parent whose X is a log axis over 1 to 100 (200 pixels a decade from x = 100) and
    /// an inset whose X limits run from -1 to 2.</summary>
    private static Figure OnALogParent() =>
        WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([-1.0, 2.0], [1.0, 3.0]).SetXLim(-1, 2).SetYLim(1, 3),
            parent: ax => ax.SetXScale(AxisScale.Log).SetXLim(1, 100).SetYLim(0, 10));

    /// <summary>A limit at or below zero on a log parent stands where matplotlib's log scale puts it, a thousand decades below the
    /// axis: <c>LogTransform</c> with <c>nonpositive='clip'</c> (the default of <c>set_xscale('log')</c>) writes <c>out[values &lt;= 0]
    /// = -1000</c>, and the indicator's rectangle is not clipped (<c>clip_on False</c>). Printed, matplotlib 3.11.2: a parent over
    /// x 1..100 at (75, 44, 465, 308) and an inset over -1..2 give the rectangle's vertices x = -232425.0 and 144.99 (75 + 465 * -1000
    /// / 2, and log10 2 of the way), and the connectors [LL, UL, LR, UR] visible = [False, True, True, False]: the rectangle runs off
    /// the left of the figure and two of its lines with it.</summary>
    [Fact]
    public void ACornerAtOrBelowZeroOnALogParent_StandsAThousandDecadesBelowTheAxis_AsMatplotlibClipsIt()
    {
        var indicator = Indicator(DrawAxes(OnALogParent()))!;

        double left = 100 + 200 * -1000.0, right = 100 + 200 * Math.Log10(2);
        AssertOutline(Outline(indicator), left: left, top: 380, right: right, bottom: 460, precision: 1e-6);
        var connectors = Connectors(indicator);
        Assert.Equal(2, connectors.Length);
        AssertConnector(connectors[0], onInset: new Point(320, 120), onRegion: new Point(left, 380), precision: 1e-6);
        AssertConnector(connectors[1], onInset: new Point(480, 280), onRegion: new Point(right, 460), precision: 1e-6);
    }

    /// <summary>A logit parent masks a limit outside 0 to 1 (<c>LogitTransform</c> with matplotlib's default <c>nonpositive='mask'</c>
    /// gives NaN), and matplotlib draws what is left: printed, matplotlib 3.11.2, a logit parent over 0.01 .. 0.99 and an inset over
    /// -0.1 .. 0.5 give the rectangle's vertices [nan, (307.5, 114), (307.5, 170), nan, nan] (its right side alone) and the connectors
    /// [LL, UL, LR, UR] visible = [False, True, False, True], the upper left one ending at the masked corner and so drawing nothing.</summary>
    [Fact]
    public void ACornerALogitParentMasks_LeavesTheSidesAndLinesThatEndThere_Undrawn()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.55, 0.4, 0.4), i => i.Plot([0.0, 0.5], [2.0, 4.0]).SetXLim(-0.1, 0.5).SetYLim(2, 4),
            parent: ax => ax.SetXScale(AxisScale.Logit).SetXLim(0.01, 0.99).SetYLim(0, 10));

        var indicator = Indicator(DrawAxes(figure))!;

        var side = Assert.Single(indicator.OfType<DrawLinesCall>());
        var points = side.Points;
        Assert.Equal(2, points.Length);
        Assert.Equal(points[0].X, points[1].X);
        Assert.Equal(300, points[0].X, 1e-9);
        Assert.Equal([340.0, 420.0], points.Select(p => p.Y).Order());
        var connector = Assert.Single(Connectors(indicator));
        AssertConnector(connector, onInset: new Point(480, 320), onRegion: points.MinBy(p => p.Y));
    }

    /// <summary>With one corner placed, no side has two ends to join, so the outline draws nothing; the rule keeps the upper left
    /// and lower right lines (every comparison with a masked corner is false), and both end at a masked corner, so neither is
    /// drawn.</summary>
    [Fact]
    public void WithOneCornerPlaced_NoSideAndNoLineToAMaskedCornerIsDrawn()
    {
        var nowhere = new Point(double.NaN, double.NaN);
        var path = new ChartRenderer.ZoomIndicatorPath(new ChartRenderer.ZoomIndicatorPath.ZoomCorners(nowhere, nowhere, nowhere, new Point(200, 300)), new Rect(320, 120, 160, 160));
        var ctx = new RecordingRenderContext();

        path.Draw(ctx, new ZoomIndicator(), Theme.MatplotlibV2);

        Assert.Equal(2, path.Connectors.Count);
        Assert.Equal(0, ctx.Count<DrawLinesCall>());
        Assert.Equal(0, ctx.Count<DrawLineCall>());
    }

    /// <summary>A parent that places no corner at all (a logit parent over limits all outside 0 to 1) draws no indicator.</summary>
    [Fact]
    public void AParentThatPlacesNoCorner_DrawsNoIndicator()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.55, 0.4, 0.4), i => i.Plot([2.0, 3.0], [2.0, 4.0]).SetXLim(2, 3).SetYLim(2, 4),
            parent: ax => ax.SetXScale(AxisScale.Logit).SetXLim(0.01, 0.99).SetYLim(0, 10));

        Assert.Null(Indicator(DrawAxes(figure)));
    }

    /// <summary>Read back from its JSON, the figure draws the indicator of a log parent the same, a thousand decades out.</summary>
    [Fact]
    public void AFigureReadBackFromJson_DrawsTheIndicatorOfALogParentTheSame()
    {
        var figure = OnALogParent();

        var read = figure.RoundTripped();

        Assert.Equal(Outline(Indicator(DrawAxes(figure))!), Outline(Indicator(DrawAxes(read))!));
        Assert.Equal(figure.ToSvg(), read.ToSvg());
        var polyline = ZoomGroup(figure.ToSvg()).Each("polyline").First();
        Assert.True(polyline.Attribute("points")!.Value.Split(' ', ',').Select(n => n.Number()).Min() < -100_000, "the left side stands a thousand decades out");
    }

    /// <summary>A 3-D parent draws no indicator: matplotlib 3.11.2 adds the rectangle of an <c>indicate_inset_zoom</c> on an
    /// <c>Axes3D</c> through the flat 2-D <c>transData</c> of the axes, not through its projection (printed: the limits 0.2 to 0.4 of
    /// an inset land at the pixels (812, 722) to (892, 802) whatever the 3-D data show), so there is no region of the data to mark.
    /// The inset itself is still drawn.</summary>
    [Fact]
    public void AThreeDParent_DrawsNoIndicator()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(1, 3).SetYLim(1, 3));
        figure.SubPlots[0].CoordinateSystem = CoordinateSystem.ThreeD;

        var ctx = DrawAxes(figure);

        Assert.Null(Indicator(ctx));
        Assert.Contains(ctx.Of<DrawRectangleCall>(), c => c.Rect == new Rect(320, 120, 160, 160));
    }

    /// <summary>A polar parent draws the region through its own transform, as matplotlib's <c>InsetIndicator</c> does: the
    /// rectangle is a <c>Rectangle</c> patch in the parent's data (x the angle, y the radius), and matplotlib 3.11.2 maps a path
    /// with one interpolation step vertex by vertex (<c>PolarTransform.transform_path_non_affine</c>: <c>if path._interpolation_steps
    /// == 1: return Path(self.transform_non_affine(path.vertices), path.codes)</c>), so the region is the four corners mapped by
    /// angle and radius, joined straight. Each connector runs from a corner of the inset to the corner of the region that
    /// <c>(x0 + ex * w, y0 + ey * h)</c> maps to, and matplotlib keeps them by its rule over the mapped lower left, upper left and
    /// lower right corners: printed for an inset at [0.65, 0.65, 0.3, 0.3] of a polar axes of radius 5 over the angles 3.0 to 3.3
    /// and the radii 2 to 3, [LL, UL, LR, UR] visible = [False, True, True, False].</summary>
    [Fact]
    public void OnAPolarParent_TheRegionIsTheLimitsMappedByAngleAndRadius_WithMatplotlibsConnectors()
    {
        var figure = WithInset(new InsetBounds(0.65, 0.05, 0.3, 0.3), i => i.Plot([3.0, 3.3], [2.0, 3.0]).SetXLim(3.0, 3.3).SetYLim(2, 3),
            parent: ax => ax.SetYLim(0, 5));
        figure.SubPlots[0].CoordinateSystem = CoordinateSystem.Polar;
        var polar = new PolarTransform(ParentArea, 5);
        Point At(double theta, double r) => polar.PolarToPixel(r, theta);

        var indicator = Indicator(DrawAxes(figure))!;

        // The inset's frame: 360..480 across, 120..240 down.
        var outline = Outline(indicator);
        Point[] expected = [At(3.0, 3), At(3.3, 3), At(3.3, 2), At(3.0, 2), At(3.0, 3)];
        Assert.Equal(expected.Length, outline.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].X, outline[i].X, 1e-9);
            Assert.Equal(expected[i].Y, outline[i].Y, 1e-9);
        }

        var connectors = Connectors(indicator);
        Assert.Equal(2, connectors.Length);
        AssertConnector(connectors[0], onInset: new Point(360, 120), onRegion: At(3.0, 3));
        AssertConnector(connectors[1], onInset: new Point(480, 240), onRegion: At(3.3, 2));
    }

    /// <summary>A ternary parent draws the region in the triangle's own frame, one unit wide and the height of the triangle, where
    /// it draws its patches: the limits of the inset are a rectangle of that frame.</summary>
    [Fact]
    public void OnATernaryParent_TheRegionStandsInTheFrameOfTheTriangle()
    {
        var figure = WithInset(new InsetBounds(0.6, 0.05, 0.3, 0.3), i => i.Plot([0.2, 0.4], [0.1, 0.3]).SetXLim(0.2, 0.4).SetYLim(0.1, 0.3));
        figure.SubPlots[0].CoordinateSystem = CoordinateSystem.Ternary;
        var frame = new DataTransform(0, 1, 0, TernaryFrame.Height, ParentArea);
        var low = frame.DataToPixel(0.2, 0.1);
        var high = frame.DataToPixel(0.4, 0.3);

        var indicator = Indicator(DrawAxes(figure))!;

        AssertOutline(Outline(indicator), left: low.X, top: high.Y, right: high.X, bottom: low.Y, precision: 1e-9);
        Assert.Equal(2, Connectors(indicator).Length);
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
        var view = new ChartRenderer.DrawnView(area, new DataTransform(0, 1, 0, 1, area));

        Assert.Null(ChartRenderer.ZoomIndicatorPath.Of(new ChartRenderer.DrawnView(area, null), inset, view));
        Assert.Null(ChartRenderer.ZoomIndicatorPath.Of(view, inset, new ChartRenderer.DrawnView(area, null)));
        Assert.NotNull(ChartRenderer.ZoomIndicatorPath.Of(view, inset, view));
    }

    /// <summary>Each corner of the region is placed: a limit at or below zero on the parent's log axes, at either end of either
    /// axis (a limit set high to low puts the low one last), stands a thousand decades below that axis (here 50 pixels a decade
    /// from the left edge, from the bottom edge 100).</summary>
    [Theory]
    [InlineData(-1.0, 2.0, 1.0, 2.0)]
    [InlineData(2.0, -1.0, 1.0, 2.0)]
    [InlineData(1.0, 2.0, -1.0, 2.0)]
    [InlineData(1.0, 2.0, 2.0, -1.0)]
    public void ALimitAtOrBelowZeroOnALogParent_AtAnyCorner_StandsAThousandDecadesOut(double xMin, double xMax, double yMin, double yMax)
    {
        var parent = new Axes();
        var inset = parent.AddInset(0.5, 0.1, 0.3, 0.3);
        var area = new Rect(0, 0, 100, 100);
        var logParent = new ChartRenderer.DrawnView(area, area.LogLog());
        var insetView = new ChartRenderer.DrawnView(new Rect(50, 10, 30, 30), new DataTransform(xMin, xMax, yMin, yMax, new Rect(50, 10, 30, 30)));

        var region = ChartRenderer.ZoomIndicatorPath.Of(logParent, inset, insetView)!.Value.Region;

        Point[] corners = [region.LowerLeft, region.UpperLeft, region.LowerRight, region.UpperRight];
        Assert.All(corners, corner => Assert.True(corner.IsDrawable()));
        if (Math.Min(xMin, xMax) < 0) Assert.Equal(-50_000, region.LowerLeft.X, 1e-6);
        else Assert.Equal(100 + 50_000, region.LowerLeft.Y, 1e-6);
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

        Assert.Equal(0.5, indicator.OfType<SetOpacityCall>().First().Opacity);
        Assert.Equal(1.0, indicator.OfType<SetOpacityCall>().Last().Opacity);
        foreach (var stroke in indicator.OfType<StrokeCall>())
        {
            Assert.Equal(figure.Theme.Neutral, stroke.Color);
            Assert.Equal(1.0, stroke.Thickness);
            Assert.Equal(LineStyle.Solid, stroke.Style);
        }
    }

    [Fact]
    public void ItsColourOpacityWidthAndStyle_ReachEveryLine()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([1.0, 3.0], [1.0, 3.0]).SetXLim(1, 3).SetYLim(1, 3),
            zoom: z => { z.Color = Colors.Red; z.Alpha = 0.8; z.LineWidth = 2; z.LineStyle = LineStyle.Dashed; });

        var indicator = Indicator(DrawAxes(figure))!;

        Assert.Equal(0.8, indicator.OfType<SetOpacityCall>().First().Opacity);
        var strokes = indicator.OfType<StrokeCall>().ToList();
        Assert.Equal(3, strokes.Count);
        foreach (var stroke in strokes)
        {
            Assert.Equal(Colors.Red, stroke.Color);
            Assert.Equal(2.0, stroke.Thickness);
            Assert.Equal(LineStyle.Dashed, stroke.Style);
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

        int parentLine = ctx.Calls.FindIndex(c => c is DrawLinesCall);
        int indicator = ctx.Calls.FindIndex(c => c is BeginGroupCall k && k.CssClass == Group);
        Assert.InRange(parentLine, 0, indicator - 1);
    }

    /// <summary>The indicator stands under the inset, as matplotlib's: <c>indicate_inset_zoom</c> returns an InsetIndicator at
    /// zorder 4.99 and the inset axes stands at 5 (matplotlib 3.11.2: <c>ins.get_zorder()</c> 5, <c>ind.get_zorder()</c> 4.99), so
    /// the inset's background covers whatever part of the region it overlaps. The whole indicator is drawn before the inset's
    /// first mark, its background.</summary>
    [Theory]
    [InlineData("default")]
    [InlineData("v2")]
    [InlineData("classic")]
    public void TheIndicator_IsDrawnBeforeTheInset(string theme)
    {
        var figure = WithInset(new InsetBounds(0.4, 0.4, 0.3, 0.3), i => i.Plot([4.0, 6.0], [4.0, 6.0]).SetXLim(3, 7).SetYLim(3, 7));
        figure.Theme = theme.AsTheme();

        var ctx = DrawAxes(figure);

        // The inset's frame: 260..380 both ways; its background is the first thing its render draws.
        int insetBackground = ctx.Calls.FindIndex(c => c is DrawRectangleCall k && k.Rect == new Rect(260, 260, 120, 120));
        int begin = ctx.Calls.FindIndex(c => c is BeginGroupCall k && k.CssClass == Group);
        int end = ctx.Calls.FindIndex(begin, c => c is EndGroupCall);
        Assert.True(begin >= 0, "no indicator was drawn");
        Assert.True(insetBackground > end, $"the indicator ends at call {end}, after the inset's background at {insetBackground}");
    }

    // ---- one geometry on every path -------------------------------------------------------------------------

    /// <summary>The SVG writer lays the subplots out in its own loop and <see cref="ChartRenderer.Render"/> (PNG, PDF, the native
    /// controls) serially; both reach the indicator through the one render pass, so both draw it at the same place.</summary>
    [Fact]
    public void TheSvgAndTheRendererPath_DrawTheSameIndicator()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([2.0, 4.0], [3.0, 5.0]));
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        var recorded = Indicator(ctx)!;

        var group = ZoomGroup(figure.ToSvg());
        var svgOutline = group.Each("polyline").First().Attribute("points")!.Value.Split(' ').Select(pair => pair.Split(','))
            .Select(xy => new Point(xy[0].Number(), xy[1].Number()))
            .ToArray();
        var svgLines = group.Each("line")
            .Select(line => (From: new Point(line.Number("x1"), line.Number("y1")), To: new Point(line.Number("x2"), line.Number("y2"))))
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

    /// <summary>Read back from its JSON, the figure draws the same picture, indicator included.</summary>
    [Fact]
    public void AFigureReadBackFromJson_DrawsTheSameIndicator()
    {
        var figure = WithInset(new InsetBounds(0.55, 0.05, 0.4, 0.4), i => i.Plot([2.0, 4.0], [3.0, 5.0]),
            zoom: z => { z.Color = Colors.Red; z.Alpha = 0.7; z.LineWidth = 1.5; z.LineStyle = LineStyle.Dotted; z.Connectors = false; });

        var read = figure.RoundTripped();

        string svg = figure.ToSvg();
        Assert.Contains(Group, svg);
        Assert.Equal(svg, read.ToSvg());
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("AnInsetWithoutAnIndicator_WritesNoZoomIndicatorField", () =>
        {
            var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0])
                .AddInset(0.55, 0.05, 0.4, 0.4, i => i.Plot([1.0, 3.0], [1.0, 3.0]))).Build();
            return new ChartSerializer().ToJson(figure);
        }) { Absent = ["zoomIndicator"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

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

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void AnOpacityOutsideZeroToOne_IsRefused(double alpha)
    {
        var zoom = new ZoomIndicator();

        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => zoom.Alpha = alpha);

        Assert.Equal("Alpha", refusal.ParamName);
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

        Assert.Equal(nameof(ZoomIndicator.LineWidth), refusal.ParamName);
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
