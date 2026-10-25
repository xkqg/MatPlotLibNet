// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>Serializes and deserializes <see cref="Figure"/> instances to and from JSON.</summary>
/// <remarks>A number JSON cannot hold is written as the string that names it: NaN, the library's missing value, as
/// <c>"NaN"</c>, and the infinities as <c>"Infinity"</c> and <c>"-Infinity"</c>. Each is read back as that number, so a
/// figure with a gap in a line or an empty heatmap cell survives the round trip unchanged. A reader outside .NET sees
/// those three strings where a number would stand.</remarks>
public sealed class ChartSerializer : IChartSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = CreateOptions(true);
    private static readonly JsonSerializerOptions CompactOptions = CreateOptions(false);

    /// <summary>The options this serializer reads a document with. Another reader of the same documents (the MCP spec
    /// reader) starts from a copy, so the naming, the converters and the number rule are one definition.</summary>
    internal static JsonSerializerOptions ReadOptions => CompactOptions;

    private static JsonSerializerOptions CreateOptions(bool indented) => new()
    {
        WriteIndented = indented,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // The one place the non-finite numbers are decided (see the class remarks): written as "NaN", "Infinity" or
        // "-Infinity" and read back as themselves. Without it the writer throws on the first missing value. A finite
        // number is written exactly as before, so a figure without one keeps its bytes.
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters =
        {
            new ColorJsonConverter(),
            // Defensive guard: any enum-typed DTO property added
            // in the future serialises as a NAME (string), never an integer ordinal.
            // Protects against silent binary-compat breakage when enums reorder across
            // library versions. Existing DTOs already manually string-convert so this
            // is additive-only (no byte change for today's output).
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        },
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
        var dto = JsonSerializer.Deserialize<FigureDto>(json, ReadOptions)
            ?? throw new JsonException("Failed to deserialize figure.");
        return DtoToFigure(dto);
    }

    /// <summary>The settings of a new figure: what a figure setting is left out at, and read back as.</summary>
    private static readonly Figure RestingFigure = new();

    /// <summary>Writes the figure: its own settings (each left out at a new figure's value; the theme left out while it
    /// is the default theme itself), then its subplots.</summary>
    private static FigureDto FigureToDto(Figure figure) => new()
    {
        Title = figure.Title,
        AltText = figure.AltText,
        Description = figure.Description,
        Width = figure.Width,
        Height = figure.Height,
        Dpi = figure.Dpi,
        BackgroundColor = figure.BackgroundColor,
        GridSpec = figure.GridSpec is { } gs ? new GridSpecDto
        {
            Rows = gs.Rows, Cols = gs.Cols,
            HeightRatios = gs.HeightRatios, WidthRatios = gs.WidthRatios
        } : null,
        SubPlots = AxesToDtos(figure.SubPlots, figure.Theme),
        Enable3DRotation = figure.Enable3DRotation.OrNullAtRest(RestingFigure.Enable3DRotation),
        FigureColorBar = ColorBarDto.From(figure.FigureColorBar),
        Theme = ReferenceEquals(figure.Theme, RestingFigure.Theme) ? null : ThemeDto.From(figure.Theme),
        Spacing = figure.Spacing == RestingFigure.Spacing ? null : SubPlotSpacingDto.From(figure.Spacing),
        ChartId = figure.ChartId,
        ServerInteraction = figure.ServerInteraction.OrNullAtRest(RestingFigure.ServerInteraction),
        EnableZoomPan = figure.EnableZoomPan.OrNullAtRest(RestingFigure.EnableZoomPan),
        EnableLegendToggle = figure.EnableLegendToggle.OrNullAtRest(RestingFigure.EnableLegendToggle),
        EnableRichTooltips = figure.EnableRichTooltips.OrNullAtRest(RestingFigure.EnableRichTooltips),
        EnableHighlight = figure.EnableHighlight.OrNullAtRest(RestingFigure.EnableHighlight),
        EnableSelection = figure.EnableSelection.OrNullAtRest(RestingFigure.EnableSelection),
        EnableDataCursor = figure.EnableDataCursor.OrNullAtRest(RestingFigure.EnableDataCursor),
        EnableTreemapDrilldown = figure.EnableTreemapDrilldown.OrNullAtRest(RestingFigure.EnableTreemapDrilldown),
        EnableSankeyHover = figure.EnableSankeyHover.OrNullAtRest(RestingFigure.EnableSankeyHover),
        InteractionTheme = figure.InteractionTheme == RestingFigure.InteractionTheme ? null : InteractionThemeDto.From(figure.InteractionTheme),
        ResponsiveSvg = figure.ResponsiveSvg.OrNullAtRest(RestingFigure.ResponsiveSvg),
    };

    /// <summary>Writes one axes. <paramref name="theme"/> is the figure's: a grid the axes changed over the theme's grid
    /// is a change (code the JSON cannot carry), so it is written as the grid it draws under that theme, marked as the
    /// axes' own (<see cref="AxesDto.GridOverridesTheme"/>) so that a grid switched off stays off under a theme that
    /// draws one.</summary>
    internal static AxesDto AxesToDto(Axes axes, Theme theme) => new()
    {
        Title = axes.Title,
        Key = axes.Key,
        ShareXKey = axes.ShareXWith?.Key,
        ShareYKey = axes.ShareYWith?.Key,
        GridPosition = axes.GridPosition is { } gp ? new GridPositionDto
        {
            RowStart = gp.RowStart, RowEnd = gp.RowEnd,
            ColStart = gp.ColStart, ColEnd = gp.ColEnd
        } : null,
        Spines = axes.Spines == new SpinesConfig() ? null : SpinesToDto(axes.Spines),
        XAxis = AxisToDto(axes.XAxis),
        YAxis = AxisToDto(axes.YAxis),
        ZAxis = AxisToDto(axes.ZAxis),
        Grid = GridDto.From(axes.GridOverride is null ? axes.Grid : axes.GridUnder(theme)),
        GridOverridesTheme = axes.GridOverride is null ? null : true,
        BarMode = axes.BarMode == BarMode.Stacked ? "stacked" : null,
        Series = SeriesToDtos(axes.Series),
        Annotations = axes.Annotations.Count > 0 ? axes.Annotations.Select(AnnotationToDto).ToList() : null,
        ReferenceLines = axes.ReferenceLines.Count > 0 ? axes.ReferenceLines.Select(r => new ReferenceLineDto
        {
            Value = r.Value, Orientation = r.Orientation.ToString().ToLowerInvariant(),
            LineStyle = r.LineStyle.ToString().ToLowerInvariant(), LineWidth = r.LineWidth, Label = r.Label,
            Color = r.Color,
        }).ToList() : null,
        SecondaryYAxis = axes.SecondaryYAxis is not null ? AxisToDto(axes.SecondaryYAxis) : null,
        SecondarySeries = axes.SecondarySeries.Count > 0 ? SeriesToDtos(axes.SecondarySeries) : null,
        SecondaryXAxis = axes.SecondaryXAxis is not null ? AxisToDto(axes.SecondaryXAxis) : null,
        XSecondarySeries = axes.XSecondarySeries.Count > 0 ? SeriesToDtos(axes.XSecondarySeries) : null,
        Spans = axes.Spans.Count > 0 ? axes.Spans.Select(s => new SpanRegionDto
        {
            Min = s.Min, Max = s.Max, Orientation = s.Orientation.ToString().ToLowerInvariant(),
            Alpha = s.Alpha,
            LineStyle = s.LineStyle != LineStyle.None ? s.LineStyle.ToString().ToLowerInvariant() : null,
            LineWidth = s.LineWidth != 1.0 ? s.LineWidth : null,
            Label = s.Label,
            Color = s.Color,
            EdgeColor = s.EdgeColor,
        }).ToList() : null,
        Trendlines = axes.Trendlines.Count > 0 ? axes.Trendlines.Select(t => new TrendlineDto
        {
            X1 = t.X1, Y1 = t.Y1, X2 = t.X2, Y2 = t.Y2,
            Color = t.Color is { } tc ? tc.ToHex() : null,
            LineStyle = t.LineStyle != Styling.LineStyle.Solid ? t.LineStyle.ToString().ToLowerInvariant() : null,
            LineWidth = t.LineWidth, Label = t.Label, IsExtended = t.IsExtended,
        }).ToList() : null,
        HorizontalLevels = axes.HorizontalLevels.Count > 0 ? axes.HorizontalLevels.Select(l => new HorizontalLevelDto
        {
            Value = l.Value,
            Color = l.Color is { } lc ? lc.ToHex() : null,
            LineStyle = l.LineStyle != Styling.LineStyle.Dashed ? l.LineStyle.ToString().ToLowerInvariant() : null,
            LineWidth = l.LineWidth, Label = l.Label,
        }).ToList() : null,
        FibonacciRetracements = axes.FibonacciRetracements.Count > 0 ? axes.FibonacciRetracements.Select(f => new FibonacciRetracementDto
        {
            PriceHigh = f.PriceHigh, PriceLow = f.PriceLow,
            Color = f.Color is { } fc ? fc.ToHex() : null,
            LineWidth = f.LineWidth, ShowLabels = f.ShowLabels,
        }).ToList() : null,
        InsetBounds = axes.InsetBounds is { } ib ? new InsetBoundsDto
        {
            X = ib.X, Y = ib.Y, Width = ib.Width, Height = ib.Height
        } : null,
        Insets = axes.Insets.Count > 0 ? AxesToDtos(axes.Insets, theme) : null,
        Elevation = axes.Elevation != 30 ? axes.Elevation : null,
        Azimuth = axes.Azimuth != -60 ? axes.Azimuth : null,
        CameraDistance = axes.CameraDistance,
        LightSourceType = axes.LightSource is Rendering.Lighting.DirectionalLight dl
            ? FormattableString.Invariant($"directional:{dl.Dx},{dl.Dy},{dl.Dz},{dl.Ambient},{dl.Diffuse}")
            : null,
        XBreaks = axes.XBreaks.Count > 0 ? axes.XBreaks.Select(b => new AxisBreakDto(b.From, b.To, b.Style.ToString().ToLowerInvariant())).ToList() : null,
        YBreaks = axes.YBreaks.Count > 0 ? axes.YBreaks.Select(b => new AxisBreakDto(b.From, b.To, b.Style.ToString().ToLowerInvariant())).ToList() : null,
        TitleStyle = TextStyleDto.From(axes.TitleStyle),
        TitleLoc = axes.TitleLoc.OrNullAtRest(TitleLocation.Center),
        Legend = LegendDto.From(axes.Legend),
        ColorBar = ColorBarDto.From(axes.ColorBar),
        Pane3D = axes.Pane3D == new Pane3DConfig() ? null : new Pane3DConfigDto
        {
            FloorColor = axes.Pane3D.FloorColor,
            LeftWallColor = axes.Pane3D.LeftWallColor,
            RightWallColor = axes.Pane3D.RightWallColor,
            Alpha = axes.Pane3D.Alpha,
            Visible = axes.Pane3D.Visible,
        },
        EnableTooltips = axes.EnableTooltips.OrNullAtRest(false),
        EnableInteractiveAttributes = axes.EnableInteractiveAttributes.OrNullAtRest(false),
        Emit3DVertexData = axes.Emit3DVertexData.OrNullAtRest(false),
        CoordinateSystem = axes.CoordinateSystem == Models.CoordinateSystem.Cartesian ? null : axes.CoordinateSystem.ToString().ToLowerInvariant(),
    };

    /// <summary>Writes one annotation: what it says and where, and everything it is drawn with. A value at the one a new
    /// annotation has is left out.</summary>
    private static AnnotationDto AnnotationToDto(Annotation a)
    {
        var rest = new Annotation(a.Text, a.X, a.Y);
        return new AnnotationDto
        {
            Text = a.Text, X = a.X, Y = a.Y,
            ArrowTargetX = a.ArrowTargetX, ArrowTargetY = a.ArrowTargetY,
            ConnectionStyle = a.ConnectionStyle != rest.ConnectionStyle ? a.ConnectionStyle.ToString().ToLowerInvariant() : null,
            ConnectionRad = a.ConnectionRad.OrNullAtRest(rest.ConnectionRad),
            ArrowHeadSize = a.ArrowHeadSize.OrNullAtRest(rest.ArrowHeadSize),
            BoxStyle = a.BoxStyle != rest.BoxStyle ? a.BoxStyle.ToString().ToLowerInvariant() : null,
            BoxPadding = a.BoxPadding.OrNullAtRest(rest.BoxPadding),
            BoxCornerRadius = a.BoxCornerRadius.OrNullAtRest(rest.BoxCornerRadius),
            Coordinates = a.Coordinates.OrNullAtRest(rest.Coordinates),
            Font = FontDto.From(a.Font),
            Color = a.Color,
            ArrowColor = a.ArrowColor,
            Alignment = a.Alignment.OrNullAtRest(rest.Alignment),
            Rotation = a.Rotation.OrNullAtRest(rest.Rotation),
            ArrowStyle = a.ArrowStyle.OrNullAtRest(rest.ArrowStyle),
            BackgroundColor = a.BackgroundColor,
            BoxFaceColor = a.BoxFaceColor,
            BoxEdgeColor = a.BoxEdgeColor,
            BoxLineWidth = a.BoxLineWidth.OrNullAtRest(rest.BoxLineWidth),
        };
    }

    /// <summary>The ONE place a list of axes is written: the figure's subplots and an axes' insets, each through
    /// <see cref="AxesToDto"/>.</summary>
    private static List<AxesDto> AxesToDtos(IEnumerable<Axes> axes, Theme theme) => axes.Select(a => AxesToDto(a, theme)).ToList();

    /// <summary>The ONE place a list of series is written: the primary list and both secondary axes, each series through
    /// <see cref="SeriesToDto"/>.</summary>
    private static List<SeriesDto> SeriesToDtos(IEnumerable<ISeries> series) => series.Select(SeriesToDto).ToList();

    /// <summary>The ticks, the minor ticks and the linear threshold of a new axis: the values an axis setting is left out
    /// at, and read back over.</summary>
    private static readonly TickConfig RestingMajorTicks = new Axis().MajorTicks;
    private static readonly TickConfig RestingMinorTicks = new Axis().MinorTicks;
    private static readonly double RestingSymLogLinThresh = new Axis().SymLogLinThresh;

    /// <summary>Writes one axis. A tick locator or formatter of the library describes itself; one written outside the
    /// library is left out (see <see cref="Axis.TickLocator"/>). The colours of the major ticks and of the label stay in
    /// the fields that carried them before the rest of the ticks and the label style reached the JSON.</summary>
    private static AxisDto AxisToDto(Axis axis) => new()
    {
        Label = axis.Label,
        Min = axis.Min,
        Max = axis.Max,
        Scale = axis.Scale.ToString().ToLowerInvariant(),
        TickColor = axis.MajorTicks.Color,
        TickLabelColor = axis.MajorTicks.LabelColor,
        LabelColor = axis.LabelStyle?.Color,
        TickLocator = (axis.TickLocator as IJsonDescribed<TickLocatorDto>)?.Describe(),
        TickFormatter = (axis.TickFormatter as IJsonDescribed<TickFormatterDto>)?.Describe(),
        Inverted = axis.Inverted.OrNullAtRest(false),
        Margin = axis.Margin,
        SymLogLinThresh = axis.SymLogLinThresh.OrNullAtRest(RestingSymLogLinThresh),
        MajorTicks = TickConfigDto.From(axis.MajorTicks with { Color = null, LabelColor = null }, RestingMajorTicks),
        MinorTicks = TickConfigDto.From(axis.MinorTicks, RestingMinorTicks),
        LabelStyle = TextStyleDto.From(axis.LabelStyle is { } style ? style with { Color = null } : null),
    };

    private static SpinesConfigDto SpinesToDto(SpinesConfig s) => new()
    {
        Top = SpineToDto(s.Top),
        Bottom = SpineToDto(s.Bottom),
        Left = SpineToDto(s.Left),
        Right = SpineToDto(s.Right)
    };

    private static SpineConfigDto SpineToDto(SpineConfig s) => new()
    {
        Visible = s.Visible,
        Position = s.Position == SpinePosition.Edge ? null : s.Position.ToString().ToLowerInvariant(),
        PositionValue = s.PositionValue,
        LineWidth = s.LineWidth,
        Color = s.Color,
        LineStyle = s.LineStyle.OrNullAtRest(LineStyle.Solid),
    };

    private static SpinesConfig DtoToSpines(SpinesConfigDto dto) => new()
    {
        Top = DtoToSpine(dto.Top),
        Bottom = DtoToSpine(dto.Bottom),
        Left = DtoToSpine(dto.Left),
        Right = DtoToSpine(dto.Right)
    };

    private static SpineConfig DtoToSpine(SpineConfigDto? dto) => dto is null ? new() : new()
    {
        Visible = dto.Visible,
        Position = dto.Position switch
        {
            "data" => SpinePosition.Data,
            "axes" => SpinePosition.Axes,
            _ => SpinePosition.Edge
        },
        PositionValue = dto.PositionValue,
        LineWidth = dto.LineWidth,
        Color = dto.Color,
        LineStyle = dto.LineStyle ?? LineStyle.Solid,
    };

    /// <summary>The ONE place the fields every series shares leave the model: its label and where it is shown when
    /// that is not the legend, its place in the drawing order, whether it is shown at all, and how many points an x/y
    /// series draws at most. Every series list (primary, both secondary axes, insets) is written through here; what a
    /// series type draws beyond these, its own <c>ToSeriesDto</c> writes.</summary>
    private static SeriesDto SeriesToDto(ISeries series)
    {
        var dto = series.ToSeriesDto();
        dto.Label = series.Label;
        if (series is IHasLabelPlacement { LabelPlacement: not SeriesLabelPlacement.Legend } carrier)
            dto.LabelPlacement = carrier.LabelPlacement.ToString().ToLowerInvariant();
        dto.ZOrder = series.ZOrder.OrNullAtRest(series is ChartSeries chart ? chart.RestingZOrder : 0);
        dto.Visible = series.Visible.OrNullAtRest(true);
        if (series is XYSeries xy)
            dto.MaxDisplayPoints = xy.MaxDisplayPoints;
        if (series is ChartSeries { ColorGroup: { } colorGroup })
            dto.ColorGroup = colorGroup;
        return dto;
    }

    /// <summary>The ONE place the fields every series shares enter the model: the counterpart of
    /// <see cref="SeriesToDto"/>, shared by the primary list and both secondary axes. A field that is absent, or an
    /// unknown placement, leaves the value the series was created with.</summary>
    private static void RestoreSharedFields(ISeries series, SeriesDto dto)
    {
        series.Label = dto.Label;
        if (series is IHasLabelPlacement carrier)
            dto.LabelPlacement.ApplyEnum<SeriesLabelPlacement>(v => carrier.LabelPlacement = v);
        if (dto.ZOrder is { } zOrder)
            series.ZOrder = zOrder;
        if (dto.Visible is { } visible)
            series.Visible = visible;
        if (series is XYSeries xy && dto.MaxDisplayPoints is { } maxDisplayPoints)
            xy.MaxDisplayPoints = maxDisplayPoints;
        if (series is ChartSeries chartSeries && dto.ColorGroup is { } colorGroup)
            chartSeries.ColorGroup = colorGroup;
    }

    /// <summary>Reads the figure back: the counterpart of <see cref="FigureToDto"/>. A setting the JSON leaves out reads
    /// as a new figure's.</summary>
    private static Figure DtoToFigure(FigureDto dto)
    {
        var figure = new Figure
        {
            Title = dto.Title,
            AltText = dto.AltText,
            Description = dto.Description,
            Width = dto.Width,
            Height = dto.Height,
            Dpi = dto.Dpi ?? RestingFigure.Dpi,
            BackgroundColor = dto.BackgroundColor,
            Enable3DRotation = dto.Enable3DRotation ?? RestingFigure.Enable3DRotation,
            FigureColorBar = dto.FigureColorBar?.ToColorBar(),
            Theme = dto.Theme?.ToTheme() ?? RestingFigure.Theme,
            Spacing = dto.Spacing?.ToSpacing() ?? RestingFigure.Spacing,
            ChartId = dto.ChartId,
            ServerInteraction = dto.ServerInteraction ?? RestingFigure.ServerInteraction,
            EnableZoomPan = dto.EnableZoomPan ?? RestingFigure.EnableZoomPan,
            EnableLegendToggle = dto.EnableLegendToggle ?? RestingFigure.EnableLegendToggle,
            EnableRichTooltips = dto.EnableRichTooltips ?? RestingFigure.EnableRichTooltips,
            EnableHighlight = dto.EnableHighlight ?? RestingFigure.EnableHighlight,
            EnableSelection = dto.EnableSelection ?? RestingFigure.EnableSelection,
            EnableDataCursor = dto.EnableDataCursor ?? RestingFigure.EnableDataCursor,
            EnableTreemapDrilldown = dto.EnableTreemapDrilldown ?? RestingFigure.EnableTreemapDrilldown,
            EnableSankeyHover = dto.EnableSankeyHover ?? RestingFigure.EnableSankeyHover,
            InteractionTheme = dto.InteractionTheme?.ToInteractionTheme() ?? RestingFigure.InteractionTheme,
            ResponsiveSvg = dto.ResponsiveSvg ?? RestingFigure.ResponsiveSvg,
        };

        if (dto.GridSpec is { } gsDto)
            figure.GridSpec = new GridSpec
            {
                Rows = gsDto.Rows, Cols = gsDto.Cols,
                HeightRatios = gsDto.HeightRatios, WidthRatios = gsDto.WidthRatios
            };

        foreach (var axDto in dto.SubPlots ?? [])
        {
            Axes axes;
            if (axDto.GridPosition is { } gpDto && figure.GridSpec is not null)
            {
                var pos = new GridPosition(gpDto.RowStart, gpDto.RowEnd, gpDto.ColStart, gpDto.ColEnd);
                axes = figure.AddSubPlot(figure.GridSpec, pos);
            }
            else
            {
                axes = figure.AddSubPlot();
            }
            RestoreAxes(axes, axDto);
        }

        // Resolve sharing references by key
        var keyedAxes = new Dictionary<string, Axes>();
        for (int i = 0; i < figure.SubPlots.Count; i++)
        {
            if (figure.SubPlots[i].Key is { } key)
                keyedAxes[key] = figure.SubPlots[i];
        }

        for (int i = 0; i < figure.SubPlots.Count && i < (dto.SubPlots?.Count ?? 0); i++)
        {
            var axDto = dto.SubPlots![i];
            if (axDto.ShareXKey is not null && keyedAxes.TryGetValue(axDto.ShareXKey, out var xTarget))
                figure.SubPlots[i].ShareXWith = xTarget;
            if (axDto.ShareYKey is not null && keyedAxes.TryGetValue(axDto.ShareYKey, out var yTarget))
                figure.SubPlots[i].ShareYWith = yTarget;
        }

        return figure;
    }

    /// <summary>The ONE place an axes is read back: a subplot and an inset alike, everything <see cref="AxesToDto"/> wrote
    /// (its axes, grid, spines, title style, legend, colour bar, series, overlays, secondary axes, insets and breaks).</summary>
    internal static void RestoreAxes(Axes axes, AxesDto axDto)
    {
        axes.Title = axDto.Title;
        axes.Key = axDto.Key;
        if (axDto.TitleStyle is { } titleStyle) axes.TitleStyle = titleStyle.ToTextStyle();
        if (axDto.TitleLoc is { } titleLoc) axes.TitleLoc = titleLoc;
        if (axDto.Legend is { } legend) axes.Legend = legend.ToLegend();
        if (axDto.ColorBar is { } colorBar) axes.ColorBar = colorBar.ToColorBar();

        if (axDto.XAxis is not null) ApplyAxis(axes.XAxis, axDto.XAxis);
        if (axDto.YAxis is not null) ApplyAxis(axes.YAxis, axDto.YAxis);
        if (axDto.ZAxis is not null) ApplyAxis(axes.ZAxis, axDto.ZAxis);
        RestorePane3D(axes, axDto.Pane3D);
        if (axDto.EnableTooltips is { } enableTooltips) axes.EnableTooltips = enableTooltips;
        if (axDto.EnableInteractiveAttributes is { } enableInteractiveAttributes) axes.EnableInteractiveAttributes = enableInteractiveAttributes;
        if (axDto.Emit3DVertexData is { } emit3DVertexData) axes.Emit3DVertexData = emit3DVertexData;
        axDto.CoordinateSystem.ApplyEnum<Models.CoordinateSystem>(v => axes.CoordinateSystem = v);
        if (axDto.Grid is not null) RestoreGrid(axes, axDto.Grid.ToGridStyle(), axDto.GridOverridesTheme is true);
        if (axDto.Spines is not null) axes.Spines = DtoToSpines(axDto.Spines);
        if (axDto.BarMode is "stacked") axes.BarMode = BarMode.Stacked;
        RestoreCameraAndLighting(axes, axDto);

        foreach (var sDto in axDto.Series ?? [])
            AddSeriesFromDto(axes, sDto);

        RestoreAnnotations(axes, axDto.Annotations);
        RestoreReferenceLines(axes, axDto.ReferenceLines);
        RestoreSecondaryAxes(axes, axDto);
        RestoreSpans(axes, axDto.Spans);
        RestoreTrendlines(axes, axDto.Trendlines);
        RestoreHorizontalLevels(axes, axDto.HorizontalLevels);
        RestoreFibonacciRetracements(axes, axDto.FibonacciRetracements);
        RestoreInsets(axes, axDto.Insets);
        RestoreAxisBreaks(axes, axDto);
    }

    /// <summary>The 3-D axes' pane colours and visibility; absent when the axes never changed them.</summary>
    private static void RestorePane3D(Axes axes, Pane3DConfigDto? dto)
    {
        if (dto is null) return;
        axes.Pane3D = new Pane3DConfig
        {
            FloorColor = dto.FloorColor,
            LeftWallColor = dto.LeftWallColor,
            RightWallColor = dto.RightWallColor,
            Alpha = dto.Alpha,
            Visible = dto.Visible,
        };
    }

    /// <summary>The 3-D camera (elevation/azimuth/distance) and its light source. Only the <c>"directional:"</c>
    /// wire format is read back; a light source written any other way is left at the axes' own default.</summary>
    private static void RestoreCameraAndLighting(Axes axes, AxesDto axDto)
    {
        if (axDto.Elevation.HasValue) axes.Elevation = axDto.Elevation.Value;
        if (axDto.Azimuth.HasValue) axes.Azimuth = axDto.Azimuth.Value;
        if (axDto.CameraDistance.HasValue) axes.CameraDistance = axDto.CameraDistance.Value;
        if (axDto.LightSourceType is not { } lsType || !lsType.StartsWith("directional:", StringComparison.Ordinal)) return;

        var parts = lsType[12..].Split(',');
        if (parts.Length >= 5 &&
            double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double ldx) &&
            double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double ldy) &&
            double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double ldz) &&
            double.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double amb) &&
            double.TryParse(parts[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double diff))
            axes.LightSource = new Rendering.Lighting.DirectionalLight(ldx, ldy, ldz, amb, diff);
    }

    private static void RestoreAnnotations(Axes axes, List<AnnotationDto>? dtos)
    {
        foreach (var aDto in dtos ?? [])
        {
            var ann = axes.Annotate(aDto.Text ?? "", aDto.X, aDto.Y);
            ann.ArrowTargetX = aDto.ArrowTargetX;
            ann.ArrowTargetY = aDto.ArrowTargetY;
            aDto.ConnectionStyle.ApplyEnum<Models.ConnectionStyle>(v => ann.ConnectionStyle = v);
            if (aDto.ConnectionRad.HasValue) ann.ConnectionRad = aDto.ConnectionRad.Value;
            if (aDto.ArrowHeadSize.HasValue) ann.ArrowHeadSize = aDto.ArrowHeadSize.Value;
            aDto.BoxStyle.ApplyEnum<Models.BoxStyle>(v => ann.BoxStyle = v);
            if (aDto.BoxPadding.HasValue) ann.BoxPadding = aDto.BoxPadding.Value;
            if (aDto.BoxCornerRadius.HasValue) ann.BoxCornerRadius = aDto.BoxCornerRadius.Value;
            if (aDto.Coordinates is { } coordinates) ann.Coordinates = coordinates;
            if (aDto.Font is { } font) ann.Font = font.ToFont();
            ann.Color = aDto.Color;
            ann.ArrowColor = aDto.ArrowColor;
            if (aDto.Alignment is { } alignment) ann.Alignment = alignment;
            if (aDto.Rotation is { } rotation) ann.Rotation = rotation;
            if (aDto.ArrowStyle is { } arrowStyle) ann.ArrowStyle = arrowStyle;
            ann.BackgroundColor = aDto.BackgroundColor;
            ann.BoxFaceColor = aDto.BoxFaceColor;
            ann.BoxEdgeColor = aDto.BoxEdgeColor;
            if (aDto.BoxLineWidth is { } boxLineWidth) ann.BoxLineWidth = boxLineWidth;
        }
    }

    private static void RestoreReferenceLines(Axes axes, List<ReferenceLineDto>? dtos)
    {
        foreach (var rDto in dtos ?? [])
        {
            var orient = rDto.Orientation is "vertical" ? Orientation.Vertical : Orientation.Horizontal;
            var rl = orient == Orientation.Horizontal ? axes.AxHLine(rDto.Value) : axes.AxVLine(rDto.Value);
            rDto.LineStyle.ApplyEnum<LineStyle>(v => rl.LineStyle = v);
            rl.LineWidth = rDto.LineWidth;
            rl.Label = rDto.Label;
            rl.Color = rDto.Color;
        }
    }

    private static void RestoreSecondaryAxes(Axes axes, AxesDto axDto)
    {
        if (axDto.SecondaryYAxis is not null)
        {
            axes.TwinX();
            ApplyAxis(axes.SecondaryYAxis!, axDto.SecondaryYAxis);
            // Route secondary series through PlotSecondary/ScatterSecondary
            AddSecondarySeriesFromDtos(axDto.SecondarySeries,
                sDto => axes.CreateSecondaryLine(sDto), sDto => axes.CreateSecondaryScatter(sDto));
        }

        if (axDto.SecondaryXAxis is not null)
        {
            axes.TwinY();
            ApplyAxis(axes.SecondaryXAxis!, axDto.SecondaryXAxis);
            // Route top-axis series through PlotXSecondary/ScatterXSecondary
            AddSecondarySeriesFromDtos(axDto.XSecondarySeries,
                sDto => axes.CreateXSecondaryLine(sDto), sDto => axes.CreateXSecondaryScatter(sDto));
        }
    }

    private static void RestoreSpans(Axes axes, List<SpanRegionDto>? dtos)
    {
        foreach (var sDto in dtos ?? [])
        {
            var orient = sDto.Orientation is "vertical" ? Orientation.Vertical : Orientation.Horizontal;
            var sp = orient == Orientation.Horizontal ? axes.AxHSpan(sDto.Min, sDto.Max) : axes.AxVSpan(sDto.Min, sDto.Max);
            sp.Alpha = sDto.Alpha;
            sDto.LineStyle.ApplyEnum<LineStyle>(v => sp.LineStyle = v);
            if (sDto.LineWidth.HasValue) sp.LineWidth = sDto.LineWidth.Value;
            sp.Label = sDto.Label;
            sp.Color = sDto.Color;
            sp.EdgeColor = sDto.EdgeColor;
        }
    }

    private static void RestoreTrendlines(Axes axes, List<TrendlineDto>? dtos)
    {
        foreach (var tDto in dtos ?? [])
        {
            var t = axes.AddTrendline(tDto.X1, tDto.Y1, tDto.X2, tDto.Y2);
            if (tDto.Color is not null) t.Color = Styling.Color.FromHex(tDto.Color);
            tDto.LineStyle.ApplyEnum<LineStyle>(v => t.LineStyle = v);
            t.LineWidth = tDto.LineWidth;
            t.Label = tDto.Label;
            t.IsExtended = tDto.IsExtended;
        }
    }

    private static void RestoreHorizontalLevels(Axes axes, List<HorizontalLevelDto>? dtos)
    {
        foreach (var lDto in dtos ?? [])
        {
            var l = axes.AddLevel(lDto.Value);
            if (lDto.Color is not null) l.Color = Styling.Color.FromHex(lDto.Color);
            lDto.LineStyle.ApplyEnum<LineStyle>(v => l.LineStyle = v);
            l.LineWidth = lDto.LineWidth;
            l.Label = lDto.Label;
        }
    }

    private static void RestoreFibonacciRetracements(Axes axes, List<FibonacciRetracementDto>? dtos)
    {
        foreach (var fDto in dtos ?? [])
        {
            var f = axes.AddFibonacci(fDto.PriceHigh, fDto.PriceLow);
            if (fDto.Color is not null) f.Color = Styling.Color.FromHex(fDto.Color);
            f.LineWidth = fDto.LineWidth;
            f.ShowLabels = fDto.ShowLabels;
        }
    }

    /// <summary>An inset is an axes: read back through <see cref="RestoreAxes"/> itself, so it keeps whatever its
    /// writer wrote.</summary>
    private static void RestoreInsets(Axes axes, List<AxesDto>? dtos)
    {
        foreach (var insetDto in dtos ?? [])
        {
            if (insetDto.InsetBounds is not { } ibDto) continue;
            RestoreAxes(axes.AddInset(ibDto.X, ibDto.Y, ibDto.Width, ibDto.Height), insetDto);
        }
    }

    private static void RestoreAxisBreaks(Axes axes, AxesDto axDto)
    {
        foreach (var bDto in axDto.XBreaks ?? [])
        {
            var style = bDto.Style is not null && Enum.TryParse<Models.BreakStyle>(bDto.Style, true, out var bs) ? bs : Models.BreakStyle.Zigzag;
            axes.AddXBreak(bDto.From, bDto.To, style);
        }
        foreach (var bDto in axDto.YBreaks ?? [])
        {
            var style = bDto.Style is not null && Enum.TryParse<Models.BreakStyle>(bDto.Style, true, out var bs) ? bs : Models.BreakStyle.Zigzag;
            axes.AddYBreak(bDto.From, bDto.To, style);
        }
    }

    /// <summary>Reads an axes' grid back. A grid that overrides the theme's is the grid the axes draws whatever the theme's
    /// grid is (a switched-off one included), so it comes back as that change; any other grid comes back as it stands,
    /// and the renderer takes the theme's grid over one that is not switched on (<see cref="Axes.GridUnder"/>).</summary>
    private static void RestoreGrid(Axes axes, GridStyle grid, bool overridesTheme)
    {
        axes.Grid = grid;
        if (overridesTheme) axes.GridOverride = _ => grid;
    }

    /// <summary>Reads one axis back: the counterpart of <see cref="AxisToDto"/>. The tick locator is made first, so that a
    /// formatter that writes what its locator chose is paired with the axis's own locator again.</summary>
    private static void ApplyAxis(Axis axis, AxisDto dto)
    {
        axis.Label = dto.Label;
        axis.Min = dto.Min;
        axis.Max = dto.Max;
        if (dto.Scale is not null && Enum.TryParse<AxisScale>(dto.Scale, true, out var scale))
            axis.Scale = scale;
        if (dto.Inverted is { } inverted) axis.Inverted = inverted;
        if (dto.Margin is { } margin) axis.Margin = margin;
        if (dto.SymLogLinThresh is { } linThresh) axis.SymLogLinThresh = linThresh;
        if (dto.TickLocator is { } locator) axis.TickLocator = locator.Restore();
        if (dto.TickFormatter is { } formatter) axis.TickFormatter = formatter.Restore(axis.TickLocator);
        if (dto.MajorTicks is { } majorTicks) axis.MajorTicks = majorTicks.ToTickConfig(RestingMajorTicks);
        if (dto.MinorTicks is { } minorTicks) axis.MinorTicks = minorTicks.ToTickConfig(RestingMinorTicks);
        if (dto.LabelStyle is { } labelStyle) axis.LabelStyle = labelStyle.ToTextStyle();
        if (dto.TickColor is { } tickColor) axis.MajorTicks = axis.MajorTicks with { Color = tickColor };
        if (dto.TickLabelColor is { } tickLabelColor) axis.MajorTicks = axis.MajorTicks with { LabelColor = tickLabelColor };
        if (dto.LabelColor is { } labelColor) axis.LabelStyle = (axis.LabelStyle ?? new TextStyle()) with { Color = labelColor };
    }

    /// <summary>Restores the series of one secondary axis: a line or a scatter is rebuilt by the axis' own factory
    /// and keeps its label; any other kind is skipped, as it always was on the right-hand axis.</summary>
    private static void AddSecondarySeriesFromDtos(IEnumerable<SeriesDto>? dtos,
        Func<SeriesDto, LineSeries> createLine, Func<SeriesDto, ScatterSeries> createScatter)
    {
        foreach (var sDto in dtos ?? [])
        {
            ISeries? sec = sDto.Type switch
            {
                "line" => createLine(sDto),
                "scatter" => createScatter(sDto),
                _ => null
            };
            if (sec is not null) RestoreSharedFields(sec, sDto);
        }
    }

    private static void AddSeriesFromDto(Axes axes, SeriesDto dto)
    {
        var type = dto.Type ?? "unknown";
        var series = SeriesRegistry.Create(type, axes, dto);
        if (series is not null)
        {
            RestoreSharedFields(series, dto);
        }
        else
        {
            // Wire compat: an unrecognised discriminator (old JSON referencing a kind this build
            // doesn't know, or a hand-crafted/corrupt payload) is still dropped leniently — the
            // figure keeps loading — but the drop is no longer silent.
            ChartDiagnostics.Emit(new ChartDiagnostic(
                nameof(ChartSerializer),
                $"Unknown series discriminator '{type}' was skipped during deserialization.",
                null));
        }
    }

}

