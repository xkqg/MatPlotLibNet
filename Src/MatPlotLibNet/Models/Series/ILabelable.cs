// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that can render data-point labels with optional format control.</summary>
public interface ILabelable
{
    /// <summary>When <see langword="true"/> each data point is annotated with its value.</summary>
    bool ShowLabels { get; set; }

    /// <summary>Optional format string applied to each label value (e.g., <c>"F2"</c> for two decimals).
    /// When <see langword="null"/> the default numeric format is used.</summary>
    string? LabelFormat { get; set; }
}
