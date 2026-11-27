// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders axes using the Polar (r, theta) coordinate system with circular grid.</summary>
public sealed class PolarAxesRenderer : AxesRenderer
{
    /// <summary>Initializes a new Polar axes renderer.</summary>
    public PolarAxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : base(axes, plotArea, ctx, theme) { }

    /// <inheritdoc />
    /// <remarks>Every polar series, hidden ones too, each through the radial scale it is drawn on (<see cref="ScaleOfTheAxes"/>,
    /// the one <see cref="Render"/> draws with); nothing is drawn.</remarks>
    internal override MarkObstacles LegendObstacles() =>
        CollectMarks(Axes.Series, SeriesPass.UnitSquare(PlotArea), new(), radial: ScaleOfTheAxes(PatchReach())).Build();

    /// <summary>The ONE radial scale of these axes: the grid, every series and every patch are drawn on it, and so are the marks a
    /// legend keeps clear of. Worked out from the series, the patches that range the radius (<see cref="PatchReach"/>, handed in so it
    /// is worked out once a draw), the limit the radial axis was given and the room the rings have.</summary>
    /// <param name="patchReach">What <see cref="PatchReach"/> gives.</param>
    private RadialScale ScaleOfTheAxes(RadialExtent? patchReach) =>
        RadialScale.For(Axes.Series, Axes.YAxis, RoundFrame.In(PlotArea).Radius, TickFont().Size, Theme, Axes.BarMode, patchReach,
            Axes.ScatterMarginIn(Theme), Axes.ROrigin);

    /// <summary>The radii the patches of these axes reach (the extremes of their outlines, a curve's turning points included, as
    /// matplotlib's <c>_update_patch_limits</c> takes them), where they range the radius: always under the library's own themes;
    /// under the matplotlib themes (<see cref="AxisRangeLook.PatchesLikeAddPatch"/>) only beside a series that ranges it, as
    /// <c>add_patch</c> asks for no range (printed: a line to r 5 beside a rectangle to r 8 gives rlim (0, 8.35); the rectangle alone
    /// leaves (0, 1)). <see langword="null"/> when none does.</summary>
    private RadialExtent? PatchReach()
    {
        if (Axes.Patches.Count == 0) return null;
        if (!Theme.Looks.Range.PatchesRange(asked: Axes.Series.Any(series => series is ChartSeries { RadialExtent: not null }))) return null;

        double low = double.PositiveInfinity, high = double.NegativeInfinity;
        foreach (var patch in Axes.Patches)
            foreach (var point in patch.Outline().ExtremePoints())
            {
                low = Math.Min(low, point.Y);
                high = Math.Max(high, point.Y);
            }
        return low <= high ? new RadialExtent(low, high, Sticky: false) : null;
    }

    /// <summary>The transform of the last render, which the patches are placed by (<see cref="DrawLayerAt"/>).</summary>
    private PolarTransform? _polar;

    /// <summary>Where the patches of the last render are cut off: the disc or ring of the axes where a patch can reach past it (a
    /// radial limit the caller set, or patches that take no part in the range); <see langword="null"/> where every patch lies on the
    /// scale.</summary>
    private ClipRegion? _patchClip;

    /// <inheritdoc />
    /// <remarks>Angle and radius through the transform of the last render, so a Cartesian inset's zoom indicator stands on the
    /// polar axes.</remarks>
    internal override IDataPlacement? DrawnPlacement => _polar is { } polar ? new PolarPlacement(polar) : null;

    /// <inheritdoc />
    /// <remarks>A polar axes draws its patches through its transform, x the angle and y the radius
    /// (<see cref="PolarPlacement"/>).</remarks>
    internal override bool DrawsPatches => true;

    /// <inheritdoc />
    /// <remarks>The grid (<see cref="DrawGrid"/>) or a patch: each vertex and control point of a patch mapped by angle and radius and
    /// joined as the outline joins them, as matplotlib maps a path of one interpolation step
    /// (<c>PolarTransform.transform_path_non_affine</c>): a rectangle is the four corners joined straight.</remarks>
    private protected override int DrawLayerAt(in SeriesPass pass, ReadOnlySpan<LayerStep> steps, int at)
    {
        if (steps[at].Layer == DataLayer.Grid) DrawGrid();
        else new PatchDrawing(Axes, Ctx, Theme).Draw(steps[at].Index, new PolarPlacement(_polar!), _patchClip);
        return at;
    }

