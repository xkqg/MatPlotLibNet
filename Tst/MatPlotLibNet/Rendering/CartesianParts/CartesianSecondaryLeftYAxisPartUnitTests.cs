// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>Direct unit tests for <see cref="CartesianSecondaryLeftYAxisPart"/> (ROADMAP.md row 101 — the left-side
/// mirror of <see cref="CartesianSecondaryYAxisPart"/>).</summary>
public class CartesianSecondaryLeftYAxisPartUnitTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(100, 0, 400, 300);
    private static readonly DataRange Range = new(0, 10, 0, 100);
    private static DataTransform PrimaryTransform() => new(0, 10, 0, 10, Plot);

    private static (CartesianSecondaryLeftYAxisPart part, RecordingRenderContext ctx) Build(Axes axes, SecondaryAxisSlot slot,
        int startingIndexInPair = 0, double outwardOffset = 0, Theme? theme = null)
    {
        var secondary = new DataTransform(Range.XMin, Range.XMax, Range.YMin, Range.YMax, Plot);
        var ctx = new RecordingRenderContext();
        var labels = new CartesianDirectLabelsPart(axes, Plot, ctx, theme ?? DefaultTheme, PrimaryTransform(), [],
            new MinMaxRange(Range.XMin, Range.XMax), 0);
        var part = new CartesianSecondaryLeftYAxisPart(axes, Plot, ctx, theme ?? DefaultTheme, PrimaryTransform(),
            slot, startingIndexInPair, outwardOffset, Range, secondary, labels);
        return (part, ctx);
    }

    [Fact]
    public void Render_DefaultOffset_DrawsTicksAtThePlotsLeftEdge()
    {
        var ax = new Axes();
        var slot = ax.AddOutwardYAxis(AxisSide.Left);
        slot.AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [0.0, 100]));
        var (part, ctx) = Build(ax, slot);

        part.Render();

        // p1 is the tick's outer (farther) end: AxisEdge - Length, mirroring the right side's p2 (AxisTickExtensions
        // .DrawTickMark: outward = -1 for TickSide.Left, so the mark grows toward smaller X).
        var length = new TickConfig().Length;
        Assert.NotEmpty(ctx.OfKind("DrawLine"));
        Assert.All(ctx.OfKind("DrawLine"), c =>
            Assert.Equal(Plot.X - length, ((Point)((dynamic)c.Args!).p1).X, 6));
    }

    [Fact]
    public void Render_WithAnOutwardOffset_DrawsTicksPastThePlotsLeftEdgeByThatOffset()
    {
        var ax = new Axes();
        var slot = ax.AddOutwardYAxis(AxisSide.Left);
        slot.AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [0.0, 100]));
        const double outwardOffset = 28.0;
        var (part, ctx) = Build(ax, slot, outwardOffset: outwardOffset);

        part.Render();

        var length = new TickConfig().Length;
        Assert.NotEmpty(ctx.OfKind("DrawLine"));
        Assert.All(ctx.OfKind("DrawLine"), c =>
            Assert.Equal(Plot.X - outwardOffset - length, ((Point)((dynamic)c.Args!).p1).X, 6));
    }

    [Fact]
    public void Render_TickLabels_AreRightAligned_SoTheyGrowFurtherLeft()
    {
        var ax = new Axes();
        var slot = ax.AddOutwardYAxis(AxisSide.Left);
        slot.AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [0.0, 100]));
        var (part, ctx) = Build(ax, slot);

        part.Render();

        Assert.NotEmpty(ctx.OfKind("DrawText"));
        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(TextAlignment.Right, (TextAlignment)((dynamic)c.Args!).alignment));
    }

    [Fact]
    public void Render_WithLabel_EmitsExtraText()
    {
        var ax = new Axes();
        var slot = ax.AddOutwardYAxis(AxisSide.Left);
        slot.AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [0.0, 100]));
        slot.Axis.Label = "Left axis";
        var (part, ctx) = Build(ax, slot);

        part.Render();

        Assert.Contains(ctx.OfKind("DrawText"), c => (string)((dynamic)c.Args!).text == "Left axis");
    }

    /// <summary>Mirrors CartesianSecondaryYAxisPartUnitTests.Render_HonoursLabelStyle_onTheAxisTitle: the title's
    /// own LabelStyle (color/size/pad) must be read here too, not only the slot's default font.</summary>
    [Fact]
    public void Render_HonoursLabelStyle_OnTheAxisTitle()
    {
        var ax = new Axes();
        var slot = ax.AddOutwardYAxis(AxisSide.Left);
        slot.AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [0.0, 100]));
        slot.Axis.Label = "Left axis";
        slot.Axis.LabelStyle = new TextStyle { Color = Colors.Green, FontSize = 19, Pad = 6 };
        var (part, ctx) = Build(ax, slot);

        part.Render();

        dynamic label = Assert.Single(ctx.OfKind("DrawText"), c => (string)((dynamic)c.Args!).text == "Left axis").Args!;
        var font = (Font)label.font;
        Assert.Equal(Colors.Green, font.Color);
        Assert.Equal(19.0, font.Size);
        // Further left than the plot's own left edge: LabelStyle.Pad is added to the offset, not ignored.
        Assert.True(((Point)label.position).X < Plot.X);
    }

    [Fact]
    public void Render_HiddenSeries_SkippedSilently()
    {
        var ax = new Axes();
        var slot = ax.AddOutwardYAxis(AxisSide.Left);
        var s = slot.AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [0.0, 100]));
        s.Visible = false;
        var (part, ctx) = Build(ax, slot);

        part.Render();

        Assert.Equal(0, ctx.CountOf("DrawLines"));
    }
}
