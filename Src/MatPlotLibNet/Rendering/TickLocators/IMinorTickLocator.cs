// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>A locator that places the minor ticks of an axis drawn on its scale by a rule of its own rather than by splitting the step
/// between two major ticks, as matplotlib's log and logit scales bring a minor locator of their own.</summary>
internal interface IMinorTickLocator : ITickLocator
{
    /// <summary>The minor ticks of an axis that shows <paramref name="view"/> and whose major ticks stand at
    /// <paramref name="majors"/>: every minor position in view, less the ones a major tick stands on.</summary>
    /// <param name="view">The ends of the axis, whichever way it runs, in the unit of its ticks.</param>
    /// <param name="majors">The major ticks of the axis.</param>
    /// <param name="tickSpace">How many labels fit along the axis (matplotlib's <c>get_tick_space</c>).</param>
    /// <param name="theme">The theme the axis is drawn under.</param>
    /// <returns>The minor tick positions, ascending.</returns>
    double[] MinorsOver(Range1D view, double[] majors, double tickSpace, Theme theme);
}
