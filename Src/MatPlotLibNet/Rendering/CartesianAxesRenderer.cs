// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders axes using the standard Cartesian (X, Y) coordinate system.</summary>
/// <remarks>Not sealed so that inset axes can answer the end-label question differently
/// (<c>CartesianInsetAxesRenderer</c>); the drawing itself (<see cref="Render"/>) is sealed.</remarks>
public class CartesianAxesRenderer : AxesRenderer
{
    /// <summary>Initializes a new Cartesian axes renderer.</summary>
    public CartesianAxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : base(axes, plotArea, ctx, theme) { }

    /// <inheritdoc />
    /// <remarks>Cartesian axes draw end-of-line labels, so a series that asked for one leaves the legend here.</remarks>
    internal override bool DrawsDirectLabels => true;

    /// <summary>The end labels of the axes being rendered; placed by <see cref="Render"/> before the series pass.</summary>
    private CartesianParts.CartesianDirectLabelsPart? _directLabels;

    /// <summary>The part that draws the primary Y axis, left slot 0 of the container, in this render: built by
    /// <see cref="Render"/> before the ticks are drawn.</summary>
    private CartesianParts.CartesianYAxisSlotPart? _primaryYPart;

    /// <summary>The axes, plot area, context and theme every part of this render draws with.</summary>
    private CartesianParts.CartesianPartContext PartContext => new(Axes, PlotArea, Ctx, Theme);

    /// <inheritdoc />
    /// <remarks>Draws the series' end label, when it asked for one, inside the series' own group, whatever axis the series
    /// is plotted against.</remarks>
    internal override void DrawInsideSeriesGroup(int seriesIndex) => _directLabels!.DrawLabelOf(seriesIndex);

    /// <inheritdoc />
    /// <remarks>Through the part that draws every Y axis (<see cref="CartesianParts.CartesianYAxisSlotPart"/>), past the
    /// widest tick label it drew.</remarks>
    private protected override void RenderYAxisTitle() => _primaryYPart!.RenderTitle(MeasuredYTickMaxWidth);

    /// <summary>The band the mirrored Y tick labels took past the right spine in this render; 0 when none were drawn.
    /// The end labels start past it.</summary>
    private double _mirroredYTickBand;

    /// <summary>Whether any right-side slot is drawn: it has series. A drawn secondary axis owns the right edge.</summary>
    private bool DrawsSecondaryY => Axes.YAxes.Right.Any(s => s.Series.Count > 0);

    /// <summary>Whether a secondary X axis is drawn: it has series. A drawn secondary axis owns the top edge.</summary>
    private bool DrawsSecondaryX => Axes.SecondaryXAxis is not null && Axes.XSecondarySeries.Count > 0;

    /// <summary>Whether the primary X ticks are mirrored on the top edge: asked for, and no drawn secondary X axis owns
    /// that edge (matplotlib's <c>twiny</c> moves the host's ticks to the bottom the same way).</summary>
    private bool MirrorsXTicks => Axes.XAxis.MajorTicks.Mirror && !DrawsSecondaryX;

    /// <summary>Whether the primary Y ticks are mirrored on the right edge: asked for, and no drawn secondary Y axis owns
    /// that edge (matplotlib's <c>twinx</c> moves the host's ticks to the left the same way).</summary>
    private bool MirrorsYTicks => Axes.YAxis.MajorTicks.Mirror && !DrawsSecondaryY;

    /// <summary>Whether every series fills the plot without Cartesian axes (<see cref="ChartSeries.DrawsWithoutAxes"/>: a
    /// radar, a pie, a treemap, a table, a stat tile): then no grid, ticks or frame are drawn. A series the library does not
    /// know says nothing, so it keeps its axes.</summary>
    private bool IsAxisLess => Axes.Series.Count > 0 && Axes.Series.All(s => s is ChartSeries { DrawsWithoutAxes: true });

    /// <inheritdoc />
    /// <remarks>Measured with what <see cref="Render"/> draws them with: the same predicates, tick sources and values
    /// (the Y ticks for this renderer's plot height), formatter, label font and band rule
    /// (<see cref="AxisTickExtensions.TickLabelBand"/>). The left band is the primary Y axis' tick labels as the drawing
    /// places them, numbers and category names alike, so the layout reserves for the primary, left slot 0 of the
    /// container, the band it draws; the mirrored right band is the same labels past the right spine. Nothing is measured
    /// for axis-less series.</remarks>
    internal override Layout.EdgeBands MeasureSideBands()
    {
        var yMajor = Axes.YAxis.MajorTicks;
        if (IsAxisLess || !yMajor.Visible) return default;
        var range = ComputeDataRanges();
        var primary = PlacePrimary(range, ViewTransform(range, Axes.YAxis, Axes.YBreaks));
        double left = yMajor.TickLabelBand(Ctx.MeasureAxisTicks(primary.Ticks, yMajor, primary.TickDraw(Theme)));
        return new Layout.EdgeBands(0, MirrorsYTicks ? left : 0, left, 0);
    }

    /// <inheritdoc />
    /// <remarks>Measured with what <see cref="Render"/> draws them with: the same predicates, tick sources and values
    /// (the X ticks for this renderer's plot width), formatter, label font, rotation and band rule
    /// (<see cref="AxisTickExtensions.TickLabelBand"/>). The X tick labels turn when the drawing turns them
    /// (<see cref="PrimaryXDraw"/>: their own rotation, or 30 degrees when upright labels would collide at this plot's
    /// width), so the layout hands this renderer the width the plot is drawn at before it reads these bands. An upright
    /// tick label's height depends on its font only, so upright numbers and dates, and the upright mirrored top band, are
    /// measured on one label; category names and turned labels over the labels the drawing places, turned as drawn
    /// (<see cref="AxisTickExtensions.LabelExtent"/>). A drawn secondary X axis owns the top edge, so the top band is then
    /// its band (<see cref="CartesianParts.CartesianSecondaryXAxisPart.MeasureBand"/>), which it draws whatever the series.
    /// The bottom band is measured for every X axis that draws tick labels; the X axis title below them is measured by the
    /// rule <see cref="AxesRenderer.RenderAxisLabels"/> places it by (<see cref="AxisTickExtensions.BottomAxisTitleExtent"/>),
    /// at its fixed offset when no tick label is drawn, axis-less series included.</remarks>
    internal override Layout.EdgeBands MeasureEndBands()
    {
        double secondaryTop = DrawsSecondaryX
            ? CartesianParts.CartesianSecondaryXAxisPart.MeasureBand(Axes, Ctx, Theme, ComputeSecondaryXDataRanges(0, 1))
            : 0;
        if (IsAxisLess) return new Layout.EdgeBands(secondaryTop, 0, 0, 0, MeasureBottomTitle(0));
        var xMajor = Axes.XAxis.MajorTicks;
        double top = secondaryTop, tallestX = 0;
        var overhang = default(Layout.SideOverhang);
        if (xMajor.Visible)
        {
            // The X tick labels as the drawing places them: the same ticks, and turned when the drawing turns them, which
            // the plot's width decides (PrimaryXDraw). Upright, a number's or a date's height is its font's, so it is
            // measured on one label; category names and turned labels are measured over the labels drawn.
            var range = ComputeDataRanges();
            var xSource = Axes.PrimaryTickSource(isX: true);
            var xTicks = PrimaryTicks(range, xSource, isX: true);
            var view = ViewTransform(range, Axes.YAxis, Axes.YBreaks);
            var xDraw = PrimaryXDraw(xTicks, xSource, view, range.YMin);
            bool uprightNumbers = xDraw.LabelRotation == 0 && !xSource.NamesCategories;
            double xLabelHeight = Ctx.MeasureText("0", xMajor.LabelFont(xDraw.BaseFont)).Height;
            tallestX = uprightNumbers ? xLabelHeight : Ctx.MeasureAxisTicks(xTicks, xMajor, xDraw);
            if (MirrorsXTicks)
                top = xMajor.TickLabelBand(xDraw.LabelRotation == 0
                    ? xLabelHeight
                    : Ctx.MeasureAxisTicks(xTicks, xMajor, MirroredXDraw(xDraw, view)));

            // How far the first and the last label stand out sideways past the plot, at the rotation they are drawn at.
            double fontSize = xMajor.LabelFont(xDraw.BaseFont).Size;
            overhang = OverhangOf(xTicks, xDraw, view, range.YMin, fontSize);
            if (MirrorsXTicks)
            {
                var mirrored = OverhangOf(xTicks, MirroredXDraw(xDraw, view), view, range.YMin, fontSize);
                overhang = new Layout.SideOverhang(Math.Max(overhang.Left, mirrored.Left), Math.Max(overhang.Right, mirrored.Right));
            }
        }
        return new Layout.EdgeBands(top, 0, 0, xMajor.TickLabelBand(tallestX), MeasureBottomTitle(tallestX), overhang);
    }

