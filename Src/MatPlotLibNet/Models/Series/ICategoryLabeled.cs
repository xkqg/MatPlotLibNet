// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that names its categories on the axis that carries them: the X axis, or the Y axis for bars that lie
/// down (<see cref="BarOrientation.Horizontal"/>).</summary>
public interface ICategoryLabeled
{
    /// <summary>The name of each category, in drawing order, shown as the tick labels of the category axis; null when
    /// the series has none.</summary>
    string[]? CategoryLabels { get; }
}
