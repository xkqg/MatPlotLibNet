// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Rendering.Lighting;
using System.Globalization;

namespace MatPlotLibNet.Serialization;

/// <summary>JSON-serializable representation of an <see cref="Models.Axes"/> subplot, including series, annotations, and decorations.</summary>
internal sealed record AxesDto
{
    public string? Title { get; init; }
    public string? Key { get; init; }
    public string? ShareXKey { get; init; }
    public string? ShareYKey { get; init; }
    /// <summary>The place among the figure's subplots (0 the first) of the subplot whose X scale this one shares, written when that
    /// subplot has no key to name it by (<c>Figure.AddSubPlot(..., sharex)</c>, matplotlib's <c>add_subplot(sharex=ax)</c>); absent
    /// when it has one (<see cref="ShareXKey"/>) or there is none. A 1.29.0 reader skips it.</summary>
    public int? ShareXSubplot { get; init; }
    /// <summary>The place among the figure's subplots of the subplot whose Y scale this one shares, when that subplot has no key.</summary>
    public int? ShareYSubplot { get; init; }
    public GridPositionDto? GridPosition { get; init; }
    /// <summary>The cell of a rows-by-columns grid the subplot was added to with <c>AddSubPlot(rows, cols, index)</c>; absent
    /// for a subplot placed another way, and for a figure whose one subplot stands in the one cell of a 1 by 1 grid. A 1.28.0
    /// reader skips it.</summary>
    public SubplotCellDto? Subplot { get; init; }
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
    /// <summary>The linked X axes below the plot (<see cref="YAxisContainer.Bottom"/>), in stack order, each with its axis, its
    /// link and its location (<see cref="XAxisSlotDto"/>: no series, which a bottom slot does not hold). Absent when there are none.</summary>
    public List<XAxisSlotDto>? BottomXAxes { get; init; }
    /// <summary>The blank gap in pixels between every Y axis title and its tick labels, and between two stacked Y axes
    /// (<see cref="YAxisContainer.TitleGap"/>). Absent at <see cref="YAxisContainer.RestingTitleGap"/>, which is what a JSON without it
    /// reads as; a value that is negative, NaN or infinite is refused.</summary>
    public double? YAxisTitleGap { get; init; }
    public List<AnnotationDto>? Annotations { get; init; }
    public List<ReferenceLineDto>? ReferenceLines { get; init; }
    public List<SpanRegionDto>? Spans { get; init; }
    /// <summary>The patches (shapes in data coordinates), in the order they were added, each told apart by its <c>type</c>
    /// (<see cref="PatchDto"/>); absent when there are none. A 1.28.0 reader skips it.</summary>
    public List<PatchDto>? Patches { get; init; }
    public List<TrendlineDto>? Trendlines { get; init; }
    public List<HorizontalLevelDto>? HorizontalLevels { get; init; }
    public List<FibonacciRetracementDto>? FibonacciRetracements { get; init; }
    /// <summary>The buy and sell signals, in the order they were added; absent when there are none. A 1.28.0 reader skips it.</summary>
    public List<SignalMarkerDto>? Signals { get; init; }
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
    /// <summary>Cartesian, polar, three-D or ternary; absent for the default, <see cref="Models.Axes.RestingCoordinateSystem"/>. A name this reader does not
    /// know is announced on <see cref="Diagnostics.ChartDiagnostics"/> and the axes is drawn as Cartesian.</summary>
    public string? CoordinateSystem { get; init; }

    /// <summary>The radius a polar axes draws at its centre (<see cref="Axes.ROrigin"/>); absent for its low end. A 1.29.0 reader
    /// skips it.</summary>
    public double? ROrigin { get; init; }