/// <summary>A wire-format enum string, applied to whatever setter wants it.</summary>
internal static class EnumParsingExtensions
{
    extension(string? value)
    {
        /// <summary>Parses this string to an enum value and applies it via <paramref name="setter"/> on success.
        /// Case-insensitive; no-ops when this string is <see langword="null"/> or not a valid member of
        /// <typeparamref name="T"/>.</summary>
        internal void ApplyEnum<T>(Action<T> setter) where T : struct, Enum
        {
            if (value is not null && Enum.TryParse<T>(value, true, out var parsed))
            {
                setter(parsed);
            }
        }
    }
}

/// <summary>Reconstructs an axes, or a secondary-axis series, from its DTO — every entry point past
/// <see cref="ChartSerializer.RestoreAxes"/> itself that needs the <see cref="Axes"/> it is restoring onto.</summary>
internal static class AxesSerializationExtensions
{
    extension(Axes target)
    {
        /// <summary>Copies everything an axes draws from <paramref name="source"/> onto this axes — series,
        /// title, both axes, grid, spines, legend, and every overlay (annotations, reference lines, spans,
        /// trendlines, levels, Fibonacci retracements, secondary axes). The same write/read pair the JSON
        /// round-trip uses, so a property this contract carries for a figure is carried here too, with nothing
        /// to hand-copy a second time: <see cref="AxesBuilder.AddInset(InsetBounds, Action{AxesBuilder})"/>
        /// composes an inset's real axes from the configured builder's this way, instead of naming one property
        /// at a time.</summary>
        internal void CopyAxesState(Axes source, Theme theme) => ChartSerializer.RestoreAxes(target, ChartSerializer.AxesToDto(source, theme));

