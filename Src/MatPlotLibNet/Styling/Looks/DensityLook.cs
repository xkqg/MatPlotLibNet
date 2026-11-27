// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>How a Gaussian kernel density is smoothed where its author names no bandwidth: a density curve, a violin's body, a pair
/// grid's diagonal. The matplotlib themes smooth it as scipy and matplotlib do (<see cref="Scipy"/>); the library's own themes as the
/// library always has (<see cref="Library"/>). A ridgeline's joint bandwidth is the library's own rule under every theme.</summary>
internal sealed record DensityLook : IThemeKeys<DensityLook>
{
    /// <inheritdoc />
    /// <remarks>The look is named as a whole: scipy's (<see cref="Scipy"/>) or the library's.</remarks>
    public void WriteKeys(ThemeDto keys) => keys.DensitiesLikeScipy = (this == Scipy).OrNullAtRest(false);

    /// <inheritdoc />
    public static DensityLook ReadKeys(ThemeDto keys) => keys.DensitiesLikeScipy == true ? Scipy : Library;

    /// <summary>The library's own densities: Silverman's rule of thumb.</summary>
    internal static DensityLook Library { get; } = new();

    /// <summary>The densities of scipy's <c>gaussian_kde</c> and matplotlib's <c>mlab.GaussianKDE</c> at their default
    /// <c>bw_method='scott'</c> (seaborn's <c>kdeplot</c> and <c>pairplot</c>, matplotlib's <c>violinplot</c>): Scott's factor.</summary>
    internal static DensityLook Scipy { get; } = new() { BandwidthFactor = GaussianKde.ScottFactor };

    /// <summary>The factor of the normal-reference bandwidth a density is smoothed with where its author names none
    /// (<see cref="GaussianKde.Bandwidth"/>).</summary>
    public double BandwidthFactor { get; init; } = GaussianKde.SilvermanFactor;
}
