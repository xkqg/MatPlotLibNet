// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that can be stacked (cumulative baseline) when multiple instances share an axes.</summary>
public interface IStackable
{
    double[] Values { get; }

    double[]? StackBaseline { get; set; }
}
