// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class ContourSeriesRenderer : SeriesRenderer<ContourSeries>
{
    public ContourSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(ContourSeries series)
    {
        new HeatmapSeriesRenderer(Context).Render(new HeatmapSeries(series.ZData) { ColorMap = series.ColorMap });
    }
}
