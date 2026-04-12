// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>Controls how a figure is saved to disk.</summary>
public sealed record SaveOptions
{
    public int Dpi { get; init; } = 96;

    public bool PrettifySvg { get; init; }

    public int? SvgDecimalPrecision { get; init; }

    public string? Title { get; init; }

    public string? Author { get; init; }
}
