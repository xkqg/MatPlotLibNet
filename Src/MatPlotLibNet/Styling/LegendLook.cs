// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>What a legend is drawn with where its author names nothing, held by <see cref="ThemeLooks.Legend"/>: the library's own
/// themes wear <see cref="Library"/>, the numbers a legend has always been drawn with; the matplotlib themes wear matplotlib's
/// <c>legend.*</c> settings and its box (<see cref="LegendBox.Matplotlib"/>), with the library's shadow and handle sizes.</summary>
internal sealed record LegendLook : IThemeKeys<LegendLook>
{
    /// <summary>The look of every library theme: the size of the tick labels, a square frame 0.5 wide in light grey at 0.8 opacity
    /// filled with the figure's background, one marker in a handle, a scatter's marker half the handle's height in from its start.</summary>
    internal static LegendLook Library { get; } = new();

    /// <summary>The size of the entry text as a multiple of the tick labels' size: <c>legend.fontsize</c>, <c>'medium'</c> (1)
    /// under the default style and <c>'large'</c> (1.2) under the classic one; 1 under the library's themes.</summary>
    public double FontScale { get; init; } = 1.0;

    /// <summary>Whether the frame has rounded corners: <c>legend.fancybox</c>, on under the default style and off under the
    /// classic one and the library's themes. A rounded corner has a radius of 0.2 of the legend's font size
    /// (<c>FancyBboxPatch(boxstyle="round,rounding_size=0.2", mutation_scale=fontsize)</c>).</summary>
    public bool FancyBox { get; init; }

    /// <summary>The opacity of the frame's fill: <c>legend.framealpha</c>, <see cref="Models.Legend.RestingFrameAlpha"/> under the default
    /// style and the library's themes and,
    /// where the classic style names none (<c>None</c>), 1. Refused outside 0 to 1 (<see cref="UnitIntervalExtensions.ThrowIfNotOpacity(double, string)"/>),
    /// as every opacity is, so a theme document that carries one is refused when it is read.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double FrameAlpha { get; init => field = value.ThrowIfNotOpacity(nameof(FrameAlpha)); } = Models.Legend.RestingFrameAlpha;

    /// <summary>The colour of the frame's outline: <c>legend.edgecolor</c>, <c>'0.8'</c> (light grey) under the default style and the
    /// library's themes, <c>'inherit'</c>, the axes' edge colour (black), under the classic one.</summary>
    public Color EdgeColor { get; init; } = RestingEdgeColor;

    /// <summary>matplotlib's <c>legend.edgecolor</c> under the default style, <c>'0.8'</c> (#CCCCCC): the library's look and the
    /// matplotlib themes that draw it read it here.</summary>
    internal static Color RestingEdgeColor => new(0xCC, 0xCC, 0xCC);

    /// <summary>How many markers the handle of a line shows: <c>legend.numpoints</c>, 1 under the default style and the library's
    /// themes and 2 under the classic one.</summary>
    public int NumPoints { get; init; } = 1;

    /// <summary>How many markers the handle of a scatter shows: <c>legend.scatterpoints</c>, 1 under the default style and the
    /// library's themes and 3 under the classic one.</summary>
    public int ScatterPoints { get; init; } = 1;

    /// <summary>Where the frame's fill comes from when the legend names no face colour: the figure's background under the library's
    /// themes, the axes' colour under matplotlib's (<c>legend.facecolor: 'inherit'</c>).</summary>
    public LegendFace Face { get; init; } = LegendFace.Figure;

    /// <summary>Where the markers of a scatter's handle stand when the legend names no number of them: the one marker half the
    /// handle's height in from its start under the library's themes, spread as matplotlib's <c>HandlerPathCollection</c> spreads them
    /// under matplotlib's. A legend that names a number of markers spreads them under every theme.</summary>
    public LegendScatterHandle ScatterHandle { get; init; } = LegendScatterHandle.AtHandleStart;

    /// <summary>The width of the frame's outline, in pixels: 0.5 under the library's themes, <c>patch.linewidth</c> (1 pt) under
    /// matplotlib's, whose frame is a patch (<c>legend.linewidth</c> None).</summary>
    public double FrameWidth { get; init; } = 0.5;

    /// <summary>What the frame's opacity applies to: the fill alone under the library's themes, the fill and the outline under
    /// matplotlib's (<see cref="LegendFrameOpacity"/>).</summary>
    public LegendFrameOpacity FrameOpacity { get; init; } = LegendFrameOpacity.Fill;

    /// <summary>The colour of the drop shadow a legend that asks for one casts.</summary>
    public Color ShadowColor { get; init; } = Colors.DropShadow;

