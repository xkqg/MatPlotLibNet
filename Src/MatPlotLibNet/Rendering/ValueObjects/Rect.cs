// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Represents an axis-aligned rectangle defined by its position and dimensions.</summary>
/// <param name="X">The left edge coordinate.</param>
/// <param name="Y">The top edge coordinate.</param>
/// <param name="Width">The horizontal dimension.</param>
/// <param name="Height">The vertical dimension.</param>
public readonly record struct Rect(double X, double Y, double Width, double Height);
