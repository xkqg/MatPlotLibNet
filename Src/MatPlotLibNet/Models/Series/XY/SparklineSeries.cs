// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a sparkline — a tiny, word-sized line chart with no axes, labels, or decorations.</summary>
/// <remarks>Designed for inline dashboard use. Renders only the line within the full plot bounds.</remarks>
public sealed class SparklineSeries : ChartSeries, IHasColor, ISeriesKind<SparklineSeries>, IWidthGiven
{
    /// <inheritdoc />
    /// <remarks>2: a sparkline is a line, a <c>Line2D</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => LegendSwatch.Of(LegendSwatchKind.SolidLine, this);

    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <summary>The y value of each point of the line. Points are spaced evenly from left to right, so a value's position
    /// in the array is its x.</summary>
    public double[] Values { get; }

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <inheritdoc cref="LineSeries.LineWidth"/>
    public double LineWidth { get => this.ReportedLineWidth; set => GivenLineWidth = value; }

    /// <summary>The line width that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenLineWidth { get; private set => Change(ref field, value); }

    double? IWidthGiven.GivenLineWidth => GivenLineWidth;

    /// <summary>Creates a new sparkline series from the given values.</summary>
    public SparklineSeries(double[] values) => Values = values;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // Over the drawable values only: a missing value (NaN) and an infinity are left out.
        var y = Values.DrawableRange();
        return new(0, Values.Length - 1, y?.Lo, y?.Hi);
    }

    static string ISeriesKind<SparklineSeries>.Kind => "sparkline";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Values = Values, Color = Color, LineWidth = GivenLineWidth
    };

    /// <summary>The reader of a <see cref="SparklineSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries colour and line width.</summary>
    internal static SparklineSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Sparkline(dto.Values ?? []);
        s.Color = dto.Color;
        if (dto.LineWidth is { } width) s.LineWidth = width;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>A sparkline has no x of its own: the index IS the x.</remarks>
    public override ChartDataTable? ToDataTable() => ChartDataTable.FromIndexedValues(new(Label ?? "value"), Values);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
