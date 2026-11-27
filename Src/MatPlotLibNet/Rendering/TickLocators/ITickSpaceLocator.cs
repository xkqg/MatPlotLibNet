// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>A locator whose ticks depend on how many labels fit along the axis, as matplotlib's <c>nbins='auto'</c> reads
/// <c>Axis.get_tick_space</c>. The axis asks it with that count where it knows the plot's length. The two that do (a log and a logit
/// scale's) name some of their scale's minor ticks among their ticks, and say which (<see cref="NamesAsMinor"/>).</summary>
/// <remarks>A separate internal interface, not a member of the public <see cref="ITickLocator"/>, so that an application can still
/// write its own locator with the one method.</remarks>
internal interface ITickSpaceLocator : ITickLocator
{
    /// <summary>The ticks between <paramref name="min"/> and <paramref name="max"/> on an axis with room for
    /// <paramref name="tickSpace"/> labels, drawn under <paramref name="theme"/>.</summary>
    /// <param name="min">The low end of the range.</param>
    /// <param name="max">The high end of the range.</param>
    /// <param name="tickSpace">How many labels fit along the axis (<see cref="MaxNLocator.LabelsAlong"/>).</param>
    /// <param name="theme">The theme the axis is drawn under.</param>
    /// <returns>The tick positions, ascending.</returns>
    double[] LocateWithSpace(double min, double max, double tickSpace, Theme theme);

    /// <summary>Whether <paramref name="tick"/>, one of the ticks this locator names over <paramref name="view"/>, is one of its scale's
    /// minor ticks with a label rather than a major tick: matplotlib draws it as a minor tick, with a minor mark and the minor pad.</summary>
    /// <param name="tick">One of the ticks the locator names.</param>
    /// <param name="view">The ends of the view the ticks were named over.</param>
    /// <returns><see langword="true"/> for a labelled minor tick.</returns>
    bool NamesAsMinor(double tick, Range1D view);
}
