// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The one place an <see cref="AxisScale"/> is turned into what the scale does: its <see cref="ScaleRule"/>. Every reader of a
/// scale (the pixel mapping, the ticks, the range an axis draws, the view the server-interaction script reads, the range a scale of
/// colours is taken over) asks the rule, and one that maps many values holds the rule it asked once.</summary>
internal static class AxisScaleExtensions
{
    extension(AxisScale scale)
    {
        /// <summary>The rule of this scale; a scale the library does not know is read as linear.</summary>
        internal ScaleRule Rule => scale switch
        {
            AxisScale.Log    => ScaleRule.Log,
            AxisScale.SymLog => ScaleRule.SymLog,
            AxisScale.Logit  => ScaleRule.Logit,
            AxisScale.Date   => ScaleRule.Date,
            _                => ScaleRule.Linear,
        };

        /// <summary>The rule of this scale bound to <paramref name="linthresh"/>: a symmetric-log scale's rule holds it
        /// (<see cref="ScaleRule.SymLogAt"/>), every other scale's is its <c>Rule</c>. What a reader that maps values asks.</summary>
        /// <param name="linthresh">The symlog threshold (<see cref="Axis.SymLogLinThresh"/>).</param>
        /// <returns>The rule.</returns>
        internal ScaleRule RuleAt(double linthresh) => scale switch
        {
            AxisScale.SymLog => ScaleRule.SymLogAt(linthresh),
            _                => scale.Rule,
        };
    }

    extension(Axis axis)
    {
        /// <summary>The rule of the axis' scale under <paramref name="theme"/>, bound to the threshold it is drawn with
        /// (<see cref="SymLogLinThreshUnder"/>, <c>RuleAt</c>): the one rule every reader that maps the axis' values, rounds its range or
        /// asks for the locator its scale brings holds.</summary>
        /// <param name="theme">The theme the axis is drawn under.</param>
        /// <returns>The rule.</returns>
        internal ScaleRule ScaleRuleUnder(Theme theme) => axis.Scale.RuleAt(axis.SymLogLinThreshUnder(theme));

        /// <summary>The symlog threshold the axis is drawn with under <paramref name="theme"/>: the one its author gave, else the theme's
        /// (<see cref="AxisRangeLook.SymLogLinThresh"/>: matplotlib's 2 under the matplotlib themes), else the library's
        /// <see cref="SymlogTransform.DefaultLinThresh"/>.</summary>
        /// <param name="theme">The theme the axis is drawn under.</param>
        /// <returns>The threshold.</returns>
        internal double SymLogLinThreshUnder(Theme theme) =>
            theme.Looks.Range.SymLogLinThreshOver(axis.GivenSymLogLinThresh, SymlogTransform.DefaultLinThresh);
    }
}
