// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>The series an axes was given last, found by its kind: what a verb that acts on "the series just added" (a bar's labels, an XY
/// series' downsampling, the colour map or normaliser of a mappable one, the prices an indicator is computed over) asks, so each says
/// that it does nothing when there is none and none searches the list itself.</summary>
internal static class AxesLastSeriesExtensions
{
    extension(Axes axes)
    {
        /// <summary>The series of kind <typeparamref name="T"/> added last, wherever it stands among the series added after it.</summary>
        /// <typeparam name="T">The kind of series: a class or an interface.</typeparam>
        /// <returns>The series, or <see langword="null"/> when none is of the kind.</returns>
        internal T? LastSeriesOf<T>() where T : class
        {
            for (int i = axes.Series.Count - 1; i >= 0; i--)
            {
                if (axes.Series[i] is T found) return found;
            }

            return null;
        }

        /// <summary>The series added last, when it is of kind <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">The kind of series: a class or an interface.</typeparam>
        /// <returns>The series, or <see langword="null"/> when there is none or the last is of another kind.</returns>
        internal T? LastSeriesIs<T>() where T : class => axes.Series.Count == 0 ? null : axes.Series[^1] as T;
    }
}
