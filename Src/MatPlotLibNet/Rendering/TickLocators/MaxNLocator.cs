// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>
/// Produces nice-number ticks like <see cref="AutoLocator"/> but caps the result to at most
/// <c>maxN</c> positions. The target count is increased progressively until the output fits
/// the cap — guaranteeing at most <c>maxN</c> ticks while still choosing aligned values.
/// </summary>
public sealed class MaxNLocator : ITickLocator, IJsonDescribed<TickLocatorDto>, IRoundingLocator
{
    private readonly int _maxN;


    /// <summary>How many label heights a tick takes along a vertical scale: the label and a label's height of room
    /// (<c>YAxis.get_tick_space</c>: "Having a spacing of at least 2 just looks good").</summary>
    private const double VerticalLabelSpan = 2;

    /// <summary>How many label heights a tick takes along a horizontal scale, where a label takes about its width, which
    /// is more than its height (<c>XAxis.get_tick_space</c>: "the aspect ratio of tick text is no more than 3:1").</summary>
    private const double HorizontalLabelSpan = 3;

    /// <summary>The label span along each direction: <see cref="HorizontalLabelSpan"/> along X, <see cref="VerticalLabelSpan"/> along Y.</summary>
    private static readonly AxisPair<double> LabelSpans = new(HorizontalLabelSpan, VerticalLabelSpan);

    /// <summary>How many label sizes one tick takes along an axis that runs along X (<paramref name="alongX"/>: three, matplotlib's
    /// <c>XAxis.get_tick_space</c>) or along Y (two, <c>YAxis.get_tick_space</c>): the one place a direction becomes a span. A 3-D axis
    /// counts as X (matplotlib's 3-D axis is an <c>XAxis</c>), the rings of a polar axes as Y (its radial axis is a <c>YAxis</c>), a colour
    /// bar as the way it runs.</summary>
    /// <param name="alongX">Whether the axis runs along X.</param>
    /// <returns>The span, in label sizes.</returns>
    internal static double LabelSpanAlong(bool alongX) => LabelSpans.Along(alongX);

    /// <summary>Initialises with the maximum number of ticks to return.</summary>
    /// <param name="maxN">The upper bound on the number of tick positions returned by <see cref="Locate"/>.</param>
    public MaxNLocator(int maxN) => _maxN = maxN;

    /// <summary>The locator for a scale drawn along a length with room for <paramref name="tickSpace"/> labels (<see cref="LabelsAlong"/>;
    /// a colour bar, the rings of a polar axes): that many ticks, held between two and nine (<see cref="TickBudget.Standard"/>; the most
    /// for a room that is no number or without end). A scale is not as dense as an axis on purpose: it is a short ruler beside a chart
    /// (<see cref="TickAxisKind.Ruler"/>).</summary>
    /// <param name="tickSpace">How many labels fit along the scale.</param>
    /// <returns>The locator.</returns>
    internal static MaxNLocator ForTickSpace(double tickSpace) => new(TickBudget.Standard.Clamp(tickSpace));

    /// <summary>How many ticks fit along a length, each taking the label sizes of its direction (<see cref="LabelSpanAlong"/>): matplotlib's
    /// <c>Axis.get_tick_space</c> (<c>floor(length / (label size * 3))</c> along X, <c>* 2</c> along Y; the ratio of two lengths, so
    /// pixels serve as points do). The one rule every kind of axis counts the room for its labels by (<see cref="TickRequest.TickSpace"/>).</summary>
    /// <param name="lengthPx">The length, in pixels.</param>
    /// <param name="labelPx">The size of a label, in pixels.</param>
    /// <param name="alongX">Whether the length runs along X.</param>
    /// <returns>The whole number of ticks that fit; for a label of no size, which takes no room, <c>2**31 - 1</c>, as matplotlib's
    /// <c>get_tick_space</c> answers it.</returns>
    internal static double LabelsAlong(double lengthPx, double labelPx, bool alongX)
    {
        double size = LabelSpanAlong(alongX) * labelPx;
        return size > 0 ? Math.Floor(lengthPx / size) : int.MaxValue;
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

    /// <inheritdoc />
    /// <remarks>The ends of the ticks it lays (matplotlib's <c>MaxNLocator.view_limits</c> under <c>round_numbers</c>): the
    /// multiples, held around the range, of the step of the first target from <c>maxN</c> down whose ticks fit the cap, the step
    /// <see cref="Locate"/> takes.</remarks>
    Range1D IRoundingLocator.RoundedOut(Range1D range)
    {
        for (int target = _maxN; target > 1; target--)
        {
            var locator = new AutoLocator(target);
            if (locator.Locate(range.Lo, range.Hi).Length <= _maxN)
                return ((IRoundingLocator)locator).RoundedOut(range);
        }

        return ((IRoundingLocator)new AutoLocator(1)).RoundedOut(range);
    }
}
