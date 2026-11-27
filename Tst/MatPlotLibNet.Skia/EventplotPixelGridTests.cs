// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The events of an eventplot in a raster export stand on the pixel grid: <c>eventplot</c> draws an <c>EventCollection</c>, a
/// <c>LineCollection</c> of vertical segments with butt ends at <c>lines.linewidth</c> (1 pt under the classic style). matplotlib 3.11.2
/// draws the events of <c>classic/eventplot.png</c> as one column of #0000FF with #CDCDFF on each side, wherever the event falls.</summary>
public sealed class EventplotPixelGridTests : VerticalMarkPixelGridTests
{
    protected override AxesBuilder WithMark(AxesBuilder axes, double value) => axes.Eventplot([[value]], s => s.Colors = [Colors.Blue]);

    protected override double RowFraction => 0.45;
}
