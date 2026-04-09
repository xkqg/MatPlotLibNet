// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>Computes tick positions along an axis for a given data range.</summary>
public interface ITickLocator
{
    /// <summary>Returns tick positions within [<paramref name="min"/>, <paramref name="max"/>].</summary>
    double[] Locate(double min, double max);
}