        /// <summary>Reconstructs a <see cref="LineSeries"/> on the secondary Y-axis from the DTO.</summary>
        internal LineSeries CreateSecondaryLine(SeriesDto dto)
        {
            var s = target.PlotSecondary(dto.XData ?? [], dto.YData ?? []);
            s.RestoreFrom(dto);
            return s;
        }

        /// <summary>Reconstructs a <see cref="ScatterSeries"/> on the secondary Y-axis from the DTO.</summary>
        internal ScatterSeries CreateSecondaryScatter(SeriesDto dto)
        {
            var s = target.ScatterSecondary(dto.XData ?? [], dto.YData ?? []);
            s.RestoreFrom(dto);
            return s;
        }

        /// <summary>Reconstructs a <see cref="LineSeries"/> on the secondary X-axis (top edge) from the DTO.</summary>
        internal LineSeries CreateXSecondaryLine(SeriesDto dto)
        {
            var s = target.PlotXSecondary(dto.XData ?? [], dto.YData ?? []);
            s.RestoreFrom(dto);
            return s;
        }

        /// <summary>Reconstructs a <see cref="ScatterSeries"/> on the secondary X-axis (top edge) from the DTO.</summary>
        internal ScatterSeries CreateXSecondaryScatter(SeriesDto dto)
        {
            var s = target.ScatterXSecondary(dto.XData ?? [], dto.YData ?? []);
            s.RestoreFrom(dto);
            return s;
        }
    }
}

