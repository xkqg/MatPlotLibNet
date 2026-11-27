// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The series lists of an axes, one per axis pair the series are drawn against.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder, remove, or renumber — the order here is drawing
/// order, which must match <see cref="Axes.AllSeries"/>'s concatenation order for <see cref="SeriesSlotExtensions.SlotIndex"/>
/// to agree with it.</remarks>
internal enum SeriesAxisPair
{
    /// <summary><see cref="Axes.Series"/>: the bottom X axis and the left Y axis.</summary>
    Primary,

    /// <summary>Every right slot's own series (<see cref="YAxisContainer.Right"/>), in slot order, numbered as one running
    /// sequence across every slot.</summary>
    SecondaryY,

    /// <summary>Every left slot's own series past the primary (<see cref="YAxisContainer.Left"/>), in slot order,
    /// numbered as one running sequence across every slot, kept apart from <see cref="SecondaryY"/>'s.</summary>
    SecondaryLeftY,

    /// <summary><see cref="Axes.XSecondarySeries"/>: the top X axis and the left Y axis.</summary>
    SecondaryX,
}

/// <summary>What the theme hands out at one colour-cycle slot: the colour of the slot, the cycler's set there, and the theme itself, which
/// the series that take a colour or a size of the theme's own are read through (<see cref="Theme.ViolinBodyColor"/>, <see cref="Theme.Looks"/>).
/// Handed by value to every series renderer and legend swatch on every draw, so it holds the three things that differ from slot to slot
/// and reads every look through the theme.</summary>
/// <param name="CycleColor">The colour of the slot: the <see cref="Theme.PropCycler"/>'s colour when the theme has a
/// cycler, otherwise <see cref="Theme.CycleColors"/> at the slot, wrapping around. A series renderer receives it as
/// the fallback for a series that names no colour of its own.</param>
/// <param name="CycledProperties">The cycler's whole set for the slot (colour, dash, marker, width), handed to the series
/// renderer alongside the colour; <see langword="null"/> when the theme has no cycler.</param>
/// <param name="Theme">The theme that handed the slot out. What it names for a kind of series (the colour of a violin, a quiver, a scatter or
/// a box: <see cref="ThemeColorRole"/>), the width it draws a line with, the outline of a line's markers, whether a verb's colour is kept:
/// the plot and the legend handle must agree on all of them, so both read them here.</param>
/// <remarks>The slot also decides how a series is stroked. A series hands it the line style and line width it was given, or
/// <see langword="null"/> for one it was not (the series keeps a given value apart from the one it reads at rest), and a value
/// that was not given is the cycler's entry for the slot, then the theme's; a given one is kept, as an explicit colour is, even
/// when it equals what a new series reads. The JSON writes a given value only, so a figure draws the same picture before and
/// after a round trip. A cycler that lists only colours hands out solid and no marker, and leaves the width to the
/// theme.</remarks>
internal readonly record struct SeriesSlot(Color CycleColor, CycledProperties? CycledProperties, Theme Theme)
{
    /// <summary>The marker a scatter point is drawn with when neither its series nor the cycler names one.</summary>
    internal const MarkerStyle RestingScatterMarker = MarkerStyle.Circle;

    /// <summary>The size across, in points, that a marker given no size is drawn at where its theme names none (a scatter's 36
    /// points squared, the diameter of a polar, ternary or 3-D scatter).</summary>
    internal const double RestingScatterSize = 6;

    /// <summary>The colour <paramref name="series"/> is drawn in: its own <see cref="IHasColor.Color"/> when it sets
    /// one, otherwise the colour its theme gives a series of its kind (<see cref="ThemeColorOf"/>: a violin's body, a quiver, its
    /// key and barbs that share no colour group with other series, a scatter's face or a box's outline under a theme that
    /// names one), otherwise the colour a verb chose for it or the slot's colour
    /// (<see cref="UnnamedColorOf"/>), at the saturation the kind's seaborn verb keeps of it (<see cref="ISaturatedFill"/>: a count
    /// plot's bars). The legend swatch (and anything else that names a series by its colour) reads this, so it shows what the series
    /// renderer draws.</summary>
    /// <param name="series">The series that holds this slot.</param>
    /// <returns>The series' explicit colour, else the theme's colour for its kind, else <see cref="UnnamedColorOf"/>.</returns>
    internal Color ColorOf(ISeries series)
    {
        if (((series as IHasColor)?.Color ?? ThemeColorOf(series)) is { } named) return named;
        var unnamed = UnnamedColorOf(series);
        return series is ISaturatedFill fill ? unnamed.Desaturated(fill.SaturationUnder(Theme)) : unnamed;
    }

    /// <summary>The colour <paramref name="series"/> is drawn in where neither it nor its theme names one for its kind: the colour a
    /// verb chose for it (<see cref="ChartSeries.VerbColor"/>) under a theme that keeps the verbs' colours
    /// (the library's own themes, not <see cref="CycleLook.GroupsWearTheCycle"/>), otherwise the slot's colour. What a series pass hands a renderer as
    /// the colour of a series that names none, so the picture and the legend agree.</summary>
    /// <param name="series">The series that holds this slot.</param>
    /// <returns>The verb's colour, else <see cref="CycleColor"/>.</returns>
    internal Color UnnamedColorOf(ISeries series) => ChosenOrCycleColor((series as ChartSeries)?.VerbColor);

    /// <summary>The colour of a part that names none: the colour its verb chose for it under a theme that keeps the verbs' colours (the
    /// library's own themes), otherwise the slot's colour. The one rule behind a series (<see cref="UnnamedColorOf"/>) and a reference line
    /// (<see cref="ReferenceLine.VerbColor"/>).</summary>
    /// <param name="chosen">The colour the verb chose; <see langword="null"/> when none did.</param>
    /// <returns>The colour of the part.</returns>
    internal Color ChosenOrCycleColor(Color? chosen) =>
        (Theme.Looks.Cycle.GroupsWearTheCycle ? null : chosen) ?? CycleColor;

    /// <summary>The colour the theme gives a series of <paramref name="series"/>' kind, or <see langword="null"/> when it gives
    /// none: the one its kind's role names (<see cref="MatplotlibArtist.ThemeColor"/>, <see cref="Rendering.SeriesSlot"/>), inside a
    /// colour group the violin's body only.</summary>
    private Color? ThemeColorOf(ISeries series) =>
        series is ChartSeries chart ? ColorUnder(chart.Artist.ThemeColor, Theme, chart.ColorGroup is not null) : null;

    /// <summary>The size across, in points, a scatter marker is drawn at: the size its series was given, otherwise the theme's when
    /// the theme names one (<see cref="ScatterLook.Area"/>, matplotlib's classic <c>s = 20</c>), otherwise
    /// <see cref="RestingScatterSize"/>. Per-point sizes are never replaced: matplotlib's default is for a call that gives no
    /// <c>s</c> at all.</summary>
    /// <param name="given">The size across, in points, the series was given; <see langword="null"/> when it was given none.</param>
    /// <returns>The size across, in points, to draw the marker at.</returns>
    internal double ScatterSizeOf(double? given) =>
        given ?? (Theme.Looks.Scatter?.Area is { } area ? area.AreaAcrossInPoints : RestingScatterSize);

    /// <summary>The diameter, in pixels, a dot is drawn at where its author named none and seaborn leaves it to <c>Axes.scatter</c>
    /// (<c>residplot</c> and <c>pairplot</c> give <c>scatterplot</c> no size): <see cref="ScatterSizeOf"/> given none, at 100 dpi.</summary>
    internal double ScatterDiameter => ScatterSizeOf(null).PointsInPixels;

    /// <summary>The size across, in points, a 3-D scatter marker is drawn at: as <see cref="ScatterSizeOf"/>, but the theme's size is
    /// <see cref="ScatterLook.Area3D"/> (<c>Axes3D.scatter</c>'s own <c>s=20</c>, in every matplotlib style).</summary>
    /// <param name="given">The size across, in points, the series was given; <see langword="null"/> when it was given none.</param>
    /// <returns>The size across, in points, to draw the marker at.</returns>
    internal double Scatter3DSizeOf(double? given) =>
        given ?? (Theme.Looks.Scatter?.Area3D is { } area ? area.AreaAcrossInPoints : RestingScatterSize);

    /// <summary>Whether the markers of a 3-D scatter are all one size, as matplotlib's <c>Path3DCollection.do_3d_projection</c> keeps
    /// them (it sorts by depth and keeps the sizes), which a theme that names a scatter look does; <see langword="false"/> (the
    /// library's own themes) shrinks a marker with its distance, to half its size at the farthest.</summary>
    internal bool Scatter3DKeepsOneSize => Theme.Looks.Scatter is not null;

    /// <summary>The least opacity the markers of a 3-D scatter fade to with their distance (<see cref="ScatterLook.DepthShade"/>);
    /// <see langword="null"/> when the theme fades none.</summary>
    internal double? Scatter3DLeastOpacity => Theme.Looks.Scatter?.DepthShade;

    /// <summary>The edge a filled scatter marker is stroked with where its author names none: the theme's colour, or the marker's own
    /// <paramref name="face"/> (<c>scatter.edgecolors = 'face'</c>), at the theme's width; <see langword="null"/> when the theme
    /// names no edge.</summary>
    /// <param name="face">The colour the marker is filled with, as drawn (its opacity included).</param>
    /// <returns>The edge, or <see langword="null"/> for none.</returns>
    internal MarkerEdge? ScatterEdgeOf(Color face) =>
        Theme.Looks.Scatter is { EdgeWidth: { } width } look ? new MarkerEdge(look.EdgeColor ?? face, width) : null;

    /// <summary>The fill and the theme's edge of a filled scatter marker whose author names neither an edge colour nor a width (a polar,
    /// ternary or 3-D scatter has no edge of its own): <paramref name="fill"/> and <see cref="ScatterEdgeOf"/>.</summary>
    /// <param name="fill">The colour the marker is filled with.</param>
    /// <returns>The style to draw the marker with.</returns>
    internal ShapeStyle ScatterStyleOf(Color fill) => ScatterStyleOf(fill, 1);

    /// <summary>The style of a scatter marker faded to <paramref name="opacity"/> of its own: the fill and the edge, which follows the
    /// face (<c>scatter.edgecolors = 'face'</c>) or is the theme's colour, each faded once, as matplotlib's depth shading
    /// fades the face and the edge of a marker.</summary>
    /// <param name="fill">The colour the marker is filled with.</param>
    /// <param name="opacity">The share of its own opacity the marker keeps, 0 to 1.</param>
    /// <returns>The style to draw the marker with.</returns>
    internal ShapeStyle ScatterStyleOf(Color fill, double opacity)
    {
        var faded = fill.ScaleAlpha(opacity);
        return Theme.JoinedAs(MatplotlibArtist.Scatter, Theme.Looks.Scatter is { EdgeWidth: { } width } look
            ? ShapeStyle.Edged(faded, look.EdgeColor is { } edge ? edge.ScaleAlpha(opacity) : faded, width)
            : ShapeStyle.Filled(faded));
    }

    /// <summary>The style of a bubble: its <paramref name="color"/> at the series' <paramref name="opacity"/> as the fill, and, where the
    /// theme names a scatter edge (<see cref="ScatterEdgeOf"/>), that edge faded to the same opacity as the fill is; the library's own
    /// themes name none and keep the edge in the bubble's own colour, one pixel wide.</summary>
    /// <param name="color">The colour of the bubble, before the series' opacity.</param>
    /// <param name="opacity">The opacity of the series, 0 to 1.</param>
    /// <returns>The style to draw the bubble with.</returns>
    internal ShapeStyle BubbleStyleOf(Color color, double opacity)
    {
        var fill = color.WithOpacityGiven(opacity);
        return ScatterEdgeOf(fill) is { } edge
            ? ShapeStyle.Edged(fill, edge.Color.WithOpacityGiven(opacity), edge.Width)
            : ShapeStyle.Edged(fill, color, LineWidthOf(LineWidthRule.OwnPixel));
    }

    /// <summary>The colour of the lines drawn over a violin (its extrema, median and mean), by the rule of
    /// <see cref="ColorOf"/>: the colour the violin names, else the theme's colour for those lines
    /// (<see cref="Theme.ViolinStatsColor"/>), else the slot's colour.</summary>
    /// <param name="violin">The violin that holds this slot.</param>
    /// <returns>The violin's explicit colour, else the theme's colour for its lines, else <see cref="CycleColor"/>.</returns>
    internal Color StatsColorOf(ViolinSeries violin) => violin.Color ?? Theme.ViolinStatsColor ?? CycleColor;

    /// <summary>The dash pattern a line is drawn with: the one the series was given, otherwise the slot's.</summary>
    /// <param name="given">The line style the series was given; <see langword="null"/> when it was given none.</param>
    /// <returns><paramref name="given"/>, else the cycler's style for the slot, else solid (<see cref="GivenStroke.RestingStyle"/>).</returns>
    internal LineStyle LineStyleOf(LineStyle? given) => given ?? CycledProperties?.LineStyle ?? GivenStroke.RestingStyle;

    /// <summary>The width, in pixels, the line of <paramref name="series"/> is drawn with: its kind's rule over the width it was
    /// given (<see cref="LineWidthOf(double?, LineWidthRule)"/>).</summary>
    /// <param name="series">The kind that states its given width and its rule.</param>
    /// <returns>The width.</returns>
    internal double LineWidthOf(IWidthGiven series) => LineWidthOf(series.GivenLineWidth, series.WidthRule);

    /// <summary>The width, in pixels, a line of the library's that nobody gives a width is drawn with: <paramref name="rule"/> under this
    /// slot's theme and cycler.</summary>
    /// <param name="rule">The rule the line is drawn wide by.</param>
    /// <returns>The width.</returns>
    internal double LineWidthOf(LineWidthRule rule) => LineWidthOf(null, rule);

    /// <summary>The width, in pixels, a line of a kind is drawn with: the kind's rule over the width the series was given, the
    /// width the cycler names for the slot (<see cref="CycledWidth"/>) and the theme's (<see cref="SeriesLineLook.Width"/>). The one door
    /// every series line and its legend swatch take.</summary>
    /// <param name="given">The line width the series was given; <see langword="null"/> when it was given none.</param>
    /// <param name="rule">The rule of the series' kind.</param>
    /// <returns>The width, by <see cref="LineWidthRule.WidthOf"/>.</returns>
    internal double LineWidthOf(double? given, LineWidthRule rule) => rule.WidthOf(given, CycledWidth, Theme.Looks.Lines.Width);

    /// <summary>The marker drawn at each point of a line (or a step): the one the series names, including
    /// <see cref="MarkerStyle.None"/>, otherwise the cycler's marker for the slot.</summary>
    /// <param name="named">The marker the series names; <see langword="null"/> when it names none.</param>
    /// <returns>The marker to draw, or <see langword="null"/> when neither the series nor the cycler names one. A cycler
    /// marker of <see cref="MarkerStyle.None"/> names none: it is what a cycler that lists no markers holds.</returns>
    internal MarkerStyle? LineMarkerOf(MarkerStyle? named) => named ?? CycledMarker;

    /// <summary>The fill and the outline of a marker of a line (or a step), and how wide the outline is. Where the theme names no look
    /// (<see cref="MarkerLook.LineEdge"/>) a marker is outlined only when its author names an edge colour, as wide as its author says (1 when
    /// none), as it has always been drawn. Where it names one (the matplotlib themes) the outline is matplotlib's <c>Line2D</c> outline:
    /// the author's colour, else (<c>markeredgecolor = 'auto'</c>) the theme's colour for a filled marker (black in the classic style)
    /// or the colour of the line, as wide as its author says, else the theme's <c>lines.markeredgewidth</c>; an unfilled marker (a plus,
    /// a cross) is drawn by that outline alone, in the colour and width it gives, and the outline's corners are joined as matplotlib's
    /// marker of the shape joins them (<see cref="MarkerShape.Join"/>: the star bevelled).</summary>
    /// <param name="style">The marker's shape.</param>
    /// <param name="line">The colour of the line the marker sits on, which <c>'auto'</c> and an unnamed face take.</param>
    /// <param name="face">The fill the author named, <see langword="null"/> when none.</param>
    /// <param name="edge">The outline colour the author named, <see langword="null"/> when none.</param>
    /// <param name="edgeWidth">The outline width the author named, in pixels, <see langword="null"/> when none.</param>
    /// <returns>The style to draw the marker with.</returns>
    internal ShapeStyle LineMarkerStyleOf(MarkerStyle style, Color line, Color? face, Color? edge, double? edgeWidth)
    {
        var fill = face ?? line;
        if (Theme.Looks.Markers.LineEdge is not { } look)
            return edge is { } stroke ? ShapeStyle.Edged(fill, stroke, edgeWidth ?? Models.Series.LineSeries.RestingMarkerEdgeWidth) : ShapeStyle.Filled(fill);

        var shape = MarkerShape.Of(style);
        return shape.StyleOf(fill, edge ?? shape.AutoEdge(line, look.FilledEdge), edgeWidth ?? look.EdgeWidth) with { StrokeJoin = shape.Join };
    }

    /// <summary>The marker a scatter point is drawn with: the one the series was given, otherwise the slot's. A scatter is its
    /// markers, so a cycler marker of <see cref="MarkerStyle.None"/> (what a cycler that lists no markers holds) leaves the
    /// scatter's circle.</summary>
    /// <param name="given">The marker the series was given; <see langword="null"/> when it was given none.</param>
    /// <returns><paramref name="given"/>, else the cycler's marker for the slot, else <see cref="RestingScatterMarker"/>.</returns>
    internal MarkerStyle ScatterMarkerOf(MarkerStyle? given) => given ?? CycledMarker ?? RestingScatterMarker;

    /// <summary>The cycler's marker for the slot, or <see langword="null"/> when it has none or names
    /// <see cref="MarkerStyle.None"/>.</summary>
    private MarkerStyle? CycledMarker =>
        CycledProperties?.MarkerStyle is { } marker and not MarkerStyle.None ? marker : null;

    /// <summary>The width the theme's cycler names for the slot; <see langword="null"/> when there is no cycler or it names no widths (a
    /// cycler that names 1.5 names a width, and wins over the theme's for a kind the cycler names).</summary>
    private double? CycledWidth => Theme.PropCycler is { NamesLineWidths: true } ? CycledProperties?.LineWidth : null;

    /// <summary>The colour <paramref name="theme"/> gives a series of this role where its author names none: a violin's body, whether
    /// or not it is in a colour group (a raincloud's violin, box and points share one); a quiver, a quiver key, barbs, a scatter's face
    /// or a box's outline outside a colour group only, since a series a verb put in a group (a connected scatter's arrows) takes the
    /// group's colour; none for <see cref="ThemeColorRole.None"/>.</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <param name="inColorGroup">Whether a verb put the series in a colour group.</param>
    /// <returns>The colour, or <see langword="null"/> when the theme gives none.</returns>
    /// <param name="role">The <see cref="ThemeColorRole"/> it works on.</param>
    internal static Color? ColorUnder(ThemeColorRole role, Theme theme, bool inColorGroup) => role switch
    {
        ThemeColorRole.ViolinBody => theme.ViolinBodyColor,
        _ when inColorGroup => null,
        ThemeColorRole.VectorField => theme.Looks.VectorField.Color,
        ThemeColorRole.ScatterFace => theme.Looks.Scatter?.Face,
        ThemeColorRole.BoxOutline => theme.Looks.Box.Outline.Color,
        _ => null,
    };
}