    /// <inheritdoc />
    public override void Render()
    {
        var patchReach = PatchReach();
        var scale = ScaleOfTheAxes(patchReach);
        double rMax = scale.Extent;
        var transform = new PolarTransform(PlotArea, scale.Min, rMax, scale.Origin);
        _polar = transform;
        var centre = new Point(transform.CenterX, transform.CenterY);
        double hole = transform.InnerRadius;

        // The marks of a polar axes are cut off at its disc by the rule of every axes (SeriesGroupExtensions.ClipsItsMarksUnder): the series
        // pass asks it for each series, the patches ask it as a patch, and a patch is cut off too when the patches take no part in the range.
        ClipRegion seriesClip = transform.Clip;
        _patchClip = Axes.Patches.Count > 0 && (patchReach is null || Axes.ClipsItsMarksUnder(Theme, MatplotlibArtist.Patch)) ? seriesClip : null;

        // The background is the circle the axes stands in, as matplotlib's polar axes draws its patch (a full wedge), not the
        // cell: under a theme whose figure is not the axes' colour the figure shows in the corners. An origin below the low end
        // leaves a hole at the centre, which the patch does not cover (the wedge's width).
        if (hole > 0)
            Ctx.DrawPath(transform.AxesOutline(), ShapeStyle.Filled(Theme.AxesBackground));
        else
            Ctx.DrawCircle(centre, transform.MaxRadius, ShapeStyle.Filled(Theme.AxesBackground));

        // The grid and its labels, the patches and the series, each where the theme puts it (DrawLayerAt).
        _scale = scale;
        RenderLayers(scale, seriesClip);

        // The circle is closed by the spine, over the series (matplotlib's spines stand at z-order 2.5, the lines at 2), and the
        // hole by the inner spine.
        DrawSpine(transform);
        if (hole > 0) DrawInnerSpine(transform, hole);

        // Legend
        RenderLegend();

        MeasuredTopTickBand = TitleRiseBand;
        RenderTitle();
    }

    /// <summary>The radial scale of the last render, which the grid is drawn on (<see cref="DrawGrid"/>).</summary>
    private RadialScale _scale;

    /// <summary>The grid at its step of the layered pass: the rings and the spokes, the axes' grid in the grid of the theme with the
    /// author's change on top (drawn unless the author switched it off), and their labels, which are not the grid and stay: matplotlib
    /// draws the theta and radial axes, grid and tick labels together, at the axis' z-order (1.5 under the default style, over the
    /// patches and the bars and under the lines; 2.5 under classic, over the lines; under everything under the library's own
    /// themes).</summary>
    private void DrawGrid()
    {
        var transform = _polar!;
        var scale = _scale;
        var centre = new Point(transform.CenterX, transform.CenterY);
        double hole = transform.InnerRadius;
        var grid = Axes.ShapeGridUnder(Theme);
        var labelFont = TickFont();

        // The rings stand at round numbers and are labelled along one spoke, at matplotlib's 22.5 degrees.
        double labelAngle = RadialScale.LabelAngleDegrees.ToRadians();
        for (int i = 0; i < scale.Rings.Length; i++)
        {
            double r = transform.PixelRadiusAt(scale.Rings[i]);
            if (grid.Visible)
                DrawRing(transform, r, grid);

            var onTheSpoke = transform.PolarToPixel(scale.Rings[i], labelAngle);
            Ctx.Draw(TextLabel.Of(scale.Labels[i]), labelFont, new TextPlacement(new Point(onTheSpoke.X + 3, onTheSpoke.Y - 3), TextAlignment.Left));
        }

        // The spokes, as matplotlib's ThetaLocator places them on a full circle: np.arange(8) * 2 * np.pi / 8, from the low end
        // of the scale (the edge of the hole, when there is one) out.
        var reach = SpokeLabelReach();
        // The line a spoke label is centred by, as the surface measures a line of the tick font: one measure a pass.
        var line = reach is null ? default : Ctx.LineExtentOf(labelFont);
        for (int i = 0; i < SpokeLabels.Length; i++)
        {
            double angle = SpokeAngle(i);
            var outer = transform.PolarToPixel(scale.Extent, angle);
            if (grid.Visible)
                Ctx.DrawLine(
                    hole > 0 ? transform.PolarToPixel(scale.Min, angle) : centre,
                    outer, grid.Stroke);

            // A centred label stands by the line of the tick font, not by its own ink, as matplotlib's spoke labels do.
            Ctx.Draw(TextLabel.Of(SpokeLabels[i]), labelFont, SpokeLabelAt(i, outer, centre, line, reach), line);
        }
    }