    /// <summary>How far the labels of <paramref name="draw"/> stand out past the left and the right edge of the plot area: for
    /// each tick, how far its label reaches before and after its anchor (<see cref="AxisTickExtensions.Reach"/>) less the
    /// room between its anchor and that edge, the largest of them; 0 when every label lies within the plot's width.</summary>
    /// <param name="xTicks">The X tick values.</param>
    /// <param name="draw">How the labels are drawn: their rotation, alignment and the sizes already measured.</param>
    /// <param name="transform">The transform the ticks are placed with.</param>
    /// <param name="yMin">The data value on the Y axis the ticks are mapped at.</param>
    /// <param name="fontSize">The labels' font size.</param>
    private Layout.SideOverhang OverhangOf(double[] xTicks, TickDrawContext draw, DataTransform transform, double yMin, double fontSize)
    {
        double left = 0, right = 0;
        var sizes = draw.LabelSizes;
        for (int i = 0; i < xTicks.Length && sizes is not null && i < sizes.Count; i++)
        {
            double x = transform.DataToPixel(xTicks[i], yMin).X;
            if (!x.IsDrawable()) continue;
            var reach = draw.Reach(sizes[i], fontSize);
            left = Math.Max(left, reach.Before - (x - PlotArea.X));
            right = Math.Max(right, reach.After - (PlotArea.X + PlotArea.Width - x));
        }
        return new Layout.SideOverhang(left, right);
    }

    /// <summary>From the bottom spine to the far side of the X axis title, as <see cref="AxesRenderer.RenderAxisLabels"/>
    /// draws it below tick labels <paramref name="tallestTickLabel"/> high: in its own style, past its style's pad
    /// (<see cref="AxisTickExtensions.BottomAxisTitleExtent"/>); 0 when the X axis has no title.</summary>
    private double MeasureBottomTitle(double tallestTickLabel)
    {
        if (Axes.XAxis.Label is not { } label) return 0;
        var font = Axes.XAxis.LabelStyle?.ApplyTo(LabelFont()) ?? LabelFont();
        return Axes.XAxis.MajorTicks.BottomAxisTitleExtent(tallestTickLabel, font.Size, Ctx.MeasureText(label, font).Height,
            Axes.XAxis.LabelStyle?.Pad ?? 0);
    }

    /// <summary>Installs the locator and formatter the axis' scale brings when the axis has no locator; the axis'
    /// own formatter is kept.</summary>
    private static void InstallScaleTicks(Axis axis, bool isX)
    {
        if (axis.TickLocator is not null) return;
        axis.TickLocator = axis.ScaleLocator(isX);
        axis.TickFormatter ??= axis.TickLocator.ScaleFormatter();
    }

    /// <summary>The primary tick values of the X axis (<paramref name="isX"/>) or the Y axis the axes draws for
    /// <paramref name="range"/>: placed by the locator of <paramref name="source"/>, otherwise by the default placement
    /// for this renderer's plot width or height; ticks inside a break of that axis are left out (they would stand in empty
    /// space or on the break marker).</summary>
    private double[] PrimaryTicks(DataRange range, TickSource source, bool isX)
    {
        var axis = isX ? Axes.XAxis : Axes.YAxis;
        double from = isX ? range.XMin : range.YMin, to = isX ? range.XMax : range.YMax;
        var ticks = source.Locator is { } locator
            ? locator.LocateInterval(from, to)
            : ComputeTickValues(from, to, axis, isX ? PlotArea.Width : PlotArea.Height);
        var breaks = isX ? Axes.XBreaks : Axes.YBreaks;
        return breaks.Count > 0
            ? ticks.Where(t => !AxisBreakMapper.IsInBreak(breaks, t)).ToArray()
            : ticks;
    }

    /// <summary>How the primary X ticks are drawn under the bottom spine, at the rotation they are drawn at
    /// (<see cref="XLabelRotation"/>: the axis' own, or 30 degrees when upright labels would run into each other at this
    /// renderer's plot width): the one context the drawing (<see cref="RenderTicks"/>) and the measuring
    /// (<see cref="MeasureEndBands"/>) share, so the band reserved is the band drawn. A turned label hangs from its end.</summary>
    /// <param name="xTicks">The X tick values.</param>
    /// <param name="source">Where the ticks come from and how they read.</param>
    /// <param name="transform">The transform the ticks are placed with: how far apart the labels stand.</param>
    /// <param name="yMin">The data value on the Y axis the ticks are mapped at.</param>
    private TickDrawContext PrimaryXDraw(double[] xTicks, TickSource source, DataTransform transform, double yMin)
    {
        var context = new TickDrawContext
        {
            Side          = TickSide.Bottom,
            AxisEdge      = Axes.Spines.Bottom.HorizontalY(PlotArea, PlotArea.Y + PlotArea.Height, transform),
            SpineHalf     = Axes.Spines.Bottom.LineWidth / 2.0,
            BaseFont      = TickFont(),
            Formatter     = source.Formatter,
            UniformFormat = BuildUniformTickFormatter(xTicks),
            Foreground    = Theme.ForegroundText,
        };
        // The labels are measured once, here, where their widths decide the rotation; the drawing and the measuring of the
        // band read the same sizes from the context.
        var font = Axes.XAxis.MajorTicks.LabelFont(context.BaseFont);
        var sizes = Array.ConvertAll(xTicks, tick => Ctx.MeasureText(context.LabelText(tick), font));
        double rotation = XLabelRotation(xTicks, sizes, transform, yMin);
        return context with
        {
            LabelRotation = rotation,
            Alignment     = rotation == 0 ? TextAlignment.Center : TextAlignment.Right,
            LabelSizes    = sizes,
        };
    }

