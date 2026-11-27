// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests;

/// <summary>The reads of the tick labels around a plot: the labels above its top spine and the labels below its bottom one, each at the
/// X it stands at, in the order drawn.</summary>
public sealed class TickLabelReadsTests
{
    private static readonly Rect Plot = new(100, 100, 200, 100);
    private static readonly Font Tick = new() { Size = 10 };

    private static RecordingRenderContext Labelled()
    {
        var ctx = new RecordingRenderContext();
        ctx.DrawText("above", new Point(150, 90), Tick, TextAlignment.Center);
        ctx.DrawText("above too", new Point(250, 90), Tick, TextAlignment.Center);
        ctx.DrawText("below", new Point(150, 215), Tick, TextAlignment.Center);
        ctx.DrawText("beside", new Point(90, 150), Tick, TextAlignment.Right);
        ((IRenderContext)ctx).DrawText("turned", new Point(200, 90), Tick, TextAlignment.Center, 45);
        return ctx;
    }

    [Fact]
    public void LabelsAbove_AreTheUprightCentredOnesAboveTheTopSpine_AtTheirX()
    {
        var labels = Labelled().LabelsAbove(Plot);

        Assert.Equal([new TickLabel("above", 150), new TickLabel("above too", 250)], labels);
    }

    [Fact]
    public void LabelsBelow_AreTheUprightCentredOnesBelowTheBottomSpine_AtTheirX()
    {
        var labels = Labelled().LabelsBelow(Plot);

        Assert.Equal([new TickLabel("below", 150)], labels);
    }
}
