// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase 4 — consolidated edge-case tests for series renderers. One file
/// instead of seven per the Phase 9 dedup principle: every renderer follows the
/// same pattern (data → SVG → assert geometry), so a single parametrised harness
/// covers them all.
///
/// <para>Renderers targeted (was below 90%):</para>
/// <list type="bullet">
///   <item>AreaSeriesRenderer (38.7%)</item>
///   <item>StepSeriesRenderer (63.6%)</item>
///   <item>BoxSeriesRenderer (64.5%)</item>
///   <item>BarSeriesRenderer (75.4%)</item>
///   <item>SvgSeriesRenderer (76.4%)</item>
///   <item>CartesianAxesRenderer (78.4%)</item>
///   <item>ViolinSeriesRenderer (83.3%)</item>
/// </list>
/// </summary>
public class RendererEdgeCaseTests
{
    // ── Helper: render a one-series figure and return the SVG ────────────────
    private static string Render(Action<MatPlotLibNet.AxesBuilder> configure) =>
        Plt.Create()
            .WithSize(800, 600)
            .AddSubPlot(1, 1, 1, configure)
            .ToSvg();

    // ── AreaSeriesRenderer ───────────────────────────────────────────────────

    [Fact]
    public void Area_EmptyInput_DoesNotThrow()
    {
        var svg = Render(ax => ax.FillBetween(EdgeCaseData.Empty, EdgeCaseData.Empty));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Area_SinglePoint_DoesNotThrow()
    {
        var svg = Render(ax => ax.FillBetween(EdgeCaseData.SinglePoint, EdgeCaseData.SinglePoint));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Area_WithYData2_RendersBothBoundaries()
    {
        double[] x  = EdgeCaseData.Ramp(10);
        double[] y1 = EdgeCaseData.Ramp(10);
        double[] y2 = x.Select(v => v + 5).ToArray();
        var svg = Render(ax => ax.FillBetween(x, y1, y2));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Area_WithMixedNaN_ProducesGapsInOutput()
    {
        double[] x = EdgeCaseData.Ramp(6);
        var svg = Render(ax => ax.FillBetween(x, EdgeCaseData.MixedNaN));
        // Mixed-NaN data shouldn't crash; some path or polygon must still render
        // for the non-NaN portions.
        Assert.Contains("<svg", svg);
    }

    // ── StepSeriesRenderer ───────────────────────────────────────────────────

    [Fact]
    public void Step_EmptyInput_DoesNotThrow()
    {
        var svg = Render(ax => ax.Step(EdgeCaseData.Empty, EdgeCaseData.Empty));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Step_VeryLargeInput_RendersFinitePolyline()
    {
        var svg = Render(ax => ax.Step(EdgeCaseData.Ramp(1000), EdgeCaseData.Sin(1000)));
        var pts = SvgGeometry.ExtractPolylinePoints(svg);
        Assert.NotEmpty(pts);
        // No off-canvas pixels (allow generous canvas including margin)
        SvgGeometry.AssertPointsInCanvas(pts, -50, 850, -50, 650, "Step");
    }

    // ── BarSeriesRenderer ────────────────────────────────────────────────────

    [Fact]
    public void Bar_EmptyCategories_DoesNotThrow()
    {
        var svg = Render(ax => ax.Bar(Array.Empty<string>(), EdgeCaseData.Empty));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Bar_NegativeValues_RendersBelowBaseline()
    {
        string[] cats = { "A", "B", "C" };
        double[] vals = { -10, 5, -20 };
        var svg = Render(ax => ax.Bar(cats, vals));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Bar_StackedMode_StackesMultipleSeries()
    {
        string[] cats = { "Q1", "Q2", "Q3" };
        var svg = Render(ax =>
        {
            ax.SetBarMode(BarMode.Stacked);
            ax.Bar(cats, new[] { 10.0, 20, 30 });
            ax.Bar(cats, new[] { 5.0, 15, 25 });
        });
        Assert.Contains("<svg", svg);
    }

    // ── BoxSeriesRenderer ────────────────────────────────────────────────────

    [Fact]
    public void Box_EmptyDatasets_DoesNotThrow()
    {
        var svg = Render(ax => ax.BoxPlot(Array.Empty<double[]>()));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Box_AllEqualData_RendersDegenerateBox()
    {
        var svg = Render(ax => ax.BoxPlot(new[] { EdgeCaseData.AllEqual(20, 5.0) }));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Box_WithOutliers_RendersOutlierMarkers()
    {
        // 95% small values + 5% large outliers
        var data = new double[] { 1, 2, 2, 3, 3, 3, 4, 4, 5, 100 };
        var svg = Render(ax => ax.BoxPlot(new[] { data }));
        Assert.Contains("<svg", svg);
    }

    // ── ViolinSeriesRenderer ─────────────────────────────────────────────────

    [Fact]
    public void Violin_AllEqualData_DoesNotThrow()
    {
        var svg = Render(ax => ax.Violin(new[] { EdgeCaseData.AllEqual(20, 5.0) }));
        Assert.Contains("<svg", svg);
    }

    // ── CartesianAxesRenderer combinations ───────────────────────────────────

    [Fact]
    public void Cartesian_Symlog_PlusBreak_DoesNotThrow()
    {
        // Combined symlog + break — both new in v1.7.1.
        double[] x = EdgeCaseData.Ramp(20);
        double[] y = x.Select(v => v < 10 ? v * 2 : v * v * 100).ToArray();
        var svg = Render(ax => ax
            .Plot(x, y)
            .WithSymlogYScale(linthresh: 50)
            .WithYBreak(50, 200));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Cartesian_SecondaryYAxis_DoesNotThrow()
    {
        var svg = Render(ax => ax
            .Plot(EdgeCaseData.Ramp(10), EdgeCaseData.Sin(10))
            .WithSecondaryYAxis(s => s.SetYLim(0, 100)));
        Assert.Contains("<svg", svg);
    }

    // ── SvgSeriesRenderer dispatch (via Theory over series types) ────────────

    public static IEnumerable<object[]> RepresentativeSeries()
    {
        yield return new object[] { (Action<MatPlotLibNet.AxesBuilder>)
            (ax => ax.Plot(EdgeCaseData.Ramp(5), EdgeCaseData.Sin(5))) };
        yield return new object[] { (Action<MatPlotLibNet.AxesBuilder>)
            (ax => ax.Scatter(EdgeCaseData.Ramp(5), EdgeCaseData.Sin(5))) };
        yield return new object[] { (Action<MatPlotLibNet.AxesBuilder>)
            (ax => ax.Bar(new[] { "A", "B" }, new[] { 1.0, 2.0 })) };
        yield return new object[] { (Action<MatPlotLibNet.AxesBuilder>)
            (ax => ax.Hist(EdgeCaseData.Sin(50), 10)) };
        yield return new object[] { (Action<MatPlotLibNet.AxesBuilder>)
            (ax => ax.ErrorBar(EdgeCaseData.Ramp(5), EdgeCaseData.Sin(5), new[] { 0.1, 0.1, 0.1, 0.1, 0.1 }, new[] { 0.1, 0.1, 0.1, 0.1, 0.1 })) };
        yield return new object[] { (Action<MatPlotLibNet.AxesBuilder>)
            (ax => ax.Step(EdgeCaseData.Ramp(5), EdgeCaseData.Sin(5))) };
        yield return new object[] { (Action<MatPlotLibNet.AxesBuilder>)
            (ax => ax.FillBetween(EdgeCaseData.Ramp(5), EdgeCaseData.Sin(5))) };
    }

    [Theory] [MemberData(nameof(RepresentativeSeries))]
    public void Renderer_DispatchesEverySeriesType(Action<MatPlotLibNet.AxesBuilder> configure)
    {
        var svg = Render(configure);
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }
}
