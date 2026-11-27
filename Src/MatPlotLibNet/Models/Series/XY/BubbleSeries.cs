// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a bubble chart — a scatter plot where marker area encodes a third variable.</summary>
public sealed class BubbleSeries : XYSeries, IHasColor, IHasAlpha, ISeriesKind<BubbleSeries>
{
    /// <inheritdoc />
    /// <remarks>A bubble chart is matplotlib's <c>scatter</c> with sizes.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Scatter;

    /// <summary>The area of each bubble, in points squared, one per data point; a larger value draws a larger bubble.</summary>
    public double[] Sizes { get; }
    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }
    /// <inheritdoc />
    public double Alpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(Alpha))); } = 0.6;

    /// <summary>Initializes a new <see cref="BubbleSeries"/> with position and size data.</summary>
    /// <param name="xData">X coordinates.</param>
    /// <param name="yData">Y coordinates.</param>
    /// <param name="sizes">Marker area in points² for each data point.</param>
    public BubbleSeries(double[] xData, double[] yData, double[] sizes) : base(xData, yData)
    {
        Sizes = sizes;
    }

    static string ISeriesKind<BubbleSeries>.Kind => "bubble";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = XData, YData = YData, Sizes = Sizes,
        Color = Color, Alpha = Alpha
    };

    /// <summary>The reader of a <see cref="BubbleSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries alpha transparency.</summary>
    internal static BubbleSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Bubble(dto.XData ?? [], dto.YData ?? [], dto.Sizes ?? []);
        s.Color = dto.Color;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The point, and the size that is its third variable.</remarks>
    public override ChartDataTable? ToDataTable() =>
        this.WithRowKeys(ChartDataTable.XY(Label, XData, YData, new NumberColumn("size", Sizes)));

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
