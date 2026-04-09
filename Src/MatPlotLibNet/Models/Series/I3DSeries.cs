// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>3D grid series with X[], Y[], Z[,] data.</summary>
public interface I3DGridSeries : ISeries
{
    /// <summary>Gets the X coordinate array.</summary>
    double[] X { get; }

    /// <summary>Gets the Y coordinate array.</summary>
    double[] Y { get; }

    /// <summary>Gets the Z data matrix.</summary>
    double[,] Z { get; }
}

/// <summary>3D point series with X[], Y[], Z[] data.</summary>
public interface I3DPointSeries : ISeries
{
    /// <summary>Gets the X coordinate array.</summary>
    double[] X { get; }

    /// <summary>Gets the Y coordinate array.</summary>
    double[] Y { get; }

    /// <summary>Gets the Z coordinate array.</summary>
    double[] Z { get; }
}
