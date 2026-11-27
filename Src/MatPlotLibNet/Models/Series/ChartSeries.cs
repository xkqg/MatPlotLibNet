// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class providing common <see cref="ISeries"/> properties. All series must implement
/// <see cref="Accept"/>, <see cref="ComputeDataRange"/>, and <see cref="ToSeriesDto"/>.</summary>
public abstract class ChartSeries : ISeries, IHasDataRange, ISeriesSerializable, IChangeTracked, IMatplotlibArtist
{
    private long _stamp;

    /// <summary>Stores <paramref name="value"/> and marks this series changed, unless it already holds an equal value. Every
    /// setter of every series writes through it, so a figure drawn again knows the series changed.</summary>
    private protected void Change<T>(ref T slot, T value) => ChangeClock.Change(ref slot, value, ref _stamp);

    /// <summary>The <see cref="ChangeClock"/> mark of the newest change to this series. A streaming kind adds the mark of its
    /// <see cref="Streaming.StreamRing{TSample}"/>, where an append is counted.</summary>
    internal virtual long LatestChange => _stamp;

    long IChangeTracked.LatestChange => LatestChange;

    /// <inheritdoc />
    public string? Label { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public bool Visible { get; set => Change(ref field, value); } = RestingVisible;

    /// <summary>Whether a series is drawn when it is told otherwise: yes. The reader and the writer of every series compare with it.</summary>
    internal const bool RestingVisible = true;

    /// <summary>Where a line's label stands when it is told none: in the legend.</summary>
    internal const SeriesLabelPlacement RestingLabelPlacement = SeriesLabelPlacement.Legend;

    /// <inheritdoc />
    public int ZOrder
    {
        get => GivenZOrder ?? RestingZOrder;
        set => GivenZOrder = value;
    }

    /// <summary>The z-order a series of this type reads while none was given: 0, unless the type is drawn behind the others by
    /// default.</summary>
    internal virtual int RestingZOrder => 0;

    /// <summary>The z-order the caller gave, <see langword="null"/> while none was given (the resting value included, once set, is
    /// given). The JSON writes it, and the matplotlib themes draw a series given none at its <see cref="ArtistZOrder"/>.</summary>
    internal int? GivenZOrder { get; private set => Change(ref field, value); }

    /// <summary>What this kind of series is to matplotlib (<see cref="MatplotlibArtist"/>): the call its colour is asked with, the zorder of
    /// the artist its verb draws, and where the theme takes its colour from where its author names none. A kind declares it once; a kind
    /// that says nothing is <see cref="MatplotlibArtist.Own"/>. A kind whose colours depend on how the instance is set up says so with
    /// <see cref="MatplotlibArtist.NamingItsColours"/>.</summary>
    internal virtual MatplotlibArtist Artist => MatplotlibArtist.Own;

    /// <summary>The coordinate system this kind of series is drawn in when it is not the Cartesian one (polar, ternary, 3-D), which
    /// <see cref="Axes.AddSeries{T}"/> puts the axes in, as the verb that makes the series does; <see langword="null"/> for a kind that
    /// leaves the axes as it is. A fact of the kind, declared once.</summary>
    internal virtual CoordinateSystem? DrawnIn => null;

    /// <inheritdoc />
    MatplotlibArtist IMatplotlibArtist.Artist => Artist;

    /// <summary>The zorder matplotlib gives the artist its verb for this kind of series draws (<see cref="MatplotlibArtist.ZOrder"/>): 1
    /// for a patch or a collection (a bar, a fill, a scatter, a mesh), unless the kind says otherwise (2 for a line, 0 for an image). The
    /// matplotlib themes draw a series given no <see cref="ZOrder"/> at it.</summary>
    internal double ArtistZOrder => Artist.ZOrder;

    /// <summary>Whether the marks of this series stand past the range it reports to its axes under <paramref name="theme"/>: an arrow of
    /// an autoscaled quiver (its length is no part of the range), a streamline's arrow, a hexagon at the edge of a hexbin's extent. A range
    /// that scaled itself then does not hold the marks, so the library's own themes cut them off at the plot too, as matplotlib's
    /// collection is (<see cref="Rendering.SeriesGroupExtensions"/>, the one clip rule). False for every other kind.</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns><see langword="true"/> when the marks may stand past the range.</returns>
    internal virtual bool MarksReachPastItsRange(Theme theme) => false;

    /// <inheritdoc />
    public abstract void Accept(ISeriesVisitor visitor, RenderArea area);

    /// <inheritdoc />
    public abstract DataRangeContribution ComputeDataRange(IAxesContext context);

    /// <inheritdoc />
    public abstract SeriesDto ToSeriesDto();

    /// <inheritdoc />
    /// <remarks>Virtual, not abstract: a series type that has no tabular form says so by saying nothing, and
    /// the ones that DO override this one method. (The interface carries the same default for a series outside
    /// this hierarchy; a class cannot override a default interface member, so the base declares it too.)</remarks>
    public virtual ChartDataTable? ToDataTable() => null;

    /// <summary>Whether this series draws its own <see cref="Label"/> as part of what it draws, so the legend does not
    /// list it: a legend entry would be a second copy of the name, in a swatch colour the series is not drawn in. A stat
    /// tile is the case: its label stands under the headline number. The legend's one entry list asks this of every
    /// series (<c>SeriesLegendEntry.RowsOf</c>), so the drawing, the hit-test and the margin reservation
    /// agree.</summary>
    internal virtual bool DrawsOwnLabel => false;

    /// <summary>Whether this series owns the vertical range of its axes outright, so that range carries no data a
    /// reader could pan or zoom to. A state timeline is the case: its segments fill the bar from edge to edge.
    /// Pan and zoom leave the Y range of an axes alone when every series on it says so.</summary>
    internal virtual bool FixesYRange => false;

    /// <summary>Whether this series fills the plot without the axes around it: its marks stand where the series puts them
    /// whatever the limits of the axes are (a radar, a pie, a treemap, a table, a stat tile), so the axes has nothing to say
    /// about it and draws no grid, ticks or frame when every series on it says so. A series that says so draws the same marks
    /// under any limits; one that moves with them does not say so.</summary>
    internal virtual bool DrawsWithoutAxes => false;

    /// <summary>Whether this series, one that draws without axes (<see cref="DrawsWithoutAxes"/>), stands outside the plot under
    /// <paramref name="theme"/>, so it leaves the plot to its axes: an axes it is alone on is then drawn as any axes is, its frame, ticks
    /// and grid included. A table below the axes is the case (matplotlib's <c>ax.table</c> leaves its axes as they are).</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    internal virtual bool StandsOutsideThePlot(Theme theme) => false;

    /// <summary>Whether the series holds nothing to draw, so that what <see cref="ComputeDataRange"/> says is the library's default for a
    /// series with no data (0 to 1). Under the matplotlib themes such a series adds nothing to the range of its axes, as matplotlib's data
    /// limits hold nothing for it (<c>AxesRangeExtensions</c>' <c>DataRangeUnder</c>); a series whose range is the same either way does
    /// not say so.</summary>
    internal virtual bool HoldsNoData => false;

    /// <summary>What a series that holds no data adds to the range of its axes under the library's own themes: 0 to 1 on both axes. The
    /// one place the default is written; the series whose default has another shape (a band of rows, residuals about 0) say theirs.</summary>
    private protected static DataRangeContribution NoDataUnderTheLibrary => new(0, 1, 0, 1);

    /// <summary>The range of a series that fills a surface to the edges of its points (a mesh, a triangulation): the library's default
    /// when it holds no data (<see cref="HoldsNoData"/>, <see cref="NoDataUnderTheLibrary"/>), else the axes pinned to the drawable extent of
    /// its points (<see cref="DataRangeContribution.PinnedOver"/>).</summary>
    /// <param name="x">The points' X coordinates.</param>
    /// <param name="y">The points' Y coordinates.</param>
    /// <returns>The range.</returns>
    private protected DataRangeContribution PinnedOverItsPoints(double[] x, double[] y) =>
        HoldsNoData ? NoDataUnderTheLibrary : DataRangeContribution.PinnedOver(x, y);

    /// <summary>The range of a series of points that fill no edge (a triangulation coloured by its faces, a field of barbs): the library's
    /// default when it holds no data (<see cref="HoldsNoData"/>, <see cref="NoDataUnderTheLibrary"/>), else the extent of its drawable
    /// points (<see cref="DataRangeContribution.Over"/>).</summary>
    /// <param name="x">The points' X coordinates.</param>
    /// <param name="y">The points' Y coordinates.</param>
    /// <returns>The range.</returns>
    private protected DataRangeContribution OverItsPoints(double[] x, double[] y) =>
        HoldsNoData ? NoDataUnderTheLibrary : DataRangeContribution.Over(x, y);

    /// <summary>Whether the series, holding no data (<see cref="HoldsNoData"/>), still asks for its axes to be scaled under the matplotlib
    /// themes: a matplotlib plotting function asks for the autoscale of both axes whatever it is handed (<c>ax.scatter([], [])</c> gives
    /// (-0.055, 0.055)); a seaborn function and <c>eventplot</c> return before they touch the axes, which keep (0, 1).</summary>
    internal virtual bool AsksWhenEmpty => true;

    /// <summary>Whether the axes this series draws without axes (<see cref="DrawsWithoutAxes"/>) still paints its background where its
    /// theme leaves an axis-less axes unpainted (<c>AxesFrameLook.NoBackgroundWithoutAxes</c>, matplotlib's <c>axis('off')</c>). A Sankey
    /// diagram does: matplotlib's <c>Sankey</c> draws on an axes it creates with no ticks and keeps its frame and patch.</summary>
    internal virtual bool KeepsAxesBackground => false;

    /// <summary>How this series is drawn in its legend entry: a filled rectangle unless the series says otherwise. The legend
    /// asks every series and holds no list of types, so a series that is drawn as a line, a marker or an error bar says so
    /// here and a new kind of series needs no change to the legend.</summary>
    internal virtual LegendSwatch LegendSwatch => LegendSwatch.Patch;

    /// <summary>Whether this series, a colour bar's data provider (<see cref="IColorBarDataProvider"/>), has the values to back a
    /// colour bar with. A map series that is a plain fill (an ocean, a land) provides the interface and no values, and the bar
    /// is then backed by the series that has some. Read through <c>ColorBar.Resolve</c>, the one place
    /// that picks the series a bar reads.</summary>
    internal virtual bool ProvidesColorBar => true;

    /// <summary>The entries this series has in its legend under <paramref name="theme"/>, each with the swatch and the colour slot it
    /// is drawn in: none for a series with no name, otherwise one under its name. A series that names its own parts says one for
    /// each of them, and a series whose handle wears a colour of the theme (a map's neutral outline) reads it here, so the legend
    /// asks every series and holds no rule for any kind (<see cref="DrawsOwnLabel"/>, and an end label that takes the name
    /// elsewhere, are the legend's own questions).</summary>
    /// <param name="theme">The theme the legend is drawn under.</param>
    /// <returns>The entries, in the order the legend lists them.</returns>
    internal virtual IReadOnlyList<LegendPart> LegendPartsUnder(Theme theme) =>
        string.IsNullOrEmpty(Label) ? [] : [new LegendPart(Label, LegendSwatch)];

    /// <summary>How many colour slots of the theme this series holds: one, unless it colours its own parts (the layers of a
    /// stacked area, the slices of a pie or a donut, the stages of a funnel), which then take one slot each from the series'
    /// first slot on. The series that follows takes the slot past them, so a line drawn after a stack of three layers wears
    /// the fourth colour of the cycle, as matplotlib's does, and not one a layer already wears.</summary>
    internal virtual int ColorSlotSpan => WearsNeutral ? 0 : 1;

    /// <summary>How the matplotlib call this series is drawn as asks its axes for a colour: not at all when the series names its colour
    /// (matplotlib's cycle moves only for a call given none), else as the call a verb composed it as (<see cref="AsksAs"/>), else as its
    /// kind's (<see cref="MatplotlibArtist.Call"/> of <see cref="Artist"/>). Under the matplotlib themes it decides which of the axes' two
    /// cycles the series takes its colour from, or neither (<see cref="Rendering.SeriesSlotExtensions.ColorSlots"/>, <see cref="CycleRule"/>);
    /// the library's own themes keep one cycle and do not ask.</summary>
    internal CycleCall CycleCall => this is IHasColor { Color: not null } ? CycleCall.Neither : AsksAs ?? Artist.Call;

    /// <summary>The call a verb composed this series as where it is not its kind's own, or <see langword="null"/>: the flagged points
    /// of a control chart are matplotlib's <c>plot(x, y, 'o')</c>, which takes the lines cycle's next colour, the one after the
    /// chart's line, where a scatter would take the patch cycle's first. Written as <c>asksAs</c>.</summary>
    internal CycleCall? AsksAs { get; set => Change(ref field, value); }

    /// <summary>Whether this series wears the theme's neutral grey (<see cref="Styling.Theme.NeutralColor"/>) in the place it
    /// would wear its own colour, until it names one: a dumbbell's connector, the outline of a map. A verb sets it instead of
    /// fixing a grey when it runs, because the theme is not known then and a fixed grey follows none; the series then holds
    /// no colour slot, since it wears no colour of the cycle. What the neutral is worn as is the series type's own: the line of
    /// an error bar, the outline of a map polygon. A colour the series names itself wins. Written as <c>wearsNeutral</c>.</summary>
    internal bool WearsNeutral { get; set => Change(ref field, value); }

    /// <summary>How far out this series reaches on a polar axes, for the radial scale the grid and every series read; none
    /// for a series that draws no radius (every kind but the polar ones). A series on a polar axes says so here and never
    /// scales itself: the scale is the axes'.</summary>
    internal virtual RadialExtent? RadialExtent => null;

    /// <summary>The group this series shares its colour with, or <see langword="null"/> for a series that holds its
    /// own slot, as every series has until a verb sets this. Series that carry the SAME value here, standing next to
    /// each other in <see cref="Axes.AllSeries"/>, are drawn in one cycled colour and consume one colour slot between
    /// them — <see cref="Rendering.SeriesSlotExtensions.ColorSlots"/> reads it; <see cref="AxesSeatExtensions.InColorGroup"/>
    /// hands out a fresh value. It never changes a series' own identity: <see cref="Rendering.SeriesSlotExtensions.SlotIndex"/>,
    /// the legend's hit-test index and the SVG's <c>data-series-index</c> stay positional, unaffected by this field, so a
    /// legend click or a diff never crosses from one member of a group to another.</summary>
    internal int? ColorGroup { get; set => Change(ref field, value); }

    /// <summary>The colour a verb chose for this series where its caller named none: the band of a Bollinger or Keltner channel,
    /// the dots of a parabolic SAR. The library's own themes draw it; a theme whose uncoloured series take the cycle
    /// (<see cref="CycleLook.GroupsWearTheCycle"/>, the matplotlib themes) draws the colour of the series' slot in its place, as
    /// matplotlib draws an artist that names no colour. A verb sets it instead of the colour itself, because the theme is not known
    /// when the verb runs. A colour the series names itself wins. Written as <c>verbColor</c>.</summary>
    internal Color? VerbColor { get; set => Change(ref field, value); }

    /// <summary>Writes into <paramref name="dto"/> what a series of the library holds beyond what any series does (see
    /// <c>ToDtoWithSharedFields</c>): the z-order it was given and the colour group, neutral wear, verb colour and cycle call that
    /// decide how it takes its colour. Each is left out while it is at rest, so a series nobody configured writes no more bytes. A
    /// kind that holds more overrides this and calls the base.</summary>
    /// <param name="dto">The record the series' own type wrote.</param>
    internal virtual void WriteSharedFields(SeriesDto dto)
    {
        dto.ZOrder = GivenZOrder;
        if (ColorGroup is { } colorGroup) dto.ColorGroup = colorGroup;
        if (WearsNeutral) dto.WearsNeutral = true;
        if (VerbColor is { } verbColor) dto.VerbColor = verbColor;
        if (AsksAs is { } asksAs) dto.AsksAs = asksAs.ToWireName();
    }

    /// <summary>Takes back what <see cref="WriteSharedFields"/> wrote; a field the record does not carry leaves the value the series
    /// was created with, and a cycle call this version does not know is announced and ignored.</summary>
    /// <param name="dto">The series as the JSON carries it.</param>
    internal virtual void ReadSharedFields(SeriesDto dto)
    {
        if (dto.ColorGroup is { } colorGroup) ColorGroup = colorGroup;
        if (dto.WearsNeutral is true) WearsNeutral = true;
        if (dto.VerbColor is { } verbColor) VerbColor = verbColor;
        if (dto.AsksAs.WireEnum<CycleCall>() is { } asksAs) AsksAs = asksAs;
    }
}