    /// <summary>The primary X ticks mirrored on the top spine: the same ticks and rotation as <paramref name="bottom"/>,
    /// labels above the edge; a turned label hangs from its start, so it rises away from the axis (the point reflection of
    /// the bottom label). The drawing and the measuring share it.</summary>
    private TickDrawContext MirroredXDraw(TickDrawContext bottom, DataTransform transform) => bottom with
    {
        Side      = TickSide.Top,
        AxisEdge  = Axes.Spines.Top.HorizontalY(PlotArea, PlotArea.Y, transform),
        SpineHalf = Axes.Spines.Top.LineWidth / 2.0,
        Alignment = bottom.LabelRotation == 0 ? TextAlignment.Center : TextAlignment.Left,
    };

    /// <summary>The primary Y axis, left slot 0 of the container, as this renderer places it over <paramref name="range"/>:
    /// the axes' own range and <paramref name="transform"/>, the ticks its grid is drawn through (<see cref="PrimaryTicks"/>)
    /// and their source. The drawing and the measuring (<see cref="MeasureSideBands"/>) read its ticks by the one context
    /// (<see cref="PlacedYSlot.TickDraw"/>), so the band reserved is the band drawn.</summary>
    /// <param name="range">The axes' range (<see cref="ComputeDataRanges"/>).</param>
    /// <param name="transform">The transform the primary series are drawn with.</param>
    /// <returns>The placed primary, at offset 0 and with no band yet: its band is known once its tick labels are drawn.</returns>
    private PlacedYSlot PlacePrimary(DataRange range, DataTransform transform)
    {
        var source = Axes.PrimaryTickSource(isX: false);
        return new PlacedYSlot
        {
            Slot = Axes.YAxes.Left[0],
            Range = range,
            Transform = transform,
            Ticks = PrimaryTicks(range, source, isX: false),
            Source = source,
            Pair = SeriesAxisPair.Primary,
        };
    }

    /// <summary>The ONE place a Y axis slot past the primary is placed over the primary's X extent
    /// <paramref name="x"/>: its own range (<see cref="ComputeSecondaryDataRanges(double,double,Axis,IReadOnlyList{ISeries})"/>),
    /// the transform its series and ticks are drawn with (<see cref="ViewTransform"/>, the rule the primary's is built by:
    /// through the slot's own scale and symlog threshold, beside the primary X axis' scale and breaks) and the ticks it places
    /// with their source (<see cref="AxesTickSourceExtensions"/>' <c>SlotTickSource</c>: its locator, or its scale's). The render, the layout's reserve
    /// (<c>ConstrainedLayoutEngine</c>) and the interaction views (<see cref="ComputeSecondaryViewTransforms"/>) all place a
    /// slot here, so none of them works its range or transform out a second way.</summary>
    /// <param name="slot">The slot.</param>
    /// <param name="x">The X extent the slot shares with the primary.</param>
    /// <returns>The slot with its range, transform and ticks; its identity, band and offset are the caller's to add.</returns>
    internal PlacedYSlot PlaceSlot(YAxisSlot slot, MinMaxRange x)
    {
        var range = ComputeSecondaryDataRanges(x.Min, x.Max, slot.Axis, slot.Series);
        return new PlacedYSlot
        {
            Slot = slot,
            Range = range,
            Transform = ViewTransform(range, slot.Axis, []),
            Ticks = slot.Axis.SlotTickValues(range, PlotArea.Height),
            Source = slot.Axis.SlotTickSource(),
        };
    }

    /// <summary>From a placed slot's edge to the far side of what it draws, as its part draws it: its tick labels measured
    /// in the context its part draws them with (<see cref="PlacedYSlot.TickDraw"/>), then its title by the one band rule
    /// (<see cref="YAxisTitleExtensions"/>) with the container's gap. The layout reserves every slot by this band and the
    /// render stacks the slots by it.</summary>
    /// <param name="placed">The slot as placed (<see cref="PlaceSlot"/>).</param>
    /// <returns>The slot's band, in pixels.</returns>
    internal double SlotBand(PlacedYSlot placed)
    {
        var axis = placed.Slot.Axis;
        double widest = Ctx.MeasureAxisTicks(placed.Ticks, axis.MajorTicks, placed.TickDraw(Theme));
        return axis.YTitleExtent(new YAxisTitleInput(axis.MajorTicks.TickLabelBand(widest), placed.Slot.Side,
            Axes.YAxes.TitleGap, LabelFont())).Band;
    }

    /// <summary>Places every Y axis slot of the container for this render, once each, in identity order
    /// (<see cref="SeriesSlotExtensions.YSlotSeats"/>): the primary as it was drawn (<paramref name="primary"/>, its band
    /// that of the tick labels it drew), then every slot past it, each with its range, transform and ticks
    /// (<see cref="PlaceSlot"/>), the place its series hold in the identity order, its band (<see cref="SlotBand"/>) and its
    /// offset: each side stacked once, nearest the plot first, with the container's gap between each pair
    /// (<see cref="Layout.AxisStackingExtensions.StackedOffsets"/>), the rule the layout reserves the sides by.</summary>
    /// <param name="primary">The primary Y axis as placed and drawn.</param>
    /// <returns>Every slot placed, the primary first.</returns>
    private List<PlacedYSlot> PlaceYSlots(PlacedYSlot primary)
    {
        var x = new MinMaxRange(primary.Range.XMin, primary.Range.XMax);
        var placed = new List<PlacedYSlot> { primary };
        foreach (var seat in Axes.YSlotSeats().Skip(1))
        {
            var slot = PlaceSlot(seat.Slot, x) with { Pair = seat.Pair, FirstIndexInPair = seat.FirstIndexInPair };
            placed.Add(slot with { Band = SlotBand(slot) });
        }

        // Each side stacked once, its slots in stack order, the primary first on the left.
        foreach (var side in new[] { AxisSide.Right, AxisSide.Left })
        {
            var onSide = placed.Select((slot, at) => new PlacedAt(slot, at)).Where(p => p.Slot.Slot.Side == side).ToList();
            var offsets = onSide.Select(p => p.Slot.Band).ToArray().StackedOffsets(Axes.YAxes.TitleGap);
            for (int i = 0; i < onSide.Count; i++)
                placed[onSide[i].At] = onSide[i].Slot with { Offset = offsets[i] };
        }
        return placed;
    }

