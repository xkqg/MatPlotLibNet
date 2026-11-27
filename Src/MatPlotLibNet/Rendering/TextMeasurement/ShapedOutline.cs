// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TextMeasurement;

/// <summary>A text's glyph outline and the width it advances.</summary>
/// <param name="PathData">SVG path data with the baseline origin at (0, 0).</param>
/// <param name="Advance">The width from the origin to where the next text would start.</param>
internal readonly record struct ShapedOutline(string PathData, double Advance);
