// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a box plot series displaying statistical distribution summaries.</summary>
public sealed class BoxSeries : DatasetSeries, IHasColor
{
    public Color? Color { get; set; }

    public Color? MedianColor { get; set; }

    public bool ShowOutliers { get; set; } = true;

    public double Widths { get; set; } = RestingWidths;

    public bool Vert { get; set; } = true;

    public double Whis { get; set; } = RestingWhis;

    private const double RestingWidths = 0.5;
    private const double RestingWhis = 1.5;

    public bool ShowMeans { get; set; }

    /// <summary>Where each box stands along the axis that carries the positions (X for upright boxes, Y for boxes that lie
    /// down), one position per dataset; a dataset the array does not reach stands at its index. The automatic range
    /// follows: it covers the places the boxes are drawn at, half a box's <see cref="Widths"/> to either side.</summary>
    public double[]? Positions { get; set; }

    /// <summary>Initializes a new instance of <see cref="BoxSeries"/> with the specified datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing the values for one box.</param>
    public BoxSeries(double[][] datasets) : base(datasets) { }

    /// <summary>How far a box reaches to either side of its position, in data units: half its width.</summary>
    internal double HalfWidth => Widths / 2;

    /// <inheritdoc />
    /// <remarks>Measured as boxes standing upright measure it, the positions along X and the values along Y, and put on the
    /// axes the boxes are drawn on: boxes that lie down (<see cref="Vert"/> false) range their positions along Y and their
    /// values along X. Along the positions, the room each box is drawn in around its position (half its width either
    /// side). A series that sets <see cref="Positions"/> ranges over those places alone; one that does not keeps the half
    /// unit around the default positions that it always had, widened for a box wider than that. A limit set on the axis
    /// that carries the positions is their range.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        var places = DrawnAround().CoverAlongPositions(Positions, new Range1D(-0.5, Datasets.Length - 0.5));
        double? limitMin = Vert ? context.XAxisMin : context.YAxisMin;
        double? limitMax = Vert ? context.XAxisMax : context.YAxisMax;
        // Over the values the value axis can place: a missing sample (NaN) and an infinity are not part of the extent, and
        // on a log axis neither is 0 or below (the box clips its lower statistic to the plot's edge there), so the range
        // starts at the lowest value it can place.
        var y = Datasets.DrawableRange(Vert ? context.YScale : context.XScale);
        var upright = new DataRangeContribution(limitMin ?? places?.Lo, limitMax ?? places?.Hi, y?.Lo, y?.Hi);
        return Vert ? upright : upright.Transposed();
    }

    /// <summary>What each box that can be drawn covers, at the position it stands at.</summary>
    private IEnumerable<Range1D> DrawnAround()
    {
        for (int i = 0; i < Datasets.Length; i++)
        {
            if (Datasets[i].DrawableRange() is null) continue;
            double at = Positions.PositionOf(i);
            yield return new Range1D(at - HalfWidth, at + HalfWidth);
        }
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "box", Datasets = Datasets,
        Color = Color,
        MedianColor = MedianColor,
        ShowOutliers = ShowOutliers.OrNullAtRest(true),
        Widths = Widths.OrNullAtRest(RestingWidths),
        Vert = Vert.OrNullAtRest(true),
        Whis = Whis.OrNullAtRest(RestingWhis),
        ShowMeans = ShowMeans.OrNullAtRest(false),
        Positions = Positions,
    };

    /// <summary>Reconstructs a <see cref="BoxSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static BoxSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.BoxPlot(dto.Datasets ?? []);
        s.Color = dto.Color;
        s.MedianColor = dto.MedianColor;
        s.ShowOutliers = dto.ShowOutliers != false;
        s.Widths = dto.Widths ?? RestingWidths;
        s.Vert = dto.Vert != false;
        s.Whis = dto.Whis ?? RestingWhis;
        s.ShowMeans = dto.ShowMeans == true;
        s.Positions = dto.Positions;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
