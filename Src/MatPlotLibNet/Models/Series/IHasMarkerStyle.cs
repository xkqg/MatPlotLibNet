// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that supports marker shape assignment.</summary>
public interface IHasMarkerStyle
{
    /// <summary>The shape drawn at each data point, such as a circle, square or triangle.</summary>
    MarkerStyle MarkerStyle { get; set; }
}
