// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// Direct unit tests for <see cref="CartesianSecondaryYAxisPart"/> (Phase B.7).
/// Covers right-edge tick marks + labels, optional secondary axis label,
/// visible/hidden series, custom tick formatter.
/// </summary>
public class CartesianSecondaryYAxisPartUnitTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static readonly DataRange Range = new(0, 10, 0, 100);
    private static DataTransform PrimaryTransform() => new(0, 10, 0, 10, Plot);

    private static CartesianSecondaryYAxisPart Build(Axes axes, RecordingRenderContext ctx, int primaryCount = 0) =>
        new(axes, Plot, ctx, DefaultTheme, PrimaryTransform(), Range, primaryCount);

    // ──────────────────────────────────────────────────────────────────────────
    // Ticks + labels
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_DefaultAxes_DrawsRightEdgeTicks()
    {
        var ax = new Axes();
        ax.TwinX();
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.True(ctx.CountOf("DrawLine") >= 1);
        Assert.True(ctx.CountOf("DrawText") >= 1);
    }

    [Fact]
    public void Render_TicksPositionedAtRightEdge()
    {
        var ax = new Axes();
        ax.TwinX();
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var tickLines = ctx.OfKind("DrawLine");
        foreach (var call in tickLines)
        {
            var p1 = (Point)((dynamic)call.Args!).p1;
            var p2 = (Point)((dynamic)call.Args!).p2;
            Assert.Equal(Plot.X + Plot.Width, p1.X);
            Assert.Equal(Plot.X + Plot.Width + 5, p2.X);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Axis label
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_WithLabel_EmitsExtraText()
    {
        var ax = new Axes();
        ax.TwinX();
        ax.SecondaryYAxis!.Label = "Right axis";
        var ctx = new RecordingRenderContext();
        int noLabelCount = 0;
        {
            var ctxNo = new RecordingRenderContext();
            var axNo = new Axes(); axNo.TwinX();
            Build(axNo, ctxNo).Render();
            noLabelCount = ctxNo.CountOf("DrawText");
        }
        Build(ax, ctx).Render();
        Assert.Equal(noLabelCount + 1, ctx.CountOf("DrawText"));
        Assert.Contains(ctx.OfKind("DrawText"),
            c => (string)((dynamic)c.Args!).text == "Right axis");
    }

    [Fact]
    public void Render_NullLabel_NoExtraText()
    {
        var ax = new Axes();
        ax.TwinX();
        ax.SecondaryYAxis!.Label = null;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        // Only tick labels, no axis label
        Assert.DoesNotContain(ctx.OfKind("DrawText"),
            c => (string)((dynamic)c.Args!).text == "");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Secondary series rendering
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_WithVisibleSecondarySeries_DrawsSeries()
    {
        var ax = new Axes();
        ax.PlotSecondary([0.0, 5, 10], [10.0, 50, 90]);
        var ctxBefore = new RecordingRenderContext();
        Build(new Axes { }.Also(a => a.TwinX()), ctxBefore).Render();
        int beforeCalls = ctxBefore.Calls.Count;

        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.True(ctx.Calls.Count > beforeCalls, "Secondary series should produce more draw calls");
    }

    [Fact]
    public void Render_HiddenSecondarySeries_SkippedSilently()
    {
        var ax = new Axes();
        var s = ax.PlotSecondary([0.0, 5, 10], [10.0, 50, 90]);
        s.Visible = false;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        // With the series hidden, only axis ticks are drawn (no series lines).
        // Verify no DrawLines (plural) call for the hidden series.
        Assert.Equal(0, ctx.CountOf("DrawLines"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tick formatter
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_CustomTickFormatter_AppliedToLabels()
    {
        var ax = new Axes();
        ax.TwinX();
        ax.SecondaryYAxis!.TickFormatter = new PercentFormatter(100);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Contains(ctx.OfKind("DrawText"),
            c => ((string)((dynamic)c.Args!).text).Contains('%'));
    }

    [Fact]
    public void Render_NullTickFormatter_UsesUniformFormat()
    {
        var ax = new Axes();
        ax.TwinX();
        ax.SecondaryYAxis!.TickFormatter = null;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        // Some numeric-looking string for each tick; just verify no % sign.
        foreach (var c in ctx.OfKind("DrawText"))
            Assert.DoesNotContain('%', (string)((dynamic)c.Args!).text);
    }
}

internal static class AxesExtensionsForTests
{
    public static Axes Also(this Axes axes, Action<Axes> configure)
    {
        configure(axes);
        return axes;
    }
}
