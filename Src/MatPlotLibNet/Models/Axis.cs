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

    /// <summary>True when <see cref="Min"/> or <see cref="Max"/> is set: that side is drawn exactly as set.</summary>
    internal bool HasLimit => Min.HasValue || Max.HasValue;

    public AxisScale Scale { get; set; } = AxisScale.Linear;

    /// <summary>Linear threshold for <see cref="AxisScale.SymLog"/>. Values within
    /// [-SymLogLinThresh, SymLogLinThresh] are mapped linearly. Default 1.0.</summary>
    public double SymLogLinThresh { get; set; } = 1.0;

    public TickConfig MajorTicks { get; set; } = new();

    // matplotlib minor tick defaults: xtick.minor.size = 2.0 pt, xtick.minor.width = 0.6 pt,
    // and minor ticks are OFF by default (ax.minorticks_on() / WithMinorTicks() enables them).
    public TickConfig MinorTicks { get; set; } = new TickConfig
    {
        Visible = false,
        Length  = 2.0 * 100.0 / 72.0,   // 2.0 pt → ~2.78 px at 100 DPI
        Width   = 0.6 * 100.0 / 72.0,   // 0.6 pt → ~0.83 px at 100 DPI
    };

    /// <summary>Draws the axis the other way round: an X axis from right to left, a Y axis from top to bottom
    /// (matplotlib's <c>Axis.set_inverted(True)</c>, the state <c>invert_xaxis</c> and <c>invert_yaxis</c> switch). The
    /// ticks are those of the same range, each drawn where its value is, by the rule a limit set high to low follows
    /// (<c>SetYLim(5.5, 0.5)</c>).</summary>
    /// <remarks>Inverted and limits set high to low ask for the same thing and are one state, not two turns: an axis whose
    /// limits are already set high to low is drawn reversed once when it is also inverted, not turned back to forward, as
    /// matplotlib's <c>set_inverted(True)</c> leaves an inverted axis inverted. Switching it on and off again returns the
    /// axis to the way its limits draw it (matplotlib's <c>invert_yaxis</c> called twice). A zoom or a pan writes the
    /// limits the way the axis is drawn, so an inverted axis stays reversed through them. Read by Cartesian axes, their
    /// secondary axes and insets; polar and 3-D axes do not read it.</remarks>
    public bool Inverted { get; set; }

    /// <summary>
    /// Data padding as a fraction of the data range (matplotlib <c>axes.xmargin</c>/<c>ymargin</c>).
    /// When <see langword="null"/> (the default), the renderer inherits the value from
    /// <see cref="Styling.Theme.AxisXMargin"/> / <see cref="Styling.Theme.AxisYMargin"/>:
    /// <c>MatplotlibClassic</c> → 0.0 (data touches spines), <c>MatplotlibV2</c>/<c>Default</c> → 0.05.
    /// </summary>
    public double? Margin { get; set; }

    /// <summary>Writes the tick labels; <see langword="null"/> for the plain numeric labels.</summary>
    /// <remarks>A figure's JSON (<c>ToJson</c>, the MCP server, the transports) carries the formatters the library
    /// ships, with what they were made with. A formatter written outside the library is code, which a JSON document
    /// cannot carry: it is left out, and the axis reads back with the plain labels.</remarks>
    public ITickFormatter? TickFormatter { get; set; }

    /// <summary>Places the ticks; <see langword="null"/> for the default nice-number ticks.</summary>
    /// <remarks>A figure's JSON carries the locators the library ships, with what they were made with. A locator
    /// written outside the library is code, which a JSON document cannot carry: it is left out, and the axis reads
    /// back with the default ticks.</remarks>
    public ITickLocator? TickLocator { get; set; }

    /// <summary>Shorthand for <see cref="TickConfig.LabelRotation"/> on <see cref="MajorTicks"/>.
    /// Degrees; negative = clockwise (matplotlib convention: <c>-45</c> = schuin rechts-onder).
    /// Default 0 (horizontal).</summary>
    public double LabelRotation
    {
        get => MajorTicks.LabelRotation;
        set => MajorTicks = MajorTicks with { LabelRotation = value };
    }
}

/// <summary>Represents the configuration of the Z axis on a 3D axes. Inherits all standard axis
/// properties (<see cref="Axis.Label"/>, <see cref="Axis.Min"/>/<see cref="Axis.Max"/>,
/// <see cref="Axis.MajorTicks"/>, etc.) and may be extended with 3D-specific properties.</summary>
public sealed class Axis3D : Axis { }

/// <summary>Specifies the scale type used for an axis.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum AxisScale
{
    /// <summary>Linear scale.</summary>
    Linear = 0,

    /// <summary>Logarithmic scale.</summary>
    Log = 1,

    /// <summary>Symmetric logarithmic scale that handles values near zero.</summary>
    SymLog = 2,

    /// <summary>Logit scale for probability data in the range (0, 1).</summary>
    Logit = 3,

    /// <summary>Date scale that interprets values as OLE Automation dates.</summary>
    Date = 4,
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

    /// <summary>Tick-label rotation in degrees (matplotlib's <c>ax.tick_params(labelrotation=...)</c>).
    /// Zero means horizontal labels; positive tilts counter-clockwise. When 0, the renderer
    /// may still auto-rotate X-axis labels to 30° if adjacent labels would overlap
    /// (matches matplotlib <c>Figure.autofmt_xdate</c>). Default 0.</summary>
    public double LabelRotation { get; init; } = 0;
}