/// <summary>Rectangular/jagged 2D array to and from a JSON list-of-lists, both directions. A <c>double[,]</c> or
/// <c>double[][]</c> (<see cref="Models.Series.PairGridSeries"/>'s per-variable samples) is not itself
/// JSON-serializable; every grid-shaped series (Heatmap, Contour(f), Image, Pcolormesh, Clustermap, Streamplot,
/// 3-D surface/wireframe, PairGrid) converts through here on the way in and out.</summary>
internal static class Grid2DSerializationExtensions
{
    extension(double[,] data)
    {
        /// <summary>Converts this rectangular 2D array to a jagged list-of-lists for JSON serialization.</summary>
        internal List<List<double>>? To2DList()
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
    }

    extension(List<List<double>>? data)
    {
        /// <summary>Converts this JSON jagged list-of-lists back to a rectangular 2D array; an empty array when
        /// <see langword="null"/> or empty.</summary>
        internal double[,] From2DList()
        {
            if (data is null || data.Count == 0)
            {
                return new double[0, 0];
            }
            int rows = data.Count, cols = data[0].Count;
            var result = new double[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    result[r, c] = data[r][c];
                }
            }
            return result;
        }

        /// <summary>Converts this JSON list-of-lists back to a jagged <c>double[][]</c>; an empty array when
        /// <see langword="null"/> or empty.</summary>
        internal double[][] FromJaggedList()
        {
            if (data is null || data.Count == 0)
            {
                return [];
            }
            var result = new double[data.Count][];
            for (int i = 0; i < data.Count; i++)
            {
                result[i] = [.. data[i]];
            }
            return result;
        }
    }

    extension(double[][] data)
    {
        /// <summary>Converts this jagged <c>double[][]</c> (variable-major) to a list-of-lists for JSON
        /// serialization. Used by <see cref="Models.Series.PairGridSeries"/> where each sub-array is one
        /// variable's samples. Returns an empty list for empty input — never <see langword="null"/>.</summary>
        internal List<List<double>> JaggedTo2DList()
        {
            var result = new List<List<double>>(data.Length);
            for (int i = 0; i < data.Length; i++) result.Add([.. data[i]]);
            return result;
        }
    }
}

/// <summary>A tree becomes wire data and back here, recursively — the one place shared by every
/// <see cref="Models.Series.HierarchicalSeries"/> (Treemap, Sunburst, Dendrogram) and
/// <see cref="Models.Series.ClustermapSeries"/>'s RowTree/ColumnTree.</summary>
internal static class TreeNodeSerializationExtensions
{
    extension(Models.TreeNode node)
    {
        /// <summary>Converts this tree, recursively, to its DTO.</summary>
        internal TreeNodeDto TreeNodeToDto() => new()
        {
            Label = node.Label,
            Value = node.Value,
            Color = node.Color,
            ColorValue = node.ColorValue,
            Headline = node.Headline,
            Url = node.Url,
            Expanded = node.Expanded,
            Hatch = node.Hatch,
            HatchColor = node.HatchColor,
            Children = node.Children.Count > 0 ? [.. node.Children.Select(child => child.TreeNodeToDto())] : null,
        };
    }

