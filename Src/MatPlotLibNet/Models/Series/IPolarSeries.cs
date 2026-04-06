// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Marker interface for series rendered in polar coordinates.</summary>
public interface IPolarSeries : ISeries
{
    /// <summary>Gets the radial data values.</summary>
    double[] R { get; }

    /// <summary>Gets the angular data values in radians.</summary>
    double[] Theta { get; }
}