/// <summary>The outline of a scatter marker: its colour and its width in pixels.</summary>
/// <param name="Color">The colour of the outline.</param>
/// <param name="Width">The width of the outline, in pixels.</param>
internal readonly record struct MarkerEdge(Color Color, double Width);

/// <summary>A Y axis slot of the container and where its series stand in the series lists that number them: the list
/// (<see cref="Pair"/>) and the index its first series has there, so series <c>i</c> of the slot is
/// <c>SlotIndex(Pair, FirstIndexInPair + i)</c>.</summary>
/// <param name="Slot">The slot.</param>
/// <param name="Pair">The list that numbers its series: <see cref="SeriesAxisPair.Primary"/> for the primary Y axis, the
/// side's own list for a slot stacked past it.</param>
/// <param name="FirstIndexInPair">The index its first series has in that list: the series of every slot nearer the plot
/// on the same list stand before it.</param>
internal readonly record struct YSlotSeat(YAxisSlot Slot, SeriesAxisPair Pair, int FirstIndexInPair);

/// <summary>The seats of a container's Y slots in identity order: the primary left slots, then every right slot past the primary, then every
/// left slot past it, each as a <see cref="YSlotSeat"/>. A view over the container that makes each seat when it is asked for, so the
/// series passes, the legend, the end labels and the layout, which walk it on every draw and every measure, allocate nothing; a
/// <c>foreach</c> over it uses the struct enumerator.</summary>
/// <param name="container">The container whose slots are numbered.</param>
internal readonly struct YSlotSeatList(YAxisContainer container) : IReadOnlyList<YSlotSeat>
{
    /// <summary>How many seats there are: every primary slot and every slot past it, on both sides.</summary>
    public int Count => container.Left.OutwardOffset + container.Right.OutwardCount + container.Left.OutwardCount;

    /// <summary>The seat at <paramref name="index"/> in identity order.</summary>
    /// <param name="index">The place in identity order, from 0.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the list.</exception>
    public YSlotSeat this[int index]
    {
        get
        {
            var left = container.Left;
            var right = container.Right;
            index.ThrowIfNotIndexOf(Count, nameof(index), "A seat of the container");

            if (index < left.OutwardOffset) return new YSlotSeat(left[index], SeriesAxisPair.Primary, 0);

            index -= left.OutwardOffset;
            return index < right.OutwardCount ? Outward(right, index) : Outward(left, index - right.OutwardCount);
        }
    }

    /// <summary>The seat of slot <paramref name="outward"/> past the primary of <paramref name="stack"/>: its series are numbered on the
    /// side's own list, after the series of every slot nearer the plot.</summary>
    private static YSlotSeat Outward(YAxisStack stack, int outward)
    {
        int first = 0;
        for (int k = 0; k < outward; k++) first += stack[stack.OutwardOffset + k].Series.Count;
        return new YSlotSeat(stack[stack.OutwardOffset + outward], EdgeTerms.Of(stack.Side).OutwardPair, first);
    }

    /// <summary>The struct enumerator a <c>foreach</c> uses.</summary>
    /// <returns>The enumerator of the seats.</returns>
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<YSlotSeat> IEnumerable<YSlotSeat>.GetEnumerator() => GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Walks the seats in identity order.</summary>
    /// <param name="list">The list whose seats are walked.</param>
    public struct Enumerator(YSlotSeatList list) : IEnumerator<YSlotSeat>
    {
        private int _index = -1;

        /// <inheritdoc />
        public readonly YSlotSeat Current => list[_index];

        readonly object System.Collections.IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext() => ++_index < list.Count;

        /// <inheritdoc />
        public void Reset() => _index = -1;

        /// <inheritdoc />
        public readonly void Dispose() { }
    }
}

