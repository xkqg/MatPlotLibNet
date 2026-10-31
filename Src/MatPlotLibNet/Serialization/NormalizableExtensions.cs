// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Serialization;

/// <summary>How a series that maps its values onto a colour map writes its normaliser to the figure JSON and takes it
/// back. The one rule for every <see cref="INormalizable"/> series, so a series added tomorrow writes and reads it the
/// same way and no series carries its own copy of the cast. The rule for the normaliser itself is shared with the one thing
/// that is no series but maps values the same way, an image placed on the figure.</summary>
internal static class NormalizableExtensions
{
    extension(INormalizer? normalizer)
    {
        /// <summary>What the JSON carries for <paramref name="normalizer"/>: the description it writes of itself, or
        /// <see langword="null"/> when there is none (it maps linearly) or it was written outside the library, which is code
        /// and not data and so stays out of the document.</summary>
        internal NormalizerDto? DescribeForJson() =>
            (normalizer as IJsonDescribed<NormalizerDto>)?.Describe();
    }

    extension(INormalizable series)
    {
        /// <summary>What the JSON carries for the normaliser of <paramref name="series"/>: the description the normaliser
        /// writes of itself, or <see langword="null"/> when the series has none (it maps linearly) or has one written outside
        /// the library, which is code and not data and so stays out of the document.</summary>
        internal NormalizerDto? DescribeNormalizer() => series.Normalizer.DescribeForJson();

        /// <summary>Gives <paramref name="series"/> the normaliser <paramref name="dto"/> describes. A DTO without one leaves the
        /// series on the normaliser it was created with; a kind this build does not know leaves it without one and is
        /// reported through the chart diagnostics.</summary>
        /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
        internal void RestoreNormalizer(SeriesDto dto)
        {
            if (dto.Normalizer is { } description)
            {
                series.Normalizer = description.Restore();
            }
        }
    }
}