    /// <summary>How far right and down the drop shadow stands from the frame, in pixels.</summary>
    public double ShadowOffset { get; init; } = 3;

    /// <summary>How the box is laid out round the rows: its padding, where it stands inside a corner, a row's and the title's height and
    /// where their text stands, its width beside a title, which column each entry takes (<see cref="LegendBox"/>).</summary>
    public LegendBox Box { get; init; } = LegendBox.Library;

    /// <summary>How much larger than the tick labels the legend's title is where the legend names no title size, in pixels: 1 under the
    /// library's themes, none under matplotlib's, whose title is <c>font.size</c> (<c>legend.title_fontsize</c> None), the tick labels'
    /// size.</summary>
    public double TitleSizeStep { get; init; } = 1;

    /// <summary>The weight of the legend's title: bold under the library's themes, normal under matplotlib's.</summary>
    public FontWeight TitleWeight { get; init; } = FontWeight.Bold;

    /// <summary>The length of a handle in multiples of the legend's font size (<c>legend.handlelength</c>, 2.0).</summary>
    public double HandleLength { get; init; } = 2.0;

    /// <summary>The height of a handle in multiples of the legend's font size (<c>legend.handleheight</c>, 0.7).</summary>
    public double HandleHeight { get; init; } = 0.7;

    /// <summary>The gap between a handle and its text in multiples of the legend's font size (<c>legend.handletextpad</c>, 0.8).</summary>
    public double HandleTextPad { get; init; } = 0.8;

    /// <summary>The share of the legend's font size the markers of a handle stand in from its ends when there is more than one
    /// (<c>HandlerNpoints.marker_pad</c>).</summary>
    private const double MarkerPad = 0.3;

    /// <summary>Where each of <paramref name="count"/> markers of a handle stands along it, as <c>HandlerNpoints.get_xdata</c> places them: a
    /// single one in the middle, several spread evenly from <c>0.3</c> of the font size in from the start to as far in from the end.</summary>
    /// <param name="count">How many markers the handle shows.</param>
    /// <param name="width">The width of the handle.</param>
    /// <param name="fontSize">The size of the legend's text.</param>
    /// <returns>The distance of each marker from the start of the handle.</returns>
    internal static double[] MarkerStops(int count, double width, double fontSize)
    {
        if (count <= 1) return [width / 2];
        double pad = MarkerPad * fontSize, span = width - 2 * pad;
        var stops = new double[count];
        for (int i = 0; i < count; i++) stops[i] = pad + span * i / (count - 1);
        return stops;
    }

    /// <summary>Where the line of a handle that shows <paramref name="count"/> markers starts and ends along it
    /// (<c>HandlerLine2D</c>): the whole handle for one marker, from the first marker to the last for several.</summary>
    /// <param name="count">How many markers the handle shows.</param>
    /// <param name="width">The width of the handle.</param>
    /// <param name="fontSize">The size of the legend's text.</param>
    /// <returns>The distances of the line's two ends from the start of the handle.</returns>
    internal static HandleSpan LineSpan(int count, double width, double fontSize)
    {
        if (count <= 1) return new HandleSpan(0, width);
        double pad = MarkerPad * fontSize;
        return new HandleSpan(pad, width - pad);
    }

    /// <summary>The size across, in pixels, a marker is drawn at in a handle <paramref name="handleHeight"/> high: the size its series
    /// (or its slot) gives in points, at least one point, and no more than the handle is high (a marker of the resting size fits it).
    /// The one rule of every kind of handle that shows a marker (a line's, a scatter's, a 3-D scatter's): each chooses only which size
    /// it hands over.</summary>
    /// <param name="points">The size across, in points, the marker is given.</param>
    /// <param name="handleHeight">The height of the handle, in pixels.</param>
    /// <returns>The size across, in pixels.</returns>
    internal static double MarkerSizeIn(double points, double handleHeight) => Math.Min(handleHeight, Math.Max(points, 1).PointsInPixels);

    /// <summary>How far above the foot of a scatter handle each of <paramref name="count"/> markers stands, as a share of the handle's
    /// height (<c>Legend._scatteryoffsets</c>: 3/8, 4/8 and 2.5/8, repeated).</summary>
    /// <param name="count">How many markers the handle shows.</param>
    /// <returns>The share for each marker.</returns>
    internal static double[] ScatterHeights(int count)
    {
        double[] cycle = [3.0 / 8, 4.0 / 8, 2.5 / 8];
        var heights = new double[Math.Max(1, count)];
        for (int i = 0; i < heights.Length; i++) heights[i] = cycle[i % cycle.Length];
        return heights;
    }

