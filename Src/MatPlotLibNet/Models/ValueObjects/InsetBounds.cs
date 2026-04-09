// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Defines the position and size of an inset axes as fractions (0-1) of the parent axes.</summary>
/// <param name="X">Horizontal position as a fraction of the parent width (0 = left, 1 = right).</param>
/// <param name="Y">Vertical position as a fraction of the parent height (0 = top, 1 = bottom).</param>
/// <param name="Width">Width as a fraction of the parent width.</param>
/// <param name="Height">Height as a fraction of the parent height.</param>
public readonly record struct InsetBounds(double X, double Y, double Width, double Height);
