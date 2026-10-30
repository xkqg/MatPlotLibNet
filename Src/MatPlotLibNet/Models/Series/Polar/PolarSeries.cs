// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for series rendered in polar coordinates (r, theta).</summary>
public abstract class PolarSeries : ChartSeries, IPolarSeries
{
    /// <inheritdoc />
    public double[] R { get; }

    /// <inheritdoc />
    public double[] Theta { get; }

    /// <summary>Initializes with R and Theta data arrays.</summary>
    protected PolarSeries(double[] r, double[] theta) { R = r; Theta = theta; }

    /// <inheritdoc />
    /// <remarks>Angle first, then the radius under the series' own label — the order the reader thinks in.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.FromNumberColumns([new("theta"), new(Label ?? "r")], [Theta, R]);

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    /// <remarks>The largest radius that can be placed and reaches out from the centre: a missing, infinite or negative radius
    /// is left out, as it is not drawn.</remarks>
    internal override RadialExtent? RadialExtent
    {
        get
        {
            double max = 0;
            foreach (double radius in R)
                if (double.IsFinite(radius) && radius > max) max = radius;
            return max > 0 ? new RadialExtent(max, Sticky: false) : null;
        }
    }
}
