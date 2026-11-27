// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet;

/// <summary>The verbs of the ticks of one axis (<see cref="AxisVerbs.Ticks"/>): where they stand, how they are written, which edges
/// carry them and whether they are drawn, each written once for every axis a builder configures.</summary>
/// <param name="Axis">The axis whose ticks the verbs set.</param>
internal readonly record struct TickVerbs(Axis Axis)
{
    /// <summary>Sets the formatter of the axis' tick labels.</summary>
    internal void Formatter(ITickFormatter formatter) => Axis.TickFormatter = formatter;

    /// <summary>Sets the locator of the axis' ticks.</summary>
    internal void Locator(ITickLocator locator) => Axis.TickLocator = locator;

    /// <summary>Rotates the axis' tick labels by <paramref name="degrees"/>.</summary>
    internal void LabelRotation(double degrees) => Axis.MajorTicks = Axis.MajorTicks with { LabelRotation = degrees };

    /// <summary>Draws the axis' ticks and labels on the opposite edge as well.</summary>
    internal void Mirror() => Axis.MajorTicks = Axis.MajorTicks with { Mirror = true };

    /// <summary>Moves the axis' tick labels to the opposite edge and keeps the marks on both.</summary>
    internal void LabelsOnTheOtherSide() => Axis.MajorTicks = Axis.MajorTicks with { Mirror = true, LabelsOnOwnSide = false };

    /// <summary>Shows or hides the axis' minor tick marks.</summary>
    internal void Minor(bool visible) => Axis.MinorTicks = Axis.MinorTicks with { Visible = visible };

    /// <summary>Hides every tick mark and tick label of the axis, major and minor.</summary>
    internal void Hide()
    {
        Axis.MajorTicks = Axis.MajorTicks with { Visible = false };
        Minor(false);
    }
}
