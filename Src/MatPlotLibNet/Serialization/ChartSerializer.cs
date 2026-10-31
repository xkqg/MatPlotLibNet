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
        Legend = LegendDto.FromPresent(figure.Legend),
        Texts = figure.Texts.Count > 0 ? [.. figure.Texts.Select(FigureTextDto.From)] : null,
        Images = figure.Images.Count > 0 ? [.. figure.Images.Select(FigureImageDto.From)] : null,
        Theme = ReferenceEquals(figure.Theme, RestingFigure.Theme) ? null : ThemeDto.From(figure.Theme),
        Spacing = figure.Spacing == RestingFigure.Spacing ? null : SubPlotSpacingDto.From(figure.Spacing),
        ChartId = figure.ChartId,
        ServerInteraction = figure.ServerInteraction.OrNullAtRest(RestingFigure.ServerInteraction),
        EnableZoomPan = figure.EnableZoomPan.OrNullAtRest(RestingFigure.EnableZoomPan),
        EnableLegendToggle = figure.EnableLegendToggle.OrNullAtRest(RestingFigure.EnableLegendToggle),
        EnableRichTooltips = figure.EnableRichTooltips.OrNullAtRest(RestingFigure.EnableRichTooltips),
        EnableHighlight = figure.EnableHighlight.OrNullAtRest(RestingFigure.EnableHighlight),
        EnableSelection = figure.EnableSelection.OrNullAtRest(RestingFigure.EnableSelection),
        SelectionGroup = figure.SelectionGroup,
        SelectedRows = figure.SelectedRows?.ToArray(),
        EnableDataCursor = figure.EnableDataCursor.OrNullAtRest(RestingFigure.EnableDataCursor),
        EnableViewerCursors = figure.EnableViewerCursors.OrNullAtRest(RestingFigure.EnableViewerCursors),
        EnableTreemapDrilldown = figure.EnableTreemapDrilldown.OrNullAtRest(RestingFigure.EnableTreemapDrilldown),
        EnableSankeyHover = figure.EnableSankeyHover.OrNullAtRest(RestingFigure.EnableSankeyHover),
        InteractionTheme = figure.InteractionTheme == RestingFigure.InteractionTheme ? null : InteractionThemeDto.From(figure.InteractionTheme),
        ResponsiveSvg = figure.ResponsiveSvg.OrNullAtRest(RestingFigure.ResponsiveSvg),
    };

    /// <summary>Writes one axes. <paramref name="theme"/> is the figure's: a grid the axes changed over the theme's grid is
    /// written as that change (<see cref="AxesDto.GridChange"/>: the properties it set, with their values), so it is still
    /// "the theme's grid, but ..." when it is read and drawn under another theme. The grid it draws under the figure's theme
    /// is written beside it, marked as the axes' own (<see cref="AxesDto.GridOverridesTheme"/>), for a reader that predates
    /// the change: it draws that grid whatever the theme is, and a grid switched off stays off under a theme that draws one.</summary>
    internal static AxesDto AxesToDto(Axes axes, Theme theme)
    {
        var rightPast = SlotsPastSlot0ToDto(axes.YAxes.Right);
        var leftPast = SlotsPastSlot0ToDto(axes.YAxes.Left);
        return new()
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
            Grid = GridDto.From(axes.GridChange is null ? axes.Grid : axes.GridUnder(theme)),
            GridOverridesTheme = axes.GridChange is null ? null : true,
            GridChange = axes.GridChange is { } gridChange ? GridChangeDto.From(gridChange) : null,
            BarMode = axes.BarMode == BarMode.Stacked ? "stacked" : null,
            Series = SeriesToDtos(axes.Series),
            Annotations = axes.Annotations.Count > 0 ? axes.Annotations.Select(AnnotationToDto).ToList() : null,
            ReferenceLines = axes.ReferenceLines.Count > 0 ? axes.ReferenceLines.Select(r => new ReferenceLineDto
            {
                Value = r.Value, Orientation = r.Orientation.ToString().ToLowerInvariant(),
                LineStyle = r.LineStyle.ToString().ToLowerInvariant(), LineWidth = r.LineWidth, Label = r.Label,
                Color = r.Color,
            }).ToList() : null,
            SecondaryYAxis = axes.YAxes.Right.Count > 0 ? AxisToDto(axes.YAxes.Right[0].Axis) : null,
            SecondarySeries = axes.YAxes.Right is { Count: > 0 } rightStack && rightStack[0].Series.Count > 0 ? SeriesToDtos(rightStack[0].Series) : null,
            SecondaryXAxis = axes.SecondaryXAxis is not null ? AxisToDto(axes.SecondaryXAxis) : null,
            XSecondarySeries = axes.XSecondarySeries.Count > 0 ? SeriesToDtos(axes.XSecondarySeries) : null,
            ExtraRightYAxes = rightPast.Frozen,
            RightYAxesOverflow = rightPast.Overflow,
            LeftYAxes = leftPast.Frozen,
            LeftYAxesOverflow = leftPast.Overflow,
            YAxisTitleGap = axes.YAxes.TitleGap.OrNullAtRest(RestingYAxisTitleGap),
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
            ZoomIndicator = ZoomIndicatorDto.From(axes.ZoomIndicator),
            Elevation = axes.Elevation != 30 ? axes.Elevation : null,
            Azimuth = axes.Azimuth != -60 ? axes.Azimuth : null,
            CameraDistance = axes.CameraDistance,
            Aspect = axes.Aspect,
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
    }

    /// <summary>The title gap of a new container (4): the value <see cref="AxesDto.YAxisTitleGap"/> is left out at, and
    /// read back as when it is absent.</summary>
    private static readonly double RestingYAxisTitleGap = new YAxisContainer().TitleGap;

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
            VerticalAlignment = a.VerticalAlignment.OrNullAtRest(rest.VerticalAlignment),
            OffsetX = a.OffsetX.OrNullAtRest(rest.OffsetX),
            OffsetY = a.OffsetY.OrNullAtRest(rest.OffsetY),
            AvoidOverlap = a.AvoidOverlap.OrNullAtRest(rest.AvoidOverlap),
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

    /// <summary>The ONE place a list of series is written: the primary list, every other Y axis' and the top axis',
    /// each series through <see cref="SeriesToDto"/>.</summary>
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
        BoundsFrom = s.BoundsFrom,
        BoundsTo = s.BoundsTo,
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
        BoundsFrom = dto.BoundsFrom,
        BoundsTo = dto.BoundsTo,
    };

    /// <summary>The ONE place the fields every series shares leave the model: its label and where it is shown when
    /// that is not the legend, its place in the drawing order, whether it is shown at all, and how many points an x/y
    /// series draws at most. Every series list (primary, every other Y axis, the top axis, insets) is written through here; what a
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
        {
            dto.MaxDisplayPoints = xy.MaxDisplayPoints;
            dto.RowKeys = xy.RowKeys;
        }

        if (series is ChartSeries { ColorGroup: { } colorGroup })
            dto.ColorGroup = colorGroup;
        if (series is ChartSeries { WearsNeutral: true })
            dto.WearsNeutral = true;
        return dto;
    }

    /// <summary>The ONE place the fields every series shares enter the model: the counterpart of
    /// <see cref="SeriesToDto"/>, shared by the primary list, every other Y axis and the top axis. A field that is absent, or an
    /// unknown placement, leaves the value the series was created with.</summary>
    internal static void RestoreSharedFields(ISeries series, SeriesDto dto)
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
        if (series is XYSeries keyed && dto.RowKeys is { } rowKeys)
            keyed.RowKeys = rowKeys;
        if (series is ChartSeries chartSeries && dto.ColorGroup is { } colorGroup)
            chartSeries.ColorGroup = colorGroup;
        if (series is ChartSeries wearing && dto.WearsNeutral is true)
            wearing.WearsNeutral = true;
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
            Legend = dto.Legend?.ToLegend(),
            Theme = dto.Theme?.ToTheme() ?? RestingFigure.Theme,
            Spacing = dto.Spacing?.ToSpacing() ?? RestingFigure.Spacing,
            ChartId = dto.ChartId,
            ServerInteraction = dto.ServerInteraction ?? RestingFigure.ServerInteraction,
            EnableZoomPan = dto.EnableZoomPan ?? RestingFigure.EnableZoomPan,
            EnableLegendToggle = dto.EnableLegendToggle ?? RestingFigure.EnableLegendToggle,
            EnableRichTooltips = dto.EnableRichTooltips ?? RestingFigure.EnableRichTooltips,
            EnableHighlight = dto.EnableHighlight ?? RestingFigure.EnableHighlight,
            EnableSelection = dto.EnableSelection ?? RestingFigure.EnableSelection,
            SelectionGroup = dto.SelectionGroup,
            SelectedRows = dto.SelectedRows,
            EnableDataCursor = dto.EnableDataCursor ?? RestingFigure.EnableDataCursor,
            EnableViewerCursors = dto.EnableViewerCursors ?? RestingFigure.EnableViewerCursors,
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

        foreach (var textDto in dto.Texts ?? [])
            textDto.AddTo(figure);
        foreach (var imageDto in dto.Images ?? [])
            imageDto.AddTo(figure);

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
        axes.ZoomIndicator = axDto.ZoomIndicator?.ToZoomIndicator();

        if (axDto.XAxis is not null) ApplyAxis(axes.XAxis, axDto.XAxis);
        if (axDto.YAxis is not null) ApplyAxis(axes.YAxis, axDto.YAxis);
        if (axDto.ZAxis is not null) ApplyAxis(axes.ZAxis, axDto.ZAxis);
        RestorePane3D(axes, axDto.Pane3D);
        if (axDto.EnableTooltips is { } enableTooltips) axes.EnableTooltips = enableTooltips;
        if (axDto.EnableInteractiveAttributes is { } enableInteractiveAttributes) axes.EnableInteractiveAttributes = enableInteractiveAttributes;
        if (axDto.Emit3DVertexData is { } emit3DVertexData) axes.Emit3DVertexData = emit3DVertexData;
        axDto.CoordinateSystem.ApplyEnum<Models.CoordinateSystem>(v => axes.CoordinateSystem = v);
        RestoreGrid(axes, axDto);
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
        axes.Aspect = axDto.Aspect;
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
            if (aDto.VerticalAlignment is { } verticalAlignment) ann.VerticalAlignment = verticalAlignment;
            if (aDto.OffsetX is { } offsetX) ann.OffsetX = offsetX;
            if (aDto.OffsetY is { } offsetY) ann.OffsetY = offsetY;
            if (aDto.AvoidOverlap is { } avoidOverlap) ann.AvoidOverlap = avoidOverlap;
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

    /// <summary>Reads every Y axis past the primary back into the container, the container's title gap, and the top X
    /// axis. The frozen 1.23.0 fields keep their published meaning: <c>secondaryYAxis</c> is right slot 0 (the primary,
    /// left slot 0, is <c>yAxis</c>); the entries of <c>extraRightYAxes</c> and <c>leftYAxes</c> are the slots past slot 0
    /// of their side, in outward order, and <c>rightYAxesOverflow</c> and <c>leftYAxesOverflow</c> continue that order.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><c>yAxisTitleGap</c> is negative, NaN or infinite.</exception>
    private static void RestoreSecondaryAxes(Axes axes, AxesDto axDto)
    {
        if (axDto.SecondaryYAxis is not null)
        {
            var first = axes.YAxes.Right.FirstOrAdd();
            ApplyAxis(first.Axis, axDto.SecondaryYAxis);
            foreach (var sDto in axDto.SecondarySeries ?? [])
                axes.CreateOnSecondarySeat(sDto, s => first.AddSeries(s));
        }

        RestoreSlotsPastSlot0(axes, axes.YAxes.Right, new(axDto.ExtraRightYAxes, axDto.RightYAxesOverflow));
        RestoreSlotsPastSlot0(axes, axes.YAxes.Left, new(axDto.LeftYAxes, axDto.LeftYAxesOverflow));
        if (axDto.YAxisTitleGap is { } titleGap)
            axes.YAxes.TitleGap = YAxisContainer.ValidTitleGap(titleGap, "yAxisTitleGap");

        if (axDto.SecondaryXAxis is not null)
        {
            axes.TwinY();
            ApplyAxis(axes.SecondaryXAxis!, axDto.SecondaryXAxis);
            foreach (var sDto in axDto.XSecondarySeries ?? [])
                axes.CreateOnSecondarySeat(sDto, s => axes.AddXSecondarySeries(s));
        }
    }

    /// <summary>The one reader of the slots past slot 0 of a side: each entry, the frozen field's and then the overflow
    /// field's, stacks exactly one slot on <paramref name="stack"/>. Slot 0 is made first (on the left it is the primary,
    /// always there), so an entry never moves down into slot 0, even from a JSON that carries no slot 0 of the right side.</summary>
    /// <param name="axes">The axes being read; its series registry builds each slot's series.</param>
    /// <param name="stack">The side's stack of the axes' Y axes.</param>
    /// <param name="entries">The side's entries past slot 0.</param>
    private static void RestoreSlotsPastSlot0(Axes axes, YAxisStack stack, YAxisSlotsPastSlot0Dto entries)
    {
        foreach (var slotDto in entries.InOutwardOrder)
        {
            stack.FirstOrAdd();
            RestoreOutwardSlot(axes, stack.Add(), slotDto);
        }
    }

    /// <summary>Restores one outward-stacked slot's own axis settings and series onto <paramref name="slot"/>,
    /// which <see cref="YAxisStack.Add"/> already created.</summary>
    private static void RestoreOutwardSlot(Axes axes, YAxisSlot slot, SecondaryAxisSlotDto slotDto)
    {
        if (slotDto.Axis is not null) ApplyAxis(slot.Axis, slotDto.Axis);
        foreach (var sDto in slotDto.Series ?? [])
            axes.CreateOnSecondarySeat(sDto, s => slot.AddSeries(s));
    }

    /// <summary>The one writer of the slots past slot 0 of a side, in outward order: the first
    /// <see cref="YAxisSlotsPastSlot0Dto.FrozenCap"/> into the side's frozen field, every further one into its overflow
    /// field, each list <see langword="null"/> (left out) when it holds none. So every slot has exactly one carrier, and a
    /// figure within the 1.23.0 caps writes the bytes 1.23.0 wrote.</summary>
    /// <param name="stack">The side's stack of the axes' Y axes.</param>
    private static YAxisSlotsPastSlot0Dto SlotsPastSlot0ToDto(YAxisStack stack)
    {
        var pastSlot0 = stack.Skip(1).Select(SlotToDto).ToList();
        return new(
            pastSlot0.Count > 0 ? pastSlot0.Take(YAxisSlotsPastSlot0Dto.FrozenCap).ToList() : null,
            pastSlot0.Count > YAxisSlotsPastSlot0Dto.FrozenCap ? pastSlot0.Skip(YAxisSlotsPastSlot0Dto.FrozenCap).ToList() : null);
    }

    private static SecondaryAxisSlotDto SlotToDto(YAxisSlot slot) => new()
    {
        Axis = AxisToDto(slot.Axis),
        Series = slot.Series.Count > 0 ? SeriesToDtos(slot.Series) : null,
    };

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

    /// <summary>Reads an axes' grid back. A <see cref="AxesDto.GridChange"/> is the change the author made, and it is the only
    /// thing read as one: it is applied over the grid of the theme the figure is drawn under. A JSON written before changes
    /// were written has only the resolved grid: marked as the axes' own it is the grid the axes draws whatever the theme's
    /// grid is (a switched-off one included), so it comes back as the change that sets every property to what it has; any
    /// other grid comes back as it stands, and the renderer takes the theme's grid over one that is not switched on
    /// (<see cref="Axes.GridUnder"/>).</summary>
    private static void RestoreGrid(Axes axes, AxesDto axDto)
    {
        var grid = axDto.Grid?.ToGridStyle();
        if (grid is not null) axes.Grid = grid;

        if (axDto.GridChange is { } change) axes.GridChange = change.ToGridChange();
        else if (axDto.GridOverridesTheme is true && grid is not null) axes.GridChange = GridChange.AllOf(grid);
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
        /// Case-insensitive; does nothing when this string is <see langword="null"/> (the JSON carries no value). A name
        /// that is not a member of <typeparamref name="T"/> (a figure written by a newer version, a hand-made payload) is
        /// still dropped leniently, so the figure keeps loading, but is announced on <see cref="ChartDiagnostics"/> instead
        /// of coming back drawn as the default without a word.</summary>
        internal void ApplyEnum<T>(Action<T> setter) where T : struct, Enum
        {
            if (value is null) return;

            if (Enum.TryParse<T>(value, true, out var parsed))
            {
                setter(parsed);
                return;
            }

            ChartDiagnostics.Emit(new ChartDiagnostic(
                nameof(ChartSerializer),
                $"Unknown {typeof(T).Name} '{value}' was ignored during deserialization; the default is kept.",
                null));
        }
    }
}