    /// <summary>A placed slot and its index in the list of every placed slot.</summary>
    private readonly record struct PlacedAt(PlacedYSlot Slot, int At);

    /// <inheritdoc />
    public sealed override void Render()
    {
        var axesBg = Theme.AxesBackground;
        Ctx.DrawRectangle(PlotArea, new ShapeStyle(axesBg, null, 0));

        // Compute data ranges, and the transform the primary series are drawn with
        var range = ComputeDataRanges();
        var transform = ViewTransform(range, Axes.YAxis, Axes.YBreaks);
        DrawnTransform = transform;

        // A scale with its own tick placement installs it when the axis has no locator: AutoDateLocator +
        // AutoDateFormatter for a date X axis (handles legacy SetXDateFormat() calls that only set scale + formatter),
        // SymlogLocator for symlog (matplotlib's set_yscale("symlog") installs SymmetricalLogLocator), LogLocator +
        // LogTickFormatter for log (without that arm a log axis wore LINEAR ticks 0, 25, 50 … over a logarithmic
        // transform, measured 2026-08-30).
        InstallScaleTicks(Axes.XAxis, isX: true);
        InstallScaleTicks(Axes.YAxis, isX: false);

        // Compute tick values once for grid + ticks (respects TickLocator / Spacing / plot size; an axis that carries
        // categories ticks them, so the grid runs through the categories and every tick setting reaches their names;
        // ticks inside a break are left out)
        var xSource = Axes.PrimaryTickSource(isX: true);
        var xTicks = PrimaryTicks(range, xSource, isX: true);
        // The primary Y axis is left slot 0 of the container and is drawn by the part that draws every Y axis.
        var primary = PlacePrimary(range, transform);
        _primaryYPart = new CartesianParts.CartesianYAxisSlotPart(PartContext, primary, DrawInsideSeriesGroup);
        var yTicks = primary.Ticks;

        // Skip standard grid/ticks/frame for axis-less series (radar, pie, treemap, table).
        // These fill the plot area and don't use a Cartesian coordinate system.
        bool radarOnly = IsAxisLess;
        if (!radarOnly)
        {
            // Grid — axes-level setting overrides theme when explicitly set (Visible=true);
            // otherwise fall back to Theme.DefaultGrid so the theme controls the default.
            // An axes that asked for something gets the theme's grid WITH that change applied; one that asked
            // nothing gets the theme's grid whole. The older reading — the axes grid when it happened to be
            // visible, the theme's otherwise — dropped a change that did not mention visibility, and could not
            // express "no grid" at all under a theme that draws one.
            var effectiveGrid = Axes.GridUnder(Theme);
            if (effectiveGrid.Visible)
                RenderGrid(xTicks, yTicks, range.YMin, transform, effectiveGrid);

            // Tick marks and labels
            RenderTicks(xTicks, xSource, range.YMin, transform);
        }

        // Render span regions (Phase B.5 — extracted to CartesianSpansPart)
        new CartesianParts.CartesianSpansPart(Axes, PlotArea, Ctx, Theme, transform, range).Render();

        // Render reference lines (Phase B.6 — extracted to CartesianReferenceLinesPart)
        new CartesianParts.CartesianReferenceLinesPart(Axes, PlotArea, Ctx, Theme, transform, range).Render();

        // Render financial drawing tools (trendlines, levels, Fibonacci retracements)
        new CartesianParts.CartesianToolsPart(Axes, PlotArea, Ctx, Theme, transform).Render();

        StackBars();

        // Every Y axis slot of the container is placed ONCE for this render (PlaceYSlots): its range, its transform, its
        // ticks, where its series stand in the identity order, its band and its offset. A slot without series is still
        // placed and charged its band, by the rule the layout reserves the sides by, so the slot past it stands where the
        // layout made room for it; only a slot WITH series is drawn. The primary, left slot 0, comes first, with the band
        // of the tick labels it drew.
        var placed = PlaceYSlots(primary with
        {
            Band = Axes.YAxis.YTitleExtent(new YAxisTitleInput(
                Axes.YAxis.MajorTicks.TickLabelBand(MeasuredYTickMaxWidth), AxisSide.Left, Axes.YAxes.TitleGap, LabelFont())).Band,
        });

        // End-of-line labels: placed once, across every series, before any is drawn, each at the height its own slot puts
        // the end of its line at. Each label is drawn inside its own series' group — by the primary pass or the part of its
        // slot, both through DrawInsideSeriesGroup — so whatever hides the series hides its label.
        _directLabels = new CartesianParts.CartesianDirectLabelsPart(PartContext, placed, _mirroredYTickBand);

        // Series
        RenderSeries(transform);

        // Axis break markers (drawn on top of series, overlaying the break region)
        if (Axes.XBreaks.Count > 0) RenderBreakMarkers(Axes.XBreaks, transform, isXAxis: true);
        if (Axes.YBreaks.Count > 0) RenderBreakMarkers(Axes.YBreaks, transform, isXAxis: false);

        // Every Y axis slot past the primary that has series, right slots then left slots, nearest the plot first, each
        // at its own offset, through the part that draws every Y axis (the primary's steps are drawn above and below).
        foreach (var slot in placed.Skip(1).Where(slot => slot.Slot.Series.Count > 0))
            new CartesianParts.CartesianYAxisSlotPart(PartContext, slot, DrawInsideSeriesGroup).Render();

        // Secondary X-axis series (Phase B.8 — extracted to CartesianSecondaryXAxisPart). It owns the top edge, so the
        // band it draws there is what the axes title rises by.
        if (DrawsSecondaryX)
        {
            var secXRange = ComputeSecondaryXDataRanges(range.YMin, range.YMax);
            var secondaryX = new CartesianParts.CartesianSecondaryXAxisPart(
                Axes, PlotArea, Ctx, Theme, transform, secXRange);
            secondaryX.Render();
            MeasuredTopTickBand = secondaryX.Band;
        }

        // The legend is placed now, ahead of the annotations, so a text that asked to move out of the way keeps clear of the box
        // the legend is drawn in a moment later; the marks it keeps clear of are the series' and that box.
        var legendBox = PlaceLegend();
        var obstacles = MarkObstacles.None;
        if (Axes.Annotations.Any(a => a.AvoidOverlap))
        {
            var marks = new MarkObstacles.Builder();
            CollectSeriesMarks(marks);
            if (legendBox is { } box) marks.AddBox(box);
            obstacles = marks.Build();
        }

        // Annotations (Phase B.4 — extracted to CartesianAnnotationsPart via Composite pattern)
        new CartesianParts.CartesianAnnotationsPart(Axes, PlotArea, Ctx, Theme, transform) { Obstacles = obstacles }.Render();

        // Signal markers (Phase B.9 — extracted to CartesianSignalsPart)
        new CartesianParts.CartesianSignalsPart(Axes, PlotArea, Ctx, Theme, transform).Render();

        // Axes spines — rendered after series so they appear on top of fills/areas
        if (!radarOnly)
            RenderSpines(transform);

        // Legend
        RenderLegend();

        // ColorBar
        RenderColorBar();

        // Axes title
        RenderTitle();

        // Axis labels
        RenderAxisLabels();
    }

