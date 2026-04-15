// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Represents a 2D size with width and height dimensions.</summary>
/// <param name="Width">The horizontal dimension.</param>
/// <param name="Height">The vertical dimension.</param>
public readonly record struct Size(double Width, double Height)
{
    /// <summary>A zero-sized extent — used as a "nothing to measure" sentinel by
    /// <see cref="Layout.LegendMeasurer.MeasureBox"/> and other measurement helpers that
    /// want a clearly-empty return value.</summary>
    public static Size Empty => new(0, 0);
}