/// <summary>The one owner of "the colour of series i" and of how it is stroked. The axes decides which slot a series
/// holds; the theme decides what that slot is worth.</summary>
/// <remarks>Every site that draws a series or names it by colour goes through here: the primary series passes in
/// <see cref="AxesRenderer"/>, the part of every Y axis slot, and the legend. Before this owner the parts that drew the
/// axes past the primary read <see cref="Theme.CycleColors"/> only while the legend read the <see cref="Theme.PropCycler"/>
/// first, so under a cycler theme a line on such an axis and its swatch wore different colours. The same goes for the dash, the marker and the
/// width: the series renderers and the legend swatch both ask the slot (the <c>SeriesSlot</c> value this hands out), and the
/// strokes a slot makes (a series' line from the stroke it was given under its kind's rule, and the lines a renderer draws in a
/// look of its own) are built here, the one place a colour, a width rule and a dash become a stroke for a series.</remarks>
internal static class SeriesSlotExtensions
{
    extension(Theme theme)
    {
        /// <summary>The colour a part of a figure that is no series wears when its author gave it none: the first colour of the theme's
        /// cycle, matplotlib's <c>C0</c> (a patch's face, a span's fill). It moves no cycle, so every such part wears the same one.</summary>
        internal Color FirstCycleColor => theme.SeriesSlot(0).CycleColor;