    /// <summary>Stacked bars: each bar starts on the sum of the stackable series added before it that have no foot of their
    /// own — the same set the data range sums; a bar with a foot of its own floats on it and is not in the stack. Render state, worked out afresh here on every render and cleared on every render that does not
    /// stack; the StackBaseline a user sets is never written, so a stacked render leaves no trace in the figure, in
    /// its JSON or in the next render. The marks a legend keeps clear of are collected from the same state.</summary>
    private void StackBars()
    {
        var layers = Axes.Series.StackLayers();
        bool stacked = Axes.BarMode == BarMode.Stacked && layers.Count > 1;
        var feet = layers.StackedFeet();
        foreach (var bar in Axes.Series.OfType<BarSeries>())
            bar.StackedBottoms = null;
        for (int i = 0; i < layers.Count; i++)
        {
            if (stacked && layers[i] is BarSeries bar)
                bar.StackedBottoms = feet[i];
        }
    }

    /// <inheritdoc />
    /// <remarks>Every series of every seat, hidden ones too (a legend that toggles a series must not move from under the
    /// pointer): the primary's through the view transform, each Y slot's through the transform <see cref="PlaceSlot"/>
    /// gives it and the top axis' through its own, the three <see cref="Render"/> draws them with, none of it drawn.</remarks>
    internal override MarkObstacles LegendObstacles()
    {
        var marks = new MarkObstacles.Builder();
        var primaryView = CollectSeriesMarks(marks);

        // The annotations' texts stand among the marks as well, at the places they are put at (a text that moves out of the
        // way is laid out around the legend afterwards, so where it ends up does not decide where the legend stands).
        new CartesianParts.CartesianAnnotationsPart(Axes, PlotArea, Ctx, Theme, primaryView).AddTextBoxes(marks);
        return marks.Build();
    }

    /// <summary>Adds the marks of every series of every seat, hidden ones too, to <paramref name="marks"/>.</summary>
    /// <param name="marks">The collection the marks go into.</param>
    /// <returns>The view transform the primary series are drawn with.</returns>
    private DataTransform CollectSeriesMarks(MarkObstacles.Builder marks)
    {
        var range = ComputeDataRanges();

        // The bars stand where they are drawn: stacked, and grouped beside one another.
        StackBars();
        ArrangeBarGroups();

        var primaryView = ViewTransform(range, Axes.YAxis, Axes.YBreaks);
        CollectMarks(Axes.Series, primaryView, marks);
        var x = new MinMaxRange(range.XMin, range.XMax);
        foreach (var seat in Axes.YSlotSeats().Skip(1))
            CollectMarks(seat.Slot.Series, PlaceSlot(seat.Slot, x).Transform, marks);
        if (DrawsSecondaryX)
        {
            var secRange = ComputeSecondaryXDataRanges(range.YMin, range.YMax);
            CollectMarks(Axes.XSecondarySeries,
                new DataTransform(secRange.XMin, secRange.XMax, secRange.YMin, secRange.YMax, PlotArea), marks);
        }
        return primaryView;
    }

    /// <inheritdoc />
    /// <remarks>The range is <see cref="ComputeDataRanges"/>' and the transform is built by <see cref="ViewTransform"/>,
    /// the two calls <see cref="Render"/> makes before it draws anything.</remarks>
    internal override DataTransform ComputeViewTransform() => ViewTransform(ComputeDataRanges(), Axes.YAxis, Axes.YBreaks);

    /// <inheritdoc />
    /// <remarks>Every slot past the primary placed where <see cref="Render"/> places it (<see cref="PlaceSlot"/>, against
    /// the primary X range every slot shares), without drawing or measuring anything; numbered outward, right slots then
    /// left slots.</remarks>
    internal override IReadOnlyList<SecondaryAxisTransform> ComputeSecondaryViewTransforms()
    {
        var range = ComputeDataRanges();
        var x = new MinMaxRange(range.XMin, range.XMax);
        var views = new List<SecondaryAxisTransform>();
        foreach (var stack in new[] { Axes.YAxes.Right, Axes.YAxes.Left })
        {
            for (int i = 0; stack.SlotAtOutward(i) is { } slot; i++)
                views.Add(new SecondaryAxisTransform(stack.Side, i, PlaceSlot(slot, x).Transform));
        }
        return views;
    }

    /// <summary>The transform that maps <paramref name="range"/> onto the plot area, for a Y axis
    /// <paramref name="yAxis"/> beside the primary X axis: axis breaks compress it, and on a log or symlog axis the pixel
    /// mapping works in the scaled space (<see cref="AxisScaleExtensions.Forward"/>) so that ticks at 100, 1000, 10000 land
    /// evenly spaced.</summary>
    /// <param name="range">The X range and the Y axis' own range.</param>
    /// <param name="yAxis">The Y axis: its scale and symlog threshold.</param>
    /// <param name="yBreaks">The breaks of that Y axis.</param>
    private DataTransform ViewTransform(DataRange range, Axis yAxis, IReadOnlyList<AxisBreak> yBreaks)
    {
        // Apply axis breaks: compress the visible range by removing break gaps
        var (cXMin, cXMax) = Axes.XBreaks.Count > 0
            ? AxisBreakMapper.CompressedRange(Axes.XBreaks, range.XMin, range.XMax)
            : new MinMaxRange(range.XMin, range.XMax);
        var (cYMin, cYMax) = yBreaks.Count > 0
            ? AxisBreakMapper.CompressedRange(yBreaks, range.YMin, range.YMax)
            : new MinMaxRange(range.YMin, range.YMax);

        var (txMin, txMax) = ScaleRange(cXMin, cXMax, Axes.XAxis.Scale, Axes.XAxis.SymLogLinThresh);
        var (tyMin, tyMax) = ScaleRange(cYMin, cYMax, yAxis.Scale, yAxis.SymLogLinThresh);

        return (Axes.XBreaks.Count > 0 || yBreaks.Count > 0
                || Axes.XAxis.Scale is AxisScale.Log or AxisScale.SymLog
                || yAxis.Scale is AxisScale.Log or AxisScale.SymLog)
            ? new DataTransform(txMin, txMax, tyMin, tyMax, PlotArea,
                Axes.XBreaks, yBreaks,
                range.XMin, range.XMax, range.YMin, range.YMax,
                Axes.XAxis.Scale, yAxis.Scale, Axes.XAxis.SymLogLinThresh, yAxis.SymLogLinThresh)
            : new DataTransform(cXMin, cXMax, cYMin, cYMax, PlotArea);
    }

    /// <summary>Maps a (raw) data range through the axis scale's Forward function so the
    /// resulting scaled range can be used by <see cref="DataTransform"/> for pixel scaling.
    /// Linear scale is identity; SymLog applies <see cref="SymlogTransform.Forward"/>; Log
    /// applies log10. Returns the scaled (min, max) pair.</summary>
    private static MinMaxRange ScaleRange(double min, double max, AxisScale scale, double linthresh) =>
        new(scale.Forward(min, linthresh), scale.Forward(max, linthresh));

