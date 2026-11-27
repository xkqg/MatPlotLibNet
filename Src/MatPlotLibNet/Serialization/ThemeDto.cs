// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a <see cref="Theme"/>. A theme is data (colours, a font, a grid, a cycle, a
/// spacing, margins, an alarm palette), so it is written property by property and a theme built with
/// <see cref="ThemeBuilder"/> comes back whole, not only the themes the library ships.
/// <para>Every property is measured against the plain theme, <see cref="Theme.Default"/>: it is written only where it
/// differs from that theme's, and read back over that theme. The plain theme leaves every optional property unset, so a
/// property that is unset is never written and always reads back unset. The name is a label like any other property:
/// it does not look a built-in theme up.</para></summary>
internal sealed record ThemeDto
{
    public string? Name { get; init; }
    public Color? Background { get; init; }
    public Color? ForegroundText { get; init; }
    public Color? AxesBackground { get; init; }
    public Color[]? CycleColors { get; init; }
    public FontDto? DefaultFont { get; init; }
    public GridDto? DefaultGrid { get; init; }
    public PropCyclerDto? PropCycler { get; init; }
    public SubPlotSpacingDto? DefaultSpacing { get; init; }
    /// <summary>The colour of a patch's edge (<see cref="Theme.PatchEdgeColor"/>, <see cref="PatchLook.EdgeColor"/>): one value, one key.</summary>
    public Color? PatchEdgeColor { get; set; }
    public Color? ViolinBodyColor { get; init; }
    public Color? ViolinStatsColor { get; init; }
    public double? AxisXMargin { get; init; }
    public double? AxisYMargin { get; init; }
    public double? ScatterMargin { get; set; }
    public double? ScatterArea { get; set; }
    public double? ScatterArea3D { get; set; }
    public Color? ScatterFace { get; set; }
    public double? ScatterEdgeWidth { get; set; }
    public Color? ScatterEdgeColor { get; set; }
    /// <summary>The least opacity a 3-D scatter marker fades to with its distance (<see cref="ScatterLook.DepthShade"/>).</summary>
    public double? ScatterDepthShade { get; set; }
    /// <summary>The look of a box plot (<see cref="BoxLook"/>), written whole; the library's own look writes no key.</summary>
    public BoxLookDto? Box { get; set; }
    public LegendPosition? DefaultLegendPosition { get; init; }
    public Color? Pane3DColor { get; init; }
    public GridDto? WallGrid { get; set; }
    public int? SurfaceStride { get; set; }
    public double? SurfaceAlpha { get; set; }
    public bool? SurfaceOutlined { get; set; }
    public double? SurfaceEdgeWidth { get; set; }
    public AutolimitMode? AutolimitMode { get; set; }
    public double? SymlogThreshold { get; set; }
    public double[]? AxisTickSteps { get; set; }
    public int? AxisTickBins { get; set; }
    public int? AxisTickMinTicks { get; set; }
    /// <summary>Whether the number of ticks follows the length of the axis (<see cref="TickLadder"/>).</summary>
    public bool? AxisTickBinsByLength { get; set; }
    /// <summary>The most powers of ten in view for which a log axis names multiples of a power (<see
    /// cref="LogLabelThresholds.Powers"/>).</summary>
    public double? LogLabelPowersThreshold { get; set; }
    /// <summary>The width of a log axis' view, in decades, above which only some multiples are named (<see
    /// cref="LogLabelThresholds.Decades"/>).</summary>
    public double? LogLabelDecadesThreshold { get; set; }
    public double? SpineLineWidth { get; set; }
    public double? CubeAxisLineWidth { get; set; }
    public double? SeriesLineWidth { get; set; }
    public bool? StrokesOnPixelGrid { get; set; }
    /// <summary>The dash and gap lengths of a dashed line (<see cref="DashLook.Dashed"/>).</summary>
    public double[]? DashedPattern { get; set; }
    /// <summary>The dash and gap lengths of a dotted line (<see cref="DashLook.Dotted"/>).</summary>
    public double[]? DottedPattern { get; set; }
    /// <summary>The dashes and gaps of a dash-dot line (<see cref="DashLook.DashDot"/>).</summary>
    public double[]? DashDotPattern { get; set; }
    /// <summary>Whether the lengths of a dash pattern grow with the line's width (<see cref="DashLook.ScalesWithWidth"/>).</summary>
    public bool? DashesScaleWithWidth { get; set; }
    /// <summary>How many pixels one unit of a dash pattern is (<see cref="DashLook.Unit"/>).</summary>
    public double? DashUnit { get; set; }
    /// <summary>How two segments of a stroke meet (<see cref="StrokeLook.Join"/>).</summary>
    public JoinStyle? StrokeJoin { get; set; }
    public bool? MinorGridLikeMajor { get; set; }
    /// <summary>Whether the minor ticks are the ones matplotlib's minor locator places (<see
    /// cref="AxisTickLook.MinorsByLocator"/>).</summary>
    public bool? MinorTicksByLocator { get; set; }
    /// <summary>Whether the patches of an axes widen its range as matplotlib's <c>add_patch</c> does (<see
    /// cref="AxisRangeLook.PatchesLikeAddPatch"/>).</summary>
    public bool? PatchesRangeLikeAddPatch { get; set; }
    /// <summary>Whether a figure legend lists every entry of the axes it gathers (<see
    /// cref="FigureLook.LegendListsEveryEntry"/>).</summary>
    public bool? FigureLegendListsEveryEntry { get; set; }
    /// <summary>Where the grid stands among what an axes draws, as a z-order (<see cref="AxesFrameLook.AxisZOrder"/>).</summary>
    public double? AxisZOrder { get; set; }
    public bool? NoPadAboveTitle { get; set; }
    /// <summary>The pad a tight layout keeps at each edge, as a multiple of the font size (<see cref="FigureLook.TightPad"/>).</summary>
    public double? TightLayoutPad { get; set; }
    public bool? NoBackgroundWithoutAxes { get; set; }
    /// <summary>Whether every mark of an axes is cut off at its plot, limit or none (<see cref="AxesFrameLook.ClipsEveryArtist"/>).</summary>
    public bool? ClipsEveryArtist { get; set; }
    public double? TitleScale { get; set; }
    public int? OffsetThreshold { get; set; }
    public TickDirection? TickDirection { get; set; }
    public double? TickLength { get; set; }
    public double? TickWidth { get; set; }
    public double? TickPad { get; set; }
    /// <summary>The width of a minor tick mark, in pixels (<see cref="AxisTickLook.MinorWidth"/>).</summary>
    public double? MinorTickWidth { get; set; }
    /// <summary>The gap between a minor tick mark and its label, in pixels (<see cref="AxisTickLook.MinorPad"/>).</summary>
    public double? MinorTickPad { get; set; }
    public bool? TicksOnAllSides { get; set; }
    public string? DefaultColorMapName { get; set; }
    public Color? VectorFieldColor { get; set; }
    public bool? ColorBarTakesRoom { get; set; }
    public bool? ColorBarStepPerColour { get; set; }
    /// <summary>Whether the end wedges of a colour bar are triangles (<see cref="ColorBarLook.ExtendTriangles"/>).</summary>
    public bool? ColorBarExtendTriangles { get; set; }
    /// <summary>Whether a colour bar draws marks at its ticks (<see cref="ColorBarLook.TickMarks"/>).</summary>
    public bool? ColorBarTickMarks { get; set; }
    /// <summary>Whether a log-normalised colour scale is coloured and labelled as matplotlib's log norm is (<see
    /// cref="ColorMapLook.LikeLogNorm"/>).</summary>
    public bool? ColoursLikeLogNorm { get; set; }
    public bool? CountsFromTheLeastDrawn { get; set; }
    /// <summary>Whether a quiver scales its arrows by matplotlib's automatic rule (<see
    /// cref="VectorFieldLook.QuiverAutoScale"/>).</summary>
    public bool? QuiverAutoScale { get; set; }
    /// <summary>Whether wind barbs are drawn as matplotlib draws them (<see cref="VectorFieldLook.BarbsLikeMatplotlib"/>).</summary>
    public bool? BarbsLikeMatplotlib { get; set; }
    /// <summary>Whether the arrows of a streamplot are drawn as matplotlib draws them (<see
    /// cref="VectorFieldLook.StreamArrowsLikeMatplotlib"/>).</summary>
    public bool? StreamArrowsLikeMatplotlib { get; set; }
    /// <summary>Whether the parts of a group of series (a band and its line) wear the next colour of the cycle (<see
    /// cref="CycleLook.GroupsWearTheCycle"/>).</summary>
    public bool? GroupsWearTheCycle { get; set; }
    /// <summary>The rule the colour cycle follows (<see cref="CycleLook.Rule"/>).</summary>
    public CycleRule? Cycles { get; set; }
    /// <summary>Whether the text a series draws is set at the theme's font size (<see cref="TextLook.SeriesTextAtFontSize"/>).</summary>
    public bool? SeriesTextAtFontSize { get; set; }
    /// <summary>Whether text is measured as matplotlib's Agg backend measures it (<see cref="TextLook.MeasuresLikeAgg"/>).</summary>
    public bool? TextMeasuredLikeAgg { get; set; }
    /// <summary>The math font set by matplotlib's name for it (<c>mathtext.fontset</c>: <c>cm</c>), when it is not the library's own.</summary>
    public string? MathTextFontSet { get; set; }
    /// <summary>The length of the caps of an error bar (<see cref="SeriesLineLook.ErrorBarCapSize"/>).</summary>
    public double? ErrorBarCapSize { get; set; }
    /// <summary>The width of the outline of a line's markers, in pixels (<see cref="LineMarkerLook.EdgeWidth"/>).</summary>
    public double? LineMarkerEdgeWidth { get; set; }
    /// <summary>The colour a filled marker of a line is outlined in (<see cref="LineMarkerLook.FilledEdge"/>).</summary>
    public Color? LineMarkerFilledEdge { get; set; }
    /// <summary>Whether a hatch takes the colour of its edge (<see cref="HatchColorRule"/>).</summary>
    public bool? HatchFollowsEdge { get; set; }
    /// <summary>The colour of a hatch that does not follow its edge (<see cref="HatchColorRule"/>).</summary>
    public Color? HatchFallback { get; set; }
    /// <summary>The gap between an axes' title and its plot, in pixels (<see cref="TextLook.TitlePad"/>).</summary>
    public double? AxesTitlePad { get; set; }
    /// <summary>The gap between an axis' label and its tick labels, in pixels (<see cref="TextLook.LabelPad"/>).</summary>
    public double? AxisLabelPad { get; set; }
    /// <summary>The size of a legend's text as a multiple of the tick labels' size (<see cref="LegendLook.FontScale"/>).</summary>
    public double? LegendFontScale { get; set; }
    /// <summary>Whether a legend's frame has rounded corners (<see cref="LegendLook.FancyBox"/>).</summary>
    public bool? LegendFancyBox { get; set; }
    /// <summary>The opacity of a legend's frame (<see cref="LegendLook.FrameAlpha"/>).</summary>
    public double? LegendFrameAlpha { get; set; }
    /// <summary>The colour of a legend's frame outline (<see cref="LegendLook.EdgeColor"/>).</summary>
    public Color? LegendEdgeColor { get; set; }
    /// <summary>How many markers the handle of a line shows in a legend (<see cref="LegendLook.NumPoints"/>).</summary>
    public int? LegendNumPoints { get; set; }
    /// <summary>How many markers the handle of a scatter shows in a legend (<see cref="LegendLook.ScatterPoints"/>).</summary>
    public int? LegendScatterPoints { get; set; }
    /// <summary>The order of magnitude at or below which the tick labels of a linear axis are scaled (<see
    /// cref="PowerLimits.Lower"/>).</summary>
    public int? FormatterLimitLower { get; set; }
    /// <summary>The order of magnitude at or above which the tick labels of a linear axis are scaled (<see
    /// cref="PowerLimits.Upper"/>).</summary>
    public int? FormatterLimitUpper { get; set; }
    /// <summary>Whether a negative number is written with the minus sign, not the hyphen (<see
    /// cref="TickLabelLook.UnicodeMinus"/>).</summary>
    public bool? UnicodeMinus { get; set; }
    /// <summary>Whether a marker fills its box as matplotlib's does (<see cref="MarkerLook.FillTheirBox"/>).</summary>
    public bool? MarkersFillTheirBox { get; set; }
    /// <summary>Whether the dot series are drawn as seaborn draws them (<see cref="DotLook.LikeSeaborn"/>).</summary>
    public bool? DotsLikeSeaborn { get; set; }
    /// <summary>Whether seaborn's verbs do what seaborn does beyond their marks (<see cref="SeabornVerbLook.Seaborn"/>).</summary>
    public bool? VerbsLikeSeaborn { get; set; }
    /// <summary>Whether a density is smoothed as scipy's and matplotlib's kernel estimators smooth it (<see cref="DensityLook.Scipy"/>).</summary>
    public bool? DensitiesLikeScipy { get; set; }
    /// <summary>Whether the lines over a violin are drawn as matplotlib draws them (<see cref="ViolinLook.Matplotlib"/>).</summary>
    public bool? ViolinsLikeMatplotlib { get; set; }
    /// <summary>Whether the wedges of a pie or a donut are laid as matplotlib lays them (<see cref="WedgeLook.Matplotlib"/>).</summary>
    public bool? WedgesLikeMatplotlib { get; set; }
    /// <summary>Whether the cells of a table are as matplotlib's <c>ax.table</c> makes them (<see cref="TableLook.Matplotlib"/>).</summary>
    public bool? TablesLikeMatplotlib { get; set; }
    /// <summary>Whether a treemap is laid out as squarify lays it out (<see cref="TreemapLook.LikeSquarify"/>).</summary>
    public bool? TreemapLikeSquarify { get; set; }
    /// <summary>Whether an image is drawn as matplotlib's <c>imshow</c> draws it (<see cref="ImageLook.LikeImshow"/>).</summary>
    public bool? ImagesLikeImshow { get; set; }
    /// <summary>Whether a spectrogram is drawn as matplotlib's <c>specgram</c> draws it (<see cref="SpectrogramLook.Specgram"/>).</summary>
    public bool? SpectrogramsLikeSpecgram { get; set; }
    /// <summary>Whether a map is drawn as cartopy draws it (<see cref="MapLook.LikeCartopy"/>).</summary>
    public bool? MapsLikeCartopy { get; set; }
    /// <summary>Whether a polar axes is drawn as matplotlib draws it (<see cref="PolarLook"/>).</summary>
    public bool? PolarsLikeMatplotlib { get; set; }
    /// <summary>Whether an axes title rises over what stands below it (<see cref="TextLook.TitleOverWhatIsBelow"/>, matplotlib's
    /// <c>axes.titley</c> None). The key keeps the name it was first written under, when only a polar axes read it.</summary>
    public bool? PolarTitleOverLabels { get; set; }
    /// <summary>The name of the interpolation an image is resampled with (<see cref="ImageLook.Interpolation"/>).</summary>
    public string? ImageInterpolation { get; set; }

