// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>The one rule that maps a data value into the space an axis is drawn in and back: log10 for
/// <see cref="AxisScale.Log"/>, <see cref="SymlogTransform"/> for <see cref="AxisScale.SymLog"/>, the value itself for
/// every other scale (the Cartesian renderer draws linear, date and logit axes linearly). The pixel mapping
/// (<see cref="DataTransform"/>), the range the renderer draws, the view the server-interaction script converts with and
/// a pan applied to an axis all go through it.</summary>
internal static class AxisScaleExtensions
{
    /// <summary>Maps <paramref name="value"/> from data units into the space <paramref name="scale"/> draws in.</summary>
    /// <param name="scale">The axis scale.</param>
    /// <param name="value">A data value.</param>
    /// <param name="linthresh">The symlog linear threshold (<see cref="Axis.SymLogLinThresh"/>); ignored otherwise.</param>
    /// <returns>The scaled value; NaN for a non-positive value on a log axis.</returns>
    internal static double Forward(this AxisScale scale, double value, double linthresh) => scale switch
    {
        AxisScale.SymLog => SymlogTransform.Forward(value, linthresh),
        AxisScale.Log    => value > 0 ? Math.Log10(value) : double.NaN,
        _                => value,
    };

    /// <summary>Maps <paramref name="value"/> from the space <paramref name="scale"/> draws in back to data units: the
    /// inverse of <see cref="Forward"/>.</summary>
    /// <param name="scale">The axis scale.</param>
    /// <param name="value">A value in the drawn space.</param>
    /// <param name="linthresh">The symlog linear threshold; ignored otherwise.</param>
    /// <returns>The data value.</returns>
    internal static double Inverse(this AxisScale scale, double value, double linthresh) => scale switch
    {
        AxisScale.SymLog => SymlogTransform.Inverse(value, linthresh),
        AxisScale.Log    => Math.Pow(10, value),
        _                => value,
    };

    /// <summary>The name the server-interaction view writes for a scale whose drawn space is not the data's own
    /// (<c>log</c>, <c>symlog</c>); <see langword="null"/> for a scale drawn linearly, which writes nothing.</summary>
    /// <param name="scale">The axis scale.</param>
    /// <returns>The view's scale name, or <see langword="null"/>.</returns>
    internal static string? ViewName(this AxisScale scale) => scale switch
    {
        AxisScale.SymLog => "symlog",
        AxisScale.Log    => "log",
        _                => null,
    };
}
