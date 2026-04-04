// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Represents a 2D point with X and Y coordinates.</summary>
/// <param name="X">The horizontal coordinate.</param>
/// <param name="Y">The vertical coordinate.</param>
public readonly record struct Point(double X, double Y);

/// <summary>Represents a 2D size with width and height dimensions.</summary>
/// <param name="Width">The horizontal dimension.</param>
/// <param name="Height">The vertical dimension.</param>
public readonly record struct Size(double Width, double Height);

/// <summary>Represents an axis-aligned rectangle defined by its position and dimensions.</summary>
/// <param name="X">The left edge coordinate.</param>
/// <param name="Y">The top edge coordinate.</param>
/// <param name="Width">The horizontal dimension.</param>
/// <param name="Height">The vertical dimension.</param>
public readonly record struct Rect(double X, double Y, double Width, double Height);

/// <summary>Represents the computed data range for a set of axes.</summary>
/// <param name="XMin">Minimum X value.</param>
/// <param name="XMax">Maximum X value.</param>
/// <param name="YMin">Minimum Y value.</param>
/// <param name="YMax">Maximum Y value.</param>
public readonly record struct DataRange(double XMin, double XMax, double YMin, double YMax);
