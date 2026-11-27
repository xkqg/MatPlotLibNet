// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Serialization;

/// <summary>How a normaliser is described for the figure JSON: the half of a colour scale that <see cref="ColorScaleWire"/> writes
/// beside the colour map, for every series that maps its values onto a colour map and for the one thing that is no series but maps
/// values the same way, an image placed on the figure.</summary>
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
}
