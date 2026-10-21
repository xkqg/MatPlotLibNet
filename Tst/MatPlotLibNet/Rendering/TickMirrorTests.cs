// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

public sealed class TickMirrorTests
{
    [Fact]
    public void MirrorProperty_DefaultFalse()
    {
        var config = new TickConfig();
        Assert.False(config.Mirror);
    }

    [Fact]
    public void MirrorProperty_CanBeSetTrue()
    {
        var config = new TickConfig { Mirror = true };
        Assert.True(config.Mirror);
    }

    [Fact]
    public void WithRecord_PreservesOtherProperties()
    {
        var config = new TickConfig { Visible = true, Mirror = false };
        var mirrored = config with { Mirror = true };
        Assert.True(mirrored.Mirror);
        Assert.True(mirrored.Visible);
    }

    [Fact]
    public void WithYTicksMirrored_SetsMirrorOnMajorTicks()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .WithYTicksMirrored())
            .Build();

        Assert.True(fig.SubPlots[0].YAxis.MajorTicks.Mirror);
    }

    [Fact]
    public void WithXTicksMirrored_SetsMirrorOnMajorTicks()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .WithXTicksMirrored())
            .Build();

        Assert.True(fig.SubPlots[0].XAxis.MajorTicks.Mirror);
    }

    [Fact]
    public void MirroredYTicks_SvgContainsRightSideLabels()
    {
        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 30.0])
                .WithYTicksMirrored())
            .Build()
            .ToSvg();

        // SVG should contain text elements for both left and right side tick labels.
        // The mirrored labels use TextAlignment.Left (right side) vs TextAlignment.Right (left side).
        // Count how many Y tick label texts appear — should be double the normal amount.
        int tickLabelCount = System.Text.RegularExpressions.Regex.Matches(svg, @"text-anchor=""start""").Count;
        Assert.True(tickLabelCount > 0, "Mirrored Y ticks should produce right-aligned labels (text-anchor=start)");
    }

    // ── Geometry of the mirrored side ────────────────────────────────────────

    private static readonly Rect Area = new(60, 40, 400, 300);

    private static RecordingRenderContext RenderAxes(Action<Axes> configure)
    {
        var axes = new Axes();
        axes.Plot([1.0, 2.0, 3.0], [10.0, 20.0, 30.0]);
        configure(axes);
        var ctx = new RecordingRenderContext();
        new CartesianAxesRenderer(axes, Area, ctx, Theme.Default).Render();
        return ctx;
    }

    private static Point P1(CallRecord c) => (Point)((dynamic)c.Args!).p1;
    private static Point P2(CallRecord c) => (Point)((dynamic)c.Args!).p2;

    /// <summary>Short horizontal marks that touch the right edge: the mirrored Y ticks (grid lines span the plot).</summary>
    private static List<CallRecord> RightMarks(RecordingRenderContext ctx, double right) =>
        ctx.OfKind("DrawLine").Where(c => P1(c).Y == P2(c).Y
            && Math.Abs(P2(c).X - P1(c).X) < 20
            && Math.Max(P1(c).X, P2(c).X) >= right - 1).ToList();

    /// <summary>Short vertical marks that touch the top edge: the mirrored X ticks.</summary>
    private static List<CallRecord> TopMarks(RecordingRenderContext ctx, double top) =>
        ctx.OfKind("DrawLine").Where(c => P1(c).X == P2(c).X
            && Math.Abs(P2(c).Y - P1(c).Y) < 20
            && Math.Min(P1(c).Y, P2(c).Y) <= top + 1).ToList();

    [Fact]
    public void MirroredYTick_Out_PointsAwayFromPlot()
    {
        var ctx = RenderAxes(ax => ax.YAxis.MajorTicks = ax.YAxis.MajorTicks with { Mirror = true });
        double right = Area.X + Area.Width;
        var ticks = new TickConfig();
        double half = new SpineConfig().LineWidth / 2;

        var marks = RightMarks(ctx, right);
        Assert.NotEmpty(marks);
        Assert.All(marks, c =>
        {
            Assert.Equal(right - half, P1(c).X, 6);
            Assert.Equal(right + ticks.Length, P2(c).X, 6);
        });
    }

    [Fact]
    public void MirroredYTick_In_PointsIntoThePlot()
    {
        var ctx = RenderAxes(ax => ax.YAxis.MajorTicks = ax.YAxis.MajorTicks with { Mirror = true, Direction = TickDirection.In });
        double right = Area.X + Area.Width;
        var ticks = new TickConfig();
        double half = new SpineConfig().LineWidth / 2;

        var marks = RightMarks(ctx, right);
        Assert.NotEmpty(marks);
        Assert.All(marks, c =>
        {
            Assert.Equal(right - ticks.Length, P1(c).X, 6);
            Assert.Equal(right + half, P2(c).X, 6);
        });
    }

    [Fact]
    public void WithXTicksMirrored_DrawsTicksAndLabelsOnTheTopEdge()
    {
        var plain = RenderAxes(_ => { });
        var ctx = RenderAxes(ax => ax.XAxis.MajorTicks = ax.XAxis.MajorTicks with { Mirror = true });
        double top = Area.Y;
        var ticks = new TickConfig();
        double half = new SpineConfig().LineWidth / 2;

        Assert.Empty(TopMarks(plain, top));
        var marks = TopMarks(ctx, top);
        Assert.NotEmpty(marks);
        Assert.All(marks, c =>
        {
            Assert.Equal(top - ticks.Length, P1(c).Y, 6);
            Assert.Equal(top + half, P2(c).Y, 6);
        });

        var topLabels = ctx.OfKind("DrawText").Where(c => ((Point)((dynamic)c.Args!).position).Y < top).ToList();
        Assert.Equal(marks.Count, topLabels.Count);
        Assert.All(topLabels, c => Assert.Equal(top - ticks.Length - ticks.Pad, ((Point)((dynamic)c.Args!).position).Y, 6));
        Assert.Equal(marks.Select(P1).Select(p => p.X), topLabels.Select(c => ((Point)((dynamic)c.Args!).position).X));
        Assert.All(topLabels, c => Assert.Equal(TextAlignment.Center, (TextAlignment)((dynamic)c.Args!).alignment));
    }

    [Fact]
    public void WithXTicksMirrored_RotatedLabels_AreAnchoredAtTheirStartSoTheyRiseAwayFromTheTopEdge()
    {
        // The bottom label of a rotated tick ends at its tick (anchored right); the top one starts there.
        var ctx = RenderAxes(ax => ax.XAxis.MajorTicks = ax.XAxis.MajorTicks with { Mirror = true, LabelRotation = 30 });
        double top = Area.Y;

        var topLabels = ctx.OfKind("DrawText").Where(c => ((Point)((dynamic)c.Args!).position).Y < top).ToList();
        Assert.NotEmpty(topLabels);
        Assert.All(topLabels, c =>
        {
            dynamic a = c.Args!;
            Assert.Equal(TextAlignment.Left, (TextAlignment)a.alignment);
            Assert.Equal(30.0, (double)a.rotation);
        });
    }
}