    /// <inheritdoc />
    /// <remarks>Above the square: the room the title rises into (<see cref="TitleRiseBand"/>), which the layout keeps so the
    /// title stays on the figure. The spoke labels round the circle are estimated by the layout as it always has.</remarks>
    internal override Layout.EdgeBands MeasureEndBands(SpineInsets insets) => new(TitleRiseBand, 0, 0, 0, Insets: insets);

    /// <summary>The band between the top of the square and the gap below the title, by the theme's polar look (<see cref="PolarLook"/>):
    /// <see cref="PolarLook.TitleRise"/> of the square's side, and where the title rises over the spoke labels
    /// (<see cref="TextLook.TitleOverWhatIsBelow"/>, matplotlib's automatic title) as far as the highest label reaches above the square. The
    /// one rule the drawing places the title by and the layout reserves by; the labels are placed by the rule they are drawn by
    /// (<see cref="SpokeLabelAt"/>).</summary>
    private double TitleRiseBand
    {
        get
        {
            var look = Theme.Looks.Polar;
            var frame = RoundFrame.In(PlotArea);
            double radius = frame.Radius;
            double band = look.TitleRise * 2 * radius;
            if (!Theme.Looks.Text.TitleOverWhatIsBelow || SpokeLabelReach() is not { } reach) return band;

            // Only a spoke on the upper half of the circle can carry its label above the square. The reach is read from the
            // circle's top, which is the square's: the drawing hands over the square, the layout the plot's width over its whole
            // cell, with the circle centred in it.
            var line = Ctx.LineExtentOf(TickFont());
            var centre = frame.Centre;
            double circleTop = centre.Y - radius, top = circleTop;
            for (int i = 0; i < SpokeLabels.Length; i++)
            {
                double angle = SpokeAngle(i), sin = Math.Sin(angle);
                if (sin <= 0) continue;
                var outer = centre.AtAngleCounterClockwise(radius, angle);
                top = Math.Min(top, CentredSpokeLabel(i, outer, reach, line).Bounds().Y);
            }

            return Math.Max(band, circleTop - top);
        }
    }

    /// <summary>The angle of spoke <paramref name="spoke"/>, in radians: every forty-five degrees from 0, as matplotlib's ThetaLocator
    /// places them on a full circle (<c>np.arange(8) * 2 * np.pi / 8</c>).</summary>
    private static double SpokeAngle(int spoke) => spoke * 2 * Math.PI / SpokeLabels.Length;


    /// <summary>Where the label of spoke <paramref name="spoke"/>, whose end stands at <paramref name="outer"/>, is drawn, by the
    /// theme's polar look: centred <see cref="PolarLook.ThetaLabelPad"/> past its tick's pad, as matplotlib's <c>ThetaTick</c> places it
    /// (<see cref="CentredSpokeLabel"/>); else the library's own place, eight pixels beside the end of the spoke, four above it or twelve
    /// below, starting away from the centre.</summary>
    private static TextPlacement SpokeLabelAt(int spoke, Point outer, Point centre, TextExtent line, double? reach)
    {
        if (reach is { } away)
            return CentredSpokeLabel(spoke, outer, away, line).Placement;

        return new TextPlacement(new Point(outer.X + (outer.X > centre.X ? 8 : -8), outer.Y + (outer.Y > centre.Y ? 12 : -4)),
            outer.X >= centre.X ? TextAlignment.Left : TextAlignment.Right);
    }