    /// <inheritdoc />
    /// <remarks>The library's look writes no key: a theme that names none leaves a legend as the library draws it. Any other look
    /// writes its six <c>legend.*</c> settings.</remarks>
    public void WriteKeys(ThemeDto keys)
    {
        if (this == Library) return;
        keys.LegendFontScale = FontScale;
        keys.LegendFancyBox = FancyBox.OrNullAtRest(false);
        keys.LegendFrameAlpha = FrameAlpha;
        keys.LegendEdgeColor = EdgeColor;
        keys.LegendNumPoints = NumPoints;
        keys.LegendScatterPoints = ScatterPoints;
    }

    /// <inheritdoc />
    /// <remarks><see cref="Library"/> when the keys name no font scale, as a theme that names none leaves a legend as the library has
    /// always drawn it; otherwise matplotlib's look made of the keys given, each key it lacks at the value the matplotlib form takes for
    /// it. A frame opacity the keys name is refused outside 0 to 1 whichever look it ends up in, as its setter refuses it.</remarks>
    public static LegendLook ReadKeys(ThemeDto keys)
    {
        double? frameAlpha = keys.LegendFrameAlpha.ThrowIfNotOpacity(nameof(ThemeDto.LegendFrameAlpha));
        return keys.LegendFontScale is { } scale
            ? Matplotlib(scale, keys.LegendFancyBox ?? false, frameAlpha ?? 1.0, keys.LegendEdgeColor ?? Colors.Black,
                keys.LegendNumPoints ?? 1, keys.LegendScatterPoints ?? 1)
            : Library;
    }

    /// <summary>A matplotlib look: matplotlib's <c>legend.*</c> settings named, its frame filled with the axes' colour, a scatter's
    /// markers spread over the handle, its box laid out as matplotlib's (<see cref="LegendBox.Matplotlib"/>) with a title of the tick labels'
    /// size in a normal weight; the library's shadow and handle sizes kept.</summary>
    /// <param name="fontScale">The size of the entry text as a multiple of the tick labels' size.</param>
    /// <param name="fancyBox">Whether the frame has rounded corners.</param>
    /// <param name="frameAlpha">The opacity of the frame's fill.</param>
    /// <param name="edgeColor">The colour of the frame's outline.</param>
    /// <param name="numPoints">How many markers the handle of a line shows.</param>
    /// <param name="scatterPoints">How many markers the handle of a scatter shows.</param>
    /// <returns>The look.</returns>
    internal static LegendLook Matplotlib(double fontScale, bool fancyBox, double frameAlpha, Color edgeColor, int numPoints, int scatterPoints) =>
        Library with
        {
            FontScale = fontScale,
            FancyBox = fancyBox,
            FrameAlpha = frameAlpha,
            EdgeColor = edgeColor,
            NumPoints = numPoints,
            ScatterPoints = scatterPoints,
            Face = LegendFace.Axes,
            ScatterHandle = LegendScatterHandle.Spread,
            Box = LegendBox.Matplotlib,
            TitleSizeStep = 0,
            TitleWeight = FontWeight.Normal,
            FrameWidth = 1.0.PointsInPixels,
            FrameOpacity = LegendFrameOpacity.Patch,
        };

    /// <summary>The opacity of a legend's frame: the one given, else this look's.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <returns>The value drawn.</returns>
    internal double FrameAlphaOver(double? given) => given ?? FrameAlpha;

    /// <summary>The colour of a legend frame's edge: the one given, else this look's.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <returns>The value drawn.</returns>
    internal Color EdgeColorOver(Color? given) => given ?? EdgeColor;

    /// <summary>Whether a legend's frame is rounded: the choice given, else this look's.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <returns>The value drawn.</returns>
    internal bool FancyBoxOver(bool? given) => given ?? FancyBox;

    /// <summary>How many markers a line's handle shows: the count given, else this look's.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <returns>The value drawn.</returns>
    internal int NumPointsOver(int? given) => given ?? NumPoints;

    /// <summary>How many markers a scatter's handle shows: the count given, else this look's.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <returns>The value drawn.</returns>
    internal int ScatterPointsOver(int? given) => given ?? ScatterPoints;

    /// <summary>Where the line of a legend handle starts and ends along it (<see cref="LegendLook.LineSpan"/>).</summary>
    /// <param name="From">The distance of the line's start from the start of the handle.</param>
    /// <param name="To">The distance of the line's end from the start of the handle.</param>
    internal readonly record struct HandleSpan(double From, double To);
}
