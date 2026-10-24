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

    /// <summary>The group this series shares its colour with, or <see langword="null"/> for a series that holds its
    /// own slot, as every series has until a verb sets this. Series that carry the SAME value here, standing next to
    /// each other in <see cref="Axes.AllSeries"/>, are drawn in one cycled colour and consume one colour slot between
    /// them — <see cref="Rendering.SeriesSlotExtensions.ColorSlotIndex"/> reads it; <see cref="Axes.NextColorGroup"/>
    /// hands out a fresh value. It never changes a series' own identity: <see cref="Rendering.SeriesSlotExtensions.SlotIndex"/>,
    /// the legend's hit-test index and the SVG's <c>data-series-index</c> stay positional, unaffected by this field, so a
    /// legend click or a diff never crosses from one member of a group to another.</summary>
    internal int? ColorGroup { get; set; }
}