    /// <summary>The theme's default colour map when it was built in code, described by what it is made of.</summary>
    public ColorMapDto? DefaultColorMap { get; set; }

    /// <summary>The theme's default colour map as the one value that sits in the two fields that carry it
    /// (<see cref="DefaultColorMapName"/>, <see cref="DefaultColorMap"/>). No field of its own: it is not written itself.</summary>
    internal ColorMapWire DefaultMapOnTheWire
    {
        get => new(DefaultColorMapName, DefaultColorMap);
        set
        {
            DefaultColorMapName = value.Name;
            DefaultColorMap = value.Description;
        }
    }
    public double? SupTitleScale { get; set; }
    public FontWeight? TitleWeight { get; set; }
    public Color? NeutralColor { get; init; }
    public AlarmPaletteDto? Alarm { get; init; }

    /// <summary>The theme every property is measured against and read back over.</summary>
    private static Theme Plain => Theme.Default;

    /// <summary>The JSON's form of <paramref name="theme"/>: its own values, then the values of its looks, each under its own flat key,
    /// which each look writes itself (<see cref="ThemeLooks.WriteKeys"/>).</summary>
    /// <param name="theme">The theme to write.</param>
    /// <returns>The record of the theme.</returns>
    internal static ThemeDto From(Theme theme)
    {
        ThemeDto keys = new()
        {
            Name = theme.Name.OrNullAtRest(Plain.Name),
            Background = theme.Background.OrNullAtRest(Plain.Background),
            ForegroundText = theme.ForegroundText.OrNullAtRest(Plain.ForegroundText),
            AxesBackground = theme.AxesBackground.OrNullAtRest(Plain.AxesBackground),
            CycleColors = theme.CycleColors.OrNullAtRest(Plain.CycleColors),
            DefaultFont = theme.DefaultFont.DescribedUnlessAtRest(Plain.DefaultFont, FontDto.From),
            DefaultGrid = theme.DefaultGrid.DescribedUnlessAtRest(Plain.DefaultGrid, GridDto.From),
            PropCycler = PropCyclerDto.From(theme.PropCycler),
            DefaultSpacing = theme.DefaultSpacing is { } spacing ? SubPlotSpacingDto.From(spacing) : null,
            ViolinBodyColor = theme.ViolinBodyColor,
            ViolinStatsColor = theme.ViolinStatsColor,
            AxisXMargin = theme.AxisXMargin.OrNullAtRest(Plain.AxisXMargin),
            AxisYMargin = theme.AxisYMargin.OrNullAtRest(Plain.AxisYMargin),
            DefaultLegendPosition = theme.DefaultLegendPosition.OrNullAtRest(Plain.DefaultLegendPosition),
            Pane3DColor = theme.Pane3DColor,
            NeutralColor = theme.NeutralColor,
            Alarm = theme.Alarm.DescribedUnlessAtRest(Plain.Alarm, AlarmPaletteDto.From),
        };
        theme.Looks.WriteKeys(keys);
        return keys;
    }

