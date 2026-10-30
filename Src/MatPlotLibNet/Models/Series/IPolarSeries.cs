// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Marker interface for series rendered in polar coordinates.</summary>
public interface IPolarSeries : ISeries
{
    /// <summary>The radius of each point, measured out from the centre of the polar axes.</summary>
    double[] R { get; }

    /// <summary>The angle of each point around the centre of the polar axes, in radians; one value per
    /// <see cref="R"/> entry.</summary>
    double[] Theta { get; }
}
