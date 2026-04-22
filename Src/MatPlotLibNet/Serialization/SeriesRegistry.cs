// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
        Register("surface", (axes, dto) =>
        {
            var z = ChartSerializer.From2DList(dto.ZGridData);
            var s = axes.Surface(dto.XData ?? [0.0, 1.0], dto.YData ?? [0.0, 1.0],
                z.GetLength(0) > 0 ? z : new double[,] { { 0, 0 }, { 0, 0 } });
            if (dto.ShowWireframe.HasValue) s.ShowWireframe = dto.ShowWireframe.Value;
            if (dto.RowStride.HasValue) s.RowStride = dto.RowStride.Value;
            if (dto.ColStride.HasValue) s.ColStride = dto.ColStride.Value;
            if (dto.Alpha.HasValue) s.Alpha = dto.Alpha.Value;
            return s;
        });
        Register("wireframe", (axes, dto) =>
        {
            var z = ChartSerializer.From2DList(dto.ZGridData);
            var s = axes.Wireframe(dto.XData ?? [0.0, 1.0], dto.YData ?? [0.0, 1.0],
                z.GetLength(0) > 0 ? z : new double[,] { { 0, 0 }, { 0, 0 } });
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            if (dto.LineWidth.HasValue) s.LineWidth = dto.LineWidth.Value;
            return s;
        });
        Register("scatter3d", (axes, dto) =>
        {
            var s = axes.Scatter3D(dto.XData ?? [0.0], dto.YData ?? [0.0], dto.ZData ?? [0.0]);
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            if (dto.MarkerSize.HasValue) s.MarkerSize = dto.MarkerSize.Value;
            return s;
        });

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
            var ranges = new BarRange[rowCount][];
            for (int r = 0; r < rowCount; r++)
            {
                int segCount = Math.Min(starts[r].Length, widths[r].Length);
                ranges[r] = new BarRange[segCount];
                for (int s2 = 0; s2 < segCount; s2++)
                    ranges[r][s2] = new BarRange(starts[r][s2], widths[r][s2]);
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

        // v1.0 Signal series
        Register("signal-xy", ChartSerializer.CreateSignalXY);
        Register("signal",    ChartSerializer.CreateSignal);

        // v1.1.1 PolarHeatmapSeries
        Register("polarheatmap", (axes, dto) =>
        {
            var s = axes.PolarHeatmap(ChartSerializer.From2DList(dto.HeatmapData),
                dto.ThetaBins ?? 8, dto.RBins ?? 4);
            if (dto.ColorMapName is not null)
                s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
            return s;
        });

        // v1.3.0 ThreeD series
        Register("line3d", (axes, dto) =>
        {
            var s = axes.Plot3D(dto.XData ?? [0.0], dto.YData ?? [0.0], dto.ZData ?? [0.0]);
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            if (dto.LineWidth.HasValue) s.LineWidth = dto.LineWidth.Value;
            if (dto.LineStyle is not null && Enum.TryParse<Styling.LineStyle>(dto.LineStyle, true, out var ls)) s.LineStyle = ls;
            return s;
        });
        Register("trisurf", (axes, dto) =>
        {
            var s = axes.Trisurf(dto.XData ?? [0.0], dto.YData ?? [0.0], dto.ZData ?? [0.0]);
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            if (dto.ShowWireframe.HasValue) s.ShowWireframe = dto.ShowWireframe.Value;
            if (dto.Alpha.HasValue) s.Alpha = dto.Alpha.Value;
            if (dto.ColorMapName is not null) s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
            return s;
        });
        Register("contour3d", (axes, dto) =>
        {
            var s = axes.Contour3D(dto.XData ?? [], dto.YData ?? [], ChartSerializer.From2DList(dto.ZGridData));
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            if (dto.Levels.HasValue) s.Levels = dto.Levels.Value;
            if (dto.LineWidth.HasValue) s.LineWidth = dto.LineWidth.Value;
            if (dto.ColorMapName is not null) s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
            return s;
        });
        Register("quiver3d", (axes, dto) =>
        {
            var s = axes.Quiver3D(dto.XData ?? [], dto.YData ?? [], dto.ZData ?? [],
                dto.UData ?? [], dto.VData ?? [], dto.WData ?? []);
            if (dto.ArrowLength.HasValue) s.ArrowLength = dto.ArrowLength.Value;
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            return s;
        });
        Register("voxels", (axes, dto) =>
        {
            var filled = VoxelDataToArray(dto.VoxelData);
            var s = axes.Voxels(filled);
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            if (dto.Alpha.HasValue) s.Alpha = dto.Alpha.Value;
            return s;
        });
        Register("text3d", (axes, dto) =>
        {
            var annotations = dto.Text3DAnnotations?
                .Select(a => new Text3DAnnotation(a.X, a.Y, a.Z, a.Text))
                .ToList() ?? [];
            var s = new Text3DSeries(annotations);
            if (dto.MarkerSize.HasValue) s.FontSize = dto.MarkerSize.Value;
            if (dto.Color.HasValue) s.Color = dto.Color.Value;
            axes.AddSeries(s);
            axes.CoordinateSystem = CoordinateSystem.ThreeD;
            return s;
        });

    }

    private static bool[,,] VoxelDataToArray(List<List<List<bool>>>? data)
    {
        if (data is null || data.Count == 0)
            return new bool[1, 1, 1];

        int xDim = data.Count;
        int yDim = data[0].Count;
        int zDim = yDim > 0 ? data[0][0].Count : 0;

        var result = new bool[xDim, yDim, zDim];
        for (int x = 0; x < xDim; x++)
            for (int y = 0; y < yDim; y++)
                for (int z = 0; z < zDim; z++)
                    result[x, y, z] = data[x][y][z];

        return result;
    }
}
