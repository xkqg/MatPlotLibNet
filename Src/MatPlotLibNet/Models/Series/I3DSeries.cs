// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>3D grid series with X[], Y[], Z[,] data.</summary>
public interface I3DGridSeries : ISeries
{
    /// <summary>The x coordinate of each grid column, in data units.</summary>
    double[] X { get; }

    /// <summary>The y coordinate of each grid row, in data units.</summary>
    double[] Y { get; }

    /// <summary>The height at each grid node, indexed as <c>Z[row, column]</c>: the first dimension follows
    /// <see cref="Y"/>, the second follows <see cref="X"/>.</summary>
    double[,] Z { get; }
}

/// <summary>3D point series with X[], Y[], Z[] data.</summary>
public interface I3DPointSeries : ISeries
{
    /// <summary>The x coordinate of each point, in data units.</summary>
    double[] X { get; }

    /// <summary>The y coordinate of each point, in data units; one value per <see cref="X"/> entry.</summary>
    double[] Y { get; }

    /// <summary>The z coordinate of each point, in data units; one value per <see cref="X"/> entry.</summary>
    double[] Z { get; }
}
