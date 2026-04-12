// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>Computes tick positions along an axis for a given data range.</summary>
public interface ITickLocator
{
    /// <summary>Returns tick positions within [<paramref name="min"/>, <paramref name="max"/>].</summary>
    /// <param name="min">The minimum value of the axis range.</param>
    /// <param name="max">The maximum value of the axis range.</param>
    /// <returns>An array of tick positions, sorted ascending, each within [<paramref name="min"/>, <paramref name="max"/>].</returns>
    double[] Locate(double min, double max);
}
