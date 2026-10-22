// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Serialization;

/// <summary>How the series that draw a hierarchy (treemap, sunburst, dendrogram) write the settings they share to the
/// figure JSON and take them back: the colour map and its limits, the normaliser, and whether the nodes are labelled.
/// The one rule for all three, so a setting added to <see cref="HierarchicalSeries"/> tomorrow is written once and a
/// series cannot carry its own copy of it.</summary>
internal static class HierarchicalSeriesExtensions
{
    /// <summary><paramref name="dto"/> with the settings every hierarchical series draws filled in; a setting at its
    /// resting value is left out, so a series nobody configured writes the bytes it wrote before.</summary>
    /// <param name="series">The series being written.</param>
    /// <param name="dto">The DTO holding the settings the series' own type adds.</param>
    internal static SeriesDto DescribeSharedSettings(this HierarchicalSeries series, SeriesDto dto) => dto with
    {
        ColorMapName = series.ColorMap.NameForJson(),
        ColorMap = series.ColorMap.DescriptionForJson(),
        VMin = series.VMin,
        VMax = series.VMax,
        ShowLabels = series.ShowLabels ? null : false,
        Normalizer = series.DescribeNormalizer(),
    };

    /// <summary>Gives <paramref name="series"/> the settings <paramref name="dto"/> carries; one the DTO does not carry
    /// leaves the series at the value it was created with.</summary>
    /// <param name="series">The series being read back.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    internal static void RestoreSharedSettings(this HierarchicalSeries series, SeriesDto dto)
    {
        series.RestoreColorMap(dto);
        series.VMin = dto.VMin;
        series.VMax = dto.VMax;
        if (dto.ShowLabels.HasValue)
        {
            series.ShowLabels = dto.ShowLabels.Value;
        }

        series.RestoreNormalizer(dto);
    }
}
