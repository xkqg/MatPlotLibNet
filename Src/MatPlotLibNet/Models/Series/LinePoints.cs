// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>The x and y values of a line, taken together, so the two arrays always describe the same points.</summary>
/// <param name="XData">The x values.</param>
/// <param name="YData">The y values, one per x value.</param>
internal readonly record struct LinePoints(double[] XData, double[] YData);
