// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="HexbinSeries"/> as a flat-top hexagonal density grid.</summary>
internal sealed class HexbinSeriesRenderer : SeriesRenderer<HexbinSeries>
{
    /// <inheritdoc />
    public HexbinSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(HexbinSeries series)
    {
        if (series.X.Length == 0) return;

        // The hex grid covers the interval shown; on a reversed axis the transform draws it the other way round.
        var xs = new Range1D(Transform.DataXMin, Transform.DataXMax).Interval;
        var ys = new Range1D(Transform.DataYMin, Transform.DataYMax).Interval;
        double xMin = xs.Lo, xMax = xs.Hi;
        double yMin = ys.Lo, yMax = ys.Hi;

        var bins = HexGrid.ComputeHexBins(series.X, series.Y, xMin, xMax, yMin, yMax, series.GridSize);
        if (bins.Count == 0) return;   // no point could be placed

        int maxCount = bins.Values.Max();
        int minCount = series.MinCount;
        double normMin = minCount - 0.5;
        double normMax = maxCount;
        if (normMin >= normMax) normMax = normMin + 1;

        var cmap = series.GetColorMapOrDefault(ColorMaps.Viridis);
        var norm = series.Normalizer ?? LinearNormalizer.Instance;
        double hexSize = HexGrid.ComputeHexSize(xMin, xMax, series.GridSize);

        foreach (var (hex, count) in bins)
        {
            if (count < minCount) continue;

            var dataCenter = HexGrid.HexCenter(hex.Q, hex.R, hexSize, xMin, yMin);
            var dataVerts = HexGrid.HexagonVertices(dataCenter.X, dataCenter.Y, hexSize * 0.95); // 5% gap

            var pixelVerts = new List<Point>(6);
            foreach (var v in dataVerts)
                pixelVerts.Add(Transform.DataToPixel(v.X, v.Y));

            var color = cmap.ColorOf(count, norm, normMin, normMax);
            Ctx.DrawPolygon(pixelVerts, new ShapeStyle(color, null, 0));
        }
    }
}