    /// <summary>Renders major and/or minor grid lines behind the plot area at each tick position.</summary>
    private void RenderGrid(double[] xTicks, double[] yTicks, double yMin, DataTransform transform, GridStyle grid)
    {
        var gridColor = grid.Color;
        var gridStyle = grid.LineStyle;
        double gridWidth = grid.LineWidth;
        bool drawX = grid.Axis is GridAxis.X or GridAxis.Both;
        bool drawY = grid.Axis is GridAxis.Y or GridAxis.Both;
        double xMin = Axes.XAxis.Min ?? 0;

        if (grid.Which is GridWhich.Major or GridWhich.Both)
        {
            if (drawX) RenderGridLines(Orientation.Horizontal, xTicks, yMin,   transform, gridColor, gridWidth, gridStyle, minor: false);
            if (drawY) RenderGridLines(Orientation.Vertical,   yTicks, xMin,   transform, gridColor, gridWidth, gridStyle, minor: false);
        }

        if (grid.Which is GridWhich.Minor or GridWhich.Both)
        {
            double minorWidth = gridWidth * 0.5;
            var minorColor = gridColor.WithAlpha((byte)(gridColor.A / 2));
            if (drawX) RenderGridLines(Orientation.Horizontal, xTicks, yMin, transform, minorColor, minorWidth, gridStyle, minor: true);
            if (drawY) RenderGridLines(Orientation.Vertical,   yTicks, xMin, transform, minorColor, minorWidth, gridStyle, minor: true);
        }
    }

    /// <summary>Renders tick marks and tick labels along the X and Y axes, including minor ticks when enabled. The X ticks
    /// are those of <paramref name="xSource"/> and the primary Y axis' those of its placed slot: numbers, dates, or the
    /// names of the categories its series stand on, drawn by the one tick drawing whatever they read; the primary Y axis
    /// draws its ticks through the part that draws every Y axis.</summary>
    private void RenderTicks(double[] xTicks, TickSource xSource, double yMin, DataTransform transform)
    {
        // --- X axis ticks ---
        var xMajor = Axes.XAxis.MajorTicks;
        var xMinor = Axes.XAxis.MinorTicks;
        var yMajor = Axes.YAxis.MajorTicks;

        if (!xMajor.Visible)
        {
            // HideAllAxes / explicit TickConfig.Visible=false → draw no tick marks or labels, numbers and names alike.
            MeasuredXTickMaxHeight = 0;
        }
        else
        {
            // At the rotation the labels are drawn at (PrimaryXDraw); what they reach below the axis, turned or not, is
            // where the X axis title stands clear of them (RenderAxisLabels).
            var xDraw = PrimaryXDraw(xTicks, xSource, transform, yMin);
            MeasuredXTickMaxHeight = Ctx.RenderAxisTicks(xTicks, yMin, transform, xMajor, xDraw);

            // Mirrored X ticks on the top spine: the same ticks, labels above the edge. The band they take is what the
            // axes title rises by.
            if (MirrorsXTicks)
                MeasuredTopTickBand = xMajor.TickLabelBand(Ctx.RenderAxisTicks(xTicks, yMin, transform, xMajor, MirroredXDraw(xDraw, transform)));

            // Minor X ticks (no labels, shorter mark)
            Ctx.RenderMinorTicks(new TickRun(xTicks, yMin, transform), xMinor, xDraw);
        }

        // --- Y axis ticks: the primary, left slot 0, through the part that draws every Y axis ---
        var yPart = _primaryYPart!;
        MeasuredYTickMaxWidth = yPart.RenderMajorTicks();

        // Mirrored Y ticks on the right spine: the same ticks hung from the right edge by the right side's terms, labels
        // past it. The end labels start past the band they take.
        if (MirrorsYTicks)
        {
            var right = YSideTerms.Right;
            _mirroredYTickBand = yMajor.TickLabelBand(Ctx.RenderAxisTicks(yPart.Ticks, Axes.XAxis.Min ?? 0, transform, yMajor,
                right.Hang(yPart.TickDraw, Axes.Spines.Right.VerticalX(PlotArea, right.EdgeX(PlotArea, 0), transform), Axes.Spines)));
        }

        // Minor Y ticks (no labels, shorter mark)
        yPart.RenderMinorTicks();
    }

    /// <summary>
    /// The rotation the X tick labels are drawn at, the one decision the drawing and the layout's measuring share
    /// (<see cref="PrimaryXDraw"/>). The axis' own non-zero <see cref="TickConfig.LabelRotation"/> always wins. Otherwise,
    /// when adjacent upright labels would overlap at the tick positions <paramref name="transform"/> places them at (so at
    /// this renderer's plot width), 30 degrees, as matplotlib's <c>Figure.autofmt_xdate</c> turns them; 0 when upright
    /// labels fit.
    /// </summary>
    /// <param name="xTicks">The X tick values.</param>
    /// <param name="sizes">The upright size of each tick's label in the font it is drawn in.</param>
    /// <param name="transform">The transform that places the ticks.</param>
    /// <param name="yMin">The data value on the Y axis the ticks are mapped at.</param>
    private double XLabelRotation(double[] xTicks, Size[] sizes, DataTransform transform, double yMin)
    {
        var major = Axes.XAxis.MajorTicks;
        if (major.LabelRotation != 0) return major.LabelRotation;
        if (xTicks.Length < 2)   return 0;

        // The widest label, then whether the tick spacing in pixels is at least that width + 4 px padding. If any pair
        // collides we return 30°.
        const double GapPx = 4;
        double maxLabelWidth = 0;
        var pixelXs = new double[xTicks.Length];
        for (int i = 0; i < xTicks.Length; i++)
        {
            if (sizes[i].Width > maxLabelWidth) maxLabelWidth = sizes[i].Width;
            pixelXs[i] = transform.DataToPixel(xTicks[i], yMin).X;
        }

        for (int i = 1; i < pixelXs.Length; i++)
        {
            double spacing = Math.Abs(pixelXs[i] - pixelXs[i - 1]);
            if (spacing < maxLabelWidth + GapPx) return 30;
        }
        return 0;
    }

    /// <summary>The combined X and Y data ranges across all series on the axes, including shared axes' series when
    /// <c>sharex</c>/<c>sharey</c> is configured. The rule is the axes' own (<see cref="AxesRangeExtensions"/>): it reads the
    /// axes and the theme and never the plot area, so whatever must know the extent before a rectangle exists asks the axes.
    /// Exposed as <c>internal</c> so regression tests can assert the padding / sticky-edge / nice-bound interaction without
    /// rendering to SVG.</summary>
    internal DataRange ComputeDataRanges() => Axes.ComputeDataRanges(Theme);

