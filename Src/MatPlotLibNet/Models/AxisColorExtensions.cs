// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>The one rule that colours an axis: its tick marks, its tick labels and its title. The spine is the
/// edge of the axes, not a part of <see cref="Axis"/>, so each builder verb adds only its own.</summary>
internal static class AxisColorExtensions
{
    /// <summary>Colours the tick marks, the tick labels and the title of <paramref name="axis"/> with
    /// <paramref name="color"/>, keeping every other tick and label setting it already carries.</summary>
    /// <param name="axis">The axis to colour; a missing <see cref="Axis.LabelStyle"/> is created.</param>
    /// <param name="color">The colour all three take.</param>
    internal static void ApplyAxisColor(this Axis axis, Color color)
    {
        axis.MajorTicks = axis.MajorTicks with { Color = color, LabelColor = color };
        axis.LabelStyle = (axis.LabelStyle ?? new TextStyle()) with { Color = color };
    }
}