    extension(TreeNodeDto dto)
    {
        /// <summary>Converts this DTO, recursively, back to a <see cref="Models.TreeNode"/> — the counterpart of
        /// <see cref="TreeNodeToDto"/>.</summary>
        internal Models.TreeNode DtoToTreeNode() => new()
        {
            Label = dto.Label,
            Value = dto.Value,
            Color = dto.Color,
            ColorValue = dto.ColorValue,
            Headline = dto.Headline,
            Url = dto.Url,
            Expanded = dto.Expanded,
            Hatch = dto.Hatch,
            HatchColor = dto.HatchColor,
            Children = dto.Children is { } children ? [.. children.Select(c => c.DtoToTreeNode())] : [],
        };
    }
}

/// <summary>A node/edge list becomes its DTO form and back here — the one place shared by every
/// <see cref="Models.Series.NetworkGraphSeries"/> JSON round trip.</summary>
internal static class GraphSerializationExtensions
{
    extension(IReadOnlyList<Models.Series.GraphNode> nodes)
    {
        /// <summary>Converts this list of nodes to its DTO form for JSON serialisation.</summary>
        internal List<GraphNodeDto> ToGraphNodeDtos()
        {
            var result = new List<GraphNodeDto>(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                result.Add(new GraphNodeDto(n.Id, n.X, n.Y, n.ColorScalar, n.SizeScalar, n.Label));
            }
            return result;
        }
    }

    extension(IReadOnlyList<Models.Series.GraphEdge> edges)
    {
        /// <summary>Converts this list of edges to its DTO form for JSON serialisation.</summary>
        internal List<GraphEdgeDto> ToGraphEdgeDtos()
        {
            var result = new List<GraphEdgeDto>(edges.Count);
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                result.Add(new GraphEdgeDto(e.From, e.To, e.Weight, e.IsDirected));
            }
            return result;
        }
    }

    extension(List<GraphNodeDto>? dtos)
    {
        /// <summary>Converts this list of DTOs back to model <see cref="Models.Series.GraphNode"/> records; an
        /// empty array when <see langword="null"/> or empty.</summary>
        internal Models.Series.GraphNode[] FromGraphNodeDtos()
        {
            if (dtos is null || dtos.Count == 0)
            {
                return [];
            }
            var result = new Models.Series.GraphNode[dtos.Count];
            for (int i = 0; i < dtos.Count; i++)
            {
                var d = dtos[i];
                result[i] = new Models.Series.GraphNode(d.Id, d.X, d.Y, d.ColorScalar, d.SizeScalar, d.Label);
            }
            return result;
        }
    }

    extension(List<GraphEdgeDto>? dtos)
    {
        /// <summary>Converts this list of DTOs back to model <see cref="Models.Series.GraphEdge"/> records; an
        /// empty array when <see langword="null"/> or empty.</summary>
        internal Models.Series.GraphEdge[] FromGraphEdgeDtos()
        {
            if (dtos is null || dtos.Count == 0)
            {
                return [];
            }
            var result = new Models.Series.GraphEdge[dtos.Count];
            for (int i = 0; i < dtos.Count; i++)
            {
                var d = dtos[i];
                result[i] = new Models.Series.GraphEdge(d.From, d.To, d.Weight, d.IsDirected);
            }
            return result;
        }
    }
}

// DTOs for JSON serialization — flat records used by System.Text.Json for round-trip serialization.
// All properties are nullable to support sparse JSON where only relevant fields are present.

/// <summary>JSON-serializable representation of a <see cref="Models.Figure"/>.</summary>
internal sealed record FigureDto
{
    public string? Title { get; init; }
    public string? AltText { get; init; }
    public string? Description { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    public double? Dpi { get; init; }
    public Color? BackgroundColor { get; init; }
    public GridSpecDto? GridSpec { get; init; }
    public List<AxesDto>? SubPlots { get; init; }
    public bool? Enable3DRotation { get; init; }
    /// <summary>The colour bar the figure draws beside all its subplots; absent when it has none.</summary>
    public ColorBarDto? FigureColorBar { get; init; }

    // The figure's own settings. Each is absent at a new figure's value, so a figure that sets none of them writes the
    // bytes it wrote before they reached the JSON, and a JSON written before then reads back at those values.

    /// <summary>The theme every part of the figure falls back on; absent while it is the default theme (matplotlib-v2).
    /// Written property by property against the plain default theme (<see cref="ThemeDto"/>).</summary>
    public ThemeDto? Theme { get; init; }
    /// <summary>The margins and gaps around and between the subplots; absent for the library's.</summary>
    public SubPlotSpacingDto? Spacing { get; init; }
    /// <summary>The chart's identifier for server interaction; absent when it has none.</summary>
    public string? ChartId { get; init; }
    /// <summary><see langword="true"/> when the figure's interaction is routed through the server.</summary>
    public bool? ServerInteraction { get; init; }
    /// <summary><see langword="true"/> when the SVG embeds zoom and pan.</summary>
    public bool? EnableZoomPan { get; init; }
    /// <summary><see langword="true"/> when the SVG embeds the legend toggle.</summary>
    public bool? EnableLegendToggle { get; init; }
    /// <summary><see langword="true"/> when the SVG embeds rich tooltips.</summary>
    public bool? EnableRichTooltips { get; init; }
    /// <summary><see langword="true"/> when the SVG embeds hover highlight.</summary>
    public bool? EnableHighlight { get; init; }
    /// <summary><see langword="true"/> when the SVG embeds rectangle selection.</summary>
    public bool? EnableSelection { get; init; }
    /// <summary><see langword="true"/> when a click on a point is reported to the server.</summary>
    public bool? EnableDataCursor { get; init; }
    /// <summary><see langword="true"/> when the SVG embeds treemap drilldown.</summary>
    public bool? EnableTreemapDrilldown { get; init; }
    /// <summary><see langword="true"/> when the SVG embeds Sankey hover emphasis.</summary>
    public bool? EnableSankeyHover { get; init; }
    /// <summary>The tokens the interaction scripts read; absent for the defaults.</summary>
    public InteractionThemeDto? InteractionTheme { get; init; }
    /// <summary><see langword="false"/> for an SVG that keeps its pixel size instead of resizing with its container.</summary>
    public bool? ResponsiveSvg { get; init; }
}

/// <summary>Data-transfer object for a <see cref="Models.GridSpec"/> layout descriptor.</summary>
internal sealed record GridSpecDto
{
    public int Rows { get; init; }
    public int Cols { get; init; }
    public double[]? HeightRatios { get; init; }
    public double[]? WidthRatios { get; init; }
}

/// <summary>Data-transfer object for a <see cref="Models.GridPosition"/> span within a grid layout.</summary>
internal sealed record GridPositionDto
{
    public int RowStart { get; init; }
    public int RowEnd { get; init; }
    public int ColStart { get; init; }
    public int ColEnd { get; init; }
}

/// <summary>Data-transfer object that groups all four axis-spine configurations for a subplot.</summary>
internal sealed record SpinesConfigDto
{
    public SpineConfigDto? Top { get; init; }
    public SpineConfigDto? Bottom { get; init; }
    public SpineConfigDto? Left { get; init; }
    public SpineConfigDto? Right { get; init; }
}

/// <summary>Data-transfer object for a single axis spine (border line) on a subplot.</summary>
internal sealed record SpineConfigDto
{
    public bool Visible { get; init; } = true;
    public string? Position { get; init; }
    public double PositionValue { get; init; }
    public double LineWidth { get; init; } = 1.0;
    public Color? Color { get; init; }
    /// <summary>The spine's line style; absent for solid, and in a JSON written before spines had one.</summary>
    public LineStyle? LineStyle { get; init; }
}

/// <summary>JSON-serializable representation of an <see cref="Models.Axes"/> subplot, including series, annotations, and decorations.</summary>
internal sealed record AxesDto
{
    public string? Title { get; init; }
    public string? Key { get; init; }
    public string? ShareXKey { get; init; }
    public string? ShareYKey { get; init; }
    public GridPositionDto? GridPosition { get; init; }
    public SpinesConfigDto? Spines { get; init; }
    public AxisDto? XAxis { get; init; }
    public AxisDto? YAxis { get; init; }
    /// <summary>The Z axis of a 3-D axes; written for every axes the way <see cref="XAxis"/> and <see cref="YAxis"/>
    /// are, since <see cref="Models.Axes.ZAxis"/> always exists even on a 2-D axes that never reads it.</summary>
    public AxisDto? ZAxis { get; init; }
    public GridDto? Grid { get; init; }
    /// <summary><see langword="true"/> when <see cref="Grid"/> is the axes' own change over the theme's grid: the axes
    /// draws it whatever the theme's grid is, a grid switched off included. Absent when the axes takes the theme's grid
    /// (or switched its own on as it stands).</summary>
    public bool? GridOverridesTheme { get; init; }
    public string? BarMode { get; init; }
    public List<SeriesDto>? Series { get; init; }
    public AxisDto? SecondaryYAxis { get; init; }
    public List<SeriesDto>? SecondarySeries { get; init; }
    public AxisDto? SecondaryXAxis { get; init; }
    public List<SeriesDto>? XSecondarySeries { get; init; }
    public List<AnnotationDto>? Annotations { get; init; }
    public List<ReferenceLineDto>? ReferenceLines { get; init; }
    public List<SpanRegionDto>? Spans { get; init; }
    public List<TrendlineDto>? Trendlines { get; init; }
    public List<HorizontalLevelDto>? HorizontalLevels { get; init; }
    public List<FibonacciRetracementDto>? FibonacciRetracements { get; init; }
    public InsetBoundsDto? InsetBounds { get; init; }
    public List<AxesDto>? Insets { get; init; }
    public double? Elevation { get; init; }
    public double? Azimuth { get; init; }
    public double? CameraDistance { get; init; }
    public string? LightSourceType { get; init; }
    // v1.1.1 Axis breaks
    public List<AxisBreakDto>? XBreaks { get; init; }
    public List<AxisBreakDto>? YBreaks { get; init; }
    /// <summary>The style of the axes title; absent when it overrides nothing.</summary>
    public TextStyleDto? TitleStyle { get; init; }
    /// <summary>Where the axes title stands; absent for the centre.</summary>
    public TitleLocation? TitleLoc { get; init; }
    /// <summary>The legend; absent while it is a new legend's.</summary>
    public LegendDto? Legend { get; init; }
    /// <summary>The colour bar; absent when the axes has none.</summary>
    public ColorBarDto? ColorBar { get; init; }
    /// <summary>The 3-D axes' pane colours and visibility; written only when it differs from the record's own
    /// defaults, the way every other at-rest part is left out.</summary>
    public Pane3DConfigDto? Pane3D { get; init; }
    /// <summary>Whether this axes shows tooltips on hover; absent for the default <see langword="false"/>.</summary>
    public bool? EnableTooltips { get; init; }
    /// <summary>Whether this axes emits the interactive attributes the client-side script reads; absent for the
    /// default <see langword="false"/>.</summary>
    public bool? EnableInteractiveAttributes { get; init; }
    /// <summary>Whether this axes emits per-vertex 3-D data for client-side reprojection; absent for the default
    /// <see langword="false"/>.</summary>
    public bool? Emit3DVertexData { get; init; }
    /// <summary>Cartesian, polar or three-D; absent for the default <c>Cartesian</c>.</summary>
    public string? CoordinateSystem { get; init; }
}

/// <summary>Data-transfer object for <see cref="Models.Pane3DConfig"/>.</summary>
internal sealed record Pane3DConfigDto
{
    public Color? FloorColor { get; init; }
    public Color? LeftWallColor { get; init; }
    public Color? RightWallColor { get; init; }
    public double Alpha { get; init; } = 1.0;
    public bool Visible { get; init; } = true;
}

/// <summary>Data-transfer object for an <see cref="Models.AxisBreak"/>.</summary>
internal sealed record AxisBreakDto(double From, double To, string? Style);

/// <summary>Data-transfer object for an inset axes bounding box expressed in figure-normalized coordinates (0–1).</summary>
internal sealed record InsetBoundsDto
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
}

