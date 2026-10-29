// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>The one rule for which series a colour bar reads: the first that provides one and has values to back it.</summary>
internal static class ColorBarProviderExtensions
{
    extension(IEnumerable<ISeries> series)
    {
        /// <summary>The series the colour bar reads its range and its colour map from.</summary>
        /// <returns>The first series that is a colour bar's data provider and has values (<see cref="ChartSeries.ProvidesColorBar"/>);
        /// <see langword="null"/> when there is none, and the bar takes its own limits and the default map.</returns>
        internal IColorBarDataProvider? ColorBarProvider() =>
            series.OfType<IColorBarDataProvider>().FirstOrDefault(provider => provider is not ChartSeries { ProvidesColorBar: false });
    }

    extension(IColorBarDataProvider? provider)
    {
        /// <summary>The normaliser the series that backs a colour bar colours its values with: the one it names, otherwise the
        /// linear one, which is also the bar's when no series backs it. A tick stands where this normaliser sends its value, so
        /// the bar and the series cannot read the colour of a value differently.</summary>
        internal INormalizer ColorBarNormalizer() =>
            provider is INormalizable normalizable ? normalizable.NormalizerOrLinear() : LinearNormalizer.Instance;
    }
}
