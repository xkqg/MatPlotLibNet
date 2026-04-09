// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that can be stacked (cumulative baseline) when multiple instances share an axes.</summary>
public interface IStackable
{
    /// <summary>Gets the numeric values used for stacking computation.</summary>
    double[] Values { get; }

    /// <summary>Gets or sets the cumulative baseline values set by the stacking algorithm.</summary>
    double[]? StackBaseline { get; set; }
}
