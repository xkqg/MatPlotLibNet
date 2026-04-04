// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>Serializes and deserializes <see cref="Figure"/> instances to and from JSON.</summary>
public sealed class ChartSerializer : IChartSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = CreateOptions(true);
    private static readonly JsonSerializerOptions CompactOptions = CreateOptions(false);

    private static JsonSerializerOptions CreateOptions(bool indented) => new()
    {
        WriteIndented = indented,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new ColorJsonConverter() }
    };

    /// <summary>Serializes a figure to its JSON representation.</summary>
    /// <param name="figure">The figure to serialize.</param>
    /// <param name="indented">Whether to produce indented JSON output.</param>
    /// <returns>A JSON string representing the figure.</returns>
    public string ToJson(Figure figure, bool indented = false)
    {
        var dto = FigureToDto(figure);
        return JsonSerializer.Serialize(dto, indented ? IndentedOptions : CompactOptions);
    }

    /// <summary>Deserializes a figure from its JSON representation.</summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The reconstructed <see cref="Figure"/> instance.</returns>
    public Figure FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<FigureDto>(json, CompactOptions)
            ?? throw new JsonException("Failed to deserialize figure.");
        return DtoToFigure(dto);
    }

    private static FigureDto FigureToDto(Figure figure) => new()
    {
        Title = figure.Title,
        Width = figure.Width,
        Height = figure.Height,
        Dpi = figure.Dpi,
        BackgroundColor = figure.BackgroundColor,
        SubPlots = figure.SubPlots.Select(AxesToDto).ToList()
    };

    private static AxesDto AxesToDto(Axes axes) => new()
    {
        Title = axes.Title,
        XAxis = AxisToDto(axes.XAxis),
        YAxis = AxisToDto(axes.YAxis),
        Grid = new GridDto { Visible = axes.Grid.Visible },
        Series = axes.Series.Select(SeriesToDto).ToList()
    };

    private static AxisDto AxisToDto(Axis axis) => new()
    {
        Label = axis.Label,
        Min = axis.Min,
        Max = axis.Max,
        Scale = axis.Scale.ToString().ToLowerInvariant()
    };

    private static SeriesDto SeriesToDto(ISeries series)
    {
        var dto = series switch
        {
            LineSeries ls => new SeriesDto
            {
                Type = "line",
                XData = ls.XData, YData = ls.YData, Color = ls.Color,
                LineStyle = ls.LineStyle.ToString().ToLowerInvariant(),
                LineWidth = ls.LineWidth
            },
            ScatterSeries ss => new SeriesDto
            {
                Type = "scatter",
                XData = ss.XData, YData = ss.YData, Color = ss.Color,
                MarkerSize = ss.MarkerSize
            },
            BarSeries bs => new SeriesDto
            {
                Type = "bar",
                Categories = bs.Categories, Values = bs.Values, Color = bs.Color,
                Orientation = bs.Orientation.ToString().ToLowerInvariant(),
                BarWidth = bs.BarWidth
            },
            HistogramSeries hs => new SeriesDto
            {
                Type = "histogram",
                Data = hs.Data, Bins = hs.Bins, Color = hs.Color
            },
            PieSeries ps => new SeriesDto
            {
                Type = "pie",
                Sizes = ps.Sizes, PieLabels = ps.Labels
            },
            BoxSeries bx => new SeriesDto { Type = "box", Datasets = bx.Datasets },
            ViolinSeries vs => new SeriesDto { Type = "violin", Datasets = vs.Datasets },
            HeatmapSeries hm => new SeriesDto { Type = "heatmap", HeatmapData = To2DList(hm.Data) },
            StemSeries st => new SeriesDto { Type = "stem", XData = st.XData, YData = st.YData },
            ContourSeries cs => new SeriesDto
            {
                Type = "contour",
                XData = cs.XData, YData = cs.YData, HeatmapData = To2DList(cs.ZData)
            },
            _ => new SeriesDto { Type = "unknown" }
        };
        dto.Label = series.Label;
        return dto;
    }

    private static Figure DtoToFigure(FigureDto dto)
    {
        var figure = new Figure
        {
            Title = dto.Title,
            Width = dto.Width,
            Height = dto.Height,
            Dpi = dto.Dpi,
            BackgroundColor = dto.BackgroundColor
        };

        foreach (var axDto in dto.SubPlots ?? [])
        {
            var axes = figure.AddSubPlot();
            axes.Title = axDto.Title;

            if (axDto.XAxis is not null) ApplyAxis(axes.XAxis, axDto.XAxis);
            if (axDto.YAxis is not null) ApplyAxis(axes.YAxis, axDto.YAxis);
            if (axDto.Grid is not null) axes.Grid = axes.Grid with { Visible = axDto.Grid.Visible };

            foreach (var sDto in axDto.Series ?? [])
                AddSeriesFromDto(axes, sDto);
        }

        return figure;
    }

    private static void ApplyAxis(Axis axis, AxisDto dto)
    {
        axis.Label = dto.Label;
        axis.Min = dto.Min;
        axis.Max = dto.Max;
        if (dto.Scale is not null && Enum.TryParse<AxisScale>(dto.Scale, true, out var scale))
            axis.Scale = scale;
    }

    private static void AddSeriesFromDto(Axes axes, SeriesDto dto)
    {
        ISeries? series = dto.Type switch
        {
            "line" => CreateLine(axes, dto),
            "scatter" => CreateScatter(axes, dto),
            "bar" => CreateBar(axes, dto),
            "histogram" => CreateHistogram(axes, dto),
            "pie" => axes.Pie(dto.Sizes ?? [], dto.PieLabels),
            "box" => axes.BoxPlot(dto.Datasets ?? []),
            "violin" => axes.Violin(dto.Datasets ?? []),
            "heatmap" => axes.Heatmap(From2DList(dto.HeatmapData)),
            "stem" => axes.Stem(dto.XData ?? [], dto.YData ?? []),
            "contour" => axes.Contour(dto.XData ?? [], dto.YData ?? [], From2DList(dto.HeatmapData)),
            _ => null
        };
        if (series is not null)
            series.Label = dto.Label;
    }

    private static LineSeries CreateLine(Axes axes, SeriesDto dto)
    {
        var s = axes.Plot(dto.XData ?? [], dto.YData ?? []);
        s.Color = dto.Color;
        s.LineWidth = dto.LineWidth ?? 1.5;
        if (dto.LineStyle is not null && Enum.TryParse<LineStyle>(dto.LineStyle, true, out var ls))
            s.LineStyle = ls;
        return s;
    }

    private static ScatterSeries CreateScatter(Axes axes, SeriesDto dto)
    {
        var s = axes.Scatter(dto.XData ?? [], dto.YData ?? []);
        s.Color = dto.Color;
        if (dto.MarkerSize.HasValue) s.MarkerSize = dto.MarkerSize.Value;
        return s;
    }

    private static BarSeries CreateBar(Axes axes, SeriesDto dto)
    {
        var s = axes.Bar(dto.Categories ?? [], dto.Values ?? []);
        s.Color = dto.Color;
        if (dto.BarWidth.HasValue) s.BarWidth = dto.BarWidth.Value;
        if (dto.Orientation is not null && Enum.TryParse<BarOrientation>(dto.Orientation, true, out var orient))
            s.Orientation = orient;
        return s;
    }

    private static HistogramSeries CreateHistogram(Axes axes, SeriesDto dto)
    {
        var s = axes.Hist(dto.Data ?? [], dto.Bins ?? 10);
        s.Color = dto.Color;
        return s;
    }

    private static List<List<double>>? To2DList(double[,] data)
    {
        int rows = data.GetLength(0), cols = data.GetLength(1);
        var result = new List<List<double>>(rows);
        for (int r = 0; r < rows; r++)
        {
            var row = new List<double>(cols);
            for (int c = 0; c < cols; c++) row.Add(data[r, c]);
            result.Add(row);
        }
        return result;
    }

    private static double[,] From2DList(List<List<double>>? data)
    {
        if (data is null || data.Count == 0) return new double[0, 0];
        int rows = data.Count, cols = data[0].Count;
        var result = new double[rows, cols];
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
            result[r, c] = data[r][c];
        return result;
    }
}

