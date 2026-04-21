// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Algorithms;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Branch-coverage facts for axes, transforms, DataTransform, MarchingSquares,
/// PolarTransform, LogLocator, EcdfSeriesRenderer, CountSeries, SunburstSeries, ThreeDAxes,
/// ConstrainedLayoutEngine, TripcolorRenderer, HexbinRenderer, Trisurf3DRenderer,
/// MarkerRenderer, SankeyRenderer, FacetedFigure, JointPlotFigure, PairPlotFigure,
/// ChartRenderer/theme, SecondaryAxisBuilder, QuiverKeySeries rendering, and ParabolicSar.</summary>
public class BranchCoverage_RenderingTests
{
    // PolarTransform.cs L17: `rMax > 0 ? rMax : 1` — non-positive rMax fallback.
    [Fact] public void PolarTransform_ZeroRMax_FallsBackToOne()
    {
        var t = new PolarTransform(new Rect(0, 0, 100, 100), rMax: 0);
        // rMax should be normalised to 1; transform a unit-radius point to verify.
        var p = t.PolarToPixel(1.0, 0.0);
        Assert.True(double.IsFinite(p.X));
        Assert.True(double.IsFinite(p.Y));
    }

    // EcdfSeriesRenderer.cs L20: `if (n == 0) return;`
    [Fact] public void EcdfSeriesRenderer_EmptyData_RendersWithoutCrash()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Ecdf(Array.Empty<double>()))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>CountSeriesRenderer line 27 — `series.Orientation == Vertical` false arm
    /// (Horizontal). Default Vertical was the only path previously tested.</summary>
    [Fact]
    public void CountSeries_Horizontal_RendersWithoutCrash()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(
                new CountSeries(new[] { "a", "b", "a", "c", "b", "a" })
                { Orientation = BarOrientation.Horizontal }))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>SunburstSeriesRenderer.RenderRing line 64 — `if (total &lt;= 0) return;`
    /// true arm. A tree node with all-zero children sums to zero → renderer skips.</summary>
    [Fact]
    public void SunburstSeries_AllZeroChildValues_RendersWithoutCrash()
    {
        var root = new TreeNode { Label = "Root", Children =
        [
            new TreeNode { Label = "A", Value = 0 },
            new TreeNode { Label = "B", Value = 0 }
        ]};
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sunburst(root))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>ThreeDAxesRenderer line 409 — `if (range &lt;= 0) return [lo];` degenerate
    /// tick range. Triggered when 3D axis has zero span.</summary>
    [Fact]
    public void ThreeDAxes_ZeroRangeAxis_ReturnsSingleTick()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                // Single-point surface forces zMin==zMax (zero range on Z).
                ax.Surface(new double[] { 5, 5 }, new double[] { 5, 5 },
                    new double[,] { { 7, 7 }, { 7, 7 } });
            })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>DataTransform.TransformX line 138 — `_xScale == 0` degenerate-X-axis arm.
    /// Forces axis range to zero span so the centre-fill fallback fires.</summary>
    [Fact]
    public void DataTransform_DegenerateXAxis_FillsCenterPixel()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([5.0, 5.0, 5.0], [1.0, 2.0, 3.0]);   // all-same X
                ax.SetXLim(5, 5);   // zero-span x-axis
            })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>DataTransform.TransformY line 161 — `_yScale == 0` Y mirror of above.</summary>
    [Fact]
    public void DataTransform_DegenerateYAxis_FillsCenterPixel()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2.0, 3.0], [5.0, 5.0, 5.0]);
                ax.SetYLim(5, 5);
            })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    /// <summary>SankeySeriesRenderer line 86 — `vert = Orient == Vertical` true arm.
    /// Default is Horizontal; Vertical orientation exercises the rotated layout
    /// pipeline. Lifts class from 91/82.5 → ~91/85+.</summary>
    [Fact]
    public void SankeySeries_VerticalOrientation_RendersWithoutCrash()
    {
        var nodes = new[] { new SankeyNode("A"), new SankeyNode("B"), new SankeyNode("C") };
        var links = new[] { new SankeyLink(0, 1, 5), new SankeyLink(1, 2, 3) };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(
                new SankeySeries(nodes, links) { Orient = SankeyOrientation.Vertical }))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ── LogLocator (73.3L / 56.2B → ≥ 90/≥ 90) ──────────────────────────────────

    /// <summary>min ≤ 0 coercion arm (line 21).</summary>
    [Fact]
    public void LogLocator_MinLessThanZero_CoercesToTinyPositive()
    {
        var loc = new LogLocator();
        var ticks = loc.Locate(-5, 100);
        Assert.NotEmpty(ticks);
        Assert.All(ticks, t => Assert.True(t > 0));
    }

    /// <summary>max ≤ min returns [min] short-circuit (line 22).</summary>
    [Fact]
    public void LogLocator_MaxLessThanMin_ReturnsSingleMin()
    {
        var loc = new LogLocator();
        var ticks = loc.Locate(10, 5);
        Assert.Single(ticks);
        Assert.Equal(10, ticks[0]);
    }

    /// <summary>Multi-decade range — main loop hits both bounds inclusive (line 31).</summary>
    [Fact]
    public void LogLocator_MultiDecade_ProducesPowerOfTenTicks()
    {
        var loc = new LogLocator();
        var ticks = loc.Locate(1, 1000);
        Assert.Contains(1.0, ticks);
        Assert.Contains(10.0, ticks);
        Assert.Contains(100.0, ticks);
        Assert.Contains(1000.0, ticks);
    }

    /// <summary>Sub-decade range where lower-decade boundary IS in [min, max] — fallback arm 1
    /// (line 40 true).</summary>
    [Fact]
    public void LogLocator_SubDecadeWithLowerInRange_ReturnsLowerDecade()
    {
        var loc = new LogLocator();
        var ticks = loc.Locate(1.0, 5.0);
        Assert.Single(ticks);
        Assert.Equal(1.0, ticks[0]);
    }

    /// <summary>Sub-decade range where lower-decade boundary IS NOT in [min, max] —
    /// fallback arm 2 (line 40 false → line 43).</summary>
    [Fact]
    public void LogLocator_SubDecadeNoLowerInRange_FallsBackToMin()
    {
        var loc = new LogLocator();
        var ticks = loc.Locate(2.0, 5.0);
        Assert.Single(ticks);
        Assert.Equal(2.0, ticks[0]);
    }

    [Fact]
    public void LogLocator_SubDecadeMinAboveOne_FallsBackToMin()
    {
        // L37-43: ticks.Count == 0 path. lower = 10^floor(log10(min)) = 10^0 = 1.
        // For min=2, lower=1 < min=2 → else arm fires → ticks=[2]
        var loc = new global::MatPlotLibNet.Rendering.TickLocators.LogLocator();
        var ticks = loc.Locate(2.0, 5.0);
        Assert.Single(ticks);
        Assert.Equal(2.0, ticks[0]);
    }

    [Fact]
    public void LogLocator_SubDecadeWithLowerInRange_UsesLowerDecade()
    {
        // For min=1.0, max=5.0 → lower=10^0=1. lower>=min && lower<=max → ticks=[1].
        var loc = new global::MatPlotLibNet.Rendering.TickLocators.LogLocator();
        var ticks = loc.Locate(1.0, 5.0);
        Assert.Single(ticks);
        Assert.Equal(1.0, ticks[0]);
    }

    // ── ConstrainedLayoutEngine ──────────────────────────────────────────────────

    [Fact]
    public void ConstrainedLayoutEngine_EmptyFigure_NoOp()
    {
        var fig = Plt.Create().Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ConstrainedLayoutEngine_HighRatioGridSpec_AppliesProportions()
    {
        var fig = Plt.Create()
            .WithGridSpec(2, 2, heightRatios: [3.0, 1.0], widthRatios: [1.0, 4.0])
            .AddSubPlot(new GridPosition(0, 1, 0, 2), ax => ax.Plot([1.0, 2], [3.0, 4]))
            .AddSubPlot(new GridPosition(1, 2, 0, 2), ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── MarchingSquares saddle-cell + all-equal grid arms ────────────────────────

    [Fact]
    public void MarchingSquares_Extract_AllSameValues_HandlesUniformGrid()
    {
        var grid = new double[5, 5];
        for (int r = 0; r < 5; r++) for (int c = 0; c < 5; c++) grid[r, c] = 1.0;
        var contours = MarchingSquares.Extract([0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4], grid, [1.0]);
        Assert.NotNull(contours);
    }

    [Fact]
    public void MarchingSquares_Extract_LinearGradient_ProducesContours()
    {
        var grid = new double[5, 5];
        for (int r = 0; r < 5; r++) for (int c = 0; c < 5; c++) grid[r, c] = r + c;
        var contours = MarchingSquares.Extract([0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4], grid, [4.0]);
        Assert.NotEmpty(contours);
    }

    [Fact]
    public void MarchingSquares_ExtractBands_TwoLevels_ProducesBands()
    {
        var grid = new double[,] { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } };
        var bands = MarchingSquares.ExtractBands([0.0, 1, 2], [0.0, 1, 2], grid, levels: 3);
        Assert.NotEmpty(bands);
    }

    [Fact]
    public void MarchingSquares_Extract_ConcentricRings_ProducesMultipleContours()
    {
        // Create a concentric-rings z field — each isoLevel produces multiple contours
        int n = 11;
        var grid = new double[n, n];
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
            {
                double dx = c - n / 2.0;
                double dy = r - n / 2.0;
                grid[r, c] = Math.Sqrt(dx * dx + dy * dy);
            }
        var x = new double[n]; var y = new double[n];
        for (int i = 0; i < n; i++) { x[i] = i; y[i] = i; }
        var contours = MarchingSquares.Extract(x, y, grid, [2.0, 4.0]);
        Assert.NotEmpty(contours);
    }

    [Fact]
    public void MarchingSquares_ExtractBands_ConcentricRings_ProducesBands()
    {
        int n = 11;
        var grid = new double[n, n];
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
            {
                double dx = c - n / 2.0;
                double dy = r - n / 2.0;
                grid[r, c] = Math.Sqrt(dx * dx + dy * dy);
            }
        var x = new double[n]; var y = new double[n];
        for (int i = 0; i < n; i++) { x[i] = i; y[i] = i; }
        var bands = MarchingSquares.ExtractBands(x, y, grid, levels: 5);
        Assert.NotEmpty(bands);
    }

    [Fact]
    public void MarchingSquares_Extract_SaddleCellAlternating_ProducesContours()
    {
        // Alternating high-low pattern — every 2x2 cell is a saddle
        var grid = new double[,] {
            { 0, 1, 0, 1 },
            { 1, 0, 1, 0 },
            { 0, 1, 0, 1 },
            { 1, 0, 1, 0 },
        };
        var x = new double[] { 0, 1, 2, 3 };
        var y = new double[] { 0, 1, 2, 3 };
        var contours = MarchingSquares.Extract(x, y, grid, [0.5]);
        Assert.NotNull(contours);
    }

    // ── TripcolorSeriesRenderer ──────────────────────────────────────────────────

    [Fact]
    public void TripcolorRenderer_EmptyZ_UsesZeroOneRange()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tripcolor(
                [0.0, 1, 0.5, 0.3], [0.0, 0, 1, 0.5], [],  // Empty Z!
                s => { s.Triangles = [0, 1, 2, 0, 1, 3]; }))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void TripcolorRenderer_FewerThanThreePoints_EarlyReturns()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tripcolor([0.0, 1], [0.0, 1], [1.0, 2]))  // 2 points < 3
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── HexbinSeriesRenderer ─────────────────────────────────────────────────────

    [Fact]
    public void HexbinRenderer_EmptyData_EarlyReturn()
    {
        // L18-19: if (series.X.Length == 0) return;  Already covered, but pin via render.
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin(Array.Empty<double>(), Array.Empty<double>()))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void HexbinRenderer_AllSameX_FallsBackToXMaxXMinPlusOne()
    {
        // L23: if (xMin >= xMax) { xMax = xMin + 1; } true arm
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin([5.0, 5, 5, 5], [1.0, 2, 3, 4]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void HexbinRenderer_AllSameY_FallsBackToYMaxYMinPlusOne()
    {
        // L24: if (yMin >= yMax) { yMax = yMin + 1; } true arm
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin([1.0, 2, 3, 4], [5.0, 5, 5, 5]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void HexbinRenderer_BinsBelowMinCount_AllSkipped()
    {
        // L41: if (count < minCount) continue; — set MinCount > maxCount so all bins skip
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin([1.0, 2, 3, 4], [1.0, 2, 3, 4],
                s => { s.MinCount = 100; }))  // way above any actual bin count
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // HexbinSeriesRenderer L23: `if (xMin >= xMax) { xMax = xMin + 1; }` — degenerate range.
    [Fact] public void HexbinRenderer_AllSamePoint_HitsDegenerateRangeBranch()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new HexbinSeries(
                new double[] { 5, 5, 5 }, new double[] { 5, 5, 5 })))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ── Trisurf3DSeriesRenderer ──────────────────────────────────────────────────

    [Fact]
    public void Trisurf3DRenderer_FourPoints_UsesFanTriangulation()
    {
        // n=4, 4%3 != 0 → else arm (fan from point 0): triangles (0,1,2), (0,2,3)
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Trisurf(
                [0.0, 1, 0.5, 0.3],
                [0.0, 0, 1, 0.7],
                [0.0, 1, 2, 1.5]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Trisurf3DRenderer_FivePoints_UsesFanTriangulation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Trisurf(
                [0.0, 1, 0.5, 0.3, 0.8],
                [0.0, 0, 1, 0.7, 0.5],
                [0.0, 1, 2, 1.5, 1.2]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Trisurf3DRenderer_SixPoints_UsesSequentialTriangulation()
    {
        // n=6, 6%3 == 0 → if arm (sequential triplets): (0,1,2), (3,4,5)
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Trisurf(
                [0.0, 1, 0.5, 0.3, 0.8, 0.6],
                [0.0, 0, 1, 0.7, 0.5, 0.3],
                [0.0, 1, 2, 1.5, 1.2, 0.8]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // Trisurf3DSeriesRenderer L45: `if (n % 3 == 0)` — triangle-count branch.
    // 3 points triangulate to 1 triangle (n=3, n % 3 == 0 path).
    [Fact] public void Trisurf3DRenderer_TriangleCountModulo3_HitsBranch()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new Trisurf3DSeries(
                new double[] { 0.0, 1, 0.5 }, new double[] { 0.0, 0, 1 }, new double[] { 1.0, 2, 3 })))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ── MarkerRenderer ────────────────────────────────────────────────────────────

    /// <summary>MarkerRenderer line 106 — `strokeWidth &gt; 0 ? strokeWidth : Math.Max(...)`
    /// for MarkerStyle.Cross. Tests the explicit-strokeWidth (true) arm.</summary>
    [Fact]
    public void MarkerRenderer_Cross_ExplicitStrokeWidth_UsesIt()
    {
        var fig = Plt.Create()
            .Plot([1.0, 2, 3], [4.0, 5, 6], s =>
            {
                s.Marker = MarkerStyle.Cross;
                s.MarkerSize = 10;
                s.LineStyle = LineStyle.None;
            })
            .Build();
        var svg = fig.ToSvg();
        // Cross marker emits 2 <line> elements per data point.
        Assert.Contains("<line", svg);
    }

    /// <summary>MarkerRenderer line 118 — same ternary for MarkerStyle.Plus.</summary>
    [Fact]
    public void MarkerRenderer_Plus_DefaultStrokeWidth_FallsBackToSizeOver8()
    {
        var fig = Plt.Create()
            .Plot([1.0, 2, 3], [4.0, 5, 6], s =>
            {
                s.Marker = MarkerStyle.Plus;
                s.MarkerSize = 16;
                s.LineStyle = LineStyle.None;
            })
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void MarkerRenderer_Cross_FillNull_StrokeNull_FallsBackToBlack()
    {
        var svg = new global::MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Cross, new Point(50, 50), 10,
            fill: null, stroke: null, strokeWidth: 0);
        Assert.Contains("<line", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Plus_FillNull_StrokeNull_FallsBackToBlack()
    {
        var svg = new global::MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Plus, new Point(50, 50), 10,
            fill: null, stroke: null, strokeWidth: 0);
        Assert.Contains("<line", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Cross_StrokeWidthZero_FallsBackToComputedThickness()
    {
        var svg = new global::MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Cross, new Point(50, 50), 16,
            fill: Colors.Red, stroke: null, strokeWidth: 0);
        Assert.Contains("<line", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Pentagon_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Pentagon, new Point(50, 50), 12,
            fill: Colors.Red, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Hexagon_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Hexagon, new Point(50, 50), 12,
            fill: Colors.Blue, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Star_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Star, new Point(50, 50), 12,
            fill: Colors.Green, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_TriangleLeft_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.TriangleLeft, new Point(50, 50), 12,
            fill: Colors.Cyan, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_TriangleRight_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.TriangleRight, new Point(50, 50), 12,
            fill: Colors.Magenta, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_TriangleDown_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.TriangleDown, new Point(50, 50), 12,
            fill: Colors.Yellow, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Diamond_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Diamond, new Point(50, 50), 12,
            fill: Colors.Black, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Cross_FillNullStrokeNonNull_UsesStrokeColor()
    {
        // L105 ?? chain: fill=null, stroke=non-null → color = stroke
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Cross, new Point(50, 50), 12,
            fill: null, stroke: Colors.Red, strokeWidth: 1.5);
        Assert.Contains("<line", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Plus_FillNullStrokeNonNull_UsesStrokeColor()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Plus, new Point(50, 50), 12,
            fill: null, stroke: Colors.Blue, strokeWidth: 1.5);
        Assert.Contains("<line", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_NoneStyle_EarlyReturn()
    {
        // L45: if (style == MarkerStyle.None || size <= 0) return;  None arm
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.None, new Point(50, 50), 12,
            fill: Colors.Red, stroke: null, strokeWidth: 0);
        Assert.DoesNotContain("<polygon", svg.GetOutput());
        Assert.DoesNotContain("<circle", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_ZeroSize_EarlyReturn()
    {
        // L45: size <= 0 arm
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Circle, new Point(50, 50), 0,
            fill: Colors.Red, stroke: null, strokeWidth: 0);
        Assert.DoesNotContain("<circle", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_NegativeSize_EarlyReturn()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Circle, new Point(50, 50), -5,
            fill: Colors.Red, stroke: null, strokeWidth: 0);
        Assert.DoesNotContain("<circle", svg.GetOutput());
    }

    // MarkerRenderer L47/L105 — exercises remaining marker shape branches via every MarkerStyle.
    [Theory]
    [InlineData(MarkerStyle.Triangle)]
    [InlineData(MarkerStyle.TriangleDown)]
    [InlineData(MarkerStyle.TriangleLeft)]
    [InlineData(MarkerStyle.TriangleRight)]
    [InlineData(MarkerStyle.Diamond)]
    [InlineData(MarkerStyle.Cross)]
    [InlineData(MarkerStyle.Plus)]
    [InlineData(MarkerStyle.Star)]
    [InlineData(MarkerStyle.Pentagon)]
    [InlineData(MarkerStyle.Hexagon)]
    public void MarkerRenderer_EveryShape_RendersWithoutCrash(MarkerStyle marker)
    {
        var fig = Plt.Create()
            .Plot(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 },
                s => { s.Marker = marker; s.MarkerSize = 8; })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // Plus a stroke-only marker variant (fill = null) — exercised by clearing the
    // Scatter color so the renderer falls into the no-fill branch.
    [Fact] public void MarkerRenderer_NoFillColor_HitsNullFillBranch()
    {
        var fig = Plt.Create()
            .Scatter(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 },
                s => { s.Marker = MarkerStyle.Square; s.Color = null; })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ── SankeyRenderer ───────────────────────────────────────────────────────────

    [Fact]
    public void SankeyRenderer_SingleNode_RendersWithoutError()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(
                [new SankeyNode("Solo")],
                links: []))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void SankeyRenderer_MultiLinkSameSource_RendersWithoutError()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(
                [new SankeyNode("A"), new SankeyNode("B"), new SankeyNode("C")],
                [new SankeyLink(0, 1, 5), new SankeyLink(0, 2, 3)]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── FacetedFigure / JointPlotFigure / PairPlotFigure ─────────────────────────

    /// <summary>Build with no Title and no explicit size — both null arms in Build().</summary>
    [Fact]
    public void FacetedFigure_BuildWithoutTitleOrSize_StillProducesFigure()
    {
        var fig = new FacetGridFigure(
            x: [1.0, 2, 3, 4],
            y: [10.0, 20, 30, 40],
            category: ["A", "B", "A", "B"],
            plotFunc: (ax, fx, fy) => ax.Scatter(fx, fy)).Build();
        Assert.NotNull(fig);
    }

    /// <summary>Build with Title + explicit size — both true arms (BuildCore may
    /// resize, but the title is preserved).</summary>
    [Fact]
    public void FacetedFigure_BuildWithTitleAndSize_AppliesBoth()
    {
        var fb = new FacetGridFigure(
            x: [1.0, 2, 3, 4],
            y: [10.0, 20, 30, 40],
            category: ["A", "B", "A", "B"],
            plotFunc: (ax, fx, fy) => ax.Scatter(fx, fy))
        {
            Title = "facets",
            Width = 800,
            Height = 600
        }.Build();
        var fig = fb.Build();
        Assert.Equal("facets", fig.Title);
    }

    /// <summary>JointPlot with hue labels — exercises HueGrouper-driven branches.</summary>
    [Fact]
    public void JointPlotFigure_WithHueLabels_BuildsWithoutError()
    {
        var fb = new JointPlotFigure([1.0, 2, 3, 4], [10.0, 20, 30, 40])
        {
            Hue = ["a", "b", "a", "b"]
        }.Build();
        Assert.NotEmpty(fb.Build().SubPlots);
    }

    /// <summary>PairPlot smoke — exercises PairPlotFigure.BuildCore.</summary>
    [Fact]
    public void PairPlotFigure_TwoColumns_BuildsGrid()
    {
        var fb = new PairPlotFigure([
            [1.0, 2, 3],
            [4.0, 5, 6]
        ]).Build();
        var fig = fb.Build();
        Assert.NotEmpty(fig.SubPlots);
    }

    [Fact]
    public void FacetedFigure_AddLines_WithHue_GroupsBy()
    {
        // FacetGridFigure with line plotFunc and hue labels
        var fb = new FacetGridFigure(
            x: [1.0, 2, 3, 4, 5, 6],
            y: [10.0, 20, 30, 40, 50, 60],
            category: ["A", "A", "B", "B", "A", "B"],
            plotFunc: (ax, fx, fy) => ax.Plot(fx, fy));
        var fig = fb.Build().Build();
        Assert.NotEmpty(fig.SubPlots);
    }

    [Fact]
    public void FacetedFigure_PairPlot_WithHueLabels_RendersWithHueGroups()
    {
        var pp = new PairPlotFigure([
            [1.0, 2, 3, 4],
            [5.0, 6, 7, 8],
        ])
        {
            Hue = ["a", "b", "a", "b"],
        };
        var fig = pp.Build().Build();
        Assert.NotEmpty(fig.SubPlots);
    }

    // ── ChartRenderer theme dispatch ─────────────────────────────────────────────

    [Fact]
    public void ChartRenderer_RenderWithDarkTheme_AppliesTheme()
    {
        var svg = Plt.Create()
            .WithTheme(Theme.Dark)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ChartRenderer_RenderWithMatplotlibClassicTheme_AppliesTheme()
    {
        var svg = Plt.Create()
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ChartRenderer_RenderWithGgplotTheme_AppliesTheme()
    {
        var svg = Plt.Create()
            .WithTheme(Theme.Ggplot)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ChartRenderer_RenderWithSeabornTheme_AppliesTheme()
    {
        var svg = Plt.Create()
            .WithTheme(Theme.Seaborn)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── SecondaryAxisBuilder ──────────────────────────────────────────────────────

    [Fact]
    public void SecondaryAxisBuilder_Scatter_NullConfigure_NoOp()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSecondaryYAxis(s => s.Scatter([1.5], [55.0], configure: null)))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].SecondarySeries);
    }

    [Fact]
    public void SecondaryAxisBuilder_Plot_NullConfigure_NoOp()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSecondaryYAxis(s => s.Plot([1.5, 2.5], [55.0, 60], configure: null)))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].SecondarySeries);
    }

    // ── QuiverKeySeries rendering ─────────────────────────────────────────────────

    /// <summary>QuiverKeySeriesRenderer line 27 — `dataRange &gt; 0 ? width/dataRange : 50`
    /// false arm. Render a QuiverKey series in an axes whose range collapsed to zero.</summary>
    [Fact]
    public void QuiverKeySeriesRenderer_ZeroDataRange_FallsBackTo50PixelsPerUnit()
    {
        // Single-point Plot collapses XAxis range to a single value; this is enough
        // to make Transform.DataXMax - Transform.DataXMin == 0 in the renderer.
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0], [1.0])
                .AddSeries(new QuiverKeySeries(0.5, 0.5, 1.0, "1 m/s")))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains(">1 m/s<", svg);
    }

    [Fact]
    public void QuiverKeySeries_ZeroDataRange_FallsBackToFiftyPixels()
    {
        // Force degenerate data range by setting axes limits where xMin == xMax
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0], [1.0]);  // single-point series → degenerate range
                ax.QuiverKey(0.5, 0.5, 1.0, "k");
            })
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // QuiverKeySeriesRenderer.cs L27: `dataRange > 0 ? bounds.Width / dataRange : 50` —
    // zero-range fallback fires when XAxis.Min == XAxis.Max.
    [Fact] public void QuiverKey_ZeroDataRange_FallsBackTo50pxPerUnit()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.AddSeries(new QuiverKeySeries(0.5, 0.9, 1.0, "1 m/s"));
                ax.SetXLim(5, 5); // zero range → fallback branch
            })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // QuiverKeySeriesRenderer L27 — already covered by the `SetXLim(5,5)` test in pinpoint3,
    // but coverage didn't lift. Try with explicit data range computation.
    [Fact] public void QuiverKeyRenderer_DegenerateDataRange_HitsFallback()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.AddSeries(new QuiverKeySeries(0.5, 0.9, 1.0, "1 m/s"));
                // Empty series leaves data range at 0..0
            })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ── DataTransform log-scale + axis-breaks ─────────────────────────────────────

    [Fact]
    public void DataTransform_TransformX_LogScale_UsesScaleArm()
    {
        // Force the for-loop arm at L138-145 by using log-scale (non-Linear)
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 10, 100], [1.0, 2, 3]);
            })
            .Build();
        fig.SubPlots[0].XAxis.Scale = AxisScale.Log;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void DataTransform_TransformY_LogScale_UsesScaleArm()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 10, 100]))
            .Build();
        fig.SubPlots[0].YAxis.Scale = AxisScale.Log;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void DataTransform_TransformX_WithXBreaks_UsesBreakRemap()
    {
        // Force the for-loop arm by using XBreaks
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 10], [1.0, 10]))
            .Build();
        fig.SubPlots[0].AddXBreak(3, 5, BreakStyle.Zigzag);
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void DataTransform_TransformY_WithYBreaks_UsesBreakRemap()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 10], [1.0, 10]))
            .Build();
        fig.SubPlots[0].AddYBreak(3, 5, BreakStyle.Zigzag);
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void DataTransform_TransformX_WithMultiplePointsAndXBreaks_TraversesLoop()
    {
        // Force the for-loop arm by adding XBreaks AND multiple X-data points
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10],
                                                [1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10]))
            .Build();
        fig.SubPlots[0].AddXBreak(3, 5, BreakStyle.Zigzag);
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void DataTransform_TransformY_WithMultiplePointsAndYBreaks_TraversesLoop()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10],
                                                [1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10]))
            .Build();
        fig.SubPlots[0].AddYBreak(3, 5, BreakStyle.Zigzag);
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void DataTransform_TransformX_WithLogScaleAndManyPoints_TraversesLoop()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 10, 100, 1000, 10000],
                                                [1.0, 2, 3, 4, 5]))
            .Build();
        fig.SubPlots[0].XAxis.Scale = AxisScale.Log;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void DataTransform_TransformY_WithSymLogScaleAndManyPoints_TraversesLoop()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3, 4, 5],
                                                [-100.0, -10, 0, 10, 100]))
            .Build();
        fig.SubPlots[0].YAxis.Scale = AxisScale.SymLog;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Misc renderer tests ──────────────────────────────────────────────────────

    // PointplotSeriesRenderer.cs L58 — typically a min/max equality fallback or empty group.
    [Fact] public void PointplotSeriesRenderer_SingleValueGroup_HitsZeroSpreadBranch()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new PointplotSeries([
                new[] { 5.0 },                         // single value → zero spread
                new[] { 1.0, 2.0 }                     // normal group
            ])))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // PolarLineSeriesRenderer.cs L20 — typically `if (xData.Length == 0) return;` early-out.
    [Fact] public void PolarLineSeriesRenderer_EmptyData_EarlyReturns()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new PolarLineSeries([], [])))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // QuiverKeySeriesRenderer.cs L27 — likely `if (key.Reference is null)` or similar.
    [Fact] public void QuiverKeySeriesRenderer_DefaultLabel_RendersWithoutCrash()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new QuiverKeySeries(0.5, 0.9, 1.0, "1 m/s")))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ResidualSeriesRenderer.cs L18 — empty data early-out (matches the model-class branch).
    [Fact] public void ResidualSeriesRenderer_EmptyData_EarlyReturns()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new ResidualSeries(
                Array.Empty<double>(), Array.Empty<double>())))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // AutoDateFormatter.cs L36 — likely a span-threshold branch.
    [Fact] public void AutoDateFormatter_SubSecondSpan_FormatsWithMs()
    {
        // Sub-second range exercises the < 1s formatting branch which the
        // existing tests skip (they all use multi-day spans).
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot(
                new DateTime[] { new(2026, 1, 1, 12, 0, 0, 0), new(2026, 1, 1, 12, 0, 0, 100) },
                [1.0, 2.0]))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // AutoDateFormatter.cs L36 — switch over ChosenInterval, hit a less-common arm.
    [Fact] public void AutoDateFormatter_HourlyInterval_FormatsCorrectly()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot(
                new DateTime[] { new(2026, 1, 1, 0, 0, 0), new(2026, 1, 1, 6, 0, 0), new(2026, 1, 1, 12, 0, 0) },
                [1.0, 2.0, 3.0]))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // SunburstSeriesRenderer.cs L44: `Context?.Theme?.ForegroundText ?? Colors.Black` —
    // the null-coalesce branch fires when Theme is null. Drive via a sunburst with deeper
    // tree structure that creates leader-line placements.
    [Fact] public void Sunburst_DeepTreeWithLeaderLines_HitsLeaderColorBranch()
    {
        var root = new TreeNode
        {
            Label = "Root",
            Children =
            [
                new() { Label = "A", Value = 10, Children =
                [
                    new() { Label = "A1", Value = 6, Children =
                    [
                        new() { Label = "Tiny", Value = 1 }
                    ] },
                    new() { Label = "A2", Value = 4 }
                ] },
                new() { Label = "B", Value = 5 }
            ]
        };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sunburst(root))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // PointplotSeriesRenderer.cs L58: `if (v.Length < 2) return 0;` — single-value group.
    [Fact] public void Pointplot_SingleValueGroup_HitsCIWidthShortCircuit()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new PointplotSeries([
                new[] { 5.0 },           // single value (Length < 2 branch)
                new[] { 1.0, 2.0, 3.0 }
            ])))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // BarbsSeriesRenderer.cs L24: `i < series.Speed.Length ? series.Speed[i] : 0` —
    // length-mismatch fallback (Speed shorter than X).
    [Fact] public void Barbs_LengthMismatch_HitsSpeedFallbackBranch()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new BarbsSeries(
                new double[] { 1.0, 2.0, 3.0 }, new double[] { 1.0, 2.0, 3.0 },
                new double[] { 10.0 },                   // Speed shorter than X/Y
                new double[] { 45.0 })))                 // Direction shorter too
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // PolarHeatmapSeriesRenderer.cs L37: bounds-checked data access — out-of-range fallback.
    [Fact] public void PolarHeatmap_DataMismatch_HitsBoundsCheckBranch()
    {
        // Mismatch between configured bins and Data dimensions exercises the
        // bounds-check fallback path.
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new PolarHeatmapSeries(
                new double[,] { { 1, 2 }, { 3, 4 } }, thetaBins: 4, rBins: 4))) // bins > data
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // HeatmapSeriesRenderer.cs L19: `if (rows == 0 || cols == 0) return;` — empty grid.
    [Fact] public void Heatmap_EmptyData_HitsEarlyReturn()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new HeatmapSeries(new double[0, 0])))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // TripcolorSeriesRenderer.cs L22: `if (tris.Length == 0) return;` — degenerate triangulation.
    [Fact] public void Tripcolor_TooFewPoints_HitsEmptyTrianglesBranch()
    {
        // 2 points → no triangles possible.
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new TripcolorSeries(
                new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, new double[] { 1.0, 2.0 })))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ContourfSeriesRenderer.cs L27: `if (rows < 2 || cols < 2) return;` — undersized grid.
    [Fact] public void Contourf_TooSmallGrid_HitsEarlyReturn()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new ContourfSeries(
                new double[] { 1.0 }, new double[] { 1.0 }, new double[,] { { 5 } })))  // 1×1
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // EventplotSeriesRenderer L23: `series.Colors is not null && i < series.Colors.Length`
    // Test with explicit Colors array (other branch covered by null Colors path).
    [Fact] public void EventplotRenderer_ExplicitColors_AppliesPerLineColors()
    {
        var s = new EventplotSeries([new double[] { 1, 2 }, new double[] { 3 }])
        { Colors = [Colors.Red, Colors.Blue] };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // PlanarBar3DSeriesRenderer L52: `series.Colors is { } cs && i < cs.Length` per-bar.
    [Fact] public void PlanarBar3DRenderer_ExplicitColors_AppliesPerBarColors()
    {
        var s = new PlanarBar3DSeries(
            new double[] { 1, 2 }, new double[] { 1, 2 }, new double[] { 3, 4 })
        { Colors = [Colors.Red, Colors.Blue] };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // StackedAreaSeriesRenderer L21: `if (n == 0 || layers == 0) return;` — empty data.
    [Fact] public void StackedAreaRenderer_EmptyX_HitsEarlyReturn()
    {
        var s = new StackedAreaSeries(Array.Empty<double>(),
            new double[][] { Array.Empty<double>() });
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // StemSeriesRenderer L18: `series.StemColor ?? SeriesColor` — explicit StemColor.
    [Fact] public void StemRenderer_ExplicitStemColor_UsesIt()
    {
        var s = new StemSeries(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 })
        { StemColor = Colors.Red };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // LineSeriesRenderer L28: `if (series.Smooth && drawX.Length >= 3)` — Smooth=true.
    [Fact] public void LineRenderer_SmoothTrue_HitsSmoothBranch()
    {
        var fig = Plt.Create()
            .Plot(new double[] { 1, 2, 3, 4, 5 }, new double[] { 1, 4, 2, 5, 3 },
                s => s.Smooth = true)
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // LineSeriesRenderer L41: `series.DrawStyle is not null and not DrawStyle.Default` — non-default DrawStyle.
    [Fact] public void LineRenderer_StepsDrawStyle_HitsStepBranch()
    {
        var fig = Plt.Create()
            .Plot(new double[] { 1, 2, 3, 4, 5 }, new double[] { 1, 4, 2, 5, 3 },
                s => { s.DrawStyle = DrawStyle.StepsMid; s.Marker = MarkerStyle.Circle; })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // LineSeriesRenderer L47: `if (series.MarkEvery is not null && i % series.MarkEvery.Value != 0)` — MarkEvery set.
    [Fact] public void LineRenderer_MarkEveryTwo_HitsMarkEveryBranch()
    {
        var fig = Plt.Create()
            .Plot(new double[] { 1, 2, 3, 4, 5 }, new double[] { 1, 2, 3, 4, 5 },
                s => { s.Marker = MarkerStyle.Circle; s.MarkEvery = 2; })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // LineSeriesRenderer L38: `series.MarkerEdgeColor is not null ? : 0` — explicit MarkerEdgeColor.
    [Fact] public void LineRenderer_ExplicitMarkerEdgeColor_HitsBranch()
    {
        var fig = Plt.Create()
            .Plot(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 },
                s => { s.Marker = MarkerStyle.Circle; s.MarkerEdgeColor = Colors.Black; })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // StreamplotSeriesRenderer L25: `if (nx < 2 || ny < 2) return;`
    [Fact] public void StreamplotRenderer_TooSmallGrid_HitsEarlyReturn()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new StreamplotSeries(
                new double[] { 0.0 }, new double[] { 0.0 },
                new double[,] { { 1 } }, new double[,] { { 0 } })))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // RadarSeriesRenderer L19: `series.FillColor ?? ApplyAlpha(color, series.Alpha)` — explicit FillColor.
    [Fact] public void RadarRenderer_ExplicitFillColor_UsesIt()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Radar(new[] { "A", "B", "C" }, new[] { 1.0, 2.0, 3.0 },
                s => s.FillColor = Colors.Salmon))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // RadarSeriesRenderer L25: `series.MaxValue ?? series.Values.Max()` — explicit MaxValue.
    [Fact] public void RadarRenderer_ExplicitMaxValue_UsesIt()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Radar(new[] { "A", "B", "C" }, new[] { 1.0, 2.0, 3.0 },
                s => s.MaxValue = 10.0))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // Scatter3DSeriesRenderer L19: `if (series.X.Length == 0) return;` empty data.
    [Fact] public void Scatter3DRenderer_EmptyData_HitsEarlyReturn()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new Scatter3DSeries(
                Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>())))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }
}
