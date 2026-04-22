// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>A position/colour pair used to build a <see cref="LinearColorMap"/>. Consumed by
/// <see cref="LinearColorMap.FromList"/> and <see cref="LinearColorMap.FromPositions"/>.
/// Positions must be strictly increasing across a stop list; adjacent stops are linearly
/// interpolated between during colour lookup.</summary>
/// <param name="Position">Scalar anchor value. Typically in <c>[0, 1]</c>; <c>FromList</c>
/// auto-normalises arbitrary ranges, while <c>FromPositions</c> requires pre-normalised input.</param>
/// <param name="Color">Colour at this position.</param>
public readonly record struct ColorStop(double Position, Color Color);