// DTOs for JSON serialization

internal sealed record FigureDto
{
    public string? Title { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    public double Dpi { get; init; }
    public Color? BackgroundColor { get; init; }
    public List<AxesDto>? SubPlots { get; init; }
}

internal sealed record AxesDto
{
    public string? Title { get; init; }
    public AxisDto? XAxis { get; init; }
    public AxisDto? YAxis { get; init; }
    public GridDto? Grid { get; init; }
    public List<SeriesDto>? Series { get; init; }
}

internal sealed record AxisDto
{
    public string? Label { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public string? Scale { get; init; }
}

internal sealed record GridDto
{
    public bool Visible { get; init; }
}

internal sealed record SeriesDto
{
    public string? Type { get; init; }
    public string? Label { get; set; }
    public double[]? XData { get; init; }
    public double[]? YData { get; init; }
    public double[]? Data { get; init; }
    public double[]? Sizes { get; init; }
    public double[]? Values { get; init; }
    public string[]? Categories { get; init; }
    public string[]? PieLabels { get; init; }
    public double[][]? Datasets { get; init; }
    public List<List<double>>? HeatmapData { get; init; }
    public Color? Color { get; init; }
    public string? LineStyle { get; init; }
    public double? LineWidth { get; init; }
    public double? MarkerSize { get; init; }
    public double? BarWidth { get; init; }
    public string? Orientation { get; init; }
    public int? Bins { get; init; }
}

internal sealed class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString();
        return hex is not null ? Color.FromHex(hex) : default;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToHex());
    }
}