/// <summary>Reconstructs an axes, or a secondary-axis series, from its DTO — every entry point past
/// <see cref="ChartSerializer.RestoreAxes"/> itself that needs the <see cref="Axes"/> it is restoring onto.</summary>
internal static class AxesSerializationExtensions
{
    extension(Axes target)
    {
        /// <summary>Reconstructs ANY registered series kind from <paramref name="dto"/> and seats it where
        /// <paramref name="reseat"/> says — the row-99 fix (ROADMAP.md: "the right-hand and top axes carry lines
        /// and points; a bar or an area there is not kept"). <see cref="SeriesRegistry.Create"/> always lands its
        /// series on <c>target.Series</c> (every factory calls its own primary verb, e.g. <c>axes.Plot</c>), so
        /// this builds normally and then moves the just-created one, instead of rewriting 70 factories to take a
        /// seat parameter. Emits the same <see cref="ChartDiagnostics"/> entry <see cref="ChartSerializer.AddSeriesFromDto"/>
        /// emits for an unknown kind, so a secondary-axis series no longer vanishes silently either.</summary>
        /// <param name="dto">The series to reconstruct.</param>
        /// <param name="reseat">Where the series belongs once built — <c>axes.AddXSecondarySeries</c>, or a Y axis
        /// slot's own <see cref="YAxisSlot.AddSeries{TSeries}"/>.</param>
        /// <returns>The reconstructed series, or <see langword="null"/> for an unknown discriminator.</returns>
        internal ISeries? CreateOnSecondarySeat(SeriesDto dto, Action<ISeries> reseat)
        {
            var type = dto.Type ?? "unknown";
            var created = SeriesRegistry.Create(type, target, dto);
            if (created is null)
            {
                ChartDiagnostics.Emit(new ChartDiagnostic(
                    nameof(ChartSerializer),
                    $"Unknown series discriminator '{type}' was skipped during deserialization.",
                    null));
                return null;
            }

            target.TakeLastSeries();
            reseat(created);
            ChartSerializer.RestoreSharedFields(created, dto);
            return created;
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
    /// <summary>The legend of the figure as a whole, which lists the labelled series of every subplot; absent when the figure has
    /// none. An empty object is a legend at its defaults (which draws), so a missing field and an empty one differ.</summary>
    public LegendDto? Legend { get; init; }
    /// <summary>The texts placed on the figure itself, in the order they were added; absent when there are none.</summary>
    public List<FigureTextDto>? Texts { get; init; }
    /// <summary>The images placed on the figure itself, in the order they were added; absent when there are none.</summary>
    public List<FigureImageDto>? Images { get; init; }

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
    /// <summary>The name that links this chart's selection to the others of the page; absent when it links to nothing.</summary>
    public string? SelectionGroup { get; init; }
    /// <summary>The rows the chart starts with selected; absent when none.</summary>
    public string[]? SelectedRows { get; init; }
    /// <summary><see langword="true"/> when a click on a point is reported to the server.</summary>
    public bool? EnableDataCursor { get; init; }

    /// <summary><see langword="true"/> when the viewers of a server-interactive chart see each other's pointers.</summary>
    public bool? EnableViewerCursors { get; init; }
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
    public double LineWidth { get; init; } = 0.8;
    public Color? Color { get; init; }
    /// <summary>The spine's line style; absent for solid, and in a JSON written before spines had one.</summary>
    public LineStyle? LineStyle { get; init; }
    /// <summary>Where the spine begins along its own axis, in data units; absent when it starts at the edge of the plot.</summary>
    public double? BoundsFrom { get; init; }
    /// <summary>Where the spine ends along its own axis, in data units; absent when it ends at the edge of the plot.</summary>
    public double? BoundsTo { get; init; }
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
    /// (or switched its own on as it stands). A reader that knows <see cref="GridChange"/> reads that instead; this is what
    /// a reader that predates it draws.</summary>
    public bool? GridOverridesTheme { get; init; }
    /// <summary>What the axes changed about the theme's grid: only the properties it set, each with its value. Applied over
    /// the grid of the theme the figure is drawn under, so the change follows the theme. Absent when the axes changed nothing.</summary>
    public GridChangeDto? GridChange { get; init; }
    public string? BarMode { get; init; }
    public List<SeriesDto>? Series { get; init; }
    public AxisDto? SecondaryYAxis { get; init; }
    public List<SeriesDto>? SecondarySeries { get; init; }
    public AxisDto? SecondaryXAxis { get; init; }
    public List<SeriesDto>? XSecondarySeries { get; init; }
    /// <summary>The right Y axes past slot 0 (<see cref="SecondaryYAxis"/>/<see cref="SecondarySeries"/> carry slot 0),
    /// in outward order: entry <c>i</c> is right slot <c>i + 1</c>, even in a JSON that has no slot 0. Frozen as 1.23.0
    /// published it, whose reader holds one entry and merges every further one onto it, so the writer puts at most
    /// <see cref="YAxisSlotsPastSlot0Dto.FrozenCap"/> slot here and the rest in <see cref="RightYAxesOverflow"/>; the
    /// reader takes every entry. Absent when the right side has no slot past slot 0.</summary>
    public List<SecondaryAxisSlotDto>? ExtraRightYAxes { get; init; }
    /// <summary>The right Y axes past the ones <see cref="ExtraRightYAxes"/> holds, in outward order, continuing where it
    /// stops (the first entry is right slot 2). Absent when there are none, so a figure within the 1.23.0 caps writes the
    /// bytes 1.23.0 wrote; a 1.23.0 reader skips this field and draws the figure without these axes.</summary>
    public List<SecondaryAxisSlotDto>? RightYAxesOverflow { get; init; }
    /// <summary>The left Y axes past the primary (<see cref="YAxis"/>/<see cref="Series"/> carry the primary, left slot 0),
    /// in outward order: entry <c>i</c> is left slot <c>i + 1</c>. Frozen as 1.23.0 published it, whose reader holds one
    /// entry and merges every further one onto it, so the writer puts at most <see cref="YAxisSlotsPastSlot0Dto.FrozenCap"/>
    /// slot here and the rest in <see cref="LeftYAxesOverflow"/>; the reader takes every entry. Absent when the left side
    /// has no slot past the primary.</summary>
    public List<SecondaryAxisSlotDto>? LeftYAxes { get; init; }
    /// <summary>The left Y axes past the ones <see cref="LeftYAxes"/> holds, in outward order, continuing where it stops
    /// (the first entry is left slot 2). Absent when there are none, so a figure within the 1.23.0 caps writes the bytes
    /// 1.23.0 wrote; a 1.23.0 reader skips this field and draws the figure without these axes.</summary>
    public List<SecondaryAxisSlotDto>? LeftYAxesOverflow { get; init; }
    /// <summary>The blank gap in pixels between every Y axis title and its tick labels, and between two stacked Y axes
    /// (<see cref="YAxisContainer.TitleGap"/>). Absent at the default of 4, which is what a JSON without it reads as; a
    /// value that is negative, NaN or infinite is refused.</summary>
    public double? YAxisTitleGap { get; init; }
    public List<AnnotationDto>? Annotations { get; init; }
    public List<ReferenceLineDto>? ReferenceLines { get; init; }
    public List<SpanRegionDto>? Spans { get; init; }
    public List<TrendlineDto>? Trendlines { get; init; }
    public List<HorizontalLevelDto>? HorizontalLevels { get; init; }
    public List<FibonacciRetracementDto>? FibonacciRetracements { get; init; }
    public InsetBoundsDto? InsetBounds { get; init; }
    public List<AxesDto>? Insets { get; init; }
    /// <summary>The zoom this axes indicates on its parent when it is an inset: the rectangle over the region it shows and the
    /// lines to its frame. Absent when it indicates none; an empty object is an indicator at its defaults. A 1.27.0 reader
    /// skips it.</summary>
    public ZoomIndicatorDto? ZoomIndicator { get; init; }
    public double? Elevation { get; init; }
    public double? Azimuth { get; init; }
    public double? CameraDistance { get; init; }

    /// <summary>The ratio of the length of a unit of Y to a unit of X on the page (<see cref="Axes.Aspect"/>); left out when the plot fills its cell.</summary>
    public double? Aspect { get; init; }
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
    /// <summary>Cartesian, polar, three-D or ternary; absent for the default <c>Cartesian</c>. A name this reader does not
    /// know is announced on <see cref="Diagnostics.ChartDiagnostics"/> and the axes is drawn as Cartesian.</summary>
    public string? CoordinateSystem { get; init; }
}

/// <summary>Data-transfer object for one <see cref="YAxisSlot"/> past slot 0 of its side: an entry of the frozen lists
/// (<see cref="AxesDto.ExtraRightYAxes"/>, <see cref="AxesDto.LeftYAxes"/>) or of the overflow lists
/// (<see cref="AxesDto.RightYAxesOverflow"/>, <see cref="AxesDto.LeftYAxesOverflow"/>).</summary>
internal sealed record SecondaryAxisSlotDto
{
    /// <summary>The slot's axis: its label, limits, ticks, scale and colours.</summary>
    public AxisDto? Axis { get; init; }
    /// <summary>The series plotted against the slot's axis; absent when it has none.</summary>
    public List<SeriesDto>? Series { get; init; }
}

/// <summary>The slots past slot 0 of one side, the way the wire carries them: the side's frozen 1.23.0 list
/// (<see cref="AxesDto.ExtraRightYAxes"/> or <see cref="AxesDto.LeftYAxes"/>) and its overflow list
/// (<see cref="AxesDto.RightYAxesOverflow"/> or <see cref="AxesDto.LeftYAxesOverflow"/>), each <see langword="null"/>
/// when it holds none.</summary>
/// <param name="Frozen">The entries of the side's frozen list.</param>
/// <param name="Overflow">The entries of the side's overflow list, continuing the outward order where
/// <paramref name="Frozen"/> stops.</param>
internal readonly record struct YAxisSlotsPastSlot0Dto(List<SecondaryAxisSlotDto>? Frozen, List<SecondaryAxisSlotDto>? Overflow)
{
    /// <summary>How many slots past slot 0 a 1.23.0 reader holds per side (right slot 1, left slot 1); it merges every
    /// further entry of the frozen list onto that one, so the writer puts no more than this many there.</summary>
    internal const int FrozenCap = 1;

    /// <summary>Every entry in outward order: the frozen list's, then the overflow list's.</summary>
    internal IEnumerable<SecondaryAxisSlotDto> InOutwardOrder => (Frozen ?? []).Concat(Overflow ?? []);
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
    /// <summary>How the text stands against its point vertically; absent for the baseline.</summary>
    public Rendering.TextVerticalAlignment? VerticalAlignment { get; init; }
    /// <summary>How many pixels the text stands right of its point; absent for none.</summary>
    public double? OffsetX { get; init; }
    /// <summary>How many pixels the text stands below its point; absent for none.</summary>
    public double? OffsetY { get; init; }
    /// <summary>Whether the text moves out of the way of what stands where it was put; absent for no.</summary>
    public bool? AvoidOverlap { get; init; }
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
    /// <summary>The discriminator that names the kind of series, such as <c>"line"</c> or <c>"bar"</c>. It picks the reader
    /// registered for it in <see cref="SeriesRegistry"/> and decides which of the other properties the series
    /// uses.</summary>
    public string? Type { get; init; }
    /// <summary>The series' legend label (<see cref="Models.Series.ChartSeries.Label"/>); absent for a series without
    /// one.</summary>
    public string? Label { get; set; }
    /// <summary>Where the series shows its <see cref="Label"/> when that is not the legend: <c>"lineend"</c> draws it at
    /// the end of the line. Absent for the default and in a JSON written before the setting existed, which reads as the
    /// legend. Only a line and a streaming line carry it.</summary>
    public string? LabelPlacement { get; set; }
    /// <summary>The X coordinate of each point of an x/y series, a 3-D series or a vector field, one per point. A streaming
    /// series writes its current snapshot here.</summary>
    public double[]? XData { get; init; }
    /// <summary>The Y coordinate of each point, one per point; for a signal series the samples themselves, spaced along X by
    /// <see cref="SignalSampleRate"/>. A streaming series writes its current snapshot here.</summary>
    public double[]? YData { get; init; }
    /// <summary>The observations of a histogram, a density curve, an empirical distribution curve or a rug plot, as they
    /// were given.</summary>
    public double[]? Data { get; init; }
    /// <summary>The size of each point of a scatter or bubble series, one per point, or the size of each slice of a pie or
    /// donut, which is drawn as its share of the total.</summary>
    public double[]? Sizes { get; init; }
    /// <summary>The value of each category or step: the heights of the bars, the steps of a waterfall, the stages of a
    /// funnel, the axes of a radar, the counts of a waffle, the weights of a word cloud, the prices of a Point and Figure
    /// chart, the samples of a sparkline and the trend line of a stat tile.</summary>
    public double[]? Values { get; init; }
    /// <summary>The name of each category, step, axis or word, in the order of <see cref="Values"/>: the bars, the waterfall
    /// steps, the radar axes, the words of a word cloud, the group names of a chord diagram, the rows of a broken-bar chart
    /// and the states of a state timeline.</summary>
    public string[]? Categories { get; init; }
    /// <summary>The names of the parts of a whole: the slices of a pie or a donut, the stages of a funnel and the layers of
    /// a stacked area.</summary>
    public string[]? PieLabels { get; init; }
    /// <summary>One array of values per group, for the series that compare groups: the violins, boxes, strips, swarms and
    /// points of a category chart, and the layers of a stacked area.</summary>
    public double[][]? Datasets { get; init; }
    /// <summary>The grid of values of a heatmap, an image, a pseudo-colour mesh or a contour, as a list of rows. A stream
    /// plot keeps the X component of its vector field here; the Y component is <see cref="VFieldData"/>.</summary>
    public List<List<double>>? HeatmapData { get; init; }
    /// <summary>The colour of the series; absent when it takes the next colour of the theme's cycle.</summary>
    public Color? Color { get; init; }
    /// <summary>The style of the series' line, as the lower-case name of the line style: <c>"solid"</c>, <c>"dashed"</c>,
    /// <c>"dotted"</c> or <c>"dashdot"</c>. Absent leaves the series at its own default.</summary>
    public string? LineStyle { get; init; }
    /// <summary>The width of the series' line; absent leaves the series at its own default.</summary>
    public double? LineWidth { get; init; }
    /// <summary>The size of the markers of a scatter-like series. A quiver key keeps the point size of its label here too,
    /// which is how every document it has been written in carries it.</summary>
    public double? MarkerSize { get; init; }
    /// <summary>The width of a bar: a share of the space for one category in a bar chart or a waterfall, the angular width
    /// of a polar bar in radians, or the side of a 3-D bar's base in data units. Absent leaves the series at its own
    /// default.</summary>
    public double? BarWidth { get; init; }
    /// <summary>The direction the series is drawn in, as the lower-case name <c>"vertical"</c> or <c>"horizontal"</c> (a bar
    /// chart, a count plot, a bullet graph, an area); absent for the series' own default.</summary>
    public string? Orientation { get; init; }
    /// <summary>The number of bins of a histogram; for a 2-D histogram, the number of bins along X (the bins along Y are
    /// <see cref="BinsY"/>).</summary>
    public int? Bins { get; init; }
    /// <summary>The number of bins of a 2-D histogram along the Y axis.</summary>
    public int? BinsY { get; init; }
    /// <summary>The second curve of an upright area: where on the Y axis the fill ends, at each X position. Absent for a
    /// fill down to zero.</summary>
    public double[]? YData2 { get; init; }
    /// <summary>The second curve of an area that lies down (<c>orientation</c> horizontal): where on the X axis it ends.</summary>
    public double[]? XData2 { get; init; }
    /// <summary>The number of rows of a waffle chart's grid; left out at the default of 10.</summary>
    public int? WaffleRows { get; init; }
    /// <summary>The number of columns of a waffle chart's grid; left out at the default of 10.</summary>
    public int? WaffleColumns { get; init; }
    /// <summary>The space between a waffle chart's cells, as a share of a cell's side; left out at the default of 0.2.</summary>
    public double? CellGap { get; init; }
    /// <summary>The price one box of a Point and Figure chart covers.</summary>
    public double? BoxSize { get; init; }
    /// <summary>The boxes a price has to come back to start a new column of a Point and Figure chart; left out at the default of 3.</summary>
    public int? Reversal { get; init; }
    /// <summary>The opacity of the series' fill or marks, from 0 (transparent) to 1 (opaque); for a chord diagram, the
    /// opacity of its ribbons. Absent leaves the series at its own default.</summary>
    public double? Alpha { get; init; }
    /// <summary>Where a step series makes its step between two points, as the lower-case name <c>"pre"</c>, <c>"mid"</c> or
    /// <c>"post"</c>.</summary>
    public string? StepPosition { get; init; }
    /// <summary>How far the error bar of each point reaches below it, one value per point.</summary>
    public double[]? YErrorLow { get; init; }
    /// <summary>How far the error bar of each point reaches above it, one value per point.</summary>
    public double[]? YErrorHigh { get; init; }
    /// <summary>How far the horizontal error bar of each point reaches to its left, one value per point; absent for a series
    /// with no horizontal error bars.</summary>
    public double[]? XErrorLow { get; init; }
    /// <summary>How far the horizontal error bar of each point reaches to its right, one value per point; absent for a
    /// series with no horizontal error bars.</summary>
    public double[]? XErrorHigh { get; init; }
    /// <summary>The size of the caps at the ends of an error bar (their half-width, in pixels) or of the intervals of a
    /// point plot.</summary>
    public double? CapSize { get; init; }
    /// <summary>The opening price of each bar of a candlestick or OHLC series.</summary>
    public double[]? Open { get; init; }
    /// <summary>The highest price of each bar of a candlestick or OHLC series.</summary>
    public double[]? High { get; init; }
    /// <summary>The lowest price of each bar of a candlestick or OHLC series.</summary>
    public double[]? Low { get; init; }
    /// <summary>The closing price of each bar of a candlestick or OHLC series.</summary>
    public double[]? Close { get; init; }
    /// <summary>The label under each bar of a candlestick or OHLC series (usually a date), one per bar; absent when the bars
    /// carry none.</summary>
    public string[]? DateLabels { get; init; }
    /// <summary>The colour of an up bar of a candlestick or OHLC series, one that closes above where it opened.</summary>
    public Color? UpColor { get; init; }
    /// <summary>The colour of a down bar of a candlestick or OHLC series, one that closes below where it opened.</summary>
    public Color? DownColor { get; init; }
    /// <summary>The width of a candle's body as a share of the space for one bar.</summary>
    public double? BodyWidth { get; init; }
    /// <summary>The X component of the vector at each point of a quiver field, one per point; a 3-D quiver carries its X
    /// components here too.</summary>
    public double[]? UData { get; init; }
    /// <summary>The Y component of the vector at each point of a quiver field, one per point; a 3-D quiver carries its Y
    /// components here too.</summary>
    public double[]? VData { get; init; }
    /// <summary>The factor each arrow's vector is multiplied by to get its length in data units; 1 draws an arrow as long as
    /// its vector.</summary>
    public double? Scale { get; init; }
    /// <summary>The length of a quiver arrow's head as a share of the arrow's own length.</summary>
    public double? ArrowHeadSize { get; init; }
    /// <summary>The fill colour of a series that has a fill of its own to name: a radar polygon, an area, the band of a
    /// regression, the filled part of a progress bar and the cells of a table.</summary>
    public Color? FillColor { get; init; }
    /// <summary>The value the outer ring of a radar chart stands for; absent when the largest value of the data
    /// does.</summary>
    public double? MaxValue { get; init; }
    /// <summary>The radius of the hole of a donut or a sunburst as a share of its outer radius.</summary>
    public double? InnerRadius { get; init; }
    /// <summary>The text written in the hole of a donut; for a stat tile, its caption.</summary>
    public string? CenterText { get; init; }
    /// <summary>The angle in degrees at which the first slice of a pie or donut starts; 90 puts it at the top.</summary>
    public double? StartAngle { get; init; }
    /// <summary>The length of the open tick and of the close tick of an OHLC bar, in units of 20 pixels.</summary>
    public double? TickWidth { get; init; }
    /// <summary>The colour of a waterfall step that raises the running total.</summary>
    public Color? IncreaseColor { get; init; }
    /// <summary>The colour of a waterfall step that lowers the running total.</summary>
    public Color? DecreaseColor { get; init; }
    /// <summary>The colour of a waterfall step that is a total, one named in <see cref="TotalIndices"/>.</summary>
    public Color? TotalColor { get; init; }

    /// <summary>The steps of a waterfall that are totals, by position from 0: a total bar stands on zero, reaches the
    /// value it holds and wears the total colour. Absent when no step is a total.</summary>
    public int[]? TotalIndices { get; init; }

    /// <summary>The name of each task of a Gantt chart, one per bar.</summary>
    public string[]? Tasks { get; init; }
    /// <summary>Where each task of a Gantt chart, or each segment of a state timeline, starts on the time axis, one per task
    /// or segment.</summary>
    public double[]? Starts { get; init; }
    /// <summary>Where each task of a Gantt chart, or each segment of a state timeline, ends on the time axis, one per task
    /// or segment.</summary>
    public double[]? Ends { get; init; }
    /// <summary>The thickness of a Gantt, broken-bar or progress-bar bar, in the units of the row spacing.</summary>
    public double? BarHeight { get; init; }
    /// <summary>The value a gauge, a bullet graph, a progress bar or a stat tile shows.</summary>
    public double? GaugeValue { get; init; }
    /// <summary>The lower bound of a gauge's dial.</summary>
    public double? GaugeMin { get; init; }
    /// <summary>The upper bound of a gauge's dial.</summary>
    public double? GaugeMax { get; init; }
    /// <summary>The colour of a gauge's needle.</summary>
    public Color? NeedleColor { get; init; }
    /// <summary>The colour of the element that sits beside the value: the track of a progress bar, the target mark of a
    /// bullet graph and the trend line of a stat tile.</summary>
    public Color? TrackColor { get; init; }

    /// <summary>The name of the series' colour map when it travels by its name: a map the library ships, or a map of your
    /// own type registered under its name. A map built in code travels in the internal <c>colorMap</c> description instead.</summary>
    public string? ColorMapName { get; init; }
    /// <summary>The lowest value the colour map covers; absent when the colour scale follows the data.</summary>
    public double? VMin { get; init; }
    /// <summary>The highest value the colour map covers; absent when the colour scale follows the data.</summary>
    public double? VMax { get; init; }
    /// <summary>The name of the interpolation an image is resampled with, such as <c>"nearest"</c>, <c>"bilinear"</c> or
    /// <c>"bicubic"</c>; absent for nearest.</summary>
    public string? Interpolation { get; init; }
    /// <summary>The grid of the Y component of a stream plot's vector field, as a list of rows; the X component is in
    /// <see cref="HeatmapData"/>.</summary>
    public List<List<double>>? VFieldData { get; init; }
    /// <summary>The smoothing bandwidth of a density curve, in the units of the data; absent when it is chosen from the
    /// data.</summary>
    public double? Bandwidth { get; init; }
    /// <summary>The degree of the polynomial a regression or residual plot fits; a regression leaves it out for 1, a straight
    /// line.</summary>
    public int? Degree { get; init; }
    /// <summary><see langword="true"/> for a regression that draws its confidence band; absent when it does not.</summary>
    public bool? ShowConfidence { get; init; }
    /// <summary>The confidence level of a regression's band or of a point plot's intervals, as a fraction: 0.95 is 95
    /// percent. A regression leaves it out for 0.95.</summary>
    public double? ConfidenceLevel { get; init; }
    /// <summary>The resolution of a hexbin plot's grid of hexagons: more of them make each one smaller. Absent for
    /// 20.</summary>
    public int? GridSize { get; init; }
    /// <summary>The fewest points a hexagon of a hexbin plot needs to be drawn; absent for 1.</summary>
    public int? MinCount { get; init; }

    // v0.8.0
    /// <summary>The height of the ticks of a rug plot as a share of the range of the Y axis.</summary>
    public double? RugHeight { get; init; }
    /// <summary>How far a strip plot spreads its points sideways, in category widths: each point is moved by a random amount
    /// of at most this much.</summary>
    public double? Jitter { get; init; }
    /// <summary>The position of each event of an event plot, one array per row.</summary>
    public double[][]? EventPositions { get; init; }
    /// <summary>The length of each tick of an event plot, in the units of the row spacing.</summary>
    public double? LineLength { get; init; }
    /// <summary>Where each segment of a broken-bar chart starts, one array per row.</summary>
    public double[][]? RangeStarts { get; init; }
    /// <summary>The width of each segment of a broken-bar chart, one array per row in the order of
    /// <see cref="RangeStarts"/>.</summary>
    public double[][]? RangeWidths { get; init; }

    // v0.8.0 Phase B
    /// <summary>The text of every cell of a table, as a list of rows; the headers are not part of it.</summary>
    public string[][]? TableCellData { get; init; }
    /// <summary>The header of each row of a table, drawn in a column of its own; absent for a table without row
    /// headers.</summary>
    public string[]? RowHeaders { get; init; }
    /// <summary>The header of each column of a table, or the column titles of a tree grid; absent for one without
    /// headers.</summary>
    public string[]? ColumnHeaders { get; init; }
    /// <summary>How the text of each data column of a table is aligned in its cell; left out when every column is left aligned.</summary>
    public Rendering.TextAlignment[]? ColumnAlignments { get; init; }
    /// <summary>The relative width of each displayed column of a table; left out when every column is as wide as the others.</summary>
    public double[]? ColumnWidths { get; init; }
    /// <summary>The samples of the signal a spectrogram analyses, in time order.</summary>
    public double[]? Signal { get; init; }
    /// <summary>The number of samples per second of a spectrogram's signal, which sets its time and frequency
    /// scales.</summary>
    public int? SampleRate { get; init; }
    /// <summary>The number of samples in each window of a spectrogram's short-time Fourier transform.</summary>
    public int? WindowSize { get; init; }
    /// <summary>The number of samples two neighbouring windows of a spectrogram share.</summary>
    public int? Overlap { get; init; }

    // v0.8.0 Phase C
    /// <summary>The Z coordinate of each point or bar of a 3-D series, and the value at each point of a triangulated colour
    /// or contour plot.</summary>
    public double[]? ZData { get; init; }
    /// <summary>The number of levels a contour plot divides its values into when it is not given explicit ones; absent for
    /// 10.</summary>
    public int? Levels { get; init; }
    /// <summary>The triangles of a triangulated colour plot as a flat list of point indices, three per triangle; absent when
    /// the mesh is triangulated from the points.</summary>
    public int[]? Triangles { get; init; }
    /// <summary>The horizontal position of a quiver key (the reference arrow), as a fraction of the width of the axes from
    /// the left.</summary>
    public double? QuiverKeyX { get; init; }
    /// <summary>The vertical position of a quiver key (the reference arrow), as a fraction of the height of the axes from
    /// the bottom.</summary>
    public double? QuiverKeyY { get; init; }
    /// <summary>The vector length a quiver key's arrow stands for, in the data units of the field it explains.</summary>
    public double? QuiverKeyU { get; init; }
    /// <summary>The text written beside a quiver key's arrow.</summary>
    public string? QuiverKeyLabel { get; init; }
    /// <summary>The wind speed at each point of a wind-barb chart, in knots: each full barb is 10 and each flag
    /// 50.</summary>
    public double[]? Speed { get; init; }
    /// <summary>The direction of the staff of each wind barb, in degrees clockwise from north.</summary>
    public double[]? Direction { get; init; }
    /// <summary>The length of a wind barb's staff, in pixels.</summary>
    public double? BarbLength { get; init; }

    // v0.9.0 Phase G — 3D enhancements
    /// <summary>The Z values of a 3-D surface, wireframe or contour as a grid, a list of rows.</summary>
    public List<List<double>>? ZGridData { get; init; }
    /// <summary><see langword="false"/> for a 3-D surface or triangle surface drawn without its wireframe edges; absent when
    /// it draws them.</summary>
    public bool? ShowWireframe { get; init; }
    /// <summary>How many grid rows a 3-D surface steps over between one drawn cell and the next; absent for 1, every
    /// row.</summary>
    public int? RowStride { get; init; }
    /// <summary>How many grid columns a 3-D surface steps over between one drawn cell and the next; absent for 1, every
    /// column.</summary>
    public int? ColStride { get; init; }

    // v1.0 Signal series (distinct from int? SampleRate used by SpectrogramSeries)
    /// <summary>The samples per X unit of a signal series (Hz for a signal over time), which spaces its samples along
    /// X.</summary>
    public double? SignalSampleRate { get; init; }
    /// <summary>The X value of the first sample of a signal series; a signal series leaves it out for 0.</summary>
    public double? SignalXStart { get; init; }

    // v1.1 Spline smoothing (LineSeries, AreaSeries)
    /// <summary><see langword="true"/> for a line or an area drawn as a smooth curve through its points; absent for straight
    /// segments.</summary>
    public bool? Smooth { get; init; }
    /// <summary>The number of interpolated points per interval of a smooth line or area; absent for 10.</summary>
    public int? SmoothResolution { get; init; }

    // v1.1.1 PolarHeatmapSeries
    /// <summary>The number of angular bins (cells around the circle) of a polar heatmap.</summary>
    public int? ThetaBins { get; init; }
    /// <summary>The number of radial bins (cells along the radius) of a polar heatmap.</summary>
    public int? RBins { get; init; }

    // v1.3.0 3D series enhancements
    /// <summary>The Z component of the vector at each point of a 3-D quiver, one per point.</summary>
    public double[]? WData { get; init; }
    /// <summary>The factor a 3-D quiver multiplies its vectors by to get the length of its arrows; absent for 1.</summary>
    public double? ArrowLength { get; init; }
    /// <summary>The cells of a voxel plot as nested lists indexed by X, then Y, then Z: <see langword="true"/> for a filled
    /// cell.</summary>
    public List<List<List<bool>>>? VoxelData { get; init; }
    /// <summary>The texts of a 3-D text series, each with its position.</summary>
    public List<Text3DAnnotationDto>? Text3DAnnotations { get; init; }

    // v1.10 HeatmapSeries extensions (annotated cells + triangular masks)
    /// <summary><see langword="true"/> for a series that annotates each data point with its value (a heatmap cell, a bar, a
    /// contour line); absent for none.</summary>
    public bool? ShowLabels { get; init; }
    /// <summary>The format string of those value labels, such as <c>"F2"</c> for two decimals; absent for the default
    /// numeric format.</summary>
    public string? LabelFormat { get; init; }
    /// <summary>Which cells of a symmetric heatmap are hidden, as the name of the mask mode, such as <c>"UpperTriangle"</c>
    /// or <c>"LowerTriangle"</c>; absent for none.</summary>
    public string? MaskMode { get; init; }
    /// <summary>The colour of the values written in the cells of a heatmap; absent when each is black or white, whichever
    /// contrasts with its cell.</summary>
    public Color? CellValueColor { get; init; }
    /// <summary>The date of the cell in row 0, column 0 of a heatmap that is a calendar (<see cref="Models.Series.HeatmapSeries.FirstCellDate"/>);
    /// absent for a grid that is no calendar. With it the table of the heatmap gives every cell its date.</summary>
    public DateTime? FirstCellDate { get; init; }

    // v1.10 DendrogramSeries
    /// <summary>Which side of the plot the root of a dendrogram sits on; absent for the top, with the leaves along the
    /// bottom.</summary>
    public DendrogramOrientation? DendrogramOrientation { get; init; }
    /// <summary>The merge distance at which a dendrogram is cut into clusters, drawn as a reference line; absent for no
    /// cut.</summary>
    public double? CutHeight { get; init; }
    /// <summary>The colour of a dendrogram's cut line; absent for the theme's neutral colour.</summary>
    public Color? CutLineColor { get; init; }
    /// <summary><see langword="false"/> for a dendrogram drawn in one colour instead of one per cluster below the cut;
    /// absent when it colours by cluster.</summary>
    public bool? ColorByCluster { get; init; }

    // v1.10 ClustermapSeries panel-ratio overrides (null = use default 0.15)
    /// <summary>The share of the plot's width that a clustermap gives to its row dendrogram; absent for 0.15.</summary>
    public double? RowDendrogramWidth { get; init; }
    /// <summary>The share of the plot's height that a clustermap gives to its column dendrogram; absent for 0.15.</summary>
    public double? ColumnDendrogramHeight { get; init; }

    // v1.10 Phase 4 — PairGridSeries
    /// <summary>The data of a pair grid as one list of values per variable
    /// (<see cref="Models.Series.PairGridSeries.Variables"/>).</summary>
    public List<List<double>>? Variables          { get; init; }
    /// <summary>The name of each variable of a pair grid, written on its axes
    /// (<see cref="Models.Series.PairGridSeries.Labels"/>); absent for none.</summary>
    public string[]?            PairGridLabels     { get; init; }
    /// <summary>The group number of each row of a pair grid's data, which colours its points by group
    /// (<see cref="Models.Series.PairGridSeries.HueGroups"/>); absent for no grouping.</summary>
    public int[]?               PairGridHueGroups  { get; init; }
    /// <summary>The legend label of each hue group of a pair grid, indexed by group number
    /// (<see cref="Models.Series.PairGridSeries.HueLabels"/>); absent for none.</summary>
    public string[]?            PairGridHueLabels  { get; init; }
    /// <summary>What a pair grid draws on its diagonal, as the name of the diagonal kind
    /// (<see cref="Models.Series.PairGridSeries.DiagonalKind"/>); absent for a histogram.</summary>
    public string?              PairGridDiagonal   { get; init; }   // enum name
    /// <summary>What a pair grid draws off its diagonal, as the name of the off-diagonal kind
    /// (<see cref="Models.Series.PairGridSeries.OffDiagonalKind"/>); absent for a scatter.</summary>
    public string?              PairGridOffDiagonal{ get; init; }   // enum name
    /// <summary>Which triangle of the grid a pair grid draws, as the name of the triangle
    /// (<see cref="Models.Series.PairGridSeries.Triangular"/>); absent for both.</summary>
    public string?              PairGridTriangular { get; init; }   // enum name
    /// <summary>The number of bins of the histograms on a pair grid's diagonal; absent for 20.</summary>
    public int?                 PairGridDiagonalBins { get; init; }
    /// <summary>The radius in pixels of the dots of a pair grid's scatter cells; absent for 3.</summary>
    public double?              PairGridMarkerSize   { get; init; }
    /// <summary>The share of the plot's size left as a gutter between the cells of a pair grid; absent for 0.02.</summary>
    public double?              PairGridCellSpacing  { get; init; }
    // v1.10 — Hexbin off-diagonal
    /// <summary>The resolution of the hexagon grid in the cells of a pair grid drawn as hexbins; absent for 15.</summary>
    public int?                 PairGridHexbinGridSize      { get; init; }
    /// <summary>The name of the colour map of a pair grid's hexbin cells when it travels by its name; a map built in code
    /// travels as a description instead.</summary>
    public string?              PairGridOffDiagonalColorMap { get; init; }

    // v1.10 — NetworkGraphSeries
    /// <summary>The nodes of a network graph.</summary>
    public List<GraphNodeDto>?  GraphNodes                     { get; init; }
    /// <summary>The edges of a network graph, each naming its two nodes by their id.</summary>
    public List<GraphEdgeDto>?  GraphEdges                     { get; init; }
    /// <summary>How a network graph places its nodes, as the name of the layout
    /// (<see cref="Models.Series.NetworkGraphSeries.Layout"/>); absent for the circular layout.</summary>
    public string?              NetworkGraphLayout             { get; init; }   // enum name
    /// <summary><see langword="false"/> for a network graph that writes no label next to its nodes; absent when it
    /// does.</summary>
    public bool?                NetworkGraphShowNodeLabels     { get; init; }
    /// <summary><see langword="true"/> for a network graph that writes the weight on each edge; absent when it does
    /// not.</summary>
    public bool?                NetworkGraphShowEdgeWeights    { get; init; }
    /// <summary>The factor an edge's weight is multiplied by to get its line thickness in pixels; absent for 1.</summary>
    public double?              NetworkGraphEdgeThicknessScale { get; init; }
    /// <summary>The factor a node's size is multiplied by to get its radius in pixels; absent for 5.</summary>
    public double?              NetworkGraphNodeRadiusScale    { get; init; }
    /// <summary>The seed of the random starting positions of the force-directed layout, which makes the same graph give the
    /// same picture; absent for 0.</summary>
    public int?                 NetworkGraphLayoutSeed         { get; init; }
    /// <summary>The most iterations the force-directed layout makes; absent for the default number.</summary>
    public int?                 NetworkGraphLayoutIterations   { get; init; }
    /// <summary>The movement below which the force-directed layout stops before it has made all its iterations; absent for
    /// no early stop.</summary>
    public double?              NetworkGraphConvergenceThreshold { get; init; }

    // v1.12 — StateTimelineSeries
    /// <summary>The colour of each segment of a state timeline, one per segment in the order of <see cref="Starts"/> and
    /// <see cref="Ends"/>.</summary>
    public List<Color>? StateSegmentColors { get; init; }

    // v1.14 — per-segment hatch: a band that says "no information" rather than a state.
    /// <summary>The hatch of each segment of a state timeline, one per segment; written only when at least one segment is
    /// hatched.</summary>
    public List<HatchPattern>? StateSegmentHatches { get; init; }

    // The condition per band, decomposed the same way the colours and hatches are. Null when no band names one.
    /// <summary>The severity of the condition each segment of a state timeline was judged to be in, one per segment; written
    /// only when at least one segment names one.</summary>
    public List<OpsSeverity>?   StateSegmentSeverities  { get; init; }
    /// <summary>Whether the source of each segment of a state timeline was observed, one per segment; written only when at
    /// least one segment names a visibility.</summary>
    public List<OpsVisibility>? StateSegmentVisibilities { get; init; }

    // v1.14 — threshold bands. Rendered by GaugeSeries since v1.9 but never serialized: a gauge sent over the
    // wire came back with its bands silently replaced by the defaults. Decomposed into parallel arrays, the
    // same shape StateTimelineSeries already uses for its segments. Named NEUTRALLY, not GaugeBand*: the
    // bullet graph carries the same (threshold, colour) bands, and two names for one shape is a fork.
    /// <summary>The upper limit of each coloured band of a gauge or a bullet graph, one per band. Absent when no bands were
    /// set; an empty array means no bands at all.</summary>
    public double[]?     BandThresholds { get; init; }
    /// <summary>The colour of each band of a gauge or a bullet graph, in the order of
    /// <see cref="BandThresholds"/>.</summary>
    public List<Color>?  BandColors     { get; init; }

    // v1.14 — StatTileSeries.Format. Rendered since v1.12 but never serialized: a tile restored from the wire
    // read "0.3" where it had read "0.3 s".
    /// <summary>The numeric format of a stat tile's headline number, with its unit; left out at the default
    /// format.</summary>
    public string?       TileFormat { get; init; }

    /// <summary>The level each step of a waterfall chart is compared with, one per step (a missing level is a step with no mark);
    /// left out when the chart has no targets.</summary>
    public double[]? Targets { get; init; }
    /// <summary>The colour of the target marks of a waterfall chart; left out for the theme's text colour.</summary>
    public Color? TargetColor { get; init; }

    // v1.14 — BulletGraphSeries.Target (the comparative tick).
    /// <summary>The level a bullet graph or a stat tile measures its value against; absent for none.</summary>
    public double?       BulletTarget { get; init; }

    // StatTileSeries.Url / Expanded — a tile that leads somewhere. Both null at the default so an unlinked
    // tile adds no bytes and every golden stays byte-identical.
    /// <summary>The address a stat tile leads to; absent for a tile without a link.</summary>
    public string?       TileUrl      { get; init; }
    /// <summary><see langword="true"/> when what a stat tile's link leads to is currently open; absent otherwise.</summary>
    public bool?         TileExpanded { get; init; }

    // The condition a tile or a band was judged to be in. Null at rest so a chart that never names one writes
    // exactly the bytes it wrote before the axis existed — a tile whose state is dropped on the wire comes back
    // drawn as quiet, which is the one failure that turns an alarm into calm.
    /// <summary>The severity of the condition a stat tile was judged to be in; absent for normal.</summary>
    public OpsSeverity?   ConditionSeverity   { get; init; }
    /// <summary>Whether the source of a stat tile is observed or has gone silent; absent for observed.</summary>
    public OpsVisibility? ConditionVisibility { get; init; }

    // v1.14 — hatch fills. NULLABLE by necessity: SeriesDto omits nulls
    // (DefaultIgnoreCondition.WhenWritingNull), so an unset hatch adds no bytes and the golden corpus of
    // every existing discriminator stays byte-identical. A non-nullable enum would always serialize and
    // would rewrite all 76 goldens for nothing.
    /// <summary>The hatch pattern drawn over the series' fill; absent for a plain fill.</summary>
    public HatchPattern? Hatch      { get; init; }
    /// <summary>The colour of the hatch strokes; absent for a colour that contrasts with the fill.</summary>
    public Color?        HatchColor { get; init; }

    /// <summary>The colour a bar's or an area's fill blends to at its tip; left out for a flat fill, which is every document
    /// written before the gradient existed.</summary>
    public Color?        GradientColor { get; init; }

    // v1.11 — RelativeRotationSeries
    /// <summary>The closing prices of each asset of a relative rotation graph, one list per asset
    /// (<see cref="Models.Series.RelativeRotationSeries.AssetCloses"/>).</summary>
    public List<List<double>>?  RrgAssetCloses       { get; init; }
    /// <summary>The closing prices of the benchmark every asset of a relative rotation graph is measured against
    /// (<see cref="Models.Series.RelativeRotationSeries.BenchmarkCloses"/>).</summary>
    public List<double>?        RrgBenchmarkCloses   { get; init; }
    /// <summary>The label of each asset of a relative rotation graph, in the order of <see cref="RrgAssetCloses"/>
    /// (<see cref="Models.Series.RelativeRotationSeries.AssetLabels"/>).</summary>
    public string[]?            RrgAssetLabels       { get; init; }
    /// <summary>The model a relative rotation graph computes its ratio and momentum with, as the name of the formula
    /// (<see cref="Models.Series.RelativeRotationSeries.Formula"/>); absent for the dual EMA.</summary>
    public string?              RrgFormula           { get; init; }   // enum name; null = DualEma (default)
    /// <summary>The short period of a relative rotation graph's formula
    /// (<see cref="Models.Series.RelativeRotationSeries.ShortPeriod"/>); absent for 10.</summary>
    public int?                 RrgShortPeriod       { get; init; }
    /// <summary>The long period of a relative rotation graph's formula
    /// (<see cref="Models.Series.RelativeRotationSeries.LongPeriod"/>); absent for 26.</summary>
    public int?                 RrgLongPeriod        { get; init; }
    /// <summary>The lookback of the momentum of a relative rotation graph
    /// (<see cref="Models.Series.RelativeRotationSeries.MomentumLookback"/>); absent for 10.</summary>
    public int?                 RrgMomentumLookback  { get; init; }
    /// <summary>The number of recent periods a relative rotation graph draws as the tail of each asset
    /// (<see cref="Models.Series.RelativeRotationSeries.TailLength"/>); absent for 8.</summary>
    public int?                 RrgTailLength        { get; init; }
    /// <summary><see langword="false"/> for a relative rotation graph drawn without its centre crosshair and quadrant fills
    /// (<see cref="Models.Series.RelativeRotationSeries.ShowQuadrantGrid"/>); absent when it draws them.</summary>
    public bool?                RrgShowQuadrantGrid  { get; init; }
    /// <summary>The absorption ratio at each bar, which colours the dots of the tails
    /// (<see cref="Models.Series.RelativeRotationSeries.AbsorptionRatioPerBar"/>); absent for dots of one colour.</summary>
    public List<double>?        RrgAbsorptionPerBar  { get; init; }
    /// <summary>The effective number of bets at each bar, which sizes the dots of the tails
    /// (<see cref="Models.Series.RelativeRotationSeries.EnbPerBar"/>); absent for dots of one size.</summary>
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

    /// <summary><see langword="true"/> for a series that wears the theme's neutral grey in place of a colour of its own
    /// (<see cref="Models.Series.ChartSeries.WearsNeutral"/>): a dumbbell's connector, the outline of a map. Absent for every other
    /// series. A series that names a colour wears that colour, and a JSON written before this field carries the grey as that colour.</summary>
    public bool? WearsNeutral { get; set; }

    /// <summary>What a series of another package writes of itself beyond the fields above, for the discriminator that package
    /// registers a reader for (<see cref="SeriesRegistry.Register"/>): a JSON value this library carries and does not read. Absent
    /// for every series of this package, and for one whose package writes none. A figure holding it, read by a build of this
    /// library older than the field, loses it silently.</summary>
    public JsonElement? PackageData { get; init; }

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
    /// <summary>The most points a streaming series keeps; when it is full the oldest point is dropped to make room for a new
    /// one.</summary>
    public int? Capacity { get; init; }

    // The author's key for each point of an XY series (XYSeries.RowKeys), one string for each; absent when the series has none.
    /// <summary>The author's key for each point of an x/y series (<see cref="Models.Series.XYSeries.RowKeys"/>), one string
    /// per point; absent for a series that has none.</summary>
    public string[]? RowKeys { get; set; }

    // Polar series data (JSON round-trip): R and Theta are their own fields, not XData/YData, since a polar
    // series' two coordinates are a radius and an angle, not a rectangular X and Y.
    /// <summary>The radius of each point of a polar series.</summary>
    public double[]? RData { get; init; }
    /// <summary>The angle of each point of a polar series, in radians.</summary>
    public double[]? ThetaData { get; init; }

    // Ternary series data (JSON round-trip): the three parts of every sample as given (not closed), named by their corner:
    // the part at the apex, at the bottom left and at the bottom right.
    /// <summary>The part at the apex of each sample of a ternary series, as given and not closed to a sum.</summary>
    public double[]? TopData { get; init; }
    /// <summary>The part at the bottom left corner of each sample of a ternary series, as given and not closed to a
    /// sum.</summary>
    public double[]? LeftData { get; init; }
    /// <summary>The part at the bottom right corner of each sample of a ternary series, as given and not closed to a
    /// sum.</summary>
    public double[]? RightData { get; init; }

    // PiperSeries data (JSON round-trip): the waters, in mg/L; the colours and the marker size travel in Colors and MarkerSize.
    /// <summary>The waters of a Piper diagram, one per sample, with the concentration of each ion in mg/L.</summary>
    public List<WaterSampleDto>? WaterSamples { get; init; }

    // ChordSeries data (JSON round-trip): the group names travel in Categories, the opacity of the ribbons in Alpha and the
    // colours in Colors; the flow matrix and the gap between arcs are their own.
    /// <summary>The flow matrix of a chord diagram as a list of rows: the entry at row <c>i</c>, column <c>j</c> is the flow
    /// from group <c>i</c> to group <c>j</c>.</summary>
    public List<List<double>>? ChordFlows { get; init; }
    /// <summary>The gap between the arcs of a chord diagram's groups; absent for the default gap.</summary>
    public double? ChordPadding { get; init; }

    // WordCloudSeries data (JSON round-trip): the words travel in Categories, their weights in Values and the colours in Colors;
    // the size range of the words is its own.
    /// <summary>The font size of the lightest word of a word cloud; absent for the default.</summary>
    public double? WordMinSize { get; init; }
    /// <summary>The font size of the heaviest word of a word cloud; absent for the default.</summary>
    public double? WordMaxSize { get; init; }

    // TreeGridSeries data (JSON round-trip): the rows themselves — the caller's own flattening of whatever
    // hierarchy it drew, kept as a flat list the same way the series already holds it.
    /// <summary>The rows of a tree grid in the order they are drawn, as the caller flattened them.</summary>
    public List<TreeGridRowDto>? TreeGridRows { get; init; }

    // Hierarchical tree data (JSON round-trip): TreemapSeries, SunburstSeries and DendrogramSeries each hold one
    // TreeNode root (HierarchicalSeries.Root); ClustermapSeries holds two (RowTree, ColumnTree) alongside its
    // already-carried matrix. One recursive TreeNodeDto (ChartSerializer.TreeNodeToDto/DtoToTreeNode) serves all
    // four — the node shape does not change with how many of them a series holds.
    /// <summary>The root of the tree of a treemap, a sunburst, an icicle or a dendrogram.</summary>
    public TreeNodeDto? TreeRoot { get; init; }
    /// <summary>The tree that orders and groups the rows of a clustermap; absent for none.</summary>
    public TreeNodeDto? RowTree { get; init; }
    /// <summary>The tree that orders and groups the columns of a clustermap; absent for none.</summary>
    public TreeNodeDto? ColumnTree { get; init; }

    // SankeySeries data (JSON round-trip): the nodes and the links between them, by index — the same shape
    // SankeyNode/SankeyLink already hold.
    /// <summary>The nodes of a Sankey diagram.</summary>
    public List<SankeyNodeDto>? SankeyNodes { get; init; }
    /// <summary>The links of a Sankey diagram, each naming its source and its target node by their index in
    /// <see cref="SankeyNodes"/>.</summary>
    public List<SankeyLinkDto>? SankeyLinks { get; init; }
}

/// <summary>DTO for one <see cref="Models.TreeNode"/>, recursively — see
/// <see cref="TreeNodeSerializationExtensions"/>.</summary>
public sealed record TreeNodeDto
{
    /// <summary>The node's name (<see cref="Models.TreeNode.Label"/>).</summary>
    public string Label { get; init; } = "";
    /// <summary>The node's value (<see cref="Models.TreeNode.Value"/>): its area in a treemap, its size in a sunburst, and
    /// for an inner node of a dendrogram its merge distance.</summary>
    public double Value { get; init; }
    /// <summary>The node's own colour (<see cref="Models.TreeNode.Color"/>); absent for the colour the series gives
    /// it.</summary>
    public Color? Color { get; init; }
    /// <summary>The second value the node's colour is read from, through the series' normalizer and colour map
    /// (<see cref="Models.TreeNode.ColorValue"/>); absent for none.</summary>
    public double? ColorValue { get; init; }
    /// <summary>The number or short text a treemap cell shows under the node's name
    /// (<see cref="Models.TreeNode.Headline"/>); absent for none.</summary>
    public string? Headline { get; init; }
    /// <summary>The address the node's rectangle links to (<see cref="Models.TreeNode.Url"/>); absent for none.</summary>
    public string? Url { get; init; }
    /// <summary>Whether the subtree the node leads to is currently shown (<see cref="Models.TreeNode.Expanded"/>); absent
    /// for a node that discloses nothing.</summary>
    public bool? Expanded { get; init; }
    /// <summary>The fill pattern over the node's rectangle (<see cref="Models.TreeNode.Hatch"/>);
    /// <see cref="HatchPattern.None"/> for a plain fill.</summary>
    public HatchPattern Hatch { get; init; } = HatchPattern.None;
    /// <summary>The colour of the hatch strokes (<see cref="Models.TreeNode.HatchColor"/>); absent for a colour that
    /// contrasts with the fill.</summary>
    public Color? HatchColor { get; init; }
    /// <summary>The node's children, each a node in turn (<see cref="Models.TreeNode.Children"/>); written only for a node
    /// that has some.</summary>
    public List<TreeNodeDto>? Children { get; init; }
}

/// <summary>DTO for one <see cref="Models.Series.WaterSample"/>: its name and the concentration of each ion in mg/L, with an
/// ion that is not there left out.</summary>
public sealed record WaterSampleDto
{
    /// <summary>The name of the sample as the legend and the table show it (<see cref="Models.Series.WaterSample.Label"/>);
    /// absent when it has none.</summary>
    public string? Label { get; init; }
    /// <summary>The concentration of calcium in mg/L (<see cref="Models.Series.WaterSample.Ca"/>); left out when the sample
    /// has none.</summary>
    public double? Ca { get; init; }
    /// <summary>The concentration of magnesium in mg/L (<see cref="Models.Series.WaterSample.Mg"/>); left out when the
    /// sample has none.</summary>
    public double? Mg { get; init; }
    /// <summary>The concentration of sodium in mg/L (<see cref="Models.Series.WaterSample.Na"/>); left out when the sample
    /// has none.</summary>
    public double? Na { get; init; }
    /// <summary>The concentration of potassium in mg/L (<see cref="Models.Series.WaterSample.K"/>), which the diagram adds
    /// to sodium; left out when the sample has none.</summary>
    public double? K { get; init; }
    // The camel-case policy would write these as hcO3, cO3 and sO4; the wire names are the ions' own lower-case symbols.
    /// <summary>The concentration of bicarbonate in mg/L (<see cref="Models.Series.WaterSample.HCO3"/>); left out when the
    /// sample has none. Written as <c>hco3</c>.</summary>
    [JsonPropertyName("hco3")] public double? HCO3 { get; init; }
    /// <summary>The concentration of carbonate in mg/L (<see cref="Models.Series.WaterSample.CO3"/>), which the diagram adds
    /// to bicarbonate; left out when the sample has none. Written as <c>co3</c>.</summary>
    [JsonPropertyName("co3")] public double? CO3 { get; init; }
    /// <summary>The concentration of chloride in mg/L (<see cref="Models.Series.WaterSample.Cl"/>); left out when the sample
    /// has none.</summary>
    public double? Cl { get; init; }
    /// <summary>The concentration of sulphate in mg/L (<see cref="Models.Series.WaterSample.SO4"/>); left out when the
    /// sample has none. Written as <c>so4</c>.</summary>
    [JsonPropertyName("so4")] public double? SO4 { get; init; }

