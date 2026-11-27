// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>Where the free ends of an axis that scales itself to its data stop.</summary>
internal sealed record AxisRangeLook : IThemeKeys<AxisRangeLook>
{
    /// <summary>The library's own range.</summary>
    internal static AxisRangeLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.ScatterMargin = ScatterMargin;
        keys.AutolimitMode = Autolimit;
        keys.SymlogThreshold = SymLogLinThresh;
        keys.PatchesRangeLikeAddPatch = PatchesLikeAddPatch.OrNullAtRest(false);
    }

    /// <inheritdoc />
    public static AxisRangeLook ReadKeys(ThemeDto keys) => new()
    {
        Autolimit = keys.AutolimitMode,
        SymLogLinThresh = keys.SymlogThreshold,
        ScatterMargin = keys.ScatterMargin,
        PatchesLikeAddPatch = keys.PatchesRangeLikeAddPatch ?? false,
    };

    /// <summary>Where the free end of an axis stops (matplotlib's <c>axes.autolimit_mode</c>): where the data and the margin end
    /// under the default style, out at the outermost ticks under the classic one, a 2-D and a 3-D axis alike. Unset: a 2-D axis
    /// widens its margin to the nice bounds of its data where those reach further, unless a series holds it to a sticky edge, and a
    /// 3-D axis ends where the data and the margin end.</summary>
    public AutolimitMode? Autolimit { get; init; }

    /// <summary>The threshold a symmetric-log axis whose author gives none is linear within: matplotlib's <c>SymmetricalLogScale</c>
    /// default <c>linthresh=2</c> under the matplotlib themes (printed with 3.11.2, the default and the classic style). Unset: the library's
    /// <see cref="Rendering.SymlogTransform.DefaultLinThresh"/>.</summary>
    public double? SymLogLinThresh { get; init; }

    /// <summary>What <see cref="Autolimit"/> does (<see cref="Rendering.AutolimitRule"/>): the one leaf the range, the cube, the radius,
    /// a contour's ask and the tick rule read.</summary>
    internal AutolimitRule AutolimitRule => AutolimitRule.Of(Autolimit);

    /// <summary>The least margin X and Y of an axes that holds a scatter with points are padded by where the axis sets none, as
    /// matplotlib's classic <c>Axes.scatter</c> raises them to 5 %. Unset: the theme's margins alone.</summary>
    public double? ScatterMargin { get; init; }

    /// <summary>Whether the patches of an axes range it the way matplotlib's <c>add_patch</c> does: only where something else asks
    /// for the range to be worked out (a series, a reference line, a span, a trend line), so an axes of patches alone stays at 0 to 1, and a rectangle
    /// with neither width nor height is left out. Otherwise the patches of an axes that holds nothing else frame it, and a
    /// rectangle of no size counts.</summary>
    public bool PatchesLikeAddPatch { get; init; }

    /// <summary>Whether the patches range an axis when <paramref name="asked"/> says whether something else asks for its range: always
    /// under the library's own themes, only when asked under the matplotlib themes (<see cref="PatchesLikeAddPatch"/>), as
    /// <c>add_patch</c> asks for none. A 2-D axis is asked with whether a value is added to it or a series or an overlay asks for it
    /// (<see cref="AutoscaleAskExtensions.AsksForTheAutoscaleAlong"/>); the radius of a polar axes with whether a series ranges it.</summary>
    /// <param name="asked">Whether something else asks for the range of the axis.</param>
    /// <returns><see langword="true"/> when the patches range the axis.</returns>
    internal bool PatchesRange(bool asked) => !PatchesLikeAddPatch || asked;

    /// <summary>Whether <paramref name="patch"/> adds to the range at all: every patch does under the library's own themes; under the
    /// matplotlib themes a rectangle with neither width nor height adds nothing, as matplotlib's <c>_update_patch_limits</c> returns for
    /// it.</summary>
    /// <param name="patch">The patch.</param>
    /// <returns><see langword="true"/> when the patch adds its outline to the range.</returns>
    internal bool Counts(Patch patch) => !PatchesLikeAddPatch || patch is not Rectangle { Width: 0, Height: 0 };

    /// <summary>The margin a scatter keeps from the axes' edge: this look's, else <paramref name="resting"/>.</summary>
    /// <param name="resting">The value its owner rests at where the theme names none.</param>
    /// <returns>The value drawn.</returns>
    internal double ScatterMarginOr(double resting) => ScatterMargin ?? resting;

    /// <summary>The linear threshold of a symmetric-log axis: the one given, else this look's, else <paramref name="resting"/>.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <param name="resting">The value its owner rests at where the theme names none.</param>
    /// <returns>The value drawn.</returns>
    internal double SymLogLinThreshOver(double? given, double resting) => given ?? SymLogLinThresh ?? resting;
}