    /// <summary>The box of spoke <paramref name="spoke"/>'s label centred as matplotlib's <c>ThetaTick.update_position</c> centres it
    /// (<c>ha='center', va='center'</c>): <c>pad = self._pad + 7</c> points out from the end of its spoke along its angle, where
    /// <c>_pad</c> is the theta axis' tick pad and the part of its tick outside the circle (<see cref="TickConfig.OutsideLength"/>), the
    /// whole <paramref name="away"/> (<see cref="SpokeLabelReach"/>). Its baseline and its top read the ascent of <paramref name="line"/>,
    /// a line of the tick font as the surface measures it (<see cref="TextExtentExtensions"/>' <c>LineExtentOf</c>, the line a digit stands in): the
    /// box is the label's middle line.</summary>
    private static TextBox CentredSpokeLabel(int spoke, Point outer, double away, TextExtent line)
    {
        double angle = SpokeAngle(spoke);
        return new TextBox(new TextPlacement(outer.AtAngleCounterClockwise(away, angle), TextAlignment.Center)
            { Vertical = TextVerticalAlignment.Center }, line);
    }

    /// <summary>How far a spoke label's middle stands from the end of its spoke under the theme's polar look: the theta axis' tick pad, the
    /// part of its tick outside the circle and <see cref="PolarLook.ThetaLabelPad"/> (matplotlib's <c>pad = self._pad + 7</c>);
    /// <see langword="null"/> under a look that places the labels its own way. Asked once a pass.</summary>
    private double? SpokeLabelReach()
    {
        if (Theme.Looks.Polar.ThetaLabelPad is not { } extra) return null;
        var ticks = Axes.XAxis.MajorTicksUnder(Theme);
        return ticks.OutsideLength + ticks.Pad + extra;
    }

    /// <summary>The names of the eight spokes, every forty-five degrees from 0, as matplotlib's ThetaFormatter writes them on a
    /// full circle (whole degrees and the degree sign): the same texts at every draw, made once.</summary>
    private static readonly string[] SpokeLabels = [.. Enumerable.Range(0, 8).Select(i => string.Invariant($"{i * 45}°"))];

    /// <summary>Draws one ring of the grid at <paramref name="radius"/> pixels from the centre, in the style of the grid: a circle when
    /// solid, a closed polyline when dotted or dashed (<see cref="ClosedStrokeExtensions"/>), as the spines are.</summary>
    private void DrawRing(PolarTransform transform, double radius, GridStyle grid) =>
        Ctx.DrawCircleStroke(new Point(transform.CenterX, transform.CenterY), radius, grid.Stroke);

    /// <summary>Draws the spine that closes the circle, on its edge: matplotlib's <c>PolarAxes._gen_axes_spines</c> makes it
    /// <c>Spine.arc_spine(self, 'top', (0.5, 0.5), 0.5, 0, 360)</c>, a spine of the type 'top', so it is drawn as the top spine
    /// is configured (<see cref="SpinesConfig.Top"/>: shown or hidden, and its colour, width and dash pattern under the theme,
    /// by the rules every spine is drawn by). A solid spine is a circle; a dotted or dashed one a closed polyline
    /// (<see cref="ClosedStrokeExtensions"/>).</summary>
    private void DrawSpine(PolarTransform transform)
    {
        var spine = Axes.Spines.Top;
        if (spine.Visible) Ctx.DrawCircleStroke(new Point(transform.CenterX, transform.CenterY), transform.MaxRadius, spine.StrokeIn(Theme));
    }

    /// <summary>Draws the spine that bounds the hole an origin below the low end leaves: matplotlib's <c>spines['inner']</c> is
    /// <c>Spine.arc_spine(self, 'bottom', ...)</c>, shown while the hole has a width, so it is drawn as the bottom spine is
    /// configured (<see cref="SpinesConfig.Bottom"/>).</summary>
    private void DrawInnerSpine(PolarTransform transform, double hole)
    {
        var spine = Axes.Spines.Bottom;
        if (spine.Visible) Ctx.DrawCircleStroke(new Point(transform.CenterX, transform.CenterY), hole, spine.StrokeIn(Theme));
    }
}
