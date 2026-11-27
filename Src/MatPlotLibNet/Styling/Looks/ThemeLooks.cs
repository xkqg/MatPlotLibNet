// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>Everything a theme says about how things are drawn beyond its colours, font and grid: one look for each concern.
/// A library theme wears <see cref="Library"/>; the matplotlib themes wear the looks <c>MatplotlibThemeFactory</c> makes once.
/// A copy of a theme copies this one reference, and the figure JSON writes each look's values under their own flat keys
/// (<see cref="ThemeDto"/>), each look its own (<see cref="IThemeKeys{TLook}"/>), so a key read alone sets the one value it names.</summary>
internal sealed record ThemeLooks : IThemeKeys<ThemeLooks>
{
    /// <summary>The looks of every library theme: each concern's library value.</summary>
    internal static ThemeLooks Library { get; } = new();

    /// <summary>Writes every look under its own keys: each look writes its own.</summary>
    /// <param name="keys">The theme's record being written.</param>
    public void WriteKeys(ThemeDto keys)
    {
        Ticks.WriteKeys(keys);
        TickLabels.WriteKeys(keys);
        Range.WriteKeys(keys);
        Frame.WriteKeys(keys);
        Cube.WriteKeys(keys);
        Text.WriteKeys(keys);
        Figure.WriteKeys(keys);
        Patch.WriteKeys(keys);
        ColorBar.WriteKeys(keys);
        ColorMap.WriteKeys(keys);
        Raster.WriteKeys(keys);
        Strokes.WriteKeys(keys);
        Cycle.WriteKeys(keys);
        Lines.WriteKeys(keys);
        Markers.WriteKeys(keys);
        Scatter?.WriteKeys(keys);
        Box.WriteKeys(keys);
        Legend.WriteKeys(keys);
        Dots.WriteKeys(keys);
        Verbs.WriteKeys(keys);
        Density.WriteKeys(keys);
        Violin.WriteKeys(keys);
        Wedges.WriteKeys(keys);
        Table.WriteKeys(keys);
        Treemap.WriteKeys(keys);
        Image.WriteKeys(keys);
        Spectrogram.WriteKeys(keys);
        Map.WriteKeys(keys);
        Polar.WriteKeys(keys);
        VectorField.WriteKeys(keys);
    }

    /// <summary>The looks <paramref name="keys"/> describe: each look reads its own keys.</summary>
    /// <param name="keys">The theme's record being read.</param>
    /// <returns>The looks.</returns>
    public static ThemeLooks ReadKeys(ThemeDto keys) => new()
    {
        Ticks = AxisTickLook.ReadKeys(keys),
        TickLabels = TickLabelLook.ReadKeys(keys),
        Range = AxisRangeLook.ReadKeys(keys),
        Frame = AxesFrameLook.ReadKeys(keys),
        Cube = CubeLook.ReadKeys(keys),
        Text = TextLook.ReadKeys(keys),
        Figure = FigureLook.ReadKeys(keys),
        Patch = PatchLook.ReadKeys(keys),
        ColorBar = ColorBarLook.ReadKeys(keys),
        ColorMap = ColorMapLook.ReadKeys(keys),
        Raster = RasterLook.ReadKeys(keys),
        Strokes = StrokeLook.ReadKeys(keys),
        Cycle = CycleLook.ReadKeys(keys),
        Lines = SeriesLineLook.ReadKeys(keys),
        Markers = MarkerLook.ReadKeys(keys),
        Scatter = ScatterLook.ReadKeys(keys),
        Box = BoxLook.ReadKeys(keys),
        Legend = LegendLook.ReadKeys(keys),
        Dots = DotLook.ReadKeys(keys),
        Verbs = SeabornVerbLook.ReadKeys(keys),
        Density = DensityLook.ReadKeys(keys),
        Violin = ViolinLook.ReadKeys(keys),
        Wedges = WedgeLook.ReadKeys(keys),
        Table = TableLook.ReadKeys(keys),
        Treemap = TreemapLook.ReadKeys(keys),
        Image = ImageLook.ReadKeys(keys),
        Spectrogram = SpectrogramLook.ReadKeys(keys),
        Map = MapLook.ReadKeys(keys),
        Polar = PolarLook.ReadKeys(keys),
        VectorField = VectorFieldLook.ReadKeys(keys),
    };

    /// <summary>Where the ticks of an axis stand and how their marks are drawn.</summary>
    public AxisTickLook Ticks { get; init; } = AxisTickLook.Library;

    /// <summary>How the labels of the ticks are written.</summary>
    public TickLabelLook TickLabels { get; init; } = TickLabelLook.Library;

    /// <summary>Where the free ends of an axis that scales itself to its data stop.</summary>
    public AxisRangeLook Range { get; init; } = AxisRangeLook.Library;

    /// <summary>The rule for the ticks of an axis that names no locator or spacing: the ticks look's ladder, and whether the range look
    /// ends an axis at round numbers. The one place the theme's autolimit mode is read as the ticks' rounding.</summary>
    internal AxisTickRule TickRule => new(Ticks.Ladder, Range.AutolimitRule.RoundsToTicks);

