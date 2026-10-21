// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>Settings for saving a figure to disk. <c>Figure.Save(path, options)</c> accepts them, and no export reads
/// them yet: the file is the same with or without them, and a PNG is drawn at the figure's size in pixels.</summary>
public sealed record SaveOptions
{
    /// <summary>The resolution in dots per inch. Not read by any export yet.</summary>
    public int Dpi { get; init; } = 96;

    /// <summary>Whether an SVG is written indented. Not read by any export yet.</summary>
    public bool PrettifySvg { get; init; }

    /// <summary>The number of decimals in the numbers of an SVG. Not read by any export yet.</summary>
    public int? SvgDecimalPrecision { get; init; }

    /// <summary>A document title. Not read by any export yet.</summary>
    public string? Title { get; init; }

    /// <summary>A document author. Not read by any export yet.</summary>
    public string? Author { get; init; }
}
