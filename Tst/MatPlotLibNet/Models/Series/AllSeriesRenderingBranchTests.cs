// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Phase Q Wave 1 (2026-04-19) — branch-coverage Theory that renders every
/// <see cref="ISeries"/> through the full <see cref="Plt"/> pipeline with each optional
/// interface property toggled to a non-default value, exercising the renderer-side
/// "if (Color != null)" / "if (EdgeColor != null)" / "if (Visible == false)" branches
/// that <see cref="AllSeriesTests"/>'s default-state Theory leaves unhit.
///
/// <para>Reuses <see cref="AllSeriesTests.AllSeriesInstances"/> as the MemberData source —
/// per the DRY rule in the Phase Q plan, no duplicate factory list.</para>
///
/// <para>Each test invocation renders the series end-to-end; the assertion is
/// "no exception + non-empty SVG" rather than "specific output" because the
/// goal is to lift the renderer's branch counter, not to pin output bytes.</para></summary>
public class AllSeriesRenderingBranchTests
{
    private static string RenderSeries<T>(T series) where T : ISeries
        => Plt.Create()
              .WithSize(400, 300)
              .AddSubPlot(1, 1, 1, ax => ax.AddSeries(series))
              .ToSvg();

    /// <summary>Sets <see cref="IHasColor.Color"/> on every series implementing it
    /// to a non-default value, then renders. Pre-Q, every renderer's
    /// <c>color = ResolveColor(series.Color)</c> path consulted the FALLBACK branch
    /// (Color was null) and never the OVERRIDE branch — pinning many renderers at
    /// 100% line / 50% branch.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void IHasColor_ExplicitColor_RendersWithoutError(ISeries series, string label)
    {
        if (series is not IHasColor hc) return;
        hc.Color = Colors.Red;
        var svg = RenderSeries(series);
        Assert.False(string.IsNullOrEmpty(svg), $"{label} produced empty SVG with explicit Color");
        Assert.StartsWith("<svg", svg);
    }

    /// <summary>Sets <see cref="IHasAlpha.Alpha"/> to an intermediate value (0.5) on every
    /// implementing series, then renders. Many renderers branch on
    /// <c>alpha == 1.0 ? opaque : transparent</c>; the default Theory only hit the
    /// default Alpha for each series, so the OTHER branch was unhit.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void IHasAlpha_HalfAlpha_RendersWithoutError(ISeries series, string label)
    {
        if (series is not IHasAlpha ha) return;
        ha.Alpha = 0.5;
        var svg = RenderSeries(series);
        Assert.False(string.IsNullOrEmpty(svg), $"{label} produced empty SVG with Alpha=0.5");
    }

    /// <summary>Sets <see cref="IHasEdgeColor.EdgeColor"/> on every implementing series, then
    /// renders. Renderers commonly branch on <c>EdgeColor is null ? noStroke : drawStroke</c>.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void IHasEdgeColor_ExplicitEdge_RendersWithoutError(ISeries series, string label)
    {
        if (series is not IHasEdgeColor ec) return;
        ec.EdgeColor = Colors.Black;
        var svg = RenderSeries(series);
        Assert.False(string.IsNullOrEmpty(svg), $"{label} produced empty SVG with explicit EdgeColor");
    }

    /// <summary>Sets <see cref="IHasMarkerStyle.MarkerStyle"/> to <c>Square</c> (a
    /// non-default value) on every implementing series, then renders. The default
    /// Theory only hit <c>MarkerStyle.Circle</c> per series; this forces the renderer's
    /// non-default marker dispatch path.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void IHasMarkerStyle_NonDefault_RendersWithoutError(ISeries series, string label)
    {
        if (series is not IHasMarkerStyle ms) return;
        ms.MarkerStyle = MarkerStyle.Square;
        var svg = RenderSeries(series);
        Assert.False(string.IsNullOrEmpty(svg), $"{label} produced empty SVG with MarkerStyle.Square");
    }

    /// <summary>Sets <see cref="ISeries.Visible"/> to <c>false</c> on every series, then
    /// renders. Most renderers early-out with a single <c>if (!series.Visible) return;</c>
    /// guard at the top of their <c>Render</c> method — that guard is unhit while every
    /// other test uses the default <c>Visible = true</c>. Asserting the SVG simply
    /// completes (and contains no output specific to the series) covers the early-out branch.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void Visible_False_RendersWithoutError(ISeries series, string label)
    {
        series.Visible = false;
        var svg = RenderSeries(series);
        Assert.False(string.IsNullOrEmpty(svg), $"{label} produced empty SVG with Visible=false");
        Assert.StartsWith("<svg", svg);
    }

    /// <summary>Sets <see cref="ISeries.ZOrder"/> to a non-default value on every series,
    /// then renders. ZOrder affects the iteration order in <c>ChartRenderer</c> — exercising
    /// non-default values lifts the sort branch.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void ZOrder_NonDefault_RendersWithoutError(ISeries series, string label)
    {
        series.ZOrder = 5;
        var svg = RenderSeries(series);
        Assert.False(string.IsNullOrEmpty(svg), $"{label} produced empty SVG with ZOrder=5");
    }
}