    /// <summary>The JSON's form of <paramref name="axes"/>. <paramref name="theme"/> is the figure's: a grid the axes changed
    /// over the theme's grid is written as that change (<see cref="GridChange"/>: the properties it set, with their values), so
    /// it is still "the theme's grid, but ..." when it is read and drawn under another theme. The grid it draws under the
    /// figure's theme is written beside it, marked as the axes' own (<see cref="GridOverridesTheme"/>), for a reader that
    /// predates the change: it draws that grid whatever the theme is, and a grid switched off stays off under a theme that
    /// draws one.</summary>
    /// <param name="axes">The axes written.</param>
    /// <param name="theme">The theme of the figure the axes is drawn in.</param>
    /// <returns>The record of the axes.</returns>
    internal static AxesDto Of(Axes axes, Theme theme)
    {
        var rightPast = YAxisSlotsPastSlot0Dto.Of(axes.YAxes.Right);
        var leftPast = YAxisSlotsPastSlot0Dto.Of(axes.YAxes.Left);
        return new()
        {
            Title = axes.Title,
            Key = axes.Key,
            ShareXKey = axes.ShareXWith?.Key,
            ShareYKey = axes.ShareYWith?.Key,
            GridPosition = axes.GridPosition is { } gp ? GridPositionDto.Of(gp) : null,
            Spines = axes.Spines == new SpinesConfig() ? null : SpinesConfigDto.Of(axes.Spines),
            XAxis = AxisDto.Of(axes.XAxis),
            YAxis = AxisDto.Of(axes.YAxis),
            ZAxis = AxisDto.Of(axes.ZAxis),
            Grid = GridDto.From(axes.GridChange is null ? axes.Grid : axes.GridUnder(theme)),
            GridOverridesTheme = (axes.GridChange is not null).OrNullAtRest(false),
            GridChange = axes.GridChange is { } gridChange ? GridChangeDto.From(gridChange) : null,
            BarMode = axes.BarMode.OrNullAtRest(Axes.RestingBarMode)?.ToWireName(),
            Series = axes.Series.ToDtos(),
            Annotations = axes.Annotations.Count > 0 ? axes.Annotations.Select(AnnotationDto.From).ToList() : null,
            ReferenceLines = axes.ReferenceLines.Count > 0 ? axes.ReferenceLines.Select(ReferenceLineDto.From).ToList() : null,
            SecondaryYAxis = axes.YAxes.Right.Count > 0 ? AxisDto.Of(axes.YAxes.Right[0].Axis) : null,
            SecondarySeries = axes.YAxes.Right is { Count: > 0 } rightStack && rightStack[0].Series.Count > 0 ? rightStack[0].Series.ToDtos() : null,
            SecondaryXAxis = axes.SecondaryXAxis is not null ? AxisDto.Of(axes.SecondaryXAxis) : null,
            XSecondarySeries = axes.XSecondarySeries.Count > 0 ? axes.XSecondarySeries.ToDtos() : null,
            ExtraRightYAxes = rightPast.Frozen,
            RightYAxesOverflow = rightPast.Overflow,
            LeftYAxes = leftPast.Frozen,
            LeftYAxesOverflow = leftPast.Overflow,
            BottomXAxes = axes.YAxes.BottomCount > 0 ? [.. axes.YAxes.Bottom.Select(XAxisSlotDto.Of)] : null,
            YAxisTitleGap = axes.YAxes.GivenTitleGap,
            Spans = axes.Spans.Count > 0 ? axes.Spans.Select(SpanRegionDto.From).ToList() : null,
            Patches = axes.Patches.Count > 0 ? PatchesOf(axes) : null,
            Trendlines = axes.Trendlines.Count > 0 ? axes.Trendlines.Select(TrendlineDto.From).ToList() : null,
            HorizontalLevels = axes.HorizontalLevels.Count > 0 ? axes.HorizontalLevels.Select(HorizontalLevelDto.From).ToList() : null,
            FibonacciRetracements = axes.FibonacciRetracements.Count > 0 ? axes.FibonacciRetracements.Select(FibonacciRetracementDto.From).ToList() : null,
            Signals = axes.Signals.Count > 0 ? [.. axes.Signals.Select(SignalMarkerDto.From)] : null,
            InsetBounds = axes.InsetBounds is { } ib ? InsetBoundsDto.Of(ib) : null,
            Insets = axes.Insets.Count > 0 ? ListOf(axes.Insets, theme) : null,
            ZoomIndicator = ZoomIndicatorDto.From(axes.ZoomIndicator),
            Elevation = axes.Elevation.OrNullAtRest(Camera3D.RestingElevation),
            Azimuth = axes.Azimuth.OrNullAtRest(Camera3D.RestingAzimuth),
            CameraDistance = axes.CameraDistance,
            Aspect = axes.Aspect,
            LightSourceType = OnTheWire(axes.LightSource),
            XBreaks = axes.XBreaks.Count > 0 ? axes.XBreaks.Select(AxisBreakDto.Of).ToList() : null,
            YBreaks = axes.YBreaks.Count > 0 ? axes.YBreaks.Select(AxisBreakDto.Of).ToList() : null,
            TitleStyle = TextStyleDto.From(axes.TitleStyle),
            TitleLoc = axes.TitleLoc.OrNullAtRest(Axes.RestingTitleLoc),
            Legend = LegendDto.From(axes.Legend),
            ColorBar = ColorBarDto.From(axes.ColorBar),
            Pane3D = Pane3DConfigDto.From(axes.Pane3D),
            EnableTooltips = axes.EnableTooltips.OrNullAtRest(false),
            EnableInteractiveAttributes = axes.EnableInteractiveAttributes.OrNullAtRest(false),
            Emit3DVertexData = axes.Emit3DVertexData.OrNullAtRest(false),
            CoordinateSystem = axes.CoordinateSystem.OrNullAtRest(Axes.RestingCoordinateSystem)?.ToWireName(),
            ROrigin = axes.ROrigin,
        };
    }

