// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>What seaborn's verbs do beyond their marks where their author names nothing: the name a count plot and a density curve give
/// their value axis, the saturation of a count plot's bars, a density curve's fill and range, what a rug asks of its axes, a residual
/// plot's range and zero line. The matplotlib themes draw them as seaborn 0.13.2 does over matplotlib's own defaults (<see cref="Seaborn"/>); the
/// library's own themes as the library always has (<see cref="Library"/>). The dots of seaborn's categorical plots are
/// <see cref="DotLook"/>'s.</summary>
internal sealed record SeabornVerbLook : IThemeKeys<SeabornVerbLook>
{
    /// <inheritdoc />
    /// <remarks>The look is named as a whole: seaborn's (<see cref="Seaborn"/>) or the library's.</remarks>
    public void WriteKeys(ThemeDto keys) => keys.VerbsLikeSeaborn = (this == Seaborn).OrNullAtRest(false);

    /// <inheritdoc />
    public static SeabornVerbLook ReadKeys(ThemeDto keys) => keys.VerbsLikeSeaborn == true ? Seaborn : Library;

    /// <summary>The library's own verbs: no axis named.</summary>
    internal static SeabornVerbLook Library { get; } = new();

    /// <summary>The verbs as seaborn 0.13.2 draws them: a count plot names its value axis 'count' and a density curve 'Density'
    /// (<c>_add_axis_labels</c>, only where the axis has no label yet).</summary>
    internal static SeabornVerbLook Seaborn { get; } = new()
    {
        NamesValueAxis = true, CountSaturation = 0.75, DensityFills = false, DensityHeadroom = 1, DensityBase = 0, DensityPoints = 200,
        DensityRangesFromZero = false, RugRangesAcross = false, RugMarginPerHeight = 2,
        ResidualPadding = 0, ResidualZeroLine = new(Color.FromHex("#333333"), 1, LineStyle.Dotted, LineWidthRule.LineCollection),
    };

    /// <summary>Whether a verb that names its value axis (<see cref="Models.Series.INamesValueAxis"/>) names it where the axis has no
    /// label of its own.</summary>
    public bool NamesValueAxis { get; init; }

    /// <summary>How much of its colour's saturation a count plot's bars keep where their author names no colour: seaborn's
    /// <c>countplot(saturation=.75)</c> (<see cref="ColorExtensions.Desaturated"/>), the colour as it is under the library's themes.</summary>
    public double CountSaturation { get; init; } = 1;

    /// <summary>Whether a density curve is filled down to 0 where its author names no choice: under the library's themes it is, seaborn's
    /// <c>kdeplot(fill=False)</c> draws the curve alone.</summary>
    public bool DensityFills { get; init; } = true;

    /// <summary>How far a density curve's range reaches above its peak, as a factor of the peak: 5 % under the library's themes, none
    /// under seaborn's, whose axes' margin alone opens above the curve.</summary>
    public double DensityHeadroom { get; init; } = 1.05;

    /// <summary>The density a curve's range sticks at below, so no margin opens under it: 0 under seaborn's (<c>kdeplot</c>'s
    /// <c>sticky_edges.y = (0, inf)</c>), none under the library's themes.</summary>
    public double? DensityBase { get; init; }

    /// <summary>How many points a density curve (and a pair grid's diagonal) is evaluated at: 100 under the library's themes, seaborn's
    /// <c>kdeplot(gridsize=200)</c> under seaborn's.</summary>
    public int DensityPoints { get; init; } = 100;

    /// <summary>Whether a density curve's range starts at 0 whether or not it is filled: under the library's themes it does; under
    /// seaborn's a curve drawn alone is ranged as its line is (matplotlib's data limits: from its lowest density), a filled one from the 0
    /// its fill stands on.</summary>
    public bool DensityRangesFromZero { get; init; } = true;

    /// <summary>Whether a rug ranges the axis across its ticks (from 0 to 1, or its height): under the library's themes it does; seaborn's
    /// <c>rugplot</c> draws its ticks in the axes' own height (<c>blended_transform_factory(transData, transAxes)</c>), which adds nothing to that range.</summary>
    public bool RugRangesAcross { get; init; } = true;

    /// <summary>How many times its height a rug raises the margin of the axis across its ticks: seaborn's <c>rugplot</c>
    /// (<c>ymarg += height * 2</c>, <c>ax.margins(x=xmarg, y=ymarg)</c>), none under the library's themes.</summary>
    public double RugMarginPerHeight { get; init; }

    /// <summary>How far a residual plot's range reaches past its residuals, as a share of their spread (of 1 where they spread less):
    /// a tenth under the library's themes, none under seaborn's, whose range is its scatter's and its zero line's.</summary>
    public double ResidualPadding { get; init; } = 0.1;

    /// <summary>The line a residual plot draws at 0: dashed in the dots' colour at half opacity, one pixel wide, under the library's
    /// themes; seaborn's <c>ax.axhline(0, ls=':', c='.2')</c> at <c>lines.linewidth</c> under seaborn's.</summary>
    public GuideLine ResidualZeroLine { get; init; } = new(null, 0.5, LineStyle.Dashed, LineWidthRule.OwnPixel);

    /// <summary>Whether a density curve is filled: as its author chose, else as this look fills it.</summary>
    /// <param name="given">The choice its author gave, <see langword="null"/> when none.</param>
    /// <returns><see langword="true"/> when it is filled.</returns>
    internal bool DensityFillsOver(bool? given) => given ?? DensityFills;
}
