// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Where the axes of one Cartesian axes stand over one plot area: the range of the primary and of every axis past it,
/// the transforms they are drawn with, their ticks, each Y slot's band and outward offset, and the top X axis. The renderer
/// draws by it (<see cref="CartesianAxesRenderer"/>), the layout reserves each slot's band by it
/// (<see cref="ConstrainedLayoutEngine"/>) and the interaction views read each slot's transform from it, so no two of them work
/// a range, a transform or a band out two ways. A value made where it is needed, holding nothing but what it was made with.</summary>
/// <param name="axes">The axes whose axes are placed.</param>
/// <param name="plotArea">The plot area, in pixels: its length along an axis sets how many ticks that axis draws.</param>
/// <param name="ctx">The context a slot's tick labels are measured with.</param>
/// <param name="theme">The active theme: the label font and, outside a draw, the margins that pad the range.</param>
/// <param name="ranges">The ranges of the draw the placement belongs to; <see langword="null"/> outside a draw.</param>
internal readonly struct CartesianAxisPlacement(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme, FigureRanges? ranges = null)
{
    /// <summary>The combined X and Y data ranges across all series on the axes, including shared axes' series when
    /// <c>sharex</c>/<c>sharey</c> is configured. The rule is the axes' own (<see cref="AxesRangeExtensions"/>): it reads the
    /// axes and the theme and never the plot area, so whatever must know the extent before a rectangle exists asks the axes.
    /// In a draw the range is the draw's (<see cref="FigureRanges"/>), worked out once for every placement the draw makes for
    /// these axes; a placement made outside a draw works it out here.</summary>
    /// <returns>The data range of the axes.</returns>
    internal DataRange ComputeDataRanges() => RangeOfAxes().Range;

    /// <summary>The range of <see cref="ComputeDataRanges"/> with whether the patches of the axes can reach past the plot
    /// (<see cref="AxesRange.PatchesMayReachPastPlot"/>), from the one pass over what asks for the range.</summary>
    /// <returns>The data range of the axes with whether its patches can reach past the plot.</returns>
    internal AxesRange RangeOfAxes() => ranges is { } drawn ? drawn.RangeOf(axes) : axes.RangeOf(theme);

    /// <summary>The transform the primary series are drawn with over <paramref name="range"/>: the primary Y axis' scale and
    /// breaks beside the primary X axis' (<see cref="ViewTransform"/>).</summary>
    /// <param name="range">The axes' range (<see cref="ComputeDataRanges"/>).</param>
    internal DataTransform PrimaryView(DataRange range) => ViewTransform(range, axes.YAxis, axes.YBreaks);

    /// <summary>The transform that maps <paramref name="range"/> onto the plot area, for a Y axis
    /// <paramref name="yAxis"/> beside the primary X axis, by the one rule every 2-D transform of these axes is built by
    /// (<see cref="DataTransform.Of"/>): axis breaks compress it, and on a log or symlog axis the pixel mapping works in the
    /// scaled space (<see cref="ScaleRule.Forward"/>) so that ticks at 100, 1000, 10000 land evenly spaced.</summary>
    /// <param name="range">The X range and the Y axis' own range.</param>
    /// <param name="yAxis">The Y axis: its scale and symlog threshold.</param>
    /// <param name="yBreaks">The breaks of that Y axis.</param>
    private DataTransform ViewTransform(DataRange range, Axis yAxis, IReadOnlyList<AxisBreak> yBreaks) =>
        DataTransform.Of(range, plotArea, AxisMapping.Of(axes.XAxis, axes.XBreaks, theme, isX: true), AxisMapping.Of(yAxis, yBreaks, theme, isX: false));

    /// <summary>The primary tick values of the X axis (<paramref name="isX"/>) or the Y axis the axes draws for
    /// <paramref name="range"/>, by the one rule every 2-D axis lays its ticks out by
    /// (<see cref="AxesTickSourceExtensions"/>' <c>TickValuesBetween</c>): the locator of <paramref name="source"/>,
    /// otherwise the axis' tick spacing, otherwise the default placement for the plot's width or height; ticks inside a break
    /// of that axis are left out (they would stand in empty space or on the break marker). An axis whose parts stand apart
    /// (<see cref="AxisBreakMap.IsDrawnInParts"/>, under a theme whose subplots stand a share apart) ticks each part as an axis of its own, over
    /// its own range and the length it takes on the plot, as each subplot of matplotlib's broken-axis recipe does.</summary>
    /// <param name="range">The data range the axes draws.</param>
    /// <param name="source">Where the primary ticks come from: the locator that places them, and how they read.</param>
    /// <param name="isX"><see langword="true"/> for the X axis, <see langword="false"/> for the Y axis.</param>
    /// <returns>The tick values.</returns>
    internal double[] PrimaryTicks(DataRange range, TickSource source, bool isX)
    {
        var shown = range.Along(isX);
        var axis = axes.AxisAlong(isX);
        double length = plotArea.LengthAlong(isX);
        var breaks = axes.BreaksAlong(isX);
        if (breaks.Count == 0) return axis.TickValuesBetween(source, shown, length, theme, isX);

        var map = AxisMapping.Of(axis, breaks, theme, isX).MapOver(shown.Lo, shown.Hi)!;
        if (!map.IsDrawnInParts) return OutsideTheBreaks(axis.TickValuesBetween(source, shown, length, theme, isX), map);

        double drawn = Math.Abs(map.CompressedRange.Max - map.CompressedRange.Min);
        var ticks = new List<double>();
        foreach (var part in map.Parts)
        {
            double partLength = length * Math.Abs(map.Remap(part.Hi) - map.Remap(part.Lo)) / drawn;
            foreach (double tick in axis.TickValuesBetween(source, part, partLength, theme, isX))
                if (tick >= part.Lo && tick <= part.Hi) ticks.Add(tick);
        }

        return [.. ticks];

        // The ticks no break hides, in order.
        static double[] OutsideTheBreaks(double[] ticks, AxisBreakMap map)
        {
            int kept = 0;
            foreach (double tick in ticks) if (!map.IsInBreak(tick)) kept++;
            var outside = new double[kept];
            kept = 0;
            foreach (double tick in ticks) if (!map.IsInBreak(tick)) outside[kept++] = tick;
            return outside;
        }
    }

    /// <summary>The primary Y axis, left slot 0 of the container, placed over <paramref name="range"/>: the axes' own range and
    /// <paramref name="transform"/>, the ticks its grid is drawn through (<see cref="PrimaryTicks"/>) and their source, hung from the
    /// left spine wherever it was moved to. The drawing and the measuring read its ticks by the one context
    /// (<see cref="PlacedAxis.TickDraw"/>), so the band reserved is the band drawn.</summary>
    /// <param name="range">The axes' range (<see cref="ComputeDataRanges"/>).</param>
    /// <param name="transform">The transform the primary series are drawn with.</param>
    /// <returns>The placed primary, at offset 0 and with no band yet: its band is known once its tick labels are drawn.</returns>
    internal PlacedAxis PlacePrimary(DataRange range, DataTransform transform)
    {
        var source = axes.PrimaryTickSource(isX: false, theme);
        var slot = axes.YAxes.Left[0];
        return new PlacedAxis
        {
            Axis = slot.Axis,
            Edge = EdgeTerms.Left,
            Range = range,
            Transform = transform,
            Ticks = PrimaryTicks(range, source, isX: false),
            Source = source,
            Series = slot.Series,
            Pair = SeriesAxisPair.Primary,
            EdgeLine = EdgeTerms.Left.SpineAt(axes.Spines, plotArea, transform),
        };
    }

    /// <summary>The primary X axis placed over <paramref name="range"/>: the axes' own range and <paramref name="transform"/>, the
    /// ticks its grid is drawn through (<see cref="PrimaryTicks"/>) and their source, hung from the bottom spine wherever it was moved
    /// to. Its series are the axes' own, drawn by the data layer.</summary>
    /// <param name="range">The axes' range (<see cref="ComputeDataRanges"/>).</param>
    /// <param name="transform">The transform the primary series are drawn with.</param>
    /// <returns>The placed primary X axis.</returns>
    internal PlacedAxis PlacePrimaryX(DataRange range, DataTransform transform)
    {
        var source = axes.PrimaryTickSource(isX: true, theme);
        return new PlacedAxis
        {
            Axis = axes.XAxis,
            Edge = EdgeTerms.Bottom,
            Range = range,
            Transform = transform,
            Ticks = PrimaryTicks(range, source, isX: true),
            Source = source,
            EdgeLine = EdgeTerms.Bottom.SpineAt(axes.Spines, plotArea, transform),
        };
    }

    /// <summary>The ONE place a Y axis slot past the primary is placed beside the primary's range
    /// <paramref name="primary"/>: its own range (<see cref="ComputeSecondaryDataRanges"/>), the transform its series and
    /// ticks are drawn with (the rule the primary's is built by: through the slot's own scale and symlog threshold, beside the
    /// primary X axis' scale and breaks) and the ticks it places with their source (<see cref="AxesTickSourceExtensions"/>'
    /// <c>SlotTickSource</c>: its locator, or its scale's). The draw, the layout's reserve and the interaction views all place a
    /// slot here, so none of them works its range or transform out a second way.</summary>
    /// <remarks>A linked slot (<see cref="YAxisSlot.IsLinked"/>) has no range of its own and is placed on
    /// <paramref name="primary"/> by the rule of a linked axis; any other slot reads only its X extent, which it shares. The
    /// caller hands over the range it already holds, so placing a slot never works the primary's range out again.</remarks>
    /// <param name="slot">The slot.</param>
    /// <param name="primary">The primary's range (<see cref="ComputeDataRanges"/>).</param>
    /// <returns>The slot with its range, transform and ticks; its identity, band and offset are the caller's to add.</returns>
    internal PlacedAxis PlaceSlot(YAxisSlot slot, DataRange primary)
    {
        if (slot.Axis.Link is { } link) return PlaceLinkedSlot(slot, link, primary);
        var range = ComputeSecondaryDataRanges(primary.XMin, primary.XMax, slot.Axis, slot.Series);
        return new PlacedAxis
        {
            Axis = slot.Axis,
            Edge = EdgeTerms.Of(slot.Side),
            Series = slot.Series,
            Range = range,
            Transform = ViewTransform(range, slot.Axis, []),
            Ticks = slot.Axis.SlotTickValues(range, plotArea.Height, theme),
            Source = slot.Axis.SlotTickSource(theme),
        };
    }

    /// <summary>A slot linked to the primary Y axis (<see cref="Axis.Link"/>, matplotlib's <c>secondary_yaxis</c>) placed: on
    /// the primary's range <paramref name="range"/> and transform, with no range of its own, its ticks laid out by the one rule
    /// of a linked axis (<see cref="LayOutLinkedTicks"/>), the rule the linked top axis follows too.</summary>
    /// <param name="slot">The linked slot.</param>
    /// <param name="link">Its link.</param>
    /// <param name="range">The primary's range.</param>
    /// <returns>The slot placed; its identity, band and offset are the caller's to add.</returns>
    private PlacedAxis PlaceLinkedSlot(YAxisSlot slot, AxisTransform link, DataRange range)
    {
        var transform = PrimaryView(range);
        var linked = LayOutLinkedTicks(slot.Axis, link, transform, isX: false, "Y");
        return new PlacedAxis
        {
            Axis = slot.Axis,
            Edge = EdgeTerms.Of(slot.Side),
            Series = slot.Series,
            Range = range,
            Transform = transform,
            Placement = linked.Placement,
            Ticks = linked.Ticks,
            Source = linked.Source,
            Diagnostic = linked.Diagnostic,
        };
    }

    /// <summary>The ONE place the top X axis is placed, for the primary's range <paramref name="primary"/>
    /// (<see cref="ComputeDataRanges"/>). The drawing, the band the layout reserves for it and the marks a legend keeps clear of
    /// all place it here, so none of them works its range, transform or ticks out a second way.
    /// <para>A top axis linked to the primary X axis (<see cref="Axis.Link"/>, matplotlib's <c>secondary_xaxis</c>) has no range
    /// of its own: it takes <paramref name="primary"/> and the primary's transform, and its ticks are laid out by the one rule of
    /// a linked axis (<see cref="LayOutLinkedTicks"/>), the rule a linked Y slot follows. Any other top axis is placed over its
    /// own range (<see cref="ComputeSecondaryXDataRanges"/>), through its own scale (a log top axis draws its series and its
    /// ticks on its log scale), with its ticks by its own locator, else the one its scale brings, else its tick spacing, else the
    /// default placement along <see cref="TopAxisLength"/> (<see cref="AxesTickSourceExtensions"/>' <c>OwnTickSource</c> and
    /// <c>TickValuesBetween</c>).</para></summary>
    /// <remarks>The Y of a top axis with a range of its own is the primary's: the same range through the primary's Y scale and
    /// breaks (matplotlib's <c>twiny</c> shares the Y axis), so a value stands at the height the primary gives it on a log, symlog
    /// or broken Y axis.</remarks>
    /// <param name="primary">The primary axes' range: a linked top axis shows its X range, and a top axis with a range of its own
    /// shares its Y range.</param>
    /// <returns>The top axis placed: its own series numbered on their own list, standing at the plot's top edge.</returns>
    internal PlacedAxis PlaceTopAxis(DataRange primary)
    {
        var top = axes.SecondaryXAxis!;
        PlacedAxis placed;
        if (top.Link is { } link)
        {
            placed = PlaceLinkedXAxis(top, link, primary, EdgeTerms.Top, "top X");
        }
        else
        {
            var range = ComputeSecondaryXDataRanges(primary.YMin, primary.YMax);
            var source = top.OwnTickSource(isX: true, theme);
            placed = new PlacedAxis
            {
                Axis = top,
                Edge = EdgeTerms.Top,
                Range = range,
                Transform = DataTransform.Of(range, plotArea, AxisMapping.Of(top, [], theme, isX: true), AxisMapping.Of(axes.YAxis, axes.YBreaks, theme, isX: false)),
                Ticks = top.TickValuesBetween(source, new Range1D(range.XMin, range.XMax), TopAxisLength, theme, isX: true),
                Source = source,
            };
        }
        return placed with
        {
            Series = axes.XSecondarySeries,
            Pair = SeriesAxisPair.SecondaryX,
            EdgeLine = EdgeTerms.Top.EdgeAt(plotArea, 0),
        };
    }

    /// <summary>The top axis placed when it is drawn (<see cref="Axes.DrawnSecondaryXAxis"/>: it has series, or it is linked), by
    /// <see cref="PlaceTopAxis"/>; <see langword="null"/> when the axes has none or it is placed nowhere it is seen.</summary>
    /// <param name="primary">The primary axes' range.</param>
    /// <returns>The top axis placed, or <see langword="null"/>.</returns>
    internal PlacedAxis? PlaceDrawnTopAxis(DataRange primary) => axes.DrawnSecondaryXAxis is null ? null : PlaceTopAxis(primary);

    /// <summary>The ONE place the linked X axes below the plot (<see cref="AxisSide.Bottom"/> of <see cref="Axes.YAxes"/>, matplotlib's
    /// <c>secondary_xaxis('bottom')</c>) are placed and stacked, once for <paramref name="range"/>: each by the rule a linked top axis is
    /// placed by, on the primary's range and transform (a bottom axis given no link has nothing to show and no ticks), drawing the bottom
    /// spine's line as its own. An axis given a location (<see cref="YAxisSlot.Location"/>) stands that fraction of the plot's height below the bottom spine; any other
    /// stands the container's gap below the lowest thing above it: what the primary X axis draws past the plot's edge, its tick labels
    /// and title hung from its spine wherever the spine stands (<paramref name="primaryPastEdge"/>, nothing where they stand inside the
    /// plot), then each axis before it. Each carries its offset, its edge and its band: from its edge to the far
    /// side of its tick labels and title (<see cref="AxesTitleExtensions"/>' <c>BandsBelow</c>), measured as its part draws them. The
    /// layout reserves the stack by it and the drawing draws each axis where it stands.</summary>
    /// <param name="range">The primary axes' range.</param>
    /// <param name="primaryPastEdge">From the plot's bottom edge to the far side of what the primary X axis draws past it
    /// (<see cref="SpineInsets.PastBottom"/> of its band below its spine).</param>
    /// <returns>Each axis of the bottom stack placed, in the stack's order.</returns>
    internal PlacedAxis[] PlaceBottomAxes(DataRange range, double primaryPastEdge)
    {
        var stack = axes.YAxes.Bottom;
        var placed = new PlacedAxis[stack.Count];
        double gap = axes.YAxes.TitleGapUnder(theme);
        double lowest = primaryPastEdge;
        for (int i = 0; i < placed.Length; i++)
        {
            var slot = stack[i];
            var axis = (slot.Axis.Link is { } link
                ? PlaceLinkedXAxis(slot.Axis, link, range, EdgeTerms.Bottom, "bottom X")
                : new PlacedAxis
                {
                    Axis = slot.Axis, Edge = EdgeTerms.Bottom, Range = range, Transform = PrimaryView(range), Ticks = [],
                    Source = new TickSource(null, slot.Axis.TickFormatter, false),
                }) with { Line = axes.Spines.Bottom };
            double offset = stack[i].Location is { } location ? -location * plotArea.Height : lowest + gap;
            axis = axis with { Offset = offset, EdgeLine = EdgeTerms.Bottom.EdgeAt(plotArea, offset) };
            double band = axis.Axis.BandsBelow(axes.TitleOf(axis.Axis, theme), ctx, theme, axis.OwnSideLabelReach(ctx, theme, axes.Spines)).Extent;
            placed[i] = axis with { Band = band };
            lowest = Math.Max(lowest, offset + band);
        }
        return placed;
    }

    /// <summary>A linked X axis, the top one or one below the plot, placed on the primary's range and transform, its ticks laid out
    /// by the one rule of a linked axis (<see cref="LayOutLinkedTicks"/>).</summary>
    /// <param name="axis">The linked axis.</param>
    /// <param name="link">Its link.</param>
    /// <param name="primary">The primary's range.</param>
    /// <param name="edge">The edge the axis hangs its ticks from.</param>
    /// <param name="name">Which axis it is, as a report names it.</param>
    private PlacedAxis PlaceLinkedXAxis(Axis axis, AxisTransform link, DataRange primary, EdgeTerms edge, string name)
    {
        var view = PrimaryView(primary);
        var linked = LayOutLinkedTicks(axis, link, view, isX: true, name);
        return new PlacedAxis
        {
            Axis = axis,
            Edge = edge,
            Range = primary,
            Transform = view,
            Placement = linked.Placement,
            Ticks = linked.Ticks,
            Source = linked.Source,
            Diagnostic = linked.Diagnostic,
        };
    }

    /// <summary>The length the default placement lays the ticks of a top axis with a range of its own along: the plot's width
    /// under a theme whose bins follow the length (<see cref="TickLadder.BinsByLength"/>), as matplotlib's <c>twiny</c> axis has
    /// the width of the axes it twins; otherwise <see cref="AutoLocator.FullDensityLength"/>, so the library's placement draws
    /// the eight intervals a top axis that sets nothing has always drawn, whatever the plot's width. The layout measures the
    /// band and the draw lays the ticks at the same length, the plot's width being the drawn one in both.</summary>
    private double TopAxisLength => theme.Looks.Ticks.Ladder is { BinsByLength: true } ? plotArea.Width : AutoLocator.FullDensityLength;

    /// <summary>The ONE rule a linked axis lays its ticks out by, the top X axis (<paramref name="isX"/>) and a Y slot past the
    /// primary alike: over the image, in the linked unit, of the range the primary shows that way (the drawn range of
    /// <paramref name="primary"/>), low to high whichever way the link runs (<see cref="AxisTransform.ImageOf"/>), from the
    /// axis' tick source (<see cref="AxesTickSourceExtensions"/>' <c>LinkedTickSource</c>: its own locator, the log locator on a
    /// log primary, as matplotlib's <c>functionlog</c> scale, otherwise its spacing or the default placement for the plot's
    /// length that way). Each tick stands where the primary shows the value it maps back to (<see cref="LinkedTickPlacement"/>),
    /// and one the primary cannot place (inside a break, or where the link has no inverse) is left out. When the link cannot
    /// place the primary's range there are no ticks, and the diagnostic the drawing reports says why.</summary>
    /// <param name="axis">The linked axis: its locator, spacing and formatter.</param>
    /// <param name="link">Its link.</param>
    /// <param name="primary">The primary's transform, over the primary's range.</param>
    /// <param name="isX">An X axis (the top one or one below the plot) when <see langword="true"/>, a Y slot past the primary
    /// otherwise.</param>
    /// <param name="name">Which axis it is, as the report of a range the link cannot place names it.</param>
    /// <returns>Where the ticks stand, the ticks, their source and, when the link cannot place the range, the diagnostic.</returns>
    private LinkedTicks LayOutLinkedTicks(Axis axis, AxisTransform link, DataTransform primary, bool isX, string name)
    {
        var drawn = primary.DrawnRange;
        var placement = LinkedTickPlacement.Along(isX, primary, link);
        var source = axis.LinkedTickSource(axes.AxisAlong(isX).Scale.Rule);
        var shown = drawn.Along(isX);
        if (link.ImageOf(shown) is not { } image)
            return new LinkedTicks(placement, [], source, UnplacedLinkDiagnostic(name, shown));

        double[] ticks = [.. axis.TickValuesBetween(source, image.Interval, plotArea.LengthAlong(isX), theme, isX)
            .Where(tick => placement.PixelAlong(tick, isX).IsDrawable())];
        return new LinkedTicks(placement, ticks, source, null);
    }

    /// <summary>The ticks of a linked axis as one placement lays them out (<see cref="LayOutLinkedTicks"/>).</summary>
    /// <param name="Placement">Where a tick of a given value stands: where the primary shows the value it maps back to.</param>
    /// <param name="Ticks">The tick values, in the linked unit; none when the link cannot place the primary's range.</param>
    /// <param name="Source">Where the ticks come from and how their labels are written.</param>
    /// <param name="Diagnostic">Why there are no ticks, when the link cannot place the primary's range; otherwise
    /// <see langword="null"/>.</param>
    private readonly record struct LinkedTicks(LinkedTickPlacement Placement, double[] Ticks, TickSource Source,
        Diagnostics.ChartDiagnostic? Diagnostic);

    /// <summary>What a linked axis reports when its link cannot place the primary's range <paramref name="shown"/>. The report
    /// names the renderer that draws the axis, as it always has.</summary>
    /// <param name="axis">Which axis it is, as the message names it: "Y", "top X" or "bottom X".</param>
    /// <param name="shown">The range the primary shows along that axis.</param>
    private static Diagnostics.ChartDiagnostic UnplacedLinkDiagnostic(string axis, Range1D shown) => new(nameof(CartesianAxesRenderer),
        string.Invariant(
            $"A linked {axis} axis draws no ticks: its transform cannot show the primary range {shown.Lo}..{shown.Hi} (a reciprocal across 0, a logarithm or a power reaching below 0, or a function that is no number at an end or maps both ends to one value)."),
        null);

    /// <summary>From a placed slot's edge to the far side of what it draws, as its part draws it: its tick labels measured
    /// in the context its part draws them with (<see cref="PlacedAxis.OwnSideLabelReach"/>: none for a slot that does not label its own
    /// side), then its title by the one band rule (<see cref="YAxisTitleExtensions"/>) with the container's gap. The layout reserves
    /// every slot by this band and the draw stacks the slots by it.</summary>
    /// <param name="placed">The slot as placed (<see cref="PlaceSlot"/>).</param>
    /// <returns>The slot's band, in pixels.</returns>
    internal double SlotBand(PlacedAxis placed)
    {
        var axis = placed.Axis;
        double widest = placed.OwnSideLabelReach(ctx, theme, axes.Spines);
        return axis.YTitleExtent(axes.TitleOf(axis, theme), axes.YAxes.TitleInputUnder(theme, placed.Edge.ContainerSide!.Value,
            axis.MajorTicksUnder(theme).TickLabelBand(widest))).Band;
    }

    /// <summary>Places every Y axis slot of the container for one draw, once each, in identity order
    /// (<see cref="SeriesSlotExtensions.YSlotSeats"/>): the primary as it was drawn (<paramref name="primary"/>, its band
    /// that of the tick labels it drew), then every slot past it, each with its range, transform and ticks
    /// (<see cref="PlaceSlot"/>), the place its series hold in the identity order, its band (<see cref="SlotBand"/>) and its
    /// offset: each side stacked once, nearest the plot first, with the container's gap between each pair
    /// (<see cref="AxisStackingExtensions.StackedOffsets"/>), the rule the layout reserves the sides by.</summary>
    /// <param name="primary">The primary Y axis as placed and drawn.</param>
    /// <returns>Every slot placed, the primary first, each past it standing at its place past the plot's edge, the edge itself at offset
    /// 0, as matplotlib's twin axes stand at the axes' edge whatever the host's spine does (<see cref="PlacedAxis.EdgeLine"/>).</returns>
    internal List<PlacedAxis> PlaceYSlots(PlacedAxis primary)
    {
        // The primary alone stands at the plot's edge (offset 0) with nothing to stack past it.
        if (axes.YAxes.Left.OutwardCount == 0 && axes.YAxes.Right.OutwardCount == 0)
            return new List<PlacedAxis>(1) { primary };

        var placed = new List<PlacedAxis> { primary };
        var seats = axes.YSlotSeats();
        for (int i = 1; i < seats.Count; i++)
        {
            var seat = seats[i];
            var slot = PlaceSlot(seat.Slot, primary.Range) with { Pair = seat.Pair, FirstIndexInPair = seat.FirstIndexInPair };
            placed.Add(slot with { Band = SlotBand(slot) });
        }

        // Each side stacked once, its slots in stack order, the primary first on the left.
        foreach (var edge in new[] { EdgeTerms.Right, EdgeTerms.Left })
        {
            // The places of the slots on this side in the list of every placed slot, and their bands, in stack order.
            int count = 0;
            foreach (var slot in placed) if (ReferenceEquals(slot.Edge, edge)) count++;
            var onSide = new int[count];
            var bands = new double[count];
            for (int at = 0, k = 0; at < placed.Count; at++)
            {
                if (!ReferenceEquals(placed[at].Edge, edge)) continue;
                onSide[k] = at;
                bands[k++] = placed[at].Band;
            }

            var offsets = bands.StackedOffsets(axes.YAxes.TitleGapUnder(theme));
            for (int i = 0; i < onSide.Length; i++)
            {
                int at = onSide[i];
                // The primary, kept as it was placed, hangs from the left spine; a slot past it is a twin and stands at its place past
                // the plot's edge (the edge itself at offset 0), whatever the host's spine on that side does, as matplotlib's twinx.
                if (at > 0) placed[at] = placed[at] with { Offset = offsets[i], EdgeLine = edge.EdgeAt(plotArea, offsets[i]) };
            }
        }
        return placed;
    }

    /// <summary>Computes the Y data range for <paramref name="series"/> plotted against <paramref name="axis"/> — one
    /// slot's own range, independent of every other slot's (each <see cref="YAxisSlot"/> keeps its own series, so nothing
    /// here folds two slots' data together).</summary>
    /// <param name="xMin">The primary axes' X minimum, shared by every slot past the primary.</param>
    /// <param name="xMax">The primary axes' X maximum, shared by every slot past the primary.</param>
    /// <param name="axis">The slot's own axis (its set limits and scale orientation).</param>
    /// <param name="series">The slot's own series — never another slot's.</param>
    internal DataRange ComputeSecondaryDataRanges(double xMin, double xMax, Axis axis, IReadOnlyList<ISeries> series)
    {
        var drawn = DrawnOnAxisPastPrimary(axis, series, isX: false);
        return new DataRange(xMin, xMax, drawn.Lo, drawn.Hi);
    }

    /// <summary>Computes the X data range for series plotted against the secondary X-axis: its series' extent, a set limit
    /// exact, then by the rules a Y slot's range follows (<see cref="ComputeSecondaryDataRanges"/>): on a log scale a floor it
    /// cannot place lifted and the margin taken in log space, a set side left as set, an inverted axis drawn high to low.</summary>
    /// <param name="yMin">The primary's Y minimum, which the top axis shares.</param>
    /// <param name="yMax">The primary's Y maximum, which the top axis shares.</param>
    /// <returns>The top axis' own X range beside the primary's Y range.</returns>
    internal DataRange ComputeSecondaryXDataRanges(double yMin, double yMax)
    {
        var drawn = DrawnOnAxisPastPrimary(axes.SecondaryXAxis!, axes.XSecondarySeries, isX: true);
        return new DataRange(drawn.Lo, drawn.Hi, yMin, yMax);
    }

    /// <summary>The ONE fold of an axis past the primary, a Y slot and the top X axis alike: its set limits
    /// (<see cref="AxisAutoscale.Seed"/>), each of its own series' extent along it (the X extent on the top axis
    /// <paramref name="isX"/>, the Y extent on a slot), asked under that axis' own scale
    /// (<see cref="AxesContextAdapter.PastPrimary"/>), then drawn as such an axis is drawn
    /// (<see cref="AxisAutoscale.DrawnPastPrimary"/>), a logit axis ending at the smallest positive value its series reach, the
    /// primary's rule (<c>SmallestPositive</c>).</summary>
    /// <param name="axis">The axis: its set limits, scale and orientation.</param>
    /// <param name="series">Its own series, never another axis'.</param>
    /// <param name="isX">The top X axis when <see langword="true"/>, a Y slot otherwise.</param>
    /// <returns>The axis' drawn range.</returns>
    private Range1D DrawnOnAxisPastPrimary(Axis axis, IReadOnlyList<ISeries> series, bool isX)
    {
        var auto = new AxisAutoscale(axis, isX, theme);
        var context = AxesContextAdapter.PastPrimary(axes, axis, series, isX);
        var folded = auto.Seed;
        double? smallestPositive = null;
        bool asked = false;
        var contributions = new DataRangeContribution[series.Count];
        for (int i = 0; i < series.Count; i++)
        {
            asked |= series[i].AsksForTheAutoscale();
            // ISeries.ComputeDataRange, not an `is IHasDataRange` test: every series carries the member, but not every one carries that
            // separate marker interface (a streaming series does not), and a series it skipped was drawn off the plot area.
            var c = series[i].DataRangeUnder(context, theme);
            contributions[i] = c;
            folded = folded.Merge(c.MinAlong(isX), c.MaxAlong(isX));
            smallestPositive = c.SmallestPositiveWith(smallestPositive, isX);
        }

        return auto.DrawnPastPrimary(folded, smallestPositive, contributions, asked);
    }
}