/// <summary>Data-transfer object for an <see cref="Models.Annotation"/> text label optionally linked to an arrow target.</summary>
internal sealed record AnnotationDto
{
    public string? Text { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double? ArrowTargetX { get; init; }
    public double? ArrowTargetY { get; init; }
    public string? ConnectionStyle { get; init; }
    public double? ConnectionRad { get; init; }
    public double? ArrowHeadSize { get; init; }
    public string? BoxStyle { get; init; }
    public double? BoxPadding { get; init; }
    public double? BoxCornerRadius { get; init; }
    /// <summary>The coordinate system X and Y are read in; absent for data coordinates.</summary>
    public AnnotationCoordinates? Coordinates { get; init; }
    /// <summary>The font of the text; absent when the text takes the theme's.</summary>
    public FontDto? Font { get; init; }
    /// <summary>The colour of the text (and of the arrow, unless it has its own); absent for the theme's text colour.</summary>
    public Color? Color { get; init; }
    /// <summary>The colour of the arrow; absent when it takes the text colour.</summary>
    public Color? ArrowColor { get; init; }
    /// <summary>How the text stands against its point; absent for left.</summary>
    public Rendering.TextAlignment? Alignment { get; init; }
    /// <summary>The text's rotation in degrees; absent for none.</summary>
    public double? Rotation { get; init; }
    /// <summary>The arrow's style; absent for the simple arrow.</summary>
    public ArrowStyle? ArrowStyle { get; init; }
    /// <summary>The fill behind the text when it has no callout box.</summary>
    public Color? BackgroundColor { get; init; }
    /// <summary>The fill of the callout box.</summary>
    public Color? BoxFaceColor { get; init; }
    /// <summary>The outline of the callout box.</summary>
    public Color? BoxEdgeColor { get; init; }
    /// <summary>The outline width of the callout box; absent for 1.</summary>
    public double? BoxLineWidth { get; init; }
}

/// <summary>Data-transfer object for a horizontal or vertical reference line drawn across a subplot.</summary>
internal sealed record ReferenceLineDto
{
    public double Value { get; init; }
    public string? Orientation { get; init; }
    public string? LineStyle { get; init; }
    public double LineWidth { get; init; }
    public string? Label { get; init; }
    /// <summary>The line's colour; absent when it takes the theme's.</summary>
    public Color? Color { get; init; }
}

/// <summary>Data-transfer object for a shaded span region (axhspan / axvspan) on a subplot.</summary>
internal sealed record SpanRegionDto
{
    public double Min { get; init; }
    public double Max { get; init; }
    public string? Orientation { get; init; }
    public double Alpha { get; init; }
    public string? LineStyle { get; init; }
    public double? LineWidth { get; init; }
    public string? Label { get; init; }
    /// <summary>The fill of the span; absent when it takes the theme's.</summary>
    public Color? Color { get; init; }
    /// <summary>The outline of the span; absent when it takes the fill colour.</summary>
    public Color? EdgeColor { get; init; }
}

/// <summary>Data-transfer object for a <see cref="Models.Tools.Trendline"/>.</summary>
internal sealed record TrendlineDto
{
    public double X1 { get; init; }
    public double Y1 { get; init; }
    public double X2 { get; init; }
    public double Y2 { get; init; }
    public string? Color { get; init; }
    public string? LineStyle { get; init; }
    public double LineWidth { get; init; } = 1.0;
    public string? Label { get; init; }
    public bool IsExtended { get; init; }
}

/// <summary>Data-transfer object for a <see cref="Models.Tools.HorizontalLevel"/>.</summary>
internal sealed record HorizontalLevelDto
{
    public double Value { get; init; }
    public string? Color { get; init; }
    public string? LineStyle { get; init; }
    public double LineWidth { get; init; } = 1.0;
    public string? Label { get; init; }
}

/// <summary>Data-transfer object for a <see cref="Models.Tools.FibonacciRetracement"/>.</summary>
internal sealed record FibonacciRetracementDto
{
    public double PriceHigh { get; init; }
    public double PriceLow { get; init; }
    public string? Color { get; init; }
    public double LineWidth { get; init; } = 1.0;
    public bool ShowLabels { get; init; } = true;
}

/// <summary>Data-transfer object for an axis configuration (label, limits, scale, and the colours of its ticks and title).</summary>
internal sealed record AxisDto
{
    public string? Label { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public string? Scale { get; init; }
    /// <summary>Colour of the major tick marks; absent in a JSON written before axis colours existed.</summary>
    public Color? TickColor { get; init; }
    /// <summary>Colour of the major tick labels; absent in a JSON written before axis colours existed.</summary>
    public Color? TickLabelColor { get; init; }
    /// <summary>Colour of the axis title; absent in a JSON written before axis colours existed.</summary>
    public Color? LabelColor { get; init; }
    /// <summary>The axis's tick locator when it is one of the library's; absent for the default ticks, and for a locator
    /// written outside the library (code the JSON cannot carry).</summary>
    public TickLocatorDto? TickLocator { get; init; }
    /// <summary>The axis's tick formatter when it is one of the library's; absent for the default labels, and for a
    /// formatter written outside the library.</summary>
    public TickFormatterDto? TickFormatter { get; init; }
    /// <summary><see langword="true"/> for an axis that runs from its maximum to its minimum.</summary>
    public bool? Inverted { get; init; }
    /// <summary>The data padding as a fraction of the range; absent when the theme's applies.</summary>
    public double? Margin { get; init; }
    /// <summary>The linear threshold of a symmetric-log axis; absent for 1.</summary>
    public double? SymLogLinThresh { get; init; }
    /// <summary>The major ticks, where they differ from a new axis's; their colours are <see cref="TickColor"/> and
    /// <see cref="TickLabelColor"/>.</summary>
    public TickConfigDto? MajorTicks { get; init; }
    /// <summary>The minor ticks, where they differ from a new axis's.</summary>
    public TickConfigDto? MinorTicks { get; init; }
    /// <summary>The style of the axis title, where it overrides anything; its colour is <see cref="LabelColor"/>.</summary>
    public TextStyleDto? LabelStyle { get; init; }
}

/// <summary>Unified flat DTO for all series types. The <see cref="Type"/> discriminator determines which properties are populated.</summary>
/// <remarks>This is intentionally a wide record — each series type uses a different subset of properties.
/// Unused properties serialize as null and are omitted from JSON output via <c>DefaultIgnoreCondition.WhenWritingNull</c>.</remarks>
public sealed record SeriesDto
{
    public string? Type { get; init; }
    public string? Label { get; set; }
    /// <summary>Where the series shows its <see cref="Label"/> when that is not the legend: <c>"lineend"</c> draws it at
    /// the end of the line. Absent for the default and in a JSON written before the setting existed, which reads as the
    /// legend. Only a line and a streaming line carry it.</summary>
    public string? LabelPlacement { get; set; }
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
    /// <summary>The size of the markers of a scatter-like series. A quiver key keeps the point size of its label here too,
    /// which is how every document it has been written in carries it.</summary>
    public double? MarkerSize { get; init; }
    public double? BarWidth { get; init; }
    public string? Orientation { get; init; }
    public int? Bins { get; init; }
    public int? BinsY { get; init; }
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

    /// <summary>The steps of a waterfall that are totals, by position from 0: a total bar stands on zero, reaches the
    /// value it holds and wears the total colour. Absent when no step is a total.</summary>
    public int[]? TotalIndices { get; init; }

    public string[]? Tasks { get; init; }
    public double[]? Starts { get; init; }
    public double[]? Ends { get; init; }
    public double? BarHeight { get; init; }
    public double? GaugeValue { get; init; }
    public double? GaugeMin { get; init; }
    public double? GaugeMax { get; init; }
    public Color? NeedleColor { get; init; }
    public Color? TrackColor { get; init; }

    /// <summary>The name of the series' colour map when it travels by its name: a map the library ships, or a map of your
    /// own type registered under its name. A map built in code travels in the internal <c>colorMap</c> description instead.</summary>
    public string? ColorMapName { get; init; }
    public double? VMin { get; init; }
    public double? VMax { get; init; }
    public string? Interpolation { get; init; }
    public List<List<double>>? VFieldData { get; init; }
    public double? Bandwidth { get; init; }
    public int? Degree { get; init; }
    public bool? ShowConfidence { get; init; }
    public double? ConfidenceLevel { get; init; }
    public int? GridSize { get; init; }
    public int? MinCount { get; init; }

    // v0.8.0
    public double? RugHeight { get; init; }
    public double? Jitter { get; init; }
    public double[][]? EventPositions { get; init; }
    public double? LineLength { get; init; }
    public double[][]? RangeStarts { get; init; }
    public double[][]? RangeWidths { get; init; }

    // v0.8.0 Phase B
    public string[][]? TableCellData { get; init; }
    public string[]? RowHeaders { get; init; }
    public string[]? ColumnHeaders { get; init; }
    public double[]? Signal { get; init; }
    public int? SampleRate { get; init; }
    public int? WindowSize { get; init; }
    public int? Overlap { get; init; }

    // v0.8.0 Phase C
    public double[]? ZData { get; init; }
    public int? Levels { get; init; }
    public int[]? Triangles { get; init; }
    public double? QuiverKeyX { get; init; }
    public double? QuiverKeyY { get; init; }
    public double? QuiverKeyU { get; init; }
    public string? QuiverKeyLabel { get; init; }
    public double[]? Speed { get; init; }
    public double[]? Direction { get; init; }
    public double? BarbLength { get; init; }

    // v0.9.0 Phase G — 3D enhancements
    public List<List<double>>? ZGridData { get; init; }
    public bool? ShowWireframe { get; init; }
    public int? RowStride { get; init; }
    public int? ColStride { get; init; }

    // v1.0 Signal series (distinct from int? SampleRate used by SpectrogramSeries)
    public double? SignalSampleRate { get; init; }
    public double? SignalXStart { get; init; }

    // v1.1 Spline smoothing (LineSeries, AreaSeries)
    public bool? Smooth { get; init; }
    public int? SmoothResolution { get; init; }

