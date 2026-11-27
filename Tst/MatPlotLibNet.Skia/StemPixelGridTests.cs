// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The stems of a stem plot in a raster export stand on the pixel grid: <c>stem</c> draws its stems with <c>vlines</c>, a
/// <c>LineCollection</c> of vertical segments with butt ends at <c>lines.linewidth</c> (1 pt under the classic style). The row read is
/// a third of the way up the stem, clear of its head.</summary>
public sealed class StemPixelGridTests : VerticalMarkPixelGridTests
{
    protected override AxesBuilder WithMark(AxesBuilder axes, double value) => axes.Stem([value], [1.0], s => s.StemColor = Colors.Blue);

    protected override double RowFraction => 0.35;
}
