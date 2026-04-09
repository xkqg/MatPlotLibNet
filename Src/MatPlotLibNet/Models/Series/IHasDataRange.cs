// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that can compute its own contribution to the axes data range.</summary>
public interface IHasDataRange
{
    /// <summary>Computes the X and Y data range for this series.</summary>
    DataRangeContribution ComputeDataRange(IAxesContext context);
}
