// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that expose an explicit override color for their primary visual element.</summary>
public interface IHasColor
{
    /// <summary>Override color for the series. When <see langword="null"/> the theme cycle color is used.</summary>
    Color? Color { get; set; }
}
