// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>Maps integer tick indices to category label strings.
/// Pass <c>reversed: true</c> for the Y-axis of a heatmap, where row 0 is rendered
/// at the bottom in SVG pixel-space while the math-Y-axis increases upward.</summary>
public sealed class CategoryFormatter : ITickFormatter
{
    private readonly string[] _labels;
    private readonly bool     _reversed;

    /// <param name="labels">Category names in display order (index 0 = first tick).</param>
    /// <param name="reversed">When <c>true</c>, index 0 maps to the last label (Y-axis heatmap convention).</param>
    public CategoryFormatter(string[] labels, bool reversed = false)
    {
        ArgumentNullException.ThrowIfNull(labels);
        _labels   = labels;
        _reversed = reversed;
    }

    /// <inheritdoc />
    public string Format(double value)
    {
        int idx = _reversed
            ? _labels.Length - 1 - (int)value
            : (int)value;
        return idx >= 0 && idx < _labels.Length ? _labels[idx] : string.Empty;
    }
}
