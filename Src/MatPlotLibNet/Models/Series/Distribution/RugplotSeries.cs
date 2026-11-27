// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a rug plot series that draws short tick marks along an axis to show the distribution of individual values.</summary>
public sealed class RugplotSeries : ChartSeries, IHasColor, IHasAlpha, IAsksOfAxes, IThemedRange, ISeriesKind<RugplotSeries>
{
    /// <inheritdoc />
    /// <remarks>Under a theme whose verbs ask as seaborn's (<see cref="SeabornVerbLook.RugMarginPerHeight"/>), seaborn's
    /// <c>rugplot(expand_margins=True)</c>: <c>ax.margins(x=xmarg, y=ymarg + 2 * height)</c>, whose <c>tight=True</c> ends both axes where
    /// the data and the margin end (<see cref="AxesAsk.TightUnder"/>); nothing under the library's own themes.</remarks>
    AxesAsk IAsksOfAxes.AskUnder(Theme theme) =>
        theme.Looks.Verbs.RugMarginPerHeight is > 0 and var perHeight
            ? AxesAsk.TightUnder(theme) with { MarginRaise = new(0, perHeight * Height) }
            : default;

    /// <inheritdoc />
    /// <remarks>Its ticks along X, and across them what the theme's verb look ranges (<see cref="SeabornVerbLook.RugRangesAcross"/>):
    /// from 0 to its height or 1 under the library's themes (<see cref="ComputeDataRange"/>), nothing under seaborn's, whose ticks
    /// stand in the axes' own height.</remarks>
    DataRangeContribution IThemedRange.RangeUnder(IAxesContext context, Theme theme) =>
        theme.Looks.Verbs.RugRangesAcross ? ComputeDataRange(context) : ComputeDataRange(context) with { YMin = null, YMax = null };

    /// <inheritdoc />
    /// <remarks>2: seaborn's <c>rugplot</c> draws a <c>LineCollection</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <summary>The observations to mark: each draws one short tick at its value along X. A missing value (NaN) or an
    /// infinity draws none.</summary>
    public Vec Data { get; }

    /// <summary>Height of each tick as a fraction of the vertical extent of the plot, measured up from its bottom edge.
    /// Default 0.05.</summary>
    public double Height { get; set => Change(ref field, value); } = 0.05;

    /// <inheritdoc />
    public double Alpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(Alpha))); } = 0.5;

    /// <summary>Width of each tick, in pixels. Default 1.</summary>
    public double LineWidth { get; set => Change(ref field, value); } = 1.0;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance of <see cref="RugplotSeries"/> with the specified data.</summary>
    /// <param name="data">The data values to display as rug ticks.</param>
    public RugplotSeries(Vec data)
    {
        Data = data;
    }

    /// <inheritdoc />
    internal override bool HoldsNoData => Data.Length == 0;

    /// <inheritdoc />
    /// <remarks>seaborn's <c>rugplot</c> returns before it touches the axes when it is handed no data: they keep (0, 1).</remarks>
    internal override bool AsksWhenEmpty => false;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (HoldsNoData) return NoDataUnderTheLibrary;
        // Rugs are always drawn within [0, Height] on the Y axis — that's the extent a standalone
        // rugplot needs. When overlaid on a KDE/hist the axis scaler will widen from there.
        // Over the drawable observations only: a missing one (NaN) and an infinity draw no tick.
        var x = Data.Data.DrawableRange();
        return new(x?.Lo, x?.Hi, 0, Math.Max(Height, 1.0));
    }

    static string ISeriesKind<RugplotSeries>.Kind => "rugplot";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Data = Data,
        Alpha = Alpha,
        LineWidth = LineWidth,
        Color = Color,
        RugHeight = Height
    };

    /// <summary>The reader of a <see cref="RugplotSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static RugplotSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Rugplot(dto.Data ?? []);
        if (dto.RugHeight is { } rugHeight) s.Height = rugHeight;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        if (dto.LineWidth is { } lineWidth) s.LineWidth = lineWidth;
        if (dto.Color is { } color) s.Color = color;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The samples the curve is estimated from.</remarks>
    public override ChartDataTable? ToDataTable() => ChartDataTable.FromIndexedValues(ChartDataColumn.Position(DataAxis.X, Label ?? "value"), Data);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
