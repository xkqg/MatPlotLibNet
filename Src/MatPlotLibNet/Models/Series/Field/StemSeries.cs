// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a stem plot series displaying data as vertical lines with markers at the tips.</summary>
/// <remarks>Under the matplotlib themes it is drawn as matplotlib's <c>stem</c> draws it: the stems and the baseline as wide as the
/// theme's lines, the heads as a line's markers, and the baseline in red over both.</remarks>
public sealed class StemSeries : ChartSeries, ISeriesKind<StemSeries>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>stem</c> draws in <c>'C0'</c> and asks neither cycle. 2: <c>stem</c> draws a <c>LineCollection</c> and <c>Line2D</c>s.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.LineCollection;

    /// <summary>The X position of each stem.</summary>
    public double[] XData { get; }

    /// <summary>The height of each stem: it runs from the baseline at Y = 0 to this value and carries a dot there. A
    /// stem whose head is missing (NaN) or infinite is skipped.</summary>
    public double[] YData { get; }

    /// <summary>The colour of the dot at the head of each stem; <see langword="null"/> uses the series colour.</summary>
    public Color? MarkerColor { get; set => Change(ref field, value); }

    /// <summary>The colour of the vertical line of each stem; <see langword="null"/> uses the series colour.</summary>
    public Color? StemColor { get; set => Change(ref field, value); }

    /// <summary>The colour of the horizontal baseline at Y = 0; <see langword="null"/> uses the theme's colour for it: under the
    /// matplotlib themes the colour of matplotlib's <c>basefmt</c> (the fourth colour of the cycle, red, under the default style; the
    /// third, red, under the classic one), under every other theme its neutral colour.</summary>
    public Color? BaselineColor { get; set => Change(ref field, value); }

    /// <summary>The colour the baseline is drawn in under <paramref name="theme"/>: the one given, else matplotlib's <c>basefmt</c>
    /// under a theme that hands out matplotlib's cycles (<c>'C3-'</c>, or <c>'C2-'</c> in classic mode, read from matplotlib's
    /// <c>Axes.stem</c>), else the theme's neutral colour.</summary>
    /// <param name="theme">The theme the stems are drawn under.</param>
    /// <returns>The colour of the baseline.</returns>
    internal Color BaselineColorIn(Theme theme) => BaselineColor ?? theme.Looks.Cycle.Rule switch
    {
        CycleRule.Matplotlib => theme.SeriesSlot(3).CycleColor,
        CycleRule.MatplotlibClassic => theme.SeriesSlot(2).CycleColor,
        _ => theme.Neutral,
    };

    /// <summary>The marker the head of each stem is drawn in; the default is <see cref="RestingMarker"/>.</summary>
    public MarkerStyle Marker { get; set => Change(ref field, value); } = RestingMarker;

    private const MarkerStyle RestingMarker = MarkerStyle.Circle;

    /// <summary>Initializes a new instance of <see cref="StemSeries"/> with the specified data.</summary>
    /// <param name="xData">The X-axis data values.</param>
    /// <param name="yData">The Y-axis data values.</param>
    public StemSeries(double[] xData, double[] yData)
    {
        XData = xData;
        YData = yData;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // Over the drawable values only: a missing stem (NaN) and an infinity are not part of the extent.
        var x = XData.DrawableRange();
        double? xMin = x?.Lo, xMax = x?.Hi;
        if (YData.DrawableRange() is not { } raw)
        {
            return new(xMin, xMax, null, null);
        }

        // Stems stand on the y=0 baseline, so the baseline is a sticky edge of Y. Along X nothing is sticky: the first
        // and last stem are marks like any other and keep the axis margin, so their heads stay whole. (They used to be
        // sticky at both ends of the data, which put the end heads on the plot's frame, half of each outside it.)
        double? stickyYMin = raw.Lo >= 0 ? 0 : null;
        double? stickyYMax = raw.Hi <= 0 ? 0 : null;
        return new(xMin, xMax, Math.Min(raw.Lo, 0), Math.Max(raw.Hi, 0),
            StickyYMin: stickyYMin, StickyYMax: stickyYMax);
    }

    static string ISeriesKind<StemSeries>.Kind => "stem";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(), XData = XData, YData = YData,
        MarkerColor = MarkerColor,
        StemColor = StemColor,
        BaselineColor = BaselineColor,
        Marker = Marker.OrNullAtRest(RestingMarker),
    };

    /// <summary>The reader of a <see cref="StemSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static StemSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Stem(dto.XData ?? [], dto.YData ?? []);
        s.MarkerColor = dto.MarkerColor;
        s.StemColor = dto.StemColor;
        s.BaselineColor = dto.BaselineColor;
        if (dto.Marker is { } marker) s.Marker = marker;
        return s;
    }

    /// <inheritdoc />
    /// <remarks>The stems' feet and heads.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.XY(Label, XData, YData);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