    /// <summary>The theme this record describes: the plain theme, renamed, with every property this record holds. Each key sets
    /// the one value of the looks it names, over the library's looks (<see cref="ThemeLooks.ReadKeys"/>), so a record that holds only
    /// some keys means what they say.</summary>
    /// <returns>The theme of the record.</returns>
    internal Theme ToTheme() => new(Name ?? Plain.Name, Plain)
    {
        Background = Background ?? Plain.Background,
        ForegroundText = ForegroundText ?? Plain.ForegroundText,
        AxesBackground = AxesBackground ?? Plain.AxesBackground,
        CycleColors = CycleColors ?? Plain.CycleColors,
        DefaultFont = DefaultFont?.ToFont() ?? Plain.DefaultFont,
        DefaultGrid = DefaultGrid?.ToGridStyle() ?? Plain.DefaultGrid,
        PropCycler = PropCycler?.ToPropCycler(),
        DefaultSpacing = DefaultSpacing?.ToSpacing(),
        ViolinBodyColor = ViolinBodyColor,
        ViolinStatsColor = ViolinStatsColor,
        AxisXMargin = AxisXMargin ?? Plain.AxisXMargin,
        AxisYMargin = AxisYMargin ?? Plain.AxisYMargin,
        DefaultLegendPosition = DefaultLegendPosition ?? Plain.DefaultLegendPosition,
        Pane3DColor = Pane3DColor,
        NeutralColor = NeutralColor,
        Alarm = Alarm?.ToAlarmPalette(Plain.Alarm) ?? Plain.Alarm,
        Looks = ThemeLooks.ReadKeys(this),
    };
}

