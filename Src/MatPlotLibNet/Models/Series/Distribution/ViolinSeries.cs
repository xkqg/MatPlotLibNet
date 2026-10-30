// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a violin plot series displaying kernel density estimations for datasets.</summary>
public sealed class ViolinSeries : DatasetSeries, IHasColor, IHasAlpha
{
    /// <summary>The colour of the violin's body and of the lines drawn over it. Left unset, the violin wears the colour its
    /// theme gives violins (matplotlib's classic style: a yellow body with red lines) and, when the theme gives none, the
    /// theme's colour at the series' place in the cycle. The legend swatch wears the colour of the body, by the same rule.</summary>
    public Color? Color { get; set; }

    /// <inheritdoc />
    public double Alpha { get; set; } = RestingAlpha;

    /// <summary>When <see langword="true"/> a line is drawn across each violin at the mean of its data. Default
    /// <see langword="false"/>.</summary>
    public bool ShowMeans { get; set; }

    /// <summary>When <see langword="true"/> a line is drawn across each violin at the median of its data. Default
    /// <see langword="false"/>.</summary>
    public bool ShowMedians { get; set; }

    /// <summary>When <see langword="true"/> (the default) a short tick marks the smallest and the largest observation
    /// of each violin.</summary>
    public bool ShowExtrema { get; set; } = true;

    /// <summary>Where each violin stands along the X axis, one position per dataset; a dataset the array does not reach
    /// stands at its index. The automatic X range follows: it covers the places the violins are drawn at, a violin's
    /// <see cref="Widths"/> to either side (one side only for a half violin, see <see cref="Side"/>).</summary>
    public double[]? Positions { get; set; }

    /// <summary>The widest a violin's body can be to either side of its position, in data units.</summary>
    public double Widths { get; set; } = RestingWidths;

    /// <summary>Which side of its position each violin is drawn on: <c>Both</c> (the default) draws the whole violin,
    /// <c>Low</c> only its left half and <c>High</c> only its right half.</summary>
    public ViolinSide Side { get; set; } = RestingSide;

    private const double RestingAlpha = 0.3;
    private const double RestingWidths = 0.5;
    private const ViolinSide RestingSide = ViolinSide.Both;

    /// <summary>Initializes a new instance of <see cref="ViolinSeries"/> with the specified datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing the values for one violin.</param>
    public ViolinSeries(double[][] datasets) : base(datasets) { }

    /// <summary>How far the extrema ticks reach to either side of a violin's position, in data units.</summary>
    internal double ExtremaHalfWidth => Widths * 0.3;

    /// <summary>How far the median and mean lines reach to either side of a violin's position, in data units.</summary>
    internal double StatsHalfWidth => Widths * 0.5;

    /// <summary>How far the body reaches left of its position where it is <paramref name="halfWidth"/> wide: not at all for a
    /// body drawn on its high side only.</summary>
    /// <param name="halfWidth">The width of the body to either side of its position at one height.</param>
    /// <returns>The reach to the left.</returns>
    internal double BodyLeft(double halfWidth) => Side == ViolinSide.High ? 0 : halfWidth;

    /// <summary>How far the body reaches right of its position where it is <paramref name="halfWidth"/> wide: not at all for
    /// a body drawn on its low side only.</summary>
    /// <param name="halfWidth">The width of the body to either side of its position at one height.</param>
    /// <returns>The reach to the right.</returns>
    internal double BodyRight(double halfWidth) => Side == ViolinSide.Low ? 0 : halfWidth;

    /// <summary>The interval a violin drawn from <paramref name="sorted"/> covers around its position, in data units: the
    /// widest of its body (one side only for a half violin), its extrema ticks and its median and mean lines, each to the
    /// side it is drawn on. The renderer reads the same body outline and the same widths, so the range never lies about
    /// what is drawn.</summary>
    /// <param name="sorted">The observations that can be drawn, ascending; at least one.</param>
    /// <returns>An interval around 0: <see cref="Range1D.Lo"/> is the reach to the left, negated.</returns>
    internal Range1D ReachAround(double[] sorted)
    {
        double body = sorted.Outline(Widths).Max(slice => slice.HalfWidth);
        double marks = Math.Max(ShowExtrema ? ExtremaHalfWidth : 0, ShowMedians || ShowMeans ? StatsHalfWidth : 0);
        return new(-Math.Max(BodyLeft(body), marks), Math.Max(BodyRight(body), marks));
    }

    /// <inheritdoc />
    /// <remarks>Along X, the room each violin is drawn in around its position (<see cref="ReachAround"/>). A series that
    /// sets <see cref="Positions"/> ranges over those places alone; one that does not keeps the room around the default
    /// positions that it always had, widened for a violin wider than that. A limit set on the X axis is the range.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        var x = DrawnAround().CoverAlongPositions(Positions, new Range1D(-1, Datasets.Length));
        // Over the values the Y axis can place: a missing sample (NaN) and an infinity are not part of the extent, and on a
        // log axis neither is 0 or below (the body is clipped to the plot's edge there), so the range starts at the lowest
        // value it can place.
        var y = Datasets.DrawableRange(context.YScale);
        return new(context.XAxisMin ?? x?.Lo, context.XAxisMax ?? x?.Hi, y?.Lo, y?.Hi);
    }

    /// <summary>What each violin that can be drawn covers, at the position it stands at.</summary>
    private IEnumerable<Range1D> DrawnAround()
    {
        for (int i = 0; i < Datasets.Length; i++)
        {
            var sample = Datasets[i].SortedDrawableValues();
            if (sample.Length == 0) continue;
            double at = Positions.PositionOf(i);
            var reach = ReachAround(sample);
            yield return new Range1D(at + reach.Lo, at + reach.Hi);
        }
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "violin", Datasets = Datasets,
        Color = Color,
        Alpha = Alpha.OrNullAtRest(RestingAlpha),
        ShowMeans = ShowMeans.OrNullAtRest(false),
        ShowMedians = ShowMedians.OrNullAtRest(false),
        ShowExtrema = ShowExtrema.OrNullAtRest(true),
        Positions = Positions,
        Widths = Widths.OrNullAtRest(RestingWidths),
        Side = Side.OrNullAtRest(RestingSide),
    };

    /// <summary>Reconstructs a <see cref="ViolinSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static ViolinSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Violin(dto.Datasets ?? []);
        s.Color = dto.Color;
        s.Alpha = dto.Alpha ?? RestingAlpha;
        s.ShowMeans = dto.ShowMeans == true;
        s.ShowMedians = dto.ShowMedians == true;
        s.ShowExtrema = dto.ShowExtrema != false;
        s.Positions = dto.Positions;
        s.Widths = dto.Widths ?? RestingWidths;
        s.Side = dto.Side ?? RestingSide;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
