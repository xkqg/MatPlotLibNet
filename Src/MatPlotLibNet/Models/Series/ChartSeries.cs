// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class providing common <see cref="ISeries"/> properties. All series must implement
/// <see cref="Accept"/>, <see cref="ComputeDataRange"/>, and <see cref="ToSeriesDto"/>.</summary>
public abstract class ChartSeries : ISeries, IHasDataRange, ISeriesSerializable
{
    /// <inheritdoc />
    public string? Label { get; set; }

    /// <inheritdoc />
    public bool Visible { get; set; } = true;

    /// <inheritdoc />
    public int ZOrder { get; set; }

    /// <summary>The z-order a series of this type is created with: 0, unless the type is drawn behind the others by
    /// default. The JSON leaves a z-order at this value out.</summary>
    internal virtual int RestingZOrder => 0;

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
    /// series (<c>AxesLegendEntryExtensions.LegendEntries</c>), so the drawing, the hit-test and the margin reservation
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

    /// <summary>How this series is drawn in its legend entry: a filled rectangle unless the series says otherwise. The legend
    /// asks every series and holds no list of types, so a series that is drawn as a line, a marker or an error bar says so
    /// here and a new kind of series needs no change to the legend.</summary>
    internal virtual LegendSwatch LegendSwatch => LegendSwatch.Patch;

    /// <summary>Whether this series, a colour bar's data provider (<see cref="IColorBarDataProvider"/>), has the values to back a
    /// colour bar with. A map series that is a plain fill (an ocean, a land) provides the interface and no values, and the bar
    /// is then backed by the series that has some. Read through <c>ColorBarProviderExtensions.ColorBarProvider</c>, the one place
    /// that picks the series a bar reads.</summary>
    internal virtual bool ProvidesColorBar => true;

    /// <summary>The entries this series has in its legend, each with the swatch and the colour slot it is drawn in: none for a
    /// series with no name, otherwise one under its name. A series that names its own parts says one for each of them, so the
    /// legend asks every series and holds no rule for any kind (<see cref="DrawsOwnLabel"/>, and an end label that takes the
    /// name elsewhere, are the legend's own questions).</summary>
    internal virtual IReadOnlyList<LegendPart> LegendParts =>
        string.IsNullOrEmpty(Label) ? [] : [new LegendPart(Label, LegendSwatch)];

    /// <summary>How many colour slots of the theme this series holds: one, unless it colours its own parts (the layers of a
    /// stacked area, the slices of a pie or a donut, the stages of a funnel), which then take one slot each from the series'
    /// first slot on. The series that follows takes the slot past them, so a line drawn after a stack of three layers wears
    /// the fourth colour of the cycle, as matplotlib's does, and not one a layer already wears.</summary>
    internal virtual int ColorSlotSpan => WearsNeutral ? 0 : 1;

    /// <summary>Whether this series wears the theme's neutral grey (<see cref="Styling.Theme.NeutralColor"/>) in the place it
    /// would wear its own colour, until it names one: a dumbbell's connector, the outline of a map. A verb sets it instead of
    /// fixing a grey when it runs, because the theme is not known then and a fixed grey follows none; the series then holds
    /// no colour slot, since it wears no colour of the cycle. What the neutral is worn as is the series type's own: the line of
    /// an error bar, the outline of a map polygon. A colour the series names itself wins. Written as <c>wearsNeutral</c>.</summary>
    internal bool WearsNeutral { get; set; }

    /// <summary>How far out this series reaches on a polar axes, for the radial scale the grid and every series read; none
    /// for a series that draws no radius (every kind but the polar ones). A series on a polar axes says so here and never
    /// scales itself: the scale is the axes'.</summary>
    internal virtual RadialExtent? RadialExtent => null;

    /// <summary>The group this series shares its colour with, or <see langword="null"/> for a series that holds its
    /// own slot, as every series has until a verb sets this. Series that carry the SAME value here, standing next to
    /// each other in <see cref="Axes.AllSeries"/>, are drawn in one cycled colour and consume one colour slot between
    /// them — <see cref="Rendering.SeriesSlotExtensions.ColorSlotIndex"/> reads it; <see cref="Axes.NextColorGroup"/>
    /// hands out a fresh value. It never changes a series' own identity: <see cref="Rendering.SeriesSlotExtensions.SlotIndex"/>,
    /// the legend's hit-test index and the SVG's <c>data-series-index</c> stay positional, unaffected by this field, so a
    /// legend click or a diff never crosses from one member of a group to another.</summary>
    internal int? ColorGroup { get; set; }
}