    /// <summary>How a density curve and a pair grid's diagonal are estimated where their author names no bandwidth: the density look's
    /// bandwidth factor on the verb look's number of points. The one rule both estimate by.</summary>
    internal Numerics.DensityRule DensityCurveRule => new(Density.BandwidthFactor, Verbs.DensityPoints);

    /// <summary>The frame of a 2-D axes: its spines, where its axis stands among what it draws, its background.</summary>
    public AxesFrameLook Frame { get; init; } = AxesFrameLook.Library;

    /// <summary>The box of a 3-D axes and the stride of its surfaces.</summary>
    public CubeLook Cube { get; init; } = CubeLook.Library;

    /// <summary>The titles of an axes and a figure, the pads around them, and the text a series writes.</summary>
    public TextLook Text { get; init; } = TextLook.Library;

    /// <summary>What a figure itself does: the pad above its top row of titles, and its legend.</summary>
    public FigureLook Figure { get; init; } = FigureLook.Library;

    /// <summary>The edge, opacity and hatch of a patch, a fill and a surface.</summary>
    public PatchLook Patch { get; init; } = PatchLook.Library;

    /// <summary>How a colour bar takes its room and is drawn.</summary>
    public ColorBarLook ColorBar { get; init; } = ColorBarLook.Library;

    /// <summary>The colour map a series that names none draws with, and how a log normaliser colours.</summary>
    public ColorMapLook ColorMap { get; init; } = ColorMapLook.Library;

    /// <summary>What a raster export does with the marks it draws.</summary>
    public RasterLook Raster { get; init; } = RasterLook.Library;

    /// <summary>How a stroke is drawn: the lengths its dashes have and how its corners join.</summary>
    public StrokeLook Strokes { get; init; } = StrokeLook.Library;

    /// <summary>How the colours of the cycle are handed out.</summary>
    public CycleLook Cycle { get; init; } = CycleLook.Library;

    /// <summary>The width of a line that names none, and the cap of an error bar.</summary>
    public SeriesLineLook Lines { get; init; } = SeriesLineLook.Library;

    /// <summary>The shapes of the markers and the edge of a line's markers.</summary>
    public MarkerLook Markers { get; init; } = MarkerLook.Library;

    /// <summary>What a scatter marker is drawn with where its author names nothing (matplotlib's <c>Axes.scatter</c> under a style).
    /// <see langword="null"/> (the library's own themes) leaves a scatter plain.</summary>
    public ScatterLook? Scatter { get; init; }

    /// <summary>What a box plot is drawn with where its author names nothing, and where its boxes stand (matplotlib's
    /// <c>Axes.boxplot</c> under a style). The library's own themes colour the box from the cycle, with no caps and no fliers
    /// (<see cref="BoxLook.Library"/>).</summary>
    public BoxLook Box { get; init; } = BoxLook.Library;

    /// <summary>What a legend is drawn with where its author names nothing: its frame, its text, its handles (matplotlib's
    /// <c>legend.*</c> settings under the matplotlib themes).</summary>
    public LegendLook Legend { get; init; } = LegendLook.Library;

    /// <summary>The dots of the categorical plots seaborn draws.</summary>
    public DotLook Dots { get; init; } = DotLook.Library;

    /// <summary>What seaborn's verbs do beyond their marks: the name they give their value axis.</summary>
    public SeabornVerbLook Verbs { get; init; } = SeabornVerbLook.Library;

    /// <summary>How a Gaussian kernel density is smoothed where its author names no bandwidth.</summary>
    public DensityLook Density { get; init; } = DensityLook.Library;

    /// <summary>The lines a violin draws over its body: its extrema, its median and its mean.</summary>
    public ViolinLook Violin { get; init; } = ViolinLook.Library;

    /// <summary>Where the wedges of a pie or a donut start, which way they follow each other, and how a pie's names are aligned.</summary>
    public WedgeLook Wedges { get; init; } = WedgeLook.Library;

    /// <summary>What the cells of a table are where their author names nothing.</summary>
    public TableLook Table { get; init; } = TableLook.Library;

    /// <summary>How a treemap is laid out.</summary>
    public TreemapLook Treemap { get; init; } = TreemapLook.Library;

    /// <summary>How an image stands on its axes and is scaled.</summary>
    public ImageLook Image { get; init; } = ImageLook.Library;

    /// <summary>What the cells of a spectrogram hold and where they stand.</summary>
    public SpectrogramLook Spectrogram { get; init; } = SpectrogramLook.Library;

    /// <summary>How a map axes is framed.</summary>
    public MapLook Map { get; init; } = MapLook.Library;

    /// <summary>Where a polar axes puts its spoke labels and its title.</summary>
    public PolarLook Polar { get; init; } = PolarLook.Library;

    /// <summary>How a quiver, its key, barbs and streamlines are drawn.</summary>
    public VectorFieldLook VectorField { get; init; } = VectorFieldLook.Library;
}
