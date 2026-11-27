// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>JSON-serializable representation of a <see cref="Models.Figure"/>.</summary>
internal sealed record FigureDto
{
    public string? Title { get; init; }
    /// <summary>The style of the figure's title; absent when it has none.</summary>
    public TextStyleDto? TitleStyle { get; init; }
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
    /// <summary>The one legend of the figure as a whole that a JSON of an earlier release carries; read as the first of the
    /// figure's legends, never written (<see cref="Legends"/> holds them).</summary>
    public LegendDto? Legend { get; init; }
    /// <summary>The legends of the figure as a whole, in the order they were added, each listing the labelled series of every
    /// subplot; absent when the figure has none. An empty object is a legend at its defaults (which draws).</summary>
    public List<LegendDto>? Legends { get; init; }
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

    /// <summary>The settings of a new figure: what a figure setting is left out at, and read back as.</summary>
    private static readonly Figure RestingFigure = new();

    /// <summary>The JSON's form of <paramref name="figure"/>: its own settings (each left out at a new figure's value; the
    /// theme left out while it is the default theme itself), then its subplots, each with the cell of a rows-by-columns grid it
    /// was added to (<see cref="SubplotCellDto.Of"/>).</summary>
    /// <param name="figure">The figure written.</param>
    /// <returns>The record of the figure.</returns>
    internal static FigureDto Of(Figure figure) => new()
    {
        Title = figure.Title,
        TitleStyle = TextStyleDto.From(figure.TitleStyle),
        AltText = figure.AltText,
        Description = figure.Description,
        Width = figure.Width,
        Height = figure.Height,
        Dpi = figure.Dpi,
        BackgroundColor = figure.BackgroundColor,
        GridSpec = figure.GridSpec is { } gs ? GridSpecDto.Of(gs) : null,
        SubPlots = [.. AxesDto.ListOf(figure.SubPlots, figure.Theme).Select((dto, i) => (dto with { Subplot = SubplotCellDto.Of(figure.SubPlots[i], figure.SubPlots.Count) })
            .WithSharesByPlace(figure.SubPlots[i], figure.SubPlots))],
        Enable3DRotation = figure.Enable3DRotation.OrNullAtRest(RestingFigure.Enable3DRotation),
        FigureColorBar = ColorBarDto.From(figure.FigureColorBar),
        Legends = figure.Legends.Count > 0 ? [.. figure.Legends.Select(legend => LegendDto.FromPresent(legend)!)] : null,
        Texts = figure.Texts.Count > 0 ? [.. figure.Texts.Select(FigureTextDto.From)] : null,
        Images = figure.Images.Count > 0 ? [.. figure.Images.Select(FigureImageDto.From)] : null,
        Theme = ReferenceEquals(figure.Theme, RestingFigure.Theme) ? null : ThemeDto.From(figure.Theme),
        Spacing = figure.Spacing.DescribedUnlessAtRest(RestingFigure.Spacing, SubPlotSpacingDto.From),
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
        InteractionTheme = figure.InteractionTheme.DescribedUnlessAtRest(RestingFigure.InteractionTheme, InteractionThemeDto.From),
        ResponsiveSvg = figure.ResponsiveSvg.OrNullAtRest(RestingFigure.ResponsiveSvg),
    };

    /// <summary>The figure this record describes. A setting it leaves out reads as a new figure's; each subplot is added
    /// where it was written (<see cref="AddSubPlotTo"/>) and read back (<see cref="AxesDto.RestoreOnto"/>), and the subplots
    /// that share an axis are joined by their keys, or by their places when the partner has none, once all are read.</summary>
    /// <returns>The figure of the record.</returns>
    /// <exception cref="ArgumentException">The record's layout rect is not four fractions of the figure that make a rectangle, or a colour map
    /// description in it cannot make a map.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A subplot's <c>yAxisTitleGap</c> is negative, NaN or infinite.</exception>
    internal Figure ToFigure()
    {
        var figure = new Figure
        {
            Title = Title,
            TitleStyle = TitleStyle?.ToTextStyle(),
            AltText = AltText,
            Description = Description,
            Width = Width,
            Height = Height,
            Dpi = Dpi ?? RestingFigure.Dpi,
            BackgroundColor = BackgroundColor,
            Enable3DRotation = Enable3DRotation ?? RestingFigure.Enable3DRotation,
            FigureColorBar = FigureColorBar?.ToColorBar(),
            Theme = Theme?.ToTheme() ?? RestingFigure.Theme,
            Spacing = Spacing?.ToSpacing() ?? RestingFigure.Spacing,
            ChartId = ChartId,
            ServerInteraction = ServerInteraction ?? RestingFigure.ServerInteraction,
            EnableZoomPan = EnableZoomPan ?? RestingFigure.EnableZoomPan,
            EnableLegendToggle = EnableLegendToggle ?? RestingFigure.EnableLegendToggle,
            EnableRichTooltips = EnableRichTooltips ?? RestingFigure.EnableRichTooltips,
            EnableHighlight = EnableHighlight ?? RestingFigure.EnableHighlight,
            EnableSelection = EnableSelection ?? RestingFigure.EnableSelection,
            SelectionGroup = SelectionGroup,
            SelectedRows = SelectedRows,
            EnableDataCursor = EnableDataCursor ?? RestingFigure.EnableDataCursor,
            EnableViewerCursors = EnableViewerCursors ?? RestingFigure.EnableViewerCursors,
            EnableTreemapDrilldown = EnableTreemapDrilldown ?? RestingFigure.EnableTreemapDrilldown,
            EnableSankeyHover = EnableSankeyHover ?? RestingFigure.EnableSankeyHover,
            InteractionTheme = InteractionTheme?.ToInteractionTheme() ?? RestingFigure.InteractionTheme,
            ResponsiveSvg = ResponsiveSvg ?? RestingFigure.ResponsiveSvg,
        };

        if (GridSpec is { } gsDto)
            figure.GridSpec = gsDto.ToGridSpec();

        if (Legend is { } older)
            figure.AddLegend(older.ToLegend());
        foreach (var legendDto in Legends ?? [])
            figure.AddLegend(legendDto.ToLegend());
        foreach (var textDto in Texts ?? [])
            textDto.AddTo(figure);
        foreach (var imageDto in Images ?? [])
            imageDto.AddTo(figure);

        foreach (var axDto in SubPlots ?? [])
            axDto.RestoreOnto(AddSubPlotTo(figure, axDto));

        // A subplot that names the key, or the place, of another to share a scale with is joined to it once every subplot stands.
        figure.ShareByKeys();
        for (int i = 0; i < (SubPlots?.Count ?? 0); i++)
            figure.ShareByPlace(figure.SubPlots[i], SubPlots![i].ShareXSubplot, SubPlots[i].ShareYSubplot);

        return figure;
    }

    /// <summary>Adds the subplot <paramref name="axDto"/> describes to <paramref name="figure"/>, placed as it was written: at its
    /// cells of a grid specification (<c>gridPosition</c>, kept whether or not the figure has the specification, as the writer
    /// writes it either way), in its cell of a rows-by-columns grid (<c>subplot</c>), or nowhere in particular.</summary>
    private static Axes AddSubPlotTo(Figure figure, AxesDto axDto)
    {
        if (axDto.GridPosition is { } gpDto)
        {
            var axes = figure.AddSubPlot();
            axes.GridPosition = gpDto.ToGridPosition();
            return axes;
        }

        return axDto.Subplot?.AddTo(figure) ?? figure.AddSubPlot();
    }
}
