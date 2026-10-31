// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The parts a correlation is made of, handed to the callback of <see cref="SignalAxesExtensions.Acorr"/> and
/// <see cref="SignalAxesExtensions.Xcorr"/> so they can be styled: the line, and the reference line at 0 when the correlation is
/// drawn as stems. Each is the very series (or overlay) on the axes, so a change made through it is the chart's.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct CorrelationParts
{
    /// <summary>Makes the parts of a correlation.</summary>
    /// <param name="line">The line of the stems, or of the markers.</param>
    /// <param name="zero">The reference line at 0, or <see langword="null"/> when the correlation is drawn as markers.</param>
    internal CorrelationParts(LineSeries line, ReferenceLine? zero)
    {
        Line = line;
        Zero = zero;
    }

    /// <summary>The one series of the chart, wearing one colour of the theme's cycle and named by one legend entry. As stems it
    /// is one polyline for each lag, from 0 to the correlation there, parted from the next by a missing point; as markers it is
    /// the lags and the correlations, drawn as circles with no line between them. What is set on it is set for every lag.</summary>
    public LineSeries Line { get; }

    /// <summary>The reference line at 0 that the stems stand on, the neutral grey of reference graphics and solid; it has no
    /// label, so the legend lists the chart once. <see langword="null"/> when the correlation is drawn as markers
    /// (<see cref="CorrelationOptions.Markers"/>), which has no such line: the two forms are never drawn together.</summary>
    public ReferenceLine? Zero { get; }
}
