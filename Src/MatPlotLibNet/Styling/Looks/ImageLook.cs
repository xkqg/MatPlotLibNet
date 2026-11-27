// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Interpolation;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>How an image stands on its axes and is scaled to its plot.</summary>
internal sealed record ImageLook : IThemeKeys<ImageLook>
{
    /// <summary>The library's own images: on 0 to the columns and 0 to the rows, scaled nearest, their values interpolated.</summary>
    internal static ImageLook Library { get; } = new();

    /// <inheritdoc />
    /// <remarks>The images of <c>imshow</c> are named as a whole (<see cref="Imshow"/>); the interpolation has a key of its own.</remarks>
    public void WriteKeys(ThemeDto keys)
    {
        keys.ImagesLikeImshow = LikeImshow.OrNullAtRest(false);
        keys.ImageInterpolation = Interpolation;
    }

    /// <inheritdoc />
    public static ImageLook ReadKeys(ThemeDto keys) => (keys.ImagesLikeImshow == true ? Imshow : Library) with { Interpolation = keys.ImageInterpolation };

    /// <summary>The images of matplotlib's <c>imshow</c> under its default and classic styles: each cell centred on its numbers
    /// (<see cref="LikeImshow"/>), square cells (<see cref="Aspect"/>, <c>image.aspect</c> <c>'equal'</c> in both styles), and a smoothed
    /// image interpolated in its colours where it is shown under three times its size (<see cref="ColoursStageWhenSmall"/>,
    /// <c>image.interpolation_stage</c> <c>'auto'</c> in both styles).</summary>
    internal static ImageLook Imshow { get; } = new() { LikeImshow = true, Aspect = 1.0, ColoursStageWhenSmall = true };

    /// <summary>Whether an image stands on its axes as matplotlib's <c>imshow</c> stands it: each cell centred on its row and column
    /// number (-0.5 to the columns less a half, -0.5 to the rows less a half), the axes showing that extent exactly with no margin
    /// and no rounding out to a tick, the first row on top (the Y axis runs down). Otherwise on 0 to the columns across and 0 to the
    /// rows up, the first row on top.</summary>
    public bool LikeImshow { get; init; }

    /// <summary>The name of the interpolation an image is scaled with where its author names none (matplotlib's
    /// <c>image.interpolation</c>): <c>"auto"</c> under the default style and <c>"bilinear"</c> under the classic one. Unset:
    /// nearest. <see cref="InterpolationOver"/> holds the rule.</summary>
    public string? Interpolation { get; init; }

    /// <summary>The aspect an image asks of its plot where the plot was given none (matplotlib's <c>image.aspect</c>, which <c>imshow</c>
    /// hands to <c>set_aspect</c>): 1 makes every cell square, the plot narrowed to the image's shape; <see langword="null"/> asks none, and
    /// the image fills the plot.</summary>
    public double? Aspect { get; init; }

    /// <summary>Whether a smoothed image shown at less than three times the size of its values is interpolated in its colours, as
    /// matplotlib's <c>image.interpolation_stage</c> <c>'auto'</c> does (<see cref="ImageFootprint.InterpolatesColours"/>); otherwise, and
    /// under the library's own look always, its values are interpolated and each coloured.</summary>
    public bool ColoursStageWhenSmall { get; init; }

    /// <summary>The name of the interpolation an image is scaled with: the one its author named, else
    /// <see cref="Interpolation"/>, else <c>"nearest"</c>.</summary>
    /// <param name="given">The interpolation the author named, <see langword="null"/> when none.</param>
    /// <returns>The name: an engine of <see cref="InterpolationRegistry"/>, or <c>"auto"</c> or
    /// <c>"antialiased"</c>.</returns>
    internal string InterpolationOver(string? given) => given ?? Interpolation ?? NearestInterpolation.Instance.Name;

    /// <summary>Whether an image shown as <paramref name="footprint"/> says and resampled by <paramref name="engine"/> is interpolated in
    /// its colours (matplotlib's <c>'rgba'</c> stage): under this look's <see cref="ColoursStageWhenSmall"/>, where it is shown under three
    /// times its size, by an engine that blends cells. A nearest image takes one cell's colour either way, so nothing is interpolated.</summary>
    /// <param name="footprint">How large the image is shown, against how many values it holds.</param>
    /// <param name="engine">The engine it is resampled with.</param>
    /// <returns><see langword="true"/> when its colours are interpolated.</returns>
    internal bool InterpolatesColours(ImageFootprint footprint, IInterpolationEngine engine) =>
        ColoursStageWhenSmall && engine is not NearestInterpolation && footprint.InterpolatesColours;
}
