// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// Direct unit tests for <see cref="CartesianSecondaryXAxisPart"/> (Phase B.8).
/// Covers top-edge tick marks + labels, optional secondary X axis label,
/// visible/hidden series, custom tick formatter.
/// </summary>
public class CartesianSecondaryXAxisPartUnitTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static readonly DataRange Range = new(0, 100, 0, 10);
    private static DataTransform PrimaryTransform() => new(0, 10, 0, 10, Plot);

    private static CartesianSecondaryXAxisPart Build(Axes axes, RecordingRenderContext ctx, int colorOffset = 0) =>
        new(axes, Plot, ctx, DefaultTheme, PrimaryTransform(), Range, colorOffset);

    [Fact]
    public void Render_DefaultAxes_DrawsTopEdgeTicks()
    {
        var ax = new Axes();
        ax.TwinY();
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.True(ctx.CountOf("DrawLine") >= 1);
        Assert.True(ctx.CountOf("DrawText") >= 1);
    }

    [Fact]
    public void Render_TicksPositionedAtTopEdge()
    {
        var ax = new Axes();
        ax.TwinY();
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        foreach (var call in ctx.OfKind("DrawLine"))
        {
            var p1 = (Point)((dynamic)call.Args!).p1;
            var p2 = (Point)((dynamic)call.Args!).p2;
            Assert.Equal(Plot.Y, p1.Y);
            Assert.Equal(Plot.Y - 5, p2.Y);
        }
    }

    [Fact]
    public void Render_WithLabel_EmitsExtraText()
    {
        var ax = new Axes();
        ax.TwinY();
        ax.SecondaryXAxis!.Label = "Top axis";
        var ctx = new RecordingRenderContext();

        var axNo = new Axes(); axNo.TwinY();
        var ctxNo = new RecordingRenderContext();
        Build(axNo, ctxNo).Render();

        Build(ax, ctx).Render();
        Assert.Equal(ctxNo.CountOf("DrawText") + 1, ctx.CountOf("DrawText"));
        Assert.Contains(ctx.OfKind("DrawText"),
            c => (string)((dynamic)c.Args!).text == "Top axis");
    }

    [Fact]
    public void Render_NullLabel_NoExtraText()
    {
        var ax = new Axes();
        ax.TwinY();
        ax.SecondaryXAxis!.Label = null;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.DoesNotContain(ctx.OfKind("DrawText"),
            c => (string)((dynamic)c.Args!).text == "");
    }

    [Fact]
    public void Render_WithVisibleSecondarySeries_DrawsSeries()
    {
        var ax = new Axes();
        ax.PlotXSecondary([0.0, 50, 100], [1.0, 5, 9]);

        var axNo = new Axes(); axNo.TwinY();
        var ctxNo = new RecordingRenderContext();
        Build(axNo, ctxNo).Render();

        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.True(ctx.Calls.Count > ctxNo.Calls.Count);
    }

    [Fact]
    public void Render_HiddenSecondarySeries_SkippedSilently()
    {
        var ax = new Axes();
        var s = ax.PlotXSecondary([0.0, 50, 100], [1.0, 5, 9]);
        s.Visible = false;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawLines"));
    }

    [Fact]
    public void Render_CustomTickFormatter_AppliedToLabels()
    {
        var ax = new Axes();
        ax.TwinY();
        ax.SecondaryXAxis!.TickFormatter = new PercentFormatter(100);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Contains(ctx.OfKind("DrawText"),
            c => ((string)((dynamic)c.Args!).text).Contains('%'));
    }

    [Fact]
    public void Render_NullTickFormatter_UsesUniformFormat()
    {
        var ax = new Axes();
        ax.TwinY();
        ax.SecondaryXAxis!.TickFormatter = null;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        foreach (var c in ctx.OfKind("DrawText"))
            Assert.DoesNotContain('%', (string)((dynamic)c.Args!).text);
    }

    [Fact]
    public void Render_ColorOffsetCyclesSeriesColors()
    {
        // Exercise the `colorOffset + i` path: adding series → colors picked from cycle.
        var ax = new Axes();
        ax.PlotXSecondary([0.0, 100], [0.0, 10]);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx, colorOffset: 3).Render();
        // Just verify it doesn't throw — colorOffset arithmetic is the concern.
        Assert.True(ctx.Calls.Count > 0);
    }
}
