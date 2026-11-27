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
///   - Smoothing path (monotone-cubic spline when Smooth=true and X.Length≥3)
///   - YData2 fill-between path (explicit lower envelope for filled area)
///   - Where predicate path (RegionsKept: a fill per region of the data, cut from the run's top; FillBetweenWhereTests holds the polygons)
///   - YData2 under a Where predicate (the region's own slice of the lower curve)
///   - Empty-data early return
/// All facts run a real Plt.Create() pipeline and assert SVG output is non-empty.</summary>
public class AreaSeriesRendererTests
{
    // Drawn under the library's own theme, whose area draws the line along its top (the matplotlib themes draw a fill as
    // matplotlib's fill_between does, without one: AreaFillTests).
    private static string Render(AreaSeries s) =>
        Plt.Create()
            .WithTheme(Theme.Default)
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Render_BasicArea_ProducesPolygon", () => Render(new AreaSeries([0.0, 1, 2, 3], [1.0, 4, 2, 5])), "<polygon"),
        // Smoothing branch. Smooth=true + ≥3 points triggers MonotoneCubicSpline.Interpolate, which expands the top edge to
        // SmoothResolution samples per segment. SVG should still render to a valid polygon.
        new DrawnToken("Render_SmoothEnabled_ExpandsTopEdgeViaSpline", () => Render(new AreaSeries([0.0, 1, 2, 3, 4], [1.0, 4, 2, 5, 3])
        {
            Smooth = true,
            SmoothResolution = 10
        }), "<polygon"),
        // Smooth=true with insufficient points (X.Length < 3) — short-circuit arm of the `series.Smooth && top.X.Length >= 3` guard.
        // The smoothing block is skipped; the renderer falls through to the basic polygon path.
        new DrawnToken("Render_SmoothEnabled_TwoPoints_SkipsSmoothing", () => Render(new AreaSeries([0.0, 1.0], [1.0, 4.0]) { Smooth = true }), "<polygon"),
        // YData2 fill-between branch. When YData2 is supplied, the renderer builds the lower envelope from YData2 instead of
        // a y=0 baseline. Tests the `series.YData2 is not null` true arm.
        new DrawnToken("Render_WithYData2_BuildsFillBetweenPolygon", () => Render(new AreaSeries([0.0, 1, 2, 3], [3.0, 4, 5, 6])
        {
            YData2 = [1.0, 1.5, 2.0, 2.5]
        }), "<polygon"),
        // Explicit FillColor arm (`series.FillColor ?? ApplyAlpha(...)`) bypasses the alpha computation when FillColor is
        // non-null.
        new DrawnToken("Render_ExplicitFillColor_UsesItDirectly", () => Render(new AreaSeries([0.0, 1, 2], [1.0, 2, 1])
        {
            FillColor = new Color(255, 100, 50, 200),
            Color = Colors.Blue
        }), "<polygon"),
        // Explicit EdgeColor arm (`series.EdgeColor ?? color`).
        new DrawnToken("Render_ExplicitEdgeColor_UsesItForBoundary", () => Render(new AreaSeries([0.0, 1, 2], [1.0, 2, 1])
        {
            EdgeColor = new Color(0, 0, 0, 255)
        }), "<polyline"),
        // Where predicate branch — splits into contiguous fill segments. The predicate `x > 1.5` keeps points 2/3/4 of
        // [1,2,3,4,5] and produces ONE contiguous segment polygon plus the unmasked top-edge polyline.
        // a region's polygon
        new DrawnToken("Render_WithWherePredicate_SplitsIntoSegments", () =>
        {
            var s = new AreaSeries([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5])
            {
                Where = (x, y) => x > 1.5
            };
            return Render(s);
        }, "<polygon", "<polyline"),
        // Where predicate yielding TWO disjoint runs. Predicate `y < 3 || y > 4` produces 2 contiguous true-runs (indices 0-1 and 4); a
        // fill is drawn for each, each producing its own polygon.
        new DrawnToken("Render_WithWhere_TwoDisjointRuns_ProducesTwoSegments", () =>
        {
            var s = new AreaSeries([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5])
            {
                Where = (x, y) => y < 3 || y > 4
            };
            return Render(s);
        }, "<polygon"),
        // The YData2 path of a region: when YData2 is set AND a Where predicate is also active, the segment uses its slice of YData2 for
        // the lower envelope.
        new DrawnToken("Render_WithWhereAndYData2_SegmentUsesY2Slice", () =>
        {
            var s = new AreaSeries([1.0, 2, 3, 4, 5], [3.0, 4, 5, 6, 7])
            {
                YData2 = [1.0, 1.5, 2.0, 2.5, 3.0],
                Where = (x, y) => x >= 2 && x <= 4
            };
            return Render(s);
        }, "<polygon"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>What <c>Stairs</c> with a baseline relies on: an area whose top edge is a staircase (two points for each bin, a
    /// missing value for a bin that is missing) is filled once and stroked once for each unbroken run. The fill is not outlined
    /// (so no edge of the staircase is drawn twice) and the one stroke follows the top edge only.</summary>
    [Fact]
    public void Render_AStaircaseWithAGap_FillsAndStrokesEachRunOnce()
    {
        var svg = Render(new AreaSeries([0.0, 1, 1, 2, 2, 3], [5.0, 5, double.NaN, double.NaN, 7, 7]));

        Assert.Equal(2, svg.Elements("polygon").Count);
        Assert.Equal(2, svg.Elements("polyline").Count);
        svg.AssertNoNonFiniteNumber();
    }

    /// <summary>The same staircase over a raised baseline (a second curve of the same length): the gap splits the fill there
    /// too, and the baseline is the lower edge of each run.</summary>
    [Fact]
    public void Render_AStaircaseWithAGapOverARaisedBaseline_FillsAndStrokesEachRunOnce()
    {
        var svg = Render(new AreaSeries([0.0, 1, 1, 2, 2, 3], [5.0, 5, double.NaN, double.NaN, 7, 7])
        {
            YData2 = [2.0, 2, 2, 2, 2, 2]
        });

        Assert.Equal(2, svg.Elements("polygon").Count);
        Assert.Equal(2, svg.Elements("polyline").Count);
        svg.AssertNoNonFiniteNumber();
    }
}
