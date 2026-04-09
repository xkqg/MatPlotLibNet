// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Serialization;

/// <summary>Registry mapping series type discriminators to factory functions for deserialization.</summary>
public static class SeriesRegistry
{
    private static readonly ConcurrentDictionary<string, Func<Axes, SeriesDto, ISeries?>> Factories = new();

    /// <summary>Registers a factory for a series type discriminator.</summary>
    public static void Register(string typeDiscriminator, Func<Axes, SeriesDto, ISeries?> factory)
        => Factories[typeDiscriminator] = factory;

    /// <summary>Creates a series from a DTO using the registered factory.</summary>
    public static ISeries? Create(string typeDiscriminator, Axes axes, SeriesDto dto)
        => Factories.TryGetValue(typeDiscriminator, out var factory) ? factory(axes, dto) : null;

    static SeriesRegistry() => RegisterDefaults();

    private static void RegisterDefaults()
    {
        Register("line", ChartSerializer.CreateLine);
        Register("scatter", ChartSerializer.CreateScatter);
        Register("bar", ChartSerializer.CreateBar);
        Register("histogram", ChartSerializer.CreateHistogram);
        Register("pie", (axes, dto) => axes.Pie(dto.Sizes ?? [], dto.PieLabels));
        Register("box", (axes, dto) => axes.BoxPlot(dto.Datasets ?? []));
        Register("violin", (axes, dto) => axes.Violin(dto.Datasets ?? []));
        Register("heatmap", (axes, dto) =>
        {
            var hs = axes.Heatmap(ChartSerializer.From2DList(dto.HeatmapData));
            if (dto.ColorMapName is not null)
                hs.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
            return hs;
        });
        Register("image", ChartSerializer.CreateImage);
        Register("histogram2d", ChartSerializer.CreateHistogram2D);
        Register("stem", (axes, dto) => axes.Stem(dto.XData ?? [], dto.YData ?? []));
        Register("contour", (axes, dto) => axes.Contour(dto.XData ?? [], dto.YData ?? [], ChartSerializer.From2DList(dto.HeatmapData)));
        Register("area", ChartSerializer.CreateArea);
        Register("step", ChartSerializer.CreateStep);
        Register("ecdf", ChartSerializer.CreateEcdf);
        Register("stackedarea", ChartSerializer.CreateStackedArea);
        Register("errorbar", ChartSerializer.CreateErrorBar);
        Register("candlestick", ChartSerializer.CreateCandlestick);
        Register("quiver", ChartSerializer.CreateQuiver);
        Register("streamplot", ChartSerializer.CreateStreamplot);
        Register("radar", ChartSerializer.CreateRadar);
        Register("donut", ChartSerializer.CreateDonut);
        Register("bubble", ChartSerializer.CreateBubble);
        Register("ohlcbar", ChartSerializer.CreateOhlcBar);
        Register("waterfall", ChartSerializer.CreateWaterfall);
        Register("funnel", ChartSerializer.CreateFunnel);
        Register("gantt", ChartSerializer.CreateGantt);
        Register("gauge", ChartSerializer.CreateGauge);
        Register("progressbar", ChartSerializer.CreateProgressBar);
        Register("sparkline", ChartSerializer.CreateSparkline);
        Register("treemap", (axes, _) => axes.Treemap(new TreeNode { Label = "Root" }));
        Register("sunburst", (axes, _) => axes.Sunburst(new TreeNode { Label = "Root" }));
        Register("sankey", (axes, _) => axes.Sankey([new SankeyNode("A")], []));
        Register("polarline", (axes, _) => axes.PolarPlot([1.0], [0.0]));
        Register("polarscatter", (axes, _) => axes.PolarScatter([1.0], [0.0]));
        Register("polarbar", (axes, _) => axes.PolarBar([1.0], [0.0]));
        Register("surface", (axes, _) => axes.Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 0 }, { 0, 0 } }));
        Register("wireframe", (axes, _) => axes.Wireframe([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 0 }, { 0, 0 } }));
        Register("scatter3d", (axes, _) => axes.Scatter3D([0.0], [0.0], [0.0]));
    }
}