    /// <summary>Computes the Y data range for <paramref name="series"/> plotted against <paramref name="axis"/> — one
    /// slot's own range, independent of every other slot's (each <see cref="YAxisSlot"/> keeps its own series, so nothing
    /// here folds two slots' data together).</summary>
    /// <param name="xMin">The primary axes' X minimum, shared by every slot past the primary.</param>
    /// <param name="xMax">The primary axes' X maximum, shared by every slot past the primary.</param>
    /// <param name="axis">The slot's own axis (its set limits and scale orientation).</param>
    /// <param name="series">The slot's own series — never another slot's.</param>
    internal DataRange ComputeSecondaryDataRanges(double xMin, double xMax, Axis axis, IReadOnlyList<ISeries> series)
    {
        double yMin = axis.Min ?? double.MaxValue;
        double yMax = axis.Max ?? double.MinValue;

        var context = new AxesContextAdapter(Axes);
        foreach (var s in series)
        {
            // ISeries.ComputeDataRange, not an `is IHasDataRange` test: every series carries the member, but
            // not every one carries that separate marker interface — a streaming series does not, so the test
            // skipped it, the range stayed at its 0..1 sentinel and the trace was drawn off the plot area
            // while the primary path (SnapshotContributions) had ranged it correctly all along.
            var c = s.ComputeDataRange(context);
            if (c.YMin.HasValue && c.YMin.Value < yMin) yMin = c.YMin.Value;
            if (c.YMax.HasValue && c.YMax.Value > yMax) yMax = c.YMax.Value;
        }

        // A set limit is exact, the same rule the primary axes follow (Range1D.WithExplicitLimits).
        var exact = new Range1D(yMin, yMax).WithExplicitLimits(axis);
        yMin = exact.Lo;
        yMax = exact.Hi;

        if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
        if (Math.Abs(yMax - yMin) < 1e-10) { yMin -= 0.5; yMax += 0.5; }

        // A log axis lifts a floor it cannot place and pads in log space, a set side is left as set, and an inverted axis
        // is drawn high to low: the rules the primary axes follow (Range1D.PositiveForLog, Padded, OrientedBy).
        var drawn = new Range1D(yMin, yMax).PositiveForLog(axis).Padded(0.05, axis).OrientedBy(axis);
        return new DataRange(xMin, xMax, drawn.Lo, drawn.Hi);
    }

    /// <summary>Computes the X data range for series plotted against the secondary X-axis.</summary>
    internal DataRange ComputeSecondaryXDataRanges(double yMin, double yMax)
    {
        double xMin = Axes.SecondaryXAxis?.Min ?? double.MaxValue;
        double xMax = Axes.SecondaryXAxis?.Max ?? double.MinValue;

        var context = new AxesContextAdapter(Axes);
        foreach (var series in Axes.XSecondarySeries)
        {
            if (series is IHasDataRange hasRange)
            {
                var c = hasRange.ComputeDataRange(context);
                if (c.XMin.HasValue && c.XMin.Value < xMin) xMin = c.XMin.Value;
                if (c.XMax.HasValue && c.XMax.Value > xMax) xMax = c.XMax.Value;
            }
        }

        // A set limit is exact, the same rule the primary axes follow (Range1D.WithExplicitLimits).
        var exact = new Range1D(xMin, xMax).WithExplicitLimits(Axes.SecondaryXAxis!);
        xMin = exact.Lo;
        xMax = exact.Hi;

        if (xMin == double.MaxValue) { xMin = 0; xMax = 1; }
        if (Math.Abs(xMax - xMin) < 1e-10) { xMin -= 0.5; xMax += 0.5; }

        double xPadding = (xMax - xMin) * 0.05;
        if (!Axes.SecondaryXAxis!.Min.HasValue) xMin -= xPadding;
        if (!Axes.SecondaryXAxis!.Max.HasValue) xMax += xPadding;

        // An inverted axis is drawn high to low, the rule the primary axes follow (Range1D.OrientedBy).
        var drawn = new Range1D(xMin, xMax).OrientedBy(Axes.SecondaryXAxis);
        return new DataRange(drawn.Lo, drawn.Hi, yMin, yMax);
    }

    /// <summary>Renders the four axis spines (border lines) based on the <see cref="Axes.Spines"/> configuration: each from
    /// the one rule that places a spine (<see cref="SpineLineExtensions"/>), the same rule the tick marks hang from.</summary>
    private void RenderSpines(DataTransform transform)
    {
        var spines = Axes.Spines;
        double bottom = PlotArea.Y + PlotArea.Height;
        double right = PlotArea.X + PlotArea.Width;

        if (spines.Bottom.Visible) DrawSpine(spines.Bottom, spines.Bottom.HorizontalLine(PlotArea, bottom, transform));
        if (spines.Top.Visible) DrawSpine(spines.Top, spines.Top.HorizontalLine(PlotArea, PlotArea.Y, transform));
        if (spines.Left.Visible) DrawSpine(spines.Left, spines.Left.VerticalLine(PlotArea, PlotArea.X, transform));
        if (spines.Right.Visible) DrawSpine(spines.Right, spines.Right.VerticalLine(PlotArea, right, transform));
    }

    private void DrawSpine(SpineConfig spine, SpineLine line)
    {
        Ctx.BeginGroup("spine");
        Ctx.DrawLine(line.From, line.To, new StrokeStyle(spine.Color ?? Theme.ForegroundText, spine.LineWidth, spine.LineStyle));
        Ctx.EndGroup();
    }

    private void RenderBreakMarkers(IReadOnlyList<Models.AxisBreak> breaks, DataTransform transform, bool isXAxis)
    {
        const double MarkerHalfSize = 6.0;
        const double MarkerThickness = 1.5;
        var markerColor = Theme.ForegroundText;
        var bgColor = Theme.AxesBackground;

        foreach (var b in breaks)
        {
            if (b.Style == Models.BreakStyle.None) continue;

            if (isXAxis)
            {
                double compressedX = AxisBreakMapper.Remap(breaks, b.From, transform.DataXMin, transform.DataXMax);
                if (double.IsNaN(compressedX)) continue;
                var pt = transform.DataToPixel(compressedX, transform.DataYMin);
                DrawAxisBreakMark(b.Style, markerColor, bgColor, MarkerHalfSize, MarkerThickness,
                    pt.X, PlotArea.Y, PlotArea.Y + PlotArea.Height, horizontal: true);
            }
            else
            {
                double compressedY = AxisBreakMapper.Remap(breaks, b.From, transform.DataYMin, transform.DataYMax);
                if (double.IsNaN(compressedY)) continue;
                var pt = transform.DataToPixel(transform.DataXMin, compressedY);
                DrawAxisBreakMark(b.Style, markerColor, bgColor, MarkerHalfSize, MarkerThickness,
                    pt.Y, PlotArea.X, PlotArea.X + PlotArea.Width, horizontal: false);
            }
        }
    }

