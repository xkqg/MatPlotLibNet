// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Tools;

/// <summary>Represents a horizontal price level (support/resistance) drawn across the full axes width.</summary>
public sealed class HorizontalLevel
{
    public double Value { get; }

    public Color? Color { get; set; }
    public LineStyle LineStyle { get; set; } = LineStyle.Dashed;
    public double LineWidth { get; set; } = 1.0;
    public string? Label { get; set; }

    public HorizontalLevel(double value)
    {
        Value = value;
    }
}
