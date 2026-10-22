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
        AxisScale.Log    => scale.HasPlaceFor(value) ? Math.Log10(value) : double.NaN,
        _                => value,
    };

    /// <summary>Whether an axis of <paramref name="scale"/> has a place for the finite <paramref name="value"/>: every
    /// value but 0 and the negative values on a log axis (a symmetric-log axis places them; the Cartesian renderer draws a
    /// logit and a date axis linearly). The one rule the mapping into the drawn space (<see cref="Forward"/>), the range an
    /// axis folds (<see cref="MissingValueExtensions.Including"/>) and the clipping of a mark's base
    /// (<see cref="DataTransform.DataToPixelClippedY"/>) share. A missing value or an infinity has no place on any axis
    /// (<see cref="MissingValueExtensions.IsDrawable(double)"/>); this says nothing about them.</summary>
    /// <param name="scale">The axis scale.</param>
    /// <param name="value">A finite data value.</param>
    /// <returns><see langword="false"/> for 0 and below on a log axis; <see langword="true"/> otherwise.</returns>
    internal static bool HasPlaceFor(this AxisScale scale, double value) => scale != AxisScale.Log || value > 0;

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