        /// <summary>What <paramref name="theme"/> hands out at slot <paramref name="slotIndex"/>.</summary>
        /// <param name="slotIndex">The slot, as <see cref="SlotIndex"/> computes it.</param>
        /// <returns>The slot's colour and, when the theme has a cycler, its full set of cycled properties.</returns>
        internal SeriesSlot SeriesSlot(int slotIndex)
        {
            var cycled = theme.PropCycler?[slotIndex];
            var colour = theme.PropCycler is { NamesColors: true } && cycled is { } named ? named.Color : theme.CycleColors[slotIndex % theme.CycleColors.Length];
            return new SeriesSlot(colour, cycled, theme);
        }
    }

    extension(CycleRule rule)
    {
        /// <summary>The cycle a series whose matplotlib call asks as <paramref name="call"/> takes its colour from under this rule, read
        /// from matplotlib 3.11.2's <c>axes/_axes.py</c>: a line from the lines cycle; a bar, a pie and a fill from the patch cycle; a
        /// scatter and a fill between two curves from the patch cycle, and a violin from the lines cycle, except in classic mode
        /// (<c>_internal.classic_mode</c>), where they take a colour of the style's and leave both cycles alone.</summary>
        /// <param name="call">How the series' call asks for a colour.</param>
        /// <returns>The cycle, or <see cref="Rendering.SeriesSlotExtensions.ColorCycle.Neither"/>.</returns>
        internal ColorCycle CycleOf(CycleCall call) => call switch
        {
            CycleCall.Line => ColorCycle.Lines,
            CycleCall.Patch => ColorCycle.Patches,
            CycleCall.Scatter or CycleCall.FillBetween => rule is CycleRule.MatplotlibClassic ? ColorCycle.Neither : ColorCycle.Patches,
            CycleCall.Violin => rule is CycleRule.MatplotlibClassic ? ColorCycle.Neither : ColorCycle.Lines,
            _ => ColorCycle.Neither,
        };
    }