    /// <summary>The patches of <paramref name="axes"/>, each with how many of the axes' own series stand before it in the legend
    /// when that is not all of them (a patch added after every series writes no place).</summary>
    /// <param name="axes">The axes written.</param>
    private static List<PatchDto> PatchesOf(Axes axes)
    {
        var patches = new List<PatchDto>(axes.Patches.Count);
        for (int i = 0; i < axes.Patches.Count; i++)
        {
            var dto = axes.Patches[i].Describe();
            int before = axes.Overlays.SeriesBeforePatch(i);
            patches.Add(before < axes.Series.Count ? dto with { SeriesBefore = before } : dto);
        }

        return patches;
    }

    /// <summary>The ONE place a list of axes is written: the figure's subplots and an axes' insets, each through
    /// <see cref="Of"/>.</summary>
    /// <param name="axes">The axes written, in their order.</param>
    /// <param name="theme">The theme of the figure they are drawn in.</param>
    internal static List<AxesDto> ListOf(IEnumerable<Axes> axes, Theme theme) => axes.Select(a => Of(a, theme)).ToList();

    /// <summary>This record of <paramref name="axes"/>, one of <paramref name="subplots"/>, with the place of each subplot it shares a
    /// scale with that has no key to name it by (<see cref="ShareXSubplot"/>, <see cref="ShareYSubplot"/>).</summary>
    /// <param name="axes">The subplot this record was written from.</param>
    /// <param name="subplots">The figure's subplots, in their order.</param>
    /// <returns>The record with the places.</returns>
    internal AxesDto WithSharesByPlace(Axes axes, IReadOnlyList<Axes> subplots) => this with
    {
        ShareXSubplot = PlaceOfUnkeyed(axes.ShareXWith, subplots),
        ShareYSubplot = PlaceOfUnkeyed(axes.ShareYWith, subplots),
    };

    /// <summary>The place of <paramref name="partner"/> among <paramref name="subplots"/> when it has no key; <see langword="null"/>
    /// for none, for one with a key (<see cref="ShareXKey"/> names it) and for one that is not a subplot of the figure.</summary>
    private static int? PlaceOfUnkeyed(Axes? partner, IReadOnlyList<Axes> subplots)
    {
        if (partner is not { Key: null }) return null;
        for (int i = 0; i < subplots.Count; i++)
            if (ReferenceEquals(subplots[i], partner)) return i;
        return null;
    }

