// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The way a text is turned. The library turns it counter-clockwise (<see cref="TextPlacement.Rotation"/>, matplotlib's
/// <c>rotation</c>); every canvas it draws on turns clockwise (SVG's <c>rotate</c>, Skia's <c>RotateDegrees</c>, MAUI's <c>Rotate</c>),
/// so each is given the opposite angle, written here once.</summary>
internal static class TextTurnExtensions
{
    extension(double counterClockwiseDegrees)
    {
        /// <summary>The same turn as a canvas that turns clockwise is told it.</summary>
        /// <returns>The angle in degrees, clockwise.</returns>
        internal double ToClockwise() => -counterClockwiseDegrees;
    }
}