    // v1.1.1 PolarHeatmapSeries
    public int? ThetaBins { get; init; }
    public int? RBins { get; init; }

    // v1.3.0 3D series enhancements
    public double[]? WData { get; init; }
    public double? ArrowLength { get; init; }
    public List<List<List<bool>>>? VoxelData { get; init; }
    public List<Text3DAnnotationDto>? Text3DAnnotations { get; init; }

    // v1.10 HeatmapSeries extensions (annotated cells + triangular masks)
    public bool? ShowLabels { get; init; }
    public string? LabelFormat { get; init; }
    public string? MaskMode { get; init; }
    public Color? CellValueColor { get; init; }

    // v1.10 DendrogramSeries
    public DendrogramOrientation? DendrogramOrientation { get; init; }
    public double? CutHeight { get; init; }
    public Color? CutLineColor { get; init; }
    public bool? ColorByCluster { get; init; }

    // v1.10 ClustermapSeries panel-ratio overrides (null = use default 0.15)
    public double? RowDendrogramWidth { get; init; }
    public double? ColumnDendrogramHeight { get; init; }

    // v1.10 Phase 4 — PairGridSeries
    public List<List<double>>? Variables          { get; init; }
    public string[]?            PairGridLabels     { get; init; }
    public int[]?               PairGridHueGroups  { get; init; }
    public string[]?            PairGridHueLabels  { get; init; }
    public string?              PairGridDiagonal   { get; init; }   // enum name
    public string?              PairGridOffDiagonal{ get; init; }   // enum name
    public string?              PairGridTriangular { get; init; }   // enum name
    public int?                 PairGridDiagonalBins { get; init; }
    public double?              PairGridMarkerSize   { get; init; }
    public double?              PairGridCellSpacing  { get; init; }
    // v1.10 — Hexbin off-diagonal
    public int?                 PairGridHexbinGridSize      { get; init; }
    public string?              PairGridOffDiagonalColorMap { get; init; }

    // v1.10 — NetworkGraphSeries
    public List<GraphNodeDto>?  GraphNodes                     { get; init; }
    public List<GraphEdgeDto>?  GraphEdges                     { get; init; }
    public string?              NetworkGraphLayout             { get; init; }   // enum name
    public bool?                NetworkGraphShowNodeLabels     { get; init; }
    public bool?                NetworkGraphShowEdgeWeights    { get; init; }
    public double?              NetworkGraphEdgeThicknessScale { get; init; }
    public double?              NetworkGraphNodeRadiusScale    { get; init; }
    public int?                 NetworkGraphLayoutSeed         { get; init; }
    public int?                 NetworkGraphLayoutIterations   { get; init; }
    public double?              NetworkGraphConvergenceThreshold { get; init; }

    // v1.12 — StateTimelineSeries
    public List<Color>? StateSegmentColors { get; init; }

    // v1.14 — per-segment hatch: a band that says "no information" rather than a state.
    public List<HatchPattern>? StateSegmentHatches { get; init; }

    // The condition per band, decomposed the same way the colours and hatches are. Null when no band names one.
    public List<OpsSeverity>?   StateSegmentSeverities  { get; init; }
    public List<OpsVisibility>? StateSegmentVisibilities { get; init; }

    // v1.14 — threshold bands. Rendered by GaugeSeries since v1.9 but never serialized: a gauge sent over the
    // wire came back with its bands silently replaced by the defaults. Decomposed into parallel arrays, the
    // same shape StateTimelineSeries already uses for its segments. Named NEUTRALLY, not GaugeBand*: the
    // bullet graph carries the same (threshold, colour) bands, and two names for one shape is a fork.
    public double[]?     BandThresholds { get; init; }
    public List<Color>?  BandColors     { get; init; }

    // v1.14 — StatTileSeries.Format. Rendered since v1.12 but never serialized: a tile restored from the wire
    // read "0.3" where it had read "0.3 s".
    public string?       TileFormat { get; init; }

    // v1.14 — BulletGraphSeries.Target (the comparative tick).
    public double?       BulletTarget { get; init; }

    // StatTileSeries.Url / Expanded — a tile that leads somewhere. Both null at the default so an unlinked
    // tile adds no bytes and every golden stays byte-identical.
    public string?       TileUrl      { get; init; }
    public bool?         TileExpanded { get; init; }

    // The condition a tile or a band was judged to be in. Null at rest so a chart that never names one writes
    // exactly the bytes it wrote before the axis existed — a tile whose state is dropped on the wire comes back
    // drawn as quiet, which is the one failure that turns an alarm into calm.
    public OpsSeverity?   ConditionSeverity   { get; init; }
    public OpsVisibility? ConditionVisibility { get; init; }

    // v1.14 — hatch fills. NULLABLE by necessity: SeriesDto omits nulls
    // (DefaultIgnoreCondition.WhenWritingNull), so an unset hatch adds no bytes and the golden corpus of
    // every existing discriminator stays byte-identical. A non-nullable enum would always serialize and
    // would rewrite all 76 goldens for nothing.
    public HatchPattern? Hatch      { get; init; }
    public Color?        HatchColor { get; init; }

    // v1.11 — RelativeRotationSeries
    public List<List<double>>?  RrgAssetCloses       { get; init; }
    public List<double>?        RrgBenchmarkCloses   { get; init; }
    public string[]?            RrgAssetLabels       { get; init; }
    public string?              RrgFormula           { get; init; }   // enum name; null = DualEma (default)
    public int?                 RrgShortPeriod       { get; init; }
    public int?                 RrgLongPeriod        { get; init; }
    public int?                 RrgMomentumLookback  { get; init; }
    public int?                 RrgTailLength        { get; init; }
    public bool?                RrgShowQuadrantGrid  { get; init; }
    public List<double>?        RrgAbsorptionPerBar  { get; init; }
    public List<double>?        RrgEnbPerBar         { get; init; }

    // The drawn properties of the series a composed chart is built from. Each is absent at the value the series is
    // created with, so a figure that never sets one writes the bytes it wrote before the field existed, and a JSON
    // from before then reads back at that value. The first three every series shares; the serializer writes them
    // beside the label, the series' own ToSeriesDto writes the rest.

    /// <summary>The series' place in the drawing order (a higher one is drawn on top). Absent when the series is at the
    /// order it is created with (0, and -1 for a filled area).</summary>
    public int? ZOrder { get; set; }

    /// <summary><see langword="false"/> for a hidden series; absent for a visible one.</summary>
    public bool? Visible { get; set; }

    /// <summary>The most points an x/y series draws before it thins itself out; absent when it draws all of them.</summary>
    public int? MaxDisplayPoints { get; set; }

    /// <summary>The group this series shares its colour with (<see cref="Models.Series.ChartSeries.ColorGroup"/>);
    /// absent for a series that holds its own slot, as every series does until a verb groups it. Read back, two
    /// series written with the same value here are drawn in one cycled colour again, as they were.</summary>
    public int? ColorGroup { get; set; }

    /// <summary>The marker shape of a line, a scatter, a stem, a step or a 3-D scatter series.</summary>
    public MarkerStyle? Marker { get; init; }

    /// <summary>The fill of a line's markers; absent when they take the line colour.</summary>
    public Color? MarkerFaceColor { get; init; }

    /// <summary>The outline of a line's markers; absent when they take the line colour.</summary>
    public Color? MarkerEdgeColor { get; init; }

    /// <summary>The outline width of a line's markers.</summary>
    public double? MarkerEdgeWidth { get; init; }

    /// <summary>Every how-many-th point of a line carries a marker; absent when every point does.</summary>
    public int? MarkEvery { get; init; }

    /// <summary>How a line or an area steps between its points: a line's <c>DrawStyle</c>, an area's <c>StepMode</c>.</summary>
    public DrawStyle? DrawStyle { get; init; }

    /// <summary>The marker colour of a stem's heads.</summary>
    public Color? MarkerColor { get; init; }

    /// <summary>The colour of a stem's stems.</summary>
    public Color? StemColor { get; init; }

    /// <summary>The colour of a stem plot's baseline, a 3-D stem plot's included.</summary>
    public Color? BaselineColor { get; init; }

    /// <summary>The outline colour of a bar, a histogram, an area, a 3-D surface or a 3-D triangle surface, and the border of a table.</summary>
    public Color? EdgeColor { get; init; }

    /// <summary>The colour of each scatter point, one per point; the same list carries the colour of each slice of a pie or
    /// a donut, each stage of a funnel, each row of an event plot and each group of a pair grid's hue.</summary>
    public List<Color>? Colors { get; init; }

    /// <summary>The outline colour of each scatter point, one per point.</summary>
    public List<Color>? EdgeColors { get; init; }

    /// <summary>The outline width of each scatter point, one per point.</summary>
    public double[]? LineWidths { get; init; }

    /// <summary>The value of each scatter point that the colour map turns into its colour, one per point.</summary>
    public double[]? C { get; init; }

    /// <summary>Where each bar starts: the bar is drawn from this value up (or down, for a negative value). Absent
    /// when every bar starts at zero. A stacked chart works its stacking out when it is drawn and never writes it here.</summary>
    public double[]? StackBaseline { get; init; }

    /// <summary>The numeric x position of each bar, for bars on a continuous x axis; absent for bars in category slots.</summary>
    public double[]? XCoordinate { get; init; }

    /// <summary>Whether a bar is centred on its category or starts at its edge.</summary>
    public BarAlignment? Align { get; init; }

    /// <summary>The width of an error bar's lines, where it differs from the series' line width.</summary>
    public double? ELineWidth { get; init; }

    /// <summary>The thickness of an error bar's caps.</summary>
    public double? CapThick { get; init; }

    /// <summary>Every how-many-th point carries an error bar; absent when every point does.</summary>
    public int? ErrorEvery { get; init; }

    /// <summary>The baseline a stacked area stacks its layers on.</summary>
    public StackedBaseline? Baseline { get; init; }

    /// <summary>Whether a violin or a box marks the mean.</summary>
    public bool? ShowMeans { get; init; }

    /// <summary>Whether a violin marks the median.</summary>
    public bool? ShowMedians { get; init; }

    /// <summary><see langword="false"/> when a violin leaves out its minimum and maximum marks.</summary>
    public bool? ShowExtrema { get; init; }

    /// <summary><see langword="false"/> when a box leaves out its outlier points.</summary>
    public bool? ShowOutliers { get; init; }

    /// <summary>The position of each violin or box, one per dataset; absent for the positions 0, 1, 2, ….</summary>
    public double[]? Positions { get; init; }

    /// <summary>The width of each violin or box.</summary>
    public double? Widths { get; init; }

    /// <summary>Which side of its position a violin is drawn on.</summary>
    public ViolinSide? Side { get; init; }

    /// <summary>The colour of a box's median line.</summary>
    public Color? MedianColor { get; init; }

    /// <summary><see langword="false"/> for a box drawn horizontally.</summary>
    public bool? Vert { get; init; }

    /// <summary>How far a box's whiskers reach, in interquartile ranges.</summary>
    public double? Whis { get; init; }

    /// <summary><see langword="false"/> when a density curve is drawn as its line alone, without the area under it;
    /// absent when the area is filled.</summary>
    public bool? Fill { get; init; }

