// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Serialization;

/// <summary>How a series that maps its values to colours writes its colour scale to the figure JSON and takes it back: the map and
/// the normaliser as one <see cref="ColorScaleWire"/>, so no series lists the two halves, and one that has a map and no normaliser (a
/// graph, a contour of lines) says the same words.</summary>
internal static class ColorScaleExtensions
{
    extension(IColormappable series)
    {
        /// <summary>The fields the JSON carries for the colour map of this series; the normaliser stays absent.</summary>
        internal ColorScaleWire ColorScaleOnTheWire() => ColorScaleWire.Of(series.ColorMap, null);

        /// <summary>Gives this series the colour map <paramref name="dto"/> writes; a DTO that writes none leaves the series on the
        /// map it was created with.</summary>
        /// <exception cref="ArgumentException">The description cannot make a map; the exception names the field.</exception>
        internal void RestoreColorScale(SeriesDto dto)
        {
            if (dto.ColorScale.Map.IsWritten)
            {
                series.ColorMap = dto.ColorScale.ReadMap();
            }
        }
    }

    extension<TSeries>(TSeries series) where TSeries : IColormappable, INormalizable
    {
        /// <summary>The fields the JSON carries for the colour map and the normaliser of this series.</summary>
        internal ColorScaleWire ColorScaleOnTheWire() => ColorScaleWire.Of(series.ColorMap, series.Normalizer);

        /// <summary>Gives this series the colour map and the normaliser <paramref name="dto"/> writes; one the DTO does not write
        /// leaves the series on the one it was created with.</summary>
        /// <exception cref="ArgumentException">The description cannot make a map; the exception names the field.</exception>
        internal void RestoreColorScale(SeriesDto dto)
        {
            var scale = dto.ColorScale;
            if (scale.Map.IsWritten)
            {
                series.ColorMap = scale.ReadMap();
            }

            if (scale.Normalizer is not null)
            {
                series.Normalizer = scale.ReadNormalizer();
            }
        }
    }
}