    extension(Axes axes)
    {
        /// <summary>The slot a series holds: its position in <see cref="Axes.AllSeries"/>. Primary series hold their own
        /// index, secondary-Y series continue after the primary ones, secondary-X series after both.
        /// <para>This is IDENTITY, not colour: the legend's hit-test index, the SVG's <c>data-series-index</c> and
        /// <c>data-legend-index</c>, and every diff that keys on either, all read this number, unaffected by
        /// <see cref="Models.Series.ChartSeries.ColorGroup"/> — two series sharing a colour group still hold two
        /// different slots here, so a click on one never crosses to the other. A series' drawn COLOUR reads
        /// <see cref="ColorSlots"/> instead.</para></summary>
        /// <param name="pair">The list the series sits in.</param>
        /// <param name="indexInPair">The series' index within that list.</param>
        /// <returns>The slot index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pair"/> is not a defined value.</exception>
        internal int SlotIndex(SeriesAxisPair pair, int indexInPair) => pair switch
        {
            SeriesAxisPair.Primary => indexInPair,
            SeriesAxisPair.SecondaryY => axes.Series.Count + indexInPair,
            SeriesAxisPair.SecondaryLeftY => axes.Series.Count + axes.YAxes.Right.OutwardSeriesCount + indexInPair,
            SeriesAxisPair.SecondaryX => axes.Series.Count + axes.YAxes.Right.OutwardSeriesCount + axes.YAxes.Left.OutwardSeriesCount + indexInPair,
            _ => throw pair.NotAMember(nameof(pair), "Not a series list of an axes."),
        };

