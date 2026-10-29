// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>Where the figure-level colour bar stands, apart from its ticks: the bar is placed against the bounding box of
/// the plot areas, a vertical one is pulled in so that it, its tick labels and its label fit inside the figure, and it has
/// no wedges. These hold before and after the ticks come from a locator.</summary>
public class FigureColorBarGeometryTests
{
    private static readonly List<Rect> OnePlot = [new Rect(50, 50, 300, 200)];

    private static RecordingRenderContext Draw(ColorBar cb)
    {
        var figure = new Figure { Width = 400, Height = 300, Theme = Theme.Default };
        var ctx = new RecordingRenderContext();
        new ChartRenderer().RenderFigureColorBar(figure, OnePlot, cb, ctx);
        return ctx;
    }

    private static Rect Frame(RecordingRenderContext ctx) => (Rect)((dynamic)ctx.OfKind("DrawRectangle")[^1].Args!).rect;

    [Fact]
    public void AVerticalBar_IsPulledInUntilItsTicksFitInsideTheFigure()
    {
        // Unpulled it would start at 350 + 10 = 360; the figure ends at 400, 4 px short of that is 396, and the bar (20 px)
        // and the 30 px kept for the tick labels leave 346.
        var frame = Frame(Draw(new ColorBar { Visible = true }));

        Assert.Equal(new Rect(346, 50, 20, 200), frame);
    }

    [Fact]
    public void AVerticalBarWithALabel_KeepsRoomForTheLabelToo()
    {
        // Theme.Default: tick font 13 - 2 = 11, so the label takes 11 + 6 = 17 px: 396 - 20 - 30 - 17 = 329.
        var frame = Frame(Draw(new ColorBar { Visible = true, Label = "Value" }));

        Assert.Equal(329, frame.X, 9);
    }

    [Fact]
    public void AHorizontalBar_RunsUnderTheBoundingBoxOfThePlots()
    {
        var frame = Frame(Draw(new ColorBar { Visible = true, Orientation = ColorBarOrientation.Horizontal }));

        // 300 px long, 300 / 20 = 15 px tall, 10 px under the plots, centred.
        Assert.Equal(new Rect(50, 260, 300, 15), frame);
    }

    [Theory]
    [InlineData(ColorBarOrientation.Vertical)]
    [InlineData(ColorBarOrientation.Horizontal)]
    public void TheFigureBar_HasNoWedges(ColorBarOrientation orientation)
    {
        var ctx = Draw(new ColorBar { Visible = true, Orientation = orientation, Extend = ColorBarExtend.Both });

        // 50 gradient steps and the frame.
        Assert.Equal(51, ctx.CountOf("DrawRectangle"));
    }
}
