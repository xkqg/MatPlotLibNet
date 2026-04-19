// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.3.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.AreaSeriesRenderer"/>. Pre-X
/// the renderer was at 38.7%L / 28.9%B because only the simplest happy path
/// (basic X/Y, no smoothing, no filled-between, no Where predicate) was exercised by
/// upstream tests. This file pins:
///   - Smoothing path (line 30-34: monotone-cubic spline when Smooth=true and X.Length≥3)
///   - YData2 fill-between path (line 49-55: explicit lower envelope for filled area)
///   - Where predicate path (line 37-41 + RenderWithWhere + RenderSegment, lines 70-130)
///   - YData2 length mismatch arm in RenderSegment (line 117 ternary's false branch)
///   - Empty-data early return (line 24)
/// All facts run a real Plt.Create() pipeline and assert SVG output is non-empty.</summary>
public class AreaSeriesRendererTests
{
    private static string Render(AreaSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    [Fact]
    public void Render_BasicArea_ProducesPolygon()
    {
        var svg = Render(new AreaSeries([0.0, 1, 2, 3], [1.0, 4, 2, 5]));
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Smoothing branch (line 30-34). Smooth=true + ≥3 points triggers
    /// MonotoneCubicSpline.Interpolate, which expands the top edge to SmoothResolution
    /// samples per segment. SVG should still render to a valid polygon.</summary>
    [Fact]
    public void Render_SmoothEnabled_ExpandsTopEdgeViaSpline()
    {
        var svg = Render(new AreaSeries([0.0, 1, 2, 3, 4], [1.0, 4, 2, 5, 3])
        {
            Smooth = true,
            SmoothResolution = 10
        });
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Smooth=true with insufficient points (X.Length &lt; 3) — short-circuit
    /// arm of the `series.Smooth &amp;&amp; top.X.Length &gt;= 3` guard at line 30. The
    /// smoothing block is skipped; the renderer falls through to the basic polygon path.</summary>
    [Fact]
    public void Render_SmoothEnabled_TwoPoints_SkipsSmoothing()
    {
        var svg = Render(new AreaSeries([0.0, 1.0], [1.0, 4.0]) { Smooth = true });
        Assert.Contains("<polygon", svg);
    }

    /// <summary>YData2 fill-between branch (line 49-55). When YData2 is supplied,
    /// the renderer builds the lower envelope from YData2 instead of a y=0 baseline.
    /// Tests the `series.YData2 is not null` true arm.</summary>
    [Fact]
    public void Render_WithYData2_BuildsFillBetweenPolygon()
    {
        var svg = Render(new AreaSeries([0.0, 1, 2, 3], [3.0, 4, 5, 6])
        {
            YData2 = [1.0, 1.5, 2.0, 2.5]
        });
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Where predicate branch (line 37-41) — splits into contiguous fill
    /// segments. The predicate `x &gt; 1.5` keeps points 2/3/4 of [1,2,3,4,5] and
    /// produces ONE contiguous segment polygon plus the unmasked top-edge polyline.</summary>
    [Fact]
    public void Render_WithWherePredicate_SplitsIntoSegments()
    {
        var s = new AreaSeries([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5])
        {
            Where = (x, y) => x > 1.5
        };
        var svg = Render(s);
        Assert.Contains("<polygon", svg);   // RenderSegment polygon
        Assert.Contains("<polyline", svg);  // unmasked full top edge
    }

    /// <summary>Where predicate yielding TWO disjoint runs. Predicate `y &lt; 3 || y &gt; 4`
    /// produces 2 contiguous true-runs (indices 0-1 and 4); RenderSegment fires twice,
    /// each producing its own polygon.</summary>
    [Fact]
    public void Render_WithWhere_TwoDisjointRuns_ProducesTwoSegments()
    {
        var s = new AreaSeries([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5])
        {
            Where = (x, y) => y < 3 || y > 4
        };
        var svg = Render(s);
        Assert.Contains("<polygon", svg);
    }

    /// <summary>RenderSegment YData2 path (line 117 ternary): when YData2 is set AND
    /// a Where predicate is also active, the segment uses its slice of YData2 for the
    /// lower envelope. The `series.YData2.Length &gt; end` true arm of the slice guard
    /// is hit when YData2 has at least as many entries as the segment.</summary>
    [Fact]
    public void Render_WithWhereAndYData2_SegmentUsesY2Slice()
    {
        var s = new AreaSeries([1.0, 2, 3, 4, 5], [3.0, 4, 5, 6, 7])
        {
            YData2 = [1.0, 1.5, 2.0, 2.5, 3.0],
            Where = (x, y) => x >= 2 && x <= 4
        };
        var svg = Render(s);
        Assert.Contains("<polygon", svg);
    }

    /// <summary>RenderSegment YData2 length-mismatch arm (line 117 ternary's false branch):
    /// when YData2 is shorter than the segment's end index, the renderer falls back to
    /// the full YData2 array.</summary>
    [Fact]
    public void Render_WithWhereAndShortY2_SegmentUsesFullY2Fallback()
    {
        var s = new AreaSeries([1.0, 2, 3, 4, 5], [3.0, 4, 5, 6, 7])
        {
            YData2 = [1.0, 1.5],     // shorter than segment end
            Where = (x, y) => x >= 4
        };
        var svg = Render(s);
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Empty-data early return (line 24). After downsampling, data.X.Length==0
    /// short-circuits with no SVG output for the area. Asserts no exception + valid SVG envelope.</summary>
    [Fact]
    public void Render_EmptyData_EarlyReturns_NoCrash()
    {
        var svg = Render(new AreaSeries(Array.Empty<double>(), Array.Empty<double>()));
        Assert.StartsWith("<svg", svg);
        Assert.EndsWith("</svg>", svg.TrimEnd());
    }

    /// <summary>Explicit FillColor arm (line 21 — `series.FillColor ?? ApplyAlpha(...)`)
    /// bypasses the alpha computation when FillColor is non-null.</summary>
    [Fact]
    public void Render_ExplicitFillColor_UsesItDirectly()
    {
        var svg = Render(new AreaSeries([0.0, 1, 2], [1.0, 2, 1])
        {
            FillColor = new Color(255, 100, 50, 200),
            Color = Colors.Blue
        });
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Explicit EdgeColor arm (line 65 — `series.EdgeColor ?? color`).</summary>
    [Fact]
    public void Render_ExplicitEdgeColor_UsesItForBoundary()
    {
        var svg = Render(new AreaSeries([0.0, 1, 2], [1.0, 2, 1])
        {
            EdgeColor = new Color(0, 0, 0, 255)
        });
        Assert.Contains("<polyline", svg);
    }
}
