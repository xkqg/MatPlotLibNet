// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Geo.GeoJson;
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
        Register("hexbin", (axes, dto) =>
        {
            var s = axes.Hexbin(dto.XData ?? [], dto.YData ?? []);
            if (dto.GridSize.HasValue) s.GridSize = dto.GridSize.Value;
            if (dto.MinCount.HasValue) s.MinCount = dto.MinCount.Value;
            if (dto.ColorMapName is not null)
                s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
            return s;
        });
        Register("regression", (axes, dto) =>
        {
            var s = axes.Regression(dto.XData ?? [], dto.YData ?? []);
            if (dto.Degree.HasValue) s.Degree = dto.Degree.Value;
            if (dto.ShowConfidence.HasValue) s.ShowConfidence = dto.ShowConfidence.Value;
            if (dto.ConfidenceLevel.HasValue) s.ConfidenceLevel = dto.ConfidenceLevel.Value;
            if (dto.LineWidth.HasValue) s.LineWidth = dto.LineWidth.Value;
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            if (dto.FillColor.HasValue) s.BandColor = dto.FillColor.Value;
            if (dto.Alpha.HasValue) s.BandAlpha = dto.Alpha.Value;
            if (dto.LineStyle is not null && Enum.TryParse<Styling.LineStyle>(dto.LineStyle, true, out var ls)) s.LineStyle = ls;
            return s;
        });
        Register("kde", (axes, dto) =>
        {
            var s = axes.Kde(dto.Data ?? []);
            if (dto.Bandwidth.HasValue) s.Bandwidth = dto.Bandwidth.Value;
            if (dto.Alpha.HasValue) s.Alpha = dto.Alpha.Value;
            if (dto.LineWidth.HasValue) s.LineWidth = dto.LineWidth.Value;
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            if (dto.LineStyle is not null && Enum.TryParse<Styling.LineStyle>(dto.LineStyle, true, out var ls)) s.LineStyle = ls;
            return s;
        });
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
        Register("contourf", (axes, dto) => axes.Contourf(dto.XData ?? [], dto.YData ?? [], ChartSerializer.From2DList(dto.HeatmapData)));
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

        // v0.8.0
        Register("rugplot", (axes, dto) =>
        {
            var s = axes.Rugplot(dto.Data ?? []);
            if (dto.RugHeight.HasValue) s.Height = dto.RugHeight.Value;
            if (dto.Alpha.HasValue) s.Alpha = dto.Alpha.Value;
            if (dto.LineWidth.HasValue) s.LineWidth = dto.LineWidth.Value;
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            return s;
        });
        Register("stripplot", (axes, dto) =>
        {
            var s = axes.Stripplot(dto.Datasets ?? []);
            if (dto.Jitter.HasValue) s.Jitter = dto.Jitter.Value;
            if (dto.MarkerSize.HasValue) s.MarkerSize = dto.MarkerSize.Value;
            if (dto.Alpha.HasValue) s.Alpha = dto.Alpha.Value;
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            return s;
        });
        Register("eventplot", (axes, dto) =>
        {
            var s = axes.Eventplot(dto.EventPositions ?? []);
            if (dto.LineWidth.HasValue) s.LineWidth = dto.LineWidth.Value;
            if (dto.LineLength.HasValue) s.LineLength = dto.LineLength.Value;
            return s;
        });
        Register("brokenbar", (axes, dto) =>
        {
            var starts = dto.RangeStarts ?? [];
            var widths = dto.RangeWidths ?? [];
            int rowCount = Math.Min(starts.Length, widths.Length);
            var ranges = new (double, double)[rowCount][];
            for (int r = 0; r < rowCount; r++)
            {
                int segCount = Math.Min(starts[r].Length, widths[r].Length);
                ranges[r] = new (double, double)[segCount];
                for (int s2 = 0; s2 < segCount; s2++)
                    ranges[r][s2] = (starts[r][s2], widths[r][s2]);
            }
            var s = axes.BrokenBarH(ranges);
            if (dto.BarHeight.HasValue) s.BarHeight = dto.BarHeight.Value;
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            if (dto.Categories is not null) s.Labels = dto.Categories;
            return s;
        });
        Register("count", (axes, dto) =>
        {
            var s = axes.Countplot(dto.Categories ?? []);
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            if (dto.BarWidth.HasValue) s.BarWidth = dto.BarWidth.Value;
            if (dto.Orientation is not null && Enum.TryParse<Models.Series.BarOrientation>(dto.Orientation, true, out var ori)) s.Orientation = ori;
            return s;
        });
        Register("pcolormesh", (axes, dto) =>
        {
            var s = axes.Pcolormesh(dto.XData ?? [], dto.YData ?? [], ChartSerializer.From2DList(dto.HeatmapData));
            if (dto.ColorMapName is not null)
                s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
            return s;
        });
        Register("residual", (axes, dto) =>
        {
            var s = axes.Residplot(dto.XData ?? [], dto.YData ?? []);
            if (dto.Degree.HasValue) s.Degree = dto.Degree.Value;
            if (dto.MarkerSize.HasValue) s.MarkerSize = dto.MarkerSize.Value;
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            return s;
        });

        // v0.8.0 Phase B
        Register("pointplot", (axes, dto) =>
        {
            var s = axes.Pointplot(dto.Datasets ?? []);
            if (dto.MarkerSize.HasValue) s.MarkerSize = dto.MarkerSize.Value;
            if (dto.CapSize.HasValue) s.CapSize = dto.CapSize.Value;
            if (dto.ConfidenceLevel.HasValue) s.ConfidenceLevel = dto.ConfidenceLevel.Value;
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            if (dto.Categories is not null) s.Categories = dto.Categories;
            return s;
        });
        Register("swarmplot", (axes, dto) =>
        {
            var s = axes.Swarmplot(dto.Datasets ?? []);
            if (dto.MarkerSize.HasValue) s.MarkerSize = dto.MarkerSize.Value;
            if (dto.Alpha.HasValue) s.Alpha = dto.Alpha.Value;
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            return s;
        });
        Register("spectrogram", (axes, dto) =>
        {
            var s = axes.Spectrogram(dto.Signal ?? [], dto.SampleRate ?? 1);
            if (dto.WindowSize.HasValue) s.WindowSize = dto.WindowSize.Value;
            if (dto.Overlap.HasValue) s.Overlap = dto.Overlap.Value;
            if (dto.ColorMapName is not null) s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
            return s;
        });
        Register("table", (axes, dto) =>
        {
            var s = axes.Table(dto.TableCellData ?? []);
            if (dto.ColumnHeaders is not null) s.ColumnHeaders = dto.ColumnHeaders;
            if (dto.RowHeaders is not null) s.RowHeaders = dto.RowHeaders;
            return s;
        });

        // v0.8.0 Phase C
        Register("tricontour", (axes, dto) =>
        {
            var s = axes.Tricontour(dto.XData ?? [], dto.YData ?? [], dto.ZData ?? []);
            if (dto.Levels.HasValue) s.Levels = dto.Levels.Value;
            if (dto.ColorMapName is not null) s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
            return s;
        });
        Register("tripcolor", (axes, dto) =>
        {
            var s = axes.Tripcolor(dto.XData ?? [], dto.YData ?? [], dto.ZData ?? []);
            if (dto.Triangles is not null) s.Triangles = dto.Triangles;
            if (dto.ColorMapName is not null) s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
            return s;
        });
        Register("quiverkey", (axes, dto) =>
            axes.QuiverKey(dto.QuiverKeyX ?? 0.5, dto.QuiverKeyY ?? 0.9, dto.QuiverKeyU ?? 1.0, dto.QuiverKeyLabel ?? ""));
        Register("barbs", (axes, dto) =>
        {
            var s = axes.Barbs(dto.XData ?? [], dto.YData ?? [], dto.Speed ?? [], dto.Direction ?? []);
            if (dto.BarbLength.HasValue) s.BarbLength = dto.BarbLength.Value;
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            return s;
        });

        // v0.8.0 Phase D
        Register("stem3d", (axes, dto) =>
        {
            var s = axes.Stem3D(dto.XData ?? [], dto.YData ?? [], dto.ZData ?? []);
            if (dto.MarkerSize.HasValue) s.MarkerSize = dto.MarkerSize.Value;
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            return s;
        });
        Register("bar3d", (axes, dto) =>
        {
            var s = axes.Bar3D(dto.XData ?? [], dto.YData ?? [], dto.ZData ?? []);
            if (dto.BarWidth.HasValue) s.BarWidth = dto.BarWidth.Value;
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            return s;
        });

        // v0.8.9 Phase F — Geo
        Register("map", (axes, dto) =>
        {
            var geoData = dto.GeoJson is not null ? Geo.GeoJson.GeoJsonReader.FromJson(dto.GeoJson) : null;
            var s = axes.Map(geoData);
            s.Projection = Models.Series.MapSeries.ProjectionFromName(dto.Projection);
            if (dto.Color.HasValue) s.EdgeColor = dto.Color.Value;
            if (dto.LineWidth.HasValue) s.LineWidth = dto.LineWidth.Value;
            s.FaceColor = dto.FillColor;
            return s;
        });
        Register("choropleth", (axes, dto) =>
        {
            var geoData = dto.GeoJson is not null ? Geo.GeoJson.GeoJsonReader.FromJson(dto.GeoJson) : null;
            var s = axes.Choropleth(geoData ?? new Geo.GeoJson.GeoJsonDocument("FeatureCollection",
                new Geo.GeoJson.GeoJsonFeatureCollection([])), dto.Values ?? []);
            s.Projection = Models.Series.MapSeries.ProjectionFromName(dto.Projection);
            if (dto.ColorMapName is not null) s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
            s.VMin = dto.VMin;
            s.VMax = dto.VMax;
            if (dto.Color.HasValue) s.EdgeColor = dto.Color.Value;
            if (dto.LineWidth.HasValue) s.LineWidth = dto.LineWidth.Value;
            return s;
        });
    }
}