        /// <summary>The slot after the last series of every seat: how many series <see cref="Axes.AllSeries"/> holds, counted
        /// without walking them. A patch's legend row toggles this slot plus the patch's place in <see cref="Axes.Patches"/>, so
        /// a patch and a series never share an index.</summary>
        internal int FirstPatchSlot => axes.SlotIndex(SeriesAxisPair.SecondaryX, axes.XSecondarySeries.Count);

        /// <summary>Every Y axis slot of the container in identity order (<see cref="YAxisContainer.IdentityOrder"/>), each
        /// with the list that numbers its series and the index its first series has there: the one rule the series passes,
        /// the legend and the end labels number a slot's series by, so a series answers to the same
        /// <see cref="SlotIndex"/> wherever it is drawn or named. Read in place from the container, never copied.</summary>
        /// <returns>The primary first, then every right slot nearest the plot first, then every left slot past the
        /// primary.</returns>
        internal YSlotSeatList YSlotSeats() => new(axes.YAxes);

        /// <summary>The colour slots of every series in <see cref="Axes.AllSeries"/>, in order: one table, built once. A series holds
        /// the next slot of its cycle, or as many as it colours parts (<see cref="ChartSeries.ColorSlotSpan"/>), the first of which is
        /// its number here; a run of series that share the SAME <see cref="ChartSeries.ColorGroup"/>, standing next to each other,
        /// holds ONE slot between them, as many as the largest span among them. A group that is not contiguous is not collapsed:
        /// each run gets its own slot, by design — every verb that builds a group adds its members back to back, and nothing in
        /// the library splits them apart afterward.
        /// <para>Which cycle is the theme's rule (<see cref="CycleLook.Rule"/>). Under the library's own themes there is one, and every
        /// series takes its next slots in the order it was added. Under the matplotlib themes there are two, as matplotlib's axes
        /// keeps them, counted apart: a series takes the next slot of the cycle its kind's call asks (<see cref="ChartSeries.CycleCall"/>,
        /// <see cref="CycleOf"/>), and a series that asks neither, or takes no colour of the cycle (a span of 0), holds slot 0 and
        /// moves neither. A colour group asks the lines cycle when one of its members does, else the patch cycle when one does: the
        /// matplotlib composition of a line with its band (a <c>fill_between</c> given the line's colour) asks for the line's. Every Y
        /// slot past the primary and the top X axis are twins, axes of their own to matplotlib (<c>twinx</c>, <c>twiny</c>), so both
        /// cycles start again at the first series of each.</para></summary>
        /// <param name="theme">The theme the axes is drawn under, whose rule hands the slots out.</param>
        /// <returns>One colour-slot number per series of <see cref="Axes.AllSeries"/>, in that order: the first slot the
        /// series colours with.</returns>
        internal int[] ColorSlots(Theme theme)
        {
            int count = axes.FirstPatchSlot;
            if (count == 0) return [];

            // Built on every draw (the series and the legend each ask), so it walks the series lists in identity order itself and
            // allocates only the table: a run of series sharing a colour group is closed when a series that does not join it is read.
            var slots = new int[count];
            var rule = theme.Looks.Cycle.Rule;
            bool twinsStartAgain = rule is not CycleRule.OneCycle;
            int lines = 0, patches = 0;
            int read = 0, runStart = 0, runSpan = 0;
            int? runGroup = null;
            bool runStartsAgain = false;
            var runCycle = ColorCycle.Neither;

            var left = axes.YAxes.Left;
            var right = axes.YAxes.Right;
            for (int s = 0; s < left.OutwardOffset; s++) Number(left[s].Series, twin: false);
            for (int s = 0; s < right.Count; s++) Number(right[s].Series, twin: true);
            for (int s = left.OutwardOffset; s < left.Count; s++) Number(left[s].Series, twin: true);
            Number(axes.XSecondarySeries, twin: true);
            CloseRun();
            return slots;

            // Every Y slot past the primary and the top X axis are twins, axes of their own to matplotlib, so under its themes both
            // cycles start again at the first series of each, unless that series continues a run begun before it.
            void Number(IReadOnlyList<ISeries> list, bool twin)
            {
                for (int k = 0; k < list.Count; k++)
                {
                    var chart = list[k] as ChartSeries;
                    int? group = chart?.ColorGroup;
                    if (read == runStart || !runGroup.HasValue || group != runGroup)
                    {
                        CloseRun();
                        runStart = read;
                        runGroup = group;
                        runStartsAgain = twinsStartAgain && twin && k == 0;
                        runCycle = ColorCycle.Neither;
                        runSpan = 0;
                    }

                    int own = chart?.ColorSlotSpan ?? 1;
                    runSpan = Math.Max(runSpan, own);
                    var asked = own == 0 ? ColorCycle.Neither
                        : rule is CycleRule.OneCycle ? ColorCycle.Lines : rule.CycleOf(chart?.CycleCall ?? CycleCall.Line);
                    if (asked == ColorCycle.Lines || (asked == ColorCycle.Patches && runCycle == ColorCycle.Neither)) runCycle = asked;
                    read++;
                }
            }

            // One cycle: a series that takes no colour keeps the running slot (a cycler still dashes it by that slot), as it always
            // has. Two: it holds the first slot, the colour matplotlib's lines.color and patch.facecolor name ('C0' under the default
            // style, 'b' under the classic one: the first colour of the cycle in both).
            void CloseRun()
            {
                if (read == runStart) return;
                if (runStartsAgain) lines = patches = 0;
                int first = runCycle switch
                {
                    ColorCycle.Lines => lines,
                    ColorCycle.Patches => patches,
                    _ => rule is CycleRule.OneCycle ? lines : 0,
                };
                if (runCycle == ColorCycle.Lines) lines += runSpan;
                else if (runCycle == ColorCycle.Patches) patches += runSpan;
                for (int k = runStart; k < read; k++) slots[k] = first;
            }
        }
    }

