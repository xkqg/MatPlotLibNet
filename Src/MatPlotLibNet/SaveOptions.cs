// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>Controls how a figure is saved to disk.</summary>
public sealed record SaveOptions
{
    /// <summary>Gets the dots-per-inch resolution used when computing pixel dimensions from physical sizes. Default 96.</summary>
    public int Dpi { get; init; } = 96;

    /// <summary>Gets whether SVG output is written with human-readable indentation. Default false (compact output).</summary>
    public bool PrettifySvg { get; init; }

    /// <summary>Gets the number of decimal places used for SVG coordinate values, or null for the default precision.</summary>
    public int? SvgDecimalPrecision { get; init; }

    /// <summary>Gets an optional document title embedded in the SVG <c>&lt;title&gt;</c> element.</summary>
    public string? Title { get; init; }

    /// <summary>Gets an optional author string embedded in the SVG metadata.</summary>
    public string? Author { get; init; }
}
