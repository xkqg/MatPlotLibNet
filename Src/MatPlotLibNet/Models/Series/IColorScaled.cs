// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>A series (or a figure image) that maps its values to colours through a colour map and a normaliser, and says over which of
/// its values the colours run: what the library adds to the public <see cref="IColormappable"/> and <see cref="INormalizable"/>. Its
/// colour scale is worked out in one place (<see cref="ColorScaledExtensions.ColorScaleUnder"/>), which its renderer, its colour bar and
/// a model that colours a part of it read. Internal, so an application can still implement the public interfaces.</summary>
internal interface IColorScaled : IColormappable, INormalizable
{
    /// <summary>The range the colours run over when they are mapped by <paramref name="mapping"/>: the kind's own values (and the ends an
    /// author gave it) through <see cref="ColorMapping.RangeOf(Rendering.Range1D?, double?, double?)"/>, which opens a range of no width
    /// one wide (<see cref="ColorMapping.Opened"/>).</summary>
    /// <param name="mapping">The map, the normaliser and the theme the series is drawn under.</param>
    /// <returns>The range, with width.</returns>
    MinMaxRange ColourRangeOver(ColorMapping mapping);
}

/// <summary>The one owner of the colour scale of a series that maps its values to colours: the map it draws with (its own, else the
/// theme's, <see cref="ColorMapLook.Of"/>), the normaliser the theme reads it as (<c>NormalizerUnder</c>) and the range its colours run
/// over (<see cref="IColorScaled.ColourRangeOver"/>). No renderer or model puts the three together itself.</summary>
internal static class ColorScaledExtensions
{
    extension(IColormappable series)
    {
        /// <summary>The colour map this series draws with under <paramref name="theme"/>: the one it names, else the theme's
        /// (<see cref="ColorMapLook.Of"/>). For a kind that colours over its levels or its own fractions (a contour, a graph).</summary>
        /// <param name="theme">The theme the series is drawn under.</param>
        /// <returns>The map.</returns>
        internal IColorMap ColorMapUnder(Theme theme) => theme.Looks.ColorMap.Of(series.ColorMap);

        /// <summary>How the values of this series become colours under <paramref name="theme"/> when it colours them linearly (a contour of
        /// lines over its levels, a 3-D contour): its map (<c>ColorMapUnder</c>) and the linear normaliser.</summary>
        /// <param name="theme">The theme the series is drawn under.</param>
        /// <returns>The mapping.</returns>
        internal ColorMapping LinearMappingUnder(Theme theme) => ColorMapping.Linear(series.ColorMap, theme);
    }

    extension<TSeries>(TSeries series) where TSeries : IColormappable, INormalizable
    {
        /// <summary>How the values of this series become colours under <paramref name="theme"/>: its map (<c>ColorMapUnder</c>), the
        /// normaliser the theme reads its own as (<c>NormalizerUnder</c>) and the theme.</summary>
        /// <param name="theme">The theme the series is drawn under.</param>
        /// <returns>The mapping.</returns>
        internal ColorMapping ColorMappingUnder(Theme theme) =>
            new(series.ColorMapUnder(theme), series.NormalizerUnder(theme), theme);

        /// <summary>The colour scale of this series over values its renderer has already worked out (the counts of a 2-D histogram, the
        /// magnitudes of a spectrogram), so they are not worked out again: the range of those the scale can place.</summary>
        /// <param name="theme">The theme the series is drawn under.</param>
        /// <param name="values">The values the colours run over.</param>
        /// <returns>The colour scale.</returns>
        internal ColormapContext ColorScaleUnder(Theme theme, double[,] values)
        {
            var mapping = series.ColorMappingUnder(theme);
            return mapping.Over(mapping.RangeOf(values));
        }

        /// <inheritdoc cref="ColorScaleUnder{TSeries}(TSeries, Theme, double[,])"/>
        internal ColormapContext ColorScaleUnder(Theme theme, int[,] values)
        {
            var mapping = series.ColorMappingUnder(theme);
            return mapping.Over(mapping.RangeOf(values));
        }
    }

    extension(IColorScaled series)
    {
        /// <summary>The range the colours of this series run over under <paramref name="theme"/>: the range its renderer colours over
        /// and its colour bar names.</summary>
        /// <param name="theme">The theme the series is drawn under.</param>
        /// <returns>The range.</returns>
        internal MinMaxRange ColourRangeUnder(Theme theme) => series.ColourRangeOver(series.ColorMappingUnder(theme));

        /// <summary>The colour scale this series is drawn with under <paramref name="theme"/>: its mapping (<c>ColorMappingUnder</c>)
        /// over the range its values give (<see cref="IColorScaled.ColourRangeOver"/>).</summary>
        /// <param name="theme">The theme the series is drawn under.</param>
        /// <returns>The colour scale.</returns>
        internal ColormapContext ColorScaleUnder(Theme theme)
        {
            var mapping = series.ColorMappingUnder(theme);
            return mapping.Over(series.ColourRangeOver(mapping));
        }
    }
}
