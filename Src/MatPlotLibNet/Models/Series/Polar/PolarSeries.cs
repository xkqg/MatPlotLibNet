// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for series rendered in polar coordinates (r, theta).</summary>
public abstract class PolarSeries : ChartSeries, IPolarSeries
{
    public double[] R { get; }

    public double[] Theta { get; }

    /// <summary>Initializes with R and Theta data arrays.</summary>
    protected PolarSeries(double[] r, double[] theta) { R = r; Theta = theta; }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);
}
