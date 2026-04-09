// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for 3D grid series with X[], Y[], Z[,] data.</summary>
public abstract class GridSeries3D : ChartSeries, I3DGridSeries
{
    /// <summary>Gets the X-axis grid coordinates.</summary>
    public double[] X { get; }

    /// <summary>Gets the Y-axis grid coordinates.</summary>
    public double[] Y { get; }

    /// <summary>Gets the Z data matrix.</summary>
    public double[,] Z { get; }

    /// <summary>Initializes with X, Y, and Z data.</summary>
    protected GridSeries3D(double[] x, double[] y, double[,] z) { X = x; Y = y; Z = z; }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);
}
