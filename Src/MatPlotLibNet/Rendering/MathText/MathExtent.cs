// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>The room a laid-out box of math takes, as TeX measures a box: its width along the baseline, how far it rises above the
/// baseline and how far it reaches below it, in pixels.</summary>
/// <param name="Width">The width.</param>
/// <param name="Height">The height above the baseline.</param>
/// <param name="Depth">The depth below the baseline.</param>
internal readonly record struct MathExtent(double Width, double Height, double Depth);
