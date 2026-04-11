// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents the configuration of a single axis (X or Y) on an axes.</summary>
public sealed class Axis
{
    public string? Label { get; set; }

    public TextStyle? LabelStyle { get; set; }

    public double? Min { get; set; }

    public double? Max { get; set; }

    public AxisScale Scale { get; set; } = AxisScale.Linear;

    public TickConfig MajorTicks { get; set; } = new();

    public TickConfig MinorTicks { get; set; } = new();

    public bool Inverted { get; set; }

    public double Margin { get; set; } = 0.05;

    public ITickFormatter? TickFormatter { get; set; }

    public ITickLocator? TickLocator { get; set; }
}

/// <summary>Specifies the scale type used for an axis.</summary>
public enum AxisScale
{
    /// <summary>Linear scale.</summary>
    Linear,

    /// <summary>Logarithmic scale.</summary>
    Log,

    /// <summary>Symmetric logarithmic scale that handles values near zero.</summary>
    SymLog,

    /// <summary>Logit scale for probability data in the range (0, 1).</summary>
    Logit,

    /// <summary>Date scale that interprets values as OLE Automation dates.</summary>
    Date
}

/// <summary>Configures the appearance and spacing of axis tick marks.</summary>
public sealed record TickConfig
{
    public bool Visible { get; init; } = true;

    public double? Spacing { get; init; }

    public string? Format { get; init; }

    public TickDirection Direction { get; init; } = TickDirection.Out;

    public double Length { get; init; } = 5.0;

    public double Width { get; init; } = 0.8;

    public Color? Color { get; init; }

    public double? LabelSize { get; init; }

    public Color? LabelColor { get; init; }

    public double Pad { get; init; } = 3.0;
}
