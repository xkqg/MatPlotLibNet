// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>The frame of a 2-D axes: how wide its spines are, where its axis stands among what it draws, whether it paints a
/// background when it draws no axes.</summary>
internal sealed record AxesFrameLook : IThemeKeys<AxesFrameLook>
{
    /// <summary>The library's own frame.</summary>
    internal static AxesFrameLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.SpineLineWidth = SpineWidth;
        keys.AxisZOrder = AxisZOrder;
        keys.NoBackgroundWithoutAxes = NoBackgroundWithoutAxes.OrNullAtRest(false);
        keys.ClipsEveryArtist = ClipsEveryArtist.OrNullAtRest(false);
    }

    /// <inheritdoc />
    public static AxesFrameLook ReadKeys(ThemeDto keys) => new()
    {
        SpineWidth = keys.SpineLineWidth,
        AxisZOrder = keys.AxisZOrder,
        NoBackgroundWithoutAxes = keys.NoBackgroundWithoutAxes ?? false,
        ClipsEveryArtist = keys.ClipsEveryArtist ?? false,
    };

    /// <summary>How wide a spine is drawn when its author gave it no width, in pixels: matplotlib's <c>axes.linewidth</c>, 1.0 pt
    /// under the classic style and 0.8 pt under the default one. The frame of a colour bar is drawn this wide too and its edges half
    /// as wide. Unset: <see cref="Models.SpineConfig.DefaultLineWidth"/>, a colour bar's frame
    /// <see cref="Rendering.ColorBarRendering.ColorBarStrip.RestingFrameWidth"/> and its edges
    /// <see cref="Rendering.ColorBarRendering.ColorBarStrip.RestingEdgeWidth"/>.</summary>
    public double? SpineWidth { get; init; }

    /// <summary>Where the grid stands among what an axes draws, as matplotlib's <c>axes.axisbelow</c> places the axis: 1.5 under the
    /// default style (over the patches at 1, under the lines at 2) and 2.5 under the classic one (over the lines too). Set, every
    /// series, patch, span, reference line and drawing tool is drawn in matplotlib's z-order with it (<c>ZOrderExtensions</c>).
    /// Unset: the grid first, under everything, and the series by their z-order.</summary>
    public double? AxisZOrder { get; init; }

    /// <summary>Whether an axes that draws no axes (every series draws without them: a sparkline, a pie, a treemap) paints no
    /// background, as matplotlib's <c>set_axis_off()</c> draws no axes patch: the classic style, whose axes colour (white) differs
    /// from its figure's (grey).</summary>
    public bool NoBackgroundWithoutAxes { get; init; }

    /// <summary>Whether every mark of an axes is cut off at its plot, as matplotlib clips every artist to the axes patch (<c>clip_on</c>
    /// True, the clip box the axes' bbox, whether or not a limit was set), so a bar's edge, a marker or a thick line at the end of the data
    /// stops at the spine; a series' label, value or name is text and stands whole past it. Otherwise the marks are cut off only where a
    /// limit was set, as the library has always drawn them: a range that scaled itself encloses every series.</summary>
    public bool ClipsEveryArtist { get; init; }

    /// <summary>The width of a spine: the one given, else this look's, else <paramref name="resting"/>.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <param name="resting">The value its owner rests at where the theme names none.</param>
    /// <returns>The value drawn.</returns>
    internal double SpineWidthOver(double? given, double resting) => given ?? SpineWidth ?? resting;

    /// <summary>The z-order the axis lines and the grid are drawn at: this look's, else <paramref name="resting"/>.</summary>
    /// <param name="resting">The value its owner rests at where the theme names none.</param>
    /// <returns>The value drawn.</returns>
    internal double AxisZOrderOr(double resting) => AxisZOrder ?? resting;
}
