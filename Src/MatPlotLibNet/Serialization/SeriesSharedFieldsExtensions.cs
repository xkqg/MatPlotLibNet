// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Serialization;

/// <summary>The fields every series shares, beside what its own type writes: its label and where it is shown when that is not
/// the legend, its place in the drawing order, whether it is shown at all, how many points an x/y series draws at most and the
/// keys of its rows, and the colour group it wears. Every series list (the primary, every other Y axis, the top axis, insets)
/// is written and read through here.</summary>
internal static class SeriesSharedFieldsExtensions
{
    extension(ISeries series)
    {
        /// <summary>The JSON's form of this series: what its own type writes (<c>ToSeriesDto</c>), with the shared fields
        /// beside it, each left out at its resting value.</summary>
        internal SeriesDto ToDtoWithSharedFields()
        {
            var dto = series.ToSeriesDto();
            dto.Label = series.Label;
            if (series is IHasLabelPlacement carrier)
                dto.LabelPlacement = carrier.LabelPlacement.OrNullAtRest(ChartSeries.RestingLabelPlacement)?.ToWireName();
            dto.Visible = series.Visible.OrNullAtRest(ChartSeries.RestingVisible);
            // A series of the library writes the z-order it was given (its resting value too, once given); another writes a z-order off 0.
            if (series is ChartSeries chart) chart.WriteSharedFields(dto);
            else dto.ZOrder = series.ZOrder.OrNullAtRest(0);
            return dto;
        }

        /// <summary>Reads the shared fields of <paramref name="dto"/> back onto this series. A field that is absent, or an
        /// unknown placement, leaves the value the series was created with.</summary>
        /// <param name="dto">The series as the JSON carries it.</param>
        internal void RestoreSharedFields(SeriesDto dto)
        {
            series.Label = dto.Label;
            if (series is IHasLabelPlacement carrier)
                if (dto.LabelPlacement.WireEnum<SeriesLabelPlacement>() is { } labelPlacement) carrier.LabelPlacement = labelPlacement;
            if (dto.ZOrder is { } zOrder) series.ZOrder = zOrder;
            if (dto.Visible is { } visible) series.Visible = visible;
            if (series is ChartSeries chart) chart.ReadSharedFields(dto);
        }
    }

    extension(IEnumerable<ISeries> series)
    {
        /// <summary>The JSON's form of every series of this list, in its order (<see cref="ToDtoWithSharedFields"/>).</summary>
        internal List<SeriesDto> ToDtos() => series.Select(s => s.ToDtoWithSharedFields()).ToList();
    }
}
