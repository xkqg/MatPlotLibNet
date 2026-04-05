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

    /// <inheritdoc />
    public string ToJson(Figure figure, bool indented = false)
    {
        var dto = FigureToDto(figure);
        return JsonSerializer.Serialize(dto, indented ? IndentedOptions : CompactOptions);
    }

    /// <inheritdoc />
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
        BarMode = axes.BarMode == BarMode.Stacked ? "stacked" : null,
        Series = axes.Series.Select(SeriesToDto).ToList(),
        Annotations = axes.Annotations.Count > 0 ? axes.Annotations.Select(a => new AnnotationDto
        {
            Text = a.Text, X = a.X, Y = a.Y,
            ArrowTargetX = a.ArrowTargetX, ArrowTargetY = a.ArrowTargetY
        }).ToList() : null,
        ReferenceLines = axes.ReferenceLines.Count > 0 ? axes.ReferenceLines.Select(r => new ReferenceLineDto
        {
            Value = r.Value, Orientation = r.Orientation.ToString().ToLowerInvariant(),
            LineStyle = r.LineStyle.ToString().ToLowerInvariant(), LineWidth = r.LineWidth, Label = r.Label
        }).ToList() : null,
        SecondaryYAxis = axes.SecondaryYAxis is not null ? AxisToDto(axes.SecondaryYAxis) : null,
        SecondarySeries = axes.SecondarySeries.Count > 0 ? axes.SecondarySeries.Select(SeriesToDto).ToList() : null,
        Spans = axes.Spans.Count > 0 ? axes.Spans.Select(s => new SpanRegionDto
        {
            Min = s.Min, Max = s.Max, Orientation = s.Orientation.ToString().ToLowerInvariant(),
            Alpha = s.Alpha
        }).ToList() : null
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
            RadarSeries rs => new SeriesDto
            {
                Type = "radar",
                Categories = rs.Categories, Values = rs.Values,
                Color = rs.Color, FillColor = rs.FillColor,
                Alpha = rs.Alpha, LineWidth = rs.LineWidth, MaxValue = rs.MaxValue
            },
            QuiverSeries qs => new SeriesDto
            {
                Type = "quiver",
                XData = qs.XData, YData = qs.YData,
                UData = qs.UData, VData = qs.VData,
                Color = qs.Color, Scale = qs.Scale, ArrowHeadSize = qs.ArrowHeadSize
            },
            CandlestickSeries cs => new SeriesDto
            {
                Type = "candlestick",
                Open = cs.Open, High = cs.High, Low = cs.Low, Close = cs.Close,
                DateLabels = cs.DateLabels, UpColor = cs.UpColor, DownColor = cs.DownColor,
                BodyWidth = cs.BodyWidth
            },
            ErrorBarSeries eb => new SeriesDto
            {
                Type = "errorbar",
                XData = eb.XData, YData = eb.YData,
                YErrorLow = eb.YErrorLow, YErrorHigh = eb.YErrorHigh,
                XErrorLow = eb.XErrorLow, XErrorHigh = eb.XErrorHigh,
                Color = eb.Color, LineWidth = eb.LineWidth, CapSize = eb.CapSize
            },
            StepSeries ss => new SeriesDto
            {
                Type = "step",
                XData = ss.XData, YData = ss.YData, Color = ss.Color,
                LineStyle = ss.LineStyle.ToString().ToLowerInvariant(),
                LineWidth = ss.LineWidth,
                StepPosition = ss.StepPosition.ToString().ToLowerInvariant()
            },
            AreaSeries ar => new SeriesDto
            {
                Type = "area",
                XData = ar.XData, YData = ar.YData, YData2 = ar.YData2,
                Color = ar.Color, Alpha = ar.Alpha,
                LineStyle = ar.LineStyle.ToString().ToLowerInvariant(),
                LineWidth = ar.LineWidth
            },
            DonutSeries ds => new SeriesDto
            {
                Type = "donut",
                Sizes = ds.Sizes, PieLabels = ds.Labels,
                InnerRadius = ds.InnerRadius, CenterText = ds.CenterText,
                StartAngle = ds.StartAngle
            },
            BubbleSeries bs2 => new SeriesDto
            {
                Type = "bubble",
                XData = bs2.XData, YData = bs2.YData, Sizes = bs2.Sizes,
                Color = bs2.Color, Alpha = bs2.Alpha
            },
            OhlcBarSeries ob => new SeriesDto
            {
                Type = "ohlcbar",
                Open = ob.Open, High = ob.High, Low = ob.Low, Close = ob.Close,
                DateLabels = ob.DateLabels, UpColor = ob.UpColor, DownColor = ob.DownColor,
                TickWidth = ob.TickWidth
            },
            WaterfallSeries ws => new SeriesDto
            {
                Type = "waterfall",
                Categories = ws.Categories, Values = ws.Values,
                IncreaseColor = ws.IncreaseColor, DecreaseColor = ws.DecreaseColor,
                TotalColor = ws.TotalColor, BarWidth = ws.BarWidth
            },
            FunnelSeries fs => new SeriesDto
            {
                Type = "funnel",
                PieLabels = fs.Labels, Values = fs.Values
            },
            GanttSeries gs => new SeriesDto
            {
                Type = "gantt",
                Tasks = gs.Tasks, Starts = gs.Starts, Ends = gs.Ends,
                Color = gs.Color, BarHeight = gs.BarHeight
            },
            GaugeSeries gg => new SeriesDto
            {
                Type = "gauge",
                GaugeValue = gg.Value, GaugeMin = gg.Min, GaugeMax = gg.Max,
                NeedleColor = gg.NeedleColor
            },
            ProgressBarSeries pb => new SeriesDto
            {
                Type = "progressbar",
                GaugeValue = pb.Value, FillColor = pb.FillColor,
                TrackColor = pb.TrackColor, BarHeight = pb.BarHeight
            },
            SparklineSeries sl => new SeriesDto
            {
                Type = "sparkline",
                Values = sl.Values, Color = sl.Color, LineWidth = sl.LineWidth
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
            if (axDto.BarMode is "stacked") axes.BarMode = BarMode.Stacked;

            foreach (var sDto in axDto.Series ?? [])
                AddSeriesFromDto(axes, sDto);

            foreach (var aDto in axDto.Annotations ?? [])
            {
                var ann = axes.Annotate(aDto.Text ?? "", aDto.X, aDto.Y);
                ann.ArrowTargetX = aDto.ArrowTargetX;
                ann.ArrowTargetY = aDto.ArrowTargetY;
            }

            foreach (var rDto in axDto.ReferenceLines ?? [])
            {
                var orient = rDto.Orientation is "vertical" ? Orientation.Vertical : Orientation.Horizontal;
                var rl = orient == Orientation.Horizontal ? axes.AxHLine(rDto.Value) : axes.AxVLine(rDto.Value);
                if (rDto.LineStyle is not null && Enum.TryParse<LineStyle>(rDto.LineStyle, true, out var ls))
                    rl.LineStyle = ls;
                rl.LineWidth = rDto.LineWidth;
                rl.Label = rDto.Label;
            }

            if (axDto.SecondaryYAxis is not null)
            {
                axes.TwinX();
                ApplyAxis(axes.SecondaryYAxis!, axDto.SecondaryYAxis);
                foreach (var sDto in axDto.SecondarySeries ?? [])
                {
                    // Route secondary series through PlotSecondary/ScatterSecondary
                    ISeries? sec = sDto.Type switch
                    {
                        "line" => CreateSecondaryLine(axes, sDto),
                        "scatter" => CreateSecondaryScatter(axes, sDto),
                        _ => null
                    };
                    if (sec is not null) sec.Label = sDto.Label;
                }
            }

            foreach (var sDto in axDto.Spans ?? [])
            {
                var orient = sDto.Orientation is "vertical" ? Orientation.Vertical : Orientation.Horizontal;
                var sp = orient == Orientation.Horizontal ? axes.AxHSpan(sDto.Min, sDto.Max) : axes.AxVSpan(sDto.Min, sDto.Max);
                sp.Alpha = sDto.Alpha;
            }
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
            "area" => CreateArea(axes, dto),
            "step" => CreateStep(axes, dto),
            "errorbar" => CreateErrorBar(axes, dto),
            "candlestick" => CreateCandlestick(axes, dto),
            "quiver" => CreateQuiver(axes, dto),
            "radar" => CreateRadar(axes, dto),
            "donut" => CreateDonut(axes, dto),
            "bubble" => CreateBubble(axes, dto),
            "ohlcbar" => CreateOhlcBar(axes, dto),
            "waterfall" => CreateWaterfall(axes, dto),
            "funnel" => CreateFunnel(axes, dto),
            "gantt" => CreateGantt(axes, dto),
            "gauge" => CreateGauge(axes, dto),
            "progressbar" => CreateProgressBar(axes, dto),
            "sparkline" => CreateSparkline(axes, dto),
            _ => null
        };
        if (series is not null)
            series.Label = dto.Label;
    }

    /// <summary>Reconstructs a <see cref="LineSeries"/> from the DTO and adds it to the axes.</summary>
    private static LineSeries CreateLine(Axes axes, SeriesDto dto)
    {
        var s = axes.Plot(dto.XData ?? [], dto.YData ?? []);
        ApplyLineProperties(s, dto);
        return s;
    }

    /// <summary>Reconstructs a <see cref="ScatterSeries"/> from the DTO and adds it to the axes.</summary>
    private static ScatterSeries CreateScatter(Axes axes, SeriesDto dto)
    {
        var s = axes.Scatter(dto.XData ?? [], dto.YData ?? []);
        s.Color = dto.Color;
        if (dto.MarkerSize.HasValue) s.MarkerSize = dto.MarkerSize.Value;
        return s;
    }

    /// <summary>Reconstructs a <see cref="BarSeries"/> from the DTO, including orientation, and adds it to the axes.</summary>
    private static BarSeries CreateBar(Axes axes, SeriesDto dto)
    {
        var s = axes.Bar(dto.Categories ?? [], dto.Values ?? []);
        s.Color = dto.Color;
        if (dto.BarWidth.HasValue) s.BarWidth = dto.BarWidth.Value;
        ApplyEnum<BarOrientation>(dto.Orientation, v => s.Orientation = v);
        return s;
    }

    /// <summary>Reconstructs a <see cref="HistogramSeries"/> from the DTO and adds it to the axes.</summary>
    private static HistogramSeries CreateHistogram(Axes axes, SeriesDto dto)
    {
        var s = axes.Hist(dto.Data ?? [], dto.Bins ?? 10);
        s.Color = dto.Color;
        return s;
    }

    /// <summary>Reconstructs a <see cref="LineSeries"/> on the secondary Y-axis from the DTO.</summary>
    private static LineSeries CreateSecondaryLine(Axes axes, SeriesDto dto)
    {
        var s = axes.PlotSecondary(dto.XData ?? [], dto.YData ?? []);
        ApplyLineProperties(s, dto);
        return s;
    }

    /// <summary>Reconstructs a <see cref="ScatterSeries"/> on the secondary Y-axis from the DTO.</summary>
    private static ScatterSeries CreateSecondaryScatter(Axes axes, SeriesDto dto)
    {
        var s = axes.ScatterSecondary(dto.XData ?? [], dto.YData ?? []);
        s.Color = dto.Color;
        if (dto.MarkerSize.HasValue) s.MarkerSize = dto.MarkerSize.Value;
        return s;
    }

    /// <summary>Reconstructs a <see cref="RadarSeries"/> from the DTO, including fill color and max value.</summary>
    private static RadarSeries CreateRadar(Axes axes, SeriesDto dto)
    {
        var s = axes.Radar(dto.Categories ?? [], dto.Values ?? []);
        s.Color = dto.Color;
        s.FillColor = dto.FillColor;
        if (dto.Alpha.HasValue) s.Alpha = dto.Alpha.Value;
        s.LineWidth = dto.LineWidth ?? 2.0;
        s.MaxValue = dto.MaxValue;
        return s;
    }

    /// <summary>Reconstructs a <see cref="QuiverSeries"/> from the DTO, including scale and arrowhead size.</summary>
    private static QuiverSeries CreateQuiver(Axes axes, SeriesDto dto)
    {
        var s = axes.Quiver(dto.XData ?? [], dto.YData ?? [], dto.UData ?? [], dto.VData ?? []);
        s.Color = dto.Color;
        if (dto.Scale.HasValue) s.Scale = dto.Scale.Value;
        if (dto.ArrowHeadSize.HasValue) s.ArrowHeadSize = dto.ArrowHeadSize.Value;
        return s;
    }

    /// <summary>Reconstructs a <see cref="CandlestickSeries"/> from the DTO, including up/down colors and date labels.</summary>
    private static CandlestickSeries CreateCandlestick(Axes axes, SeriesDto dto)
    {
        var s = axes.Candlestick(dto.Open ?? [], dto.High ?? [], dto.Low ?? [], dto.Close ?? [], dto.DateLabels);
        if (dto.UpColor.HasValue) s.UpColor = dto.UpColor.Value;
        if (dto.DownColor.HasValue) s.DownColor = dto.DownColor.Value;
        if (dto.BodyWidth.HasValue) s.BodyWidth = dto.BodyWidth.Value;
        return s;
    }

    /// <summary>Reconstructs an <see cref="ErrorBarSeries"/> from the DTO, including optional X error bars.</summary>
    private static ErrorBarSeries CreateErrorBar(Axes axes, SeriesDto dto)
    {
        var s = axes.ErrorBar(dto.XData ?? [], dto.YData ?? [], dto.YErrorLow ?? [], dto.YErrorHigh ?? []);
        s.Color = dto.Color;
        s.LineWidth = dto.LineWidth ?? 1.5;
        if (dto.CapSize.HasValue) s.CapSize = dto.CapSize.Value;
        s.XErrorLow = dto.XErrorLow;
        s.XErrorHigh = dto.XErrorHigh;
        return s;
    }

    /// <summary>Reconstructs a <see cref="StepSeries"/> from the DTO, including step position.</summary>
    private static StepSeries CreateStep(Axes axes, SeriesDto dto)
    {
        var s = axes.Step(dto.XData ?? [], dto.YData ?? []);
        s.Color = dto.Color;
        s.LineWidth = dto.LineWidth ?? 1.5;
        ApplyEnum<LineStyle>(dto.LineStyle, v => s.LineStyle = v);
        ApplyEnum<StepPosition>(dto.StepPosition, v => s.StepPosition = v);
        return s;
    }

    /// <summary>Reconstructs an <see cref="AreaSeries"/> from the DTO, including optional second Y dataset for fill-between.</summary>
    private static AreaSeries CreateArea(Axes axes, SeriesDto dto)
    {
        var s = axes.FillBetween(dto.XData ?? [], dto.YData ?? [], dto.YData2);
        s.Color = dto.Color;
        if (dto.Alpha.HasValue) s.Alpha = dto.Alpha.Value;
        s.LineWidth = dto.LineWidth ?? 1.5;
        ApplyEnum<LineStyle>(dto.LineStyle, v => s.LineStyle = v);
        return s;
    }

    /// <summary>Reconstructs a <see cref="DonutSeries"/> from the DTO, including inner radius and center text.</summary>
    private static DonutSeries CreateDonut(Axes axes, SeriesDto dto)
    {
        var s = axes.Donut(dto.Sizes ?? [], dto.PieLabels);
        if (dto.InnerRadius.HasValue) s.InnerRadius = dto.InnerRadius.Value;
        s.CenterText = dto.CenterText;
        if (dto.StartAngle.HasValue) s.StartAngle = dto.StartAngle.Value;
        return s;
    }

    /// <summary>Reconstructs a <see cref="BubbleSeries"/> from the DTO, including alpha transparency.</summary>
    private static BubbleSeries CreateBubble(Axes axes, SeriesDto dto)
    {
        var s = axes.Bubble(dto.XData ?? [], dto.YData ?? [], dto.Sizes ?? []);
        s.Color = dto.Color;
        if (dto.Alpha.HasValue) s.Alpha = dto.Alpha.Value;
        return s;
    }

    /// <summary>Reconstructs an <see cref="OhlcBarSeries"/> from the DTO, including up/down colors and tick width.</summary>
    private static OhlcBarSeries CreateOhlcBar(Axes axes, SeriesDto dto)
    {
        var s = axes.OhlcBar(dto.Open ?? [], dto.High ?? [], dto.Low ?? [], dto.Close ?? [], dto.DateLabels);
        if (dto.UpColor.HasValue) s.UpColor = dto.UpColor.Value;
        if (dto.DownColor.HasValue) s.DownColor = dto.DownColor.Value;
        if (dto.TickWidth.HasValue) s.TickWidth = dto.TickWidth.Value;
        return s;
    }

    /// <summary>Reconstructs a <see cref="WaterfallSeries"/> from the DTO, including increase/decrease/total colors.</summary>
    private static WaterfallSeries CreateWaterfall(Axes axes, SeriesDto dto)
    {
        var s = axes.Waterfall(dto.Categories ?? [], dto.Values ?? []);
        if (dto.IncreaseColor.HasValue) s.IncreaseColor = dto.IncreaseColor.Value;
        if (dto.DecreaseColor.HasValue) s.DecreaseColor = dto.DecreaseColor.Value;
        if (dto.TotalColor.HasValue) s.TotalColor = dto.TotalColor.Value;
        if (dto.BarWidth.HasValue) s.BarWidth = dto.BarWidth.Value;
        return s;
    }

    /// <summary>Reconstructs a <see cref="FunnelSeries"/> from the DTO.</summary>
    private static FunnelSeries CreateFunnel(Axes axes, SeriesDto dto)
    {
        var s = axes.Funnel(dto.PieLabels ?? [], dto.Values ?? []);
        return s;
    }

    /// <summary>Reconstructs a <see cref="GanttSeries"/> from the DTO, including bar height.</summary>
    private static GanttSeries CreateGantt(Axes axes, SeriesDto dto)
    {
        var s = axes.Gantt(dto.Tasks ?? [], dto.Starts ?? [], dto.Ends ?? []);
        s.Color = dto.Color;
        if (dto.BarHeight.HasValue) s.BarHeight = dto.BarHeight.Value;
        return s;
    }

    /// <summary>Reconstructs a <see cref="GaugeSeries"/> from the DTO, including min/max range and needle color.</summary>
    private static GaugeSeries CreateGauge(Axes axes, SeriesDto dto)
    {
        var s = axes.Gauge(dto.GaugeValue ?? 0);
        if (dto.GaugeMin.HasValue) s.Min = dto.GaugeMin.Value;
        if (dto.GaugeMax.HasValue) s.Max = dto.GaugeMax.Value;
        if (dto.NeedleColor.HasValue) s.NeedleColor = dto.NeedleColor.Value;
        return s;
    }

    /// <summary>Reconstructs a <see cref="ProgressBarSeries"/> from the DTO, including fill and track colors.</summary>
    private static ProgressBarSeries CreateProgressBar(Axes axes, SeriesDto dto)
    {
        var s = axes.ProgressBar(dto.GaugeValue ?? 0);
        if (dto.FillColor.HasValue) s.FillColor = dto.FillColor.Value;
        if (dto.TrackColor.HasValue) s.TrackColor = dto.TrackColor.Value;
        if (dto.BarHeight.HasValue) s.BarHeight = dto.BarHeight.Value;
        return s;
    }

    /// <summary>Reconstructs a <see cref="SparklineSeries"/> from the DTO, including color and line width.</summary>
    private static SparklineSeries CreateSparkline(Axes axes, SeriesDto dto)
    {
        var s = axes.Sparkline(dto.Values ?? []);
        s.Color = dto.Color;
        s.LineWidth = dto.LineWidth ?? 1.5;
        return s;
    }

    /// <summary>Parses a string to an enum value and applies it via the <paramref name="setter"/> delegate if successful.</summary>
    /// <remarks>Uses case-insensitive parsing. No-ops when <paramref name="value"/> is null or not a valid enum member.</remarks>
    private static void ApplyEnum<T>(string? value, Action<T> setter) where T : struct, Enum
    {
        if (value is not null && Enum.TryParse<T>(value, true, out var parsed))
            setter(parsed);
    }

    /// <summary>Applies the common line series properties (color, width, style) from a DTO to a <see cref="LineSeries"/>.</summary>
    /// <remarks>Shared between primary and secondary line series deserialization to avoid duplication.</remarks>
    private static void ApplyLineProperties(LineSeries s, SeriesDto dto)
    {
        s.Color = dto.Color;
        s.LineWidth = dto.LineWidth ?? 1.5;
        ApplyEnum<LineStyle>(dto.LineStyle, v => s.LineStyle = v);
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

// DTOs for JSON serialization — flat records used by System.Text.Json for round-trip serialization.
// All properties are nullable to support sparse JSON where only relevant fields are present.

/// <summary>JSON-serializable representation of a <see cref="Models.Figure"/>.</summary>
internal sealed record FigureDto
{
    public string? Title { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    public double Dpi { get; init; }
    public Color? BackgroundColor { get; init; }
    public List<AxesDto>? SubPlots { get; init; }
}

/// <summary>JSON-serializable representation of an <see cref="Models.Axes"/> subplot, including series, annotations, and decorations.</summary>
internal sealed record AxesDto
{
    public string? Title { get; init; }
    public AxisDto? XAxis { get; init; }
    public AxisDto? YAxis { get; init; }
    public GridDto? Grid { get; init; }
    public string? BarMode { get; init; }
    public List<SeriesDto>? Series { get; init; }
    public AxisDto? SecondaryYAxis { get; init; }
    public List<SeriesDto>? SecondarySeries { get; init; }
    public List<AnnotationDto>? Annotations { get; init; }
    public List<ReferenceLineDto>? ReferenceLines { get; init; }
    public List<SpanRegionDto>? Spans { get; init; }
}

internal sealed record AnnotationDto
{
    public string? Text { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double? ArrowTargetX { get; init; }
    public double? ArrowTargetY { get; init; }
}

internal sealed record ReferenceLineDto
{
    public double Value { get; init; }
    public string? Orientation { get; init; }
    public string? LineStyle { get; init; }
    public double LineWidth { get; init; }
    public string? Label { get; init; }
}

internal sealed record SpanRegionDto
{
    public double Min { get; init; }
    public double Max { get; init; }
    public string? Orientation { get; init; }
    public double Alpha { get; init; }
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

/// <summary>Unified flat DTO for all series types. The <see cref="Type"/> discriminator determines which properties are populated.</summary>
/// <remarks>This is intentionally a wide record — each series type uses a different subset of properties.
/// Unused properties serialize as null and are omitted from JSON output via <c>DefaultIgnoreCondition.WhenWritingNull</c>.</remarks>
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
    public double[]? YData2 { get; init; }
    public double? Alpha { get; init; }
    public string? StepPosition { get; init; }
    public double[]? YErrorLow { get; init; }
    public double[]? YErrorHigh { get; init; }
    public double[]? XErrorLow { get; init; }
    public double[]? XErrorHigh { get; init; }
    public double? CapSize { get; init; }
    public double[]? Open { get; init; }
    public double[]? High { get; init; }
    public double[]? Low { get; init; }
    public double[]? Close { get; init; }
    public string[]? DateLabels { get; init; }
    public Color? UpColor { get; init; }
    public Color? DownColor { get; init; }
    public double? BodyWidth { get; init; }
    public double[]? UData { get; init; }
    public double[]? VData { get; init; }
    public double? Scale { get; init; }
    public double? ArrowHeadSize { get; init; }
    public Color? FillColor { get; init; }
    public double? MaxValue { get; init; }
    public double? InnerRadius { get; init; }
    public string? CenterText { get; init; }
    public double? StartAngle { get; init; }
    public double? TickWidth { get; init; }
    public Color? IncreaseColor { get; init; }
    public Color? DecreaseColor { get; init; }
    public Color? TotalColor { get; init; }
    public string[]? Tasks { get; init; }
    public double[]? Starts { get; init; }
    public double[]? Ends { get; init; }
    public double? BarHeight { get; init; }
    public double? GaugeValue { get; init; }
    public double? GaugeMin { get; init; }
    public double? GaugeMax { get; init; }
    public Color? NeedleColor { get; init; }
    public Color? TrackColor { get; init; }
}

/// <summary>Converts <see cref="Color"/> values to and from hex strings (e.g., "#FF0000") during JSON serialization.</summary>
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
