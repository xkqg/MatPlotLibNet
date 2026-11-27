// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>How a series' given stroke (<see cref="GivenStroke"/>) is written to the JSON and read back: its width as
/// <c>lineWidth</c> and its dash pattern as <c>lineStyle</c>, each only while it was given. The one writer and the one reader of
/// that form, for every kind that holds a given stroke.</summary>
internal static class GivenStrokeDtoExtensions
{
    extension(GivenStroke stroke)
    {
        /// <summary>The dash pattern as the JSON names it (the member's name in lower case); <see langword="null"/> while none was
        /// given.</summary>
        internal string? StyleName => stroke.Style?.ToWireName();
    }

    extension(SeriesDto dto)
    {
        /// <summary>The stroke this DTO gives a series, laid over the one the series holds (<see cref="GivenStroke.Over"/>): a key that
        /// is absent keeps the series' part, and so does a dash name that is no <see cref="LineStyle"/>, which is announced as every
        /// unknown name the reader meets is.</summary>
        /// <param name="held">The stroke the series holds.</param>
        /// <returns>The stroke the series holds after reading this DTO.</returns>
        internal GivenStroke StrokeOver(GivenStroke held)
        {
            return dto.Stroking.Over(held);
        }
    }
}
