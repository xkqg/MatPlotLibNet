// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Styling;

/// <summary>A fill that blends from one colour to another along a straight line: <paramref name="StartColor"/> at
/// <paramref name="Start"/>, <paramref name="EndColor"/> at <paramref name="End"/>, the colours of the points between them
/// mixed in proportion to how far along the line the point lies, and the nearest end's colour beyond either end. The points
/// are in the units the shape that carries the gradient is drawn in, the pixels of the figure.</summary>
/// <param name="Start">Where <paramref name="StartColor"/> is reached.</param>
/// <param name="End">Where <paramref name="EndColor"/> is reached.</param>
/// <param name="StartColor">The colour at <paramref name="Start"/>.</param>
/// <param name="EndColor">The colour at <paramref name="End"/>.</param>
public readonly record struct LinearGradient(Point Start, Point End, Color StartColor, Color EndColor);
