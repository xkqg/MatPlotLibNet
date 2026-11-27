// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>The centre line and the two control limits of a control chart, in the units of its values, given to
/// <see cref="XYAxesExtensions.ControlChart"/> to draw them as they are instead of estimating them from the values: the
/// limits of an earlier period the process is held to, or limits set by a specification.
/// <para>The three are finite numbers with <see cref="Lower"/> at or below <see cref="Center"/> and <see cref="Center"/> at
/// or below <see cref="Upper"/>; the verb refuses any other.</para></summary>
/// <param name="Center">The centre line: where the process is expected to run.</param>
/// <param name="Lower">The lower control limit (LCL): a value below it is a violation.</param>
/// <param name="Upper">The upper control limit (UCL): a value above it is a violation.</param>
public readonly record struct ControlLimits(double Center, double Lower, double Upper)
{
    /// <summary>Refuses limits that are not three finite numbers with Lower &lt;= Center &lt;= Upper: a centre outside its limits,
    /// or a lower limit above the upper, marks every value or none.</summary>
    /// <returns>The same limits, for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A limit is not finite, or the three are not in that order; the message names the
    /// one refused and its value.</exception>
    internal ControlLimits ThrowIfNotOrdered()
    {
        Lower.ThrowIfNotFinite(Limits, "The lower limit");
        Upper.ThrowIfNotFinite(Limits, "The upper limit").ThrowIfBelow(Lower, Limits, "The upper limit");
        Center.ThrowIfNotFinite(Limits, "The centre line").ThrowIfNotIn(Lower, Upper, Limits, "The centre line");
        return this;
    }

    /// <summary>The parameter the limits are given in, named by a refusal.</summary>
    private const string Limits = "limits";
}
