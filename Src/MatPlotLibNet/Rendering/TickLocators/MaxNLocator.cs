// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>
/// Produces nice-number ticks like <see cref="AutoLocator"/> but caps the result to at most
/// <c>maxN</c> positions. The target count is increased progressively until the output fits
/// the cap — guaranteeing at most <c>maxN</c> ticks while still choosing aligned values.
/// </summary>
public sealed class MaxNLocator : ITickLocator, IJsonDescribed<TickLocatorDto>
{
    private readonly int _maxN;

    /// <summary>The fewest ticks a scale drawn along a length asks for.</summary>
    internal const int FewestTicks = 2;

    /// <summary>The most ticks a scale drawn along a length asks for.</summary>
    internal const int MostTicks = 9;

    /// <summary>How many label heights a tick takes along a vertical scale: the label and a label's height of room.</summary>
    internal const double VerticalLabelSpan = 2;

    /// <summary>How many label heights a tick takes along a horizontal scale, where a label takes about its width, which
    /// is more than its height.</summary>
    internal const double HorizontalLabelSpan = 3;

    /// <summary>Initialises with the maximum number of ticks to return.</summary>
    /// <param name="maxN">The upper bound on the number of tick positions returned by <see cref="Locate"/>.</param>
    public MaxNLocator(int maxN) => _maxN = maxN;

    /// <summary>The locator for a scale that is drawn along a length (a colour bar, a polar axis): as many ticks as fit,
    /// each taking <paramref name="labelSpan"/> label heights, but at least <see cref="FewestTicks"/> and at most
    /// <see cref="MostTicks"/>. A scale is not as dense as an axis on purpose: it is a short ruler beside a chart.</summary>
    /// <param name="lengthPx">The length of the scale in pixels.</param>
    /// <param name="tickFontPx">The size of the tick font in pixels.</param>
    /// <param name="labelSpan">How many label heights one tick takes (<see cref="VerticalLabelSpan"/> or
    /// <see cref="HorizontalLabelSpan"/>).</param>
    internal static MaxNLocator ForLength(double lengthPx, double tickFontPx, double labelSpan)
    {
        double fit = Math.Floor(lengthPx / (labelSpan * tickFontPx));
        return new MaxNLocator(double.IsFinite(fit) ? (int)Math.Clamp(fit, FewestTicks, MostTicks) : FewestTicks);
    }

    /// <inheritdoc />
    TickLocatorDto IJsonDescribed<TickLocatorDto>.Describe() => new MaxNTickLocatorDto(_maxN);

    /// <inheritdoc />
    /// <remarks>
    /// Iteratively decreases the target count from <c>maxN</c> down to 1 until the nice-number
    /// algorithm produces no more than <c>maxN</c> positions. This guarantees the cap is respected
    /// while still aligning ticks to round values — it does not skip or thin an existing set.
    /// </remarks>
    public double[] Locate(double min, double max)
    {
        // Start from maxN and reduce the target until ticks fit.
        for (int target = _maxN; target >= 1; target--)
        {
            double[] ticks = new AutoLocator(target).Locate(min, max);
            if (ticks.Length <= _maxN)
                return ticks;
        }
        return [min];
    }
}
