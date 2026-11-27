// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>What a theme draws a scatter marker with where its author names nothing, as matplotlib's <c>Axes.scatter</c> does under a
/// style: a size, a face colour, an edge, and how a 3-D scatter fades with its distance. A theme that names a look draws the markers
/// of a 3-D scatter all one size, as matplotlib's <c>Path3DCollection</c> keeps them; a theme that names none (the library's own)
/// shrinks a marker with its distance. A value left <see langword="null"/> is left to the series (and to the library's
/// own resting look). The scatters it covers are <see cref="Models.Series.ScatterSeries"/>, <see cref="Models.Series.PolarScatterSeries"/>,
/// <see cref="Models.Series.TernaryScatterSeries"/> and <see cref="Models.Series.Scatter3DSeries"/>.</summary>
/// <param name="Area">The area, in points squared, of a marker whose series was given no size (<c>s</c>: <c>20</c> under the classic style,
/// <c>rcParams['lines.markersize'] ** 2</c> under the default one, which is the library's own size,
/// <see cref="Models.Series.ScatterSeries.RestingMarkerSize"/>, so none). A size the series was given is drawn as given, the resting size
/// included.</param>
/// <param name="Area3D">The same for a 3-D scatter, whose <c>s</c> defaults to 20 in every style (<c>Axes3D.scatter(…, s=20)</c>).</param>
/// <param name="Face">The face colour of a marker whose author names none, and which does not take a colour of the cycle for it
/// (<c>"b"</c> under the classic style); <see langword="null"/> takes the next colour of the cycle.</param>
/// <param name="EdgeWidth">The width of the edge of a filled marker, in pixels (<c>patch.linewidth</c>); <see langword="null"/> draws
/// no edge.</param>
/// <param name="EdgeColor">The colour of that edge (<c>patch.edgecolor</c>, forced by <c>patch.force_edgecolor</c> under the classic
/// style); <see langword="null"/> draws it in the marker's face colour (<c>scatter.edgecolors = 'face'</c>).</param>
/// <param name="DepthShade">The least opacity a 3-D scatter marker fades to with its distance, the farthest marker being that opaque
/// (<c>axes3d.depthshade_minalpha</c>, 0.3, where <c>axes3d.depthshade</c> is on: the default style); <see langword="null"/> fades none
/// (the classic style, where it is off).</param>
internal readonly record struct ScatterLook(double? Area, double? Area3D, Color? Face, double? EdgeWidth, Color? EdgeColor, double? DepthShade)
    : IThemeKeys<ScatterLook?>
{
    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.ScatterArea = Area;
        keys.ScatterArea3D = Area3D;
        keys.ScatterFace = Face;
        keys.ScatterEdgeWidth = EdgeWidth;
        keys.ScatterEdgeColor = EdgeColor;
        keys.ScatterDepthShade = DepthShade;
    }

    /// <inheritdoc />
    /// <remarks>A record that names none of the six parts names no look: a theme that names none leaves a scatter plain.</remarks>
    public static ScatterLook? ReadKeys(ThemeDto keys) =>
        keys is { ScatterArea: null, ScatterArea3D: null, ScatterFace: null, ScatterEdgeWidth: null, ScatterEdgeColor: null, ScatterDepthShade: null }
            ? null
            : new ScatterLook(keys.ScatterArea, keys.ScatterArea3D, keys.ScatterFace, keys.ScatterEdgeWidth, keys.ScatterEdgeColor, keys.ScatterDepthShade);
}