    /// <summary>Which of matplotlib's two colour cycles of an axes a series takes its colour from (<see cref="CycleRule"/>).</summary>
    internal enum ColorCycle
    {
        /// <summary>Neither: the series takes a colour of its own, of its colour map, or the first of the cycle, and moves no cycle.</summary>
        Neither,

        /// <summary>The cycle of the patches (<c>_get_patches_for_fill</c>).</summary>
        Patches,

        /// <summary>The cycle of the lines (<c>_get_lines</c>).</summary>
        Lines,
    }

    extension(SeriesSlot slot)
    {
        /// <summary>The stroke the line of <paramref name="series"/> is drawn with: <paramref name="color"/>, and the stroke the series was
        /// given under its kind's rule (<c>StrokeOf(Color, GivenStroke, LineWidthRule)</c>).</summary>
        /// <param name="color">The colour the series is drawn in.</param>
        /// <param name="series">The kind that states its given stroke and its rule.</param>
        /// <returns>The stroke to draw with.</returns>
        internal StrokeStyle StrokeOf(Color color, IGivenStroked series) => slot.StrokeOf(color, series.Stroke, series.WidthRule);

        /// <summary>The stroke a line of a kind is drawn with: <paramref name="color"/> and the dash pattern and width the series was
        /// given, otherwise the slot's (<see cref="SeriesSlot.LineStyleOf"/>, <see cref="SeriesSlot.LineWidthOf(double?, LineWidthRule)"/>). The one place a
        /// renderer and a legend swatch build a series' stroke from, so they cannot disagree.</summary>
        /// <param name="color">The colour the series is drawn in.</param>
        /// <param name="given">The stroke the series was given (<see cref="GivenStroke"/>: each part <see langword="null"/> while not
        /// given).</param>
        /// <param name="rule">The rule of the series' kind.</param>
        /// <returns>The stroke to draw with.</returns>
        internal StrokeStyle StrokeOf(Color color, GivenStroke given, LineWidthRule rule) =>
            StrokeStyle.Of(color, slot.LineWidthOf(given.Width, rule), slot.LineStyleOf(given.Style));

