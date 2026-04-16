// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that supports marker shape assignment.</summary>
public interface IHasMarkerStyle
{
    MarkerStyle MarkerStyle { get; set; }
}