    /// <summary>The explicit iso-levels of a filled contour, where it has them instead of the evenly spaced ones
    /// <see cref="Levels"/> asks for.</summary>
    public double[]? LevelValues { get; init; }

    /// <summary><see langword="false"/> when a filled contour draws no iso-lines over its fill; absent when it does.</summary>
    public bool? ShowLines { get; init; }

    /// <summary>The hatch of each band of a filled contour, lowest band first, or of each slice of a pie; absent when none is hatched.</summary>
    public List<HatchPattern>? Hatches { get; init; }

    /// <summary>The gap between the cells of a treemap, where it differs from the default.</summary>
    public double? Padding { get; init; }

    /// <summary>What a treemap does with a label wider than its cell; absent when it draws every label whole.</summary>
    public TreemapLabelFit? LabelFit { get; init; }

    /// <summary>The point size of the headline a treemap leaf shows, where it differs from the default.</summary>
    public double? HeadlineFontSize { get; init; }

    /// <summary>The narrowest sweep, in degrees, of a sunburst segment that still shows its label.</summary>
    public double? MinLabelSweepDegrees { get; init; }

    /// <summary>How an image is composited over what is behind it; absent for plain alpha compositing.</summary>
    public BlendMode? BlendMode { get; init; }

    /// <summary>The outer radius of a polar heatmap, in data units; absent for 1.</summary>
    public double? RMax { get; init; }

    // The settings of the series that drew more than their JSON said. Each is absent at the value the series is created
    // with, so a figure that never sets one writes the bytes it wrote before the field existed, and a JSON from before
    // then reads back at that value. Colours, hatches, alpha, line width and the like reuse the fields above.

    /// <summary><see langword="true"/> for a histogram whose bars add up the counts below them; absent for plain counts.</summary>
    public bool? Cumulative { get; init; }

    /// <summary><see langword="true"/> for a histogram scaled so that its bars cover an area of 1; absent for counts.</summary>
    public bool? Density { get; init; }

    /// <summary>How a histogram is drawn: as bars, as an outline or as a filled step; absent for bars.</summary>
    public HistType? HistType { get; init; }

    /// <summary>The weight of each observation of a histogram, one per value; absent when every observation counts once.</summary>
    public double[]? Weights { get; init; }

    /// <summary>The width of a histogram bar as a fraction of its bin; absent for the whole bin.</summary>
    public double? RWidth { get; init; }

    /// <summary>The format of the percentage written on each slice of a pie, such as <c>"%1.1f%%"</c>; absent for none.</summary>
    public string? AutoPct { get; init; }

    /// <summary><see langword="true"/> for a pie whose slices run counter-clockwise; absent for clockwise.</summary>
    public bool? CounterClockwise { get; init; }

    /// <summary>How far each slice of a pie is pulled out of the circle, one per slice; absent when none is.</summary>
    public double[]? Explode { get; init; }

    /// <summary><see langword="true"/> for a pie drawn with a shadow; absent for none.</summary>
    public bool? Shadow { get; init; }

    /// <summary>The radius of a pie; absent when the plot area decides it.</summary>
    public double? Radius { get; init; }

    /// <summary>The width of a Sankey diagram's nodes, where it differs from the default.</summary>
    public double? NodeWidth { get; init; }

    /// <summary>The gap between the nodes of a Sankey diagram, where it differs from the default.</summary>
    public double? NodePadding { get; init; }

    /// <summary>The opacity of a Sankey diagram's links, where it differs from the default.</summary>
    public double? LinkAlpha { get; init; }

    /// <summary>How the node columns of a Sankey diagram are aligned; absent for justified.</summary>
    public SankeyNodeAlignment? NodeAlignment { get; init; }

    /// <summary>How many passes a Sankey diagram makes to untangle its links; absent for the default number.</summary>
    public int? Iterations { get; init; }

    /// <summary>What colours a Sankey link: its source, its target or a blend of the two; absent for the blend.</summary>
    public SankeyLinkColorMode? LinkColorMode { get; init; }

    /// <summary>Whether a Sankey diagram runs left to right or top to bottom; absent for left to right.</summary>
    public SankeyOrientation? SankeyOrientation { get; init; }

    /// <summary><see langword="true"/> for a Sankey diagram that writes its labels inside the nodes; absent for outside.</summary>
    public bool? InsideLabels { get; init; }

    /// <summary>The size of the arrow heads of a stream plot, where it differs from the default.</summary>
    public double? ArrowSize { get; init; }

    /// <summary>How closely a stream plot packs its streamlines, where it differs from the default.</summary>
    public double? StreamDensity { get; init; }

    /// <summary><see langword="false"/> for a residual plot that draws no line at zero; absent when it does.</summary>
    public bool? ShowZeroLine { get; init; }

    /// <summary>The fill of the header cells of a table; absent for the theme's.</summary>
    public Color? HeaderColor { get; init; }

    /// <summary>The height of a table row, where it differs from the default.</summary>
    public double? CellHeight { get; init; }

    /// <summary>The padding inside a table cell, where it differs from the default.</summary>
    public double? CellPadding { get; init; }

    /// <summary>The point size of the text a table, a tree grid or a line contour's labels write, where it differs from
    /// the default.</summary>
    public double? FontSize { get; init; }

    /// <summary>The width of one value column of a tree grid, where it differs from the default.</summary>
    public double? ColumnWidth { get; init; }

    /// <summary>How far one level of depth indents a row of a tree grid, where it differs from the default.</summary>
    public double? IndentWidth { get; init; }

    /// <summary>The height of one row of a tree grid, where it differs from the default.</summary>
    public double? RowHeight { get; init; }

    /// <summary><see langword="true"/> for a line contour that fills the bands between its levels; absent for lines.</summary>
    public bool? Filled { get; init; }

    /// <summary>The normaliser a series maps its values to colours with, when it is one of the library's;
    /// absent for the linear mapping and for a normaliser written outside the library. Internal: the wire carries it,
    /// the public shape of this record does not.</summary>
    [JsonInclude]
    internal NormalizerDto? Normalizer { get; init; }

    /// <summary>The series' colour map when it was built in code, described by what it is made of; absent when it
    /// travels by its name (<see cref="ColorMapName"/>) or cannot be written. Internal: the wire carries it, the public
    /// shape of this record does not.</summary>
    [JsonInclude]
    internal ColorMapDto? ColorMap { get; init; }

    /// <summary>A pair grid's off-diagonal colour map when it was built in code, described by what it is made of; absent
    /// when it travels by its name (<see cref="PairGridOffDiagonalColorMap"/>). Internal, as <see cref="ColorMap"/>.</summary>
    [JsonInclude]
    internal ColorMapDto? PairGridOffDiagonalColorMapDescription { get; init; }

    // Streaming series (JSON round-trip): the ring buffer's capacity and its current snapshot. The snapshot reuses
    // XData/YData/Open/High/Low/Close/YData+SignalSampleRate+SignalXStart — the same fields the non-streaming
    // Line/Candlestick/Signal series already carry — so a restored stream starts from the picture it was, not empty.
    public int? Capacity { get; init; }

    // Polar series data (JSON round-trip): R and Theta are their own fields, not XData/YData, since a polar
    // series' two coordinates are a radius and an angle, not a rectangular X and Y.
    public double[]? RData { get; init; }
    public double[]? ThetaData { get; init; }

    // TreeGridSeries data (JSON round-trip): the rows themselves — the caller's own flattening of whatever
    // hierarchy it drew, kept as a flat list the same way the series already holds it.
    public List<TreeGridRowDto>? TreeGridRows { get; init; }

    // Hierarchical tree data (JSON round-trip): TreemapSeries, SunburstSeries and DendrogramSeries each hold one
    // TreeNode root (HierarchicalSeries.Root); ClustermapSeries holds two (RowTree, ColumnTree) alongside its
    // already-carried matrix. One recursive TreeNodeDto (ChartSerializer.TreeNodeToDto/DtoToTreeNode) serves all
    // four — the node shape does not change with how many of them a series holds.
    public TreeNodeDto? TreeRoot { get; init; }
    public TreeNodeDto? RowTree { get; init; }
    public TreeNodeDto? ColumnTree { get; init; }

    // SankeySeries data (JSON round-trip): the nodes and the links between them, by index — the same shape
    // SankeyNode/SankeyLink already hold.
    public List<SankeyNodeDto>? SankeyNodes { get; init; }
    public List<SankeyLinkDto>? SankeyLinks { get; init; }
}

/// <summary>DTO for one <see cref="Models.TreeNode"/>, recursively — see
/// <see cref="TreeNodeSerializationExtensions"/>.</summary>
public sealed record TreeNodeDto
{
    public string Label { get; init; } = "";
    public double Value { get; init; }
    public Color? Color { get; init; }
    public double? ColorValue { get; init; }
    public string? Headline { get; init; }
    public string? Url { get; init; }
    public bool? Expanded { get; init; }
    public HatchPattern Hatch { get; init; } = HatchPattern.None;
    public Color? HatchColor { get; init; }
    public List<TreeNodeDto>? Children { get; init; }
}

/// <summary>DTO for one <see cref="Models.Series.TreeGridRow"/>.</summary>
public sealed record TreeGridRowDto
{
    public string Label { get; init; } = "";
    public List<string>? Cells { get; init; }
    public int Depth { get; init; }
    public string? Url { get; init; }
    public bool? Expanded { get; init; }
    public Color? Accent { get; init; }
}

/// <summary>DTO for a single 3D text annotation positioned at (X, Y, Z).</summary>
public sealed record Text3DAnnotationDto(double X, double Y, double Z, string Text);

/// <summary>DTO for one <see cref="MatPlotLibNet.Models.Series.GraphNode"/>.</summary>
public sealed record GraphNodeDto(string Id, double X, double Y, double ColorScalar, double SizeScalar, string? Label);

/// <summary>DTO for one <see cref="MatPlotLibNet.Models.Series.GraphEdge"/>.</summary>
public sealed record GraphEdgeDto(string From, string To, double Weight, bool IsDirected);

/// <summary>DTO for one <see cref="Models.SankeyNode"/>.</summary>
public sealed record SankeyNodeDto(string Label, Color? Color = null, string? SubLabel = null, Color? SubLabelColor = null, int? Column = null);

/// <summary>DTO for one <see cref="Models.SankeyLink"/>.</summary>
public sealed record SankeyLinkDto(int SourceIndex, int TargetIndex, double Value);

/// <summary>Converts <see cref="Color"/> values to and from hex strings (e.g., "#FF0000") during JSON serialization.</summary>
internal sealed class ColorJsonConverter : JsonConverter<Color>
{
    /// <inheritdoc />
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString();
        return hex is not null ? Color.FromHex(hex) : default;
    }

    /// <inheritdoc />
    /// <remarks>An opaque colour is written <c>#RRGGBB</c>, as it always was; a colour with transparency
    /// <c>#RRGGBBAA</c>, so its alpha survives the round trip (a translucent bad colour of a colour map, a stop that fades out).</remarks>
    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.A == byte.MaxValue ? value.ToHex() : $"{value.ToHex()}{value.A:X2}");
    }
}
