// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>How a filled contour's colour bar colours its bands, as matplotlib's <c>Colorbar</c> draws a <c>ContourSet</c> (3.11.2:
/// <c>boundaries = cs._levels</c>, <c>values = cs.cvalues</c>, <c>ticks = FixedLocator(cs.levels, nbins=10)</c>): the bar is one block
/// per band, as tall as the next whatever the levels, each in the colour of the middle of its two levels (the geometric mean under a log norm) over the levels, with a
/// tick on the levels thinned to ten. A value is in the band its two levels hold it (one below the first level in the lowest
/// band, one above the last in the highest), and takes the colour of that band. Internal: the series hands it to the bar under
/// the themes that draw the bar so.</summary>
/// <param name="levels">The levels that bound the bands, ascending; at least two.</param>
/// <param name="colours">The normaliser the series colours its values with, which places the middle of a band on the colour map.</param>
internal sealed class ContourBandNormalizer(double[] levels, INormalizer colours) : INormalizer, IDiscreteNormalizer
{
    /// <inheritdoc />
    double[] IDiscreteNormalizer.Boundaries => levels;

    /// <inheritdoc />
    int IDiscreteNormalizer.MostNamed => IDiscreteNormalizer.MostTicks;

    /// <inheritdoc />
    double IDiscreteNormalizer.MapFractionOf(int band) => FractionOf(band);

    /// <inheritdoc />
    public double Normalize(double value, double min, double max)
    {
        int band = 0;
        while (band < levels.Length - 2 && value >= levels[band + 1]) band++;
        return FractionOf(band);
    }

    /// <summary>The fraction of the map the middle of <paramref name="band"/> is at, over the first level to the last: the
    /// middle the fill's colour is taken at.</summary>
    private double FractionOf(int band) =>
        colours.Normalize(colours.MiddleOf(levels[band], levels[band + 1]), levels[0], levels[^1]);
}