/// <summary>The figure JSON's form of a <see cref="Styling.PropCycler"/>: the four lists it steps through, as it was built
/// with them. A list left out reads as the cycler's own single default; a cycler given no colours leaves the colours to its theme.</summary>
internal sealed record PropCyclerDto
{
    public Color[]? Colors { get; init; }
    public LineStyle[]? LineStyles { get; init; }
    public MarkerStyle[]? MarkerStyles { get; init; }
    public double[]? LineWidths { get; init; }

    /// <summary>The JSON's form of <paramref name="cycler"/>, or <see langword="null"/> when there is none.</summary>
    /// <param name="cycler">The cycler of the theme; <see langword="null"/> for none.</param>
    /// <returns>The record of the cycler.</returns>
    internal static PropCyclerDto? From(PropCycler? cycler) => cycler is null ? null : new()
    {
        Colors = cycler.ColorCycle.Count > 0 ? [.. cycler.ColorCycle] : null,
        LineStyles = [.. cycler.LineStyleCycle],
        MarkerStyles = [.. cycler.MarkerStyleCycle],
        LineWidths = cycler.LineWidthCycle.Count > 0 ? [.. cycler.LineWidthCycle] : null,
    };

    /// <summary>The cycler this record describes.</summary>
    internal PropCycler ToPropCycler() => new(Colors ?? [], LineStyles ?? [], MarkerStyles ?? [], LineWidths ?? []);
}

/// <summary>The figure JSON's form of a theme's <see cref="AlarmPalette"/>: each colour written only where it differs
/// from the palette it is measured against.</summary>
internal sealed record AlarmPaletteDto
{
    public Color? Resting { get; init; }
    public Color? Warning { get; init; }
    public Color? Critical { get; init; }
    public Color? Unknown { get; init; }

    /// <summary>The JSON's form of <paramref name="palette"/>, measured against <paramref name="resting"/>.</summary>
    internal static AlarmPaletteDto From(AlarmPalette palette, AlarmPalette resting) => new()
    {
        Resting = palette.Resting.OrNullAtRest(resting.Resting),
        Warning = palette.Warning.OrNullAtRest(resting.Warning),
        Critical = palette.Critical.OrNullAtRest(resting.Critical),
        Unknown = palette.Unknown.OrNullAtRest(resting.Unknown),
    };

    /// <summary>The palette this record describes, over <paramref name="resting"/>.</summary>
    internal AlarmPalette ToAlarmPalette(AlarmPalette resting) => new(
        Resting ?? resting.Resting,
        Warning ?? resting.Warning,
        Critical ?? resting.Critical,
        Unknown ?? resting.Unknown);
}
