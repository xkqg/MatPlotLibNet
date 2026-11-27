// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The way from one pixel to another: the step along each axis, how long the line is and, from those, which way it points
/// and which ways are square to it. Made by <see cref="PointExtensions.HeadingTo"/>, so an arrow head, a connector, a tick and a
/// quiver shaft all take their direction from one sum.</summary>
/// <param name="Dx">The step along X, from the first point to the second.</param>
/// <param name="Dy">The step along Y, from the first point to the second.</param>
/// <param name="Length">The length of the line: the square root of the sum of the two squares.</param>
internal readonly record struct Heading(double Dx, double Dy, double Length)
{
    /// <summary>The X share of the direction: <see cref="Dx"/> over <see cref="Length"/>. A line of no length has no direction, so it
    /// is nothing, not a division by zero; a step that is not a number stays not a number.</summary>
    internal double Ux => Length == 0 ? 0 : Dx / Length;

    /// <summary>The Y share of the direction: <see cref="Dy"/> over <see cref="Length"/>, nothing for a line of no length.</summary>
    internal double Uy => Length == 0 ? 0 : Dy / Length;

    /// <summary>The unit step along the line.</summary>
    internal Point Unit => new(Ux, Uy);

    /// <summary>The unit step square to the line, a quarter turn the way X turns into Y: <c>(-Uy, Ux)</c>. On a surface whose Y runs
    /// down the page it points to the right of a line that runs to the right.</summary>
    internal Point LeftNormal => new(-Uy, Ux);

    /// <summary>The unit step square to the line, the other way: <c>(Uy, -Ux)</c>.</summary>
    internal Point RightNormal => new(Uy, -Ux);
}