    private void DrawAxisBreakMark(
        Models.BreakStyle style, Color lineColor, Color bgColor,
        double halfSize, double thickness,
        double pos, double edgeA, double edgeB, bool horizontal)
    {
        const double GapHalf = 4.0;
        if (horizontal)
            Ctx.DrawRectangle(new Rect(pos - GapHalf, edgeA, GapHalf * 2, edgeB - edgeA), new ShapeStyle(bgColor, null, 0));
        else
            Ctx.DrawRectangle(new Rect(edgeA, pos - GapHalf, edgeB - edgeA, GapHalf * 2), new ShapeStyle(bgColor, null, 0));

        DrawBreakSegments(
            horizontal ? Orientation.Horizontal : Orientation.Vertical,
            crossStart: edgeA, crossEnd: edgeB,
            perpPos: pos, halfSize: halfSize,
            thickness: thickness, lineColor: lineColor,
            style: style);
    }

    // ── Internal helpers (testable via InternalsVisibleTo) ───────────────────

    /// <summary>Draws major or minor grid lines for one axis orientation.
    /// Horizontal → vertical lines at each X tick; Vertical → horizontal lines at each Y tick.
    /// <paramref name="fixedCoord"/> is the data-space value of the perpendicular axis used in the
    /// DataToPixel lookup (yMin for X ticks, xMin for Y ticks).</summary>
    internal void RenderGridLines(
        Orientation orientation,
        double[] ticks,
        double fixedCoord,
        DataTransform transform,
        Color color, double width, LineStyle style,
        bool minor)
    {
        bool isHorizontal = orientation == Orientation.Horizontal;
        double areaMin = isHorizontal ? PlotArea.X : PlotArea.Y;
        double areaMax = isHorizontal ? PlotArea.X + PlotArea.Width : PlotArea.Y + PlotArea.Height;

        if (!minor)
        {
            foreach (var tick in ticks)
            {
                var pt = isHorizontal
                    ? transform.DataToPixel(tick, fixedCoord)
                    : transform.DataToPixel(fixedCoord, tick);
                if (isHorizontal)
                    Ctx.DrawLine(new Point(pt.X, PlotArea.Y), new Point(pt.X, PlotArea.Y + PlotArea.Height),
                        new StrokeStyle(color, width, style));
                else
                    Ctx.DrawLine(new Point(PlotArea.X, pt.Y), new Point(PlotArea.X + PlotArea.Width, pt.Y),
                        new StrokeStyle(color, width, style));
            }
        }
        else if (ticks.Length >= 2)
        {
            double majorStep = ticks[1] - ticks[0];
            double minorStep = majorStep / 5;
            double rangeStart = ticks[0] - majorStep;
            double rangeEnd   = ticks[^1] + majorStep;
            for (double mt = rangeStart; mt <= rangeEnd + minorStep * 0.01; mt += minorStep)
            {
                mt = Math.Round(mt, 10);
                if (Array.Exists(ticks, t => Math.Abs(t - mt) < minorStep * 0.01)) continue;
                var pt = isHorizontal
                    ? transform.DataToPixel(mt, fixedCoord)
                    : transform.DataToPixel(fixedCoord, mt);
                double pos = isHorizontal ? pt.X : pt.Y;
                if (pos < areaMin || pos > areaMax) continue;
                if (isHorizontal)
                    Ctx.DrawLine(new Point(pt.X, PlotArea.Y), new Point(pt.X, PlotArea.Y + PlotArea.Height),
                        new StrokeStyle(color, width, style));
                else
                    Ctx.DrawLine(new Point(PlotArea.X, pt.Y), new Point(PlotArea.X + PlotArea.Width, pt.Y),
                        new StrokeStyle(color, width, style));
            }
        }
    }

    /// <summary>Draws the zigzag or straight-line segments of an axis break marker.
    /// The white-rectangle gap erasure is done by the caller (<see cref="DrawAxisBreakMark"/>).
    /// <paramref name="crossStart"/>/<paramref name="crossEnd"/> span the axis extent (e.g. PlotArea.Y to PlotArea.Y+Height for horizontal);
    /// <paramref name="perpPos"/> is the break position on the perpendicular axis.</summary>
    internal void DrawBreakSegments(
        Orientation orientation,
        double crossStart, double crossEnd,
        double perpPos, double halfSize,
        double thickness, Color lineColor,
        BreakStyle style)
    {
        double span = crossEnd - crossStart;
        bool horiz = orientation == Orientation.Horizontal;

        if (style == BreakStyle.Zigzag)
        {
            if (horiz)
            {
                Ctx.DrawLine(new Point(perpPos - halfSize, crossStart + span * 0.4),
                    new Point(perpPos, crossStart + span * 0.5), new StrokeStyle(lineColor, thickness, LineStyle.Solid));
                Ctx.DrawLine(new Point(perpPos, crossStart + span * 0.5),
                    new Point(perpPos + halfSize, crossStart + span * 0.6), new StrokeStyle(lineColor, thickness, LineStyle.Solid));
                Ctx.DrawLine(new Point(perpPos - halfSize, crossEnd - span * 0.6),
                    new Point(perpPos, crossEnd - span * 0.5), new StrokeStyle(lineColor, thickness, LineStyle.Solid));
                Ctx.DrawLine(new Point(perpPos, crossEnd - span * 0.5),
                    new Point(perpPos + halfSize, crossEnd - span * 0.4), new StrokeStyle(lineColor, thickness, LineStyle.Solid));
            }
            else
            {
                Ctx.DrawLine(new Point(crossStart + span * 0.4, perpPos - halfSize),
                    new Point(crossStart + span * 0.5, perpPos), new StrokeStyle(lineColor, thickness, LineStyle.Solid));
                Ctx.DrawLine(new Point(crossStart + span * 0.5, perpPos),
                    new Point(crossStart + span * 0.6, perpPos + halfSize), new StrokeStyle(lineColor, thickness, LineStyle.Solid));
            }
        }
        else if (style == BreakStyle.Straight)
        {
            if (horiz)
            {
                Ctx.DrawLine(new Point(perpPos - halfSize * 0.5, crossStart + span * 0.3),
                    new Point(perpPos + halfSize * 0.5, crossStart + span * 0.7),
                    new StrokeStyle(lineColor, thickness, LineStyle.Solid));
                Ctx.DrawLine(new Point(perpPos - halfSize * 0.5 + halfSize * 0.6, crossStart + span * 0.3),
                    new Point(perpPos + halfSize * 0.5 + halfSize * 0.6, crossStart + span * 0.7),
                    new StrokeStyle(lineColor, thickness, LineStyle.Solid));
            }
            else
            {
                Ctx.DrawLine(new Point(crossStart + span * 0.3, perpPos - halfSize * 0.5),
                    new Point(crossStart + span * 0.7, perpPos + halfSize * 0.5),
                    new StrokeStyle(lineColor, thickness, LineStyle.Solid));
                Ctx.DrawLine(new Point(crossStart + span * 0.3, perpPos - halfSize * 0.5 + halfSize * 0.6),
                    new Point(crossStart + span * 0.7, perpPos + halfSize * 0.5 + halfSize * 0.6),
                    new StrokeStyle(lineColor, thickness, LineStyle.Solid));
            }
        }
    }
}
