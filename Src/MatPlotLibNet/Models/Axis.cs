// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Models;

/// <summary>Represents the configuration of a single axis (X or Y) on an axes.</summary>
public sealed class Axis
{
    /// <summary>Gets or sets the axis label text.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the minimum value of the axis range.</summary>
    public double? Min { get; set; }

    /// <summary>Gets or sets the maximum value of the axis range.</summary>
    public double? Max { get; set; }

    /// <summary>Gets or sets the scale type for this axis.</summary>
    public AxisScale Scale { get; set; } = AxisScale.Linear;

    /// <summary>Gets or sets the major tick configuration for this axis.</summary>
    public TickConfig MajorTicks { get; set; } = new();

    /// <summary>Gets or sets the minor tick configuration for this axis.</summary>
    public TickConfig MinorTicks { get; set; } = new();

    /// <summary>Gets or sets whether the axis direction is inverted.</summary>
    public bool Inverted { get; set; }

    /// <summary>Gets or sets a custom tick formatter for this axis.</summary>
    public ITickFormatter? TickFormatter { get; set; }

    /// <summary>
    /// Gets or sets a custom tick locator for this axis.
    /// When set, overrides the default nice-number algorithm.
    /// When <see cref="TickConfig.Spacing"/> is set and no locator is provided, a
    /// <see cref="MultipleLocator"/> is used automatically.
    /// </summary>
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
    /// <summary>Gets whether the tick marks are visible.</summary>
    public bool Visible { get; init; } = true;

    /// <summary>Gets the spacing between tick marks.</summary>
    public double? Spacing { get; init; }

    /// <summary>Gets the format string for tick labels.</summary>
    public string? Format { get; init; }
}