    /// <summary>The ONE place an axes is read back: a subplot and an inset alike, everything <see cref="Of"/> wrote (its axes,
    /// grid, spines, title style, legend, colour bar, series, overlays, secondary axes, insets and breaks).</summary>
    /// <param name="axes">The axes read into: a subplot or an inset already added to its figure or its parent.</param>
    /// <exception cref="ArgumentException">A colour map description in the record cannot make a map; the exception names the field.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><c>yAxisTitleGap</c> is negative, NaN or infinite.</exception>
    internal void RestoreOnto(Axes axes)
    {
        axes.Title = Title;
        axes.Key = Key;
        // The subplot of a key is joined once the figure holds every subplot (Figure.ShareByKeys).
        axes.ShareXKey = ShareXKey;
        axes.ShareYKey = ShareYKey;
        if (TitleStyle is { } titleStyle) axes.TitleStyle = titleStyle.ToTextStyle();
        if (TitleLoc is { } titleLoc) axes.TitleLoc = titleLoc;
        if (Legend is { } legend) axes.Legend = legend.ToLegend();
        if (ColorBar is { } colorBar) axes.ColorBar = colorBar.ToColorBar();
        axes.ZoomIndicator = ZoomIndicator?.ToZoomIndicator();

        XAxis?.ApplyTo(axes.XAxis);
        YAxis?.ApplyTo(axes.YAxis);
        ZAxis?.ApplyTo(axes.ZAxis);
        Pane3D?.AddTo(axes);
        if (EnableTooltips is { } enableTooltips) axes.EnableTooltips = enableTooltips;
        if (EnableInteractiveAttributes is { } enableInteractiveAttributes) axes.EnableInteractiveAttributes = enableInteractiveAttributes;
        if (Emit3DVertexData is { } emit3DVertexData) axes.Emit3DVertexData = emit3DVertexData;
        if (CoordinateSystem.WireEnum<Models.CoordinateSystem>() is { } coordinateSystem) axes.CoordinateSystem = coordinateSystem;
        if (ROrigin is { } rOrigin) axes.ROrigin = rOrigin;
        RestoreGrid(axes);
        if (Spines is { } spines) axes.Spines = spines.ToSpines();
        if (BarMode.WireEnum<Models.BarMode>() is { } barMode) axes.BarMode = barMode;
        RestoreCameraAndLighting(axes);

        foreach (var sDto in Series ?? [])
            axes.AddSeriesFromDto(sDto);

        foreach (var annotationDto in Annotations ?? [])
            annotationDto.AddTo(axes);
        foreach (var referenceLineDto in ReferenceLines ?? [])
            referenceLineDto.AddTo(axes);
        RestoreSecondaryAxes(axes);
        foreach (var spanDto in Spans ?? [])
            spanDto.AddTo(axes);
        foreach (var patchDto in Patches ?? [])
        {
            int count = axes.Patches.Count;
            patchDto.AddTo(axes);
            if (axes.Patches.Count > count && patchDto.SeriesBefore is { } before) axes.Overlays.PlaceLastPatchAfter(before, axes.Series.Count);
        }
        foreach (var trendlineDto in Trendlines ?? [])
            trendlineDto.AddTo(axes);
        foreach (var levelDto in HorizontalLevels ?? [])
            levelDto.AddTo(axes);
        foreach (var retracementDto in FibonacciRetracements ?? [])
            retracementDto.AddTo(axes);
        foreach (var signalDto in Signals ?? [])
            signalDto.AddTo(axes);
        RestoreInsets(axes);
        RestoreAxisBreaks(axes);
    }

    /// <summary>The 3-D camera (elevation/azimuth/distance) and its light source. Only the <c>"directional:"</c> wire format is
    /// read back; a light source written any other way is left at the axes' own default.</summary>
    private void RestoreCameraAndLighting(Axes axes)
    {
        if (Elevation is { } elevation) axes.Elevation = elevation;
        if (Azimuth is { } azimuth) axes.Azimuth = azimuth;
        if (CameraDistance is { } cameraDistance) axes.CameraDistance = cameraDistance;
        axes.Aspect = Aspect;
        if (LightFromTheWire(LightSourceType) is { } light) axes.LightSource = light;
    }

    /// <summary>Reads every Y axis past the primary back into the container, the container's title gap, and the top X axis.
    /// The frozen 1.23.0 fields keep their published meaning: <c>secondaryYAxis</c> is right slot 0 (the primary, left slot 0,
    /// is <c>yAxis</c>); the entries of <c>extraRightYAxes</c> and <c>leftYAxes</c> are the slots past slot 0 of their side, in
    /// outward order, and <c>rightYAxesOverflow</c> and <c>leftYAxesOverflow</c> continue that order.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><c>yAxisTitleGap</c> is negative, NaN or infinite.</exception>
    private void RestoreSecondaryAxes(Axes axes)
    {
        if (SecondaryYAxis is { } secondaryYAxis)
        {
            var first = axes.YAxes.Right.FirstOrAdd();
            secondaryYAxis.ApplyTo(first.Axis);
            foreach (var sDto in SecondarySeries ?? [])
                axes.CreateOnSecondarySeat(sDto, s => first.AddSeries(s));
        }

        new YAxisSlotsPastSlot0Dto(ExtraRightYAxes, RightYAxesOverflow).AddTo(axes, axes.YAxes.Right);
        new YAxisSlotsPastSlot0Dto(LeftYAxes, LeftYAxesOverflow).AddTo(axes, axes.YAxes.Left);
        foreach (var bottomDto in BottomXAxes ?? [])
            bottomDto.RestoreOnto(axes.YAxes.Bottom.Add());
        if (YAxisTitleGap is { } titleGap)
            axes.YAxes.TitleGap = YAxisContainer.ValidTitleGap(titleGap, "yAxisTitleGap");

        if (SecondaryXAxis is { } secondaryXAxis)
        {
            axes.TwinY();
            secondaryXAxis.ApplyTo(axes.SecondaryXAxis!);
            foreach (var sDto in XSecondarySeries ?? [])
                axes.CreateOnSecondarySeat(sDto, s => axes.AddXSecondarySeries(s));
        }
    }

