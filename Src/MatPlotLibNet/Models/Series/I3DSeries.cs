// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>3D grid series with X[], Y[], Z[,] data.</summary>
public interface I3DGridSeries : ISeries
{
    double[] X { get; }

    double[] Y { get; }

    double[,] Z { get; }
}

/// <summary>3D point series with X[], Y[], Z[] data.</summary>
public interface I3DPointSeries : ISeries
{
    double[] X { get; }

    double[] Y { get; }

    double[] Z { get; }
}