    /// <summary>The DTO of <paramref name="sample"/>: every ion that is present, none that is zero.</summary>
    /// <param name="sample">The water.</param>
    internal static WaterSampleDto From(Models.Series.WaterSample sample) => new()
    {
        Label = sample.Label,
        Ca = sample.Ca.OrNullAtRest(0.0), Mg = sample.Mg.OrNullAtRest(0.0), Na = sample.Na.OrNullAtRest(0.0), K = sample.K.OrNullAtRest(0.0),
        HCO3 = sample.HCO3.OrNullAtRest(0.0), CO3 = sample.CO3.OrNullAtRest(0.0), Cl = sample.Cl.OrNullAtRest(0.0), SO4 = sample.SO4.OrNullAtRest(0.0),
    };

    /// <summary>The water this DTO stands for: an ion it leaves out is zero.</summary>
    internal Models.Series.WaterSample ToWaterSample() => new()
    {
        Label = Label,
        Ca = Ca ?? 0, Mg = Mg ?? 0, Na = Na ?? 0, K = K ?? 0, HCO3 = HCO3 ?? 0, CO3 = CO3 ?? 0, Cl = Cl ?? 0, SO4 = SO4 ?? 0,
    };
}

/// <summary>DTO for one <see cref="Models.Series.TreeGridRow"/>.</summary>
public sealed record TreeGridRowDto
{
    /// <summary>The row's name, drawn at the left and indented by its depth
    /// (<see cref="Models.Series.TreeGridRow.Label"/>).</summary>
    public string Label { get; init; } = "";
    /// <summary>The row's values, one per column (<see cref="Models.Series.TreeGridRow.Cells"/>).</summary>
    public List<string>? Cells { get; init; }
    /// <summary>How deep the row sits, 0 being the root (<see cref="Models.Series.TreeGridRow.Depth"/>).</summary>
    public int Depth { get; init; }
    /// <summary>Where the row leads (<see cref="Models.Series.TreeGridRow.Url"/>); absent for a row that is plain
    /// text.</summary>
    public string? Url { get; init; }
    /// <summary>Whether the subtree under the row is shown (<see cref="Models.Series.TreeGridRow.Expanded"/>); absent for a
    /// leaf, which discloses nothing.</summary>
    public bool? Expanded { get; init; }
    /// <summary>The accent colour of the row's text (<see cref="Models.Series.TreeGridRow.Accent"/>); absent for
    /// none.</summary>
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