    /// <summary>An inset is an axes: read back through <see cref="RestoreOnto"/> itself, so it keeps whatever its writer
    /// wrote. An inset without bounds is skipped.</summary>
    private void RestoreInsets(Axes axes)
    {
        foreach (var insetDto in Insets ?? [])
        {
            if (insetDto.InsetBounds is not { } ibDto) continue;
            insetDto.RestoreOnto(ibDto.AddInsetTo(axes));
        }
    }

    private void RestoreAxisBreaks(Axes axes)
    {
        Restore(XBreaks, isX: true);
        Restore(YBreaks, isX: false);

        // The breaks one axis wrote, read back in their order.
        void Restore(List<AxisBreakDto>? written, bool isX)
        {
            foreach (var bDto in written ?? [])
            {
                var axisBreak = bDto.ToAxisBreak();
                axes.AddBreak(isX, axisBreak.From, axisBreak.To, axisBreak.Style);
            }
        }
    }

    /// <summary>Reads an axes' grid back. A <see cref="GridChange"/> is the change the author made, and it is the only thing
    /// read as one: it is applied over the grid of the theme the figure is drawn under. A JSON written before changes were
    /// written has only the resolved grid: marked as the axes' own it is the grid the axes draws whatever the theme's grid is
    /// (a switched-off one included), so it comes back as the change that sets every property to what it has; any other grid
    /// comes back as it stands, and the renderer takes the theme's grid over one that is not switched on
    /// (<see cref="AxesGridExtensions.GridUnder"/>).</summary>
    private void RestoreGrid(Axes axes)
    {
        var grid = Grid?.ToGridStyle();
        if (grid is not null) axes.Grid = grid;

        if (GridChange is { } change) axes.GridChange = change.ToGridChange();
        else if (GridOverridesTheme is true && grid is not null) axes.GridChange = Styling.GridChange.AllOf(grid);
    }

    /// <summary>The kind a directional light is written under, before its numbers.</summary>
    private const string Directional = "directional:";

    /// <summary>What stands between two numbers of the light.</summary>
    private const char Separator = ',';

    /// <summary>How many numbers a directional light is written with.</summary>
    private const int Numbers = 5;

    /// <summary>The text the light is written as, or <see langword="null"/> for no light and a light of a kind no text names.</summary>
    /// <param name="light">The value it works on.</param>
    internal static string? OnTheWire(ILightSource? light) => light is DirectionalLight d
        ? string.Invariant(
            $"{Directional}{d.Dx}{Separator}{d.Dy}{Separator}{d.Dz}{Separator}{d.Ambient}{Separator}{d.Diffuse}")
        : null;

    /// <summary>The directional light the text names (its first five numbers when it carries more), or <see langword="null"/> when it
    /// names none: no text, another kind, fewer than five numbers or a part that is no number.</summary>
    /// <returns>The light, or <see langword="null"/>.</returns>
    /// <param name="text">The value it works on.</param>
    internal static DirectionalLight? LightFromTheWire(string? text)
    {
        if (text is null || !text.StartsWith(Directional, StringComparison.Ordinal)) return null;

        var parts = text[Directional.Length..].Split(Separator);
        if (parts.Length < Numbers) return null;

        Span<double> numbers = stackalloc double[Numbers];
        for (int i = 0; i < Numbers; i++)
        {
            if (!parts[i].TryReadInvariant(NumberStyles.Float, out numbers[i])) return null;
        }

        return new DirectionalLight(numbers[0], numbers[1], numbers[2], numbers[3], numbers[4]);
    }
}