        /// <summary>The stroke of a line a renderer draws in a look of its own (a guide, a needle, a stem, a cap): <paramref name="color"/>, solid,
        /// as wide as <paramref name="rule"/> draws under this slot. The one place a renderer's own line becomes a stroke, so no renderer joins a
        /// colour to a width rule by hand.</summary>
        /// <param name="color">The colour of the line.</param>
        /// <param name="rule">The rule the line is drawn wide by.</param>
        /// <returns>The stroke.</returns>
        internal StrokeStyle StrokeOf(Color color, LineWidthRule rule) => StrokeStyle.Solid(color, slot.LineWidthOf(rule));

        /// <summary>The stroke of a kind that takes a width from its author and no dash (a sparkline): <paramref name="color"/>, solid, as wide
        /// as <see cref="SeriesSlot.LineWidthOf(IWidthGiven)"/> draws it.</summary>
        /// <param name="color">The colour of the line.</param>
        /// <param name="series">The kind that states its given width and its rule.</param>
        /// <returns>The stroke.</returns>
        internal StrokeStyle StrokeOf(Color color, IWidthGiven series) => StrokeStyle.Solid(color, slot.LineWidthOf(series));

        /// <summary>The stroke of a line a renderer draws in a look of its own with a dash of its own (a dashed guide or crosshair, a violin's
        /// mean): <paramref name="color"/> in <paramref name="dash"/>, as wide as <paramref name="rule"/> draws under this slot.</summary>
        /// <param name="color">The colour of the line.</param>
        /// <param name="rule">The rule the line is drawn wide by.</param>
        /// <param name="dash">The dash pattern of the line.</param>
        /// <returns>The stroke.</returns>
        internal StrokeStyle StrokeOf(Color color, LineWidthRule rule, LineStyle dash) => StrokeStyle.Of(color, slot.LineWidthOf(rule), dash);
    }
}
