// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a pseudocolor mesh series that renders a non-uniform rectangular grid as colored cells.</summary>
/// <remarks>X has M+1 edge coordinates, Y has N+1 edge coordinates, and C is an N×M data matrix.</remarks>
public sealed class PcolormeshSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable
{
    public Vec X { get; }

    public Vec Y { get; }

    public double[,] C { get; }

    public IColorMap? ColorMap { get; set; }

    public INormalizer? Normalizer { get; set; }

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange()
    {
        double min = double.MaxValue, max = double.MinValue;
        for (int r = 0; r < C.GetLength(0); r++)
            for (int c = 0; c < C.GetLength(1); c++)
            {
                if (C[r, c] < min) min = C[r, c];
                if (C[r, c] > max) max = C[r, c];
            }
        return min < max ? new(min, max) : new(0, 1);
    }

    /// <summary>Initializes a new instance of <see cref="PcolormeshSeries"/>.</summary>
    /// <param name="x">The M+1 X-axis edge coordinates.</param>
    /// <param name="y">The N+1 Y-axis edge coordinates.</param>
    /// <param name="c">The N×M data matrix.</param>
    public PcolormeshSeries(Vec x, Vec y, double[,] c)
    {
        X = x;
        Y = y;
        C = c;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0 || Y.Length == 0) return new(0, 1, 0, 1);
        return new(X.Min(), X.Max(), Y.Min(), Y.Max());
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "pcolormesh",
        XData = X,
        YData = Y,
        HeatmapData = ChartSerializer.To2DList(C),
        ColorMapName = ColorMap?.Name
    };

    /// <summary>Reconstructs a <see cref="PcolormeshSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static PcolormeshSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Pcolormesh(dto.XData ?? [], dto.YData ?? [], ChartSerializer.From2DList(dto.HeatmapData));
        if (dto.ColorMapName is not null)
        {
            s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
