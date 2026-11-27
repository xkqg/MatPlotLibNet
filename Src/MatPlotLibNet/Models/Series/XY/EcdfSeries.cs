// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an empirical cumulative distribution function (ECDF) step-function series.</summary>
public sealed class EcdfSeries : ChartSeries, IHasColor, ISeriesKind<EcdfSeries>, IGivenStroked
{
    /// <inheritdoc />
    /// <remarks>2: <c>ecdf</c> draws a <c>Line2D</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => LegendSwatch.Line(this);

    /// <summary>The raw sample the curve is made from, in the order it was given. Missing values (NaN) and infinities
    /// are kept here but are not observations, so they take no part in the curve.</summary>
    public double[] Data { get; }

    /// <summary>The drawable values of <see cref="Data"/> in ascending order: the positions of the steps along the X axis.</summary>
    public double[] SortedX { get; }

    /// <summary>The cumulative fraction at each entry of <see cref="SortedX"/>, from 1/n for the smallest value up to 1
    /// for the largest: the height the curve steps to there.</summary>
    public double[] CdfY { get; }

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <inheritdoc cref="LineSeries.LineWidth"/>
    public double LineWidth { get => this.ReportedLineWidth; set => Stroke = Stroke with { Width = value }; }

    /// <summary>The width and the dash pattern the line was given, each <see langword="null"/> while it was not.</summary>
    internal GivenStroke Stroke { get; set => Change(ref field, value); }

    GivenStroke IGivenStroked.Stroke => Stroke;

    /// <summary>The dash pattern of the step line. Default solid.</summary>
    public LineStyle LineStyle { get => Stroke.StyleOrResting; set => Stroke = Stroke with { Style = value }; }

    /// <summary>Creates a new ECDF series from the given raw data.</summary>
    /// <param name="data">The raw data values to compute the ECDF from. A missing value (NaN) and an infinity are not
    /// observations: the ECDF is made from the others, so its steps rise by 1/n over the n drawable values.</param>
    public EcdfSeries(double[] data)
    {
        Data = data;
        SortedX = data.SortedDrawableValues();
        int n = SortedX.Length;
        CdfY = new double[n];
        for (int i = 0; i < n; i++)
            CdfY[i] = (i + 1.0) / n;
    }

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>ecdf</c> refuses a sample of no value; under the matplotlib themes an ECDF of none asks for its axes as
    /// the other matplotlib functions do and adds nothing.</remarks>
    internal override bool HoldsNoData => Data.Length == 0;

    /// <inheritdoc />
    /// <remarks>Over the drawable values; with none at all (no data) 0..1, and with data none of which can be drawn no X
    /// range.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        SortedX.DrawableRange() is { } x
            ? new(x.Lo, x.Hi, 0, 1)
            : HoldsNoData ? NoDataUnderTheLibrary : new(null, null, 0, 1);

    static string ISeriesKind<EcdfSeries>.Kind => "ecdf";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Data = Data,
        Color = Color,
        Stroking = Stroke
    };

    /// <summary>The reader of a <see cref="EcdfSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static EcdfSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Ecdf(dto.Data ?? []);
        if (dto.Color is { } color) s.Color = color;
        s.Stroke = dto.StrokeOver(s.Stroke);
        return s;
    }

    /// <inheritdoc />
    /// <remarks>The samples the curve is computed from.</remarks>
    public override ChartDataTable? ToDataTable() => ChartDataTable.FromIndexedValues(ChartDataColumn.Position(DataAxis.X, Label ?? "value"), Data);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
