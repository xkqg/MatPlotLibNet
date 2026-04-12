// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Data;

/// <summary>A named, color-coded subset of XY data — one entry per unique hue category.</summary>
/// <param name="Label">The category label (displayed in the legend).</param>
/// <param name="Color">The color assigned to this group from the active palette.</param>
/// <param name="X">X values for all observations in this group.</param>
/// <param name="Y">Y values for all observations in this group.</param>
public sealed record HueGroup(string Label, Color Color, double[] X, double[] Y);
