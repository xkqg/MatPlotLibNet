// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents the configuration of a single axis (X or Y) on an axes.</summary>
public class Axis
{
    public string? Label { get; set; }

    public TextStyle? LabelStyle { get; set; }

    public double? Min { get; set; }

    public double? Max { get; set; }

    public AxisScale Scale { get; set; } = AxisScale.Linear;

    public TickConfig MajorTicks { get; set; } = new();

    // matplotlib minor tick defaults: xtick.minor.size = 2.0 pt, xtick.minor.width = 0.6 pt,
    // and minor ticks are OFF by default (ax.minorticks_on() / WithMinorTicks() enables them).
    public TickConfig MinorTicks { get; set; } = new TickConfig
    {
        Visible = false,
        Length  = 2.0 * 100.0 / 72.0,   // 2.0 pt → ~2.78 px at 100 DPI
        Width   = 0.6 * 100.0 / 72.0,   // 0.6 pt → ~0.83 px at 100 DPI
    };

    public bool Inverted { get; set; }

    /// <summary>
    /// Data padding as a fraction of the data range (matplotlib <c>axes.xmargin</c>/<c>ymargin</c>).
    /// When <see langword="null"/> (the default), the renderer inherits the value from
    /// <see cref="Styling.Theme.AxisXMargin"/> / <see cref="Styling.Theme.AxisYMargin"/>:
    /// <c>MatplotlibClassic</c> → 0.0 (data touches spines), <c>MatplotlibV2</c>/<c>Default</c> → 0.05.
    /// </summary>
    public double? Margin { get; set; }

    public ITickFormatter? TickFormatter { get; set; }

    public ITickLocator? TickLocator { get; set; }
}

/// <summary>Represents the configuration of the Z axis on a 3D axes. Inherits all standard axis
/// properties (<see cref="Axis.Label"/>, <see cref="Axis.Min"/>/<see cref="Axis.Max"/>,
/// <see cref="Axis.MajorTicks"/>, etc.) and may be extended with 3D-specific properties.</summary>
public sealed class Axis3D : Axis { }

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

    // matplotlib `xtick.major.size = 3.5` POINTS → at 100 DPI = 4.861 px.
    // We store in pixels (the unit Skia and SVG render in), pre-converted.
    public double Length { get; init; } = 3.5 * 100.0 / 72.0;

    // matplotlib `xtick.major.width = 0.8` POINTS → at 100 DPI = 1.111 px.
    public double Width { get; init; } = 0.8 * 100.0 / 72.0;

    public Color? Color { get; init; }

    public double? LabelSize { get; init; }

    public Color? LabelColor { get; init; }

    // matplotlib `xtick.major.pad = 3.5` POINTS → at 100 DPI = 4.861 px.
    public double Pad { get; init; } = 3.5 * 100.0 / 72.0;

    /// <summary>When <c>true</c>, ticks and labels are drawn on both sides of the axes
    /// (e.g. Y ticks on both left and right spines). Equivalent to matplotlib's
    /// <c>ax.tick_params(right=True, labelright=True)</c>. Default <c>false</c>.</summary>
    public bool Mirror { get; init; } = false;
}
